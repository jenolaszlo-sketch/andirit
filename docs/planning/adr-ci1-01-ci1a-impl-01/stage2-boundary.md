# ADR-CI1-01-CI1A-IMPL-01-S2: Roslyn Semantic Occurrence Extraction and Verified Provenance

**Project:** Andirit  
**Milestone:** ADR-CI1-01-CI1A-IMPL-01-S2 — Stage 2 implementation boundary and qualification plan  
**Status:** **SPECIFIED — READY FOR INDEPENDENT SPECIFICATION REVIEW**  
**Production implementation:** **NOT AUTHORIZED**  
**Architecture authority:** `docs/decisions/adr-ci1-01-owner-decision.md` (Candidate C selected)  
**Stage 1 acceptance:** `docs/decisions/adr-ci1-01-ci1a-s1-owner-acceptance.md`  
**Obligation reconciliation:** `docs/decisions/adr-ci1-01-ci1a-s1-obligation-reconciliation.md`  
**Spec base commit:** `76ebaae84d741a38f92d2422c63742197c36bc82`

---

## 1. Purpose

Define the bounded Stage 2 implementation scope: integrate **real C# semantic occurrence extraction** with the **accepted Stage 1 native storage foundation**, establishing an end-to-end verified provenance chain from source bytes to persisted occurrence facts.

Stage 2 must not silently become the full public Find Usages feature.

---

## 2. Stage 2 Objective

Establish an independently reviewable evidence chain:

```
Real source inputs (bytes)
    → Verified compilation context (compctx1)
    → Semantic binding (Roslyn, exact defining compilation)
    → Validated occurrence facts (usage4 / asmctx3)
    → Accepted native publication (Candidate C, same transaction)
    → Internal retrieval (bounded, deterministic, publication-consistent)
```

Every step must be conservatively refusal-aware: incomplete scope, missing compilation, ambiguous binding, unsupported target → explicit refusal, never empty success.

---

## 3. Core Implementation Scope

Stage 2 is authorized to implement the following components **only**:

### 3.1 Real C# Source-Unit Discovery
- Extend `CSharpProjectDiscovery` to discover and present actual `.csproj` files for indexing.
- Source bytes obtained via `FileSystemCodeRepositoryProvider` (existing).
- Each source unit corresponds to one Roslyn compilation.

### 3.2 Verified Source-Content Acquisition (S-01 Prerequisite)
- Source bytes used for hashing **must be** the exact bytes fed to Roslyn syntax tree construction.
- `CSharpCodeGraphPlugin` must capture and record the source hash used per document.
- Compilation options and references captured as part of compilation context.

### 3.3 Compilation Context Construction (compctx1)
- Build `compctx1` from verified compilation inputs: source hashes, compilation options, reference identities.
- Reject if compilation cannot be fully constructed from accepted inputs.

### 3.4 Analysis Scope and Compilation Universe (analysisscope1 / compilation-universe1)
- Analysis scope = declared project set for the indexing run.
- Compilation universe = live compilations verified for the run.
- Binding requires: complete declared scope, exactly one live defining compilation, exact declaration correspondence.

### 3.5 Exact Target Identity from Roslyn (asmctx3)
- For each invocation occurrence, resolve the target symbol via Roslyn semantic model.
- Construct `asmctx3` from the **verified defining compilation**: `compctx1` + metadata identity + module name + identifier-token digest + full-declaration digest + shape digest, using the accepted canonical format.
- Reject if the target cannot be bound to a verified defining compilation; an external or out-of-scope target is an explicit refusal, not a confirmed fact.

### 3.6 Source Occurrence Extraction
- Extract supported invocation occurrences from the **same live Roslyn compilation** used for graph relationships.
- Supported category for CI-1A: `Invocation` (ordinary method invocation).
- Unsupported categories recorded in coverage, never as facts.

### 3.7 Conversion to Accepted `usage4` Occurrence Facts
- Construct `CodeOccurrenceFact` with the accepted R2 inputs:
  - `usage4` identity over repository, plugin, R2 semantics version, semantic
    source unit, path, source hash, span, target document, `asmctx3`, category;
  - `asmctx3` canonical target identity;
  - Semantic source unit ownership (calling project) and a separate storage
    contributor `(plugin, storage unit)`;
  - Source hash (actual indexed bytes);
  - Content hash (invocation site; payload evidence, not an identity field);
  - Declaration span (perpendicular evidence);
  - Semantics version = 4.
