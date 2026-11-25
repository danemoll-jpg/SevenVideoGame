# QF31: the raw memory clips (Art/Video/memory_clips/, Dan's material for his own edit; the stand-in montage until it is in) and FIN_12's
# "Received" stills (7 / Panther / their bike / the saucer hidden, checked) - one short check run per moment, one at a time, muted,
# minimized, Below Normal (Run-Check.ps1), then Tools/Video/encode_memclips.py encodes / installs them.
# Usage: powershell -File Tools/Run-MemoryCaptures.ps1 [-Out <dir>] [-Only ch1,aboard,...]
param([string]$Out = "$env:TEMP\seven_memclips", [string]$Only = "")
$repo = Resolve-Path "$PSScriptRoot\.."
$runs = [ordered]@{
  # QF34 (2): the memories - 7 and Panther TOGETHER in every shot (ClipRecorder's two-shot camera; Panther's visibility counted per frame),
  # in story order. A key is a capture name, a dialogue line id, or "@t<game seconds>".
  "ch1"      = '-testspeed 2 -chapter1 -stopafter 60 -memclips s01_cell_start=mem_01_the_first_meeting:6';
  "escape"   = '-testspeed 2 -chapter1 -ch1cp 1 -stopafter 60 -memclips @t7=mem_02_the_escape_side_by_side:6';
  "aboard"   = '-testspeed 2 -ch1aboard -stopafter 420 -memclips s10_03_eating_two_shot=mem_03_the_galley_meal@scene:6;s10_01_fridge_cat_on_table=mem_04_free_range@scene:6';
  "venus"    = '-testspeed 2 -chapter2 -quick -stopafter 300 -memclips VEN_B01_06=mem_06_the_naming:6;v08_b01_panther_stretch_grows=mem_07_he_grows:6;v15_b01_he_lies_down_large=mem_08_guarding_the_ship:6';
  "eggchair" = '-testspeed 2 -biacp 0 -stopafter 120 -memclips ba03_the_window=mem_05_his_head_in_her_lap:6';
    "purring"  = '-testspeed 2 -biacp 7 -stopafter 200 -memclips BIA_12_01=mem_10_back_together_on_the_floor:4.5;BIA_12_05=mem_11_the_ship_is_vibrating:5';
  "streets"  = '-testspeed 2 -catcp 0 -stopafter 300 -memclips CAT_01_02=mem_09_the_egg_chair_him_in_her_lap:5;ctl_control_back_Cattalus=mem_12_the_cattalus_streets:6';
  "race"     = '-testspeed 2 -catcp 3 -stopafter 420 -memclips CAT_08W_01=mem_13_after_the_race:6';
  # (the "Received" stills are unchanged since QF31 - rerun the old temple / mars / city keys from git history if they are ever needed)
}
$sel = if ($Only) { $Only.Split(",") } else { @($runs.Keys) }
$dirs = @()
foreach ($k in $sel) {
  $dir = Join-Path $Out $k; if (Test-Path $dir) { Remove-Item -Recurse -Force $dir }
  & "$PSScriptRoot\Run-Check.ps1" -Out $dir -Exe "$repo\Unity\Builds\Seven\SevenDemo.exe" -Args $runs[$k] -TimeoutSec 900 | Out-Null
  $log = Join-Path $dir "capture_log.txt"
  if (Test-Path $log) { Select-String -Path $log -Pattern "MEMORY CLIP|RECEIVED STILL" | ForEach-Object { "$k : $($_.Line)" } } else { "$k : no capture_log" }
  $dirs += $dir
}
Push-Location $repo; python Tools/Video/encode_memclips.py @dirs; Pop-Location
