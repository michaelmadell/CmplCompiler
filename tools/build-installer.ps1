<#
.SYNOPSIS
Builds the CmplCompiler Windows Inno Setup 7 installer (.exe).
#>

[CmdletBinding()]
param(
    [switch]$Rebuild
)

$ErrorActionPreference = 'Stop'

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = (Resolve-Path "$ScriptDir\..").Path
$IssScript = Join-Path $RepoRoot "CmplCompiler.iss"

if (-not (Test-Path $IssScript)) {
    Write-Error "Could not find $IssScript"
    exit 1
}

# 1. Ensure Windows binaries exist
$WinCliExe = Join-Path $RepoRoot "build\win-cli\cmpl.exe"
$WinGuiExe = Join-Path $RepoRoot "build\win-gui\cmpl.exe"

if ($Rebuild -or -not (Test-Path $WinCliExe)) {
    Write-Host "Building Windows CLI..." -ForegroundColor Yellow
    & (Join-Path $ScriptDir "build-win-cli.ps1")
}

if ($Rebuild -or -not (Test-Path $WinGuiExe)) {
    Write-Host "Building Windows GUI..." -ForegroundColor Yellow
    & (Join-Path $ScriptDir "build-win-gui.ps1")
}

# 2. Locate ISCC.exe
$Iscc = Get-Command "iscc" -ErrorAction SilentlyContinue
if (-not $Iscc) {
    $KnownPaths = @(
        "$env:LOCALAPPDATA\Programs\Inno Setup 7\ISCC.exe",
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
    )
    foreach ($p in $KnownPaths) {
        if (Test-Path $p) {
            $Iscc = $p
            break
        }
    }
} else {
    $Iscc = $Iscc.Source
}

if (-not $Iscc) {
    Write-Error "Inno Setup compiler (ISCC.exe) not found. Please install Inno Setup 7."
    exit 1
}

Write-Host "Using Inno Setup compiler: $Iscc" -ForegroundColor Green
Write-Host "Compiling installer: $IssScript" -ForegroundColor Cyan

& $Iscc $IssScript

if ($LASTEXITCODE -ne 0) {
    Write-Error "ISCC failed with exit code $LASTEXITCODE."
    exit $LASTEXITCODE
}

Write-Host "Installer built successfully in dist/" -ForegroundColor Green
