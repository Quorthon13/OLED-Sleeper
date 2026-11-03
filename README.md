# OLED Sleeper 😴 – Blackout or Dim Secondary Monitors on Windows

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

OLED Sleeper is a lightweight Windows tool to blackout or dim idle monitors, helping users prevent OLED burn-in and temporarily sleep secondary monitors for focus, gaming, or distraction-free work.

<p align="center">
  <img src="https://github.com/user-attachments/assets/0f7c9110-094c-4fdb-8109-62fdd11e87cd" alt="OLED Sleeper Demonstration"> 
</p>

---

## The Problem

Many users have multi-monitor setups but want to turn off or dim secondary monitors temporarily without putting the entire computer to sleep. OLED and other displays can also suffer from burn-in or image retention if static images stay on screen too long. Windows’ built-in power settings are all-or-nothing — there’s no per-monitor control.

## The Solution

OLED Sleeper monitors each screen for activity. When a monitor is idle for a set time, it will either black it out or dim its brightness based on your preference. 

<p align="center">
  <img width="500" height="410" alt="oled-sleeper-002" src="https://github.com/user-attachments/assets/20022234-fe5f-4573-a4e4-ff5ef07b622b" />
</p>

---

## Features

* **Three Idle Detection Modes:** Customize how the application determines if a monitor is idle:
    * **Mouse:** Tracks cursor movement specifically on the target monitor.
    * **Focused Application:** Tracks activity within the active window currently displayed on that monitor.
    * **System-Wide Input:** Tracks overall keyboard and mouse input across the entire system (similar to standard Windows idle detection).
* **Manual Blackout:** Black out or restore the selected monitor with a button or a recorded global shortcut. Moving the mouse onto that display wakes it, even when idle management is disabled.
* **Per-Monitor Control:** Blackout or dim any monitor independently.
* **Two Action Modes:** Full blackout or dimming (DDC/CI supported).
* **Instant Wake-Up:** Restore the monitor immediately when activity is detected.
* **Native WPF Application:** Built from the ground up using native Win32 calls. Requires no external dependencies or third-party tools.

---

## Requirements

* **Operating System:** Windows 10 or 11
* **DDC/CI Support (for Dimming Mode):** Dimming requires a monitor that supports DDC/CI brightness control via VCP codes. Most modern monitors support this, but it is not guaranteed on all displays.

---

## How to Use

1. Download the latest installer or the portable zip from the [Releases page](https://github.com/Quorthon13/OLED-Sleeper/releases).
2. Run the installer and follow the on-screen prompts. During installation, you will be prompted to configure automatic startup and create shortcuts. For the portable version, just extract the zip anywhere and run it.
3. Open OLED Sleeper from your Start Menu or desktop shortcut.
4. Use the interface to select your target monitors, choose your preferred idle detection mode, and set your idle timers.
5. Apply your settings. The application will minimize to the system tray and run in the background.

### Manual blackout

Select a monitor, then click **Blackout Now** (or **Restore Monitor** when it is already blacked out).
To assign a shortcut, click the **Manual Blackout Shortcut** field and press Ctrl, Alt, Shift, or Win with another key, then save settings. For example, pressing Shift and the top-row 4 key records `Shift+4`.
Press Escape in the field to clear it; Tab leaves the field. Existing shortcuts pause while you record a replacement.

Shortcuts work while the settings window is hidden. Each monitor needs a different combination. Windows-reserved shortcuts and shortcuts registered by another app may be unavailable; an error is shown beside the field. The Windows key alone and hardware-only keys such as Fn are not supported.

Manual blackout uses the existing overlay and the monitor's saved **Also set brightness to zero** preference. Move the pointer onto the display (or move it if it was already there) to wake it, or use the button/shortcut again. Other enabled idle activity rules still apply. This feature does not lock a screen in blackout or put the monitor into hardware standby.

---

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
