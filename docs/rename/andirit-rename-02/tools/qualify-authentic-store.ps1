param(
    [Parameter(Mandatory=$true)][string]$Repository,
    [Parameter(Mandatory=$true)][string]$Workspace,
    [Parameter(Mandatory=$true)][string]$AuthenticSeed,
    [Parameter(Mandatory=$true)][string]$ExpectedSeedSha256,
    [Parameter(Mandatory=$true)][string]$OriginalSourceRoot,
    [Parameter(Mandatory=$true)][string]$ExpectedReport,
    [ValidateSet('projection','native')][string]$Profile='projection'
)
$ErrorActionPreference='Stop'
if(Test-Path -LiteralPath $Workspace){throw 'Choose a new external disposable workspace; nothing is overwritten'}
$repoPath=(Resolve-Path -LiteralPath $Repository).Path
$workspacePath=[IO.Path]::GetFullPath($Workspace)
if($workspacePath.StartsWith($repoPath+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Workspace must be outside checkout'}
if((Get-FileHash -LiteralPath $AuthenticSeed -Algorithm SHA256).Hash -ne $ExpectedSeedSha256){throw 'Authentic seed hash mismatch'}
$sourcePath=(Resolve-Path -LiteralPath $OriginalSourceRoot).Path
New-Item -ItemType Directory -Path "$workspacePath/probe","$workspacePath/store","$workspacePath/reports" | Out-Null
Copy-Item -LiteralPath $AuthenticSeed -Destination "$workspacePath/store/graph.db"
foreach($language in @('ts','js')){Copy-Item -LiteralPath "$sourcePath/baseline-$language.json" -Destination "$workspacePath/store/baseline-$language.json"}
Copy-Item -LiteralPath "$PSScriptRoot/HistoricalProbe.cs" -Destination "$workspacePath/probe/Program.cs"
$refs=foreach($suffix in @('','.Abstractions','.TreeSitter','.LatticeDb','.CSharp','.Testing')){'<ProjectReference Include="'+$repoPath+'/src/Andirit'+$suffix+'/Andirit'+$suffix+'.csproj" />'}
$project='<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup>'+($refs -join '')+'</ItemGroup></Project>'
[IO.File]::WriteAllText("$workspacePath/probe/probe.csproj",$project)
dotnet build "$workspacePath/probe/probe.csproj" -c Release *> "$workspacePath/build.txt"
if($LASTEXITCODE -ne 0){throw 'Probe build failed; see external transcript'}
$env:ANDIRIT_FIXTURE_SOURCE_ROOT=$sourcePath
foreach($step in @(@('inspect','reopen'),@('index','repeat'),@('cancel','cancel'),@('inspect','after'))){
    $operation,$label=$step
    dotnet "$workspacePath/probe/bin/Release/net10.0/probe.dll" $operation "$workspacePath/store" "$workspacePath/reports/$label.json" $label $Profile
    if($LASTEXITCODE -ne 0){throw "Probe failed: $label"}
}
python "$PSScriptRoot/verify-observations.py" "$workspacePath/reports" $ExpectedReport
if($LASTEXITCODE -ne 0){throw 'Strict authentic-store comparison failed'}
if((Get-FileHash -LiteralPath $AuthenticSeed -Algorithm SHA256).Hash -ne $ExpectedSeedSha256){throw 'Original seed changed'}
