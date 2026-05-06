<#
Installs Gemini for Chrome Task Manager for the current Windows user.

This is intentionally per-user so it works without admin rights. Windows still
shows the app through the normal Installed Apps / Apps & Features uninstall UI.
#>

[CmdletBinding()]
param(
    [switch]$NoLaunch
)

$ErrorActionPreference = 'Stop'

$AppName = 'Gemini for Chrome Task Manager'
$LegacyAppName = 'Gemini for Chrome Manager'
$AppId = 'GeminiForChromeTaskManager'
$LegacyAppId = 'GeminiForChromeManager'
$Version = '0.5.0'
$Publisher = 'Ghostwheel'
$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectPath = Join-Path $ScriptRoot 'src\GeminiForChromeManager.csproj'
$PublishPath = Join-Path $ScriptRoot 'dist\publish'
$InstallPath = Join-Path $env:LOCALAPPDATA 'Programs\Gemini for Chrome Task Manager'
$LegacyInstallPath = Join-Path $env:LOCALAPPDATA 'Programs\Gemini for Chrome Manager'
$ExePath = Join-Path $InstallPath 'GeminiForChromeManager.exe'
$StartMenuFolder = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs'
$ShortcutPath = Join-Path $StartMenuFolder 'Gemini for Chrome Task Manager.lnk'
$LegacyShortcutPath = Join-Path $StartMenuFolder 'Gemini for Chrome Manager.lnk'
$UninstallKeyPath = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\GeminiForChromeTaskManager'
$LegacyUninstallKeyPath = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\GeminiForChromeManager'
$DataPath = Join-Path $env:APPDATA 'Gemini for Chrome Task Manager'
$LegacyDataPath = Join-Path $env:APPDATA 'Gemini for Chrome Manager'

function Publish-App {
    Write-Host 'Publishing application...'

    dotnet publish $ProjectPath `
        --configuration Release `
        --runtime win-x64 `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:EnableCompressionInSingleFile=true `
        -o $PublishPath
}

function Stop-RunningApp {
    $processes = @(Get-Process -Name 'GeminiForChromeManager' -ErrorAction SilentlyContinue)

    if ($processes.Count -eq 0) {
        return
    }

    Write-Host 'Stopping running application...'

    foreach ($process in $processes) {
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
    }

    foreach ($process in $processes) {
        try {
            Wait-Process -Id $process.Id -Timeout 10 -ErrorAction Stop
        }
        catch {
            Start-Sleep -Milliseconds 500
        }
    }

    $deadline = (Get-Date).AddSeconds(15)

    while ((Get-Date) -lt $deadline) {
        $remaining = @(Get-Process -Name 'GeminiForChromeManager' -ErrorAction SilentlyContinue)

        if ($remaining.Count -eq 0) {
            Start-Sleep -Milliseconds 750
            return
        }

        foreach ($process in $remaining) {
            Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        }

        Start-Sleep -Milliseconds 500
    }
}

function Copy-AppFiles {
    Write-Host ('Installing to {0}' -f $InstallPath)

    New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null

    $attempt = 0
    $maxAttempts = 10

    while ($true) {
        try {
            Copy-Item -Path (Join-Path $PublishPath '*') -Destination $InstallPath -Recurse -Force
            Copy-Item -Path (Join-Path $ScriptRoot 'Uninstall-GeminiForChromeManager.ps1') -Destination $InstallPath -Force
            return
        }
        catch {
            $attempt++

            if ($attempt -ge $maxAttempts) {
                throw
            }

            Write-Host ('Install file copy was blocked; retrying ({0}/{1})...' -f $attempt, $maxAttempts)
            Start-Sleep -Seconds 1
        }
    }
}

function Remove-LegacyInstallArtifacts {
    Remove-Item -Path $LegacyShortcutPath -Force -ErrorAction SilentlyContinue
    Remove-Item -Path $LegacyUninstallKeyPath -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -Path $LegacyInstallPath -Recurse -Force -ErrorAction SilentlyContinue
    Remove-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name $LegacyAppName -ErrorAction SilentlyContinue
}

function Move-LegacyData {
    if (-not (Test-Path $LegacyDataPath)) {
        return
    }

    if (-not (Test-Path $DataPath)) {
        Move-Item -LiteralPath $LegacyDataPath -Destination $DataPath -Force
        return
    }

    $legacyTasks = Join-Path $LegacyDataPath 'tasks.json'
    $currentTasks = Join-Path $DataPath 'tasks.json'

    if ((Test-Path $legacyTasks) -and -not (Test-Path $currentTasks)) {
        Copy-Item -LiteralPath $legacyTasks -Destination $currentTasks -Force
    }

    Remove-Item -LiteralPath $LegacyDataPath -Recurse -Force -ErrorAction SilentlyContinue
}

function New-StartMenuShortcut {
    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut($ShortcutPath)
    $shortcut.TargetPath = $ExePath
    $shortcut.WorkingDirectory = $InstallPath
    $shortcut.IconLocation = $ExePath
    $shortcut.Description = $AppName
    $shortcut.Save()
}

function Register-Uninstaller {
    $uninstallScript = Join-Path $InstallPath 'Uninstall-GeminiForChromeManager.ps1'
    $uninstallCommand = 'powershell.exe -NoProfile -ExecutionPolicy Bypass -File "{0}"' -f $uninstallScript
    $quietUninstallCommand = 'powershell.exe -NoProfile -ExecutionPolicy Bypass -File "{0}" -Quiet' -f $uninstallScript
    $estimatedSize = [int]((Get-ChildItem $InstallPath -Recurse -File | Measure-Object -Property Length -Sum).Sum / 1KB)

    New-Item -Path $UninstallKeyPath -Force | Out-Null
    New-ItemProperty -Path $UninstallKeyPath -Name 'DisplayName' -Value $AppName -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $UninstallKeyPath -Name 'DisplayVersion' -Value $Version -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $UninstallKeyPath -Name 'Publisher' -Value $Publisher -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $UninstallKeyPath -Name 'InstallLocation' -Value $InstallPath -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $UninstallKeyPath -Name 'DisplayIcon' -Value $ExePath -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $UninstallKeyPath -Name 'UninstallString' -Value $uninstallCommand -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $UninstallKeyPath -Name 'QuietUninstallString' -Value $quietUninstallCommand -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $UninstallKeyPath -Name 'EstimatedSize' -Value $estimatedSize -PropertyType DWord -Force | Out-Null
    New-ItemProperty -Path $UninstallKeyPath -Name 'NoModify' -Value 1 -PropertyType DWord -Force | Out-Null
    New-ItemProperty -Path $UninstallKeyPath -Name 'NoRepair' -Value 1 -PropertyType DWord -Force | Out-Null
}

Publish-App
Stop-RunningApp
Remove-LegacyInstallArtifacts
Move-LegacyData
Copy-AppFiles
New-StartMenuShortcut
Register-Uninstaller

if (-not $NoLaunch) {
    Start-Process -FilePath $ExePath -WorkingDirectory $InstallPath
}

Write-Host ''
Write-Host 'Installed Gemini for Chrome Task Manager.'
Write-Host ('Executable: {0}' -f $ExePath)
Write-Host 'Uninstall from Windows Settings > Apps > Installed apps, or run the installed uninstall script.'
