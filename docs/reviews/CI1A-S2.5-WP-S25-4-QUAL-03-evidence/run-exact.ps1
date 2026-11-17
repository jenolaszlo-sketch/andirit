$ErrorActionPreference='Stop'
$work=Split-Path -Parent $PSScriptRoot
& "$PSScriptRoot/run-gates.ps1" -Root "$work/production" -Evidence "$PSScriptRoot/gates"
& "$PSScriptRoot/run-review.ps1" -Baseline
& "$PSScriptRoot/run-review.ps1"
& "$PSScriptRoot/run-historical.ps1" -Work $work
$out="$PSScriptRoot/supplemental"
$env:NUGET_PACKAGES=Join-Path $env:USERPROFILE '.nuget/packages'
$env:DOTNET_CLI_HOME="$work/dotnet-home"
$cli=@('build',"$work/supplemental-probe/Qual.csproj",'-c','Release',"-p:RepoRoot=$work/production",'-p:NuGetAudit=false')
"Command: dotnet $($cli -join ' ')" | Set-Content "$out/build.log"
& dotnet @cli *>> "$out/build.log"
"Exit code: $LASTEXITCODE" | Add-Content "$out/build.log"
if($LASTEXITCODE -ne 0){throw 'Supplemental build failed'}
foreach($phase in 'publish','reopen'){
    $cli=@("$work/supplemental-probe/bin/Release/net10.0/Andirit.CSharp.Tests.dll","control-delegates-$phase","$work/stores/delegates-exact.ltdb","$out/control-delegates-$phase.json","$work/production/docs/reviews/CI1A-S2.5-WP-S25-4-QUAL-01-evidence/fixture")
    "UTC: $([DateTimeOffset]::UtcNow.ToString('o'))`nCommand: dotnet $($cli -join ' ')" | Set-Content "$out/$phase.log"
    & dotnet @cli *>> "$out/$phase.log"
    "Exit code: $LASTEXITCODE" | Add-Content "$out/$phase.log"
    Write-Output "supplemental/$phase = $LASTEXITCODE"
}
