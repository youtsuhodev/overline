; Inno Setup script for Overline
; Build with: iscc /DAppVersion=0.1.0 installer\Overline.iss
; Published output must exist first (see build-installer.bat).

#ifndef AppVersion
  #define AppVersion "0.1.0"
#endif

#define AppName "Overline"
#define AppPublisher "Overline"
#define AppExeName "Overline.exe"
#define PublishDir "..\artifacts\publish\win-x64"

[Setup]
AppId={{E5B2F9C4-3D7E-4A9B-9C1D-2F6A8B4C5D6E}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppVerName={#AppName} {#AppVersion}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
OutputDir=..\artifacts\installer
OutputBaseFilename=Overline-Setup-{#AppVersion}
SetupIconFile=..\Overline.App\Assets\Overline.ico
UninstallDisplayIcon={app}\{#AppExeName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequiredOverridesAllowed=dialog
DisableDirPage=auto
Uninstallable=yes

[Languages]
Name: "french"; MessagesFile: "compiler:Languages\French.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#PublishDir}\{#AppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

[Registry]
; Store a version marker so the app could detect the installed location if needed.
Root: HKCU; Subkey: "Software\Overline"; ValueType: string; ValueName: "InstallDir"; ValueData: "{app}"; Flags: uninsdeletekey