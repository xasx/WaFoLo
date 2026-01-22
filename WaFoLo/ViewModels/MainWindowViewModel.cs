using System.ComponentModel;
using System.Runtime.CompilerServices;
using WaFoLo.Models;

namespace WaFoLo.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private WatchdogConfig? _config;
        private bool _isConfigVisible;
        private string _activityLog = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        public WatchdogConfig? Config
        {
            get => _config;
            set
            {
                _config = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LogFilePath));
                OnPropertyChanged(nameof(TriggerPattern));
                OnPropertyChanged(nameof(ExpectedPattern));
                OnPropertyChanged(nameof(TimeoutDisplay));
                OnPropertyChanged(nameof(ModeDisplay));
                OnPropertyChanged(nameof(LogTimestampFormat));
                OnPropertyChanged(nameof(LogTimestampPosition));
                OnPropertyChanged(nameof(MonitoredProcessNameDisplay));
                OnPropertyChanged(nameof(WaitForProcessDisplay));
                OnPropertyChanged(nameof(ProcessCheckIntervalDisplay));
                OnPropertyChanged(nameof(AutoCloseOnSuccessDisplay));
                OnPropertyChanged(nameof(AutoCloseDelayDisplay));
                OnPropertyChanged(nameof(RestartProcessOnTimeoutDisplay));
                OnPropertyChanged(nameof(RestartProcessDelayDisplay));
                OnPropertyChanged(nameof(MaxRestartAttemptsDisplay));
                OnPropertyChanged(nameof(ProcessRestartCommandDisplay));
            }
        }

        public bool IsConfigVisible
        {
            get => _isConfigVisible;
            set
            {
                _isConfigVisible = value;
                OnPropertyChanged();
            }
        }

        public string ActivityLog
        {
            get => _activityLog;
            set
            {
                _activityLog = value;
                OnPropertyChanged();
            }
        }

        public string LogFilePath => _config?.LogFilePath ?? string.Empty;
        public string TriggerPattern => _config?.TriggerLinePattern ?? string.Empty;
        public string ExpectedPattern => _config?.ExpectedLinePattern ?? string.Empty;
        public string TimeoutDisplay => $"{_config?.TimeoutSeconds ?? 0} seconds";
        public string ModeDisplay => _config?.TestMode == true ? "TEST MODE (no actual reboot)" : "PRODUCTION MODE";
        public string LogTimestampFormat => _config?.LogTimestampFormat ?? string.Empty;
        public string LogTimestampPosition => _config?.LogTimestampPosition ?? string.Empty;
        public string MonitoredProcessNameDisplay => string.IsNullOrWhiteSpace(_config?.MonitoredProcessName) ? "None" : _config!.MonitoredProcessName;
        public string WaitForProcessDisplay => _config?.WaitForMonitoredProcess == true ? "Yes" : "No";
        public string ProcessCheckIntervalDisplay => $"{_config?.ProcessCheckIntervalSeconds ?? 0} seconds";
        public string AutoCloseOnSuccessDisplay => _config?.AutoCloseOnSuccess == true ? "Yes" : "No";
        public string AutoCloseDelayDisplay => $"{_config?.AutoCloseDelaySeconds ?? 0} seconds";
        public string RestartProcessOnTimeoutDisplay => _config?.RestartProcessOnTimeout == true ? "Yes" : "No";
        public string RestartProcessDelayDisplay => $"{_config?.RestartProcessDelaySeconds ?? 0} seconds";
        public string MaxRestartAttemptsDisplay => $"{_config?.MaxRestartAttempts ?? 0}";
        public string ProcessRestartCommandDisplay => string.IsNullOrWhiteSpace(_config?.ProcessRestartCommand) ? "Not configured" : _config!.ProcessRestartCommand;


        public void AppendActivityLog(string message)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            ActivityLog += $"[{timestamp}] {message}\n";
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
