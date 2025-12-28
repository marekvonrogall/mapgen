using System.Text.Json.Serialization;

namespace MapService.DTOs
{
    public class ConstraintsDto
    {
        [JsonPropertyName("max_items_per_group")]
        public int? MaxItemsPerGroup { get; set; }
            
        [JsonPropertyName("max_items_per_material")]
        public int? MaxItemsPerMaterial { get; set; }
        
        [JsonPropertyName("max_items_per_category")]
        public int? MaxItemsPerCategory { get; set; }
        
        [JsonPropertyName("excluded_items")]
        public List<string>? ExcludedItems { get; set; }
        
        [JsonPropertyName("excluded_groups")]
        public List<string>? ExcludedGroups { get; set; }
        
        [JsonPropertyName("excluded_materials")]
        public List<string>? ExcludedMaterials { get; set; }
        
        [JsonPropertyName("excluded_categories")]
        public List<string>? ExcludedCategories { get; set; }
            
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
}
