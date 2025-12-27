using System.Text.Json.Serialization;

namespace MapService
{
    public class BingoItemDto
    {
        [JsonPropertyName("earliest_version")]
        public string EarliestGameVersion { get; set; } = "";

        [JsonPropertyName("latest_version")]
        public string LatestGameVersion { get; set; } = "";

        [JsonPropertyName("items")]
        public List<BingoItem> Items { get; set; } = new();
    }

    public class BingoItem
    {
        [JsonPropertyName("id")]
        public required string Id { get; set; }

        [JsonPropertyName("name")] 
        public required string Name { get; set; }

        [JsonPropertyName("sprite")]
        public required string Sprite { get; set; }

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