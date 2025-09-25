; -- Inno Setup Script for OLED Sleeper (x64 Version) --
; This script should be placed in a subfolder, e.g., "/installer".

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; You can generate a new one via Tools -> "Generate GUID" in Inno Setup.
AppId={{782DD1AF-DB60-48D7-8787-0838B581E16F}}
AppName=OLED Sleeper
AppVersion=2.0.0
AppPublisher=Quorthon13
AppPublisherURL=https://github.com/Quorthon13/OLED-Sleeper
AppSupportURL=https://github.com/Quorthon13/OLED-Sleeper/issues
PrivilegesRequired=lowest

; --- Paths and Filenames ---
; This is the name of the final installer file.
OutputBaseFilename=OLED-Sleeper-Setup-2.0.0-BETA-x64
; Assumes a "publish-x64" folder next to this script.
SourceDir=.
; Puts the final installer into an "InstallerOutput" folder next to this script.
OutputDir=.\InstallerOutput
; Installs to the 64-bit Program Files directory.
DefaultDirName={autopf64}\OLED Sleeper

; --- Icon Settings ---
; Sets the icon for the installer .exe itself. Path is relative to the script's location.
SetupIconFile=..\OLED-Sleeper\Assets\icon.ico
; Sets the icon for the Add/Remove Programs entry.
UninstallDisplayIcon={app}\OLED-Sleeper.exe

; --- General Settings ---
DefaultGroupName=OLED Sleeper
AllowNoIcons=yes
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
; Creates checkboxes on the "Select Additional Tasks" page of the installer.
Name: "startup"; Description: "Launch OLED Sleeper when Windows starts"; GroupDescription: "Additional options:";
Name: "desktopicon"; Description: "Create a desktop icon"; GroupDescription: "Additional shortcuts:";

[Files]
; Copies all files from your publish directory into the installation directory.
Source: ".\publish-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
; Creates shortcuts in the Start Menu and (optionally) on the Desktop.
; IMPORTANT: Change "OLEDSleeper.exe" if your executable has a different name.
Name: "{group}\OLED Sleeper"; Filename: "{app}\OLED-Sleeper.exe"
Name: "{autodesktop}\OLED Sleeper"; Filename: "{app}\OLED-Sleeper.exe"; Tasks: desktopicon

[Registry]
; Creates a registry entry to run the application at startup if the "startup" task is checked.
; IMPORTANT: Change "OLEDSleeper.exe" if your executable has a different name.
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "OLED Sleeper"; ValueData: """{app}\OLED-Sleeper.exe"""; Tasks: startup

[Run]
; Gives the user an option to run the application immediately after installation finishes.
; IMPORTANT: Change "OLEDSleeper.exe" if your executable has a different name.
Filename: "{app}\OLED-Sleeper.exe"; Description: "{cm:LaunchProgram,OLED Sleeper}"; Flags: nowait postinstall skipifsilent
