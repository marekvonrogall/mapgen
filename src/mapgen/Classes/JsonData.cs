using System.Text.Json;
using MapService.DTOs;

namespace MapService.Classes
{
    public static class JsonData
    {
        private static readonly Lazy<ItemsJsonDto> CachedData = new(() =>
        {
            var json = File.ReadAllText("items.json");
            return JsonSerializer.Deserialize<ItemsJsonDto>(json) ?? new ItemsJsonDto();
        });

        public static List<BingoItemDto> BingoItems() => CachedData.Value.Items;
        public static string EarliestGameVersion() => CachedData.Value.EarliestGameVersion;
        public static string LatestGameVersion() => CachedData.Value.LatestGameVersion;
    }
}
