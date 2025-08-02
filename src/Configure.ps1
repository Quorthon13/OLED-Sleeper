#
#===============================================================================
# ==                    OLED Sleeper - Configuration Wizard                    ==
#===============================================================================
#
# Description:
#   This PowerShell script serves as an interactive setup wizard for
#   'OLED-Sleeper.ahk', the AutoHotkey script. It guides the user through a
#   three-step process:
#     1. It uses NirSoft's MultiMonitorTool to detect all active monitors.
#     2. It prompts the user to select which of these monitors should be managed.
#     3. It asks for an idle-time duration in minutes.
#
#   Finally, it launches 'OLED-Sleeper.ahk' in the background with the
#   selected monitors and timer settings as command-line arguments.
#
# Dependencies:
#   - ..\tools\MultiMonitorTool.exe
#   - OLED-Sleeper.ahk (in the same src directory)
#
#===============================================================================

# Set the output encoding to UTF-8 to ensure proper display of special
# characters, including monitor names and emojis.
$OutputEncoding = [System.Text.Encoding]::UTF8

#=================================================================
# ==                   FUNCTION DEFINITIONS                    ==
#=================================================================

# Displays the details of a given monitor object in a clean, readable format.
# This helps the user easily identify each monitor during the selection process.
function Show-MonitorDetails {
    param (
        [Parameter(Mandatory=$true)]
        [PSObject]$MonitorObject
    )

    # Some monitors might have a 'Monitor Name' while others only have a 'Name' (Device ID).
    # This ensures we always display the most descriptive name available.
    $modelName = $MonitorObject.'Monitor Name'
    if ([string]::IsNullOrWhiteSpace($modelName)) {
        $modelName = $MonitorObject.'Name'
    }

    Write-Host "Monitor Found: " -NoNewline
    Write-Host $modelName -ForegroundColor Green

    Write-Host "   - Details: $($MonitorObject.Resolution) at position $($MonitorObject.'Left-Top')" -ForegroundColor White
    Write-Host "   - ID: " -NoNewline
    Write-Host $MonitorObject.'Name' -ForegroundColor Gray
    Write-Host ""
}

#=================================================================
# ==                       INITIALIZATION                        ==
#=================================================================

# --- Path Definitions ---
# Use the script's own location to build robust paths.
# $PSScriptRoot is the 'src' directory.
# $projectRoot is the parent directory (the repository root).
$scriptRoot = $PSScriptRoot
$projectRoot = (Get-Item $scriptRoot).Parent.FullName

# Define paths for all dependencies relative to the project structure.
$multiMonitorToolPath = Join-Path -Path $projectRoot -ChildPath "tools\MultiMonitorTool.exe"
$ahkScriptPath        = Join-Path -Path $scriptRoot -ChildPath "OLED-Sleeper.ahk"
$csvPath              = Join-Path -Path $projectRoot -ChildPath "monitors.csv"

# Initialize variables to store user selections.
$secondaryIDs = New-Object System.Collections.ArrayList
$time_min = $null
$time_ms = $null

# --- Prerequisite Checks ---
# The script cannot run without its core components. These checks ensure all
# dependencies are present before proceeding.
if (-not (Test-Path -Path $multiMonitorToolPath)) {
    Write-Host "ERROR: MultiMonitorTool.exe not found in the tools directory." -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit
}
if (-not (Test-Path -Path $ahkScriptPath)) {
    Write-Host "ERROR: The AHK script ($ahkScriptPath) was not found." -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit
}

