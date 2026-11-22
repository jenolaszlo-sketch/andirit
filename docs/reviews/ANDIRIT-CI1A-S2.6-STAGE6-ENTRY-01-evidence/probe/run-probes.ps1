#!/usr/bin/env pwsh
# Stage 6 entry probe driver — ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01.
#
# Runs every probe scenario in its own process so that a native abort or an
# unexpected exit cannot contaminate sibling scenarios, and writes one
# receipt per scenario plus an environment receipt.
#
# Usage:
#   powershell -ExecutionPolicy Bypass -File run-probes.ps1 -Work <scratch/work-dir> -Out <receipt-dir>

param(
    [string]$Work = "D:\m54b-stage6\work",
    [string]$Out = "D:\m54b-stage6\receipts"
)

$ErrorActionPreference = 'Stop'
$probe = Join-Path $PSScriptRoot 'Stage6Probe.csproj'
$scenarios = @(
    'api-surface',
    'pre-invocation',
    'managed-wait',
    'native-window',
    'post-commit',
    'commit-unknown',
    'dispose-race',
    'cleanup',
    'control',
    'reopen'
)

New-Item -ItemType Directory -Force -Path $Work, $Out | Out-Null

$environment = Join-Path $Out 'environment.txt'
"probe-harness: $probe" | Set-Content $environment -Encoding utf8
"generated: $([DateTimeOffset]::UtcNow.ToString('o'))" | Add-Content $environment -Encoding utf8
"dotnet --version: $(& dotnet --version)" | Add-Content $environment -Encoding utf8
"dotnet --info:" | Add-Content $environment -Encoding utf8
(& dotnet --info 2>&1 | Out-String).TrimEnd() | Add-Content $environment -Encoding utf8
"host os: $([System.Environment]::OSVersion.VersionString)" | Add-Content $environment -Encoding utf8
"process architecture: $env:PROCESSOR_ARCHITECTURE" | Add-Content $environment -Encoding utf8

$summary = Join-Path $Out 'probe-summary.txt'
"scenario|exit-code|checks-passed|checks-failed" | Set-Content $summary -Encoding utf8

foreach ($scenario in $scenarios) {
    $scenarioWork = Join-Path $Work $scenario
    $receipt = Join-Path $Out "$scenario.log"
    New-Item -ItemType Directory -Force -Path $scenarioWork | Out-Null

    "scenario: $scenario" | Set-Content $receipt -Encoding utf8
    "work-dir: $scenarioWork" | Add-Content $receipt -Encoding utf8
    "command: dotnet run --project Stage6Probe.csproj -c Release --no-build -- $scenario <work-dir>" | Add-Content $receipt -Encoding utf8
    "" | Add-Content $receipt -Encoding utf8

    $output = & dotnet run --project $probe -c Release --no-build -- $scenario $scenarioWork 2>&1
    $exitCode = $LASTEXITCODE
    ($output | Out-String).TrimEnd() | Add-Content $receipt -Encoding utf8
    "" | Add-Content $receipt -Encoding utf8
    "exit-code: $exitCode" | Add-Content $receipt -Encoding utf8

    $passed = @($output | Where-Object { $_ -match '^\|CHECK\|.*\|PASS\|' -or $_ -match '^CHECK\|.*\|PASS\|' }).Count
    $failed = @($output | Where-Object { $_ -match '^\|CHECK\|.*\|FAIL\|' -or $_ -match '^CHECK\|.*\|FAIL\|' }).Count
    "$scenario|$exitCode|$passed|$failed" | Add-Content $summary -Encoding utf8
    Write-Host "$scenario exit=$exitCode passed=$passed failed=$failed"
}

Write-Host "receipts: $Out"
