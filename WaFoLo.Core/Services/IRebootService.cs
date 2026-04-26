namespace WaFoLo.Services
{
    /// <summary>
    /// Interface for managing system reboot operations.
    /// </summary>
    public interface IRebootService
    {
        bool InitiateReboot(int delaySeconds = 30, string message = "Watchdog timeout - expected log entry not found");
        bool AbortReboot();
        bool HasAdministratorPrivileges();
    }
}
