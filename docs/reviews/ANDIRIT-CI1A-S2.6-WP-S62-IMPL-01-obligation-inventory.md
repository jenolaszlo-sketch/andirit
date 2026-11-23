# Stage 6 S6.2 obligation inventory — ANDIRIT-CI1A-S2.6-WP-S62-IMPL-01

**Date**: 2026-11-24
**Scope**: every governing obligation that touches S6.2, its owner, its current state at the
baseline `92bf73a61794fa3bccad53d23dd7e65c51807cd5`, and its S6.2 assignment.
**Sources**: byte-exact excerpts in `ANDIRIT-CI1A-S2.6-WP-S62-IMPL-01-evidence/probe/contract-reconciliation.txt`.

This inventory is derived from existing contracts only. It introduces no new requirement and closes
no obligation: the Stage-2 acceptance, S6.1 acceptance, and all deferred obligations remain exactly
as recorded.

---

## 1. Deferred obligations (owner: Stage 6)

From the residual-risk register of the Stage 6 entry assessment and the S6.1 acceptance record.
Every DF item except DF-7 is classified as *qualification of existing behavior*.

| ID | Deferred item | Nature | S6.1 assignment | S6.2 assignment |
| --- | --- | --- | --- | --- |
| DF-1 | Full D3 real-fixture part (native-mirror full corpus) | qualification of existing behavior | **out** — later work package (S6.4) | **out** |
| DF-2 | D4.5 full native fault matrix | qualification of existing behavior | **partly in** — S6.1 qualifies the *cancellation* case of D4.5 (D4 invariant 5); the remaining fault-matrix rows stay with the later package | **partly in** — S6.2 qualifies the *crash/power-loss* case of D4.5 (D4 invariant 4); remaining rows stay with S6.3 |
| DF-3 | Full S-02 ten-invariant native matrix (applicable subset qualified) | qualification of existing behavior | **partly in** — S6.1 qualifies the cancellation path and the unknown-outcome case; the complete matrix is a later package (S6.3) | **partly in** — S6.2 qualifies the *failed staging* case (D4 invariant 4) and *reopen after crash*; the complete matrix is a later package (S6.3) |
| DF-4 | F-02 crash/power-loss/multi-process durability | qualification of existing behavior | **out** — later work package (S6.2); unchanged by S6.1 (Q-S61-8) | **in** — **primary S6.2 deliverable** |
| DF-5 | Q2-F09 (rebuild blocks queries / no cancellation), Q2-F12, I-04 cancellation | qualification of existing behavior | **partly in** — S6.1 qualifies the cancellation path (I-04, Q2-F09 cancellation aspect); Q2-F12 and the rebuild-blocking aspect are not cancellation-path cases and remain deferred | **partly in** — Q2-F12 (crash/power-loss) is the primary S6.2 deliverable; rebuild-blocking aspect remains deferred |
| DF-6 | Full platform certification (other distributions/architectures; internal locking/WAL/mmap instrumentation) | qualification of existing behavior | **out** — later work package (S6.5) | **out** |
| DF-7 | WP-S24-3 optional cross-target scope; public Find Usages; optimization | optional capability — separate owner commissions (unauthorized) | **out — unauthorized** | **out — unauthorized** |
| DF-8 | M9.5 cancellation path; M12.1–M12.3 full native matrix | named deferral (Stage 6) | **partly in** — S6.1 takes M9.5 (dedicated native cancel-path test) and the M12.2 row (D4 invariant 5, native); M12.1 and M12.3 remain with the later full-matrix package | **partly in** — S6.2 takes the DF-4 / D4 invariant 4 row; M12.1 and M12.3 remain with the later full-matrix package (S6.3) |

---

## 2. Requirement-level obligations

