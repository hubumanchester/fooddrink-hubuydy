namespace FoodVisionMauiDemo.Services
{
    public static class RiskLabelFormatter
    {
        public static string ToLoadLabel(string tag)
        {
            return Normalize(tag) switch
            {
                "high_sugar" => "Sugar load",
                "high_fat" => "Fat load",
                "high_salt" => "Salt load",
                "high_carb" => "Refined carb load",
                "balanced" => "Balanced",
                "high_protein" => "Protein support",
                _ => ToDisplayText(tag)
            };
        }

        public static bool IsRiskLoadTag(string tag)
        {
            return Normalize(tag) is "high_sugar" or "high_fat" or "high_salt" or "high_carb";
        }

        public static string FormatTagList(IEnumerable<string> tags)
        {
            var labels = tags
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Select(ToLoadLabel)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return labels.Count > 0 ? string.Join(", ", labels) : "No risk tags";
        }

        private static string Normalize(string tag)
        {
            return tag.Trim()
                .ToLowerInvariant()
                .Replace(" ", "_")
                .Replace("-", "_");
        }

        private static string ToDisplayText(string value)
        {
            var words = Normalize(value).Split('_', StringSplitOptions.RemoveEmptyEntries);
            return words.Length == 0
                ? "Unknown"
                : string.Join(" ", words.Select(word => char.ToUpperInvariant(word[0]) + word[1..]));
        }
    }
}
