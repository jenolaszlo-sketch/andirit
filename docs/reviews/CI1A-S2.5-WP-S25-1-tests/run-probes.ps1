# ANDIRIT-CI1A-S2.5-WP-S25-1-BIND-01 â€” historical fixture binding probe runner.
#
# Procedure:
#   1. Verify the committed fixture masters against their COMPLETE recorded SHA-256 values.
#   2. Stage exact copies (pre-REM .ltdb + .ltdb-wal together; baseline .db alone) to a
#      disposable work directory; the committed masters are NEVER opened by a store.
#   3. Execute each case in a fresh OS process against a fresh staged copy.
#   4. Re-verify the committed masters; write integrity receipt.
#   5. Remove the work directory on success (kept on failure for postmortem).
#
# The staged copies may be mutated by the store engine (open-mutation disclosure) â€”
# that is expected and is why only disposable copies are opened.
[CmdletBinding()]
param(
    [string]$RepoRoot,
    [string]$WorkDir
)

$ErrorActionPreference = 'Stop'
if (-not $RepoRoot) { $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../../..")).Path }
$evidence = Join-Path $RepoRoot 'docs/reviews/CI1A-S2.5-WP-S25-1-BIND-01-evidence'
$fixtureRoot = Join-Path $evidence 'fixtures'
$project = Join-Path $PSScriptRoot 'BindProbe.csproj'

$hashA = '7646b98288a9803cff6f9930c78a3e7df5c676f6fc4e0cd2df2af5e79bc68885'
$hashAw = '8ca170cdc55a4428a5506bcb447a0667ab10c92c77ef149ae36460a5603aeab4'
$hashB = 'f21a72bee2c2e378e16766ca7b84c29010ab4d50360bf3111a82706f947781e0'

$masterA = Join-Path $fixtureRoot 'prerem/prerem-authentic.ltdb'
$masterAw = Join-Path $fixtureRoot 'prerem/prerem-authentic.ltdb-wal'
$masterB = Join-Path $fixtureRoot 'baseline/baseline-graph-only.master.db'

if (-not $WorkDir) {
    $WorkDir = Join-Path $env:TEMP ('andirit-s25-1-' + [guid]::NewGuid().ToString('N').Substring(0, 8))
}

function Get-Sha256([string]$Path) { (Get-FileHash -Algorithm SHA256 $Path).Hash.ToLowerInvariant() }

function Assert-Hash([string]$Path, [string]$Expected) {
    if (-not (Test-Path $Path)) { throw "Missing fixture file: $Path" }
    $actual = Get-Sha256 $Path
    if ($actual -ne $Expected) { throw "FIXTURE HASH MISMATCH for $Path`nexpected $Expected`nactual   $actual" }
}

# 1. verify masters before
Assert-Hash $masterA $hashA
Assert-Hash $masterAw $hashAw
Assert-Hash $masterB $hashB

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
    "command: dotnet run --project BindProbe.csproj -c Release -- $Case <staged> <out> <original> <expected>" | Out-File $Log -Encoding utf8
    "staged: $StagedPath" | Out-File -Append $Log -Encoding utf8
    & dotnet run --project $project -c Release -- $Case $StagedPath $Out $OriginalPath $OriginalHash *>&1 |
        Out-File -Append -Encoding utf8 $Log
    $code = $LASTEXITCODE
    $ok = $false
    if (Test-Path $Out) { $ok = (Get-Content $Out -Raw | ConvertFrom-Json).ok }
    $script:runs.Add([pscustomobject]@{ run = $RunId; case = $Case; out = (Split-Path $Out -Leaf); exitCode = $code; ok = $ok })
    if ($code -ne 0 -or -not $ok) { $script:exitCode = 1 }
}

# 2. stage + run: fresh copy per run; each run is its own OS process
Invoke-Case 'pre-rem'    (Stage-SetA (Join-Path $WorkDir 'run1/A')) $masterA $hashA (Join-Path $evidence 'Q1-P13B.json')        (Join-Path $evidence 'Q1-P13B.log')        'run1'
Invoke-Case 'pre-rem'    (Stage-SetA (Join-Path $WorkDir 'run2/A')) $masterA $hashA (Join-Path $evidence 'Q1-P13B-REPEAT.json') (Join-Path $evidence 'Q1-P13B-REPEAT.log') 'run2'
Invoke-Case 'graph-only' (Stage-SetB (Join-Path $WorkDir 'run3/B')) $masterB $hashB (Join-Path $evidence 'Q1-GRAPH-ONLY.json')  (Join-Path $evidence 'Q1-GRAPH-ONLY.log')  'run3'
Invoke-Case 'graph-only' (Stage-SetB (Join-Path $WorkDir 'run4/B')) $masterB $hashB (Join-Path $evidence 'Q1-GRAPH-ONLY-REPEAT.json') (Join-Path $evidence 'Q1-GRAPH-ONLY-REPEAT.log') 'run4'

# 3. re-verify masters after (committed originals must be byte-identical)
$mastersAfter = @(
    [pscustomobject]@{ path = 'fixtures/prerem/prerem-authentic.ltdb';           expected = $hashA;  actual = (Get-Sha256 $masterA) },
    [pscustomobject]@{ path = 'fixtures/prerem/prerem-authentic.ltdb-wal';       expected = $hashAw; actual = (Get-Sha256 $masterAw) },
    [pscustomobject]@{ path = 'fixtures/baseline/baseline-graph-only.master.db'; expected = $hashB;  actual = (Get-Sha256 $masterB) }
)
foreach ($m in $mastersAfter) { if ($m.actual -ne $m.expected) { $exitCode = 1 } }

$integrity = [ordered]@{
    commission   = 'ANDIRIT-CI1A-S2.5-WP-S25-1-BIND-01'
    procedure    = 'complete SHA-256 verification -> exact staging copies -> fresh process per case -> masters re-verified; committed masters never opened by a store'
    workDir      = $WorkDir
    stagedCopies = $staged
    runs         = $runs
    mastersAfter = $mastersAfter
    generatedAt  = (Get-Date).ToString('o')
}
$integrity | ConvertTo-Json -Depth 6 | Out-File (Join-Path $evidence 'Q1-INTEGRITY.json') -Encoding utf8

Write-Output ("integrity: " + (@($mastersAfter | Where-Object { $_.actual -ne $_.expected }).Count) + ' master mismatches (expect 0)')
foreach ($r in $runs) { Write-Output ("run {0}: {1} exit={2} ok={3}" -f $r.run, $r.case, $r.exitCode, $r.ok) }

if ($exitCode -eq 0) {
    Remove-Item -Recurse -Force $WorkDir
    Write-Output "work directory removed (success): $WorkDir"
} else {
    Write-Output "work directory kept for postmortem: $WorkDir"
}
exit $exitCode
