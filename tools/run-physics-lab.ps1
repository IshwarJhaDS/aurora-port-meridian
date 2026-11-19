$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$player = Join-Path $repo 'unity/PhewaValley/Builds/Validation/PhewaValley.exe'
if (!(Test-Path -LiteralPath $player)) { throw 'Build the Unity validation player before opening the physics lab.' }
Start-Process -FilePath $player -ArgumentList '-aurora-physics-lab -screen-fullscreen 0 -screen-width 1280 -screen-height 720'
