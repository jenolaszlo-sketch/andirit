# S6.3 Obligation Inventory — ANDIRIT-CI1A-S2.6-WP-S63-ENTRY-01

**Date**: 2026-11-24
**Scope**: Every governing obligation that touches S6.3, its owner, its current state at the S6.3 baseline (`359a9e8a8768d4cc9204e898ef76f0b4eba38d47`), and its S6.3 assignment.
**Sources**: Byte-exact excerpts in `ANDIRIT-CI1A-S2.6-WP-S63-ENTRY-01-evidence/contract-reconciliation.txt`.

This inventory is derived from existing contracts only. It introduces no new requirement and closes no obligation: the Stage-2 acceptance, S6.1/S6.2 qualifications, and all deferred obligations remain exactly as recorded.

---

## 1. Deferred obligations (owner: Stage 6 → S6.3)

From the residual-risk register of `M54-OWNER-01` and the Stage-6 entry proposal. Every DF item except DF-7 is classified as *qualification of existing behavior*.

| ID | Deferred item | Nature | S6.1/S6.2 disposition | S6.3 assignment |
| --- | --- | --- | --- | --- |
| DF-1 | Full D3 real-fixture part (native-mirror full corpus) | qualification of existing behavior | **Out** — S6.4 | **Out** — S6.4 |
| DF-2 | D4.5 full native fault matrix | qualification of existing behavior | **Partly** — S6.1 (cancellation), S6.2 (failed/unknown) | **In** — remaining fault matrix: stale index, supersession, rebuild/reopen, contributor/deletion, ordering, concurrency |
| DF-3 | Full S-02 ten-invariant native matrix (applicable subset qualified) | qualification of existing behavior | **Partly** — S6.1 (cancellation/unknown), S6.2 (failed/unknown) | **In** — complete matrix: all 10 invariants + supersession/reopen/concurrency |
| DF-4 | F-02 crash/power-loss/multi-process durability | qualification of existing behavior | **Partly** — S6.2 (D-03, D-07 genuine) | **Out** — S6.2 complete; S6.3 does not extend crash simulation |
| DF-5 | Q2-F09 (rebuild blocks queries / no cancellation), Q2-F12, I-04 cancellation | qualification of existing behavior | **Partly** — S6.1 (cancellation), S6.2 (unknown-outcome) | **Out** — rebuild-blocking aspect not in S6.3 scope; Q2-F12 recorded as limitation |
| DF-6 | Full platform certification (other distributions/architectures; internal locking/WAL/mmap instrumentation) | qualification of existing behavior | **Out** — S6.5 | **Out** — S6.5 |
| DF-7 | WP-S24-3 optional cross-target scope; public Find Usages; optimization | optional capability — separate owner commissions (unauthorized) | **Out — unauthorized** | **Out — unauthorized** |
| DF-8 | M9.5 cancellation path; M12.1–M12.3 full native matrix | named deferral (Stage 6) | **M9.5/M12.2 done (S6.1)** | **In** — M12.1 and M12.3 |

---

## 2. Requirement-level obligations (D4 invariants 1–10)

From `ADR-CI1-01` owner decision §D.4. The ten explicit invariants governing index maintenance, publication, and cancellation.

