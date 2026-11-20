#!/usr/bin/env pwsh
# ANDIRIT-CI1A-S2.5-WP-S25-4-GATE-QUALIFICATION-01 focused preservation gates.
# Named receipts for R1/R2/R3, Q03-F-01, merge/identity, refusal behavior and
# the frozen R2 oracle, executed on the pinned exact-byte export.
param(
    [Parameter(Mandatory)][string]$Root,
    [Parameter(Mandatory)][string]$Evidence
)
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $Root
$env:DOTNET_CLI_HOME = "D:\gate-qualification\ANDIRIT-CI1A-S2.5-WP-S25-4-GATE-QUALIFICATION-01\dotnet-home"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'; $env:DOTNET_NOLOGO = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:TMP = "D:\gate-qualification\ANDIRIT-CI1A-S2.5-WP-S25-4-GATE-QUALIFICATION-01\tmp"
$env:TEMP = $env:TMP
$ProgressPreference = 'SilentlyContinue'
New-Item -Path $Evidence -ItemType Directory -Force | Out-Null

function Gate([string]$Name, [string[]]$Cli, [string]$Workdir) {
    $file = Join-Path $Evidence "$Name.log"
    Push-Location -LiteralPath $Workdir
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        "Working directory: $Workdir" | Set-Content $file
        "Command: dotnet $($Cli -join ' ')" | Add-Content $file
        & dotnet @Cli *>> $file
        $status = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $previous
        Pop-Location
    }
    "Exit code: $status" | Add-Content $file
    Write-Host "$Name = $status"
    return $status
}

$exits = [ordered]@{}
$csharp = 'tests/Andirit.CSharp.Tests/Andirit.CSharp.Tests.csproj'
$native = 'tests/Andirit.LatticeDb.Tests/Andirit.LatticeDb.Tests.csproj'

# R3 complete-scope membership (Q03-F-01 remediation) + merge/identity union/conflict
# + lifecycle membership + refusal mapping (S24) + missing-authority Q03-F-01 test.
$exits['membership-identity'] = Gate membership-identity @(
    'test', $csharp, '-c', 'Release', '--no-build', '--no-restore',
    '--filter', 'FullyQualifiedName~CompleteScopeMembership|FullyQualifiedName~MissingCurrentAuthority|FullyQualifiedName~L04_IdenticalRealFactIdentity|FullyQualifiedName~L05_ContributorConflict|FullyQualifiedName~S24SurfaceRefusalMapping|FullyQualifiedName~IndexingLifecycle_RemovesProjectUnitWhenSolutionMembershipChanges') $Root

# R1 graph noninterference + R2 schema-2/legacy identity + R3 native remediation suite.
$exits['native-preservation'] = Gate native-preservation @(
    'test', $native, '-c', 'Release', '--no-build', '--no-restore',
    '--filter', 'FullyQualifiedName~N8_D3GraphNoninterference|FullyQualifiedName~HistoricalStorageIdentityTests|FullyQualifiedName~OccurrenceRemediationNativeTests') $Root

# Frozen R2 semantic oracle (public contract/identity byte preservation).
$p = 'docs/planning/adr-ci1-01-ci1a-s2-0'
$exits['oracle'] = Gate oracle @(
    'run', '--project', "$p/oracle/SemanticCorpusOracle.csproj", '-c', 'Release',
    '--no-restore', '-p:NuGetAudit=false', '--',
    '--corpus', "$p/corpus",
    '--expected', "$p/oracle/expected/oracle-expected-r2.json",
    '--output', "$Evidence/oracle-r2.json") $Root

[psobject]$exits | ConvertTo-Json | Out-File (Join-Path $Evidence 'preservation-exits.json') -Encoding utf8
$exits.GetEnumerator() | ForEach-Object { Write-Host "gate $($_.Key) = $($_.Value)" }