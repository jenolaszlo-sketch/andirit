# ANDIRIT-CI1A-S2.5-S25-SQ04-Q1 — independent fixture authentication (Q1).
# Reviewer-owned verification: complete hashes recomputed directly from Git blob bytes,
# working-tree bytes, and the external originals; byte lengths; writer commits verified as
# Git objects; binary attributes; single-introducing-commit (no regeneration) check.
# Complete hashes only — never abbreviated prefixes.
[CmdletBinding()]
param(
    [string]$RepoRoot,
    [string]$ExternalRoot = 'C:\Users\Laszlos\source\repos\Solo'
)

$ErrorActionPreference = 'Stop'
if (-not $RepoRoot) { $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../../..")).Path }
$evidence = Join-Path $RepoRoot 'docs/reviews/CI1A-S2.5-S25-SQ04-Q1-evidence'
$provenance = Join-Path $RepoRoot 'docs/reviews/CI1A-S2.5-WP-S25-1-BIND-01-evidence/fixtures/PROVENANCE.md'

$ok = $true
$checks = New-Object System.Collections.Generic.List[object]
$details = New-Object System.Collections.Generic.List[object]
function Assert-Check([string]$Name, [bool]$Condition, [object]$Detail) {
    if (-not $Condition) { $script:ok = $false }
    $script:checks.Add([pscustomobject]@{ check = $Name; pass = $Condition; detail = $Detail })
}

function Get-Sha256([string]$Path) { (Get-FileHash -Algorithm SHA256 $Path).Hash.ToLowerInvariant() }
function Get-BlobSha256([string]$RevPath) {
    $tmp = Join-Path ([IO.Path]::GetTempPath()) ("sq04blob-" + [guid]::NewGuid().ToString('N') + ".bin")
    cmd /c "git -C `"$RepoRoot`" cat-file blob $RevPath > `"$tmp`""
    try { return (Get-FileHash -Algorithm SHA256 $tmp).Hash.ToLowerInvariant() } finally { Remove-Item $tmp -Force }
}

# Reviewer-derived complete expectations (each asserted to be recorded in the committed PROVENANCE.md).
$fixtures = @(
    @{
        name = 'Set A: pre-REM occurrence store (.ltdb)'
        path = 'docs/reviews/CI1A-S2.5-WP-S25-1-BIND-01-evidence/fixtures/prerem/prerem-authentic.ltdb'
        sha256 = '7646b98288a9803cff6f9930c78a3e7df5c676f6fc4e0cd2df2af5e79bc68885'
        bytes = 86016
        external = Join-Path $ExternalRoot 'adr-ci1-01-ci1a-s2-boundary-rem-01-q1/evidence/prerem-authentic.ltdb'
        writerCommit = '4c6d26a1d103a62fffe2e2743467eedb5f21afc2'
        writerTree = '320e965f219d0b615da29665d7fa3e85f8bbfc15'
    },
    @{
        name = 'Set A: pre-REM WAL (.ltdb-wal)'
        path = 'docs/reviews/CI1A-S2.5-WP-S25-1-BIND-01-evidence/fixtures/prerem/prerem-authentic.ltdb-wal'
        sha256 = '8ca170cdc55a4428a5506bcb447a0667ab10c92c77ef149ae36460a5603aeab4'
        bytes = 4096
        external = Join-Path $ExternalRoot 'adr-ci1-01-ci1a-s2-boundary-rem-01-q1/evidence/prerem-authentic.ltdb-wal'
        writerCommit = '4c6d26a1d103a62fffe2e2743467eedb5f21afc2'
        writerTree = '320e965f219d0b615da29665d7fa3e85f8bbfc15'
    },
    @{
        name = 'Set B: R2 baseline graph-only store (.db)'
        path = 'docs/reviews/CI1A-S2.5-WP-S25-1-BIND-01-evidence/fixtures/baseline/baseline-graph-only.master.db'
        sha256 = 'f21a72bee2c2e378e16766ca7b84c29010ab4d50360bf3111a82706f947781e0'
        bytes = 163840
        external = Join-Path $ExternalRoot 'adr-ci1-01-exp-02-r2/evidence/historical-master/baseline-graph-only.master.db'
        writerCommit = '31c33a934aa13e373d9a3a6ffbc54d789eef092c'
        writerTree = 'a265f561243de37e1636490f99acb0b34245809e'
    }
)

foreach ($f in $fixtures) {
    $full = Join-Path $RepoRoot $f.path
    $blob = Get-BlobSha256 ("HEAD:" + ($f.path -replace '\\', '/'))
    $work = Get-Sha256 $full
    $workLen = (Get-Item $full).Length
    $ext = $null
    if (Test-Path $f.external) { $ext = Get-Sha256 $f.external }
    $commitType = (& git -C $RepoRoot cat-file -t $f.writerCommit 2>$null)
    $commitTree = (& git -C $RepoRoot log -1 --format=%T $f.writerCommit 2>$null)
    $attrs = (& git -C $RepoRoot check-attr -a -- $f.path) -join ' | '
    $provHit = (Select-String -Path $provenance -Pattern $f.sha256 -SimpleMatch | Measure-Object).Count

    Assert-Check "$($f.name): complete hash recorded in committed PROVENANCE.md" ($provHit -ge 1) "hits=$provHit"
    Assert-Check "$($f.name): Git blob bytes match expected" ($blob -eq $f.sha256) "blob=$blob"
    Assert-Check "$($f.name): working-tree bytes match expected (blob==work)" ($work -eq $f.sha256 -and $work -eq $blob) "work=$work"
    Assert-Check "$($f.name): exact byte length" ($workLen -eq $f.bytes) "length=$workLen"
    Assert-Check "$($f.name): external original matches" ($ext -eq $f.sha256) "external=$ext"
    Assert-Check "$($f.name): writer commit exists as Git object" ($commitType -eq 'commit') "type=$commitType"
    Assert-Check "$($f.name): writer commit tree matches recorded provenance" ($commitTree -eq $f.writerTree) "tree=$commitTree"
    Assert-Check "$($f.name): no EOL normalization (text unset)" ($attrs -match 'text: unset') $attrs

    $details.Add([pscustomobject]@{
        fixture = $f.name
        gitBlobSha256 = $blob
        workingSha256 = $work
        externalSha256 = $ext
        expectedSha256 = $f.sha256
        bytes = $workLen
        expectedBytes = $f.bytes
        writerCommit = $f.writerCommit
        writerCommitType = $commitType
        writerCommitTree = $commitTree
        gitAttributes = $attrs
        provenanceCompleteHashHits = $provHit
    })
}

# Single introducing commit for the fixture directory (never regenerated).
$fixtureCommits = & git -C $RepoRoot log --format=%H -- 'docs/reviews/CI1A-S2.5-WP-S25-1-BIND-01-evidence/fixtures/'
$head = (& git -C $RepoRoot rev-parse HEAD).Trim()
Assert-Check 'fixture directory introduced by exactly one commit' (@($fixtureCommits).Count -eq 1) "commits=$($fixtureCommits -join ',')"
Assert-Check 'fixture introducing commit is the reviewed HEAD' (@($fixtureCommits)[0] -eq $head) "head=$head"

[ordered]@{
    commission = 'ANDIRIT-CI1A-S2.5-S25-SQ04-Q1'
    method = 'complete SHA-256 from Git blob bytes + working tree + external originals; writer commits as Git objects; attribute and regeneration checks; complete hashes only'
    provenance = $provenance
    ok = $ok
    checks = $checks
    details = $details
    generatedAt = (Get-Date).ToString('o')
} | ConvertTo-Json -Depth 6 | Out-File (Join-Path $evidence 'Q1-AUTH.json') -Encoding utf8

$failed = @($checks | Where-Object { -not $_.pass }).Count
Write-Output ("Q1 authentication ok={0} checks={1} failed={2}" -f $ok, $checks.Count, $failed)
exit $(if ($ok) { 0 } else { 1 })
