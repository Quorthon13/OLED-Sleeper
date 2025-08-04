#===============================================================================
#==                      OLED Sleeper - Configuration Wizard                  ==
#===============================================================================
#
# Description:
#   This PowerShell script serves as an interactive setup wizard for the
#   OLED Sleeper and OLED Dimmer AutoHotkey scripts. It can configure the
#   scripts to run on startup or remove a previously created startup task.
#
# Dependencies:
#   - ..\tools\MultiMonitorTool\MultiMonitorTool.exe
#   - ..\tools\ControlMyMonitor\ControlMyMonitor.exe
#   - OLED-Sleeper.ahk (in the same src directory)
#   - OLED-Dimmer.ahk (in the same src directory)
#   - Startup-Task.ps1 (in the same src directory)
#
#===============================================================================

# Set the output encoding to UTF-8 to ensure proper display of special
# characters, including monitor names and emojis.
$OutputEncoding = [System.Text.Encoding]::UTF8

#=================================================================
#==                      FUNCTION DEFINITIONS                   ==
#=================================================================

function Show-MonitorDetails {
    param ([Parameter(Mandatory = $true)][PSObject]$MonitorObject)

    # If the friendly 'Monitor Name' is blank, fall back to the system 'Name' (e.g., \\.\DISPLAY1)
    if ([string]::IsNullOrWhiteSpace($MonitorObject.'Monitor Name')) {
        $MonitorObject.'Monitor Name' = $MonitorObject.'Name'
    }

    # Use gray for labels and distinct colors for the values for better readability.
    Write-Host "Monitor Found: " -ForegroundColor Gray -NoNewline
    Write-Host $MonitorObject.'Monitor Name' -ForegroundColor Green

    Write-Host "   - Details: " -ForegroundColor Gray -NoNewline
    Write-Host "$($MonitorObject.Resolution) at position $($MonitorObject.'Left-Top')" -ForegroundColor White

    Write-Host "   - ID: " -ForegroundColor Gray -NoNewline
    Write-Host $MonitorObject.'Name' -ForegroundColor White
    
    Write-Host "" # Adds a blank line for spacing after each monitor
}


#=================================================================
#==                       INITIALIZATION                        ==
#=================================================================

# --- Path Definitions ---
$scriptRoot = $PSScriptRoot
$projectRoot = (Get-Item $scriptRoot).Parent.FullName

$multiMonitorToolPath = Join-Path -Path $projectRoot -ChildPath "tools\MultiMonitorTool\MultiMonitorTool.exe"
$controlMyMonitorPath = Join-Path -Path $projectRoot -ChildPath "tools\ControlMyMonitor\ControlMyMonitor.exe"
$sleeperAhkPath = Join-Path -Path $scriptRoot -ChildPath "OLED-Sleeper.ahk"
$dimmerAhkPath = Join-Path -Path $scriptRoot -ChildPath "OLED-Dimmer.ahk"
$csvPath = Join-Path -Path $projectRoot -ChildPath "monitors.csv"

# --- Startup Script and Config Paths ---
$startupTaskScriptPath = Join-Path -Path $scriptRoot -ChildPath "Startup-Task.ps1"
$startupConfigFilePath = Join-Path -Path $projectRoot -ChildPath "config\Startup.config.psd1"
$shortcutPath = Join-Path -Path ([System.Environment]::GetFolderPath('Startup')) -ChildPath "OLED Startup Task.lnk"

# --- Emoji Definitions ---
$checkEmoji = [System.Char]::ConvertFromUtf32(0x2705)      # ✅
$crossEmoji = [System.Char]::ConvertFromUtf32(0x274C)      # ❌
$blackoutEmoji = [System.Char]::ConvertFromUtf32(0x1F31A)  # 🌚
$dimmerEmoji = [System.Char]::ConvertFromUtf32(0x1F506)    # 🔆

