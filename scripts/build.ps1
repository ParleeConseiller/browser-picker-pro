<#
.SYNOPSIS
    Browser Picker Pro build script
.DESCRIPTION
    Builds, tests, and optionally publishes Browser Picker Pro.
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
    $OutputDir = Join-Path $RootDir "publish" $Runtime
    Write-Host "[Publish] Publishing to $OutputDir..." -ForegroundColor Yellow
    dotnet publish (Join-Path $RootDir "src\BrowserPickerPro.UI\BrowserPickerPro.UI.csproj") `
        --configuration $Configuration `
        --runtime $Runtime `
        --self-contained true `
        -p:PublishSingleFile=true `
        --output $OutputDir
    if ($LASTEXITCODE -ne 0) { throw "Publish failed" }
    Write-Host "[Publish] Output: $OutputDir" -ForegroundColor Green
}

Write-Host ""
Write-Host "=== Build completed successfully ===" -ForegroundColor Green
