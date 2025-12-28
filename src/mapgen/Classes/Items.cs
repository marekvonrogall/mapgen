using MapService.DTOs;

namespace MapService.Classes
{
    public static class Items
    {
        public static (bool Success, List<ResponseItemDto>? Items, List<string>? Errors) GenerateItems(SettingsDto settings, List<BingoItemDto> bingoItems)
        {
            var random = Random.Shared;
            var items = new List<ResponseItemDto>();
            var selectedItems = new HashSet<string>();

            if (!Constraints.ValidPlacementModes.Contains(settings.PlacementMode))
                return (false, null, new List<string> { "Placement mode must be 'random', 'circular' or 'flipped'." });

            var allowedIndexes = settings.Difficulties!
                .Select(d => Constraints.DifficultyOrder.IndexOf(d))
                .Where(i => i >= 0)
                .Distinct()
                .OrderBy(i => i)
                .ToList();

            if (allowedIndexes.Count == 0)
                return (false, null, new List<string> { "No valid difficulties provided." });

            int minIndex = allowedIndexes.Min();
            int maxIndex = allowedIndexes.Max();

            int maxDistance = settings.GridSize / 2;
            var groupCounts = new Dictionary<string, int>();
            var materialCounts = new Dictionary<string, int>();
            var categoryCounts = new Dictionary<string, int>();

            // ring-to-difficulty mapping for circular/flipped
            Dictionary<int, List<int>> ringDifficultyMap = new();
            if (settings.PlacementMode == "circular" || settings.PlacementMode == "flipped")
            {
                for (int ring = 0; ring <= maxDistance; ring++)
                {
                    bool isCenter = ring == maxDistance;
                    if (isCenter)
                    {
                        int centerIndex = settings.PlacementMode == "circular"
                            ? Math.Min(maxIndex + 1, Constraints.DifficultyOrder.Count - 1) // hardest in center
                            : Math.Max(minIndex - 1, 0); // easiest in center
                        ringDifficultyMap[ring] = new List<int> { centerIndex };
                    }
                    else
                    {
                        double fractionStart = (double)ring / maxDistance;
                        double fractionEnd = (double)(ring + 1) / maxDistance;

                        int startIdx, endIdx;
                        if (settings.PlacementMode == "circular")
                        {
                            startIdx = (int)Math.Floor(fractionStart * (allowedIndexes.Count - 1));
                            endIdx = (int)Math.Ceiling(fractionEnd * (allowedIndexes.Count - 1));
                        }
                        else // flipped
                        {
                            startIdx = allowedIndexes.Count - 1 -
                                       (int)Math.Ceiling(fractionEnd * (allowedIndexes.Count - 1));
                            endIdx = allowedIndexes.Count - 1 -
                                     (int)Math.Floor(fractionStart * (allowedIndexes.Count - 1));
                        }

                        startIdx = Math.Clamp(startIdx, 0, allowedIndexes.Count - 1);
                        endIdx = Math.Clamp(endIdx, 0, allowedIndexes.Count - 1);

                        ringDifficultyMap[ring] = allowedIndexes
                            .Skip(Math.Min(startIdx, endIdx))
                            .Take(Math.Abs(endIdx - startIdx) + 1)
                            .ToList();
                    }
                }
            }

            // grid generation
            for (int row = 0; row < settings.GridSize; row++)
            {
                for (int column = 0; column < settings.GridSize; column++)
                {
                    string difficulty;

                    if (settings.PlacementMode == "random")
                    {
                        int randomIndex = allowedIndexes[random.Next(allowedIndexes.Count)];
                        difficulty = Constraints.DifficultyOrder[randomIndex];
                    }
                    else
                    {
                        int ring = maxDistance - Math.Max(Math.Abs(row - maxDistance), Math.Abs(column - maxDistance));
                        var possibleIndexes = ringDifficultyMap[ring];
                        int chosenIndex = possibleIndexes[random.Next(possibleIndexes.Count)];
                        difficulty = Constraints.DifficultyOrder[chosenIndex];
                    }

                    var excludedItems = settings.Constraints!.ExcludedItems ?? new List<string>();
                    var excludedGroups = settings.Constraints!.ExcludedGroups ?? new List<string>();
                    var excludedMaterials = settings.Constraints!.ExcludedMaterials ?? new List<string>();
                    var excludedCategories = settings.Constraints!.ExcludedCategories ?? new List<string>();
                    var maxItemsPerGroup = settings.Constraints!.MaxItemsPerGroup ?? 0;
                    var maxItemsPerMaterial = settings.Constraints!.MaxItemsPerMaterial ?? 0;
                    var maxItemsPerCategory = settings.Constraints!.MaxItemsPerCategory ?? 0;
                    
                    // item selection
                    var itemList = bingoItems
                        .Where(item => GameVersion.VersionIsSmallerOrEqual(settings.GameVersion!, item.Version) && !selectedItems.Contains(item.Name))
                        .Where(item => item.Difficulty == difficulty)
                        .Where(item => !excludedItems.Contains(item.Name))
                        .Where(item => !excludedMaterials.Contains(item.Material))
                        .Where(item => !item.Groups.Any(g => excludedGroups.Contains(g)))
                        .Where(item => !item.Categories.Any(g => excludedCategories.Contains(g)))
                        .Where(item =>
                        {
                            // Check group & material counts
                            bool groupOk = maxItemsPerGroup == 0 || item.Groups.All(g => groupCounts.GetValueOrDefault(g, 0) < maxItemsPerGroup);
                            bool materialOk = maxItemsPerMaterial == 0 || string.IsNullOrEmpty(item.Material) || materialCounts.GetValueOrDefault(item.Material, 0) < maxItemsPerMaterial;
                            bool categoryOk = maxItemsPerCategory == 0 || item.Categories.All(g => categoryCounts.GetValueOrDefault(g, 0) < maxItemsPerCategory);
                            return groupOk && materialOk && categoryOk;
                        })
                        .ToList();
                    
                    if (itemList.Count == 0)
                        return (false, null, new List<string> { "Cannot create bingo board with current constraints! (Less items that meet the requirements than cells on the bingo board)" });
                    
                    BingoItemDto selectedItem = itemList[random.Next(itemList.Count)];
                    selectedItems.Add(selectedItem.Name);

                    // Update group / material counts
                    foreach (var g in selectedItem.Groups)
                        groupCounts[g] = groupCounts.GetValueOrDefault(g, 0) + 1;
                    if (!string.IsNullOrEmpty(selectedItem.Material))
                        materialCounts[selectedItem.Material] = materialCounts.GetValueOrDefault(selectedItem.Material, 0) + 1;

                    var completed = settings.Teams?.ToDictionary(t => t.Name, _ => false);

                    items.Add(new ResponseItemDto
                    {
                        Row = row,
                        Column = column,
                        Id = selectedItem.Id,
                        Name = selectedItem.Name,
                        Sprite = selectedItem.Sprite,
                        Difficulty = selectedItem.Difficulty,
                        CompletedStatus = completed!
                    });
                }
            }

            return (true, items, null);
        }
    }
}
