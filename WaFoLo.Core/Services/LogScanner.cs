using WaFoLo.Models;
using WaFoLo.Utilities;

namespace WaFoLo.Services
{
    public class LogScanner
    {
        private readonly ITimestampParser _timestampParser;

        public event EventHandler<string>? LogActivity;

        public LogScanner(ITimestampParser timestampParser)
        {
            _timestampParser = timestampParser ?? throw new ArgumentNullException(nameof(timestampParser));
        }

        public LogScanResult ScanExistingLines(
            List<LogLineInfo> allLines,
            DateTime sessionThreshold,
            string triggerPattern,
            string expectedPattern,
            string thresholdSource)
        {
            LogActivity?.Invoke(this, "Scanning existing log lines...");
            LogActivity?.Invoke(this, $"Session threshold: {sessionThreshold:yyyy-MM-dd HH:mm:ss} ({thresholdSource})");

            foreach (var line in allLines)
            {
                line.Timestamp = _timestampParser.ExtractTimestamp(line.Content);
            }

            int totalLineCount = allLines.Count;
            LogActivity?.Invoke(this, $"Read {totalLineCount} total lines from log file.");

            var relevantLines = allLines
                .Where(l => l.Timestamp.HasValue && l.Timestamp.Value >= sessionThreshold)
                .ToList();

            int filteredCount = totalLineCount - relevantLines.Count;
            LogActivity?.Invoke(this, $"Filtered out {filteredCount} lines from before session threshold.");
            LogActivity?.Invoke(this, $"Analyzing {relevantLines.Count} lines from current session.");

            var triggerLines = relevantLines
                .Where(l => l.Content.Contains(triggerPattern, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var expectedLines = relevantLines
                .Where(l => l.Content.Contains(expectedPattern, StringComparison.OrdinalIgnoreCase))
                .ToList();

            LogActivity?.Invoke(this, $"Found {triggerLines.Count} trigger line(s) and {expectedLines.Count} expected line(s) in current session.");

            if (triggerLines.Count == 0)
            {
                LogActivity?.Invoke(this, "No trigger pattern found in current session. Waiting for new entries...");
                return new LogScanResult
                {
                    Status = ScanStatus.NoTriggerFound
                };
            }

            var lastTrigger = triggerLines.Last();
            LogActivity?.Invoke(this, $"Last trigger at line {lastTrigger.LineNumber}: {lastTrigger.Timestamp?.ToString("yyyy-MM-dd HH:mm:ss") ?? "no timestamp"}");

            var expectedAfterTrigger = expectedLines
                .Where(e => e.LineNumber > lastTrigger.LineNumber)
                .OrderBy(e => e.LineNumber)
                .LastOrDefault();

            if (expectedAfterTrigger != null)
            {
                LogActivity?.Invoke(this, $"Found expected line after last trigger at line {expectedAfterTrigger.LineNumber}.");
                LogActivity?.Invoke(this, "Sequence completed in current session. System is healthy.");
                return new LogScanResult
                {
                    Status = ScanStatus.SequenceCompleted,
                    LastTriggerLine = lastTrigger
                };
            }
            else
            {
                LogActivity?.Invoke(this, "INCOMPLETE SEQUENCE: Trigger found without expected line in current session.");
                LogActivity?.Invoke(this, "Starting timeout monitoring NOW...");

                return new LogScanResult
                {
                    Status = ScanStatus.IncompleteSequence,
                    LastTriggerLine = lastTrigger
                };
            }
        }
    }

    public class LogScanResult
    {
        public ScanStatus Status { get; set; }
        public LogLineInfo? LastTriggerLine { get; set; }
    }

    public enum ScanStatus
    {
        NoTriggerFound,
        SequenceCompleted,
        IncompleteSequence
    }
}
