# S23-LIFE-Q1 independent reviewer probe orchestration.
# Every phase that matters for a fresh-process boundary is a separate OS process
# (a fresh `dotnet run` invocation). Probe databases are working artifacts under
# evidence/probe-databases and are not committed.
param(
    [string] $Head = ""
)
$ErrorActionPreference = "Continue"
$root = $PSScriptRoot
$evidence = Join-Path $root "..\CI1A-S2.3-S23-LIFE-Q1-evidence"
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
    & dotnet run --project (Join-Path $root "S23LifeQ1Probe.csproj") -c Release --no-restore `
        -- $verb $db $json $detail 2>&1 | Out-File $log -Encoding utf8
    $exit = $LASTEXITCODE
    $result = Get-Content $json -Raw | ConvertFrom-Json
    $notes = if ($result.notes) { ($result.notes -join " | ") } else { "" }
    [pscustomobject]@{
        Phase = $out; Verb = $verb; Exit = $exit; Ok = $result.ok; Pid = $result.processId
        Database = (Split-Path $db -Leaf); Detail = $detail; Notes = $notes
    }
}

$rows = @()

# --- 1. Real cross-project publication and fresh-process durable reopen.
$db = Join-Path $work "plugin.ltdb"; Clear-Db $db
$rows += Run-Phase "plugin-publish" $db "P01-plugin-publish.json"
$rows += Run-Phase "plugin-verify" $db "P02-plugin-fresh-verify.json"

# --- 2. Negative authority control (no current authority -> unavailable).
$db = Join-Path $work "negative.ltdb"; Clear-Db $db
$rows += Run-Phase "plugin-negative" $db "P03-plugin-negative.json"

# --- 3. Stale-defining-context refusal with a stable target identity (real facts).
$db = Join-Path $work "authority.ltdb"; Clear-Db $db
$rows += Run-Phase "authority-reopen" $db "P04-authority-reopen.json"

# --- 4. Real-fact lifecycle: publish -> reopen -> replace -> delete.
$db = Join-Path $work "lifecycle.ltdb"; Clear-Db $db
$rows += Run-Phase "lifecycle" $db "P05-lifecycle.json"

# --- 5. SQ-03 native schedule and concurrency.
$db = Join-Path $work "sq03-schedule.ltdb"; Clear-Db $db
$rows += Run-Phase "sq03-schedule" $db "P06-sq03-schedule.json"
$db = Join-Path $work "sq03-concurrent.ltdb"; Clear-Db $db
$rows += Run-Phase "sq03-concurrent" $db "P07-sq03-concurrent.json"

# --- 6. Environment receipt.
$db = Join-Path $work "environment.ltdb"; Clear-Db $db
$rows += Run-Phase "environment" $db "P08-environment.json" $Head

$rows | Format-Table Phase, Verb, Exit, Ok, Pid -AutoSize | Out-String -Width 200 |
    Out-File (Join-Path $evidence "probe-matrix.txt") -Encoding utf8
$rows | ConvertTo-Json -Depth 4 | Out-File (Join-Path $evidence "probe-matrix.json") -Encoding utf8
$failed = @($rows | Where-Object { -not $_.Ok -or $_.Exit -ne 0 })
"TOTAL=" + $rows.Count + " FAILED=" + $failed.Count
if ($failed.Count -gt 0) { $failed | ForEach-Object { "FAILED " + $_.Phase + ": " + $_.Notes } }
