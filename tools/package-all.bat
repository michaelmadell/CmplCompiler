@echo off
setlocal enabledelayedexpansion

set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..") do set "REPO_ROOT=%%~fI"
set "DIST_DIR=%REPO_ROOT%\dist"
set "BUILD_DIR=%REPO_ROOT%\build"
set "STAGING_DIR=%BUILD_DIR%\staging"

echo === Packaging Cmpl Release Artifacts ===

set "REBUILD=0"
if "%~1"=="-r" set "REBUILD=1"
if "%~1"=="--rebuild" set "REBUILD=1"
if "%~1"=="-h" goto :help
if "%~1"=="--help" goto :help

set "WIN_CLI_EXE=%BUILD_DIR%\win-cli\cmpl.exe"
set "WIN_GUI_EXE=%BUILD_DIR%\win-gui\cmpl.exe"
set "LINUX_CLI=%BUILD_DIR%\linux-cli\cmpl"

if !REBUILD! equ 1 goto :do_build
if not exist "%WIN_CLI_EXE%" goto :do_build
if not exist "%WIN_GUI_EXE%" goto :do_build
if not exist "%LINUX_CLI%" goto :do_build
goto :do_package

:do_build
echo Building Windows CLI...
call "%SCRIPT_DIR%build-win-cli.bat"
if !ERRORLEVEL! neq 0 exit /b !ERRORLEVEL!

echo Building Windows GUI...
call "%SCRIPT_DIR%build-win-gui.bat"
if !ERRORLEVEL! neq 0 exit /b !ERRORLEVEL!

echo Building Linux CLI...
call "%SCRIPT_DIR%build-linux-cli.bat"
if !ERRORLEVEL! neq 0 exit /b !ERRORLEVEL!

:do_package
if exist "%STAGING_DIR%" rmdir /s /q "%STAGING_DIR%"
if not exist "%DIST_DIR%" mkdir "%DIST_DIR%"
if not exist "%STAGING_DIR%\win" mkdir "%STAGING_DIR%\win"
if not exist "%STAGING_DIR%\linux" mkdir "%STAGING_DIR%\linux"

echo Packaging Windows release...
copy /y "%WIN_CLI_EXE%" "%STAGING_DIR%\win\cmpl.exe" >nul
copy /y "%WIN_GUI_EXE%" "%STAGING_DIR%\win\cmpl-gui.exe" >nul
if exist "%REPO_ROOT%\LICENSE.txt" copy /y "%REPO_ROOT%\LICENSE.txt" "%STAGING_DIR%\win\" >nul
if exist "%REPO_ROOT%\README.md" copy /y "%REPO_ROOT%\README.md" "%STAGING_DIR%\win\" >nul

set "WIN_ZIP1=%DIST_DIR%\cmlp-x86_64-win.zip"
set "WIN_ZIP2=%DIST_DIR%\cmpl-x86_64-win.zip"
if exist "%WIN_ZIP1%" del /f /q "%WIN_ZIP1%"
if exist "%WIN_ZIP2%" del /f /q "%WIN_ZIP2%"

tar -a -cf "%WIN_ZIP1%" -C "%STAGING_DIR%\win" .
if !ERRORLEVEL! neq 0 (
    echo Error: failed to create Windows zip archive
    exit /b !ERRORLEVEL!
)
copy /y "%WIN_ZIP1%" "%WIN_ZIP2%" >nul
copy /y "%WIN_ZIP1%" "%BUILD_DIR%\cmlp-x86_64-win.zip" >nul
copy /y "%WIN_ZIP2%" "%BUILD_DIR%\cmpl-x86_64-win.zip" >nul

echo Created: %WIN_ZIP1%
echo Created: %WIN_ZIP2%

echo Packaging Linux release...
copy /y "%LINUX_CLI%" "%STAGING_DIR%\linux\cmpl" >nul
if exist "%REPO_ROOT%\LICENSE.txt" copy /y "%REPO_ROOT%\LICENSE.txt" "%STAGING_DIR%\linux\" >nul
if exist "%REPO_ROOT%\README.md" copy /y "%REPO_ROOT%\README.md" "%STAGING_DIR%\linux\" >nul

set "LINUX_TAR=%DIST_DIR%\cmpl-x86_64-linux.tar.gz"
if exist "%LINUX_TAR%" del /f /q "%LINUX_TAR%"
tar -czf "%LINUX_TAR%" -C "%STAGING_DIR%\linux" .
if !ERRORLEVEL! neq 0 (
    echo Error: failed to create Linux tar archive
    exit /b !ERRORLEVEL!
)
copy /y "%LINUX_TAR%" "%BUILD_DIR%\cmpl-x86_64-linux.tar.gz" >nul

echo Created: %LINUX_TAR%

if exist "%STAGING_DIR%" rmdir /s /q "%STAGING_DIR%"

echo === Packaging Complete ===
exit /b 0

:help
echo Usage: package-all.bat [--rebuild]
exit /b 0
