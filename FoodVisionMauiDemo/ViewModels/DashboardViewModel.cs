using System.Diagnostics;
using FoodVisionMauiDemo.Models;
using FoodVisionMauiDemo.Repositories;
using FoodVisionMauiDemo.Services;
using FoodVisionMauiDemo.Views;
using Microsoft.Maui.Graphics;

namespace FoodVisionMauiDemo.ViewModels
{
    public class DashboardViewModel : BaseViewModel
    {
        private readonly MealLogRepository _mealLogRepository;
        private readonly RiskAnalysisService _riskAnalysisService;
        private string _statusMessage = "Loading dashboard...";
        private string _todayRiskLevel = "Low";
        private string _primaryRiskTag = "No dominant risk";
        private string _todayRiskSummary = "No meals logged today.";
        private string _recentMealSummary = "No recent meal yet.";
        private int _todayMealCount;
        private bool _isBusy;
        private bool _hasRecentMeal;
        private string _dominantRiskTagKey = "balanced";

        public DashboardViewModel()
            : this(new MealLogRepository(), new RiskAnalysisService())
        {
        }

        public DashboardViewModel(MealLogRepository mealLogRepository, RiskAnalysisService riskAnalysisService)
        {
            _mealLogRepository = mealLogRepository;
            _riskAnalysisService = riskAnalysisService;
            ScanFoodCommand = new Command(async () => await Shell.Current.GoToAsync("//VisionScanPage"));
            ViewTodayLogCommand = new Command(async () => await Shell.Current.GoToAsync("//DailyLogPage"));
            ViewInsightsCommand = new Command(async () => await Shell.Current.GoToAsync("//InsightsPage"));
            FindNearbyCommand = new Command(async () => await GoToNearbyAsync());
        }

        public string AppName => "NutriLens KG";

        public string Tagline => "Vision-based food scanner and dietary risk assistant";

        public string CurrentPhaseStatus => "Scan, explain, save, analyse, recommend, and find nearby balanced options.";

        public Command ScanFoodCommand { get; }

        public Command ViewTodayLogCommand { get; }

        public Command ViewInsightsCommand { get; }

        public Command FindNearbyCommand { get; }

        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        public string TodayRiskLevel
        {
            get => _todayRiskLevel;
            private set
            {
                if (SetProperty(ref _todayRiskLevel, value))
                    OnPropertyChanged(nameof(RiskLevelTextColor));
            }
        }

        public Color RiskLevelTextColor => TodayRiskLevel switch
        {
            "High" => Color.FromArgb("#EF4444"),
            "Moderate" => Color.FromArgb("#F59E0B"),
            _ => Color.FromArgb("#16A34A")
        };

        public int TodayMealCount
        {
            get => _todayMealCount;
            private set => SetProperty(ref _todayMealCount, value);
        }

        public string PrimaryRiskTag
        {
            get => _primaryRiskTag;
            private set => SetProperty(ref _primaryRiskTag, value);
        }

        public string TodayRiskSummary
        {
            get => _todayRiskSummary;
            private set => SetProperty(ref _todayRiskSummary, value);
        }

        public string RecentMealSummary
        {
            get => _recentMealSummary;
            private set => SetProperty(ref _recentMealSummary, value);
        }

        public bool HasRecentMeal
        {
            get => _hasRecentMeal;
            private set => SetProperty(ref _hasRecentMeal, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set => SetProperty(ref _isBusy, value);
        }

        public async Task LoadAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                StatusMessage = "Loading dashboard...";

                var todayMeals = await _mealLogRepository.GetTodayMealsAsync();
                var recentMeals = await _mealLogRepository.GetRecentMealsAsync(1);
                var analysis = await _riskAnalysisService.AnalyzeLastSevenDaysAsync();

                TodayMealCount = todayMeals.Count;
                TodayRiskLevel = analysis.TodayRiskLevel;
                PrimaryRiskTag = string.IsNullOrWhiteSpace(analysis.DominantRiskTagKey)
                    ? "No dominant risk"
                    : analysis.DominantRiskDisplayName;
                _dominantRiskTagKey = string.IsNullOrWhiteSpace(analysis.DominantRiskTagKey)
                    ? "balanced"
                    : analysis.DominantRiskTagKey;
                TodayRiskSummary = BuildTodayRiskSummary(todayMeals);
                SetRecentMeal(recentMeals.FirstOrDefault());

                StatusMessage = todayMeals.Count > 0
                    ? "Dashboard updated from your local meal log."
                    : "No meals logged today. Scan your first food.";
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                StatusMessage = "Could not load dashboard data. Please try again.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task GoToNearbyAsync()
        {
            var parameters = new Dictionary<string, object>
            {
                ["RiskTag"] = _dominantRiskTagKey
            };

            await Shell.Current.GoToAsync(nameof(NearbyFoodPage), parameters);
        }

        private void SetRecentMeal(MealLogItem? meal)
        {
            HasRecentMeal = meal != null;
            RecentMealSummary = meal == null
                ? "No recent meal yet."
                : $"{meal.FoodName} - {meal.MealSummary} at {meal.DateTimeText}. {meal.RiskTagsText}";
        }

        private static string BuildTodayRiskSummary(IEnumerable<MealLogItem> meals)
        {
            var counts = meals
                .SelectMany(meal => meal.RiskTags)
                .Where(RiskLabelFormatter.IsRiskLoadTag)
                .GroupBy(RiskLabelFormatter.ToLoadLabel, StringComparer.OrdinalIgnoreCase)
                .Select(group => $"{group.Key}: {group.Count()} meal{(group.Count() == 1 ? string.Empty : "s")}")
                .ToList();

            return counts.Count > 0
                ? string.Join(", ", counts)
                : "No repeated risk load signals logged today.";
        }
    }
}
