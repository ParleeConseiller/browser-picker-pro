# File: measure-picker-latency.ps1
# Description: Measures "link click -> picker window on screen" latency for a
#              Browser Picker Pro executable (legacy exe or the 2.x launcher),
#              by launching it with a URL and polling for the visible picker window.
# Author: Jose-Jorge HERNANDEZ
# Company: Parlee Conseiller, Inc.
# Date: 2026-11-25
# Last edit date: 2026-11-25
# Version: 1.1.0
#
# Usage:
#   .\scripts\measure-picker-latency.ps1 -Exe .\publish\win-x64\BrowserPickerProLauncher.exe -Runs 10
#   .\scripts\measure-picker-latency.ps1 -Exe J:\Temp\old\BrowserPickerPro.exe -Legacy -Runs 5
#
# -Legacy : the exe exits after each pick (v1.x), so the script kills the
#           process it started (by PID, never by name) after each run.
# default : resident mode (v2.x) - the script asks the picker to hide again
#           by posting WM_CLOSE, which the resident picker turns into "hide to tray".

param(
    # Path of the executable Windows would run for a link.
    [Parameter(Mandatory = $true)][string]$Exe,
    # How many timed runs to perform.
    [int]$Runs = 10,
    # URL passed to the exe (never opened: the picker is dismissed each run).
    [string]$Url = 'https://example.com/latency-test',
    # Treat the exe as the legacy one-shot app.
    [switch]$Legacy,
    # Give up on a run after this many milliseconds.
    [int]$TimeoutMs = 15000
)

# Window title the picker window carries (BrowserPickerWindow.xaml Title).
$PickerTitle = 'Browser Picker Pro'
# WM_CLOSE message id, used to dismiss the resident picker without killing it.
$WM_CLOSE = 0x0010

# Small Win32 helper: find a visible top-level window by exact title.
Add-Type -Namespace Bpp -Name Win -MemberDefinition @'
[DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr FindWindowW(string cls, string title);
[DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
[DllImport("user32.dll")] public static extern bool PostMessageW(IntPtr h, uint msg, IntPtr w, IntPtr l);
[StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
[DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
'@ -ErrorAction SilentlyContinue

# v2 parks the resident picker far off-screen instead of hiding it; anything left of this
# x coordinate counts as "not on screen".
$ParkedThreshold = -30000

# Returns the handle of the on-screen picker window, or 0 when none is on screen.
function Get-VisiblePicker {
    # [NullString]::Value, not $null: PowerShell turns $null into "" for a
    # string parameter, and FindWindow("") matches no window class at all.
    $h = [Bpp.Win]::FindWindowW([NullString]::Value, $PickerTitle)
    if ($h -eq [IntPtr]::Zero -or -not [Bpp.Win]::IsWindowVisible($h)) { return [IntPtr]::Zero }
    $r = New-Object Bpp.Win+RECT
    [void][Bpp.Win]::GetWindowRect($h, [ref]$r)
    if ($r.Left -le $ParkedThreshold) { return [IntPtr]::Zero }
    return $h
}

$results = @()
for ($i = 1; $i -le $Runs; $i++) {
    # Make sure no picker is on screen before starting the clock.
    $deadline = [Diagnostics.Stopwatch]::StartNew()
    while ((Get-VisiblePicker) -ne [IntPtr]::Zero -and $deadline.ElapsedMilliseconds -lt 3000) { Start-Sleep -Milliseconds 20 }

    $sw = [Diagnostics.Stopwatch]::StartNew()
    $proc = Start-Process -FilePath $Exe -ArgumentList "`"$Url`"" -PassThru
    $h = [IntPtr]::Zero
    # Poll every 2 ms: fine-grained enough for sub-50 ms results.
    while ($sw.ElapsedMilliseconds -lt $TimeoutMs) {
        $h = Get-VisiblePicker
        if ($h -ne [IntPtr]::Zero) { break }
        Start-Sleep -Milliseconds 2
    }
    $ms = $sw.ElapsedMilliseconds
    if ($h -eq [IntPtr]::Zero) { Write-Warning "Run $i timed out"; $ms = $null }
    else { Write-Host ("Run {0}: {1} ms" -f $i, $ms) }
    $results += $ms

    # Let the opening animation finish so the next run starts from a hidden window.
    Start-Sleep -Milliseconds 900
    if ($Legacy) {
        # One-shot exe: stop exactly the process this run started.
        if (-not $proc.HasExited) { Stop-Process -Id $proc.Id -Force }
    }
    elseif ($h -ne [IntPtr]::Zero) {
        # Resident exe: ask the picker to hide back to the tray.
        [void][Bpp.Win]::PostMessageW($h, $WM_CLOSE, [IntPtr]::Zero, [IntPtr]::Zero)
    }
    Start-Sleep -Milliseconds 600
}

# Summary: median is the headline number (first run is often a cold outlier).
$ok = $results | Where-Object { $_ -ne $null } | Sort-Object
if ($ok.Count -gt 0) {
    $median = $ok[[int][Math]::Floor(($ok.Count - 1) / 2)]
    [pscustomobject]@{
        Exe    = (Split-Path $Exe -Leaf)
        Runs   = $ok.Count
        MinMs  = $ok[0]
        Median = $median
        MaxMs  = $ok[-1]
        AllMs  = ($results -join ', ')
    }
}