# --- Data Gathering ---
# Run MultiMonitorTool silently to export all monitor information into a CSV file.
# The -Wait flag ensures PowerShell waits for this command to finish before continuing.
Start-Process -FilePath $multiMonitorToolPath -ArgumentList "/scomma `"$csvPath`"" -Wait

# Verify that the CSV file was created successfully.
if (-not (Test-Path -Path $csvPath)) {
    Write-Host "ERROR: monitors.csv not found. Make sure MultiMonitorTool.exe is working." -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit
}

# Import the monitor data from the CSV and filter for only 'Active' monitors.
$monitors = Import-Csv -Path $csvPath
$activeMonitors = $monitors | Where-Object { $_.Active -eq 'Yes' }

# If no active monitors are found, there's nothing to manage. Exit gracefully.
if ($activeMonitors.Count -lt 1) {
    Write-Host "ERROR: No active monitors were detected." -ForegroundColor Red
    Read-Host "Press Enter to exit"
    if (Test-Path -Path $csvPath) {
        Remove-Item -Path $csvPath
    }
    exit
}

#=================================================================
# ==            MONITOR SELECTION (Multi-Select)               ==
#=================================================================

Clear-Host
Write-Host "--- Step 1: Select Monitors to Manage ---" -ForegroundColor Cyan
Write-Host "Select one or more monitors to be managed by the script."
Write-Host "These monitors will be blacked out when idle."
Write-Host ""

# Iterate through each active monitor and ask the user if they want to manage it.
foreach ($monitor in $activeMonitors) {
    Show-MonitorDetails -MonitorObject $monitor

    Write-Host "Manage this monitor? (y/n): " -ForegroundColor Yellow -NoNewline
    $shouldManage = Read-Host

    # If the user answers 'y', add the monitor's device ID to our list.
    if ($shouldManage.ToLower() -eq 'y') {
        [void]$secondaryIDs.Add($monitor.'Name')
        Write-Host "  -> Added '$($monitor.'Monitor Name')' to the list." -ForegroundColor Green
    }
    Write-Host "-------------------------------------------------" -ForegroundColor Cyan
}

# If the user didn't select any monitors, abort the script.
if ($secondaryIDs.Count -eq 0) {
    Write-Host "No monitors were selected. Aborting script." -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit
}

#=================================================================
# ==                        TIME INPUT                         ==
#=================================================================

Clear-Host
Write-Host "--- Step 2: Set Idle Timer ---" -ForegroundColor Cyan
Write-Host ""
Write-Host "Monitors selected. Now enter the sleep timer duration."
Write-Host ""

# Loop indefinitely until the user provides a valid number for the time.
while ($true) {
    Write-Host "Enter the idle time in minutes (e.g., 30, 1.5, 0.5): " -ForegroundColor Yellow -NoNewline
    $time_min_str = Read-Host

    # Use a culture-invariant parser to handle both '.' and ',' as decimal separators.
    $culture = [System.Globalization.CultureInfo]::InvariantCulture
    if ([double]::TryParse($time_min_str, [System.Globalization.NumberStyles]::Float, $culture, [ref]$time_min)) {
        # Convert the user's input from minutes to milliseconds for the AHK script.
        $time_ms = [math]::Round($time_min * 60000)
        break # Exit the loop on valid input.
    } else {
        Write-Host "Invalid input. Please enter a number." -ForegroundColor Red
    }
}

#=================================================================
# ==                       FINAL ACTION                        ==
#=================================================================

Clear-Host
Write-Host "Setup Complete!" -ForegroundColor Green
Write-Host ""

# Join the list of monitor IDs into a single semicolon-separated string,
# which is the format expected by the AHK script's command-line arguments.
$secondaryIDsString = $secondaryIDs -join ';'

# --- Confirmation Summary ---
# Display a summary of the selected settings to the user.
Write-Host "MONITORS TO MANAGE: " -ForegroundColor Cyan
foreach ($id in $secondaryIDs) {
    Write-Host "   - $id" -ForegroundColor White
}

Write-Host "TIMER:                 " -ForegroundColor Cyan -NoNewline
Write-Host "$time_min minutes ($time_ms ms)" -ForegroundColor White
Write-Host ""

# Define the checkmark emoji for the success message.
$checkEmoji = [System.Char]::ConvertFromUtf32(0x2705)

# --- Launch Background Process ---
# Start the AHK script, passing the monitor IDs and the timer in milliseconds
# as two separate arguments.
Start-Process -FilePath $ahkScriptPath -ArgumentList """$secondaryIDsString""", "$time_ms"

Write-Host "$checkEmoji Monitor watcher started in the background." -ForegroundColor Green
Write-Host "This window will close automatically in 10 seconds, or you can close it manually." -ForegroundColor White
Start-Sleep -Seconds 10

#=================================================================
# ==                         CLEANUP                           ==
#=================================================================

# Remove the temporary CSV file generated by MultiMonitorTool.
if (Test-Path -Path $csvPath) {
    Remove-Item -Path $csvPath
}