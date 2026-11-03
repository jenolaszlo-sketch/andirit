# R2 producer qualification results

## Environment

- .NET SDK 10.0.401; runtime .NET 10.0.12; Roslyn 5.9.0.0; C# `CSharp14`.
- OS Microsoft Windows 10.0.26200, X64.
- Restore/build flags: `-p:NuGetAudit=false` (no other modification).

## Oracle build

`dotnet build docs/planning/adr-ci1-01-ci1a-s2-0/oracle/SemanticCorpusOracle.csproj -p:NuGetAudit=false` — 0 warnings, 0 errors.

## R2 oracle runs

- Frozen expected: `oracle/expected/oracle-expected-r2.json` (145620 bytes,
  SHA-256 `274A7AAE9EB6724879D619416E449C52759B44085D7B718DAD3434B15387CBBF`),
  frozen from an unseeded pre-qualification run.
- Two separate qualified process executions against the frozen expected:
  **185/185 checks passed** each (176 oracle checks + 9 `expected.*` frozen
  section comparisons), exit 0.
- As-produced canonical JSON SHA-256, both processes:
  `97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9`.
  Stored LF-normalized copies (`oracle-r2-run-1.json`, `oracle-r2-run-2.json`):
  `577603295884242155FACF737066D019C60198DE31E6A01FBED644C9CE40891D` (identical).
- All nine compared sections pass: `expected.semantics`, `expected.identities`,
  `expected.variantIdentities`, `expected.variants`, `expected.crossView`,
  `expected.e02`, `expected.coverage`, `expected.counts`, `expected.lifecycle`.
- Mutation-control baseline: the isolated copy used for the external controls
  passes 185/185 before mutation (`PROVENANCE-MUTATION-RESULTS.md`).

## Full solution regression

Executed `dotnet test Andirit.slnx` (log: `solution-test.log`); all four
projects passed with zero failures or skips. Distinct per-project reruns saved
TRX files under `trx/`.

| Test project | Passed | Failed | Skipped |
|---|---:|---:|---:|
| Andirit.LatticeDb.Tests | 43 | 0 | 0 |
| Andirit.TreeSitter.Tests | 157 | 0 | 0 |
| Andirit.Tests | 325 | 0 | 0 |
| Andirit.CSharp.Tests | 120 | 0 | 0 |
| **Total** | **645** | **0** | **0** |

The regression equals the historical 645 total; the R2 changes touch only the
planning-tree oracle and this evidence package, so no production behavior
change is implied or claimed.

## Focused R2 results

- Q2-F01: single canonical pathway; 9/9 cross-view sites agree; S01/INV-01
  equal the R1 frozen main vectors; ID-N1..N5 pass; external regression
  mutation exits 1 (12 failures).
- Q2-F02: E02 routed through the provenance gate; 8/8 case definitions and
  PROV-1..8 pass; `expected.e02` is a compared frozen section; five external
  gate/authority mutations all exit 1.
- DEL01: counts 20/32 → 32 preserved with corrected canonical identities;
  DEL-N1..N3 and Snapshot B controls pass; reinjection mutation exits 1.
- Historical: 114/114 (Q1 pin) and 130/130 (R1 pin) reproduced from isolated
  archives; historical expected bytes verified against pins
  (`HISTORICAL-PRESERVATION.md`).
