using System.Text.Json.Serialization;

namespace MapService.DTOs
{
    public class ItemsJsonDto
    {
        [JsonPropertyName("earliest_version")]
        public string EarliestGameVersion { get; set; } = "";
    
        [JsonPropertyName("latest_version")]
        public string LatestGameVersion { get; set; } = "";
    
        [JsonPropertyName("items")]
        public List<BingoItemDto> Items { get; set; } = new();
    }
}
