# Check-run launcher (CLAUDE.md testing rules): muted, minimized without activation (never steals focus), Below Normal.
# Usage: powershell -File Tools/Run-Check.ps1 -Out <dir> -Args "<extra switches>" [-Exe <path>] [-TimeoutSec 900]
param([Parameter(Mandatory=$true)][string]$Out, [string]$Args = "", [string]$Exe = "$PSScriptRoot\..\Unity\Builds\Seven\SevenDemo.exe", [int]$TimeoutSec = 900)
New-Item -ItemType Directory -Force $Out | Out-Null
$Exe = (Resolve-Path $Exe).Path
$cmd = "`"$Exe`" -mute -capture `"$Out`" -screen-fullscreen 0 -screen-width 1280 -screen-height 720 -logFile `"$Out\Player.log`" $Args"
$si = ([wmiclass]"Win32_ProcessStartup").CreateInstance(); $si.ShowWindow = 7; $si.PriorityClass = 16384
$r = ([wmiclass]"Win32_Process").Create($cmd, (Split-Path $Exe), $si)
if ($r.ReturnValue -ne 0) { Write-Output "start failed: $($r.ReturnValue)"; exit 1 }
$procId = $r.ProcessId; $t0 = Get-Date
while (Get-Process -Id $procId -ErrorAction SilentlyContinue) { if (((Get-Date) - $t0).TotalSeconds -gt $TimeoutSec) { Stop-Process -Id $procId -Force; Write-Output "TIMEOUT after $TimeoutSec s"; break }; Start-Sleep -Seconds 3 }
Write-Output ("done in {0:0} s" -f ((Get-Date) - $t0).TotalSeconds)
if (Test-Path "$Out\capture_log.txt") { Write-Output "capture_log lines: $((Get-Content "$Out\capture_log.txt").Count)" } else { Write-Output "no capture_log.txt" }
