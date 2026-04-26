using System.Globalization;
using System.Text.RegularExpressions;

namespace WaFoLo.Utilities
{
    /// <summary>
    /// Utility for parsing timestamps from log lines.
    /// </summary>
    public class TimestampParser : ITimestampParser
    {
        private readonly string _primaryFormat;
        private readonly string[] _fallbackFormats;

        public TimestampParser(string primaryFormat)
        {
            _primaryFormat = primaryFormat;
            _fallbackFormats = new[]
            {
                "yyyy-MM-dd HH:mm:ss",
                "yyyy-MM-dd HH:mm:ss.fff",
                "dd/MM/yyyy HH:mm:ss",
                "MM/dd/yyyy HH:mm:ss",
                "yyyy-MM-ddTHH:mm:ss",
                "yyyy-MM-ddTHH:mm:ss.fff"
            };
        }

        /// <summary>
        /// Attempts to extract a timestamp from a log line.
        /// Returns null if no timestamp can be parsed.
        /// </summary>
        public DateTime? ExtractTimestamp(string logLine)
        {
            if (string.IsNullOrWhiteSpace(logLine))
                return null;

            try
            {
                var allFormats = new[] { _primaryFormat }.Concat(_fallbackFormats).Distinct();

                foreach (var format in allFormats)
                {
                    if (string.IsNullOrWhiteSpace(format))
                        continue;

                    // Extract substring that could match the pattern
                    int patternLength = format.Length;
                    if (logLine.Length >= patternLength)
                    {
                        string potentialTimestamp = logLine.Substring(0, Math.Min(patternLength + 5, logLine.Length));

                        // Try to extract timestamp using regex for common formats
                        var timestampMatch = Regex.Match(potentialTimestamp,
                            @"(\d{4}[-/]\d{2}[-/]\d{2}[T\s]\d{2}:\d{2}:\d{2}(?:\.\d{3})?)|(\d{2}[-/]\d{2}[-/]\d{4}\s\d{2}:\d{2}:\d{2})");

                        if (timestampMatch.Success)
                        {
                            // Try exact format first
                            if (DateTime.TryParseExact(timestampMatch.Value, format, CultureInfo.InvariantCulture,
                                DateTimeStyles.None, out DateTime result))
                            {
                                return result;
                            }

                            // Fall back to general parsing
                            if (DateTime.TryParse(timestampMatch.Value, out DateTime result2))
                            {
                                return result2;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Ignore parsing errors
            }

            return null;
        }

        /// <summary>
        /// Determines if a timestamp is recent based on a threshold time.
        /// </summary>
        public bool IsTimestampRecent(DateTime? timestamp, DateTime? thresholdTime)
        {
            if (!timestamp.HasValue)
                return true; // If no timestamp, assume recent

            if (!thresholdTime.HasValue)
                return false; // If no threshold, treat as old

            return timestamp.Value >= thresholdTime.Value;
        }
    }
}
