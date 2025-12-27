using System.Text.Json.Serialization;

namespace MapService.DTOs
{
    public class SettingsDto
    {
        [JsonPropertyName("grid_size")]
        public required int GridSize { get; set; }

        [JsonPropertyName("game_mode")]
        public required string GameMode { get; set; }
        
        [JsonPropertyName("teams")]
        public List<TeamDto>? Teams { get; set; }

        [JsonPropertyName("constraints")]
        public ConstraintsDto? Constraints { get; set; }
        
        [JsonPropertyName("colors")]
        public ColorsDto? Colors { get; set; }
    }
}
