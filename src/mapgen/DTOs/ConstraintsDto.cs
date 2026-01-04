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
        
        [JsonPropertyName("blacklisted_items")]
        public List<string>? BlacklistedItems { get; set; }
        
        [JsonPropertyName("blacklisted_groups")]
        public List<string>? BlacklistedGroups { get; set; }
        
        [JsonPropertyName("blacklisted_materials")]
        public List<string>? BlacklistedMaterials { get; set; }
        
        [JsonPropertyName("blacklisted_categories")]
        public List<string>? BlacklistedCategories { get; set; }
        
        [JsonPropertyName("must_pass_all_blacklists")]
        public bool? MustPassAllBlacklists { get; set; }
        
        [JsonPropertyName("whitelisted_items")]
        public List<string>? WhitelistedItems { get; set; }
        
        [JsonPropertyName("whitelisted_groups")]
        public List<string>? WhitelistedGroups { get; set; }
        
        [JsonPropertyName("whitelisted_materials")]
        public List<string>? WhitelistedMaterials { get; set; }
        
        [JsonPropertyName("whitelisted_categories")]
        public List<string>? WhitelistedCategories { get; set; }
        
        [JsonPropertyName("must_pass_all_whitelists")]
        public bool? MustPassAllWhitelists { get; set; }
            
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
