<#
.SYNOPSIS
Builds the linux-cli binary using a pre-built cmpl compiler if available, falling back to dotnet tooling.
#>

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = (Resolve-Path "$ScriptDir\..").Path
$CmplFile = Join-Path $RepoRoot "examples\self-host.cmpl"
$ProjectFile = Join-Path $RepoRoot "CmplPiler\CmplPiler.csproj"
$OutputDir = Join-Path $RepoRoot "build\linux-cli"
$ProfileName = "linux-cli"

Write-Host "=== Building linux-cli ===" -ForegroundColor Cyan

# 1. Search for built cmpl compiler
$CmplBin = $null
$ResolvedOutputDir = if (Test-Path $OutputDir) { (Resolve-Path $OutputDir).Path } else { $null }

# Check PATH
$PathCmd = Get-Command "cmpl" -ErrorAction SilentlyContinue
if ($PathCmd) {
    $pathDir = Split-Path -Parent (Resolve-Path $PathCmd.Source)
    if (-not $ResolvedOutputDir -or $pathDir -ne $ResolvedOutputDir) {
        $CmplBin = $PathCmd.Source
    }
}

# Check known build locations (preferring locations outside output dir to avoid file locks)
if (-not $CmplBin) {
    $Candidates = @(
        (Join-Path $RepoRoot "build\win-cli\cmpl.exe"),
        (Join-Path $RepoRoot "build\win-gui\cmpl.exe"),
        (Join-Path $RepoRoot "build\cli-release\cmpl.exe"),
        (Join-Path $RepoRoot "build\windows-x64\cmpl.exe"),
        (Join-Path $RepoRoot "build\linux-x64\cmpl"),
        (Join-Path $RepoRoot "CmplPiler\bin\Release\net10.0\cmpl.exe"),
        (Join-Path $RepoRoot "CmplPiler\bin\Release\net10.0-windows\win-x64\cmpl.exe"),
        (Join-Path $RepoRoot "CmplPiler\bin\Debug\net10.0\cmpl.exe"),
        (Join-Path $RepoRoot "build\linux-cli\cmpl")
    )
    foreach ($Candidate in $Candidates) {
        if (Test-Path $Candidate) {
            $candidateDir = Split-Path -Parent (Resolve-Path $Candidate)
            if ($ResolvedOutputDir -and $candidateDir -eq $ResolvedOutputDir) {
                continue
            }
            $CmplBin = $Candidate
            break
        }
    }
}

$BuildSucceeded = $false

if ($CmplBin) {
    Write-Host "Found pre-built cmpl compiler at: $CmplBin" -ForegroundColor Green
    Write-Host "Attempting build using cmpl ($CmplFile -p $ProfileName)..." -ForegroundColor Cyan
    try {
        & $CmplBin $CmplFile -p $ProfileName
        if ($LASTEXITCODE -eq 0) {
            $BuildSucceeded = $true
            Write-Host "cmpl build succeeded!" -ForegroundColor Green
        } else {
            Write-Warning "cmpl build exited with code $LASTEXITCODE. Reverting to dotnet tooling..."
        }
    } catch {
        Write-Warning "Failed to execute cmpl: $($_.Exception.Message). Reverting to dotnet tooling..."
    }
} else {
    Write-Host "No pre-built cmpl compiler found. Using dotnet tooling..." -ForegroundColor Yellow
}

if (-not $BuildSucceeded) {
    Write-Host "Running dotnet tooling..." -ForegroundColor Cyan
    & dotnet publish $ProjectFile -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -o $OutputDir
    if ($LASTEXITCODE -ne 0) {
        Write-Error "dotnet build failed with exit code $LASTEXITCODE."
        exit $LASTEXITCODE
    }
    Write-Host "dotnet build succeeded!" -ForegroundColor Green
}
