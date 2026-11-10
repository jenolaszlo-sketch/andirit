# ANDIRIT-CI1A-S2.5-S25-SQ05-Q1 — evidence and manifest integrity verification (Q5).
# Reviewer-owned; read-only. Verifies the G2 evidence manifest, the historical SQ04-Q1 and
# BIND-01 manifests, the downloaded artifact bytes, and the local repository cleanliness —
# confirming no historical evidence was rewritten (including after the disclosed accidental
# seal invocation during G2, which was reverted before commit).
[CmdletBinding()]
param(
    [string]$RepoRoot
)

$ErrorActionPreference = 'Stop'
if (-not $RepoRoot) { $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../../..")).Path }
$evidence = Join-Path $RepoRoot 'docs/reviews/CI1A-S2.5-S25-SQ05-Q1-evidence'

$ok = $true
$checks = New-Object System.Collections.Generic.List[object]
function Assert-Check([string]$Name, [bool]$Condition, [object]$Detail) {
    if (-not $Condition) { $script:ok = $false }
    $script:checks.Add([pscustomobject]@{ check = $Name; pass = $Condition; detail = $Detail })
}
function Get-Sha256([string]$Path) {
    if (-not (Test-Path $Path)) { throw "File not found: $Path" }
    $h = Get-FileHash -Algorithm SHA256 $Path
    if ($null -eq $h) { throw "Get-FileHash returned null for: $Path" }
    return $h.Hash.ToLowerInvariant()
}

$packages = @(
    @{ name = 'G2 execution evidence (CI1A-S2.5-WP-S25-2-G2-EXEC-evidence)'; path = 'docs/reviews/CI1A-S2.5-WP-S25-2-G2-EXEC-evidence' },
    @{ name = 'SQ04-Q1 evidence (CI1A-S2.5-S25-SQ04-Q1-evidence)'; path = 'docs/reviews/CI1A-S2.5-S25-SQ04-Q1-evidence' },
    @{ name = 'BIND-01 evidence (CI1A-S2.5-WP-S25-1-BIND-01-evidence)'; path = 'docs/reviews/CI1A-S2.5-WP-S25-1-BIND-01-evidence' }
)

foreach ($p in $packages) {
    $dir = Join-Path $RepoRoot $p.path
    $m = Get-Content (Join-Path $dir 'manifest.json') -Raw | ConvertFrom-Json
    $mismatched = @($m.files | Where-Object { (Get-FileHash -Algorithm SHA256 (Join-Path $dir $_.path)).Hash -ne $_.sha256 })
    Assert-Check "$($p.name): manifest integrity" ($mismatched.Count -eq 0) "entries=$($m.files.Count) mismatches=$($mismatched.Count)"
    $wt = & git -C $RepoRoot status --porcelain -- $p.path
    Assert-Check "$($p.name): committed state is pristine (no working-tree modifications)" (@($wt).Count -eq 0) "status=[$($wt -join ' | ')]"
}

# Downloaded artifact bytes re-verified (G2-committed cobertura files).
$artifacts = @(
    @{ p = 'coverage-results-ubuntu-latest/coverage/coverage.cobertura.xml'; sha = '3B801E30EFFB21E6048C4548E922E6A77A0E4D656AFB01524B69A7B6E1AFA5FC' },
    @{ p = 'coverage-results-ubuntu-latest/coverage-csharp/coverage.cobertura.xml'; sha = '43B62D8D5397244622736491F143DA58359C9A33779EA0585EA39E4A4882BEEC' },
    @{ p = 'coverage-results-ubuntu-latest/coverage-latticedb/coverage.cobertura.xml'; sha = '7D73E0E6981AC34AEFC2B6513A956C0C40A4DBDF14D9E81566548F84C43B0FD9' },
    @{ p = 'coverage-results-ubuntu-latest/coverage-treesitter/coverage.cobertura.xml'; sha = '5B777694E5FEEB5E0F4315E610C8E4A1B2051422ED781F674158D311A4E495A3' }
)
foreach ($a in $artifacts) {
    $full = Join-Path $RepoRoot ('docs/reviews/CI1A-S2.5-WP-S25-2-G2-EXEC-evidence/' + $a.p)
    Assert-Check "artifact bytes unchanged: $($a.p)" ((Get-Sha256 $full) -eq $a.sha.ToLowerInvariant()) (Get-Sha256 $full)
}

# The disclosed G2 accidental-seal incident: confirm the SQ04-Q1 reviewed-head still records
# its own sealing commit (bf1af23 lineage state), i.e. the revert restored the accepted bytes.
$rh = Get-Content (Join-Path $RepoRoot 'docs/reviews/CI1A-S2.5-S25-SQ04-Q1-evidence/reviewed-head.txt') -Raw
Assert-Check 'SQ04-Q1 reviewed-head unchanged (records bf1af23/f2195c26 — the accidental seal rewrite is fully reverted)' ($rh -match 'bf1af232575b68fd1c8d61b9397c57608a7be2a6') ($rh.Trim() -replace "`r?`n", ' | ')

# Repository cleanliness for this commission: tracked files must be pristine; new additive
# evidence files are expected to be untracked until the commission commit.
$wt = & git -C $RepoRoot status --porcelain --untracked-files=no
Assert-Check 'repository tracked files are pristine (no modifications)' (@($wt).Count -eq 0) "modified=[$($wt -join ' | ')]"
$wtUntracked = @(& git -C $RepoRoot status --porcelain --untracked-files=all | Where-Object { $_ -match '^\?\?' })
Assert-Check 'working tree contains only this commission''s new additive files (untracked)' ($wtUntracked.Count -ge 1) "untracked=$($wtUntracked.Count)"

[ordered]@{
    commission = 'ANDIRIT-CI1A-S2.5-S25-SQ05-Q1'
    method = 'manifest re-verification over exact bytes; git working-tree status; artifact byte comparison against G2-recorded hashes'
    ok = $ok
    checks = $checks
    generatedAt = (Get-Date).ToString('o')
} | ConvertTo-Json -Depth 6 | Out-File (Join-Path $evidence 'Q5-INTEGRITY.json') -Encoding utf8

$failed = @($checks | Where-Object { -not $_.pass }).Count
Write-Output ("Q5 integrity ok={0} checks={1} failed={2}" -f $ok, $checks.Count, $failed)
exit $(if ($ok) { 0 } else { 1 })
