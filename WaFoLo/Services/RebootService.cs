using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WaFoLo.Services
{
    /// <summary>
    /// Service for managing system reboot operations.
    /// </summary>
    public class RebootService : IRebootService
    {
        private Process? _shutdownProcess;

        // P/Invoke declarations for Windows API
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool InitiateSystemShutdownEx(
            string? lpMachineName,
            string? lpMessage,
            uint dwTimeout,
            bool bForceAppsClosed,
            bool bRebootAfterShutdown,
            uint dwReason);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool AbortSystemShutdown(string? lpMachineName);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool AdjustTokenPrivileges(
            IntPtr TokenHandle,
            bool DisableAllPrivileges,
            ref TOKEN_PRIVILEGES NewState,
            uint BufferLength,
            IntPtr PreviousState,
            IntPtr ReturnLength);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool OpenProcessToken(
            IntPtr ProcessHandle,
            uint DesiredAccess,
            out IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool LookupPrivilegeValue(
            string? lpSystemName,
            string lpName,
            out LUID lpLuid);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        // Constants
        private const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
        private const uint TOKEN_QUERY = 0x0008;
        private const uint SE_PRIVILEGE_ENABLED = 0x00000002;
        private const string SE_SHUTDOWN_NAME = "SeShutdownPrivilege";
        private const uint SHTDN_REASON_MAJOR_APPLICATION = 0x00040000;
        private const uint SHTDN_REASON_MINOR_HUNG = 0x00000005;
        private const uint SHTDN_REASON_FLAG_PLANNED = 0x80000000;

        // Structures
        [StructLayout(LayoutKind.Sequential)]
        private struct LUID
        {
            public uint LowPart;
            public int HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TOKEN_PRIVILEGES
        {
            public uint PrivilegeCount;
            public LUID Luid;
            public uint Attributes;
        }

        /// <summary>
        /// Initiates a system reboot with the specified delay in seconds.
        /// </summary>
        /// <param name="delaySeconds">Delay before reboot in seconds</param>
        /// <param name="message">Message to display to users</param>
        /// <returns>True if reboot was initiated successfully, false otherwise</returns>
        public bool InitiateReboot(int delaySeconds = 30, string message = "Watchdog timeout - expected log entry not found")
        {
            // First, try using Windows API with shutdown privilege
            if (EnableShutdownPrivilege())
            {
                try
                {
                    uint reason = SHTDN_REASON_MAJOR_APPLICATION | 
                                  SHTDN_REASON_MINOR_HUNG | 
                                  SHTDN_REASON_FLAG_PLANNED;

                    bool success = InitiateSystemShutdownEx(
                        null,                           // Local machine
                        message,                        // Message
                        (uint)delaySeconds,            // Timeout in seconds
                        false,                         // Don't force apps to close immediately
                        true,                          // Reboot after shutdown
                        reason);                       // Reason code

                    if (success)
                    {
                        return true;
                    }
                }
                catch (Exception)
                {
                    // Fall through to fallback method
                }
            }

            // Fallback to shutdown.exe without runas
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "shutdown.exe",
                    Arguments = $"/r /t {delaySeconds} /c \"{message}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                    // Removed Verb = "runas" to avoid UAC prompt
                };

                _shutdownProcess = Process.Start(psi);
                return _shutdownProcess != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Aborts a pending system shutdown/reboot.
        /// </summary>
        /// <returns>True if abort was successful, false otherwise</returns>
        public bool AbortReboot()
        {
            // Try Windows API first
            try
            {
                if (EnableShutdownPrivilege())
                {
                    bool success = AbortSystemShutdown(null);
                    if (success)
                    {
                        return true;
                    }
                }
            }
            catch (Exception)
            {
                // Fall through to fallback
            }

            // Fallback to shutdown.exe
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "shutdown.exe",
                    Arguments = "/a",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                var process = Process.Start(psi);
                return process != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if administrator privileges are available.
        /// </summary>
        public bool HasAdministratorPrivileges()
        {
            try
            {
                var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Enables the shutdown privilege for the current process.
        /// </summary>
        /// <returns>True if privilege was enabled successfully</returns>
        private bool EnableShutdownPrivilege()
        {
            IntPtr tokenHandle = IntPtr.Zero;

            try
            {
                // Open the process token
                if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out tokenHandle))
                {
                    return false;
                }

                // Lookup the shutdown privilege value
                if (!LookupPrivilegeValue(null, SE_SHUTDOWN_NAME, out LUID shutdownLuid))
                {
                    return false;
                }

                // Set up the privilege structure
                TOKEN_PRIVILEGES tokenPrivileges = new TOKEN_PRIVILEGES
                {
                    PrivilegeCount = 1,
                    Luid = shutdownLuid,
                    Attributes = SE_PRIVILEGE_ENABLED
                };

                // Enable the privilege
                return AdjustTokenPrivileges(
                    tokenHandle,
                    false,
                    ref tokenPrivileges,
                    0,
                    IntPtr.Zero,
                    IntPtr.Zero);
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                if (tokenHandle != IntPtr.Zero)
                {
                    CloseHandle(tokenHandle);
                }
            }
        }
    }
}
