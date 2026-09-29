#define MyAppName "File Organizer - Auto Clean & Sort"
#define MyAppVersion "1.0.2.0"
#define MyAppPublisher "YoussifYG"
#define MyAppExeName "SmartFileOrganizer.UI.exe"
#define PublishDir "publish"

[Setup]
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
AppId={{D3E8E421-44B6-4B11-8A4E-7BC188F35DE2}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\SmartFileOrganizer
DefaultGroupName={#MyAppName}
OutputDir=EXE
OutputBaseFilename=FileOrganizer_Setup_1.0.2.0_UNSIGNED
Compression=lzma
SolidCompression=yes
WizardStyle=modern
AppMutex=SmartFileOrganizerMutex
CloseApplications=yes
UsedUserAreasWarning=no

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Excludes: "*.pdb"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}"
