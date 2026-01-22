namespace WaFoLo.Services
{
    /// <summary>
    /// Factory for creating ILogMonitorService instances.
    /// </summary>
    public interface ILogMonitorServiceFactory
    {
        ILogMonitorService Create();
    }

    /// <summary>
    /// Default implementation of ILogMonitorServiceFactory.
    /// </summary>
    public class LogMonitorServiceFactory : ILogMonitorServiceFactory
    {
        public ILogMonitorService Create()
        {
            return new LogMonitorService();
        }
    }
}
