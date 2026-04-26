namespace WaFoLo.Services
{
    public class RebootManager
    {
        private readonly IRebootService _rebootService;
        private readonly IDialogService _dialogService;

        public event EventHandler<string>? LogActivity;
        public event EventHandler? RebootInitiated;
        public event EventHandler? RebootAborted;

        public RebootManager(IRebootService rebootService, IDialogService dialogService)
        {
            _rebootService = rebootService ?? throw new ArgumentNullException(nameof(rebootService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        }

        public bool HandleTimeout(bool testMode, DateTime? triggerTime, int timeoutSeconds)
        {
            if (testMode)
            {
                LogActivity?.Invoke(this, "TEST MODE: Reboot would be triggered now.");
                _dialogService.ShowWarning(
                    $"TIMEOUT OCCURRED!\n\nTrigger time: {triggerTime:yyyy-MM-dd HH:mm:ss}\nTimeout: {timeoutSeconds} seconds\n\nIn production mode, the system would reboot now.",
                    "Test Mode - Reboot Triggered");
                return true;
            }
            else
            {
                LogActivity?.Invoke(this, "Initiating system reboot in 30 seconds...");
                return InitiateReboot();
            }
        }

        public bool InitiateReboot()
        {
            try
            {
                bool success = _rebootService.InitiateReboot(30, "Watchdog timeout - expected log entry not found");

                if (success)
                {
                    LogActivity?.Invoke(this, "System reboot scheduled in 30 seconds. Click ABORT to cancel.");
                    RebootInitiated?.Invoke(this, EventArgs.Empty);
                    return true;
                }
                else
                {
                    throw new InvalidOperationException("Failed to start reboot process.");
                }
            }
            catch (Exception ex)
            {
                LogActivity?.Invoke(this, $"Failed to initiate reboot: {ex.Message}");

                if (!_rebootService.HasAdministratorPrivileges())
                {
                    LogActivity?.Invoke(this, "Note: Administrator privileges are required.");
                    _dialogService.ShowError(
                        $"Failed to initiate reboot: {ex.Message}\n\nThis application requires administrator privileges.",
                        "Reboot Error");
                }
                else
                {
                    _dialogService.ShowError(
                        $"Failed to initiate reboot: {ex.Message}",
                        "Reboot Error");
                }
                return false;
            }
        }

        public bool AbortReboot()
        {
            try
            {
                bool success = _rebootService.AbortReboot();

                if (success)
                {
                    LogActivity?.Invoke(this, "Shutdown aborted by user.");
                    RebootAborted?.Invoke(this, EventArgs.Empty);

                    _dialogService.ShowInfo(
                        "Shutdown has been cancelled.",
                        "Shutdown Aborted");
                    return true;
                }
                else
                {
                    throw new InvalidOperationException("Failed to abort shutdown.");
                }
            }
            catch (Exception ex)
            {
                LogActivity?.Invoke(this, $"Failed to abort shutdown: {ex.Message}");
                _dialogService.ShowError(
                    $"Failed to abort shutdown: {ex.Message}",
                    "Error");
                return false;
            }
        }
    }
}
