# P2 (1): the whole game, start to credits, as a player - one player-style run per chapter, in story order, one at a time
# (muted, minimized, Below Normal through Run-Check.ps1). Longer than testing rule 4's 20 minutes: an investigation (Dan's OK, P2).
# Writes <Out>\fullgame_summary.txt: per piece the playtest's last line, soft-locks, build-check-style failures found in the log.
# Usage: powershell -File Tools/Run-FullGame.ps1 [-Out <dir>] [-Only moon,venus1,...] [-Exe <path>]
param([string]$Out = "$env:TEMP\seven_fullgame", [string]$Only = "", [string]$Exe = "$PSScriptRoot\..\Unity\Builds\Seven\SevenDemo.exe")
$pieces = [ordered]@{
  "moon"     = "-testspeed 2 -playtest 5 -playtestfrom 4";
  "venus1"   = "-testspeed 2 -venusplay -venushintfast";
  "venus2"   = "-testspeed 2 -venusplay2 -venushintfast";
  "mars"     = "-testspeed 2 -marsplay -menupick `"Venus|Venus: the debrief`"";
  "europa"   = "-testspeed 2 -europaplay -menupick `"Venus|Venus: the debrief`"";
  "asteroid" = "-testspeed 2 -astplay";
  "biaveh"   = "-testspeed 2 -biaplay";
  "cat_win"  = "-testspeed 2 -catplay";
  "cat_lose" = "-testspeed 2 -catplay -catcp 3 -catlose";
  "finale"   = "-testspeed 2 -finplay";
}
New-Item -ItemType Directory -Force $Out | Out-Null
$sum = Join-Path $Out "fullgame_summary.txt"
$sel = if ($Only) { $Only.Split(",") } else { @($pieces.Keys) }
foreach ($k in $sel) {
  $dir = Join-Path $Out $k
  if (Test-Path $dir) { Remove-Item -Recurse -Force $dir }
  $t0 = Get-Date
  $r = powershell -File "$PSScriptRoot\Run-Check.ps1" -Out $dir -Exe $Exe -Args $pieces[$k] -TimeoutSec 3600
  $mins = ((Get-Date) - $t0).TotalMinutes
  $log = Join-Path $dir "capture_log.txt"
  $line = "no capture_log"
  $soft = 0; $gate = ""
  if (Test-Path $log) {
    $l = Get-Content $log
    $line = ($l | Select-String -Pattern "PLAYTEST" | Select-Object -Last 1).Line
    $soft = ($l | Select-String -CaseSensitive -Pattern "SOFT-LOCK").Count
    $gate = ($l | Select-String -Pattern "^IMAGE GATE \(QF26 2\)" | Select-Object -Last 1).Line
  }
  $entry = "{0}: {1:0.0} real min; {2}; SOFT-LOCK lines {3}; {4}; {5}" -f $k, $mins, $line, $soft, $gate, ($r -join " / ")
  Add-Content $sum $entry
  Write-Output $entry
}
