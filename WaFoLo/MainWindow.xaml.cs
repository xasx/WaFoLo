using MahApps.Metro.Controls;
using System.IO;
using System.Windows;
using WaFoLo.Models;
using WaFoLo.Services;
using WaFoLo.Utilities;
using WaFoLo.ViewModels;

namespace WaFoLo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private readonly IConfigurationService _configService;
        private readonly WatchdogOrchestrator _orchestrator;
        private readonly TimeoutManager _timeoutManager;
        private readonly ProcessMonitor _processMonitor;
        private readonly RebootManager _rebootManager;
        private readonly AutoCloseManager _autoCloseManager;
        private readonly MainWindowViewModel _viewModel;
        private readonly DateTime _applicationStartTime;

        private LogScanner? _logScanner;
        private WatchdogConfig? _config;
        private readonly IFileLoggerService _fileLogger;

        public MainWindow(
            IConfigurationService configService,
            ITimestampParserFactory timestampParserFactory,
            ILogMonitorServiceFactory logMonitorServiceFactory,
            IProcessDetectionService processDetection,
            IRebootService rebootService,
            IFileLoggerService fileLogger)
        {
            InitializeComponent();

            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _fileLogger = fileLogger ?? throw new ArgumentNullException(nameof(fileLogger));
            _applicationStartTime = DateTime.Now;

            _orchestrator = new WatchdogOrchestrator(logMonitorServiceFactory, timestampParserFactory);
            _timeoutManager = new TimeoutManager();
            _processMonitor = new ProcessMonitor(processDetection, _applicationStartTime);
            _rebootManager = new RebootManager(rebootService);
            _autoCloseManager = new AutoCloseManager();
            _viewModel = new MainWindowViewModel();

            DataContext = _viewModel;

            WireUpEventHandlers();

            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;

            LogActivity($"Application started. Log file: {_fileLogger.LogFilePath}");
        }

        private void WireUpEventHandlers()
        {
            _orchestrator.LogActivity += (s, msg) => LogActivity(msg);
            _orchestrator.LogFileCreated += OnLogFileCreated;
            
            // CRITICAL FIX: Use BeginInvoke instead of Invoke to prevent blocking the monitoring thread
            _orchestrator.NewLogLine += (s, line) => Dispatcher.BeginInvoke(() => ProcessLogLine(line));
            _orchestrator.StatusChanged += (s, e) => Dispatcher.BeginInvoke(() => UpdateStatus(e.StatusText, e.Status));

            _timeoutManager.TimeoutOccurred += (s, e) => Dispatcher.BeginInvoke(HandleTimeout);
            _timeoutManager.ProgressUpdated += (s, e) => Dispatcher.BeginInvoke(() => UpdateTimeoutProgress(e));
            _timeoutManager.TriggerReset += (s, e) => Dispatcher.BeginInvoke(ResetTriggerUI);

            _processMonitor.LogActivity += (s, msg) => LogActivity(msg);
            _processMonitor.ProcessDetected += (s, e) => Dispatcher.BeginInvoke(OnProcessDetected);
            _processMonitor.StatusChanged += (s, e) => Dispatcher.BeginInvoke(() => UpdateStatus(e.StatusText, e.Status));

            _rebootManager.LogActivity += (s, msg) => LogActivity(msg);
            _rebootManager.RebootInitiated += (s, e) => Dispatcher.BeginInvoke(() => AbortButton.Visibility = Visibility.Visible);
            _rebootManager.RebootAborted += (s, e) => Dispatcher.BeginInvoke(() => _timeoutManager.Reset());

            _autoCloseManager.LogActivity += (s, msg) => LogActivity(msg);
            _autoCloseManager.CountdownUpdated += (s, countdown) => Dispatcher.BeginInvoke(() => CloseButton.Content = $"CLOSE APPLICATION ({countdown}s)");
            _autoCloseManager.ApplicationClosing += (s, e) => Dispatcher.BeginInvoke(() => Application.Current.Shutdown());
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await LoadConfigurationAsync();
                _viewModel.Config = _config;

                if (_config?.ShowConfigurationOnStartup == true)
                {
                    ShowConfiguration();
                }

                StartMonitoring();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to initialize: {ex.Message}\n\nThe application will close.",
                    "Initialization Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }

        private async Task LoadConfigurationAsync()
        {
            _config = await _configService.LoadConfigurationAsync();

            if (File.Exists(_configService.GetConfigPath()))
            {
                LogActivity($"Loaded configuration from: {_configService.GetConfigPath()}");
            }
            else
            {
                LogActivity($"Created default config file: {_configService.GetConfigPath()}");
            }

            if (string.IsNullOrWhiteSpace(_config.LogFilePath))
            {
                throw new InvalidOperationException("Log file path is not configured.");
            }

            if (!File.Exists(_config.LogFilePath))
            {
                LogActivity($"Log file does not exist yet: {_config.LogFilePath}");
                LogActivity("Waiting for log file to be created...");
            }

            _orchestrator.InitializeTimestampParser(_config.LogTimestampFormat);
        }

        private void ToggleConfig_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.IsConfigVisible)
            {
                HideConfiguration();
            }
            else
            {
                ShowConfiguration();
            }
        }

        private void ShowConfiguration()
        {
            ConfigGroupBox.Visibility = Visibility.Visible;
            ToggleConfigButton.Content = "Hide Configuration";
            _viewModel.IsConfigVisible = true;
        }

        private void HideConfiguration()
        {
            ConfigGroupBox.Visibility = Visibility.Collapsed;
            ToggleConfigButton.Content = "Show Configuration";
            _viewModel.IsConfigVisible = false;
        }

        private void UpdateStatus(string statusText, MonitoringStatus status)
        {
            StatusTextBlock.Text = statusText;
            StatusTextBlock.Foreground = status switch
            {
                MonitoringStatus.Active => System.Windows.Media.Brushes.Green,
                MonitoringStatus.WaitingForProcess => System.Windows.Media.Brushes.Orange,
                MonitoringStatus.WaitingForLogFile => System.Windows.Media.Brushes.Orange,
                MonitoringStatus.Error => System.Windows.Media.Brushes.Red,
                _ => System.Windows.Media.Brushes.Black
            };
        }

        private void StartMonitoring()
        {
            try
            {
                if (_config == null) return;

                if (_config.WaitForMonitoredProcess && !string.IsNullOrWhiteSpace(_config.MonitoredProcessName))
                {
                    if (!_processMonitor.IsProcessRunning(_config.MonitoredProcessName))
                    {
                        _processMonitor.StartWaitingForProcess(_config.MonitoredProcessName, _config.ProcessCheckIntervalSeconds);
                        return;
                    }
                }

                DateTime processStartTime = _processMonitor.DetectProcessStartTime(_config.MonitoredProcessName);
                StartLogMonitoring(processStartTime);
            }
            catch (Exception ex)
            {
                UpdateStatus("Error", MonitoringStatus.Error);
                LogActivity($"Failed to start monitoring: {ex.Message}");
            }
        }

        private void OnProcessDetected()
        {
            if (_config == null) return;
            DateTime processStartTime = _processMonitor.DetectProcessStartTime(_config.MonitoredProcessName);
            StartLogMonitoring(processStartTime);
        }

        private void StartLogMonitoring(DateTime sessionThreshold)
        {
            try
            {
                if (_config == null) return;

                _orchestrator.StartMonitoring(_config.LogFilePath);

                if (File.Exists(_config.LogFilePath))
                {
                    ScanExistingLines(sessionThreshold);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus("Error", MonitoringStatus.Error);
                LogActivity($"Failed to start monitoring: {ex.Message}");
            }
        }

        private void ScanExistingLines(DateTime sessionThreshold)
        {
            if (_config == null) return;

            try
            {
                var timestampParser = new TimestampParser(_config.LogTimestampFormat);
                _logScanner = new LogScanner(timestampParser);
                _logScanner.LogActivity += (s, msg) => LogActivity(msg);

                var allLines = _orchestrator.ReadAllLines();
                string thresholdSource = _processMonitor.MonitoredProcessStartTime.HasValue
                    ? $"process '{_config.MonitoredProcessName}' start time"
                    : "application start time";

                var result = _logScanner.ScanExistingLines(
                    allLines,
                    sessionThreshold,
                    _config.TriggerLinePattern,
                    _config.ExpectedLinePattern,
                    thresholdSource);

                switch (result.Status)
                {
                    case ScanStatus.NoTriggerFound:
                        LastTriggerTextBlock.Text = "Waiting for trigger...";
                        break;

                    case ScanStatus.SequenceCompleted:
                        LastTriggerTextBlock.Text = $"Last trigger: Completed (line {result.LastTriggerLine?.LineNumber})";
                        ShowSuccessOptions();
                        break;

                    case ScanStatus.IncompleteSequence:
                        var triggerTime = result.LastTriggerLine?.Timestamp ?? DateTime.Now;
                        LastTriggerTextBlock.Text = $"Last trigger: {triggerTime:yyyy-MM-dd HH:mm:ss} (line {result.LastTriggerLine?.LineNumber})";

                        TimeSpan elapsed = DateTime.Now - triggerTime;
                        double remainingTime = _config.TimeoutSeconds - elapsed.TotalSeconds;

                        if (remainingTime > 0)
                        {
                            LogActivity($"Time elapsed since trigger: {elapsed.TotalSeconds:F1}s, remaining: {remainingTime:F1}s");
                            TimeoutProgressBar.Visibility = Visibility.Visible;
                            TimeoutCountdownText.Visibility = Visibility.Visible;
                            _timeoutManager.StartTimeout(triggerTime, _config.TimeoutSeconds);
                        }
                        else
                        {
                            LogActivity($"TIMEOUT already exceeded! Elapsed: {elapsed.TotalSeconds:F1}s");
                            HandleTimeout();
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                LogActivity($"Error scanning existing lines: {ex.Message}");
            }
        }

        private void OnLogFileCreated(object? sender, FileSystemEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (_config == null) return;
                DateTime sessionThreshold = _processMonitor.MonitoredProcessStartTime ?? _applicationStartTime;
                StartLogMonitoring(sessionThreshold);
            });
        }

        private void ProcessLogLine(string line)
        {
            if (_config == null) return;

            _fileLogger.LogMessage($"[ProcessLogLine] Processing: {line}");
            _fileLogger.LogMessage($"[ProcessLogLine] TimeoutManager.IsActive: {_timeoutManager.IsActive}");

            if (line.Contains(_config.TriggerLinePattern, StringComparison.OrdinalIgnoreCase))
            {
                DateTime? timestamp = _orchestrator.ExtractTimestamp(line);
                DateTime triggerTime = timestamp ?? DateTime.Now;

                LastTriggerTextBlock.Text = $"Last trigger: {triggerTime:yyyy-MM-dd HH:mm:ss}";
                LogActivity($"TRIGGER DETECTED: {line}");

                TimeoutProgressBar.Value = 0;
                TimeoutProgressBar.Visibility = Visibility.Visible;
                TimeoutCountdownText.Visibility = Visibility.Visible;

                _timeoutManager.StartTimeout(triggerTime, _config.TimeoutSeconds);
                _fileLogger.LogMessage($"[ProcessLogLine] Started timeout monitoring");
            }

            if (_timeoutManager.IsActive && line.Contains(_config.ExpectedLinePattern, StringComparison.OrdinalIgnoreCase))
            {
                TimeSpan elapsed = DateTime.Now - _timeoutManager.TriggerTime!.Value;
                LogActivity($"EXPECTED LINE FOUND after {elapsed.TotalSeconds:F1}s: {line}");
                _fileLogger.LogMessage($"[ProcessLogLine] EXPECTED LINE DETECTED - Stopping timeout");

                _timeoutManager.Reset();
                ShowSuccessOptions();
            }
            else if (_timeoutManager.IsActive)
            {
                _fileLogger.LogMessage($"[ProcessLogLine] Waiting for expected pattern. Current line does not match.");
            }
        }

        private void UpdateTimeoutProgress(TimeoutProgressEventArgs e)
        {
            TimeoutProgressBar.Value = e.Percentage;
            TimeoutCountdownText.Text = $"Time remaining: {e.RemainingSeconds:F1}s";
        }

        private void ResetTriggerUI()
        {
            TimeoutProgressBar.Visibility = Visibility.Collapsed;
            TimeoutCountdownText.Visibility = Visibility.Collapsed;
            LastTriggerTextBlock.Text = "Last trigger: Completed successfully";
            AbortButton.Visibility = Visibility.Collapsed;
        }

        private void ShowSuccessOptions()
        {
            if (_config == null) return;

            CloseButton.Visibility = Visibility.Visible;

            if (_config.AutoCloseOnSuccess)
            {
                _autoCloseManager.StartCountdown(_config.AutoCloseDelaySeconds);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            _autoCloseManager.Stop();
            LogActivity("Application closed by user.");
            Application.Current.Shutdown();
        }

        private int _processRestartAttempts = 0;

        private void HandleTimeout()
        {
            if (_config == null) return;

            LogActivity($"TIMEOUT! Expected line not found within {_config.TimeoutSeconds} seconds.");

            // Check if process restart should be attempted first
            if (_config.RestartProcessOnTimeout && 
                !string.IsNullOrWhiteSpace(_config.MonitoredProcessName) &&
                _processRestartAttempts < _config.MaxRestartAttempts)
            {
                AttemptProcessRestart();
                return;
            }

            // Reset restart counter for next timeout scenario
            _processRestartAttempts = 0;

            // Proceed with reboot
            if (_rebootManager.HandleTimeout(_config.TestMode, _timeoutManager.TriggerTime, _config.TimeoutSeconds))
            {
                if (_config.TestMode)
                {
                    _timeoutManager.Reset();
                }
            }
        }
        private async void AttemptProcessRestart()
        {
            if (_config == null) return;

            _processRestartAttempts++;
            LogActivity($"Attempting to restart process '{_config.MonitoredProcessName}' (attempt {_processRestartAttempts}/{_config.MaxRestartAttempts})...");

            try
            {
                // Kill all instances of the process
                var processes = System.Diagnostics.Process.GetProcessesByName(_config.MonitoredProcessName);

                if (processes.Length > 0)
                {
                    foreach (var proc in processes)
                    {
                        LogActivity($"Stopping process PID {proc.Id}...");
                        proc.Kill();
                        proc.WaitForExit(5000); // Wait up to 5 seconds for graceful exit
                        LogActivity($"Process stopped.");
                    }
                }
                else
                {
                    LogActivity($"WARNING: Process '{_config.MonitoredProcessName}' not found.");
                }

                // Wait a moment for cleanup
                await Task.Delay(2000);

                // Attempt to restart the process using configured command
                if (!string.IsNullOrWhiteSpace(_config.ProcessRestartCommand))
                {
                    LogActivity($"Restarting process using command: {_config.ProcessRestartCommand}");

                    var startInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = _config.ProcessRestartCommand,
                        UseShellExecute = true,
                        CreateNoWindow = false
                    };

                    System.Diagnostics.Process.Start(startInfo);
                    LogActivity($"Process restart command executed.");
                }
                else
                {
                    LogActivity($"No restart command configured. Waiting for external restart...");
                }

                // Wait for process to stabilize
                LogActivity($"Waiting {_config.RestartProcessDelaySeconds} seconds for process to stabilize...");
                await Task.Delay(_config.RestartProcessDelaySeconds * 1000);

                // Check if process is running again
                if (_processMonitor.IsProcessRunning(_config.MonitoredProcessName))
                {
                    LogActivity($"Process '{_config.MonitoredProcessName}' detected running after restart.");

                    // Reset the timeout manager and start monitoring again
                    _timeoutManager.Reset();

                    DateTime restartTime = DateTime.Now;
                    LogActivity($"Resuming monitoring from process restart time: {restartTime:yyyy-MM-dd HH:mm:ss}");

                    // Start a new timeout period to see if the sequence completes
                    _timeoutManager.StartTimeout(restartTime, _config.TimeoutSeconds);
                    LastTriggerTextBlock.Text = $"Monitoring after process restart: {restartTime:yyyy-MM-dd HH:mm:ss}";
                    TimeoutProgressBar.Visibility = Visibility.Visible;
                    TimeoutCountdownText.Visibility = Visibility.Visible;
                }
                else
                {
                    LogActivity($"ERROR: Process '{_config.MonitoredProcessName}' did not restart.");
                    HandleRestartFailure();
                }
            }
            catch (Exception ex)
            {
                LogActivity($"ERROR restarting process: {ex.Message}");
                HandleRestartFailure();
            }
        }

        private void HandleRestartFailure()
        {
            LogActivity("Proceeding to reboot...");
            _processRestartAttempts = 0;
            
            if (_rebootManager.HandleTimeout(_config.TestMode, _timeoutManager.TriggerTime, _config.TimeoutSeconds))
            {
                if (_config.TestMode)
                {
                    _timeoutManager.Reset();
                }
            }
        }

        private void LogActivity(string message)
        {
            _viewModel.AppendActivityLog(message);
            Dispatcher.BeginInvoke(() => ActivityLogTextBox.ScrollToEnd(), System.Windows.Threading.DispatcherPriority.Background);
            _fileLogger.LogMessage(message);
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _timeoutManager.Dispose();
            _processMonitor.Dispose();
            _autoCloseManager.Dispose();
            _orchestrator.Dispose();
        }

        private void AbortShutdown_Click(object sender, RoutedEventArgs e)
        {
            _rebootManager.AbortReboot();
        }
    }
}
