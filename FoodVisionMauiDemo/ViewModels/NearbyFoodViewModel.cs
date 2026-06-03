using System.Collections.ObjectModel;
using System.Diagnostics;
using FoodVisionMauiDemo.Models;
using FoodVisionMauiDemo.Services;
using Microsoft.Maui.ApplicationModel;

namespace FoodVisionMauiDemo.ViewModels
{
    public class NearbyFoodViewModel : BaseViewModel
    {
        private readonly AppSettingsService _appSettingsService;
        private readonly LocationService _locationService;
        private readonly RiskToPlaceQueryService _riskToPlaceQueryService;
        private readonly AmapPlaceSearchService _amapPlaceSearchService;
        private readonly ShakeService _shakeService;
        private readonly FeedbackService _feedbackService;
        private string _riskTag = "balanced";
        private string _riskDisplayName = "Balanced";
        private string _searchKeywords = "餐厅|咖啡|超市";
        private string _customKeywords = string.Empty;
        private string _activeSearchKeywords = "餐厅|咖啡|超市";
        private string _searchModeLabel = "Using suggested keywords";
        private string _statusMessage = "Ready to search nearby balanced options.";
        private string _shakeStatusMessage = "Shake refresh is not active yet.";
        private string _shakeFeedbackMessage = "Shake detected.";
        private int _shakeRefreshCount;
        private int _shakeFeedbackPulse;
        private int _shakeFeedbackVersion;
        private bool _isBusy;
        private bool _hasError;
        private bool _isUsingCustomKeywords;
        private bool _shakeSubscribed;
        private bool _isShakeFeedbackVisible;

        public NearbyFoodViewModel()
            : this(
                new AppSettingsService(),
                new LocationService(),
                new RiskToPlaceQueryService(),
                new AmapPlaceSearchService(),
                new ShakeService(),
                new FeedbackService())
        {
        }

        public NearbyFoodViewModel(
            AppSettingsService appSettingsService,
            LocationService locationService,
            RiskToPlaceQueryService riskToPlaceQueryService,
            AmapPlaceSearchService amapPlaceSearchService,
            ShakeService shakeService,
            FeedbackService feedbackService)
        {
            _appSettingsService = appSettingsService;
            _locationService = locationService;
            _riskToPlaceQueryService = riskToPlaceQueryService;
            _amapPlaceSearchService = amapPlaceSearchService;
            _shakeService = shakeService;
            _feedbackService = feedbackService;

            RefreshPlacesCommand = new Command(async () => await LoadPlacesAsync(), () => !IsBusy);
            SearchCustomPlacesCommand = new Command(async () => await SearchCustomPlacesAsync(), () => !IsBusy);
            UseRecommendedKeywordsCommand = new Command(async () => await UseRecommendedKeywordsAsync(), () => !IsBusy);
        }

        public ObservableCollection<NearbyPlace> Places { get; } = new();

        public Command RefreshPlacesCommand { get; }

        public Command SearchCustomPlacesCommand { get; }

        public Command UseRecommendedKeywordsCommand { get; }

        public string RiskDisplayName
        {
            get => _riskDisplayName;
            private set => SetProperty(ref _riskDisplayName, value);
        }

        public string SearchKeywords
        {
            get => _searchKeywords;
            private set => SetProperty(ref _searchKeywords, value);
        }

        public string CustomKeywords
        {
            get => _customKeywords;
            set
            {
                if (SetProperty(ref _customKeywords, value))
                    UpdateActiveSearchState();
            }
        }

        public string ActiveSearchKeywords
        {
            get => _activeSearchKeywords;
            private set => SetProperty(ref _activeSearchKeywords, value);
        }

        public string SearchModeLabel
        {
            get => _searchModeLabel;
            private set => SetProperty(ref _searchModeLabel, value);
        }

        public bool IsUsingCustomKeywords
        {
            get => _isUsingCustomKeywords;
            private set => SetProperty(ref _isUsingCustomKeywords, value);
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

        public string ShakeFeedbackMessage
        {
            get => _shakeFeedbackMessage;
            private set => SetProperty(ref _shakeFeedbackMessage, value);
        }

        public int ShakeRefreshCount
        {
            get => _shakeRefreshCount;
            private set => SetProperty(ref _shakeRefreshCount, value);
        }

        public int ShakeFeedbackPulse
        {
            get => _shakeFeedbackPulse;
            private set => SetProperty(ref _shakeFeedbackPulse, value);
        }

        public bool IsShakeFeedbackVisible
        {
            get => _isShakeFeedbackVisible;
            private set => SetProperty(ref _isShakeFeedbackVisible, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    RefreshPlacesCommand.ChangeCanExecute();
                    SearchCustomPlacesCommand.ChangeCanExecute();
                    UseRecommendedKeywordsCommand.ChangeCanExecute();
                }
            }
        }

        public bool HasPlaces => Places.Count > 0;

        public bool HasNoPlaces => Places.Count == 0;

        public bool HasError
        {
            get => _hasError;
            private set => SetProperty(ref _hasError, value);
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            var requestedRiskTag = query.TryGetValue("RiskTag", out var riskTagValue) && riskTagValue is string riskTag
                ? riskTag
                : "balanced";

            _riskTag = _riskToPlaceQueryService.NormalizeRiskTag(requestedRiskTag);
            RiskDisplayName = _riskToPlaceQueryService.GetDisplayRisk(_riskTag);
            SearchKeywords = _riskToPlaceQueryService.GetKeywordsForRisk(_riskTag);
            UpdateActiveSearchState();
        }

        public async Task LoadPlacesAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                HasError = false;
                ClearPlaces();

                if (!_appSettingsService.UseLiveApi)
                {
                    HasError = true;
                    StatusMessage = "Live nearby search is disabled.";
                    return;
                }

