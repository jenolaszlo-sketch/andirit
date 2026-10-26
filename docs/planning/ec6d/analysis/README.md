# EC-6D baseline reproduction

This folder contains a non-production harness and the observed output for the
purpose-built TypeScript corpus at `../corpus/ts-interface-impact/`. The corpus
is bound to a Git tree in `consumer-baseline.md`.

## Environment

- Windows x64
- .NET SDK 10.0.401
- Hetu source at the EC-6C owner-accepted planning baseline
- TypeScript Tree-sitter plugin `0.2.0-preview.6+v0.23.2.abi14.p9`
- In-memory graph store; no database or source file is modified by the harness

NuGet's online vulnerability-data lookup was unavailable in this environment
and `NU1900` is treated as an error by the repository. Restore with
`NuGetAudit=false`; package integrity and versions remain governed by the
project files and the local NuGet cache.

## Reproduce extraction and queries

Run from the repository root:

```powershell
dotnet restore docs/planning/ec6d/analysis/ConsumerBaseline.csproj --ignore-failed-sources -m:1 -p:RestoreUseStaticGraphEvaluation=true -p:NuGetAudit=false
dotnet build docs/planning/ec6d/analysis/ConsumerBaseline.csproj --no-restore -m:1
dotnet run --no-build --project docs/planning/ec6d/analysis/ConsumerBaseline.csproj -- docs/planning/ec6d/corpus/ts-interface-impact > docs/planning/ec6d/analysis/observed-results.json
```

The harness indexes the fixture through `CodeIndexingService`, exports its
latest publication through `CodePublicationSnapshot`, then queries the
qualified-name, implementation, impact, module-neighborhood, and export
surfaces through `CodeGraphQueryService`. The JSON records distinct facts,
index-unit contributors, evidence providers and locations, result bounds,
and the applicable query provenance. It contains no source contents.

For repeatability, execute the run twice and compare `IndexIdentity` and
`CanonicalFactsSha256` in the JSON. Both matched in the recorded run:

- `IndexIdentity`: `667ec2d4df6b821e63b2f7d1adad6c63492aaf453b9af639f1aa6e9b0fa77ec8`
- `CanonicalFactsSha256`: `05bf7c7de0e5e1a552c77759cc3bba39aff8d72e093405379e5acfd2d99b0d3c`

The snapshot integrity hash includes publication/run metadata and is not used
as the cross-run graph-fact digest.

## Existing regression suites

The four test projects were run individually with the same baseline source and
SDK. All passed, for 388 passed, 0 failed, and 0 skipped:

| Project | Result | Log |
| --- | ---: | --- |
| `Penghou.Hetu.Tests` | 160 passed | [core-test-run.txt](core-test-run.txt) |
| `Penghou.Hetu.CSharp.Tests` | 46 passed | [csharp-test-run.txt](csharp-test-run.txt) |
| `Penghou.Hetu.LatticeDb.Tests` | 26 passed | [latticedb-test-run.txt](latticedb-test-run.txt) |
| `Penghou.Hetu.TreeSitter.Tests` | 156 passed | [treesitter-test-run.txt](treesitter-test-run.txt) |

The test projects were run separately because a one-shot solution build filled
the available disk before the core project completed. The initial sandboxed
solution run also hit a testhost connection timeout; the individual runs used
normal process communication. These environment issues did not occur in the
completed project runs.

The small fixture run is correctness evidence for this scenario only. It is
not a large-repository performance measurement.
