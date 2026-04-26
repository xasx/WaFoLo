namespace WaFoLo.Services
{
    /// <summary>
    /// Creates <see cref="DispatcherTimerAdapter"/> instances — the WPF production
    /// implementation of <see cref="IWatchdogTimerFactory"/>.
    /// </summary>
    internal sealed class DispatcherTimerFactory : IWatchdogTimerFactory
    {
        public IWatchdogTimer CreateTimer() => new DispatcherTimerAdapter();
    }
}
 