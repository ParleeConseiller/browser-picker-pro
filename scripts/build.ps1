# File: build.ps1
# Description: Builds, tests and optionally publishes Browser Picker Pro — the WPF app
#              (self-contained single file) plus the NativeAOT link launcher, side by side.
# Author: Jose-Jorge HERNANDEZ
# Company: Parlee Conseiller, Inc.
# Date: 2026-06-12
# Last edit date: 2026-11-25
# Version: 1.1.0
<#
.SYNOPSIS
    Browser Picker Pro build script
.DESCRIPTION
    Builds, tests, and optionally publishes Browser Picker Pro.
    -Publish writes publish/<rid>/BrowserPickerPro.exe and BrowserPickerProLauncher.exe.
    Created by Parlee Conseiller, Inc.
#>

param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$Test,
    [switch]$Publish,
    [switch]$Clean
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent $ScriptDir
$SolutionFile = Join-Path $RootDir "BrowserPickerPro.sln"

Write-Host "=== Browser Picker Pro Build Script ===" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration | Runtime: $Runtime" -ForegroundColor Gray
Write-Host ""

if ($Clean) {
    Write-Host "[Clean] Removing build artifacts..." -ForegroundColor Yellow
    Get-ChildItem $RootDir -Recurse -Directory -Filter "bin" | Remove-Item -Recurse -Force
    Get-ChildItem $RootDir -Recurse -Directory -Filter "obj" | Remove-Item -Recurse -Force
    Write-Host "[Clean] Done." -ForegroundColor Green
}

Write-Host "[Restore] Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore $SolutionFile
if ($LASTEXITCODE -ne 0) { throw "Restore failed" }

Write-Host "[Build] Building solution..." -ForegroundColor Yellow
dotnet build $SolutionFile --configuration $Configuration --no-restore
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

if ($Test) {
    Write-Host "[Test] Running tests..." -ForegroundColor Yellow
    dotnet test $SolutionFile --configuration $Configuration --no-build --verbosity normal
    if ($LASTEXITCODE -ne 0) { throw "Tests failed" }
}

if ($Publish) {
    # Nested Join-Path: the 3-argument form only exists in PowerShell 7, not Windows PowerShell 5.1.
    $OutputDir = Join-Path (Join-Path $RootDir "publish") $Runtime

    # A resident (tray) instance running FROM the output folder locks the exe.
    # Stop exactly those processes, by PID — never every BrowserPickerPro by name.
    $targets = @("BrowserPickerPro.exe", "BrowserPickerProLauncher.exe") | ForEach-Object { Join-Path $OutputDir $_ }
    Get-Process -ErrorAction SilentlyContinue |
        Where-Object { $_.Path -and ($targets -contains $_.Path) } |
        ForEach-Object {
            Write-Host "[Publish] Stopping running instance PID $($_.Id) ($($_.Path))" -ForegroundColor Yellow
            Stop-Process -Id $_.Id -Force
        }

    Write-Host "[Publish] Publishing app to $OutputDir..." -ForegroundColor Yellow
    dotnet publish (Join-Path $RootDir "src\BrowserPickerPro.UI\BrowserPickerPro.UI.csproj") `
        --configuration $Configuration `
        --runtime $Runtime `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        --output $OutputDir
    if ($LASTEXITCODE -ne 0) { throw "Publish failed" }

    # NativeAOT links with MSVC's link.exe, located through vswhere.exe — which is not on
    # PATH by default ("'vswhere.exe' is not recognized" → MSB3073 otherwise).
    $vsInstaller = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer"
    if (Test-Path (Join-Path $vsInstaller "vswhere.exe")) { $env:PATH = "$vsInstaller;$env:PATH" }

    Write-Host "[Publish] Publishing NativeAOT launcher..." -ForegroundColor Yellow
    dotnet publish (Join-Path $RootDir "src\BrowserPickerPro.Launcher\BrowserPickerPro.Launcher.csproj") `
        --configuration $Configuration `
        --runtime $Runtime `
        --output $OutputDir
    if ($LASTEXITCODE -ne 0) { throw "Launcher publish failed (needs the VS 'Desktop development with C++' workload)" }

    Write-Host "[Publish] Output: $OutputDir" -ForegroundColor Green
}
Write-Host ""
Write-Host "=== Build completed successfully ===" -ForegroundColor Green
