using System.Drawing;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MapService.Controllers;

[ApiController]
[Route("[controller]")]
public class MapController : ControllerBase
{
    private readonly HttpClient _httpClient;
    public MapController(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
    }

    [HttpGet("ping")]
    public IActionResult Ping()
    {
        string message = "works! (mapgen)";
        return Ok(new
        {
            message
        });
    }
    
    private static readonly List<string> DifficultyOrder = new() { "very easy", "easy", "medium", "hard", "very hard" };
    private static readonly string[] ValidGameModes = { "1P", "2P", "3P", "4P" };
    private static readonly string[] ValidPlacementModes = { "random", "circular", "flipped" };

    private Dictionary<string, object>? GetConstraints(Constraints? requestConstraints, List<string> errors)
    {
        var constraintsMap = new Dictionary<string, object?>
        {
            { "min_padding", requestConstraints?.MinPadding },
            { "max_padding", requestConstraints?.MaxPadding },
            { "min_line_width", requestConstraints?.MinLineWidth },
            { "max_line_width", requestConstraints?.MaxLineWidth },
            { "min_border_width", requestConstraints?.MinBorderWidth },
            { "max_border_width", requestConstraints?.MaxBorderWidth },
            { "pixel_perfect", requestConstraints?.PixelPerfect },
            { "fill_board", requestConstraints?.FillBoard },
            { "center_board", requestConstraints?.CenterBoard }
        };

        var minMaxMap = new Dictionary<string, string>()
        {
            { "min_padding", "max_padding" },
            { "min_line_width", "max_line_width" },
            { "min_border_width", "max_border_width" }
        };

        var returnConstraints = new Dictionary<string, object>();

        foreach (var (name, value) in constraintsMap)
        {
            if (value == null)
            {
                continue;
            }
            
            if (value is int intVal)
            {
                if (intVal < 0)
                {
                    errors.Add($"Constraints: '{name}': Must be >= 0, got {intVal}");
                    continue;
                }
            }
            
            Console.WriteLine($"Added constraint: {name}, with value: {value}");
            returnConstraints.Add(name, value);
        }

        foreach (var (min, max) in minMaxMap)
        {
            if (!returnConstraints.ContainsKey(min) || !returnConstraints.ContainsKey(max))
                continue;
            
            if (returnConstraints[min] is int minValue && returnConstraints[max] is int maxValue)
            {
                if (minValue > maxValue)
                {
                    errors.Add($"Constraints: '{min}': Cannot be greater than '{max}' ({minValue} > {maxValue})");
                }
            }
        }
        
        if (returnConstraints.Count > 0)
        {
            return returnConstraints;
        }
        
        return null;
    }

    private Dictionary<string, string>? GetHexColors(Colors? requestColors, List<string> errors)
    {
        var colorMap = new Dictionary<string, string?>
        {
            { "background_color", requestColors?.BackgroundColor },
            { "foreground_color", requestColors?.ForegroundColor },
            { "line_color", requestColors?.LineColor },
            { "border_color", requestColors?.BorderColor }
        };

        var returnColors = new Dictionary<string, string>();
            
        foreach (var (name, value) in colorMap)
        {
            if (value == null)
            {
                continue;
            }
            try
            {
                var color = ColorTranslator.FromHtml(value);
                returnColors.Add(name, $"#{color.R:X2}{color.G:X2}{color.B:X2}");
            }
            catch
            {
                errors.Add($"Invalid color value '{value}' for color '{name}' provided.");
            }
        }

        if (returnColors.Count > 0)
        {
            return returnColors;
        }

        return null;
    }
    
    private bool IsValidVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return false;

        var parts = version.Split('.');

        // Version format: x.x or x.x.x
        if (parts.Length < 2 || parts.Length > 3)
            return false;

        foreach (var part in parts)
        {
            if (!int.TryParse(part, out var number))
                return false;

            if (number < 0)
                return false;
        }

