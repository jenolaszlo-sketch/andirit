
$ErrorActionPreference='Stop'
$repo='D:\projects\Andirit'
$env:GIT_DIR="$repo/.git"; $env:GIT_WORK_TREE=$repo; $env:GIT_OPTIONAL_LOCKS='0'
$inventory=Get-Content -Raw -LiteralPath "$repo\docs\planning\adr-ci1-01-ci1a-s2-5\wp-s25-4-evidence-inventory.md"
$matches=[regex]::Matches($inventory,'\]\(\.\./\.\./reviews/(?<dir>[^/)]+)/manifest\.json\) \| `(?<head>[a-f0-9]{40})` \| `(?<tree>[a-f0-9]{40})` \| (?<count>\d+) \| `(?<hash>[a-f0-9]{64})`')
if ($matches.Count -ne 9) { throw "Expected nine inventory records, parsed $($matches.Count)" }
foreach ($m in $matches) {
 $dir=Join-Path "$repo\docs\reviews" $m.Groups['dir'].Value
 $path=Join-Path $dir 'manifest.json'
 $manifest=Get-Content -Raw -LiteralPath $path | ConvertFrom-Json
 $head=$m.Groups['head'].Value
 $tree=git rev-parse "${head}^{tree}"
 $treeExit=$LASTEXITCODE
 git merge-base --is-ancestor $head 48370711a6247b972dfc83628618924e864be374
 $ancestor=$LASTEXITCODE -eq 0
 $bad=@(foreach ($entry in $manifest.files) { 
  $file=Join-Path $dir $entry.path
  if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { "missing: $($entry.path)"; continue }
  $size=(Get-Item -LiteralPath $file -Force).Length
  $hash=(Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash
  if ($size -ne $entry.bytes -or $hash -ne $entry.sha256) { "$($entry.path): $size $hash; expected $($entry.bytes) $($entry.sha256)" }
 })
 [pscustomobject]@{package=$m.Groups['dir'].Value; hash=(Get-FileHash -LiteralPath $path).Hash; hashMatches=((Get-FileHash -LiteralPath $path).Hash -eq $m.Groups['hash'].Value); reviewedHead=$head; reviewedTree=$tree; identityMatches=($treeExit -eq 0 -and $tree -eq $m.Groups['tree'].Value -and $manifest.reviewedHead -eq $head -and ($null -eq $manifest.reviewedTree -or $manifest.reviewedTree -eq $tree)); ancestor=$ancestor; entries=$manifest.files.Count; countMatches=($manifest.files.Count -eq [int]$m.Groups['count'].Value); mismatches=$bad} | ConvertTo-Json -Depth 5 -Compress
}
Get-ChildItem -LiteralPath "$repo\docs\reviews\CI1A-S2.5-WP-S25-1-BIND-01-evidence\fixtures" -File | ForEach-Object { [pscustomobject]@{name=$_.Name; size=$_.Length; sha256=(Get-FileHash -LiteralPath $_.FullName).Hash} | ConvertTo-Json -Compress }
Get-FileHash -LiteralPath "$repo\docs\reviews\CI1A-S2.5-WP-S25-3-P2-QUAL-evidence\oracle-r2.json" | Select-Object -ExpandProperty Hash

