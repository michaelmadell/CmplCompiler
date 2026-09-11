<#
.SYNOPSIS
Packages Windows and Linux release binaries into distributable archives:
- cmpl-x86_64-win.zip
- cmpl-x86_64-linux.tar.gz
#>

[CmdletBinding()]
param(
    [switch]$Rebuild
)

$ErrorActionPreference = 'Stop'

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = (Resolve-Path "$ScriptDir\..").Path
$DistDir = Join-Path $RepoRoot "dist"
$BuildDir = Join-Path $RepoRoot "build"
$StagingDir = Join-Path $BuildDir "staging"

Write-Host "=== Packaging Cmpl Release Artifacts ===" -ForegroundColor Cyan

# 1. Ensure builds exist or rebuild if requested
$WinCliExe = Join-Path $RepoRoot "build\win-cli\cmpl.exe"
$WinGuiExe = Join-Path $RepoRoot "build\win-gui\cmpl.exe"
$LinuxCli = Join-Path $RepoRoot "build\linux-cli\cmpl"

if ($Rebuild -or -not (Test-Path $WinCliExe)) {
    Write-Host "Building Windows CLI..." -ForegroundColor Yellow
    & (Join-Path $ScriptDir "build-win-cli.ps1")
}

if ($Rebuild -or -not (Test-Path $WinGuiExe)) {
    Write-Host "Building Windows GUI..." -ForegroundColor Yellow
    & (Join-Path $ScriptDir "build-win-gui.ps1")
}

if ($Rebuild -or -not (Test-Path $LinuxCli)) {
    Write-Host "Building Linux CLI..." -ForegroundColor Yellow
    & (Join-Path $ScriptDir "build-linux-cli.ps1")
}

# 2. Setup output and staging directories
if (Test-Path $StagingDir) {
    Remove-Item -Recurse -Force $StagingDir
}
New-Item -ItemType Directory -Force -Path $DistDir | Out-Null
$WinStaging = Join-Path $StagingDir "win"
$LinuxStaging = Join-Path $StagingDir "linux"
New-Item -ItemType Directory -Force -Path $WinStaging | Out-Null
New-Item -ItemType Directory -Force -Path $LinuxStaging | Out-Null

# 3. Package Windows release
Write-Host "Packaging Windows release..." -ForegroundColor Cyan
Copy-Item $WinCliExe (Join-Path $WinStaging "cmpl.exe")
Copy-Item $WinGuiExe (Join-Path $WinStaging "cmpl-gui.exe")
if (Test-Path (Join-Path $RepoRoot "LICENSE.txt")) {
    Copy-Item (Join-Path $RepoRoot "LICENSE.txt") $WinStaging
}
if (Test-Path (Join-Path $RepoRoot "README.md")) {
    Copy-Item (Join-Path $RepoRoot "README.md") $WinStaging
}

$WinZipName = "cmpl-x86_64-win.zip"
$WinZipDist = Join-Path $DistDir $WinZipName
$WinZipBuild = Join-Path $BuildDir $WinZipName

if (Test-Path $WinZipDist) { Remove-Item -Force $WinZipDist }

# Create zip from staging folder contents
$winFiles = Get-ChildItem -Path $WinStaging | Select-Object -ExpandProperty FullName
Compress-Archive -Path $winFiles -DestinationPath $WinZipDist -Force
Copy-Item $WinZipDist $WinZipBuild -Force

Write-Host "Created: $WinZipDist" -ForegroundColor Green

# 4. Package Linux release
Write-Host "Packaging Linux release..." -ForegroundColor Cyan
Copy-Item $LinuxCli (Join-Path $LinuxStaging "cmpl")
if (Test-Path (Join-Path $RepoRoot "LICENSE.txt")) {
    Copy-Item (Join-Path $RepoRoot "LICENSE.txt") $LinuxStaging
}
if (Test-Path (Join-Path $RepoRoot "README.md")) {
    Copy-Item (Join-Path $RepoRoot "README.md") $LinuxStaging
}

$LinuxTarName = "cmpl-x86_64-linux.tar.gz"
$LinuxTarDist = Join-Path $DistDir $LinuxTarName
$LinuxTarBuild = Join-Path $BuildDir $LinuxTarName

if (Test-Path $LinuxTarDist) { Remove-Item -Force $LinuxTarDist }

# Use tar to create .tar.gz
$tarCmd = Get-Command "tar" -ErrorAction SilentlyContinue
if ($tarCmd) {
    & tar -czf $LinuxTarDist -C $LinuxStaging .
} else {
    Write-Error "tar command not found. Unable to create .tar.gz archive."
    exit 1
}
Copy-Item $LinuxTarDist $LinuxTarBuild -Force

Write-Host "Created: $LinuxTarDist" -ForegroundColor Green

# Clean up staging
Remove-Item -Recurse -Force $StagingDir

Write-Host "=== Packaging Complete ===" -ForegroundColor Green
