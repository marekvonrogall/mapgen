using System.Collections.Frozen;
using System.Text.Json;
using MapService.DTOs;

namespace MapService.Classes
{
    public class ConstraintEntry
    {
        public object? Value { get; init; }
        public List<string> AllowedModes { get; init; } = new();
    }
    public static class Constraints
    {
        public static readonly List<string> DifficultyOrder = new() { "very easy", "easy", "medium", "hard", "very hard" };
        public static readonly List<string> DefaultDifficulties = new() { "easy", "medium", "hard" };
        public static readonly string[] ValidGameModes = { "Bingo", "Lockout", "Race" };
        public static readonly string[] ValidPlacementModes = { "random", "circular", "flipped", "lines" };
        public static readonly int MaxTeamCount = 4;
        
        private static void ValidateSetConstraint(List<string>? constraints, FrozenSet<string> allowedValues, string typeName, List<string> errors)
        {
            if (constraints == null) return;

            var distinctConstraints = constraints.Distinct(StringComparer.OrdinalIgnoreCase);

            foreach (var item in distinctConstraints)
            {
                if (!allowedValues.Contains(item))
                {
                    errors.Add($"Constraints: {typeName}: Unknown value '{item}'");
                }
            }
        }
        
        public static ConstraintsDto? GetConstraints(ConstraintsDto? requestConstraints, string gameMode, List<string> errors)
        {
            ValidateSetConstraint(requestConstraints?.BlacklistedItems, JsonData.ItemIdsAndNames, "Excluded Items", errors);
            ValidateSetConstraint(requestConstraints?.BlacklistedGroups, JsonData.Groups, "Excluded Groups", errors);
            ValidateSetConstraint(requestConstraints?.BlacklistedMaterials, JsonData.Materials, "Excluded Materials", errors);
            ValidateSetConstraint(requestConstraints?.BlacklistedCategories, JsonData.Categories, "Excluded Categories", errors);
            
            ValidateSetConstraint(requestConstraints?.WhitelistedItems, JsonData.ItemIdsAndNames, "Whitelisted Items", errors);
            ValidateSetConstraint(requestConstraints?.WhitelistedGroups, JsonData.Groups, "Whitelisted Groups", errors);
            ValidateSetConstraint(requestConstraints?.WhitelistedMaterials, JsonData.Materials, "Whitelisted Materials", errors);
            ValidateSetConstraint(requestConstraints?.WhitelistedCategories, JsonData.Categories, "Whitelisted Categories", errors);
            
            var constraintsMap = new Dictionary<string, ConstraintEntry>
            {
                {
                    "race_skips",
                    new ConstraintEntry
                        { Value = requestConstraints?.RaceSkips, AllowedModes = new List<string> { "Race" } }
                },
                {
                    "min_padding",
                    new ConstraintEntry
                        { Value = requestConstraints?.MinPadding, AllowedModes = new List<string> { "Bingo", "Lockout" } }
                },
                {
                    "max_padding",
                    new ConstraintEntry
                        { Value = requestConstraints?.MaxPadding, AllowedModes = new List<string> { "Bingo", "Lockout" } }
                },
                {
                    "min_line_width",
                    new ConstraintEntry
                        { Value = requestConstraints?.MinLineWidth, AllowedModes = new List<string> { "Bingo", "Lockout" } }
                },
                {
                    "max_line_width",
                    new ConstraintEntry
                        { Value = requestConstraints?.MaxLineWidth, AllowedModes = new List<string> { "Bingo", "Lockout" } }
                },
                {
                    "min_border_width",
                    new ConstraintEntry
                        { Value = requestConstraints?.MinBorderWidth, AllowedModes = new List<string> { "Bingo", "Lockout" } }
                },
                {
                    "max_border_width",
                    new ConstraintEntry
                        { Value = requestConstraints?.MaxBorderWidth, AllowedModes = new List<string> { "Bingo", "Lockout" } }
                },
                {
                    "pixel_perfect",
                    new ConstraintEntry
                        { Value = requestConstraints?.PixelPerfect, AllowedModes = new List<string> { "Bingo", "Lockout" } }
                },
                {
                    "fill_board",
                    new ConstraintEntry
                        { Value = requestConstraints?.FillBoard, AllowedModes = new List<string> { "Bingo", "Lockout" } }
                },
                {
                    "center_board",
                    new ConstraintEntry
                        { Value = requestConstraints?.CenterBoard, AllowedModes = new List<string> { "Bingo", "Lockout" } }
                },
                {
                    "max_items_per_group",
                    new ConstraintEntry
                        { Value = requestConstraints?.MaxItemsPerGroup, AllowedModes = new List<string> { "Bingo", "Lockout", "Race" } }
                },
                {
                    "max_items_per_material",
                    new ConstraintEntry
                        { Value = requestConstraints?.MaxItemsPerMaterial, AllowedModes = new List<string> { "Bingo", "Lockout", "Race" } }
                },
                {
                    "max_items_per_category",
                    new ConstraintEntry
                        { Value = requestConstraints?.MaxItemsPerCategory, AllowedModes = new List<string> { "Bingo", "Lockout", "Race" } }
                },
                {
                    "blacklisted_items",
                    new ConstraintEntry
                        { Value = requestConstraints?.BlacklistedItems, AllowedModes = new List<string> { "Bingo", "Lockout", "Race" } }
                },
                {
                    "blacklisted_groups",
                    new ConstraintEntry
                        { Value = requestConstraints?.BlacklistedGroups, AllowedModes = new List<string> { "Bingo", "Lockout", "Race" } }
                },
                {
                    "blacklisted_materials",
                    new ConstraintEntry
                    {
                        Value = requestConstraints?.BlacklistedMaterials, AllowedModes = new List<string> { "Bingo", "Lockout", "Race" }
                    }
                },
                {
                    "blacklisted_categories",
                    new ConstraintEntry
                    {
                        Value = requestConstraints?.BlacklistedCategories, AllowedModes = new List<string> { "Bingo", "Lockout", "Race" }
                    }
                },
                {
                    "must_pass_all_blacklists",
                    new ConstraintEntry
                    {
                        Value = requestConstraints?.MustPassAllBlacklists, AllowedModes = new List<string> { "Bingo", "Lockout", "Race" }
                    }
                },
                {
                    "whitelisted_items",
                    new ConstraintEntry
                        { Value = requestConstraints?.WhitelistedItems, AllowedModes = new List<string> { "Bingo", "Lockout", "Race" } }
                },
                {
                    "whitelisted_groups",
                    new ConstraintEntry
                        { Value = requestConstraints?.WhitelistedGroups, AllowedModes = new List<string> { "Bingo", "Lockout", "Race" } }
                },
                {
                    "whitelisted_materials",
                    new ConstraintEntry
                    {
                        Value = requestConstraints?.WhitelistedMaterials, AllowedModes = new List<string> { "Bingo", "Lockout", "Race" }
                    }
                },
                {
                    "whitelisted_categories",
                    new ConstraintEntry
                    {
                        Value = requestConstraints?.WhitelistedCategories, AllowedModes = new List<string> { "Bingo", "Lockout", "Race" }
                    }
                },
                {
                    "must_pass_all_whitelists",
                    new ConstraintEntry
                    {
                        Value = requestConstraints?.MustPassAllWhitelists, AllowedModes = new List<string> { "Bingo", "Lockout", "Race" }
                    }
                }
            };

            var minMaxMap = new Dictionary<string, string>()
            {
                { "min_padding", "max_padding" },
                { "min_line_width", "max_line_width" },
                { "min_border_width", "max_border_width" }
            };

            var returnConstraints = new Dictionary<string, object>();

            foreach (var (name, entry) in constraintsMap)
            {
                var value = entry.Value;

                if (value == null)
                    continue;

                if (!entry.AllowedModes.Contains(gameMode, StringComparer.OrdinalIgnoreCase))
                {
                    errors.Add($"Constraints: '{name}' is not supported for game mode '{gameMode}'");
                    continue;
                }

                if (value is int intVal && intVal < 0)
                {
                    errors.Add($"Constraints: '{name}': Must be >= 0, got {intVal}");
                    continue;
                }

                returnConstraints.Add(name, value);
            }

            foreach (var (min, max) in minMaxMap)
            {
                if (!returnConstraints.ContainsKey(min) || !returnConstraints.ContainsKey(max))
                    continue;

                if (returnConstraints[min] is int minValue && returnConstraints[max] is int maxValue)
                {
                    if (minValue > maxValue)
                    {
                        errors.Add($"Constraints: '{min}': Cannot be greater than '{max}' ({minValue} > {maxValue})");
                    }
                }
            }

            if (requestConstraints?.DifficultyOffset != null)
            {
                var maxOffset = DifficultyOrder.Count;
                var minOffset = DifficultyOrder.Count * -1;
                if (requestConstraints.DifficultyOffset > maxOffset || requestConstraints.DifficultyOffset < minOffset)
                    errors.Add($"constraints: 'difficulty_offset' must be in the range of {minOffset} and {maxOffset}");
                else returnConstraints.Add("difficulty_offset", requestConstraints.DifficultyOffset);
            }
            
            if (returnConstraints.Count > 0)
            {
                var json = JsonSerializer.Serialize(returnConstraints);
                return JsonSerializer.Deserialize<ConstraintsDto>(json);
            }

            return null;
        }
    }
}
