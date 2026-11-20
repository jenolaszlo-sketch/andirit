param([Parameter(Mandatory)][ValidateSet("Commit","Verify")][string]$Action,
      [Parameter(Mandatory)][string]$Staged,
      [string]$Repo = 'D:/projects/Andirit')

$ErrorActionPreference = 'Stop'
$start = '6d4d45f7aedc85f4b52310cf9a3fa6082f885296'
$startTree = 'd60f3527090aa0a11a68abb10ce05cd3c6e87ee9'
$f02 = '786574e34de0e5e1b242a784a5dc7297e0162bb1'
$production = '25bc6e3b4305c7ce088deb36cfb000138dc76648'
$productionTree = '2a1309d8e08f230e61a1cd1bf0f1b596179ea8b3'
$origin = '48370711a6247b972dfc83628618924e864be374'
$report = 'docs/reviews/ANDIRIT-CI1A-S2.5-WP-S25-4-R2.md'
$package = 'docs/reviews/CI1A-S2.5-WP-S25-4-R2-evidence'
$qualified = 'docs/reviews/CI1A-S2.5-WP-S25-4-QUAL-02-evidence'
$implementation = @(
    'src/Andirit.CSharp/CSharpCodeGraphPlugin.Compilation.cs',
    'src/Andirit.CSharp/CSharpCodeGraphPlugin.Session.cs',
    'src/Andirit.CSharp/VerifiedCSharpOccurrenceExtractor.cs',
    'tests/Andirit.CSharp.Tests/S23PublicationIntegrationTests.cs'
)

function Invoke-RepoGit([string[]]$Cli) {
    & git --no-optional-locks -C $Repo @Cli
    if ($LASTEXITCODE -ne 0) { throw "Git command failed ($LASTEXITCODE): $($Cli -join ' ')" }
}

function Check-Inventory([string[]]$Expected, [string[]]$Actual) {
    if ($Expected.Count -ne $Actual.Count -or @(Compare-Object -CaseSensitive $Expected $Actual).Count) {
        throw 'Unexpected changed-file inventory'
    }
}

function Verify-RepoState([int]$Ahead) {
    $status = @(Invoke-RepoGit @('status','--porcelain=v1','--branch'))
    $worktrees = @(Invoke-RepoGit @('worktree','list','--porcelain'))
    $stashes = @(Invoke-RepoGit @('stash','list'))
    $divergence = Invoke-RepoGit @('rev-list','--left-right','--count','refs/remotes/origin/main...HEAD')
    if ($status.Count -ne 1 -or $status[0] -cne "## main...origin/main [ahead $Ahead]" -or
        @($worktrees | Where-Object { $_ -like 'worktree *' }).Count -ne 1 -or $stashes.Count -ne 0 -or
        $divergence -notmatch "^0\s+$Ahead$" -or
        (Invoke-RepoGit @('rev-parse','refs/remotes/origin/main')) -cne $origin) {
        throw 'Wrong branch, worktree, stash or divergence state'
    }
    $status
    $worktrees
    "Recorded origin/main divergence (behind/ahead): $divergence"
    'Stashes: 0'
}
function Verify-Seal([string]$PackagePath) {
    python (Join-Path $PackagePath 'seal.py') --verify $PackagePath
    if ($LASTEXITCODE -ne 0) { throw "Seal mismatch at $PackagePath" }
}

if ((Invoke-RepoGit @('rev-parse',"$start^{tree}")) -cne $startTree -or
    (Invoke-RepoGit @('rev-parse',"$production^{tree}")) -cne $productionTree -or
    (Invoke-RepoGit @('rev-parse',"$f02^")) -cne $start -or
    (Invoke-RepoGit @('rev-parse',"$production^")) -cne $f02) {
    throw 'Wrong starting or implementation identity'
}
$implementationChanges = @(Invoke-RepoGit @('diff','--name-status','--no-renames',$start,$production))
Check-Inventory @($implementation | ForEach-Object { "M" + [char]9 + $_ }) $implementationChanges
$implementationChanges
$head = Invoke-RepoGit @('rev-parse','HEAD')
if ($Action -eq 'Commit') {
    if ($head -cne $production) { throw 'Wrong evidence commit parent' }
    Verify-RepoState 7
    Verify-Seal (Join-Path $Staged $package)
    foreach ($path in $report,$package) {
        if (Test-Path -LiteralPath (Join-Path $Repo $path)) { throw "Entry already exists at $path" }
        Copy-Item -LiteralPath (Join-Path $Staged $path) -Destination (Join-Path $Repo $path) -Recurse
    }
    Verify-Seal (Join-Path $Repo $package)
    $manifest = Get-Content -Raw -LiteralPath (Join-Path $Repo "$package/manifest.json") | ConvertFrom-Json
    $additions = @($report,"$package/manifest.json") + @($manifest.files | ForEach-Object { "$package/$($_.path)" })
    $expected = @($additions | ForEach-Object { "A" + [char]9 + $_ })
    Invoke-RepoGit @('add','--',$report,$package)
    Check-Inventory $expected @(Invoke-RepoGit @('diff','--cached','--name-status','--no-renames'))
    Invoke-RepoGit @('commit','-m','docs(review): record bounded Stage-2 R2 remediation and M5.4 investigation (ANDIRIT-CI1A-S2.5-WP-S25-4-R2)')
    $head = Invoke-RepoGit @('rev-parse','HEAD')
}
if ((Invoke-RepoGit @('rev-parse',"$head^")) -cne $production) { throw 'Incorrect post-commit ancestry' }
$manifest = Get-Content -Raw -LiteralPath (Join-Path $Repo "$package/manifest.json") | ConvertFrom-Json
$additions = @($report,"$package/manifest.json") + @($manifest.files | ForEach-Object { "$package/$($_.path)" })
$evidenceChanges = @(Invoke-RepoGit @('diff','--name-status','--no-renames',$production,$head))
Check-Inventory @($additions | ForEach-Object { "A" + [char]9 + $_ }) $evidenceChanges
$evidenceChanges
foreach ($rev in $start,$f02,$production,$head) {
    Invoke-RepoGit @('show','-s','--format=%H %T %P %s',$rev)
}
Invoke-RepoGit @('diff','--exit-code',$production,$head,'--','.',":(exclude)$report",":(exclude)$package/**")
$protect = @('diff','--exit-code',$start,$head,'--','.',":(exclude)$report",":(exclude)$package/**") +
    @($implementation | ForEach-Object { ":(exclude)$_" })
Invoke-RepoGit $protect
Verify-Seal (Join-Path $Repo $package)
Verify-Seal (Join-Path $Repo $qualified)
python (Join-Path $Repo "$qualified/verify-integrity.py")
if ($LASTEXITCODE -ne 0) { throw 'Historical manifests failed' }
$oracle = Join-Path $Repo "$package/gates/oracle-r2.json"
$hash = (Get-FileHash -LiteralPath $oracle -Algorithm SHA256).Hash.ToLowerInvariant()
if ($hash -cne '97f6c5e5a4cc77e089481c7643732b7a07a31d483c2643bd7a95fdad7ced39c9' -or
    (Get-Item -LiteralPath $oracle).Length -ne 146851) { throw 'Frozen oracle identity differs' }
"R2 frozen oracle verified: $hash (146851 bytes)"
Verify-RepoState 8
'Protected paths and historical evidence verified. R2 post-commit verification passed.'

