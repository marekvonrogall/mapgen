using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MapService.DTOs;
using MapService.Classes;

namespace MapService.Services
{

    public class MapGenerationService
    {
        private readonly HttpClient _httpClient;

        public MapGenerationService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<(bool Success, object? Data, List<string> Errors)> CreateMapAsync(CreateRequestDto request)
        {
            var mapgenErrors = new List<string>();

            // Grid Size
            int gridSize = request.GridSize ?? 5;
            if (gridSize < 1 || gridSize > 9)
                mapgenErrors.Add($"Invalid grid size {gridSize}. The grid size must be in the range of 1 and 9.");

            // Game Mode
            string gameMode = request.GameMode ?? "1P";
            bool validGameMode = Constraints.ValidGameModes.Contains(gameMode, StringComparer.OrdinalIgnoreCase);
            if (!validGameMode)
                mapgenErrors.Add("Invalid game mode. Accepted values are 1P, 2P, 3P, or 4P.");

            int teamCount = 0;
            if (validGameMode && int.TryParse(gameMode[..1], out int firstDigit))
                teamCount = firstDigit;

            // Game Version
            string gameVersion = request.GameVersion ?? JsonData.LatestGameVersion();
            if (!GameVersion.IsValidVersion(gameVersion))
                mapgenErrors.Add($"Invalid game version '{gameVersion}' provided.");
            else if (!GameVersion.VersionIsGreaterOrEqual(JsonData.EarliestGameVersion(), gameVersion) ||
                     !GameVersion.VersionIsSmallerOrEqual(JsonData.LatestGameVersion(), gameVersion))
                mapgenErrors.Add($"Specified game version '{gameVersion}' is unsupported. Supported versions are {JsonData.EarliestGameVersion()}-{JsonData.LatestGameVersion()}");

            // Team Names
            string[] teamList =
                string.IsNullOrWhiteSpace(request.TeamNames)
                    ? Enumerable.Range(1, teamCount).Select(i => $"team_{i}").ToArray()
                    : request.TeamNames.Split(',',
                        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (teamList.Distinct().Count() != teamList.Length)
                mapgenErrors.Add("Duplicate team names are not allowed.");
            if (teamList.Length != teamCount)
                mapgenErrors.Add($"Expected {teamCount} team names for game mode {gameMode}, got {teamList.Length}.");

            // Placement Mode
            string placementMode = string.IsNullOrWhiteSpace(request.PlacementMode)
                ? "circular"
                : request.PlacementMode.ToLowerInvariant();

            if (!Constraints.ValidPlacementModes.Contains(placementMode))
                mapgenErrors.Add($"Invalid placement mode {placementMode}. Valid values are: random, circular & flipped.");

            // Difficulty
            var difficultyList = (request.Difficulty ?? "easy,medium,hard")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(d => d.ToLower())
                .Distinct()
                .ToList();

            if (difficultyList.Contains("all"))
                difficultyList = Constraints.DifficultyOrder.ToList();

            if (!difficultyList.All(d => Constraints.DifficultyOrder.Contains(d)))
                mapgenErrors.Add($"Invalid difficulty value(s). Valid values are: {string.Join(", ", Constraints.DifficultyOrder)} or all.");
            
            // Max Items Per Group / Material
            int maxItemsPerGroup = request.Constraints?.MaxItemsPerGroup ?? 1;
            int maxItemsPerMaterial = request.Constraints?.MaxItemsPerMaterial ?? 1;

            if (maxItemsPerGroup < 0 || maxItemsPerMaterial < 0)
                mapgenErrors.Add("Max items per group/material cannot be negative. Disable group/material check by setting it to 0.");
            
            // Constraints
            var constraints = Constraints.GetConstraints(request.Constraints, mapgenErrors);
            
            // Colors
            var colors = Colors.GetHexColors(request.Colors, mapgenErrors);

            if (mapgenErrors.Any())
                return (false, null, mapgenErrors);

            var placements = Placements.GetPlacements(gameMode, teamList);
            var bingoItems = JsonData.BingoItems();

            var payload = new MapRawDto
            {
                Settings = new SettingsDto
                {
                    GridSize = gridSize,
                    GameMode = gameMode,
                    Teams = teamList.Select(t => new TeamDto { Name = t, Placement = placements[t] }).ToList(),
                    Constraints = constraints,
                    Colors = colors
                },
                Items = Items.GenerateItems(
                    gameVersion,
                    gridSize,
                    bingoItems,
                    teamList,
                    difficultyList,
                    maxItemsPerGroup,
                    maxItemsPerMaterial,
                    placementMode
                )
            };

            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var response = await _httpClient.PostAsync(
                "http://imggen:5000/generate",
                new StringContent(JsonSerializer.Serialize(payload, options), Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
            {
                string errorMessage = "Failed to generate image.";
                try
                {
                    var imggenErrorContent = await response.Content.ReadFromJsonAsync<JsonElement>();
                    if (imggenErrorContent.TryGetProperty("imggen", out var errorProp))
                        errorMessage = errorProp.GetString() ?? errorMessage;
                }
                catch {}

                return (false, payload, new List<string> { errorMessage });
            }

            var imggenResponse = await response.Content.ReadAsStringAsync();
            var mapUrl = JsonSerializer.Deserialize<JsonElement>(imggenResponse).GetProperty("url").GetString();
            
            var responseDto = new CreateResponseDto
            {
                MapUrl = mapUrl!,
                MapRaw = payload
            };
            
            return (true, responseDto, new List<string>());
        }
    }
}