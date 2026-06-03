using FoodVisionMauiDemo.Models;

namespace FoodVisionMauiDemo.Services
{
    public class RecommendationService
    {
        private readonly AppSettingsService _settingsService;

        public RecommendationService()
            : this(new AppSettingsService())
        {
        }

        public RecommendationService(AppSettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        public IReadOnlyList<FoodRecommendation> RecommendAlternatives(RiskAnalysisResult analysis, int count = 3)
        {
            if (!analysis.HasData || string.IsNullOrWhiteSpace(analysis.DominantRiskTagKey))
                return ApplyPreferenceFilter(GenericRecommendations(count), count);

            var recommendations = analysis.Meals
                .Where(meal => RiskAnalysisService.GetRiskScoreForTag(meal, analysis.DominantRiskTagKey) >= 5.0 ||
                               meal.Tags.Any(tag => RiskAnalysisService.NormalizeTag(tag) == analysis.DominantRiskTagKey))
                .SelectMany(meal => meal.Alternatives.Select(alternative => new FoodRecommendation
                {
                    Title = alternative,
                    Reason = $"Suggested as a lighter swap for recent {analysis.DominantRiskDisplayName.ToLowerInvariant()} meals."
                }))
                .GroupBy(recommendation => recommendation.Title, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .Take(count)
                .ToList();

            recommendations = ApplyPreferenceFilter(recommendations, count).ToList();

            return recommendations.Count > 0
                ? recommendations
                : ApplyPreferenceFilter(GenericRecommendations(count), count);
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

        private IReadOnlyList<FoodRecommendation> ApplyPreferenceFilter(
            IEnumerable<FoodRecommendation> recommendations,
            int count)
        {
            var blockedTerms = GetBlockedTerms();
            var filtered = recommendations
                .Where(recommendation => !blockedTerms.Any(term =>
                    recommendation.Title.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    recommendation.Reason.Contains(term, StringComparison.OrdinalIgnoreCase)))
                .Take(count)
                .ToList();

            if (filtered.Count > 0)
                return filtered;

            return new List<FoodRecommendation>
            {
                new()
                {
                    Title = "Simple vegetable bowl",
                    Reason = "A flexible balanced option that can be adjusted around your saved dietary preferences."
                },
                new()
                {
                    Title = "Fresh salad with lean protein",
                    Reason = "Keeps the recommendation broad when allergen or preference filters remove specific swaps."
                },
                new()
                {
                    Title = "Steamed vegetables with a plain protein",
                    Reason = "A lower-risk fallback when recent meals repeat sugar, salt, fat, or refined carbohydrate."
                }
            }.Take(count).ToList();
        }

        private IReadOnlyList<string> GetBlockedTerms()
        {
            var terms = new List<string>();

            if (_settingsService.DietaryPreference == "Gluten-free" || _settingsService.AvoidGluten)
                terms.AddRange(new[] { "bread", "toast", "wrap", "wheat", "wholegrain", "crust", "noodle", "pasta" });

            if (_settingsService.DietaryPreference == "Dairy-free" || _settingsService.AvoidDairy)
                terms.AddRange(new[] { "milk", "cheese", "cream", "yogurt", "dairy" });

            if (_settingsService.AvoidNuts)
                terms.AddRange(new[] { "nut", "nuts", "almond", "peanut" });

            if (_settingsService.AvoidEgg)
                terms.Add("egg");

            return terms;
        }
    }
}
