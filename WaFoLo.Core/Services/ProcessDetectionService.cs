using System.Diagnostics;

namespace WaFoLo.Services
{
    /// <summary>
    /// Service for detecting and monitoring external processes.
    /// </summary>
    public class ProcessDetectionService : IProcessDetectionService
    {
        /// <summary>
        /// Detects the start time of a monitored process.
        /// Returns null if the process is not running or an error occurs.
        /// </summary>
        public DateTime? DetectProcessStartTime(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName))
                return null;

            try
            {
                var processes = Process.GetProcessesByName(processName);

                if (processes.Length == 0)
                    return null;

                // Get the earliest start time if multiple instances exist
                return processes.Min(p => p.StartTime);
            }
            catch (Exception)
            {
                // Process access denied or other error
                return null;
            }
        }

        /// <summary>
        /// Checks if a process is currently running.
        /// </summary>
        public bool IsProcessRunning(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName))
                return false;

            try
            {
                var processes = Process.GetProcessesByName(processName);
                return processes.Length > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the count of running instances of a process.
        /// </summary>
        public int GetProcessInstanceCount(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName))
                return 0;

            try
            {
                var processes = Process.GetProcessesByName(processName);
                return processes.Length;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        /// <summary>
        /// Gets the full executable path of a running process.
        /// Returns null if the process is not found or access is denied.
        /// </summary>
        public string? GetProcessExecutablePath(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName))
                return null;

            try
            {
                var processes = Process.GetProcessesByName(processName);

                if (processes.Length == 0)
                    return null;

                // Return the path of the first instance
                return processes[0].MainModule?.FileName;
            }
            catch (Exception)
            {
                // Process access denied or other error
                return null;
            }
        }
    }
}