# --- Data Variables ---
$managedMonitors = New-Object System.Collections.ArrayList
$blackoutMonitors = New-Object System.Collections.ArrayList
$dimmerMonitors = New-Object System.Collections.ArrayList
$time_min = $null
$time_ms = $null

#=================================================================
#==                           MAIN MENU                         ==
#=================================================================
Clear-Host
Write-Host "--- OLED Sleeper & Dimmer Setup ---" -ForegroundColor Cyan
Write-Host "1. Configure / Update Startup Task"
Write-Host "2. Remove Startup Task"
Write-Host "3. Exit"
$mainChoice = Read-Host "`nPlease choose an option"

if ($mainChoice -eq '2') {
    # --- REMOVAL LOGIC ---
    Write-Host "`n--- Removing Startup Task & Configuration ---" -ForegroundColor Cyan
    $itemRemoved = $false
    
    # Remove the startup shortcut
    if (Test-Path -Path $shortcutPath) {
        try {
            Remove-Item -Path $shortcutPath -Force
            Write-Host "$checkEmoji Startup shortcut has been successfully deleted." -ForegroundColor Green
            $itemRemoved = $true
        }
        catch {
            Write-Host "$crossEmoji Failed to delete startup shortcut: $($_.Exception.Message)" -ForegroundColor Red
        }
    }

    # Remove the configuration directory automatically
    $configDirectory = Split-Path -Path $startupConfigFilePath -Parent
    if (Test-Path -Path $configDirectory) {
        try {
            Remove-Item -Path $configDirectory -Recurse -Force
            Write-Host "$checkEmoji Associated configuration directory has been deleted." -ForegroundColor Green
            $itemRemoved = $true
        }
        catch {
            Write-Host "$crossEmoji Failed to delete config directory: $($_.Exception.Message)" -ForegroundColor Red
        }
    }

    if (-not $itemRemoved) {
        Write-Host "No startup task or configuration was found. Nothing to remove." -ForegroundColor Yellow
    }

    Read-Host "`nPress Enter to exit."
    exit
}
elseif ($mainChoice -ne '1') {
    # Exit if the user chooses option 3 or any other invalid input
    exit
}

# --- Continue to configuration if option 1 was chosen ---

#=================================================================
#==                        CONFIGURATION                        ==
#=================================================================

# --- Prerequisite Checks ---
if (-not (Test-Path -Path $multiMonitorToolPath)) { Write-Host "ERROR: MultiMonitorTool.exe not found." -ForegroundColor Red; Read-Host; exit }
if (-not (Test-Path -Path $controlMyMonitorPath)) { Write-Host "ERROR: ControlMyMonitor.exe not found." -ForegroundColor Red; Read-Host; exit }
if (-not (Test-Path -Path $sleeperAhkPath)) { Write-Host "ERROR: OLED-Sleeper.ahk not found." -ForegroundColor Red; Read-Host; exit }
if (-not (Test-Path -Path $dimmerAhkPath)) { Write-Host "ERROR: OLED-Dimmer.ahk not found." -ForegroundColor Red; Read-Host; exit }
if (-not (Test-Path -Path $startupTaskScriptPath)) { Write-Host "ERROR: Startup-Task.ps1 not found." -ForegroundColor Red; Read-Host; exit }

