<#
Uninstalls Gemini for Chrome Task Manager for the current Windows user.
#>

[CmdletBinding()]
param(
    [switch]$Quiet
)

$ErrorActionPreference = 'Stop'

$AppName = 'Gemini for Chrome Task Manager'
$LegacyAppName = 'Gemini for Chrome Manager'
$InstallPath = Join-Path $env:LOCALAPPDATA 'Programs\Gemini for Chrome Task Manager'
$LegacyInstallPath = Join-Path $env:LOCALAPPDATA 'Programs\Gemini for Chrome Manager'
$ShortcutPath = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\Gemini for Chrome Task Manager.lnk'
$LegacyShortcutPath = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\Gemini for Chrome Manager.lnk'
$UninstallKeyPath = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\GeminiForChromeTaskManager'
$LegacyUninstallKeyPath = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\GeminiForChromeManager'
$RunKeyPath = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
$DataPath = Join-Path $env:APPDATA 'Gemini for Chrome Task Manager'
$LegacyDataPath = Join-Path $env:APPDATA 'Gemini for Chrome Manager'

if (-not $Quiet) {
    $answer = Read-Host ('Uninstall {0}? Type Y to continue' -f $AppName)

    if ($answer -notin @('Y', 'y', 'Yes', 'yes')) {
        Write-Host 'Uninstall cancelled.'
        return
    }
}

Get-Process -Name 'GeminiForChromeManager' -ErrorAction SilentlyContinue |
    Stop-Process -Force -ErrorAction SilentlyContinue

Remove-ItemProperty -Path $RunKeyPath -Name $AppName -ErrorAction SilentlyContinue
Remove-ItemProperty -Path $RunKeyPath -Name $LegacyAppName -ErrorAction SilentlyContinue
Remove-Item -Path $ShortcutPath -Force -ErrorAction SilentlyContinue
Remove-Item -Path $LegacyShortcutPath -Force -ErrorAction SilentlyContinue
Remove-Item -Path $UninstallKeyPath -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path $LegacyUninstallKeyPath -Recurse -Force -ErrorAction SilentlyContinue

# If this script is running from the install folder, a short background cleanup
# avoids leaving the script file locked during removal.
$cleanupCommand = 'timeout /t 2 /nobreak > nul & rmdir /s /q "{0}" & rmdir /s /q "{1}" & rmdir /s /q "{2}" & rmdir /s /q "{3}"' -f $InstallPath, $LegacyInstallPath, $DataPath, $LegacyDataPath
Start-Process -FilePath 'cmd.exe' -ArgumentList '/c', $cleanupCommand -WindowStyle Hidden

if (-not $Quiet) {
    Write-Host ('Uninstalled {0}.' -f $AppName)
}