| ID | Invariant (exact wording) | State at S6.3 baseline | S6.3 assignment |
| --- | --- | --- | --- |
| **D4-1** | *"A query may use only an index consistent with the accepted published occurrence-fact generation."* | Open | **In** — stale-index refusal tests (P-01) |
| **D4-2** | *"A stale, unbound or unverifiable derived index must refuse with an explicit status."* | Open | **In** — explicit refusal status tests (P-02) |
| **D4-3** | *"A stale index must not silently trigger an unbounded full-store scan."* | Open | **In** — bounded scan enforcement tests (P-03) |
| **D4-4** | *"Failed staging must preserve the last accepted publication and corresponding valid index state."* | **Partly discharged (S6.2 D-01, D-06)** | **In** — verify with occurrence index (P-04) |
| **D4-5** | *"Cancelled staging must not expose partial facts or index entries."* | **Discharged (S6.1 Q-01…Q-06, Q-11)** | **Out** — S6.1 complete |
| **D4-6** | *"Superseded publication must not replace a newer accepted generation."* | Open | **In** — supersession stress tests (P-05, P-06) |
| **D4-7** | *"Rebuild/reopen must produce a semantically equivalent published index."* | Open | **In** — reopen equivalence tests (P-07, P-08) |
| **D4-8** | *"Contributor changes and scoped deletion must update publication and index visibility consistently."* | Open | **In** — union/conflict/deletion tests (P-09, P-10) |
| **D4-9** | *"Bounded queries must have deterministic ordering and explicit result limits."* | Open | **In** — ordering/limit tests (P-11) |
| **D4-10** | *"Query behaviour during concurrent publication must follow a defined snapshot or freshness contract."* | Open | **In** — native concurrency/republication tests (P-12, P-15) |

---

## 3. S-02 / CI1A specification obligations

From S-01 obligation reconciliation §S-02 and CI1A specification line 194.

| ID | Obligation (exact wording) | State at S6.3 baseline | S6.3 assignment |
| --- | --- | --- | --- |
| **S-02-1** | *"All ten invariants must be exercised against actual production `LatticeCodeGraphStore` storage, including failed/cancelled/superseded publication, reopen and a native concurrency/republication scenario."* | **Partly** — S6.1 (cancelled), S6.2 (failed/unknown) | **In** — complete the matrix |
| **S-02-2** | Failed publication exercised against production storage | **Partly (S6.2 D-01…D-04)** | **In** — extend to occurrence index |
| **S-02-3** | Cancelled publication exercised against production storage | **Discharged (S6.1)** | **Out** — S6.1 complete |
| **S-02-4** | Superseded publication exercised against production storage | Open | **In** — supersession stress (P-05, P-13) |
| **S-02-5** | Reopen exercised against production storage | Open | **In** — ordinary + rebuild reopen (P-07, P-08, P-14) |
| **S-02-6** | Native concurrency/republication scenario | Open | **In** — multi-process writer race (P-12, P-15) |
| **S-02-7** | *"In-process deterministic interleaving is retained as a diagnostic only and is not accepted as native atomicity evidence."* | Open | **In** — record limit explicitly (P-16) |

---

## 4. S2.5 qualification matrix obligations

| ID | Matrix row | State at S6.3 baseline | S6.3 assignment |
| --- | --- | --- | --- |
| **M12.1** | D4 invariants 1–4, 6–9 (stale index, supersession, rebuild/reopen, contributor changes, ordering/limits) — full native matrix | Open | **In** — primary S6.3 deliverable |
| **M12.2** | D4 invariant 5 (cancelled staging) — *"Cancel path test native"* | **Discharged (S6.1)** | **Out** — S6.1 complete |
| **M12.3** | D4 invariant 10 (query behaviour during concurrent publication — snapshot/concurrent publication) — full native matrix | Open | **In** — primary S6.3 deliverable |

---

## 5. CI1A obligation register

| ID | Obligation | State at S6.3 baseline | S6.3 assignment |
| --- | --- | --- | --- |
| **I-04** | *"Simple publication-bound maintenance and serialized policy fixed; cancellation remains unqualified."* | **Discharged (S6.1)** | **Out** — S6.1 complete |
| **I-05** | Supersession/reopen qualification | Open | **In** |
| **I-06** | Concurrency/republication qualification | Open | **In** |
| **I-07** | Full D4 matrix qualification | Open | **In** |

---

## 6. Preserved qualifications and residual findings (carried forward)

