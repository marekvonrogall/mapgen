using MapService.DTOs;

namespace MapService.Classes
{
    public class ItemSettings
    {
        private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

        public List<string> WhitelistedItems { get; init; } = new();
        public List<string> WhitelistedGroups { get; init; } = new();
        public List<string> WhitelistedMaterials { get; init; } = new();
        public List<string> WhitelistedCategories { get; init; } = new();

        public List<string> BlacklistedItems { get; init; } = new();
        public List<string> BlacklistedGroups { get; init; } = new();
        public List<string> BlacklistedMaterials { get; init; } = new();
        public List<string> BlacklistedCategories { get; init; } = new();

        public int? MaxItemsPerGroup { get; init; }
        public int? MaxItemsPerMaterial { get; init; }
        public int? MaxItemsPerCategory { get; init; }

        public bool? MustPassAllWhitelists { get; init; }
        public bool? MustPassAllBlacklists { get; init; }

        public HashSet<string> WhitelistedItemsSet => new(WhitelistedItems, Comparer);
        public HashSet<string> WhitelistedGroupsSet => new(WhitelistedGroups, Comparer);
        public HashSet<string> WhitelistedMaterialsSet => new(WhitelistedMaterials, Comparer);
        public HashSet<string> WhitelistedCategoriesSet => new(WhitelistedCategories, Comparer);

        public HashSet<string> BlacklistedItemsSet => new(BlacklistedItems, Comparer);
        public HashSet<string> BlacklistedGroupsSet => new(BlacklistedGroups, Comparer);
        public HashSet<string> BlacklistedMaterialsSet => new(BlacklistedMaterials, Comparer);
        public HashSet<string> BlacklistedCategoriesSet => new(BlacklistedCategories, Comparer);

        public int MaxItemsPerGroupOrDefault => MaxItemsPerGroup ?? 1;
        public int MaxItemsPerMaterialOrDefault => MaxItemsPerMaterial ?? 1;
        public int MaxItemsPerCategoryOrDefault => MaxItemsPerCategory ?? 0;

        public bool MustPassAllWhitelistsOrDefault => MustPassAllWhitelists ?? false;
        public bool MustPassAllBlacklistsOrDefault => MustPassAllBlacklists ?? false;
    }

    public static class Items
    {
        private static (string? Difficulty, List<string>? Errors) GetRandomDifficulty(SettingsDto settings,
            Random random, List<string> availableDifficulties, List<string>? excludedDifficulties = null)
        {
            excludedDifficulties ??= new List<string>();
            var allowedDifficultiesForCell = availableDifficulties
                .Where(d => settings.Difficulties!.Contains(d)
                            && !excludedDifficulties.Contains(d))
                .ToList();

            if (allowedDifficultiesForCell.Count == 0)
                return (null,
                [
                    "Cannot create bingo board with current constraints! (Less items that meet the requirements than cells on the bingo board)"
                ]);
            return (allowedDifficultiesForCell[random.Next(allowedDifficultiesForCell.Count)], null);
        }

