using System.Text.Json.Serialization;

namespace MapService
{
    public class CreateRequest
    {
        [JsonPropertyName("grid_size")]
        public int? GridSize { get; set; }

        [JsonPropertyName("game_mode")]
        public string? GameMode { get; set; }

        [JsonPropertyName("team_names")]
        public string? TeamNames { get; set; }

        [JsonPropertyName("difficulty")]
        public string? Difficulty { get; set; }

        [JsonPropertyName("max_per_group_or_material")]
        public int? MaxPerGroupOrMaterial { get; set; }
    }
}
