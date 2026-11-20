#!/usr/bin/env pwsh
# Frozen R2 semantic oracle on the pinned export (the oracle project is NOT part
# of Andirit.slnx, so it needs its own restore before run; QUAL-04 method).
param(
    [Parameter(Mandatory)][string]$Root,
    [Parameter(Mandatory)][string]$Evidence
)
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $Root
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'; $env:DOTNET_NOLOGO = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:TMP = "D:\gate-qualification\ANDIRIT-CI1A-S2.5-WP-S25-4-GATE-QUALIFICATION-01\tmp"
$env:TEMP = $env:TMP
$p = 'docs/planning/adr-ci1-01-ci1a-s2-0/oracle/SemanticCorpusOracle.csproj'
$log = Join-Path $Evidence 'oracle.log'
$file = Join-Path $Evidence 'oracle-r2.json'
$restore = Join-Path $Evidence 'oracle-restore.log'

function Gate([string]$Name, [string[]]$Cli, [string]$Log) {
    Push-Location -LiteralPath $Root
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        "Working directory: $Root" | Set-Content $Log
        "Command: dotnet $($Cli -join ' ')" | Add-Content $Log
        & dotnet @Cli *>> $Log
        $status = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $previous
        Pop-Location
    }
    "Exit code: $status" | Add-Content $Log
    Write-Host "$Name = $status"
    return $status
}

$r = Gate 'oracle-restore' @('restore', $p, '-p:NuGetAudit=false', '--ignore-failed-sources') $restore
if ($r -eq 0) {
    Gate 'oracle' @(
        'run', '--project', $p, '-c', 'Release', '-p:NuGetAudit=false', '--',
        '--corpus', 'docs/planning/adr-ci1-01-ci1a-s2-0/corpus',
        '--expected', 'docs/planning/adr-ci1-01-ci1a-s2-0/oracle/expected/oracle-expected-r2.json',
        '--output', $file) $log
    if (Test-Path -LiteralPath $file) {
        Get-FileHash -LiteralPath $file | Format-List | Out-File (Join-Path $Evidence 'oracle-hash.txt')
    }
}
Write-Host "oracle exit=$LASTEXITCODE"