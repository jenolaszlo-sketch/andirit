# Stage 2 Handoff — ADR-CI1-01-CI1A-IMPL-01-S2

**Project:** Andirit  
**Milestone:** ADR-CI1-01-CI1A-IMPL-01-S2 — Stage 2 implementation handoff (specification only)  
**Status:** Specification complete. **Implementation NOT AUTHORIZED.**  
**Parent:** `docs/planning/adr-ci1-01-ci1a-impl-01/stage2-boundary.md`  
**Acceptance matrix:** `docs/planning/adr-ci1-01-ci1a-impl-01/stage2-acceptance-matrix.md`  
**Stage 1 acceptance:** `docs/decisions/adr-ci1-01-ci1a-s1-owner-acceptance.md`  
**Obligation reconciliation:** `docs/decisions/adr-ci1-01-ci1a-s1-obligation-reconciliation.md`  
**Recorded:** 2026-11-01

**Current handoff update (2026-11-03):** S2.0/F12 is **ACCEPTED WITH
QUALIFICATIONS** for the source-bound R2 semantic reference; Q3 remains
**SUPPORTED WITH QUALIFICATIONS**. Stage 1/REM-01/S2.1 qualified acceptances
stand. [OWNER-01](../../decisions/adr-ci1-01-ci1a-s2-0-corpus-01-owner-acceptance.md)
closes the corpus prerequisite and preserves the original native/integrated
F12 requirements at S2.3–S2.5. **S2.2 SEMANTIC EXTRACTION — NOT AUTHORIZED BY
OWNER-01**; S2.3 and later remain gated. Current gate order and next eligible
separate S2.2 scope-preparation commission are in
[the owner handoff/work queue](s2-0-owner-handoff.md). The dated specification
history below grants no present implementation authority; the consolidated
obligation register and later accepted slice decisions control current gates.

---

## 1. Handoff Summary

Stage 1 established the **native Candidate C storage foundation**:
- Separate `AndiritOccurrenceUnit/Page/Meta` labels in LatticeDB
- Atomic graph+occurrence publication in single transaction
- Source-unit ownership, replacement, deletion, contributor union, conflict rejection
- Bounded deterministic target-index queries
- Durable reopen with projection reconstruction
- D3 noninterference in synthetic corpus
- Schema/refusal consistent with D5 no-migration

Stage 2 connects **real Roslyn semantic extraction** to this foundation.

---

## 2. Prerequisites for Stage 2 Authorization

The following must be resolved **before** Stage 2 implementation may begin:

| Prerequisite | Source | Status | Resolution Required |
|--------------|--------|--------|---------------------|
| **S-01 Provenance chain specified** | Obligation reconciliation | Contract partly fixed by REM-01 | REM-01 fixes canonical identity inputs; S2.1 must define and implement source acquisition → hash → `compctx1` → live universe → semantic binding → `usage4` |
| **IF-03 Coverage vocabulary defined** | Obligation reconciliation | Contract fixed by REM-01; Stage 2 implementation remains | Resolution/coverage/availability dimensions and complete-empty rules are recorded; Stage 2 owns production extraction and reader integration |
| **SQ-02 Unknown outcome model declared** | Obligation reconciliation | Fixed by REM-01 | Conservative quarantine/reopen policy recorded and tested for the ordinary path; crash/multi-process remains later |
| **SQ-03 Concurrency model declared** | Obligation reconciliation | Fixed by REM-01 | Serialized `_writeGate` model accepted; native schedule test remains a Stage 2 acceptance item |
| **Stage 1 owner acceptance recorded** | `adr-ci1-01-ci1a-s1-owner-acceptance.md` | ✅ Done | — |
| **Obligation reconciliation complete** | `adr-ci1-01-ci1a-s1-obligation-reconciliation.md` | ✅ Done | — |

**No Stage 2 implementation commission may be authorized until the four open
prerequisites are resolved.** Stage 1 owner acceptance and obligation
reconciliation are already recorded and do not count as open work.

---

## 3. Stage 2 Implementation Scope (Authorized Components)

