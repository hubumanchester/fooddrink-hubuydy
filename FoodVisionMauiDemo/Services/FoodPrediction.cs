namespace FoodVisionMauiDemo.Services
{
    public class FoodPrediction
    {
        public string Label { get; set; } = string.Empty;
        public float Confidence { get; set; }

        public string DisplayLabel => Label.Replace("_", " ");

        public string DisplayText => $"{DisplayLabel} — {Confidence * 100.0f:F1}%";
    }
}
