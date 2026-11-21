# Evidence package — ANDIRIT-CI1A-S2.5-WP-S25-4-M54-B-QUAL-01

**Class**: REVIEWER (independent qualification) evidence.
**Subject**: M5.4 qualification under the amended contract
(`docs/decisions/adr-ci1-01-ci1a-s2-5-m54-amend-owner-01.md` §6) at the pinned implementation
revision `eafd632b...` / tree `823e3c65…`.
**Report**: `../ANDIRIT-CI1A-S2.5-WP-S25-4-M54-B-QUAL-01.md` (bound by Git).

## Outcome

**11/11 exercises passed** (0 failed, 0 skipped); **all 11 in-process refusal-path branches
hit**; **no prohibited-direction outcome**; **no Option A escalation condition triggered**.
Formal disposition: **SUPPORTED** (the qualification supports the amended criteria; the unproven
reachability/association items are recorded as residual limitations per the amendment, not as
open gates).

## Package map

| Path | Content |
| --- | --- |
| `README.md` | This file |
| `reproduce.md` | Reproduction instructions and expected results |
| `environment.txt` | Toolchain/environment identity of the harness execution |
| `qualification-run.log` | Raw execution log (11 passed) |
| `trx/m54b-results.trx` | Test results |
| `coverage/coverage.cobertura.xml` | Coverlet artifact over the pinned production assemblies |
| `receipts/` | Per-exercise JSON receipts (11) + refusal-path coverage receipt |
| `harness/` | Reviewer-owned harness (two projects + runner + verifier); see `reproduce.md` §4 |
| `matrix.md` | Requirement-to-evidence disposition matrix |
| `findings.md` | Findings, residual limitations, recommended disposition |
| `manifest.json` | Self-excluding SHA-256 manifest of this package |

## Binding commands

1. Exact-byte export of `eafd632` + independent blob verification (4,555 blobs, 0/0/0).
2. `run-qualification.ps1 -RepoRoot <pinned export> -Evidence <work>` — builds and executes the
   harness with coverage, then independently verifies the refusal-path lines.
3. Rehash verification of this package against `manifest.json`.