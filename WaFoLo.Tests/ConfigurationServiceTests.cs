using System.IO;
using System.Text.Json;
using WaFoLo.Models;
using WaFoLo.Services;

namespace WaFoLo.Tests
{
    public class ConfigurationServiceTests : IDisposable
    {
        private readonly string _tempDir;

        public ConfigurationServiceTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }

        // ── LoadConfigurationAsync: missing file → default config created ─────

        [Fact]
        public async Task LoadConfigurationAsync_FileMissing_CreatesDefaultAndReturns()
        {
            string configPath = Path.Combine(_tempDir, "config.json");
            var service = new ConfigurationService(configPath);

            WatchdogConfig config = await service.LoadConfigurationAsync();

            Assert.NotNull(config);
            Assert.False(string.IsNullOrEmpty(config.LogFilePath));
            Assert.True(File.Exists(configPath), "Default config file should be written to disk.");
        }

        [Fact]
        public async Task LoadConfigurationAsync_FileMissing_DefaultConfigHasExpectedValues()
        {
            string configPath = Path.Combine(_tempDir, "config.json");
            var service = new ConfigurationService(configPath);

            WatchdogConfig config = await service.LoadConfigurationAsync();

            // The default config has TestMode = true
            Assert.True(config.TestMode);
            Assert.True(config.TimeoutSeconds > 0);
        }

        // ── SaveConfigurationAsync / LoadConfigurationAsync round-trip ─────────

        [Fact]
        public async Task SaveAndLoad_RoundTrip_PreservesValues()
        {
            string configPath = Path.Combine(_tempDir, "config.json");
            var service = new ConfigurationService(configPath);

            var original = new WatchdogConfig
            {
                LogFilePath = @"C:\logs\test.log",
                TriggerLinePattern = "TRIGGER_PATTERN",
                ExpectedLinePattern = "EXPECTED_PATTERN",
                TimeoutSeconds = 60,
                TestMode = false,
                ShowConfigurationOnStartup = true,
                LogTimestampFormat = "yyyy-MM-dd HH:mm:ss",
                LogTimestampPosition = "start",
                MonitoredProcessName = "TestProcess",
                AutoCloseOnSuccess = true,
                AutoCloseDelaySeconds = 5,
                RestartProcessOnTimeout = true,
                MaxRestartAttempts = 3
            };

            await service.SaveConfigurationAsync(original);
            WatchdogConfig loaded = await service.LoadConfigurationAsync();

            Assert.Equal(original.LogFilePath, loaded.LogFilePath);
            Assert.Equal(original.TriggerLinePattern, loaded.TriggerLinePattern);
            Assert.Equal(original.ExpectedLinePattern, loaded.ExpectedLinePattern);
            Assert.Equal(original.TimeoutSeconds, loaded.TimeoutSeconds);
            Assert.Equal(original.TestMode, loaded.TestMode);
            Assert.Equal(original.ShowConfigurationOnStartup, loaded.ShowConfigurationOnStartup);
            Assert.Equal(original.LogTimestampFormat, loaded.LogTimestampFormat);
            Assert.Equal(original.MonitoredProcessName, loaded.MonitoredProcessName);
            Assert.Equal(original.AutoCloseOnSuccess, loaded.AutoCloseOnSuccess);
            Assert.Equal(original.AutoCloseDelaySeconds, loaded.AutoCloseDelaySeconds);
            Assert.Equal(original.RestartProcessOnTimeout, loaded.RestartProcessOnTimeout);
            Assert.Equal(original.MaxRestartAttempts, loaded.MaxRestartAttempts);
        }

        // ── LoadConfigurationAsync: invalid JSON ──────────────────────────────

        [Fact]
        public async Task LoadConfigurationAsync_InvalidJson_ThrowsException()
        {
            string configPath = Path.Combine(_tempDir, "config.json");
            await File.WriteAllTextAsync(configPath, "{ this is not valid json");

            var service = new ConfigurationService(configPath);

            await Assert.ThrowsAnyAsync<Exception>(() => service.LoadConfigurationAsync());
        }

        // ── GetConfigPath ─────────────────────────────────────────────────────

        [Fact]
        public void GetConfigPath_ReturnsPathPassedToConstructor()
        {
            string configPath = Path.Combine(_tempDir, "myconfig.json");
            var service = new ConfigurationService(configPath);

            Assert.Equal(configPath, service.GetConfigPath());
        }

        // ── SaveConfigurationAsync: produces valid JSON ───────────────────────

        [Fact]
        public async Task SaveConfigurationAsync_WritesValidJson()
        {
            string configPath = Path.Combine(_tempDir, "config.json");
            var service = new ConfigurationService(configPath);

            await service.SaveConfigurationAsync(new WatchdogConfig
            {
                LogFilePath = "test.log",
                TimeoutSeconds = 30
            });

            string json = await File.ReadAllTextAsync(configPath);
            var doc = JsonDocument.Parse(json); // should not throw
            Assert.NotNull(doc);
        }
    }
}
