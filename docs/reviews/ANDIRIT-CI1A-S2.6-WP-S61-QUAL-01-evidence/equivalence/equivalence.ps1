param(
    [Parameter(Mandatory = $true)][string]$OutputDir
)

$ErrorActionPreference = "Stop"
$reviewer = "D:\m54b-stage6\qual-s61"
$base = "D:\m54b-stage6\qual-base"
$impl = "D:\m54b-stage6\qual-impl"
$out = $OutputDir
New-Item -ItemType Directory -Force -Path $out | Out-Null

$assemblies = @(
    @{ Proj = "src/Andirit.Abstractions/Andirit.Abstractions.csproj"; Dll = "Andirit.Abstractions.dll" },
    @{ Proj = "src/Andirit/Andirit.csproj";                           Dll = "Andirit.dll" },
    @{ Proj = "src/Andirit.LatticeDb/Andirit.LatticeDb.csproj";       Dll = "Andirit.LatticeDb.dll" }
)

foreach ($root in @($base, $impl)) {
    Write-Host "=== building $root ==="
    dotnet build (Join-Path $root "Andirit.slnx") -c Release --nologo 2>&1 |
        Select-String -Pattern "error|Warning\(s\)|Error\(s\)" | Select-Object -First 6
    if ($LASTEXITCODE -ne 0) { throw "build failed for $root" }
}

# reviewer-owned dump tool
Push-Location (Join-Path $reviewer "revdump")
dotnet build revdump.csproj -c Release -v minimal 2>&1 | Select-String -Pattern "error|Build succeeded" | Select-Object -First 4
if ($LASTEXITCODE -ne 0) { throw "revdump build failed" }
Pop-Location
$revdump = Join-Path $reviewer "revdump\bin\Release\net10.0\revdump.exe"

$report = New-Object System.Collections.Generic.List[string]
$report.Add("ANDIRIT-CI1A-S2.6-WP-S61-QUAL-01 - reviewer-owned production equivalence")
$report.Add("baseline:      $base  (2d1e8ec88e397d0c36b7f258eb1a1c0840e56157)")
$report.Add("implementation: $impl  (01624943233f16447c3176b7c905c544642dfb2c)")
$report.Add("method: independent rebuild of both revisions, then a reviewer-written")
$report.Add("        System.Reflection.Metadata dump of every type, member, signature blob")
$report.Add("        and IL byte sequence. MVID and the PE debug directory are excluded")
$report.Add("        (a deterministic build derives them from the source hash).")
$report.Add("")

$failures = 0
foreach ($a in $assemblies) {
    $rel = (Split-Path $a.Proj -Parent) + "\bin\Release\net10.0\" + $a.Dll
    $b = Join-Path $base $rel
    $i = Join-Path $impl $rel
    $bd = Join-Path $out ("base-" + $a.Dll + ".dump")
    $id = Join-Path $out ("impl-" + $a.Dll + ".dump")
    & $revdump $b $bd 2>&1 | Out-Null
    & $revdump $i $id 2>&1 | Out-Null
    $bHash = (Get-FileHash $bd -Algorithm SHA256).Hash
    $iHash = (Get-FileHash $id -Algorithm SHA256).Hash
    $bLines = (Get-Content $bd).Count
    $iLines = (Get-Content $id).Count
    $equal = $bHash -eq $iHash
    if (-not $equal) { $failures++ }
    $report.Add("ASSEMBLY $($a.Dll)")
    $report.Add("  baseline       : $bLines lines  sha256=$bHash")
    $report.Add("  implementation : $iLines lines  sha256=$iHash")
    $report.Add("  compiled-program identical: $equal")
    $report.Add("")
    Write-Host ("{0}: identical={1}" -f $a.Dll, $equal)

    # public API surface
    $ba = Join-Path $out ("base-api-" + $a.Dll + ".dump")
    $ia = Join-Path $out ("impl-api-" + $a.Dll + ".dump")
    & $revdump $b $ba --api 2>&1 | Out-Null
    & $revdump $i $ia --api 2>&1 | Out-Null
    $diff = Compare-Object (Get-Content $ba) (Get-Content $ia)
    $apiEqual = ($null -eq $diff)
    if (-not $apiEqual) { $failures++ }
    $report.Add("  public API surface identical: $apiEqual  (diff lines: $(@($diff).Count))")
    if (-not $apiEqual) { $diff | Select-Object -First 20 | ForEach-Object { $report.Add("    " + $_.ToString()) } }
    $report.Add("")
}

$report.Add("RESULT: 3 production assemblies compared; $failures differing.")
$report | Out-File -FilePath (Join-Path $out "equivalence.txt") -Encoding utf8
Write-Host "failures=$failures -> $(Join-Path $out 'equivalence.txt')"
exit $failures
