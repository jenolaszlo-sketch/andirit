# S24-READER-Q2 independent requalification probe orchestration.
# Fresh reviewer (Q2) probes plus fresh-process re-execution of the committed Q1
# reviewer probes (reuse marked explicitly). Each phase is its own OS process.
param([string] $Head = "")
$ErrorActionPreference = "Continue"
$root = $PSScriptRoot
$q1 = Join-Path $root "..\CI1A-S2.4-S24-READER-Q1-tests"
$evidence = Join-Path $root "..\CI1A-S2.4-S24-READER-Q2-evidence"
New-Item -ItemType Directory -Force -Path $evidence | Out-Null
$work = Join-Path $evidence "probe-databases"
New-Item -ItemType Directory -Force -Path $work | Out-Null

function Clear-Db($db) {
    foreach ($suffix in @("", "-wal", "-journal", ".wal")) {
        if (Test-Path ($db + $suffix)) { Remove-Item -Force ($db + $suffix) }
    }
}
function Run-Phase($project, $verb, $out, $detail = "") {
    $db = Join-Path $work ($out + ".ltdb"); Clear-Db $db
    $json = Join-Path $evidence $out
    $log = [System.IO.Path]::ChangeExtension($json, ".log")
    & dotnet run --project $project -c Release --no-restore -- $verb $db $json $detail 2>&1 | Out-File $log -Encoding utf8
    $exit = $LASTEXITCODE
    $result = Get-Content $json -Raw | ConvertFrom-Json
    [pscustomobject]@{ Phase = $out; Verb = $verb; Exit = $exit; Ok = $result.ok; Pid = $result.processId
        Notes = (($result.notes) -join " | ") }
}

$q2 = Join-Path $root "S24ReaderQ2Probe.csproj"
$rows = @()
$rows += Run-Phase $q2 "malformed" "Q2-P01-malformed.json"
$rows += Run-Phase $q2 "canonical" "Q2-P02-canonical.json"
$rows += Run-Phase $q2 "precedence" "Q2-P03-precedence.json"
$rows += Run-Phase $q2 "pagination" "Q2-P04-pagination.json"
$rows += Run-Phase $q2 "bounded" "Q2-P05-bounded.json"
$rows += Run-Phase $q2 "noninterference" "Q2-P06-noninterference.json"
$rows += Run-Phase $q2 "environment" "Q2-P07-environment.json" $Head

# Fresh-process re-execution of the committed Q1 reviewer probes (reused tooling).
$q1proj = Join-Path $q1 "S24ReaderQ1Probe.csproj"
foreach ($verb in @("oracle", "tokens", "publication", "coverage", "noninterference", "authority", "bounded")) {
    $rows += Run-Phase $q1proj $verb ("Q2-reuse-" + $verb + ".json")
}

$rows | Format-Table Phase, Verb, Exit, Ok, Pid -AutoSize | Out-String -Width 200 |
    Out-File (Join-Path $evidence "probe-matrix.txt") -Encoding utf8
$rows | ConvertTo-Json -Depth 4 | Out-File (Join-Path $evidence "probe-matrix.json") -Encoding utf8
$failed = @($rows | Where-Object { -not $_.Ok -or $_.Exit -ne 0 })
"TOTAL=" + $rows.Count + " FAILED=" + $failed.Count
if ($failed.Count -gt 0) { $failed | ForEach-Object { "FAILED " + $_.Phase + ": " + $_.Notes } }
