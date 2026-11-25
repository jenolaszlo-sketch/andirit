param(
    [Parameter(Mandatory)] [string] $Results,
    [Parameter(Mandatory)] [ValidatePattern('^[a-f0-9]{40}$')] [string] $ExpectedSha,
    [switch] $Capture,
    [string] $ExpectedRunId,
    [string] $ExpectedRunAttempt,
    [string] $ExpectedRunnerOS,
    [string] $ExpectedRunnerArch
)

$ErrorActionPreference = 'Stop'
$projects = @('core', 'csharp', 'treesitter', 'latticedb')
$expectedFiles = $projects | ForEach-Object { "$_/tests.trx" }
$manifest = Join-Path $Results 'subject.json'

function Get-ResultHashes {
    param([string] $Directory)
    $root = (Resolve-Path -LiteralPath $Directory).ProviderPath
    $hashes = [ordered]@{}
    foreach ($file in Get-ChildItem -LiteralPath $root -Recurse -File -Filter '*.trx' | Sort-Object FullName) {
        $relativePath = [IO.Path]::GetRelativePath($root, $file.FullName).Replace('\', '/')
        $hashes[$relativePath] = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    }
    return $hashes
}

if ($Capture) {
    $head = git rev-parse HEAD
    if ($LASTEXITCODE -ne 0 -or $head -cne $ExpectedSha) {
        throw "Checked-out HEAD '$head' differs from expected subject '$ExpectedSha'."
    }
    git diff --quiet HEAD -- src tests .github
    if ($LASTEXITCODE -ne 0) { throw 'Tracked implementation or CI files differ from the tested subject.' }
    $sdk = dotnet --version
    if ($LASTEXITCODE -ne 0) { throw 'Unable to identify the test SDK.' }
    $dotnetInfo = dotnet --info
    if ($LASTEXITCODE -ne 0) { throw 'Unable to capture .NET runtime and operating system details.' }
    New-Item -ItemType Directory -Path $Results -Force | Out-Null
    @{
        headSha = $head
        runnerOS = $env:RUNNER_OS
        runnerArchitecture = $env:RUNNER_ARCH
        runId = $env:GITHUB_RUN_ID
        runAttempt = $env:GITHUB_RUN_ATTEMPT
        sdk = $sdk
        dotnetInfo = ($dotnetInfo -join "`n")
        trxSha256 = Get-ResultHashes $Results
    } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $manifest -Encoding utf8
    return
}

$subject = Get-Content -LiteralPath $manifest -Raw | ConvertFrom-Json -AsHashtable
if ($subject.headSha -cne $ExpectedSha -or [string]::IsNullOrWhiteSpace($subject.sdk) -or
    [string]::IsNullOrWhiteSpace($subject.dotnetInfo) -or [string]::IsNullOrWhiteSpace($subject.runnerOS) -or
    [string]::IsNullOrWhiteSpace($subject.runnerArchitecture) -or [string]::IsNullOrWhiteSpace($subject.runId) -or
    [string]::IsNullOrWhiteSpace($subject.runAttempt)) {
    throw 'The subject receipt lacks the exact commit or required runner metadata.'
}
if (($ExpectedRunId -and $subject.runId -cne $ExpectedRunId) -or
    ($ExpectedRunAttempt -and $subject.runAttempt -cne $ExpectedRunAttempt) -or
    ($ExpectedRunnerOS -and $subject.runnerOS -cne $ExpectedRunnerOS) -or
    ($ExpectedRunnerArch -and $subject.runnerArchitecture -cne $ExpectedRunnerArch)) {
    throw 'The subject receipt does not match the requested run, attempt or runner.'
}
Write-Output "subject=$($subject.headSha) run=$($subject.runId) attempt=$($subject.runAttempt) os=$($subject.runnerOS) arch=$($subject.runnerArchitecture) sdk=$($subject.sdk)"
$actualHashes = Get-ResultHashes $Results
if ($actualHashes.Count -ne $expectedFiles.Count -or $subject.trxSha256.Count -ne $expectedFiles.Count) {
    throw 'Expected exactly one bound TRX file for each of the four CI test projects.'
}

$latticedb = @{}
foreach ($project in $projects) {
    $path = "$project/tests.trx"
    if (-not $actualHashes[$path] -or $actualHashes[$path] -cne $subject.trxSha256[$path]) {
        throw "Missing or unbound result file '$path'."
    }
    $trx = [xml]::new()
    $trx.XmlResolver = $null
    $trx.Load((Resolve-Path -LiteralPath (Join-Path $Results $path)).ProviderPath)
    $ns = [Xml.XmlNamespaceManager]::new($trx.NameTable)
    $ns.AddNamespace('t', 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010')
    $definitions = @{}
    foreach ($definition in $trx.SelectNodes('/t:TestRun/t:TestDefinitions/t:UnitTest', $ns)) {
        $method = $definition.SelectSingleNode('t:TestMethod', $ns)
        if (-not $method -or -not $method.className -or -not $method.name -or -not $definition.id) {
            throw "$project TRX contains an incomplete test definition."
        }
        $definitions.Add($definition.id, "$($method.className).$($method.name)")
    }
    $resultNodes = $trx.SelectNodes('/t:TestRun/t:Results/t:UnitTestResult', $ns)
    $counters = $trx.SelectSingleNode('/t:TestRun/t:ResultSummary/t:Counters', $ns)
    if (-not $counters -or $resultNodes.Count -eq 0 -or $resultNodes.Count -ne [int]$counters.total) {
        throw "$project TRX has missing or inconsistent results."
    }
    foreach ($result in $resultNodes) {
        $name = $definitions[$result.testId]
        if (-not $name -or -not $result.outcome) { throw "$project TRX has an unbound result." }
        if ($project -eq 'latticedb') { $latticedb[$name] += @($result) }
    }
    Write-Output "$project total=$($counters.total) passed=$($counters.passed) failed=$($counters.failed) notExecuted=$($counters.notExecuted) sha256=$($actualHashes[$path])"
}

$publicationClass = 'Andirit.LatticeDb.Tests.NativePublicationCorrectnessTests'
$durabilityClass = 'Andirit.LatticeDb.Tests.NativeDurabilityTests'
$mandatory = [ordered]@{
    'P-01' = "$publicationClass.P01_QueryPinnedToSupersededGeneration_RefusesExplicitly"
    'P-02' = "$publicationClass.P02_OccurrencePageNotBoundToAcceptedPublication_RefusesExplicitlyOnOpen"
    'P-03' = "$publicationClass.P03_StaleIndexDoesNotScanTheWholeStore_AndBoundedQueryExaminesOnlyItsTargetPage"
    'P-04' = "$publicationClass.P04_FailedCompletion_PreservesPriorOccurrencePublicationAndIndex"
    'P-05' = "$publicationClass.P05_OlderGenerationsSuperseded_LatestAcceptedAndDurable"
    'P-06' = "$publicationClass.P06_RunPlannedAgainstSupersededBaseline_CannotCompleteAndRegressGeneration"
    'P-07' = "$publicationClass.P07_OrdinaryReopen_OccurrenceQueryResultsEquivalent"
    'P-08' = "$publicationClass.P08_RebuildFromDurableState_OccurrenceIndexEquivalent"
    'P-09' = "$publicationClass.P09_ContributorUnionAndConflictRejection_VisibilityConsistent"
    'P-10' = "$publicationClass.P10_ScopedDeletion_RemovesOnlyAffectedContextLeavesIndexClean"
    'P-11' = "$publicationClass.P11_BoundedQuery_DeterministicSiteOrderAndExplicitLimit"
    'P-12' = "$publicationClass.P12_ConcurrentReaderDuringPublication_SeesAtomicSnapshot"
    'P-13' = "$publicationClass.P13_RapidSequentialPublications_OnlyLatestAccepted"
    'P-14' = "$publicationClass.P14_UnknownCommitOutcome_QuarantineAndReopenCorroborates"
    'P-15' = "$publicationClass.P15_MultiProcessPublication_FileLockSerializesAndReopenSeesCommittedPublication"
    'P-16' = "$publicationClass.P16_InterleavingIsNotANativeEvidenceSurface"
    'D-05' = "$durabilityClass.ConcurrentMultiProcessAccess_FileLockingAndConsistency"
    'D-07' = "$durabilityClass.AbruptProcessTermination_DurableStateConsistent"
    'D-07-early-exit' = "$durabilityClass.D07_ExitBeforeReadyFailsPromptly"
}
$requiredObservations = @{
    'P-12' = @('readerQueued=true')
    'P-15' = @('READY|', 'PUBLISHED|run:p15-pub|', 'sha256=')
    'D-05' = @('READY|', 'PUBLISHED|run:durability-pub|', 'CANCELLED|', 'sha256=')
    'D-07' = @('READY|', 'KILLED exit=', 'PUBLISHED|run:d07-after-recovery|', 'sha256=')
}
foreach ($case in $mandatory.Keys) {
    $name = $mandatory[$case]
    $rows = $latticedb[$name]
    if ($rows.Count -ne 1 -or $rows[0].outcome -cne 'Passed') {
        throw "Mandatory case $case ($name) needs one Passed result. Observed: $($rows.outcome -join ',')."
    }
    foreach ($marker in $requiredObservations[$case]) {
        if (-not ([string]$rows[0].Output.StdOut).Contains($marker, [StringComparison]::Ordinal)) {
            throw "Mandatory case $case ($name) lacks its '$marker' observation."
        }
    }
    Write-Output "$case $name $($rows[0].outcome) duration=$($rows[0].duration)"
}
