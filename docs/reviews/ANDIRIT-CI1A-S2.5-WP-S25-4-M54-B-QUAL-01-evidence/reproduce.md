# Reproduction — M54-B-QUAL-01

Reviewer-owned harness. All commands run on Windows x64 against the **pinned exact-byte export**
of the qualified implementation revision (method established by QUAL-04/GATE-QUALIFICATION-01).

## 1. Pin the implementation (exact bytes, no normalization)

```powershell
git -C D:\projects\andirit -c core.autocrlf=false archive --format=tar eafd632b... > production.tar
# extract with Python tarfile into <work>\production, then verify every tracked blob:
python verify-exports.py D:\projects\andirit eafd632b... <work>\production
# expected: git entries=4555 exported=4555 missing=0 extra=0 mismatched=0
```

(The independent verifier is preserved in
`docs/reviews/ANDIRIT-CI1A-S2.5-WP-S25-4-GATE-QUALIFICATION-01-evidence/verify-exports.py`.)

## 2. Build and run the qualification harness

```powershell
# from the package harness/ directory:
powershell -ExecutionPolicy Bypass -File run-qualification.ps1 -RepoRoot <work>\production -Evidence <work>\evidence
```

The runner: (1) builds the reopen host and the test host against `$(RepoRoot)`; (2) executes all
pre-registered exercises with coverlet instrumentation
(`/p:Include=[Andirit]%2c[Andirit.CSharp]%2c[Andirit.LatticeDb]`, no threshold gate — the
harness intentionally exercises only the refusal paths); (3) runs the independent refusal-path
line verifier.

## 3. Expected results

| Receipt | Key expectation |
| --- | --- |
| `receipts/e1a.json` | `MissingDefiningCompilation` at resolve/mint/usage; no fact; `RequireCompilation` refuses |
| `receipts/e1b.json` | both construction guards refuse with `MissingDefiningCompilation` |
| `receipts/e1c.json` | `Incomplete` (missing scope unit / empty universe) distinct from `MissingDefiningCompilation` |
| `receipts/e2.json` | all six non-Complete statuses refuse the mint; `mintedAny: false` |
| `receipts/e2p.json` | positive control mints; target id equals the reviewer's independent computation |
| `receipts/e3a.json` | 1 fact; external=1; unresolved=1; unsupportedCategories=1; authority recorded |
| `receipts/e3b.json` | distinct-process reopen, authority-bound: `Available`, fact id preserved, counters preserved |
| `receipts/e3b2.json` | partial coverage: `IncompleteAnalysis` with the truthful refusal reason |
| `receipts/e3c.json` | authority-masking tamper: `AnalysisUnavailable`, 0 occurrences |
| `receipts/a4.json` | incomplete universe: no authority envelope, no facts, Partial coverage |
| `receipts/a4b.json` | `PrepareNativeUnit` refuses with `MissingVerifiedOccurrenceAuthority` |
| `receipts/refusal-path-coverage.json` | `allTargetsHit: true` (11 in-process refusal branches) |

Environment: `environment.txt`. Raw execution log: `qualification-run.log`. Test results:
`trx/m54b-results.trx` (11 passed / 0 failed / 0 skipped). Coverage artifact:
`coverage/coverage.cobertura.xml`.

## 4. Harness layout

| Path | Role |
| --- | --- |
| `harness/M54BHarness.Tests.csproj` | xUnit test host (AssemblyName `Andirit.CSharp.Tests`, reserved reviewer identity) |
| `harness/M54BReopen/M54BReopen.csproj` | distinct-process reopen host (same identity; public store API + the live authority boundary) |
| `harness/UniverseFactory.cs` | construction helpers + receipt emitter |
| `harness/SharedFixture.cs` | deterministic admissible-universe fixture (complete and standalone variants) |
| `harness/M54BExercises.cs` | E1 (isolated refusal), E2 (mint-gate screen), E2p (positive control), E3a (extraction), A4 |
| `harness/M54BStoreExercises.cs` | E3b/E3b2/E3c (native store chain), A4b (publication guard), durable-row tamper |
| `harness/run-qualification.ps1` | runner |
| `harness/verify-refusal-lines.py` | independent refusal-path line verifier |

Nothing in the harness modifies production code, canonical tests, dependencies, or CI policy.