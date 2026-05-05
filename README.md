# WaFoLo

**Wa**tch**Fo**r**Lo**g — A Windows desktop log file watchdog that monitors log files for trigger/expected line patterns, enforces timeouts, and can automatically reboot the system or restart processes when issues are detected.

## Features

- **Real-time log monitoring** — Watches log files for new lines and detects trigger patterns
- **Sequence detection** — Tracks trigger → expected line sequences with configurable timeout
- **Process monitoring** — Optionally waits for a specific process to be running before starting monitoring
- **Automatic recovery actions**:
  - Process restart on timeout (with configurable retry attempts)
  - System reboot on timeout (with abort capability and test mode)
  - Auto-close application on success
- **WPF UI** — Modern interface built with MahApps.Metro
- **Configurable** — JSON-based configuration with runtime editing
- **Test mode** — Simulates reboot behavior without actually rebooting
- **Comprehensive logging** — Activity log displayed in UI and written to file

## Project Structure

```
WaFoLo.slnx
├── WaFoLo/                  # WPF application (UI layer)
│   ├── MainWindow.xaml      # Main window UI
│   ├── ViewModels/          # MVVM view models
│   ├── Services/            # WPF-specific services (reboot, dialogs, timers)
│   └── config.json          # Configuration file
├── WaFoLo.Core/             # Core business logic (platform-independent)
│   ├── Models/              # Data models (WatchdogConfig, LogLineInfo)
│   ├── Services/            # Core services (monitoring, scanning, timeout, etc.)
│   └── Utilities/           # Timestamp parser and factory
└── WaFoLo.Tests/            # Unit tests (xUnit + Moq)
```

## Tech Stack

- **.NET 10** (net10.0-windows)
- **WPF** with **MahApps.Metro** for the UI
- **Microsoft.Extensions.DependencyInjection** for DI
- **xUnit** + **Moq** for testing
- **Nullable reference types** and **implicit usings** enabled

## Configuration

Configuration is managed via `config.json` in the application directory. All keys are case-sensitive and use PascalCase to match the C# property names.

| Setting | Type | Description |
|---|---|---|
| `LogFilePath` | `string` | Path to the log file to monitor |
| `TriggerLinePattern` | `string` | Text pattern that starts the timeout countdown |
| `ExpectedLinePattern` | `string` | Text pattern that stops the countdown (success) |
| `TimeoutSeconds` | `int` | Seconds to wait for the expected line after trigger |
| `TestMode` | `bool` | When `true`, simulates reboot instead of actually rebooting |
| `ShowConfigurationOnStartup` | `bool` | Show configuration panel on launch |
| `LogTimestampFormat` | `string` | DateTime format string for parsing log timestamps (e.g., `"yyyy-MM-dd HH:mm:ss"`) |
| `LogTimestampPosition` | `string` | Position of the timestamp in log lines (`"start"` or `"end"`) |
| `MonitoredProcessName` | `string` | Process name (without `.exe`) to wait for before monitoring |
| `WaitForMonitoredProcess` | `bool` | Whether to wait for the process to start before monitoring |
| `ProcessCheckIntervalSeconds` | `int` | How often to check for the monitored process |
| `AutoCloseOnSuccess` | `bool` | Auto-close app after successful sequence |
| `AutoCloseDelaySeconds` | `int` | Countdown duration in seconds before auto-close |
| `RestartProcessOnTimeout` | `bool` | Attempt process restart before reboot on timeout |
| `RestartProcessDelaySeconds` | `int` | Seconds to wait after restart for process stabilization |
| `MaxRestartAttempts` | `int` | Maximum process restart retry attempts before rebooting |
| `ProcessRestartCommand` | `string` | Full path to the executable for restarting the process |

## Getting Started

### Prerequisites

- .NET 10 SDK
- Windows OS (WPF requirement)

### Build

```bash
dotnet build WaFoLo.slnx
```

### Run

```bash
dotnet run --project WaFoLo
```

### Test

```bash
dotnet test WaFoLo.Tests
```

## How It Works

1. **Startup** — Loads configuration and optionally displays it
2. **Process Wait** (optional) — Waits for a monitored process to start
3. **Log Monitoring** — Scans existing lines and watches for new lines
4. **Trigger Detection** — When a trigger pattern is found, starts a countdown
5. **Expected Line** — If the expected pattern appears before timeout, success
6. **Timeout** — If timeout expires, attempts process restart (if configured) then reboot
7. **Auto-Close** — On success, can automatically close the application after a delay

## License

See [LICENSE.txt](LICENSE.txt) for details.
