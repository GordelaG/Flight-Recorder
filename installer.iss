; Inno Setup Script for Flight Recorder
#define MyAppName "Flight Recorder"
#ifndef MyAppVersion
#define MyAppVersion "0.27.0"
#endif
#define MyAppPublisher "GordelaG"
#define MyAppURL "https://github.com/GordelaG/Flight-Recorder"
#define MyAppExeName "FlightRecorder.Client.exe"

[Setup]
AppId={{E680E768-3067-4DF8-B053-C7B90FDDE55C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DisableProgramGroupPage=yes
OutputDir=installer_output
OutputBaseFilename=FlightRecorder-Setup
SetupIconFile=FlightRecorder.Client\Logo.ico
UninstallDisplayIcon={app}\Logo.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64
CloseApplications=yes
RestartApplications=yes

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Dirs]
Name: "{app}\SavedFlights"; Permissions: users-modify

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "FlightRecorder.Client\Logo.ico"; DestDir: "{app}"; Flags: ignoreversion
; vPilot Plugin: Installed silently if vPilot is present on user's machine
Source: "publish\FlightRecorder.VpilotPlugin.dll"; DestDir: "{code:GetVpilotPluginsDir}"; Flags: ignoreversion; Check: HasVpilot

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\Logo.ico"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\Logo.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
var
  VpilotPluginsPath: string;
  VpilotChecked: Boolean;
  VpilotFound: Boolean;

function FindVpilotPluginsDir(): string;
var
  InstallPath: string;
begin
  Result := '';
  
  // 1. Check HKCU Uninstall Registry
  if RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\vPilot', 'InstallLocation', InstallPath) then
  begin
    InstallPath := Trim(InstallPath);
    if (InstallPath <> '') and DirExists(InstallPath) then
    begin
      Result := AddBackslash(InstallPath) + 'Plugins';
      Exit;
    end;
  end;

  // 2. Check UninstallString in HKCU Registry
  if RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\vPilot', 'UninstallString', InstallPath) then
  begin
    InstallPath := ExtractFilePath(Trim(InstallPath));
    if (InstallPath <> '') and DirExists(InstallPath) then
    begin
      Result := AddBackslash(InstallPath) + 'Plugins';
      Exit;
    end;
  end;

  // 3. Fallback: %LOCALAPPDATA%\vPilot\vPilot.exe
  InstallPath := ExpandConstant('{localappdata}\vPilot');
  if FileExists(AddBackslash(InstallPath) + 'vPilot.exe') then
  begin
    Result := AddBackslash(InstallPath) + 'Plugins';
    Exit;
  end;
end;

function HasVpilot(): Boolean;
begin
  if not VpilotChecked then
  begin
    VpilotPluginsPath := FindVpilotPluginsDir();
    VpilotFound := (VpilotPluginsPath <> '');
    VpilotChecked := True;
  end;
  Result := VpilotFound;
end;

function GetVpilotPluginsDir(Param: string): string;
begin
  if HasVpilot() then
    Result := VpilotPluginsPath
  else
    Result := '';
end;
