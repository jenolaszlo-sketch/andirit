param([switch]$Baseline)
$ErrorActionPreference='Stop'
$work=Split-Path -Parent $PSScriptRoot
$profile=if($Baseline){'baseline'}else{'corrected'}
$root=Join-Path $work $(if($Baseline){'baseline'}else{'production'})
$probe=Join-Path $work $(if($Baseline){'baseline-probe'}else{'probe'})
$out=Join-Path $PSScriptRoot "probes/$profile"
$stores=Join-Path $work "stores/exact/$profile"
New-Item -ItemType Directory -Path $out,$stores -Force | Out-Null
$env:NUGET_PACKAGES=Join-Path $env:USERPROFILE '.nuget/packages'
$env:DOTNET_CLI_HOME=Join-Path $work 'dotnet-home'
$env:S254_QUAL02_PROFILE='corrected'
function Logged([string]$name,[string[]]$cli) {
    $log=Join-Path $out "$name.log"
    "UTC: $([DateTimeOffset]::UtcNow.ToString('o'))`nCommand: dotnet $($cli -join ' ')" | Set-Content $log
    & dotnet @cli 2>&1 | Out-File $log -Append
    $code=$LASTEXITCODE
    "Exit code: $code" | Add-Content $log
    Write-Host "$profile/$name = $code"
    return $code
}
if((Logged build @('build',"$probe/Qual.csproj",'-c','Release',"-p:RepoRoot=$root",'-p:NuGetAudit=false')) -ne 0){exit 1}
$dll="$probe/bin/Release/net10.0/Andirit.CSharp.Tests.dll"
Get-FileHash $dll | ConvertTo-Json | Set-Content "$out/executable.json"
$fixture="$root/docs/reviews/CI1A-S2.5-WP-S25-4-QUAL-01-evidence/fixture"
function Probe([string]$phase,[string]$db,[string]$inputs=$fixture) {
    $null=Logged $phase @($dll,$phase,$db,"$out/$phase.json",$inputs)
}
$cases=@('ctor','property','reference','incomplete','declared')
if(!$Baseline){$cases+=@('constructors','properties','groups','overlap','mixed','unbound','ambiguous','external','unrelated','multiple','mixedmissing','empty','local','inactive')}
foreach($case in $cases){foreach($phase in 'publish','reopen'){Probe "control-$case-$phase" "$stores/$case.ltdb"}}
if($Baseline){exit 0}
foreach($case in 'b3','b3-mixed','m54-transitive','m54-direct','m54-returned','m54-returneddirect','m54-interface','m54-interfacedirect','multiple-definitions','coverage-boundary','state-known','state-unknown','state-supersession','review-transition','review-none','review-refusal') {
    foreach($phase in 'publish','reopen'){Probe "$case-$phase" "$stores/$case.ltdb"}
}
Probe 'control-provenance-publish' "$stores/provenance.ltdb"
Probe 'identity-discriminators' "$stores/identity.ltdb"
foreach($phase in 'b6-publish','b7-conflict','b7-reopen','b6-delete-a','b6-reopen-a','b6-delete-b','b6-reopen-b'){Probe $phase "$stores/contributors.ltdb" $out}
Probe 'd3' "$stores/d3.ltdb"
Probe 'd4-schedule' "$stores/d4.ltdb"
foreach($phase in 'd5-namespace','d5-schema','d5-page') {
    Copy-Item -LiteralPath "$stores/d3.ltdb" -Destination "$stores/$phase.ltdb"
    if(Test-Path "$stores/d3.ltdb-wal"){Copy-Item -LiteralPath "$stores/d3.ltdb-wal" -Destination "$stores/$phase.ltdb-wal"}
    Probe $phase "$stores/$phase.ltdb"
}

