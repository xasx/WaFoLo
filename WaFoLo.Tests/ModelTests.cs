using WaFoLo.Models;

namespace WaFoLo.Tests
{
    public class ModelTests
    {
        // ── LogLineInfo ───────────────────────────────────────────────────────

        [Fact]
        public void LogLineInfo_DefaultValues_AreCorrect()
        {
            var info = new LogLineInfo();

            Assert.Equal(string.Empty, info.Content);
            Assert.Null(info.Timestamp);
            Assert.Equal(0, info.LineNumber);
        }

        [Fact]
        public void LogLineInfo_CanSetProperties()
        {
            var now = DateTime.Now;
            var info = new LogLineInfo
            {
                Content = "Test log line",
                Timestamp = now,
                LineNumber = 42
            };

            Assert.Equal("Test log line", info.Content);
            Assert.Equal(now, info.Timestamp);
            Assert.Equal(42, info.LineNumber);
        }

        // ── WatchdogConfig ────────────────────────────────────────────────────

        [Fact]
        public void WatchdogConfig_DefaultValues_AreCorrect()
        {
            var config = new WatchdogConfig();

            Assert.Equal(string.Empty, config.LogFilePath);
            Assert.Equal(string.Empty, config.TriggerLinePattern);
            Assert.Equal(string.Empty, config.ExpectedLinePattern);
            Assert.Equal(0, config.TimeoutSeconds);
            Assert.False(config.TestMode);
            Assert.False(config.ShowConfigurationOnStartup);
            Assert.Equal(string.Empty, config.LogTimestampFormat);
            Assert.Equal(string.Empty, config.LogTimestampPosition);
            Assert.Equal(string.Empty, config.MonitoredProcessName);
            Assert.False(config.WaitForMonitoredProcess);
            Assert.Equal(5, config.ProcessCheckIntervalSeconds);
            Assert.False(config.AutoCloseOnSuccess);
            Assert.Equal(10, config.AutoCloseDelaySeconds);
            Assert.False(config.RestartProcessOnTimeout);
            Assert.Equal(15, config.RestartProcessDelaySeconds);
            Assert.Equal(1, config.MaxRestartAttempts);
            Assert.Null(config.ProcessRestartCommand);
        }

        [Fact]
        public void WatchdogConfig_CanSetAllProperties()
        {
            var config = new WatchdogConfig
            {
                LogFilePath = @"C:\logs\app.log",
                TriggerLinePattern = "TRIGGER",
                ExpectedLinePattern = "EXPECTED",
                TimeoutSeconds = 120,
                TestMode = true,
                ShowConfigurationOnStartup = true,
                LogTimestampFormat = "yyyy-MM-dd HH:mm:ss",
                LogTimestampPosition = "start",
                MonitoredProcessName = "MyApp",
                WaitForMonitoredProcess = true,
                ProcessCheckIntervalSeconds = 10,
                AutoCloseOnSuccess = true,
                AutoCloseDelaySeconds = 5,
                RestartProcessOnTimeout = true,
                RestartProcessDelaySeconds = 30,
                MaxRestartAttempts = 3,
                ProcessRestartCommand = @"C:\restart.bat"
            };

            Assert.Equal(@"C:\logs\app.log", config.LogFilePath);
            Assert.Equal("TRIGGER", config.TriggerLinePattern);
            Assert.Equal("EXPECTED", config.ExpectedLinePattern);
            Assert.Equal(120, config.TimeoutSeconds);
            Assert.True(config.TestMode);
            Assert.True(config.ShowConfigurationOnStartup);
            Assert.Equal("yyyy-MM-dd HH:mm:ss", config.LogTimestampFormat);
            Assert.Equal("start", config.LogTimestampPosition);
            Assert.Equal("MyApp", config.MonitoredProcessName);
            Assert.True(config.WaitForMonitoredProcess);
            Assert.Equal(10, config.ProcessCheckIntervalSeconds);
            Assert.True(config.AutoCloseOnSuccess);
            Assert.Equal(5, config.AutoCloseDelaySeconds);
            Assert.True(config.RestartProcessOnTimeout);
            Assert.Equal(30, config.RestartProcessDelaySeconds);
            Assert.Equal(3, config.MaxRestartAttempts);
            Assert.Equal(@"C:\restart.bat", config.ProcessRestartCommand);
        }
    }
}
