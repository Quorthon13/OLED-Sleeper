


MultiMonitorTool v2.20
Copyright (c) 2012 - 2025 Nir Sofer
Web site: https://www.nirsoft.net



Description
===========

MultiMonitorTool is a small tool that allows you to do some actions
related to working with multiple monitors. With MultiMonitorTool, you can
disable/enable monitors, set the primary monitor, save and load the
configuration of all monitors, and move windows from one monitor to
another. You can do these actions from the user interface or from
command-line, without displaying user interface. MultiMonitorTool also
provides a preview window, which allows you to watch a preview of every
monitor on your system.



System Requirements and Limitations
===================================


* This utility works on any version of Windows, starting from Windows
  XP and up to Windows 11. Both 32-bit and x64 systems are supported.
* When you disable a monitor on a system with 3 monitors or more, and
  then later enable back the monitor, the operating system may put the
  monitor in another position. In order to avoid this problem, use the
  'Save Monitors Configuration' option to save your monitors
  configuration into a file and then load it later using the 'Load
  Monitors Configuration' option.
* This utility is only useful if your desktop is extended to multiple
  monitors. If you have multiple monitors that display the same content,
  then MultiMonitorTool is useless for you.



Versions History
================


* Version 2.20
  o Added /SetScale command-line option to set the display scaling on
    Windows 11/10, for example:
    MultiMonitorTool.exe /SetScale "\\.\DISPLAY1" 150
  o Added new display scaling columns: 'Current Scale' and 'Maximum
    Scale'.

* Version 2.15
  o Added a workaround for the new problems appeared in Windows 11
    24H2 update. The workaround is applied in the following features: Set
    as primary monitor, Load monitors configuration, /SetMonitors
    command, /SetPrimary command, /LoadConfig command.
  o Notice for Windows 11 24H2: If you are in situation that every
    action you try in MultiMonitorTool fails completely, you should go to
    the monitors settings of Windows and change something, or unplug and
    plug one of the monitors, and then go back to MultiMonitorTool and
    try again.
  o When loading a monitor configuration, MultiMonitorTool now
    applies the monitors configuration multiple times, because on complex
    monitors systems it's needed to set all monitors properly. Added
    MonitorsConfigNumOfCalls value to MultiMonitorTool.cfg to control the
    number of times that the monitors configuration is set. The default
    is 5 times.
  o The resolution and position information of disabled monitors is
    now taken directly from the Registry, which is more reliable data
    source. This information is also used to enable back the monitor.
  o Added /EnableAtPosition command, which allows you to enable a
    monitor in the specified position, for example:
    MultiMonitorTool.exe /EnableAtPosition "\\.\DISPLAY1" 1920 0

* Version 2.11
  o Fixed issue: On some systems, when a monitor was disconnected,
    MultiMonitorTool displayed incorrect monitor id and other incorrect
    monitor information. From this version the monitor information fields
    will be empty if the monitor information is not available.

* Version 2.10
  o Added 'Short Monitor ID' column. You can use the value displayed
    in this column in all command-line options.
  o Added 'Use Short Monitor ID as Name' to the 'Copy /SetMonitors
    Command Mode' option.

* Version 2.05
  o Added drop-down menu to the toolbar for opening the recent config
    files.
  o The serial number of the monitor is now stored inside the monitor
    config file.
  o Added 'Use Serial Number In Load Config' option. If it's turned
    on, MultiMonitorTool uses the serial number of the monitor when
    loading the monitor configuration file.

