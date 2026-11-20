#!/usr/bin/env pwsh
# Independent exact-byte export of the pinned remediation revision.
# Method established by ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-04:
# plain `git archive` with conversion disabled, extracted with Python tarfile.
param(
    [string]$Repo = "D:\projects\andirit",
    [string]$Revision = "eafd632b19703b3f93dfdf5a6eff5ae34d06a0a6",
    # C: system volume exhausted (0 GB free) mid-build; reviewer execution moved
    # to D: (425 GB free). Recorded deviation with reason in the report.
    [string]$Work = "D:\gate-qualification\ANDIRIT-CI1A-S2.5-WP-S25-4-GATE-QUALIFICATION-01"
)
$ErrorActionPreference = 'Stop'
$export = Join-Path $Work 'export'
$tar = Join-Path $Work 'export.tar'
New-Item -ItemType Directory -Force -Path (Join-Path $Work 'logs') | Out-Null
if (Test-Path -LiteralPath $export) { Remove-Item -LiteralPath $export -Recurse -Force }
New-Item -ItemType Directory -Force -Path $export | Out-Null
# Disable all Git normalization for the archive; -c core.autocrlf=false is the
# belt-and-braces value (git archive never smudges blob bytes).
git -c core.autocrlf=false -c core.eol=native archive --format=tar --output=$tar $Revision
# Raw tar identity is recorded before extraction.
$tarHash = (Get-FileHash -LiteralPath $tar -Algorithm SHA256).Hash
$tarBytes = (Get-Item -LiteralPath $tar).Length
"tar-sha256=$tarHash"          | Set-Content (Join-Path $Work 'logs\export-identity.txt')
"tar-bytes=$tarBytes"          | Add-Content (Join-Path $Work 'logs\export-identity.txt')
"revision=$Revision"           | Add-Content (Join-Path $Work 'logs\export-identity.txt')
# Byte-exact extraction; no newline conversion, no filter normalization.
python -c "import tarfile,sys; tarfile.open(sys.argv[1]).extractall(sys.argv[2], filter='data')" $tar $export
# Verify every exported file against the pinned Git blobs (QUAL-04 independent verifier).
python (Join-Path $Work 'scripts\verify-exports.py') $Repo $Revision $export
"export verifier exit code: $LASTEXITCODE" | Add-Content (Join-Path $Work 'logs\export-identity.txt')