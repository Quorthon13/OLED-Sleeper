#SingleInstance force
#Requires AutoHotkey v2.0

;===============================================================================
; OLED Dimmer
;
; Description:
;   Prevents OLED burn-in on secondary displays by monitoring user activity.
;   If no mouse or window activity is detected on a given monitor for a defined
;   idle time, it dims that screen to a specified brightness level.
;   When activity resumes, the original brightness is restored instantly.
;
; Dependencies:
;   - ControlMyMonitor.exe (located in the ..\tools directory)
;   - MultiMonitorTool.exe (located in the ..\tools directory)
;
; Usage:
;   Run the script with two arguments:
;   1. A semicolon-separated list of monitor device IDs, each with a
;      target brightness level (e.g., \\.\DISPLAY2:10 for 10% brightness)
;   2. Idle threshold in milliseconds before dimming (e.g. 30000 for 30s)
;
;   Example:
;     "OLED-Dimmer.ahk" "\\.\DISPLAY2:10;\\.\DISPLAY3:5" "30000"
;===============================================================================


; === PATH DEFINITIONS ===
; Build robust paths relative to the script's location.
global ProjectRoot := A_ScriptDir . "\.."
global LogFile     := ProjectRoot . "\OLED-Dimmer.log"
global MultiTool   := ProjectRoot . "\tools\MultiMonitorTool\MultiMonitorTool.exe"
global ControlTool := ProjectRoot . "\tools\ControlMyMonitor\ControlMyMonitor.exe"
global TempCsvFile := ProjectRoot . "\monitors_temp.csv"


; === STARTUP LOG CLEAR ===
try FileDelete(LogFile) ; Ensure a clean log on every script start

; === CONFIGURATION VARIABLES ===
global MonitorIdList := ""      ; Stores the raw input list of monitor ID and level pairs
global IdleThreshold := 0       ; Time (ms) before a monitor is considered idle
global CheckInterval := 50      ; Frequency (ms) to check each monitor's state

; === INTERNAL STATE ===
global MonitoredScreens := []   ; List of monitor state maps for each target screen


; ==============================================================================
; LOGGING FUNCTION — Appends messages to log file with timestamps
; ==============================================================================

Log(message) {
    global LogFile
    try FileAppend(Format("{1} - {2}`n", A_Now, message), LogFile)
}


; ==============================================================================
; INITIALIZATION BLOCK — Validates inputs, prepares state, and builds monitor list
; ==============================================================================

Log("--- Script started ---")

; --- Handle required command-line arguments ---
if A_Args.Length < 2 {
    Log("ERROR: Not enough arguments passed.")
    MsgBox("This script requires 2 arguments:`n1. Monitor ID:Level list`n2. Idle timeout (ms).", "Error", 48)
    ExitApp
}

MonitorIdList := A_Args[1]
IdleThreshold := Integer(A_Args[2])

Log("Monitor ID list: " . MonitorIdList)
Log("Idle threshold: " . IdleThreshold . " ms")

; --- Ensure brightness is restored on script exit ---
OnExit(CleanupOnExit)

; --- Parse and initialize each monitor ID ---
for pair in StrSplit(MonitorIdList, ";") {
    parts := StrSplit(pair, ":")
    if parts.Length != 2 {
        Log("WARNING: Invalid monitor entry skipped: " . pair)
        continue
    }

    id := Trim(parts[1])
    dimLevel := Integer(Trim(parts[2]))

    Log("Attempting to initialize monitor: " . id)
    monitorRect := GetMonitorRect(id)

    if monitorRect {
        ; Store state for this monitor
        screenState := Map(
            "ID", id,
            "Rect", monitorRect,
            "TargetDimLevel", dimLevel,
            "OriginalBrightness", -1, ; -1 indicates not yet recorded
            "IsDimmed", false,
            "LastActiveTime", A_TickCount
        )
        MonitoredScreens.Push(screenState)
        Log("Monitor initialized: " . id . " with target dim level: " . dimLevel . "%")
    } else {
        Log("ERROR: Could not find monitor ID: " . id)
        MsgBox("Monitor not found: " . id ".`nPlease verify the ID and ensure MultiMonitorTool.exe is available.", "Warning", 48)
    }
}

if MonitoredScreens.Length = 0 {
    Log("FATAL: No valid monitors were initialized.")
    MsgBox("Initialization failed. No monitors found. Exiting.", "Error", 48)
    ExitApp
}

Log("Initialization complete. Monitoring " . MonitoredScreens.Length . " screen(s).")
SetTimer(CheckAllMonitors, CheckInterval)
return


; ==============================================================================
; MAIN LOOP — Checks each monitored screen for user activity or inactivity
; ==============================================================================

