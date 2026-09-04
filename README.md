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

1. Go to the [Releases page](https://github.com/Quorthon13/OLED-Sleeper/releases) and pick a download:

   | | Size | Needs .NET? | Settings live in |
   |---|---|---|---|
   | **`OLED-Sleeper-<version>-Setup.exe`** | ~2 MB | Downloads the .NET 8 Desktop runtime during install | `%APPDATA%\OLED-Sleeper` |
   | **`OLED-Sleeper-<version>-Portable-x64.zip`** | ~64 MB | No, everything is bundled | `Data\` beside the executable |

   The installer is small because it fetches the runtime at install time; the portable build carries it, which
   is the whole size difference. Both are the same application. Verify either against the
   `SHA256SUMS.txt` published alongside it.

   Take the portable build if you want no installation, or need to run from a USB stick or a machine you
   cannot install software on. Take the installer otherwise.

2. **Installer:** run it and follow the on-screen prompts. You will be asked whether to start OLED Sleeper
   with Windows and whether to create a desktop shortcut. Note that **updating resets your per-monitor
   settings to defaults** — logs are kept.

   **Portable:** extract the zip anywhere and run `OLED-Sleeper.exe`. Keep the other files in the folder beside it.
   Settings, state and logs are written to a `Data\` folder next to the executable, so the whole thing moves
   with the folder. The folder must be writable — OLED Sleeper checks at startup and tells you if it is not.

3. Open OLED Sleeper from your Start Menu or desktop shortcut, or by running the portable executable.
4. Use the interface to select your target monitors, choose your preferred idle detection mode, and set your idle timers.
5. Apply your settings. The application will minimize to the system tray and run in the background.

---

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
