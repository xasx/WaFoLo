using WaFoLo.Services;

namespace WaFoLo.Tests.TestDoubles
{
    /// <summary>
    /// Controllable timer test double. Call <see cref="Fire"/> to simulate a tick.
    /// </summary>
    internal sealed class FakeTimer : IWatchdogTimer
    {
        public TimeSpan Interval { get; set; }
        public bool IsEnabled { get; private set; }

        public event EventHandler? Tick;

        public void Start() => IsEnabled = true;
        public void Stop() => IsEnabled = false;

        /// <summary>Manually fires a single tick.</summary>
        public void Fire() => Tick?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Timer factory that returns <see cref="FakeTimer"/> instances and keeps
    /// track of every timer it has created.
    /// </summary>
    internal sealed class FakeTimerFactory : IWatchdogTimerFactory
    {
        private readonly List<FakeTimer> _timers = new();

        public IReadOnlyList<FakeTimer> CreatedTimers => _timers;

        public FakeTimer? Last => _timers.Count > 0 ? _timers[^1] : null;

        public IWatchdogTimer CreateTimer()
        {
            var t = new FakeTimer();
            _timers.Add(t);
            return t;
        }
    }
}
