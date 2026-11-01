# ADR-CI1-01-CI1A-IMPL-01 — Candidate C Production Integration Specification

**Project:** Andirit
**Milestone:** ADR-CI1-01-CI1A-IMPL-01 — Candidate C production integration and
qualification specification.
**Status:** **SPECIFIED — READY FOR INDEPENDENT SPECIFICATION REVIEW.**
**Production implementation:** **NOT AUTHORIZED.**
**Architecture authority:** `docs/decisions/adr-ci1-01-owner-decision.md`
(Candidate C selected; B2 evaluated, not selected).
**Owner review acceptance:** `docs/decisions/adr-ci1-01-owner-01-q1-acceptance.md`.
**Spec base commit:** `ffdb14881511258c36d3fdce01bcdab3d2463ad9`.

This document specifies the work for a separate implementation agent and an
independent reviewer. It authorizes nothing; it must be read with the owner
decision, the Q1 review package, and the companion files
[`source-integration-map.md`](source-integration-map.md) and
[`acceptance-matrix.md`](acceptance-matrix.md).

---

## 1. Purpose

Define, precisely and without reopening the occurrence-storage architecture, how
Andirit production integrates **Candidate C: separate, source-owned occurrence
facts with native LatticeDB persistence**, preserving D3/D4/D5 and the accepted
`usage4`/`asmctx3` identity semantics.

## 2. Scope

In scope: C# method-invocation occurrences for supported lexical owners; typed
occurrence facts; source-unit ownership; atomic native publication; target-index
paging; coverage/refusal vocabulary; D3 noninterference; D5 no-migration
refusal + re-ingestion; production-backed qualification; evidence packaging.

**Out of scope (must not be added):** indirect impact / `GetChangeImpactV1`;
all references; runtime dispatch expansion; constructor/method-group/local-function
*selectable targets*; `changeKind`/impact path fields; B2; experimental-schema
migration or dual-write; background index services; distributed coordination;
unrelated code-graph features; broad storage-framework redesign.

## 3. Identity and nomenclature map

| Concept | Accepted contract (owner decision §F) | Production requirement |
| --- | --- | --- |
| Occurrence identity | `usage4` domain-separated SHA-256 tuple | implemented by a production identity module; not a new scheme |
| Target identity | `asmctx3` over `compctx1` + metadata + module + identifier digest + declaration digest + shape digest | same semantics |
| Compilation context | `compctx1` | constructed from the actual indexed source/compilation inputs (S-01) |
| Analysis scope | `analysisscope1` | declared unit set derived from the indexing run's project set |
| Universe | `compilation-universe1` | verified live compilations; binding requires a live instance |
| C fact format | `occurrence-facts/4` (experimental) | production schema version(s), new labels; not a byte-compatible copy |
| Legacy predecessor | `usage1…3` / `asmctx1…2` / `occurrence-facts/1…3` | rejected/superseded; never reinterpreted |

The implementation **must preserve the accepted identity semantics** and **must
not** substitute a simpler hash derived from documentation id, source path or
symbol descriptor, and **must not** treat a test-owned oracle as production
authority.

## 4. Production integration surfaces

The concrete surfaces, files, existing responsibilities, required changes,
unchanged contracts, new internal contracts, tests and dependencies are in
[`source-integration-map.md`](source-integration-map.md). Summary of required
additions:

1. Occurrence payload/coverage abstractions in `Andirit.Abstractions`.
2. Additive occurrence list on `CodeGraphBatch` and `CodeIndexUnitReplacement`.
3. `ICodeOccurrenceReader` (and, if needed, `ICodeOccurrenceIndexStore`) with
   in-memory and LatticeDB implementations.
4. Occurrence staging/completion folded into the existing
   `StageIndexUnitAsync`/`CompleteIndexRunAsync` path (one publication
   coordinator; one native transaction).
5. New LatticeDB labels + schema handling; additive snapshot section.

No production boundary beyond these is required; if one proves missing, the
implementation must specify the smallest addition and flag it for review.

## 5. Candidate C production contract

### 5.1 Occurrence facts

A production occurrence record must preserve the accepted semantics for:

- **Exact target identity** — `asmctx3` (defining compilation context + exact
  declaration correspondence).
- **Physical source site** — normalized relative path and one-based,
  end-exclusive UTF-16 span; identifier-token span and full-declaration span
  recorded with distinct meanings.
- **Source project unit** — the unit that produces/owns the fact.
- **Source content identity** — the exact source-byte hash used by the
  descriptor (S-01).
- **Invocation/reference category** — supported set for CI-1A (ordinary method
  invocation); unsupported categories recorded in coverage, not as facts.
- **Declaration and source coordinates** — perpendicular evidence (line/column),
  never the discriminator alone.
