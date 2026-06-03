using System.Collections.ObjectModel;
using System.Diagnostics;
using FoodVisionMauiDemo.Models;
using FoodVisionMauiDemo.Services;
using FoodVisionMauiDemo.Views;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Graphics;

namespace FoodVisionMauiDemo.ViewModels
{
    public class InsightsViewModel : BaseViewModel
    {
        private readonly RiskAnalysisService _riskAnalysisService;
        private readonly RecommendationService _recommendationService;
        private readonly SpeechService _speechService;
        private readonly ShakeService _shakeService;
        private readonly FeedbackService _feedbackService;
        private bool _shakeSubscribed;
        private bool _highRiskFeedbackSent;
        private string _todayRiskLevel = "Low";
        private string _reason = "There is not enough meal data yet. Add more meals to see trends.";
        private string _dataMessage = string.Empty;
        private string _trendSummary = "Recent risk tag trend is stable.";
        private string _normalizedRiskScoreText = "0.0";
        private string _dominantRiskTagKey = "balanced";
        private string _statusMessage = "Loading insights...";
        private string _shakeStatusMessage = "Shake refresh is not active yet.";
        private int _shakeRefreshCount;
        private bool _isBusy;
        private bool _hasData;
        private bool _hasLimitedData;

        public InsightsViewModel()
            : this(new RiskAnalysisService(), new RecommendationService(), new SpeechService(), new ShakeService(), new FeedbackService())
        {
        }

        public InsightsViewModel(
            RiskAnalysisService riskAnalysisService,
            RecommendationService recommendationService,
            SpeechService speechService,
            ShakeService shakeService,
            FeedbackService feedbackService)
        {
            _riskAnalysisService = riskAnalysisService;
            _recommendationService = recommendationService;
            _speechService = speechService;
            _shakeService = shakeService;
            _feedbackService = feedbackService;
            FindNearbyCommand = new Command(async () => await GoToNearbyAsync());
            ReadRiskSummaryCommand = new Command(async () => await ReadRiskSummaryAsync(), () => !IsBusy);
        }

        public ObservableCollection<RiskTagScore> WeeklyScores { get; } = new();

        public ObservableCollection<FoodRecommendation> Recommendations { get; } = new();

        public Command FindNearbyCommand { get; }

        public Command ReadRiskSummaryCommand { get; }

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

        public string TrendSummary
        {
            get => _trendSummary;
            private set => SetProperty(ref _trendSummary, value);
        }

        public string NormalizedRiskScoreText
        {
            get => _normalizedRiskScoreText;
            private set => SetProperty(ref _normalizedRiskScoreText, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        public string ShakeStatusMessage
        {
            get => _shakeStatusMessage;
            private set => SetProperty(ref _shakeStatusMessage, value);
        }

        public int ShakeRefreshCount
        {
            get => _shakeRefreshCount;
            private set => SetProperty(ref _shakeRefreshCount, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                    ReadRiskSummaryCommand.ChangeCanExecute();
            }
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
                TrendSummary = analysis.TrendSummary;
                NormalizedRiskScoreText = analysis.NormalizedRiskScore.ToString("0.0");
                HasData = analysis.HasData;
                HasLimitedData = analysis.HasLimitedData;
                StatusMessage = analysis.HasData
                    ? $"Based on {analysis.TotalMealCount} logged meal(s) from the last 7 days."
                    : "There is not enough meal data yet.";

                if (analysis.TodayRiskLevel == "High" && !_highRiskFeedbackSent)
                {
                    _highRiskFeedbackSent = true;
                    await _feedbackService.WarningAsync();
                }
                else if (analysis.TodayRiskLevel != "High")
                {
                    _highRiskFeedbackSent = false;
                }
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
                TrendSummary = "Recent risk tag trend is stable.";
                NormalizedRiskScoreText = "0.0";
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

        public void StartShakeListening()
        {
            if (_shakeSubscribed)
                return;

            _shakeService.Shaken += OnShaken;
            var started = _shakeService.Start();
            _shakeSubscribed = true;
            ShakeStatusMessage = started
                ? "Shake refresh is active. Shake the device to refresh recommendations."
                : "Shake refresh is unavailable on this device.";
        }

        public void StopShakeListening()
        {
            if (!_shakeSubscribed)
                return;

            _shakeService.Shaken -= OnShaken;
            _shakeService.Stop();
            _shakeSubscribed = false;
            ShakeStatusMessage = "Shake refresh is paused.";
        }

        private async void OnShaken(object? sender, EventArgs e)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                ShakeRefreshCount++;
                StatusMessage = "Shake detected. Refreshing recommendations...";
                await LoadAsync();
                StatusMessage = $"Recommendations refreshed by shake. Count: {ShakeRefreshCount}.";
                ShakeStatusMessage = $"Last shake refresh: {DateTime.Now:t}. Shake count: {ShakeRefreshCount}.";
            });
        }

        private async Task ReadRiskSummaryAsync()
        {
            try
            {
                var summary = HasData
                    ? $"Today risk level is {TodayRiskLevel}. {Reason} {TrendSummary} {DataMessage}"
                    : "There is not enough meal data yet. Add more meals to see trends.";

                StatusMessage = "Reading risk summary aloud...";
                await _speechService.SpeakAsync(summary);
                StatusMessage = "Risk summary read aloud.";
            }
            catch (SpeechServiceException ex)
            {
                StatusMessage = ex.Message;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                StatusMessage = "Could not read the risk summary aloud.";
            }
        }
    }
}
