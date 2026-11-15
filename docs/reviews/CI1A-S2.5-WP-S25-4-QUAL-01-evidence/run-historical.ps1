
$ErrorActionPreference='Stop'
Set-Location 'D:\projects\Andirit'
$out='C:\Users\Laszlos\source\repos\Solo\andirit-wp-s25-4-qual-01'
$tempRoot=Join-Path ([IO.Path]::GetTempPath()) ('andirit-s254-hist-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tempRoot | Out-Null
$masterRoot=Join-Path (Get-Location) 'docs/reviews/CI1A-S2.5-WP-S25-1-BIND-01-evidence/fixtures'
$cases=@(
 @{id='pre-rem';paths=@('prerem/prerem-authentic.ltdb','prerem/prerem-authentic.ltdb-wal');hashes=@('7646b98288a9803cff6f9930c78a3e7df5c676f6fc4e0cd2df2af5e79bc68885','8ca170cdc55a4428a5506bcb447a0667ab10c92c77ef149ae36460a5603aeab4'); expected='store-wide CodeOccurrenceSchemaException on open'},
 @{id='graph-only';paths=@('baseline/baseline-graph-only.master.db');hashes=@('f21a72bee2c2e378e16766ca7b84c29010ab4d50360bf3111a82706f947781e0');expected='historical-master-1, one Logger.Write(string) caller, AnalysisUnavailable and incomplete'}
)
function Digest($path) { if (Test-Path -LiteralPath $path -PathType Leaf) { (Get-FileHash -LiteralPath $path).Hash.ToLowerInvariant() } }
$probe='docs/reviews/CI1A-S2.5-S25-SQ04-Q1-tests/Sq04QualProbe.csproj'
dotnet build $probe -c Release *>&1 | Out-File -LiteralPath (Join-Path $out 'historical-probe-build.log')
if ($LASTEXITCODE) { exit $LASTEXITCODE }
$records=@(foreach ($case in $cases) {
 $dir=Join-Path $tempRoot $case.id
 New-Item -ItemType Directory -Path $dir | Out-Null
 $files=@(for ($i=0;$i -lt $case.paths.Count;$i++) {
  $master=Join-Path $masterRoot $case.paths[$i]
  $copy=Join-Path $dir (Split-Path -Leaf $master)
  $hash=Digest $master
  if ($hash -ne $case.hashes[$i]) { throw "Master hash mismatch for $master" }
  Copy-Item -LiteralPath $master -Destination $copy
  if ((Digest $copy) -ne $hash) { throw "Copy hash mismatch for $copy" }
  [ordered]@{master=$master;copy=$copy;bytes=(Get-Item -LiteralPath $copy).Length;expectedHash=$case.hashes[$i];masterBefore=$hash;copyBefore=Digest $copy}
 })
 $dll=Get-ChildItem -LiteralPath (Join-Path (Split-Path $probe) 'bin/Release/net10.0') -Filter '*.runtimeconfig.json' | Select-Object -First 1
 $exe=Join-Path $dll.Directory.FullName ($dll.Name.Replace('.runtimeconfig.json','.dll'))
 $json=Join-Path $out ("historical-" + $case.id + ".json")
 $log=Join-Path $out ("historical-" + $case.id + ".log")
 $command=@($exe,$case.id,$files[0].copy,$json)
 ("Command: dotnet " + ($command -join ' ')) | Set-Content -LiteralPath $log
 dotnet @command *>&1 | Out-File -LiteralPath $log -Append
 $code=$LASTEXITCODE
 foreach ($file in $files) { $file['copyAfter']=Digest $file.copy; $file['masterAfter']=Digest $file.master; if ($file.masterAfter -ne $file.masterBefore) { throw "Master mutated: $($file.master)" } }
 [pscustomobject]@{case=$case.id;expected=$case.expected;files=$files;exitCode=$code;executableSha256=(Digest $exe);command=$command;resultFile=(Split-Path -Leaf $json)}
})
$records | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'historical-copy-integrity.json')
$records | Select-Object case,exitCode

