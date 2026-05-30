using System.Diagnostics;

namespace FoodVisionMauiDemo.Services
{
    public class SpeechService
    {
        public Task SpeakAsync(string text, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Debug.WriteLine($"[SpeechService] Placeholder speech request: {text}");
            return Task.CompletedTask;
        }
    }
}
