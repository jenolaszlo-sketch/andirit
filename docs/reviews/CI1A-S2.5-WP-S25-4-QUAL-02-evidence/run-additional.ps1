param([Parameter(Mandatory)][string]$Work, [switch]$Baseline, [switch]$Declared)
$ErrorActionPreference='Stop'
$env:NUGET_PACKAGES = Join-Path $env:USERPROFILE '.nuget/packages'
$evidence="$Work/evidence/additional"
$fixtures="$Work/corrected/docs/reviews/CI1A-S2.5-WP-S25-4-QUAL-01-evidence/fixture"
$stores="$Work/database/additional"
$revision="$Work/corrected"
$probe="$Work/control"
if ($Baseline) {
    $evidence="$Work/evidence/baseline-comparison"
    $stores="$Work/database/baseline-comparison"
    $revision="$Work/baseline"
    $probe="$Work/control-baseline"
    New-Item -Path $probe -ItemType Directory -Force | Out-Null
    Get-ChildItem -LiteralPath "$Work/control" -File | Copy-Item -Destination $probe
}
if ($Declared) {
    $evidence = if ($Baseline) { "$Work/evidence/baseline-declared" } else { "$Work/evidence/declared" }
    $stores = if ($Baseline) { "$Work/database/baseline-declared" } else { "$Work/database/declared" }
    New-Item -ItemType Directory -Path $evidence,$stores -Force | Out-Null
}
New-Item -ItemType Directory -Path $evidence,$stores -Force | Out-Null
function Run-Logged($name, [string[]]$argsList) {
    $path="$evidence/$name.log"
    "Command: dotnet $($argsList -join ' ')" | Set-Content $path
    & dotnet @argsList 2>&1 | Out-File $path -Append
    $status=$LASTEXITCODE
    "Exit code: $status" | Add-Content $path
    Write-Host "$name = $status"
    return $status
}
$build = Run-Logged build @('build',"$probe/Qual.csproj",'-c','Release',"-p:RepoRoot=$revision",'-p:NuGetAudit=false')
if ($build -ne 0) { exit $build }
$dll="$probe/bin/Release/net10.0/Andirit.CSharp.Tests.dll"
Get-FileHash -LiteralPath $dll | Format-List | Out-File "$evidence/executable.txt"
$profiles = if ($Baseline) { @('ctor','property','reference','incomplete') } else {
    @('provenance','ctor','property','reference','local','inactive','empty','incomplete')
}
if ($Declared) { $profiles = @('declared') }
foreach ($profile in $profiles) {
    foreach ($phase in 'publish','reopen') {
        if ($profile -eq 'provenance' -and $phase -eq 'reopen') { continue }
        $name="control-$profile-$phase"
        $null= Run-Logged $name @($dll,$name,"$stores/$profile.ltdb","$evidence/$name.json",$fixtures)
    }
}


