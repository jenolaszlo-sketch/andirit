# ADR-CI1-01-CI1A-S1-OBLIGATION-RECONCILIATION

**Project:** Andirit  
**Milestone:** ADR-CI1-01-CI1A-S1-OBLIGATION-RECONCILIATION — Owner reconciliation of all Stage 1 independent review findings  
**Recorded:** 2026-11-01  
**Authority:** `ADR-CI1-01-CI1A-S1-OWNER-ACCEPTANCE` (this document's companion)

---

## 1. Complete Obligation Register

| Field | Value |
|-------|-------|
| Source review | `ADR-CI1-01-CI1A-IMPL-01-S1-Q1` (`C:\Users\Laszlos\source\repos\Solo\adr-ci1-01-ci1a-impl-01-s1-q1`) |
| Review disposition | `SUPPORTED WITH QUALIFICATIONS` |
| Total findings | 15 (7 significant qualifications, 5 implementation follow-ups, 3 minor) |
| Earlier controlling obligations | S-01, S-02, D3, D4, D5 |

---

## 2. Significant Qualifications (7)

### SQ-01: Target-Page Integrity (Derived Cache)

| Field | Value |
|-------|-------|
| **Source** | `FINDINGS.md` §SQ-01; `NATIVE-STORAGE-REVIEW.md` §B3; `ADVERSARIAL-PROBES.md` P2 |
| **Requirement** | Persisted target pages (`AndiritOccurrencePage`) are authoritative and tamper-evident |
| **Current evidence** | Page tampering preserving `PublicationIndexRunId` passes open validation; projection rebuilt from `AndiritOccurrenceUnit` facts; queries return correct results but corrupted page persists until next publication |
| **Stage 1 disposition** | **Qualified** — Pages are derived cache, not independently authoritative. Unit facts are authoritative. |
| **Owner classification** | **Accepted limitation (no immediate remediation)** |
| **Rationale** | Query correctness is preserved (projection rebuilt from authoritative units). Page corruption self-corrects on next publication. No correctness defect. |
| **Required work** | Document in native storage contract: "`AndiritOccurrencePage` is a materialized view derived from `AndiritOccurrenceUnit` facts. Page integrity not independently validated on open." |
| **Evidence required** | Updated documentation in `LatticeCodeGraphStore` class comments and schema evidence. |
| **Authorization** | Existing documentation commission (this work) |
| **Closure authority** | Owner acceptance of this reconciliation |
| **Blocking stage** | None — does not block Stage 1 or Stage 2 |

---

### SQ-02: Unknown Commit Outcome

| Field | Value |
|-------|-------|
| **Source** | `FINDINGS.md` §SQ-02; `PUBLICATION-ATOMICITY-REVIEW.md` §C4; Q3-QUAL-03 |
| **Requirement** | Distinguish confirmed failure, confirmed success, unknown native commit outcome |
| **Current evidence** | LatticeDB local store throws on failure or returns on success. No ambiguous state. Process crash during `txn.Commit()` → state either pre- or post-commit on reopen (WAL). Acknowledgment loss not distinguishable. N6 tests managed `before-commit` fault only. |
| **Stage 1 disposition** | **Qualified** — Explicit Stage 1 boundary. No false success/failure reporting in known paths. |
| **Owner classification** | **Prerequisite before Stage 2 authorization** |
| **Rationale** | Stage 2 must not rely on an unqualified commit outcome model. The limitation must be explicitly acknowledged in Stage 2 publication paths. |
| **Required work** | 1. Document Stage 1 limitation in Stage 2 specification.<br>2. Stage 2 must define conservative behavior: on any persistence failure, replay prior accepted state; never claim success without verified commit.<br>3. If Stage 2 introduces multi-process or distributed publication, unknown-outcome reconciliation must be independently qualified. |
| **Evidence required** | Stage 2 specification includes explicit unknown-outcome section; Stage 2 qualification plan includes crash-recovery test (if applicable). |
| **Authorization** | Stage 2 specification commission (`ADR-CI1-01-CI1A-IMPL-01-S2`) |
| **Closure authority** | Stage 2 independent review + owner acceptance |
| **Blocking stage** | Stage 2 authorization |

---

### SQ-03: Concurrency and Snapshot Semantics (Invariant 10)

| Field | Value |
|-------|-------|
| **Source** | `FINDINGS.md` §SQ-03; `D4-CONCURRENCY-REVIEW.md` §H2; Q3-QUAL-03 |
| **Requirement** | Query behavior during concurrent publication follows a defined snapshot or freshness contract |
| **Current evidence** | `_writeGate` (SemaphoreSlim) serializes ALL reads and writes. Queries block during publication. No MVCC, no snapshot isolation. Freshness re-check only via caller-provided `ExpectedPublication`. No concurrent stress test. |
| **Stage 1 disposition** | **Qualified** — Serialization is the demonstrated model. Not snapshot isolation. Acknowledged Stage 1 boundary. |
| **Owner classification** | **Prerequisite before Stage 2 authorization** |
| **Rationale** | Stage 2 must not assume snapshot isolation. The current serialization model must be explicitly documented as the Stage 1 contract. Stage 2 may propose a stronger contract but must independently qualify it. |
| **Required work** | 1. Document current contract: "Queries serialize behind publication; observe either prior or new complete state; no partial/intermediate visibility."<br>2. Stage 2 specification must either (a) accept serialization as the concurrency model with explicit qualification, or (b) define and implement a stronger contract (e.g., publication versioning with non-blocking reads) with independent evidence.<br>3. If (b), Stage 2 must include concurrent stress test with measurable isolation level. |
| **Evidence required** | Stage 2 concurrency model documented; independent qualification of chosen model. |
| **Authorization** | Stage 2 specification commission |
| **Closure authority** | Stage 2 independent review + owner acceptance |
| **Blocking stage** | Stage 2 authorization |

---

### SQ-04: Historical Prior-Writer Fixture (SQ-06)

| Field | Value |
|-------|-------|
| **Source** | `FINDINGS.md` §SQ-04; `SCHEMA-D5-REVIEW.md` §I4; `ADVERSARIAL-PROBES.md` (gap) |
| **Requirement** | Schema refusal tested against authentic prior-writer databases |
| **Current evidence** | Refusal tested by tampering fresh stores (version 99, missing meta, namespace mismatch). No historical fixture generated. D5 accepts no migration. |
| **Stage 1 disposition** | **Not demonstrated** — Explicit Stage 1 qualification (SQ-06 per producer). |
| **Owner classification** | **Stage 2 deliverable** |
| **Rationale** | D5 no-migration policy is accepted. However, refusal behavior on actual old-format databases must be verified before broader CI-1A acceptance. This is naturally a Stage 2 task when real project data and fixture generation are in scope. |
| **Required work** | 1. Generate immutable historical fixture master(s) for superseded experimental occurrence schemas (`occurrence-facts/1..3`, `usage1..3`, `asmctx1/2`).<br>2. Define disposable copy procedure with per-instance hashes.<br>3. Execute refusal tests against historical fixtures in Stage 2 qualification. |
| **Evidence required** | Immutable fixture master hashes; disposable copy procedure; refusal test results against historical fixtures. |
| **Authorization** | Stage 2 implementation commission (fixture generation) |
| **Closure authority** | Stage 2 independent review |
| **Blocking stage** | Stage 6 / broader CI-1A acceptance (not Stage 2 authorization) |

---

### SQ-05: Cross-Platform Native Behavior

| Field | Value |
|-------|-------|
| **Source** | `FINDINGS.md` §SQ-05; `PERFORMANCE-REVIEW.md`; Q3-QUAL-03 |
| **Requirement** | Native LatticeDB behavior verified on supported platforms |
| **Current evidence** | Windows 10.0.26200 X64 only (.NET 10.0.401, Debug). Linux/macOS not tested. |
| **Stage 1 disposition** | **Qualified** — Single-platform evidence. Explicit Stage 1 qualification. |
| **Owner classification** | **Stage 2 deliverable** (minimum: Linux CI) |
| **Rationale** | Cross-platform native behavior (file locking, WAL, memory mapping) must be verified before production deployment. Windows-only qualification is insufficient for CI-1A acceptance but acceptable for Stage 1 boundary. |
| **Required work** | 1. Add Linux CI pipeline for `Andirit.LatticeDb.Tests` (native tests).<br>2. Execute native test suite on Linux in Stage 2.<br>3. Document any platform-specific behavior differences. |
| **Evidence required** | Linux CI run logs showing 38/38 native tests pass. |
| **Authorization** | Stage 2 implementation commission (CI infrastructure) |
| **Closure authority** | Stage 2 independent review |
| **Blocking stage** | Stage 6 / broader CI-1A acceptance |

---

### SQ-06: Namespace Mismatch Refusal Test Coverage

| Field | Value |
|-------|-------|
| **Source** | `FINDINGS.md` §SQ-06; `SCHEMA-D5-REVIEW.md` §I1; `ADVERSARIAL-PROBES.md` P7 |
| **Requirement** | Both version and namespace mismatch independently refused |
| **Current evidence** | Code validates both (lines 480-487 `RestoreOccurrences`). Test `Store_RejectsUnsupportedOccurrenceSchemaOnOpen` only tampers version (99). Namespace validation not independently tested. |
| **Stage 1 disposition** | **Test coverage gap** — Code correct, test missing. |
| **Owner classification** | **Stage 2 deliverable** (minimum test gate) |
| **Rationale** | Trivial test addition. Must be completed before Stage 2 acceptance to claim complete schema refusal coverage. |
| **Required work** | Add test `Store_RejectsUnsupportedOccurrenceNamespaceOnOpen` implementing Probe P7 design (tamper meta.Namespace to invalid value, verify `CodeOccurrenceSchemaException`). |
| **Evidence required** | New test passing in `Andirit.LatticeDb.Tests`. |
| **Authorization** | Stage 2 implementation commission (or separate test remediation) |
| **Closure authority** | Stage 2 independent review |
| **Blocking stage** | Stage 2 acceptance |

---

### SQ-07: Publication/Reopen Complexity Scales With Repository Size

| Field | Value |
|-------|-------|
| **Source** | `FINDINGS.md` §SQ-07; `TARGET-INDEX-REVIEW.md` §F4; `PERFORMANCE-REVIEW.md` |
| **Requirement** | All operations bounded by query page size |
| **Current evidence** | Query is bounded (`Examined` = page size). Publication and reopen scale with total repository occurrence facts (full page rebuild O(F log F), full index rebuild O(F)). Explicit Stage 1 design choice (incremental deferred). |
| **Stage 1 disposition** | **Qualified** — Design choice honestly documented. Not a correctness defect. |
| **Owner classification** | **Accepted limitation (no immediate remediation); Stage 6 optimization** |
| **Rationale** | Stage 1 explicitly defers incremental page maintenance. The O(total facts) publication cost is a known tradeoff for simplicity. Stage 2 must measure real-project costs but not optimize. Stage 6 may authorize incremental optimization. |
| **Required work** | 1. Document clearly: "Query retrieval bounded; publication/reopen scale with repository size. Incremental page maintenance deferred to Stage 6."<br>2. Stage 2 must measure real-project publication/reopen costs.<br>3. Stage 6 may introduce native-node-ID optimization or incremental maintenance if measurement warrants. |
| **Evidence required** | Stage 2 performance measurements on real project data; Stage 6 optimization decision. |
| **Authorization** | Stage 2 specification (measurement); Stage 6 (optimization) |
| **Closure authority** | Owner acceptance of measurement results; Stage 6 architecture decision |
| **Blocking stage** | None for Stage 2 |

---

## 3. Implementation Follow-Ups (5)

### IF-01: Contributor Union Algorithm Detail

| Field | Value |
|-------|-------|
| **Source** | `FINDINGS.md` §IF-01; `InMemoryCodeGraphStore.RebuildOccurrenceIndex` (892-950); `PersistOccurrences` (627-652) |
| **Requirement** | Contributor union uses `(pluginId, unitId)` deduplication with ordinal ordering |
| **Current evidence** | Implemented and tested in N4. Must match Q3 probes B4-01…B4-06. |
| **Owner classification** | **Stage 2 deliverable** (verification against Q3 probes) |
| **Required work** | Stage 2 must verify contributor union behavior matches Q3-verified identity semantics. |
| **Evidence required** | Stage 2 qualification includes contributor union adversary tests. |

### IF-02: Target-Index Page Encoding

| Field | Value |
|-------|-------|
| **Source** | `FINDINGS.md` §IF-02; `PersistedOccurrencePage` (1019-1024); `PersistOccurrences` (627-652) |
| **Requirement** | Page format, ordering, pagination |
| **Current evidence** | Single page per target. Ordering: path, startLine, startColumn, endLine, endColumn, ID. No pagination. |
| **Owner classification** | **Stage 2 deliverable** (pagination if needed for large target pages) |
| **Required work** | Stage 2 must verify page encoding handles real-project target sizes. Pagination if target facts exceed planner bounds. |

### IF-03: Occurrence Coverage Categories

| Field | Value |
|-------|-------|
| **Source** | `FINDINGS.md` §IF-03; `CodeOccurrenceCoverage` (lines 236-241) |
| **Requirement** | Full disjoint coverage states: Confirmed, External, Ambiguous, Unresolved, Unsupported, ConditionalInactive |
| **Current evidence** | Only `InvocationFacts` and `UnsupportedCategories` tracked. |
| **Owner classification** | **Prerequisite before Stage 2 authorization** |
| **Rationale** | Stage 2 Roslyn extraction must emit correct coverage states. The vocabulary must be defined before implementation to avoid rework. |
| **Required work** | 1. Extend `CodeRelationshipCoverage` or new `CodeOccurrenceCoverage` with all six states.<br>2. Define refusal mapping: unsupported target → `Unsupported`; incomplete universe → `Unresolved`; ambiguous binding → `Ambiguous`; etc.<br>3. Stage 2 implementation uses these states. |
| **Evidence required** | Updated abstraction types; Stage 2 uses them correctly. |
| **Authorization** | Pre-Stage 2 specification commission (or Stage 2 spec) |
| **Closure authority** | Stage 2 independent review |
| **Blocking stage** | Stage 2 authorization |

### IF-04: `ICodeOccurrenceReader` Full Contract

| Field | Value |
|-------|-------|
| **Source** | `FINDINGS.md` §IF-04; `CodeOccurrenceReader.cs` (22-79) |
| **Requirement** | Full internal query surface: pagination token, cross-target query, coverage reporting |
| **Current evidence** | Minimal signature: `targetId`, `expectedPublication`, `maxResults`. No pagination token, no cross-target. |
| **Owner classification** | **Stage 2 deliverable** |
| **Rationale** | Stage 2 internal qualification needs richer query surface but public Find Usages API remains excluded (ADR-CI1-02). |
| **Required work** | Extend `CodeOccurrenceQuery`/`CodeOccurrenceQueryResult` with pagination token, optional cross-target mode, coverage breakdown. Preserve internal-only documentation. |

### IF-05: Unsupported-Schema Vocabulary (I-03)

| Field | Value |
|-------|-------|
| **Source** | `FINDINGS.md` §IF-05; `CodeOccurrenceAvailability` enum (4-20) |
| **Requirement** | Exact status codes for each refusal scenario fully documented |
| **Current evidence** | Enum has 5 values but exact mapping to scenarios not exhaustively documented. |
| **Owner classification** | **Stage 2 deliverable** (documentation completion) |
| **Required work** | Document exact refusal mapping: graph-only → `AnalysisUnavailable`; version mismatch → `UnsupportedSchema`; namespace mismatch → `UnsupportedSchema`; page/publication mismatch → `CorruptedIndex`; stale publication → `StalePublication`; etc. |

---

## 4. Minor Findings (3)

### MD-01: Evidence Manifest Self-Exclusion Pattern

| Field | Value |
|-------|-------|
| **Source** | `FINDINGS.md` §MD-01; `adr-ci1-01-ci1a-impl-01-s1-sha256-manifest.txt` |
| **Issue** | Manifest is self-excluded but doesn't include itself in hash list |
| **Owner classification** | **Documentation clarification** |
| **Resolution** | Standard self-excluded manifest pattern. No change needed. Document pattern in evidence packaging guide. |

### MD-02: Performance Probe Single-Run

| Field | Value |
|-------|-------|
| **Source** | `FINDINGS.md` §MD-02; `OccurrenceStage1PerformanceProbe.cs` |
| **Issue** | Single-run Debug measurements reported without variance |
| **Owner classification** | **Documentation clarification** |
| **Resolution** | Already explicitly labeled as "not thresholds, not benchmark contract." No change needed. |

### MD-03: Public API "Internal" Contract Tension

| Field | Value |
|-------|-------|
| **Source** | `FINDINGS.md` §MD-03; `CodeOccurrenceReader.cs` lines 71-73 |
| **Issue** | `ICodeOccurrenceReader` documented as "Internal... not a public user-facing query API" but type is `public` |
| **Owner classification** | **Accepted design choice** |
| **Rationale** | Types in `Andirit` namespace include many internal-building-block types. The "internal" documentation is a contract signal, not a visibility modifier. Stage 2 must not promote this to a public Find Usages API without separate owner decision (ADR-CI1-02). |
| **Resolution** | Preserve current design. Add explicit note in Stage 2 specification: "`ICodeOccurrenceReader` is an internal contract; public API requires separate owner decision." |

---

## 5. Earlier Controlling Obligations

### S-01: Source-Byte Provenance

| Field | Value |
|-------|-------|
| **Source** | `ADR-CI1-01-OWNER-01-Q1-ACCEPTANCE` §B.1; `ADR-CI1-01-OWNER-DECISION` §F; `FINDINGS.md` §5.4 |
| **Requirement** | Descriptor hashes must be constructed from actual indexed source bytes and Roslyn compilation inputs. Arbitrary supplied bytes insufficient. |
| **Current evidence** | Not implemented in Stage 1 (controlled records used). |
| **Owner classification** | **Prerequisite before Stage 2 authorization** |
| **Rationale** | This is the central Stage 2 correctness requirement. Without it, semantic occurrence identity is unproven. |
| **Required work** | Stage 2 specification must define end-to-end proof chain: source acquisition → hash → compilation context → semantic binding → occurrence identity. |
| **Evidence required** | Stage 2 positive/negative provenance tests (changed bytes, changed options, missing compilation, etc.). |
| **Authorization** | Stage 2 specification commission |
| **Closure authority** | Stage 2 independent review + owner acceptance |
| **Blocking stage** | Stage 2 authorization |

### S-02: Full Native D4 Qualification

| Field | Value |
|-------|-------|
| **Source** | `ADR-CI1-01-OWNER-DECISION` §D.4; `ADR-CI1-01-OWNER-01-Q1-ACCEPTANCE` §B.1 |
| **Requirement** | All 10 D4 invariants exercised against actual production `LatticeCodeGraphStore` |
| **Current evidence** | Stage 1 covers 6/10 native (invariants 1-4, 6, 7, 9). Invariants 5, 8, 10 partially native; 10 = serialization only. |
| **Owner classification** | **Later CI-1A obligation (Stage 6)** |
| **Rationale** | Stage 1 establishes the qualified subset. Full native D4 matrix is a broader CI-1A obligation. Stage 2 inherits applicable invariants but does not close the full matrix. |
| **Required work** | Stage 6 native D4 matrix: all 10 invariants with native evidence, including supersession stress, true concurrent publication, cancellation path, unknown outcome. |
| **Authorization** | Stage 6 commission (or later CI-1A) |
| **Closure authority** | Stage 6 independent review + owner acceptance |
| **Blocking stage** | Stage 6 |

### D3: Legacy Graph Noninterference (Full)

| Field | Value |
|-------|-------|
| **Source** | `ADR-CI1-01-OWNER-DECISION` §D.3; `FINDINGS.md` §G4 |
| **Requirement** | Universal graph noninterference across real-project ingestion |
| **Current evidence** | Synthetic corpus only. 579 regression tests pass. |
| **Owner classification** | **Later CI-1A obligation (Stage 6)** |
| **Rationale** | Stage 1 supports D3 in tested scope. Full D3 regression with real `.csproj` ingestion is Stage 6. |
| **Authorization** | Stage 6 commission |

### D5: No Migration (Accepted)

| Field | Value |
|-------|-------|
| **Source** | `ADR-CI1-01-OWNER-DECISION` §D.5 |
| **Requirement** | No migration of superseded experimental occurrence schemas |
| **Current evidence** | Stage 1 implements refusal, no migration code. D5 accepted. |
| **Owner classification** | **Accepted and closed for Stage 1** |
| **Resolution** | Stage 2 must not introduce migration. Re-ingestion rebuild is the only path. |

---

## 6. Stage 2 Prerequisite Decisions (Reviewer vs Owner)

| Reviewer Recommendation (from `OWNER-HANDOFF.md`) | Owner Disposition | Rationale |
|---------------------------------------------------|-------------------|-----------|
| Unknown commit outcome | **Prerequisite before Stage 2 auth** | Must be explicitly documented in Stage 2 spec |
| Snapshot/concurrency contract | **Prerequisite before Stage 2 auth** | Stage 2 must declare its concurrency model |
| Historical fixture | **Stage 2 deliverable** | Natural Stage 2 task with real fixtures |
| Cross-platform qualification | **Stage 2 deliverable** | Linux CI is implementation work |
| S-01 source-byte provenance | **Prerequisite before Stage 2 auth** | Central Stage 2 correctness requirement |
| Full `ICodeOccurrenceReader` | **Stage 2 deliverable** | Implementation work |
| Coverage categories | **Prerequisite before Stage 2 auth** | Vocabulary must exist before extraction |

**Key owner decision:** The reviewer's "S-01 prerequisite" and "Coverage categories prerequisite" are retained as **prerequisites before Stage 2 authorization**. The Stage 2 specification commission must resolve these before implementation begins.

The reviewer's "Historical fixture" and "Cross-platform" are reclassified as **Stage 2 deliverables** (implementation work) because they naturally belong in the Stage 2 execution phase when real project data and CI infrastructure are being built. This does not weaken correctness — they become explicit Stage 2 acceptance gates.

---

## 7. Summary Matrix

| ID | Category | Owner Classification | Blocks Stage 2 Auth? | Blocks Stage 2 Accept? | Blocks CI-1A Accept? |
|----|----------|---------------------|----------------------|------------------------|---------------------|
| SQ-01 | Significant | Accepted limitation | No | No | No |
| SQ-02 | Significant | Prerequisite | **Yes** | No | No |
| SQ-03 | Significant | Prerequisite | **Yes** | No | No |
| SQ-04 | Significant | S2 deliverable | No | **Yes** | No |
| SQ-05 | Significant | S2 deliverable | No | **Yes** | No |
| SQ-06 | Significant | S2 deliverable (test) | No | **Yes** | No |
| SQ-07 | Significant | Accepted limitation | No | No | No |
| IF-01 | Follow-up | S2 deliverable | No | **Yes** | No |
| IF-02 | Follow-up | S2 deliverable | No | **Yes** | No |
| IF-03 | Follow-up | Prerequisite | **Yes** | No | No |
| IF-04 | Follow-up | S2 deliverable | No | **Yes** | No |
| IF-05 | Follow-up | S2 deliverable | No | **Yes** | No |
| MD-01 | Minor | Documentation | No | No | No |
| MD-02 | Minor | Documentation | No | No | No |
| MD-03 | Minor | Design choice | No | No | No |
| S-01 | Earlier | Prerequisite | **Yes** | No | No |
| S-02 | Earlier | Later CI-1A (S6) | No | No | **Yes** (Stage 6) |
| D3-full | Earlier | Later CI-1A (S6) | No | No | **Yes** (Stage 6) |
| D5 | Earlier | Accepted/Closed | No | No | No |

---

## 8. Closure Statement

All Stage 1 findings are accounted for. No finding is dismissed. Each has an explicit owner classification, required work, evidence requirement, authorization path, and closure authority. The Stage 2 boundary decision incorporates these classifications.

**This reconciliation document is the authoritative obligation register for CI-1A Stage 1 acceptance.**