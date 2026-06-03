using FoodVisionMauiDemo.Models;
using FoodVisionMauiDemo.Repositories;

namespace FoodVisionMauiDemo.Services
{
    public class RiskAnalysisService
    {
        private static readonly Dictionary<string, string> RiskDimensionLabels = new()
        {
            ["high_sugar"] = "Sugar load",
            ["high_fat"] = "Fat load",
            ["high_salt"] = "Salt load",
            ["high_carb"] = "Refined carb load"
        };

        private readonly MealLogRepository _mealLogRepository;
        private readonly KnowledgeGraphService _knowledgeGraphService;

        public RiskAnalysisService()
            : this(new MealLogRepository(), new KnowledgeGraphService())
        {
        }

        public RiskAnalysisService(
            MealLogRepository mealLogRepository,
            KnowledgeGraphService knowledgeGraphService)
        {
            _mealLogRepository = mealLogRepository;
            _knowledgeGraphService = knowledgeGraphService;
        }

        public async Task<RiskAnalysisResult> AnalyzeLastSevenDaysAsync()
        {
            var localStart = DateTime.Today.AddDays(-6);
            var utcStart = localStart.ToUniversalTime();
            var meals = (await _mealLogRepository.GetRiskMealsSinceAsync(utcStart)).ToList();
            var weeklyScores = CreateEmptyWeeklyScores();

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
            var todayScoreMap = RiskDimensionLabels.Keys.ToDictionary(key => key, _ => 0.0);
            var recentScoreMap = RiskDimensionLabels.Keys.ToDictionary(key => key, _ => 0.0);
            var earlierScoreMap = RiskDimensionLabels.Keys.ToDictionary(key => key, _ => 0.0);

            var todayStartUtc = DateTime.Today.ToUniversalTime();
            var todayEndUtc = DateTime.Today.AddDays(1).ToUniversalTime();
            var recentStartUtc = DateTime.Today.AddDays(-2).ToUniversalTime();
            var todayMealCount = 0;
            var weightedMealExposure = 0.0;

            foreach (var meal in meals)
            {
                var scores = await GetEffectiveRiskScoresAsync(meal);
                var portionWeight = GetPortionWeight(meal.Portion);
                var recencyWeight = GetRecencyWeight(meal.CreatedAtUtc);
                var protectiveFactor = GetProtectiveFactor(scores);
                var coOccurrenceMultiplier = GetCoOccurrenceMultiplier(scores);
                var isToday = meal.CreatedAtUtc >= todayStartUtc && meal.CreatedAtUtc < todayEndUtc;

                weightedMealExposure += portionWeight * recencyWeight;
                if (isToday)
                    todayMealCount++;

                foreach (var dimension in RiskDimensionLabels.Keys)
                {
                    var dimensionScore = GetScore(scores, dimension);
                    if (dimensionScore <= 0)
                        continue;

                    var weeklyContribution = CalculateContribution(
                        dimensionScore,
                        portionWeight,
                        recencyWeight,
                        protectiveFactor,
                        coOccurrenceMultiplier);

                    weeklyScoreMap[dimension].Score += weeklyContribution;

                    if (isToday)
                    {
                        todayScoreMap[dimension] += CalculateContribution(
                            dimensionScore,
                            portionWeight,
                            recencyWeight: 1.0,
                            protectiveFactor,
                            coOccurrenceMultiplier);
                    }

                    if (meal.CreatedAtUtc >= recentStartUtc)
                        recentScoreMap[dimension] += weeklyContribution;
                    else
                        earlierScoreMap[dimension] += weeklyContribution;
                }
            }

            foreach (var score in weeklyScores)
            {
                var recentDailyAverage = recentScoreMap[score.TagKey] / 3.0;
                var earlierDailyAverage = earlierScoreMap[score.TagKey] / 4.0;
                score.Trend = GetTrend(recentDailyAverage, earlierDailyAverage);
            }

            var dominantScore = weeklyScores.OrderByDescending(score => score.Score).First();
            var weeklyBurden = weeklyScores.Sum(score => score.Score);
            var weeklyAverageBurden = weeklyBurden / Math.Max(1.0, weightedMealExposure);
            var normalizedRiskScore = Math.Clamp(weeklyAverageBurden * 25.0, 0.0, 100.0);
            var todayBurden = todayScoreMap.Values.Sum();
            var todayAverageBurden = todayMealCount > 0 ? todayBurden / todayMealCount : 0.0;
            var todayMaxDimension = todayScoreMap.Values.DefaultIfEmpty(0).Max();
            var trendSummary = BuildTrendSummary(weeklyScores);

            var result = new RiskAnalysisResult
            {
                WeeklyScores = weeklyScores,
                TodayRiskLevel = GetTodayRiskLevel(
                    normalizedRiskScore,
                    todayBurden,
                    todayAverageBurden,
                    todayMaxDimension,
                    todayMealCount),
                DominantRiskTagKey = dominantScore.Score > 0 ? dominantScore.TagKey : string.Empty,
                DominantRiskDisplayName = dominantScore.Score > 0 ? dominantScore.DisplayName : "No dominant risk",
                NormalizedRiskScore = normalizedRiskScore,
                TrendSummary = trendSummary,
                TotalMealCount = meals.Count,
                TodayMealCount = todayMealCount,
                Meals = meals,
                DataMessage = meals.Count < 3
                    ? "Based on available records. Add more meals for a clearer trend."
                    : "Based on weighted food scores from meals logged in the last 7 days."
            };

            result.Reason = BuildReason(result, dominantScore, todayBurden, todayAverageBurden);

            return result;
        }

