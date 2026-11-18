# Compile gameplay against the installed Mono player, in an isolated copy. This does not
# import assets or replace the shipped build. A licensed editor is required for shader QA.
param([switch]$RunChecks)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repo 'unity/PhewaValley'
$source = Join-Path $project 'Builds/Windows'
$preview = Join-Path $project 'Builds/Validation'
if (!(Test-Path (Join-Path $source 'PhewaValley_Data/Managed/Assembly-CSharp.dll'))) {
    throw 'An existing Mono Windows player is required in unity/PhewaValley/Builds/Windows.'
}
$editors = Join-Path $env:ProgramFiles 'Unity/Hub/Editor'
$editor = Get-ChildItem -LiteralPath $editors -Directory | Sort-Object Name -Descending | Select-Object -First 1
$sdk = Join-Path $editor.FullName 'Editor/Data/DotNetSdk'
$compiler = Get-ChildItem -Path "$sdk/sdk/*/Roslyn/bincore/csc.dll" | Select-Object -First 1
if (!$compiler) { throw 'Unity bundled C# compiler was not found.' }
New-Item -ItemType Directory -Force -Path $preview | Out-Null
foreach ($item in Get-ChildItem -LiteralPath $source) {
    if ($item.Name -notlike '*BackUpThisFolder*') { Copy-Item -LiteralPath $item.FullName -Destination $preview -Recurse -Force }
}
# The preview must use the current authored mission content, rather than the old build copy.
Copy-Item -LiteralPath "$project/Assets/StreamingAssets/PhewaValley/world.json" -Destination "$preview/PhewaValley_Data/StreamingAssets/PhewaValley/world.json" -Force
$response = @('-target:library', '-nostdlib+', '-langversion:9.0', '-nologo',
    "-out:`"$preview/PhewaValley_Data/Managed/Assembly-CSharp.dll`"")
$response += Get-ChildItem -Path "$source/PhewaValley_Data/Managed/*.dll" |
    Where-Object Name -ne 'Assembly-CSharp.dll' | ForEach-Object { "-r:`"$($_.FullName)`"" }
$response += Get-ChildItem -Path "$project/Assets/PhewaValley/Scripts" -Filter '*.cs' -Recurse |
    ForEach-Object { "`"$($_.FullName)`"" }
$responseFile = Join-Path $preview 'compile.rsp'
$response | Set-Content -LiteralPath $responseFile -Encoding utf8
& "$sdk/dotnet.exe" $compiler.FullName "@$responseFile"
if ($LASTEXITCODE -ne 0) { throw 'Unity gameplay compilation failed.' }
if ($RunChecks) {
    $log = Join-Path $repo 'docs/verification/controller-runtime.log'
    New-Item -ItemType Directory -Force -Path (Split-Path $log) | Out-Null
    $run = Start-Process -FilePath "$preview/PhewaValley.exe" -ArgumentList "-batchmode -nographics -phewa-verify -phewa-skip-shader-check -logFile `"$log`"" -WindowStyle Hidden -PassThru
    if (!$run.WaitForExit(180000)) { $run.Kill(); throw "Controller checks timed out. See $log" }
    Get-Content -LiteralPath $log | Select-String '\[verify\]'
    if ($run.ExitCode -ne 0) { throw "Controller checks failed ($($run.ExitCode)). See $log" }
}
Write-Output "Gameplay preview: $preview/PhewaValley.exe"
Write-Output 'New shaders/assets require a licensed Unity editor build; they are not imported into this preview.'
