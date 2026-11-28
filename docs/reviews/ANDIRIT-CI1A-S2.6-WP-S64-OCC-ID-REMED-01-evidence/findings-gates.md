# Findings and gate matrix

## Findings

| Finding | Status |
| --- | --- |
| OCC-ID-01 contract inconsistency | S2.1 authorizes `file|<absolute path>|sha256` and process-TPA references; S2.0/F12 requires host-independent content-bound runtime references. **Open; owner transition decision required** |
| OCC-ID-02 ambient host references | Production descriptors include testhost/xunit/consumer assemblies; the same source yields different target identities by host. **Confirmed** |
| OCC-ID-03 absolute path identity | Byte-identical references at different install paths change identity. **Confirmed by code and descriptor** |
| OCC-ID-04 cross-host campaign | Console and xUnit hosts executed; isolated consumer, alternate paths, Linux/macOS and semantic negative controls **not executed** |
| OCC-ID-05 compatibility transition | Changing reference identity changes persisted occurrence identities; no migration/rebuild contract authorized. **Owner decision required** |
| R1-01 C4 exact ledger | **Open**; derived but not frozen against a reconciled contract |
| R1-04 H1 | **Not executed** |
| R1-05 D3 implementation | **Not executed** |
| R1-06 | **Owner exception recorded**; content integrity remains supported |
| F-S64E-03 / DF-1 | **Open** |
| F-S64E-04 | **Open** |
| F-S64E-05 | **Open** for D3 |

## Gate matrix

| Gate | Requirement | Result |
| --- | --- | --- |
| O-G1 | Baseline and predecessor seals verified | **Pass** |
| O-G2 | Reproduce host identity divergence | **Pass** |
| O-G3 | Root-cause analysis | **Pass** |
| O-G4 | Contract/decision-history reconciliation | **Pass** |
| O-G5 | Bounded correction implemented | **Not implemented**; incompatible identity transition needs owner authorization |
| O-G6 | Cross-host determinism campaign | **Partial**; console/xUnit only |
| O-G7 | Semantic-difference negative controls | **Not executed** in production host |
| O-G8 | Existing-store/published compatibility | **Assessed; not compatible without transition; no compatibility claimed** |
| O-G9 | Completed C4 occurrence ledger | **Not frozen**; blocked on O-G5 |
| O-G10 | Regression inventory | Unchanged 1,034; no production change |
| O-G11 | R1-06 owner exception | **Recorded** |
| O-G12 | Sealed producer evidence | **Pass after seal** |
| O-G13 | Proposed qualification commission | **Pass** |

## Disposition

**B — OCCURRENCE IDENTITY RECONCILIATION SUPPORTED WITH OUTSTANDING REQUIREMENTS.**

The correct boundary is established, but implementing it requires an
incompatible identity transition. No correction, migration, D3 campaign or
identity freeze is claimed.
