using System.Text.Json;
using SQLite;

namespace FoodVisionMauiDemo.Models
{
    public class FoodNodeSnapshot
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public int ScanRecordId { get; set; }

        public string FoodKey { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public string Cuisine { get; set; } = string.Empty;

        public string TagsJson { get; set; } = "[]";

        public string AllergensJson { get; set; } = "[]";

        public string IngredientsJson { get; set; } = "[]";

        public string AlternativesJson { get; set; } = "[]";

        public string Explanation { get; set; } = string.Empty;

        public bool IsFallback { get; set; }

        [Ignore]
        public List<string> Tags
        {
            get => DeserializeList(TagsJson);
            set => TagsJson = SerializeList(value);
        }

        [Ignore]
        public List<string> Allergens
        {
            get => DeserializeList(AllergensJson);
            set => AllergensJson = SerializeList(value);
        }

        [Ignore]
        public List<string> Ingredients
        {
            get => DeserializeList(IngredientsJson);
            set => IngredientsJson = SerializeList(value);
        }

        [Ignore]
        public List<string> Alternatives
        {
            get => DeserializeList(AlternativesJson);
            set => AlternativesJson = SerializeList(value);
        }

        public static FoodNodeSnapshot FromKnowledgeNode(FoodKnowledgeNode node, bool isFallback)
        {
            return new FoodNodeSnapshot
            {
                FoodKey = node.FoodKey,
                DisplayName = node.DisplayName,
                Cuisine = node.Cuisine,
                Tags = node.Tags,
                Allergens = node.Allergens,
                Ingredients = node.Ingredients,
                Alternatives = node.Alternatives,
                Explanation = node.Explanation,
                IsFallback = isFallback
            };
        }

        private static string SerializeList(IEnumerable<string>? values)
        {
            return JsonSerializer.Serialize(values ?? Array.Empty<string>());
        }

        private static List<string> DeserializeList(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new List<string>();

            try
            {
                return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }
    }
}
