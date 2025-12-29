using MapService.DTOs;

namespace MapService.Classes
{
    public static class Items
    {
        public static (bool Success, List<ResponseItemDto>? Items, List<string>? Errors) GenerateItems(SettingsDto settings, List<BingoItemDto> bingoItems)
        {
            var constraints = settings.Constraints ?? new ConstraintsDto();
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

            int maxDistance = settings.GridSize!.Value / 2;
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
            var whitelistedItems = constraints.WhitelistedItems ?? new List<string>();
            var whitelistedGroups = constraints.WhitelistedGroups ?? new List<string>();
            var whitelistedMaterials = constraints.WhitelistedMaterials ?? new List<string>();
            var whitelistedCategories = constraints.WhitelistedCategories ?? new List<string>();
            var whitelistedItemsSet = new HashSet<string>(whitelistedItems.Distinct(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);
            var whitelistedGroupsSet = new HashSet<string>(whitelistedGroups.Distinct(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);
            var whitelistedMaterialsSet = new HashSet<string>(whitelistedMaterials.Distinct(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);
            var whitelistedCategoriesSet = new HashSet<string>(whitelistedCategories.Distinct(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);
            
            var excludedItems = constraints.BlacklistedItems ?? new List<string>();
            var excludedGroups = constraints.BlacklistedGroups ?? new List<string>();
            var excludedMaterials = constraints.BlacklistedMaterials ?? new List<string>();
            var excludedCategories = constraints.BlacklistedCategories ?? new List<string>();
            var excludedItemsSet = new HashSet<string>(excludedItems.Distinct(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);
            var excludedGroupsSet = new HashSet<string>(excludedGroups.Distinct(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);
            var excludedMaterialsSet = new HashSet<string>(excludedMaterials.Distinct(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);
            var excludedCategoriesSet = new HashSet<string>(excludedCategories.Distinct(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);
            
            var maxItemsPerGroup = constraints.MaxItemsPerGroup ?? 1;
            var maxItemsPerMaterial = constraints.MaxItemsPerMaterial ?? 1;
            var maxItemsPerCategory = constraints.MaxItemsPerCategory ?? 0;
            
            for (int row = 0; row < settings.GridSize; row++)
            {
                for (int column = 0; column < settings.GridSize; column++)
                {
                    // item selection
                    var baseCandidates = bingoItems
                        // Item Version & Duplicates    
                        .Where(item => GameVersion.VersionIsSmallerOrEqual(settings.GameVersion!, item.Version))
                        .Where(item => !selectedItems.Contains(item.Name))
                        // Whitelist
                        .Where(item =>
                            whitelistedItemsSet.Count == 0 ||
                            whitelistedItemsSet.Contains(item.Id) ||
                            whitelistedItemsSet.Contains(item.Name)
                        )
                        .Where(item =>
                            whitelistedMaterialsSet.Count == 0 ||
                            (!string.IsNullOrEmpty(item.Material) &&
                             whitelistedMaterialsSet.Contains(item.Material))
                        )
                        .Where(item =>
                            whitelistedGroupsSet.Count == 0 ||
                            item.Groups.Any(g => whitelistedGroupsSet.Contains(g))
                        )
                        .Where(item =>
                            whitelistedCategoriesSet.Count == 0 ||
                            item.Categories.Any(c => whitelistedCategoriesSet.Contains(c))
                        )
                        // Blacklist
                        .Where(item => !excludedItemsSet.Contains(item.Id) && !excludedItemsSet.Contains(item.Name))
                        .Where(item => !excludedMaterialsSet.Contains(item.Material))
                        .Where(item => !item.Groups.Any(g => excludedGroupsSet.Contains(g)))
                        .Where(item => !item.Categories.Any(c => excludedCategoriesSet.Contains(c)))
                        // Group / Material / Category count
                        .Where(item =>
                        {
                            bool groupOk = maxItemsPerGroup == 0 || item.Groups.All(g =>
                                groupCounts.GetValueOrDefault(g, 0) < maxItemsPerGroup);
                            bool materialOk = maxItemsPerMaterial == 0 || string.IsNullOrEmpty(item.Material) ||
                                              materialCounts.GetValueOrDefault(item.Material, 0) < maxItemsPerMaterial;
                            bool categoryOk = maxItemsPerCategory == 0 || item.Categories.All(c =>
                                categoryCounts.GetValueOrDefault(c, 0) < maxItemsPerCategory);
                            return groupOk && materialOk && categoryOk;
                        })
                        .ToList();
                    
                    if (baseCandidates.Count == 0)
                        return (false, null, new List<string> { "Cannot create bingo board with current constraints! (Less items that meet the requirements than cells on the bingo board)" });
                    
                    // Difficulty
                    var allowedDifficulties = baseCandidates
                        .Select(i => i.Difficulty)
                        .Distinct()
                        .ToList();
                    
                    string difficulty;

                    if (settings.PlacementMode == "random")
                    {
                        var allowedDifficultiesForCell = allowedDifficulties
                            .Where(d => settings.Difficulties!.Contains(d))
                            .ToList();
                        
                        if (allowedDifficultiesForCell.Count == 0)
                            return (false, null,
                                new List<string>
                                {
                                    "Cannot create bingo board with current constraints! (Less items that meet the requirements than cells on the bingo board)"
                                });
                        difficulty = allowedDifficultiesForCell[random.Next(allowedDifficultiesForCell.Count)];
                    }
                    else
                    {
                        int ring = maxDistance - Math.Max(Math.Abs(row - maxDistance), Math.Abs(column - maxDistance));
                        var possibleIndexes = ringDifficultyMap[ring]
                            .Where(i => allowedDifficulties.Contains(Constraints.DifficultyOrder[i]))
                            .ToList();

                        if (possibleIndexes.Count == 0)
                            return (false, null,
                                new List<string>
                                {
                                    "Cannot create bingo board with current constraints! (Less items that meet the requirements than cells on the bingo board)"
                                });

                        int chosenIndex = possibleIndexes[random.Next(possibleIndexes.Count)];
                        difficulty = Constraints.DifficultyOrder[chosenIndex];
                    }

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
                        materialCounts[selectedItem.Material] = materialCounts.GetValueOrDefault(selectedItem.Material, 0) + 1;

                    var completed = settings.Teams!.ToDictionary(t => t.Name, _ => false);

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
