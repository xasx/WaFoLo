namespace WaFoLo.Utilities
{
    /// <summary>
    /// Interface for parsing timestamps from log lines.
    /// </summary>
    public interface ITimestampParser
    {
        DateTime? ExtractTimestamp(string logLine);
        bool IsTimestampRecent(DateTime? timestamp, DateTime? thresholdTime);
    }
}
