# ANDIRIT-CI1A-S2.5-WP-S25-3-P2-QUAL-01 — reviewer qualification orchestration.
# Preflight, PowerShell known-answer anchors (independent toolchain path), producer-artifact
# audit, reviewer harness (Part A + integrated Part B in fresh processes), regression gates,
# and summaries written into the P2 evidence directory.
[CmdletBinding()]
param(
    [string]$RepoRoot
)

$ErrorActionPreference = 'Stop'
if (-not $RepoRoot) { $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../../..")).Path }
$evidence = Join-Path $RepoRoot 'docs/reviews/CI1A-S2.5-WP-S25-3-P2-QUAL-evidence'
$project = Join-Path $PSScriptRoot 'P2Qual.csproj'
$db = Join-Path $env:TEMP ('p2-qual-' + [guid]::NewGuid().ToString('N').Substring(0, 8) + '.ltdb')
$exitCode = 0

New-Item -ItemType Directory -Force -Path $evidence | Out-Null

Write-Output '=== P2 reviewer build ==='
"command: dotnet build P2Qual.csproj -c Release`n" | Out-File (Join-Path $evidence 'build.log') -Encoding utf8
& dotnet build $project -c Release *>&1 | Out-File -Append -Encoding utf8 (Join-Path $evidence 'build.log')
"exit_code=$LASTEXITCODE" | Out-File -Append -Encoding utf8 (Join-Path $evidence 'build.log')
if ($LASTEXITCODE -ne 0) { throw 'reviewer harness build failed' }

Write-Output '=== preflight ==='
$preflight = @(
    "head=$(git -C $RepoRoot rev-parse HEAD)"
    "tree=$(git -C $RepoRoot rev-parse 'HEAD^{tree}')"
    "branch=$(git -C $RepoRoot rev-parse --abbrev-ref HEAD)"
    "dirty_or_untracked_lines=$((git -C $RepoRoot status --porcelain --untracked-files=all | Measure-Object -Line).Lines)"
    "worktrees=$((git -C $RepoRoot worktree list | Measure-Object -Line).Lines)"
    "stashes=$((git -C $RepoRoot stash list | Measure-Object -Line).Lines)"
    "origin_main_local_ref=$(git -C $RepoRoot rev-parse origin/main)"
    "commits_ahead_of_origin_main_local_ref=$(git -C $RepoRoot rev-list --count origin/main..HEAD)"
    "note=no fetch/push/remote contact performed by this run"
) -join "`n"
$preflight | Out-File (Join-Path $evidence 'preflight.log') -Encoding utf8

Write-Output '=== normative inputs ==='
$normative = @()
foreach ($f in @(
    'docs/planning/adr-ci1-01-ci1a-s2-5/s01-canonical-form-contract.md',
    'docs/planning/adr-ci1-01-ci1a-s2-5/s01-canonical-form-v1-clarification-01.md',
    'docs/decisions/adr-ci1-01-ci1a-s2-5-wp-s25-3-p1-contract-clarify-owner-01.md',
    'docs/reviews/ANDIRIT-CI1A-S2.5-WP-S25-3-P1-PROOF-01.md',
    'docs/reviews/ANDIRIT-CI1A-S2.5-WP-S25-3-P1-DELTA-01.md')) {
    $full = Join-Path $RepoRoot $f
    $normative += "$f bytes=$((Get-Item $full).Length) sha256=$((Get-FileHash -Algorithm SHA256 $full).Hash.ToLowerInvariant())"
}
$normative -join "`n" | Out-File (Join-Path $evidence 'normative-inputs.log') -Encoding utf8

Write-Output '=== PowerShell known-answer anchors (independent toolchain path) ==='
$katLines = @()
function Get-ReviewerKat([string]$domain, [string]$field) {
    $ms = New-Object System.IO.MemoryStream
    foreach ($s in @($domain, $field)) {
        $b = [Text.Encoding]::UTF8.GetBytes($s)
        $l = [BitConverter]::GetBytes([int]$b.Length)
        [Array]::Reverse($l)
        $ms.Write($l, 0, 4)
        $ms.Write($b, 0, $b.Length)
    }
    $ms.Seek(0, 'Begin') | Out-Null
    (Get-FileHash -InputStream $ms -Algorithm SHA256).Hash.ToLowerInvariant()
}
$katLines += "v1 identifier/'ReviewerPing' = $(Get-ReviewerKat 'asmctx3-identifier' 'ReviewerPing')"
$katLines += "v2 declarations/'M:Review.Core.Spec.Confirm(System.Int32@)' = $(Get-ReviewerKat 'asmctx3-declarations' 'M:Review.Core.Spec.Confirm(System.Int32@)')"
$katLines += "v3 target/'method|1|T' = $(Get-ReviewerKat 'asmctx3-target' 'method|1|T')"
$katLines += "v4 identifier/'' = $(Get-ReviewerKat 'asmctx3-identifier' '')"
$unicodeField = -join ([int[]](0x0418, 0x0434, 0x0435, 0x043D, 0x0442, 0x0438, 0x0444, 0x0438, 0x043A, 0x0430, 0x0442, 0x043E, 0x0440, 0x03A9) | ForEach-Object { [char]$_ })
$katLines += "v5 identifier/'$unicodeField' = $(Get-ReviewerKat 'asmctx3-identifier' $unicodeField)"
$katLines += "note=these PowerShell values were computed before the reviewer C# harness was written and are embedded as literals in Reviewer.cs"
$katLines -join "`n" | Out-File (Join-Path $evidence 'kat-powershell.log') -Encoding utf8

Write-Output '=== producer-artifact audit ==='
$audit = @()
foreach ($pkg in @(
    @{ name = 'p1'; dir = 'docs/reviews/CI1A-S2.5-WP-S25-3-P1-PROOF-evidence'; expected = 23 },
    @{ name = 'delta'; dir = 'docs/reviews/CI1A-S2.5-WP-S25-3-P1-DELTA-evidence'; expected = 17 })) {
    $m = Get-Content (Join-Path $RepoRoot (Join-Path $pkg.dir 'manifest.json')) -Raw | ConvertFrom-Json
    $mismatch = 0
    foreach ($f in $m.files) {
        $h = (Get-FileHash -Algorithm SHA256 (Join-Path $RepoRoot (Join-Path $pkg.dir $f.path))).Hash
        if ($h -ne $f.sha256) { $mismatch++ }
    }
    $audit += "$($pkg.name)_manifest_entries=$($m.files.Count) expected=$($pkg.expected) mismatches=$mismatch reviewedHead=$($m.reviewedHead)"
}
& git -C $RepoRoot diff --quiet 90c5fbe HEAD -- docs/reviews/CI1A-S2.5-WP-S25-3-P1-PROOF-tests docs/reviews/CI1A-S2.5-WP-S25-3-P1-PROOF-evidence docs/reviews/ANDIRIT-CI1A-S2.5-WP-S25-3-P1-PROOF-01.md
$audit += "p1_package_unchanged_since_90c5fbe=$($LASTEXITCODE -eq 0)"
& git -C $RepoRoot diff --quiet 976fe52 HEAD -- docs/reviews/CI1A-S2.5-WP-S25-3-P1-DELTA-tests docs/reviews/CI1A-S2.5-WP-S25-3-P1-DELTA-evidence docs/reviews/ANDIRIT-CI1A-S2.5-WP-S25-3-P1-DELTA-01.md
$audit += "delta_package_unchanged_since_976fe52=$($LASTEXITCODE -eq 0)"
& git -C $RepoRoot diff --quiet 349136d HEAD -- docs/planning/adr-ci1-01-ci1a-s2-5/s01-canonical-form-contract.md
$audit += "canonical_forms_v1_unchanged_since_349136d=$($LASTEXITCODE -eq 0)"
& git -C $RepoRoot diff --quiet 8709724 HEAD -- docs/planning/adr-ci1-01-ci1a-s2-5/s01-canonical-form-v1-clarification-01.md docs/decisions/adr-ci1-01-ci1a-s2-5-wp-s25-3-p1-contract-clarify-owner-01.md
$audit += "clarification_unchanged_since_8709724=$($LASTEXITCODE -eq 0)"
& git -C $RepoRoot merge-base --is-ancestor 8709724 HEAD
$audit += "clarify_commit_is_ancestor_of_head=$($LASTEXITCODE -eq 0)"
& git -C $RepoRoot merge-base --is-ancestor 349136d 8709724
$audit += "p0_commit_is_ancestor_of_clarify=$($LASTEXITCODE -eq 0)"
& git -C $RepoRoot diff --quiet 349136d HEAD -- src tests .github
$audit += "production_tests_ci_unchanged_since_p0=$($LASTEXITCODE -eq 0)"
$audit -join "`n" | Out-File (Join-Path $evidence 'producer-audit.log') -Encoding utf8
if ($audit -match '=False') { Write-Output 'PRODUCER AUDIT FOUND A DISCREPANCY'; $exitCode = 1 }

Write-Output '=== reviewer artifact audit (recompute recorded primitives) ==='
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'audit-artifacts.ps1') -RepoRoot $RepoRoot *>&1 | Out-File (Join-Path $evidence 'Q5-ARTIFACT-AUDIT.log') -Encoding utf8
"exit_code=$LASTEXITCODE" | Out-File -Append -Encoding utf8 (Join-Path $evidence 'Q5-ARTIFACT-AUDIT.log')
if ($LASTEXITCODE -ne 0) { $exitCode = 1 }
$artifactAudit = Get-Content (Join-Path $evidence 'Q5-ARTIFACT-AUDIT.json') -Raw | ConvertFrom-Json
if (-not $artifactAudit.allPass) { Write-Output 'REVIEWER ARTIFACT AUDIT CHECKS FAILED'; $exitCode = 1 }

