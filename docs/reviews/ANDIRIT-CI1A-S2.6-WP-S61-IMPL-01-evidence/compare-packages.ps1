param(
    [Parameter(Mandatory = $true)][string]$BaselineNupkg,
    [Parameter(Mandatory = $true)][string]$ImplementationNupkg,
    [Parameter(Mandatory = $true)][string]$Output
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.IO.Compression.FileSystem

function Get-Entries([string]$path) {
    $zip = [System.IO.Compression.ZipFile]::OpenRead($path)
    $map = @{}
    foreach ($entry in $zip.Entries) {
        $stream = $entry.Open()
        $ms = New-Object System.IO.MemoryStream
        $stream.CopyTo($ms)
        $bytes = $ms.ToArray()
        $sha = [System.Security.Cryptography.SHA256]::Create().ComputeHash($bytes)
        $map[$entry.FullName] = @{
            length = $bytes.Length
            sha256 = ([BitConverter]::ToString($sha)).Replace('-', '').ToLowerInvariant()
        }
        $stream.Dispose()
        $ms.Dispose()
    }
    $zip.Dispose()
    return $map
}

$baseline = Get-Entries $BaselineNupkg
$implementation = Get-Entries $ImplementationNupkg

$lines = @(
    "ANDIRIT-CI1A-S2.6-WP-S61-IMPL-01 - package content comparison",
    "baseline:      $BaselineNupkg",
    "implementation: $ImplementationNupkg",
    "",
    "scheme: exact entry list, exact uncompressed byte length and SHA-256 of every entry.",
    ""
)

$allNames = @($baseline.Keys) + @($implementation.Keys) | Sort-Object -Unique
foreach ($name in $allNames) {
    $b = $baseline[$name]
    $i = $implementation[$name]
    if ($null -eq $b) {
        $lines += "ADDED      $name (bytes=$($i.length) sha256=$($i.sha256))"
    }
    elseif ($null -eq $i) {
        $lines += "REMOVED    $name (bytes=$($b.length) sha256=$($b.sha256))"
    }
    elseif ($b.sha256 -eq $i.sha256) {
        $lines += "IDENTICAL  $name (bytes=$($b.length) sha256=$($b.sha256))"
    }
    else {
        $lines += "DIFFERENT  $name (baseline bytes=$($b.length) sha256=$($b.sha256); implementation bytes=$($i.length) sha256=$($i.sha256))"
    }
}

$added = @($allNames | Where-Object { $null -eq $baseline[$_] }).Count
$removed = @($allNames | Where-Object { $null -eq $implementation[$_] }).Count
$different = @($allNames | Where-Object { $null -ne $baseline[$_] -and $null -ne $implementation[$_] -and $baseline[$_].sha256 -ne $implementation[$_].sha256 }).Count
$identical = @($allNames | Where-Object { $null -ne $baseline[$_] -and $null -ne $implementation[$_] -and $baseline[$_].sha256 -eq $implementation[$_].sha256 }).Count

$lines += ""
$lines += "entries: $($allNames.Count) identical: $identical different: $different added: $added removed: $removed"
$lines += ""
$lines += "note: no XML documentation file is emitted (GenerateDocumentationFile is not enabled"
$lines += "      for these projects), so the cancellation contract is published in the"
$lines += "      source-level API documentation and not as a package artifact."

$lines | Out-File -FilePath $Output -Encoding utf8
Write-Host "entries=$($allNames.Count) identical=$identical different=$different added=$added removed=$removed -> $Output"
