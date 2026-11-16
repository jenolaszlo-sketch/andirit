param([Parameter(Mandatory)][ValidateSet("Commit", "Verify")][string]$Action,
      [Parameter(Mandatory)][string]$Staged,
      [string]$Repo = 'D:/projects/Andirit')

$ErrorActionPreference = 'Stop'
$start = 'd48feeaa316f76901b6e37264d6d115f689a7000'
$startTree = '8050060b64fa2444282167c2a0f60131795c0f64'
$r1 = 'afa136f4033f35d9a657d8c5f1d5db92d736b679'
$base = '48370711a6247b972dfc83628618924e864be374'
$report = 'docs/reviews/ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-02.md'
$package = 'docs/reviews/CI1A-S2.5-WP-S25-4-QUAL-02-evidence'
$message = 'docs(review): record independent R1 requalification and Stage-2 integration findings (ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-02)'

function Invoke-AndiritGit([string[]]$Cli) {
    & git --no-optional-locks -C $Repo @Cli
    if ($LASTEXITCODE -ne 0) { throw "Git command failed ($LASTEXITCODE): $($Cli -join ' ')" }
}

function Assert-Inventory([string[]]$Paths, [string[]]$State) {
    $wanted = @($Paths | ForEach-Object { "A" + [char]9 + $_ })
    if ($State.Count -ne $wanted.Count -or @(Compare-Object -CaseSensitive $wanted $State).Count) {
        throw 'Qualification addition inventory does not match the exact sealed package and report.'
    }
}

function Assert-RepositoryState([int]$Ahead) {
    $status = @(Invoke-AndiritGit @('status','--porcelain=v1','--branch'))
    if ($status.Count -ne 1 -or $status[0] -cne "## main...origin/main [ahead $Ahead]") {
        throw "Unexpected branch or worktree status: $($status -join ', ')"
    }
    $worktrees = @(Invoke-AndiritGit @('worktree','list','--porcelain'))
    if (@($worktrees | Where-Object { $_ -like 'worktree *' }).Count -ne 1) { throw 'Worktree inventory differs' }
    if (@(Invoke-AndiritGit @('stash','list')).Count -ne 0) { throw 'Unexpected stash' }
    $divergence = Invoke-AndiritGit @('rev-list','--left-right','--count','refs/remotes/origin/main...HEAD')
    if ($divergence -notmatch "^0\s+$Ahead$") { throw 'Unexpected Git divergence' }
    if ((Invoke-AndiritGit @('rev-parse','refs/remotes/origin/main')) -cne $base) { throw 'Recorded origin/main differs' }
    $status
    $worktrees
    "Recorded origin/main divergence (behind/ahead): $divergence"
    'Stashes: 0'
}

$head = Invoke-AndiritGit @('rev-parse','HEAD')
if ((Invoke-AndiritGit @('rev-parse',"$start^{tree}")) -cne $startTree) { throw 'Starting tree differs' }
if ((Invoke-AndiritGit @('rev-parse',"$start^")) -cne $r1) { throw 'R1 direct ancestry differs' }

if ($Action -eq 'Commit') {
    if ($head -cne $start) { throw 'Starting HEAD differs' }
    Assert-RepositoryState 4
    foreach ($relative in $report,$package) {
        if (Test-Path -LiteralPath (Join-Path $Repo $relative)) { throw "An entry already exists at $relative" }
        if (!(Test-Path -LiteralPath (Join-Path $Staged $relative))) { throw "Missing staged input $relative" }
    }
    $manifest = Get-Content -Raw (Join-Path $Staged "$package/manifest.json") | ConvertFrom-Json
    python (Join-Path $Staged "$package/seal.py") --verify (Join-Path $Staged $package)
    if ($LASTEXITCODE -ne 0) { throw 'Pre-copy package hash mismatch' }
    foreach ($relative in $report,$package) {
        Copy-Item -LiteralPath (Join-Path $Staged $relative) -Destination (Join-Path $Repo $relative) -Recurse
    }
    python (Join-Path $Repo "$package/seal.py") --verify (Join-Path $Repo $package)
    if ($LASTEXITCODE -ne 0) { throw 'Post-copy package hash mismatch' }
    $paths = @($report,"$package/manifest.json") + @($manifest.files | ForEach-Object { "$package/$($_.path)" })
    Invoke-AndiritGit @('add','--',$report,$package)
    $inventory = @(Invoke-AndiritGit @('diff','--cached','--name-status','--no-renames'))
    Assert-Inventory $paths $inventory
    Invoke-AndiritGit @('commit','-m',$message)
    $head = Invoke-AndiritGit @('rev-parse','HEAD')
}
if ((Invoke-AndiritGit @('rev-parse',"$head^")) -cne $start) { throw 'The qualification commit must directly descend from start' }
Invoke-AndiritGit @('show','-s','--format=%H %T %P %s',$start)
Invoke-AndiritGit @('show','-s','--format=%H %T %P %s',$head)
$manifest = Get-Content -Raw (Join-Path $Repo "$package/manifest.json") | ConvertFrom-Json
$paths = @($report,"$package/manifest.json") + @($manifest.files | ForEach-Object { "$package/$($_.path)" })
$inventory = @(Invoke-AndiritGit @('diff','--name-status','--no-renames',$start,$head))
Assert-Inventory $paths $inventory
$inventory
Invoke-AndiritGit @('diff','--exit-code',$r1,$head,'--','.',':(exclude)docs/**')
Invoke-AndiritGit @('diff','--exit-code',$start,$head,'--','.',":(exclude)$report",":(exclude)$package/**")
python (Join-Path $Repo "$package/seal.py") --verify (Join-Path $Repo $package)
if ($LASTEXITCODE -ne 0) { throw 'Post-commit QUAL-02 seal mismatch' }
python (Join-Path $Repo "$package/verify-integrity.py")
if ($LASTEXITCODE -ne 0) { throw 'Post-commit historical integrity verification failed' }
Assert-RepositoryState 5
'Protected paths and historical evidence unchanged. Verification passed.'


