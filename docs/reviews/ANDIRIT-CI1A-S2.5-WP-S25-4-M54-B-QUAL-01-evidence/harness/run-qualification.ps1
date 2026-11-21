#!/usr/bin/env pwsh
# M54-B-QUAL-01 reviewer harness runner (reviewer-owned).
# Builds the harness against the PINNED production export and executes all
# pre-registered exercises with coverlet instrumentation for refusal-path
# evidence. Usage:
#   powershell -ExecutionPolicy Bypass -File run-qualification.ps1 `
#     -RepoRoot <pinned exact-byte export> [-Evidence <evidence dir>]
param(
    [Parameter(Mandatory)][string]$RepoRoot,
    [string]$Evidence = "D:\m54b-qual"
)
$ErrorActionPreference = 'Stop'
$env:TMP = "$Evidence\tmp"; $env:TEMP = "$Evidence\tmp"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'; $env:DOTNET_NOLOGO = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
New-Item -ItemType Directory -Force -Path $env:TMP, "$Evidence\receipts", "$Evidence\coverage", "$Evidence\trx" | Out-Null
$env:M54B_RECEIPTS = "$Evidence\receipts"
$harness = Join-Path $PSScriptRoot '.'

# 1. Build the reopen host (distinct process) and the test host.
dotnet build "$harness\M54BReopen\M54BReopen.csproj" -c Release -p:RepoRoot=$RepoRoot -p:NuGetAudit=false --ignore-failed-sources
dotnet build "$harness\M54BHarness.Tests.csproj" -c Release -p:RepoRoot=$RepoRoot -p:NuGetAudit=false --ignore-failed-sources
$env:M54B_REOPEN_DLL = "$harness\bin\Release\net10.0\M54BReopen\..\..\..\..\M54BReopen\bin\Release\net10.0\Andirit.CSharp.Tests.dll"
# (the reopen project emits to its own output directory)
$env:M54B_REOPEN_DLL = (Get-Item "$harness\M54BReopen\bin\Release\net10.0\Andirit.CSharp.Tests.dll").FullName

# 2. Execute all exercises with coverage instrumentation (no coverage gate:
#    the harness intentionally exercises only the refusal paths).
dotnet test "$harness\M54BHarness.Tests.csproj" -c Release --no-build `
    /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura "/p:CoverletOutput=$Evidence/coverage/" `
    '/p:Include=[Andirit]%2c[Andirit.CSharp]%2c[Andirit.LatticeDb]' `
    --logger "trx;LogFileName=$Evidence\trx\m54b-results.trx" *> "$Evidence\qualification-run.log"

# 3. Independently verify the refusal-path coverage.
python "$PSScriptRoot\verify-refusal-lines.py" "$Evidence\coverage\coverage.cobertura.xml" "$Evidence\receipts\refusal-path-coverage.json"
"harness exit: $LASTEXITCODE"
exit $LASTEXITCODE