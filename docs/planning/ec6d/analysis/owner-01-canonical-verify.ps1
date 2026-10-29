# Read-only canonical verification; writes its report in this planning directory.
$ErrorActionPreference = 'Stop'
$repoPath = 'C:/Users/Laszlos/source/repos/Penghou.Hetu'
$snapshot = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'owner-01-canonical-preflight.json') -Raw | ConvertFrom-Json
function Invoke-ReadGit([string[]] $GitArgs) {
    $result = @(git -C $repoPath @GitArgs)
    if ($LASTEXITCODE -ne 0) { throw "Git failed: $GitArgs" }
    return $result
}
$currentRefs = @(Invoke-ReadGit -GitArgs @('show-ref'))
if (Compare-Object $snapshot.refs $currentRefs) { throw 'Canonical refs changed' }
$currentWorktrees = @(Invoke-ReadGit -GitArgs @('worktree', 'list', '--porcelain'))
if (Compare-Object $snapshot.worktrees $currentWorktrees) { throw 'Canonical worktree inventory changed' }
$states = @()
foreach ($entry in $snapshot.states) {
    $state = @(git -C $entry.path status --porcelain=v1 --untracked-files=all)
    if ($LASTEXITCODE -ne 0) { throw "Cannot inspect $($entry.path)" }
    if ($state.Count -or $entry.status.Count) { throw "Unexpected dirty protected worktree: $($entry.path)" }
    $states += [pscustomobject]@{ path = $entry.path; result = 'UNCHANGED_CLEAN' }
}
$main = @(Invoke-ReadGit -GitArgs @('show', '-s', '--format=%H %T %P', 'main'))
if ($main[0] -ne '558f8ac82ac1b5e07da68f4a71a70240eaf38409 08a01a1b7d5d3109b991bcd05d57272fd3f5f018 0da1f56d3afc4448fb04e33a6aa84ebff3513599') { throw 'Canonical main mismatch' }
$null = git -C $repoPath cat-file -e f3de16ffd04015f345fcdd65e8ca996fdb19555d
$objectExit = $LASTEXITCODE
if ($objectExit -ne 1) { throw "Expected Q3 absent from canonical object database; exit=$objectExit" }
$report = [pscustomobject]@{
    result = 'PASS'
    repo = $repoPath
    main = $main[0]
    refsUnchanged = $true
    worktreeInventoryUnchanged = $true
    worktrees = $states
    q3ObjectAbsent = $true
    catFileExit = $objectExit
    commands = @('git -C <canonical> show-ref', 'git -C <canonical> worktree list --porcelain',
        'git -C <each protected path> status --porcelain=v1 --untracked-files=all',
        'git -C <canonical> show -s --format=%H %T %P main',
        'git -C <canonical> cat-file -e f3de16ffd04015f345fcdd65e8ca996fdb19555d')
    mutations = 'None; output report only in isolated acceptance worktree'
}
$report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'owner-01-canonical-validation.json') -Encoding utf8
Write-Output "PASS: canonical refs/inventory unchanged; $($states.Count) worktrees unchanged and clean; main identity matches; Q3 absent (expected exit 1)"
exit 0
