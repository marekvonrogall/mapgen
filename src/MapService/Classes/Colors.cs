using System.Drawing;
using System.Text.Json;
using MapService.DTOs;

namespace MapService.Classes
{
    public static class Colors
    {
        public static ColorsDto? GetHexColors(ColorsDto? requestColors, List<string> errors)
        {
            var colorMap = new Dictionary<string, string?>
            {
                { "background_color", requestColors?.BackgroundColor },
                { "foreground_color", requestColors?.ForegroundColor },
                { "line_color", requestColors?.LineColor },
                { "border_color", requestColors?.BorderColor }
            };

            var returnColors = new Dictionary<string, string>();

            foreach (var (name, value) in colorMap)
            {
                if (value == null)
                {
                    continue;
                }

                try
                {
                    var color = ColorTranslator.FromHtml(value);
                    returnColors.Add(name, $"#{color.R:X2}{color.G:X2}{color.B:X2}");
                }
                catch
                {
                    errors.Add($"Invalid color value '{value}' for color '{name}' provided.");
                }
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
