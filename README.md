# OLED Sleeper 😴

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A simple but powerful tool for Windows users to prevent OLED burn-in and create a distraction-free workspace by automatically blacking them out when idle.
<p align="center">
  <img src="https://github.com/user-attachments/assets/93c2a968-e093-4817-a78c-38e94d4823df" alt="OLED Sleeper Demonstration">  
</p>

---
## The Problem

OLED displays are susceptible to permanent image retention (burn-in) from static images. Additionally, many power users want to turn off secondary monitors for focus or gaming without putting the entire computer to sleep. Standard Windows power settings can't do this; it's all or nothing.

---
## The Solution

OLED Sleeper runs quietly in the background, monitoring user activity on a per-screen basis. If a designated monitor is idle for a user-defined period, the script overlays it with a pure black screen. As soon as you move your mouse to that screen, the overlay vanishes instantly.

This is not only essential for **protecting OLEDs** but is also perfect for any user (with **LCD or OLED**) who wants to create a focused environment by selectively blacking out monitors—a feature Windows doesn't offer.

---
### Features

* **Per-Monitor Control:** Choose exactly which monitors to manage.
* **Adjustable Idle Timer:** Set any idle duration you want.
* **Instant Wake-Up:** The black overlay is removed the moment activity is detected.
* **Lightweight:** The background script has a tiny memory and CPU footprint.
* **Simple Setup:** A user-friendly wizard walks you through the initial configuration.

---
## Requirements

* **Operating System:** Windows 10 or 11.
* **Dependency:** **[AutoHotkey v2](https://www.autohotkey.com/)** must be installed on your system.

---
## How to Use

1.  Download the latest release from the [Releases page](https://github.com/Quorthon13/OLED-Sleeper/releases) or clone this repository.
2.  Unzip the folder to a permanent location on your computer.
3.  Double-click **`setup.bat`**.
4.  Follow the on-screen instructions to select your target monitors and set an idle timer.

That's it! The script will now run in the background and monitor your displays for the rest of your session.

**Note:** To re-launch the watcher after a restart, simply run **`setup.bat`** again.

---
## Credits

This project relies on the excellent `MultiMonitorTool.exe` utility developed by **NirSoft**. You can find more of their work at [www.nirsoft.net](https://www.nirsoft.net).

---
## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