| ID | Requirement (source wording) | State at baseline | S6.2 assignment |
| --- | --- | --- | --- |
| **D4 invariant 4** (owner decision §D.4) | *"Failed staging must preserve the last accepted publication and corresponding valid index state."* | accepted as an invariant; the native case explicitly deferred (S2.5 matrix) | **in** — the native failed-staging case and its preservation proof |
| **D4 invariant 5** (owner decision §D.4) | *"Cancelled staging must not expose partial facts or index entries."* | accepted as an invariant; the native case explicitly deferred (S2.5 matrix M12.2) | **in** — already discharged by S6.1 (cancellation path); S6.2 re-verifies the durability aspect |
| **D4 qualification clause** (owner decision §D.4) | *"CI-1A must reproduce the applicable stale-index, cancellation, failed-publication, supersession and reopen tests using actual production storage and APIs. The previous in-process experiment does not establish native or cross-process atomicity; record those limits explicitly."* | applicable subset qualified in Stage 2; failed-publication and crash-recovery cases outstanding | **in** — failed-publication case and crash-recovery reopen against production storage |
| **S-02** (S-01 obligation reconciliation) | All ten D4 invariants exercised against the actual production `LatticeCodeGraphStore`; Stage 6 native matrix to include *"supersession stress, true concurrent publication, cancellation path, unknown outcome"* | open — Stage 6 obligation | **partly in** — failed staging case and crash-recovery reopen; the complete matrix is a later package (S6.3) |
| **S-02 specification** (CI1A specification) | *"All ten invariants must be exercised against actual production `LatticeCodeGraphStore` storage, including failed/cancelled/superseded publication, reopen and a native concurrency/republication scenario."* | open | **partly in** — the failed-staging case and crash-recovery reopen |
| **DF-4** (F-02) | Managed fault injection is not crash/power-loss/abrupt-kill/multi-process durability | open — Stage 6 | **in** — this is the primary S6.2 deliverable; the bounded limitation is published |
| **Q2-F12** (owner decision §G.2) | *"Crash/power-loss/multi-process/cross-platform not demonstrated"* | open — Stage 6 | **in** — primary S6.2 deliverable |
| **I-04** (CI1A obligation register, line 216) | *"Simple publication-bound maintenance and serialized policy fixed; cancellation remains unqualified."* | open — Stage 6 | **out** — S6.1 discharged the cancellation aspect; remaining aspects unchanged |
| **Q2-F09** (owner decision §G.2; register line 227) | *"C rebuild blocks queries; no cancellation"* — *"addressed by D4 (maintenance policy; CI-1A must qualify a production-backed rebuild path)"* | open — Stage 6 | **partly in** — S6.1 discharged the cancellation aspect; the rebuild-blocking aspect remains deferred |
| **Q2-F12** (owner decision §G.2; register line 230) | *"Crash/power-loss/multi-process/cross-platform not demonstrated"* | open — Stage 6 | **in** — primary S6.2 deliverable |
| **F-02** | Managed fault injection is not crash/power-loss/abrupt-kill/multi-process durability | open — Stage 6 | **in** — same as DF-4; the bounded limitation is published (Q-S62-1) |
| **SQ-05 / §14** | Full Windows/Linux/macOS certification | Linux minimum qualified in Stage 2; full certification deferred | **out** — DF-6; but S6.2's own Linux evidence row must use genuine hosted Linux |

---

## 3. Stage 6 work-package map

| Work package | Scope | Relationship to S6.1/S6.2 |
| --- | --- | --- |
| **S6.1** | native cancellation + applicable D4.5/S-02 subset | **complete** — accepted with qualifications |
| **S6.2** | durability (crash/power-loss/abrupt-kill/multi-process) | **this commission's boundary** — consumes S6.1's cancel-path receipts |
| S6.3 | full native matrices (D4.5 fault matrix, complete S-02) | consumes S6.1 and S6.2; not begun |
| S6.4 | real-fixture D3 | consumes the accepted corpus and the D4/D5 qualifiers; not begun |
| S6.5 | platform certification matrix | consumes S6.1–S6.4; not begun |

Each package carries its own qualification/acceptance gate; **no package grants release**, and no
Stage-6 work package may waive a Stage-2 qualification.

---

## 4. Obligations explicitly *not* touched by S6.2

M9.1–M9.4 and M9.6–M9.10 (stale-index, schema availability, bounded `Examined`, NEW-01 recovery,
supersession, reopen equivalence, union/deletion/conflict, ordering/limits, snapshot serialization),
M8.1–M8.4 (graph identity invariants), M13.x (master-provenance copies), M14.x (Linux CI expansion),
S-01 canonicalization proof, D3 full, D5 schema/refusal, and every published package identity.

---

## 5. Protected obligations carried into S6.2

Q-L1…Q-L8, MR-1…MR-4, GH-1…GH-4 and DF-1…DF-8 remain in force; the osx-arm64 runtime limitation
(content evidence only) remains; the accepted merge, identity, refusal and accounting semantics, the
historical regression suites, the coverage thresholds and the CI gates are unchanged.
S6.1 qualifications **Q-S61-1…Q-S61-8** remain in force.