namespace WaFoLo.Utilities
{
    /// <summary>
    /// Factory for creating ITimestampParser instances.
    /// </summary>
    public interface ITimestampParserFactory
    {
        ITimestampParser Create(string timestampFormat);
    }

    /// <summary>
    /// Default implementation of ITimestampParserFactory.
    /// </summary>
    public class TimestampParserFactory : ITimestampParserFactory
    {
        public ITimestampParser Create(string timestampFormat)
        {
            return new TimestampParser(timestampFormat);
        }
    }
}