Write-Output '=== reviewer Part A (independent derivation) ==='
"command: dotnet run --project P2Qual.csproj -c Release --no-build -- run Q1-P2A.json" | Out-File (Join-Path $evidence 'Q1-P2A.log') -Encoding utf8
& dotnet run --project $project -c Release --no-build -- run (Join-Path $evidence 'Q1-P2A.json') *>&1 | Out-File -Append -Encoding utf8 (Join-Path $evidence 'Q1-P2A.log')
"exit_code=$LASTEXITCODE" | Out-File -Append -Encoding utf8 (Join-Path $evidence 'Q1-P2A.log')
if ($LASTEXITCODE -ne 0) { $exitCode = 1 }
$partA = Get-Content (Join-Path $evidence 'Q1-P2A.json') -Raw | ConvertFrom-Json
if (-not $partA.allPass) { Write-Output 'REVIEWER PART A CHECKS FAILED'; $exitCode = 1 }

Write-Output '=== reviewer Part B: integrate (fresh process, S1-S5) ==='
"command: dotnet run --project P2Qual.csproj -c Release --no-build -- integrate <db> Q2-INT.json" | Out-File (Join-Path $evidence 'Q2-INT.log') -Encoding utf8
& dotnet run --project $project -c Release --no-build -- integrate $db (Join-Path $evidence 'Q2-INT.json') *>&1 | Out-File -Append -Encoding utf8 (Join-Path $evidence 'Q2-INT.log')
"exit_code=$LASTEXITCODE" | Out-File -Append -Encoding utf8 (Join-Path $evidence 'Q2-INT.log')
if ($LASTEXITCODE -ne 0) { $exitCode = 1 }

