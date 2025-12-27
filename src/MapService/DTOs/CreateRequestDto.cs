using System.Text.Json.Serialization;

namespace MapService.DTOs
{
    public class CreateRequestDto
    {
        [JsonPropertyName("grid_size")]
        public int? GridSize { get; set; }
    
        [JsonPropertyName("game_mode")]
        public string? GameMode { get; set; }
            
        [JsonPropertyName("game_version")]
        public string? GameVersion { get; set; }
    
        [JsonPropertyName("team_names")]
        public string? TeamNames { get; set; }
            
        [JsonPropertyName("placement_mode")]
        public string? PlacementMode { get; set; }
    
        [JsonPropertyName("difficulty")]
        public string? Difficulty { get; set; }
            
        [JsonPropertyName("constraints")]
        public ConstraintsDto? Constraints { get; set; }
            
        [JsonPropertyName("colors")]
        public ColorsDto? Colors { get; set; }
    }
}
