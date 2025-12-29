using System.Text.Json.Serialization;

namespace MapService.DTOs
{
    public class UpdateResponseDto
    {
        [JsonPropertyName("map_url")]
        public required string MapUrl { get; set; }
        
        [JsonPropertyName("bingo")]
        public string? BingoStatus { get; set; }
    }
}