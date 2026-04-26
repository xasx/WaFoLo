using Moq;
using WaFoLo.Services;
using WaFoLo.Tests.TestDoubles;

namespace WaFoLo.Tests
{
    public class ProcessMonitorTests
    {
        private static readonly DateTime AppStart = new DateTime(2024, 1, 1, 10, 0, 0);

        private static (ProcessMonitor monitor, Mock<IProcessDetectionService> detection, FakeTimerFactory factory)
            Create()
        {
            var detection = new Mock<IProcessDetectionService>();
            var factory = new FakeTimerFactory();
            return (new ProcessMonitor(detection.Object, AppStart, factory), detection, factory);
        }

        [Fact]
        public void Constructor_NullDetection_ThrowsArgumentNullException()
        {
            var factory = new FakeTimerFactory();
            Assert.Throws<ArgumentNullException>(() =>
                new ProcessMonitor(null!, AppStart, factory));
        }

        [Fact]
        public void Constructor_NullTimerFactory_ThrowsArgumentNullException()
        {
            var detection = new Mock<IProcessDetectionService>();
            Assert.Throws<ArgumentNullException>(() =>
                new ProcessMonitor(detection.Object, AppStart, null!));
        }

        [Fact]
        public void DetectProcessStartTime_EmptyProcessName_ReturnsApplicationStartTime()
        {
            var (monitor, _, _) = Create();

            var result = monitor.DetectProcessStartTime(string.Empty);

            Assert.Equal(AppStart, result);
        }

        [Fact]
        public void DetectProcessStartTime_ProcessNotRunning_ReturnsApplicationStartTime()
        {
            var (monitor, detection, _) = Create();
            detection.Setup(d => d.DetectProcessStartTime("myapp")).Returns((DateTime?)null);

            var result = monitor.DetectProcessStartTime("myapp");

            Assert.Equal(AppStart, result);
        }

        [Fact]
        public void DetectProcessStartTime_ProcessRunning_ReturnsProcessStartTime()
        {
            var (monitor, detection, _) = Create();
            var processStart = new DateTime(2024, 1, 1, 9, 30, 0);
            detection.Setup(d => d.DetectProcessStartTime("myapp")).Returns(processStart);
            detection.Setup(d => d.GetProcessInstanceCount("myapp")).Returns(1);

            var result = monitor.DetectProcessStartTime("myapp");

            Assert.Equal(processStart, result);
            Assert.Equal(processStart, monitor.MonitoredProcessStartTime);
        }

        [Fact]
        public void DetectProcessStartTime_DetectionThrows_ReturnsApplicationStartTime()
        {
            var (monitor, detection, _) = Create();
            detection.Setup(d => d.DetectProcessStartTime(It.IsAny<string>()))
                     .Throws(new InvalidOperationException("access denied"));

            var result = monitor.DetectProcessStartTime("myapp");

            Assert.Equal(AppStart, result);
        }

        [Fact]
        public void IsProcessRunning_DelegatesToDetectionService()
        {
            var (monitor, detection, _) = Create();
            detection.Setup(d => d.IsProcessRunning("myapp")).Returns(true);

            Assert.True(monitor.IsProcessRunning("myapp"));
        }

        [Fact]
        public void StartWaitingForProcess_StartsTimer()
        {
            var (monitor, _, factory) = Create();

            monitor.StartWaitingForProcess("myapp", 5);

            Assert.Single(factory.CreatedTimers);
            Assert.True(factory.Last!.IsEnabled);
            Assert.Equal(TimeSpan.FromSeconds(5), factory.Last.Interval);
        }

        [Fact]
        public void StartWaitingForProcess_RaisesStatusChangedWithWaitingForProcess()
        {
            var (monitor, _, _) = Create();
            MonitoringStatusChangedEventArgs? raisedArgs = null;
            monitor.StatusChanged += (_, e) => raisedArgs = e;

            monitor.StartWaitingForProcess("myapp", 5);

            Assert.NotNull(raisedArgs);
            Assert.Equal(MonitoringStatus.WaitingForProcess, raisedArgs!.Status);
        }

        [Fact]
        public void TimerTick_WhenProcessDetected_StopsTimerAndRaisesProcessDetected()
        {
            var (monitor, detection, factory) = Create();
            detection.Setup(d => d.IsProcessRunning("myapp")).Returns(true);

            ProcessDetectedEventArgs? processArgs = null;
            monitor.ProcessDetected += (_, e) => processArgs = e;

            monitor.StartWaitingForProcess("myapp", 5);
            factory.Last!.Fire();

            Assert.NotNull(processArgs);
            Assert.Equal("myapp", processArgs!.ProcessName);
            Assert.False(factory.Last.IsEnabled);
        }

        [Fact]
        public void TimerTick_WhenProcessNotYetRunning_KeepsTimerRunning()
        {
            var (monitor, detection, factory) = Create();
            detection.Setup(d => d.IsProcessRunning("myapp")).Returns(false);

            bool processDetected = false;
            monitor.ProcessDetected += (_, _) => processDetected = true;

            monitor.StartWaitingForProcess("myapp", 5);
            factory.Last!.Fire();

            Assert.False(processDetected);
            Assert.True(factory.Last.IsEnabled);
        }

        [Fact]
        public void Dispose_StopsTimer()
        {
            var (monitor, _, factory) = Create();

            monitor.StartWaitingForProcess("myapp", 5);
            monitor.Dispose();

            Assert.False(factory.Last!.IsEnabled);
        }
    }
}
