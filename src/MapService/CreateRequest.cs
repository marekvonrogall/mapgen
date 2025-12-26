using System.Text.Json.Serialization;

namespace MapService
{
    public class CreateRequest
    {
        [JsonPropertyName("grid_size")]
        public int? GridSize { get; set; }

        [JsonPropertyName("game_mode")]
        public string? GameMode { get; set; }

        [JsonPropertyName("team_names")]
        public string? TeamNames { get; set; }
        
        [JsonPropertyName("placement_mode")]
        public string? PlacementMode { get; set; }

        [JsonPropertyName("difficulty")]
        public string? Difficulty { get; set; }
        
        [JsonPropertyName("constraints")]
        public Constraints? Constraints { get; set; }
        
        [JsonPropertyName("colors")]
        public Colors? Colors { get; set; }
    }

    public class Constraints
    {
        [JsonPropertyName("max_items_per_group")]
        public int? MaxItemsPerGroup { get; set; }
        
        [JsonPropertyName("max_items_per_material")]
        public int? MaxItemsPerMaterial { get; set; }
        
        [JsonPropertyName("min_padding")]
        public int? MinPadding { get; set; }
        
        [JsonPropertyName("max_padding")]
        public int? MaxPadding { get; set; }
        
        [JsonPropertyName("min_line_width")]
        public int? MinLineWidth { get; set; }
        
        [JsonPropertyName("max_line_width")]
        public int? MaxLineWidth { get; set; }
        
        [JsonPropertyName("min_border_width")]
        public int? MinBorderWidth { get; set; }
        
        [JsonPropertyName("max_border_width")]
        public int? MaxBorderWidth { get; set; }
        
        [JsonPropertyName("pixel_perfect")]
        public bool? PixelPerfect { get; set; }
        
        [JsonPropertyName("fill_board")]
        public bool? FillBoard { get; set; }
        
        [JsonPropertyName("center_board")]
        public bool? CenterBoard { get; set; }
    }

    public class Colors
    {
        [JsonPropertyName("background_color")]
        public string? BackgroundColor { get; set; }
        
        [JsonPropertyName("bg_color")]
        public string? BackgroundColorAlias { set { BackgroundColor = value; } }

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
