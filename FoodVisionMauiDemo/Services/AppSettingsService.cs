using Microsoft.Maui.Storage;

namespace FoodVisionMauiDemo.Services
{
    public class AppSettingsService
    {
        private const string UseLiveApiKey = "settings_use_live_api";

        public bool UseLiveApi
        {
            get => Preferences.Default.Get(UseLiveApiKey, true);
            set => Preferences.Default.Set(UseLiveApiKey, value);
        }
    }
}
