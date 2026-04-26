using Moq;
using WaFoLo.Services;

namespace WaFoLo.Tests
{
    public class RebootManagerTests
    {
        private static (RebootManager manager, Mock<IRebootService> reboot, Mock<IDialogService> dialog)
            Create()
        {
            var reboot = new Mock<IRebootService>();
            var dialog = new Mock<IDialogService>();
            return (new RebootManager(reboot.Object, dialog.Object), reboot, dialog);
        }

        [Fact]
        public void Constructor_NullRebootService_ThrowsArgumentNullException()
        {
            var dialog = new Mock<IDialogService>();
            Assert.Throws<ArgumentNullException>(() =>
                new RebootManager(null!, dialog.Object));
        }

        [Fact]
        public void Constructor_NullDialogService_ThrowsArgumentNullException()
        {
            var reboot = new Mock<IRebootService>();
            Assert.Throws<ArgumentNullException>(() =>
                new RebootManager(reboot.Object, null!));
        }

        [Fact]
        public void HandleTimeout_TestMode_ShowsWarningDialogAndReturnsTrue()
        {
            var (manager, _, dialog) = Create();

            bool result = manager.HandleTimeout(testMode: true, triggerTime: DateTime.Now, timeoutSeconds: 30);

            Assert.True(result);
            dialog.Verify(d => d.ShowWarning(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void HandleTimeout_TestMode_DoesNotCallRebootService()
        {
            var (manager, reboot, _) = Create();

            manager.HandleTimeout(testMode: true, triggerTime: DateTime.Now, timeoutSeconds: 30);

            reboot.Verify(r => r.InitiateReboot(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void HandleTimeout_TestMode_RaisesLogActivityWithTestModeMessage()
        {
            var (manager, _, _) = Create();
            string? logMessage = null;
            manager.LogActivity += (_, msg) => logMessage = msg;

            manager.HandleTimeout(testMode: true, triggerTime: DateTime.Now, timeoutSeconds: 30);

            Assert.NotNull(logMessage);
            Assert.Contains("TEST MODE", logMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void HandleTimeout_ProductionMode_CallsInitiateReboot()
        {
            var (manager, reboot, _) = Create();
            reboot.Setup(r => r.InitiateReboot(It.IsAny<int>(), It.IsAny<string>())).Returns(true);

            manager.HandleTimeout(testMode: false, triggerTime: DateTime.Now, timeoutSeconds: 30);

            reboot.Verify(r => r.InitiateReboot(30, It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void InitiateReboot_Success_RaisesRebootInitiatedAndReturnsTrue()
        {
            var (manager, reboot, _) = Create();
            reboot.Setup(r => r.InitiateReboot(It.IsAny<int>(), It.IsAny<string>())).Returns(true);
            bool rebootInitiated = false;
            manager.RebootInitiated += (_, _) => rebootInitiated = true;

            bool result = manager.InitiateReboot();

            Assert.True(result);
            Assert.True(rebootInitiated);
        }

        [Fact]
        public void InitiateReboot_Failure_ShowsErrorDialogAndReturnsFalse()
        {
            var (manager, reboot, dialog) = Create();
            reboot.Setup(r => r.InitiateReboot(It.IsAny<int>(), It.IsAny<string>())).Returns(false);
            reboot.Setup(r => r.HasAdministratorPrivileges()).Returns(true);

            bool result = manager.InitiateReboot();

            Assert.False(result);
            dialog.Verify(d => d.ShowError(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void InitiateReboot_FailureWithoutAdminPrivileges_MentionsAdminRequirement()
        {
            var (manager, reboot, dialog) = Create();
            reboot.Setup(r => r.InitiateReboot(It.IsAny<int>(), It.IsAny<string>())).Returns(false);
            reboot.Setup(r => r.HasAdministratorPrivileges()).Returns(false);

            string? errorMessage = null;
            dialog.Setup(d => d.ShowError(It.IsAny<string>(), It.IsAny<string>()))
                  .Callback<string, string>((msg, _) => errorMessage = msg);

            manager.InitiateReboot();

            Assert.NotNull(errorMessage);
            Assert.Contains("administrator", errorMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void AbortReboot_Success_RaisesRebootAbortedAndReturnsTrue()
        {
            var (manager, reboot, dialog) = Create();
            reboot.Setup(r => r.AbortReboot()).Returns(true);
            bool aborted = false;
            manager.RebootAborted += (_, _) => aborted = true;

            bool result = manager.AbortReboot();

            Assert.True(result);
            Assert.True(aborted);
            dialog.Verify(d => d.ShowInfo(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void AbortReboot_Failure_ShowsErrorDialogAndReturnsFalse()
        {
            var (manager, reboot, dialog) = Create();
            reboot.Setup(r => r.AbortReboot()).Returns(false);

            bool result = manager.AbortReboot();

            Assert.False(result);
            dialog.Verify(d => d.ShowError(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void HandleTimeout_ProductionMode_RaisesLogActivity()
        {
            var (manager, reboot, _) = Create();
            reboot.Setup(r => r.InitiateReboot(It.IsAny<int>(), It.IsAny<string>())).Returns(true);
            string? logMessage = null;
            manager.LogActivity += (_, msg) => logMessage = msg;

            manager.HandleTimeout(testMode: false, triggerTime: DateTime.Now, timeoutSeconds: 30);

            Assert.NotNull(logMessage);
        }
    }
}
