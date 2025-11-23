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

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateRequest request)
    {
        int gridSize = request.GridSize ?? 5;
        string gameMode = request.GameMode ?? "1P";
        string teamNames = request.TeamNames ?? "";
        string difficulty = request.Difficulty ?? "medium";
        int maxPerGroupOrMaterial = request.MaxPerGroupOrMaterial ?? 2;

        if (gridSize < 1 || gridSize > 9)
        {
            return BadRequest(new { error = "Invalid grid_size. grid_size must be in range of 1 and 9." });
        }

        if (!new[] { "1P", "2P", "3P", "4P" }.Contains(gameMode))
        {
            return BadRequest(new { error = "Invalid game mode. Accepted values are 1P, 2P, 3P, or 4P." });
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

        var validDifficulties = new[] { "very easy", "easy", "medium", "hard", "very hard" };
        if (!validDifficulties.Contains(difficulty))
        {
            return BadRequest(new { error = $"Invalid difficulty '{request.Difficulty}'. Valid values are: {string.Join(", ", validDifficulties)}." });
        }

        var placements = GetPlacements(gameMode, teamList);

        var bingoItems = await LoadValidBingoItems();
        if (bingoItems == null)
        {
            return StatusCode(500, new { error = "Failed to load valid bingo items." });
        }

        var teams = new List<object>();

        for (int i = 0; i < teamList.Length; i++)
        {
            teams.Add(new
            {
                name = teamList[i],
                placement = placements[teamList[i]]
            });
        }

        var payload = new
        {
            settings = new { grid_size = gridSize, game_mode = gameMode, teams },
            items = GenerateItems(gridSize, bingoItems, teamList, difficulty, maxPerGroupOrMaterial)
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

    private List<object> GenerateItems(int gridSize, List<BingoItem> bingoItems, string[] teams, string inputDifficulty, int maxPerGroupOrMaterial)
    {
        var random = new Random();
        var items = new List<object>();
        var selectedItems = new HashSet<string>();

        var difficulties = new List<string> { "very easy", "easy", "medium", "hard", "very hard" };
        int inputIndex = difficulties.IndexOf(inputDifficulty.ToLower());
        if (inputIndex == -1) throw new ArgumentException("Invalid difficulty provided.");

        int maxCenterIndex = inputDifficulty.ToLower() switch
        {
            "very easy" => difficulties.IndexOf("easy"),
            "easy" => difficulties.IndexOf("medium"),
            "medium" => difficulties.IndexOf("hard"),
            "hard" => difficulties.IndexOf("very hard"),
            "very hard" => difficulties.IndexOf("very hard"),
            _ => throw new ArgumentException("Invalid difficulty provided.")
        };

        int center = gridSize / 2;
        int maxDistance = center;

        // groups & materials
        var allGroups = bingoItems.SelectMany(b => b.Groups).Distinct().ToList();
        var allMaterials = bingoItems.Select(b => b.Material).Where(m => !string.IsNullOrEmpty(m)).Distinct().ToList();

        var groupCounts = new Dictionary<string, int>();
        var materialCounts = new Dictionary<string, int>();

        for (int row = 0; row < gridSize; row++)
        {
            for (int column = 0; column < gridSize; column++)
            {
                int ringDistance = Math.Max(Math.Abs(row - center), Math.Abs(column - center));
                double ringFactor = 1.0 - ((double)ringDistance / maxDistance);
                int difficultyOffset = (int)Math.Round(ringFactor * (maxCenterIndex - inputIndex));
                int chosenIndex = inputIndex + difficultyOffset;

                // difficulty index
                chosenIndex = Math.Min(chosenIndex, maxCenterIndex);
                string difficulty = difficulties[chosenIndex];

                // filter items by difficulty, counts, and uniqueness
                var itemList = bingoItems
                    .Where(item => item.Difficulty == difficulty && !selectedItems.Contains(item.Name))
                    .Where(item =>
                    {
                        // Check group counts
                        bool groupOk = true;
                        foreach (var g in item.Groups)
                        {
                            if (groupCounts.GetValueOrDefault(g, 0) >= maxPerGroupOrMaterial)
                                groupOk = false;
                        }

                        // Check material counts
                        bool materialOk = true;
                        if (!string.IsNullOrEmpty(item.Material) && materialCounts.GetValueOrDefault(item.Material, 0) >= maxPerGroupOrMaterial)
                            materialOk = false;

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

                var completed = teams.ToDictionary(team => team, team => false);

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
    
    private Dictionary<string, double> GetDifficultyChances(string inputDifficulty)
    {
        return inputDifficulty.ToLower() switch
        {
            "very easy" => new Dictionary<string, double>
            {
                { "very easy", 0.9 },
                { "easy", 0.1 },
                { "medium", 0.0 },
                { "hard", 0.0 },
                { "very hard", 0.0 }
            },
            "easy" => new Dictionary<string, double>
            {
                { "very easy", 0.6 },
                { "easy", 0.3 },
                { "medium", 0.1 },
                { "hard", 0.0 },
                { "very hard", 0.0 }
            },
            "medium" => new Dictionary<string, double>
            {
                { "very easy", 0.4 },
                { "easy", 0.3 },
                { "medium", 0.2 },
                { "hard", 0.1 },
                { "very hard", 0.0 }
            },
            "hard" => new Dictionary<string, double>
            {
                { "very easy", 0.1 },
                { "easy", 0.2 },
                { "medium", 0.3 },
                { "hard", 0.3 },
                { "very hard", 0.1 }
            },
            "very hard" => new Dictionary<string, double>
            {
                { "very easy", 0.0 },
                { "easy", 0.1 },
                { "medium", 0.2 },
                { "hard", 0.4 },
                { "very hard", 0.3 }
            },
            _ => throw new ArgumentException("Invalid difficulty provided.")
        };
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
