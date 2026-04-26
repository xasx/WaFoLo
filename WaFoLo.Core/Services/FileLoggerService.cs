using System.IO;

namespace WaFoLo.Services
{
    /// <summary>
    /// Service for writing log messages to a file with thread-safe file access
    /// </summary>
    public class FileLoggerService : IFileLoggerService
    {
        private readonly string _logFilePath;
        private readonly object _lockObject = new object();

        public string LogFilePath => _logFilePath;

        public FileLoggerService(string logDirectory = "Logs")
        {
            var logLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, logDirectory);
            // Create logs directory if it doesn't exist
            if (!Directory.Exists(logLocation))
            {
                Directory.CreateDirectory(logLocation);
            }

            // Create log file with timestamp in name
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _logFilePath = Path.Combine(logLocation, $"WaFoLo_{timestamp}.log");
        }

        public void LogMessage(string message)
        {
            try
            {
                string timestampedMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";

                lock (_lockObject)
                {
                    File.AppendAllText(_logFilePath, timestampedMessage + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                // Silently fail to avoid disrupting the application
                // Could optionally write to Event Log or Debug output
                System.Diagnostics.Debug.WriteLine($"Failed to write to log file: {ex.Message}");
            }
        }
    }
}
