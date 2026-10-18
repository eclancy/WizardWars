; The Windows installer: one WizardWars-Setup.exe that installs the game for the current user.
;
;   ISCC /DAppVersion=0.3.2 tools/release/installer.iss
;
; Normally run by tools/release/release.sh after package.py. Needs Inno Setup 6
; (winget install JRSoftware.InnoSetup). See docs/releasing.md.
;
; It installs per user, into %LOCALAPPDATA%\Programs\WizardWars, with no admin prompt. That folder
; stays writable by the player, which the self-updater (scripts/UpdateScreen.cs) needs: it
; robocopies each update into the game's own folder.

#ifndef AppVersion
  #error Pass the version: ISCC /DAppVersion=X.Y.Z tools/release/installer.iss
#endif

[Setup]
; Never change the AppId: it is how Windows and later installers recognise an installed copy.
AppId={{7CB5915F-4809-4117-B9CC-1D7355374BFA}
AppName=Wizard Wars
AppVersion={#AppVersion}
; Apps & features shows the name alone; the version has its own column there.
UninstallDisplayName=Wizard Wars
AppPublisher=ECEC
AppPublisherURL=https://ecec.dev/games/wizard-wars
; Paths below are relative to the repo root.
SourceDir=..\..
PrivilegesRequired=lowest
DefaultDirName={localappdata}\Programs\WizardWars
DisableProgramGroupPage=yes
DisableDirPage=auto
UsePreviousAppDir=yes
WizardStyle=modern
SetupIconFile=icon.ico
UninstallDisplayIcon={app}\WizardWars.exe
OutputDir=build\release
OutputBaseFilename=WizardWars-Setup
Compression=lzma2/max
SolidCompression=yes
CloseApplications=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "build\windows\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion

[Icons]
Name: "{autoprograms}\Wizard Wars"; Filename: "{app}\WizardWars.exe"
Name: "{autodesktop}\Wizard Wars"; Filename: "{app}\WizardWars.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\WizardWars.exe"; Description: "Play Wizard Wars now"; Flags: postinstall nowait skipifsilent

[UninstallDelete]
; Everything in the game's folder, including files later updates added. Save data and settings
; live in %APPDATA%\Godot\app_userdata\Wizard Wars and are left alone.
Type: filesandordirs; Name: "{app}"