- Refuse/record coverage for: ambiguous binding, missing defining compilation, unsupported target kind, incomplete universe.

### 3.8 Publication into Stage 1 Native Storage
- Occurrence facts flow through existing ingestion: `CodeGraphBatch` → `CodeGraphIngestionSink` → `StageIndexUnitAsync` → `CompleteIndexRunAsync`.
- Native persistence unchanged: `AndiritOccurrenceUnit/Page/Meta` in same transaction as graph.
- Replacement/deletion/reopen/contributor/conflict behavior inherited from Stage 1.

### 3.9 Conservative Refusal Vocabulary
- Use/extend `CodeOccurrenceAvailability`: `Available`, `AnalysisUnavailable`, `UnsupportedSchema`, `StalePublication`, `CorruptedIndex`.
- Coverage states (IF-03): `Confirmed`, `External`, `Ambiguous`, `Unresolved`, `Unsupported`, `ConditionalInactive`.
- No state collapses to successful empty result.

### 3.10 Internal Retrieval for Qualification
- Use existing `ICodeOccurrenceReader` (target-scoped, bounded, deterministic).
- Extend with pagination token and coverage reporting (IF-04) as needed for qualification.
- Preserve internal-only contract; no public Find Usages API.

### 3.11 Real-Project Positive and Negative Tests
- Real C# fixture covering: method calls, references, overloads, cross-project, changed source, changed options, missing compilation, ambiguous binding, valid no-usage cases.

---

## 4. Semantic Identity Preservation

Stage 2 must preserve the accepted identity contracts **without substitution**:

| Identity | Contract | Authority |
|----------|----------|-----------|
| `usage4` | Domain-separated SHA-256 over repository, plugin, R2 semantics version, semantic source unit, path, source hash, span, target document, `asmctx3`, category (big-endian length-prefixed parts) | `ADR-CI1-01-OWNER-DECISION` §F; R2 §5 |
| `asmctx3` | `asmctx3:sha256:` + domain-separated SHA-256 over `compctx1`, metadata identity, module, identifier digest, full-declaration digest, shape digest; exact declaration correspondence required | `ADR-CI1-01-OWNER-DECISION` §F; R2 §4 |
| `compctx1` | Compilation descriptors from actual indexed source bytes | S-01; `ADR-CI1-01-OWNER-DECISION` §F |
| `analysisscope1` | Declared unit set for the run | S-01; `ADR-CI1-01-OWNER-DECISION` §F |
| `compilation-universe1` | Verified live compilations | S-01; `ADR-CI1-01-OWNER-DECISION` §F |

**Do not** create a new semantic identity scheme. **Do not** use documentation IDs, source paths, or symbol descriptors as shortcuts.

---

## 5. Scope-Relative Completeness

Stage 2 must distinguish and correctly report:

| State | Required Behavior |
|-------|-------------------|
| Verified analyzed source | Occurrence facts emitted with `Confirmed` coverage |
| Verified defining compilation | Target identity `asmctx3` bound to exact compilation |
| Missing defining compilation | Refusal with `MissingDefiningCompilation` / `Unresolved` coverage |
| Ambiguous matching declarations | Refusal with `Ambiguous` coverage |
| Valid cross-project invocation | Target bound to dependency's verified compilation; `Confirmed` when the defining compilation is in the declared universe |
| External/out-of-scope target | `UnsupportedExternalEvidence` / `External` refusal; no fact, no canonical `asmctx3` |
| Unsupported target kind | `Unsupported` coverage; no fact emitted |
| Incomplete compilation universe | `Incomplete` / `Unresolved` refusal; no partial facts and no complete-empty claim |
| Valid complete-empty analysis | `Available` with zero facts only when the declared scope is verified complete; `AnalysisUnavailable` only if no analysis was performed |

---

## 6. Source Ownership

- Occurrence facts belong to the **calling/source unit** (the project containing the invocation).
- The defining target may belong to a different project or compilation.
- Stage 1 replacement/deletion lifecycle applies: replacing a source unit retires its occurrence facts; contributor union preserved across units.
- Stage 2 must not conflate calling-unit ownership with defining-unit ownership.

---

## 7. Interaction with Stage 1 Native Storage

**Preserved without change:**

