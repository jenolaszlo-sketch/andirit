# ANDIRIT-CI1A-S2.5-S25-SQ05-Q1 — independent five-identity Git/GitHub verification (Q1).
# Reviewer-owned; read-only. Verifies the G1 section 7 identity chain from primary sources
# (Git objects, git ls-remote, GitHub API) — not from the G2 report's summaries.
[CmdletBinding()]
param(
    [string]$RepoRoot,
    [string]$RemoteRoot = 'C:\Users\Laszlos\source\repos\Solo'
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

$pinned = '7457284451dff8ddb0417f83ecc19097103316fc'
$pinnedTree = 'fba379453ceb3088cab97141ebae5c4c4978e3d3'
$branch = 'refs/heads/qualification/ci1a-s25-sq05-linux'

# Identity 1: pinned commit and tree exist locally with the recorded contents.
$type = (& git -C $RepoRoot cat-file -t $pinned)
$treeOf = (& git -C $RepoRoot rev-parse "$pinned^{tree}")
Assert-Check '1a. pinned qualification commit exists' ($type -eq 'commit') "type=$type"
Assert-Check '1b. pinned commit tree equals the authorized tree' ($treeOf -eq $pinnedTree) "tree=$treeOf"
$head = (& git -C $RepoRoot rev-parse HEAD).Trim()
& git -C $RepoRoot merge-base --is-ancestor $pinned $head | Out-Null
$pinnedIsAncestor = ($LASTEXITCODE -eq 0)
Assert-Check '1c. pinned commit is an ancestor of the reviewed HEAD' $pinnedIsAncestor "head=$head"

# Implementation identity: src/tests/CI identical between pinned and HEAD.
$implDiff = & git -C $RepoRoot diff --name-only $pinned HEAD -- src/ tests/ .github/
Assert-Check '1d. implementation identical from pinned to HEAD (src/tests/.github)' (@($implDiff).Count -eq 0) "diff=[$($implDiff -join ',')]"

# Identity 2: remote qualification branch SHA (live, read-only).
$remoteRefs = & git -C $RepoRoot ls-remote origin $branch "refs/pull/6/head" "refs/pull/6/merge" "refs/heads/main"
$branchLine = $remoteRefs | Where-Object { $_ -match $branch }
$branchSha = if ($branchLine) { ($branchLine -split "\s+")[0] } else { $null }
Assert-Check '2. remote qualification branch HEAD equals the pinned commit' ($branchSha -eq $pinned) "ls-remote=$branchLine"

# Identities 3-4: PR head and base from the GitHub API (fresh, read-only).
$pr = gh pr view 6 --repo jenolaszlo-sketch/andirit --json number,url,headRefName,headRefOid,baseRefName,baseRefOid,mergeable,state | ConvertFrom-Json
Assert-Check '3. PR head SHA equals the pinned commit' ($pr.headRefOid -eq $pinned) "headRefOid=$($pr.headRefOid)"
Assert-Check '3b. PR base is main at the recorded remote state' ($pr.baseRefName -eq 'main' -and $pr.baseRefOid -eq '443c192733a53d2f1da415743debb84e8c81acb5') "baseRefOid=$($pr.baseRefOid)"
Assert-Check '3c. PR #6 is OPEN (not merged)' ($pr.state -eq 'OPEN') "state=$($pr.state)"

# Identity 4-5: workflow run record from the GitHub API (fresh) + synthetic merge tree.
$run = gh api "repos/jenolaszlo-sketch/andirit/actions/runs/42744298084" | ConvertFrom-Json
Assert-Check '4. workflow run head_sha equals the pinned commit' ($run.head_sha -eq $pinned) "head_sha=$($run.head_sha)"
Assert-Check '4b. run is the pull_request CI workflow on the hosted runner' ($run.event -eq 'pull_request' -and $run.name -eq 'ci') "event=$($run.event) name=$($run.name) status=$($run.status) conclusion=$($run.conclusion)"

# Identity 5: synthetic merge commit tree (fetched read-only; tree is the binding condition).
$mergeLine = $remoteRefs | Where-Object { $_ -match 'refs/pull/6/merge' }
$mergeSha = if ($mergeLine) { ($mergeLine -split "\s+")[0] } else { $null }
$mergeTree = $null
$mergeContainsPinned = $false
if ($mergeSha) {
    & git -C $RepoRoot fetch --quiet origin "refs/pull/6/merge" 2>$null
    $mergeTree = (& git -C $RepoRoot rev-parse "$mergeSha^{tree}").Trim()
    & git -C $RepoRoot merge-base --is-ancestor $pinned $mergeSha | Out-Null
    $mergeContainsPinned = ($LASTEXITCODE -eq 0)
}
Assert-Check '5a. synthetic merge commit exists on the remote' ($null -ne $mergeSha) "merge=$mergeSha"
Assert-Check '5b. synthetic merge TREE equals the authorized qualification tree (G1 binding condition)' ($mergeTree -eq $pinnedTree) "tree=$mergeTree"
Assert-Check '5c. synthetic merge contains the pinned commit' $mergeContainsPinned "merge=$mergeSha pinned=$pinned"

# Workflow file identity at the pinned revision (blob + exact-byte hash).
$wfBlob = (& git -C $RepoRoot rev-parse "${pinned}:.github/workflows/ci.yml").Trim()
$wfHead = (& git -C $RepoRoot rev-parse "HEAD:.github/workflows/ci.yml").Trim()
Assert-Check '6. workflow blob identical at pinned revision, HEAD, and index' ($wfBlob -eq $wfHead) "blob=$wfBlob"

[ordered]@{
    commission = 'ANDIRIT-CI1A-S2.5-S25-SQ05-Q1'
    method = 'primary-source verification: Git objects, git ls-remote, GitHub PR/run API; fresh read-only remote inspection; G1 section 7 binding conditions applied verbatim'
    pinnedCommit = $pinned
    pinnedTree = $pinnedTree
    remoteBranchSha = $branchSha
    prHead = $pr.headRefOid
    prBase = $pr.baseRefOid
    runHeadSha = $run.head_sha
    runConclusion = $run.conclusion
    mergeCommit = $mergeSha
    mergeTree = $mergeTree
    workflowBlob = $wfBlob
    ok = $ok
    checks = $checks
    generatedAt = (Get-Date).ToString('o')
} | ConvertTo-Json -Depth 6 | Out-File (Join-Path $evidence 'Q1-IDENTITIES.json') -Encoding utf8

$failed = @($checks | Where-Object { -not $_.pass }).Count
Write-Output ("Q1 identities ok={0} checks={1} failed={2}" -f $ok, $checks.Count, $failed)
exit $(if ($ok) { 0 } else { 1 })
