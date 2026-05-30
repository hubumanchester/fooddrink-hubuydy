namespace FoodVisionMauiDemo.ViewModels
{
    using FoodVisionMauiDemo.Services;

    public class SettingsViewModel : BaseViewModel
    {
        private readonly AppSettingsService _appSettingsService;
        private bool _useLiveApi;

        public SettingsViewModel()
            : this(new AppSettingsService())
        {
        }

        public SettingsViewModel(AppSettingsService appSettingsService)
        {
            _appSettingsService = appSettingsService;
            _useLiveApi = _appSettingsService.UseLiveApi;
        }

        public string ModelName => "MobileNetV2 Food-101 ONNX";

        public string ResourceLocation => "Resources/Raw";

        public string PlaceholderNote => "Theme, privacy controls, and advanced accessibility settings will be added in later phases.";

        public bool UseLiveApi
        {
            get => _useLiveApi;
            set
            {
                if (SetProperty(ref _useLiveApi, value))
                    _appSettingsService.UseLiveApi = value;
            }
        }

        public string AmapKeyStatus => AmapApiOptions.HasConfiguredApiKey
            ? "AMap Web Service key configured."
            : "AMap Web Service key is not configured.";
    }
}
