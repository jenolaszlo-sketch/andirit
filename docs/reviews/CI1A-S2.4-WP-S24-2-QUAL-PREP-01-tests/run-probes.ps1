# WP-S24-2 qualification-preparation probe orchestration (producer-side).
param([string] $Head = "")
$ErrorActionPreference = "Continue"
$root = $PSScriptRoot
$evidence = Join-Path $root "..\CI1A-S2.4-WP-S24-2-QUAL-PREP-01-evidence"
New-Item -ItemType Directory -Force -Path $evidence | Out-Null
$work = Join-Path $evidence "probe-databases"
New-Item -ItemType Directory -Force -Path $work | Out-Null

function Run-Phase($verb, $out, $detail = "") {
    $db = Join-Path $work ($out + ".ltdb")
    foreach ($suffix in @("", "-wal", "-journal", ".wal")) {
        if (Test-Path ($db + $suffix)) { Remove-Item -Force ($db + $suffix) }
    }
    $json = Join-Path $evidence $out
    $log = [System.IO.Path]::ChangeExtension($json, ".log")
    & dotnet run --project (Join-Path $root "S24SurfaceProbe.csproj") -c Release --no-restore `
        -- $verb $db $json $detail 2>&1 | Out-File $log -Encoding utf8
    $result = Get-Content $json -Raw | ConvertFrom-Json
    [pscustomobject]@{ Phase = $out; Verb = $verb; Exit = $LASTEXITCODE; Ok = $result.ok; Pid = $result.processId
        Notes = (($result.notes) -join " | ") }
}

$rows = @()
$rows += Run-Phase "scaling" "P01-scaling-100.json" "100"
$rows += Run-Phase "scaling" "P02-scaling-1000.json" "1000"
$rows += Run-Phase "scaling" "P03-scaling-10000.json" "10000"
$rows += Run-Phase "measure" "P04-measure-1000.json" "1000"
$rows += Run-Phase "environment" "P05-environment.json" $Head

$rows | Format-Table Phase, Verb, Exit, Ok, Pid -AutoSize | Out-String -Width 200 |
    Out-File (Join-Path $evidence "probe-matrix.txt") -Encoding utf8
$rows | ConvertTo-Json -Depth 4 | Out-File (Join-Path $evidence "probe-matrix.json") -Encoding utf8
$failed = @($rows | Where-Object { -not $_.Ok -or $_.Exit -ne 0 })
"TOTAL=" + $rows.Count + " FAILED=" + $failed.Count
if ($failed.Count -gt 0) { $failed | ForEach-Object { "FAILED " + $_.Phase + ": " + $_.Notes } }
