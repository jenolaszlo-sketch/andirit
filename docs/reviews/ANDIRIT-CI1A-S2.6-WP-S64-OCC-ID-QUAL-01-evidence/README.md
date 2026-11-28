# OCC-ID transition independent qualification evidence

Supports
[ANDIRIT-CI1A-S2.6-WP-S64-OCC-ID-QUAL-01-REPORT.md](../ANDIRIT-CI1A-S2.6-WP-S64-OCC-ID-QUAL-01-REPORT.md).

**Disposition: C — NOT SUPPORTED.** The version transition and seal largely
reconcile, but the framework reference-resolution gate fails: the declared target
framework is ignored and a host-runtime implementation-assembly closure is used.

## Contents

| Path | Purpose |
| --- | --- |
| `evidence/seal-verification.json` | Independent Git-object seal verification |
| `evidence/framework-adjudication.json` | Target-framework closure finding |
| `evidence/framework-probe.json` | net10/net8/netstandard probe output |
| `evidence/c4-adjudication.json` | Independent C4 result and reference-scope comparison |
| `evidence/c4-independent.json` | Full independently derived 51-fact ledger |
| `evidence/persisted-compatibility.json` | Predecessor refusal and adversarial controls |
| `evidence/reconstruction.json` | New-store reconstruction and loss limits |
| `evidence/regression-inventory.json` | 1,034 → 1,044 reconciliation, 29/19, coverage, package |
| `evidence/hosted-provenance.json` | Synthetic-merge, runner architecture and counts |
| `evidence/independent-release-tests.log` | Independent full Release run |
| `evidence/independent-pack.log`, `independent-smoke.log` | Independent package smoke |
| `evidence/predecessor-*`, `successor-*` | Raw predecessor/successor store inspection and query |
| `tools/` | Independent qualification harnesses and verification scripts |

## Seal

```text
python docs/reviews/ANDIRIT-CI1A-S2.6-WP-S64-OCC-ID-QUAL-01-evidence/seal.py verify HEAD
```

The seal adds only `manifest.json` and does not modify predecessor or producer
seals.
