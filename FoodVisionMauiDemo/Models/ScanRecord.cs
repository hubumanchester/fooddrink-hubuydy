using SQLite;

namespace FoodVisionMauiDemo.Models
{
    public class ScanRecord
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public string ConfirmedLabel { get; set; } = string.Empty;

        public string FoodName { get; set; } = string.Empty;

        public string ImagePath { get; set; } = string.Empty;

        public string MealType { get; set; } = string.Empty;

        public string Portion { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;
    }
}
