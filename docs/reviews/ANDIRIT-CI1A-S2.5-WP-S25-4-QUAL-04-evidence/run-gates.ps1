param(
    [Parameter(Mandatory)][string]$Root,      # pinned exact-byte export root
    [Parameter(Mandatory)][string]$Evidence,  # gate log directory
    [string]$CheckoutRoot = ""                # optional checkout-profile export for the format gate
)
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $Root
$env:DOTNET_CLI_HOME = "C:\Windows\Temp\opencode\qual04\dotnet-home"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'; $env:DOTNET_NOLOGO = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$ProgressPreference = 'SilentlyContinue'
New-Item -Path $Evidence -ItemType Directory -Force | Out-Null

function Gate([string]$Name, [string[]]$Cli, [string]$Workdir) {
    $file = Join-Path $Evidence "$Name.log"
    Push-Location -LiteralPath $Workdir
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'   # native stderr is expected output here
    try {
        "Working directory: $Workdir" | Set-Content $file
        "Command: dotnet $($Cli -join ' ')" | Add-Content $file
        & dotnet @Cli *>> $file
        $status = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $previous
        Pop-Location
    }
    "Exit code: $status" | Add-Content $file
    Write-Host "$Name = $status"
    return $status
}

$trx = Join-Path $Evidence 'trx'
New-Item -Path $trx -ItemType Directory -Force | Out-Null
$exits = [ordered]@{}

$exits['toolchain'] = Gate toolchain @('--info') $Root
$exits['release'] = Gate release @('build','Andirit.slnx','-c','Release','-p:NuGetAudit=false','--ignore-failed-sources') $Root
if ($exits['release'] -eq 0) {
    $common = @('-c','Release','--no-build')
    $csharp = 'tests/Andirit.CSharp.Tests/Andirit.CSharp.Tests.csproj'
    $native = 'tests/Andirit.LatticeDb.Tests/Andirit.LatticeDb.Tests.csproj'
    $exits['discovery'] = Gate discovery (@('test','Andirit.slnx') + $common + @('--list-tests')) $Root
    $exits['full'] = Gate full (@('test','Andirit.slnx') + $common + @('--logger','trx','--results-directory',$trx)) $Root
    $exits['f02'] = Gate f02 (@('test',$csharp) + $common + @('--filter','FullyQualifiedName~UnsupportedSites_CountOnce')) $Root
    $exits['f03'] = Gate f03 (@('test',$csharp) + $common + @('--filter','FullyQualifiedName~MissingDeclaredUnit')) $Root
    $exits['r1'] = Gate r1 (@('test',$csharp) + $common + @('--filter','FullyQualifiedName~ExternalBinding_')) $Root
    $exits['binding'] = Gate binding (@('test',$csharp) + $common + @('--filter','FullyQualifiedName~S2_2|FullyQualifiedName~S23PublicationIntegration')) $Root
    $exits['publication'] = Gate publication (@('test',$csharp) + $common + @('--filter','FullyQualifiedName~S23|FullyQualifiedName~S24|FullyQualifiedName~CSharpProvenance')) $Root
    $exits['native'] = Gate native (@('test',$native) + $common) $Root
    $exits['format'] = Gate format @('format','Andirit.slnx','--no-restore','--verify-no-changes') $Root
    if ($CheckoutRoot -ne '') {
        $exits['checkout-format'] = Gate 'checkout-format' @('format','Andirit.slnx','--no-restore','--verify-no-changes') $CheckoutRoot
    }
    $exits['coverage-core'] = Gate 'coverage-core' @(
        'test','tests/Andirit.Tests/Andirit.Tests.csproj') $Root
    $exits['coverage-latticedb'] = Gate 'coverage-latticedb' @(
        'test','tests/Andirit.LatticeDb.Tests/Andirit.LatticeDb.Tests.csproj') $Root
}

$p = 'docs/planning/adr-ci1-01-ci1a-s2-0'
$exits['oracle'] = Gate oracle @('run','--project',"$p/oracle/SemanticCorpusOracle.csproj",'-c','Release','-p:NuGetAudit=false','--',
    '--corpus',"$p/corpus",'--expected',"$p/oracle/expected/oracle-expected-r2.json",'--output',"$Evidence/oracle-r2.json") $Root
if (Test-Path -LiteralPath "$Evidence/oracle-r2.json") {
    Get-FileHash -LiteralPath "$Evidence/oracle-r2.json" | Format-List | Out-File "$Evidence/oracle-hash.txt"
}
[pscustomobject]$exits | ConvertTo-Json | Out-File "$Evidence/gate-exits.json" -Encoding utf8
$exits.GetEnumerator() | ForEach-Object { Write-Host "gate $($_.Key) = $($_.Value)" }