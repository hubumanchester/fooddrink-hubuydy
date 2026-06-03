namespace FoodVisionMauiDemo.Models
{
    public class RiskMealSnapshot
    {
        public int ScanRecordId { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public string FoodKey { get; set; } = string.Empty;

        public string FoodName { get; set; } = string.Empty;

        public string Portion { get; set; } = string.Empty;

        public List<string> Tags { get; set; } = new();

        public Dictionary<string, double> RiskScores { get; set; } = new();

        public List<string> Allergens { get; set; } = new();

        public List<string> Alternatives { get; set; } = new();
    }
}
