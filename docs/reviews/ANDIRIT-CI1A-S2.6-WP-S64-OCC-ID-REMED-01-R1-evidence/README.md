# Occurrence identity transition evidence

Supports
[ANDIRIT-CI1A-S2.6-WP-S64-OCC-ID-REMED-01-R1](../ANDIRIT-CI1A-S2.6-WP-S64-OCC-ID-REMED-01-R1.md).
Disposition **A — versioned identity transition implemented — ready for
independent qualification**.

## Contents

| Path | Purpose |
| --- | --- |
| `baseline.md` | Investigation baseline, predecessor seals, owner authorization |
| `transition-contract.md` | Successor domains, formulas, framework closure and compatibility |
| `implementation.md` | Production changes, generation detection, reconstruction, test additions |
| `cross-host.md` | Console/xUnit, cross-path, framework closure and hosted results |
| `c4-oracle.md` | Independent C4 oracle, frozen values and variants |
| `compatibility.md` | Persisted/published boundary and re-ingestion |
| `findings-gates.md` | Finding closure and R1 gate matrix |
| `identities.json` | Machine-readable baseline, candidate and successor identities |
| `expected/c4-occurrences.json` | Frozen C4 baseline ledger |
| `expected/c4-oracle-variants/` | Frozen removal and caller-change ledgers |
| `tools/c4-successor-oracle/` | Independent ledger generator |
| `tools/console-host-probe/` | Console-host reproduction |
| `evidence/host-probe-console.json` | Console-host target/cardinality/reference set |
| `evidence/local-release-tests.log` | Local Release test run |
| `evidence/local-regression-summary.txt` | Test and coverage summary |
| `evidence/local-csharp-coverage.log` | CSharp coverage run |
| `evidence/local-package-smoke.log` | Pack/consume smoke |
| `seal.py`, `manifest.json` | Self-excluding evidence seal |

## Result

- Deterministic managed runtime reference closure; no process TPA.
- Content-bound `refctx1` identity; no absolute paths.
- Successor `compctx3`/`compctx4`/`asmctx4`/`usage5` domains.
- Persisted generation 2 detection and safe predecessor refusal.
- Historical stores preserved; reconstruction by re-ingestion into a new store.
- Independent C4 ledger: 51 facts, 50/1, two contributors; four frozen variants.
- 1,044 local tests pass; coverage thresholds and package smoke pass.
- Windows console/xUnit and cross-path targets agree; hosted Windows, Linux and
  macOS jobs succeeded (run 46132247172).

## Seal

```text
python docs/reviews/ANDIRIT-CI1A-S2.6-WP-S64-OCC-ID-REMED-01-R1-evidence/seal.py verify HEAD
```

The seal adds only `manifest.json`. It does not modify any historical seal.


