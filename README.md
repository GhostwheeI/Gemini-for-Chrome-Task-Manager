# Gemini for Chrome Task Manager

[![Build Status](https://github.com/Ghostwheel/GeminiForChromeManager/workflows/build/badge.svg)](https://github.com/Ghostwheel/GeminiForChromeManager/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)
[![Version](https://img.shields.io/badge/version-1.0.0-success.svg)](#install)

**Gemini for Chrome Task Manager** is the premium GUI and system tray companion for scheduling automated prompt executions through Google Chrome's built-in **Ask Gemini** side panel.

Designed for power users, developers, and researchers who rely on Gemini in Chrome, this application provides an intuitive graphical interface for repeatable scheduled tasks with automatic `Start Task` approval handling, run history, completion monitoring, and comprehensive cleanup.

---

## 🌟 Key Features

- **Automated Scheduling**: Schedule and execute Gemini prompts directly in the Chrome side-panel.
- **Advanced Task Configuration**: Define customized prompts, next run times, repeat intervals, reasoning models (`Auto`, `Fast`, `Thinking`, `Pro`), and completion actions.
- **Smart Tray Menu**: Right-click the system tray icon for rapid access to task creation, status toggles, run history, Chrome profile selection, and global settings.
- **Automatic Execution Approval**: Intelligently handles `Start Task` approvals automatically, exclusively during scheduled task runs.
- **Robust Completion Monitoring**: Seamlessly monitors the Gemini side-panel for task states—including completion, runtime errors, interruptions, and timeouts.
- **Automated Cleanup**: Closes the Gemini side panel and automatically tears down any task-specific Chrome windows or tabs opened during execution.
- **Comprehensive Run History**: View detailed per-task execution logs, start/end timestamps, result outputs, and definitive exception codes.
- **Theme Support**: Seamlessly transitions between Light, Dark, and Automatic system themes.
- **Rotating Diagnostic Logs**: Easily accessible GUI controls for log diagnostics with advanced JSON-configurable retention and timing.
- **Clean Installation**: Native Windows "Installed Apps" behavior for seamless per-user install and uninstall.

---

## 🛠️ Requirements

- **Operating System**: Windows 10 or later.
- **Browser**: Google Chrome.
- **Gemini Access**: Gemini in Chrome enabled for the desired Chrome profile.
- The `Ask Gemini` button/side panel must be available in your Chrome UI.

For official guidance on enabling Gemini in Chrome:
[Official Gemini in Chrome Help](https://support.google.com/chrome/answer/16283624)

---

## 🚀 Installation

Download the latest **GUI/tray release** zip from the [GitHub Releases](#) page, extract it to a local folder, and run the following command from PowerShell:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Install-GeminiForChromeManager.ps1
```

> **Note**: The installer is scoped per-user and does not require administrator privileges.

It installs cleanly to:
`%LocalAppData%\Programs\Gemini for Chrome Task Manager`

The installation process automatically provisions:
- A Start Menu shortcut.
- A Windows "Installed Apps" uninstall entry.
- A system tray icon upon launch.

### Source Builds
If running from a source checkout, the installer will automatically compile the application using `dotnet publish`.

*Readiness Check*: During installation, the script verifies Gemini availability in Chrome. If readiness cannot be strictly confirmed, a warning prompt will appear. You may choose to review the Google setup page or proceed by acknowledging the warning.

---

## 💻 Usage & Tray Menu

The application runs quietly in the system tray. Right-click the icon to access:

- **Status**: Displays the current operational state (e.g., `No Scheduled Tasks`, `Idle`, or active task status).
- **Create a Task**: Launches the task editor.
- **Enable/Disable Tasks**: Quick-access submenu to toggle task activity via checkboxes.
- **Configure Tasks**: Direct access to edit existing task configurations.
- **Task History**: Access execution logs for specific tasks.
- **Chrome Profile [Experimental]**: Target specific Chrome profile directories for scheduled runs.
- **Settings**: Modify application-level configurations.
- **About**: View version info and task statistics.
- **Exit**: Gracefully shut down the application.

### Task Options
Each task can be configured with:
- Task Name & Prompt.
- Schedule constraints (Run once, or repeat by Minutes/Hours/Days).
- Reasoning engine preference (`Auto`, `Fast`, `Thinking`, `Pro`).
- Post-completion actions (e.g., `Show notification`).
- "Run immediately after save" overrides.

### Execution & Cleanup Lifecycle
When a scheduled task triggers, the app:
1. Surfaces Google Chrome to the foreground (required as the Gemini side panel is a Chrome UI surface).
2. Transmits the prompt.
3. Monitors the panel until a terminal state is reached (Idle, Error, Interrupted, Timeout).
4. Automatically closes the side panel and the temporary Chrome window/tab to restore your workspace.

---

## 📊 Telemetry & Task History

Task history is persistently stored in JSON format at:
`%AppData%\Gemini for Chrome Task Manager\task-history.json`

The system retains the 100 most recent entries per task, recording timestamps, results, and definite exception codes:

| Code | Description |
|---|---|
| `GCTM-001` | Task prompt was empty. Execution skipped. |
| `GCTM-002` | Gemini side panel opened, but the prompt box could not be located. |
| `GCTM-003` | Gemini displayed a definite error or retry state. |
| `GCTM-004` | Gemini displayed an interrupted, stopped, or cancelled state. |
| `GCTM-005` | Gemini failed to return to an idle state within the configured timeout. |
| `GCTM-006` | The task runner encountered an unexpected application exception. |

---

## ⚙️ Advanced Configuration

Standard settings (Start with Windows, Diagnostics, Themes) are available via the GUI. Advanced parameters can be modified directly via the configuration file:

`%AppData%\Gemini for Chrome Task Manager\settings.json`

```json
{
  "StartWithWindows": false,
  "Theme": "Auto",
  "ChromeProfileDirectory": "Default",
  "PollSeconds": 1.0,
  "DiagnosticLoggingEnabled": true,
  "MaxLogKilobytes": 256,
  "MaxLogFiles": 5,
  "HeartbeatMinutes": 1,
  "TaskCompletionTimeoutMinutes": 60,
  "TaskIdleStableSeconds": 20
}
```

*Diagnostic Logs* are automatically rotated and stored at:
`%AppData%\Gemini for Chrome Task Manager\logs`

---

## 🗑️ Uninstallation

You can seamlessly uninstall the application using standard Windows settings:
**Windows Settings > Apps > Installed apps > Gemini for Chrome Task Manager**

Alternatively, execute the provided uninstallation script:
```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Uninstall-GeminiForChromeManager.ps1
```

---

## 🧪 Developer Verification

To verify functionality from a source checkout, execute the following build and test sequence:

```powershell
dotnet build .\src\GeminiForChromeManager.csproj --configuration Release -p:EnableWindowsTargeting=true
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Install-GeminiForChromeManager.ps1 -NoLaunch -BuildFromSource
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Test-Smoke.ps1
```

The comprehensive smoke test safely backs up current user data, provisions a temporary testing task, validates exception tracking (`GCTM-001`), and restores the environment perfectly.

---

## 📄 License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for full details.
