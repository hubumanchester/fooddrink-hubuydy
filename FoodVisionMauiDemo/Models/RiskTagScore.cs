namespace FoodVisionMauiDemo.Models
{
    public class RiskTagScore
    {
        public string TagKey { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public double Score { get; set; }

        public string Trend { get; set; } = "Stable";

        public string LevelText => Score switch
        {
            >= 2.5 => "High",
            >= 1.0 => "Moderate",
            _ => "Low"
        };

        public string ShortLabel => TagKey switch
        {
            "high_sugar" => "S",
            "high_fat" => "F",
            "high_salt" => "Na",
            "high_carb" => "C",
            _ => "R"
        };

        public string ScoreWithTrendText => $"{LevelText} · {Score:0.0} · {Trend}";
    }
}
