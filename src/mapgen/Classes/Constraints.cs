using System.Collections.Frozen;
using System.Text.Json;
using MapService.DTOs;

namespace MapService.Classes
{
    public static class Constraints
    {
        public static readonly List<string> DifficultyOrder = new() { "very easy", "easy", "medium", "hard", "very hard" };
        public static readonly List<string> DefaultDifficulties = new() { "easy", "medium", "hard" };
        public static readonly string[] ValidGameModes = { "1P", "2P", "3P", "4P" };
        public static readonly string[] ValidPlacementModes = { "random", "circular", "flipped" };
        
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
        
        public static ConstraintsDto? GetConstraints(ConstraintsDto? requestConstraints, List<string> errors)
        {
            ValidateSetConstraint(requestConstraints?.BlacklistedItems, JsonData.ItemIdsAndNames, "Excluded Items", errors);
            ValidateSetConstraint(requestConstraints?.BlacklistedGroups, JsonData.Groups, "Excluded Groups", errors);
            ValidateSetConstraint(requestConstraints?.BlacklistedMaterials, JsonData.Materials, "Excluded Materials", errors);
            ValidateSetConstraint(requestConstraints?.BlacklistedCategories, JsonData.Categories, "Excluded Categories", errors);
            
            ValidateSetConstraint(requestConstraints?.WhitelistedItems, JsonData.ItemIdsAndNames, "Whitelisted Items", errors);
            ValidateSetConstraint(requestConstraints?.WhitelistedGroups, JsonData.Groups, "Whitelisted Groups", errors);
            ValidateSetConstraint(requestConstraints?.WhitelistedMaterials, JsonData.Materials, "Whitelisted Materials", errors);
            ValidateSetConstraint(requestConstraints?.WhitelistedCategories, JsonData.Categories, "Whitelisted Categories", errors);

            
            var constraintsMap = new Dictionary<string, object?>
            {
                { "min_padding", requestConstraints?.MinPadding },
                { "max_padding", requestConstraints?.MaxPadding },
                { "min_line_width", requestConstraints?.MinLineWidth },
                { "max_line_width", requestConstraints?.MaxLineWidth },
                { "min_border_width", requestConstraints?.MinBorderWidth },
                { "max_border_width", requestConstraints?.MaxBorderWidth },
                { "pixel_perfect", requestConstraints?.PixelPerfect },
                { "fill_board", requestConstraints?.FillBoard },
                { "center_board", requestConstraints?.CenterBoard },
                { "max_items_per_group", requestConstraints?.MaxItemsPerGroup },
                { "max_items_per_material", requestConstraints?.MaxItemsPerMaterial },
                { "max_items_per_category", requestConstraints?.MaxItemsPerCategory },
                { "blacklisted_items", requestConstraints?.BlacklistedItems },
                { "blacklisted_groups", requestConstraints?.BlacklistedGroups },
                { "blacklisted_materials", requestConstraints?.BlacklistedMaterials },
                { "blacklisted_categories", requestConstraints?.BlacklistedCategories },
                { "whitelisted_items", requestConstraints?.WhitelistedItems },
                { "whitelisted_groups", requestConstraints?.WhitelistedGroups },
                { "whitelisted_materials", requestConstraints?.WhitelistedMaterials },
                { "whitelisted_categories", requestConstraints?.WhitelistedCategories },
                { "must_pass_all_whitelists", requestConstraints?.MustPassAllWhitelists }
            };

            var minMaxMap = new Dictionary<string, string>()
            {
                { "min_padding", "max_padding" },
                { "min_line_width", "max_line_width" },
                { "min_border_width", "max_border_width" }
            };

            var returnConstraints = new Dictionary<string, object>();

            foreach (var (name, value) in constraintsMap)
            {
                if (value == null)
                {
                    continue;
                }

                if (value is int intVal)
                {
                    if (intVal < 0)
                    {
                        errors.Add($"Constraints: '{name}': Must be >= 0, got {intVal}");
                        continue;
                    }
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
            
            if (returnConstraints.Count > 0)
            {
                var json = JsonSerializer.Serialize(returnConstraints);
                return JsonSerializer.Deserialize<ConstraintsDto>(json);
            }

            return null;
        }
    }
}