CheckAllMonitors(*) {
    CoordMode("Mouse", "Screen") ; Get mouse position relative to full screen
    MouseGetPos(&mx, &my)

    for screen in MonitoredScreens {
        rect := screen['Rect']
        activity := false

        ; === Activity check 1: Mouse cursor is currently on this monitor ===
        if (mx >= rect['Left'] && mx < rect['Right'] && my >= rect['Top'] && my < rect['Bottom']) {
            activity := true
        }

        ; === Activity check 2: Active window is located on this monitor ===
        if !activity {
            try {
                if activeWin := WinActive("A") {
                    WinGetPos(&wx, &wy,,, activeWin)
                    if (wx >= rect['Left'] && wx < rect['Right'] && wy >= rect['Top'] && wy < rect['Bottom']) {
                        activity := true
                    }
                }
            }
        }

        ; === Reaction: Activity detected ===
        if activity {
            screen['LastActiveTime'] := A_TickCount

            if screen['IsDimmed'] {
                Log("Activity resumed on " . screen['ID'] . ". Restoring brightness to " . screen['OriginalBrightness'] . "%.")
                SetBrightness(screen['ID'], screen['OriginalBrightness'])
                screen['IsDimmed'] := false
            }

        ; === Reaction: Monitor has been idle for longer than threshold ===
        } else if !screen['IsDimmed'] && (A_TickCount - screen['LastActiveTime'] > IdleThreshold) {
            ; Record current brightness before dimming
            currentBrightness := GetBrightness(screen['ID'])
            screen['OriginalBrightness'] := currentBrightness

            Log(screen['ID'] . " exceeded idle threshold. Dimming from " . currentBrightness . "% to " . screen['TargetDimLevel'] . "%.")
            SetBrightness(screen['ID'], screen['TargetDimLevel'])
            screen['IsDimmed'] := true
        }
    }
}


; ==============================================================================
; HELPER FUNCTIONS — Wrappers for external monitor tools
; ==============================================================================

; Sets monitor brightness to a specific value using ControlMyMonitor.exe
SetBrightness(monitorID, brightness) {
    global ControlTool
    cmd := Format('"{1}" /SetValue "{2}\Monitor0" 10 {3}', ControlTool, monitorID, brightness)
    RunWait(cmd,, "Hide")
}

; Gets the current brightness of a monitor
GetBrightness(monitorID) {
    global ControlTool
    return RunWait(Format('"{1}" /GetValue "{2}\Monitor0" 10', ControlTool, monitorID),, "Hide")
}

; Gets a monitor's screen coordinates using MultiMonitorTool.exe
GetMonitorRect(monitorID) {
    global MultiTool, TempCsvFile
    Log("Querying geometry for: " . monitorID)

    ; Export current monitor data to temporary CSV file
    RunWait(Format('"{1}" /scomma "{2}"', MultiTool, TempCsvFile),, "Hide")
    if !FileExist(TempCsvFile) {
        Log("ERROR: Output CSV not found after running MultiMonitorTool.")
        return false
    }

    csvData := FileRead(TempCsvFile)
    Loop Parse csvData, "`n", "`r" {
        if A_Index = 1 || A_LoopField = ""
            continue ; Skip header or blank line

        columns := []
        Loop Parse A_LoopField, "CSV" {
            columns.Push(A_LoopField)
        }

        ; Column 13 = Monitor ID. Match against target.
        if (columns.Length >= 13 && columns[13] = monitorID) {
            Log("Found matching monitor entry.")

            ; Parse resolution and position
            res := StrSplit(columns[1], "X")
            width := Integer(Trim(res[1]))
            height := Integer(Trim(res[2]))

            pos := StrSplit(columns[2], ",")
            left := Integer(Trim(pos[1]))
            top := Integer(Trim(pos[2]))

            FileDelete(TempCsvFile)

            return Map(
                "Left", left,
                "Top", top,
                "Right", left + width,
                "Bottom", top + height
            )
        }
    }

    Log("ERROR: Monitor not found in CSV: " . monitorID)
    FileDelete(TempCsvFile)
    return false
}


; ==============================================================================
; CLEANUP FUNCTION — Restores original brightness when script exits
; ==============================================================================

CleanupOnExit(ExitReason, ExitCode) {
    global MonitoredScreens
    Log("--- Exiting (Reason: " . ExitReason . ") ---")
    Log("Cleaning up by restoring brightness...")

    for screen in MonitoredScreens {
        try {
            if screen['IsDimmed'] {
                Log("Restoring brightness for monitor: " . screen['ID'] . " to " . screen['OriginalBrightness'] . "%")
                SetBrightness(screen['ID'], screen['OriginalBrightness'])
            }
        } catch {
            Log("WARNING: Failed to restore brightness for: " . screen['ID'])
        }
    }

    Log("Brightness restoration complete. Cleanup complete.")
}