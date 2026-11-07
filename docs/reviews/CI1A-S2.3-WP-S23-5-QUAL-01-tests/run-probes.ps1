# S23-WP-S23-5-QUAL-01 integrated-qualification probe orchestration.
# Fresh-process boundaries are real: each critical phase is its own `dotnet run`.
param(
    [string] $Head = ""
)
$ErrorActionPreference = "Continue"
$root = $PSScriptRoot
$evidence = Join-Path $root "..\CI1A-S2.3-WP-S23-5-QUAL-01-evidence"
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
    & dotnet run --project (Join-Path $root "S23Wp5QualProbe.csproj") -c Release --no-restore `
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

# I-01: real multi-project fixture, fresh-process publish + reopen.
$db = Join-Path $work "i01.ltdb"; Clear-Db $db
$rows += Run-Phase "publish" $db "P01-i01-publish.json"
$rows += Run-Phase "verify" $db "P02-i01-verify-fresh.json"

# I-02: recovery x authority x carrier (fault process + fresh reopen process).
$db = Join-Path $work "i02.ltdb"; Clear-Db $db
$rows += Run-Phase "recovery-fault" $db "P03-i02-recovery-fault.json"
$rows += Run-Phase "recovery-verify" $db "P04-i02-recovery-verify-fresh.json"

# I-04 / SQ-06: isolated namespace mismatch (tamper process + fresh open process).
$db = Join-Path $work "i04.ltdb"; Clear-Db $db
$rows += Run-Phase "namespace-corrupt" $db "P05-i04-namespace-corrupt.json"
$rows += Run-Phase "namespace-open" $db "P06-i04-namespace-open.json"

# I-03 / I-07: real source change, stale refusal, deletion, reopen.
$db = Join-Path $work "i07.ltdb"; Clear-Db $db
$rows += Run-Phase "lifecycle" $db "P07-i03-i07-lifecycle.json"

# Environment receipt.
$db = Join-Path $work "env.ltdb"; Clear-Db $db
$rows += Run-Phase "environment" $db "P08-environment.json" $Head

$rows | Format-Table Phase, Verb, Exit, Ok, Pid -AutoSize | Out-String -Width 200 |
    Out-File (Join-Path $evidence "probe-matrix.txt") -Encoding utf8
$rows | ConvertTo-Json -Depth 4 | Out-File (Join-Path $evidence "probe-matrix.json") -Encoding utf8
$failed = @($rows | Where-Object { -not $_.Ok -or $_.Exit -ne 0 })
"TOTAL=" + $rows.Count + " FAILED=" + $failed.Count
if ($failed.Count -gt 0) { $failed | ForEach-Object { "FAILED " + $_.Phase + ": " + $_.Notes } }
