# Gemini for Chrome Task Manager

Windows tray utility for scheduling Gemini-in-Chrome tasks.

Primary features:

- Schedule Gemini for Chrome tasks
- Define the prompt, next run, repeat interval, and completion action
- Auto-click Gemini `Start Task` approvals when a scheduled task starts

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

## Configure

Right-click the tray icon to change settings:

- `Create a Task`
- `Enable/Disable Tasks`
- `Configure Tasks`
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

Advanced settings can be changed in `settings.json`. The default configuration is:

```json
{
  "StartWithWindows": false,
  "PollSeconds": 1.0,
  "DiagnosticLoggingEnabled": true,
  "MaxLogKilobytes": 256,
  "MaxLogFiles": 5,
  "HeartbeatMinutes": 1
}
```

Scheduled tasks are stored in:

```text
%AppData%\Gemini for Chrome Task Manager\tasks.json
```

Scheduled task behavior:

- At the scheduled time, the app opens Gemini in Chrome.
- If `Run Immediately` is checked while saving a task, the app starts that task right away.
- It copies the task prompt to the clipboard.
- It attempts to paste and send the prompt in the active Gemini page.
- `Start Task` approvals are clicked automatically for that scheduled task while it starts.
- The completion action runs when the scheduled task starts.

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
