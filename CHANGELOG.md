# Changelog

## 1.0.0 - 2026-05-06

Initial production release.

- Windows tray app for scheduling Gemini in Chrome side-panel tasks.
- Per-user installer and uninstaller with Windows Installed Apps registration.
- Task creation, enable/disable toggles, task configuration, task history, settings, and Chrome profile selection from the tray menu.
- Prompt, schedule, repeat interval, reasoning mode, completion action, and run-immediately task options.
- Automatic `Start Task` approval handling only while scheduled tasks are running.
- Gemini side-panel completion monitoring for completed, error, interrupted, and timeout states.
- Cleanup that closes the Gemini side panel and restores Chrome toward the state it was in before the task run.
- Rotating diagnostic logs with GUI controls for diagnostics and advanced JSON settings for retention/timing.
