param(
    [Parameter(Mandatory)][string]$Root,      # production export root
    [Parameter(Mandatory)][string]$Harness,   # harness project directory
    [Parameter(Mandatory)][string]$Out,       # receipts directory
    [string]$Tag = "r3",
    [string]$Fixture = "",                    # optional fixture root (default: QUAL-01 corpus in $Root)
    [string[]]$Only = @(),                    # optional case-name prefix filter
    [switch]$Keep                             # preserve existing store files (multi-build cross-revision runs)
)
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$env:DOTNET_CLI_HOME = "C:\Windows\Temp\opencode\qual04\dotnet-home"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'; $env:DOTNET_NOLOGO = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
New-Item -ItemType Directory -Path $Out -Force | Out-Null
New-Item -ItemType Directory -Path "C:\Windows\Temp\opencode\qual04\stores" -Force | Out-Null
$dll = Join-Path $Harness "bin\Release\net10.0\Andirit.CSharp.Tests.dll"
$fixtureRoot = $Fixture
if ([string]::IsNullOrWhiteSpace($fixtureRoot)) {
    $fixtureRoot = Join-Path $Root "docs\reviews\CI1A-S2.5-WP-S25-4-QUAL-01-evidence\fixture"
}
$failed = $false

function Run-Case([string]$case, [string]$phase, [string]$dbName, [string]$extra, [bool]$clear = $true) {
    if ($Only.Count -gt 0 -and (@($Only) | Where-Object { $case.StartsWith($_) } | Measure-Object).Count -eq 0) { return }
    $db = "C:\Windows\Temp\opencode\qual04\stores\$dbName.ltdb"
    $receipt = Join-Path $Out "$Tag-$case-$phase.json"
    # Only the publishing phase starts from an empty durable file; the verify
    # phase must reopen exactly what the publishing process left behind.
    if ($clear -and $phase -eq 'publish' -and -not $Keep) {
        Remove-Item $db -Force -ErrorAction SilentlyContinue
        Remove-Item "$db-wal" -Force -ErrorAction SilentlyContinue
    }
    $cli = @($dll, $case, $db, $receipt)
    if ($extra) { $cli += $extra }
    $env:QUAL4_PHASE = $phase
    "UTC: $([DateTimeOffset]::UtcNow.ToString('o'))`nCase: $case Phase: $phase`nCommand: dotnet $($cli -join ' ')" |
        Set-Content "$receipt.log"
    & dotnet @cli *>> "$receipt.log"
    $code = $LASTEXITCODE
    "Exit code: $code" | Add-Content "$receipt.log"
    Write-Output "$Tag/$case/$phase = $code"
    if ($code -ne 0) { $script:failed = $true }
    $env:QUAL4_PHASE = $null
}

if (-not (Test-Path $dll)) {
    Write-Output "harness dll missing: $dll (build first)"
    exit 2
}

# Mandatory membership controls (fresh-process publish + verify each).
foreach ($case in 'M1','M2','M3','M4','M5','M6','M7','M8') {
    foreach ($phase in 'publish','verify') { Run-Case $case $phase "$Tag-$case" $null }
}
# Adversarial controls.
foreach ($case in 'A1','A2','A3','A4','A6') {
    foreach ($phase in 'publish','verify') { Run-Case $case $phase "$Tag-$case" $null }
}
# Replacement lifecycle: 2/2 -> 3/2 -> 2/2 -> 2/2 on one store (kept across all
# generations and cleared once at the start).
Remove-Item "C:\Windows\Temp\opencode\qual04\stores\$Tag-lifecycle.ltdb" -Force -ErrorAction SilentlyContinue
Remove-Item "C:\Windows\Temp\opencode\qual04\stores\$Tag-lifecycle.ltdb-wal" -Force -ErrorAction SilentlyContinue
Remove-Item "C:\Windows\Temp\opencode\qual04\stores\$Tag-lifecycle.ltdb.publication.json" -Force -ErrorAction SilentlyContinue
foreach ($generation in 'first','partial','restored','repeat') {
    foreach ($phase in 'publish','verify') {
        Run-Case 'lifecycle' "$phase-$generation" "$Tag-lifecycle" $null $false
    }
}
# Legacy: matching-authority historical layout, tamper boundaries.
foreach ($phase in 'publish','verify') { Run-Case 'X2' $phase "$Tag-X2" $null }
foreach ($case in 'X4a','X4b','X3') {
    foreach ($phase in 'publish','verify') { Run-Case $case $phase "$Tag-$case" $null }
}
# Preservation: corpus classification, unsupported sites, unavailable analysis.
foreach ($case in 'F254-01','F254-02','F254-03-none') {
    foreach ($phase in 'publish','verify') {
        Run-Case $case $phase "$Tag-$case" $(if ($case -eq 'F254-01') { $fixtureRoot } else { $null })
    }
}
# Genuine cross-revision store (publication and same-revision reopen).
foreach ($phase in 'publish','verify') { Run-Case 'X1' $phase "$Tag-X1" $null }

if ($failed) { exit 1 }
exit 0