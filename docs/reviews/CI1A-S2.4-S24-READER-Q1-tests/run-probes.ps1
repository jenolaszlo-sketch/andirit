# S24-READER-Q1 independent qualification probe orchestration.
# Reviewer-owned; each phase is its own OS process.
param([string] $Head = "")
$ErrorActionPreference = "Continue"
$root = $PSScriptRoot
$evidence = Join-Path $root "..\CI1A-S2.4-S24-READER-Q1-evidence"
New-Item -ItemType Directory -Force -Path $evidence | Out-Null
$work = Join-Path $evidence "probe-databases"
New-Item -ItemType Directory -Force -Path $work | Out-Null

function Clear-Db($db) {
    foreach ($suffix in @("", "-wal", "-journal", ".wal")) {
        if (Test-Path ($db + $suffix)) { Remove-Item -Force ($db + $suffix) }
    }
}
function Run-Phase($verb, $out, $detail = "") {
    $db = Join-Path $work ($out + ".ltdb"); Clear-Db $db
    $json = Join-Path $evidence $out
    $log = [System.IO.Path]::ChangeExtension($json, ".log")
    & dotnet run --project (Join-Path $root "S24ReaderQ1Probe.csproj") -c Release --no-restore `
        -- $verb $db $json $detail 2>&1 | Out-File $log -Encoding utf8
    $exit = $LASTEXITCODE
    $result = Get-Content $json -Raw | ConvertFrom-Json
    $notes = if ($result.notes) { ($result.notes -join " | ") } else { "" }
    [pscustomobject]@{ Phase = $out; Verb = $verb; Exit = $exit; Ok = $result.ok; Pid = $result.processId; Notes = $notes }
}

$rows = @()
$rows += Run-Phase "oracle" "P01-oracle.json"
$rows += Run-Phase "tokens" "P02-tokens.json"
$rows += Run-Phase "bounded" "P03-bounded.json"
$rows += Run-Phase "publication" "P04-publication.json"
$rows += Run-Phase "coverage" "P05-coverage.json"
$rows += Run-Phase "noninterference" "P06-noninterference.json"
$rows += Run-Phase "authority" "P07-authority.json"
$rows += Run-Phase "environment" "P08-environment.json" $Head

$rows | Format-Table Phase, Verb, Exit, Ok, Pid -AutoSize | Out-String -Width 200 |
    Out-File (Join-Path $evidence "probe-matrix.txt") -Encoding utf8
$rows | ConvertTo-Json -Depth 4 | Out-File (Join-Path $evidence "probe-matrix.json") -Encoding utf8
$failed = @($rows | Where-Object { -not $_.Ok -or $_.Exit -ne 0 })
"TOTAL=" + $rows.Count + " FAILED=" + $failed.Count
if ($failed.Count -gt 0) { $failed | ForEach-Object { "FAILED " + $_.Phase + ": " + $_.Notes } }
