# ADR-CI1-01-CI1A-S2.5-ENTRY-OWNER-01 — S2.5 entry authorization and bounded Stage-2 qualification plan

**Commission:** `ANDIRIT-CI1A-S2.5-ENTRY-OWNER-01`.
**Project:** Andirit. **Recorded:** 2026-11-09. **Canonical branch:** `main`.
**Owner decision:** **`S2.5 ENTRY AUTHORIZED WITH QUALIFICATIONS`**.
**Gate:** **the S2.5 planning boundary is authorized; it adopts the bounded S2.5 work-package plan and fixes the S-01 integrated-proof contract.** Each of `WP-S25-1 … WP-S25-4` requires its own explicit commission.

> **Status: RECORDED — OWNER-AUTHORIZED (planning boundary only).**
>
> This decision verifies the [S2.5 entry assessment](../reviews/ANDIRIT-CI1A-S2.5-ENTRY-ASSESS-01.md),
> adopts the bounded work-package plan
> ([work-package plan](../planning/adr-ci1-01-ci1a-s2-5/work-package-plan.md)), fixes the
> SQ-04 amendment treatment, the SQ-05 execution routes, the S-01 proof contract, and the
> Stage-2 obligation reconciliation. It implements nothing, imports no fixture, executes no
> qualification (no Linux execution), changes no production/test/schema/CI byte, pushes
> nothing, and authorizes no work package and no successor stage.

This is an additive owner decision. It changes no production source, test, reviewer evidence, oracle or corpus, prior decision, or historical review artifact.

## 1. Verified repository preflight

| Object | Value |
| --- | --- |
| Entry HEAD | `918565be235e276c58719c51738a065fbef64ac9` (verified) |
| Entry tree | `fd1e26a6f67de7ae861dc796970c114af6bdc2c2` (verified) |
| Branch / worktrees | `main`; exactly one (`D:/projects/Andirit`); clean tracked/index; no stashes; no concurrent writer |
| Local vs remote | `origin` `https://github.com/jenolaszlo-sketch/andirit.git`; recorded `origin/main` is **0 behind / 66 ahead** (`git rev-list --left-right --count origin/main...HEAD` → `0  66`); local ref only, **no fetch** performed; **nothing pushed** in this work stream |
| Binding prerequisites | S2.3 accepted (`adr-ci1-01-ci1a-s2-3-accept-owner-01.md`); S2.4 accepted (`adr-ci1-01-ci1a-s2-4-accept-owner-01.md`, `2fbf209`); S2.5 entry assessment (`918565b`) |
| Environment | Windows `10.0.26200` **x64**; .NET SDK `10.0.401`; runtime `10.0.12` |

**Evidence integrity (read-only re-verification).** Eight committed manifests verified with **0 mismatches**: QUAL-PREP 21, SURFACE-Q1 28, READER-Q1 27, READER-Q2 41, R1 19, FINAL-Q1 17, S23-LIFE-Q1 32, S23-WP-S23-5-QUAL-01 32. The two external authentic masters were re-hashed and match their **complete** recorded values (§6). No unexpected repository state, authority conflict, or evidence-integrity problem was found. No reset/clean/restore/rebase/prune/worktree/fetch/push was performed.

## 2. Authoritative S2.5 scope and resolution of M-A … M-F

S2.5 is defined by the accepted Stage-2 records, not by this decision: [boundary](../planning/adr-ci1-01-ci1a-impl-01/stage2-boundary.md) §8 (independent Stage 2 qualification: independent semantic reproduction; the S-01 proof chain; applicable D3/D4; native reopen and direct inspection; recording of remaining S-02/historical/cross-platform obligations; gate = independent review before Stage-2 owner acceptance); [acceptance matrix](../planning/adr-ci1-01-ci1a-impl-01/stage2-acceptance-matrix.md) §16 and its §13–§15/§11 rows; [obligation register](adr-ci1-01-ci1a-s2-obligation-register.md) §8; [G-ACCEPT](adr-ci1-01-ci1a-s2-2-g-accept-owner-01.md) §18; the [S-01 canonicalization disposition](../planning/adr-ci1-01-ci1a-s2-3/s01-canonicalization-disposition.md) §5/§7; and the S2.3/S2.4 acceptances (§7 of each). The [entry assessment](../reviews/ANDIRIT-CI1A-S2.5-ENTRY-ASSESS-01.md) §2–§8 is verified as evidence and is adopted.

