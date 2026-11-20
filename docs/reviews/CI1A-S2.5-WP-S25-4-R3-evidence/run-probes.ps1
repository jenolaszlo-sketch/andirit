$ErrorActionPreference = 'Stop'
$work = $PSScriptRoot
$root = "$work/production"
$evidence = "$work/evidence"
$out = "$evidence/probes/corrected"
New-Item -ItemType Directory -Path $out -Force | Out-Null
$env:DOTNET_CLI_HOME = "$work/dotnet-home"
$env:NUGET_PACKAGES = Join-Path $env:USERPROFILE '.nuget/packages'
$env:S254_QUAL02_PROFILE = 'corrected'
function Logged([string]$name, [string[]]$cli) {
    $log = "$out/$name.log"
    "Command: dotnet $($cli -join ' ')" | Set-Content $log
    dotnet @cli *>> $log
    $code = $LASTEXITCODE
    "Exit code: $code" | Add-Content $log
    Write-Host "$name $code"
    if ($code -ne 0) { $script:failed = $true }
}
$failed = $false
Logged 'build' @('build', "$work/probe/Qual.csproj", '-c', 'Release', "-p:RepoRoot=$root", '-p:NuGetAudit=false')
if ($failed) { exit 1 }
$dll = "$work/probe/bin/Release/net10.0/Andirit.CSharp.Tests.dll"
$fixture = "$root/docs/reviews/CI1A-S2.5-WP-S25-4-QUAL-01-evidence/fixture"
function Probe([string]$case, [string]$store, [string]$inputs = $fixture, [string]$receipt = $case) {
    Logged $receipt @($dll, $case, $store, "$out/$receipt.json", $inputs)
}
foreach ($case in 'b3','b3-mixed','control-ctor','control-property','control-reference','control-empty','control-incomplete','control-multiple','m54-direct','review-transition','review-none','review-refusal','state-known','state-unknown','state-supersession') {
    foreach ($phase in 'publish','reopen') { Probe "$case-$phase" "$work/stores/r3-$case.ltdb" }
}
foreach ($case in 'control-ctor','control-empty','m54-direct') {
    $prior = "$work/stores/r2-$case.ltdb"
    $copy = "$work/stores/r3-legacy-$case.ltdb"
    Copy-Item -LiteralPath $prior -Destination $copy
    if (Test-Path "$prior-wal") { Copy-Item -LiteralPath "$prior-wal" -Destination "$copy-wal" }
    Probe "$case-reopen" $copy $fixture "legacy-$case-reopen"
}
foreach ($phase in 'b6-publish','b7-conflict','b7-reopen','b6-delete-a','b6-reopen-a','b6-delete-b','b6-reopen-b') {
    Probe $phase "$work/stores/r3-contributors.ltdb" $out
}
Probe 'd3' "$work/stores/r3-d3.ltdb"
Probe 'd4-schedule' "$work/stores/r3-d4.ltdb"
foreach ($phase in 'd5-namespace','d5-schema','d5-page') {
    $copy = "$work/stores/r3-$phase.ltdb"
    Copy-Item -LiteralPath "$work/stores/r3-d3.ltdb" -Destination $copy
    if (Test-Path "$work/stores/r3-d3.ltdb-wal") { Copy-Item -LiteralPath "$work/stores/r3-d3.ltdb-wal" -Destination "$copy-wal" }
    Probe $phase $copy
}
if ($failed) { exit 1 }
