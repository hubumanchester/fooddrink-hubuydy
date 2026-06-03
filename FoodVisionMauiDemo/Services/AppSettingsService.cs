using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace FoodVisionMauiDemo.Services
{
    public class AppSettingsService
    {
        private const string UseLiveApiKey = "settings_use_live_api";
        private const string TtsEnabledKey = "settings_tts_enabled";
        private const string TtsSpeedKey = "settings_tts_speed";
        private const string DarkModeKey = "settings_dark_mode";
        private const string FontSizeKey = "settings_font_size";
        private const string DietaryPreferenceKey = "settings_dietary_preference";
        private const string AvoidGlutenKey = "settings_avoid_gluten";
        private const string AvoidDairyKey = "settings_avoid_dairy";
        private const string AvoidNutsKey = "settings_avoid_nuts";
        private const string AvoidEggKey = "settings_avoid_egg";
        private const string SaveScanHistoryKey = "settings_save_scan_history";

        public bool UseLiveApi
        {
            get => Preferences.Default.Get(UseLiveApiKey, true);
            set => Preferences.Default.Set(UseLiveApiKey, value);
        }

        public bool TtsEnabled
        {
            get => Preferences.Default.Get(TtsEnabledKey, true);
            set => Preferences.Default.Set(TtsEnabledKey, value);
        }

        public double TtsSpeed
        {
            get => Preferences.Default.Get(TtsSpeedKey, 1.0d);
            set => Preferences.Default.Set(TtsSpeedKey, Math.Clamp(value, 0.5d, 1.5d));
        }

        public bool DarkMode
        {
            get => Preferences.Default.Get(DarkModeKey, false);
            set
            {
                Preferences.Default.Set(DarkModeKey, value);
                ApplyVisualSettings();
            }
        }

        public string FontSize
        {
            get => Preferences.Default.Get(FontSizeKey, "Normal");
            set
            {
                Preferences.Default.Set(FontSizeKey, value);
                ApplyVisualSettings();
            }
        }

        public string DietaryPreference
        {
            get => Preferences.Default.Get(DietaryPreferenceKey, "None");
            set => Preferences.Default.Set(DietaryPreferenceKey, value);
        }

        public bool AvoidGluten
        {
            get => Preferences.Default.Get(AvoidGlutenKey, false);
            set => Preferences.Default.Set(AvoidGlutenKey, value);
        }

        public bool AvoidDairy
        {
            get => Preferences.Default.Get(AvoidDairyKey, false);
            set => Preferences.Default.Set(AvoidDairyKey, value);
        }

        public bool AvoidNuts
        {
            get => Preferences.Default.Get(AvoidNutsKey, false);
            set => Preferences.Default.Set(AvoidNutsKey, value);
        }

        public bool AvoidEgg
        {
            get => Preferences.Default.Get(AvoidEggKey, false);
            set => Preferences.Default.Set(AvoidEggKey, value);
        }

        public bool SaveScanHistory
        {
            get => Preferences.Default.Get(SaveScanHistoryKey, true);
            set => Preferences.Default.Set(SaveScanHistoryKey, value);
        }

        public void ApplyVisualSettings()
        {
            if (Application.Current == null)
                return;

            Application.Current.UserAppTheme = DarkMode ? AppTheme.Dark : AppTheme.Light;

            var scale = FontSize switch
            {
                "Large" => 1.15d,
                "Extra Large" => 1.3d,
                _ => 1.0d
            };

            Application.Current.Resources["TitleFontSize"] = 28d * scale;
            Application.Current.Resources["SectionFontSize"] = 18d * scale;
            Application.Current.Resources["BodyFontSize"] = 15d * scale;
            Application.Current.Resources["SmallFontSize"] = 13d * scale;
            Application.Current.Resources["ButtonFontSize"] = 14d * scale;
        }
    }
}
