using System.IO;
using System.Text;
using WaFoLo.Models;

namespace WaFoLo.Services
{
    /// <summary>
    /// Service responsible for monitoring log files and detecting pattern matches.
    /// </summary>
    public class LogMonitorService : ILogMonitorService
    {
        private FileSystemWatcher? _fileWatcher;
        private StreamReader? _streamReader;
        private FileStream? _fileStream;
        private long _lastPosition;
        private Timer? _pollingTimer;
        private readonly object _lockObject = new object();
        
        public event EventHandler<FileSystemEventArgs>? LogFileCreated;
        public event EventHandler<FileSystemEventArgs>? LogFileChanged;
        public event EventHandler<string>? NewLogLine;
        public event EventHandler<string>? DiagnosticLog;

        public bool IsMonitoring => _fileWatcher?.EnableRaisingEvents == true;

        /// <summary>
        /// Start monitoring a log file for changes using polling.
        /// </summary>
        public void StartMonitoring(string logFilePath)
        {
            LogDiagnostic($"[DIAG] StartMonitoring called for: {logFilePath}");
            LogDiagnostic($"[DIAG] Current time: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            
            string directory = Path.GetDirectoryName(logFilePath) ?? string.Empty;
            
            if (!Directory.Exists(directory))
            {
                LogDiagnostic($"[DIAG] Directory does not exist, creating: {directory}");
                Directory.CreateDirectory(directory);
            }

            if (File.Exists(logFilePath))
            {
                LogDiagnostic($"[DIAG] File exists, opening for monitoring...");
                
                lock (_lockObject)
                {
                    try
                    {
                        _fileStream = new FileStream(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        _streamReader = new StreamReader(_fileStream, Encoding.UTF8);
                        _lastPosition = 0;

                        LogDiagnostic($"[DIAG] FileStream opened successfully");
                        LogDiagnostic($"[DIAG] Current file size: {_fileStream.Length} bytes");
                        LogDiagnostic($"[DIAG] File last write time: {File.GetLastWriteTime(logFilePath):yyyy-MM-dd HH:mm:ss.fff}");
                    }
                    catch (Exception ex)
                    {
                        LogDiagnostic($"[DIAG] ERROR opening file: {ex.Message}");
                        throw;
                    }
                }
            }
            else
            {
                LogDiagnostic($"[DIAG] File does not exist yet, will check for it during polling");
            }

            // Use polling timer exclusively - no FileSystemWatcher
            _pollingTimer = new Timer(CheckForNewLinesOrFile, logFilePath, TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(250));
            LogDiagnostic($"[DIAG] Polling timer started (250ms interval)");
        }

        /// <summary>
        /// Polling callback to check for file creation or new lines.
        /// </summary>
        private void CheckForNewLinesOrFile(object? state)
        {
            string logFilePath = (string)state!;
            
            lock (_lockObject)
            {
                // If file stream not open yet, try to open it
                if (_fileStream == null)
                {
                    if (File.Exists(logFilePath))
                    {
                        try
                        {
                            LogDiagnostic($"[DIAG] [POLLING] File now exists, opening: {logFilePath}");
                            _fileStream = new FileStream(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                            _streamReader = new StreamReader(_fileStream, Encoding.UTF8);
                            _lastPosition = 0;
                            LogDiagnostic($"[DIAG] [POLLING] FileStream opened successfully");
                            
                            // Notify that file was created
                            LogFileCreated?.Invoke(this, new FileSystemEventArgs(WatcherChangeTypes.Created, 
                                Path.GetDirectoryName(logFilePath) ?? string.Empty, 
                                Path.GetFileName(logFilePath)));
                        }
                        catch (Exception ex)
                        {
                            LogDiagnostic($"[DIAG] [POLLING] ERROR opening file: {ex.Message}");
                            return;
                        }
                    }
                    else
                    {
                        return; // File still doesn't exist
                    }
                }

                // Check for new content
                try
                {
                    long currentLength = _fileStream!.Length;
                    
                    if (currentLength > _lastPosition)
                    {
                        LogDiagnostic($"[DIAG] [POLLING] Detected new content: {currentLength - _lastPosition} bytes (position: {_lastPosition} -> {currentLength})");
                        ReadNewLinesInternal();
                        
                        // Notify that file changed
                        LogFileChanged?.Invoke(this, new FileSystemEventArgs(WatcherChangeTypes.Changed,
                            Path.GetDirectoryName(logFilePath) ?? string.Empty,
                            Path.GetFileName(logFilePath)));
                    }
                }
                catch (Exception ex)
                {
                    LogDiagnostic($"[DIAG] [POLLING] ERROR: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Read all lines from the file.
        /// </summary>
        public List<LogLineInfo> ReadAllLines()
        {
            lock (_lockObject)
            {
                LogDiagnostic($"[DIAG] ReadAllLines called at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                
                if (_fileStream == null || _streamReader == null)
                {
                    LogDiagnostic($"[DIAG] No file stream available");
                    return new List<LogLineInfo>();
                }

                long fileLength = _fileStream.Length;
                LogDiagnostic($"[DIAG] Current file length: {fileLength} bytes");
                
                if (fileLength == 0)
                {
                    LogDiagnostic($"[DIAG] File is empty");
                    return new List<LogLineInfo>();
                }

                // Seek to beginning of file
                _fileStream.Seek(0, SeekOrigin.Begin);
                _streamReader.DiscardBufferedData();
                LogDiagnostic($"[DIAG] Seeked to beginning of file");

                var lines = new List<LogLineInfo>();
                int lineNumber = 1;
                string? line;

                while ((line = _streamReader.ReadLine()) != null)
                {
                    lines.Add(new LogLineInfo
                    {
                        Content = line,
                        LineNumber = lineNumber++
                    });
                }

                _lastPosition = _fileStream.Position;
                LogDiagnostic($"[DIAG] Read {lines.Count} lines, new position: {_lastPosition}");

                return lines;
            }
        }

        /// <summary>
        /// Read the last lines from the file up to a maximum count.
        /// </summary>
        public List<LogLineInfo> ReadExistingLines(int maxLines)
        {
            lock (_lockObject)
            {
                LogDiagnostic($"[DIAG] ReadExistingLines called (maxLines: {maxLines})");
                
                if (_fileStream == null || _streamReader == null)
                    return new List<LogLineInfo>();

                long fileLength = _fileStream.Length;
                if (fileLength == 0)
                    return new List<LogLineInfo>();

                const int bufferSize = 8192;
                byte[] buffer = new byte[bufferSize];
                var lines = new List<string>(maxLines);
                long position = fileLength;
                var currentLine = new List<byte>(256);
                bool isFirstIteration = true;

                // Read backwards in chunks
                while (position > 0 && lines.Count < maxLines)
                {
                    int bytesToRead = (int)Math.Min(bufferSize, position);
                    position -= bytesToRead;
                    
                    _fileStream.Seek(position, SeekOrigin.Begin);
                    int bytesRead = _fileStream.Read(buffer, 0, bytesToRead);

                    // Process buffer backwards
                    for (int i = bytesRead - 1; i >= 0; i--)
                    {
                        byte b = buffer[i];
                        
                        if (b == '\n')
                        {
                            // Skip trailing newline at end of file
                            if (isFirstIteration && currentLine.Count == 0)
                            {
                                isFirstIteration = false;
                                continue;
                            }
                            
                            // Found a complete line
                            if (currentLine.Count > 0 || !isFirstIteration)
                            {
                                currentLine.Reverse();
                                
                                // Remove trailing \r if present (Windows line ending)
                                if (currentLine.Count > 0 && currentLine[currentLine.Count - 1] == '\r')
                                {
                                    currentLine.RemoveAt(currentLine.Count - 1);
                                }
                                
                                string line = Encoding.UTF8.GetString(currentLine.ToArray());
                                lines.Add(line);
                                currentLine.Clear();
                                
                                if (lines.Count >= maxLines)
                                    break;
                            }
                            isFirstIteration = false;
                        }
                        else
                        {
                            currentLine.Add(b);
                            isFirstIteration = false;
                        }
                    }
                    
                    if (lines.Count >= maxLines)
                        break;
                }

                // Handle any remaining bytes as the first line (if we reached start of file)
                if (currentLine.Count > 0 && lines.Count < maxLines)
                {
                    currentLine.Reverse();
                    
                    // Remove leading \r if present
                    if (currentLine.Count > 0 && currentLine[0] == '\r')
                    {
                        currentLine.RemoveAt(0);
                    }

                    string line = Encoding.UTF8.GetString(currentLine.ToArray());
                    lines.Add(line);
                }

                // Lines are in reverse order, so reverse them back
                lines.Reverse();

                // Convert to LogLineInfo
                var result = lines.Select((line, index) => new LogLineInfo
                {
                    Content = line,
                    LineNumber = index + 1  // This is relative to the subset
                }).ToList();

                // Position at end of file for monitoring new content
                _fileStream.Seek(fileLength, SeekOrigin.Begin);
                _streamReader.DiscardBufferedData();
                _lastPosition = fileLength;

                LogDiagnostic($"[DIAG] Read {result.Count} existing lines, positioned at end");

                return result;
            }
        }

        private void OnLogFileCreated(object? sender, FileSystemEventArgs e)
        {
            LogDiagnostic($"[DIAG] [FSW] File created event: {e.FullPath} at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            LogFileCreated?.Invoke(sender, e);
        }

        private void OnLogFileChanged(object? sender, FileSystemEventArgs e)
        {
            LogDiagnostic($"[DIAG] [FSW] File changed event at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            
            lock (_lockObject)
            {
                ReadNewLinesInternal();
            }

            LogFileChanged?.Invoke(sender, e);
        }

        private void ReadNewLinesInternal()
        {
            if (_fileStream == null || _streamReader == null)
                return;

            try
            {
                long currentLength = _fileStream.Length;
                
                if (currentLength < _lastPosition)
                {
                    // File was truncated or rotated
                    LogDiagnostic($"[DIAG] File truncated/rotated (was {_lastPosition}, now {currentLength})");
                    _lastPosition = 0;
                    _fileStream.Seek(0, SeekOrigin.Begin);
                    _streamReader.DiscardBufferedData();
                }
                else if (currentLength == _lastPosition)
                {
                    // No new content
                    return;
                }

                _fileStream.Seek(_lastPosition, SeekOrigin.Begin);
                _streamReader.DiscardBufferedData();

                string? line;
                int linesRead = 0;
                while ((line = _streamReader.ReadLine()) != null)
                {
                    linesRead++;
                    LogDiagnostic($"[DIAG] New line detected at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}: {line.Substring(0, Math.Min(50, line.Length))}...");
                    NewLogLine?.Invoke(this, line);
                }

                _lastPosition = _fileStream.Position;
                LogDiagnostic($"[DIAG] Read {linesRead} new line(s), new position: {_lastPosition}");
            }
            catch (Exception ex)
            {
                LogDiagnostic($"[DIAG] ERROR reading new lines: {ex.Message}");
            }
        }

        private void LogDiagnostic(string message)
        {
            DiagnosticLog?.Invoke(this, message);
        }

        public void Dispose()
        {
            LogDiagnostic($"[DIAG] Disposing LogMonitorService at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            
            _pollingTimer?.Dispose();
            _pollingTimer = null;

            _streamReader?.Dispose();
            _fileStream?.Dispose();
            
            LogDiagnostic($"[DIAG] LogMonitorService disposed");
        }
    }
}