| Stage 1 Guarantee | Preservation |
|-------------------|--------------|
| Candidate C separate occurrence facts | No graph edges for occurrences |
| D3 legacy graph noninterference | Graph tests must pass unchanged |
| D4 publication authority | Single transaction, same `CodeGraphPublication` |
| D5 no migration | No experimental schema migration; re-ingestion rebuild |
| Source-unit ownership | `CodeIndexUnitReplacement` ownership model |
| Contributor union/conflict | Identity-based union; payload-hash conflict rejection |
| Native schema versioning | `andirit-occurrence-facts/1`, schema v1, semantics v4 |
| Target-index ordering | Deterministic path → line → column → ID |
| Correct reopen | Projection rebuilt from `AndiritOccurrenceUnit` rows |
| Conservative refusal | Explicit availability states; no empty success |

**If Stage 2 reveals a Stage 1 native correctness defect:** Stop the affected integration path. Require source-bound remediation before proceeding. Do not hide defect by changing semantic extraction behavior.

### Publication Performance Note

Stage 1 rebuilds target pages per repository per completion (O(total facts)). Real-project ingestion may expose higher costs. Stage 2 must **measure** this during qualification. Do not preauthorize incremental maintenance or native-node-ID optimization.

---

## 8. Stage 2 Staging Proposal

### S2.0: Contracts and Authority (Prerequisite Gate)
- Confirm Stage 1 owner acceptance (`ADR-CI1-01-CI1A-S1-OWNER-ACCEPTANCE`).
- Establish Stage 2 start prerequisites per obligation reconciliation.
- Pin current compilation-source interfaces (`CSharpCodeGraphPlugin`, `CSharpProjectDiscovery`).
- Fix contract-level resolution/coverage/availability semantics (IF-03); the
  production type implementation is a separately authorized slice.
- Record the SQ-02 outcome/retry policy and the accepted serialized SQ-03 model.
- Identify and freeze a real-project fixture and independent oracle, or
  explicitly authorize a bounded S2.0 preparation slice to create them.
- Record baseline native tests.

**Gate:** Preauthorization contracts are fixed (S-01 trust chain, IF-03
semantics, SQ-02 policy, SQ-03 model, fixture timing). Future implementation and
evidence are not credited as passed at this gate.

### S2.1: Verified Source and Compilation Context
- Bind actual source inputs to Roslyn syntax tree construction.
- Construct authoritative `compctx1` from verified bytes + options + references.
- Establish source hashes per document.
- Preserve exact identity and scope boundaries.

**Gate:** Independent positive/negative provenance tests:
- Changed source bytes → new identity
- Changed compilation options → new identity
- Missing defining compilation → refusal
- Valid cross-project binding → confirmed

### S2.2: Roslyn Occurrence Extraction
- Extract supported invocation occurrences from live Roslyn compilation.
- Bind targets to verified defining compilations (`asmctx3`).
- Generate accepted `usage4` records.
- Refuse unsupported/ambiguous cases with correct coverage.

**Gate:** Deterministic semantic fixtures and adversarial target-binding tests:
- Equal-descriptor distinct compilations → distinct `asmctx3`
- Omitted/unresolved/unbound contexts → refusal
- Linked contexts → distinct definitions
- Cross-project positive → confirmed

### S2.3: Production Publication Integration
- Publish extracted occurrences through existing source-unit lifecycle.
- Preserve native transactional consistency (same transaction as graph).
- Verify replacement/deletion after real source changes.
- Verify contributor behavior across units.

**Gate:** Native persistence, reopen, and exact occurrence identity verified:
- N1–N8 equivalent tests with real Roslyn facts
- Replacement/deletion/reopen cycle with real source changes

### S2.4: Internal Query and Coverage
- Extend `ICodeOccurrenceReader` with pagination, coverage reporting (IF-04).
- Verify bounded ordering and explicit limits.
- Return honest coverage and refusal states.

**Gate:** Real-project end-to-end query assertions with coverage breakdown.

### S2.5: Independent Stage 2 Qualification
- Reproduce positive and negative semantic tests independently.
- Verify S-01 proof chain (source bytes → compilation → binding → occurrence identity).
- Verify applicable D3/D4 behavior (real-project graph noninterference, publication consistency).
- Reopen native database and independently inspect results.
- Record remaining S-02, historical fixture, cross-platform obligations.

**Gate:** Independent review before Stage 2 owner acceptance.

---

## 9. Real-Project Qualification Fixture

Stage 2 must use an actual C# repository (owned or reproducible) covering:

