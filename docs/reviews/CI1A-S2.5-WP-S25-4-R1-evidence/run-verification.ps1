param([Parameter(Mandatory)][string]$Scratch)
$ErrorActionPreference = 'Stop'
$v = Join-Path $Scratch verification
$corrected = Join-Path $Scratch corrected
$sealed = 'docs/reviews/CI1A-S2.5-WP-S25-4-QUAL-01-evidence'
$dotnetCsharp = 'tests/Andirit.CSharp.Tests/Andirit.CSharp.Tests.csproj'
$dotnetNative = 'tests/Andirit.LatticeDb.Tests/Andirit.LatticeDb.Tests.csproj'
$trx = Join-Path $v trx
New-Item -ItemType Directory -Path $corrected,$trx | Out-Null

function Run-Dotnet($log, [string[]]$cli) {
    $file = Join-Path $v "$log.log"
    "Command: dotnet $($cli -join ' ')" | Set-Content $file
    "Started UTC: $([DateTimeOffset]::UtcNow.ToString('o'))" | Tee-Object $file -Append
    & dotnet @cli 2>&1 | Tee-Object $file -Append
    $code = $LASTEXITCODE
    "Exit code: $code" | Tee-Object $file -Append
    if ($code -ne 0) { throw "Failed gate: $log" }
}

function Run-Probe($phase, $db, $inputDir) {
    $dll = Join-Path $corrected 'probe/bin/Release/net10.0/Andirit.CSharp.Tests.dll'
    Run-Dotnet "probe-$phase" @($dll, $phase, $db, "$corrected/$phase.json", $inputDir)
}

$revision = git rev-parse HEAD
$tree = git rev-parse 'HEAD^{tree}'
"HEAD: $revision","Tree: $tree","Recorded origin/main: $(git rev-parse origin/main)" |
    Set-Content "$v/gate-revision.txt"
if ((git status --porcelain=v1) -or ((git rev-parse HEAD^) -ne '8ab2d25d058e5bb4301094078f1737b139868e6a')) {
    throw 'Expected clean implementation commit'
}

Run-Dotnet release-build @('build','Andirit.slnx','-c','Release')
Run-Dotnet focused-r1 @('test',$dotnetCsharp,'-c','Release','--no-build','--filter','FullyQualifiedName~ExternalBinding_')
$env:S254_R1_EXTERNAL_PATH = "$corrected/testhost.db"
try {
    foreach ($phase in 'publish','verify') {
        $env:S254_R1_EXTERNAL_PHASE = $phase
        "S254_R1_EXTERNAL_PHASE=$phase" | Add-Content "$v/testhost-environment.txt"
        Run-Dotnet "testhost-$phase" @('test',$dotnetCsharp,'-c','Release','--no-build','--filter',
            'FullyQualifiedName~ExternalBinding_CoverageSurvivesAuthorityBoundPublicationAndReopen',
            '--logger','console;verbosity=detailed')
    }
}
finally { Remove-Item Env:S254_R1_EXTERNAL_PHASE,Env:S254_R1_EXTERNAL_PATH }
Run-Dotnet focused-binding @('test',$dotnetCsharp,'-c','Release','--no-build','--filter',
    'FullyQualifiedName~Verified|FullyQualifiedName~Occurrence|FullyQualifiedName~S24|FullyQualifiedName~S2_2Wp3|FullyQualifiedName~S2_2Wp4|FullyQualifiedName~S2_2Wp5|FullyQualifiedName~S2_2Wp8|FullyQualifiedName~S23PublicationIntegrationTests')
Run-Dotnet focused-native @('test',$dotnetNative,'-c','Release','--no-build','--filter',
    'FullyQualifiedName~OccurrenceStage1NativeTests|FullyQualifiedName~New01RecoveryProjectionTests|FullyQualifiedName~S24SurfaceRefusalMappingTests|FullyQualifiedName~NativeGraphMirrorTests|FullyQualifiedName~S23Wp5NamespaceRefusalTests|FullyQualifiedName~HistoricalStorageIdentityTests')
Run-Dotnet focused-refusal @('test',$dotnetNative,'-c','Release','--no-build','--filter',
    'FullyQualifiedName~Store_Rejects|FullyQualifiedName~Refusal')
Run-Dotnet full-solution @('test','Andirit.slnx','-c','Release','--no-build','--logger','trx','--results-directory',$trx)

$planning = 'docs/planning/adr-ci1-01-ci1a-s2-0'
Run-Dotnet oracle @('run','--project',"$planning/oracle/SemanticCorpusOracle.csproj",'-c','Release','--',
    '--corpus',"$planning/corpus",'--expected',"$planning/oracle/expected/oracle-expected-r2.json",
    '--output',"$v/oracle-r2.json")
$expectedHash = '97f6c5e5a4cc77e089481c7643732b7a07a31d483c2643bd7a95fdad7ced39c9'
$actualHash = (Get-FileHash "$v/oracle-r2.json" -Algorithm SHA256).Hash.ToLowerInvariant()
$referenceHash = (Get-FileHash "$sealed/oracle-r2.json" -Algorithm SHA256).Hash.ToLowerInvariant()
$size = (Get-Item "$v/oracle-r2.json").Length
"Generated SHA-256: $actualHash","Sealed QUAL-01 SHA-256: $referenceHash","Bytes: $size" |
    Tee-Object "$v/oracle-identity.txt"
if ($actualHash -ne $expectedHash -or $referenceHash -ne $actualHash -or $size -ne 146851) {
    throw 'Frozen oracle drift'
}
Run-Dotnet format @('format','Andirit.slnx','--verify-no-changes')

Copy-Item -LiteralPath "$sealed/probe","$sealed/fixture" -Destination $corrected -Recurse
Run-Dotnet probe-build @('build',"$corrected/probe/Qual.csproj",'-c','Release','-p:RepoRoot=D:/projects/Andirit')
foreach ($phase in 'b3-publish','b3-reopen') { Run-Probe $phase "$corrected/b3.db" "$corrected/fixture" }
foreach ($phase in 'b3-missing','b3-ambiguous') { Run-Probe $phase "$corrected/$phase.db" "$corrected/fixture" }
foreach ($phase in 'b6-publish','b7-conflict','b7-reopen','b6-delete-a','b6-reopen-a','b6-delete-b','b6-reopen-b') {
    Run-Probe $phase "$corrected/contributors.db" $corrected
}
Run-Probe d3 "$corrected/d3.db" "$corrected/fixture"
Run-Probe d4-schedule "$corrected/d4.db" "$corrected/fixture"
foreach ($phase in 'd5-namespace','d5-schema','d5-page') {
    Copy-Item "$corrected/d3.db" "$corrected/$phase.db"
    foreach ($suffix in '-wal','.wal','-journal') {
        if (Test-Path "$corrected/d3.db$suffix") {
            Copy-Item "$corrected/d3.db$suffix" "$corrected/$phase.db$suffix"
        }
    }
    Run-Probe $phase "$corrected/$phase.db" "$corrected/fixture"
}

if ((git rev-parse HEAD) -ne $revision -or (git status --porcelain=v1)) {
    throw 'Verification changed the tracked checkout'
}
