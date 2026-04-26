using System.IO;
using WaFoLo.Models;
using WaFoLo.Services;

namespace WaFoLo.Tests
{
    public class LogMonitorServiceTests : IDisposable
    {
        private readonly string _tempDir;

        public LogMonitorServiceTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }

        // ── ReadAllLines ──────────────────────────────────────────────────────

        [Fact]
        public void ReadAllLines_BeforeStartMonitoring_ReturnsEmptyList()
        {
            using var service = new LogMonitorService();
            var lines = service.ReadAllLines();

            Assert.Empty(lines);
        }

        [Fact]
        public void ReadAllLines_AfterStartMonitoringOnExistingFile_ReturnsAllLines()
        {
            string logFile = Path.Combine(_tempDir, "app.log");
            File.WriteAllLines(logFile, new[] { "Line one", "Line two", "Line three" });

            using var service = new LogMonitorService();
            service.StartMonitoring(logFile);

            var lines = service.ReadAllLines();

            Assert.Equal(3, lines.Count);
            Assert.Equal("Line one", lines[0].Content);
            Assert.Equal("Line two", lines[1].Content);
            Assert.Equal("Line three", lines[2].Content);
        }

        [Fact]
        public void ReadAllLines_AssignsSequentialLineNumbers()
        {
            string logFile = Path.Combine(_tempDir, "app.log");
            File.WriteAllLines(logFile, new[] { "A", "B", "C" });

            using var service = new LogMonitorService();
            service.StartMonitoring(logFile);

            var lines = service.ReadAllLines();

            Assert.Equal(1, lines[0].LineNumber);
            Assert.Equal(2, lines[1].LineNumber);
            Assert.Equal(3, lines[2].LineNumber);
        }

        [Fact]
        public void ReadAllLines_EmptyFile_ReturnsEmptyList()
        {
            string logFile = Path.Combine(_tempDir, "empty.log");
            File.WriteAllText(logFile, string.Empty);

            using var service = new LogMonitorService();
            service.StartMonitoring(logFile);

            var lines = service.ReadAllLines();
            Assert.Empty(lines);
        }

        // ── ReadExistingLines ─────────────────────────────────────────────────

        [Fact]
        public void ReadExistingLines_BeforeStartMonitoring_ReturnsEmptyList()
        {
            using var service = new LogMonitorService();
            Assert.Empty(service.ReadExistingLines(10));
        }

        [Fact]
        public void ReadExistingLines_MaxLinesLessThanTotal_ReturnsLastNLines()
        {
            string logFile = Path.Combine(_tempDir, "app.log");
            var allLines = Enumerable.Range(1, 10).Select(i => $"Line {i}").ToArray();
            File.WriteAllLines(logFile, allLines);

            using var service = new LogMonitorService();
            service.StartMonitoring(logFile);

            var result = service.ReadExistingLines(3);

            Assert.Equal(3, result.Count);
            Assert.Equal("Line 8", result[0].Content);
            Assert.Equal("Line 9", result[1].Content);
            Assert.Equal("Line 10", result[2].Content);
        }

        [Fact]
        public void ReadExistingLines_MaxLinesGreaterThanTotal_ReturnsAllLines()
        {
            string logFile = Path.Combine(_tempDir, "app.log");
            File.WriteAllLines(logFile, new[] { "Alpha", "Beta" });

            using var service = new LogMonitorService();
            service.StartMonitoring(logFile);

            var result = service.ReadExistingLines(100);

            Assert.Equal(2, result.Count);
        }

        // ── IsMonitoring ──────────────────────────────────────────────────────

        [Fact]
        public void IsMonitoring_BeforeStart_ReturnsFalse()
        {
            using var service = new LogMonitorService();
            Assert.False(service.IsMonitoring);
        }

        [Fact]
        public void IsMonitoring_AfterStartMonitoringExistingFile_ReturnsTrue()
        {
            string logFile = Path.Combine(_tempDir, "app.log");
            File.WriteAllText(logFile, "content");

            using var service = new LogMonitorService();
            service.StartMonitoring(logFile);

            Assert.True(service.IsMonitoring);
        }

        [Fact]
        public void IsMonitoring_AfterStartMonitoringNonExistentFile_ReturnsTrue()
        {
            // Monitoring begins even before the file exists (polling waits for it)
            string logFile = Path.Combine(_tempDir, "future.log");

            using var service = new LogMonitorService();
            service.StartMonitoring(logFile);

            Assert.True(service.IsMonitoring);
        }

        // ── StartMonitoring: non-existent file ────────────────────────────────

        [Fact]
        public void StartMonitoring_NonExistentFile_DoesNotThrow()
        {
            string logFile = Path.Combine(_tempDir, "future.log");

            using var service = new LogMonitorService();
            var exception = Record.Exception(() => service.StartMonitoring(logFile));

            Assert.Null(exception);
        }

        [Fact]
        public void StartMonitoring_NonExistentFile_CreatesDirectoryIfNeeded()
        {
            string subDir = Path.Combine(_tempDir, "subdir");
            string logFile = Path.Combine(subDir, "app.log");

            using var service = new LogMonitorService();
            service.StartMonitoring(logFile);

            Assert.True(Directory.Exists(subDir));
        }

        // ── DiagnosticLog event ───────────────────────────────────────────────

        [Fact]
        public void StartMonitoring_ExistingFile_FiresDiagnosticLogEvents()
        {
            string logFile = Path.Combine(_tempDir, "app.log");
            File.WriteAllText(logFile, "some content");

            using var service = new LogMonitorService();
            var messages = new List<string>();
            service.DiagnosticLog += (_, msg) => messages.Add(msg);

            service.StartMonitoring(logFile);

            Assert.NotEmpty(messages);
        }

        // ── Dispose ───────────────────────────────────────────────────────────

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes_DoesNotThrow()
        {
            var service = new LogMonitorService();
            service.Dispose();
            var exception = Record.Exception(() => service.Dispose());

            Assert.Null(exception);
        }
    }
}
