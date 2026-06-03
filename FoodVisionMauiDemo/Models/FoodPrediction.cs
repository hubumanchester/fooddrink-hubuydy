namespace FoodVisionMauiDemo.Models
{
    public class FoodPrediction
    {
        public int Rank { get; set; }

        public string Label { get; set; } = string.Empty;
        public float Confidence { get; set; }

        public string DisplayLabel => Label.Replace("_", " ");

        public string ConfidenceText => $"{Confidence * 100.0f:F1}%";

        public string RankedDisplayLabel => Rank > 0 ? $"{Rank}. {DisplayLabel}" : DisplayLabel;

        public string DisplayText => $"{DisplayLabel} - {ConfidenceText}";

        public bool IsLowConfidence => Confidence > 0 && Confidence < 0.5f;
    }
}
