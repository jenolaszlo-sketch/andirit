# ANDIRIT-CI1A-S2.4-ENTRY-ASSESS-01 — S2.4 entry assessment (internal bounded reader / coverage continuation)

**Commission:** `ANDIRIT-CI1A-S2.4-ENTRY-ASSESS-01`.
**Project:** Andirit. **Recorded:** 2026-11-08. **Canonical branch:** `main`.
**Role:** independent entry assessment; assessment and documentation only.
**Disposition:** **`ENTRY READY WITH QUALIFICATIONS`**.

> This is an entry assessment. It authorizes **no** implementation, **no** production or
> test change, **no** new API/schema contract, and **no** successor package. The
> proposed S2.4 work packages in §6 are drafts for separate owner authorization only.

---

## 1. Verified repository preflight

| Check | Result |
| --- | --- |
| HEAD | `3e3209f685fc9e91b2c6e7cf4e374ca4c0d951eb` (expected) |
| Tree | `626ad5e937879e28c65c4bc94cb6191436470642` (expected) |
| Branch / worktrees | `main`; exactly one (`D:/projects/Andirit`) |
| Tracked/index, stashes, push | clean; no stashes; nothing pushed |
| S2.3 acceptance | present: `docs/decisions/adr-ci1-01-ci1a-s2-3-accept-owner-01.md` (HEAD commit `3e3209f`) |
| Environment | Windows `10.0.26200` **x64**; .NET SDK `10.0.401`; runtime `10.0.12` |

Preflight matched every expected condition; no unexpected state or evidence inconsistency was found. No reset/clean/restore/rebase/prune/worktree/push was performed.

---

## 2. Authoritative S2.4 scope

S2.4 is **not** independently planned in a committed work-package plan; its capability is defined across the Stage-2 boundary, the S2.2 acceptance, the S2.1 boundary, and the obligation register. Reconstructed from those authoritative sources:

