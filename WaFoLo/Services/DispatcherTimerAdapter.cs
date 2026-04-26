using System.Windows.Threading;

namespace WaFoLo.Services
{
    /// <summary>
    /// Wraps <see cref="DispatcherTimer"/> to implement <see cref="IWatchdogTimer"/>.
    /// </summary>
    internal sealed class DispatcherTimerAdapter : IWatchdogTimer
    {
        private readonly DispatcherTimer _inner = new();

        public DispatcherTimerAdapter()
        {
            _inner.Tick += (s, e) => Tick?.Invoke(this, e);
        }

        public TimeSpan Interval
        {
            get => _inner.Interval;
            set => _inner.Interval = value;
        }

        public bool IsEnabled => _inner.IsEnabled;

        public event EventHandler? Tick;

        public void Start() => _inner.Start();
        public void Stop() => _inner.Stop();
    }
}