* Version 2.00
  o Added /SetMonitors command-line option, which allows you to set
    the desired settings of multiple monitors at once, without using an
    external config file, for example:
    MultiMonitorTool.exe /SetMonitors "Name=\\.\DISPLAY2 BitsPerPixel=32
    Width=1600 Height=900 DisplayFlags=0 DisplayFrequency=60
    DisplayOrientation=0 PositionX=1920 PositionY=0" "Name=\\.\DISPLAY1
    Primary=1 BitsPerPixel=32 Width=1920 Height=1080 DisplayFlags=0
    DisplayFrequency=60 DisplayOrientation=0 PositionX=0 PositionY=0"
  o For the 'Name' variable in this command, you can use the monitor
    name, Monitor ID, and the serial number of the monitor.
  o Added 'Copy /SetMonitors Command' (Under the Edit menu), which
    generates the /SetMonitors Command for the selected monitors and then
    copies the command to the clipboard. You can generate the
    /SetMonitors command for your current monitors configuration, and
    then later execute the command to restore this monitors configuration.
  o Added 'Copy /SetMonitors Command Mode' (Under the Options menu),
    which allows you to choose how the /SetMonitors command is generated
    ('Use \\.\DISPLAYx as Name', 'Use Monitor ID as Name', or 'Use Serial
    Number as Name')

* Version 1.96
  o When trying to enable a monitor from command-line ( /enable
    command) using the monitor number (e.g: /enable 2 or /enable
    \\.\DISPLAY1 ), MultiMonitorTool now tries to enable the monitor even
    if it cannot be found in the monitors list.

* Version 1.95
  o When disabling and then enabling a monitor on a system with 3
    monitors or more, MultiMonitorTool tries to put it back in the
    original position. Currently, this feature works only when you do the
    disable and enable without exit from MultiMonitorTool and it doesn't
    work from command-line.
  o The 'Use Monitor ID In Load Config' option is now turned on by
    default.

* Version 1.92
  o Fixed bug: When using the /PreviewOnly command-line option,
    MultiMonitorTool ignored the current value of preview update rate
    ('Advanced Options' window).

* Version 1.91
  o You can now use the serial number of the monitor ('Monitor Serial
    Number' column) in all command-line options.

* Version 1.90
  o Added new options: 'Turn Off Monitors', 'Turn On Monitors', and
    'Switch Off/On Monitors'. These options works only on Windows Vista
    or later with hardware that supports DDC/CI.
  o Added new command-line options: /TurnOff , /TurnOn , and
    /SwitchOffOn

* Version 1.86
  o 'Use Monitor ID In Load Config' option is now turned on by
    default.

* Version 1.85
  o When making changes from command-line ( /enable, /disable, and so
    on...) MultiMonitorTool now loads all monitors to the internal list
    that is used to delect the specified monitor, including monitors
    detected as 'Duplicate' and 'Disconnected'. In previous versions,
    MultiMonitorTool failed to find the monitor if it was detected as
    Duplicate/Disconnected.
  o Made a small change that hopefully will fix incorrect MonitorID
    problem on some systems.
  o When duplicate monitors are detected and 'Show Duplicate
    Monitors' is turned off, MultiMonitorTool now takes the first monitor
    item instead of the second one.
  o Added 'Disconnected' column.

* Version 1.83
  o Added 'Show Duplicate Monitors' option. On version 1.80, a bug
    that caused MultiMonitorTool to display the same monitor more than
    once was fixed, but it seems that on some systems, this bug fix may
    cause MultiMonitorTool to ignore a second monitor. So if you don't
    see all monitors on your system, you should try to turn on the 'Show
    Duplicate Monitors' option...

* Version 1.82
  o You can now specify the Monitor ID value in all command-line
    options (/disable, /enable, /switch, and so on), for example:
    MultiMonitorTool.exe /enable
    "MONITOR\GSM59A4\{4d36e96e-e325-11ce-bfc1-08002be10318}\0012"

* Version 1.81
  o Added 'Show Disconnected Monitors' option.

* Version 1.80
  o Fixed bug: In some monitor configurations, MultiMonitorTool
    displayed the same monitor more than once.
  o The monitor ID of every monitor is now saved into the monitors
    configuration file.
  o Added 'Use Monitor ID In Load Config' option, If you turn it on,
    MultiMonitorTool will use the monitor ID to detect the correct
    monitors when you load a monitors configuration file.

* Version 1.75
  o Added new columns: Monitor ID, Monitor Key, Monitor String,
    Monitor Name, Monitor Serial Number. (The values of 'Monitor Name'
    and 'Monitor Serial Number' columns are taken from the EDID data of
    the monitor)
  o Fixed bug: The Left-Top column displayed the coodinates as
    top-left instead of left-top.

* Version 1.72
  o Fixed bug: MultiMonitorTool failed to remember the last
    size/position of the main window and the monitor preview window if
    they were not located in the primary monitor.

* Version 1.71:
  o Fixed MultiMonitorTool to avoid from moving to another monitor
    the secondary taskbar (Shell_SecondaryTrayWnd class) of Windows 8.

* Version 1.70:
  o Added option to set the update rate of the monitor preview window
    (the default rate is 1000 milliseconds) in the Advanced Options
    window (Ctrl+O). If you have a fast computer with a good video
    adapter, you can set update rate to around 50 milliseconds and get a
    real-time view of another monitor.

* Version 1.64:
  o Added 'Always On Top' option for the preview window.

* Version 1.63:
  o Added 'Always On Top' option.

* Version 1.62:
  o Fixed issue: /SaveConfig command-line option failed to save the
    config file when running it from a batch file without specifying the
    full path of the config file.

* Version 1.61:
  o Added 'Move All Windows To Primary Monitor' option
    (Ctrl+Shift+F1), which allows you to easily move all windows from all
    monitors to the primary monitor.

* Version 1.60:
  o Added option to change the display orientation, from the user
    interface and from command-line ( /SetOrientation ).

* Version 1.55:
  o Added 'Sort On Every Update' option. If it's turned on,
    MultiMonitorTool will sort the list of monitors and the list of
    Windows, every time that there is a change.

* Version 1.51:
  o Added 'Clear Recent Files List' option.

* Version 1.50:
  o You can now choose to load the most recent 5 monitor
    configurations from the tray menu.

* Version 1.45:
  o Added /PreviewOnly command-line option, which allows you to run
    MultiMonitorTool in monitor preview only mode. In this mode, only the
    monitor preview window is displayed. Example:
    MultiMonitorTool.exe /PreviewOnly \\.\DISPLAY1

* Version 1.40:
  o Added option to save the monitors list to
    html/xml/csv/comma-delimited file from command-line

* Version 1.35:
  o Added option to choose another font (name and size) for using on
    the main window.

* Version 1.32:
  o Fixed bug: MultiMonitorTool failed to detect windows that their
    top-left and right-bottom points are not located in any valid
    monitor. MultiMonitorTool also failed to move these windows with
    /MoveWindow command-line option.

* Version 1.31:
  o When you run MultiMonitorTool, it now automatically selects the
    monitor that you selected in the previous time you used
    MultiMonitorTool.
  o Added /SelectedMonitior command-line option, which allows you to
    open MultiMonitorTool with the selected monitor you need. For example:
    MultiMonitorTool.exe /SelectedMonitior \\.\DISPLAY2

* Version 1.30:
  o Added 'Maximum Resolution' column.
  o Added 'Set Maximum Resolution' option.
  o Added /setmax command-line option.
  o Fixed the status-bar flickering.

* Version 1.26:
  o Added command-line support for moving windows from all monitors
    to the primary monitor:
    MultiMonitorTool.exe /MoveWindow Primary All
  o Added /SetNextPrimary command-line option.

* Version 1.25:
  o Added 'Start As Hidden' option. When this option and 'Put Icon On
    Tray' option are turned on, the main window of MultiMonitorTool will
    be invisible on start.
  o Added /StartAsHidden command-line option.
  o Added 'Monitor Preview Window' to the tray menu.

* Version 1.21:
  o Added 'Auto Size Columns+Headers' option, which allows you to
    automatically resize the columns according to the row values and
    column headers.
  o Fixed issue: The properties dialog-box and other windows opened
    in the wrong monitor, on multi-monitors system.
  o Added 'Mark Odd/Even Rows' option, under the View menu. When it's
    turned on, the odd and even rows are displayed in different color, to
    make it easier to read a single line.

* Version 1.20:
  o Added new command-line options: /MonitorPreviewWindow ,
    /PreviewMode , /DrawCursorPreview , and more...

* Version 1.15:
  o Added 'Load Recent Monitors Config' menu, which allows you to
    easily load the last 10 monitors configurations that you previously
    saved or loaded.

* Version 1.10:
  o Added 'Draw Cursor In Monitor Preview' option.
  o Fixed the /WindowLeft, /WindowTop, /WindowWidth, /WindowHeight
    command-line options to work even if the window remains in the same
    monitor.

* Version 1.05:
  o Added 'Orientation' column (Default, 90 Degrees, 180 Degrees, or
    270 Degrees)
  o The display orientation is now saved into the monitors
    configuration file, and the orientation of the monitor is updated
    when you load the monitors configuration file.
  o Added 'Next' option to the /MoveWindow command-line parameter,
    which allows you to move the window to the next monitor.
  o Added 4 new command-line parameters: /WindowLeft, /WindowTop,
    /WindowWidth, /WindowHeight - which allows you to set the desired
    window size/position when moving the window to another monitor with
    /MoveWindow command-line option

* Version 1.00 - First release.




Using MultiMonitorTool
======================

MultiMonitorTool doesn't require any installation process or additional
dll files. In order to start using it, simply run the executable file -
MultiMonitorTool.exe

The main window of MultiMonitorTool contains 2 panes: The upper pane
displays the list of all monitors detected on your system. When you
select a monitor in the upper pane, the lower pane displays the details
of all visible windows on the selected monitor.

You can select one or more monitors in the upper pane, and then use the
following options: Disable Selected Monitors (Ctrl+F6), Enable Selected
Monitors (Ctrl+F7), Disable/Enable Switch (Ctrl+F8), or Set As Primary
Monitor (Ctrl+F9)
You can also select one or more Windows in the lower pane, and then use
the 'Move Window To Next Monitor' and 'Move Window To Primary Monitor'
options in order to easily move Windows from one monitor to the other.



Save/Load Monitors Configuration
================================

MultiMonitorTool allows you to save the current configuration of all
monitors on your system, including the screen resolution, colors depth,
and monitor position of every monitor, by using the 'Save Monitors
Configuration' option (Ctrl+Shift+S).
You can restore back the saved monitors configuration by using the 'Load
Monitors Configuration' option (Ctrl+Shift+L).
You can also save/load the monitors configuration from command-line, by
using the /SaveConfig and /LoadConfig command-line options.



Preview Window
==============

The preview window shows you a preview of the monitor you select in the
upper pane. It might be useful if non-primary monitors are turned off and
you want to view the windows displayed in the other monitors. In order to
enable/disable the monitor preview window, simply press F2. You can also
resize the preview window to any size you like.



Command-Line Options
====================

You can use the following command-line options to change the monitor
configuration or to move windows automatically without displaying any
user interface.
The <Monitor> parameter in all command-line options may contain the
following values:
* Primary - Specifies that you want to do the action on the primary
  monitor.
* Monitor Name, as it appears in the 'Name' column, for example:
  \\.\DISPLAY1 , \\.\DISPLAY2, \\.\DISPLAY3
* Monitor Number - The number that appears in the monitor name. (1 for
  \\.\DISPLAY1, 2 for \\.\DISPLAY2, and so on...)
* Monitor ID, as it appears in the 'Monitor ID' column, for example:
  MONITOR\GSM59A4\{4d36e96e-e325-11ce-bfc1-08002be10318}\0008
* Short Monitor ID, as it appears in the 'Short Monitor ID' column, for
  example: GSM59A4
* The serial number of the monitor, as it appears in the 'Monitor
  Serial Number' column.



/SetMonitors <Monitor 1 Config> <Monitor 2 Config> <Monitor 3 Config>...
Allows you to set the desired settings of multiple monitors at once,
without using an external config file, for example:
MultiMonitorTool.exe /SetMonitors "Name=\\.\DISPLAY1 Primary=1
BitsPerPixel=32 Width=1920 Height=1080 DisplayFlags=0 DisplayFrequency=60
DisplayOrientation=0 PositionX=0 PositionY=0" "Name=\\.\DISPLAY2
BitsPerPixel=32 Width=1600 Height=900 DisplayFlags=0 DisplayFrequency=60
DisplayOrientation=0 PositionX=1920 PositionY=0"

In order to generate the /SetMonitors command for your current monitors
configuration, simply select all monitors in the main window (Ctrl+A),
and then use the 'Copy /SetMonitors Command' option (Under the Edit
menu). The generated /SetMonitors command will be copied to the
clipboard. By default, the /SetMonitors command is generated using the
Monitor ID as name, but you can change it to monitor name or serial
number from Options -> Copy /SetMonitors Command Mode.

Optionally, you can remove some of the variables from the generated
/SetMonitors command, if you usually don't change them on your system.
For example, if you don't change the monitor resolution and orientation,
and you only need to change the monitor positions, you can remove all
variables except of the Name, PositionX, and PositionY:
MultiMonitorTool.exe /SetMonitors "Name=\\.\DISPLAY1 PositionX=0
PositionY=0" "Name=\\.\DISPLAY2 PositionX=1920 PositionY=0"

You can also use the /SetMonitors command to set only specific display
settings of specific monitor. For example, the following command sets the
monitor with the specified serial number to 1920 X 1080 resolution:
MultiMonitorTool.exe /SetMonitors "Name=334DFRGV451 Width=1920
Height=1080"

/disable <Monitors>
Disables the specified monitors. You can specify a single monitor or
multiple monitors.
Examples:
MultiMonitorTool.exe /disable 1
MultiMonitorTool.exe /disable \\.\DISPLAY3
MultiMonitorTool.exe /disable 1 2 3

/enable <Monitors>
Enables the specified monitors.
Examples:
MultiMonitorTool.exe /enable 3
MultiMonitorTool.exe /enable \\.\DISPLAY1
MultiMonitorTool.exe /enable 3 2

/EnableAtPosition <Monitor> <X Position> <Y Position>
Enables the specified monitor at the specified position. If the /Enable
command doesn't work properly for you, you should try to enable the
monitor at the desired position.
Example:
MultiMonitorTool.exe /EnableAtPosition "\\.\DISPLAY2" 1920 0

/switch <Monitors>
Switches the specified monitors between enabled and disabled state.
Examples:
MultiMonitorTool.exe /switch 3
MultiMonitorTool.exe /switch \\.\DISPLAY1
MultiMonitorTool.exe /switch \\.\DISPLAY2 \\.\DISPLAY3

/setmax <Monitors>
Set the maximum resolution on the specified monitors.
Examples:
MultiMonitorTool.exe /setmax 3
MultiMonitorTool.exe /setmax \\.\DISPLAY2 \\.\DISPLAY3

/SetOrientation <Monitor> <Orientation [0, 90, 180, 270] >
Set the orientation of the specified monitor.
Examples:
MultiMonitorTool.exe /SetOrientation 2 270
MultiMonitorTool.exe /SetOrientation \\.\DISPLAY2 180 \\.\DISPLAY3 90
MultiMonitorTool.exe /SetOrientation 1 0 2 0

/SetPrimary <Monitor>
Set the primary monitor.
Examples:
MultiMonitorTool.exe /SetPrimary 2
MultiMonitorTool.exe /SetPrimary \\.\DISPLAY2

/SetNextPrimary
Set the next available monitor as the primary monitor. For example, if
you have a system with 3 monitors and the first monitor is the primary,
running this command will set the second monitor as the primary. running
this command again will set the third monitor as the primary. And...
running this command again will set (again) the first monitor as the
primary.

/SetScale <Monitor> <Scale Value>
Set the display scaling on Windows 11/10. The <Scale Value> is the
absolute scale value in percent, for example: 100,125,150,175,200,225,
and so on...
You can also specify a relative value. In this case - you have to specify
'0' to set the recommended display scaling, a positive number (1, 2, 3,
...) to set display scaling larger than recommended, or a negative number
(-1, -2, -3, ...) to set display scaling smaller than recommended.

Example:
MultiMonitorTool.exe /SetScale "\\.\DISPLAY1" 125
MultiMonitorTool.exe /SetScale "Primary" 150
MultiMonitorTool.exe /SetScale "\\.\DISPLAY2" -1

/TurnOff <Monitors>
Turns off the specified monitors. This feature works only if you have
Windows Vista or later and the hardware supports DDC/CI.
Examples:
MultiMonitorTool.exe /TurnOff 1
MultiMonitorTool.exe /TurnOff \\.\DISPLAY3
MultiMonitorTool.exe /TurnOff 1 2 3

/TurnOn <Monitors>
Turns on the specified monitors. This feature works only if you have
Windows Vista or later and the hardware supports DDC/CI.
Examples:
MultiMonitorTool.exe /TurnOn 3
MultiMonitorTool.exe /TurnOn \\.\DISPLAY1
MultiMonitorTool.exe /TurnOn 3 2

/SwitchOffOn <Monitors>
Switches the specified monitors between on and off state. This feature
works only if you have Windows Vista or later and the hardware supports
DDC/CI.
Examples:
MultiMonitorTool.exe /SwitchOffOn 3
MultiMonitorTool.exe /SwitchOffOn \\.\DISPLAY1
MultiMonitorTool.exe /SwitchOffOn \\.\DISPLAY2 \\.\DISPLAY3

/PreviewOnly <Monitor> {x} {y} {width} {height}
Allows you to run MultiMonitorTool in monitor preview only mode. In this
mode, only the monitor preview window is displayed.
You must specify the monitor name to display in the preview window.
Optionally, you can also specify the position and size to set the preview
window.
Examples:
MultiMonitorTool.exe /PreviewOnly \\.\DISPLAY2
MultiMonitorTool.exe /PreviewOnly \\.\DISPLAY1 10 10 300 200

/SaveConfig <Filename>
Saves the current configuration of all your monitors into the specified
filename, including the screen resolution, colors depth, and monitor
position.
You can load the saved configuration back into the system by using the
/LoadConfig command-line option.
Example:
MultiMonitorTool.exe /SaveConfig "c:\temp\monitors1.cfg"

/LoadConfig <Filename>
Loads the monitors configuration that you previouly saved with /SaveConfig
Example:
MultiMonitorTool.exe /LoadConfig "c:\temp\monitors1.cfg"


/MoveWindow <To Monitor> Process <Process Name>
/MoveWindow <To Monitor> Title <Title Text>
/MoveWindow <To Monitor> Class <Window Class>
/MoveWindow <To Monitor> All <From Monitor>
This command-line allows you to move windows from one monitor to the
other, according to the specified conditions. You can also use this
command-line option with /WindowLeft, /WindowTop, /WindowWidth, and
/WindowHeight, in order to set the width/size of the moved window. (See
examples below)
Here's some example:
* Move all windows located on all monitors to the primary monitor: (The
  <From Monitor> parameter is omitted, so windows are moved from all
  monitors)
  MultiMonitorTool.exe /MoveWindow Primary All
* Move all windows of Internet Explorer to the primary monitor:
  MultiMonitorTool.exe /MoveWindow Primary Process "iexplore.exe"
* Move all windows of Firefox to the next monitor:
  MultiMonitorTool.exe /MoveWindow Next Process "firefox.exe"
* Move all windows that their title contains the 'Hello World' string
  to '\\.\DISPLAY2' monitor:
  MultiMonitorTool.exe /MoveWindow 2 Title "Hello World"
* Move all windows of Windows Explorer (with 'CabinetWClass' window
  class) to '\\.\DISPLAY3' monitor:
  MultiMonitorTool.exe /MoveWindow 3 Class "CabinetWClass"
* Move all windows from '\\.\DISPLAY3' monitor to '\\.\DISPLAY2'
  monitor:
  MultiMonitorTool.exe /MoveWindow 2 All 3
* Move all windows of Firefox to the next monitor, and set their
  position to (10, 10), relatively to the target monitor.
  MultiMonitorTool.exe /MoveWindow Next Process "firefox.exe" /WindowLeft
  10 /WindowTop 10
* Move all windows of Firefox to the next monitor, and set their width
  to (600, 400).
  MultiMonitorTool.exe /MoveWindow Next Process "firefox.exe"
  /WindowWidth 600 /WindowHeight 400


/MonitorPreviewWindow <0 | 1>
Specifies whether to display the monitor preview window. 0 = No, 1 = Yes.

/PreviewMode <1 | 2>
Specifies the preview mode. 1 = Fast and Low Quality, 2 = Slow and High
Quality.

/DrawCursorPreview <0 | 1>
Specifies whether to draw the cursor inside the preview window. 0 = No, 1
= Yes.

/HideInactiveMonitors <0 | 1>
Specifies whether to hide inactive monitors. 0 = No, 1 = Yes.

/TrayIcon <0 | 1>
Specifies whether to put an icon on the system tray. 0 = No, 1 = Yes.

/StartAsHidden <0 | 1>
Specifies whether to start the main window as hidden. 0 = No, 1 = Yes.
Ths command-line option works only when TrayIcon option is turned on.

/AutoRefresh <0 | 1>
Specifies whether to automatically refresh the monitors/windows
information. 0 = No, 1 = Yes.

/stext <Filename>
Save the monitors list into a regular text file.

/stab <Filename>
Save the monitors list into a tab-delimited text file.

/scomma <Filename>
Save the monitors list into a comma-delimited text file (csv).

/stabular <Filename>
Save the monitors list into a tabular text file.

/shtml <Filename>
Save the monitors list into HTML file (Horizontal).

/sverhtml <Filename>
Save the monitors list into HTML file (Vertical).

/sxml <Filename>
Save the monitors list to XML file.



Have fun with the preview window
================================

If you activate the monitor preview window and extend its size to a
little less than the size of the entire monitor, the preview window will
draw itself again and again and again... and the final result will be a
window inside window inside window, like you can see in the following
screenshot:




Translating MultiMonitorTool to other languages
===============================================

In order to translate MultiMonitorTool to other language, follow the
instructions below:
1. Run MultiMonitorTool with /savelangfile parameter:
   MultiMonitorTool.exe /savelangfile
   A file named MultiMonitorTool_lng.ini will be created in the folder of
   MultiMonitorTool utility.
2. Open the created language file in Notepad or in any other text
   editor.
3. Translate all string entries to the desired language. Optionally,
   you can also add your name and/or a link to your Web site.
   (TranslatorName and TranslatorURL values) If you add this information,
   it'll be used in the 'About' window.
4. After you finish the translation, Run MultiMonitorTool, and all
   translated strings will be loaded from the language file.
   If you want to run MultiMonitorTool without the translation, simply
   rename the language file, or move it to another folder.



License
=======

This utility is released as freeware. You are allowed to freely
distribute this utility via floppy disk, CD-ROM, Internet, or in any
other way, as long as you don't charge anything for this and you don't
sell it or distribute it as a part of commercial product. If you
distribute this utility, you must include all files in the distribution
package, without any modification !



Disclaimer
==========

The software is provided "AS IS" without any warranty, either expressed
or implied, including, but not limited to, the implied warranties of
merchantability and fitness for a particular purpose. The author will not
be liable for any special, incidental, consequential or indirect damages
due to loss of data or any other reason.



Feedback
========

If you have any problem, suggestion, comment, or you found a bug in my
utility, you can send a message to nirsofer@yahoo.com
