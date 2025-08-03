#===============================================================================
#==                          OLED - Startup Task                              ==
#===============================================================================
#
# Description:
#   This script validates the monitor setup, then reads the saved configuration 
#   and launches the AHK scripts non-interactively. The AHK scripts are
#   responsible for restoring brightness from the previous session.
#
#===============================================================================

# --- Function to show an error message box ---
function Show-ErrorPopup {
    param ([string]$Message)
    Add-Type -AssemblyName System.Windows.Forms
    [System.Windows.Forms.MessageBox]::Show($Message, "OLED Startup Task Error", "OK", "Error")
}

# --- Path Definitions ---
$scriptRoot = $PSScriptRoot
$projectRoot = (Get-Item $scriptRoot).Parent.FullName

$sleeperAhkPath = Join-Path -Path $scriptRoot -ChildPath "OLED-Sleeper.ahk"
$dimmerAhkPath = Join-Path -Path $scriptRoot -ChildPath "OLED-Dimmer.ahk"
$startupConfigFilePath = Join-Path -Path $projectRoot -ChildPath "config\Startup.config.psd1"
$multiMonitorToolPath = Join-Path -Path $projectRoot -ChildPath "tools\MultiMonitorTool\MultiMonitorTool.exe"
$controlMyMonitorPath = Join-Path -Path $projectRoot -ChildPath "tools\ControlMyMonitor\ControlMyMonitor.exe"
$tempCsvPath = Join-Path -Path $env:TEMP -ChildPath "monitors_startup.csv"
$shortcutPath = Join-Path -Path ([System.Environment]::GetFolderPath('Startup')) -ChildPath "OLED Startup Task.lnk"

# --- Function to handle fatal errors by cleaning up and exiting ---
function Handle-FatalError {
    param ([string]$ErrorMessage)
    
    $fullMessage = $ErrorMessage + [Environment]::NewLine + [Environment]::NewLine + "The startup task has been automatically removed to prevent future errors. Please run Configure.ps1 to set it up again."
    Show-ErrorPopup -Message $fullMessage

    # --- Automatically remove the invalid startup task and config ---
    try {
        if (Test-Path -Path $shortcutPath) {
            Remove-Item -Path $shortcutPath -Force
        }
        $configDirectory = Split-Path -Path $startupConfigFilePath -Parent
        if (Test-Path -Path $configDirectory) {
            Remove-Item -Path $configDirectory -Recurse -Force
        }
    }
    catch {
        # If removal fails, there's not much more we can do. The user has been notified.
    }
    
    exit
}


#=================================================================
#==                      PREREQUISITE CHECKS                    ==
#=================================================================
if (-not (Test-Path -Path $startupConfigFilePath)) {
    Handle-FatalError -ErrorMessage "Startup configuration file not found: $startupConfigFilePath."
    exit
}
if (-not (Test-Path -Path $multiMonitorToolPath)) {
    Handle-FatalError -ErrorMessage "Dependency not found: MultiMonitorTool.exe`nPlease ensure the 'tools' directory is intact."
}
if (-not (Test-Path -Path $controlMyMonitorPath)) {
    Handle-FatalError -ErrorMessage "Dependency not found: ControlMyMonitor.exe`nPlease ensure the 'tools' directory is intact."
}
if (-not (Test-Path -Path $sleeperAhkPath)) {
    Handle-FatalError -ErrorMessage "Dependency not found: OLED-Sleeper.ahk`nPlease ensure all script files are in the 'src' directory."
}
if (-not (Test-Path -Path $dimmerAhkPath)) {
    Handle-FatalError -ErrorMessage "Dependency not found: OLED-Dimmer.ahk`nPlease ensure all script files are in the 'src' directory."
}


# --- Load Configuration ---
# Use Import-Clixml to read the rich object data
$config = Import-Clixml -Path $startupConfigFilePath

#=================================================================
#==                      MONITOR VALIDATION                     ==
#=================================================================

# --- Get current monitor state ---
Start-Process -FilePath $multiMonitorToolPath -ArgumentList "/scomma `"$tempCsvPath`"" -Wait -NoNewWindow
if (-not (Test-Path -Path $tempCsvPath)) {
    Handle-FatalError -ErrorMessage "Failed to get current monitor information using MultiMonitorTool.exe."
}
$currentMonitors = Import-Csv -Path $tempCsvPath
Remove-Item -Path $tempCsvPath -Force

# --- Compare stored monitors with current monitors ---
$activeMonitorIDs = $currentMonitors | Where-Object { $_.Active -eq 'Yes' } | Select-Object -ExpandProperty 'Monitor ID'
$storedMonitors = $config.BlackoutMonitors + $config.DimmerMonitors
$missingMonitors = @()

foreach ($storedMonitor in $storedMonitors) {
    if ($storedMonitor.MonitorID -notin $activeMonitorIDs) {
        $missingMonitors += $storedMonitor.MonitorID
    }
}

# --- If monitors are missing, show error, delete the invalid task, and exit ---
if ($missingMonitors.Count -gt 0) {
    $errorMessage = "Monitor configuration has changed. The following configured monitors were not found:" + [Environment]::NewLine + [Environment]::NewLine
    $errorMessage += $missingMonitors -join [Environment]::NewLine
    Handle-FatalError -ErrorMessage $errorMessage
}


#=================================================================
#==                        LAUNCH SCRIPTS                       ==
#=================================================================

# --- Terminate Existing AHK Scripts ---
try {
    $ahkProcesses = Get-Process | Where-Object { $_.ProcessName -like 'AutoHotkey*' }
    if ($ahkProcesses) {
        foreach ($proc in $ahkProcesses) {
            $cimProc = Get-CimInstance -ClassName Win32_Process -Filter "ProcessId = $($proc.Id)"
            if ($cimProc.CommandLine -like "*OLED-Sleeper.ahk*" -or $cimProc.CommandLine -like "*OLED-Dimmer.ahk*") {
                if ($proc.CloseMainWindow()) {
                    $proc.WaitForExit(2000)
                }
                if (-not $proc.HasExited) {
                    Stop-Process -Id $proc.Id -Force
                }
            }
        }
    }
}
catch {
    # Silently continue if this fails.
}

# --- Launch Processes based on Config ---
# The AHK scripts will now handle restoring brightness upon activity
if ($config.BlackoutMonitors) {
    $blackoutString = ($config.BlackoutMonitors.Name) -join ';'
    Start-Process -FilePath $sleeperAhkPath -ArgumentList """$blackoutString""", "$($config.IdleTimeMS)"
}

if ($config.DimmerMonitors) {
    $dimmerString = ($config.DimmerMonitors | ForEach-Object { "$($_.Name):$($_.DimLevel)" }) -join ';'
    Start-Process -FilePath $dimmerAhkPath -ArgumentList """$dimmerString""", "$($config.IdleTimeMS)"
}

# The script will now exit. The AHK processes will continue running in the background.