**D1 confirmations.** (1) S2.3 and S2.4 are accepted with qualifications (recorded, re-verified §1). (2) Remaining obligations are explicitly identified (assessment §3; decision §9). (3) No unresolved prerequisite prevents bounded S2.5 entry — every open item has a bounded path, with the SQ-04 amendment, SQ-05 route, and S-01 contingency explicitly recorded. (4) The proposed packages respect the accepted architecture — tests/evidence/fixture/CI/qualification work only; the only potential production touchpoint (a demonstrated S-01 canonicalization gap) is **not** pre-authorized and requires a separate bounded remediation commission. (5) The remaining Stage-2 acceptance conditions are separately identifiable (§9/§11).

| ID | Assessment question | Decision |
| --- | --- | --- |
| **M-A** | Adopt the S2.5 scope, decomposition and gates | **Adopted** (§3, §4; plan) |
| **M-B** | SQ-04 amendment for formats without authentic prior writers; fixture-binding procedure | **Planning decision** (§6): available formats proceed; no-writer codecs require an explicit amendment at acceptance; no synthetic substitution |
| **M-C** | SQ-05 evidence route; push/runner authorization; WSL2 policy | **Two defined routes** (§7); either needs separate authorization; WSL2/Docker excluded absent an explicit normative amendment |
| **M-D** | S-01 completion-gate boundary; remediation authorization policy | **Fixed** (§8): independent proof first; a demonstrated defect requires a separate bounded remediation commission; no correction pre-authorized |
| **M-E** | WP-S24-3 deferral | **Preserved** without reopen (§10/§12) |
| **M-F** | Stage-2 acceptance criteria confirmation | **Recorded** (§11): the separate acceptance decision must be based on the complete reconciliation and the S2.5 gate evidence — never on earlier decisions alone |

## 3. Entry disposition

**`S2.5 ENTRY AUTHORIZED WITH QUALIFICATIONS`.**

This establishes the **planning boundary only**. It adopts the bounded S2.5 work-package plan ([`docs/planning/adr-ci1-01-ci1a-s2-5/work-package-plan.md`](../planning/adr-ci1-01-ci1a-s2-5/work-package-plan.md)) and fixes the S-01 proof contract (§8). It **does not** authorize any individual work package, any fixture import, any qualification execution (including Linux), any push or PR, any CI change, or any Stage-2 acceptance. Entry is qualified because the SQ-04 amendment, the SQ-05 route authorization, and the S-01 contingency (§§6–8) remain open owner decisions.

## 4. WP-S25-1 … WP-S25-4 boundaries

The full ten-field decomposition (obligation ownership, entry prerequisites, fixtures/environments, permitted change surfaces, required evidence, independence, blocking failures, qualification disposition, owner-acceptance gate, exclusions) is operative in the [plan](../planning/adr-ci1-01-ci1a-s2-5/work-package-plan.md). The adopting boundaries:

