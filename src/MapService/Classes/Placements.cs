using MapService.DTOs;

namespace MapService.Classes
{
    public static class Placements
    {
        public static readonly List<string> ValidPlacements = new() {
            "full", "top", "bottom", "left", "right", "top-left", "top-right", "bottom-left", "bottom-right"
        };
        
        public static readonly Dictionary<string, List<string[]>> ValidPlacementCombinations = new()
        {
            ["1P"] = new List<string[]> { new[] { "full" } },
            ["2P"] = new List<string[]> 
            { 
                new[] { "top", "bottom" }, 
                new[] { "bottom", "top" }, 
                new[] { "left", "right" }, 
                new[] { "right", "left" } 
            },
            ["3P"] = new List<string[]>
            {
                new[] { "top", "bottom-left", "bottom-right" },
                new[] { "bottom", "top-left", "top-right" },
                new[] { "left", "top-right", "bottom-right" },
                new[] { "right", "top-left", "bottom-left" }
            },
            ["4P"] = new List<string[]>
            {
                new[] { "top-left", "top-right", "bottom-left", "bottom-right" }
            }
        };

        private static void AssignSequential(List<TeamDto> teams, Dictionary<string, string> result, HashSet<string> assigned, params string[] order)
        {
            int idx = 0;

            foreach (var team in teams)
            {
                while (idx < order.Length && assigned.Contains(order[idx]))
                    idx++;

                if (idx >= order.Length)
                    throw new InvalidOperationException("No placement left");

                result[team.Name] = order[idx];
                assigned.Add(order[idx]);
                idx++;
            }
        }

        private static readonly Dictionary<string, string> Opposites =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["bottom"] = "top",
                ["top"] = "bottom",
                ["left"] = "right",
                ["right"] = "left"
            };

        private static void Assign2P(List<TeamDto> teams, Dictionary<string, string> result, HashSet<string> assigned)
        {
            foreach (var team in teams)
            {
                var placement = Opposites
                                    .FirstOrDefault(p => assigned.Contains(p.Key)).Value
                                ?? "left";

                result[team.Name] = placement;
                assigned.Add(placement);
            }
        }

        private static readonly Dictionary<string, string[]> BigToSmalls =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["bottom"] = new[] { "top-left", "top-right" },
                ["top"] = new[] { "bottom-left", "bottom-right" },
                ["left"] = new[] { "top-right", "bottom-right" },
                ["right"] = new[] { "top-left", "bottom-left" }
            };

        private static void Assign3P(List<TeamDto> teams, Dictionary<string, string> result, HashSet<string> assigned)
        {
            var big = assigned.FirstOrDefault(BigToSmalls.ContainsKey);

            if (big == null)
            {
                var smalls = assigned.Intersect(BigToSmalls.Values.SelectMany(v => v)).ToList();

                big = BigToSmalls
                          .FirstOrDefault(kv => smalls.All(s => kv.Value.Contains(s)))
                          .Key
                      ?? BigToSmalls.Keys.First(k => !assigned.Contains(k));
            }

            var availableSmalls = BigToSmalls[big]
                .Where(s => !assigned.Contains(s))
                .ToList();

            int smallIdx = 0;

            foreach (var team in teams)
            {
                if (!assigned.Contains(big))
                {
                    result[team.Name] = big;
                    assigned.Add(big);
                }
                else if (smallIdx < availableSmalls.Count)
                {
                    result[team.Name] = availableSmalls[smallIdx];
                    assigned.Add(availableSmalls[smallIdx]);
                    smallIdx++;
                }
                else
                {
                    var fallback = ValidPlacements.First(p => !assigned.Contains(p));
                    result[team.Name] = fallback;
                    assigned.Add(fallback);
                }
            }
        }


        public static Dictionary<string, string> AssignDefaultPlacements(string gameMode, List<TeamDto> teams)
        {
            var result = new Dictionary<string, string>();
            var assigned = new HashSet<string>(
                teams.Where(t => !string.IsNullOrWhiteSpace(t.Placement))
                    .Select(t => t.Placement!),
                StringComparer.OrdinalIgnoreCase);

            foreach (var team in teams.Where(t => !string.IsNullOrWhiteSpace(t.Placement)))
            {
                result[team.Name] = team.Placement!;
            }

            var unassigned = teams.Where(t => string.IsNullOrWhiteSpace(t.Placement)).ToList();

            switch (gameMode)
            {
                case "1P":
                    AssignSequential(unassigned, result, assigned, "full");
                    break;

                case "2P":
                    Assign2P(unassigned, result, assigned);
                    break;

                case "3P":
                    Assign3P(unassigned, result, assigned);
                    break;

                case "4P":
                    AssignSequential(
                        unassigned,
                        result,
                        assigned,
                        ValidPlacementCombinations["4P"][0]);
                    break;

                default:
                    throw new InvalidOperationException($"Invalid game mode {gameMode}.");
            }

            return result;
        }
    }
}
