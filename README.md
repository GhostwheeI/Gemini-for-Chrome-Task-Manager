# Gemini for Chrome Task Manager

Gemini for Chrome Task Manager is the **GUI/tray variant** of the Gemini for Chrome task scheduler. It is a Windows tray app for scheduling prompts through Chrome's built-in **Ask Gemini** side panel.

It is designed for people who already use Gemini in Chrome and want a graphical task manager for repeatable scheduled tasks with automatic `Start Task` approval handling, run history, completion detection, and cleanup.

## Features

- Schedule Gemini in Chrome side-panel tasks.
- Define prompt, next run, repeat interval, reasoning mode, completion action, and whether the task should run immediately after save.
- Right-click tray menu for task creation, task enable/disable, task configuration, task history, Chrome profile selection, settings, about, and exit.
- Automatic `Start Task` approval clicking only while scheduled Gemini tasks are running.
- Gemini side-panel completion monitoring for completed, error, interrupted, and timeout states.
- Chrome cleanup after task completion: closes the Gemini side panel and the task tab/window the app opened.
- Per-task run history with start time, end time, result, and exception code when a definite issue prevented completion.
- Light, dark, and automatic system theme modes.
- Rotating diagnostic logs with GUI control for diagnostic logging and JSON settings for advanced retention/timing.
- Per-user install/uninstall through normal Windows Installed Apps behavior.

## Requirements

- Windows 10 or later.
- Google Chrome.
- Gemini in Chrome enabled for the Chrome profile you want to use.
- The `Ask Gemini` button/side panel must be available in Chrome.

Official Gemini in Chrome help:

```text
https://support.google.com/chrome/answer/16283624
```

## Install

Download the latest **GUI/tray release** zip from GitHub Releases, extract it, then run:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Install-GeminiForChromeManager.ps1
```

The installer is per-user and does not require admin rights. It installs to:

```text
%LocalAppData%\Programs\Gemini for Chrome Task Manager
```

It creates:

- A Start Menu shortcut.
- A Windows Installed Apps uninstall entry.
- A tray icon when the app is running.

The GUI release package includes a prebuilt Windows tray app. When running from a source checkout instead, the installer builds the app with `dotnet publish`.

Before installation, the installer checks whether Gemini in Chrome appears to be available. If readiness cannot be confirmed, it shows a warning with a link to Google's official setup/help page. You can cancel, open the help page, or check **I understand this Application may not work** to continue anyway.

## Tray Menu

Right-click the tray icon:

- `Status`: shows `No Scheduled Tasks`, `Idle`, or the active task status.
- `Create a Task`: opens the task editor.
- `Enable/Disable Tasks`: submenu of configured tasks with visible check boxes.
- `Configure Tasks`: submenu of configured tasks that opens the task editor.
- `Task History`: submenu of configured tasks that opens run history for that task.
- `Chrome Profile [Experimental]`: selects the Chrome profile directory used for scheduled runs.
- `Settings`: app-level settings.
- `About`: version and task summary.
- `Exit`: closes the tray app.

## Task Options

Each scheduled task supports:

- Task name.
- Prompt.
- Schedule: run once or repeat every N minutes/hours/days.
- Next run date and time.
- Reasoning: `Auto`, `Fast`, `Thinking`, or `Pro`.
- Completion action: `Do Nothing` or `Show notification`.
- Run immediately after save.
- Enabled/disabled state.

`Start Task` approvals are always allowed during a scheduled task run. Outside that scheduled-task approval window, the approval watcher stays inactive.

## Task Completion And Cleanup

After sending a prompt, the app monitors Chrome's Gemini side panel. It treats a task as finished when Gemini returns to a stable idle state. It flags definite failures when Gemini exposes an error, interruption, or when the configured completion timeout is reached.

After the task reaches a terminal state, the app attempts to:

- Close the Gemini side panel.
- Close the task-created Chrome window if Chrome was not already running.
- Otherwise close the active task tab, leaving the user's existing Chrome session in place.

Chrome must come to the foreground while a task starts because the Gemini side panel is a Chrome UI surface, not a background API.

## Task History

Task history is stored in:

```text
%AppData%\Gemini for Chrome Task Manager\task-history.json
```

The app keeps the most recent 100 history entries per task. Each entry includes:

- Started date/time.
- Ended date/time.
- Whether a definite exception was detected.
- Exception code, when applicable.
- Result text.

Exception codes:

- `GCTM-001`: Task prompt was empty, so nothing could be sent to Gemini.
- `GCTM-002`: Gemini side panel opened, but the prompt box could not be found or focused.
- `GCTM-003`: Gemini displayed a definite error or retry state.
- `GCTM-004`: Gemini displayed an interrupted, stopped, or cancelled state.
- `GCTM-005`: Gemini did not return to idle before the configured completion timeout.
- `GCTM-006`: The task runner hit an unexpected application exception.

## Settings

Settings are stored in:

```text
%AppData%\Gemini for Chrome Task Manager\settings.json
```

The Settings window exposes:

- `Start with Windows`
- `Diagnostic Logging`
- `Theme`: `Auto`, `Light`, or `Dark`

Advanced settings can be changed directly in `settings.json`:

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

Task definitions are stored in:

```text
%AppData%\Gemini for Chrome Task Manager\tasks.json
```

Diagnostic logs are stored in:

```text
%AppData%\Gemini for Chrome Task Manager\logs
```

By default, the app keeps at most five diagnostic log files and rotates each at about 256 KB.

## Uninstall

Use Windows Settings > Apps > Installed apps, then uninstall `Gemini for Chrome Task Manager`.

You can also run:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Uninstall-GeminiForChromeManager.ps1
```

## Developer Verification

From a source checkout:

```powershell
dotnet build .\src\GeminiForChromeManager.csproj --configuration Release
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Install-GeminiForChromeManager.ps1 -NoLaunch -BuildFromSource
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Test-Smoke.ps1
```

The smoke test backs up current user task/history data, starts the installed app with a temporary empty-prompt task, verifies a `GCTM-001` history entry, restores the original data, and restarts the app if it was running before the test.

## License

MIT License. See [LICENSE](LICENSE).