| Package | Bounded scope | Standing exclusions | Gates |
| --- | --- | --- | --- |
| **WP-S25-1 — SQ-04 historical fixtures** | Authenticate the two prior-writer masters (complete hashes §6); repository-side evidence bindings; reproducible native refusal qualification; authentic-vs-constructed separation; no-writer codec register for amendment | No master alteration/regeneration; no store opened in place; no synthetic fixture as authentic; no migration/reinterpretation; no production change | Independent review (reviewer ≠ producer) + separate `WP-S25-1-OWNER` decision |
| **WP-S25-2 — SQ-05 Linux native qualification** | Current expanded `Andirit.LatticeDb.Tests` on genuine Linux; native receipts; behavior/refusal matrix; regression and environment evidence | No push/PR without separate authorization; no WSL2/Docker substitution absent amendment; no Windows results as Linux evidence; no macOS; no full certification; no crash/power-loss | Independent review (SHA binding, suite completeness) + separate `WP-S25-2-OWNER` decision |
| **WP-S25-3 — S-01 integrated proof (central)** | Independent integrated proof: bytes → compilation → binding → canonical component digests → identity; independent semantic reproduction; real-project end-to-end; publication/authority/persistence/native reopen; applicable D3/D4/D5; native-mirror observation; refusal/recovery evidence. Methodology fixed before testing; proof construction and independent qualification are separated | No production corrections pre-authorized (demonstrated defect → separate bounded remediation commission); no identity-scheme change; no public API; no cross-target; no optimization; no crash/power-loss claims | Independent review (reviewer ≠ implementer; independent oracle) + separate `WP-S25-3-OWNER` decision |
| **WP-S25-4 — Final integrated Stage-2 qualification** | Reconcile accepted Stage-2 evidence; independently qualify remaining mandatory interactions; confirm S-01 proof; verify SQ-04/SQ-05 dispositions; reconcile applicable D3/D4/D5; classify every remaining qualification/deferred requirement; prepare the evidence for a separate Stage-2 acceptance | Earlier owner decisions are not substitutes for final integration evidence; no new functionality; no thresholds; no optimization; no Stage 6 work; no public Find Usages; cross-target excluded; the Stage-2 acceptance decision itself is not part of this package | Independent review (reviewer ≠ implementer; no counting aggregation) → feeds the **separate Stage-2 owner-acceptance decision** |

## 5. Work-package dependency and authorization matrix

| Package | Exact obligation ownership | Depends on | May parallel | Requires | Push/PR needed? | Authorization state |
| --- | --- | --- | --- | --- | --- | --- |
| WP-S25-1 | SQ-04 (available formats); M-01; Q2-F11; D5 refusal | This decision; external masters present & re-hashed | WP-S25-2, WP-S25-3 | Separate WP-S25-1 commission | No | **Planned; NOT AUTHORIZED** |
| WP-S25-2 | SQ-05; Q2-F12 (Linux minimum); matrix §14 | This decision; route A or B approved | WP-S25-1, WP-S25-3 | Separate WP-S25-2 commission **plus** route authorization (push scope / runner) | **Only via route A, separately authorized** | **Planned; NOT AUTHORIZED** |
| WP-S25-3 | S-01; boundary §8; G-ACCEPT §18 | This decision; frozen reviewed SHA; no unaccepted evidence dependency | WP-S25-1, WP-S25-2 | Separate WP-S25-3 commission | No | **Planned; NOT AUTHORIZED** |
| WP-S25-4 | S2.5 gate as a whole; reconciliation (register §8); SQ-06 remainder; applicable SQ-02/SQ-03; D3-full real-fixture; applicable D4/D5; IF residuals | WP-S25-1 + WP-S25-2 + WP-S25-3 completed and independently reviewed; regression floors green | — | Separate WP-S25-4 commission | Only if new Linux evidence is generated (route A rules apply) | **Planned; NOT AUTHORIZED** |
| Stage-2 acceptance | Owner decision | WP-S25-4 report + complete reconciliation + SQ-04 amendment + SQ-05 pass + S-01 proof | — | Separate Stage-2 owner-acceptance decision | — | **NOT AUTHORIZED** |

**Sequencing.** Not strictly sequential: WP-S25-1, WP-S25-2, and WP-S25-3 may run in parallel where their prerequisites permit. **WP-S25-4’s exact entry prerequisites are defined at its commission time from the then-current accepted states**, and at minimum require the three predecessor packages completed and independently reviewed. S-01 proof construction must not silently depend on unaccepted or unavailable evidence; where a behavior is not covered by WP-S25-2, it must be listed explicitly as not Linux-verified. **No package is authorized merely because this plan is adopted.**

## 6. SQ-04 authenticity and amendment requirements

**Planning decision (D4).** SQ-04 is **not** declared fully discharged by this decision. Authentic formats currently supported by actual prior-writer evidence (complete hashes; re-verified §1, re-verify at package entry):

