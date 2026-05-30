using FoodVisionMauiDemo.Models;

namespace FoodVisionMauiDemo.Services
{
    public class RecommendationService
    {
        public IReadOnlyList<FoodRecommendation> RecommendAlternatives(RiskAnalysisResult analysis, int count = 3)
        {
            if (!analysis.HasData || string.IsNullOrWhiteSpace(analysis.DominantRiskTagKey))
                return GenericRecommendations(count);

            var recommendations = analysis.Meals
                .Where(meal => meal.Tags.Any(tag => RiskAnalysisService.NormalizeTag(tag) == analysis.DominantRiskTagKey))
                .SelectMany(meal => meal.Alternatives.Select(alternative => new FoodRecommendation
                {
                    Title = alternative,
                    Reason = $"Suggested as a lighter swap for recent {analysis.DominantRiskDisplayName.ToLowerInvariant()} meals."
                }))
                .GroupBy(recommendation => recommendation.Title, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .Take(count)
                .ToList();

            return recommendations.Count > 0
                ? recommendations
                : GenericRecommendations(count);
        }

        private static IReadOnlyList<FoodRecommendation> GenericRecommendations(int count)
        {
            return new List<FoodRecommendation>
            {
                new()
                {
                    Title = "Grilled protein with vegetables",
                    Reason = "A balanced option that can reduce excess sugar, salt, fat, or refined carbohydrate."
                },
                new()
                {
                    Title = "Wholegrain bowl with lean protein",
                    Reason = "Adds fibre and protein while keeping the meal more filling."
                },
                new()
                {
                    Title = "Salad with dressing on the side",
                    Reason = "Keeps vegetables central and helps control sodium and fat."
                }
            }.Take(count).ToList();
        }
    }
}
