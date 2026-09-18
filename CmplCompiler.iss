; Script generated for Inno Setup 7.1.0
; CmplCompiler Windows Installer

#define MyAppName "CmplCompiler"
#define MyAppVersion "1.0.0.2"
#define MyAppPublisher "Michael Madell"
#define MyAppURL "https://github.com/michaelmadell/CmplCompiler"
#define MyAppExeName "cmpl.exe"
#define MyAppGuiExeName "cmpl-gui.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
AppId={{5E65D7B1-4A59-4D6F-9AC6-F4F1D77C72B9}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/issues
AppUpdatesURL={#MyAppURL}/releases
DefaultDirName={autopf64}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=LICENSE.txt
OutputDir=dist
OutputBaseFilename=CmplCompiler-{#MyAppVersion}-x64-Setup
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog commandline
ChangesEnvironment=yes
ChangesAssociations=yes
CloseApplications=yes
RestartApplications=no
UninstallDisplayIcon={app}\{#MyAppGuiExeName}
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppName} Setup
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "envPath"; Description: "Add CmplCompiler to PATH (recommended for command line usage)"; GroupDescription: "System Integration:"; Flags: checkedonce
Name: "assocCmpl"; Description: "Associate .cmpl files with CmplCompiler"; GroupDescription: "File Associations:"; Flags: checkedonce
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "build\win-cli\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "build\win-gui\{#MyAppExeName}"; DestDir: "{app}"; DestName: "{#MyAppGuiExeName}"; Flags: ignoreversion
Source: "LICENSE.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "cmpl.schema.json"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}\CmplCompiler GUI"; Filename: "{app}\{#MyAppGuiExeName}"
Name: "{autoprograms}\{#MyAppName}\CmplCompiler Readme"; Filename: "{app}\README.md"
Name: "{autoprograms}\{#MyAppName}\Uninstall CmplCompiler"; Filename: "{uninstallexe}"
Name: "{autodesktop}\CmplCompiler GUI"; Filename: "{app}\{#MyAppGuiExeName}"; Tasks: desktopicon

[Registry]
; Associate .cmpl files with CmplCompiler
Root: HKA; Subkey: "Software\Classes\.cmpl"; ValueType: string; ValueName: ""; ValueData: "CmplCompiler.Project"; Flags: uninsdeletevalue; Tasks: assocCmpl
Root: HKA; Subkey: "Software\Classes\CmplCompiler.Project"; ValueType: string; ValueName: ""; ValueData: "CMPL Build File"; Flags: uninsdeletekey; Tasks: assocCmpl
Root: HKA; Subkey: "Software\Classes\CmplCompiler.Project\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppGuiExeName},0"; Tasks: assocCmpl
Root: HKA; Subkey: "Software\Classes\CmplCompiler.Project\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppGuiExeName}"" ""%1"""; Tasks: assocCmpl
Root: HKA; Subkey: "Software\Classes\CmplCompiler.Project\shell\build"; ValueType: string; ValueName: ""; ValueData: "Build with cmpl"; Tasks: assocCmpl
Root: HKA; Subkey: "Software\Classes\CmplCompiler.Project\shell\build\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""; Tasks: assocCmpl

[Run]
Filename: "{app}\{#MyAppGuiExeName}"; Description: "{cm:LaunchProgram,CmplCompiler GUI}"; Flags: nowait postinstall skipifsilent

[Code]
const
  EnvironmentKey = 'SYSTEM\CurrentControlSet\Control\Session Manager\Environment';
  UserEnvironmentKey = 'Environment';

procedure CurStepChanged(CurStep: TSetupStep);
var
  OrigPath, NewPath, AppDir: string;
  Hive: Integer;
  Key: string;
begin
  if (CurStep = ssPostInstall) and WizardIsTaskSelected('envPath') then
  begin
    AppDir := ExpandConstant('{app}');
    if IsAdminInstallMode then
    begin
      Hive := HKEY_LOCAL_MACHINE;
      Key := EnvironmentKey;
    end
    else
    begin
      Hive := HKEY_CURRENT_USER;
      Key := UserEnvironmentKey;
    end;

    if not RegQueryStringValue(Hive, Key, 'Path', OrigPath) then
      OrigPath := '';

    if Pos(';' + AppDir + ';', ';' + OrigPath + ';') = 0 then
    begin
      if (OrigPath <> '') and (OrigPath[Length(OrigPath)] <> ';') then
        NewPath := OrigPath + ';' + AppDir
      else
        NewPath := OrigPath + AppDir;

      RegWriteExpandStringValue(Hive, Key, 'Path', NewPath);
    end;
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  OrigPath, NewPath, AppDir: string;
  Hive: Integer;
  Key: string;
  P, L: Integer;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    AppDir := ExpandConstant('{app}');
    if IsAdminInstallMode then
    begin
      Hive := HKEY_LOCAL_MACHINE;
      Key := EnvironmentKey;
    end
    else
    begin
      Hive := HKEY_CURRENT_USER;
      Key := UserEnvironmentKey;
    end;

    if RegQueryStringValue(Hive, Key, 'Path', OrigPath) then
    begin
      P := Pos(';' + AppDir + ';', ';' + OrigPath + ';');
      if P > 0 then
      begin
        NewPath := ';' + OrigPath + ';';
        L := Length(AppDir);
        Delete(NewPath, P, L + 1);
        if (Length(NewPath) > 0) and (NewPath[1] = ';') then
          Delete(NewPath, 1, 1);
        if (Length(NewPath) > 0) and (NewPath[Length(NewPath)] = ';') then
          Delete(NewPath, Length(NewPath), 1);
        RegWriteExpandStringValue(Hive, Key, 'Path', NewPath);
      end;
    end;
  end;
end;
