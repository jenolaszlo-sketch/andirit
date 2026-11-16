param([Parameter(Mandatory)][string]$Work, [switch]$Retry)
$ErrorActionPreference='Stop'
$env:NUGET_PACKAGES = Join-Path $env:USERPROFILE '.nuget/packages'
$evidence="$Work/evidence/state"
$fixtures="$Work/corrected/docs/reviews/CI1A-S2.5-WP-S25-4-QUAL-01-evidence/fixture"
$stores="$Work/database/state"
if ($Retry) {
    $evidence="$Work/evidence/state-retry"
    $stores="$Work/database/state-retry"
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
$build = Run-Logged build @('build',"$Work/control/Qual.csproj",'-c','Release',"-p:RepoRoot=$Work/corrected",'-p:NuGetAudit=false')
if ($build -ne 0) { exit $build }
$dll="$Work/control/bin/Release/net10.0/Andirit.CSharp.Tests.dll"
Get-FileHash -LiteralPath $dll | Format-List | Out-File "$evidence/executable.txt"
$profiles = if ($Retry) { @('known','supersession') } else { @('lifecycle','known','unknown','supersession') }
foreach ($profile in $profiles) {
    $phases = if ($profile -eq 'lifecycle') { @('publish','replace','reopen','delete','deletedreopen') } else { @('publish','reopen') }
    foreach ($phase in $phases) {
        $name="state-$profile-$phase"
        $null= Run-Logged $name @($dll,$name,"$stores/$profile.ltdb","$evidence/$name.json",$fixtures)
    }
}
foreach ($name in 'identity-discriminators','multiple-definitions-publish','multiple-definitions-reopen') {
    $null= Run-Logged $name @($dll,$name,"$stores/multiple.ltdb","$evidence/$name.json",$fixtures)
}

if ($Retry) {
    foreach ($name in 'coverage-boundary-publish','coverage-boundary-reopen') {
        $null= Run-Logged $name @($dll,$name,"$stores/partial.ltdb","$evidence/$name.json",$fixtures)
    }
}
