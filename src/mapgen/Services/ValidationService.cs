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
                string gameMode = settings?.GameMode?.ToLower() ?? "bingo";
                bool validGameMode = Constraints.ValidGameModes.Contains(gameMode, StringComparer.OrdinalIgnoreCase);
                if (!validGameMode)
                    mapgenErrors.Add("Invalid game mode. Accepted values are bingo & lockout.");

                // Game Version
                string gameVersion = settings?.GameVersion ?? JsonData.LatestGameVersion();
                if (!GameVersion.IsValidVersion(gameVersion))
                    mapgenErrors.Add($"Invalid game version '{gameVersion}' provided.");
                else if (!GameVersion.VersionIsGreaterOrEqual(JsonData.EarliestGameVersion(), gameVersion) ||
                         !GameVersion.VersionIsSmallerOrEqual(JsonData.LatestGameVersion(), gameVersion))
                    mapgenErrors.Add($"Specified game version '{gameVersion}' is unsupported. Supported versions are {JsonData.EarliestGameVersion()}-{JsonData.LatestGameVersion()}");

                // Teams
                int specifiedTeamCount = settings?.TeamCount ?? settings?.Teams?.Count ?? 1;
                if (specifiedTeamCount > Constraints.MaxTeamCount || specifiedTeamCount < 1)
                    mapgenErrors.Add("Invalid team count. Accepted team size ranges from 1 to 4.");
                
                var teamCount = settings?.Teams?.Count ?? specifiedTeamCount;
                var teams = settings?.Teams 
                            ?? Enumerable.Range(1, teamCount)
                                .Select(i => new TeamDto { Name = $"team_{i}" })
                                .ToList();

                var normalizedTeams = new List<TeamDto>();

                if (teams.Count != specifiedTeamCount)
                    mapgenErrors.Add($"Expected {specifiedTeamCount} teams for specified team count of {specifiedTeamCount}, got {teams.Count}.");
                
                var nameSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var placementSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var colorSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var defaultPlacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                
                // Assign default placements
                try
                {
                    if (string.Equals(gameMode, "bingo", StringComparison.OrdinalIgnoreCase))
                    {
                        defaultPlacements = Placements.AssignDefaultPlacements(specifiedTeamCount, teams);
                    }
                    else if (string.Equals(gameMode, "lockout", StringComparison.OrdinalIgnoreCase))
                    {
                        defaultPlacements = teams
                            .ToDictionary(t => t.Name, _ => "full", StringComparer.OrdinalIgnoreCase);
                    }
                    else
                    {
                        mapgenErrors.Add($"Couldn't determine default placements for game mode {gameMode}.");
                    }
                }
                catch (Exception ex)
                {
                    mapgenErrors.Add(ex.Message);
                }

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
                    var placement = defaultPlacements.GetValueOrDefault(team.Name);
                    if (string.Equals(gameMode, "bingo", StringComparison.OrdinalIgnoreCase))
                    {
                        placement = team.Placement?.ToLower() ?? placement;

                        if (string.IsNullOrWhiteSpace(placement))
                            mapgenErrors.Add($"Missing placement for team '{team.Name}'.");

                        else if (!Placements.ValidPlacements.Contains(placement))
                            mapgenErrors.Add($"Invalid placement '{placement}' for team '{team.Name}'.");

                        else if (!placementSet.Add(placement))
                            mapgenErrors.Add($"Duplicate placement '{placement}' is not allowed.");
                    }
                    else if (string.Equals(gameMode, "lockout", StringComparison.OrdinalIgnoreCase))
                    {   
                        if (team.Placement is not (null or "full"))
                            mapgenErrors.Add($"Invalid placement '{team.Placement}' for team '{team.Name}'. Game mode 'lockout' only supports team placement 'full'.");
                    }

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
                        Placement = placement ?? "",
                        Color = hexColor
                    });
                }

                // Validate All Team Placements
                var placementList = normalizedTeams.Select(t => t.Placement!).ToList();
                if (string.Equals(gameMode, "bingo", StringComparison.OrdinalIgnoreCase))
                {
                    bool validCombination =
                        Placements.ValidPlacementCombinations.TryGetValue(specifiedTeamCount, out var allowedSets) &&
                        allowedSets.Any(set => set.All(placementList.Contains) &&
                                               placementList.All(set.Contains));

                    if (!validCombination)
                        mapgenErrors.Add($"Invalid placement combination for team count of {specifiedTeamCount}.");
                }
                else if (string.Equals(gameMode, "lockout", StringComparison.OrdinalIgnoreCase))
                {
                    if (placementList.Any(p => p != "full"))
                        mapgenErrors.Add("Game mode 'lockout' only supports team placement 'full'.");
                }

                // Placement Mode
                string placementMode = string.IsNullOrWhiteSpace(settings?.PlacementMode)
                    ? "circular"
                    : settings.PlacementMode.ToLowerInvariant();

                if (!Constraints.ValidPlacementModes.Contains(placementMode))
                    mapgenErrors.Add($"Invalid placement mode {placementMode}. Valid values are: random, circular, flipped & lines.");

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
                
                // Constraints
                var constraints = Constraints.GetConstraints(settings?.Constraints, mapgenErrors);

                // Colors
                var colors = Colors.GetHexColors(settings?.Colors, mapgenErrors);

                var validatedSettings = new SettingsDto
                {
                    GridSize = gridSize,
                    GameMode = gameMode,
                    TeamCount = specifiedTeamCount,
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
