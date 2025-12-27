using System.Text.Json.Serialization;

namespace MapService.DTOs
{
    public class CreateResponseDto
    {
        [JsonPropertyName("map_url")]
        public required string MapUrl { get; set; }
        
        [JsonPropertyName("map_raw")]
        public required MapRawDto MapRaw { get; set; }
    }
}
