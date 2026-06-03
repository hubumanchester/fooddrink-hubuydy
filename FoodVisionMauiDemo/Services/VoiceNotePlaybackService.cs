using System.Diagnostics;
using Debug = System.Diagnostics.Debug;
#if ANDROID
using Android.Content;
using Android.Media;
using Android.OS;
using AndroidBuild = Android.OS.Build;
using AndroidBuildVersionCodes = Android.OS.BuildVersionCodes;
using AndroidStream = Android.Media.Stream;
using JavaFile = Java.IO.File;
#endif

namespace FoodVisionMauiDemo.Services
{
    public class VoiceNotePlaybackService : IDisposable
    {
        private const long MinimumPlayableFileSizeBytes = 2048;

#if ANDROID
        private MediaPlayer? _player;
        private ParcelFileDescriptor? _audioFileDescriptor;
        private AudioManager? _audioManager;
#endif

        public event EventHandler<string>? PlaybackCompleted;

        public bool IsPlaying { get; private set; }

        public string CurrentFilePath { get; private set; } = string.Empty;

        public Task PlayAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                throw new VoiceNotePlaybackException("Voice note audio file is missing.");

            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length < MinimumPlayableFileSizeBytes)
                throw new VoiceNotePlaybackException("This voice note looks empty. Please record it again and speak for at least one second.");

            Stop();

#if ANDROID
            try
            {
                _player = new MediaPlayer();
                ConfigureAudioOutput(_player);
                var descriptor = ParcelFileDescriptor.Open(new JavaFile(filePath), ParcelFileMode.ReadOnly);
                if (descriptor?.FileDescriptor == null)
                    throw new VoiceNotePlaybackException("Voice note audio file could not be opened.");

                _audioFileDescriptor = descriptor;
                _player.SetDataSource(descriptor.FileDescriptor);
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

        private void ConfigureAudioOutput(MediaPlayer player)
        {
            if (AndroidBuild.VERSION.SdkInt >= AndroidBuildVersionCodes.Lollipop)
            {
                using var builder = new AudioAttributes.Builder();
                builder.SetUsage(AudioUsageKind.Media);
                builder.SetContentType(AudioContentType.Speech);
                using var attributes = builder.Build();

                if (attributes != null)
                    player.SetAudioAttributes(attributes);
            }
            else
            {
#pragma warning disable CS0618
                player.SetAudioStreamType(AndroidStream.Music);
#pragma warning restore CS0618
            }

            player.SetVolume(1f, 1f);
            TryRequestAudioFocus();
        }

        private void TryRequestAudioFocus()
        {
            try
            {
                if (Platform.CurrentActivity?.GetSystemService(Context.AudioService) is not AudioManager audioManager)
                    return;

                _audioManager = audioManager;
#pragma warning disable CS0618
                _audioManager.RequestAudioFocus(null, AndroidStream.Music, AudioFocus.GainTransient);
#pragma warning restore CS0618
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VoiceNotePlaybackService] Audio focus request failed: {ex.Message}");
            }
        }

        private void TryAbandonAudioFocus()
        {
            try
            {
                if (_audioManager == null)
                    return;

#pragma warning disable CS0618
                _audioManager.AbandonAudioFocus(null);
#pragma warning restore CS0618
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VoiceNotePlaybackService] Audio focus cleanup failed: {ex.Message}");
            }
            finally
            {
                _audioManager = null;
            }
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
                TryAbandonAudioFocus();
                TryCloseAudioFileDescriptor();
                _player = null;
                IsPlaying = false;
                CurrentFilePath = string.Empty;
            }
        }

        private void TryCloseAudioFileDescriptor()
        {
            try
            {
                _audioFileDescriptor?.Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VoiceNotePlaybackService] Audio file descriptor cleanup failed: {ex.Message}");
            }
            finally
            {
                _audioFileDescriptor = null;
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
