namespace FoodVisionMauiDemo.Models
{
    public class RiskScoreDisplayItem
    {
        public string Key { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public double Score { get; set; }

        public string ScoreText => $"{Score:0.0}/10";
    }
}
