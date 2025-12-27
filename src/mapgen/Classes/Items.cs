using MapService.DTOs;

namespace MapService.Classes
{
    public static class Items
    {
        public static List<ResponseItemDto> GenerateItems(string gameVersion, int gridSize, List<BingoItemDto> bingoItems, string[] teams, List<string> allowedDifficulties, int maxPerGroup, int maxPerMaterial, string placementMode)
        {
            var random = Random.Shared;
            var items = new List<ResponseItemDto>();
            var selectedItems = new HashSet<string>();
    
            if (!Constraints.ValidPlacementModes.Contains(placementMode))
                throw new ArgumentException("Placement mode must be 'random', 'circular' or 'flipped'.");
        
            var allowedIndexes = allowedDifficulties
                .Select(d => Constraints.DifficultyOrder.IndexOf(d))
                .Where(i => i >= 0)
                .Distinct()
                .OrderBy(i => i)
                .ToList();
        
            if (allowedIndexes.Count == 0)
                throw new ArgumentException("No valid difficulties provided.");
    
            int minIndex = allowedIndexes.Min();
            int maxIndex = allowedIndexes.Max();
        
            int maxDistance = gridSize / 2;
            var groupCounts = new Dictionary<string, int>();
            var materialCounts = new Dictionary<string, int>();
        
            // ring-to-difficulty mapping for circular/flipped
            Dictionary<int, List<int>> ringDifficultyMap = new();
            if (placementMode == "circular" || placementMode == "flipped")
            {
                for (int ring = 0; ring <= maxDistance; ring++)
                {
                    bool isCenter = ring == maxDistance;
                    if (isCenter)
                    {
                        int centerIndex = placementMode == "circular"
                            ? Math.Min(maxIndex + 1, Constraints.DifficultyOrder.Count - 1) // hardest in center
                            : Math.Max(minIndex - 1, 0);                        // easiest in center
                        ringDifficultyMap[ring] = new List<int> { centerIndex };
                    }
                    else
                    {
                        double fractionStart = (double)ring / maxDistance;
                        double fractionEnd = (double)(ring + 1) / maxDistance;
        
                        int startIdx, endIdx;
                        if (placementMode == "circular")
                        {
                            startIdx = (int)Math.Floor(fractionStart * (allowedIndexes.Count - 1));
                            endIdx = (int)Math.Ceiling(fractionEnd * (allowedIndexes.Count - 1));
                        }
                        else // flipped
                        {
                            startIdx = allowedIndexes.Count - 1 - (int)Math.Ceiling(fractionEnd * (allowedIndexes.Count - 1));
                            endIdx = allowedIndexes.Count - 1 - (int)Math.Floor(fractionStart * (allowedIndexes.Count - 1));
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
            for (int row = 0; row < gridSize; row++)
            {
                for (int column = 0; column < gridSize; column++)
                {
                    string difficulty;
        
                    if (placementMode == "random")
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
    
                    // item selection
                    var itemList = bingoItems
                        .Where(item => GameVersion.VersionIsSmallerOrEqual(gameVersion, item.Version) && !selectedItems.Contains(item.Name))
                        .Where(item => item.Difficulty == difficulty)
                        .Where(item =>
                        {
                            // Check group & material counts
                            bool groupOk = maxPerGroup == 0 || item.Groups.All(g => groupCounts.GetValueOrDefault(g, 0) < maxPerGroup);
                            bool materialOk = maxPerMaterial == 0 || string.IsNullOrEmpty(item.Material) || materialCounts.GetValueOrDefault(item.Material, 0) < maxPerMaterial;
                            return groupOk && materialOk;
                        })
                        .ToList();
    
                    // ignore if no items match criteria
                    if (itemList.Count == 0)
                    {
                        itemList = bingoItems
                            .Where(item => item.Difficulty == difficulty && !selectedItems.Contains(item.Name))
                            .ToList();
                    }
    
                    if (itemList.Count == 0) throw new InvalidOperationException($"No available bingo items for difficulty '{difficulty}' at row {row}, column {column}.");
    
                    BingoItemDto selectedItem = itemList[random.Next(itemList.Count)];
                    selectedItems.Add(selectedItem.Name);
    
                    // Update group / material counts
                    foreach (var g in selectedItem.Groups)
                        groupCounts[g] = groupCounts.GetValueOrDefault(g, 0) + 1;
                    if (!string.IsNullOrEmpty(selectedItem.Material))
                        materialCounts[selectedItem.Material] = materialCounts.GetValueOrDefault(selectedItem.Material, 0) + 1;
    
                    var completed = teams.ToDictionary(team => team, _ => false);
    
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
    
            return items;
        }
    }
}
