# Stage 6 obligation inventory — ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01

**Date**: 2026-11-22
**Scope**: every governing obligation that touches Stage 6, its owner, its current state at the
baseline `b5a81f81fade9807f706b4586329efe416cf1558`, and its S6.1 assignment.
**Sources**: byte-exact excerpts in `ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-evidence/contract-reconciliation.txt`.

This inventory is derived from existing contracts only. It introduces no new requirement and closes
no obligation: the Stage-2 acceptance and all deferred obligations remain exactly as recorded.

---

## 1. Deferred obligations (owner: Stage 6)

From the residual-risk register of `M54-OWNER-01` (lines 39–50) and the Stage-6 entry proposal
(lines 9–20). Every DF item except DF-7 is classified as *qualification of existing behavior*.

| ID | Deferred item | Nature | S6.1 assignment |
| --- | --- | --- | --- |
| DF-1 | Full D3 real-fixture part (native-mirror full corpus) | qualification of existing behavior | **out** — later work package (S6.4) |
| DF-2 | D4.5 full native fault matrix | qualification of existing behavior | **partly in** — S6.1 qualifies the *cancellation* case of D4.5 (D4 invariant 5); the remaining fault-matrix rows stay with the later package |
| DF-3 | Full S-02 ten-invariant native matrix (applicable subset qualified) | qualification of existing behavior | **partly in** — S6.1 qualifies the cancellation path and the unknown-outcome case; the complete matrix is a later package (S6.3) |
| DF-4 | F-02 crash/power-loss/multi-process durability | qualification of existing behavior | **out** — later work package (S6.2); unchanged by S6.1 (Q-S61-8) |
| DF-5 | Q2-F09 (rebuild blocks queries / no cancellation), Q2-F12, I-04 cancellation | qualification of existing behavior | **partly in** — S6.1 qualifies the cancellation path (I-04, Q2-F09 cancellation aspect); Q2-F12 and the rebuild-blocking aspect are not cancellation-path cases and remain deferred |
| DF-6 | Full platform certification (other distributions/architectures; internal locking/WAL/mmap instrumentation) | qualification of existing behavior | **out** — later work package (S6.5) |
| DF-7 | WP-S24-3 optional cross-target scope; public Find Usages; optimization | optional capability — separate owner commissions (unauthorized) | **out — unauthorized** |
| DF-8 | M9.5 cancellation path; M12.1–M12.3 full native matrix | named deferral (Stage 6) | **partly in** — S6.1 takes M9.5 (dedicated native cancel-path test) and the M12.2 row (D4 invariant 5, native); M12.1 and M12.3 remain with the later full-matrix package |

## 2. Requirement-level obligations

