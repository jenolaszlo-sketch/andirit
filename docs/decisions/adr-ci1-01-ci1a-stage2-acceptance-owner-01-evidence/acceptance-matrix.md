# Consolidated Stage-2 acceptance matrix — STAGE2-ACCEPTANCE-OWNER-01

**Recorded:** 2026-11-22 with `adr-ci1-01-ci1a-stage2-acceptance-owner-01.md`.
Consolidates the binding Stage-2 acceptance matrix (planning) and the S2.5 qualification
matrix row dispositions into one acceptance record. No accepted work is reopened; no Stage 6
obligation is discharged.

## 1. Stage-2 acceptance matrix (planning contract) — reconciliation

| Requirement (stage2-boundary §12) | Stage-2 deliverable | Evidence | Acceptance state |
| --- | --- | --- | --- |
| S-01 provenance | Actual compilation-input proof | S2.1/WP-S25-3 integrated proof; frozen R2 oracle byte-identical | **Accepted with qualifications** |
| `usage4`/`asmctx3` identity | Production semantic binding | S2.2 minting; M54-B-QUAL-01 target-id independence | **Accepted with qualifications** |
| Cross-project binding | Verified defining compilation | B3 (QUAL-04); L04/L05 lifecycle tests on `eafd632` | **Accepted with qualifications** |
| Source ownership | Real extraction and replacement | R1/R2/R3 (QUAL-01…04); GATE-QUALIFICATION-01 preservation | **Accepted with qualifications** |
| Reader contract | Internal semantic query integration | S2.4 acceptance (WP-S24-1/2, S24-FINAL-Q1) | **Accepted with qualifications** |
| Coverage/refusal | Explicit semantic coverage | M5.1–M5.8 qualified; IF-03; M5.4 amended contract | **Accepted with qualifications** |
| D3 | Real-project preservation | M8.1–M8.4 (QUAL-04); native-mirror parity (M14.2 scope) | **Accepted with qualifications** (full D3 deferred DF-1) |
| D4 | Preserve applicable invariants | M9.1–M9.10 (QUAL-04); native publication/reopen | **Accepted with qualifications** (full matrix deferred DF-2/DF-8) |
| D5 | No incompatible re-interpretation | M10.1–M10.6; SQ-04 amended boundary; D5 refusal layers | **Accepted with qualifications** |
| SQ-02 (unknown outcome) | Documented conservative behavior | M9.4; NEW-01 recovery-projection tests | **Accepted with qualifications** |
| SQ-03 (concurrency) | Serialized single-writer | Q-L6 qualification | **Accepted with qualifications** |
| SQ-06 remainder | meta.Namespace refusal | M15.1 PASS (QUAL-04); final-owner-01 §3 | **Satisfied** |
| Performance/operational | Publication/reopen/query costs | SQ-07 recorded measurements (no new threshold) | **Accepted with qualifications** |
| Cross-platform (SQ-05) | Expanded Linux native suite | UBUNTU-OWNER-01; run 44746950426 | **Accepted with qualifications** (full certification DF-6) |

## 2. S2.5 qualification matrix (64-row) — final disposition

46 PASS · 10 PASS-WITH-QUALIFICATION · 0 FAIL · 1 REUSED QUALIFIED · 4 DEFERRED. The three
QUAL-04 BLOCKED rows are dispositioned:

| Row | Former blocker | Final disposition | Decision record |
| --- | --- | --- | --- |
| M5.4 | reachability unresolved | **SATISFIED UNDER AMENDED CONTRACT** (MR-1…MR-4 binding) | `m54-amend-owner-01.md`, `m54-owner-01.md` |
| M11.4 | corrected-revision genuine Ubuntu absent | **ACCEPTED WITH QUALIFICATIONS** (Ubuntu 26.04.1 x64, 75/0/0) | `…-ubuntu-owner-01.md` |
| M14.1 | current expanded suite on Linux | **ACCEPTED WITH QUALIFICATIONS** (75-case suite, five-identity equivalence) | `…-ubuntu-owner-01.md` |
| M14.2 | platform behavior record | **ACCEPTED WITH QUALIFICATION** (suite-level parity; internals uninstrumented) | `…-ubuntu-owner-01.md` |

## 3. Repository quality gates (Stage-2 acceptance consequence)

| Gate | Policy | Corrected-revision result | Evidence |
| --- | --- | --- | --- |
| Exact-byte formatting | `dotnet format Andirit.slnx --no-restore --verify-no-changes` | exit 0 (byte-exact export; SDK 10.0.401) + green in run 44746950426 | GATE-QUALIFICATION-01; `…-gate-owner-01.md` |
| LatticeDb coverage | ≥70 line and branch (`ci.yml`) | 900/969 = 92.87% lines; 368/466 = 78.96% branches; exit 0 | GATE-QUALIFICATION-01; genuine-Ubuntu run |
| Core / CSharp / TreeSitter coverage | 55 / 75 / 60 | pass in run 44746950426 (recorded CI job summary) | `…-ubuntu-owner-01.md` §4 |
| Whole-solution tests | all pass | 949 passed / 0 failed / 0 skipped (`eafd632`); green matrix on Ubuntu | GATE-QUALIFICATION-01; run 44746950426 |
| Pack | success | pass in run 44746950426 | `…-ubuntu-owner-01.md` §4 |

## 4. Acceptance boundary

Stage 2 acceptance is bounded to the qualified scope of §1–§3 and the qualifications of the
acceptance record §5–§7. Not accepted by this record: Stage 6 obligations (DF-1…DF-8),
WP-S24-3, public Find Usages, optimization, migration/dual-write, or any platform/generation
beyond the demonstrated Linux environment.