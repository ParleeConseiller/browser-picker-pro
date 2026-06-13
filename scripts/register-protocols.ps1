<#
.SYNOPSIS
    Register Browser Picker Pro as a Windows browser handler (standalone script).
.DESCRIPTION
    Registers HTTP, HTTPS, and file associations in HKCU (no admin required).
    Created by Parlee Conseiller, Inc.
#>

param(
    [string]$ExePath = "$PSScriptRoot\..\publish\win-x64\BrowserPickerPro.exe",
    [switch]$Unregister
)

$ProgId = "BrowserPickerProHTML"
$AppName = "BrowserPickerPro"
$DisplayName = "Browser Picker Pro"

if ($Unregister) {
    Write-Host "Unregistering Browser Picker Pro..." -ForegroundColor Yellow
    Remove-Item "HKCU:\SOFTWARE\Classes\$ProgId" -Recurse -ErrorAction SilentlyContinue
    Remove-Item "HKCU:\SOFTWARE\$AppName" -Recurse -ErrorAction SilentlyContinue
    $regApps = "HKCU:\SOFTWARE\RegisteredApplications"
    if (Get-ItemProperty $regApps -Name $AppName -ErrorAction SilentlyContinue) {
        Remove-ItemProperty $regApps -Name $AppName
    }
    Write-Host "Unregistered successfully." -ForegroundColor Green
    return
}

if (-not (Test-Path $ExePath)) {
    Write-Error "Executable not found: $ExePath. Build the project first with: .\build.ps1 -Publish"
    exit 1
}

$ExePathResolved = Resolve-Path $ExePath | Select-Object -ExpandProperty Path
Write-Host "Registering Browser Picker Pro..." -ForegroundColor Cyan
Write-Host "Executable: $ExePathResolved" -ForegroundColor Gray

# Register ProgId
$classesRoot = "HKCU:\SOFTWARE\Classes"
New-Item "$classesRoot\$ProgId" -Force | Out-Null
Set-ItemProperty "$classesRoot\$ProgId" -Name "(Default)" -Value $DisplayName
New-Item "$classesRoot\$ProgId\DefaultIcon" -Force | Out-Null
Set-ItemProperty "$classesRoot\$ProgId\DefaultIcon" -Name "(Default)" -Value "`"$ExePathResolved`",0"
New-Item "$classesRoot\$ProgId\shell\open\command" -Force | Out-Null
Set-ItemProperty "$classesRoot\$ProgId\shell\open\command" -Name "(Default)" -Value "`"$ExePathResolved`" `"%1`""

# Register Capabilities
$capPath = "HKCU:\SOFTWARE\$AppName\Capabilities"
New-Item $capPath -Force | Out-Null
Set-ItemProperty $capPath -Name "ApplicationDescription" -Value "Choose the right browser every time."
Set-ItemProperty $capPath -Name "ApplicationName" -Value $DisplayName

New-Item "$capPath\URLAssociations" -Force | Out-Null
Set-ItemProperty "$capPath\URLAssociations" -Name "http"  -Value $ProgId
Set-ItemProperty "$capPath\URLAssociations" -Name "https" -Value $ProgId
Set-ItemProperty "$capPath\URLAssociations" -Name "ftp"   -Value $ProgId

New-Item "$capPath\FileAssociations" -Force | Out-Null
Set-ItemProperty "$capPath\FileAssociations" -Name ".html" -Value $ProgId
Set-ItemProperty "$capPath\FileAssociations" -Name ".htm"  -Value $ProgId
Set-ItemProperty "$capPath\FileAssociations" -Name ".url"  -Value $ProgId

# Register with Windows
$regApps = "HKCU:\SOFTWARE\RegisteredApplications"
if (-not (Test-Path $regApps)) { New-Item $regApps -Force | Out-Null }
Set-ItemProperty $regApps -Name $AppName -Value "SOFTWARE\$AppName\Capabilities"

Write-Host ""
Write-Host "Registration complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Open Windows Settings -> Default Apps" -ForegroundColor White
Write-Host "  2. Search for 'Browser Picker Pro'" -ForegroundColor White
Write-Host "  3. Select it as default for HTTP, HTTPS, .html, .htm, and .url" -ForegroundColor White
Write-Host ""
Start-Process "ms-settings:defaultapps"
