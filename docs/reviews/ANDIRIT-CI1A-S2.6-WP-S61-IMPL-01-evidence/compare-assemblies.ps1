param(
    [Parameter(Mandatory = $true)][string]$BaselineRoot,
    [Parameter(Mandatory = $true)][string]$ImplementationRoot,
    [Parameter(Mandatory = $true)][string]$Output
)

$ErrorActionPreference = "Stop"
$exe = "D:\m54b-stage6\wp-s61\dllcompare\bin\Release\net10.0\dllcompare.exe"

$assemblies = @(
    'src/Andirit.Abstractions/bin/Release/net10.0/Andirit.Abstractions.dll',
    'src/Andirit/bin/Release/net10.0/Andirit.dll',
    'src/Andirit.LatticeDb/bin/Release/net10.0/Andirit.LatticeDb.dll'
)

$lines = @(
    'ANDIRIT-CI1A-S2.6-WP-S61-IMPL-01 - managed assembly semantic comparison (all production assemblies)',
    "baseline root:       $BaselineRoot",
    "implementation root: $ImplementationRoot",
    ""
)

$failed = 0
foreach ($assembly in $assemblies) {
    $baseline = Join-Path $BaselineRoot $assembly
    $implementation = Join-Path $ImplementationRoot $assembly
    $lines += "================================================================"
    $lines += "ASSEMBLY $assembly"
    $lines += "================================================================"
    $toolOutput = (& $exe $baseline $implementation 2>&1 | Out-String)
    if ($LASTEXITCODE -ne 0) { $failed++ }
    $lines += $toolOutput.TrimEnd()
    $lines += ""
}

$lines += "RESULT: $($assemblies.Count) production assemblies compared; $failed differing."

$lines | Out-File -FilePath $Output -Encoding utf8
Write-Host "assemblies=$($assemblies.Count) differing=$failed -> $Output"
exit $failed