- **Publication identity** — the fact is meaningful only with its publication.
- **Contributor/proof information** — the set of `(plugin, unit)` contributors;
  immutable payload; sorted provenance.
- **Validated provenance** — refusal where correspondence is unproven.
- **Versioned contract and codec** — explicit version; unknown versions refused.

Occurrence identity must be **domain-separated, deterministic, length-prefixed**;
no wall clock, process id, object address, run id or enumeration order may
participate.

### 5.2 Source ownership

Occurrence facts belong to the source unit (`CodeFactOrigin`:
repository/plugin/unit/run/sourcePath/sourceHash) that produced them. Unit
replacement must retire obsolete facts belonging to that unit; deleting a unit
must remove its contributions **without deleting facts still supported by other
valid contributors** (contributor union semantics).

### 5.3 Conflict handling

The implementation must distinguish, with distinct outcomes:

| Situation | Required outcome |
| --- | --- |
| identical occurrence + compatible contributors | union (one fact, multiple contributors) |
| conflicting immutable payloads for one identity | reject publication (`conflict`), retain accepted state |
| duplicate publication attempt | idempotent no-op or explicit rejection; never duplication |
| obsolete/superseded generation | cannot replace a newer accepted generation |
| missing source evidence | refuse; record `AnalysisUnavailable` reason |
| unsupported identity context | refuse with explicit status |

An identity conflict **must not** be silently resolved by overwriting or merging
incompatible data.

### 5.4 Native persistence

