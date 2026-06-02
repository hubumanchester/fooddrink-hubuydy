using System.Diagnostics;
using Microsoft.Maui.Devices.Sensors;

namespace FoodVisionMauiDemo.Services
{
    public class ShakeService
    {
        private const double MagnitudeThreshold = 1.45;
        private const double VectorDeltaThreshold = 0.55;
        private const int RequiredImpulses = 2;
        private static readonly TimeSpan ShakeCooldown = TimeSpan.FromSeconds(1.5);
        private static readonly TimeSpan ImpulseWindow = TimeSpan.FromMilliseconds(850);
        private bool _isListening;
        private DateTimeOffset _lastShakeAt = DateTimeOffset.MinValue;
        private DateTimeOffset _impulseWindowStartedAt = DateTimeOffset.MinValue;
        private int _impulseCount;
        private bool _hasPreviousReading;
        private double _lastX;
        private double _lastY;
        private double _lastZ;

        public event EventHandler? Shaken;

        public bool IsListening => _isListening;

        public bool Start()
        {
            if (_isListening)
                return true;

            try
            {
                if (!Accelerometer.Default.IsSupported)
                {
                    Debug.WriteLine("[ShakeService] Accelerometer is not supported on this device.");
                    return false;
                }

                Accelerometer.Default.ShakeDetected += OnShakeDetected;
                Accelerometer.Default.ReadingChanged += OnReadingChanged;
                Accelerometer.Default.Start(SensorSpeed.Game);
                _isListening = true;
                ResetManualDetector();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ShakeService] Shake detection unavailable: {ex.Message}");
                Accelerometer.Default.ShakeDetected -= OnShakeDetected;
                Accelerometer.Default.ReadingChanged -= OnReadingChanged;
                _isListening = false;
                return false;
            }
        }

        public void Stop()
        {
            if (!_isListening)
                return;

            try
            {
                Accelerometer.Default.ShakeDetected -= OnShakeDetected;
                Accelerometer.Default.ReadingChanged -= OnReadingChanged;
                Accelerometer.Default.Stop();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ShakeService] Could not stop shake detection: {ex.Message}");
            }
            finally
            {
                _isListening = false;
            }
        }

        private void OnShakeDetected(object? sender, EventArgs e)
        {
            RaiseShakeDetected();
        }

        private void OnReadingChanged(object? sender, AccelerometerChangedEventArgs e)
        {
            var reading = e.Reading.Acceleration;
            var magnitude = Math.Sqrt(
                reading.X * reading.X +
                reading.Y * reading.Y +
                reading.Z * reading.Z);

            if (!_hasPreviousReading)
            {
                _lastX = reading.X;
                _lastY = reading.Y;
                _lastZ = reading.Z;
                _hasPreviousReading = true;
                return;
            }

            var vectorDelta = Math.Sqrt(
                Math.Pow(reading.X - _lastX, 2) +
                Math.Pow(reading.Y - _lastY, 2) +
                Math.Pow(reading.Z - _lastZ, 2));

            _lastX = reading.X;
            _lastY = reading.Y;
            _lastZ = reading.Z;

            if (magnitude >= MagnitudeThreshold || vectorDelta >= VectorDeltaThreshold)
                RegisterShakeImpulse();
        }

        private void RegisterShakeImpulse()
        {
            var now = DateTimeOffset.UtcNow;
            if (now - _impulseWindowStartedAt > ImpulseWindow)
            {
                _impulseWindowStartedAt = now;
                _impulseCount = 0;
            }

            _impulseCount++;
            if (_impulseCount < RequiredImpulses)
                return;

            _impulseCount = 0;
            _impulseWindowStartedAt = now;
            RaiseShakeDetected();
        }

        private void RaiseShakeDetected()
        {
            var now = DateTimeOffset.UtcNow;
            if (now - _lastShakeAt < ShakeCooldown)
                return;

            _lastShakeAt = now;
            Shaken?.Invoke(this, EventArgs.Empty);
        }

        private void ResetManualDetector()
        {
            _hasPreviousReading = false;
            _impulseCount = 0;
            _impulseWindowStartedAt = DateTimeOffset.MinValue;
            _lastX = 0;
            _lastY = 0;
            _lastZ = 0;
        }
    }
}
