using System.Text.Json.Serialization;

namespace MapService.DTOs
{
    public class MapRawDto
    {
        [JsonPropertyName("settings")]
        public required SettingsDto Settings { get; set; }
        
        [JsonPropertyName("items")]
        public required List<ResponseItemDto> Items { get; set; }
    }
}
