# S23-LIFE-Q1 independent evidence integrity manifest.
# SHA-256 over the exact bytes of every committed evidence file, sorted by
# relative path, self-excluded. Probe databases (working artifacts) and the
# manifest itself are not listed.
param(
    [string] $Head = "",
    [string] $Tree = ""
)
$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$evidence = (Resolve-Path (Join-Path $root "..\CI1A-S2.3-S23-LIFE-Q1-evidence")).Path
$manifestPath = Join-Path $evidence "manifest.json"

$files = Get-ChildItem -Path $evidence -Recurse -File |
    Where-Object {
        $_.FullName -ne $manifestPath -and
        ($_.FullName -notmatch '\\probe-databases\\')
    } |
    ForEach-Object {
        $rel = $_.FullName.Substring($evidence.Length + 1).Replace('\', '/')
        $hash = (Get-FileHash -Algorithm SHA256 -Path $_.FullName).Hash
        [pscustomobject]@{ path = $rel; bytes = $_.Length; sha256 = $hash }
    } |
    Sort-Object path

$manifest = [ordered]@{
    commission  = "ANDIRIT-CI1A-S2.3-WP-S23-4-S23-LIFE-Q1"
    scheme      = "SHA-256 over exact bytes; sorted relative paths; self-excluded; report/source bound by Git"
    reviewedHead = $Head
    reviewedTree = $Tree
    files       = $files
}
$manifest | ConvertTo-Json -Depth 5 | Out-File -FilePath $manifestPath -Encoding utf8
"MANIFEST files=" + $files.Count + " -> " + $manifestPath
