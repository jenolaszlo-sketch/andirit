param([Parameter(Mandatory)][string]$Work)
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath "$Work/corrected"
$out = "$Work/evidence/confirmation"
$temp = "$Work/database/replay"
New-Item -Path $temp -ItemType Directory -Force | Out-Null
function Recorded([string]$Name, [string[]]$Cli) {
    $log = "$out/$Name.log"
    "Command: dotnet $($Cli -join ' ')" | Set-Content -LiteralPath $log
    & dotnet @Cli 2>&1 | Out-File -LiteralPath $log -Append
    $code = $LASTEXITCODE
    "Exit code: $code" | Add-Content -LiteralPath $log
    Write-Host "$Name = $code"
    return $code
}
$p2 = 'docs/reviews/CI1A-S2.5-WP-S25-3-P2-QUAL-tests/bin/Release/net10.0/Andirit.CSharp.Tests.dll'
foreach ($phase in 'integrate','reopen') {
    $null = Recorded "p2-correct-order-$phase" @($p2,$phase,"$temp/p2.ltdb","$out/p2-correct-order-$phase.json")
}
$sq04 = 'docs/reviews/CI1A-S2.5-S25-SQ04-Q1-tests/bin/Release/net10.0/Sq04QualProbe.dll'
$source = 'D:/projects/Andirit/docs/reviews/CI1A-S2.5-WP-S25-1-BIND-01-evidence/fixtures/baseline/baseline-graph-only.master.db'
$copy = "$temp/graph-only.db"
Copy-Item -LiteralPath $source -Destination $copy
$masterBefore = (Get-FileHash -LiteralPath $source).Hash.ToLowerInvariant()
$copyBefore = (Get-FileHash -LiteralPath $copy).Hash.ToLowerInvariant()
if ($masterBefore -cne 'f21a72bee2c2e378e16766ca7b84c29010ab4d50360bf3111a82706f947781e0' -or
    $copyBefore -cne $masterBefore) { throw 'Graph-only fixture hash mismatch' }
$code = Recorded sq04-graph-only-retry @($sq04,'graph-only',$copy,"$out/sq04-graph-only-retry.json")
$masterAfter = (Get-FileHash -LiteralPath $source).Hash.ToLowerInvariant()
$copyAfter = (Get-FileHash -LiteralPath $copy).Hash.ToLowerInvariant()
if ($masterBefore -cne $masterAfter) { throw 'Graph-only master altered' }
[ordered]@{master=$source;copy=$copy;bytes=(Get-Item -LiteralPath $copy).Length;
    masterBefore=$masterBefore;masterAfter=$masterAfter;copyBefore=$copyBefore;copyAfter=$copyAfter;exitCode=$code} |
    ConvertTo-Json | Set-Content "$out/sq04-graph-only-retry-hashes.json"
