using System.Drawing;
using System.Text.Json;
using MapService.DTOs;

namespace MapService.Classes
{
    public static class Colors
    {
        public static string? IsValidHexColor(string? colorValue, string contextName, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(colorValue))
                return null;

            try
            {
                var color = ColorTranslator.FromHtml(colorValue);
                return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            }
            catch
            {
                errors.Add($"Invalid color value '{colorValue}' for '{contextName}'.");
                return null;
            }
        }
            
        public static ColorsDto? GetHexColors(ColorsDto? requestColors, List<string> errors)
        {
            if (requestColors == null)
                return null;
            
            var colorMap = new Dictionary<string, string?>
            {
                { "background_color", requestColors.BackgroundColor },
                { "foreground_color", requestColors.ForegroundColor },
                { "line_color", requestColors.LineColor },
                { "border_color", requestColors.BorderColor }
            };

            var returnColors = new Dictionary<string, string>();

            foreach (var (name, value) in colorMap)
            {
                var hex = IsValidHexColor(value, name, errors);
                if (hex != null)
                    returnColors.Add(name, hex);
            }

            if (returnColors.Count > 0)
            {
                var json = JsonSerializer.Serialize(returnColors);
                return JsonSerializer.Deserialize<ColorsDto>(json);
            }

            return null;
        }
    }
}
