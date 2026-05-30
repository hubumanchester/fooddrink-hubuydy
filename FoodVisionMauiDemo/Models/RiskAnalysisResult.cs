namespace FoodVisionMauiDemo.Models
{
    public class RiskAnalysisResult
    {
        public List<RiskTagScore> WeeklyScores { get; set; } = new();

        public string TodayRiskLevel { get; set; } = "Low";

        public string DominantRiskTagKey { get; set; } = string.Empty;

        public string DominantRiskDisplayName { get; set; } = "No dominant risk";

        public string Reason { get; set; } = string.Empty;

        public string DataMessage { get; set; } = string.Empty;

        public int TotalMealCount { get; set; }

        public int TodayMealCount { get; set; }

        public bool HasData => TotalMealCount > 0;

        public bool HasLimitedData => TotalMealCount > 0 && TotalMealCount < 3;

        public List<RiskMealSnapshot> Meals { get; set; } = new();
    }
}
