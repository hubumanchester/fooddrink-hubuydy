namespace FoodVisionMauiDemo.Models
{
    public class MealLogItem
    {
        public int Id { get; set; }

        public string FoodName { get; set; } = string.Empty;

        public string ConfirmedLabel { get; set; } = string.Empty;

        public string MealType { get; set; } = string.Empty;

        public string Portion { get; set; } = string.Empty;

        public string ImagePath { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; }

        public List<string> RiskTags { get; set; } = new();

        public string TimeText => CreatedAtUtc.ToLocalTime().ToString("HH:mm");

        public string DateTimeText => CreatedAtUtc.ToLocalTime().ToString("MMM d, HH:mm");

        public string RiskTagsText => RiskTags.Count > 0 ? string.Join(", ", RiskTags) : "No risk tags";

        public string MealSummary => $"{MealType} - {Portion}";
    }
}
