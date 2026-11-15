
$ErrorActionPreference='Stop'
Set-Location 'D:\projects\Andirit'
$out='C:\Users\Laszlos\source\repos\Solo\andirit-wp-s25-4-qual-01'
$store=Join-Path ([IO.Path]::GetTempPath()) ('andirit-d4-' + [guid]::NewGuid().ToString('N') + '.ltdb')
$project='docs/reviews/CI1A-S2.5-WP-S25-3-P2-QUAL-tests/P2Qual.csproj'
$log=Join-Path $out 'd4-established-probe-build.log'
"Command: dotnet build $project -c Release" | Set-Content -LiteralPath $log
dotnet build $project -c Release *>&1 | Out-File -LiteralPath $log -Append
$code=$LASTEXITCODE
"Exit code: $code" | Add-Content -LiteralPath $log
if ($code) { exit $code }
$dll=(Resolve-Path -LiteralPath (Join-Path (Split-Path $project) 'bin/Release/net10.0/Andirit.CSharp.Tests.dll')).Path
Get-FileHash -LiteralPath $dll | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'd4-established-executable.json')
foreach ($phase in 'integrate','reopen','recover') {
 $json=Join-Path $out "d4-established-$phase.json"
 $log=Join-Path $out "d4-established-$phase.log"
 "Command: dotnet $dll $phase $store $json" | Set-Content -LiteralPath $log
 dotnet $dll $phase $store $json *>&1 | Out-File -LiteralPath $log -Append
 $code=$LASTEXITCODE
 "Exit code: $code" | Add-Content -LiteralPath $log
 Write-Host "$phase exit $code"
 if ($code) { exit $code }
}