| ID | Requirement (source wording) | State at baseline | S6.1 assignment |
| --- | --- | --- | --- |
| **D4 invariant 5** (owner decision §D.4) | *"Cancelled staging must not expose partial facts or index entries."* | accepted as an invariant; the native case explicitly deferred (S2.5 matrix M12.2) | **in** — the native cancelled-staging case and its non-exposure proof |
| **D4 qualification clause** (owner decision §D.4) | *"CI-1A must reproduce the applicable stale-index, cancellation, failed-publication, supersession and reopen tests using actual production storage and APIs. The previous in-process experiment does not establish native or cross-process atomicity; record those limits explicitly."* | applicable subset qualified in Stage 2; cancellation case outstanding | **in** — cancellation case against production storage, with the interruption limit recorded explicitly |
| **S-02** (S-01 obligation reconciliation) | All ten D4 invariants exercised against the actual production `LatticeCodeGraphStore`; Stage 6 native matrix to include *"supersession stress, true concurrent publication, cancellation path, unknown outcome"* | open — Stage 6 obligation | **partly in** — cancellation path and unknown-outcome case; the full matrix is a later package |
| **S-02 specification** (CI1A specification, line 194) | *"All ten invariants must be exercised against actual production `LatticeCodeGraphStore` storage, including failed/cancelled/superseded publication, reopen and a native concurrency/republication scenario. In-process deterministic interleaving is retained as a diagnostic only and is not accepted as native atomicity evidence."* | open | **partly in** — the cancelled-publication case; note the rule that deterministic interleaving is diagnostic only |
| **M9.5** (S2.5 qualification matrix, line 90) | Stage-1 cancellation behavior preserved — *"Dedicated native cancel-path test"*; evidence class `E / no`; owner Stage 6; receipt: record Stage 6 referral | deferred with a named owner; claiming completion is blocked | **in** — this is the primary S6.1 deliverable |
| **M12.2** (S2.5 qualification matrix, line 122) | D4 invariant 5 — *"Explicitly deferred to Stage 6"*; *"Cancel path test native"* | deferred with a named owner | **in** |
| **M12.1 / M12.3** (S2.5 qualification matrix, lines 121, 123) | D4 invariants 1–4, 6–9 and 10 (snapshot/concurrent publication) — full native matrix | deferred with a named owner | **out** — later full-matrix package |
| **I-04** (CI1A obligation register, line 216) | *"Simple publication-bound maintenance and serialized policy fixed; cancellation remains unqualified."* | open — Stage 6 | **in** — the cancellation policy is specified as part of S6.1 |
| **Q2-F09** (owner decision §G.2; register line 227) | *"C rebuild blocks queries; no cancellation"* — *"addressed by D4 (maintenance policy; CI-1A must qualify a production-backed rebuild path)"* | open — Stage 6 | **partly in** — the cancellation aspect only; the rebuild-blocking aspect remains deferred |
| **Q2-F12** (owner decision §G.2; register line 230) | *"Crash/power-loss/multi-process/cross-platform not demonstrated"* | open — Stage 6 | **out** — unchanged (Q-S61-8) |
| **F-02** | Managed fault injection is not crash/power-loss/abrupt-kill/multi-process durability | open — Stage 6 | **out** — unchanged |
| **SQ-05 / §14** | Full Windows/Linux/macOS certification | Linux minimum qualified in Stage 2; full certification deferred | **out** — DF-6; but S6.1's own Linux evidence row must use genuine hosted Linux |

## 3. Stage 6 work-package map (from the entry proposal)

| Work package | Scope | Relationship to S6.1 |
| --- | --- | --- |
| **S6.1** | native cancellation + applicable D4.5/S-02 subset | **this commission's boundary** |
| S6.2 | durability (crash/power-loss/multi-process) | consumes S6.1's cancel-path receipts; not begun |
| S6.3 | full native matrices (D4.5 fault matrix, complete S-02) | consumes S6.1 and S6.2; not begun |
| S6.4 | real-fixture D3 | consumes the accepted corpus and the D4/D5 qualifiers; not begun |
| S6.5 | platform certification matrix | consumes S6.1–S6.4; not begun |

Each package carries its own qualification/acceptance gate; **no package grants release**, and no
Stage-6 work package may waive a Stage-2 qualification.

## 4. Obligations explicitly *not* touched by S6.1

M9.1–M9.4 and M9.6–M9.10 (stale-index, schema availability, bounded `Examined`, NEW-01 recovery,
supersession, reopen equivalence, union/deletion/conflict, ordering/limits, snapshot serialization),
M8.1–M8.4 (graph identity invariants), M13.x (master-provenance copies), M14.x (Linux CI expansion),
S-01 canonicalization proof, D3 full, D5 schema/refusal, and every published package identity.

## 5. Protected obligations carried into S6.1

Q-L1…Q-L8, MR-1…MR-4, GH-1…GH-4 and DF-1…DF-8 remain in force; the osx-arm64 runtime limitation
(content evidence only) remains; the accepted merge, identity, refusal and accounting semantics, the
historical regression suites, the coverage thresholds and the CI gates are unchanged.
