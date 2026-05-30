namespace FoodVisionMauiDemo.Models
{
    public class FoodKnowledgeResult
    {
        public FoodKnowledgeNode Node { get; set; } = new();

        public bool IsFallback { get; set; }

        public string Message { get; set; } = string.Empty;
    }
}
