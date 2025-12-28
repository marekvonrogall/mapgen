using MapService.DTOs;
using MapService.Classes;

namespace MapService.Services
{
    public class ValidationService
    {
        public SettingsDto ValidateRequest(SettingsDto? settings, List<string> mapgenErrors)
        {
            {
                // Grid Size
                int gridSize = settings?.GridSize ?? 5;
                if (gridSize < 1 || gridSize > 9)
                    mapgenErrors.Add($"Invalid grid size {gridSize}. The grid size must be in the range of 1 and 9.");

                // Game Mode
                string gameMode = settings?.GameMode ?? "1P";
                bool validGameMode = Constraints.ValidGameModes.Contains(gameMode, StringComparer.OrdinalIgnoreCase);
                if (!validGameMode)
                    mapgenErrors.Add("Invalid game mode. Accepted values are 1P, 2P, 3P, or 4P.");

                int teamCount = 0;
                if (validGameMode && int.TryParse(gameMode[..1], out int firstDigit))
                    teamCount = firstDigit;

                // Game Version
                string gameVersion = settings?.GameVersion ?? JsonData.LatestGameVersion();
                if (!GameVersion.IsValidVersion(gameVersion))
                    mapgenErrors.Add($"Invalid game version '{gameVersion}' provided.");
                else if (!GameVersion.VersionIsGreaterOrEqual(JsonData.EarliestGameVersion(), gameVersion) ||
                         !GameVersion.VersionIsSmallerOrEqual(JsonData.LatestGameVersion(), gameVersion))
                    mapgenErrors.Add($"Specified game version '{gameVersion}' is unsupported. Supported versions are {JsonData.EarliestGameVersion()}-{JsonData.LatestGameVersion()}");

                // Teams
                var teams = settings?.Teams ?? new List<TeamDto>();
                var normalizedTeams = new List<TeamDto>();

                if (teams.Count != teamCount)
                    mapgenErrors.Add($"Expected {teamCount} teams for game mode {gameMode}, got {teams.Count}.");
                
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
                bool validCombination =
                    Placements.ValidPlacementCombinations.TryGetValue(gameMode, out var allowedSets) &&
                    allowedSets.Any(set => set.All(p => placementList.Contains(p)) &&
                                           placementList.All(p => set.Contains(p)));

                if (!validCombination)
                    mapgenErrors.Add($"Invalid placement combination for game mode {gameMode}.");

                // Placement Mode
                string placementMode = string.IsNullOrWhiteSpace(settings?.PlacementMode)
                    ? "circular"
                    : settings.PlacementMode.ToLowerInvariant();

                if (!Constraints.ValidPlacementModes.Contains(placementMode))
                    mapgenErrors.Add($"Invalid placement mode {placementMode}. Valid values are: random, circular & flipped.");

                // Difficulty
                var difficultyList = (settings?.Difficulties ?? Constraints.DefaultDifficulties.ToList())
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
                int maxItemsPerGroup = settings?.Constraints?.MaxItemsPerGroup ?? 1;
                int maxItemsPerMaterial = settings?.Constraints?.MaxItemsPerMaterial ?? 1;

                if (maxItemsPerGroup < 0 || maxItemsPerMaterial < 0)
                    mapgenErrors.Add("Max items per group/material cannot be negative. Disable group/material check by setting it to 0.");

                // Constraints
                var constraints = Constraints.GetConstraints(settings?.Constraints, mapgenErrors);

                // Colors
                var colors = Colors.GetHexColors(settings?.Colors, mapgenErrors);

                var validatedSettings = new SettingsDto
                {
                    GridSize = gridSize,
                    GameMode = gameMode,
                    GameVersion = gameVersion,
                    PlacementMode = placementMode,
                    Difficulties = difficultyList,
                    Teams = normalizedTeams,
                    Constraints = constraints,
                    Colors = colors
                };
                
                return validatedSettings;
            }
        }
    }
}
