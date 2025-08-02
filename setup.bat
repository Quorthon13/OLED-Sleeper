@echo off
:: ============================================================================
:: ==                  OLED Sleeper - Configuration Launcher                 ==
:: ============================================================================
::
:: Description:
::   This batch file serves as a convenient shortcut to run the PowerShell
::   configuration wizard (Configure.ps1).
::
::   It uses the '-ExecutionPolicy Bypass' flag to ensure the script can run
::   without being blocked by system security policies, making it easy for
::   any user to launch.
::
:: Usage:
::   Simply double-click this file to start the monitor setup process.
::

:: Launch PowerShell, bypass the execution policy for this single instance,
:: and execute the main selector script located in the current directory.
powershell.exe -ExecutionPolicy Bypass -File ".\src\Configure.ps1"