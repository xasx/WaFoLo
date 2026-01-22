using System.Windows.Threading;

namespace WaFoLo.Services
{
    public class TimeoutManager : IDisposable
    {
        private DispatcherTimer? _timeoutTimer;
        private DispatcherTimer? _progressTimer;
        private DateTime? _triggerTime;
        private int _timeoutSeconds;

        public event EventHandler? TimeoutOccurred;
        public event EventHandler<TimeoutProgressEventArgs>? ProgressUpdated;
        public event EventHandler? TriggerReset;

        public DateTime? TriggerTime => _triggerTime;
        public bool IsActive => _triggerTime.HasValue;

        public TimeoutManager()
        {
            _timeoutTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timeoutTimer.Tick += OnTimeoutTimerTick;

            _progressTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _progressTimer.Tick += OnProgressTimerTick;
        }

        public void StartTimeout(DateTime triggerTime, int timeoutSeconds)
        {
            _triggerTime = triggerTime;
            _timeoutSeconds = timeoutSeconds;

            TimeSpan elapsed = DateTime.Now - triggerTime;
            double remainingTime = timeoutSeconds - elapsed.TotalSeconds;

            if (remainingTime > 0)
            {
                _timeoutTimer?.Start();
                _progressTimer?.Start();
            }
            else
            {
                TimeoutOccurred?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Reset()
        {
            _triggerTime = null;
            _timeoutTimer?.Stop();
            _progressTimer?.Stop();
            TriggerReset?.Invoke(this, EventArgs.Empty);
        }

        private void OnTimeoutTimerTick(object? sender, EventArgs e)
        {
            if (!_triggerTime.HasValue)
                return;

            TimeSpan elapsed = DateTime.Now - _triggerTime.Value;

            if (elapsed.TotalSeconds >= _timeoutSeconds)
            {
                _timeoutTimer?.Stop();
                _progressTimer?.Stop();
                TimeoutOccurred?.Invoke(this, EventArgs.Empty);
            }
        }

        private void OnProgressTimerTick(object? sender, EventArgs e)
        {
            if (!_triggerTime.HasValue)
                return;

            TimeSpan elapsed = DateTime.Now - _triggerTime.Value;
            double remaining = _timeoutSeconds - elapsed.TotalSeconds;
            double percentage = (elapsed.TotalSeconds / _timeoutSeconds) * 100;

            ProgressUpdated?.Invoke(this, new TimeoutProgressEventArgs(
                Math.Min(percentage, 100),
                Math.Max(0, remaining)
            ));
        }

        public void Dispose()
        {
            _timeoutTimer?.Stop();
            _progressTimer?.Stop();
        }
    }

    public class TimeoutProgressEventArgs : EventArgs
    {
        public double Percentage { get; }
        public double RemainingSeconds { get; }

        public TimeoutProgressEventArgs(double percentage, double remainingSeconds)
        {
            Percentage = percentage;
            RemainingSeconds = remainingSeconds;
        }
    }
}
