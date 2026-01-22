using System.Windows.Threading;

namespace WaFoLo.Services
{
    public class ProcessMonitor : IDisposable
    {
        private readonly IProcessDetectionService _processDetection;
        private readonly DateTime _applicationStartTime;
        private DispatcherTimer? _processCheckTimer;
        private string? _monitoredProcessName;
        private bool _isWaitingForProcess;

        public event EventHandler<string>? LogActivity;
        public event EventHandler<ProcessDetectedEventArgs>? ProcessDetected;
        public event EventHandler<MonitoringStatusChangedEventArgs>? StatusChanged;

        public DateTime? MonitoredProcessStartTime { get; private set; }

        public ProcessMonitor(IProcessDetectionService processDetection, DateTime applicationStartTime)
        {
            _processDetection = processDetection ?? throw new ArgumentNullException(nameof(processDetection));
            _applicationStartTime = applicationStartTime;
        }

        public DateTime DetectProcessStartTime(string processName)
        {
            _monitoredProcessName = processName;

            if (string.IsNullOrWhiteSpace(processName))
            {
                LogActivity?.Invoke(this, "No monitored process configured. Using application start time as session threshold.");
                MonitoredProcessStartTime = _applicationStartTime;
                return _applicationStartTime;
            }

            try
            {
                DateTime? processStartTime = _processDetection.DetectProcessStartTime(processName);

                if (processStartTime == null)
                {
                    LogActivity?.Invoke(this, $"Monitored process '{processName}' is not currently running.");
                    LogActivity?.Invoke(this, "Using application start time as session threshold.");
                    MonitoredProcessStartTime = _applicationStartTime;
                    return _applicationStartTime;
                }

                int instanceCount = _processDetection.GetProcessInstanceCount(processName);

                MonitoredProcessStartTime = processStartTime;
                LogActivity?.Invoke(this, $"Monitored process '{processName}' detected.");
                LogActivity?.Invoke(this, $"Process started at: {MonitoredProcessStartTime:yyyy-MM-dd HH:mm:ss}");
                LogActivity?.Invoke(this, $"Found {instanceCount} instance(s) of the process.");

                return processStartTime.Value;
            }
            catch (Exception ex)
            {
                LogActivity?.Invoke(this, $"Error detecting monitored process: {ex.Message}");
                LogActivity?.Invoke(this, "Using application start time as session threshold.");
                MonitoredProcessStartTime = _applicationStartTime;
                return _applicationStartTime;
            }
        }

        public void StartWaitingForProcess(string processName, int checkIntervalSeconds)
        {
            _monitoredProcessName = processName;
            _isWaitingForProcess = true;

            StatusChanged?.Invoke(this, new MonitoringStatusChangedEventArgs("Waiting for monitored process...", MonitoringStatus.WaitingForProcess));
            LogActivity?.Invoke(this, $"Waiting for monitored process '{processName}' to start...");
            LogActivity?.Invoke(this, $"Process check interval: {checkIntervalSeconds} seconds");

            _processCheckTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(checkIntervalSeconds)
            };
            _processCheckTimer.Tick += OnProcessCheckTimerTick;
            _processCheckTimer.Start();
        }

        public bool IsProcessRunning(string processName)
        {
            return _processDetection.IsProcessRunning(processName);
        }

        private void OnProcessCheckTimerTick(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_monitoredProcessName) || !_isWaitingForProcess)
                return;

            if (_processDetection.IsProcessRunning(_monitoredProcessName))
            {
                _processCheckTimer?.Stop();
                _isWaitingForProcess = false;

                LogActivity?.Invoke(this, $"Monitored process '{_monitoredProcessName}' detected!");
                ProcessDetected?.Invoke(this, new ProcessDetectedEventArgs(_monitoredProcessName));
            }
        }

        public void Dispose()
        {
            _processCheckTimer?.Stop();
        }
    }

    public class ProcessDetectedEventArgs : EventArgs
    {
        public string ProcessName { get; }

        public ProcessDetectedEventArgs(string processName)
        {
            ProcessName = processName;
        }
    }
}
