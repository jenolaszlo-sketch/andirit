$ErrorActionPreference = 'Stop'
$work = $PSScriptRoot
$out = "$work/evidence/membership-tests"
New-Item -ItemType Directory -Path $out -Force | Out-Null
Set-Location "$work/production"
$project = 'tests/Andirit.CSharp.Tests/Andirit.CSharp.Tests.csproj'
$env:DOTNET_CLI_HOME = "$work/dotnet-home"
function Run-Test([string]$name, [string]$project, [string]$filter) {
    $log = "$out/$name.log"
    $cli = @('test', $project, '-c', 'Release', '--no-build', '--filter', $filter, '--logger', 'console;verbosity=detailed')
    "Phase: $env:S23_REOPEN_FRESH_PROCESS`nCommand: dotnet $($cli -join ' ')" | Set-Content $log
    dotnet @cli *>> $log
    $code = $LASTEXITCODE
    "Exit code: $code" | Add-Content $log
    if ($code -ne 0) { $script:failed = $true }
    Write-Output "$name $code"
}
$failed = $false
Run-Test 'accounting' $project 'FullyQualifiedName~CompleteScopeMembership'
Run-Test 'disjoint' 'tests/Andirit.Tests/Andirit.Tests.csproj' 'FullyQualifiedName~S2_2Wp6CoverageMergeTests|FullyQualifiedName~OccurrenceInMemoryParityTests'
$env:S23_REOPEN_FRESH_PROCESS_PATH = "$work/stores/fresh-membership"
foreach ($phase in 'publish','verify') {
    $env:S23_REOPEN_FRESH_PROCESS = $phase
    Run-Test $phase $project 'FullyQualifiedName~CompleteScopeMembership_PublishesDisjointRows|FullyQualifiedName~CompleteScopeMembership_ReopensLegacyRowsWithMatchingAuthority'
}
foreach ($generation in 'first','partial','restored','repeat') {
    foreach ($action in 'publish','verify') {
        $phase = "$action-$generation"
        $env:S23_REOPEN_FRESH_PROCESS = $phase
        Run-Test $phase $project 'FullyQualifiedName~CompleteScopeMembership_ReplacementRestoresWithoutAccumulating'
    }
}
if ($failed) { exit 1 }
