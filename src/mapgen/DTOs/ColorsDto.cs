using System.Text.Json.Serialization;

namespace MapService.DTOs
{
    public class ColorsDto
    {
        [JsonPropertyName("background_color")]
        public string? BackgroundColor { get; set; }
        
        [JsonPropertyName("bg_color")]
        public string? BackgroundColorAlias { set { BackgroundColor = value; } }
        
        [JsonPropertyName("outer_background_color")]
        public string? OuterBackgroundColor { get; set; }
        
        [JsonPropertyName("outer_bg_color")]
        public string? OuterBackgroundColorAlias { set { OuterBackgroundColor = value; } }

        [JsonPropertyName("foreground_color")]
        public string? ForegroundColor { get; set; }
        
        [JsonPropertyName("fg_color")]
        public string? ForegroundColorAlias { set { ForegroundColor = value; } }
        
        [JsonPropertyName("line_color")]
        public string? LineColor { get; set; }
        
        [JsonPropertyName("border_color")]
        public string? BorderColor { get; set; }
    }
}
