namespace WaFoLo.Models
{
    /// <summary>
    /// Configuration settings for the watchdog monitor.
    /// </summary>
    public class WatchdogConfig
    {
        public string LogFilePath { get; set; } = string.Empty;
        public string TriggerLinePattern { get; set; } = string.Empty;
        public string ExpectedLinePattern { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; }
        public bool TestMode { get; set; }
        public bool ShowConfigurationOnStartup { get; set; }
        public string LogTimestampFormat { get; set; } = string.Empty;
        public string LogTimestampPosition { get; set; } = string.Empty;
        public string MonitoredProcessName { get; set; } = string.Empty;
        public bool WaitForMonitoredProcess { get; set; }
        public int ProcessCheckIntervalSeconds { get; set; } = 5;
        public bool AutoCloseOnSuccess { get; set; } = false;
        public int AutoCloseDelaySeconds { get; set; } = 10;
        
        /// <summary>
        /// Whether to attempt restarting the monitored process before rebooting on timeout
        /// </summary>
        public bool RestartProcessOnTimeout { get; set; } = false;

        /// <summary>
        /// How many seconds to wait after process restart before timing out again
        /// </summary>
        public int RestartProcessDelaySeconds { get; set; } = 15;

        /// <summary>
        /// Maximum number of process restart attempts before proceeding to reboot
        /// </summary>
        public int MaxRestartAttempts { get; set; } = 1;

        /// <summary>
        /// Command to execute for restarting the monitored process
        /// </summary>
        public string? ProcessRestartCommand { get; set; }
    }
}
