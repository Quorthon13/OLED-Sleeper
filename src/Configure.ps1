#
#===============================================================================
# ==                    OLED Sleeper - Configuration Wizard                    ==
#===============================================================================
#
# Description:
#   This PowerShell script serves as an interactive setup wizard for the
#   OLED Sleeper and OLED Dimmer AutoHotkey scripts.
#
# Dependencies:
#   - ..\tools\MultiMonitorTool\MultiMonitorTool.exe
#   - ..\tools\ControlMyMonitor\ControlMyMonitor.exe
#   - OLED-Sleeper.ahk (in the same src directory)
#   - OLED-Dimmer.ahk (in the same src directory)
#
#===============================================================================

# Set the output encoding to UTF-8 to ensure proper display of special
# characters, including monitor names and emojis.
$OutputEncoding = [System.Text.Encoding]::UTF8

#=================================================================
# ==                   FUNCTION DEFINITIONS                    ==
#=================================================================

function Show-MonitorDetails {
    param ([Parameter(Mandatory = $true)][PSObject]$MonitorObject)
    if ([string]::IsNullOrWhiteSpace($MonitorObject.'Monitor Name')) {
        $MonitorObject.'Monitor Name' = $MonitorObject.'Name'
    }
    Write-Host "Monitor Found: " -NoNewline
    Write-Host $MonitorObject.'Monitor Name' -ForegroundColor Green
    Write-Host "   - Details: $($MonitorObject.Resolution) at position $($MonitorObject.'Left-Top')" -ForegroundColor White
    Write-Host "   - ID: " -NoNewline
    Write-Host $MonitorObject.'Name' -ForegroundColor Gray
    Write-Host ""
}

#=================================================================
# ==                       INITIALIZATION                        ==
#=================================================================

# --- Path Definitions ---
$scriptRoot = $PSScriptRoot
$projectRoot = (Get-Item $scriptRoot).Parent.FullName

$multiMonitorToolPath = Join-Path -Path $projectRoot -ChildPath "tools\MultiMonitorTool\MultiMonitorTool.exe"
$controlMyMonitorPath = Join-Path -Path $projectRoot -ChildPath "tools\ControlMyMonitor\ControlMyMonitor.exe"
$sleeperAhkPath = Join-Path -Path $scriptRoot -ChildPath "OLED-Sleeper.ahk"
$dimmerAhkPath = Join-Path -Path $scriptRoot -ChildPath "OLED-Dimmer.ahk"
$csvPath = Join-Path -Path $projectRoot -ChildPath "monitors.csv"

# --- Data Variables ---
$managedMonitors = New-Object System.Collections.ArrayList
$blackoutMonitors = New-Object System.Collections.ArrayList
$dimmerMonitors = New-Object System.Collections.ArrayList
$time_min = $null
$time_ms = $null

# --- Prerequisite Checks ---
if (-not (Test-Path -Path $multiMonitorToolPath)) { Write-Host "ERROR: MultiMonitorTool.exe not found." -ForegroundColor Red; Read-Host; exit }
if (-not (Test-Path -Path $controlMyMonitorPath)) { Write-Host "ERROR: ControlMyMonitor.exe not found." -ForegroundColor Red; Read-Host; exit }
if (-not (Test-Path -Path $sleeperAhkPath)) { Write-Host "ERROR: OLED-Sleeper.ahk not found." -ForegroundColor Red; Read-Host; exit }
if (-not (Test-Path -Path $dimmerAhkPath)) { Write-Host "ERROR: OLED-Dimmer.ahk not found." -ForegroundColor Red; Read-Host; exit }

