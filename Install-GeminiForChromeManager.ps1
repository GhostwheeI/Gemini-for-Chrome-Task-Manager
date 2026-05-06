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
$Version = '0.9.0'
$Publisher = 'Ghostwheel'
$OfficialGeminiChromeHelpUrl = 'https://support.google.com/chrome/answer/16283624'
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

function Get-ChromeInstallInfo {
    $paths = @(
        (Join-Path $env:ProgramFiles 'Google\Chrome\Application\chrome.exe'),
        (Join-Path ${env:ProgramFiles(x86)} 'Google\Chrome\Application\chrome.exe'),
        (Join-Path $env:LOCALAPPDATA 'Google\Chrome\Application\chrome.exe')
    )

    foreach ($path in $paths) {
        if (Test-Path -LiteralPath $path) {
            $item = Get-Item -LiteralPath $path
            return [pscustomobject]@{
                Found = $true
                Path = $path
                Version = $item.VersionInfo.ProductVersion
            }
        }
    }

    return [pscustomobject]@{
        Found = $false
        Path = ''
        Version = ''
    }
}

function Test-GeminiInChromeSetup {
    $chrome = Get-ChromeInstallInfo
    $reasons = New-Object System.Collections.Generic.List[string]

    if (-not $chrome.Found) {
        $reasons.Add('Google Chrome was not found in the standard install locations.')
    }

    $localStatePath = Join-Path $env:LOCALAPPDATA 'Google\Chrome\User Data\Local State'
    $profileIsEligible = $false

    if (Test-Path -LiteralPath $localStatePath) {
        try {
            $localState = Get-Content -LiteralPath $localStatePath -Raw | ConvertFrom-Json
            $profiles = $localState.profile.info_cache.PSObject.Properties

            foreach ($profile in $profiles) {
                if ($profile.Value.is_glic_eligible -eq $true) {
                    $profileIsEligible = $true
                    break
                }
            }
        }
        catch {
            $reasons.Add('Chrome local state could not be read to confirm Gemini eligibility.')
        }
    }
    else {
        $reasons.Add('Chrome local state was not found, so Gemini in Chrome could not be confirmed.')
    }

    $preferencesRoot = Join-Path $env:LOCALAPPDATA 'Google\Chrome\User Data'
    $hasGeminiSidePanelState = $false

    if (Test-Path -LiteralPath $preferencesRoot) {
        $preferenceFiles = @(Get-ChildItem -LiteralPath $preferencesRoot -Filter 'Preferences' -Recurse -File -ErrorAction SilentlyContinue)

        foreach ($file in $preferenceFiles) {
            try {
                $text = Get-Content -LiteralPath $file.FullName -Raw

                if ($text -match '"kGlic"' -or $text -match '"glic_rollout_eligibility"\s*:\s*true' -or $text -match '"gemini_thread"') {
                    $hasGeminiSidePanelState = $true
                    break
                }
            }
            catch {
            }
        }
    }

    if (-not $profileIsEligible) {
        $reasons.Add('No Chrome profile was confirmed as eligible for Gemini in Chrome.')
    }

    if (-not $hasGeminiSidePanelState) {
        $reasons.Add('No existing Gemini side panel state was found in Chrome preferences.')
    }

    return [pscustomobject]@{
        Ready = $chrome.Found -and $profileIsEligible -and $hasGeminiSidePanelState
        Chrome = $chrome
        Reasons = $reasons.ToArray()
    }
}

function Show-GeminiSetupWarning {
    param(
        [Parameter(Mandatory)]
        $SetupResult
    )

    Add-Type -AssemblyName System.Windows.Forms
    Add-Type -AssemblyName System.Drawing

    $form = New-Object System.Windows.Forms.Form
    $form.Text = 'Gemini in Chrome setup not confirmed'
    $form.StartPosition = 'CenterScreen'
    $form.FormBorderStyle = 'FixedDialog'
    $form.MaximizeBox = $false
    $form.MinimizeBox = $false
    $form.ClientSize = New-Object System.Drawing.Size(620, 360)

    $label = New-Object System.Windows.Forms.Label
    $label.AutoSize = $false
    $label.Location = New-Object System.Drawing.Point(14, 14)
    $label.Size = New-Object System.Drawing.Size(590, 190)
    $label.Text = "Gemini for Chrome Task Manager uses the official Gemini side panel in Chrome. The installer could not confirm that Gemini in Chrome is ready on this Windows profile.`r`n`r`nChecks:`r`n- " + ($SetupResult.Reasons -join "`r`n- ") + "`r`n`r`nYou can open Google's official setup/help page, cancel installation, or continue anyway."
    $form.Controls.Add($label)

    $checkbox = New-Object System.Windows.Forms.CheckBox
    $checkbox.Location = New-Object System.Drawing.Point(17, 225)
    $checkbox.Size = New-Object System.Drawing.Size(560, 24)
    $checkbox.Text = 'I understand this Application may not work'
    $form.Controls.Add($checkbox)

    $openHelpButton = New-Object System.Windows.Forms.Button
    $openHelpButton.Location = New-Object System.Drawing.Point(17, 285)
    $openHelpButton.Size = New-Object System.Drawing.Size(170, 30)
    $openHelpButton.Text = 'Open Official Setup Help'
    $openHelpButton.Add_Click({ Start-Process $OfficialGeminiChromeHelpUrl })
    $form.Controls.Add($openHelpButton)

    $continueButton = New-Object System.Windows.Forms.Button
    $continueButton.Location = New-Object System.Drawing.Point(365, 285)
    $continueButton.Size = New-Object System.Drawing.Size(115, 30)
    $continueButton.Text = 'Continue Anyway'
    $continueButton.Enabled = $false
    $continueButton.DialogResult = [System.Windows.Forms.DialogResult]::OK
    $form.Controls.Add($continueButton)

    $cancelButton = New-Object System.Windows.Forms.Button
    $cancelButton.Location = New-Object System.Drawing.Point(490, 285)
    $cancelButton.Size = New-Object System.Drawing.Size(115, 30)
    $cancelButton.Text = 'Cancel Install'
    $cancelButton.DialogResult = [System.Windows.Forms.DialogResult]::Cancel
    $form.Controls.Add($cancelButton)

    $checkbox.Add_CheckedChanged({ $continueButton.Enabled = $checkbox.Checked })
    $form.AcceptButton = $continueButton
    $form.CancelButton = $cancelButton

    return $form.ShowDialog()
}

function Confirm-GeminiInChromeSetup {
    $setupResult = Test-GeminiInChromeSetup

    if ($setupResult.Ready) {
        Write-Host 'Gemini in Chrome setup check passed.'
        return
    }

    Write-Host 'Gemini in Chrome setup could not be confirmed.'

    foreach ($reason in $setupResult.Reasons) {
        Write-Host ('- {0}' -f $reason)
    }

    $dialogResult = Show-GeminiSetupWarning -SetupResult $setupResult

    if ($dialogResult -ne [System.Windows.Forms.DialogResult]::OK) {
        throw 'Installation cancelled because Gemini in Chrome setup was not confirmed.'
    }
}

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

Confirm-GeminiInChromeSetup
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