| ID | Topic | Recorded disposition | S6.3 handling |
| --- | --- | --- | --- |
| Q-S61-1 | Active native execution not interruptible | Accepted limitation | Carried forward; S6.3 does not simulate interruption |
| Q-S61-2 | Native window non-cancellable, must be awaited | Accepted limitation | Carried forward |
| Q-S61-3 | D4 invariant 5 discharged by atomicity, not interruption | Accepted | Carried forward |
| Q-S61-4 | osx-arm64 unqualified (content only) | Accepted limitation | Carried forward; S6.3 no macOS runtime |
| Q-S61-5 | Linux evidence must be genuine hosted | Mandatory | **Enforced** for all S6.3 test cases |
| Q-S61-6 | Cancellation surface platform-invariant; runtime evidence not | Accepted | Carried forward |
| Q-S61-7 | Scope remains qualification of existing behavior | Accepted | **S6.3 = qualification only** |
| Q-S61-8 | F-02 durability boundary untouched | Accepted | Carried forward |
| Q-S62-1 | Crash/power-loss/abrupt-kill/multi-process not qualified | Published limitation | Carried forward; S6.3 does not simulate |
| Q-S62-2 | DF-4 managed fault ≠ crash | Deferred | Carried forward |
| Q-S62-3 | M12.1/M12.3, full S-02/D4.5 deferred to S6.3 | Partly discharged | **S6.3 completes** |
| Q-S62-4 | osx-arm64 unqualified | Preserved | Carried forward |
| Q-S62-5 | DF-4 managed fault ≠ crash | Correctly scoped | Carried forward |
| Q-S62-6 | Pre-existing 4-file lineage gap | Recorded, not repaired | Carried forward |
| F-QU-01 | D-05 skipped due to test logic bug | Medium — accept with required regression follow-up | **S6.3 follow-up: fix D-05 for P-15** |
| F-QU-02 | D-05 skipped — multi-process genuine evidence not in suite | Low — accept, discharged by independent verification | Carried forward; P-15 provides genuine evidence |
| F-QU-03 | D-12 native-ABI half not in committed test | Low — accept, discharged by re-census | Carried forward |
| F-QU-04 | D-02 plan expectation unachievable for untimed canceller | Low — accept, plan deviation justified | Carried forward |
| F-QU-05 | Q-10 mapping divergence (traceability label) | Low — accept, traceability labelling only | Carried forward |
| F-QU-06 | Producer's §6/§7 quote Windows coverage for hosted heading | Low — accept with erratum | Carried forward |
| F-QU-07 | Producer's package/API receipts at baseline HEAD | Low — accept, evidence note | Carried forward |
| F-QU-08 | D-08 omits "no temporary artifact" assertion | Low — accept, handle-release is load-bearing | Carried forward |

---

## 7. Obligations explicitly NOT touched by S6.3

- M9.1–M9.4, M9.6–M9.10 (stale-index schema availability, bounded `Examined`, NEW-01 recovery, supersession, reopen equivalence, union/deletion/conflict, ordering/limits, snapshot serialization) — **these are the M12.1/M12.3 rows themselves, which S6.3 DOES cover**
- M8.1–M8.4 (graph identity invariants) — D3, not D4
- M13.x (master-provenance copies) — separate track
- M14.x (Linux CI expansion) — DF-6/S6.5
- S-01 canonicalization proof — separate
- D3 full (legacy graph noninterference) — S6.4
- D5 schema/refusal (unsupported schema) — S6.1/S6.2 covered schema validation; D5 migration policy is intentional no-migration
- Every published package identity — not modified

---

## 8. Protected obligations carried into S6.3

Q-L1…Q-L8, MR-1…MR-4, GH-1…GH-4 and DF-1…DF-8 remain in force; the osx-arm64 runtime limitation (content evidence only) remains; the accepted merge, identity, refusal and accounting semantics, the historical regression suites, the coverage thresholds (55/75/60/70) and the CI gates are unchanged. No Stage-2 acceptance decision was reopened. No S6.1/S6.2 qualification was modified.

---

**Recorded**: 2026-11-24
**Baseline**: `359a9e8a8768d4cc9204e898ef76f0b4eba38d47`
**Canonical branch**: `main`