using System.Text.Json.Serialization;

namespace MapService.DTOs
{
    public class UpdateRequestDto
    {
        [JsonPropertyName("map_raw")]
        public MapRawDto? MapRaw { get; set; }
        
        [JsonPropertyName("settings")]
        public SettingsDto? Settings { get; set; }
        
        [JsonPropertyName("items")]
        public List<ResponseItemDto>? Items { get; set; }
    }
}