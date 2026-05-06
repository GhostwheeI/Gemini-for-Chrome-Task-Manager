<#
Runs non-interactive smoke checks for Gemini for Chrome Task Manager.

The script backs up the current per-user task/history files, runs a temporary
empty-prompt task to verify scheduler/history exception recording, restores the
original data, and restarts the app if it was running before the test.
#>

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$AppName = 'GeminiForChromeManager'
$InstallPath = Join-Path $env:LOCALAPPDATA 'Programs\Gemini for Chrome Task Manager'
$ExePath = Join-Path $InstallPath 'GeminiForChromeManager.exe'
$DataPath = Join-Path $env:APPDATA 'Gemini for Chrome Task Manager'
$TasksPath = Join-Path $DataPath 'tasks.json'
$HistoryPath = Join-Path $DataPath 'task-history.json'
$BackupRoot = Join-Path $env:TEMP ('GeminiForChromeTaskManager-Smoke-{0}' -f ([guid]::NewGuid().ToString('N')))
$TasksBackup = Join-Path $BackupRoot 'tasks.json'
$HistoryBackup = Join-Path $BackupRoot 'task-history.json'

function Stop-App {
    Get-Process -Name $AppName -ErrorAction SilentlyContinue |
        Stop-Process -Force -ErrorAction SilentlyContinue
}

function Start-App {
    if (-not (Test-Path -LiteralPath $ExePath)) {
        throw "Installed executable not found: $ExePath"
    }

    Start-Process -FilePath $ExePath -WorkingDirectory $InstallPath
}

function Backup-UserData {
    New-Item -ItemType Directory -Path $BackupRoot -Force | Out-Null

    if (Test-Path -LiteralPath $TasksPath) {
        Copy-Item -LiteralPath $TasksPath -Destination $TasksBackup -Force
    }

    if (Test-Path -LiteralPath $HistoryPath) {
        Copy-Item -LiteralPath $HistoryPath -Destination $HistoryBackup -Force
    }
}

function Restore-UserData {
    New-Item -ItemType Directory -Path $DataPath -Force | Out-Null

    if (Test-Path -LiteralPath $TasksBackup) {
        Copy-Item -LiteralPath $TasksBackup -Destination $TasksPath -Force
    }
    else {
        Remove-Item -LiteralPath $TasksPath -Force -ErrorAction SilentlyContinue
    }

    if (Test-Path -LiteralPath $HistoryBackup) {
        Copy-Item -LiteralPath $HistoryBackup -Destination $HistoryPath -Force
    }
    else {
        Remove-Item -LiteralPath $HistoryPath -Force -ErrorAction SilentlyContinue
    }
}

function Write-SmokeTask {
    New-Item -ItemType Directory -Path $DataPath -Force | Out-Null

    $task = [ordered]@{
        Id = 'smoke-empty-prompt'
        Name = 'Smoke Empty Prompt'
        Enabled = $true
        Prompt = ''
        ReasoningLevel = 0
        ScheduleKind = 0
        NextRunLocal = (Get-Date).AddSeconds(-5).ToString('o')
        RepeatEvery = 1
        RepeatUnit = 1
        CompletionAction = 0
        LastRunLocal = $null
        LastResult = 'Never run'
    }

    $json = '[' + ($task | ConvertTo-Json -Depth 5) + ']'
    Set-Content -LiteralPath $TasksPath -Value $json -Encoding UTF8
    Remove-Item -LiteralPath $HistoryPath -Force -ErrorAction SilentlyContinue
}

function Assert-SmokeResult {
    if (-not (Test-Path -LiteralPath $HistoryPath)) {
        throw 'Smoke test failed: task-history.json was not created.'
    }

    $history = Get-Content -LiteralPath $HistoryPath -Raw | ConvertFrom-Json
    $entry = @($history) | Where-Object { $_.TaskId -eq 'smoke-empty-prompt' } | Select-Object -First 1

    if (-not $entry) {
        throw 'Smoke test failed: expected history entry was not found.'
    }

    if ($entry.HadException -ne $true -or $entry.ExceptionCode -ne 'GCTM-001') {
        throw ('Smoke test failed: expected GCTM-001, got HadException={0}, ExceptionCode={1}.' -f $entry.HadException, $entry.ExceptionCode)
    }

    Write-Host 'Smoke test passed: scheduler recorded GCTM-001 history entry.'
}

$wasRunning = @(Get-Process -Name $AppName -ErrorAction SilentlyContinue).Count -gt 0

try {
    Backup-UserData
    Stop-App
    Start-Sleep -Seconds 1
    Write-SmokeTask
    Start-App
    Start-Sleep -Seconds 20
    Stop-App
    Assert-SmokeResult
}
finally {
    Stop-App
    Restore-UserData

    if ($wasRunning) {
        Start-App
    }

    Remove-Item -LiteralPath $BackupRoot -Recurse -Force -ErrorAction SilentlyContinue
}
