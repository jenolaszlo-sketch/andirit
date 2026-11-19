param(
    [Parameter(Mandatory)][string]$R3Root,
    [Parameter(Mandatory)][string]$R2Root,
    [Parameter(Mandatory)][string]$Evidence
)
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$env:DOTNET_CLI_HOME = "C:\Windows\Temp\opencode\qual04\dotnet-home"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'

function Invoke-PolicyGate([string]$Name, [string]$Root, [string]$Log, [string]$Output) {
    $project = 'tests/Andirit.LatticeDb.Tests/Andirit.LatticeDb.Tests.csproj'
    $arguments = @(
        'test', $project,
        '-c', 'Release', '--no-restore',
        '/p:CollectCoverage=true',
        "/p:CoverletOutput=$Output",
        '/p:CoverletOutputFormat=cobertura',
        '/p:Include=[Andirit.LatticeDb]*',
        '/p:Threshold=70',
        '/p:ThresholdType=line%2cbranch'
    )
    Push-Location -LiteralPath $Root
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        "Working directory: $Root" | Set-Content -LiteralPath $Log
        "Command: dotnet $($arguments -join ' ')" | Add-Content -LiteralPath $Log
        & dotnet @arguments *>> $Log
        $status = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $previous
        Pop-Location
    }
    "Exit code: $status" | Add-Content -LiteralPath $Log
    return $status
}

New-Item -ItemType Directory -Path $Evidence -Force | Out-Null
$r3Output = Join-Path $Evidence 'gates/coverage-latticedb'
$r2Output = Join-Path $Evidence 'gates-r2/coverage-latticedb'
New-Item -ItemType Directory -Path $r3Output -Force | Out-Null
New-Item -ItemType Directory -Path $r2Output -Force | Out-Null
$r3 = Invoke-PolicyGate 'r3' $R3Root (Join-Path $Evidence 'gates/coverage-latticedb-policy.log') ($r3Output + '\')
$r2 = Invoke-PolicyGate 'r2' $R2Root (Join-Path $Evidence 'gates-r2/coverage-latticedb-policy.log') ($r2Output + '\')
# Coverlet 6.0.4 interprets this argument as an output stem. Normalize the
# resulting file to the package's stable coverage-latticedb/coverage.cobertura.xml
# path and remove the duplicate stem file.
$r3Generated = Join-Path $Evidence 'gates/coverage-latticedb.cobertura.xml'
$r2Generated = Join-Path $Evidence 'gates-r2/coverage-latticedb.cobertura.xml'
if (Test-Path -LiteralPath $r3Generated) {
    Copy-Item -LiteralPath $r3Generated -Destination (Join-Path $r3Output 'coverage.cobertura.xml') -Force
    Remove-Item -LiteralPath $r3Generated -Force
}
if (Test-Path -LiteralPath $r2Generated) {
    Copy-Item -LiteralPath $r2Generated -Destination (Join-Path $r2Output 'coverage.cobertura.xml') -Force
    Remove-Item -LiteralPath $r2Generated -Force
}
[ordered]@{
    policy = 'CI LatticeDb: Include=[Andirit.LatticeDb]*, Threshold=70, ThresholdType=line,branch'
    r3 = $r3
    r2 = $r2
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Evidence 'gates/coverage-policy-exits.json') -Encoding utf8
Write-Host "R3 coverage policy exit=$r3"
Write-Host "R2 coverage policy exit=$r2"
exit 0
