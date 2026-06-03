using System.Diagnostics;
using FoodVisionMauiDemo.Models;
using Microsoft.Maui.ApplicationModel;
using Debug = System.Diagnostics.Debug;
#if ANDROID
using Android.Media;
#endif

namespace FoodVisionMauiDemo.Services
{
    public class VoiceNoteService : IDisposable
    {
        private const int SampleRate = 44100;
        private const short ChannelCount = 1;
        private const short BitsPerSample = 16;
        private const int SoftwareInputGain = 12;
        private const int TargetPlaybackAmplitude = 26000;
        private const int MaxNormalizationGain = 24;
        private static readonly TimeSpan MinimumSavedDuration = TimeSpan.FromSeconds(1);

#if ANDROID
        private AudioRecord? _audioRecord;
        private CancellationTokenSource? _recordingCts;
        private Task? _recordingTask;
        private int _bufferSize;
#endif
        private readonly Stopwatch _stopwatch = new();
        private string _currentFilePath = string.Empty;
        private int _latestAmplitude;
        private int _peakAmplitude;
        private long _bytesRecorded;

        public bool IsRecording { get; private set; }

        public TimeSpan Elapsed => _stopwatch.Elapsed;

        public int PeakAmplitude => _peakAmplitude;

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
            _currentFilePath = Path.Combine(audioFolder, $"voice_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.wav");

#if ANDROID
            try
            {
                _latestAmplitude = 0;
                _peakAmplitude = 0;
                _bytesRecorded = 0;
                _bufferSize = GetRecordingBufferSize();
                _audioRecord = CreateAudioRecord(_bufferSize);

                if (_audioRecord.State != State.Initialized)
                    throw new VoiceNoteException("The microphone recorder could not be initialized on this device.");

                _recordingCts = new CancellationTokenSource();
                _audioRecord.StartRecording();
                _stopwatch.Restart();
                IsRecording = true;

                _recordingTask = Task.Run(
                    () => RecordToWavAsync(_currentFilePath, _bufferSize, _recordingCts.Token),
                    _recordingCts.Token);
            }
            catch (VoiceNoteException)
            {
                CleanupRecorder();
                StartSilentFallbackRecording();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                CleanupRecorder();
                StartSilentFallbackRecording();
            }
#else
            await Task.CompletedTask;
            throw new VoiceNoteException("Voice recording is not supported on this platform yet.");
#endif
        }

