param(
    [Parameter(Mandatory = $true)][string]$Out,
    [string]$Project = "tests/Andirit.LatticeDb.Tests/Andirit.LatticeDb.Tests.csproj",
    [string]$Configuration = "Release"
)

# Executes the S6.1 qualification matrix Q-01..Q-12 plus the post-commit-cancellation
# regression guard, one case per receipt, against the production LatticeCodeGraphStore.
#
# Usage (from the repository root):
#   powershell -ExecutionPolicy Bypass -File run-cases.ps1 -Out <receipt-dir>

$ErrorActionPreference = "Continue"
New-Item -ItemType Directory -Force -Path $Out | Out-Null

$cases = [ordered]@{
    "Q-01" = "CancellationBeforeNativeInvocation_LeavesTheDurableStateUnchanged"
    "Q-02" = "CancellationRacingTheNativeWindow_NeverExposesPartialState"
    "Q-03" = "CancellationInsideTheOpenNativeTransaction_IsNotObservedAndCommitsAtomically"
    "Q-04" = "CancellationRacingWithSuccessfulCompletion_NeverReportsCancellationAfterACommit"
    "Q-05" = "CancellationAfterTheIrreversibleCommit_DoesNotReportCancellation"
    "Q-06" = "ReopenAfterACancelledStaging_PreservesTheAcceptedPublication"
    "Q-07" = "ConcurrentCancellationAndDisposal_WaitsForTheInFlightNativeWrite"
    "Q-08" = "DisposalReleasesTheNativeHandle"
    "Q-09" = "SuccessfulNonCancelledControl_PublishesAndReopens"
    "Q-10" = "AcceptedStoreSemanticsRemainIntactAcrossACancelledStaging"
    "Q-11" = "UnknownCommitOutcome_IsReportedAndQuarantinedAndReopenCorroborates"
    "Q-12" = "PinnedNativeDependency_ExposesNoCancellationEntryPoint"
    "REG" = "Regression_CancellationAfterASuccessfulCommit_MustNotSurfaceAsCancelled"
}

$summary = @()
foreach ($case in $cases.GetEnumerator()) {
    $id = $case.Key
    $test = $case.Value
    $log = Join-Path $Out "$id.log"
    $started = (Get-Date).ToUniversalTime().ToString("o")
    $output = (& dotnet test $Project -c $Configuration --no-restore --no-build `
        --filter "FullyQualifiedName~$test" `
        --logger "console;verbosity=detailed" 2>&1 | Out-String)
    $exit = $LASTEXITCODE
    $finished = (Get-Date).ToUniversalTime().ToString("o")

    $header = @(
        "case: $id",
        "test: Andirit.LatticeDb.Tests.NativeCancellationPathTests.$test",
        "project: $Project",
        "configuration: $Configuration",
        "command: dotnet test $Project -c $Configuration --no-restore --no-build --filter `"FullyQualifiedName~$test`" --logger `"console;verbosity=detailed`"",
        "started-utc: $started",
        "finished-utc: $finished",
        "exit-code: $exit",
        ""
    )
    ($header -join "`r`n") + $output | Out-File -FilePath $log -Encoding utf8

    $result = "UNKNOWN"
    if ($output -match "Test Run Successful\." -and $output -match "Passed:\s+1") { $result = "PASS" }
    elseif ($output -match "Test Run Failed\." -or $output -match "Failed:\s+[1-9]") { $result = "FAIL" }
    elseif ($output -match "No test matches") { $result = "NOT-RUN" }

    $summary += [pscustomobject]@{
        case = $id
        test = "Andirit.LatticeDb.Tests.NativeCancellationPathTests.$test"
        result = $result
        exitCode = $exit
        startedUtc = $started
        finishedUtc = $finished
        log = "$id.log"
    }
    Write-Host "$id $result exit=$exit $test"
}

$summary | ConvertTo-Json -Depth 4 | Out-File -FilePath (Join-Path $Out "case-summary.json") -Encoding utf8
$passed = @($summary | Where-Object { $_.result -eq "PASS" }).Count
$failed = @($summary | Where-Object { $_.result -ne "PASS" }).Count
Write-Host "cases: $($summary.Count) passed: $passed failed: $failed"
if ($failed -gt 0) { exit 1 }
exit 0
