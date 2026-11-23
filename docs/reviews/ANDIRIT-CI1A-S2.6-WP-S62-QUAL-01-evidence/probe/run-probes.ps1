param(
    [Parameter(Mandatory = $true)][string]$RepoRoot,
    [Parameter(Mandatory = $true)][string]$Output
)

$ErrorActionPreference = "Stop"
$probe = "D:\projects\andirit\docs\reviews\ANDIRIT-CI1A-S2.6-WP-S62-QUAL-01-evidence\probe"

Push-Location $probe
try {
    dotnet build Stage6DurabilityProbe.csproj -c Release "-p:RepoRoot=$RepoRoot" -v minimal 2>&1 |
        Select-String -Pattern "error|Build succeeded" | ForEach-Object { $_.Line }
    if ($LASTEXITCODE -ne 0) { throw "probe build failed" }

    $exe = Join-Path $probe "bin\Release\net10.0\Stage6DurabilityProbe.exe"
    & $exe 2>&1 | Out-File -FilePath $Output -Encoding utf8
    $code = $LASTEXITCODE
}
finally {
    Pop-Location
}

Write-Host "probe exit: $code -> $Output"
exit $code