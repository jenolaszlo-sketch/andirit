#!/usr/bin/env pwsh
# ANDIRIT-CI1A-S2.5-WP-S25-4-GATE-QUALIFICATION-01 gate execution.
# Independent reviewer execution on the pinned exact-byte export.
# Method mirrors ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-04 (run-gates.ps1 /
# run-coverage-policy.ps1); no producer artifacts are reused as inputs.
param(
    [Parameter(Mandatory)][string]$Root,      # pinned exact-byte export root
    [Parameter(Mandatory)][string]$Evidence,  # gate log/output directory
    [string]$Config = "Release"
)
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $Root
$env:DOTNET_CLI_HOME = "D:\gate-qualification\ANDIRIT-CI1A-S2.5-WP-S25-4-GATE-QUALIFICATION-01\dotnet-home"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'; $env:DOTNET_NOLOGO = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
# C: system volume exhausted mid-build; build/temp scratch redirected to D:.
$env:TMP = "D:\gate-qualification\ANDIRIT-CI1A-S2.5-WP-S25-4-GATE-QUALIFICATION-01\tmp"
$env:TEMP = $env:TMP
New-Item -ItemType Directory -Force -Path $env:TMP | Out-Null
$ProgressPreference = 'SilentlyContinue'
New-Item -Path $Evidence -ItemType Directory -Force | Out-Null

function Gate([string]$Name, [string[]]$Cli, [string]$Workdir) {
    $file = Join-Path $Evidence "$Name.log"
    Push-Location -LiteralPath $Workdir
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'   # native stderr is expected output here
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

# --- environment identity (GQ-02 requirement) -------------------------------
"PSVersion: $($PSVersionTable.PSVersion)" | Set-Content (Join-Path $Evidence 'environment.txt')
"OS: $([System.Environment]::OSVersion.VersionString)" | Add-Content (Join-Path $Evidence 'environment.txt')
"UTC: $([System.DateTime]::UtcNow.ToString('o'))" | Add-Content (Join-Path $Evidence 'environment.txt')
Gate toolchain @('--info') $Root | Out-Null

# --- restore (fresh export has no obj/; CI restores before --no-restore) ----
$exits['restore'] = Gate restore @('restore','Andirit.slnx','-p:NuGetAudit=false','--ignore-failed-sources') $Root

# --- GQ-02: exact-byte formatting gate (binding CI command) -----------------
# CI: dotnet format Andirit.slnx --verify-no-changes --no-restore  (on Windows
# runner profile and in exact-byte profile alike; --no-restore requires the
# restore above).
$exits['format'] = Gate format @('format','Andirit.slnx','--no-restore','--verify-no-changes') $Root

# --- GQ-04: Release build ----------------------------------------------------
$exits['release'] = Gate release @('build','Andirit.slnx','-c',$Config,'--no-restore','-p:NuGetAudit=false','--ignore-failed-sources') $Root

if ($exits['release'] -eq 0) {

    # --- GQ-03: binding LatticeDb coverage gate (CI parameters verbatim) -------
    # CI: Include=[Andirit.LatticeDb]* Threshold=70 ThresholdType=line,branch
    #      CoverletOutputFormat=cobertura
    $covDir = Join-Path $Evidence 'coverage-latticedb'
    New-Item -Path $covDir -ItemType Directory -Force | Out-Null
    $exits['coverage-latticedb'] = Gate 'coverage-latticedb' @(
        'test','tests/Andirit.LatticeDb.Tests/Andirit.LatticeDb.Tests.csproj',
        '-c',$Config,'--no-restore',
        '/p:CollectCoverage=true',
        "/p:CoverletOutput=$covDir/",
        '/p:CoverletOutputFormat=cobertura',
        '/p:Include=[Andirit.LatticeDb]*',
        '/p:Threshold=70',
        '/p:ThresholdType=line%2cbranch') $Root

    # --- GQ-04: whole-solution tests (Release, no build) ----------------------
    $trx = Join-Path $Evidence 'trx'
    New-Item -Path $trx -ItemType Directory -Force | Out-Null
    $exits['full'] = Gate full @('test','Andirit.slnx','-c',$Config,'--no-restore','--no-build',
        '--logger','trx','--results-directory',$trx) $Root
}

[psobject]$exits | ConvertTo-Json | Out-File (Join-Path $Evidence 'gate-exits.json') -Encoding utf8
$exits.GetEnumerator() | ForEach-Object { Write-Host "gate $($_.Key) = $($_.Value)" }