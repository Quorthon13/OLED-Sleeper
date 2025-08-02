# OLED Sleeper 😴

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A simple but powerful tool for Windows users to prevent OLED burn-in and create a distraction-free workspace by automatically blacking them out or dimming them when idle.

<p align="center">
  <img src="https://github.com/user-attachments/assets/93c2a968-e093-4817-a78c-38e94d4823df" alt="OLED Sleeper Demonstration">  
</p>

---

## The Problem

OLED displays are susceptible to permanent image retention (burn-in) from static images. Additionally, many power users want to turn off secondary monitors for focus or gaming without putting the entire computer to sleep. Standard Windows power settings can't do this; it's all or nothing.

---

## The Solution

OLED Sleeper runs quietly in the background, checking for activity (mouse movement or active window presence) on each screen. If a designated monitor remains idle for a user-defined period, the script will either overlay it with a pure black screen or reduce its brightness — depending on your chosen mode.

This is useful not only for **protecting OLEDs**, but also for creating a more focused environment on any multi-monitor setup.

---

## How It Works

The script checks for:

- **Mouse movement** over each monitor
- **Active window focus** on that monitor

If neither is detected for a set time, the monitor is considered idle. No other input is monitored.

---

## Features

* **Per-Monitor Control:** Choose exactly which monitors to manage.
* **Adjustable Idle Timer:** Set any idle duration you want.
* **Two Idle Modes:** Choose between full **blackout** or brightness **dimming** for each monitor.
* **Instant Wake-Up:** Overlays are removed and brightness is restored the moment activity is detected.
* **Lightweight:** Uses minimal memory and CPU.
* **Simple Setup:** A user-friendly wizard walks you through the initial configuration.

---

## Requirements

* **Operating System:** Windows 10 or 11
* **Dependency:** [AutoHotkey v2](https://www.autohotkey.com/) must be installed
* **DDC/CI Support (for Dimming Mode):** Dimming requires a monitor that supports DDC/CI brightness control via VCP codes. Most modern OLED monitors support this, but it is not guaranteed on all displays.

---

## How to Use

1. Download the latest release from the [Releases page](https://github.com/Quorthon13/OLED-Sleeper/releases) or clone this repository.
2. Unzip the folder to a permanent location on your computer.
3. Double-click **`setup.bat`**.
4. Follow the on-screen instructions to select your target monitors, choose blackout or dim mode, and set an idle timer.

That's it. The script will now run in the background and monitor your displays for the rest of your session.

**Note:** To re-launch the watcher after a restart, simply run **`setup.bat`** again.

---

## Credits

This project relies on the excellent utilities developed by **NirSoft**:

- [`MultiMonitorTool`](https://www.nirsoft.net/utils/multi_monitor_tool.html)
- [`ControlMyMonitor`](https://www.nirsoft.net/utils/control_my_monitor.html)

You can find more of their work at [www.nirsoft.net](https://www.nirsoft.net).

---

## License

This project is licensed under the MIT License – see the [LICENSE](LICENSE) file for details.
