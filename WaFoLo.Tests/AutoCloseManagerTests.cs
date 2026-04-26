using WaFoLo.Services;
using WaFoLo.Tests.TestDoubles;

namespace WaFoLo.Tests
{
    public class AutoCloseManagerTests
    {
        private static (AutoCloseManager manager, FakeTimerFactory factory) Create()
        {
            var factory = new FakeTimerFactory();
            return (new AutoCloseManager(factory), factory);
        }

        [Fact]
        public void Constructor_NullFactory_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new AutoCloseManager(null!));
        }

        [Fact]
        public void IsActive_BeforeStart_ReturnsFalse()
        {
            var (manager, _) = Create();
            Assert.False(manager.IsActive);
        }

        [Fact]
        public void StartCountdown_CreatesAndStartsTimer()
        {
            var (manager, factory) = Create();

            manager.StartCountdown(5);

            Assert.Single(factory.CreatedTimers);
            Assert.True(factory.Last!.IsEnabled);
        }

        [Fact]
        public void StartCountdown_TimerIntervalIsOneSecond()
        {
            var (manager, factory) = Create();

            manager.StartCountdown(5);

            Assert.Equal(TimeSpan.FromSeconds(1), factory.Last!.Interval);
        }

        [Fact]
        public void StartCountdown_RaisesLogActivityEvent()
        {
            var (manager, _) = Create();
            string? logMessage = null;
            manager.LogActivity += (_, msg) => logMessage = msg;

            manager.StartCountdown(3);

            Assert.NotNull(logMessage);
            Assert.Contains("3", logMessage);
        }

        [Fact]
        public void StartCountdown_RaisesCountdownUpdatedWithInitialValue()
        {
            var (manager, _) = Create();
            int? receivedCountdown = null;
            manager.CountdownUpdated += (_, countdown) => receivedCountdown = countdown;

            manager.StartCountdown(5);

            Assert.Equal(5, receivedCountdown);
        }

        [Fact]
        public void IsActive_AfterStartCountdown_ReturnsTrue()
        {
            var (manager, _) = Create();

            manager.StartCountdown(5);

            Assert.True(manager.IsActive);
        }

        [Fact]
        public void Tick_DecrementsCountdownAndRaisesCountdownUpdated()
        {
            var (manager, factory) = Create();
            var updates = new List<int>();
            manager.CountdownUpdated += (_, c) => updates.Add(c);

            manager.StartCountdown(3);
            updates.Clear(); // ignore the initial CountdownUpdated from StartCountdown

            factory.Last!.Fire();

            Assert.Single(updates);
            Assert.Equal(2, updates[0]);
        }

        [Fact]
        public void Tick_WhenCountdownReachesZero_RaisesApplicationClosing()
        {
            var (manager, factory) = Create();
            bool closingRaised = false;
            manager.ApplicationClosing += (_, _) => closingRaised = true;

            manager.StartCountdown(1);
            factory.Last!.Fire(); // countdown hits 0

            Assert.True(closingRaised);
        }

        [Fact]
        public void Tick_WhenCountdownReachesZero_StopsTimer()
        {
            var (manager, factory) = Create();

            manager.StartCountdown(1);
            factory.Last!.Fire();

            Assert.False(factory.Last.IsEnabled);
        }

        [Fact]
        public void Stop_StopsActiveTimer()
        {
            var (manager, factory) = Create();

            manager.StartCountdown(5);
            manager.Stop();

            Assert.False(factory.Last!.IsEnabled);
        }

        [Fact]
        public void Dispose_StopsTimer()
        {
            var (manager, factory) = Create();

            manager.StartCountdown(5);
            manager.Dispose();

            Assert.False(factory.Last!.IsEnabled);
        }

        [Fact]
        public void MultipleCountdownTicks_RaisesCorrectSequenceOfCountdownValues()
        {
            var (manager, factory) = Create();
            var updates = new List<int>();
            manager.CountdownUpdated += (_, c) => updates.Add(c);

            manager.StartCountdown(3);
            updates.Clear();

            factory.Last!.Fire(); // 2
            factory.Last!.Fire(); // 1

            Assert.Equal(new[] { 2, 1 }, updates);
        }
    }
}
