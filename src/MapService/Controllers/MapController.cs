using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using System.Linq;

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
    public async Task<IActionResult> Create(int? gridSize = 5, string gamemode = "1P", string team_names = "", string difficulty = "easy")
    {
        if (!new[] { "1P", "2P", "3P", "4P" }.Contains(gamemode))
        {
            return BadRequest(new { error = "Invalid gamemode. Accepted values are 1P, 2P, 3P, or 4P." });
        }

        if (string.IsNullOrWhiteSpace(team_names))
        {
            return BadRequest(new { error = "Teams must be provided." });
        }

        string[] teamList = team_names.Split(",");
        if ((gamemode == "1P" && teamList.Length != 1) ||
            (gamemode == "2P" && teamList.Length != 2) ||
            (gamemode == "3P" && teamList.Length != 3) ||
            (gamemode == "4P" && teamList.Length != 4))
        {
            return BadRequest(new { error = $"Invalid number of teams for gamemode {gamemode}." });
        }
        
        var uniqueTeams = teamList.Distinct().ToList();
        if (uniqueTeams.Count != teamList.Length)
        {
            return BadRequest(new { error = "Duplicate team names are not allowed." });
        }

        var placements = GetPlacements(gamemode, teamList);

        var bingoItems = await LoadValidBingoItems();
        if (bingoItems == null)
        {
            return StatusCode(500, new { error = "Failed to load valid bingo items." });
        }

        Dictionary<string, object> teams = new Dictionary<string, object>();

        for (int i = 0; i < teamList.Length; i++)
        {
            teams.Add($"team{i + 1}", new
            {
                name = teamList[i],
                placement = placements[teamList[i]]
            });
        }

        var payload = new
        {
            settings = new { grid_size = gridSize, gamemode, teams },
            items = GenerateItems(gridSize.Value, bingoItems, teamList, difficulty)
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

    private async Task<Dictionary<string, List<BingoItem>>> LoadValidBingoItems()
    {
        try
        {
            var json = await System.IO.File.ReadAllTextAsync("valid_bingo_items.json");
            return JsonSerializer.Deserialize<Dictionary<string, List<BingoItem>>>(json);
        }
        catch
        {
            return null;
        }
    }

    private List<object> GenerateItems(int gridSize, Dictionary<string, List<BingoItem>> bingoItems, string[] teams, string inputDifficulty)
    {
        var random = new Random();
        var items = new List<object>();
        var types = new[] { "block", "item" };
        var selectedItems = new HashSet<string>();
    
        var difficultyChances = GetDifficultyChances(inputDifficulty);
    
        int centerStart = (gridSize - 1) / 2;
        int centerEnd = gridSize / 2;
    
        for (int row = 0; row < gridSize; row++)
        {
            for (int column = 0; column < gridSize; column++)
            {
                string difficulty;

                if (row >= centerStart && row <= centerEnd && column >= centerStart && column <= centerEnd)
                {
                    switch (inputDifficulty.ToLower())
                    {
                        case "very easy":
                        case "easy":
                            difficulty = random.NextDouble() < difficultyChances["medium"] ? "medium" : "easy";
                            break;
                        case "medium":
                            difficulty = random.NextDouble() < difficultyChances["hard"] ? "hard" : "medium";
                            break;
                        case "hard":
                        case "very hard":
                            difficulty = random.NextDouble() < difficultyChances["very hard"] ? "very hard" : "hard";
                            break;
                        default:
                            throw new ArgumentException("Invalid difficulty provided.");
                    }
                }
                else if (Math.Abs(row - centerStart) <= 1 && Math.Abs(column - centerStart) <= 1 &&
                         Math.Abs(row - centerEnd) <= 1 && Math.Abs(column - centerEnd) <= 1)
                {
                    switch (inputDifficulty.ToLower())
                    {
                        case "very easy":
                        case "easy":
                            difficulty = random.NextDouble() < difficultyChances["easy"] ? "easy" : "medium";
                            break;
                        case "medium":
                            difficulty = random.NextDouble() < difficultyChances["medium"] ? "medium" : "hard";
                            break;
                        case "hard":
                        case "very hard":
                            difficulty = random.NextDouble() < difficultyChances["hard"] ? "hard" : "very hard";
                            break;
                        default:
                            throw new ArgumentException("Invalid difficulty provided.");
                    }
                }
                else
                {
                    switch (inputDifficulty.ToLower())
                    {
                        case "very easy":
                        case "easy":
                            difficulty = random.NextDouble() < difficultyChances["very easy"] ? "very easy" : "easy";
                            break;
                        case "medium":
                            difficulty = random.NextDouble() < difficultyChances["easy"] ? "easy" : "medium";
                            break;
                        case "hard":
                        case "very hard":
                            difficulty = random.NextDouble() < difficultyChances["medium"] ? "medium" : "hard";
                            break;
                        default:
                            throw new ArgumentException("Invalid difficulty provided.");
                    }
                }
    
                var type = types[random.Next(types.Length)];
                var itemList = bingoItems[type].Where(item => item.Difficulty == difficulty).ToList();
                if (itemList.Count == 0) continue;
    
                BingoItem selectedItem;
                do
                {
                    selectedItem = itemList[random.Next(itemList.Count)];
                } while (selectedItems.Contains(selectedItem.Name));
    
                selectedItems.Add(selectedItem.Name);
    
                var completed = new List<Dictionary<string, bool>>
                {
                    new Dictionary<string, bool>()
                };
                foreach (var team in teams)
                {
                    completed[0][team] = false;
                }
    
                items.Add(new
                {
                    row,
                    column,
                    type,
                    name = selectedItem.Name,
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


    private Dictionary<string, string> GetPlacements(string gamemode, string[] teams)
    {
        return gamemode switch
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
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("difficulty")]
        public string Difficulty { get; set; }
    }
}
