using System.IO;
using Moq;
using WaFoLo.Models;
using WaFoLo.Services;
using WaFoLo.Utilities;

namespace WaFoLo.Tests
{
    public class WatchdogOrchestratorTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly Mock<ILogMonitorServiceFactory> _mockFactory;
        private readonly Mock<ILogMonitorService> _mockMonitor;
        private readonly Mock<ITimestampParserFactory> _mockParserFactory;
        private readonly Mock<ITimestampParser> _mockParser;
        private readonly WatchdogOrchestrator _orchestrator;

        public WatchdogOrchestratorTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDir);

            _mockMonitor = new Mock<ILogMonitorService>();
            _mockFactory = new Mock<ILogMonitorServiceFactory>();
            _mockFactory.Setup(f => f.Create()).Returns(_mockMonitor.Object);

            _mockParser = new Mock<ITimestampParser>();
            _mockParserFactory = new Mock<ITimestampParserFactory>();
            _mockParserFactory.Setup(f => f.Create(It.IsAny<string>())).Returns(_mockParser.Object);

            _orchestrator = new WatchdogOrchestrator(_mockFactory.Object, _mockParserFactory.Object);
        }

        public void Dispose()
        {
            _orchestrator.Dispose();
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }

        // ── Constructor ───────────────────────────────────────────────────────

        [Fact]
        public void Constructor_NullFactory_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new WatchdogOrchestrator(null!, _mockParserFactory.Object));
        }

        [Fact]
        public void Constructor_NullParserFactory_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new WatchdogOrchestrator(_mockFactory.Object, null!));
        }

        // ── InitializeTimestampParser ─────────────────────────────────────────

        [Fact]
        public void InitializeTimestampParser_CallsFactory()
        {
            _orchestrator.InitializeTimestampParser("yyyy-MM-dd HH:mm:ss");

            _mockParserFactory.Verify(f => f.Create("yyyy-MM-dd HH:mm:ss"), Times.Once);
        }

        // ── ExtractTimestamp ──────────────────────────────────────────────────

        [Fact]
        public void ExtractTimestamp_BeforeInit_ReturnsNull()
        {
            // No parser initialised
            var result = _orchestrator.ExtractTimestamp("2024-01-01 00:00:00 line");
            Assert.Null(result);
        }

        [Fact]
        public void ExtractTimestamp_AfterInit_DelegatesToParser()
        {
            var expected = new DateTime(2024, 6, 1, 0, 0, 0);
            _mockParser.Setup(p => p.ExtractTimestamp(It.IsAny<string>())).Returns(expected);

            _orchestrator.InitializeTimestampParser("yyyy-MM-dd HH:mm:ss");
            var result = _orchestrator.ExtractTimestamp("2024-06-01 00:00:00 line");

            Assert.Equal(expected, result);
        }

        // ── StartMonitoring: file does not exist ──────────────────────────────

        [Fact]
        public void StartMonitoring_FileDoesNotExist_RaisesWaitingForLogFileStatus()
        {
            string nonExistentPath = Path.Combine(_tempDir, "missing.log");
            MonitoringStatusChangedEventArgs? raisedStatus = null;
            _orchestrator.StatusChanged += (_, e) => raisedStatus = e;

            _orchestrator.StartMonitoring(nonExistentPath);

            Assert.NotNull(raisedStatus);
            Assert.Equal(MonitoringStatus.WaitingForLogFile, raisedStatus!.Status);
        }

        // ── StartMonitoring: file exists ──────────────────────────────────────

        [Fact]
        public void StartMonitoring_FileExists_RaisesActiveStatus()
        {
            string logFile = Path.Combine(_tempDir, "app.log");
            File.WriteAllText(logFile, "content");

            MonitoringStatusChangedEventArgs? raisedStatus = null;
            _orchestrator.StatusChanged += (_, e) => raisedStatus = e;

            _orchestrator.StartMonitoring(logFile);

            Assert.NotNull(raisedStatus);
            Assert.Equal(MonitoringStatus.Active, raisedStatus!.Status);
        }

        [Fact]
        public void StartMonitoring_CallsMonitorStartMonitoring()
        {
            string logFile = Path.Combine(_tempDir, "app.log");
            File.WriteAllText(logFile, "content");

            _orchestrator.StartMonitoring(logFile);

            _mockMonitor.Verify(m => m.StartMonitoring(logFile), Times.Once);
        }

        // ── ReadAllLines ──────────────────────────────────────────────────────

        [Fact]
        public void ReadAllLines_BeforeStart_ReturnsEmptyList()
        {
            var lines = _orchestrator.ReadAllLines();
            Assert.Empty(lines);
        }

        [Fact]
        public void ReadAllLines_AfterStart_DelegatesToMonitor()
        {
            string logFile = Path.Combine(_tempDir, "app.log");
            File.WriteAllText(logFile, "content");

            var expectedLines = new List<LogLineInfo>
            {
                new LogLineInfo { Content = "Line 1", LineNumber = 1 }
            };
            _mockMonitor.Setup(m => m.ReadAllLines()).Returns(expectedLines);

            _orchestrator.StartMonitoring(logFile);
            var lines = _orchestrator.ReadAllLines();

            Assert.Equal(expectedLines, lines);
        }

        // ── Dispose ───────────────────────────────────────────────────────────

        [Fact]
        public void Dispose_DisposesUnderlyingMonitor()
        {
            string logFile = Path.Combine(_tempDir, "app.log");
            File.WriteAllText(logFile, "content");

            _orchestrator.StartMonitoring(logFile);
            _orchestrator.Dispose();

            _mockMonitor.Verify(m => m.Dispose(), Times.AtLeastOnce);
        }
    }
}