| Requirement | Example |
|-------------|---------|
| Multiple C# files | ≥ 10 files |
| Method calls | ≥ 50 invocations |
| Method references | ≥ 20 references |
| Overloads | ≥ 5 overloaded methods |
| Namespaces | ≥ 3 namespaces |
| Multiple projects | ≥ 2 projects (cross-project binding) |
| Cross-project target binding | Invocation in Project A → method in Project B |
| Changed source | Modified invocation site → new identity |
| Changed compilation context | Changed options/references → new identity |
| Unit replacement/deletion | Real source change triggers replacement |
| Valid no-usage cases | Method with zero invocations in analyzed scope |
| Unsupported/incomplete analysis | External dependency not in universe |

**Critical end-to-end acceptance test:** At least one qualification path using:
1. Actual Roslyn source
2. Actual compilation
3. Real semantic binding
4. Candidate C native occurrence publication
5. Database reopen
6. Internal target lookup
7. Verified site positions and target identities

**Expected results must be independently established** (not generated by the same extractor being tested).

The repository currently contains a small real fixture at
`docs/planning/ci1-scope-01/fixtures/` with 9 C# files and a 32-row proposed
oracle. It is below these minima, is not referenced by Stage 2 and is not a
qualifying fixture. It is a seed for an authorized preparation slice, not an
accepted oracle.

---

## 10. Stage 2 Query and Coverage Boundary

### Internal Reader (Permitted)
- Stage 2 may extend `ICodeOccurrenceReader` for qualification.
- Must preserve: exact target identity, publication consistency, deterministic ordering, bounded retrieval, contributor correctness, explicit refusal/availability.
- **Internal-only contract preserved:** "Not a public user-facing query API."

### Coverage Vocabulary (IF-03 Resolution — Prerequisite)
Before Stage 2 implementation begins, define:

| Coverage State | Meaning | Refusal Mapping |
|----------------|---------|-----------------|
| `Confirmed` | Occurrence fact emitted, fully verified | `Available` only when the declared scope is complete |
| `External` | Target is external or the defining unit is outside the declared universe | Refusal / `External`; no canonical fact |
| `Ambiguous` | Multiple live defining bindings | Refusal / `Ambiguous` |
| `Unresolved` | Binding incomplete (missing compilation, etc.) | Refusal / `Unresolved` |
| `Unsupported` | Target kind or source construct not supported | Refusal/coverage; no fact |
| `ConditionalInactive` | Conditionally compiled out under the declared configuration | Exclusion/coverage; not a confirmed fact |
| `Incomplete` | Analysis ran but the declared scope is not proven complete | `IncompleteAnalysis`; no complete-empty claim |

Coverage resolution is not the same as publication availability. A query result
must carry the analysis state and scope identity; it must not infer complete-empty
from an empty fact collection. See
`docs/planning/adr-ci1-01-ci1a-s2-rem-01/coverage-and-availability.md`.

### Public API Exclusion (Mandatory)
- Stage 2 **must not** implement the complete public Find Usages API.
- No UI, external endpoint, or broad public query capabilities.
- ADR-CI1-02 (public query shape) remains a separate owner decision.
- Stage 2 reader work permitted **only** to the extent needed for semantic integration and independent qualification.

---

## 11. Explicit Stage 2 Exclusions

The Stage 2 commission must not implicitly authorize:

| Excluded | Reason |
|----------|--------|
| Full public Find Usages feature | ADR-CI1-02 separate decision |
| Arbitrary language support (TypeScript, etc.) | C# only for CI-1A |
| Tree-sitter occurrence extraction | Roslyn only for CI-1A |
| New graph traversal semantics | D3 preserves existing |
| B2 implementation | Evaluated, not selected |
| Experimental-schema migration | D5 no-migration |
| Dual-write | D5 no-migration |
| New distributed/background indexing | D4 simple model |
| Broad LatticeDB framework refactoring | Out of scope |
| Native-node-ID optimization | Explicitly deferred |
| Incremental index redesign | Explicitly deferred |
| Package publication | Separate milestone |
| Full cross-platform certification by default | Stage 2 deliverable (Linux CI) |
| Claims of universal semantic completeness | Scope-relative only |

Any newly required work outside the accepted architecture must be separately scoped and authorized.

---

## 12. Stage 2 Acceptance Matrix

