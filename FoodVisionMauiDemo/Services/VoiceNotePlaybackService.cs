using System.Diagnostics;
#if ANDROID
using Android.Media;
#endif

namespace FoodVisionMauiDemo.Services
{
    public class VoiceNotePlaybackService : IDisposable
    {
#if ANDROID
        private MediaPlayer? _player;
#endif

        public event EventHandler<string>? PlaybackCompleted;

        public bool IsPlaying { get; private set; }

        public string CurrentFilePath { get; private set; } = string.Empty;

        public Task PlayAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                throw new VoiceNotePlaybackException("Voice note audio file is missing.");

            Stop();

#if ANDROID
            try
            {
                _player = new MediaPlayer();
                _player.SetDataSource(filePath);
                _player.Completion += OnPlaybackCompleted;
                _player.Error += OnPlaybackError;
                _player.Prepare();
                _player.Start();

                CurrentFilePath = filePath;
                IsPlaying = true;
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                CleanupPlayer();
                throw new VoiceNotePlaybackException("Could not play this voice note.", ex);
            }
#else
            throw new VoiceNotePlaybackException("Voice note playback is not supported on this platform yet.");
#endif
        }

        public void Stop()
        {
#if ANDROID
            try
            {
                if (_player != null && IsPlaying)
                    _player.Stop();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VoiceNotePlaybackService] Could not stop playback: {ex.Message}");
            }
            finally
            {
                CleanupPlayer();
            }
#else
            IsPlaying = false;
            CurrentFilePath = string.Empty;
#endif
        }

        public void Dispose()
        {
            Stop();
        }

#if ANDROID
        private void OnPlaybackCompleted(object? sender, EventArgs e)
        {
            var completedFilePath = CurrentFilePath;
            CleanupPlayer();
            PlaybackCompleted?.Invoke(this, completedFilePath);
        }

        private void OnPlaybackError(object? sender, MediaPlayer.ErrorEventArgs e)
        {
            Debug.WriteLine($"[VoiceNotePlaybackService] Playback error: what={e.What}, extra={e.Extra}");
            var completedFilePath = CurrentFilePath;
            CleanupPlayer();
            PlaybackCompleted?.Invoke(this, completedFilePath);
            e.Handled = true;
        }

        private void CleanupPlayer()
        {
            try
            {
                if (_player != null)
                {
                    _player.Completion -= OnPlaybackCompleted;
                    _player.Error -= OnPlaybackError;
                    _player.Reset();
                    _player.Release();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VoiceNotePlaybackService] Player cleanup failed: {ex.Message}");
            }
            finally
            {
                _player = null;
                IsPlaying = false;
                CurrentFilePath = string.Empty;
            }
        }
#endif
    }

    public class VoiceNotePlaybackException : Exception
    {
        public VoiceNotePlaybackException(string message)
            : base(message)
        {
        }

        public VoiceNotePlaybackException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
