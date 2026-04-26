using WaFoLo.Services;
using WaFoLo.Tests.TestDoubles;

namespace WaFoLo.Tests
{
    public class TimeoutManagerTests
    {
        private static (TimeoutManager manager, FakeTimerFactory factory) Create()
        {
            var factory = new FakeTimerFactory();
            return (new TimeoutManager(factory), factory);
        }

        [Fact]
        public void Constructor_NullFactory_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new TimeoutManager(null!));
        }

        [Fact]
        public void Constructor_CreatesTwoTimers()
        {
            var (_, factory) = Create();

            Assert.Equal(2, factory.CreatedTimers.Count);
        }

        [Fact]
        public void IsActive_InitiallyFalse()
        {
            var (manager, _) = Create();
            Assert.False(manager.IsActive);
        }

        [Fact]
        public void TriggerTime_InitiallyNull()
        {
            var (manager, _) = Create();
            Assert.Null(manager.TriggerTime);
        }

        [Fact]
        public void StartTimeout_SetsIsActiveToTrue()
        {
            var (manager, _) = Create();

            manager.StartTimeout(DateTime.Now.AddSeconds(-1), 60);

            Assert.True(manager.IsActive);
        }

        [Fact]
        public void StartTimeout_FutureExpiry_StartsBothTimers()
        {
            var (manager, factory) = Create();

            manager.StartTimeout(DateTime.Now, 60);

            Assert.True(factory.CreatedTimers[0].IsEnabled);
            Assert.True(factory.CreatedTimers[1].IsEnabled);
        }

        [Fact]
        public void StartTimeout_AlreadyElapsed_RaisesTimeoutOccurred()
        {
            var (manager, _) = Create();
            bool raised = false;
            manager.TimeoutOccurred += (_, _) => raised = true;

            manager.StartTimeout(DateTime.Now.AddSeconds(-100), 10);

            Assert.True(raised);
        }

        [Fact]
        public void TimeoutTimer_Tick_WhenElapsed_RaisesTimeoutOccurredAndStopsTimers()
        {
            var (manager, factory) = Create();
            bool raised = false;
            manager.TimeoutOccurred += (_, _) => raised = true;

            // Set a trigger in the far past so the tick will exceed the timeout
            manager.StartTimeout(DateTime.Now.AddSeconds(-1000), 1);
            factory.CreatedTimers[0].Fire(); // fire the timeout timer

            Assert.True(raised);
            Assert.False(factory.CreatedTimers[0].IsEnabled);
            Assert.False(factory.CreatedTimers[1].IsEnabled);
        }

        [Fact]
        public void ProgressTimer_Tick_RaisesProgressUpdated()
        {
            var (manager, factory) = Create();
            TimeoutProgressEventArgs? args = null;
            manager.ProgressUpdated += (_, e) => args = e;

            manager.StartTimeout(DateTime.Now.AddSeconds(-5), 60);
            factory.CreatedTimers[1].Fire(); // fire the progress timer

            Assert.NotNull(args);
            Assert.InRange(args!.Percentage, 0, 100);
            Assert.True(args.RemainingSeconds >= 0);
        }

        [Fact]
        public void Reset_SetsIsActiveToFalse()
        {
            var (manager, _) = Create();

            manager.StartTimeout(DateTime.Now, 60);
            manager.Reset();

            Assert.False(manager.IsActive);
        }

        [Fact]
        public void Reset_StopsTimers()
        {
            var (manager, factory) = Create();

            manager.StartTimeout(DateTime.Now, 60);
            manager.Reset();

            Assert.False(factory.CreatedTimers[0].IsEnabled);
            Assert.False(factory.CreatedTimers[1].IsEnabled);
        }

        [Fact]
        public void Reset_RaisesTriggerReset()
        {
            var (manager, _) = Create();
            bool raised = false;
            manager.TriggerReset += (_, _) => raised = true;

            manager.StartTimeout(DateTime.Now, 60);
            manager.Reset();

            Assert.True(raised);
        }

        [Fact]
        public void Dispose_StopsTimers()
        {
            var (manager, factory) = Create();

            manager.StartTimeout(DateTime.Now, 60);
            manager.Dispose();

            Assert.False(factory.CreatedTimers[0].IsEnabled);
            Assert.False(factory.CreatedTimers[1].IsEnabled);
        }
    }
}
