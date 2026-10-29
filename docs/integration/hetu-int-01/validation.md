# Fresh integration validation

Environment: Windows 11 x64 (10.0.26200), .NET SDK 10.0.401, MSBuild 18.9.11, net10.0. No WSL2, native Linux or macOS migration run is claimed by local evidence.

| Gate | Result |
|---|---|
| Release full solution build | Pass, zero warnings/errors |
| Core regressions | 160 passed |
| C# regressions | 46 passed |
| Tree-sitter / EC-6B / EC-6C / Elixir regressions | 156 passed |
| LatticeDB persistence/query/native/failure regressions | 26 passed |
| Total | **388 passed, zero failed/skipped** |
| Formatting | Pass |
| CI coverage gates | Pass: core 80.59% line / 63.05% branch; C# 93.62% / 80.95%; Tree-sitter 89.32% / 75.36%; LatticeDB 90.49% / 77.68% |
| Local package-native consumer smoke | Pass for TypeScript and JavaScript |
| Durable migration | [Pass](migration-rehearsal.md) |
| Consumer fixture | Pass: 73 nodes, 23 declarations, 99 edges, one unresolved relationship; canonical hash matches superseding Q2 CRLF evidence |
| Accepted-history whitespace | `git diff --check main HEAD`: pass before evidence additions |

Exact commands (absolute paths and output locations appear in raw logs):

```powershell
dotnet build <canonical>/Penghou.Hetu.slnx -c Release
dotnet test <canonical>/Penghou.Hetu.slnx -c Release --no-build --no-restore --logger trx --results-directory <evidence>/test-results
dotnet format <canonical>/Penghou.Hetu.slnx --verify-no-changes --no-restore
dotnet test <canonical>/tests/<suite>/<suite>.csproj -c Release --no-restore /p:CollectCoverage=true /p:CoverletOutput=<evidence>/coverage/<suite>/ /p:CoverletOutputFormat=cobertura /p:Include=<assembly-filter> /p:Threshold=<threshold> /p:ThresholdType=line%2cbranch
dotnet pack <canonical>/Penghou.Hetu.slnx -c Release --no-restore --output <evidence>/smoke-packages -p:Version=0.2.0-preview.6-int01-smoke
$env:SMOKE_FEED='<evidence>/smoke-packages'
dotnet run --project <canonical>/smoke/TreeSitterPackage/TreeSitterPackage.csproj -c Release -p:SmokerPackageVersion=0.2.0-preview.6-int01-smoke
dotnet run --project <canonical>/docs/planning/ec6d/analysis/ConsumerBaseline.csproj -c Release -- <canonical>/docs/planning/ec6d/corpus/ts-interface-impact
python <evidence>/verify_migration.py
```

Coverage suite/filter/threshold values match the unchanged CI workflow: core `[Penghou.Hetu]*%2c[Penghou.Hetu.Abstractions]*` / 55; C# `[Penghou.Hetu.CSharp]*` / 75; Tree-sitter `[Penghou.Hetu.TreeSitter]*` / 60; LatticeDB `[Penghou.Hetu.LatticeDb]*` / 70. Coverlet enforces line and branch thresholds.

Logs: [build](raw/build.txt), [tests](raw/tests.txt), [format](raw/format.txt), [package smoke](raw/package-smoke.txt), [consumer](raw/consumer-baseline.txt), [consumer comparison](raw/consumer-verification.json). TRX results and coverage command outputs are retained under raw. Historical totals were not substituted for these fresh runs.

Consumer canonical hash `631c0ab3960790d714f824259cede7e7da89c3fc930f410b109c8cc4c44ecb29` matches the previously reviewed CRLF corpus. The original-byte hash remains different, as already documented by Q2. `PublicationConsistent=false` reproduced the accepted snapshot limitation; integration does not silently declare it fixed.

CI safety: `ci.yml` runs build/test/format/pack on main pushes, without publishing. `publish.yml` triggers only `v*` tags or manual dispatch. Benchmarks are manual. No workflow changed, package was published, release was created, or tag was pushed. Branch metadata showed main unprotected and rulesets empty; no required protection was bypassed.

Remote integration CI **passed** for exact main `4aadf0e659e3e536e12c4eb64214a9796f4b323b`: [run 40799525098](https://github.com/jenolaszlo-sketch/penghou-hetu/actions/runs/40799525098). Observed jobs: pack, test (windows-latest), test (ubuntu-latest), test (macos-latest), all success. [Bound CI result](raw/ci-integration.json). These native hosted regression runs do not replace the Windows-focused independent qualification or establish native Linux/macOS migration qualification.

Final retirement/evidence changes are documentation and evidence only. The final handoff separately reports the follow-up documentation push's CI state. Post-retirement connectivity checks passed in both repositories; new evidence hashes/links and final staged whitespace are verified before committing.