        private static (string? Difficulty, List<string>? Errors) GetItemDifficulty(SettingsDto settings, Random random,
            List<int> randomColumnOrder, List<BingoItemDto> baseCandidates,
            Dictionary<int, List<int>> ringDifficultyMap, int row, int column)
        {
            var availableDifficulties = baseCandidates
                .Select(i => i.Difficulty)
                .Distinct()
                .OrderBy(d => Constraints.DifficultyOrder.IndexOf(d))
                .ToList();

            if (settings.PlacementMode == "random")
            {
                return GetRandomDifficulty(settings, random, availableDifficulties);
            }

            string difficulty;
            int maxDistance = settings.GridSize!.Value / 2;
            int ring = maxDistance - Math.Max(Math.Abs(row - maxDistance), Math.Abs(column - maxDistance));

            var possibleIndexes = ringDifficultyMap[ring]
                .Where(i => availableDifficulties.Contains(Constraints.DifficultyOrder[i]))
                .ToList();

            bool isCenter = ring == maxDistance;
            if (isCenter || settings.PlacementMode is "circular" or "flipped")
            {
                if (possibleIndexes.Count == 0)
                    return (null,
                    [
                        "Cannot create bingo board with current constraints! (Less items that meet the requirements than cells on the bingo board)"
                    ]);

                int chosenIndex = possibleIndexes[random.Next(possibleIndexes.Count)];
                difficulty = Constraints.DifficultyOrder[chosenIndex];
            }
            else if (settings.PlacementMode == "lines")
            {
                var highestDifficulty = settings.Difficulties!.Last();
                var secondHighestDifficulty = settings.Difficulties!.Count >= 2
                    ? settings.Difficulties![^2]
                    : highestDifficulty;

                if (column == randomColumnOrder[row])
                    difficulty = secondHighestDifficulty;
                else
                {
                    var excludedDifficulties = new List<string>();
                    if (settings.Difficulties.Count >= 3)
                        excludedDifficulties.Add(secondHighestDifficulty);
                    if (settings.Difficulties.Count >= 2)
                        excludedDifficulties.Add(highestDifficulty);

                    var (randomDifficulty, errors) =
                        GetRandomDifficulty(settings, random, availableDifficulties, excludedDifficulties);
                    if (errors is not null)
                        return (null, errors);
                    if (randomDifficulty is null)
                        return (null, ["Couldn't determine difficulties!"]);

                    difficulty = randomDifficulty;
                }
            }
            else
            {
                return (null, ["Placement mode must be 'random', 'circular', 'flipped' or 'lines'."]);
            }

            return (difficulty, null);
        }

        private static (List<BingoItemDto>? BaseCandidates, List<string>? Errors) GetBaseCandidates(
            List<BingoItemDto> bingoItems, ItemSettings itemSettings, SettingsDto settings,
            HashSet<string> selectedItems, Dictionary<string, int> groupCounts, Dictionary<string, int> materialCounts,
            Dictionary<string, int> categoryCounts)
        {
            var baseCandidates = bingoItems
                // Item Version & Duplicates    
                .Where(item => item.Difficulty != "unobtainable")
                .Where(item => GameVersion.VersionIsSmallerOrEqual(settings.GameVersion!, item.Version))
                .Where(item => selectedItems.Contains(item.Name))
                // Whitelist
                .Where(item =>
                {
                    var checks = new List<bool>();

                    if (itemSettings.WhitelistedItemsSet.Count > 0)
                        checks.Add(itemSettings.WhitelistedItemsSet.Contains(item.Id) ||
                                   itemSettings.WhitelistedItemsSet.Contains(item.Name));

                    if (itemSettings.WhitelistedMaterialsSet.Count > 0 && !string.IsNullOrEmpty(item.Material))
                        checks.Add(itemSettings.WhitelistedMaterialsSet.Contains(item.Material));

                    if (itemSettings.WhitelistedGroupsSet.Count > 0)
                        checks.Add(item.Groups.Any(g => itemSettings.WhitelistedGroupsSet.Contains(g)));

                    if (itemSettings.WhitelistedCategoriesSet.Count > 0)
                        checks.Add(item.Categories.Any(c => itemSettings.WhitelistedCategoriesSet.Contains(c)));

                    if (checks.Count == 0)
                        return true;

                    return itemSettings.MustPassAllWhitelistsOrDefault
                        ? checks.All(x => x)
                        : checks.Any(x => x);
                })
                // Blacklist
                .Where(item =>
                {
                    var checks = new List<bool>();

                    if (itemSettings.BlacklistedItemsSet.Count > 0)
                        checks.Add(itemSettings.BlacklistedItemsSet.Contains(item.Id) ||
                                   itemSettings.BlacklistedItemsSet.Contains(item.Name));

                    if (itemSettings.BlacklistedMaterialsSet.Count > 0 && !string.IsNullOrEmpty(item.Material))
                        checks.Add(itemSettings.BlacklistedMaterialsSet.Contains(item.Material));

                    if (itemSettings.BlacklistedGroupsSet.Count > 0)
                        checks.Add(item.Groups.Any(g => itemSettings.BlacklistedGroupsSet.Contains(g)));

                    if (itemSettings.BlacklistedCategoriesSet.Count > 0)
                        checks.Add(item.Categories.Any(c => itemSettings.BlacklistedCategoriesSet.Contains(c)));

                    if (checks.Count == 0)
                        return true;

                    return itemSettings.MustPassAllBlacklistsOrDefault
                        ? !checks.All(x => x)
                        : !checks.Any(x => x);
                })
                // Group / Material / Category count
                .Where(item =>
                {
                    bool groupOk = itemSettings.MaxItemsPerGroupOrDefault == 0 || item.Groups.All(g =>
                        groupCounts.GetValueOrDefault(g, 0) < itemSettings.MaxItemsPerGroupOrDefault);
                    bool materialOk = itemSettings.MaxItemsPerMaterialOrDefault == 0 ||
                                      string.IsNullOrEmpty(item.Material) ||
                                      materialCounts.GetValueOrDefault(item.Material, 0) <
                                      itemSettings.MaxItemsPerMaterialOrDefault;
                    bool categoryOk = itemSettings.MaxItemsPerCategoryOrDefault == 0 || item.Categories.All(c =>
                        categoryCounts.GetValueOrDefault(c, 0) < itemSettings.MaxItemsPerCategoryOrDefault);
                    return groupOk && materialOk && categoryOk;
                })
                .ToList();
            if (baseCandidates.Count == 0)
                return (null,
                [
                    "Cannot create bingo board with current constraints! (Less items that meet the requirements than cells on the bingo board)"
                ]);
            return (baseCandidates, null);
        }

