using System.Windows.Threading;

namespace WaFoLo.Services
{
    public class AutoCloseManager : IDisposable
    {
        private DispatcherTimer? _autoCloseTimer;
        private int _autoCloseCountdown;

        public event EventHandler<string>? LogActivity;
        public event EventHandler<int>? CountdownUpdated;
        public event EventHandler? ApplicationClosing;

        public bool IsActive => _autoCloseTimer != null && _autoCloseTimer.IsEnabled;

        public void StartCountdown(int delaySeconds)
        {
            _autoCloseCountdown = delaySeconds;
            LogActivity?.Invoke(this, $"Application will automatically close in {_autoCloseCountdown} seconds...");

            _autoCloseTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _autoCloseTimer.Tick += OnAutoCloseTimerTick;
            _autoCloseTimer.Start();

            CountdownUpdated?.Invoke(this, _autoCloseCountdown);
        }

        public void Stop()
        {
            _autoCloseTimer?.Stop();
        }

        private void OnAutoCloseTimerTick(object? sender, EventArgs e)
        {
            _autoCloseCountdown--;

            if (_autoCloseCountdown <= 0)
            {
                _autoCloseTimer?.Stop();
                LogActivity?.Invoke(this, "Auto-closing application...");
                ApplicationClosing?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                CountdownUpdated?.Invoke(this, _autoCloseCountdown);
            }
        }

        public void Dispose()
        {
            _autoCloseTimer?.Stop();
        }
    }
}
