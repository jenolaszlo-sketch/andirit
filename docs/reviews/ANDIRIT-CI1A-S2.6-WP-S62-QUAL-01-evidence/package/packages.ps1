param([Parameter(Mandatory = $true)][string]$OutputDir)

$ErrorActionPreference = "Stop"
$out = $OutputDir
New-Item -ItemType Directory -Force -Path $out | Out-Null

foreach ($pair in @(@("92bf73a61794fa3bccad53d23dd7e65c51807cd5", "base"), @("459b50c48f0ac2ef0a7bb4ca1f7d15c2a3d7e838", "impl"))) {
    $rev = $pair[0]; $tag = $pair[1]
    $dest = Join-Path $out ("pkg-" + $tag)
    New-Item -ItemType Directory -Force -Path $dest | Out-Null
    Write-Host "=== packing $tag ==="
    dotnet pack (Join-Path (Join-Path $rev "src\Andirit.LatticeDb\Andirit.LatticeDb.csproj")) -c Release --no-restore --nologo -o $dest 2>&1 |
        Select-String -Pattern "error|Successfully created package" | Select-Object -First 4
    if ($LASTEXITCODE -ne 0) { throw "pack failed for $tag" }
}

# reviewer-owned entry-by-entry comparison
$report = New-Object System.Collections.Generic.List[string]
$report.Add("ANDIRIT-CI1A-S2.6-WP-S62-QUAL-01 - reviewer-owned package content comparison")
$report.Add("subject: Andirit.LatticeDb.0.2.0-preview.7.nupkg rebuilt from both pinned revisions")
$report.Add("")

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

function Entries($zip) {
    $map = @{}
    foreach ($e in $zip.Entries) {
        if ($e.FullName.EndsWith("/")) { continue }
        $ms = New-Object System.IO.MemoryStream
        $s = $e.Open()
        $s.CopyTo($ms)
        $map[$e.FullName] = @{ Bytes = $ms.Length; Sha = [System.BitConverter]::ToString([System.Security.Cryptography.SHA256]::Create().ComputeHash($ms.ToArray())).Replace("-", "").ToLowerInvariant() }
        $s.Dispose(); $ms.Dispose()
    }
    return $map
}

$bp = Join-Path $out "pkg-base\Andirit.LatticeDb.0.2.0-preview.7.nupkg"
$ip = Join-Path $out "pkg-impl\Andirit.LatticeDb.0.2.0-preview.7.nupkg"
$bz = [System.IO.Compression.ZipFile]::OpenRead($bp)
$iz = [System.IO.Compression.ZipFile]::OpenRead($ip)
try {
    $b = Entries $bz
    $i = Entries $iz
    $all = @($b.Keys) + @($i.Keys) | Sort-Object -Unique
    $identical = 0; $different = 0; $added = 0; $removed = 0
    foreach ($k in $all) {
        if (-not $b.ContainsKey($k)) { $added++; $report.Add("ADDED      $k"); continue }
        if (-not $i.ContainsKey($k)) { $removed++; $report.Add("REMOVED    $k"); continue }
        if ($b[$k].Sha -eq $i[$k].Sha) { $identical++; $report.Add("IDENTICAL  $k  (bytes=$($b[$k].Bytes) sha256=$($b[$k].Sha))") }
        else { $different++; $report.Add("DIFFERENT  $k  (baseline bytes=$($b[$k].Bytes) sha256=$($b[$k].Sha); implementation bytes=$($i[$k].Bytes) sha256=$($i[$k].Sha))") }
    }
    $report.Add("")
    $report.Add("entries total: $($all.Count)  identical: $identical  different: $different  added: $added  removed: $removed")
}
finally { $bz.Dispose(); $iz.Dispose() }

$report | Out-File -FilePath (Join-Path $out "package-comparison.txt") -Encoding utf8
Write-Host "package comparison written"