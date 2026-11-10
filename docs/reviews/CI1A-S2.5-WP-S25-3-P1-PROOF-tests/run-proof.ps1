# ANDIRIT-CI1A-S2.5-WP-S25-3-P1-PROOF-01 — proof orchestration.
# Runs the oracle (Part A), the integrated publish/reopen/recovery verbs (Part B, fresh
# processes per verb), the regression gates, and writes receipts into the P1 evidence dir.
# The proof database lives outside the repository (system temp).
#
# Oracle exit handling: the oracle verb exits non-zero because the three v1 §7 row-13
# expectations fail against v1-as-written (findings F-P1-02). That is expected evidence,
# not a script failure. This script therefore verifies the oracle receipt structurally:
# KA must be 10/10, forms 9/9, and every matrix failure must be one of R13a/R13b/R13c.
[CmdletBinding()]
param(
    [string]$RepoRoot
)

$ErrorActionPreference = 'Stop'
if (-not $RepoRoot) { $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../../..")).Path }
$evidence = Join-Path $RepoRoot 'docs/reviews/CI1A-S2.5-WP-S25-3-P1-PROOF-evidence'
$project = Join-Path $PSScriptRoot 'P1Proof.csproj'
$db = Join-Path $env:TEMP ('p1-proof-' + [guid]::NewGuid().ToString('N').Substring(0, 8) + '.ltdb')
$exitCode = 0

New-Item -ItemType Directory -Force -Path $evidence | Out-Null

function Invoke-Verb([string]$verb, [string]$out, [string]$log, [string[]]$extra) {
    "command: dotnet run --project P1Proof.csproj -c Release -- $verb $($extra -join ' ') $out" | Out-File $log -Encoding utf8
    & dotnet run --project $project -c Release --no-build -- $verb @extra $out *>&1 | Out-File -Append -Encoding utf8 $log
    return $LASTEXITCODE
}

Write-Output '=== P1 build ==='
"command: dotnet build P1Proof.csproj -c Release`n" | Out-File (Join-Path $evidence 'build.log') -Encoding utf8
& dotnet build $project -c Release *>&1 | Out-File -Append -Encoding utf8 (Join-Path $evidence 'build.log')
"exit_code=$LASTEXITCODE" | Out-File -Append -Encoding utf8 (Join-Path $evidence 'build.log')
if ($LASTEXITCODE -ne 0) { throw 'P1 harness build failed' }

Write-Output '=== Part A: oracle ==='
$c = Invoke-Verb 'oracle' (Join-Path $evidence 'Q1-ORACLE.json') (Join-Path $evidence 'Q1-ORACLE.log') @()
Write-Output "oracle exit: $c (non-zero is expected: v1 row-13 text defects, findings F-P1-01/F-P1-02)"
$oracle = Get-Content (Join-Path $evidence 'Q1-ORACLE.json') -Raw | ConvertFrom-Json
$matrixFailures = @($oracle.matrix | Where-Object { -not $_.pass } | ForEach-Object { $_.id })
$unexpectedMatrix = @($matrixFailures | Where-Object { $_ -notin @('R13a', 'R13b', 'R13c') })
function Test-AllPassed([string]$fraction) {
    $parts = $fraction -split '/'
    return $parts.Count -eq 2 -and $parts[0] -eq $parts[1]
}
if (-not (Test-AllPassed $oracle.knownAnswersPass) -or -not (Test-AllPassed $oracle.formCasesPass) -or $unexpectedMatrix.Count -ne 0) {
    Write-Output "ORACLE UNEXPECTED RESULT: ka=$($oracle.knownAnswersPass) forms=$($oracle.formCasesPass) failures=$($matrixFailures -join ',')"
    $exitCode = 1
}

Write-Output '=== Part B: publish (S1-S5) ==='
$c = Invoke-Verb 'publish' (Join-Path $evidence 'Q2-INTEGRATION.json') (Join-Path $evidence 'Q2-INTEGRATION.log') @($db)
Write-Output "publish exit: $c"
if ($c -ne 0) { $exitCode = 1 }

Write-Output '=== Part B: reopen fresh process (S6-S7) ==='
$c = Invoke-Verb 'reopen' (Join-Path $evidence 'Q3-REOPEN.json') (Join-Path $evidence 'Q3-REOPEN.log') @($db)
Write-Output "reopen exit: $c"
if ($c -ne 0) { $exitCode = 1 }

Write-Output '=== Part B: recovery fresh process (S8) ==='
$c = Invoke-Verb 'recovery' (Join-Path $evidence 'Q4-RECOVERY.json') (Join-Path $evidence 'Q4-RECOVERY.log') @($db)
Write-Output "recovery exit: $c"
if ($c -ne 0) { $exitCode = 1 }

Write-Output '=== Toolchain provenance ==='
"command: dotnet --info`n" | Out-File (Join-Path $evidence 'toolchain.log') -Encoding utf8
& dotnet --info *>&1 | Out-File -Append -Encoding utf8 (Join-Path $evidence 'toolchain.log')

Write-Output '=== Regression gates ==='
"command: dotnet build Andirit.slnx -c Release`n" | Out-File (Join-Path $evidence 'release-build.log') -Encoding utf8
& dotnet build (Join-Path $RepoRoot 'Andirit.slnx') -c Release *>&1 | Out-File -Append -Encoding utf8 (Join-Path $evidence 'release-build.log')
"exit_code=$LASTEXITCODE" | Out-File -Append -Encoding utf8 (Join-Path $evidence 'release-build.log')
if ($LASTEXITCODE -ne 0) { $exitCode = 1 }

"`ncommand: dotnet test Andirit.slnx -c Release --no-build`n" | Out-File (Join-Path $evidence 'full-solution-tests.log') -Encoding utf8
& dotnet test (Join-Path $RepoRoot 'Andirit.slnx') -c Release --no-build *>&1 | Out-File -Append -Encoding utf8 (Join-Path $evidence 'full-solution-tests.log')
"exit_code=$LASTEXITCODE" | Out-File -Append -Encoding utf8 (Join-Path $evidence 'full-solution-tests.log')
if ($LASTEXITCODE -ne 0) { $exitCode = 1 }

"`ncommand: dotnet test tests/Andirit.LatticeDb.Tests --filter Store_Rejects|Refusal`n" | Out-File (Join-Path $evidence 'focused-historical-tests.log') -Encoding utf8
& dotnet test (Join-Path $RepoRoot 'tests/Andirit.LatticeDb.Tests/Andirit.LatticeDb.Tests.csproj') -c Release --no-build --filter "FullyQualifiedName~Store_Rejects|FullyQualifiedName~Refusal" *>&1 | Out-File -Append -Encoding utf8 (Join-Path $evidence 'focused-historical-tests.log')
"exit_code=$LASTEXITCODE" | Out-File -Append -Encoding utf8 (Join-Path $evidence 'focused-historical-tests.log')
if ($LASTEXITCODE -ne 0) { $exitCode = 1 }

"`ncommand: frozen R2 oracle`n" | Out-File (Join-Path $evidence 'oracle.log') -Encoding utf8
& dotnet run --project (Join-Path $RepoRoot 'docs/planning/adr-ci1-01-ci1a-s2-0/oracle/SemanticCorpusOracle.csproj') -c Release -- --corpus (Join-Path $RepoRoot 'docs/planning/adr-ci1-01-ci1a-s2-0/corpus') --expected (Join-Path $RepoRoot 'docs/planning/adr-ci1-01-ci1a-s2-0/oracle/expected/oracle-expected-r2.json') --output (Join-Path $evidence 'oracle-r2.json') *>&1 | Out-File -Append -Encoding utf8 (Join-Path $evidence 'oracle.log')
"exit_code=$LASTEXITCODE" | Out-File -Append -Encoding utf8 (Join-Path $evidence 'oracle.log')
if ($LASTEXITCODE -ne 0) { $exitCode = 1 }

"`ncommand: dotnet format Andirit.slnx --verify-no-changes`n" | Out-File (Join-Path $evidence 'format.log') -Encoding utf8
& dotnet format (Join-Path $RepoRoot 'Andirit.slnx') --verify-no-changes *>&1 | Out-File -Append -Encoding utf8 (Join-Path $evidence 'format.log')
"exit_code=$LASTEXITCODE" | Out-File -Append -Encoding utf8 (Join-Path $evidence 'format.log')
if ($LASTEXITCODE -ne 0) { $exitCode = 1 }

Write-Output '=== summaries ==='
$fullSummary = (Select-String -Path (Join-Path $evidence 'full-solution-tests.log') -Pattern 'Passed!|Failed!' | ForEach-Object { $_.Line.Trim() }) -join ' ;; '
$focusedSummary = (Select-String -Path (Join-Path $evidence 'focused-historical-tests.log') -Pattern 'Passed!|Failed!' | ForEach-Object { $_.Line.Trim() }) -join ' ;; '
$frozen = Get-Item (Join-Path $evidence 'oracle-r2.json')
$frozenHash = (Get-FileHash -Algorithm SHA256 $frozen.FullName).Hash.ToLowerInvariant()
$contract = Join-Path $RepoRoot 'docs/planning/adr-ci1-01-ci1a-s2-5/s01-canonical-form-contract.md'
$contractHash = (Get-FileHash -Algorithm SHA256 $contract).Hash.ToLowerInvariant()

$summary = [ordered]@{
    commission      = 'ANDIRIT-CI1A-S2.5-WP-S25-3-P1-PROOF-01'
    partA           = "knownAnswers=$($oracle.knownAnswersPass) forms=$($oracle.formCasesPass) matrix=$($oracle.matrixPass) matrixFailures=$($matrixFailures -join ',')"
    partB           = "receipts present: Q2-INTEGRATION.json Q3-REOPEN.json Q4-RECOVERY.json"
    fullSolution    = $fullSummary
    focusedRefusals = $focusedSummary
    frozenOracle    = "bytes=$($frozen.Length) sha256=$frozenHash expectedBytes=146851 expectedSha256=97f6c5e5a4cc77e089481c7643732b7a07a31d483c2643bd7a95fdad7ced39c9 match=$($frozen.Length -eq 146851 -and $frozenHash -eq '97f6c5e5a4cc77e089481c7643732b7a07a31d483c2643bd7a95fdad7ced39c9')"
    contract        = "bytes=$((Get-Item $contract).Length) sha256=$contractHash"
    proofDatabase   = $db
}
$summary.GetEnumerator() | ForEach-Object { "$($_.Key): $($_.Value)" } | Out-File (Join-Path $evidence 'receipt-summary.log') -Encoding utf8
$summary.GetEnumerator() | ForEach-Object { "$($_.Key): $($_.Value)" }
if ($frozen.Length -ne 146851 -or $frozenHash -ne '97f6c5e5a4cc77e089481c7643732b7a07a31d483c2643bd7a95fdad7ced39c9') {
    Write-Output 'FROZEN ORACLE ARTIFACT MISMATCH'
    $exitCode = 1
}

Write-Output ("proof database retained for inspection: " + $db)
exit $exitCode
