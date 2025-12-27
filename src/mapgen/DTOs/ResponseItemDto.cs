using System.Text.Json.Serialization;

namespace MapService.DTOs
{
    public class ResponseItemDto
    {
        [JsonPropertyName("row")]
        public required int Row { get; set; }

        [JsonPropertyName("column")] 
        public required int Column { get; set; }
        
        [JsonPropertyName("id")]
        public required string Id { get; set; }

        [JsonPropertyName("name")] 
        public required string Name { get; set; }

        [JsonPropertyName("sprite")]
        public required string Sprite { get; set; }
        
        [JsonPropertyName("difficulty")]
        public required string Difficulty { get; set; }
        
        [JsonPropertyName("completed")]
        public required Dictionary<string, bool> CompletedStatus { get; set; }
    }
}