        public static double GetRiskScoreForTag(RiskMealSnapshot meal, string riskTag)
        {
            return GetScore(GetEffectiveRiskScores(meal), NormalizeTag(riskTag));
        }

        private static List<RiskTagScore> CreateEmptyWeeklyScores()
        {
            return RiskDimensionLabels
                .Select(pair => new RiskTagScore
                {
                    TagKey = pair.Key,
                    DisplayName = pair.Value,
                    Score = 0
                })
                .ToList();
        }

        private static double CalculateContribution(
            double dimensionScore,
            double portionWeight,
            double recencyWeight,
            double protectiveFactor,
            double coOccurrenceMultiplier)
        {
            var intensity = Math.Clamp(dimensionScore, 0, 10) / 10.0;
            return intensity * portionWeight * recencyWeight * coOccurrenceMultiplier * (1.0 - protectiveFactor);
        }

        private async Task<Dictionary<string, double>> GetEffectiveRiskScoresAsync(RiskMealSnapshot meal)
        {
            if (meal.RiskScores.Count > 0)
                return GetEffectiveRiskScores(meal);

            if (string.IsNullOrWhiteSpace(meal.FoodKey))
                return GetEffectiveRiskScores(meal);

            var result = await _knowledgeGraphService.GetFoodKnowledgeAsync(meal.FoodKey);
            var scores = result.Node.RiskScores
                .ToDictionary(
                    pair => NormalizeTag(pair.Key),
                    pair => Math.Clamp(pair.Value, 0, 10));

            if (scores.Count == 0)
                scores = GetEffectiveRiskScores(meal);

            meal.RiskScores = scores;
            return scores;
        }

        private static Dictionary<string, double> GetEffectiveRiskScores(RiskMealSnapshot meal)
        {
            if (meal.RiskScores.Count > 0)
            {
                return meal.RiskScores
                    .ToDictionary(
                        pair => NormalizeTag(pair.Key),
                        pair => Math.Clamp(pair.Value, 0, 10));
            }

            var scores = new Dictionary<string, double>();
            foreach (var tag in meal.Tags.Select(NormalizeTag))
            {
                if (tag is "high_sugar" or "high_fat" or "high_salt" or "high_carb")
                    scores[tag] = 7.2;
                else if (tag == "balanced")
                    scores[tag] = 7.0;
                else if (tag == "high_protein")
                    scores[tag] = 7.0;
            }

            return scores;
        }

        private static double GetScore(IReadOnlyDictionary<string, double> scores, string key)
        {
            return scores.TryGetValue(NormalizeTag(key), out var score)
                ? Math.Clamp(score, 0, 10)
                : 0.0;
        }

