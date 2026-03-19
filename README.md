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

Benefits include: 
- Protecting OLEDs from burn-in 
- Creating a distraction-free workspace 
- Temporarily sleeping secondary monitors while gaming or working 
- Saving energy on idle screens

---

## Features

* **Native WPF Application:** Built from the ground up using native Win32 calls. Requires no external dependencies or third-party tools.
* **Three Idle Detection Modes:** Customize how the application determines if a monitor is idle:
    * **Mouse:** Tracks cursor movement specifically on the target monitor.
    * **Focused Application:** Tracks activity within the active window currently displayed on that monitor.
    * **System-Wide Input:** Tracks overall keyboard and mouse input across the entire system (similar to standard Windows idle detection).
* **Per-Monitor Control:** Blackout or dim any monitor independently.
* **Two Action Modes:** Full blackout or dimming (DDC/CI supported).
* **Focus Mode:** Temporarily hide secondary screens to reduce distractions.
* **Instant Wake-Up:** Restore the monitor immediately when activity is detected.

---

## Requirements

* **Operating System:** Windows 10 or 11
* **DDC/CI Support (for Dimming Mode):** Dimming requires a monitor that supports DDC/CI brightness control via VCP codes. Most modern monitors support this, but it is not guaranteed on all displays.

---

## How to Use

1. Download the latest installer from the [Releases page](https://github.com/Quorthon13/OLED-Sleeper/releases).
2. Run the installer and follow the on-screen prompts. During installation, you will be prompted to configure automatic startup and create shortcuts.
3. Open OLED Sleeper from your Start Menu or desktop shortcut.
4. Use the interface to select your target monitors, choose your preferred idle detection mode, and set your idle timers.
5. Apply your settings. The application will minimize to the system tray and run in the background.

---

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.