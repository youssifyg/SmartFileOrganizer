#define MyAppName "File Organizer - Auto Clean & Sort"
#define MyAppVersion "1.0.0.0"
#define MyAppPublisher "YoussifYG"
#define MyAppExeName "SmartFileOrganizer.UI.exe"
#define PublishDir "C:\Users\Youssef\.gemini\antigravity\scratch\SmartFileOrganizer\src\SmartFileOrganizer.UI\bin\Release\net8.0-windows\win-x64\publish"

[Setup]
AppId={{D3E8E421-44B6-4B11-8A4E-7BC188F35DE2}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\SmartFileOrganizer
DefaultGroupName={#MyAppName}
OutputDir=D:\Work
OutputBaseFilename=SmartFileOrganizer_Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
AppMutex=SmartFileOrganizerMutex
CloseApplications=yes
UsedUserAreasWarning=no

[Files]
Source: "{#PublishDir}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}"
Type: filesandordirs; Name: "{localappdata}\SmartFileOrganizer"