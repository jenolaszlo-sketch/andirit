# Occurrence identity remediation evidence

Supports
[ANDIRIT-CI1A-S2.6-WP-S64-OCC-ID-REMED-01](../ANDIRIT-CI1A-S2.6-WP-S64-OCC-ID-REMED-01.md).
Disposition **B — reconciliation supported with outstanding requirements**.

## Contents

| Path | Purpose |
| --- | --- |
| `baseline-verification.txt` | Seal verification and reproduction values |
| `identities.json` | Exact baseline, seals and observed identities |
| `root-cause.md` | Reference-set/identity root cause and classification |
| `contract-reconciliation.md` | `compctx1`/`compctx2`/`asmctx3`/`usage4` history and boundary |
| `cross-host.md` | Console/xUnit observations and deferred campaign items |
| `compatibility.md` | Persisted/published identity impact; no compatibility claimed |
| `c4-oracle-status.md` | Derived-but-unfrozen C4 ledger |
| `R1-06-owner-exception.md` | Narrow historical packaging exception |
| `findings-gates.md` | Findings and O-G1…O-G13 |
| `evidence/production-descriptor.txt` | Production `compctx2` descriptor/reference set |
| `evidence/host-probe-console.json` | Console-host production target/cardinality |
| `evidence/c4-oracle-unvalidated.json` | Unvalidated oracle ledger |
| `evidence/c4-oracle-variants/` | Removal/caller-change variants |
| `evidence/C4OccurrenceProbe.cs` | Reproduction source preserved (not committed to tests) |
| `seal.py`, `manifest.json` | Self-excluding remediation seal |

## Result

- Divergence reproduced across console/xUnit/oracle hosts.
- `compctx2` is authorized and not the defect; target generation uses the
  `compctx1` mapping.
- The defect is ambient process-TPA discovery plus absolute-path reference
  identity under a contract inconsistency with the frozen host-independent
  oracle.
- Correction is an incompatible persisted identity transition; not implemented.
- C4 occurrence ledger is derived but not frozen.
- No production/test change; inventory 1,034.

## Seal

```text
python docs/reviews/ANDIRIT-CI1A-S2.6-WP-S64-OCC-ID-REMED-01-evidence/seal.py verify HEAD
```

The seal adds only `manifest.json`.