        public static (bool Success, List<ResponseItemDto>? Items, List<string>? Errors) GenerateItems(
            SettingsDto settings, List<BingoItemDto> bingoItems)
        {
            var constraints = settings.Constraints ?? new ConstraintsDto();
            var random = Random.Shared;
            var items = new List<ResponseItemDto>();
            var selectedItems = new HashSet<string>();

            if (!Constraints.ValidPlacementModes.Contains(settings.PlacementMode))
                return (false, null, ["Placement mode must be 'random', 'circular', 'flipped' or 'lines'."]);

            var allowedIndexes = settings.Difficulties!
                .Select(d => Constraints.DifficultyOrder.IndexOf(d))
                .Where(i => i >= 0)
                .Distinct()
                .OrderBy(i => i)
                .ToList();

            if (allowedIndexes.Count == 0)
                return (false, null, ["No valid difficulties provided."]);

            int minIndex = allowedIndexes.Min();
            int maxIndex = allowedIndexes.Max();
            int maxDistance = settings.GridSize!.Value / 2;
            int difficultyOffset = settings.Constraints?.DifficultyOffset ?? 0;

            var groupCounts = new Dictionary<string, int>();
            var materialCounts = new Dictionary<string, int>();
            var categoryCounts = new Dictionary<string, int>();

            // ring-to-difficulty mapping for circular/flipped
            Dictionary<int, List<int>> ringDifficultyMap = new();
            if (settings.PlacementMode is "circular" or "flipped" or "lines")
            {
                for (int r = 0; r <= maxDistance; r++)
                {
                    bool isCenter = r == maxDistance;
                    if (isCenter)
                    {
                        int centerIndex = Math.Clamp(
                            settings.PlacementMode is "flipped"
                                ? minIndex - difficultyOffset // easiest in center
                                : maxIndex + difficultyOffset, // hardest in center,
                            0, Constraints.DifficultyOrder.Count - 1
                        );
                        ringDifficultyMap[r] = new List<int> { centerIndex };
                    }
                    else
                    {
                        double fractionStart = (double)r / maxDistance;
                        double fractionEnd = (double)(r + 1) / maxDistance;

                        int startIdx, endIdx;
                        if (settings.PlacementMode is "flipped")
                        {
                            startIdx = allowedIndexes.Count - 1 -
                                       (int)Math.Ceiling(fractionEnd * (allowedIndexes.Count - 1));
                            endIdx = allowedIndexes.Count - 1 -
                                     (int)Math.Floor(fractionStart * (allowedIndexes.Count - 1));
                        }
                        else // flipped
                        {
                            startIdx = (int)Math.Floor(fractionStart * (allowedIndexes.Count - 1));
                            endIdx = (int)Math.Ceiling(fractionEnd * (allowedIndexes.Count - 1));
                        }

                        startIdx = Math.Clamp(startIdx, 0, allowedIndexes.Count - 1);
                        endIdx = Math.Clamp(endIdx, 0, allowedIndexes.Count - 1);

                        ringDifficultyMap[r] = allowedIndexes
                            .Skip(Math.Min(startIdx, endIdx))
                            .Take(Math.Abs(endIdx - startIdx) + 1)
                            .ToList();
                    }
                }
            }

            // item Settings
            var itemSettings = new ItemSettings
            {
                WhitelistedItems = constraints.WhitelistedItems ?? new List<string>(),
                WhitelistedGroups = constraints.WhitelistedGroups ?? new List<string>(),
                WhitelistedMaterials = constraints.WhitelistedMaterials ?? new List<string>(),
                WhitelistedCategories = constraints.WhitelistedCategories ?? new List<string>(),

                BlacklistedItems = constraints.BlacklistedItems ?? new List<string>(),
                BlacklistedGroups = constraints.BlacklistedGroups ?? new List<string>(),
                BlacklistedMaterials = constraints.BlacklistedMaterials ?? new List<string>(),
                BlacklistedCategories = constraints.BlacklistedCategories ?? new List<string>(),

                MaxItemsPerGroup = constraints.MaxItemsPerGroup,
                MaxItemsPerMaterial = constraints.MaxItemsPerMaterial,
                MaxItemsPerCategory = constraints.MaxItemsPerCategory,

                MustPassAllWhitelists = constraints.MustPassAllWhitelists,
                MustPassAllBlacklists = constraints.MustPassAllBlacklists
            };

            var randomColumnOrder = Enumerable.Range(0, settings.GridSize.Value)
                .OrderBy(_ => random.Next())
                .ToList();

            for (int row = 0; row < settings.GridSize; row++)
            {
                for (int column = 0; column < settings.GridSize; column++)
                {
                    // item selection
                    var (baseCandidates, baseCandidateErrors) = GetBaseCandidates(bingoItems, itemSettings, settings,
                        selectedItems, groupCounts, materialCounts, categoryCounts);

                    if (baseCandidateErrors is not null)
                        return (false, null, baseCandidateErrors);
                    if (baseCandidates is null)
                        return (false, null, ["Couldn't determine board items!"]);

                    // Difficulty
                    var (difficulty, itemDifficultyErrors) = GetItemDifficulty(settings, random, randomColumnOrder,
                        baseCandidates, ringDifficultyMap, row, column);

                    if (itemDifficultyErrors is not null)
                        return (false, null, itemDifficultyErrors);
                    if (difficulty is null)
                        return (false, null, ["Couldn't determine difficulties!"]);

                    var itemList = baseCandidates
                        .Where(item => item.Difficulty == difficulty)
                        .ToList();

                    BingoItemDto selectedItem = itemList[random.Next(itemList.Count)];
                    selectedItems.Add(selectedItem.Name);

                    // Update group / material / category counts
                    foreach (var g in selectedItem.Groups)
                        groupCounts[g] = groupCounts.GetValueOrDefault(g, 0) + 1;
                    foreach (var c in selectedItem.Categories)
                        categoryCounts[c] = categoryCounts.GetValueOrDefault(c, 0) + 1;
                    if (!string.IsNullOrEmpty(selectedItem.Material))
                        materialCounts[selectedItem.Material] =
                            materialCounts.GetValueOrDefault(selectedItem.Material, 0) + 1;

                    var completed = settings.Teams!.ToDictionary(t => t.Name, _ => false);

                    items.Add(new ResponseItemDto
                    {
                        Row = row,
                        Column = column,
                        Id = selectedItem.Id,
                        Name = selectedItem.Name,
                        Sprite = selectedItem.Sprite,
                        Difficulty = selectedItem.Difficulty,
                        CompletedStatus = completed
                    });
                }
            }

            return (true, items, null);
        }
    }
}