# --- Data Gathering ---
Start-Process -FilePath $multiMonitorToolPath -ArgumentList "/scomma `"$csvPath`"" -Wait
if (-not (Test-Path -Path $csvPath)) { Write-Host "ERROR: monitors.csv not found." -ForegroundColor Red; Read-Host; exit }
$monitors = Import-Csv -Path $csvPath
$activeMonitors = $monitors | Where-Object { $_.Active -eq 'Yes' }
if ($activeMonitors.Count -lt 1) { Write-Host "ERROR: No active monitors were detected." -ForegroundColor Red; Read-Host; if (Test-Path -Path $csvPath) { Remove-Item -Path $csvPath }; exit }

#=================================================================
#==                      MONITOR & ACTION SETUP                 ==
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
Write-Host ""
Write-Host "$blackoutEmoji Blackout Mode: " -ForegroundColor Gray -NoNewline
Write-Host "Covers screen with a black overlay (ideal for OLEDs)." -ForegroundColor White
Write-Host "$dimmerEmoji Dimmer Mode:  " -ForegroundColor Gray -NoNewline
Write-Host "Lowers monitor brightness via DDC/CI." -ForegroundColor White
Write-Host ""
Write-Host "-------------------------------------------------" -ForegroundColor Cyan

foreach ($monitor in $managedMonitors) {
    Write-Host "Configuring action for: " -ForegroundColor Gray -NoNewline
    Write-Host "$($monitor.'Monitor Name')" -ForegroundColor Green
    
    $action = Read-Host "Action? (1 = Blackout, 2 = Dim)"
    
    if ($action -eq '1') {
        # Store an object with both Name and Monitor ID
        $monitorInfo = [PSCustomObject]@{
            Name = $monitor.Name
            MonitorID = $monitor.'Monitor ID'
        }
        [void]$blackoutMonitors.Add($monitorInfo)
        Write-Host "  -> " -ForegroundColor Gray -NoNewline
        Write-Host "$blackoutEmoji Set to BLACKOUT." -ForegroundColor White
    }
    elseif ($action -eq '2') {
        while ($true) {
            $dimLevel = 0  
            $dimLevelStr = Read-Host "   Enter brightness level (0-100)"
            if ([int]::TryParse($dimLevelStr, [ref]$dimLevel) -and $dimLevel -ge 0 -and $dimLevel -le 100) {
                # Store an object with Name, Monitor ID, and Dim Level
                $monitorInfo = [PSCustomObject]@{
                    Name = $monitor.Name
                    MonitorID = $monitor.'Monitor ID'
                    DimLevel = $dimLevel
                }
                [void]$dimmerMonitors.Add($monitorInfo)
                Write-Host "  -> " -ForegroundColor Gray -NoNewline
                Write-Host "$dimmerEmoji Set to DIM to $dimLevel%." -ForegroundColor White
                break
            }
            else {
                Write-Host "   Invalid input. Please enter a number between 0 and 100." -ForegroundColor Red
            }
        }
    }
    else {
        Write-Host "  -> " -ForegroundColor Gray -NoNewline
        Write-Host "Invalid choice. Skipping monitor." -ForegroundColor Red
    }
    Write-Host "-------------------------------------------------" -ForegroundColor Cyan
}

#=================================================================
#==                           TIME INPUT                        ==
#=================================================================

Clear-Host
Write-Host "--- Step 3: Set Idle Timer ---" -ForegroundColor Cyan
Write-Host "A single timer will be used for all actions." -ForegroundColor White
while ($true) {
    # We use Write-Host with -NoNewline to color the prompt for Read-Host.
    Write-Host "Enter the idle time in minutes (e.g., 30, 1.5): " -NoNewline -ForegroundColor Green
    $time_min_str = Read-Host

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
#==                           FINAL ACTION                      ==
#=================================================================

# --- Step 4: Finalize Setup ---
Clear-Host
Write-Host "--- Step 4: Finalize Setup ---" -ForegroundColor Cyan

$setupStartup = Read-Host "Set this configuration to run automatically when Windows starts? (y/n)"
if ($setupStartup.ToLower() -eq 'y') {
    # If yes, we save the config file AND create the startup shortcut.
    
    # --- Save the configuration file ---
    $configDirectory = Split-Path -Path $startupConfigFilePath -Parent
    if (-not (Test-Path -Path $configDirectory)) {
        New-Item -Path $configDirectory -ItemType Directory | Out-Null
    }

    # Create a hashtable to hold the configuration data
    $configData = @{
        BlackoutMonitors = $blackoutMonitors
        DimmerMonitors   = $dimmerMonitors
        IdleTimeMS       = $time_ms
    }
    
    # Export the rich object data using Export-Clixml
    $configData | Export-Clixml -Path $startupConfigFilePath

    Write-Host "`n$checkEmoji Configuration saved to `"$startupConfigFilePath`"." -ForegroundColor Green

    # --- Create the startup shortcut ---
    try {
        $wshell = New-Object -ComObject WScript.Shell
        $shortcut = $wshell.CreateShortcut($shortcutPath)

        $shortcut.TargetPath = "powershell.exe"
        $shortcut.Arguments = "-ExecutionPolicy Bypass -WindowStyle Hidden -File `"$startupTaskScriptPath`""
        $shortcut.WorkingDirectory = $scriptRoot
        $shortcut.IconLocation = "powershell.exe,0"
        $shortcut.Description = "Launches the OLED Dimmer/Sleeper scripts."
        
        $shortcut.Save()

        Write-Host "$checkEmoji Shortcut created in your Startup folder. The task will run on next login." -ForegroundColor Green
    }
    catch {
        Write-Host "$crossEmoji Failed to create startup shortcut: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# --- Terminate Existing AHK Scripts ---
try {
    # Get all processes that could be our AHK scripts
    $ahkProcesses = Get-Process | Where-Object { $_.ProcessName -like 'AutoHotkey*' }
    
    if ($ahkProcesses) {
        Write-Host "`n--- Gracefully closing existing script instances... ---" -ForegroundColor Cyan
        foreach ($proc in $ahkProcesses) {
            # We need to get the full command line to identify our specific scripts
            $cimProc = Get-CimInstance -ClassName Win32_Process -Filter "ProcessId = $($proc.Id)"
            
            if ($cimProc.CommandLine -like "*OLED-Sleeper.ahk*" -or $cimProc.CommandLine -like "*OLED-Dimmer.ahk*") {
                Write-Host "Stopping process: $($proc.Name) (PID: $($proc.Id))" -ForegroundColor Red
                Stop-Process -Id $proc.Id -Force
            }
        }
    }
}
catch {
    # This might fail if user doesn't have permissions, but we don't want to halt the script.
    Write-Host "Could not check for existing processes. Continuing..." -ForegroundColor Yellow
}


