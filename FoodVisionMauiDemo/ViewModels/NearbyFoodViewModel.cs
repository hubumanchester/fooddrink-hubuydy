using System.Collections.ObjectModel;
using System.Diagnostics;
using FoodVisionMauiDemo.Models;
using FoodVisionMauiDemo.Services;

namespace FoodVisionMauiDemo.ViewModels
{
    public class NearbyFoodViewModel : BaseViewModel
    {
        private readonly AppSettingsService _appSettingsService;
        private readonly LocationService _locationService;
        private readonly RiskToPlaceQueryService _riskToPlaceQueryService;
        private readonly AmapPlaceSearchService _amapPlaceSearchService;
        private string _riskTag = "balanced";
        private string _riskDisplayName = "Balanced";
        private string _searchKeywords = "餐厅|咖啡|超市";
        private string _statusMessage = "Ready to search nearby balanced options.";
        private bool _isBusy;
        private bool _hasError;

        public NearbyFoodViewModel()
            : this(
                new AppSettingsService(),
                new LocationService(),
                new RiskToPlaceQueryService(),
                new AmapPlaceSearchService())
        {
        }

        public NearbyFoodViewModel(
            AppSettingsService appSettingsService,
            LocationService locationService,
            RiskToPlaceQueryService riskToPlaceQueryService,
            AmapPlaceSearchService amapPlaceSearchService)
        {
            _appSettingsService = appSettingsService;
            _locationService = locationService;
            _riskToPlaceQueryService = riskToPlaceQueryService;
            _amapPlaceSearchService = amapPlaceSearchService;

            RefreshPlacesCommand = new Command(async () => await LoadPlacesAsync(), () => !IsBusy);
        }

        public ObservableCollection<NearbyPlace> Places { get; } = new();

        public Command RefreshPlacesCommand { get; }

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

        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                    RefreshPlacesCommand.ChangeCanExecute();
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

                StatusMessage = "Searching live nearby places...";
                var reason = BuildRecommendationReason();
                var places = await _amapPlaceSearchService.SearchAroundAsync(
                    location,
                    SearchKeywords,
                    reason);

                foreach (var place in places)
                    Places.Add(place);

                OnPropertyChanged(nameof(HasPlaces));
                OnPropertyChanged(nameof(HasNoPlaces));
                StatusMessage = $"Found {Places.Count} nearby place(s).";
            }
            catch (NearbySearchException ex)
            {
                Debug.WriteLine(ex);
                HasError = true;
                StatusMessage = ex.UserMessage;
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

        private string BuildRecommendationReason()
        {
            return _riskTag == "balanced"
                ? "General balanced nearby option."
                : $"Recommended to balance recent {RiskDisplayName.ToLowerInvariant()} pattern.";
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