| Source | Statement |
| --- | --- |
| [stage2-boundary.md](../planning/adr-ci1-01-ci1a-impl-01/stage2-boundary.md) §8 (S2.4) | *Internal Query and Coverage:* extend `ICodeOccurrenceReader` with pagination and coverage reporting (IF-04); verify bounded ordering and explicit limits; return honest coverage and refusal states. Gate: real-project end-to-end query assertions with coverage breakdown |
| stage2-boundary.md §10 | Internal reader may be extended for qualification; must preserve exact target identity, publication consistency, deterministic ordering, bounded retrieval, contributor correctness, explicit refusal/availability; **internal-only**; public Find Usages needs ADR-CI1-02 |
| stage2-boundary.md §11 | Exclude public Find Usages, new traversal semantics, migration/dual-write, incremental/native-node-ID redesign, broad refactoring |
| [S2.2 G-ACCEPT](adr-ci1-01-ci1a-s2-2-g-accept-owner-01.md) §18 | S2.4 = **internal bounded reader continuation/pagination and coverage reporting (`ICodeOccurrenceReader`, IF-04); publication-change handling; partial/cross-target reader surface. No public API** |
| [S2.1 authorization boundary](adr-ci1-01-ci1a-s2-1-authorization-boundary.md) §7 | S2.4 requires **bounded publication-consistent continuation/coverage** |
| [S1 obligation reconciliation](adr-ci1-01-ci1a-s1-obligation-reconciliation.md) IF-04 | Required work: extend `CodeOccurrenceQuery`/`CodeOccurrenceQueryResult` with **pagination token, optional cross-target mode, coverage breakdown**; preserve internal-only documentation |
| S1 reconciliation IF-02 | Page format/ordering/pagination: separately verify **real-project target sizes**; **pagination if target facts exceed planner bounds** |
| S1 reconciliation IF-05 / MD-03 | Document the exact refusal mapping; preserve the internal contract; no public promotion |
| [Obligation register](adr-ci1-01-ci1a-s2-obligation-register.md) F10 / IF-02 / IF-03 / IF-04 / IF-05 / SQ-01 / SQ-07 / MD-02 / MD-03 / I-03 / I-04 / S-02 / D4 / D3-full | Continuation across publication changes; ordering/limits; completeness/availability assertions; preserve refusal (no silent scan); no public API; measure costs; full D3/D4 S2.5/Stage 6 |
| [stage2-acceptance-matrix.md](../planning/adr-ci1-01-ci1a-impl-01/stage2-acceptance-matrix.md) §5 rows 7–8, §16 | `Incomplete` and complete-empty reader behavior assigned to S2.2/S2.3/**S2.4** implementation with S2.5 acceptance |

**Exact intended capability.** Extend the **internal** occurrence reader (`ICodeOccurrenceReader` / `CodeOccurrenceQuery` / `CodeOccurrenceQueryResult`) so that a caller can:
1. **Continue/paginate** a target’s occurrences deterministically beyond a single page (a continuation token/offset), while `Examined` stays bound to the target page;
2. issue an **optional cross-target** request (multi-target, bounded), if the owner places it in S2.4 scope;
3. receive a **coverage breakdown** carrying analysis state and scope/universe identity (distinct from availability);
4. obtain **publication-change handling**: a continuation bound to a superseded publication must be **refused** (`StalePublication`), never silently scanned or blended;
5. keep exact target identity, deterministic ordering (path, startLine, startColumn, endLine, endColumn, ID), bounded retrieval, contributor correctness, and explicit refusal/availability.

**Current reader surface (implementation gap).** `FindOccurrencesAsync(CodeOccurrenceQuery)` today exposes only `targetId`, `expectedPublication`, `maxResults`; the result carries `Availability`, `Publication`, `Occurrences`, `TotalMatches`, `Examined`, `RefusalReason`, `Coverage`, and a derived `Truncated`. There is **no continuation token and no cross-target mode** (`src/Andirit/CodeOccurrenceReader.cs`), matching the IF-04 gap statement.

**Explicitly excluded.** Public Find Usages (ADR-CI1-02 — no committed decision file exists), new graph/traversal semantics, MVCC/snapshot isolation, schema-format migration, dual-write, incremental/index-optimization redesign, S2.5/Stage 6 platform and crash work.

---

## 3. S2.3 handoff and dependencies S2.4 may rely on

The S2.3 decision ([accept-owner-01](adr-ci1-01-ci1a-s2-3-accept-owner-01.md)) records `S2.3 ACCEPTED WITH QUALIFICATIONS` at entry `e1ccaf6`, and authorizes **no** successor. S2.4 may depend on these **accepted** interfaces and invariants:

| Dependency | Accepted source |
| --- | --- |
| Internal occurrence reader contract and types (`ICodeOccurrenceReader`, `CodeOccurrenceQuery/Result`, `CodeOccurrenceAvailability`, `CodeOccurrenceCoverage`, `CodeOccurrenceHit`) | D3/D5 Stage-1 + S2.3 accepted |
| Publication-bound availability and refusal (`Available`/`IncompleteAnalysis`/`AnalysisUnavailable`/`StalePublication`/`UnsupportedSchema`/`CorruptedIndex`) | S2.3 accept decision §5 |
| Authority gate on `LatticeCodeGraphStore.FindOccurrencesAsync` (verified authority required; no false-available) | S2.3 §5; must be re-validated per page |
| Deterministic ordering and bounded `Examined` (D4.3/D4.9) | S2.3 §6 |
| Serialized `_writeGate` model; Windows-x64 single-writer | S2.3 §5/§6 |
| Coverage/availability separation (IF-03, REM-01) and complete-empty vs unavailable | S2.3 §5 |
| Schema/namespace refusal (D5); namespace test now closed (SQ-06 for S2.3) | S2.3 §3 |

**No accepted S2.3 work is reopened.** The assessment found no evidence of a genuine blocking defect in S2.3; S2.4 extends the internal reader only.

---

## 4. Dependency and obligation matrix

Classification of prerequisites for S2.4 (**implementation** entry), keeping every named obligation visible.

**A. Satisfied by accepted evidence**
- S2.3 native publication, reopen, availability/coverage and refusal semantics (S2.3 accept, `3e3209f`).
- IF-03 coverage/availability contract (REM-01) and the coverage vocabulary (stage2-boundary §10).
- Internal reader contract + existing bounded target query (`maxResults`/`Examined`/`Truncated`).
- D4.1/D4.2/D4.3/D4.7/D4.9 in the tested scope; MD-03 internal-only boundary; D5 no-migration.

**B. Satisfied with qualifications**
- **IF-02** — page order/limits qualified **synthetically**; real-project target sizes and planner bounds not yet verified (S2.4/S2.5).
- **IF-05** — refusal vocabulary exists; exhaustive mapping only partially documented (S2.4 contract).
- **F10** — coverage partially corrected; continuation/cross-target surface missing.
- **SQ-01** — page-integrity boundary accepted; corrupted/stale index must refuse, not silently scan.
- **SQ-07 / MD-02** — publication/reopen/query costs not measured for real targets; no threshold may be invented.

**C. Open but non-blocking (deferred beyond S2.4, to S2.5/Stage 2/Stage 6)**
- **SQ-04** authentic historical fixtures; **SQ-05** expanded Linux native qualification; **SQ-06** remainder (independent-review/owner step + Stage-2 placement); **S-01** integrated proof; **D4.5** cancelled staging and **full S-02** (Stage 6); **D3-full** real-project corpus; platform/independence boundaries.
- Carried S2.3 qualifications F-01, F-02, F-03, Q1-Q1, Q1-Q2, Q2-Q01, Q2-Q02, O-Q2-01, Q-I01, Q-I08 (none is an S2.4 prerequisite; all remain binding).

**D. Blocking for S2.4 *implementation* entry (not for this assessment)**
- The **exact continuation/cross-target/publication-change contract is not authoritatively fixed** (IF-04 names the fields but not the token semantics, cross-target request shape, ordering guarantee across pages, or publication-change behavior). A bounded owner/design decision is required before any `CodeOccurrenceQuery/Result` change.
- There is **no committed S2.4 work-package plan** (no `docs/planning/adr-ci1-01-ci1a-s2-4/`); the decomposition in §6 is proposed, not accepted.

**E. Requires an explicit owner decision**
1. Fix the internal reader continuation/cross-target/coverage/publication-change contract.
2. Adopt an S2.4 work-package decomposition and its independent gates.
3. Decide whether **cross-target** mode is in S2.4 scope now or deferred (IF-04 says "optional").
4. Confirm the **no-public-API** boundary (MD-03 / ADR-CI1-02 absent).
5. Confirm no persisted-format change (a continuation token must be transient/opaque, not persisted).

---

## 5. Implementation readiness and architectural risks

**Foundation sufficiency.** The foundation is sufficient: the internal reader, coverage types, publication-bound availability, authority gate, ordering/limit behavior, and the S2.3-accepted publication/reopen are in place; the required changes are additive extensions to `CodeOccurrenceQuery`/`CodeOccurrenceQueryResult` and the two reader implementations (`InMemoryCodeGraphStore`, `LatticeCodeGraphStore`), within the accepted internal-only boundary and D5 no-migration.

**Architectural risks.**
- **R-1 Continuation stability across publication changes.** A continuation token must bind the publication identity; a superseded/foreign token must return `StalePublication` (0 hits, 0 examined) and must never blend pages from two publications or silently restart an unbounded scan. (F10; D4.1/D4.2/D4.3.)
- **R-2 Cross-target unbounded scan.** A cross-target mode risks violating D4.3; it must define a bounded global budget and keep `Examined` truthful per target page, or be deferred.
- **R-3 Coverage vs availability conflation.** The coverage breakdown must not let an empty page imply complete-empty; `IsComplete` must remain tied to complete declared scope (IF-03).
- **R-4 Authority re-validation per page.** Each page must re-run the S2.3 authority gate; a continuation must not bypass it.
- **R-5 Determinism/ordering across pages.** Ordering must remain the accepted tuple order and be stable under continuation.
- **R-6 Contract surface creep.** Extending public `Andirit` types must not become a de-facto public Find Usages API (MD-03); no UI/endpoint.
- **R-7 Persistence/token.** If a token is serialized to the store, that is a schema change (D5-forbidden); tokens must stay transient.
- **R-8 Platform/independence.** Windows-x64 only; same-harness independence limitation persists; no MVCC.

**Required integration/regression evidence (for the eventual package).** Real-project target-size/order/end-coordinate verification (IF-02); continuation determinism and publication-change refusal; coverage-breakdown assertions (complete/partial/unavailable/complete-empty); bounded-`Examined` proof; whole-solution green; frozen R2 oracle byte-identical (`185/185`, `97F6C5E5…39C9`); format/build clean; self-excluding evidence manifest; zero production change outside the authorized internal reader.

---

## 6. Proposed work-package boundaries (drafts; NOT authorized)

All packages are `tests`/internal-reader only, preserve D3/D4/D5 and MD-03, add **no** public API and **no** persisted-format change, and remain Windows-x64/serialized.

| Package | Scope | Acceptance / gate |
| --- | --- | --- |
| **WP-S24-0 (entry/design decision)** | Documentation only: fix the exact internal reader continuation/cross-target/coverage/publication-change contract, adopt the S2.4 work-package decomposition, confirm MD-03/no-persistence | Owner decision (no implementation) |
| **WP-S24-1** | Implement the internal bounded **continuation** and **coverage breakdown** surface; publication-change refusal; determinism; bounded `Examined`. No cross-target unless WP-S24-0 includes it | Independent `S24-READER-Q1` + owner decision; regression + oracle floors |
| **WP-S24-2** | **IF-05** exact refusal-mapping documentation/tests; **IF-02** real-target size/order/planner-bound verification; **SQ-07/MD-02** cost measurement (no thresholds) | Independent `S24-SURFACE-Q1` + owner decision |
| **WP-S24-3 (optional)** | **Cross-target** bounded request, only if authorized, with a defined global budget | Independent gate if added |

**First bounded package recommendation:** `WP-S24-1` (internal continuation + coverage breakdown), preceded by `WP-S24-0` to fix the contract. `WP-S24-0` and `WP-S24-1` are **not authorized** by this assessment.

---

## 7. Entry disposition and rationale

**`ENTRY READY WITH QUALIFICATIONS`.**

**Rationale.** S2.4’s capability and obligations are authoritatively defined across the Stage-2 boundary, S2.2 acceptance, S2.1 boundary, and obligation register; and the S2.3 foundation that S2.4 depends on is accepted with qualifications. The required work is an additive, internal-only extension within the accepted architecture — not a redesign — so entry is supportable. It is **qualified**, not unconditional, because (1) the exact continuation/cross-target/publication-change contract is not yet fixed; (2) no S2.4 work-package plan/decomposition is committed; (3) IF-02/IF-05/F10/SQ-07 obligations must be carried into the packages; (4) the MD-03 no-public-API and D5 no-persistence boundaries must be explicitly confirmed; (5) all carried S2.3 qualifications and S2.5/Stage-2 obligations remain binding. No condition rises to `ENTRY BLOCKED`: there is no missing prerequisite, no S2.3 defect, and the missing decisions are bounded and resolvable within the accepted internal-reader boundary.

---

## 8. Recommended next commission (explicitly NOT authorized)

**Proposed:** `ANDIRIT-CI1A-S2.4-ENTRY-OWNER-01` — a bounded **S2.4 entry/design owner decision** (documentation only) that fixes the internal reader continuation/cross-target/coverage/publication-change contract and adopts the S2.4 work-package decomposition. Only after that decision would `WP-S24-1` be eligible for a separate implementation commission with an independent `S24-READER-Q1` gate and owner decision.

Do **not** begin S2.4 implementation, S2.5, Stage 6, or public Find Usages; authorize no new public API, schema migration, dual-write, or ADR-CI1-02 behavior.

---

## 9. Missing-decisions register (recorded, not resolved)

| # | Missing decision | Blocking point | Needed authority |
| --- | --- | --- | --- |
| M-1 | Exact continuation-token semantics (opaque form, ordering, page-size bounds, end condition) | WP-S24-1 implementation | Owner (WP-S24-0) |
| M-2 | Publication-change behavior for an in-flight continuation (refuse vs restart) | WP-S24-1 | Owner (WP-S24-0) |
| M-3 | Whether **cross-target** mode is in S2.4 or deferred; its budget semantics | WP-S24-1/3 scope | Owner |
| M-4 | Coverage-breakdown request/response shape (fields; scope/universe identity) | WP-S24-1 | Owner |
| M-5 | S2.4 work-package decomposition and independent gates | all S2.4 implementation | Owner |
| M-6 | Confirmation that continuation tokens are transient (no D5 persisted-format change) | WP-S24-1 | Owner |
| M-7 | Confirmation that `Andirit` reader types remain an internal contract (no public Find Usages; ADR-CI1-02 absent) | standing | Owner |

---

*Additive documentation only. No production code, test, oracle/corpus, planning contract, or prior decision was modified; historical artifacts are preserved unchanged.*