Actual LatticeDB storage must represent occurrence facts, source units,
generations/publications, staging state, accepted publication authority,
target-index pages and contributor/provenance data using **new** labels (distinct
from the graph's `Hetu*` labels). All changes for a unit/run must complete in one
native write transaction so publication is atomic.

The experimental Candidate C implementation is **technical evidence**, not a
production API. Where the experiment differs from the production store (label
model, statement limits, transaction boundary, page encoding), the implementation
must adapt to the existing production store. **No graph-visible occurrence edges**
may be introduced.

## 6. D3 — Legacy graph noninterference

D3 is an accepted invariant. Occurrence facts must not appear as extra legacy
graph nodes/edges. Preserve: `FindCallers`; `FindReferences`; aggregate
relationships; graph identities; graph counts; native graph traversal; existing
graph consumers; relevant snapshots and historical graph behavior.

### 6.1 Required D3 corpus

Locate/compose the smallest existing-corpus combination covering: methods and
overloads; call relationships; references; cross-file and cross-project
relationships; contributor union; unit replacement; unit deletion; graph counts
and enumerations; native LatticeDB graph reads.

Candidate existing fixtures (to be confirmed by the implementer):
`tests/Andirit.CSharp.Tests/CSharpCodeGraphPluginTests.cs`
(e.g. `ExtractAsync_EmitsInheritanceImplementsAndCallsEdges`,
`ExtractAsync_CrossProjectCallsResolveToDependencySymbols`,
`ExtractAsync_RebindRemovesStaleCallRelationships`,
`ExtractAsync_RepeatedReferencesKeepFirstEvidenceAndVarIsNotUnresolved`),
`tests/Andirit.CSharp.Tests/CSharpCorrectnessRegressionTests.cs`,
`tests/Andirit.Tests/CodeGraphQueryServiceTests.cs`,
`tests/Andirit.LatticeDb.Tests/LatticeCodeGraphStoreTests.cs`, and
`Andirit.Testing.CodeGraphStoreConformanceSuite`. Missing negative cases (occurrence
invisibility, count invariance, native-mirror invariance) must be added — not a
new test framework.

### 6.2 Qualification

Capture legacy graph observations **before** occurrence ingestion and **after**:
(1) initial occurrence publication, (2) unit replacement, (3) unit deletion,
(4) reopen, (5) failed occurrence publication. Compare exact graph identities,
counts, query results and traversal results. **Any unapproved graph semantic
delta is a blocking CI-1A defect.**

---

## 7. D4 — Native publication and index invariants

The **ten accepted invariants are transcribed exactly** in
[`acceptance-matrix.md`](acceptance-matrix.md) §1 and must appear in the
implementation's own acceptance matrix without substitution. The matrix in
`acceptance-matrix.md` §2 binds each to a positive test, a negative/adversarial
test, an environment and the independent review stage.

Maintenance policy (accepted simple model): build/update the index within the
publication lifecycle; make it valid only when its publication identity is
corroborated; refuse stale/inconsistent state; permit explicit bounded
reconstruction; verify before serving. **No** background maintenance service,
distributed coordinator or automatic repair framework.

**S-02 requirement.** All ten invariants must be exercised against actual
production `LatticeCodeGraphStore` storage, including failed/cancelled/
superseded publication, reopen and a native concurrency/republication scenario.
In-process deterministic interleaving is retained as a diagnostic only and is
**not** accepted as native atomicity evidence.

## 8. D5 — Historical compatibility and re-ingestion

No migration of superseded experimental occurrence schemas; no dual-write with
B2; explicit unsupported-schema refusal; re-ingestion rebuild; do not reinterpret
historical aggregate edges as occurrence facts; do not auto-delete incompatible
databases; preserve historical experiment databases.

The implementation must keep the distinct states separate (see
`acceptance-matrix.md` §4): supported graph-only history; occurrence analysis
absent; unsupported experimental occurrence format; stale publication;
corrupted/malformed storage; new compatible publication. None may collapse into a
successful complete-empty result.

**Schema handling.** Schema-2 stores (graph-only) remain readable as before; an
occurrence query against them returns `AnalysisUnavailable`/reindex-required. The
new occurrence schema is a distinct version; unknown/unsupported versions are
refused (I-03 vocabulary). Re-ingestion writes the new compatible schema. The
aggregate/Hetu historical contract (ADR-CI1-05; EC6D-OWNER-02) is untouched.

## 9. Identity, provenance and analysis scope (S-01)

- The compilation descriptor (`compctx1`) must be constructed from the **actual
  indexed source bytes** and the **actual compilation used for binding**. A hash
  of arbitrary supplied bytes is insufficient (S-01).
- The analysis scope (`analysisscope1`) is the declared unit set for the run
  (derived from the run's project set); completeness is scope-relative.
- Confirmation requires a complete declared scope, exactly one live binding and
  exact declaration correspondence; otherwise refuse with a recorded reason.
- Source/options/reference/defining-unit changes change the context and require
  reindex; no prior identity remains authoritative.
- Calling unit owns the occurrence; the target identity comes from the defining
  unit.

## 10. Qualification and test strategy

- **Production-backed tests** for D3/D4/D5 against `LatticeCodeGraphStore` and
  `InMemoryCodeGraphStore`; conformance extended via
  `CodeGraphStoreConformanceSuite`.
- **Identity/provenance adversaries** (equal-descriptor, omitted/unresolved/
  unbound, linked contexts, cross-project positive, invalidation).
- **Determinism**: repeated extraction/relocation/reopen yield stable ids/order;
  separate-process stability.
- **Interleaving hook (I-05)**: the experimental `QueryInterleavingHook` must
  **not** exist in production; the production freshness re-check is internal.
- **Evidence packaging**: self-excluded, reproducible SHA-256 inventory of
  implementation and test evidence.
- **Independent review**: performed by a different agent/session; must reproduce
  D3/D4/D5 and verify principal query results come from production code, not a
  test oracle.

## 11. Implementation sequence (proposed)

1. **Abstractions** — occurrence payload/coverage/identity contracts.
2. **Ingestion** — additive batch/unit-replacement occurrence lists + validation.
3. **In-memory store** — occurrence staging/completion/reader (fast feedback).
4. **LatticeDB store** — new labels, atomic completion, reopen, schema handling.
5. **Native vertical slice** — the smallest production-backed gate (below).
6. **Query surface** — separate bounded occurrence reader.
7. **D3 corpus + D5 matrix**, then full D4 native matrix.
8. **Evidence packaging + independent review handoff.**

**Smallest native acceptance gate (do this first):** ingest one real C# project
unit → publish typed occurrence facts atomically to a real LatticeDB store →
query one target page (bounded/deterministic) → replace and delete the unit
(retiring pages/contributors) → reopen and reproduce the ordered result → inject
failed/stale publication → run the D3 graph-invariance corpus. This establishes
native publication/retrieval correctness and D3 without the full Find Usages
feature.

## 12. Deliverables

- Implementation across S1–S8 (see `source-integration-map.md`).
- Production-backed test suites and the D3 corpus.
- Self-excluded evidence manifest.
- An independent-review handoff.

## 13. Acceptance criteria

1. All D4 invariants verified against native storage (S-02).
2. D3 graph noninterference verified; no unapproved delta.
3. D5 states separated; unsupported schemas refused; re-ingestion works.
4. Accepted identity semantics preserved; S-01 provenance requirement met.
5. Coverage/refusal vocabulary pinned (I-03); interleaving hook absent (I-05);
   maintenance/cancellation policy specified (I-04); D3 corpus named (I-02);
   integration surfaces pinned (I-01).
6. Independent review reproduces the matrices and does not rely on producer
   tests as its own qualification.

## 14. Explicit exclusions and final disposition

This specification does **not** authorize implementation, B2, migration,
background services, distributed coordination, unrelated code-graph features, or
public-query API changes beyond the separate occurrence reader. It must not
reopen the occurrence-storage architecture decision.

**`CI1A-IMPL-01 SPECIFIED — READY FOR INDEPENDENT SPECIFICATION REVIEW`.**
