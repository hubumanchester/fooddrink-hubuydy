namespace FoodVisionMauiDemo.Models
{
    public class NearbyPlace
    {
        public string Name { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public int? DistanceMeters { get; set; }

        public string Tel { get; set; } = string.Empty;

        public string RecommendationReason { get; set; } = string.Empty;

        public string DistanceText => DistanceMeters.HasValue ? $"{DistanceMeters.Value} m" : "Distance unavailable";

        public string TelText => string.IsNullOrWhiteSpace(Tel) ? "No phone listed" : Tel;
    }
}
