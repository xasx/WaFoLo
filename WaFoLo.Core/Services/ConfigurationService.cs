using System.IO;
using System.Text.Json;
using WaFoLo.Models;

namespace WaFoLo.Services
{
    /// <summary>
    /// Service for loading and saving watchdog configuration.
    /// </summary>
    public class ConfigurationService : IConfigurationService
    {
        private readonly string _configPath;

        public ConfigurationService(string? configPath = null)
        {
            _configPath = configPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
        }

        /// <summary>
        /// Loads configuration from file. Creates a default configuration if file doesn't exist.
        /// </summary>
        public async Task<WatchdogConfig> LoadConfigurationAsync()
        {
            if (!File.Exists(_configPath))
            {
                var defaultConfig = CreateDefaultConfiguration();
                await SaveConfigurationAsync(defaultConfig);
                return defaultConfig;
            }

            string json = await File.ReadAllTextAsync(_configPath);
            var config = JsonSerializer.Deserialize<WatchdogConfig>(json);

            if (config == null)
            {
                throw new InvalidOperationException("Failed to deserialize configuration.");
            }

            return config;
        }

        /// <summary>
        /// Saves configuration to file.
        /// </summary>
        public async Task SaveConfigurationAsync(WatchdogConfig config)
        {
            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_configPath, json);
        }

        /// <summary>
        /// Creates a default configuration.
        /// </summary>
        private WatchdogConfig CreateDefaultConfiguration()
        {
            return new WatchdogConfig
            {
                LogFilePath = "C:\\logs\\application.log",
                TriggerLinePattern = "Starting critical operation",
                ExpectedLinePattern = "Operation completed successfully",
                TimeoutSeconds = 30,
                TestMode = true,
                ShowConfigurationOnStartup = false,
                LogTimestampFormat = "yyyy-MM-dd HH:mm:ss",
                LogTimestampPosition = "start",
                MonitoredProcessName = "YourApplication",
                ProcessRestartCommand = "C:\\Path\\To\\YourApplication.exe"
            };
        }

        /// <summary>
        /// Gets the configuration file path.
        /// </summary>
        public string GetConfigPath() => _configPath;
    }
}
