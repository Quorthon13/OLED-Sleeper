#SingleInstance force
#Requires AutoHotkey v2.0

;===============================================================================
; OLED Sleeper
;
; Description:
;   Prevents OLED burn-in on secondary displays by monitoring user activity.
; If no mouse or window activity is detected on a given monitor for a defined
; idle time, it reduces brightness to 0 and overlays that screen with a
; full black window.
;   When activity resumes, the blackout is lifted and brightness is restored.
;
; Dependencies:
;   - MultiMonitorTool.exe (located in the ..\tools directory)
;   - ControlMyMonitor.exe (located in the ..\tools directory)
;
; Usage:
;   Run the script with two arguments:
;   1. A semicolon-separated list of monitor device IDs (e.g. \\.\DISPLAY2)
;   2. Idle threshold in milliseconds before blackout (e.g. 30000 for 30s)
;
;   Example:
;     "OLED-Sleeper.ahk" "\\.\DISPLAY2;\\.\DISPLAY3" "30000"
;===============================================================================


; === PATH DEFINITIONS ===
; Build robust paths relative to the script's location.
global ProjectRoot := A_ScriptDir . "\.."
global LogFile     := ProjectRoot . "\OLED-Sleeper.log"
global MultiTool   := ProjectRoot . "\tools\MultiMonitorTool\MultiMonitorTool.exe"
global ControlTool := ProjectRoot . "\tools\ControlMyMonitor\ControlMyMonitor.exe"
global TempCsvFile := ProjectRoot . "\monitors_sleeper_temp.csv"
global RestoreFile := ProjectRoot . "\config\sleeper_restore.dat"

; === CONFIGURATION VARIABLES ===
global MonitorIdList := ""      ; Stores the raw input list of monitor IDs
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
; INITIALIZATION BLOCK — Validates inputs, prepares GUIs, and builds monitor list
; ==============================================================================

Log("--- Script started ---")

; --- Restore Brightness from Previous Session (if needed) ---
if FileExist(RestoreFile) {
    Log("Restore file found. Restoring brightness from previous session.")
    try {
        loop read RestoreFile
        {
            parts := StrSplit(A_LoopReadLine, ":")
            if (parts.Length = 2) {
                id := parts[1]
                brightness := Integer(parts[2])
                Log("Restoring brightness for " . id . " to " . brightness . "%")
                SetBrightness(id, brightness)
            }
        }
        FileDelete(RestoreFile)
        Log("Brightness restored and restore file deleted.")
    } catch {
        Log("ERROR: Failed to process brightness restore file.")
    }
}

; --- Handle required command-line arguments ---
if A_Args.Length < 2 {
    Log("ERROR: Not enough arguments passed.")
    MsgBox("This script requires 2 arguments:`n1. Monitor ID list`n2. Idle timeout (ms).", "Error", 48)
    ExitApp
}

MonitorIdList := A_Args[1]
IdleThreshold := Integer(A_Args[2])

Log("Monitor ID list: " . MonitorIdList)
Log("Idle threshold: " . IdleThreshold . " ms")

; --- Ensure overlays are removed on script exit ---
OnExit(CleanupOnExit)

