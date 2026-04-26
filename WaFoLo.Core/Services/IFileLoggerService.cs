namespace WaFoLo.Services
{
    /// <summary>
    /// Service for writing log messages to a file
    /// </summary>
    public interface IFileLoggerService
    {
        /// <summary>
        /// Writes a log message to the log file
        /// </summary>
        /// <param name="message">The message to log</param>
        void LogMessage(string message);

        /// <summary>
        /// Gets the path to the log file
        /// </summary>
        string LogFilePath { get; }
    }
}