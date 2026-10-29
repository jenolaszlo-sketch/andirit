param(
    [Parameter(Mandatory=$true)][string]$Repository,
    [Parameter(Mandatory=$true)][string]$Workspace
)
$ErrorActionPreference='Stop'
if(Test-Path -LiteralPath $Workspace){throw 'Workspace must be a new disposable path outside the checkout.'}
$repoPath=(Resolve-Path -LiteralPath $Repository).Path
$workspacePath=[IO.Path]::GetFullPath($Workspace)
if($workspacePath.StartsWith($repoPath+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Choose a workspace outside the checkout'}
New-Item -ItemType Directory -Path $workspacePath | Out-Null
git -C $repoPath archive --format=zip "--output=$workspacePath/baseline.zip" b8d096672334f6bed443974ca548f558896fe54c
if($LASTEXITCODE -ne 0){throw 'Exact baseline archive failed'}
Expand-Archive -LiteralPath "$workspacePath/baseline.zip" -DestinationPath "$workspacePath/baseline-source"
foreach($version in @('baseline','andirit')){
    $product=if($version -eq 'baseline'){'Hetu'}else{'Andirit'}
    $source=if($version -eq 'baseline'){"$workspacePath/baseline-source"}else{$repoPath}
    $folder="$workspacePath/probe-$version"
    New-Item -ItemType Directory -Path $folder | Out-Null
    $code=[IO.File]::ReadAllText("$PSScriptRoot/MigrationProbe.cs").Replace('Penghou.Andirit',"Penghou.$product")
    [IO.File]::WriteAllText("$folder/Program.cs",$code)
    $refs=foreach($suffix in @('','.Abstractions','.TreeSitter','.LatticeDb','.CSharp','.Testing')){'<ProjectReference Include="'+$source+'/src/Penghou.'+$product+$suffix+'/Penghou.'+$product+$suffix+'.csproj" />'}
    $project='<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup>'+($refs -join '')+'</ItemGroup></Project>'
    [IO.File]::WriteAllText("$folder/probe.csproj",$project)
    dotnet build "$folder/probe.csproj" -c Release *> "$workspacePath/build-$version.txt"
    if($LASTEXITCODE -ne 0){throw "Build failed: $version; see transcript"}
}
New-Item -ItemType Directory -Path "$workspacePath/reports" | Out-Null
foreach($profile in @('projection','native')){
    $root=[IO.Path]::GetFullPath("$workspacePath/stores/$profile")
    dotnet "$workspacePath/probe-baseline/bin/Release/net10.0/probe.dll" seed $root "$workspacePath/reports/baseline-$profile.json" baseline $profile
    if($LASTEXITCODE -ne 0){throw 'Baseline seed failed'}
    Copy-Item -LiteralPath "$root/graph.db" -Destination "$workspacePath/seed-$profile.db"
    Copy-Item -LiteralPath "$workspacePath/seed-$profile.db" -Destination "$root/graph.db" -Force
    foreach($step in @(@('inspect','reopen'),@('index','repeat'),@('cancel','cancel'),@('inspect','after'))){
        $operation,$label=$step
        dotnet "$workspacePath/probe-andirit/bin/Release/net10.0/probe.dll" $operation $root "$workspacePath/reports/$label-$profile.json" $label $profile
        if($LASTEXITCODE -ne 0){throw "Probe failed: $profile $label"}
    }
}
python "$PSScriptRoot/verify-durable.py" "$workspacePath/reports"
if($LASTEXITCODE -ne 0){throw 'Strict durable comparison failed'}
