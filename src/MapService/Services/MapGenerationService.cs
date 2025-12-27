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
            int gridSize = request.Settings?.GridSize ?? 5;
            if (gridSize < 1 || gridSize > 9)
                mapgenErrors.Add($"Invalid grid size {gridSize}. The grid size must be in the range of 1 and 9.");

            // Game Mode
            string gameMode = request.Settings?.GameMode ?? "1P";
            bool validGameMode = Constraints.ValidGameModes.Contains(gameMode, StringComparer.OrdinalIgnoreCase);
            if (!validGameMode)
                mapgenErrors.Add("Invalid game mode. Accepted values are 1P, 2P, 3P, or 4P.");

            int teamCount = 0;
            if (validGameMode && int.TryParse(gameMode[..1], out int firstDigit))
                teamCount = firstDigit;

            // Game Version
            string gameVersion = request.Settings?.GameVersion ?? JsonData.LatestGameVersion();
            if (!GameVersion.IsValidVersion(gameVersion))
                mapgenErrors.Add($"Invalid game version '{gameVersion}' provided.");
            else if (!GameVersion.VersionIsGreaterOrEqual(JsonData.EarliestGameVersion(), gameVersion) ||
                     !GameVersion.VersionIsSmallerOrEqual(JsonData.LatestGameVersion(), gameVersion))
                mapgenErrors.Add($"Specified game version '{gameVersion}' is unsupported. Supported versions are {JsonData.EarliestGameVersion()}-{JsonData.LatestGameVersion()}");

            // Teams
            var teams = request.Settings?.Teams ?? new List<TeamDto>();
            var normalizedTeams = new List<TeamDto>();
            
            if (teams.Count != teamCount)
            {
                mapgenErrors.Add(
                    $"Expected {teamCount} teams for game mode {gameMode}, got {teams.Count}."
                );
            }
            
            var nameSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var placementSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var colorSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            // Assign default placements
            var defaultPlacements = Placements.AssignDefaultPlacements(gameMode, teams);
            
            foreach (var team in teams)
            {
                // Validate Team Name
                if (string.IsNullOrWhiteSpace(team.Name))
                {
                    mapgenErrors.Add("Team names cannot be empty.");
                    continue;
                }
            
                if (!nameSet.Add(team.Name))
                    mapgenErrors.Add($"Duplicate team name '{team.Name}' is not allowed.");
            
                // Validate Team Placement
                var placement = team.Placement ?? defaultPlacements.GetValueOrDefault(team.Name);
            
                if (string.IsNullOrWhiteSpace(placement))
                    mapgenErrors.Add($"Missing placement for team '{team.Name}'.");
                
                else if (!Placements.ValidPlacements.Contains(placement))
                    mapgenErrors.Add($"Invalid placement '{placement}' for team '{team.Name}'.");
                
                else if (!placementSet.Add(placement))
                    mapgenErrors.Add($"Duplicate placement '{placement}' is not allowed.");
                
                // Validate Team Color
                string? hexColor = null;
                if (!string.IsNullOrWhiteSpace(team.Color))
                {
                    if (!colorSet.Add(team.Color))
                        mapgenErrors.Add($"Duplicate color '{team.Color}' is not allowed.");
                    
                    hexColor = Colors.IsValidHexColor(team.Color, $"team '{team.Name}'", mapgenErrors);
                }
                
                normalizedTeams.Add(new TeamDto
                {
                    Name = team.Name,
                    Placement = placement,
                    Color = hexColor
                });
            }
            
            // Validate All Team Placements
            var placementList = normalizedTeams.Select(t => t.Placement!).ToList();
            bool validCombination = Placements.ValidPlacementCombinations.TryGetValue(gameMode, out var allowedSets) &&
                                    allowedSets.Any(set => set.All(p => placementList.Contains(p)) &&
                                                           placementList.All(p => set.Contains(p)));
            
            if (!validCombination) 
                mapgenErrors.Add($"Invalid placement combination for game mode {gameMode}.");
            
            // Placement Mode
            string placementMode = string.IsNullOrWhiteSpace(request.Settings?.PlacementMode)
                ? "circular"
                : request.Settings.PlacementMode.ToLowerInvariant();

            if (!Constraints.ValidPlacementModes.Contains(placementMode))
                mapgenErrors.Add($"Invalid placement mode {placementMode}. Valid values are: random, circular & flipped.");

            // Difficulty
            var difficultyList = (request.Settings?.Difficulties ?? Constraints.DefaultDifficulties.ToList())
                .Select(d => d.ToLowerInvariant())
                .Distinct()
                .ToList();

            if (difficultyList.Count == 0)
                difficultyList = Constraints.DefaultDifficulties.ToList();

            if (difficultyList.Contains("all"))
                difficultyList = Constraints.DifficultyOrder.ToList();

            if (!difficultyList.All(d => Constraints.DifficultyOrder.Contains(d)))
                mapgenErrors.Add($"Invalid difficulty value(s). Valid values are: {string.Join(", ", Constraints.DifficultyOrder)} or all.");
            
            // Max Items Per Group / Material
            int maxItemsPerGroup = request.Settings?.Constraints?.MaxItemsPerGroup ?? 1;
            int maxItemsPerMaterial = request.Settings?.Constraints?.MaxItemsPerMaterial ?? 1;

            if (maxItemsPerGroup < 0 || maxItemsPerMaterial < 0)
                mapgenErrors.Add("Max items per group/material cannot be negative. Disable group/material check by setting it to 0.");
            
            // Constraints
            var constraints = Constraints.GetConstraints(request.Settings?.Constraints, mapgenErrors);
            
            // Colors
            var colors = Colors.GetHexColors(request.Settings?.Colors, mapgenErrors);

            if (mapgenErrors.Any())
                return (false, null, mapgenErrors);

            var bingoItems = JsonData.BingoItems();

            var payload = new MapRawDto
            {
                Settings = new SettingsDto
                {
                    GridSize = gridSize,
                    GameMode = gameMode,
                    Teams = normalizedTeams,
                    Constraints = constraints,
                    Colors = colors
                },
                Items = Items.GenerateItems(
                    gameVersion,
                    gridSize,
                    bingoItems,
                    normalizedTeams.Select(t => t.Name).ToArray(),
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