| Artifact | Provenance | Complete SHA-256 | Bytes | Demonstrated behavior |
| --- | --- | --- | --- | --- |
| `prerem-authentic.ltdb` (+ `-wal`) | Pre-REM production code (`4c6d26a1…`, tree `320e965f…`, `git archive`); REM-01-Q1 package | `.ltdb`: `7646b98288a9803cff6f9930c78a3e7df5c676f6fc4e0cd2df2af5e79bc68885`; `.ltdb-wal`: `8ca170cdc55a4428a5506bcb447a0667ab10c92c77ef149ae36460a5603aeab4` | 86,016 | P13b: refusal without reinterpretation (`CodeOccurrenceSchemaException`) |
| `baseline-graph-only.master.db` | Frozen baseline plugin + store (`31c33a934a…`, tree `a265f561…`); R2 package | `f21a72bee2c2e378e16766ca7b84c29010ab4d50360bf3111a82706f947781e0` | 163,840 | H-01/H-02/H-03: legacy graph readable; occurrence `AnalysisUnavailable`; empty/legacy `AnalysisUnavailable`; open-mutation disclosed; master never opened |

**Named experimental codecs lacking authentic writer provenance:** `occurrence-facts/1..3` (experiment native-format lineage), `usage1..3`, `asmctx1/2` (superseded tuple/context generations). R2 records “Authentic prior C fact-store writer (none exists)”, and Andirit never shipped these schemas (D5). **No synthetic fixture may be represented as authentic historical evidence, and these cases are not silently waived.** Each requires a subsequent **explicit owner amendment** (at `WP-S25-1-OWNER` or the Stage-2 acceptance decision) identifying: exact affected format; unsupported historical claim; alternative evidence (if any, clearly non-authentic); residual risk; future obligation; effect on S2.5/Stage-2 acceptance.

**Consequence.** If the contract required unavailable authentic evidence **with no permitted exception**, the affected Stage-2 acceptance claim is **blocked** until an authentic source is found or the owner amends the contract. The accepted F15/REM §7 route (“formats with no authentic writer are recorded as an explicit limitation … infeasibility returns to the owner for an amendment”) permits the limitation route; the owner must still record it.

## 7. SQ-05 native Linux execution requirements

Native Linux qualification is **mandatory within its accepted scope** (acceptance matrix §14; register §6): the current expanded `Andirit.LatticeDb.Tests` suite (original 38 plus additions) must pass on **genuine Linux** with CI logs/receipts and a platform behavior matrix; full platform certification remains Stage 6. The current local repository is **66 commits ahead** of the recorded `origin/main`, and nothing has been pushed; **the existing `ubuntu-latest` CI configuration does not establish successful Linux execution** for the current work — no run exists for the expanded suite.

**Future Linux execution authorization gate (routes).**
- **Route A — owner-authorized CI publication and execution.** A **separate authorization** must identify the remote publication mechanism, scope, and affected commits **before any push or PR operation**. **This entry decision is not push authorization.**
- **Route B — owner-provided genuine Linux runner.** Requires reproducible runner metadata and trustworthy native receipts; a reviewer-controlled native host is preferred for independence.
- **WSL2/Docker and other Windows-hosted compatibility environments do not substitute for genuine native Linux evidence unless an explicit normative amendment permits it.**

No Linux qualification is executed in this commission, and the repository, remote references, CI configuration, and history are not modified.

## 8. S-01 integrated proof contract (D6)

Minimum integrated proof requirements for WP-S25-3, using the accepted Stage-2 documents (boundary §3.2/§4/§5, matrix §1–§2, G-ACCEPT §18, S-01 disposition §5/§7, identity-contract/REM-01 documents). Where normative requirements are incomplete, the missing decision is identified — **canonicalization semantics are not invented**:

