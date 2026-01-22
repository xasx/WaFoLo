using System.IO;
using WaFoLo.Models;
using WaFoLo.Utilities;

namespace WaFoLo.Services
{
    public class WatchdogOrchestrator : IDisposable
    {
        private readonly ILogMonitorServiceFactory _logMonitorServiceFactory;
        private readonly ITimestampParserFactory _timestampParserFactory;
        private ILogMonitorService? _logMonitor;
        private ITimestampParser? _timestampParser;

        public event EventHandler<string>? LogActivity;
        public event EventHandler<FileSystemEventArgs>? LogFileCreated;
        public event EventHandler<FileSystemEventArgs>? LogFileChanged;
        public event EventHandler<string>? NewLogLine;
        public event EventHandler<MonitoringStatusChangedEventArgs>? StatusChanged;

        public WatchdogOrchestrator(
            ILogMonitorServiceFactory logMonitorServiceFactory,
            ITimestampParserFactory timestampParserFactory)
        {
            _logMonitorServiceFactory = logMonitorServiceFactory ?? throw new ArgumentNullException(nameof(logMonitorServiceFactory));
            _timestampParserFactory = timestampParserFactory ?? throw new ArgumentNullException(nameof(timestampParserFactory));
        }

        public void InitializeTimestampParser(string timestampFormat)
        {
            _timestampParser = _timestampParserFactory.Create(timestampFormat);
        }

        public void StartMonitoring(string logFilePath)
        {
            LogActivity?.Invoke(this, $"[ORCHESTRATOR] StartMonitoring called at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            
            if (_logMonitor != null)
            {
                _logMonitor.LogFileCreated -= OnLogFileCreated;
                _logMonitor.LogFileChanged -= OnLogFileChanged;
                _logMonitor.NewLogLine -= OnNewLogLine;
                _logMonitor.DiagnosticLog -= OnDiagnosticLog;
                _logMonitor.Dispose();
            }

            _logMonitor = _logMonitorServiceFactory.Create();
            _logMonitor.LogFileCreated += OnLogFileCreated;
            _logMonitor.LogFileChanged += OnLogFileChanged;
            _logMonitor.NewLogLine += OnNewLogLine;
            _logMonitor.DiagnosticLog += OnDiagnosticLog;

            if (!File.Exists(logFilePath))
            {
                StatusChanged?.Invoke(this, new MonitoringStatusChangedEventArgs("Waiting for log file...", MonitoringStatus.WaitingForLogFile));
                LogActivity?.Invoke(this, $"Log file does not exist yet: {logFilePath}");
                LogActivity?.Invoke(this, "Waiting for log file to be created...");
            }
            else
            {
                StatusChanged?.Invoke(this, new MonitoringStatusChangedEventArgs("Monitoring active", MonitoringStatus.Active));
                LogActivity?.Invoke(this, "Monitoring started successfully.");
            }

            _logMonitor.StartMonitoring(logFilePath);
        }

        public List<LogLineInfo> ReadAllLines()
        {
            LogActivity?.Invoke(this, $"[ORCHESTRATOR] ReadAllLines called at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            return _logMonitor?.ReadAllLines() ?? new List<LogLineInfo>();
        }

        public DateTime? ExtractTimestamp(string line)
        {
            return _timestampParser?.ExtractTimestamp(line);
        }

        private void OnLogFileCreated(object? sender, FileSystemEventArgs e)
        {
            LogActivity?.Invoke(this, $"Log file created: {e.FullPath}");
            LogFileCreated?.Invoke(sender, e);
        }

        private void OnLogFileChanged(object? sender, FileSystemEventArgs e)
        {
            LogActivity?.Invoke(this, $"Log file changed: {e.FullPath}");
            LogFileChanged?.Invoke(sender, e);
        }

        private void OnNewLogLine(object? sender, string line)
        {
            LogActivity?.Invoke(this, $"[ORCHESTRATOR] NewLogLine event at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            NewLogLine?.Invoke(sender, line);
        }

        private void OnDiagnosticLog(object? sender, string message)
        {
            LogActivity?.Invoke(this, message);
        }

        public void Dispose()
        {
            _logMonitor?.Dispose();
        }
    }

    public class MonitoringStatusChangedEventArgs : EventArgs
    {
        public string StatusText { get; }
        public MonitoringStatus Status { get; }

        public MonitoringStatusChangedEventArgs(string statusText, MonitoringStatus status)
        {
            StatusText = statusText;
            Status = status;
        }
    }

    public enum MonitoringStatus
    {
        Idle,
        WaitingForProcess,
        WaitingForLogFile,
        Active,
        Error
    }
}
