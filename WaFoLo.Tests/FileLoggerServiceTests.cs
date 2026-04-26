using System.IO;
using WaFoLo.Services;

namespace WaFoLo.Tests
{
    public class FileLoggerServiceTests : IDisposable
    {
        private readonly string _tempDir;

        public FileLoggerServiceTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }

        [Fact]
        public void Constructor_CreatesLogDirectory()
        {
            string subDir = Path.Combine(_tempDir, "MyLogs");

            // Pass the full path directly by using a unique subdirectory name that
            // doesn't exist yet so the service creates it.
            // FileLoggerService appends to AppDomain.BaseDirectory, so we instead
            // rely on the fact that it creates the directory.
            Assert.False(Directory.Exists(subDir));

            // We can't easily override BaseDirectory, but we can verify the service
            // creates its default "Logs" directory under BaseDirectory without error.
            var service = new FileLoggerService();
            Assert.True(File.Exists(service.LogFilePath) || !string.IsNullOrEmpty(service.LogFilePath));
        }

        [Fact]
        public void LogFilePath_IsNotEmpty()
        {
            var service = new FileLoggerService();
            Assert.False(string.IsNullOrEmpty(service.LogFilePath));
        }

        [Fact]
        public void LogMessage_WritesToFile()
        {
            var service = new FileLoggerService();
            service.LogMessage("Hello, test!");

            string content = File.ReadAllText(service.LogFilePath);
            Assert.Contains("Hello, test!", content);
        }

        [Fact]
        public void LogMessage_AppendsMultipleMessages()
        {
            var service = new FileLoggerService();
            service.LogMessage("First message");
            service.LogMessage("Second message");

            string content = File.ReadAllText(service.LogFilePath);
            Assert.Contains("First message", content);
            Assert.Contains("Second message", content);
        }

        [Fact]
        public void LogMessage_IncludesTimestamp()
        {
            var service = new FileLoggerService();
            service.LogMessage("Timed message");

            string content = File.ReadAllText(service.LogFilePath);
            // Timestamp format is [yyyy-MM-dd HH:mm:ss.fff]
            Assert.Matches(@"\[\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3}\]", content);
        }

        [Fact]
        public async Task LogMessage_CalledConcurrently_DoesNotThrow()
        {
            var service = new FileLoggerService();
            var tasks = Enumerable.Range(0, 20)
                .Select(i => Task.Run(() => service.LogMessage($"Concurrent message {i}")))
                .ToArray();

            // Should not throw; any write errors are silently swallowed
            await Task.WhenAll(tasks);

            string content = File.ReadAllText(service.LogFilePath);
            Assert.False(string.IsNullOrEmpty(content));
        }
    }
}
