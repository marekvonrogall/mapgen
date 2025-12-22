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
    public async Task<IActionResult> Ping()
    {
        string message = "works! (mapgen)";
        return Ok(new
        {
            message
        });
    }
    
    private static readonly List<string> DifficultyOrder = new() { "very easy", "easy", "medium", "hard", "very hard" };
    private static readonly string[] ValidGameModes = { "1P", "2P", "3P", "4P" };
    private static readonly string[] ValidPlacementModes = { "random", "circles", "flipped" };

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateRequest request)
    {
        var errors = new List<string>();
        
        // Grid Size
        int gridSize = request.GridSize ?? 5;
        if (gridSize < 1 || gridSize > 9)
            errors.Add($"Invalid grid size {gridSize}. The grid size must be in the range of 1 and 9.");

        // Game Mode
        string gameMode = request.GameMode ?? "1P";
        bool validGameMode = ValidGameModes.Contains(gameMode, StringComparer.OrdinalIgnoreCase);
        if (!validGameMode)
            errors.Add("Invalid game mode. Accepted values are 1P, 2P, 3P, or 4P.");

        int teamCount = 0;
        if (validGameMode && int.TryParse(gameMode[..1], out int firstDigit))
            teamCount = firstDigit;

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
            errors.Add("Duplicate team names are not allowed.");
        if (teamList.Length != teamCount)
            errors.Add($"Expected {teamCount} team names for game mode {gameMode}, got {teamList.Length}.");

        // Placement Mode
        string placementMode = string.IsNullOrWhiteSpace(request.PlacementMode) ? "circles" : request.PlacementMode;
        if (!ValidPlacementModes.Contains(placementMode))
            errors.Add($"Invalid placement mode {placementMode}. Valid values are: random, circles & flipped.");
        
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
            errors.Add($"Invalid difficulty value(s). Valid values are: {string.Join(", ", DifficultyOrder)} or all.");

        // Max Items Per Group / Material
        int maxPerGroupOrMaterial = request.MaxPerGroupOrMaterial ?? 1;
        if (maxPerGroupOrMaterial < 0)
            errors.Add("Max items per group or material cannot be negative. Disable group/material check by setting it to 0.");
        
        if (errors.Any())
            return BadRequest(new { errors });

        var placements = GetPlacements(gameMode, teamList);

        var bingoItems = LoadValidBingoItems();
        if (bingoItems.Count == 0)
        {
            return StatusCode(500, new { error = "Failed to load valid bingo items." });
        }

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
            settings = new { grid_size = gridSize, game_mode = gameMode, teams },
            items = GenerateItems(gridSize, bingoItems, teamList, difficultyList, maxPerGroupOrMaterial, placementMode)
        };

        var jsonPayload = JsonSerializer.Serialize(payload);
        var response = await _httpClient.PostAsync("http://imggen:5000/generate", new StringContent(jsonPayload, Encoding.UTF8, "application/json"));

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, new 
            { 
                error = "Failed to generate image.", 
                serviceError = errorContent, 
                mapRAW = payload 
            });
        }

        var imggenResponse = await response.Content.ReadAsStringAsync();
        var mapURL = JsonSerializer.Deserialize<JsonElement>(imggenResponse).GetProperty("url").GetString();

        return Content(JsonSerializer.Serialize(new { mapURL, mapRAW = payload }), "application/json");
    }

    private static readonly Lazy<List<BingoItem>> _cachedItems =
    new(() =>
    {
        var json = System.IO.File.ReadAllText("items.json");
        return JsonSerializer.Deserialize<List<BingoItem>>(json) ?? new List<BingoItem>();
    });

    private List<BingoItem> LoadValidBingoItems()
    {
        return _cachedItems.Value;
    }

    private List<object> GenerateItems(int gridSize, List<BingoItem> bingoItems, string[] teams, List<string> allowedDifficulties, int maxPerGroupOrMaterial, string placementMode)
    {
        var random = Random.Shared;
        var items = new List<object>();
        var selectedItems = new HashSet<string>();

        placementMode = placementMode?.ToLowerInvariant() ?? "random";
        if (!ValidPlacementModes.Contains(placementMode))
            throw new ArgumentException("Placement mode must be 'random', 'circles' or 'flipped'.");
    
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
    
        // ring-to-difficulty mapping for circles/flipped
        Dictionary<int, List<int>> ringDifficultyMap = new();
        if (placementMode == "circles" || placementMode == "flipped")
        {
            for (int ring = 0; ring <= maxDistance; ring++)
            {
                bool isCenter = ring == maxDistance;
                if (isCenter)
                {
                    int centerIndex = placementMode == "circles"
                        ? Math.Min(maxIndex + 1, DifficultyOrder.Count - 1) // hardest in center
                        : Math.Max(minIndex - 1, 0);                        // easiest in center
                    ringDifficultyMap[ring] = new List<int> { centerIndex };
                }
                else
                {
                    double fractionStart = (double)ring / maxDistance;
                    double fractionEnd = (double)(ring + 1) / maxDistance;
    
                    int startIdx, endIdx;
                    if (placementMode == "circles")
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
                    .Where(item => item.Difficulty == difficulty && !selectedItems.Contains(item.Name))
                    .Where(item =>
                    {
                        // Check group & material counts
                        bool groupOk = maxPerGroupOrMaterial == 0 || item.Groups.All(g => groupCounts.GetValueOrDefault(g, 0) < maxPerGroupOrMaterial);
                        bool materialOk = maxPerGroupOrMaterial == 0 || string.IsNullOrEmpty(item.Material) || materialCounts.GetValueOrDefault(item.Material, 0) < maxPerGroupOrMaterial;
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

    private class BingoItem
    {   [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("sprite")]
        public string Sprite { get; set; }

        [JsonPropertyName("group")]
        public string GroupRaw { get; set; }

        public List<string> Groups => ParseJsonArray(GroupRaw);

        private List<string> ParseJsonArray(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return new List<string>();
            raw = raw.Trim('[', ']', ' ');
            return raw.Split(',', StringSplitOptions.RemoveEmptyEntries)
                      .Select(s => s.Trim('\'', '"', ' ')).ToList();
        }

        [JsonPropertyName("material")]
        public string Material { get; set; }

        [JsonPropertyName("difficulty")]
        public string Difficulty { get; set; }
    }
}
