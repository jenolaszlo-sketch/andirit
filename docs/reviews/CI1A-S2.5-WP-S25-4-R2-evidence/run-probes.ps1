param(
    [Parameter(Mandatory)][string]$Work,
    [Parameter(Mandatory)][string]$Root,
    [switch]$Baseline
)
$ErrorActionPreference = 'Stop'
$env:NUGET_PACKAGES = Join-Path $env:USERPROFILE '.nuget/packages'
$env:DOTNET_NOLOGO = '1'
$env:S254_QUAL02_PROFILE = 'corrected'
$fixture = "$Root/docs/reviews/CI1A-S2.5-WP-S25-4-QUAL-01-evidence/fixture"
$profile = if ($Baseline) { 'baseline' } else { 'corrected' }
$source = if ($Baseline) { "$Work/probe" } else { "$Work/corrected-probe" }
$out = "$Work/evidence/probes/$profile"
$stores = "$Work/probe-stores/$profile"
New-Item -ItemType Directory -Path $out,$stores -Force | Out-Null

function Invoke-Logged([string]$Name, [string[]]$Cli) {
    $log = "$out/$Name.log"
    "Command: dotnet $($Cli -join ' ')" | Set-Content -LiteralPath $log
    & dotnet @Cli 2>&1 | Out-File -LiteralPath $log -Append
    $code = $LASTEXITCODE
    "Exit code: $code" | Add-Content -LiteralPath $log
    Write-Host "$Name = $code"
    return $code
}

if ((Invoke-Logged build @('build',"$source/Qual.csproj",'-c','Release',"-p:RepoRoot=$Root",'-p:NuGetAudit=false')) -ne 0) { exit 1 }
$dll = "$source/bin/Release/net10.0/Andirit.CSharp.Tests.dll"
Get-FileHash -LiteralPath $dll | Format-List | Out-File "$out/executable.txt"

function Invoke-Probe([string]$Phase, [string]$Database, [string]$Inputs = $fixture) {
    $null = Invoke-Logged $Phase @($dll,$Phase,$Database,"$out/$Phase.json",$Inputs)
}
$controls = @('ctor','property','reference','declared','incomplete')
if (!$Baseline) { $controls += @('local','inactive','empty') }
foreach ($case in $controls) {
    foreach ($phase in 'publish','reopen') {
        Invoke-Probe "control-$case-$phase" "$stores/$case.ltdb"
    }
}
if ($Baseline) { exit 0 }
foreach ($case in 'b3','b3-mixed','m54-transitive','m54-direct','multiple-definitions','coverage-boundary','state-known','state-unknown','state-supersession') {
    foreach ($phase in 'publish','reopen') {
        Invoke-Probe "$case-$phase" "$stores/$case.ltdb"
    }
}
foreach ($phase in 'b6-publish','b7-conflict','b7-reopen','b6-delete-a','b6-reopen-a','b6-delete-b','b6-reopen-b') {
    Invoke-Probe $phase "$stores/contributors.ltdb" $out
}
Invoke-Probe 'd3' "$stores/d3.ltdb"
Invoke-Probe 'd4-schedule' "$stores/d4.ltdb"
foreach ($phase in 'd5-namespace','d5-schema','d5-page') {
    Copy-Item -LiteralPath "$stores/d3.ltdb" -Destination "$stores/$phase.ltdb"
    if (Test-Path -LiteralPath "$stores/d3.ltdb-wal") {
        Copy-Item -LiteralPath "$stores/d3.ltdb-wal" -Destination "$stores/$phase.ltdb-wal"
    }
    Invoke-Probe $phase "$stores/$phase.ltdb"
}