        return true;
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateRequest request)
    {
        try
        {
            var mapgenErrors = new List<string>();
            
            // Grid Size
            int gridSize = request.GridSize ?? 5;
            if (gridSize < 1 || gridSize > 9)
                mapgenErrors.Add($"Invalid grid size {gridSize}. The grid size must be in the range of 1 and 9.");

            // Game Mode
            string gameMode = request.GameMode ?? "1P";
            bool validGameMode = ValidGameModes.Contains(gameMode, StringComparer.OrdinalIgnoreCase);
            if (!validGameMode)
                mapgenErrors.Add("Invalid game mode. Accepted values are 1P, 2P, 3P, or 4P.");

            int teamCount = 0;
            if (validGameMode && int.TryParse(gameMode[..1], out int firstDigit))
                teamCount = firstDigit;

            // Game Version
            string gameVersion = request.GameVersion ?? GetLatestGameVersion();
            if (!IsValidVersion(gameVersion))
                mapgenErrors.Add($"Invalid game version '{gameVersion}' provided.");
            else if (!VersionIsGreaterOrEqual(GetEarliestGameVersion(), gameVersion) || !VersionIsSmallerOrEqual(GetLatestGameVersion(), gameVersion))
                mapgenErrors.Add($"Specified game version '{gameVersion}' is unsupported. Supported versions are {GetEarliestGameVersion()}-{GetLatestGameVersion()}");
            
            // Team Names
            string[] teamList;
            if (string.IsNullOrWhiteSpace(request.TeamNames))
                teamList = Enumerable.Range(1, teamCount).Select(i => $"team_{i}").ToArray();
            else
                teamList = request.TeamNames
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(t => t.Trim())
                    .ToArray();

            var uniqueTeams = teamList.Distinct().ToList();
            if (teamList.Length != uniqueTeams.Count)
                mapgenErrors.Add("Duplicate team names are not allowed.");
            if (teamList.Length != teamCount)
                mapgenErrors.Add($"Expected {teamCount} team names for game mode {gameMode}, got {teamList.Length}.");

            // Placement Mode
            string placementMode = string.IsNullOrWhiteSpace(request.PlacementMode) ? "circular" : request.PlacementMode.ToLowerInvariant();
            if (!ValidPlacementModes.Contains(placementMode))
                mapgenErrors.Add($"Invalid placement mode {placementMode}. Valid values are: random, circular & flipped.");

            // Difficulty
            string difficultyInput = string.IsNullOrWhiteSpace(request.Difficulty) ? "easy,medium,hard" : request.Difficulty;
            var difficultyList = difficultyInput
                .Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(d => d.ToLower())
                .Distinct()
                .ToList();
            if (difficultyList.Contains("all"))
                difficultyList = DifficultyOrder.ToList();
            if (!difficultyList.All(d => DifficultyOrder.Contains(d)))
                mapgenErrors.Add($"Invalid difficulty value(s). Valid values are: {string.Join(", ", DifficultyOrder)} or all.");

            // Max Items Per Group / Material
            int maxItemsPerGroup = request.Constraints?.MaxItemsPerGroup ?? 1;
            int maxItemsPerMaterial = request.Constraints?.MaxItemsPerMaterial ?? 1;

            if (maxItemsPerGroup < 0 || maxItemsPerMaterial < 0)
                mapgenErrors.Add("Max items per group/material cannot be negative. Disable group/material check by setting it to 0.");

            // Constraints
            var constraints = GetConstraints(request.Constraints, mapgenErrors);

            // Colors
            var colors = GetHexColors(request.Colors, mapgenErrors);

            if (mapgenErrors.Any())
                return BadRequest(new { errors = new { mapgen = mapgenErrors } });

            var placements = GetPlacements(gameMode, teamList);

            var bingoItems = GetBingoItems();
            if (bingoItems.Count == 0)
                return StatusCode(500, new { errors = new { mapgen = "Failed to load valid bingo items!" } });

            var teams = new List<object>();

            foreach (var team in teamList)
            {
                teams.Add(new
                {
                    name = team,
                    placement = placements[team]
                });
            }

            var payload = new
            {
                settings = new { grid_size = gridSize, game_mode = gameMode, teams, constraints, colors },
                items = GenerateItems(gameVersion, gridSize, bingoItems, teamList, difficultyList, maxItemsPerGroup, maxItemsPerMaterial, placementMode)
            };

            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            var jsonPayload = JsonSerializer.Serialize(payload, options);
            var response = await _httpClient.PostAsync("http://imggen:5000/generate",
                new StringContent(jsonPayload, Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
            {
                var imggenErrorContent = await response.Content.ReadFromJsonAsync<JsonElement>();
                var imggenErrorMessage = imggenErrorContent.TryGetProperty("imggen", out var errorProp) ? errorProp.GetString() : "Unknown imggen error";

                return StatusCode((int)response.StatusCode, new
                {
                    errors = new
                    {
                        mapgen = "Failed to generate image!",
                        imggen = imggenErrorMessage
                    },
                    map_raw = payload
                });
            }

            var imggenResponse = await response.Content.ReadAsStringAsync();
            var mapUrl = JsonSerializer.Deserialize<JsonElement>(imggenResponse).GetProperty("url").GetString();

            return Content(JsonSerializer.Serialize(new { map_url = mapUrl, map_raw = payload }, options), "application/json");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { errors = new { mapgen = ex.Message, stack = ex.StackTrace }});
        }
    }

    private static readonly Lazy<BingoItemDto> CachedData = new(() =>
    {
        var json = System.IO.File.ReadAllText("items.json");
        return JsonSerializer.Deserialize<BingoItemDto>(json) ?? new BingoItemDto();
    });

    private List<BingoItem> GetBingoItems()
    {
        return CachedData.Value.Items;
    }
    
    private string GetEarliestGameVersion()
    {
        return CachedData.Value.EarliestGameVersion;
    }

    private string GetLatestGameVersion()
    {
        return CachedData.Value.LatestGameVersion;
    }

    private bool VersionIsSmallerOrEqual(string baseVersion, string inputVersion)
    {
        int[] baseParts = baseVersion.Split(".").Select(int.Parse).ToArray();
        int[] inputParts = inputVersion.Split(".").Select(int.Parse).ToArray();

        int maxLength = Math.Max(baseParts.Length, inputParts.Length);

        for (int i = 0; i < maxLength; i++)
        {
            int basePart = i < baseParts.Length ? baseParts[i] : 0;
            int inputPart = i < inputParts.Length ? inputParts[i] : 0;

            if (inputPart < basePart) return true;
            if (inputPart > basePart) return false;
        }

        return true;
    }
    
    private bool VersionIsGreaterOrEqual(string baseVersion, string inputVersion)
    {
        int[] baseParts = baseVersion.Split(".").Select(int.Parse).ToArray();
        int[] inputParts = inputVersion.Split(".").Select(int.Parse).ToArray();

        int maxLength = Math.Max(baseParts.Length, inputParts.Length);

        for (int i = 0; i < maxLength; i++)
        {
            int basePart = i < baseParts.Length ? baseParts[i] : 0;
            int inputPart = i < inputParts.Length ? inputParts[i] : 0;

            if (inputPart > basePart) return true;
            if (inputPart < basePart) return false;
        }

        return true;
    }
    
    private List<object> GenerateItems(string gameVersion, int gridSize, List<BingoItem> bingoItems, string[] teams, List<string> allowedDifficulties, int maxPerGroup, int maxPerMaterial, string placementMode)
    {
        var random = Random.Shared;
        var items = new List<object>();
        var selectedItems = new HashSet<string>();

        if (!ValidPlacementModes.Contains(placementMode))
            throw new ArgumentException("Placement mode must be 'random', 'circular' or 'flipped'.");
    
        var allowedIndexes = allowedDifficulties
            .Select(d => DifficultyOrder.IndexOf(d))
            .Where(i => i >= 0)
            .Distinct()
            .OrderBy(i => i)
            .ToList();
    
        if (allowedIndexes.Count == 0)
            throw new ArgumentException("No valid difficulties provided.");

        int minIndex = allowedIndexes.Min();
        int maxIndex = allowedIndexes.Max();
    
        int maxDistance = gridSize / 2;
        var groupCounts = new Dictionary<string, int>();
        var materialCounts = new Dictionary<string, int>();
    
        // ring-to-difficulty mapping for circular/flipped
        Dictionary<int, List<int>> ringDifficultyMap = new();
        if (placementMode == "circular" || placementMode == "flipped")
        {
            for (int ring = 0; ring <= maxDistance; ring++)
            {
                bool isCenter = ring == maxDistance;
                if (isCenter)
                {
                    int centerIndex = placementMode == "circular"
                        ? Math.Min(maxIndex + 1, DifficultyOrder.Count - 1) // hardest in center
                        : Math.Max(minIndex - 1, 0);                        // easiest in center
                    ringDifficultyMap[ring] = new List<int> { centerIndex };
                }
                else
                {
                    double fractionStart = (double)ring / maxDistance;
                    double fractionEnd = (double)(ring + 1) / maxDistance;
    
                    int startIdx, endIdx;
                    if (placementMode == "circular")
                    {
                        startIdx = (int)Math.Floor(fractionStart * (allowedIndexes.Count - 1));
                        endIdx = (int)Math.Ceiling(fractionEnd * (allowedIndexes.Count - 1));
                    }
                    else // flipped
                    {
                        startIdx = allowedIndexes.Count - 1 - (int)Math.Ceiling(fractionEnd * (allowedIndexes.Count - 1));
                        endIdx = allowedIndexes.Count - 1 - (int)Math.Floor(fractionStart * (allowedIndexes.Count - 1));
                    }
    
                    startIdx = Math.Clamp(startIdx, 0, allowedIndexes.Count - 1);
                    endIdx = Math.Clamp(endIdx, 0, allowedIndexes.Count - 1);
    
                    ringDifficultyMap[ring] = allowedIndexes
                        .Skip(Math.Min(startIdx, endIdx))
                        .Take(Math.Abs(endIdx - startIdx) + 1)
                        .ToList();
                }
            }
        }
    
        // grid generation
        for (int row = 0; row < gridSize; row++)
        {
            for (int column = 0; column < gridSize; column++)
            {
                string difficulty;
    
                if (placementMode == "random")
                {
                    int randomIndex = allowedIndexes[random.Next(allowedIndexes.Count)];
                    difficulty = DifficultyOrder[randomIndex];
                }
                else
                {
                    int ring = maxDistance - Math.Max(Math.Abs(row - maxDistance), Math.Abs(column - maxDistance));
                    var possibleIndexes = ringDifficultyMap[ring];
                    int chosenIndex = possibleIndexes[random.Next(possibleIndexes.Count)];
                    difficulty = DifficultyOrder[chosenIndex];
                }

                // item selection
                var itemList = bingoItems
                    .Where(item => VersionIsSmallerOrEqual(gameVersion, item.Version) && !selectedItems.Contains(item.Name))
                    .Where(item => item.Difficulty == difficulty)
                    .Where(item =>
                    {
                        // Check group & material counts
                        bool groupOk = maxPerGroup == 0 || item.Groups.All(g => groupCounts.GetValueOrDefault(g, 0) < maxPerGroup);
                        bool materialOk = maxPerMaterial == 0 || string.IsNullOrEmpty(item.Material) || materialCounts.GetValueOrDefault(item.Material, 0) < maxPerMaterial;
                        return groupOk && materialOk;
                    })
                    .ToList();

                // ignore if no items match criteria
                if (itemList.Count == 0)
                {
                    itemList = bingoItems
                        .Where(item => item.Difficulty == difficulty && !selectedItems.Contains(item.Name))
                        .ToList();
                }

                if (itemList.Count == 0) throw new InvalidOperationException($"No available bingo items for difficulty '{difficulty}' at row {row}, column {column}.");

                BingoItem selectedItem = itemList[random.Next(itemList.Count)];
                selectedItems.Add(selectedItem.Name);

                // Update group / material counts
                foreach (var g in selectedItem.Groups)
                    groupCounts[g] = groupCounts.GetValueOrDefault(g, 0) + 1;
                if (!string.IsNullOrEmpty(selectedItem.Material))
                    materialCounts[selectedItem.Material] = materialCounts.GetValueOrDefault(selectedItem.Material, 0) + 1;

                var completed = teams.ToDictionary(team => team, _ => false);

                items.Add(new
                {
                    row,
                    column,
                    id = selectedItem.Id,
                    name = selectedItem.Name,
                    sprite = selectedItem.Sprite,
                    difficulty = selectedItem.Difficulty,
                    completed
                });
            }
        }

        return items;
    }
    
    private Dictionary<string, string> GetPlacements(string gameMode, string[] teams)
    {
        return gameMode switch
        {
            "1P" => new Dictionary<string, string>
            {
                { teams[0], "full" }
            },
            "2P" => new Dictionary<string, string>
            {
                { teams[0], "top" },
                { teams[1], "bottom" }
            },
            "3P" => new Dictionary<string, string>
            {
                { teams[0], "bottom" },
                { teams[1], "top-right" },
                { teams[2], "top-left" }
            },
            "4P" => new Dictionary<string, string>
            {
                { teams[0], "top-right" },
                { teams[1], "top-left" },
                { teams[2], "bottom-right" },
                { teams[3], "bottom-left" }
            },
            _ => throw new InvalidOperationException($"Invalid game mode {gameMode} provided!")
        };
    }
}
