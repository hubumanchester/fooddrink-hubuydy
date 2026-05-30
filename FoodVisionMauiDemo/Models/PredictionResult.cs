using SQLite;

namespace FoodVisionMauiDemo.Models
{
    public class PredictionResult
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public int ScanRecordId { get; set; }

        public int Rank { get; set; }

        public string Label { get; set; } = string.Empty;

        public float Confidence { get; set; }

        [Ignore]
        public string DisplayLabel => Label.Replace("_", " ");

        [Ignore]
        public string ConfidenceText => $"{Confidence * 100.0f:F1}%";

        public static PredictionResult FromFoodPrediction(FoodPrediction prediction)
        {
            return new PredictionResult
            {
                Rank = prediction.Rank,
                Label = prediction.Label,
                Confidence = prediction.Confidence
            };
        }
    }
}
