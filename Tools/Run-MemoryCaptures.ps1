# QF31: the raw memory clips (Art/Video/memory_clips/, Dan's material for his own edit; the stand-in montage until it is in) and FIN_12's
# "Received" stills (7 / Panther / their bike / the saucer hidden, checked) - one short check run per moment, one at a time, muted,
# minimized, Below Normal (Run-Check.ps1), then Tools/Video/encode_memclips.py encodes / installs them.
# Usage: powershell -File Tools/Run-MemoryCaptures.ps1 [-Out <dir>] [-Only ch1,aboard,...]
param([string]$Out = "$env:TEMP\seven_memclips", [string]$Only = "")
$repo = Resolve-Path "$PSScriptRoot\.."
$runs = [ordered]@{
  "ch1"      = '-testspeed 2 -chapter1 -stopafter 110 -memclips s02_energy_point_dark_closeup=mem_01_the_first_meeting_in_the_cell:9';
  "aboard"   = '-testspeed 2 -ch1aboard -stopafter 330 -memclips s10_01_fridge_cat_on_table=mem_02_free_range_chicken:7;s10_03_eating_two_shot=mem_03_the_galley_meal:6;s10_14_explore_seated_cat_on_lap=mem_04_his_head_in_her_lap:6';
  "venus"    = '-testspeed 2 -chapter2 -quick -stopafter 240 -memclips v07b_b01_insert_hand_in_the_grip=mem_05_the_venus_naming:9;v13a_b01_points_at_him=mem_06_look_after_the_ship:8';
  "temple"   = '-testspeed 2 -venusselect C03 -stopafter 200 -recstills v14b_c03_guardian_standing_cape=rec_1_venus';
  "mars"     = '-testspeed 2 -marscp 6 -stopafter 300 -recstills m40_the_guard_and_the_caretaker_working=rec_2_mars;m30_the_worker_and_the_caretaker=rec_2b_mars;m31_inside_the_temple=rec_2c_mars';
  "biaveh"   = '-testspeed 2 -biacp 4 -stopafter 300 -memclips b32_relay_three_restored=mem_07_the_quiet_passage_reconnection:14';
  "race"     = '-testspeed 2 -catcp 3 -stopafter 200 -memclips _race_=mem_08_the_cattalus_race:7';
  "city"     = '-testspeed 2 -catcp 0 -stopafter 420 -recstills c05_juniper_approaches=rec_6_cattalus;c03_a_cat_waits=rec_6b_cattalus;c16_the_stewards_desk=rec_6c_cattalus;c17_duchess=rec_6d_cattalus -memclips c06_kneeling_with_juniper=mem_09_juniper_street:7';
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
