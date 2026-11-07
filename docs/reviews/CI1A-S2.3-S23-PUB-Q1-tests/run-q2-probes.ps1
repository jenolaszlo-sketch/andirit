# S23-PUB-Q2 reviewer probe orchestration: independent fresh-process rerun of the
# committed probe harness (one OS process per phase, fresh databases, Q2-prefixed
# outputs). The Q1/R2 evidence is never modified.
param(
    [string] $Prefix = "Q2"
)
$ErrorActionPreference = "Continue"
$root = $PSScriptRoot
$harness = Join-Path $root "..\CI1A-S2.3-S23-PUB-Q1-tests\S23PubQ1Probe.csproj"
$evidence = Join-Path $root "..\CI1A-S2.3-WP-S23-3A-S23-PUB-Q2-evidence"
New-Item -ItemType Directory -Force -Path $evidence | Out-Null
$work = Join-Path $evidence "probe-databases"
New-Item -ItemType Directory -Force -Path $work | Out-Null

function Clear-Db($db) {
    foreach ($suffix in @("", "-wal", "-journal", ".wal")) {
        if (Test-Path ($db + $suffix)) { Remove-Item -Force ($db + $suffix) }
    }
}

function Hash-Db($db) {
    if (Test-Path $db) { (Get-FileHash $db -Algorithm SHA256).Hash.Substring(0, 16) } else { "absent" }
}

function Run-Phase($verb, $db, $out, $detail = "") {
    $json = Join-Path $evidence $out
    $log = [System.IO.Path]::ChangeExtension($json, ".log")
    & dotnet run --project $harness -c Release --no-restore -- $verb $db $json $detail 2>&1 |
        Out-File $log -Encoding utf8
    $exit = $LASTEXITCODE
    $result = if (Test-Path $json) { Get-Content $json -Raw | ConvertFrom-Json } else { $null }
    [pscustomobject]@{
        Phase = $out; Verb = $verb; Exit = $exit
        Ok = if ($result) { $result.ok } else { $false }
        Pid = if ($result) { $result.processId } else { 0 }
        DbHash = Hash-Db $db; Detail = $detail
        Notes = if ($result) { ($result.notes -join " | ") } else { "(no result)" }
    }
}

$rows = @()
$db = Join-Path $work "positive.ltdb"; Clear-Db $db
$rows += Run-Phase "publish" $db "$Prefix-P01-publish.json"
$rows += Run-Phase "verify" $db "$Prefix-P02-verify-fresh.json"

foreach ($mode in @("missing-endpoint", "inconsistent-edge", "malformed-row")) {
    $db = Join-Path $work ("corrupt-" + $mode + ".ltdb"); Clear-Db $db
    $rows += Run-Phase "publish" $db ("$Prefix-P07-publish-" + $mode + ".json") $mode
    $rows += Run-Phase "corrupt" $db ("$Prefix-P08-corrupt-" + $mode + ".json") $mode
    $rows += Run-Phase "corrupt-verify" $db ("$Prefix-P09-refuse-" + $mode + ".json") $mode
}

$rows += Run-Phase "graphonly-publish" (Join-Path $work "graphonly-ref-first.ltdb") "$Prefix-P03-graphonly-ref-first-publish.json" "ref-first"
$rows += Run-Phase "graphonly-verify" (Join-Path $work "graphonly-ref-first.ltdb") "$Prefix-P04-graphonly-ref-first-verify.json"
$rows += Run-Phase "graphonly-publish" (Join-Path $work "graphonly-refd-first.ltdb") "$Prefix-P05-graphonly-refd-first-publish.json" "refd-first"
$rows += Run-Phase "graphonly-verify" (Join-Path $work "graphonly-refd-first.ltdb") "$Prefix-P06-graphonly-refd-first-verify.json"

# The F-Q1-01 scenarios, both stage-key orders.
$db = Join-Path $work "segment-failclosed.ltdb"; Clear-Db $db
$rows += Run-Phase "segment-stage" $db "$Prefix-P10-segment-stage.json" "referencing-first"
$rows += Run-Phase "segment-resume-complete" $db "$Prefix-P11-segment-resume-failclosed.json" "referencing-first"
$db = Join-Path $work "segment-restage.ltdb"; Clear-Db $db
$rows += Run-Phase "segment-stage" $db "$Prefix-P12-segment-stage-restage.json" "referencing-first"
$rows += Run-Phase "segment-resume-restage" $db "$Prefix-P13-segment-resume-restaged.json" "referencing-first"
$db = Join-Path $work "segment-defining-first.ltdb"; Clear-Db $db
$rows += Run-Phase "segment-stage" $db "$Prefix-P17-segment-stage-defining-first.json" "defining-first"
$rows += Run-Phase "segment-resume-complete" $db "$Prefix-P18-segment-resume-defining-first.json" "defining-first"

$rows += Run-Phase "negative-authority" (Join-Path $work "negative-authority.ltdb") "$Prefix-P14-negative-authority.json"
$rows += Run-Phase "stale-after-reopen" (Join-Path $work "stale-reopen.ltdb") "$Prefix-P15-stale-after-reopen.json"
$rows += Run-Phase "replay-probe" (Join-Path $work "unused.ltdb") "$Prefix-P16-replay-probe.json"

$rows | Format-Table Phase, Verb, Exit, Ok, Pid, DbHash -AutoSize | Out-String -Width 200 |
    Out-File (Join-Path $evidence "$Prefix-probe-matrix.txt") -Encoding utf8
$rows | ConvertTo-Json -Depth 4 | Out-File (Join-Path $evidence "$Prefix-probe-matrix.json") -Encoding utf8
$failed = @($rows | Where-Object { -not $_.Ok -or $_.Exit -ne 0 })
"TOTAL=" + $rows.Count + " FAILED=" + $failed.Count
if ($failed.Count -gt 0) { $failed | ForEach-Object { "FAILED " + $_.Phase + ": " + $_.Notes } }