| Requirement | Minimum content |
| --- | --- |
| Canonical component-digest inputs and serialization rules | `identifierDigest`/`declarationDigest`/`shapeDigest` derived from the verified live compilation per the accepted R2/identity contracts (domain-separated, length-prefixed UTF-8 parts; document IDs / paths / symbol descriptors are not shortcuts). Exact per-component canonical forms remain as accepted; the **component canonical-form completion** is the S-01 qualification target, not a new semantics |
| Independently derived digest expectations | Frozen R2 vectors + an independent canonicalization oracle; **never implementation-generated values as the sole oracle** |
| Semantic reproduction methodology | Independent positive/negative reproduction over the frozen corpus and generated-real workloads (reviewer ≠ implementer; fresh processes); expected values from accepted contracts |
| Publication and authority invariants | `asmctx3` from the verified defining compilation; real `compctx2` association; authority/orphan-owner gate preserved; no false-available |
| Recovery/reopen evidence | Durable reopen; managed-recovery consistency (known failure, unknown-outcome quarantine); **managed recovery ≠ crash/power-loss durability** |
| Native platform coverage | Windows x64 (serialized store) as the primary platform; Linux coverage only as demonstrated by WP-S25-2 within covered behaviors; uncovered behaviors listed explicitly |
| D3 noninterference | Reviewer-owned before/after graph observations incl. the native mirror (D3-full real-fixture part) |
| Applicable D4/D5 | Applicable D4 invariants (incl. recovery/reopen, bounded `Examined`, serialized schedule); D5 refusal/schema preservation; D4.5/full S-02 remain Stage 6 |
| Failure and refusal behavior | Explicit refusal vocabulary preserved; malformed/missing/unbound → refusal, never empty success |
| Evidence provenance | Fresh/reused/authentic/generated/constructed classification; raw receipts; self-excluding SHA-256 manifest |

**Separation.** **S-01 proof construction is separate from independent S-01 qualification**: the reviewer’s independent qualification (oracle + fresh probes) is the acceptance-relevant evidence. **Missing decision (M-D):** the owner must confirm at the WP-S25-3 commission the precise acceptance criteria for “canonical derivation” (adversarial case list and the freeze point of the canonical forms); if a production defect is discovered during later qualification, a **separate bounded remediation commission** is required — **no production correction is pre-authorized** by this decision.

## 9. Stage-2 obligation reconciliation (D7)

The tracked matrix of the [entry assessment](../reviews/ANDIRIT-CI1A-S2.5-ENTRY-ASSESS-01.md) §3 is adopted as the working reconciliation, classified as: **discharged** · **accepted with qualifications** · **open assigned to S2.5** · **open assigned to Stage 6** · **blocked pending external dependency** · **subject to explicit owner amendment**. Summary (no obligation is closed merely because associated tests passed):

| Obligation | Classification | Owner |
| --- | --- | --- |
| SQ-04 (available formats) | Open → S2.5 (WP-S25-1) | Owner (`WP-S25-1-OWNER`) |
| SQ-04 (no-writer codecs) | Subject to explicit owner amendment | Owner (amendment §6) |
| SQ-05 | Open → S2.5 (WP-S25-2); blocked pending the route authorization (external dependency: owner push/runner) | Owner |
| SQ-06 remainder | Accepted with qualifications (closure step + Stage-2 placement + floor re-verification) | WP-S25-4 + acceptance decision |
| S-01 (incl. canonicalization) | Open → S2.5 (WP-S25-3); central | Owner |
| D3-full | Partially qualified; real-fixture/native-mirror part open → S2.5 (WP-S25-4); full canonical corpus **Stage 6** | Owner |
| Applicable D4 / D5 | Accepted with qualifications; integrated verification → S2.5 | WP-S25-4 |
| D4.5 (cancelled staging) | **Open assigned to Stage 6** (explicitly not counted) | Stage 6 — **not moved into S2.5** |
| Full S-02 (ten-invariant matrix) | **Open assigned to Stage 6** (applicable subset + serialized schedule in S2.5) | Stage 6 — **not moved into S2.5** |
| IF-02 / IF-05 / SQ-07 residuals | Accepted with qualifications (S3-obs-01 / S1-obs-01 / S4-obs-01 preserved); reconciliation → S2.5 | WP-S25-4 |
| SQ-02 / SQ-03 | Accepted with qualifications; integrated reconciliation → S2.5; crash/power-loss unclaimed | WP-S25-4 |
| IF-01 / IF-03 / IF-04 / F10, I-05, M-01, MD-01…03 | Discharged or accepted with qualifications per the assessment §3; reconciliation/standing checks → S2.5 | WP-S25-4 |
| Crash/power-loss/multi-process; full platform certification; optimization; public Find Usages; WP-S24-3 | Deferred/unauthorized beyond S2.5 (Stage 6 / separate decisions) | Separate owner decisions |

