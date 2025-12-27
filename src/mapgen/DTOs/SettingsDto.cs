using System.Text.Json.Serialization;

namespace MapService.DTOs
{
    public class SettingsDto
    {
        [JsonPropertyName("grid_size")]
        public required int GridSize { get; set; }

        [JsonPropertyName("game_mode")]
        public required string GameMode { get; set; }
        
        [JsonPropertyName("game_version")]
        public string? GameVersion { get; set; }
        
        [JsonPropertyName("placement_mode")]
        public string? PlacementMode { get; set; }
    
        [JsonPropertyName("difficulties")]
        public List<string>? Difficulties { get; set; }
        
        [JsonPropertyName("teams")]
        public List<TeamDto>? Teams { get; set; }

        [JsonPropertyName("constraints")]
        public ConstraintsDto? Constraints { get; set; }
        
        [JsonPropertyName("colors")]
        public ColorsDto? Colors { get; set; }
    }
}
