# ANDIRIT-CI1A-S2.5-WP-S25-3-P1-DELTA-01 — delta proof orchestration.
# Preflight/normative-input/preservation receipts, delta harness run, regression
# gates, and summaries written into the delta evidence directory. No database is
# created (the delta proof is compilation-level; it does not repeat the P1 store chain).
[CmdletBinding()]
param(
    [string]$RepoRoot
)

$ErrorActionPreference = 'Stop'
if (-not $RepoRoot) { $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../../..")).Path }
$evidence = Join-Path $RepoRoot 'docs/reviews/CI1A-S2.5-WP-S25-3-P1-DELTA-evidence'
$project = Join-Path $PSScriptRoot 'DeltaProof.csproj'
$exitCode = 0

New-Item -ItemType Directory -Force -Path $evidence | Out-Null

Write-Output '=== P1 delta build ==='
"command: dotnet build DeltaProof.csproj -c Release`n" | Out-File (Join-Path $evidence 'build.log') -Encoding utf8
& dotnet build $project -c Release *>&1 | Out-File -Append -Encoding utf8 (Join-Path $evidence 'build.log')
"exit_code=$LASTEXITCODE" | Out-File -Append -Encoding utf8 (Join-Path $evidence 'build.log')
if ($LASTEXITCODE -ne 0) { throw 'delta harness build failed' }

Write-Output '=== preflight ==='
$preflight = @(
    "head=$(git -C $RepoRoot rev-parse HEAD)"
    "tree=$(git -C $RepoRoot rev-parse 'HEAD^{tree}')"
    "branch=$(git -C $RepoRoot rev-parse --abbrev-ref HEAD)"
    "dirty_or_untracked_lines=$((git -C $RepoRoot status --porcelain --untracked-files=all | Measure-Object -Line).Lines)"
    "worktrees=$((git -C $RepoRoot worktree list | Measure-Object -Line).Lines)"
    "stashes=$((git -C $RepoRoot stash list | Measure-Object -Line).Lines)"
    "origin_main_local_ref=$(git -C $RepoRoot rev-parse origin/main)"
    "note=the recorded remote publication state is historical; no remote operation is performed by this run"
) -join "`n"
$preflight | Out-File (Join-Path $evidence 'preflight.log') -Encoding utf8

Write-Output '=== normative inputs ==='
$normative = @()
foreach ($f in @(
    'docs/planning/adr-ci1-01-ci1a-s2-5/s01-canonical-form-contract.md',
    'docs/planning/adr-ci1-01-ci1a-s2-5/s01-canonical-form-v1-clarification-01.md',
    'docs/decisions/adr-ci1-01-ci1a-s2-5-wp-s25-3-p1-contract-clarify-owner-01.md',
    'docs/decisions/adr-ci1-01-ci1a-s2-5-wp-s25-3-p0-contract-owner-01.md',
    'docs/decisions/adr-ci1-01-ci1a-s2-5-wp-s25-3-entry-owner-01.md')) {
    $full = Join-Path $RepoRoot $f
    $normative += "$f bytes=$((Get-Item $full).Length) sha256=$((Get-FileHash -Algorithm SHA256 $full).Hash.ToLowerInvariant())"
}
$normative -join "`n" | Out-File (Join-Path $evidence 'normative-inputs.log') -Encoding utf8

Write-Output '=== preservation checks ==='
$preservation = @()
& git -C $RepoRoot diff --quiet 90c5fbe HEAD -- docs/reviews/CI1A-S2.5-WP-S25-3-P1-PROOF-tests docs/reviews/CI1A-S2.5-WP-S25-3-P1-PROOF-evidence docs/reviews/ANDIRIT-CI1A-S2.5-WP-S25-3-P1-PROOF-01.md
$preservation += "p1_package_unchanged_since_90c5fbe=$($LASTEXITCODE -eq 0) exit=$LASTEXITCODE"
& git -C $RepoRoot diff --quiet 349136d HEAD -- docs/planning/adr-ci1-01-ci1a-s2-5/s01-canonical-form-contract.md
$preservation += "canonical_forms_v1_unchanged_since_349136d=$($LASTEXITCODE -eq 0) exit=$LASTEXITCODE"
& git -C $RepoRoot diff --quiet 8709724 HEAD -- docs/planning/adr-ci1-01-ci1a-s2-5/s01-canonical-form-v1-clarification-01.md docs/decisions/adr-ci1-01-ci1a-s2-5-wp-s25-3-p1-contract-clarify-owner-01.md
$preservation += "clarification_addendum_unchanged_since_8709724=$($LASTEXITCODE -eq 0) exit=$LASTEXITCODE"
$p1Manifest = Get-Content (Join-Path $RepoRoot 'docs/reviews/CI1A-S2.5-WP-S25-3-P1-PROOF-evidence/manifest.json') -Raw | ConvertFrom-Json
$p1Mismatch = 0
foreach ($f in $p1Manifest.files) {
    $h = (Get-FileHash -Algorithm SHA256 (Join-Path $RepoRoot (Join-Path 'docs/reviews/CI1A-S2.5-WP-S25-3-P1-PROOF-evidence' $f.path))).Hash
    if ($h -ne $f.sha256) { $p1Mismatch++ }
}
$preservation += "p1_manifest_entries=$($p1Manifest.files.Count) mismatches=$p1Mismatch"
$preservation -join "`n" | Out-File (Join-Path $evidence 'preservation.log') -Encoding utf8
if ($p1Mismatch -ne 0) { $exitCode = 1 }

Write-Output '=== delta harness run ==='
"command: dotnet run --project DeltaProof.csproj -c Release --no-build -- run Q1-DELTA.json" | Out-File (Join-Path $evidence 'Q1-DELTA.log') -Encoding utf8
& dotnet run --project $project -c Release --no-build -- run (Join-Path $evidence 'Q1-DELTA.json') *>&1 | Out-File -Append -Encoding utf8 (Join-Path $evidence 'Q1-DELTA.log')
$deltaExit = $LASTEXITCODE
"exit_code=$deltaExit" | Out-File -Append -Encoding utf8 (Join-Path $evidence 'Q1-DELTA.log')
if ($deltaExit -ne 0) { $exitCode = 1 }
$delta = Get-Content (Join-Path $evidence 'Q1-DELTA.json') -Raw | ConvertFrom-Json
if (-not $delta.allPass) { Write-Output 'DELTA CHECKS FAILED'; $exitCode = 1 }

Write-Output '=== toolchain provenance ==='
"command: dotnet --info`n" | Out-File (Join-Path $evidence 'toolchain.log') -Encoding utf8
& dotnet --info *>&1 | Out-File -Append -Encoding utf8 (Join-Path $evidence 'toolchain.log')

Write-Output '=== regression gates ==='
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
$summary = [ordered]@{
    commission     = 'ANDIRIT-CI1A-S2.5-WP-S25-3-P1-DELTA-01'
    deltaChecks    = "$($delta.checkSummary) allPass=$($delta.allPass)"
    deltaPairs     = "revisionPairs=$($delta.revisionPairs.Count)"
    fullSolution   = $fullSummary
    focused        = $focusedSummary
    frozenOracle   = "bytes=$($frozen.Length) sha256=$frozenHash expectedBytes=146851 expectedSha256=97f6c5e5a4cc77e089481c7643732b7a07a31d483c2643bd7a95fdad7ced39c9 match=$($frozen.Length -eq 146851 -and $frozenHash -eq '97f6c5e5a4cc77e089481c7643732b7a07a31d483c2643bd7a95fdad7ced39c9')"
    preservation   = ($preservation -join ' ;; ')
}
$summary.GetEnumerator() | ForEach-Object { "$($_.Key): $($_.Value)" } | Out-File (Join-Path $evidence 'receipt-summary.log') -Encoding utf8
$summary.GetEnumerator() | ForEach-Object { "$($_.Key): $($_.Value)" }
if ($frozen.Length -ne 146851 -or $frozenHash -ne '97f6c5e5a4cc77e089481c7643732b7a07a31d483c2643bd7a95fdad7ced39c9') {
    Write-Output 'FROZEN ORACLE ARTIFACT MISMATCH'
    $exitCode = 1
}

exit $exitCode
