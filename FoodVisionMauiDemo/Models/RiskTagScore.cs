namespace FoodVisionMauiDemo.Models
{
    public class RiskTagScore
    {
        public string TagKey { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public double Score { get; set; }

        public string ScoreText => Score.ToString("0.0");
    }
}
