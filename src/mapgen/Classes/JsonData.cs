using System.Collections.Frozen;
using System.Text.Json;
using MapService.DTOs;

namespace MapService.Classes
{
    public static class JsonData
    {
        private static readonly ItemsJsonDto CachedData = JsonSerializer.Deserialize<ItemsJsonDto>(
            File.ReadAllText("items.json")
        ) ?? new ItemsJsonDto();
        
        public static List<BingoItemDto> BingoItems() => CachedData.Items;
        public static string EarliestGameVersion() => CachedData.EarliestGameVersion;
        public static string LatestGameVersion() => CachedData.LatestGameVersion;

        public static FrozenSet<string> Groups { get; } =
            BingoItems()
                .SelectMany(i => i.Groups)
                .ToFrozenSet(StringComparer.OrdinalIgnoreCase);

        public static FrozenSet<string> Materials { get; } =
            BingoItems()
                .Select(i => i.Material)
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        
        public static FrozenSet<string> Categories { get; } =
            BingoItems()
                .SelectMany(i => i.Categories)
                .ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        
        public static FrozenSet<string> ItemIdsAndNames { get; } =
            BingoItems()
                .SelectMany(i => new[] { i.Id, i.Name })
                .ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    }
}
