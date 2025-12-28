using System.Text.Json.Serialization;

namespace MapService.DTOs
{
    public class UpdateResponseDto
    {
        [JsonPropertyName("url")]
        public required string Url { get; set; }
        
        [JsonPropertyName("bingo")]
        public string? BingoStatus { get; set; }
    }
}