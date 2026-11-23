param(
    [Parameter(Mandatory = $true)][string]$RepoRoot,
    [Parameter(Mandatory = $true)][string]$Output
)

$ErrorActionPreference = "Stop"
$probe = "D:\m54b-stage6\qual-s61\probe"

Push-Location $probe
try {
    dotnet build ReviewerProbe.csproj -c Release "-p:RepoRoot=$RepoRoot" -v minimal 2>&1 |
        Select-String -Pattern "error|Build succeeded" | ForEach-Object { $_.Line }
    if ($LASTEXITCODE -ne 0) { throw "probe build failed" }

    $exe = Join-Path $probe "bin\Release\net10.0\Andirit.LatticeDb.Tests.exe"
    & $exe 2>&1 | Out-File -FilePath $Output -Encoding utf8
    $code = $LASTEXITCODE
}
finally {
    Pop-Location
}

Write-Host "probe exit: $code -> $Output"
exit $code
