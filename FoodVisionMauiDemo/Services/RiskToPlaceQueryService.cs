namespace FoodVisionMauiDemo.Services
{
    public class RiskToPlaceQueryService
    {
        public string GetKeywordsForRisk(string? riskTag)
        {
            return NormalizeRiskTag(riskTag) switch
            {
                "high_sugar" => "轻食|沙拉|健康餐",
                "high_fat" => "沙拉|轻食|日料",
                "high_salt" => "轻食|健康餐|沙拉",
                "high_carb" => "健身餐|沙拉|蛋白餐",
                _ => "餐厅|咖啡|超市"
            };
        }

        public string GetDisplayRisk(string? riskTag)
        {
            return NormalizeRiskTag(riskTag) switch
            {
                "high_sugar" => "Sugar load",
                "high_fat" => "Fat load",
                "high_salt" => "Salt load",
                "high_carb" => "Refined carb load",
                _ => "Balanced"
            };
        }

        public string NormalizeRiskTag(string? riskTag)
        {
            if (string.IsNullOrWhiteSpace(riskTag))
                return "balanced";

            var normalized = riskTag.Trim()
                .ToLowerInvariant()
                .Replace(" ", "_")
                .Replace("-", "_");

            return normalized switch
            {
                "high_sugar" or "high_fat" or "high_salt" or "high_carb" => normalized,
                _ => "balanced"
            };
        }
    }
}
