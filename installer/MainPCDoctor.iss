; JuDoctor — Inno Setup Script
; Build: dotnet publish first, then compile this script with Inno Setup 6+

#define AppName      "JuDoctor"
#define AppVersion   "1.0.0"
#define AppPublisher "ju0o"
#define AppExeName   "MainPCDoctor.Desktop.exe"
#define PublishDir   "..\src\MainPCDoctor.Desktop\bin\publish\win-x64"

[Setup]
; Keep the existing AppId so installed V1 builds remain upgrade-compatible.
AppId={{F4A2B3C8-91DE-4F5A-B862-D3E7A1C05F2B}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
; Keep the internal install directory stable for V1 compatibility.
DefaultDirName={autopf}\MainPCDoctor
DefaultGroupName={#AppName}
OutputDir=.\output
OutputBaseFilename=JuDoctor-{#AppVersion}-Setup
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\{#AppExeName}

; No elevation needed (HKCU only)
PrivilegesRequiredOverridesAllowed=dialog

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "korean";  MessagesFile: "compiler:Languages\Korean.isl"

[Tasks]
Name: "desktopicon";    Description: "{cm:CreateDesktopIcon}";   GroupDescription: "{cm:AdditionalIcons}"
Name: "startupentry";   Description: "Launch JuDoctor on Windows startup"; GroupDescription: "Options"

[Files]
; All publish output (self-contained win-x64)
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}";           Filename: "{app}\{#AppExeName}"
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"
Name: "{userdesktop}\{#AppName}";     Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Registry]
; Keep the internal startup value name stable for V1 compatibility.
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; \
  ValueType: string; ValueName: "MainPCDoctor"; \
  ValueData: """{app}\{#AppExeName}"" --tray"; \
  Tasks: startupentry; Flags: uninsdeletevalue

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; \
  Flags: nowait postinstall skipifsilent

[UninstallRun]
; Kill process before uninstall
Filename: "taskkill.exe"; Parameters: "/f /im {#AppExeName}"; Flags: runhidden; RunOnceId: "KillApp"
