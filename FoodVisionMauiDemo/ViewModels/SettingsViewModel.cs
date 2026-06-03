using System.Diagnostics;
using FoodVisionMauiDemo.Services;
using Microsoft.Maui.Storage;

namespace FoodVisionMauiDemo.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private readonly AppSettingsService _appSettingsService;
        private readonly SpeechService _speechService;
        private bool _useLiveApi;
        private bool _ttsEnabled;
        private bool _darkMode;
        private bool _avoidGluten;
        private bool _avoidDairy;
        private bool _avoidNuts;
        private bool _avoidEgg;
        private bool _saveScanHistory;
        private double _ttsSpeed;
        private string _fontSize = "Normal";
        private string _dietaryPreference = "None";
        private string _statusMessage = "Settings are saved on this device.";

        public SettingsViewModel()
            : this(new AppSettingsService())
        {
        }

        public SettingsViewModel(AppSettingsService appSettingsService)
        {
            _appSettingsService = appSettingsService;
            _speechService = new SpeechService(appSettingsService);
            _useLiveApi = _appSettingsService.UseLiveApi;
            _ttsEnabled = _appSettingsService.TtsEnabled;
            _ttsSpeed = _appSettingsService.TtsSpeed;
            _darkMode = _appSettingsService.DarkMode;
            _fontSize = _appSettingsService.FontSize;
            _dietaryPreference = _appSettingsService.DietaryPreference;
            _avoidGluten = _appSettingsService.AvoidGluten;
            _avoidDairy = _appSettingsService.AvoidDairy;
            _avoidNuts = _appSettingsService.AvoidNuts;
            _avoidEgg = _appSettingsService.AvoidEgg;
            _saveScanHistory = _appSettingsService.SaveScanHistory;
            TestTtsCommand = new Command(async () => await TestTtsAsync());
            ClearLocalDataCommand = new Command(async () => await ClearLocalDataAsync());
        }

        public IReadOnlyList<string> FontSizeOptions { get; } = new[] { "Normal", "Large", "Extra Large" };

        public IReadOnlyList<string> DietaryPreferenceOptions { get; } = new[] { "None", "Gluten-free", "Dairy-free" };

        public Command ClearLocalDataCommand { get; }

        public Command TestTtsCommand { get; }

        public string ModelName => "MobileNetV2 Food-101 ONNX";

        public string ResourceLocation => "Resources/Raw";

        public string PlaceholderNote => "Settings are stored locally on this device with Preferences.";

        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        public bool UseLiveApi
        {
            get => _useLiveApi;
            set
            {
                if (SetProperty(ref _useLiveApi, value))
                    _appSettingsService.UseLiveApi = value;
            }
        }

        public bool TtsEnabled
        {
            get => _ttsEnabled;
            set
            {
                if (SetProperty(ref _ttsEnabled, value))
                    _appSettingsService.TtsEnabled = value;
            }
        }

        public double TtsSpeed
        {
            get => _ttsSpeed;
            set
            {
                if (SetProperty(ref _ttsSpeed, value))
                    _appSettingsService.TtsSpeed = value;
            }
        }

        public bool DarkMode
        {
            get => _darkMode;
            set
            {
                if (SetProperty(ref _darkMode, value))
                    _appSettingsService.DarkMode = value;
            }
        }

        public string FontSize
        {
            get => _fontSize;
            set
            {
                if (SetProperty(ref _fontSize, value))
                    _appSettingsService.FontSize = value;
            }
        }

        public string DietaryPreference
        {
            get => _dietaryPreference;
            set
            {
                if (SetProperty(ref _dietaryPreference, value))
                    _appSettingsService.DietaryPreference = value;
            }
        }

        public bool AvoidGluten
        {
            get => _avoidGluten;
            set
            {
                if (SetProperty(ref _avoidGluten, value))
                    _appSettingsService.AvoidGluten = value;
            }
        }

        public bool AvoidDairy
        {
            get => _avoidDairy;
            set
            {
                if (SetProperty(ref _avoidDairy, value))
                    _appSettingsService.AvoidDairy = value;
            }
        }

        public bool AvoidNuts
        {
            get => _avoidNuts;
            set
            {
                if (SetProperty(ref _avoidNuts, value))
                    _appSettingsService.AvoidNuts = value;
            }
        }

        public bool AvoidEgg
        {
            get => _avoidEgg;
            set
            {
                if (SetProperty(ref _avoidEgg, value))
                    _appSettingsService.AvoidEgg = value;
            }
        }

        public bool SaveScanHistory
        {
            get => _saveScanHistory;
            set
            {
                if (SetProperty(ref _saveScanHistory, value))
                    _appSettingsService.SaveScanHistory = value;
            }
        }

        public string AmapKeyStatus => AmapApiOptions.HasConfiguredApiKey
            ? "AMap Web Service key configured."
            : "AMap Web Service key is not configured.";

        public async Task ClearLocalDataAsync()
        {
            try
            {
                var appData = FileSystem.Current.AppDataDirectory;
                DeleteFileIfExists(Path.Combine(appData, "nutrilenskg.db3"));
                DeleteDirectoryIfExists(Path.Combine(appData, "images"));
                DeleteDirectoryIfExists(Path.Combine(appData, "audio"));

                StatusMessage = "Local meal data, images, and voice notes were cleared. Restart the app if old records are still visible.";
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                StatusMessage = "Could not clear local data. Please try again.";
            }
        }

        private async Task TestTtsAsync()
        {
            try
            {
                StatusMessage = await _speechService.GetDiagnosticSummaryAsync();
                await _speechService.SpeakAsync("NutriLens KG Text to Speech test. If you can hear this, speech output is working.");
                StatusMessage = "TTS test completed. If there was no sound, check the emulator speaker, macOS output device, and Android Text-to-Speech settings.";
            }
            catch (SpeechServiceException ex)
            {
                StatusMessage = ex.Message;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                StatusMessage = "Could not run the TTS test on this device.";
            }
        }

        private static void DeleteFileIfExists(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }

        private static void DeleteDirectoryIfExists(string path)
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
    }
}
