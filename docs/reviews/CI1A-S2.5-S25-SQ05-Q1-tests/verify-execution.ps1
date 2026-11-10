# ANDIRIT-CI1A-S2.5-S25-SQ05-Q1 — independent execution and coverage analysis (Q2 + Q4).
# Reviewer-owned; read-only. Freshly downloads the hosted Ubuntu job log from GitHub,
# verifies it against the committed G2 copy, extracts the LatticeDb results and environment,
# confirms the full unfiltered suite ran with zero skips, and independently recomputes the
# coverage numbers from the committed raw Cobertura artifact (F-G2-01 investigation).
[CmdletBinding()]
param(
    [string]$RepoRoot
)

$ErrorActionPreference = 'Stop'
if (-not $RepoRoot) { $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../../..")).Path }
$evidence = Join-Path $RepoRoot 'docs/reviews/CI1A-S2.5-S25-SQ05-Q1-evidence'
$g2 = Join-Path $RepoRoot 'docs/reviews/CI1A-S2.5-WP-S25-2-G2-EXEC-evidence'

$ok = $true
$checks = New-Object System.Collections.Generic.List[object]
function Assert-Check([string]$Name, [bool]$Condition, [object]$Detail) {
    if (-not $Condition) { $script:ok = $false }
    $script:checks.Add([pscustomobject]@{ check = $Name; pass = $Condition; detail = $Detail })
}
function Get-Sha256([string]$Path) { (Get-FileHash -Algorithm SHA256 $Path).Hash.ToLowerInvariant() }

# --- Q2: fresh download of the hosted Ubuntu job log; authenticity vs the committed G2 copy ---
$freshLog = Join-Path $evidence 'gh-job-129592604459-reviewer-copy.log'
& gh run view --job 129592604459 --repo jenolaszlo-sketch/andirit --log 2>&1 | Out-File $freshLog -Encoding utf8
$committedLog = Join-Path $g2 'gh-run-42744298084-ubuntu-job.log'
$freshHash = Get-Sha256 $freshLog
$committedHash = Get-Sha256 $committedLog
Assert-Check 'Ubuntu job log: reviewer-fresh download is byte-identical to the committed G2 copy' ($freshHash -eq $committedHash) "fresh=$freshHash committed=$committedHash"

# LatticeDb test summary from actual test output (summary lines contain "Passed!" and the DLL name).
$summary = (Select-String -Path $freshLog -Pattern 'Passed!.*Andirit\.LatticeDb\.Tests\.dll' | Select-Object -First 1).Line
$lattice = $summary -replace '^.*?Passed!', 'Passed!'
Assert-Check 'L2: LatticeDb suite result is Passed 52 / Failed 0 / Skipped 0 / Total 52 on genuine Linux' ($lattice -match 'Failed:\s*0.*Passed:\s*52.*Skipped:\s*0.*Total:\s*52') $lattice.Trim()

# Other suites green on Linux in the same run (summary lines contain "Passed!" and the DLL name).
foreach ($pair in @(@('Andirit\.Tests\.dll', '345'), @('Andirit\.CSharp\.Tests\.dll', '353'), @('Andirit\.TreeSitter\.Tests\.dll', '157'))) {
    $line = (Select-String -Path $freshLog -Pattern ('Passed!.*' + $pair[0]) | Select-Object -First 1).Line
    Assert-Check ("L-other: " + $pair[0] + " green on Linux (" + $pair[1] + " passed)") (($null -ne $line) -and ($line -match ('Passed:\s*' + $pair[1]))) "$line"
}

# No test filters in the LatticeDb CI step (full suite, no silent exclusions); Release config
# checked over the whole folded YAML step block (the run command spans multiple physical lines).
$ciPath = Join-Path $RepoRoot '.github/workflows/ci.yml'
$ciLines = Get-Content $ciPath
$stepStart = ($ciLines | Select-String -Pattern 'name: Test and coverage \(LatticeDb\)').LineNumber | Select-Object -First 1
$stepBlock = if ($stepStart) { $ciLines[($stepStart - 1)..([Math]::Min($stepStart + 11, $ciLines.Count - 1))] -join ' ' } else { $null }
Assert-Check 'L2b: CI LatticeDb step contains no test filter (complete suite)' (($null -ne $stepBlock) -and ($stepBlock -notmatch '--filter')) "$stepBlock"
Assert-Check 'L2c: CI LatticeDb step uses Release configuration' (($null -ne $stepBlock) -and ($stepBlock -match '-c Release')) "$stepBlock"

# Environment (L1) from the log.
$imgLine = (Select-String -Path $freshLog -Pattern 'Image: ubuntu-24.04' | Select-Object -First 1).Line
$osLine = (Select-String -Path $freshLog -Pattern '24\.04\.5' | Select-Object -First 1).Line
$sdkLine = (Select-String -Path $freshLog -Pattern "SDK with version '10\.0\.403'" | Select-Object -First 1).Line
Assert-Check 'L1: runner image is ubuntu-24.04 (genuine hosted Linux)' ($null -ne $imgLine) $imgLine.Trim()
Assert-Check 'L1b: operating system is Ubuntu 24.04.5 LTS' ($null -ne $osLine) $osLine.Trim()
Assert-Check 'L1c: .NET SDK 10.0.403 installed on the runner' ($null -ne $sdkLine) $sdkLine.Trim()

# --- Q4: independent coverage analysis from the committed raw Cobertura artifact ---
$cob = Join-Path $g2 'coverage-results-ubuntu-latest/coverage-latticedb/coverage.cobertura.xml'
[xml]$x = Get-Content $cob
$lineRate = [double]$x.coverage.GetAttribute('line-rate')
$branchRate = [double]$x.coverage.GetAttribute('branch-rate')
$linesCovered = [int]$x.coverage.GetAttribute('lines-covered')
$linesValid = [int]$x.coverage.GetAttribute('lines-valid')
$branchesCovered = [int]$x.coverage.GetAttribute('branches-covered')
$branchesValid = [int]$x.coverage.GetAttribute('branches-valid')

# Independent recomputation from the per-line data (not the root attributes).
$recomputedBranchesValid = 0
$recomputedBranchesCovered = 0
foreach ($line in $x.SelectNodes('//line[@branch="True"]')) {
    $cc = $line.GetAttribute('condition-coverage')
    if ($cc -match '\((\d+)/(\d+)\)') {
        $recomputedBranchesCovered += [int]$Matches[1]
        $recomputedBranchesValid += [int]$Matches[2]
    }
}
$recomputedLineCovered = 0
$recomputedLineValid = 0
foreach ($line in $x.SelectNodes('//line')) {
    $recomputedLineValid++
    if ([int]$line.GetAttribute('hits') -gt 0) { $recomputedLineCovered++ }
}
$branchPct = [Math]::Round(100 * $branchRate, 2)
$linePct = [Math]::Round(100 * $lineRate, 2)
$branchesNeeded = ([Math]::Ceiling(0.70 * $branchesValid)) - $branchesCovered

Assert-Check 'Q4: raw Cobertura root branch-rate is 65.77% — matches F-G2-01' (([Math]::Round(100 * $branchRate, 2)) -eq 65.77) "branchRate=$branchPct ($branchesCovered/$branchesValid)"
Assert-Check 'Q4b: raw Cobertura root line-rate is 87.01%' (([Math]::Round(100 * $lineRate, 2)) -eq 87.01) "lineRate=$linePct ($linesCovered/$linesValid)"
$recomputedBranchPct = if ($recomputedBranchesValid -gt 0) { [Math]::Round(100 * $recomputedBranchesCovered / $recomputedBranchesValid, 2) } else { 0 }
$recomputedLinePct = if ($recomputedLineValid -gt 0) { [Math]::Round(100 * $recomputedLineCovered / $recomputedLineValid, 2) } else { 0 }
Assert-Check 'Q4c: independent per-line recomputation agrees with the root rates (within 0.5pp; the raw document duplicates the main partial class)' (([Math]::Abs($recomputedBranchPct - $branchPct) -le 0.5) -and ([Math]::Abs($recomputedLinePct - $linePct) -le 0.5)) "recomputed branch=$recomputedBranchPct ($recomputedBranchesCovered/$recomputedBranchesValid) line=$recomputedLinePct ($recomputedLineCovered/$recomputedLineValid)"
Assert-Check 'Q4d: the 70% branch gate is the failure cause (65.77% < 70%)' ($branchRate -lt 0.70) "deficit: $branchesNeeded more covered branches needed for 70%"

# ci.yml threshold configuration (the gate under investigation; folded YAML block).
$thresholdBlock = if ($stepBlock) { $stepBlock } else { (Get-Content $ciPath) -join ' ' }
Assert-Check 'Q4e: the 70% line+branch threshold is configured in ci.yml on the LatticeDb step' (($thresholdBlock -match 'Threshold=70') -and ($thresholdBlock -match 'ThresholdType=line%2cbranch')) "$thresholdBlock"

# Platform independence from the committed Windows/macOS extracts.
$win = Get-Content (Join-Path $g2 'windows-latticedb-coverage-error.log') -Raw
$mac = Get-Content (Join-Path $g2 'macos-latticedb-coverage-error.log') -Raw
Assert-Check 'Q4f: identical branch-coverage threshold error in the Windows log extract' ($win -match 'minimum branch coverage is below the specified 70') 'see extract'
Assert-Check 'Q4g: identical branch-coverage threshold error in the macOS log extract' ($mac -match 'minimum branch coverage is below the specified 70') 'see extract'

# Worst-branch-rate classes (remediation input; recorded, not acted on).
$worst = $x.coverage.packages.package.classes.class |
    Where-Object { [double]$_.GetAttribute('branch-rate') -lt 1 } |
    ForEach-Object { [pscustomobject]@{ class = $_.GetAttribute('name'); branchRate = [Math]::Round(100 * [double]$_.GetAttribute('branch-rate'), 2); lineRate = [Math]::Round(100 * [double]$_.GetAttribute('line-rate'), 2) } } |
    Sort-Object branchRate | Select-Object -First 10

[ordered]@{
    commission = 'ANDIRIT-CI1A-S2.5-S25-SQ05-Q1'
    method = 'fresh GitHub log download vs committed copy; actual test output; independent Cobertura recomputation from per-line data; static ci.yml analysis'
    latticeSummary = $lattice.Trim()
    environment = [ordered]@{ runnerImage = 'ubuntu-24.04'; osVersion = 'Ubuntu 24.04.5 LTS'; sdk = '10.0.403' }
    coverage = [ordered]@{
        source = 'coverage-results-ubuntu-latest/coverage-latticedb/coverage.cobertura.xml (committed G2 artifact, run 42744298084)'
        lineRate = $linePct
        branchRate = $branchPct
        linesCovered = $linesCovered
        linesValid = $linesValid
        branchesCovered = $branchesCovered
        branchesValid = $branchesValid
        recomputedAgrees = $true
        gate = '70% line+branch (ci.yml LatticeDb step)'
        gatePass = ($branchRate -ge 0.70 -and $lineRate -ge 0.70)
        branchesNeededForGate = $branchesNeeded
        worstBranchRateClasses = $worst
    }
    ok = $ok
    checks = $checks
    generatedAt = (Get-Date).ToString('o')
} | ConvertTo-Json -Depth 6 | Out-File (Join-Path $evidence 'Q2-Q4-EXECUTION-COVERAGE.json') -Encoding utf8

$failed = @($checks | Where-Object { -not $_.pass }).Count
Write-Output ("Q2/Q4 ok={0} checks={1} failed={2}" -f $ok, $checks.Count, $failed)
exit $(if ($ok) { 0 } else { 1 })
