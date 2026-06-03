using System.Diagnostics;
using Microsoft.Maui.Media;

namespace FoodVisionMauiDemo.Services
{
    public class SpeechService
    {
        private readonly AppSettingsService _settingsService;

        public SpeechService()
            : this(new AppSettingsService())
        {
        }

        public SpeechService(AppSettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        public async Task SpeakAsync(string text, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_settingsService.TtsEnabled)
                throw new SpeechServiceException("Text-to-Speech is disabled in Settings.");

            if (string.IsNullOrWhiteSpace(text))
                throw new SpeechServiceException("There is no text to read aloud.");

            try
            {
                await TextToSpeech.Default.SpeakAsync(
                    text,
                    await CreateSpeechOptionsAsync(cancellationToken),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                throw new SpeechServiceException("Could not read this text aloud on this device.", ex);
            }
        }

        public async Task<string> GetDiagnosticSummaryAsync()
        {
            try
            {
                var locales = (await TextToSpeech.Default.GetLocalesAsync()).ToList();
                if (locales.Count == 0)
                    return "No Text-to-Speech voices were reported by this device.";

                var preferred = SelectPreferredLocale(locales);
                if (preferred == null)
                    return $"Text-to-Speech voices available: {locales.Count}. Using the system default voice.";

                return $"Text-to-Speech voices available: {locales.Count}. Using: {preferred.Name} ({preferred.Language}-{preferred.Country}).";
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return "Could not read Text-to-Speech voice information from this device.";
            }
        }

        private async Task<SpeechOptions> CreateSpeechOptionsAsync(CancellationToken cancellationToken)
        {
            var locales = (await TextToSpeech.Default.GetLocalesAsync()).ToList();
            cancellationToken.ThrowIfCancellationRequested();

            var options = new SpeechOptions
            {
                Locale = SelectPreferredLocale(locales),
                Pitch = 1.0f,
                Volume = 1.0f
            };

            // Some MAUI/device combinations expose speech rate. Keep this reflective
            // so the app still builds where only pitch and volume are available.
            var rateProperty = options.GetType().GetProperty("Rate");
            if (rateProperty?.CanWrite == true)
            {
                var value = Convert.ChangeType(_settingsService.TtsSpeed, rateProperty.PropertyType);
                rateProperty.SetValue(options, value);
            }

            return options;
        }

        private static Locale? SelectPreferredLocale(IReadOnlyList<Locale> locales)
        {
            if (locales.Count == 0)
                return null;

            return locales.FirstOrDefault(locale =>
                       locale.Language.Equals("en", StringComparison.OrdinalIgnoreCase) &&
                       locale.Country.Equals("US", StringComparison.OrdinalIgnoreCase))
                   ?? locales.FirstOrDefault(locale =>
                       locale.Language.Equals("en", StringComparison.OrdinalIgnoreCase))
                   ?? locales.FirstOrDefault();
        }
    }

    public class SpeechServiceException : Exception
    {
        public SpeechServiceException(string message)
            : base(message)
        {
        }

        public SpeechServiceException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
