using System.Text.Json.Serialization;

namespace MapService.DTOs
{
    public class CreateRequestDto
    {
        [JsonPropertyName("settings")]
        public SettingsDto? Settings { get; set; }
    }
}