; --- Parse and initialize each monitor ID ---
monitorIDs := StrSplit(MonitorIdList, ";")
for id in monitorIDs {
    id := Trim(id)
    if id = ""
        continue

    Log("Attempting to initialize monitor: " . id)
    monitorRect := GetMonitorRect(id)

    if monitorRect {
        ; Create a GUI window that fills the screen and is invisible to the taskbar
        blackoutGui := Gui("+AlwaysOnTop -Caption +ToolWindow -DPIScale")
        blackoutGui.BackColor := "000000" ; Fully black window

        ; Store state for this monitor
        screenState := Map(
            "ID", id,
            "Rect", monitorRect,
            "Gui", blackoutGui,
            "IsBlackedOut", false,
            "OriginalBrightness", -1, ; -1 indicates not yet recorded
            "LastActiveTime", A_TickCount
        )
        MonitoredScreens.Push(screenState)
        Log("Monitor initialized: " . id)
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
                    ; Get the window's position (x,y) AND its size (width, height)
                    WinGetPos(&wx, &wy, &ww, &wh, activeWin)

                    ; Calculate the absolute center point of the window
                    winCenterX := wx + (ww // 2)
                    winCenterY := wy + (wh // 2)

                    ; Check if the window's CENTER POINT is on the monitor
                    if (winCenterX >= rect['Left'] && winCenterX < rect['Right'] && winCenterY >= rect['Top'] && winCenterY < rect['Bottom']) {
                        activity := true
                    }
                }
            }
        }

        ; === Reaction: Activity detected ===
        if activity {
            screen['LastActiveTime'] := A_TickCount

            if screen['IsBlackedOut'] {
                Log("Activity resumed on " . screen['ID'] . ". Restoring brightness and unhiding overlay.")
                SetBrightness(screen['ID'], screen['OriginalBrightness'])
                screen['Gui'].Hide()
                screen['IsBlackedOut'] := false
                ClearRestoreState(screen['ID'])
            }

        ; === Reaction: Monitor has been idle for longer than threshold ===
        } else if !screen['IsBlackedOut'] && (A_TickCount - screen['LastActiveTime'] > IdleThreshold) {
            currentBrightness := GetBrightness(screen['ID'])
            screen['OriginalBrightness'] := currentBrightness

            Log(screen['ID'] . " exceeded idle threshold. Dimming to 0% and blacking out.")
            SetBrightness(screen['ID'], 0)
            SaveRestoreState(screen['ID'], currentBrightness)

            x := rect['Left'], y := rect['Top']
            w := rect['Right'] - rect['Left'], h := rect['Bottom'] - rect['Top']
            screen['Gui'].Show("x" . x . " y" . y . " w" . w . " h" . h . " NoActivate")
            screen['IsBlackedOut'] := true
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

            ; Log complete geometry information
            Log("Monitor Geometry Details:")
            Log("  Monitor ID: " . monitorID)
            Log("  Resolution: " . width . "x" . height)
            Log("  Position: Left=" . left . ", Top=" . top)
            Log("  Bounds: Right=" . (left + width) . ", Bottom=" . (top + height))
            Log("  Full Rect: {Left:" . left . ", Top:" . top . ", Right:" . (left + width) . ", Bottom:" . (top + height) . "}")

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
; STATE MANAGEMENT FUNCTIONS — Manages the brightness_restore.dat file
; ==============================================================================

SaveRestoreState(monitorID, brightness) {
    global RestoreFile
    Log("Saving restore state for " . monitorID . " -> " . brightness . "%")
    
    content := ""
    found := false
    if FileExist(RestoreFile) {
        loop read RestoreFile
        {
            if InStr(A_LoopReadLine, monitorID . ":") {
                content .= monitorID . ":" . brightness . "`n"
                found := true
            } else {
                content .= A_LoopReadLine . "`n"
            }
        }
    }
    if !found {
        content .= monitorID . ":" . brightness . "`n"
    }
    
    try {
        file := FileOpen(RestoreFile, "w", "UTF-8")
        file.Write(Trim(content, "`n"))
        file.Close()
    } catch {
        Log("ERROR: Failed to write to restore file.")
    }
}

ClearRestoreState(monitorID) {
    global RestoreFile
    Log("Clearing restore state for " . monitorID)
    
    if !FileExist(RestoreFile)
        return

    content := ""
    loop read RestoreFile
    {
        if !InStr(A_LoopReadLine, monitorID . ":") {
            content .= A_LoopReadLine . "`n"
        }
    }

    try {
        file := FileOpen(RestoreFile, "w", "UTF-8")
        file.Write(Trim(content, "`n"))
        file.Close()
        ; If the file is now empty, delete it
        if (file.Length = 0) {
            FileDelete(RestoreFile)
        }
    } catch {
        Log("ERROR: Failed to clear from restore file.")
    }
}


; ==============================================================================
; CLEANUP FUNCTION — Destroys GUI overlays and restores brightness on manual exit
; ==============================================================================

CleanupOnExit(ExitReason, ExitCode) {
    global MonitoredScreens
    Log("--- Exiting (Reason: " . ExitReason . ") ---")

    for screen in MonitoredScreens {
        try {
            if (screen['IsBlackedOut'] && ExitReason = 'Menu') {
                Log("Restoring brightness for monitor: " . screen['ID'] . " to " . screen['OriginalBrightness'] . "%")
                SetBrightness(screen['ID'], screen['OriginalBrightness'])
                ClearRestoreState(screen['ID'])
            }
            if IsObject(screen['Gui']) {
                screen['Gui'].Destroy()
                Log("Destroyed GUI for monitor: " . screen['ID'])
            }
        } catch {
            Log("WARNING: Failed to clean up for: " . screen['ID'])
        }
    }

    Log("Cleanup completed.")
}