| Requirement | Current Status | Stage 2 Deliverable | Required Evidence | Acceptance Gate |
|-------------|----------------|---------------------|-------------------|-----------------|
| S-01 provenance | Open | Actual compilation-input proof | Positive/negative Roslyn tests | S2 independent review |
| `usage4`/`asmctx3` | Qualified experimental | Production semantic binding | Target identity probes | S2 implementation |
| Cross-project binding | Not yet production | Verified defining compilation | Real multiproject fixture | S2 independent review |
| Source ownership | Stage 1 native qualified | Real extraction and replacement | Native lifecycle tests | S2 independent review |
| Reader contract | Partial Stage 1 | Internal semantic query integration | Bounded query + coverage tests | S2 independent review |
| Coverage/refusal | Partial (IF-03) | Explicit semantic coverage | Negative/incomplete cases | S2 independent review |
| D3 | Synthetic corpus qualified | Real-project preservation | Before/after graph observations | S2 independent review |
| D4 | Partial native qualification | Preserve applicable invariants | Native publication evidence | S2 independent review |
| D5 | Stage 1 supported | No incompatible re-interpretation | Schema/refusal tests | CI-1A acceptance |
| SQ-02 (unknown outcome) | Qualified S1 boundary | Documented conservative behavior | REM-01 policy + native ack-loss test | S2.3/S2.5 |
| SQ-03 (concurrency) | Serialization only | Declared concurrency model | REM-01 model + native schedule test | S2.3/S2.5 |
| IF-03 (coverage) | Contract fixed by REM-01 | Full vocabulary defined | Contract + production coverage/reopen evidence | **Pre-S2 auth contract**; S2 acceptance evidence |
| Historical fixture (SQ-04) | Not generated | Immutable master + copies | Fixture hashes + refusal tests | S2 independent review |
| Cross-platform (SQ-05) | Windows only | Linux CI + tests | Linux CI pass | S2 independent review |
| Namespace mismatch test (SQ-06) | Missing | New test added | Test passing | S2 independent review |

---

## 13. Stage 2 Handoff

### Required Documentation Deliverables

| Document | Path | Status |
|----------|------|--------|
| Stage 2 boundary | `docs/planning/adr-ci1-01-ci1a-impl-01/stage2-boundary.md` | This document |
| Stage 2 acceptance matrix | `docs/planning/adr-ci1-01-ci1a-impl-01/stage2-acceptance-matrix.md` | Committed |
| Stage 2 handoff | `docs/planning/adr-ci1-01-ci1a-impl-01/stage2-handoff.md` | Committed |
| REM-01 reconciliation | `docs/planning/adr-ci1-01-ci1a-s2-rem-01/` | Committed |
| Updated roadmap | `ROADMAP.md`, `docs/agent-code-intelligence-roadmap.md` | To be updated |

### Next Commission (Not Authorized)

**Proposed:** `ADR-CI1-01-CI1A-IMPL-01-S2` — Roslyn Semantic Occurrence Extraction and Verified Provenance

**Prerequisites for authorization:**
1. Stage 1 owner acceptance recorded (this milestone)
2. Obligation reconciliation complete (this milestone)
3. Stage 2 specification reviewed independently
4. IF-03 coverage vocabulary defined in abstractions
5. S-01 provenance chain specified in Stage 2 spec
6. SQ-02/SQ-03 concurrency/unknown-outcome model declared in Stage 2 spec

---

## 14. Stop Conditions

Stop before authorizing Stage 2 implementation if:

- S-01 provenance chain cannot be expressed as a reproducible production trust chain.
- IF-03 coverage vocabulary not defined.
- Stage 2 requires an unapproved semantic identity change.
- Stage 2 requires breaking accepted Stage 1 native publication contracts.
- D3/D4/D5 guarantees would need to be weakened.
- A public Find Usages API becomes mandatory.
- Production code changes become necessary during this specification commission.

---

## 15. Final Disposition

**`ADR-CI1-01-CI1A-IMPL-01-S2 SPECIFIED — READY FOR INDEPENDENT SPECIFICATION REVIEW`**

Stage 2 boundary is defined with:
- Explicit permitted components and interfaces
- Functional changes bounded to Roslyn extraction → Candidate C publication
- S-01 provenance as central acceptance requirement
- Real-project fixture and independent qualification plan
- All exclusions enumerated
- No authorization to implement

The next independent specification review must verify this boundary before any Stage 2 implementation commission is authorized.