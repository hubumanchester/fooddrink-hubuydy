using System.Diagnostics;
using System.Text.Json;
using FoodVisionMauiDemo.Models;

namespace FoodVisionMauiDemo.Services
{
    public class KnowledgeGraphService
    {
        private Dictionary<string, FoodKnowledgeNode>? _nodesByKey;

        public async Task<FoodKnowledgeResult> GetFoodKnowledgeAsync(string? confirmedLabel)
        {
            if (string.IsNullOrWhiteSpace(confirmedLabel))
            {
                return CreateFallback(
                    "Unknown food",
                    "No confirmed food label was provided. Please go back to Scan and confirm a prediction.");
            }

            try
            {
                await EnsureLoadedAsync();
            }
            catch (FileNotFoundException ex)
            {
                Debug.WriteLine(ex);
                return CreateFallback(
                    confirmedLabel,
                    "The local food knowledge graph is missing. Please check that food_knowledge_graph.json is included in Resources/Raw.");
            }
            catch (JsonException ex)
            {
                Debug.WriteLine(ex);
                return CreateFallback(
                    confirmedLabel,
                    "The local food knowledge graph could not be read. Please check the JSON format.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return CreateFallback(
                    confirmedLabel,
                    "The local food knowledge graph could not be loaded. Please try again later.");
            }

            var normalizedKey = NormalizeKey(confirmedLabel);
            if (_nodesByKey != null && _nodesByKey.TryGetValue(normalizedKey, out var node))
            {
                return new FoodKnowledgeResult
                {
                    Node = NormalizeNode(node),
                    IsFallback = false,
                    Message = "Knowledge graph match found."
                };
            }

            return CreateFallback(
                confirmedLabel,
                "This food is not fully covered in the local knowledge graph yet.");
        }

        private async Task EnsureLoadedAsync()
        {
            if (_nodesByKey != null)
                return;

            await using var stream = await OpenKnowledgeGraphFileAsync();
            var document = await JsonSerializer.DeserializeAsync<FoodKnowledgeGraphDocument>(
                stream,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            var foods = document?.Foods ?? new List<FoodKnowledgeNode>();

            _nodesByKey = foods
                .Where(node => !string.IsNullOrWhiteSpace(node.FoodKey))
                .Select(NormalizeNode)
                .GroupBy(node => NormalizeKey(node.FoodKey))
                .ToDictionary(group => group.Key, group => group.First());
        }

        private static async Task<Stream> OpenKnowledgeGraphFileAsync()
        {
            try
            {
                return await FileSystem.Current.OpenAppPackageFileAsync("food_knowledge_graph.json");
            }
            catch (Exception ex)
            {
                throw new FileNotFoundException("food_knowledge_graph.json was not found in the app package.", ex);
            }
        }

        private static FoodKnowledgeNode NormalizeNode(FoodKnowledgeNode node)
        {
            node.FoodKey = NormalizeKey(node.FoodKey);
            node.DisplayName = string.IsNullOrWhiteSpace(node.DisplayName)
                ? ToDisplayText(node.FoodKey)
                : node.DisplayName.Trim();
            node.Cuisine = string.IsNullOrWhiteSpace(node.Cuisine) ? "Unknown" : node.Cuisine.Trim();
            node.Tags = CleanList(node.Tags).Select(ToDisplayText).ToList();
            node.RiskScores = NormalizeRiskScores(node.RiskScores, node.Tags);
            node.Allergens = CleanList(node.Allergens).Select(ToDisplayText).ToList();
            node.Ingredients = CleanList(node.Ingredients).Select(ToDisplayText).ToList();
            node.Alternatives = CleanList(node.Alternatives).Select(ToDisplayText).ToList();
            node.Explanation = string.IsNullOrWhiteSpace(node.Explanation)
                ? "No detailed explanation is available for this food yet."
                : node.Explanation.Trim();

            return node;
        }

        private static List<string> CleanList(IEnumerable<string>? values)
        {
            return values?
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();
        }

        private static FoodKnowledgeResult CreateFallback(string label, string message)
        {
            var displayName = ToDisplayText(label);

            return new FoodKnowledgeResult
            {
                IsFallback = true,
                Message = message,
                Node = new FoodKnowledgeNode
                {
                    FoodKey = NormalizeKey(label),
                    DisplayName = displayName,
                    Cuisine = "Unknown",
                    Tags = new List<string> { "Not Covered Yet" },
                    RiskScores = new Dictionary<string, double>(),
                    Allergens = new List<string>(),
                    Ingredients = new List<string>(),
                    Alternatives = new List<string>
                    {
                        "Choose a balanced portion",
                        "Add vegetables or lean protein",
                        "Check ingredients if you have allergies"
                    },
                    Explanation = message
                }
            };
        }

        private static string NormalizeKey(string value)
        {
            return value.Trim()
                .ToLowerInvariant()
                .Replace(" ", "_")
                .Replace("-", "_");
        }

        private static Dictionary<string, double> NormalizeRiskScores(
            Dictionary<string, double>? scores,
            IEnumerable<string> tags)
        {
            var normalized = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            if (scores != null)
            {
                foreach (var pair in scores)
                {
                    var key = NormalizeKey(pair.Key);
                    if (IsKnownScoreKey(key))
                        normalized[key] = Math.Clamp(pair.Value, 0, 10);
                }
            }

            foreach (var tag in tags.Select(NormalizeKey))
            {
                if (IsKnownScoreKey(tag) && !normalized.ContainsKey(tag))
                    normalized[tag] = tag is "balanced" or "high_protein" ? 7.0 : 7.2;
            }

            return normalized;
        }

        private static bool IsKnownScoreKey(string key)
        {
            return key is "high_sugar" or "high_fat" or "high_salt" or "high_carb" or "balanced" or "high_protein";
        }

        private static string ToDisplayText(string value)
        {
            var words = NormalizeKey(value).Split('_', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0)
                return "Unknown";

            return string.Join(" ", words.Select(word => char.ToUpperInvariant(word[0]) + word[1..]));
        }

        private class FoodKnowledgeGraphDocument
        {
            public List<FoodKnowledgeNode> Foods { get; set; } = new();
        }
    }
}
