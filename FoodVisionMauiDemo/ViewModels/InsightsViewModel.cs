using System.Collections.ObjectModel;
using System.Diagnostics;
using FoodVisionMauiDemo.Models;
using FoodVisionMauiDemo.Services;
using FoodVisionMauiDemo.Views;

namespace FoodVisionMauiDemo.ViewModels
{
    public class InsightsViewModel : BaseViewModel
    {
        private readonly RiskAnalysisService _riskAnalysisService;
        private readonly RecommendationService _recommendationService;
        private string _todayRiskLevel = "Low";
        private string _reason = "There is not enough meal data yet. Add more meals to see trends.";
        private string _dataMessage = string.Empty;
        private string _dominantRiskTagKey = "balanced";
        private string _statusMessage = "Loading insights...";
        private bool _isBusy;
        private bool _hasData;
        private bool _hasLimitedData;

        public InsightsViewModel()
            : this(new RiskAnalysisService(), new RecommendationService())
        {
        }

        public InsightsViewModel(RiskAnalysisService riskAnalysisService, RecommendationService recommendationService)
        {
            _riskAnalysisService = riskAnalysisService;
            _recommendationService = recommendationService;
            FindNearbyCommand = new Command(async () => await GoToNearbyAsync());
        }

        public ObservableCollection<RiskTagScore> WeeklyScores { get; } = new();

        public ObservableCollection<FoodRecommendation> Recommendations { get; } = new();

        public Command FindNearbyCommand { get; }

        public string TodayRiskLevel
        {
            get => _todayRiskLevel;
            private set => SetProperty(ref _todayRiskLevel, value);
        }

        public string Reason
        {
            get => _reason;
            private set => SetProperty(ref _reason, value);
        }

        public string DataMessage
        {
            get => _dataMessage;
            private set => SetProperty(ref _dataMessage, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set => SetProperty(ref _isBusy, value);
        }

        public bool HasData
        {
            get => _hasData;
            private set
            {
                if (SetProperty(ref _hasData, value))
                    OnPropertyChanged(nameof(HasNoData));
            }
        }

        public bool HasNoData => !HasData;

        public bool HasLimitedData
        {
            get => _hasLimitedData;
            private set => SetProperty(ref _hasLimitedData, value);
        }

        public async Task LoadAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                StatusMessage = "Loading insights...";

                var analysis = await _riskAnalysisService.AnalyzeLastSevenDaysAsync();
                var recommendations = _recommendationService.RecommendAlternatives(analysis);

                ReplaceCollection(WeeklyScores, analysis.WeeklyScores);
                ReplaceCollection(Recommendations, recommendations);

                TodayRiskLevel = analysis.TodayRiskLevel;
                _dominantRiskTagKey = string.IsNullOrWhiteSpace(analysis.DominantRiskTagKey)
                    ? "balanced"
                    : analysis.DominantRiskTagKey;
                Reason = analysis.Reason;
                DataMessage = analysis.DataMessage;
                HasData = analysis.HasData;
                HasLimitedData = analysis.HasLimitedData;
                StatusMessage = analysis.HasData
                    ? $"Based on {analysis.TotalMealCount} logged meal(s) from the last 7 days."
                    : "There is not enough meal data yet.";
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                HasData = false;
                HasLimitedData = false;
                _dominantRiskTagKey = "balanced";
                TodayRiskLevel = "Low";
                Reason = "Insights could not be loaded. Please try again.";
                DataMessage = "There is not enough meal data yet. Add more meals to see trends.";
                StatusMessage = "Could not load insights.";
                ReplaceCollection(WeeklyScores, Array.Empty<RiskTagScore>());
                ReplaceCollection(Recommendations, Array.Empty<FoodRecommendation>());
            }
            finally
            {
                IsBusy = false;
            }
        }

        private static void ReplaceCollection<T>(ObservableCollection<T> collection, IEnumerable<T> values)
        {
            collection.Clear();
            foreach (var value in values)
                collection.Add(value);
        }

        private async Task GoToNearbyAsync()
        {
            var parameters = new Dictionary<string, object>
            {
                ["RiskTag"] = string.IsNullOrWhiteSpace(_dominantRiskTagKey) ? "balanced" : _dominantRiskTagKey
            };

            await Shell.Current.GoToAsync(nameof(NearbyFoodPage), parameters);
        }
    }
}