                if (!AmapApiOptions.HasConfiguredApiKey)
                {
                    HasError = true;
                    StatusMessage = "Nearby search is unavailable due to API configuration.";
                    return;
                }

                StatusMessage = "Getting your current location...";
                var location = await _locationService.GetCurrentLocationAsync();

                var keywords = GetEffectiveKeywords();
                if (string.IsNullOrWhiteSpace(keywords))
                {
                    HasError = true;
                    StatusMessage = "Enter a keyword or use the suggested recommendation.";
                    return;
                }

                StatusMessage = $"Searching live nearby places for {FormatKeywords(keywords)}...";
                var reason = BuildRecommendationReason();
                var places = await _amapPlaceSearchService.SearchAroundAsync(
                    location,
                    keywords,
                    reason,
                    allowBroadFallback: !IsUsingCustomKeywords);

                foreach (var place in places)
                    Places.Add(place);

                OnPropertyChanged(nameof(HasPlaces));
                OnPropertyChanged(nameof(HasNoPlaces));
                StatusMessage = $"Found {Places.Count} nearby place(s) for {FormatKeywords(keywords)}.";
            }
            catch (NearbySearchException ex)
            {
                Debug.WriteLine(ex);
                HasError = true;
                StatusMessage = IsUsingCustomKeywords &&
                    ex.UserMessage == "No nearby places found for this recommendation."
                        ? "No nearby places found for this keyword."
                        : ex.UserMessage;
                ClearPlaces();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                HasError = true;
                StatusMessage = "Could not connect to the live nearby search service.";
                ClearPlaces();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SearchCustomPlacesAsync()
        {
            if (string.IsNullOrWhiteSpace(CustomKeywords))
            {
                HasError = true;
                StatusMessage = "Enter a keyword, such as salad, sushi, hotpot, or coffee.";
                ClearPlaces();
                return;
            }

            UpdateActiveSearchState();
            await LoadPlacesAsync();
        }

        private async Task UseRecommendedKeywordsAsync()
        {
            CustomKeywords = string.Empty;
            UpdateActiveSearchState();
            await LoadPlacesAsync();
        }

        public void StartShakeListening()
        {
            if (_shakeSubscribed)
                return;

            _shakeService.Shaken += OnShaken;
            var started = _shakeService.Start();
            _shakeSubscribed = true;
            ShakeStatusMessage = started
                ? "Shake refresh is active. Shake the device to refresh nearby food."
                : "Shake refresh is unavailable on this device or emulator.";
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
            await MainThread.InvokeOnMainThreadAsync(HandleShakeAsync);
        }

        private async Task HandleShakeAsync()
        {
            ShakeRefreshCount++;
            var feedbackVersion = ShowShakeFeedback("Shake detected. Refreshing nearby places...");
            await _feedbackService.SuccessAsync();

            if (IsBusy)
            {
                StatusMessage = "Shake detected. Nearby search is already running.";
                ShakeStatusMessage = $"Last shake: {DateTime.Now:t}. Current search is still running.";
                ShakeFeedbackMessage = "Shake received. Current search is already running.";
                await HideShakeFeedbackAfterDelayAsync(feedbackVersion);
                return;
            }

            StatusMessage = "Shake detected. Refreshing nearby places...";
            await LoadPlacesAsync();
            StatusMessage = $"Nearby places refreshed by shake. Count: {ShakeRefreshCount}.";
            ShakeStatusMessage = $"Last shake refresh: {DateTime.Now:t}. Shake count: {ShakeRefreshCount}.";
            ShakeFeedbackMessage = "Nearby places refreshed.";
            await HideShakeFeedbackAfterDelayAsync(feedbackVersion);
        }

        private int ShowShakeFeedback(string message)
        {
            ShakeFeedbackMessage = message;
            IsShakeFeedbackVisible = true;
            ShakeFeedbackPulse++;
            return ++_shakeFeedbackVersion;
        }

        private async Task HideShakeFeedbackAfterDelayAsync(int feedbackVersion)
        {
            await Task.Delay(1700);
            if (feedbackVersion == _shakeFeedbackVersion)
                IsShakeFeedbackVisible = false;
        }

        private string BuildRecommendationReason()
        {
            if (IsUsingCustomKeywords)
                return $"Matched your custom search: {FormatKeywords(GetEffectiveKeywords())}.";

            return _riskTag == "balanced"
                ? "General balanced nearby option."
                : $"Recommended to balance recent {RiskDisplayName.ToLowerInvariant()} pattern.";
        }

        private string GetEffectiveKeywords()
        {
            return string.IsNullOrWhiteSpace(CustomKeywords)
                ? SearchKeywords
                : NormalizeKeywordInput(CustomKeywords);
        }

        private void UpdateActiveSearchState()
        {
            IsUsingCustomKeywords = !string.IsNullOrWhiteSpace(CustomKeywords);
            ActiveSearchKeywords = GetEffectiveKeywords();
            SearchModeLabel = IsUsingCustomKeywords
                ? "Using custom keyword"
                : "Using suggested keywords";
        }

        private static string NormalizeKeywordInput(string keywords)
        {
            return string.Join("|", keywords
                .Split(new[] { '|', ',', '，', ';', '；' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(keyword => keyword.Trim())
                .Where(keyword => !string.IsNullOrWhiteSpace(keyword)));
        }

        private static string FormatKeywords(string keywords)
        {
            return keywords.Replace("|", " / ");
        }

        private void ClearPlaces()
        {
            if (Places.Count > 0)
                Places.Clear();

            OnPropertyChanged(nameof(HasPlaces));
            OnPropertyChanged(nameof(HasNoPlaces));
        }
    }
}
