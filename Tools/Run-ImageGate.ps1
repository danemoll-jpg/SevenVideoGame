# QF26 (2): the image-quality gate in every chapter - one short check run per chapter / scene (muted, minimized, Below Normal,
# one at a time), the in-game gate's verdict per shot (picture measures + NaN + subject), compared with the known baseline.
# Writes Builds/ImageGate/gate_<version>.txt ending "IMAGE GATE RELEASE (QF26 2): PASS / FAIL"; exit 1 on FAIL.
#   PASS = no NaN anywhere, no failure in Cattalus (QF26 re-proof), and no failure in another chapter that is not in the baseline
#          (Tools/image_gate_baseline.json: the shots that failed when the gate was introduced - to be fixed, never added to).
# Usage: powershell -File Tools/Run-ImageGate.ps1 [-Out <dir>] [-Only moon,venus,...] [-Reuse] [-WriteBaseline]
#   -Reuse: score the capture folders already in -Out (no new runs).  -WriteBaseline: record today's non-Cattalus failures.
param([string]$Out = "$env:TEMP\seven_imagegate", [string]$Only = "", [switch]$Reuse, [switch]$WriteBaseline)
$repo = Resolve-Path "$PSScriptRoot\.."
$exe = "$repo\Unity\Builds\Seven\SevenDemo.exe"
$version = (Get-Content "$repo\Unity\Builds\Seven\version.txt" -ErrorAction SilentlyContinue | Select-Object -First 1)
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
}
$sel = if ($Only) { $Only.Split(",") } else { @($runs.Keys) }
$basePath = "$PSScriptRoot\image_gate_baseline.json"
$baseline = @{}
if (Test-Path $basePath) { (Get-Content $basePath -Raw | ConvertFrom-Json).PSObject.Properties | ForEach-Object { $baseline[$_.Name] = @($_.Value) } }
$lines = @(); $release = $true; $newBase = [ordered]@{}
foreach ($k in $sel) {
  $dir = Join-Path $Out $k
  if (-not $Reuse) {
    if (Test-Path $dir) { Remove-Item -Recurse -Force $dir }
    powershell -File "$PSScriptRoot\Run-Check.ps1" -Out $dir -Exe $exe -Args $runs[$k] -TimeoutSec 600 | Out-Null
  }
  $log = "$dir\capture_log.txt"
  if (-not (Test-Path $log)) { $lines += "${k}: no capture_log.txt - the run did not finish"; $release = $false; continue }
  $fails = @(); $nan = 0; $shots = 0
  foreach ($m in (Select-String -Path $log -Pattern "IMAGE GATE (\S+\.png): (PASS|FAIL)")) {
    $shots++
    $name = $m.Matches[0].Groups[1].Value -replace '^\d+_', ''
    if ($m.Matches[0].Groups[2].Value -eq "FAIL") { $fails += $name; if ($m.Line -match "NaN / Inf") { $nan++ } }
  }
  $known = if ($baseline.ContainsKey($k)) { $baseline[$k] } else { @() }
  $new = @($fails | Where-Object { $known -notcontains $_ })
  $fixed = @($known | Where-Object { $fails -notcontains $_ })
  $ok = ($nan -eq 0) -and ($new.Count -eq 0) -and -not ($k -eq "cattalus" -and $fails.Count -gt 0)
  if ($WriteBaseline -and $k -ne "cattalus" -and $nan -eq 0) { $newBase[$k] = $fails; $ok = $true }
  if (-not $ok) { $release = $false }
  $lines += "${k}: IMAGE GATE (QF26 2): $shots shots, failed $($fails.Count) (known baseline $($fails.Count - $new.Count), new $($new.Count), NaN $nan, baseline shots now passing $($fixed.Count)) - $(if ($ok) { 'ok' } else { 'FAIL' })"
  foreach ($n in $new) { $lines += "    new failure: $n" }
}
if ($WriteBaseline) { ($newBase | ConvertTo-Json -Depth 3) | Set-Content -Encoding utf8 $basePath; $lines += "baseline written: $basePath" }
$lines += "IMAGE GATE RELEASE (QF26 2): $(if ($release) { 'PASS' } else { 'FAIL' }) - version $version"
New-Item -ItemType Directory -Force "$repo\Builds\ImageGate" | Out-Null
$report = "$repo\Builds\ImageGate\gate_$version.txt"
$lines | Set-Content -Encoding utf8 $report
$lines
Write-Output "report: $report"
exit $(if ($release) { 0 } else { 1 })
