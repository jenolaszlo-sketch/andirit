param(
    [Parameter(Mandatory=$true)][string]$Repository,
    [Parameter(Mandatory=$true)][string]$Workspace,
    [string]$P9='5548bf4a870733341a1471eb26a9727192dc9f48'
)
$ErrorActionPreference='Stop'
if(Test-Path -LiteralPath $Workspace){throw 'Choose a new disposable workspace; existing paths are never overwritten.'}
New-Item -ItemType Directory -Path $Workspace | Out-Null
$Workspace=(Resolve-Path -LiteralPath $Workspace).Path
Copy-Item -LiteralPath "$PSScriptRoot/MigrationProbe.cs","$PSScriptRoot/verify_migration.py" -Destination $Workspace
$versions=[ordered]@{p6='558f8ac82ac1b5e07da68f4a71a70240eaf38409';p8='8c1d5e3faf1fbc33162a2fa87dacfbbdf84f4340';p9=$P9}
foreach($version in $versions.Keys){
    $zip=Join-Path $Workspace "$version.zip"
    git -C $Repository archive --format=zip "--output=$zip" $versions[$version]
    if($LASTEXITCODE -ne 0){throw "Cannot export exact $version source"}
    Expand-Archive -LiteralPath $zip -DestinationPath "$Workspace/source-$version"
}
# The original executed script is retained as evidence. Substitute only source
# locations, leaving the commands, probe and assertions identical.
$script=Get-Content -Raw -LiteralPath "$PSScriptRoot/rehearse.ps1"
$script=$script.Replace('C:\Users\Laszlos\source\repos\Penghou.Hetu.closure-ec5',"$Workspace/source-p6")
$script=$script.Replace('C:\Users\Laszlos\source\repos\Penghou.Hetu.ec6c-1-export-shape-inventory',"$Workspace/source-p8")
$script=$script.Replace('C:\Users\Laszlos\source\repos\Penghou.Hetu',"$Workspace/source-p9")
Set-Content -LiteralPath "$Workspace/rehearse.ps1" -Value $script
& "$Workspace/rehearse.ps1"
if($LASTEXITCODE -ne 0){throw 'Rehearsal failed'}
python "$Workspace/verify_migration.py"
if($LASTEXITCODE -ne 0){throw 'Migration comparisons failed'}
Write-Output "Evidence retained at $Workspace. No Git worktree or branch was created."
