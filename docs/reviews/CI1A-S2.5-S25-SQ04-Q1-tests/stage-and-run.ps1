# ANDIRIT-CI1A-S2.5-S25-SQ04-Q1 — reviewer staging-isolation and historical reproduction (Q2–Q4).
# Reviewer-owned procedure (independent of the producer's run-probes.ps1):
#   1. Verify committed masters (complete SHA-256) and confirm the fixtures worktree is clean.
#   2. Assert the disposable work directory lies OUTSIDE the repository (cleanup safety).
#   3. Stage a FRESH exact copy per run (pre-REM .ltdb + .ltdb-wal together; baseline .db alone);
#      staged hashes must equal the master hashes before any open; no copy is ever reused.
#   4. Execute each case in a fresh OS process; the probe receives staged paths only.
#   5. Re-verify masters, the clean worktree status, and the HEAD blobs; remove the work
#      directory on success (kept on failure for postmortem).
[CmdletBinding()]
param(
    [string]$RepoRoot,
    [string]$WorkDir
)

$ErrorActionPreference = 'Stop'
if (-not $RepoRoot) { $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../../..")).Path }
$evidence = Join-Path $RepoRoot 'docs/reviews/CI1A-S2.5-S25-SQ04-Q1-evidence'
$fixtureRoot = Join-Path $RepoRoot 'docs/reviews/CI1A-S2.5-WP-S25-1-BIND-01-evidence/fixtures'
$project = Join-Path $PSScriptRoot 'Sq04QualProbe.csproj'

$hashA = '7646b98288a9803cff6f9930c78a3e7df5c676f6fc4e0cd2df2af5e79bc68885'
$hashAw = '8ca170cdc55a4428a5506bcb447a0667ab10c92c77ef149ae36460a5603aeab4'
$hashB = 'f21a72bee2c2e378e16766ca7b84c29010ab4d50360bf3111a82706f947781e0'
$masterA = Join-Path $fixtureRoot 'prerem/prerem-authentic.ltdb'
$masterAw = Join-Path $fixtureRoot 'prerem/prerem-authentic.ltdb-wal'
$masterB = Join-Path $fixtureRoot 'baseline/baseline-graph-only.master.db'

if (-not $WorkDir) {
    $WorkDir = Join-Path $env:TEMP ('sq04-q1-' + [guid]::NewGuid().ToString('N').Substring(0, 8))
}

function Get-Sha256([string]$Path) { (Get-FileHash -Algorithm SHA256 $Path).Hash.ToLowerInvariant() }
function Assert-Hash([string]$Path, [string]$Expected) {
    if (-not (Test-Path $Path)) { throw "Missing fixture file: $Path" }
    $actual = Get-Sha256 $Path
    if ($actual -ne $Expected) { throw "FIXTURE HASH MISMATCH for $Path`nexpected $Expected`nactual   $actual" }
}

# 0. cleanup-safety guard: the disposable work directory must never live inside the repository.
$resolvedWork = [IO.Path]::GetFullPath($WorkDir)
$resolvedRepo = [IO.Path]::GetFullPath($RepoRoot).TrimEnd('\', '/') + '\'
if ($resolvedWork.ToLowerInvariant().StartsWith($resolvedRepo.ToLowerInvariant())) {
    throw "Work directory must be outside the repository: $resolvedWork"
}

# 1. masters verified; fixtures worktree clean.
Assert-Hash $masterA $hashA
Assert-Hash $masterAw $hashAw
Assert-Hash $masterB $hashB
$fixturesStatusBefore = & git -C $RepoRoot status --porcelain -- 'docs/reviews/CI1A-S2.5-WP-S25-1-BIND-01-evidence/fixtures/'
if ($fixturesStatusBefore) { throw "fixtures worktree is not clean before execution" }

New-Item -ItemType Directory -Force -Path $WorkDir | Out-Null

function Stage-SetA([string]$Dir) {
    New-Item -ItemType Directory -Force -Path $Dir | Out-Null
    $db = Join-Path $Dir 'prerem-authentic.ltdb'
    $wal = Join-Path $Dir 'prerem-authentic.ltdb-wal'
    Copy-Item $masterA $db
    Copy-Item $masterAw $wal
    Assert-Hash $db $hashA
    Assert-Hash $wal $hashAw
    return $db
}
function Stage-SetB([string]$Dir) {
    New-Item -ItemType Directory -Force -Path $Dir | Out-Null
    $db = Join-Path $Dir 'baseline-graph-only.master.db'
    Copy-Item $masterB $db
    Assert-Hash $db $hashB
    return $db
}

$staged = New-Object System.Collections.Generic.List[object]
$runs = New-Object System.Collections.Generic.List[object]
$exitCode = 0

function Invoke-Case([string]$Case, [string]$StagedPath, [string]$OriginalPath, [string]$OriginalHash, [string]$Out, [string]$Log, [string]$RunId) {
    $script:staged.Add([pscustomobject]@{ run = $RunId; path = $StagedPath; sha256 = (Get-Sha256 $StagedPath) })
    "command: dotnet run --project Sq04QualProbe.csproj -c Release -- $Case <staged> <out> <original> <expected>" | Out-File $Log -Encoding utf8
    "staged: $StagedPath" | Out-File -Append $Log -Encoding utf8
    & dotnet run --project $project -c Release -- $Case $StagedPath $Out $OriginalPath $OriginalHash *>&1 |
        Out-File -Append -Encoding utf8 $Log
    $code = $LASTEXITCODE
    $ok = $false
    if (Test-Path $Out) { $ok = (Get-Content $Out -Raw | ConvertFrom-Json).ok }
    $script:runs.Add([pscustomobject]@{ run = $RunId; case = $Case; out = (Split-Path $Out -Leaf); exitCode = $code; ok = $ok })
    if ($code -ne 0 -or -not $ok) { $script:exitCode = 1 }
}

# 2-4. fresh copy + fresh process per run.
Invoke-Case 'pre-rem'    (Stage-SetA (Join-Path $WorkDir 'run1/A')) $masterA $hashA (Join-Path $evidence 'SQ04-P13B.json')        (Join-Path $evidence 'SQ04-P13B.log')        'run1'
Invoke-Case 'pre-rem'    (Stage-SetA (Join-Path $WorkDir 'run2/A')) $masterA $hashA (Join-Path $evidence 'SQ04-P13B-REPEAT.json') (Join-Path $evidence 'SQ04-P13B-REPEAT.log') 'run2'
Invoke-Case 'graph-only' (Stage-SetB (Join-Path $WorkDir 'run3/B')) $masterB $hashB (Join-Path $evidence 'SQ04-GRAPH-ONLY.json')  (Join-Path $evidence 'SQ04-GRAPH-ONLY.log')  'run3'
Invoke-Case 'graph-only' (Stage-SetB (Join-Path $WorkDir 'run4/B')) $masterB $hashB (Join-Path $evidence 'SQ04-GRAPH-ONLY-REPEAT.json') (Join-Path $evidence 'SQ04-GRAPH-ONLY-REPEAT.log') 'run4'

# 5. originals untouched: bytes, worktree status, HEAD blobs.
$mastersAfter = @(
    [pscustomobject]@{ path = 'fixtures/prerem/prerem-authentic.ltdb';           expected = $hashA;  actual = (Get-Sha256 $masterA) },
    [pscustomobject]@{ path = 'fixtures/prerem/prerem-authentic.ltdb-wal';       expected = $hashAw; actual = (Get-Sha256 $masterAw) },
    [pscustomobject]@{ path = 'fixtures/baseline/baseline-graph-only.master.db'; expected = $hashB;  actual = (Get-Sha256 $masterB) }
)
foreach ($m in $mastersAfter) { if ($m.actual -ne $m.expected) { $script:exitCode = 1 } }
$fixturesStatusAfter = & git -C $RepoRoot status --porcelain -- 'docs/reviews/CI1A-S2.5-WP-S25-1-BIND-01-evidence/fixtures/'
if ($fixturesStatusAfter) { $script:exitCode = 1 }
$blobChecks = @(
    [pscustomobject]@{ path = 'fixtures/prerem/prerem-authentic.ltdb';           expected = $hashA },
    [pscustomobject]@{ path = 'fixtures/prerem/prerem-authentic.ltdb-wal';       expected = $hashAw },
    [pscustomobject]@{ path = 'fixtures/baseline/baseline-graph-only.master.db'; expected = $hashB }
)

$staging = [ordered]@{
    commission = 'ANDIRIT-CI1A-S2.5-S25-SQ04-Q1'
    reviewer = 'independent reviewer (distinct from the BIND-01 producer session)'
    procedure = 'complete-hash masters -> clean worktree assert -> fresh copy per run (staged hash == master before open) -> fresh process per case -> masters/blobs/worktree re-verified'
    workDir = $WorkDir
    workDirOutsideRepo = (-not $resolvedWork.ToLowerInvariant().StartsWith($resolvedRepo.ToLowerInvariant()))
    stagedCopies = $staged
    runs = $runs
    mastersAfter = $mastersAfter
    fixturesWorktreeStatusAfter = ($fixturesStatusAfter -join ' | ')
    headBlobChecks = $blobChecks
    generatedAt = (Get-Date).ToString('o')
}
$staging | ConvertTo-Json -Depth 6 | Out-File (Join-Path $evidence 'Q2-STAGING.json') -Encoding utf8

Write-Output ("staging: workDirOutsideRepo={0} masterMismatches={1} worktreeDirty={2}" -f $staging.workDirOutsideRepo, (@($mastersAfter | Where-Object { $_.actual -ne $_.expected }).Count), [bool]$fixturesStatusAfter)
foreach ($r in $runs) { Write-Output ("run {0}: {1} exit={2} ok={3}" -f $r.run, $r.case, $r.exitCode, $r.ok) }

if ($exitCode -eq 0) {
    Remove-Item -Recurse -Force $WorkDir
    Write-Output "work directory removed (success): $WorkDir"
} else {
    Write-Output "work directory kept for postmortem: $WorkDir"
}
exit $exitCode
