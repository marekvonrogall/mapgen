namespace MapService.Classes
{
    public static class Placements
    {
        public static Dictionary<string, string> GetPlacements(string gameMode, string[] teams)
        {
            return gameMode switch
            {
                "1P" => new Dictionary<string, string>
                {
                    { teams[0], "full" }
                },
                "2P" => new Dictionary<string, string>
                {
                    { teams[0], "top" },
                    { teams[1], "bottom" }
                },
                "3P" => new Dictionary<string, string>
                {
                    { teams[0], "bottom" },
                    { teams[1], "top-right" },
                    { teams[2], "top-left" }
                },
                "4P" => new Dictionary<string, string>
                {
                    { teams[0], "top-right" },
                    { teams[1], "top-left" },
                    { teams[2], "bottom-right" },
                    { teams[3], "bottom-left" }
                },
                _ => throw new InvalidOperationException($"Invalid game mode {gameMode} provided!")
            };
        }
    }
}
