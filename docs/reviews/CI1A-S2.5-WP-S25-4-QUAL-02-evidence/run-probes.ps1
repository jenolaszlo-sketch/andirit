param([Parameter(Mandatory)][string]$Work)
$ErrorActionPreference = 'Stop'
$env:NUGET_PACKAGES = Join-Path $env:USERPROFILE '.nuget/packages'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'
$package = "$Work/evidence/probes"
$fixture = "$Work/corrected/docs/reviews/CI1A-S2.5-WP-S25-4-QUAL-01-evidence/fixture"
function Invoke-Logged([string]$Path, [string[]]$Cli) {
    "Command: dotnet $($Cli -join ' ')" | Set-Content -LiteralPath $Path
    & dotnet @Cli 2>&1 | Out-File -LiteralPath $Path -Append
    $code = $LASTEXITCODE
    "Exit code: $code" | Add-Content -LiteralPath $Path
    Write-Host "$(Split-Path -Leaf $Path) = $code"
    return $code
}
foreach ($profile in 'baseline','corrected') {
    $env:S254_QUAL02_PROFILE = $profile
    $output = "$package/$profile"
    $scratch = "$Work/database/$profile"
    $source = "$Work/probe-$profile"
    New-Item -ItemType Directory -Path $output,$scratch -Force | Out-Null
    if (!(Test-Path -LiteralPath $source)) {
        Copy-Item -LiteralPath "$Work/probe" -Destination $source -Recurse
    }
    $build = Invoke-Logged "$output/build.log" @('build',"$source/Qual.csproj",'-c','Release',"-p:RepoRoot=$Work/$profile",'-p:NuGetAudit=false')
    if ($build -ne 0) { continue }
    $dll = "$source/bin/Release/net10.0/Andirit.CSharp.Tests.dll"
    Get-FileHash -LiteralPath $dll | Format-List | Out-File "$output/executable.txt"
    function Probe([string]$Run, [string]$Phase, [string]$Db, [string]$InputPath = $fixture) {
        $null = Invoke-Logged "$output/$Run.log" @($dll,$Phase,$Db,"$output/$Run.json",$InputPath)
    }
    Probe 'b3-publish' 'b3-publish' "$scratch/b3.ltdb"
    Probe 'b3-reopen' 'b3-reopen' "$scratch/b3.ltdb"
    if ($profile -ne 'corrected') { continue }
    Probe 'b3-mixed-publish' 'b3-mixed-publish' "$scratch/mixed.ltdb"
    Probe 'b3-mixed-reopen' 'b3-mixed-reopen' "$scratch/mixed.ltdb"
    Probe 'b3-missing' 'b3-missing' "$scratch/missing.ltdb"
    Probe 'b3-ambiguous' 'b3-ambiguous' "$scratch/ambiguous.ltdb"
    foreach ($phase in 'b6-publish','b7-conflict','b7-reopen','b6-delete-a','b6-reopen-a','b6-delete-b','b6-reopen-b') {
        Probe $phase $phase "$scratch/contributors.ltdb" $output
    }
    foreach ($sample in 1..3) {
        Probe "d3-$sample" 'd3' "$scratch/d3-$sample.ltdb"
    }
    Probe 'd4-schedule' 'd4-schedule' "$scratch/schedule.ltdb"
    foreach ($phase in 'd5-namespace','d5-schema','d5-page') {
        Copy-Item -LiteralPath "$scratch/d3-1.ltdb" -Destination "$scratch/$phase.ltdb"
        if (Test-Path -LiteralPath "$scratch/d3-1.ltdb-wal") {
            Copy-Item -LiteralPath "$scratch/d3-1.ltdb-wal" -Destination "$scratch/$phase.ltdb-wal"
        }
        Probe $phase $phase "$scratch/$phase.ltdb"
    }
}
