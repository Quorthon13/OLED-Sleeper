; -- Inno Setup Script for OLED Sleeper (Unified x64/x86 Version) --
; This script should be placed in a subfolder, e.g., "/installer".

#include "CodeDependencies.iss"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; You can generate a new one via Tools -> "Generate GUID" in Inno Setup.
AppId={{782DD1AF-DB60-48D7-8787-0838B581E16F}}
AppName=OLED Sleeper
UninstallDisplayName=OLED Sleeper
AppVersion=2.0.0
AppPublisher=Quorthon13
AppPublisherURL=https://github.com/Quorthon13/OLED-Sleeper
AppSupportURL=https://github.com/Quorthon13/OLED-Sleeper/issues
PrivilegesRequired=lowest

; --- Paths and Filenames ---
; This is the name of the final installer file. Removed "-x64" as it is now unified.
OutputBaseFilename=OLED-Sleeper-Setup-2.0.0-BETA
; Assumes publish-x64 and publish-x86 folders next to this script.a
SourceDir=.
; Puts the final installer into an "InstallerOutput" folder next to this script.
OutputDir=.\InstallerOutput
; {autopf} automatically resolves to "Program Files" on 64-bit systems 
; and "Program Files (x86)" on 32-bit systems.
DefaultDirName={autopf}\OLED Sleeper

; --- Architecture Settings ---
; Tells the installer to run in 64-bit mode on x64 systems.
ArchitecturesInstallIn64BitMode=x64

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
; Copies x64 files if installing on a 64-bit system.
Source: ".\publish-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: Is64BitInstallMode
; Copies x86 files if installing on a 32-bit system.
Source: ".\publish-x86\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: not Is64BitInstallMode

[Icons]
; Creates shortcuts in the Start Menu and (optionally) on the Desktop.
; IMPORTANT: Change "OLED-Sleeper.exe" if your executable has a different name.
Name: "{group}\OLED Sleeper"; Filename: "{app}\OLED-Sleeper.exe"
Name: "{autodesktop}\OLED Sleeper"; Filename: "{app}\OLED-Sleeper.exe"; Tasks: desktopicon

[Registry]
; Creates a registry entry to run the application at startup if the "startup" task is checked.
; The uninsdeletevalue flag ensures it is completely removed when the user uninstalls the app.
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "OLED Sleeper"; ValueData: """{app}\OLED-Sleeper.exe"""; Flags: uninsdeletevalue; Tasks: startup

[Run]
; Gives the user an option to run the application immediately after installation finishes.
; IMPORTANT: Change "OLED-Sleeper.exe" if your executable has a different name.
Filename: "{app}\OLED-Sleeper.exe"; Description: "{cm:LaunchProgram,OLED Sleeper}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Forcefully closes the application before the uninstaller attempts to delete its files.
Filename: "{cmd}"; Parameters: "/C ""taskkill /im OLED-Sleeper.exe /f /t"""; RunOnceId: "CloseOLEDSleeper"; Flags: runhidden

[InstallDelete]
; Deletes the settings folder and its contents before installing new files
Type: filesandordirs; Name: "{userappdata}\OLED-Sleeper"

[UninstallDelete]
; Deletes the settings folder and its contents during uninstallation
Type: filesandordirs; Name: "{userappdata}\OLED-Sleeper"

[Code]
function InitializeSetup(): Boolean;
begin
  // This automatically downloads + installs .NET 8.0 Desktop Runtime if missing
  Dependency_AddDotNet80Desktop;

  Result := True;
end;