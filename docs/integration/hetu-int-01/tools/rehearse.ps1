$ErrorActionPreference='Stop'
$out=$PSScriptRoot
$sources=[ordered]@{ p6='C:\Users\Laszlos\source\repos\Penghou.Hetu.closure-ec5'; p8='C:\Users\Laszlos\source\repos\Penghou.Hetu.ec6c-1-export-shape-inventory'; p9='C:\Users\Laszlos\source\repos\Penghou.Hetu' }
$reports=Join-Path $out 'migration-reports'
New-Item -ItemType Directory -Path $reports -Force | Out-Null
foreach($version in $sources.Keys) {
    $dir=Join-Path $out "probe-$version"
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
    $source=$sources[$version]
    $project=@"
<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup><Compile Include="..\MigrationProbe.cs" Link="Program.cs"/><ProjectReference Include="$source\src\Penghou.Hetu.TreeSitter\Penghou.Hetu.TreeSitter.csproj"/><ProjectReference Include="$source\src\Penghou.Hetu.LatticeDb\Penghou.Hetu.LatticeDb.csproj"/></ItemGroup></Project>
"@
    Set-Content -LiteralPath "$dir/probe.csproj" -Value $project
    dotnet build "$dir/probe.csproj" -c Release *> "$out/probe-$version-build.txt"
    if($LASTEXITCODE -ne 0){Get-Content "$out/probe-$version-build.txt" -Tail 50; throw "$version probe build failed"}
}
function Invoke-Probe([string]$version,[string]$operation,[string]$workspace,[string]$label,[string]$profile) {
    $arguments=@("$out/probe-$version/bin/Release/net10.0/probe.dll",$operation,$workspace,"$reports/$label-$profile.json",$label,$profile)
    ('dotnet '+($arguments -join ' ')) | Add-Content -LiteralPath "$out/migration-commands.txt"
    dotnet @arguments *> "$reports/$label-$profile.txt"
    if($LASTEXITCODE -ne 0){Get-Content "$reports/$label-$profile.txt" -Tail 40; throw "Probe failed: $label $profile"}
    Get-Content "$reports/$label-$profile.txt"
}
foreach($profile in @('projection','native')) {
    $seed=Join-Path $out "stores/p6-seed-$profile"
    Invoke-Probe 'p6' 'seed' $seed 'p6-seed' $profile
    $direct=Join-Path $out "stores/direct-$profile"
    $staged=Join-Path $out "stores/staged-$profile"
    Copy-Item -LiteralPath $seed -Destination $direct -Recurse
    Copy-Item -LiteralPath $seed -Destination $staged -Recurse
    Invoke-Probe 'p9' 'inspect' $direct 'direct-before' $profile
    Invoke-Probe 'p9' 'index' $direct 'direct-p9' $profile
    Invoke-Probe 'p9' 'index' $direct 'direct-repeat' $profile
    Invoke-Probe 'p9' 'cancel' $direct 'direct-cancel' $profile
    Invoke-Probe 'p8' 'index' $staged 'staged-p8' $profile
    Invoke-Probe 'p9' 'index' $staged 'staged-p9' $profile
    Invoke-Probe 'p9' 'index' $staged 'staged-repeat' $profile
    Invoke-Probe 'p9' 'inspect' $staged 'staged-reopen' $profile
    Invoke-Probe 'p6' 'inspect' $seed 'p6-seed-reopen' $profile
}
