using System.Diagnostics;
using FoodVisionMauiDemo.Models;
using Microsoft.Maui.ApplicationModel;
#if ANDROID
using Android.Media;
#endif

namespace FoodVisionMauiDemo.Services
{
    public class VoiceNoteService : IDisposable
    {
#if ANDROID
        private MediaRecorder? _recorder;
#endif
        private readonly Stopwatch _stopwatch = new();
        private string _currentFilePath = string.Empty;

        public bool IsRecording { get; private set; }

        public TimeSpan Elapsed => _stopwatch.Elapsed;

        public async Task StartRecordingAsync()
        {
            if (IsRecording)
                return;

            var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();
            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<Permissions.Microphone>();

            if (status != PermissionStatus.Granted)
                throw new VoiceNoteException("Microphone permission is required to record a voice note.");

            var audioFolder = Path.Combine(FileSystem.Current.AppDataDirectory, "audio");
            Directory.CreateDirectory(audioFolder);
            _currentFilePath = Path.Combine(audioFolder, $"voice_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.m4a");

#if ANDROID
            try
            {
                _recorder = new MediaRecorder();
                _recorder.SetAudioSource(AudioSource.Mic);
                _recorder.SetOutputFormat(OutputFormat.Mpeg4);
                _recorder.SetAudioEncoder(AudioEncoder.Aac);
                _recorder.SetAudioSamplingRate(44100);
                _recorder.SetAudioEncodingBitRate(128000);
                _recorder.SetOutputFile(_currentFilePath);
                _recorder.Prepare();
                _recorder.Start();

                _stopwatch.Restart();
                IsRecording = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                CleanupRecorder();
                throw new VoiceNoteException("Could not start voice recording on this device.", ex);
            }
#else
            await Task.CompletedTask;
            throw new VoiceNoteException("Voice recording is not supported on this platform yet.");
#endif
        }

        public Task<VoiceNoteInfo> StopRecordingAsync()
        {
            if (!IsRecording)
                throw new VoiceNoteException("No voice recording is currently active.");

            _stopwatch.Stop();
            IsRecording = false;

#if ANDROID
            try
            {
                _recorder?.Stop();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                TryDeletePartialFile();
                throw new VoiceNoteException("Could not save the voice note. Please try recording again.", ex);
            }
            finally
            {
                CleanupRecorder();
            }
#endif

            var fileInfo = File.Exists(_currentFilePath)
                ? new FileInfo(_currentFilePath)
                : null;

            return Task.FromResult(new VoiceNoteInfo
            {
                FilePath = _currentFilePath,
                FileSizeBytes = fileInfo?.Length ?? 0,
                Duration = _stopwatch.Elapsed
            });
        }

        public void Dispose()
        {
            if (IsRecording)
            {
                try
                {
#if ANDROID
                    _recorder?.Stop();
#endif
                }
                catch
                {
                    // Best-effort cleanup when the page is leaving.
                }
            }

            CleanupRecorder();
        }

        private void CleanupRecorder()
        {
#if ANDROID
            try
            {
                _recorder?.Reset();
                _recorder?.Release();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VoiceNoteService] Recorder cleanup failed: {ex.Message}");
            }
            finally
            {
                _recorder = null;
            }
#endif
        }

        private void TryDeletePartialFile()
        {
            try
            {
                if (File.Exists(_currentFilePath))
                    File.Delete(_currentFilePath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VoiceNoteService] Could not delete partial voice note: {ex.Message}");
            }
        }
    }

    public class VoiceNoteException : Exception
    {
        public VoiceNoteException(string message)
            : base(message)
        {
        }

        public VoiceNoteException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
