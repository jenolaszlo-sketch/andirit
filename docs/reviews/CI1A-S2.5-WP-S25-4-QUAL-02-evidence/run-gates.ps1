param([Parameter(Mandatory)][string]$Root, [Parameter(Mandatory)][string]$Evidence)
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $Root
$env:DOTNET_CLI_HOME = Join-Path (Split-Path -Parent $Root) 'dotnet-home'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$ProgressPreference = 'SilentlyContinue'
function Gate([string]$Name, [string[]]$Cli) {
    $file = Join-Path $Evidence "$Name.log"
    "Working directory: $Root" | Set-Content $file
    "Command: dotnet $($Cli -join ' ')" | Add-Content $file
    & dotnet @Cli 2>&1 | Out-File $file -Append
    $status = $LASTEXITCODE
    "Exit code: $status" | Add-Content $file
    Write-Host "$Name = $status"
    return $status
}

$trx = Join-Path $Evidence 'trx'
New-Item -Path $trx -ItemType Directory -Force | Out-Null
$null = Gate toolchain @('--info')
$build = Gate release @('build','Andirit.slnx','-c','Release','-p:NuGetAudit=false','--ignore-failed-sources')
if ($build -eq 0) {
    $common = @('-c','Release','--no-build')
    $csharp = 'tests/Andirit.CSharp.Tests/Andirit.CSharp.Tests.csproj'
    $native = 'tests/Andirit.LatticeDb.Tests/Andirit.LatticeDb.Tests.csproj'
    $null = Gate discovery (@('test','Andirit.slnx') + $common + @('--list-tests'))
    $null = Gate full (@('test','Andirit.slnx') + $common + @('--logger','trx','--results-directory',$trx))
    $null = Gate r1 (@('test',$csharp) + $common + @('--filter','FullyQualifiedName~ExternalBinding_'))
    $null = Gate binding (@('test',$csharp) + $common + @('--filter','FullyQualifiedName~S2_2|FullyQualifiedName~S23PublicationIntegration'))
    $null = Gate native (@('test',$native) + $common)
    $null = Gate format @('format','Andirit.slnx','--no-restore','--verify-no-changes')
}
$p = 'docs/planning/adr-ci1-01-ci1a-s2-0'
$null = Gate oracle @('run','--project',"$p/oracle/SemanticCorpusOracle.csproj",'-c','Release','-p:NuGetAudit=false','--','--corpus',"$p/corpus",'--expected',"$p/oracle/expected/oracle-expected-r2.json",'--output',"$Evidence/oracle-r2.json")
if (Test-Path -LiteralPath "$Evidence/oracle-r2.json") {
    Get-FileHash -LiteralPath "$Evidence/oracle-r2.json" | Format-List | Out-File "$Evidence/oracle-hash.txt"
}

