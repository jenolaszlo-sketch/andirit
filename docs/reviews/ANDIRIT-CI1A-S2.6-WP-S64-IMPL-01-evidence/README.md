# S6.4 D3 implementation evidence

Supports [ANDIRIT-CI1A-S2.6-WP-S64-IMPL-01](../ANDIRIT-CI1A-S2.6-WP-S64-IMPL-01.md).
Disposition **B — implementation supported with outstanding requirements**.

## Contents

| Path | Purpose |
| --- | --- |
| `baseline-verification.txt` | Re-entry seal and corpus validation |
| `identities.json` | Exact baseline and re-entry identities |
| `blocker/C4-OCCURRENCE-BLOCKER.md` | R1-01 contract mismatch and owner options |
| `blocker/production-descriptor.txt` | Production `compctx2` descriptor and reference set |
| `blocker/C4OccurrenceBlockerTests.cs` | Reproduction test (not committed to the test project) |
| `blocker/D3OccurrenceOracle.*`, `D3Campaign.*` | Independent C4 oracle and production campaign source |
| `blocker/c4-occurrences*.json` | Unvalidated C4 oracle ledger and removal/change variants |
| `blocker/observed-attempt.json` | Observed query attempt |
| `R1-06-adjudication.md` | Prior-seal packaging decision |
| `df1-evidence-matrix.md` | DF-1 requirement status |
| `findings-gates.md` | Updated findings and I-G1…I-G11 |
| `seal.py`, `manifest.json` | Self-excluding implementation seal |

## Result

- The 69-file re-entry seal verifies before implementation.
- The C4 graph ledger is valid; the exact occurrence ledger is **blocked** by the
  production `compctx2` / frozen `compctx1` identity mismatch.
- D3-01…08, raw mirror/H1/failure/reopen campaigns were not executed.
- Regression inventory remains 1,034; no production or test code changed.
- DF-1 and S6.4 remain open.

## Reproduce the blocker

```text
# C4 oracle (independent)
dotnet build blocker/D3OccurrenceOracle.csproj -c Release
dotnet blocker/bin/Release/net10.0/D3OccurrenceOracle.dll <c4-root> out.json baseline

# Production comparison (test source preserved under blocker/)
# Restore C4OccurrenceBlockerTests.cs into tests/Andirit.CSharp.Tests temporarily,
# run the single test, and inspect the target/descriptor diagnostics.
```

The test must not be committed while it fails; it is preserved as evidence until
the identity contract is reconciled.

## Seal

```text
python docs/reviews/ANDIRIT-CI1A-S2.6-WP-S64-IMPL-01-evidence/seal.py verify HEAD
```

This seal adds only `manifest.json`; it does not reproduce the R1-06 anomaly.
