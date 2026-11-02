# R1 producer qualification results

## Oracle runs

- Build: `dotnet build docs/planning/adr-ci1-01-ci1a-s2-0/oracle/SemanticCorpusOracle.csproj --no-restore` — 0 warnings, 0 errors.
- R1 expected: `oracle-expected-r1.json`. Two separate process executions: **130/130 checks passed** each.
- Canonical JSON SHA-256, both processes: `0A7BB361509AEBEE70A1305679EE22C6C35ECC92E3B973C2A5D08E6854049CCC`.
- Output files: `oracle-r1-run-1.json`, `oracle-r1-run-2.json`; console logs: `oracle-r1-run-1.log`, `oracle-r1-run-2.log`.
- Historical baseline: extracted the Q1-pinned archive for `82f101c24e21828a41a30d4342ba73db9376e24e` to a temporary directory and ran its original oracle against its original expected file: **114/114 checks passed**. Result: `historical-oracle-82f101c.json`.

## Full solution regression

Executed `dotnet test Andirit.slnx`; all four projects passed with zero failures or skips. Individual reruns saved distinct TRX files under `trx/`.

| Test project | Passed | Failed | Skipped |
|---|---:|---:|---:|
| Andirit.LatticeDb.Tests | 43 | 0 | 0 |
| Andirit.TreeSitter.Tests | 157 | 0 | 0 |
| Andirit.Tests | 325 | 0 | 0 |
| Andirit.CSharp.Tests | 120 | 0 | 0 |
| **Total** | **645** | **0** | **0** |

## Focused correction results

- DEL01: authoritative deletion complete; missing required source incomplete; stale prior contribution rejected; surviving `App/Bulk.cs` facts preserved; repeated snapshot deterministic.
- OPT01: actual `CheckOverflow` differs; source bytes and parse options identical; options digest and App contexts change; defining target identity follows R2; baseline restoration reproduces.
- E02: target resolves in Roslyn; absent defining authority refuses; positive authority control changes eligibility.
- CLS01/N01: Roslyn reports CS0121 overload-resolution ambiguity; CLS02 reports CS0176 invalid invocation.
- S15: event member resolves as `E:Corpus.Core.Methods.Changed`; handler resolves as separate `M:Corpus.App.Sites.OnChanged`.
- Model coverage: complete-empty, incomplete, and NEW-04 results explicitly labeled as model expectations.

## Integrity and archive probes

The R1 verifier and clean archive/tamper results are recorded after the final source-bound manifest is built. The archive trust anchor is the manifest SHA-256 printed in the final handoff.