| Component | Files (Expected) | Description |
|-----------|------------------|-------------|
| Verified source acquisition | `src/Andirit.CSharp/CSharpCodeGraphPlugin*.cs`, `CSharpProjectDiscovery.cs` | Capture exact source bytes fed to Roslyn; record per-document hashes |
| Compilation context (compctx1) | New `Andirit.Occurrences` module or `Andirit.Abstractions` extension | Build `compctx1` from verified bytes + options + references |
| Analysis scope / universe | `CodeGraphIngestionSink` / `CodeIndexingService` integration | Declare project set; verify live compilations |
| Target identity (asmctx3) | `CodeOccurrenceIdentity` (extend) | Resolve via Roslyn; construct from verified defining compilation |
| Occurrence extraction | `CSharpCodeGraphPlugin` session/graph builder | Extract `Invocation` occurrences from live compilation |
| Fact conversion (usage4) | `CodeOccurrenceFact.Create` plus the canonical R2 identity inputs | Convert Roslyn semantic data → accepted `usage4` records; pass semantic source unit, target document, `asmctx3` and source hash; do not use the pre-REM four-field target helper |
| Coverage/refusal emission | `CodeGraphExtractionResult` extension | Emit `Confirmed`/`External`/`Ambiguous`/`Unresolved`/`Unsupported`/`ConditionalInactive` |
| Publication integration | Existing `CodeGraphBatch` → `CodeGraphIngestionSink` → `StageIndexUnitAsync` | No new publication path; uses Stage 1 atomic transaction |
| Internal reader extensions | `CodeOccurrenceReader.cs` / `ICodeOccurrenceReader` | Pagination token, coverage reporting (IF-04) |
| Real-project fixture | Test project under `tests/` or external | Representative C# repository with cross-project calls |

---

## 4. Explicitly NOT Authorized in Stage 2

| Excluded | Reason |
|----------|--------|
| Public Find Usages API | ADR-CI1-02 separate decision |
| Tree-sitter occurrence extraction | C# only for CI-1A |
| New language support | Out of scope |
| B2 implementation | Evaluated, not selected |
| Experimental-schema migration | D5 no-migration |
| Dual-write | D5 no-migration |
| Incremental page maintenance | Explicitly deferred (SQ-07) |
| Native-node-ID optimization | Explicitly deferred |
| Background/distributed indexing | D4 simple model |
| Full cross-platform certification by default | Linux CI is deliverable, not full certification |
| Universal semantic completeness claims | Scope-relative only |

---

## 5. Stage 2 Qualification Evidence (Required for Independent Review)

| Evidence | Description | Format |
|----------|-------------|--------|
| S-01 provenance tests | Positive: changed bytes/options → new identity; Negative: missing compilation → refusal | Test code + independent execution logs |
| Semantic identity probes | Equal-descriptor distinct compilations → distinct `asmctx3`; cross-project binding verified | Test code + independent execution logs |
| Real-project fixture | Fixture repository + independently established expected occurrence sites | Fixture hashes + expected results document |
| Native publication tests | N1–N8 equivalent with real Roslyn facts; replacement/deletion/reopen cycle | Test code + independent execution logs |
| D3 real-project | Before/after graph snapshots identical across occurrence lifecycle | Graph observation logs |
| D4 native invariants | Applicable invariants exercised against real `LatticeCodeGraphStore` | Test code + independent execution logs |
| Schema/D5 | Refusal tests against historical fixtures (SQ-04) + namespace mismatch (SQ-06) | Test code + independent execution logs |
| Cross-platform | Linux CI pass for `Andirit.LatticeDb.Tests` | CI pipeline logs |
| Performance | Publication/reopen/query measurements on real fixture | Measurement logs |

**All qualification evidence must be independently reproducible.** Producer test runs are not independent qualification.

---

## 6. Real-Project Fixture Requirements

| Property | Requirement |
|----------|-------------|
| Ownership | Andirit-owned or reproducible (public GitHub repo with pinned commit) |
| Size | ≥ 10 C# files, ≥ 2 projects |
| Invocations | ≥ 50 method invocations |
| Cross-project | ≥ 5 cross-project invocations |
| Overloads | ≥ 5 overloaded methods |
| References | ≥ 20 method references |
| Change scenarios | Source edit, option change, reference change |
| Negative cases | Missing compilation, ambiguous binding, unsupported target, incomplete universe |
| Expected results | Independently established (not generated by extractor under test) |

---

## 7. Stage 2 Independent Review Prerequisites

Before Stage 2 implementation is authorized, an **independent specification review** of this boundary must:

