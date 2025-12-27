namespace MapService.Classes
{
    public static class GameVersion
    {
        public static bool IsValidVersion(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
                return false;

            var parts = version.Split('.');

            // Version format: x.x or x.x.x
            if (parts.Length < 2 || parts.Length > 3)
                return false;

            foreach (var part in parts)
            {
                if (!int.TryParse(part, out var number))
                    return false;

                if (number < 0)
                    return false;
            }

            return true;
        }

        public static bool VersionIsSmallerOrEqual(string baseVersion, string inputVersion)
        {
            int[] baseParts = baseVersion.Split(".").Select(int.Parse).ToArray();
            int[] inputParts = inputVersion.Split(".").Select(int.Parse).ToArray();

            int maxLength = Math.Max(baseParts.Length, inputParts.Length);

            for (int i = 0; i < maxLength; i++)
            {
                int basePart = i < baseParts.Length ? baseParts[i] : 0;
                int inputPart = i < inputParts.Length ? inputParts[i] : 0;

                if (inputPart < basePart) return true;
                if (inputPart > basePart) return false;
            }

            return true;
        }

        public static bool VersionIsGreaterOrEqual(string baseVersion, string inputVersion)
        {
            int[] baseParts = baseVersion.Split(".").Select(int.Parse).ToArray();
            int[] inputParts = inputVersion.Split(".").Select(int.Parse).ToArray();

            int maxLength = Math.Max(baseParts.Length, inputParts.Length);

            for (int i = 0; i < maxLength; i++)
            {
                int basePart = i < baseParts.Length ? baseParts[i] : 0;
                int inputPart = i < inputParts.Length ? inputParts[i] : 0;

                if (inputPart > basePart) return true;
                if (inputPart < basePart) return false;
            }

            return true;
        }
    }
}
