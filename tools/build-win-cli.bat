@echo off
setlocal enabledelayedexpansion

set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..") do set "REPO_ROOT=%%~fI"
set "CMPL_FILE=%REPO_ROOT%\examples\self-host.cmpl"
set "PROJECT_FILE=%REPO_ROOT%\CmplPiler\CmplPiler.csproj"
set "OUTPUT_DIR=%REPO_ROOT%\build\win-cli"
set "PROFILE=win-cli"

echo === Building win-cli ===

set "CMPL_BIN="

rem Check PATH
where cmpl >nul 2>nul
if %ERRORLEVEL% equ 0 (
    for /f "delims=" %%I in ('where cmpl 2^>nul') do (
        if not defined CMPL_BIN set "CMPL_BIN=%%I"
    )
)

rem Check known build locations (preferring locations outside current output dir)
if not defined CMPL_BIN (
    for %%F in (
        "%REPO_ROOT%\build\win-gui\cmpl.exe"
        "%REPO_ROOT%\build\windows-x64\cmpl.exe"
        "%REPO_ROOT%\build\cli-release\cmpl.exe"
        "%REPO_ROOT%\CmplPiler\bin\Release\net10.0-windows\win-x64\cmpl.exe"
        "%REPO_ROOT%\CmplPiler\bin\Release\net10.0\cmpl.exe"
        "%REPO_ROOT%\CmplPiler\bin\Debug\net10.0\cmpl.exe"
        "%REPO_ROOT%\build\win-cli\cmpl.exe"
    ) do (
        if not defined CMPL_BIN if exist "%%~F" set "CMPL_BIN=%%~F"
    )
)

set "BUILD_SUCCEEDED=0"

if defined CMPL_BIN (
    echo Found pre-built cmpl compiler at: !CMPL_BIN!
    echo Attempting build using cmpl ^(%CMPL_FILE% -p %PROFILE%^)...
    "!CMPL_BIN!" "%CMPL_FILE%" -p %PROFILE%
    if !ERRORLEVEL! equ 0 (
        set "BUILD_SUCCEEDED=1"
        echo cmpl build succeeded!
    ) else (
        echo cmpl build exited with code !ERRORLEVEL!. Reverting to dotnet tooling...
    )
) else (
    echo No pre-built cmpl compiler found. Using dotnet tooling...
)

if !BUILD_SUCCEEDED! neq 1 (
    echo Running dotnet tooling...
    dotnet publish "%PROJECT_FILE%" -c Release -r win-x64 -p:IncludeGui=false --self-contained true -p:PublishSingleFile=true -o "%OUTPUT_DIR%"
    if !ERRORLEVEL! neq 0 (
        echo dotnet build failed with exit code !ERRORLEVEL!.
        exit /b !ERRORLEVEL!
    )
    echo dotnet build succeeded!
)

exit /b 0