Write-Output '=== reviewer Part B: reopen (fresh process, S6-S7) ==='
"command: dotnet run --project P2Qual.csproj -c Release --no-build -- reopen <db> Q3-REOPEN.json" | Out-File (Join-Path $evidence 'Q3-REOPEN.log') -Encoding utf8
& dotnet run --project $project -c Release --no-build -- reopen $db (Join-Path $evidence 'Q3-REOPEN.json') *>&1 | Out-File -Append -Encoding utf8 (Join-Path $evidence 'Q3-REOPEN.log')
"exit_code=$LASTEXITCODE" | Out-File -Append -Encoding utf8 (Join-Path $evidence 'Q3-REOPEN.log')
if ($LASTEXITCODE -ne 0) { $exitCode = 1 }

Write-Output '=== reviewer Part B: recover (fresh process, S8) ==='
"command: dotnet run --project P2Qual.csproj -c Release --no-build -- recover <db> Q4-RECOVERY.json" | Out-File (Join-Path $evidence 'Q4-RECOVERY.log') -Encoding utf8
& dotnet run --project $project -c Release --no-build -- recover $db (Join-Path $evidence 'Q4-RECOVERY.json') *>&1 | Out-File -Append -Encoding utf8 (Join-Path $evidence 'Q4-RECOVERY.log')
"exit_code=$LASTEXITCODE" | Out-File -Append -Encoding utf8 (Join-Path $evidence 'Q4-RECOVERY.log')
if ($LASTEXITCODE -ne 0) { $exitCode = 1 }
$recover = Get-Content (Join-Path $evidence 'Q4-RECOVERY.json') -Raw | ConvertFrom-Json
$tamperOutcome = $recover.tamperedPayloadRefusal.outcome
if ($tamperOutcome -ne 'refused') { Write-Output 'TAMPER REFUSAL NOT OBSERVED'; $exitCode = 1 }

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
$frozen = Get-Item (Join-Path $evidence 'oracle-r2.json')
$frozenHash = (Get-FileHash -Algorithm SHA256 $frozen.FullName).Hash.ToLowerInvariant()
$summary = [ordered]@{
    commission   = 'ANDIRIT-CI1A-S2.5-WP-S25-3-P2-QUAL-01'
    reviewerPartA = "checks=$($partA.checkSummary) allPass=$($partA.allPass)"
    integrated   = "db=$db tamperRefusal=$tamperOutcome"
    fullSolution = $fullSummary
    frozenOracle = "bytes=$($frozen.Length) sha256=$frozenHash expectedBytes=146851 expectedSha256=97f6c5e5a4cc77e089481c7643732b7a07a31d483c2643bd7a95fdad7ced39c9 match=$($frozen.Length -eq 146851 -and $frozenHash -eq '97f6c5e5a4cc77e089481c7643732b7a07a31d483c2643bd7a95fdad7ced39c9')"
}
$summary.GetEnumerator() | ForEach-Object { "$($_.Key): $($_.Value)" } | Out-File (Join-Path $evidence 'receipt-summary.log') -Encoding utf8
$summary.GetEnumerator() | ForEach-Object { "$($_.Key): $($_.Value)" }
if ($frozen.Length -ne 146851 -or $frozenHash -ne '97f6c5e5a4cc77e089481c7643732b7a07a31d483c2643bd7a95fdad7ced39c9') {
    Write-Output 'FROZEN ORACLE ARTIFACT MISMATCH'
    $exitCode = 1
}

exit $exitCode
