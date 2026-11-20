# WP-S25-4 closure checklist (current state)

**Updated:** 2026-11-21, after the M5.4 contract clarification (`adr-ci1-01-ci1a-s2-5-m54-amend-owner-01.md`).
**Read with:** the [obligation inventory](wp-s25-4-obligation-inventory.md), the
[qualification matrix](wp-s25-4-qualification-matrix.md), the
[entry owner record](../../reviews/ANDIRIT-CI1A-S2.5-WP-S25-4-ENTRY-OWNER-01.md), and the cited
decisions. This checklist is additive status; it grants nothing and closes nothing.

Legend: ✅ closed/accepted · 🔄 amendment-approved, pending qualification · ⛔ blocked (separate commission) · ⏸ open (separate owner decision)

## A. Repository quality gates (WP-S25-4)

| Item | State | Evidence |
| --- | --- | --- |
| Exact-byte formatting gate (QUAL-04) | ✅ **CLOSED (repository level)** | GATE-QUALIFICATION-01; `adr-ci1-01-ci1a-s2-5-wp-s25-4-gate-owner-01.md` §2 |
| F-G2-01 LatticeDb coverage gate (368/466 = 78.96%; 900/969 = 92.87%) | ✅ **CLOSED (repository level)** | Same; producer-evidence findings F-01…F-06 preserved |
| Full CI matrix green on the qualified implementation | ✅ demonstrated (run 44746950426, all four jobs) | G2/G3 packages; G4 acceptance |
| R1/R2/R3 and Q03-F-01 qualified behavior preserved | ✅ | QUAL-01…04 + GATE-QUALIFICATION-01 preservation receipts |
| Frozen R2 oracle byte-identical (185/185) | ✅ | GATE-QUALIFICATION-01 |

## B. Cross-platform obligations

| Item | State | Evidence |
| --- | --- | --- |
| M11.4 | ✅ **ACCEPTED WITH QUALIFICATIONS** (Ubuntu 26.04.1 x64 / SDK 10.0.404 demonstrated scope; managed-fault-only; uninstrumented internals; receipt-based independence) | `adr-ci1-01-ci1a-s2-5-wp-s25-4-ubuntu-owner-01.md` |
| M14.1 | ✅ **ACCEPTED WITH QUALIFICATIONS** (75/0/0, no filter, five-identity equivalence) | Same |
| M14.2 | ✅ **ACCEPTED WITH QUALIFICATION** (suite-level parity; internals uninstrumented) | Same |
| SQ-05 | ✅ current-scope satisfaction re-based (52-test acceptance preserved as historical, superseded for current scope) | Same + `adr-ci1-01-ci1a-s2-5-wp-s25-4-ubuntu-findings-disposition-01.md` |
| Historical findings cleanup | ⏸ recommended errata/reconciliation records (G3-F01 PR #6 merge; G3-F02 SQ-05 G2 package completeness) | Findings-disposition record §G3-F01/F02 |

## C. Refusal and coverage obligations

| Item | State | Evidence |
| --- | --- | --- |
| M5.4 (`Unresolved` refusal — missing defining compilation/universe) | 🔄 **AMENDMENT-APPROVED — PENDING INDEPENDENT QUALIFICATION** | `adr-ci1-01-ci1a-s2-5-m54-amend-owner-01.md`; commission `ANDIRIT-CI1A-S2.5-WP-S25-4-M54-B-QUAL-01` (prepared, **not authorized to execute**); on success → owner satisfaction record |
| M5.1–M5.3, M5.5–M5.8 (conservative refusal/coverage) | ✅ qualified (QUAL-01…04; membership/availability evidence) | QUAL-04 matrix row dispositions |
| SQ-06 remainder (isolated meta.Namespace refusal) | ⛔ **BLOCKED/open** — register row requires the isolated tamper test; carried as an open S2.5 obligation | `wp-s25-4-obligation-inventory.md` SQ-06 row |
| S-01 integrated proof | ✅ qualified (WP-S25-3 closed with qualifications) | WP-S25-3 closure record |

## D. Stage-2 closure prerequisites (all separate owner decisions)

- [ ] **WP-S25-4 final integrated qualification** — remaining after C: execute `M54-B-QUAL-01` (owner authorization), then the WP-S25-4 final-closure owner decision reconciling the full matrix.
- [ ] **Stage-2 final integration + Stage-2 owner acceptance** — must restate: the closed gates (format, F-G2-01), the M11.4/M14.1/M14.2 accepted qualifications and demonstrated Linux environment scope, the M5.4 amended basis + conservative false-negative limitation + unproven-reachability statement, the SQ-04 amended-contract qualifications, F-01/F-02, and the G3-F01/F02 findings. **"Reachability proven" must not be recorded.**
- [ ] **Stage-6 items (unauthorized, unaffected)**: full D3 real-fixture part, D4.5, full S-02 native matrix, crash/power-loss durability (F-02), full platform certification.
- [ ] Reserve: WP-S24-3 (optional, deferred, unauthorized); public Find Usages (unauthorized); optimization (unauthorized); migration/dual-write (forbidden, D5).

## E. Remote-resource state (as of 2026-11-21)

- PR #7 (`qualification/wp-s25-4-sq05-linux-r2`, head `eafd632`): **open, unmerged** — evidence anchor for the corrected-revision Linux qualification; disposition at Stage-2 acceptance or a separate bounded decision.
- PR #6 (`qualification/ci1a-s25-sq05-linux`, head `7457284…`): **merged 2026-11-10T23:05:29Z** (fast-forward) after the SQ-05 G3 assessment recorded it open — owner reconciliation recommended (G3-F01); branch retained.
- `origin/main` unchanged at `48370711…`.