using System.Diagnostics;
using Microsoft.Maui.Devices;

namespace FoodVisionMauiDemo.Services
{
    public class FeedbackService
    {
        public Task SuccessAsync()
        {
            TryHaptic(HapticFeedbackType.Click);
            TryVibrate(TimeSpan.FromMilliseconds(300));
            return Task.CompletedTask;
        }

        public Task WarningAsync()
        {
            TryHaptic(HapticFeedbackType.LongPress);
            TryVibrate(TimeSpan.FromMilliseconds(300));
            return Task.CompletedTask;
        }

        private static void TryHaptic(HapticFeedbackType type)
        {
            try
            {
                if (HapticFeedback.Default.IsSupported)
                    HapticFeedback.Default.Perform(type);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FeedbackService] Haptic feedback unavailable: {ex.Message}");
            }
        }

        private static void TryVibrate(TimeSpan duration)
        {
            try
            {
                Vibration.Default.Vibrate(duration);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FeedbackService] Vibration unavailable: {ex.Message}");
            }
        }
    }
}
