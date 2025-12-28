using System.Text.Json.Serialization;

namespace MapService.DTOs
{
    public class BingoItemDto
    {
        [JsonPropertyName("id")]
        public required string Id { get; set; }

        [JsonPropertyName("name")] 
        public required string Name { get; set; }

        [JsonPropertyName("sprite")]
        public required string Sprite { get; set; }
        
        [JsonPropertyName("category")]
        public required string CategoryRaw { get; set; }
        
        public List<string> Categories => ParseJsonArray(CategoryRaw);

        [JsonPropertyName("group")]
        public required string GroupRaw { get; set; }

        public List<string> Groups => ParseJsonArray(GroupRaw);

        private List<string> ParseJsonArray(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return new List<string>();
            raw = raw.Trim('[', ']', ' ');
            return raw.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim('\'', '"', ' ')).ToList();
        }

        [JsonPropertyName("material")]
        public required string Material { get; set; }

        [JsonPropertyName("difficulty")]
        public required string Difficulty { get; set; }
        
        [JsonPropertyName("version")]
        public required string Version { get; set; }
    }
}
