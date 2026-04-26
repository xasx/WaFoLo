using System.IO;
using WaFoLo.Models;

namespace WaFoLo.Services
{
    /// <summary>
    /// Interface for monitoring log files and detecting pattern matches.
    /// </summary>
    public interface ILogMonitorService : IDisposable
    {
        event EventHandler<FileSystemEventArgs>? LogFileCreated;
        event EventHandler<FileSystemEventArgs>? LogFileChanged;
        event EventHandler<string>? NewLogLine;
        event EventHandler<string>? DiagnosticLog;

        bool IsMonitoring { get; }

        void StartMonitoring(string logFilePath);
        List<LogLineInfo> ReadAllLines();
        List<LogLineInfo> ReadExistingLines(int maxLines);
    }
}
