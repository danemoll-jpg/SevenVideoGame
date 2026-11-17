# Round 8: builds 7: The Observer's launcher (Launcher/) with Visual Studio 18's C++ compiler (x64, static CRT).
#   powershell -File Tools/Build-Launcher.ps1            -> Builds/Launcher/SevenLauncher.exe (the windowed launcher)
#   powershell -File Tools/Build-Launcher.ps1 -CoreOnly  -> Builds/Launcher/LauncherChecks.exe (console: verify / status / check; used by the
#                                                           release tools to check signatures and the starter with the launcher's own code)
# The Windows SDK comes from vcvars64 (the installed Windows Kits 10.0.28000.0). -Sdk <folder> uses an unpacked NuGet copy of the SDK instead
# (layout microsoft.windows.sdk.cpp/c/{Include,bin}/10.0.28000.0, microsoft.windows.sdk.cpp.x64/c/{um,ucrt}/x64).
# Start it hidden at Below Normal priority, never while Unity or SevenDemo is running (one heavy job at a time).
param([switch]$CoreOnly, [string]$Sdk = '')
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$out=Join-Path $root 'Builds/Launcher'
$obj=Join-Path $out 'obj'
New-Item -ItemType Directory -Force $out, $obj | Out-Null
$vc='C:\Program Files\Microsoft Visual Studio\18\Community\VC\Auxiliary\Build\vcvars64.bat'
if(-not (Test-Path $vc)){throw "Visual Studio 18 C++ tools not found: $vc"}
$script=Join-Path $obj 'build-launcher.cmd'
$entry=if($CoreOnly){'core_cli.cpp'}else{'launcher.cpp'}
$subsystem=if($CoreOnly){'CONSOLE'}else{'WINDOWS'}
$exe=if($CoreOnly){'LauncherChecks.exe'}else{'SevenLauncher.exe'}
$sdkEnv=''
$rc='rc.exe'
if($Sdk){
  $rc=Join-Path $Sdk 'microsoft.windows.sdk.cpp/c/bin/10.0.28000.0/x64/rc.exe'
  $sdkEnv="set `"INCLUDE=$Sdk\microsoft.windows.sdk.cpp\c\Include\10.0.28000.0\ucrt;$Sdk\microsoft.windows.sdk.cpp\c\Include\10.0.28000.0\shared;$Sdk\microsoft.windows.sdk.cpp\c\Include\10.0.28000.0\um;%INCLUDE%`"`r`nset `"LIB=$Sdk\microsoft.windows.sdk.cpp.x64\c\um\x64;$Sdk\microsoft.windows.sdk.cpp.x64\c\ucrt\x64;%LIB%`""
}
$rcStep=if($CoreOnly){''}else{"`"$rc`" /nologo /fo `"$obj\launcher.res`" `"$root\Launcher\launcher.rc`"`r`nif errorlevel 1 exit /b %errorlevel%"}
$res=if($CoreOnly){''}else{"`"$obj\launcher.res`""}
@"
@echo off
call "$vc" >nul
if errorlevel 1 exit /b %errorlevel%
$sdkEnv
cl /nologo /TC /O2 /MT /c /Fo:"$obj\miniz.obj" "$root\Launcher\vendor\miniz.c"
if errorlevel 1 exit /b %errorlevel%
$rcStep
cl /nologo /utf-8 /std:c++17 /EHsc /O2 /MT /W4 /Fe:"$out\$exe" /Fo:"$obj\\" "$root\Launcher\$entry" "$root\Launcher\core.cpp" "$root\Launcher\store.cpp" "$obj\miniz.obj" $res /link /SUBSYSTEM:$subsystem
"@ | Set-Content -LiteralPath $script
$ErrorActionPreference='Continue'; & cmd.exe /d /c $script 2>&1 | Out-Host; $code=$LASTEXITCODE; $ErrorActionPreference='Stop'   # (vcvars writes a harmless vswhere line to stderr)
if($code -ne 0){throw "Launcher compilation failed: $code"}
Get-FileHash -LiteralPath "$out/$exe"