## 10. Binding qualifications (D8)

**Preserved without reinterpretation:** F-01, F-02, F-03, Q1-Q1, Q1-Q2, S-01, Q2-Q01, Q2-Q02, O-Q2-01, Q-I01, Q-I08; the **same-harness reviewer independence limitation**; **Windows-x64** and the **serialized single-writer** model; **managed fault-injection durability limits**; **restricted real-authority cross-unit union**; **F-OBS-Q1-01** (target-local `O(n log n)` pagination — observation, not a violation); **F-OBS-Q1-03** (unkeyed token checksum, not cryptographic authentication); **Q2-OBS-01**; **S1-obs-01, S3-obs-01, S4-obs-01**; existing historical-authenticity limitations; deferred Stage-2 and Stage 6 obligations. F-Q1-01 and F-OBS-Q1-02 remain resolved; Q2-P1-01 remains closed (including the authority path).

**Standing boundaries:** WP-S24-3 remains **optional, deferred and unauthorized** (exact-target contributor aggregation in scope; multi-target enumeration not included); no public Find Usages; no cross-target implementation; no storage migration; no persisted continuation sessions; no dual-write; no index redesign; no pagination/index optimization; no Stage 6 development; no crash/power-loss/multi-process claims; no full platform certification. **If mandatory proof requires crossing these boundaries, a separate blocking architectural decision must be recorded before the affected work** — nothing here silently expands the accepted architecture.

## 11. Stage-2 exit prerequisites

The separate **Stage-2 owner-acceptance decision** requires, at minimum: (1) the S2.5 gate evidence per boundary §8/§12 and the acceptance matrix §16 (all mandatory requirements passing independent Stage 2 review); (2) the WP-S25-4 report with a **complete obligation reconciliation** in which every row is discharged, qualified, deferred (with its assigned stage), blocked with a named resolution, or amendment-subject; (3) the S-01 integrated proof accepted (incl. the canonical component-digest derivation); (4) SQ-04 available-format evidence accepted **and** the no-writer amendment recorded; (5) SQ-05 genuine-Linux evidence accepted; (6) SQ-06 remainder placed; (7) all binding qualifications carried into the acceptance; (8) explicit non-acceptance of the deferred scope (crash/power-loss, full platform certification, full D3/D4/S-02, optimization, public Find Usages, WP-S24-3). Stage-2 final qualification is **not complete** and the acceptance decision is **not authorized** by this decision.

## 12. Explicit successor authorization status

- **WP-S25-1: planned — NOT AUTHORIZED.** **WP-S25-2: planned — NOT AUTHORIZED.** **WP-S25-3: planned — NOT AUTHORIZED.** **WP-S25-4: planned — NOT AUTHORIZED.**
- **SQ-04 fixture binding is recommended as the first bounded follow-up commission** (`WP-S25-1`), where its prerequisites are satisfied; the Linux execution infrastructure (route authorization) may proceed through a **separate decision**.
- **Stage-2 final qualification is not complete. Stage-2 owner acceptance remains unauthorized.**
- **S2.5 package implementation, fixture import, native/Linux qualification execution, CI changes, and repository publication: NOT AUTHORIZED.**
- **Stage 6: not authorized. Public Find Usages: not authorized. Pagination/index optimization: not authorized. WP-S24-3: optional, deferred, unauthorized.**
- No new public API, persisted format, migration, dual-write, or production remediation is authorized. No package is authorized by this plan’s adoption.

## 13. Repository discipline

Only this decision document and the S2.5 work-package plan are added, on canonical `main`, under the single-worktree policy. No production code, test, CI configuration, historical review, evidence, oracle/corpus, prior decision, or existing planning contract is modified. No fixture was imported; no qualification ran; no branch, additional worktree, fetch, push, or package publication is performed. The decision commit/tree identities and final working-tree status are reported in the handoff.
