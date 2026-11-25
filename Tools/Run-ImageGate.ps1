# QF26 (2): the image-quality gate in every chapter - one short check run per chapter / scene (muted, minimized, Below Normal,
# one at a time), the in-game gate's verdict per shot (picture measures + NaN + subject), compared with the known baseline.
# Writes Builds/ImageGate/gate_<version>.txt ending "IMAGE GATE RELEASE (QF26 2): PASS / FAIL"; exit 1 on FAIL.
#   PASS = no NaN anywhere, no failure in Cattalus (QF26 re-proof), and no failure in another chapter that is not in the baseline
#          (Tools/image_gate_baseline.json: the shots that failed when the gate was introduced - to be fixed, never added to).
# QF29: scored by Tools/image_gate_score.py - a release is allowed when every failing shot is in the baseline and none got worse.
# Usage: powershell -File Tools/Run-ImageGate.ps1 [-Out <dir>] [-Only moon,venus,...] [-Reuse] [-WriteReference]
#   -Reuse: score the capture folders already in -Out (no new runs).  -WriteReference: record the baseline shots' failing measures once (QF29).
#   -GameDir <dir>: gate a copy of a build (Round 21: one release is gated while the next one builds); default Unity\Builds\Seven.
param([string]$Out = "$env:TEMP\seven_imagegate", [string]$Only = "", [switch]$Reuse, [switch]$WriteReference, [string]$GameDir = "")
$repo = Resolve-Path "$PSScriptRoot\.."
if (-not $GameDir) { $GameDir = "$repo\Unity\Builds\Seven" }
$exe = "$GameDir\SevenDemo.exe"
$version = (Get-Content "$GameDir\version.txt" -ErrorAction SilentlyContinue | Select-Object -First 1)
$runs = [ordered]@{
  "fight"    = "-fight -quick";
  "saucer"   = "-saucer -stopafter 90";
  "ratwing"  = "-ratwing";
  "moon"     = "-testspeed 2 -chapter1 -ch1cp 6 -stopafter 200";
  "venus"    = "-testspeed 2 -chapter2 -quick -stopafter 240";
  "europa"   = "-testspeed 2 -europacp 0 -stopafter 200";
  "mars"     = "-testspeed 2 -marscp 0 -stopafter 200";
  "asteroid" = "-testspeed 2 -astcp 0 -stopafter 200";
  "biaveh"   = "-testspeed 2 -biacp 0 -stopafter 200";
  "cattalus" = "-testspeed 2 -catcp 0 -stopafter 200";
  "finale_aboard" = "-testspeed 2 -fincp 0 -stopafter 150";   # Round 19: FIN_01 aboard (no baseline: every shot must pass)
  "finale"   = "-testspeed 2 -fincp 1";                       # Round 19: the whole Moon finale to the credits
  "grandcircuit" = "-testspeed 2 -gcproof";                  # Round 20: the racing mode (both tracks, the AI on every vehicle; no baseline)
}
$sel = if ($Only) { $Only.Split(",") } else { @($runs.Keys) }
foreach ($k in $sel) {
  $dir = Join-Path $Out $k
  if (-not $Reuse) {
    if (Test-Path $dir) { Remove-Item -Recurse -Force $dir }
    powershell -File "$PSScriptRoot\Run-Check.ps1" -Out $dir -Exe $exe -Args $runs[$k] -TimeoutSec $(if ($k -eq "finale" -or $k -eq "grandcircuit") { 1800 } else { 900 }) | Out-Null
  }
}
$extra = @(); if ($WriteReference) { $extra += "--write-reference" }
python "$PSScriptRoot\image_gate_score.py" $Out $version ($sel -join ",") @extra
exit $LASTEXITCODE
