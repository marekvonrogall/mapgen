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

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateRequest request)
    {
        int gridSize = request.GridSize ?? 5;
        string gameMode = request.GameMode ?? "1P";
        string teamNames = request.TeamNames ?? "";
        string difficultyInput = request.Difficulty ?? "easy,medium,hard";
        int maxPerGroupOrMaterial = request.MaxPerGroupOrMaterial ?? 1;
        string placementMode = request.PlacementMode ?? "circles";

        if (gridSize < 1 || gridSize > 9)
        {
            return BadRequest(new { error = "Invalid grid_size. grid_size must be in range of 1 and 9." });
        }

        if (!new[] { "1P", "2P", "3P", "4P" }.Contains(gameMode))
        {
            return BadRequest(new { error = "Invalid game mode. Accepted values are 1P, 2P, 3P, or 4P." });
        }

        if (!new[] { "random", "circles", "flipped" }.Contains(placementMode))
        {
            return BadRequest(new { error = "Invalid placement mode. Accepted values are 'random', 'circles' and 'flipped'." });
        }

        if (string.IsNullOrWhiteSpace(teamNames))
        {
            return BadRequest(new { error = "Teams must be provided." });
        }

        string[] teamList = teamNames.Split(",");
        if ((gameMode == "1P" && teamList.Length != 1) ||
            (gameMode == "2P" && teamList.Length != 2) ||
            (gameMode == "3P" && teamList.Length != 3) ||
            (gameMode == "4P" && teamList.Length != 4))
        {
            return BadRequest(new { error = $"Invalid number of teams for game mode {gameMode}." });
        }
        
        var uniqueTeams = teamList.Distinct().ToList();
        if (uniqueTeams.Count != teamList.Length)
        {
            return BadRequest(new { error = "Duplicate team names are not allowed." });
        }

        var validDifficulties = DifficultyOrder;
        var difficultyList = difficultyInput
            .Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(d => d.ToLower())
            .Distinct()
            .ToList();
        
        if (difficultyList.Contains("all"))
        {
            difficultyList = validDifficulties.ToList();
        }

        if (!difficultyList.All(d => validDifficulties.Contains(d)))
        {
            return BadRequest(new
            {
                error = $"Invalid difficulty value(s). Valid values are: {string.Join(", ", validDifficulties)} or 'all'."
            });
        }

        var placements = GetPlacements(gameMode, teamList);

        var bingoItems = await LoadValidBingoItems();
        if (bingoItems == null)
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

    private async Task<List<BingoItem>> LoadValidBingoItems()
    {
        try
        {
            var json = await System.IO.File.ReadAllTextAsync("minecraft_1.21.10_items.json");
            return JsonSerializer.Deserialize<List<BingoItem>>(json);
        }
        catch
        {
            return null;
        }
    }

    private List<object> GenerateItems(int gridSize, List<BingoItem> bingoItems, string[] teams, List<string> allowedDifficulties, int maxPerGroupOrMaterial, string placementMode)
    {
        var random = new Random();
        var items = new List<object>();
        var selectedItems = new HashSet<string>();

        placementMode = placementMode?.ToLowerInvariant() ?? "random";
        if (!new[] { "random", "circles", "flipped" }.Contains(placementMode))
            throw new ArgumentException("placement must be 'random', 'circles' or 'flipped'.");
    
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
    
    private Dictionary<string, string>? GetPlacements(string gameMode, string[] teams)
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
        _ => null
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
