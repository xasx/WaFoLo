using WaFoLo.Utilities;

namespace WaFoLo.Tests
{
    public class TimestampParserTests
    {
        // ── ExtractTimestamp ──────────────────────────────────────────────────

        [Fact]
        public void ExtractTimestamp_NullInput_ReturnsNull()
        {
            var parser = new TimestampParser("yyyy-MM-dd HH:mm:ss");
            Assert.Null(parser.ExtractTimestamp(null!));
        }

        [Fact]
        public void ExtractTimestamp_EmptyInput_ReturnsNull()
        {
            var parser = new TimestampParser("yyyy-MM-dd HH:mm:ss");
            Assert.Null(parser.ExtractTimestamp(string.Empty));
        }

        [Fact]
        public void ExtractTimestamp_WhitespaceOnly_ReturnsNull()
        {
            var parser = new TimestampParser("yyyy-MM-dd HH:mm:ss");
            Assert.Null(parser.ExtractTimestamp("   "));
        }

        [Fact]
        public void ExtractTimestamp_NoTimestampInLine_ReturnsNull()
        {
            var parser = new TimestampParser("yyyy-MM-dd HH:mm:ss");
            Assert.Null(parser.ExtractTimestamp("This line has no timestamp at all."));
        }

        [Fact]
        public void ExtractTimestamp_ValidIsoTimestampAtStart_ReturnsParsedDateTime()
        {
            var parser = new TimestampParser("yyyy-MM-dd HH:mm:ss");
            DateTime? result = parser.ExtractTimestamp("2024-03-15 10:30:45 Some log message");

            Assert.NotNull(result);
            Assert.Equal(2024, result!.Value.Year);
            Assert.Equal(3, result.Value.Month);
            Assert.Equal(15, result.Value.Day);
            Assert.Equal(10, result.Value.Hour);
            Assert.Equal(30, result.Value.Minute);
            Assert.Equal(45, result.Value.Second);
        }

        [Fact]
        public void ExtractTimestamp_ValidIsoTimestampWithMilliseconds_ReturnsParsedDateTime()
        {
            var parser = new TimestampParser("yyyy-MM-dd HH:mm:ss.fff");
            DateTime? result = parser.ExtractTimestamp("2024-03-15 10:30:45.123 Some log message");

            Assert.NotNull(result);
            Assert.Equal(2024, result!.Value.Year);
            Assert.Equal(10, result.Value.Hour);
        }

        [Fact]
        public void ExtractTimestamp_FallbackFormatMatches_ReturnsParsedDateTime()
        {
            // Primary format is different, but fallback "yyyy-MM-dd HH:mm:ss" should match
            var parser = new TimestampParser("dd/MM/yyyy HH:mm:ss");
            DateTime? result = parser.ExtractTimestamp("2024-01-20 08:00:00 App started");

            Assert.NotNull(result);
            Assert.Equal(2024, result!.Value.Year);
            Assert.Equal(1, result.Value.Month);
            Assert.Equal(20, result.Value.Day);
        }

        [Fact]
        public void ExtractTimestamp_IsoTSeparatorFormat_ReturnsParsedDateTime()
        {
            var parser = new TimestampParser("yyyy-MM-ddTHH:mm:ss");
            DateTime? result = parser.ExtractTimestamp("2024-06-01T12:00:00 message");

            Assert.NotNull(result);
            Assert.Equal(2024, result!.Value.Year);
            Assert.Equal(6, result.Value.Month);
            Assert.Equal(12, result.Value.Hour);
        }

        // ── IsTimestampRecent ─────────────────────────────────────────────────

        [Fact]
        public void IsTimestampRecent_NullTimestamp_ReturnsTrue()
        {
            var parser = new TimestampParser("yyyy-MM-dd HH:mm:ss");
            Assert.True(parser.IsTimestampRecent(null, DateTime.Now));
        }

        [Fact]
        public void IsTimestampRecent_NullThreshold_ReturnsFalse()
        {
            var parser = new TimestampParser("yyyy-MM-dd HH:mm:ss");
            Assert.False(parser.IsTimestampRecent(DateTime.Now, null));
        }

        [Fact]
        public void IsTimestampRecent_TimestampAfterThreshold_ReturnsTrue()
        {
            var parser = new TimestampParser("yyyy-MM-dd HH:mm:ss");
            var threshold = new DateTime(2024, 1, 1, 0, 0, 0);
            var timestamp = new DateTime(2024, 1, 2, 0, 0, 0);

            Assert.True(parser.IsTimestampRecent(timestamp, threshold));
        }

        [Fact]
        public void IsTimestampRecent_TimestampBeforeThreshold_ReturnsFalse()
        {
            var parser = new TimestampParser("yyyy-MM-dd HH:mm:ss");
            var threshold = new DateTime(2024, 1, 2, 0, 0, 0);
            var timestamp = new DateTime(2024, 1, 1, 0, 0, 0);

            Assert.False(parser.IsTimestampRecent(timestamp, threshold));
        }

        [Fact]
        public void IsTimestampRecent_TimestampEqualsThreshold_ReturnsTrue()
        {
            var parser = new TimestampParser("yyyy-MM-dd HH:mm:ss");
            var time = new DateTime(2024, 5, 10, 12, 0, 0);

            Assert.True(parser.IsTimestampRecent(time, time));
        }
    }
}
