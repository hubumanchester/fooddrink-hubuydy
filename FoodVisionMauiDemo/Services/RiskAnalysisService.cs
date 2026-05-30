using FoodVisionMauiDemo.Models;
using FoodVisionMauiDemo.Repositories;

namespace FoodVisionMauiDemo.Services
{
    public class RiskAnalysisService
    {
        private static readonly Dictionary<string, string> RiskTagLabels = new()
        {
            ["high_sugar"] = "High Sugar",
            ["high_fat"] = "High Fat",
            ["high_salt"] = "High Salt",
            ["high_carb"] = "High Carb"
        };

        private readonly MealLogRepository _mealLogRepository;

        public RiskAnalysisService()
            : this(new MealLogRepository())
        {
        }

        public RiskAnalysisService(MealLogRepository mealLogRepository)
        {
            _mealLogRepository = mealLogRepository;
        }

        public async Task<RiskAnalysisResult> AnalyzeLastSevenDaysAsync()
        {
            var localStart = DateTime.Today.AddDays(-6);
            var utcStart = localStart.ToUniversalTime();
            var meals = (await _mealLogRepository.GetRiskMealsSinceAsync(utcStart)).ToList();

            var weeklyScores = RiskTagLabels
                .Select(pair => new RiskTagScore
                {
                    TagKey = pair.Key,
                    DisplayName = pair.Value,
                    Score = 0
                })
                .ToList();

            if (meals.Count == 0)
            {
                return new RiskAnalysisResult
                {
                    WeeklyScores = weeklyScores,
                    TodayRiskLevel = "Low",
                    Reason = "There is not enough meal data yet. Add more meals to see trends.",
                    DataMessage = "There is not enough meal data yet. Add more meals to see trends.",
                    Meals = meals
                };
            }

            var weeklyScoreMap = weeklyScores.ToDictionary(score => score.TagKey, score => score);
            var todayScoreMap = RiskTagLabels.Keys.ToDictionary(key => key, _ => 0.0);
            var todayStartUtc = DateTime.Today.ToUniversalTime();
            var todayEndUtc = DateTime.Today.AddDays(1).ToUniversalTime();

            foreach (var meal in meals)
            {
                var weight = GetPortionWeight(meal.Portion);
                var normalizedTags = meal.Tags.Select(NormalizeTag).ToHashSet();

                foreach (var tag in RiskTagLabels.Keys)
                {
                    if (!normalizedTags.Contains(tag))
                        continue;

                    weeklyScoreMap[tag].Score += weight;

                    if (meal.CreatedAtUtc >= todayStartUtc && meal.CreatedAtUtc < todayEndUtc)
                        todayScoreMap[tag] += weight;
                }
            }

            var dominantScore = weeklyScores.OrderByDescending(score => score.Score).First();
            var todayTotalScore = todayScoreMap.Values.Sum();
            var todayMaxScore = todayScoreMap.Values.DefaultIfEmpty(0).Max();
            var todayMealCount = meals.Count(meal => meal.CreatedAtUtc >= todayStartUtc && meal.CreatedAtUtc < todayEndUtc);

            var result = new RiskAnalysisResult
            {
                WeeklyScores = weeklyScores,
                TodayRiskLevel = GetTodayRiskLevel(todayTotalScore, todayMaxScore),
                DominantRiskTagKey = dominantScore.Score > 0 ? dominantScore.TagKey : string.Empty,
                DominantRiskDisplayName = dominantScore.Score > 0 ? dominantScore.DisplayName : "No dominant risk",
                TotalMealCount = meals.Count,
                TodayMealCount = todayMealCount,
                Meals = meals,
                DataMessage = meals.Count < 3
                    ? "Based on available records. Add more meals for a clearer trend."
                    : "Based on meals logged in the last 7 days."
            };

            result.Reason = BuildReason(result, dominantScore);

            return result;
        }

        private static string BuildReason(RiskAnalysisResult result, RiskTagScore dominantScore)
        {
            if (!result.HasData)
                return "There is not enough meal data yet. Add more meals to see trends.";

            if (dominantScore.Score <= 0)
                return "No frequent high sugar, high fat, high salt, or high carb pattern appears in the available records.";

            var pattern = dominantScore.TagKey switch
            {
                "high_sugar" => "High-sugar meals appear in the recent log.",
                "high_fat" => "High-fat meals appear frequently this week.",
                "high_salt" => "High-salt meals appear frequently this week.",
                "high_carb" => "High-carb meals appear frequently this week.",
                _ => $"{dominantScore.DisplayName} meals appear frequently this week."
            };

            return $"{pattern} Weighted score: {dominantScore.Score:0.0}.";
        }

        private static string GetTodayRiskLevel(double todayTotalScore, double todayMaxScore)
        {
            if (todayTotalScore >= 3.0 || todayMaxScore >= 2.0)
                return "High";

            if (todayTotalScore >= 1.0)
                return "Moderate";

            return "Low";
        }

        private static double GetPortionWeight(string portion)
        {
            return portion.Trim().ToLowerInvariant() switch
            {
                "small" => 0.5,
                "large" => 1.5,
                _ => 1.0
            };
        }

        public static string NormalizeTag(string tag)
        {
            return tag.Trim()
                .ToLowerInvariant()
                .Replace(" ", "_")
                .Replace("-", "_");
        }
    }
}
