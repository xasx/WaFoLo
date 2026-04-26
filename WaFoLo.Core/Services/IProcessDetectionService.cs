namespace WaFoLo.Services
{
    /// <summary>
    /// Interface for detecting and monitoring external processes.
    /// </summary>
    public interface IProcessDetectionService
    {
        DateTime? DetectProcessStartTime(string processName);
        bool IsProcessRunning(string processName);
        int GetProcessInstanceCount(string processName);
    }
}
