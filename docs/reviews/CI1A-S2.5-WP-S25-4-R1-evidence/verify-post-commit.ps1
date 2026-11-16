$ErrorActionPreference = 'Stop'
$start = '8ab2d25d058e5bb4301094078f1737b139868e6a'
$baseline = '48370711a6247b972dfc83628618924e864be374'
$entry = '581b84a5e910626b87fb260becf90aac500dd7bb'
$implementation = 'afa136f4033f35d9a657d8c5f1d5db92d736b679'
$historical = 'docs/reviews/CI1A-S2.5-WP-S25-4-QUAL-01-evidence'
$package = 'docs/reviews/CI1A-S2.5-WP-S25-4-R1-evidence'
$report = 'docs/reviews/ANDIRIT-CI1A-S2.5-WP-S25-4-R1.md'

function Assert-ExitZero($context) {
    if ($LASTEXITCODE -ne 0) { throw "$context failed (exit $LASTEXITCODE)" }
}

function Assert-Inventory($expected, $observed) {
    if (@(Compare-Object -CaseSensitive @($expected) @($observed)).Count) {
        throw 'Changed-file inventory differed'
    }
}

$final = git rev-parse HEAD
Assert-ExitZero 'Resolve final HEAD'
if ((git branch --show-current) -ne 'main' -or (git rev-parse HEAD^) -ne $implementation -or
    (git rev-parse "$implementation^") -ne $start -or (git rev-parse "$start^") -ne $entry -or
    (git rev-parse "$entry^") -ne $baseline -or (git rev-parse origin/main) -ne $baseline) {
    throw 'Unexpected branch, direct parents or recorded origin/main'
}
foreach ($rev in $baseline,$entry,$start,$implementation,$final) {
    git show -s --format='%H %T %P %s' $rev
    Assert-ExitZero 'Read commit and tree'
}
$divergence = git rev-list --left-right --count origin/main...HEAD
if ($divergence -notmatch '^0\s+4$') { throw 'Unexpected local divergence' }
"Recorded origin/main to local main (behind/ahead): $divergence"

$implementationFiles = @(
    'M' + "`t" + 'src/Andirit.CSharp/VerifiedCSharpAnalysis.cs'
    'M' + "`t" + 'tests/Andirit.CSharp.Tests/S23PublicationIntegrationTests.cs'
    'M' + "`t" + 'tests/Andirit.CSharp.Tests/S2_2Wp5R1AmbiguityTests.cs'
    'M' + "`t" + 'tests/Andirit.CSharp.Tests/S2_2Wp8CorpusIntegrationTests.cs'
)
Assert-Inventory $implementationFiles (git diff --name-status $start $implementation)
git diff --check $start $implementation
Assert-ExitZero 'Implementation whitespace check'
git diff --exit-code $baseline $start -- . ':(exclude)docs/**'
Assert-ExitZero 'Starting tree versus frozen protected surfaces'
git diff --exit-code $start $final -- docs ':(exclude)docs/reviews/ANDIRIT-CI1A-S2.5-WP-S25-4-R1.md' ':(exclude)docs/reviews/CI1A-S2.5-WP-S25-4-R1-evidence/**'
Assert-ExitZero 'Historical documentation and evidence Git byte preservation'
git diff --exit-code $start $final -- . ':(exclude)docs/**' ':(exclude)src/Andirit.CSharp/VerifiedCSharpAnalysis.cs' ':(exclude)tests/Andirit.CSharp.Tests/S23PublicationIntegrationTests.cs' ':(exclude)tests/Andirit.CSharp.Tests/S2_2Wp5R1AmbiguityTests.cs' ':(exclude)tests/Andirit.CSharp.Tests/S2_2Wp8CorpusIntegrationTests.cs'
Assert-ExitZero 'Protected-path Git identity'

python "$historical/verify-manifest.py"
Assert-ExitZero 'QUAL-01 seal'
$history = & "$historical/verify-history.ps1"
$records = @($history | Select-Object -First 9 | ForEach-Object { $_ | ConvertFrom-Json })
if ($records.Count -ne 9 -or @($records | Where-Object {
    !$_.hashMatches -or !$_.identityMatches -or !$_.ancestor -or !$_.countMatches -or $_.mismatches.Count
}).Count -or ($records | Measure-Object entries -Sum).Sum -ne 183) { throw 'Historical seal verification failed' }
Remove-Item Env:GIT_DIR,Env:GIT_WORK_TREE,Env:GIT_OPTIONAL_LOCKS
$history
'Historical seals: nine manifests and 183 entries verified'

$manifest = Get-Content -Raw "$package/manifest.json" | ConvertFrom-Json
if ($manifest.reviewedHead -ne $implementation -or $manifest.reviewedTree -ne (git rev-parse "$implementation^{tree}")) {
    throw 'R1 evidence is not bound to the implementation'
}
$root = (Resolve-Path $package).Path
$entries = @(Get-ChildItem -LiteralPath $root -Recurse -Force -File | ForEach-Object {
    $_.FullName.Substring($root.Length + 1).Replace('\','/')
} | Where-Object { $_ -cne 'manifest.json' })
Assert-Inventory $entries $manifest.files.path
foreach ($item in $manifest.files) {
    $path = Join-Path $root $item.path
    if ((Get-Item -Force -LiteralPath $path).Length -ne $item.bytes -or
        (Get-FileHash -Algorithm SHA256 -LiteralPath $path).Hash -ne $item.sha256) {
        throw "R1 manifest mismatch: $($item.path)"
    }
}
"R1 seal: $($entries.Count) entries verified"
"R1 manifest SHA-256: $((Get-FileHash "$package/manifest.json" -Algorithm SHA256).Hash.ToLowerInvariant())"

$evidenceFiles = @('A' + "`t" + $report; 'A' + "`t" + "$package/manifest.json")
$evidenceFiles += @($entries | ForEach-Object { 'A' + "`t" + "$package/$_" })
$inventory = git diff --name-status $implementation $final
Assert-Inventory $evidenceFiles $inventory
'Final changed-file inventory:'
git diff --name-status $start $final

$oracleHash = '97f6c5e5a4cc77e089481c7643732b7a07a31d483c2643bd7a95fdad7ced39c9'
foreach ($oracle in "$historical/oracle-r2.json","$package/verification/oracle-r2.json") {
    if ((Get-FileHash $oracle -Algorithm SHA256).Hash -ne $oracleHash -or (Get-Item $oracle).Length -ne 146851) {
        throw "Frozen oracle bytes differ: $oracle"
    }
    "Frozen oracle verified: $oracleHash 146851 $oracle"
}

$state = git --no-optional-locks status --porcelain=v1 --branch
Assert-ExitZero 'Git status'
if (@($state).Count -ne 1 -or $state -ne '## main...origin/main [ahead 4]') { throw 'Unclean final state' }
$state
$worktrees = @(git worktree list --porcelain)
Assert-ExitZero 'Worktree inventory'
if (@($worktrees | Where-Object { $_ -like 'worktree *' }).Count -ne 1) { throw 'Expected a single worktree' }
$worktrees
$stashes = @(git stash list)
Assert-ExitZero 'Stash inventory'
if ($stashes.Count) { throw 'Unexpected stashes' }
'Stashes: 0'
'Post-commit verification passed.'
