# WP-S24-2 evidence integrity manifest (S24-2-local; self-excluding SHA-256).
# Writes only into the S24-2 evidence directory; never touches historical evidence.
param([string] $Head = "", [string] $Tree = "")
$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$evidence = (Resolve-Path (Join-Path $root "..\CI1A-S2.4-WP-S24-2-QUAL-PREP-01-evidence")).Path
$manifestPath = Join-Path $evidence "manifest.json"
$files = Get-ChildItem -Path $evidence -Recurse -File |
    Where-Object { $_.FullName -ne $manifestPath -and ($_.FullName -notmatch '\\probe-databases\\') } |
    ForEach-Object {
        [pscustomobject]@{
            path = $_.FullName.Substring($evidence.Length + 1).Replace('\', '/');
            bytes = $_.Length;
            sha256 = (Get-FileHash -Algorithm SHA256 $_.FullName).Hash
        }
    } | Sort-Object path
$manifest = [ordered]@{
    commission = "ANDIRIT-CI1A-S2.4-WP-S24-2-QUAL-PREP-01"
    scheme = "SHA-256 over exact bytes; sorted relative paths; self-excluded"
    startingHead = $Head
    startingTree = $Tree
    files = $files
}
$manifest | ConvertTo-Json -Depth 5 | Out-File $manifestPath -Encoding utf8
"MANIFEST files=" + $files.Count + " -> " + $manifestPath
