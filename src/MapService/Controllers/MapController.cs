using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;

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
    public async Task<IActionResult> Create(int? gridSize = 5, string gamemode = "1P", string teams = "", string difficulty = "easy")
    {
        if (!new[] { "1P", "2P", "3P", "4P" }.Contains(gamemode))
        {
            return BadRequest(new { error = "Invalid gamemode. Accepted values are 1P, 2P, 3P, or 4P." });
        }

        if (string.IsNullOrWhiteSpace(teams))
        {
            return BadRequest(new { error = "Teams must be provided." });
        }

        string[] teamList = teams.Split(",");
        if ((gamemode == "2P" && teamList.Length != 2) ||
            (gamemode == "3P" && teamList.Length != 3) ||
            (gamemode == "4P" && teamList.Length != 4))
        {
            return BadRequest(new { error = $"Invalid number of teams for gamemode {gamemode}." });
        }

        var placements = GetPlacements(gamemode, teamList);

        var bingoItems = await LoadValidBingoItems();
        if (bingoItems == null)
        {
            return StatusCode(500, new { error = "Failed to load valid bingo items." });
        }

        var payload = new
        {
            settings = new[] { new { grid_size = gridSize, gamemode, placements } },
            items = GenerateItems(gridSize.Value, bingoItems, teamList)
        };

        var jsonPayload = JsonSerializer.Serialize(payload);
        var response = await _httpClient.PostAsync("http://imggen:5000/generate", new StringContent(jsonPayload, Encoding.UTF8, "application/json"));

        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode, new { error = "Failed to generate image." });
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

    private List<object> GenerateItems(int gridSize, Dictionary<string, List<BingoItem>> bingoItems, string[] teams)
    {
        var random = new Random();
        var items = new List<object>();
        var types = new[] { "block", "item" };

        for (int row = 0; row < gridSize; row++)
        {
            for (int column = 0; column < gridSize; column++)
            {
                var type = types[random.Next(types.Length)];
                var itemList = bingoItems[type];
                var selectedItem = itemList[random.Next(itemList.Count)];

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
                    completed
                });
            }
        }

        return items;
    }

    private object GetPlacements(string gamemode, string[] teams)
    {
        return gamemode switch
        {
            "1P" => null,
            "2P" => new[]
            {
                new Dictionary<string, string>
                {
                    { teams[0], "top" },
                    { teams[1], "bottom" }
                }
            },
            "3P" => new[]
            {
                new Dictionary<string, string>
                {
                    { teams[0], "bottom" },
                    { teams[1], "top-right" },
                    { teams[2], "top-left" }
                }
            },
            "4P" => new[]
            {
                new Dictionary<string, string>
                {
                    { teams[0], "top-right" },
                    { teams[1], "top-left" },
                    { teams[2], "bottom-right" },
                    { teams[3], "bottom-left" }
                }
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
