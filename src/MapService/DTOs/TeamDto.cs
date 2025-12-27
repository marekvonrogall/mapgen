using System.Text.Json.Serialization;

namespace MapService.DTOs
{
    public class TeamDto
    {
        [JsonPropertyName("name")]
        public required string Name { get; set; }

        [JsonPropertyName("placement")]
        public required string Placement { get; set; }
    }
}
