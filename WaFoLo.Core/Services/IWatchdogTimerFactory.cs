namespace WaFoLo.Services
{
    /// <summary>
    /// Factory that creates <see cref="IWatchdogTimer"/> instances.
    /// Inject a test double of this interface to control timers in unit tests.
    /// </summary>
    public interface IWatchdogTimerFactory
    {
        IWatchdogTimer CreateTimer();
    }
}
