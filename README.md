# Gemini for Chrome Task Manager

Windows tray utility for scheduling Gemini-in-Chrome tasks.

Primary features:

- Schedule Gemini for Chrome tasks
- Define the prompt, reasoning mode, next run, repeat interval, and completion action
- Auto-click Gemini `Start Task` approvals when a scheduled task starts
- Uses Chrome's built-in `Ask Gemini` side panel instead of navigating to the Gemini web app

## Install

Run:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Install-GeminiForChromeManager.ps1
```

The installer is per-user and does not require admin rights. It installs to:

```text
%LocalAppData%\Programs\Gemini for Chrome Task Manager
```

It also creates:

- A Start Menu shortcut
- A Windows Installed Apps uninstall entry
- A tray icon while the app is running

Before installing, the installer checks whether Gemini in Chrome appears to be available for the local Chrome profile. If that check cannot confirm readiness, it shows a warning with a link to Google's official Gemini in Chrome setup/help page:

```text
https://support.google.com/chrome/answer/16283624
```

## Configure

Right-click the tray icon to change settings:

- `Create a Task`
- `Enable/Disable Tasks`
- `Configure Tasks`
- `Task History`
- `Chrome Profile [Experimental]`
- `Settings`
- `About`
- `Exit`

Settings are stored in:

```text
%AppData%\Gemini for Chrome Task Manager\settings.json
```

The Settings window intentionally exposes only:

- `Start with Windows`
- `Diagnostic Logging`
- `Theme`

Advanced settings can be changed in `settings.json`. The default configuration is:

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

Scheduled tasks are stored in:

```text
%AppData%\Gemini for Chrome Task Manager\tasks.json
```

Task run history is stored in:

```text
%AppData%\Gemini for Chrome Task Manager\task-history.json
```

The app keeps the most recent 100 history entries per task. History records include when the run started, when it ended, whether a definite exception was detected, and the exception code when one applies.

Task exception codes:

- `GCTM-001`: Task prompt was empty, so nothing could be sent to Gemini.
- `GCTM-002`: Gemini side panel opened, but the prompt box could not be found or focused.
- `GCTM-003`: Gemini displayed a definite error or retry state.
- `GCTM-004`: Gemini displayed an interrupted, stopped, or cancelled state.
- `GCTM-005`: Gemini did not return to idle before the configured completion timeout.
- `GCTM-006`: The task runner hit an unexpected application exception.

Scheduled task behavior:

- At the scheduled time, the app opens Chrome and uses the `Ask Gemini` button to open Gemini in Chrome's side panel.
- Chrome is launched with the selected `Chrome Profile [Experimental]` profile directory. `Default` is used unless another detected Chrome profile is selected.
- Chrome must be brought to the foreground while a scheduled task starts because the Gemini side panel is a Chrome UI surface, not a background API.
- If `Run Immediately` is checked while saving a task, the app starts that task right away.
- It applies the task's selected reasoning mode when the side panel exposes the mode picker.
- It copies the task prompt to the clipboard.
- It attempts to paste and send the prompt in the Chrome Gemini side panel prompt box.
- `Start Task` approvals are clicked automatically for that scheduled task while it starts.
- The app monitors the Gemini side panel for completion, error, interruption, or timeout signals.
- When the task stops moving forward, the app closes the Gemini side panel and the dedicated Chrome task tab/window it opened.
- The completion action runs after the task monitor finishes.

Diagnostic logs are stored in:

```text
%AppData%\Gemini for Chrome Task Manager\logs
```

Diagnostic logging can be enabled or disabled from `Settings`. Log retention is controlled from `settings.json`. By default, the app keeps at most five `diagnostic*.log` files, and each file rotates at about 256 KB. It logs startup, settings changes, candidate detections, clicks, cooldown skips, errors, and heartbeat summaries with scan counts and timing. It does not write a separate line for every idle scan.

## Uninstall

Use Windows Settings > Apps > Installed apps, then uninstall `Gemini for Chrome Task Manager`.

You can also run:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Uninstall-GeminiForChromeManager.ps1
```
