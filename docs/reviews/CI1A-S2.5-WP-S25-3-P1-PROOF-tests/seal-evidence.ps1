# ANDIRIT-CI1A-S2.5-WP-S25-3-P1-PROOF-01 — self-excluding evidence manifest seal.
# SHA-256 over the exact bytes of every committed evidence file (sorted relative paths;
# manifest itself excluded). This seal belongs to the P1 package only — never run another
# commission's seal script.
[CmdletBinding()]
param(
    [string]$RepoRoot
)

$ErrorActionPreference = 'Stop'
if (-not $RepoRoot) { $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../../..")).Path }
$evidence = Join-Path $RepoRoot 'docs/reviews/CI1A-S2.5-WP-S25-3-P1-PROOF-evidence'
$manifestPath = Join-Path $evidence 'manifest.json'

$files = Get-ChildItem $evidence -Recurse -File -Force |
    Where-Object { $_.FullName -ne $manifestPath -and $_.FullName -notmatch '\\(staging|work)\\' } |
    ForEach-Object {
        [pscustomobject]@{
            path   = $_.FullName.Substring($evidence.Length + 1).Replace('\', '/')
            bytes  = $_.Length
            sha256 = (Get-FileHash -Algorithm SHA256 $_.FullName).Hash
        }
    } | Sort-Object path

$head = (& git -C $RepoRoot rev-parse HEAD).Trim()
$tree = (& git -C $RepoRoot rev-parse "HEAD^{tree}").Trim()
$branch = (& git -C $RepoRoot rev-parse --abbrev-ref HEAD).Trim()

[ordered]@{
    commission   = 'ANDIRIT-CI1A-S2.5-WP-S25-3-P1-PROOF-01'
    scheme       = 'SHA-256 over exact bytes; sorted relative paths; self-excluded'
    reviewedHead = $head
    reviewedTree = $tree
    branch       = $branch
    contract     = 'docs/planning/adr-ci1-01-ci1a-s2-5/s01-canonical-form-contract.md (canonical-forms v1)'
    files        = $files
} | ConvertTo-Json -Depth 5 | Out-File $manifestPath -Encoding utf8

"reviewed_head $head`nreviewed_tree $tree`nbranch $branch" | Out-File (Join-Path $evidence 'reviewed-head.txt') -Encoding utf8

$mismatched = @($files | Where-Object { (Get-FileHash -Algorithm SHA256 (Join-Path $evidence $_.path)).Hash -ne $_.sha256 })
foreach ($m in $mismatched) { Write-Output ("MISMATCH: {0}" -f $m.path) }
Write-Output ("manifest entries={0} mismatches={1}" -f $files.Count, $mismatched.Count)
exit $(if ($mismatched.Count -eq 0) { 0 } else { 1 })
