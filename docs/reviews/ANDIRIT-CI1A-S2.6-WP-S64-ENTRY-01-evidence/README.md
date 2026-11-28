# S6.4 entry evidence

This package supports the [S6.4 entry assessment](../ANDIRIT-CI1A-S2.6-WP-S64-ENTRY-01.md). `baseline-verification.txt` identifies the verified Git objects, remote runs, released packages and evidence comparisons. `reviewed-inputs.json` records the inspected fixture and implementation blobs from the exact baseline. It is an entry inventory. The successor will freeze its selected runtime inputs and independent oracle separately.

## Windows entry observation

The standalone observer built the reviewed production source and published a compact derivative of the linked-file project fixture to two fresh databases. Both lanes used the same source and query parameters. The graph-only sink forwarded graph facts while omitting occurrence batches and authority. The integrated lane persisted one occurrence through the normal ingestion sink. LatticeDbSharp read the raw graph and occurrence rows directly. The sampled incoming traversal returned the same ordered pair of logical edges through native serving, managed projection and re-enabled reopen in both lanes.

| Windows x64 observation | Initial mirror-enabled publish | Mirror-enabled reopen | Mirror-disabled publish | Re-enabled reopen |
| --- | ---: | ---: | ---: | ---: |
| Physical `HetuGraphNode` rows, both lanes | 21 | 22 | 22 | 43 |
| Physical nodes with the one linked-file logical ID, both lanes | 2 | 3 | 3 | 5 |
| `HetuMirror` rows, both lanes | 1 | 1 | 0 | 1 |
| Durable facts, graph-only / integrated | 0 / 1 | 0 / 1 | 0 / 1 | 0 / 1 |

There were 20 distinct logical graph node IDs. The raw census exposes an unmapped linked-node duplicate already at initial publish and the additional orphan after the first enabled reopen. The disabled publication removes the manifest while retaining all 22 old physical nodes. Re-enable adds 21 more nodes. The raw observations do not count all edge payloads or constitute S6.4 lifecycle qualification. [Graph-only receipt](mirror-graph-only.json) · [Integrated receipt](mirror-integrated.json)

Run the observer with disposable artifact and database paths. A fresh database is required for each run. The only database writes are to those disposable files. This observer is evidence for the entry findings and is outside the test solution.

```powershell
$project = 'docs/reviews/ANDIRIT-CI1A-S2.6-WP-S64-ENTRY-01-evidence/mirror-observer/ObserveMirror.csproj'
$scratch = Join-Path ([System.IO.Path]::GetTempPath()) ('andirit-s64-entry-' + [guid]::NewGuid())
New-Item -ItemType Directory -Path $scratch | Out-Null
dotnet build $project -c Release --artifacts-path $scratch -p:NuGetAudit=false
if ($LASTEXITCODE -ne 0) { throw 'Entry observer build failed' }
foreach ($mode in 'graph-only','integrated') {
    dotnet (Join-Path $scratch 'bin/ObserveMirror/release/ObserveMirror.dll') (Join-Path $scratch "$mode.ltdb") $mode
    if ($LASTEXITCODE -ne 0) { throw "Entry observer failed in $mode mode" }
}
```

The entry observer used Windows x64, SDK 10.0.401, runtime 10.0.12 and the LatticeDbSharp 0.2.0 pin. An initial scratch attempt used lowercase JSON keys in the observer and stopped before producing a finding. The corrected observer uses the actual persisted `Id`/`Value` keys. A second run on fresh stores reproduced each JSON receipt byte-for-byte after LF canonicalization. The outputs are Windows evidence only.

## Seal verification

The payload adds the entry reports and this evidence directory. The following commit adds only `manifest.json`. The seal binds each payload path to its committed Git blob, byte length and SHA-256 and checks the branch's direct ancestry from reviewed main. Use this command from the repository root.

```text
python docs/reviews/ANDIRIT-CI1A-S2.6-WP-S64-ENTRY-01-evidence/seal.py verify HEAD
```
