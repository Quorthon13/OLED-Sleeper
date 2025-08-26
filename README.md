# OLED Sleeper 😴 – Blackout or Dim Secondary Monitors on Windows

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

OLED Sleeper is a lightweight Windows tool to **black out or dim idle monitors**, helping users **prevent OLED burn-in** and **temporarily sleep secondary monitors** for focus, gaming, or distraction-free work.

<p align="center">
  <img src="https://github.com/user-attachments/assets/0f7c9110-094c-4fdb-8109-62fdd11e87cd" alt="OLED Sleeper Demonstration"> 
</p>

---

## The Problem

Many users have multi-monitor setups but want to **turn off or dim secondary monitors temporarily** without putting the entire computer to sleep.  
OLED and other displays can also suffer from **burn-in or image retention** if static images stay on screen too long.  
Windows’ built-in power settings are all-or-nothing — there’s no per-monitor control.

## The Solution

OLED Sleeper monitors each screen for activity (mouse or active window).  
When a monitor is idle for a set time, it will either **black it out** or **dim its brightness**, depending on your preference.  

Benefits include:  
- Protecting OLEDs from burn-in  
- Creating a **distraction-free workspace**  
- Temporarily **sleeping secondary monitors** while gaming or working  
- Saving energy on idle screens

---

## Features

* **Per-Monitor Control:** Blackout or dim any monitor independently.
* **Idle Timer:** Customize how long a monitor should be idle before action.
* **Two Modes:** Full blackout or dimming (DDC/CI supported).
* **Automatic Startup:** Runs at login without manual intervention.
* **Focus Mode:** Temporarily hide secondary screens to reduce distractions.
* **Lightweight:** Minimal CPU/memory usage.
* **Instant Wake-Up:** Restore monitor immediately when activity is detected.

---

## Requirements

* **Operating System:** Windows 10 or 11
* **Dependency:** [AutoHotkey v2](https://www.autohotkey.com/) must be installed
* **DDC/CI Support (for Dimming Mode):** Dimming requires a monitor that supports DDC/CI brightness control via VCP codes. Most modern OLED monitors support this, but it is not guaranteed on all displays.

---

## How to Use

1.  Download the latest release from the [Releases page](https://github.com/Quorthon13/OLED-Sleeper/releases) or clone this repository.
2.  Unzip the folder to a permanent location on your computer (e.g., `C:\Program Files\OLED-Sleeper`).
3.  Double-click **`setup.bat`**.
4.  A menu will appear:
    * **To set up for the first time or to change your settings**, choose option `1`. The wizard will guide you through selecting monitors, modes, and an idle time. When prompted, allow it to create the startup task.
    * **To remove the startup task and settings**, choose option `2`. This will delete the automatic startup shortcut and all saved configurations. Note that this action does not stop any scripts that are currently running.

That's it. Once configured, the script will run silently in the background and launch automatically every time you log in.

---

## Credits

This project relies on the excellent utilities developed by **NirSoft**:

-   [`MultiMonitorTool`](https://www.nirsoft.net/utils/multi_monitor_tool.html)
-   [`ControlMyMonitor`](https://www.nirsoft.net/utils/control_my_monitor.html)

You can find more of their work at [www.nirsoft.net](https://www.nirsoft.net)

---

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