1. Verify S-01 provenance chain is specified as a reproducible production trust chain.
2. Verify IF-03 coverage vocabulary is fully defined in abstractions.
3. Verify SQ-02/SQ-03 concurrency and unknown-outcome models are explicitly declared.
4. Verify Stage 2 scope matches this handoff (no scope creep).
5. Verify a real-project fixture is identified and frozen, or that a bounded
   S2.0 preparation slice is explicitly authorized, with expected results
   independently established before extractor qualification.
6. Verify all exclusions are enforced in the specification.

The independent reviewer must not be the implementation agent.

---

## 8. Stage 2 Acceptance Gates

| Gate | Authority | Required Before |
|------|-----------|-----------------|
| S2.0 Contracts | Stage 2 spec review | Implementation |
| S2.1 Provenance | Independent provenance tests | S2.2 |
| S2.2 Extraction | Deterministic semantic fixtures | S2.3 |
| S2.3 Publication | Native lifecycle with real facts | S2.4 |
| S2.4 Query/Coverage | Real-project query assertions | S2.5 |
| S2.5 Independent Review | All above + D3/D4/D5/S-02 evidence | Stage 2 owner acceptance |

---

## 9. Remaining Post-Stage 2 Obligations (CI-1A Stage 6+)

| Obligation | Target Stage | Notes |
|------------|--------------|-------|
| S-02 Full native D4 qualification | Stage 6 | All 10 invariants native; true concurrent stress; unknown outcome |
| Full D3 regression (real corpus) | Stage 6 | Before/after graph observations across full C# plugin corpus |
| Historical fixture SQ-04 | Stage 2 acceptance (available formats) | Immutable master + disposable copies + refusal tests; formats with no authentic prior writer are an explicit limitation, not replaced by fresh-store tampering |
| Linux native minimum | Stage 2 acceptance | Expanded applicable native suite on Linux; full Windows/Linux/macOS certification remains Stage 6 |
| Native-node-ID page optimization | Stage 6/B2 | If measurement warrants |
| Incremental page maintenance | Stage 6 | If measurement warrants |
| Public Find Usages API | ADR-CI1-02 | Separate owner decision |
| S-01 independent qualification | Stage 2 acceptance | Required before Stage 2 owner acceptance; not implemented by REM-01 |

---

## 10. Risk Register

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Roslyn binding produces different `asmctx3` than expected | Medium | High | Independent semantic identity probes before integration |
| Real-project publication cost exceeds acceptable threshold | Medium | Medium | Measure in S2.3; defer incremental to Stage 6 |
| Cross-project binding fails for complex dependencies | Medium | High | Fixture includes multi-project; adversarial target-binding tests |
| S-01 provenance chain cannot be fully automated | Low | High | Specify manual verification steps if needed; document gap |
| Historical fixture generation proves infeasible | Low | Medium | Document the unavailable-writer limitation and return to the owner for an explicit amendment; do not substitute tampered fresh stores for an authentic prior-writer gate |

---

## 11. Next Actions

1. **Independent specification review** of `stage2-boundary.md` and `stage2-acceptance-matrix.md`.
2. **Resolve the four open prerequisites** (S-01 contract, IF-03 contract,
   SQ-02 policy, SQ-03 model); Stage 1 owner acceptance and obligation
   reconciliation are already recorded.
3. **Authorize Stage 2 implementation** via separate commission `ADR-CI1-01-CI1A-IMPL-01-S2`.
4. **Execute Stage 2** in bounded increments (S2.0 → S2.5).
5. **Independent Stage 2 review** → Stage 2 owner acceptance.

---

## 12. Stop Conditions

Stage 2 implementation must stop if:

- S-01 provenance chain cannot be expressed as reproducible trust chain.
- Stage 2 requires unapproved semantic identity change.
- Stage 2 breaks Stage 1 native publication contracts.
- D3/D4/D5 guarantees would need weakening.
- Public Find Usages API becomes mandatory.
- Production code changes required during specification commission.

---

## 13. Final Disposition

**`ADR-CI1-01-CI1A-IMPL-01-S2 SPECIFIED — READY FOR INDEPENDENT SPECIFICATION REVIEW`**

This handoff defines the Stage 2 implementation boundary, prerequisites, scope, qualification evidence, and acceptance gates. It does not authorize implementation.

The next step is an independent specification review of this boundary before any implementation commission is authorized.