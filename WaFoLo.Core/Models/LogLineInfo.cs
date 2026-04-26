namespace WaFoLo.Models
{
    /// <summary>
    /// Represents a single log line with metadata.
    /// </summary>
    public class LogLineInfo
    {
        public string Content { get; set; } = string.Empty;
        public DateTime? Timestamp { get; set; }
        public int LineNumber { get; set; }
    }
}
