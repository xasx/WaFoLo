using WaFoLo.Models;

namespace WaFoLo.Services
{
    /// <summary>
    /// Interface for loading and saving watchdog configuration.
    /// </summary>
    public interface IConfigurationService
    {
        Task<WatchdogConfig> LoadConfigurationAsync();
        Task SaveConfigurationAsync(WatchdogConfig config);
        string GetConfigPath();
    }
}