        private static double GetProtectiveFactor(IReadOnlyDictionary<string, double> scores)
        {
            var balanceSupport = GetScore(scores, "balanced") / 10.0;
            var proteinSupport = GetScore(scores, "high_protein") / 10.0;

            return Math.Min(0.25, balanceSupport * 0.15 + proteinSupport * 0.10);
        }

        private static double GetCoOccurrenceMultiplier(IReadOnlyDictionary<string, double> scores)
        {
            var strongRiskCount = RiskDimensionLabels.Keys.Count(key => GetScore(scores, key) >= 7.0);
            return 1.0 + Math.Min(0.18, Math.Max(0, strongRiskCount - 1) * 0.06);
        }

        private static string BuildReason(
            RiskAnalysisResult result,
            RiskTagScore dominantScore,
            double todayBurden,
            double todayAverageBurden)
        {
            if (!result.HasData)
                return "There is not enough meal data yet. Add more meals to see trends.";

            if (dominantScore.Score <= 0)
                return "No frequent high sugar, high fat, high salt, or high carb pattern appears in the available records.";

            var pattern = dominantScore.TagKey switch
            {
                "high_sugar" => "Sugar load is the strongest recent risk signal.",
                "high_fat" => "Fat load is the strongest recent risk signal.",
                "high_salt" => "Salt load is the strongest recent risk signal.",
                "high_carb" => "Refined carbohydrate load is the strongest recent risk signal.",
                _ => $"{dominantScore.DisplayName} is the strongest recent risk signal."
            };

            return $"{pattern} 7-day weighted score: {dominantScore.Score:0.0}. Today load: {todayBurden:0.0}. Average per meal today: {todayAverageBurden:0.0}. Overall balance indicator: {result.NormalizedRiskScore:0.0}/100.";
        }

        private static string GetTodayRiskLevel(
            double normalizedRiskScore,
            double todayBurden,
            double todayAverageBurden,
            double todayMaxDimension,
            int todayMealCount)
        {
            if (todayMealCount == 0)
                return normalizedRiskScore >= 50.0 ? "Moderate" : "Low";

            if (todayBurden >= 3.4 ||
                todayAverageBurden >= 2.4 ||
                todayMaxDimension >= 1.8 ||
                normalizedRiskScore >= 75.0)
            {
                return "High";
            }

            if (todayBurden >= 1.4 ||
                todayAverageBurden >= 1.1 ||
                todayMaxDimension >= 0.9 ||
                normalizedRiskScore >= 38.0)
            {
                return "Moderate";
            }

            return "Low";
        }

        private static string GetTrend(double recentDailyAverage, double earlierDailyAverage)
        {
            if (recentDailyAverage >= earlierDailyAverage + 0.15)
                return "Increasing";

            if (earlierDailyAverage >= recentDailyAverage + 0.15)
                return "Decreasing";

            return "Stable";
        }

        private static string BuildTrendSummary(IEnumerable<RiskTagScore> scores)
        {
            var increasing = scores
                .Where(score => score.Score > 0 && score.Trend == "Increasing")
                .Select(score => score.DisplayName)
                .ToList();

            if (increasing.Count > 0)
                return $"{string.Join(", ", increasing)} trend is increasing.";

            var decreasing = scores
                .Where(score => score.Score > 0 && score.Trend == "Decreasing")
                .Select(score => score.DisplayName)
                .ToList();

            return decreasing.Count > 0
                ? $"{string.Join(", ", decreasing)} trend is decreasing."
                : "Recent weighted risk trend is stable.";
        }

        private static double GetPortionWeight(string portion)
        {
            return portion.Trim().ToLowerInvariant() switch
            {
                "small" => 0.7,
                "large" => 1.4,
                _ => 1.0
            };
        }

        private static double GetRecencyWeight(DateTime createdAtUtc)
        {
            var localDate = createdAtUtc.ToLocalTime().Date;
            var daysAgo = Math.Max(0, (DateTime.Today - localDate).Days);

            return daysAgo switch
            {
                0 => 1.15,
                <= 2 => 1.05,
                <= 4 => 0.95,
                _ => 0.85
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
