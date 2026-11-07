# S23-PUB-Q1 independent reviewer probe orchestration.
# Every phase is a separate OS process (fresh dotnet run invocation).
param(
    [string] $Head = ""
)
$ErrorActionPreference = "Continue"
$root = $PSScriptRoot
$evidence = Join-Path $root "..\CI1A-S2.3-S23-PUB-Q1-evidence"
New-Item -ItemType Directory -Force -Path $evidence | Out-Null
$work = Join-Path $evidence "probe-databases"
New-Item -ItemType Directory -Force -Path $work | Out-Null

function Clear-Db($db) {
    foreach ($suffix in @("", "-wal", "-journal", ".wal")) {
        if (Test-Path ($db + $suffix)) { Remove-Item -Force ($db + $suffix) }
    }
}

function Run-Phase($verb, $db, $out, $detail = "") {
    $json = Join-Path $evidence $out
    $log = [System.IO.Path]::ChangeExtension($json, ".log")
    & dotnet run --project (Join-Path $root "S23PubQ1Probe.csproj") -c Release --no-restore `
        -- $verb $db $json $detail 2>&1 | Out-File $log -Encoding utf8
    $exit = $LASTEXITCODE
    $result = Get-Content $json -Raw | ConvertFrom-Json
    [pscustomobject]@{
        Phase = $out; Verb = $verb; Exit = $exit; Ok = $result.ok; Pid = $result.processId
        Database = (Split-Path $db -Leaf); Detail = $detail
        Notes = ($result.notes -join " | ")
    }
}

$rows = @()

# --- 1. Positive control: real cross-project publication, fresh-process reopen.
$db = Join-Path $work "positive.ltdb"; Clear-Db $db
$rows += Run-Phase "publish" $db "P01-publish.json"
$rows += Run-Phase "verify" $db "P02-verify-fresh.json"

# --- 2. Graph-only controls (both persisted orders), fresh-process reopen.
$db = Join-Path $work "graphonly-ref-first.ltdb"; Clear-Db $db
$rows += Run-Phase "graphonly-publish" $db "P03-graphonly-ref-first-publish.json" "ref-first"
$rows += Run-Phase "graphonly-verify" $db "P04-graphonly-ref-first-verify.json"
$db = Join-Path $work "graphonly-refd-first.ltdb"; Clear-Db $db
$rows += Run-Phase "graphonly-publish" $db "P05-graphonly-refd-first-publish.json" "refd-first"
$rows += Run-Phase "graphonly-verify" $db "P06-graphonly-refd-first-verify.json"

# --- 3. Corruption controls (each on its own freshly published durable file).
foreach ($mode in @("missing-endpoint", "inconsistent-edge", "malformed-row")) {
    $db = Join-Path $work ("corrupt-" + $mode + ".ltdb"); Clear-Db $db
    $rows += Run-Phase "publish" $db ("P07-publish-" + $mode + ".json") $mode
    $rows += Run-Phase "corrupt" $db ("P08-corrupt-" + $mode + ".json") $mode
    $rows += Run-Phase "corrupt-verify" $db ("P09-refuse-" + $mode + ".json") $mode
}

# --- 4. Q1-Q1 segment resumption (fail-closed and verified re-staging branches),
#        for both persisted stage-key orders (unit ids are sha256(project path)).
$db = Join-Path $work "segment-failclosed.ltdb"; Clear-Db $db
$rows += Run-Phase "segment-stage" $db "P10-segment-stage.json" "referencing-first"
$rows += Run-Phase "segment-resume-complete" $db "P11-segment-resume-failclosed.json" "referencing-first"
$db = Join-Path $work "segment-restage.ltdb"; Clear-Db $db
$rows += Run-Phase "segment-stage" $db "P12-segment-stage-restage.json" "referencing-first"
$rows += Run-Phase "segment-resume-restage" $db "P13-segment-resume-restaged.json" "referencing-first"
$db = Join-Path $work "segment-defining-first.ltdb"; Clear-Db $db
$rows += Run-Phase "segment-stage" $db "P17-segment-stage-defining-first.json" "defining-first"
$rows += Run-Phase "segment-resume-complete" $db "P18-segment-resume-defining-first.json" "defining-first"

# --- 5. Negative authority controls.
$db = Join-Path $work "negative-authority.ltdb"; Clear-Db $db
$rows += Run-Phase "negative-authority" $db "P14-negative-authority.json"

# --- 6. Stale controls after fresh-process reopen.
$db = Join-Path $work "stale-reopen.ltdb"; Clear-Db $db
$rows += Run-Phase "stale-after-reopen" $db "P15-stale-after-reopen.json"

# --- 7. Replay-level ordering / mixed-repository / repeated-replacement.
$rows += Run-Phase "replay-probe" (Join-Path $work "unused.ltdb") "P16-replay-probe.json"

$rows | Format-Table Phase, Verb, Exit, Ok, Pid -AutoSize | Out-String -Width 200 |
    Out-File (Join-Path $evidence "probe-matrix.txt") -Encoding utf8
$rows | ConvertTo-Json -Depth 4 | Out-File (Join-Path $evidence "probe-matrix.json") -Encoding utf8
$failed = @($rows | Where-Object { -not $_.Ok -or $_.Exit -ne 0 })
"TOTAL=" + $rows.Count + " FAILED=" + $failed.Count
if ($failed.Count -gt 0) { $failed | ForEach-Object { "FAILED " + $_.Phase + ": " + $_.Notes } }