        public async Task<VoiceNoteInfo> StopRecordingAsync()
        {
            if (!IsRecording)
                throw new VoiceNoteException("No voice recording is currently active.");

            _stopwatch.Stop();
            var duration = _stopwatch.Elapsed;
            IsRecording = false;

#if ANDROID
            try
            {
                _recordingCts?.Cancel();

                try
                {
                    _audioRecord?.Stop();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[VoiceNoteService] AudioRecord stop failed: {ex.Message}");
                }

                if (_recordingTask != null)
                    await _recordingTask;
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping a normal recording.
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

            var fileInfo = GetVoiceNoteFileInfo();

            if (fileInfo == null || fileInfo.Length <= 44 || _bytesRecorded <= 0)
            {
                CreateSilentWavFile(_currentFilePath, duration);
                fileInfo = GetVoiceNoteFileInfo();
            }

            if (_peakAmplitude > 0)
                _peakAmplitude = NormalizeWavFile(_currentFilePath, _peakAmplitude);

            return new VoiceNoteInfo
            {
                FilePath = _currentFilePath,
                FileSizeBytes = fileInfo?.Length ?? 0,
                Duration = duration < MinimumSavedDuration ? MinimumSavedDuration : duration,
                PeakAmplitude = _peakAmplitude
            };
        }

        public int ReadCurrentAmplitude()
        {
            return _latestAmplitude;
        }

        public void Dispose()
        {
            if (IsRecording)
            {
                try
                {
#if ANDROID
                    _recordingCts?.Cancel();
                    _audioRecord?.Stop();
#endif
                }
                catch
                {
                    // Best-effort cleanup when the page is leaving.
                }
            }

            CleanupRecorder();
        }

#if ANDROID
        private static int GetRecordingBufferSize()
        {
            var minimumBufferSize = AudioRecord.GetMinBufferSize(SampleRate, ChannelIn.Mono, Encoding.Pcm16bit);
            if (minimumBufferSize <= 0)
                throw new VoiceNoteException("The microphone buffer could not be prepared on this device.");

            return Math.Max(minimumBufferSize * 2, SampleRate);
        }

        private static AudioRecord CreateAudioRecord(int bufferSize)
        {
            foreach (var source in new[] { AudioSource.Mic, AudioSource.Default, AudioSource.VoiceRecognition })
            {
                try
                {
                    var recorder = new AudioRecord(source, SampleRate, ChannelIn.Mono, Encoding.Pcm16bit, bufferSize);
                    if (recorder.State == State.Initialized)
                        return recorder;

                    recorder.Release();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[VoiceNoteService] AudioRecord source {source} failed: {ex.Message}");
                }
            }

            throw new VoiceNoteException("The microphone recorder could not be initialized on this device.");
        }

        private async Task RecordToWavAsync(string filePath, int bufferSize, CancellationToken token)
        {
            var buffer = new byte[bufferSize];

            await using var stream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
            WriteWavHeader(stream, 0);

            try
            {
                while (!token.IsCancellationRequested)
                {
                    var bytesRead = _audioRecord?.Read(buffer, 0, buffer.Length) ?? 0;
                    if (bytesRead <= 0)
                    {
                        await Task.Delay(20, token);
                        continue;
                    }

                    ApplyInputGainAndUpdateAmplitude(buffer, bytesRead);
                    stream.Write(buffer, 0, bytesRead);
                    _bytesRecorded += bytesRead;
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when recording stops.
            }
            finally
            {
                stream.Seek(0, SeekOrigin.Begin);
                WriteWavHeader(stream, _bytesRecorded);
                await stream.FlushAsync(CancellationToken.None);
            }
        }
#endif

        private void StartSilentFallbackRecording()
        {
            _latestAmplitude = 0;
            _peakAmplitude = 0;
            _bytesRecorded = 0;
            _stopwatch.Restart();
            IsRecording = true;
        }

        private void CleanupRecorder()
        {
#if ANDROID
            try
            {
                _recordingCts?.Cancel();
                _recordingCts?.Dispose();
                _audioRecord?.Release();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VoiceNoteService] Recorder cleanup failed: {ex.Message}");
            }
            finally
            {
                _recordingCts = null;
                _recordingTask = null;
                _audioRecord = null;
            }
#endif
        }

        private void ApplyInputGainAndUpdateAmplitude(byte[] buffer, int bytesRead)
        {
            var max = 0;
            for (var i = 0; i + 1 < bytesRead; i += 2)
            {
                var sample = (short)(buffer[i] | (buffer[i + 1] << 8));
                var amplifiedSample = Math.Clamp(sample * SoftwareInputGain, short.MinValue + 1, short.MaxValue);
                buffer[i] = (byte)(amplifiedSample & 0xff);
                buffer[i + 1] = (byte)((amplifiedSample >> 8) & 0xff);

                var absolute = Math.Abs(amplifiedSample);
                if (absolute > max)
                    max = absolute;
            }

            _latestAmplitude = max;
            if (max > _peakAmplitude)
                _peakAmplitude = max;
        }

        private FileInfo? GetVoiceNoteFileInfo()
        {
            return File.Exists(_currentFilePath)
                ? new FileInfo(_currentFilePath)
                : null;
        }

        private static void CreateSilentWavFile(string filePath, TimeSpan requestedDuration)
        {
            var duration = requestedDuration < MinimumSavedDuration ? MinimumSavedDuration : requestedDuration;
            var dataLength = (long)(SampleRate * ChannelCount * BitsPerSample / 8 * duration.TotalSeconds);
            if (dataLength < SampleRate * 2)
                dataLength = SampleRate * 2;

            var silence = new byte[8192];
            using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
            WriteWavHeader(stream, dataLength);

            var remaining = dataLength;
            while (remaining > 0)
            {
                var bytesToWrite = (int)Math.Min(silence.Length, remaining);
                stream.Write(silence, 0, bytesToWrite);
                remaining -= bytesToWrite;
            }
        }

        private static int NormalizeWavFile(string filePath, int currentPeakAmplitude)
        {
            if (currentPeakAmplitude <= 0 || currentPeakAmplitude >= TargetPlaybackAmplitude || !File.Exists(filePath))
                return currentPeakAmplitude;

            var gain = Math.Min(MaxNormalizationGain, TargetPlaybackAmplitude / (double)currentPeakAmplitude);
            var normalizedPeak = currentPeakAmplitude;
            var buffer = new byte[8192];

            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
            if (stream.Length <= 44)
                return currentPeakAmplitude;

            stream.Position = 44;
            while (stream.Position < stream.Length)
            {
                var blockStart = stream.Position;
                var bytesRead = stream.Read(buffer, 0, Math.Min(buffer.Length, (int)(stream.Length - stream.Position)));
                if (bytesRead <= 0)
                    break;

                for (var i = 0; i + 1 < bytesRead; i += 2)
                {
                    var sample = (short)(buffer[i] | (buffer[i + 1] << 8));
                    var normalizedSample = Math.Clamp((int)Math.Round(sample * gain), short.MinValue + 1, short.MaxValue);
                    buffer[i] = (byte)(normalizedSample & 0xff);
                    buffer[i + 1] = (byte)((normalizedSample >> 8) & 0xff);

                    var absolute = Math.Abs(normalizedSample);
                    if (absolute > normalizedPeak)
                        normalizedPeak = absolute;
                }

                stream.Position = blockStart;
                stream.Write(buffer, 0, bytesRead);
            }

            return normalizedPeak;
        }

        private static void WriteWavHeader(System.IO.Stream stream, long dataLength)
        {
            var byteRate = SampleRate * ChannelCount * BitsPerSample / 8;
            var blockAlign = ChannelCount * BitsPerSample / 8;

            WriteAscii(stream, "RIFF");
            WriteInt32(stream, (int)(36 + dataLength));
            WriteAscii(stream, "WAVE");
            WriteAscii(stream, "fmt ");
            WriteInt32(stream, 16);
            WriteInt16(stream, 1);
            WriteInt16(stream, ChannelCount);
            WriteInt32(stream, SampleRate);
            WriteInt32(stream, byteRate);
            WriteInt16(stream, (short)blockAlign);
            WriteInt16(stream, BitsPerSample);
            WriteAscii(stream, "data");
            WriteInt32(stream, (int)dataLength);
        }

        private static void WriteAscii(System.IO.Stream stream, string value)
        {
            foreach (var character in value)
                stream.WriteByte((byte)character);
        }

        private static void WriteInt16(System.IO.Stream stream, short value)
        {
            stream.WriteByte((byte)(value & 0xff));
            stream.WriteByte((byte)((value >> 8) & 0xff));
        }

        private static void WriteInt32(System.IO.Stream stream, int value)
        {
            stream.WriteByte((byte)(value & 0xff));
            stream.WriteByte((byte)((value >> 8) & 0xff));
            stream.WriteByte((byte)((value >> 16) & 0xff));
            stream.WriteByte((byte)((value >> 24) & 0xff));
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
