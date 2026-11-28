# Target-framework and compatibility remediation evidence

Supports
[ANDIRIT-CI1A-S2.6-WP-S64-OCC-ID-REMED-01-R2.md](../ANDIRIT-CI1A-S2.6-WP-S64-OCC-ID-REMED-01-R2.md).

**Disposition: A — framework and compatibility remediation implemented — ready
for independent requalification.**

## Contents

| Path | Purpose |
| --- | --- |
| `baseline.md` | R1/qualification baseline, seals and authorization |
| `framework-contract.md` | Supported TFM matrix, selection, refusal and content rules |
| `implementation.md` | Resolver, compilation consistency, generation 3 and preflight |
| `negative-controls.md` | Reproduced defects and corrected results |
| `c4-oracle.md` | Target-framework C4 ledger, freeze chronology and historical delta |
| `compatibility.md` | Non-mutating store refusal and reconstruction |
| `cross-host.md` | Local and hosted cross-host/platform results |
| `evidence/` | Probe, oracle, compatibility, coverage, package and hosted receipts |
| `expected/` | Frozen target-framework C4 ledger and variants |
| `tools/independent-c4-oracle/` | Independent ledger generator |
| `seal.py`, `manifest.json` | Self-excluding remediation seal |

## Result

- Declared TFM selects installed reference packs; consumer runtime no longer does.
- net8.0/net9.0/net10.0/netstandard2.1 supported; netstandard2.0 and others refused.
- Actual Roslyn references match the identity descriptor.
- Generation-3 marker distinguishes corrected stores from R1 generation-2 stores.
- Read-only preflight refuses incompatible stores before schema/replay with no
  main/WAL mutation.
- C4 target-framework ledger independently derived and frozen before comparison:
  target `23ffa6…ab68`, 51 facts, 50/1, two contributors, four variants.
- 1,059 local and hosted tests pass; 29 mirror tests and 19 S6.3 named cases pass;
  coverage floors and package smoke pass.
- Hosted Windows x64, Linux x64 and macOS ARM64 jobs succeeded on a tree identical
  to the candidate; the receipt is a synthetic PR merge commit, not an exact
  candidate checkout.

## Seal

```text
python docs/reviews/ANDIRIT-CI1A-S2.6-WP-S64-OCC-ID-REMED-01-R2-evidence/seal.py verify HEAD
```

The seal adds only `manifest.json`. All predecessor and R1 seals are preserved.
