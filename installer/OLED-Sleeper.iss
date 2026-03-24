; Inno Setup installer script for OLED Sleeper
; Builds a single installer supporting both x64 and x86 deployments.

#include "CodeDependencies.iss"

#define AppVersion "2.0.0"

[Setup]
; Unique application identifier used by Windows for installation tracking.
AppId={{782DD1AF-DB60-48D7-8787-0838B581E16F}}

; Application metadata shown in the installer and system UI.
AppName=OLED Sleeper
UninstallDisplayName=OLED Sleeper
AppVersion={#AppVersion}
AppPublisher=Quorthon13
AppPublisherURL=https://github.com/Quorthon13/OLED-Sleeper
AppSupportURL=https://github.com/Quorthon13/OLED-Sleeper/issues

; Installation runs without elevation.
PrivilegesRequired=lowest

; Output installer configuration.
OutputBaseFilename=OLED-Sleeper-{#AppVersion}-Setup
SourceDir=.
OutputDir=.\InstallerOutput

; Default installation directory.
DefaultDirName={autopf}\OLED Sleeper

; Enables 64-bit installation mode when running on x64 systems.
ArchitecturesInstallIn64BitMode=x64

; Installer and uninstall entry icons.
SetupIconFile=..\OLED-Sleeper\Assets\icon.ico
UninstallDisplayIcon={app}\OLED-Sleeper.exe

; General installer UI and compression settings.
DefaultGroupName=OLED Sleeper
AllowNoIcons=yes
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Languages]
; Default language configuration.
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
; Optional installation tasks presented to the user.
Name: "startup"; Description: "Launch OLED Sleeper when Windows starts"; GroupDescription: "Additional options:";
Name: "desktopicon"; Description: "Create a desktop icon"; GroupDescription: "Additional shortcuts:";

[Files]
; Install platform-specific binaries based on system architecture.
Source: ".\publish-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: Is64BitInstallMode
Source: ".\publish-x86\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: not Is64BitInstallMode

[Icons]
; Start Menu and optional Desktop shortcuts.
Name: "{group}\OLED Sleeper"; Filename: "{app}\OLED-Sleeper.exe"
Name: "{autodesktop}\OLED Sleeper"; Filename: "{app}\OLED-Sleeper.exe"; Tasks: desktopicon

[Registry]
; Optional autostart entry created when the startup task is selected.
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "OLED Sleeper"; ValueData: """{app}\OLED-Sleeper.exe"" -h"; Flags: uninsdeletevalue; Tasks: startup

[Run]
; Optionally launch the application after installation completes.
Filename: "{app}\OLED-Sleeper.exe"; Description: "{cm:LaunchProgram,OLED Sleeper}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Ensure the application process is terminated before file removal.
Filename: "{cmd}"; Parameters: "/C ""taskkill /im OLED-Sleeper.exe /f /t"""; RunOnceId: "CloseOLEDSleeper"; Flags: runhidden

[Code]

// Removes the application's autostart registry entry if present.
procedure RemoveStartupKey();
begin
  if RegValueExists(HKEY_CURRENT_USER, 'Software\Microsoft\Windows\CurrentVersion\Run', 'OLED Sleeper') then
  begin
    Log('Removing startup registry key.');
    RegDeleteValue(HKEY_CURRENT_USER, 'Software\Microsoft\Windows\CurrentVersion\Run', 'OLED Sleeper');
  end;
end;

// Runs during installation to clear any existing autostart entry.
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssInstall then
  begin
    RemoveStartupKey();
  end;
end;

// Runs during uninstallation to ensure the autostart entry is removed.
procedure CurUninstallStepChanged(UninstallStep: TUninstallStep);
begin
  if UninstallStep = usUninstall then
  begin
    RemoveStartupKey();
  end;
end;

// Requests download of required runtime dependencies before installation begins.
function InitializeSetup(): Boolean;
begin
  Dependency_AddDotNet80Desktop;
  Result := True;
end;