namespace WaFoLo.Services
{
    /// <summary>
    /// Abstraction over a repeating timer, allowing test doubles to replace
    /// <see cref="System.Windows.Threading.DispatcherTimer"/> in unit tests.
    /// </summary>
    public interface IWatchdogTimer
    {
        TimeSpan Interval { get; set; }
        bool IsEnabled { get; }
        event EventHandler Tick;
        void Start();
        void Stop();
    }
}
