using Moq;
using WaFoLo.Models;
using WaFoLo.Services;
using WaFoLo.Utilities;

namespace WaFoLo.Tests
{
    public class LogScannerTests
    {
        private readonly Mock<ITimestampParser> _mockParser;
        private readonly LogScanner _scanner;

        public LogScannerTests()
        {
            _mockParser = new Mock<ITimestampParser>();
            _scanner = new LogScanner(_mockParser.Object);
        }

        // ── Constructor ───────────────────────────────────────────────────────

        [Fact]
        public void Constructor_NullParser_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new LogScanner(null!));
        }

        // ── ScanExistingLines: No trigger found ───────────────────────────────

        [Fact]
        public void ScanExistingLines_EmptyLines_ReturnsNoTriggerFound()
        {
            _mockParser.Setup(p => p.ExtractTimestamp(It.IsAny<string>()))
                       .Returns((DateTime?)null);

            var result = _scanner.ScanExistingLines(
                new List<LogLineInfo>(),
                DateTime.MinValue,
                "TRIGGER",
                "EXPECTED",
                "test");

            Assert.Equal(ScanStatus.NoTriggerFound, result.Status);
            Assert.Null(result.LastTriggerLine);
        }

        [Fact]
        public void ScanExistingLines_NoTriggerPattern_ReturnsNoTriggerFound()
        {
            var sessionStart = new DateTime(2024, 1, 1, 12, 0, 0);
            var lineTime = new DateTime(2024, 1, 1, 13, 0, 0);

            _mockParser.Setup(p => p.ExtractTimestamp(It.IsAny<string>()))
                       .Returns(lineTime);

            var lines = new List<LogLineInfo>
            {
                new LogLineInfo { Content = "App started", LineNumber = 1 },
                new LogLineInfo { Content = "Some operation", LineNumber = 2 }
            };

            var result = _scanner.ScanExistingLines(lines, sessionStart, "TRIGGER", "EXPECTED", "test");

            Assert.Equal(ScanStatus.NoTriggerFound, result.Status);
        }

        // ── ScanExistingLines: Lines filtered by threshold ────────────────────

        [Fact]
        public void ScanExistingLines_AllLinesBeforeThreshold_ReturnsNoTriggerFound()
        {
            // Lines have timestamps, but all are before the session threshold
            var sessionStart = new DateTime(2024, 1, 2, 0, 0, 0);
            var oldTime = new DateTime(2024, 1, 1, 0, 0, 0);

            _mockParser.Setup(p => p.ExtractTimestamp(It.IsAny<string>()))
                       .Returns(oldTime);

            var lines = new List<LogLineInfo>
            {
                new LogLineInfo { Content = "2024-01-01 00:00:00 TRIGGER event", LineNumber = 1 },
                new LogLineInfo { Content = "2024-01-01 00:00:01 EXPECTED result", LineNumber = 2 }
            };

            var result = _scanner.ScanExistingLines(lines, sessionStart, "TRIGGER", "EXPECTED", "test");

            Assert.Equal(ScanStatus.NoTriggerFound, result.Status);
        }

        // ── ScanExistingLines: Sequence completed ─────────────────────────────

        [Fact]
        public void ScanExistingLines_TriggerFollowedByExpected_ReturnsSequenceCompleted()
        {
            var sessionStart = new DateTime(2024, 1, 1, 0, 0, 0);
            var lineTime = new DateTime(2024, 1, 1, 12, 0, 0);

            _mockParser.Setup(p => p.ExtractTimestamp(It.IsAny<string>()))
                       .Returns(lineTime);

            var lines = new List<LogLineInfo>
            {
                new LogLineInfo { Content = "App starting...", LineNumber = 1 },
                new LogLineInfo { Content = "Starting critical operation", LineNumber = 2 },
                new LogLineInfo { Content = "Operation completed successfully", LineNumber = 3 }
            };

            var result = _scanner.ScanExistingLines(
                lines, sessionStart,
                "Starting critical operation",
                "Operation completed successfully",
                "test");

            Assert.Equal(ScanStatus.SequenceCompleted, result.Status);
            Assert.NotNull(result.LastTriggerLine);
            Assert.Equal(2, result.LastTriggerLine!.LineNumber);
        }

        // ── ScanExistingLines: Incomplete sequence ────────────────────────────

        [Fact]
        public void ScanExistingLines_TriggerWithoutExpected_ReturnsIncompleteSequence()
        {
            var sessionStart = new DateTime(2024, 1, 1, 0, 0, 0);
            var lineTime = new DateTime(2024, 1, 1, 12, 0, 0);

            _mockParser.Setup(p => p.ExtractTimestamp(It.IsAny<string>()))
                       .Returns(lineTime);

            var lines = new List<LogLineInfo>
            {
                new LogLineInfo { Content = "App starting...", LineNumber = 1 },
                new LogLineInfo { Content = "Starting critical operation", LineNumber = 2 }
            };

            var result = _scanner.ScanExistingLines(
                lines, sessionStart,
                "Starting critical operation",
                "Operation completed successfully",
                "test");

            Assert.Equal(ScanStatus.IncompleteSequence, result.Status);
            Assert.NotNull(result.LastTriggerLine);
            Assert.Equal(2, result.LastTriggerLine!.LineNumber);
        }

        [Fact]
        public void ScanExistingLines_ExpectedBeforeTrigger_ReturnsIncompleteSequence()
        {
            // Expected line appears before the trigger → should not count
            var sessionStart = new DateTime(2024, 1, 1, 0, 0, 0);
            var lineTime = new DateTime(2024, 1, 1, 12, 0, 0);

            _mockParser.Setup(p => p.ExtractTimestamp(It.IsAny<string>()))
                       .Returns(lineTime);

            var lines = new List<LogLineInfo>
            {
                new LogLineInfo { Content = "Operation completed successfully", LineNumber = 1 },
                new LogLineInfo { Content = "Starting critical operation",     LineNumber = 2 }
            };

            var result = _scanner.ScanExistingLines(
                lines, sessionStart,
                "Starting critical operation",
                "Operation completed successfully",
                "test");

            Assert.Equal(ScanStatus.IncompleteSequence, result.Status);
        }

        // ── LogActivity event ─────────────────────────────────────────────────

        [Fact]
        public void ScanExistingLines_FiresLogActivityEvents()
        {
            var sessionStart = new DateTime(2024, 1, 1, 0, 0, 0);
            var lineTime = new DateTime(2024, 1, 1, 12, 0, 0);

            _mockParser.Setup(p => p.ExtractTimestamp(It.IsAny<string>()))
                       .Returns(lineTime);

            var logMessages = new List<string>();
            _scanner.LogActivity += (_, msg) => logMessages.Add(msg);

            _scanner.ScanExistingLines(
                new List<LogLineInfo>(), sessionStart, "TRIGGER", "EXPECTED", "source");

            Assert.NotEmpty(logMessages);
        }
    }
}