# --- Data Gathering ---
Start-Process -FilePath $multiMonitorToolPath -ArgumentList "/scomma `"$csvPath`"" -Wait
if (-not (Test-Path -Path $csvPath)) { Write-Host "ERROR: monitors.csv not found." -ForegroundColor Red; Read-Host; exit }
$monitors = Import-Csv -Path $csvPath
$activeMonitors = $monitors | Where-Object { $_.Active -eq 'Yes' }
if ($activeMonitors.Count -lt 1) { Write-Host "ERROR: No active monitors were detected." -ForegroundColor Red; Read-Host; if (Test-Path -Path $csvPath) { Remove-Item -Path $csvPath }; exit }

#=================================================================
# ==                  MONITOR & ACTION SETUP                     ==
#=================================================================

# --- Step 1: Select which monitors to manage ---
Clear-Host
Write-Host "--- Step 1: Select Monitors to Manage ---" -ForegroundColor Cyan
foreach ($monitor in $activeMonitors) {
    Show-MonitorDetails -MonitorObject $monitor
    $shouldManage = Read-Host "Manage this monitor? (y/n)"
    if ($shouldManage.ToLower() -eq 'y') {
        [void]$managedMonitors.Add($monitor)
    }
    Write-Host "-------------------------------------------------" -ForegroundColor Cyan
}

if ($managedMonitors.Count -eq 0) {
    Write-Host "No monitors were selected. Aborting script." -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit
}

# --- Step 2: Choose an action for each selected monitor ---
Clear-Host
Write-Host "--- Step 2: Configure Actions for Selected Monitors ---" -ForegroundColor Cyan
foreach ($monitor in $managedMonitors) {
    Write-Host "Configuring action for: $($monitor.'Monitor Name')" -ForegroundColor Green
    $action = Read-Host "Action? (1 = Blackout, 2 = Dim)"
    
    if ($action -eq '1') {
        [void]$blackoutMonitors.Add($monitor.Name)
        Write-Host "  -> Set to BLACKOUT." -ForegroundColor White
    }
    elseif ($action -eq '2') {
        while ($true) {
            $dimLevel = 0  
            $dimLevelStr = Read-Host "   Enter brightness level (0-100)"
            if ([int]::TryParse($dimLevelStr, [ref]$dimLevel) -and $dimLevel -ge 0 -and $dimLevel -le 100) {
                $dimInfo = "$($monitor.Name):$dimLevel"
                [void]$dimmerMonitors.Add($dimInfo)
                Write-Host "  -> Set to DIM to $dimLevel%." -ForegroundColor White
                break
            }
            else {
                Write-Host "   Invalid input. Please enter a number between 0 and 100." -ForegroundColor Red
            }
        }

    }
    else {
        Write-Host "  -> Invalid choice. Skipping monitor." -ForegroundColor Red
    }
    Write-Host "-------------------------------------------------" -ForegroundColor Cyan
}

#=================================================================
# ==                        TIME INPUT                         ==
#=================================================================

Clear-Host
Write-Host "--- Step 3: Set Idle Timer ---" -ForegroundColor Cyan
Write-Host "A single timer will be used for all actions."
while ($true) {
    $time_min_str = Read-Host "Enter the idle time in minutes (e.g., 30, 1.5)"
    $culture = [System.Globalization.CultureInfo]::InvariantCulture
    if ([double]::TryParse($time_min_str, [System.Globalization.NumberStyles]::Float, $culture, [ref]$time_min)) {
        $time_ms = [math]::Round($time_min * 60000)
        break
    }
    else {
        Write-Host "Invalid input. Please enter a number." -ForegroundColor Red
    }
}

#=================================================================
# ==                       FINAL ACTION                        ==
#=================================================================

Clear-Host
Write-Host "Setup Complete!" -ForegroundColor Green
$checkEmoji = [System.Char]::ConvertFromUtf32(0x2705)
$launchedSomething = $false

# --- Launch Blackout Script if needed ---
if ($blackoutMonitors.Count -gt 0) {
    $blackoutString = $blackoutMonitors -join ';'
    Start-Process -FilePath $sleeperAhkPath -ArgumentList """$blackoutString""", "$time_ms"
    Write-Host "$checkEmoji Blackout watcher started for $($blackoutMonitors.Count) monitor(s)." -ForegroundColor Green
    $launchedSomething = $true
}

# --- Launch Dimmer Script if needed ---
if ($dimmerMonitors.Count -gt 0) {
    $dimmerString = $dimmerMonitors -join ';'
    Start-Process -FilePath $dimmerAhkPath -ArgumentList """$dimmerString""", "$time_ms"
    Write-Host "$checkEmoji Dimmer watcher started for $($dimmerMonitors.Count) monitor(s)." -ForegroundColor Green
    $launchedSomething = $true
}

if (-not $launchedSomething) {
    Write-Host "No valid actions were configured. Nothing to start." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "This window will close automatically in 10 seconds."
Start-Sleep -Seconds 10

#=================================================================
# ==                         CLEANUP                           ==
#=================================================================
if (Test-Path -Path $csvPath) {
    Remove-Item -Path $csvPath
}