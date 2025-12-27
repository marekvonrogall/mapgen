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
        public static ConstraintsDto? GetConstraints(ConstraintsDto? requestConstraints, List<string> errors)
        {
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
                { "center_board", requestConstraints?.CenterBoard }
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

                Console.WriteLine($"Added constraint: {name}, with value: {value}");
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