# --- Launch Scripts Now ---
Write-Host "`n--- Launching Scripts Now (for the current session) ---" -ForegroundColor Cyan
$launchedSomething = $false

# --- Launch Blackout Watcher if needed ---
if ($blackoutMonitors.Count -gt 0) {
    # Extract just the 'Name' property for the command-line argument
    $blackoutString = ($blackoutMonitors.Name) -join ';'
    Start-Process -FilePath $sleeperAhkPath -ArgumentList """$blackoutString""", "$time_ms"
    Write-Host "$checkEmoji Blackout Watcher: " -ForegroundColor Gray -NoNewline
    Write-Host "Started for $($blackoutMonitors.Count) monitor(s)." -ForegroundColor Green
    $launchedSomething = $true
}

# --- Launch Dimmer Watcher if needed ---
if ($dimmerMonitors.Count -gt 0) {
    # Build the string in the format "Name:DimLevel"
    $dimmerString = ($dimmerMonitors | ForEach-Object { "$($_.Name):$($_.DimLevel)" }) -join ';'
    Start-Process -FilePath $dimmerAhkPath -ArgumentList """$dimmerString""", "$time_ms"
    Write-Host "$checkEmoji Dimmer Watcher: " -ForegroundColor Gray -NoNewline
    Write-Host "Started for $($dimmerMonitors.Count) monitor(s)." -ForegroundColor Green
    $launchedSomething = $true
}

if (-not $launchedSomething) {
    Write-Host "No valid actions were configured. Nothing to start." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "This window will close automatically in 10 seconds."
Start-Sleep -Seconds 10

#=================================================================
#==                             CLEANUP                         ==
#=================================================================
if (Test-Path -Path $csvPath) {
    Remove-Item -Path $csvPath
}