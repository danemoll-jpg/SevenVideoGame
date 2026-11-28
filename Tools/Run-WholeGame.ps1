# QF38: the WHOLE game as a player from a NEW save, in ONE run per order (WholeGameRun.cs) - muted, minimized, Below Normal
# through Run-Check.ps1. Order A = Mars first, the asteroid first, the race won; order B = Europa first, the mothership first,
# the race lost. Writes <Out>\wholegame_summary.txt with each run's WHOLE GAME line and the QF38 check summaries.
# Usage: powershell -File Tools/Run-WholeGame.ps1 [-Out <dir>] [-Only A|B] [-Exe <path>]
param([string]$Out = "$PSScriptRoot\..\Builds\WholeGame", [string]$Only = "", [string]$Exe = "$PSScriptRoot\..\Unity\Builds\Seven\SevenDemo.exe")
$runs = [ordered]@{
  "A_mars_asteroid_win"   = "-testspeed 2 -playtest 5 -playtestfrom 4 -wholegame mars,asteroid";
  "B_europa_biaveh_lose"  = "-testspeed 2 -playtest 5 -playtestfrom 4 -wholegame europa,biaveh -catlose";
}
New-Item -ItemType Directory -Force $Out | Out-Null
$sum = Join-Path $Out "wholegame_summary.txt"
foreach ($k in $runs.Keys) {
  if ($Only -and -not $k.StartsWith($Only)) { continue }
  $dir = Join-Path $Out $k
  if (Test-Path $dir) { Remove-Item -Recurse -Force $dir }
  $t0 = Get-Date
  $r = & "$PSScriptRoot\Run-Check.ps1" -Out $dir -Exe $Exe -Args $runs[$k] -TimeoutSec 7200
  $mins = ((Get-Date) - $t0).TotalMinutes
  $log = Join-Path $dir "capture_log.txt"
  Add-Content $sum ("== {0}: {1:0.0} real min; {2}" -f $k, $mins, ($r -join " / "))
  if (Test-Path $log) {
    $pat = "WHOLE GAME \(QF38\)|BRANCH AUDIT|HAND AUDIT \(QF38 1\) SUMMARY|WINDOW VIEW \(QF38 7\) SUMMARY|TEMPLE ENTRANCE \(QF38 3\)|ESCAPE AUTODRIVE|NEXT DESTINATION|BRANCH \(QF38 8\)|SAVE SLOTS|VISION RELEASE|SOFT-LOCK|IMAGE GATE \(QF26 2\)|REVISIT|PANTHER WITH HER|GUIDE LIGHT|TELEPATHY SHOWN|TUNNELER ESTABLISHED|CLAMP RELEASE|CLIMB SECTION|VOICE OVERLAP|DIALOGUE FRAMING|card '"
    Get-Content $log | Select-String -Pattern $pat | ForEach-Object { Add-Content $sum ("   " + $_.Line) }
  }
  Write-Output "$k done in $([math]::Round($mins,1)) min"
}
