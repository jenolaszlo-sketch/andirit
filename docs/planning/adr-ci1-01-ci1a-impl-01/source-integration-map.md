# Source-integration map — ADR-CI1-01-CI1A-IMPL-01

**Authority:** `docs/decisions/adr-ci1-01-owner-decision.md` (Candidate C).
**Reviewed against:** canonical `D:\projects\Andirit` at `ffdb148` (spec base).
**Status:** specification input. **No implementation authorized.**

All type names, namespaces and paths below are the current canonical production
identities (namespace `Andirit` throughout). Experimental adapter names
(`CLatticeOccurrenceStore`, `B2Backend`, `OccurrenceV3`, etc.) do **not** exist in
production and are technical evidence only, not APIs.

## 1. Integration surfaces

### S1 — C# semantic extraction and project-unit ownership
- **Existing component:** `CSharpCodeGraphPlugin` (+ `.Session`, `.GraphBuilder`,
  `.Compilation`, `.Symbols`, `.PropertyFactory`), `CSharpProjectDiscovery`.
- **Files:** `src/Andirit.CSharp/CSharpCodeGraphPlugin*.cs`,
  `src/Andirit.CSharp/CSharpProjectDiscovery.cs`.
- **Existing responsibility:** discover `*.csproj`, build Roslyn compilations,
  emit nodes/declarations/edges through the sink; unit id
  `csharp:project:{IdentityHash(projectPath)}`
  (`CSharpProjectDiscovery.IndexUnitId`); source hashing
  (`CSharpProjectDiscovery` SHA-256).
- **Required CI-1A change:** additionally extract typed occurrence facts from the
  **same live compilation** used for relationship binding; construct compilation
  descriptors (`compctx1`) from the **actual indexed source bytes**; compute
  target identity (`asmctx3`) and occurrence identity (`usage4`).
- **Must remain unchanged:** existing node/edge emission, ids, normalization,
  coverage counters, unit identity.
- **New internal contract:** occurrence extraction + identity/provenance module
  (proposed `Andirit.Occurrences` namespace inside `Andirit`, or an
  `Andirit.Abstractions` contract + `Andirit`/`Andirit.CSharp` implementation).
- **Acceptance tests:** identity/provenance adversaries (equal-descriptor
  compilations, omitted/unresolved/unbound, linked contexts, cross-project
  positives).
- **Dependencies:** S2 occurrence payload contracts.

### S2 — Graph-fact batch, origin and ingestion sink
- **Existing component:** `CodeGraphBatch`, `CodeFactOrigin`,
  `CodeGraphBatchLimits`, `ICodeGraphSink`, `CodeGraphIngestionSink`,
  `CodeGraphBatchValidator`.
- **Files:** `src/Andirit.Abstractions/CodeGraphBatch.cs`,
  `src/Andirit/CodeGraphIngestionSink.cs`, `src/Andirit/CodeGraphBatchValidator.cs`.
- **Existing responsibility:** validate bounded batches; accumulate per index
  unit; stage one `CodeIndexUnitReplacement` when `CompletesIndexUnit`.
- **Required CI-1A change:** carry an additive occurrence-fact list on the batch
  and on the unit replacement; validate occurrence payload/identity/limits.
- **Must remain unchanged:** existing limits, node/edge validation, origin
  equality, rejection semantics.
- **Acceptance tests:** occurrence validation (identity format, category, span,
  duplicate identity in unit, payload conflict).
- **Dependencies:** S1, S3.

### S3 — Publication lifecycle and store contract
- **Existing component:** `ICodeGraphIndexStore` (`StageIndexUnitAsync`,
  `StageIndexUnitDeletionAsync`, `CompleteIndexRunAsync`),
  `ICodeGraphReader`, `CodeIndexingService` (`CodeIndexingLifecycle.cs`),
  `CodeGraphPublication`, `CodeRepositoryIndexState`, `CodeIndexIdentity`,
  `CodeSourceManifest`, `CodePublicationSnapshot`.
- **Files:** `src/Andirit/ICodeGraphStore.cs`,
  `src/Andirit/CodeIndexingLifecycle.cs`, `src/Andirit/CodeGraphStoreModels.cs`,
  `src/Andirit/CodeIndexIdentity.cs`, `src/Andirit/CodeSourceManifest.cs`,
  `src/Andirit/CodePublicationSnapshot.cs`.
- **Existing responsibility:** stage unit replacements; publish atomically on
  `CompleteIndexRunAsync`; publish incremental source state; bind results to a
  publication.
- **Required CI-1A change:** stage/complete occurrence facts **within the same
  unit and the same completion transaction** so publication is atomic; add an
  **additive** occurrence section to `CodePublicationSnapshot` (new schema
  version); expose an occurrence reader distinct from `ICodeGraphReader`.
- **Must remain unchanged:** graph publication semantics, source-state model,
  `CodeIndexIdentity` composition, legacy reader surface.
- **New contract:** `ICodeOccurrenceReader` (and, if needed,
  `ICodeOccurrenceIndexStore`), plus `CodeOccurrenceFact` and
  `CodeOccurrenceCoverage` abstractions.
- **Acceptance tests:** D4 invariants (see `acceptance-matrix.md`).
- **Dependencies:** S2, S4.

### S4 — Store implementations and native persistence
- **Existing component:** `InMemoryCodeGraphStore`, `LatticeCodeGraphStore`,
  `NativeGraphMirror`, `LatticeDbStoreOptions`.
- **Files:** `src/Andirit/InMemoryCodeGraphStore.cs`,
  `src/Andirit.LatticeDb/LatticeCodeGraphStore.cs`,
  `src/Andirit.LatticeDb/NativeGraphMirror.cs`,
  `src/Andirit.LatticeDb/LatticeDbStoreOptions.cs`.
- **Existing responsibility:** stage/publish graph tables; LatticeDB schema
  version `CurrentSchemaVersion = 2`; labels `HetuMetadata`, `HetuRepository`,
  `HetuRun`, `HetuIndexState`, `HetuUnit`, `HetuStage`, `HetuBaseline`; native
  write transactions (`BeginWriteTransaction`) and read transactions.
- **Required CI-1A change:** persist occurrence facts, source-unit ownership,
  publication authority, target-index pages and contributor/provenance data in
  **new** labels; complete in one native write transaction; support reopen;
  accept schema 2 read-only (occurrence-unavailable) and the new occurrence
  schema; refuse unknown/unsupported versions.
- **Must remain unchanged:** graph labels/semantics, schema-2 graph readability,
  native mirror for the graph.
- **Acceptance tests:** native publication/refusal/reopen/concurrency; D3 graph
  noninterference.
- **Dependencies:** S3.

### S5 — Query services
- **Existing component:** `CodeGraphQueryService`, `CodeGraphPublicationQuery`,
  `CodeGraphTraversalPresets` (internal), `CodeGraphQueryEnvelope`.
- **Files:** `src/Andirit/CodeGraphQueryService.cs`,
  `src/Andirit/CodeGraphPublicationQuery.cs`,
  `src/Andirit/CodeGraphTraversalPresets.cs`, `src/Andirit/CodeQueryOperations.cs`.
- **Existing responsibility:** `FindCallers`/`FindReferences` via the `Calls` /
  `References` presets; bounded traversal; publication-bound envelopes.
- **Required CI-1A change:** add a **separate** occurrence query surface
  (target-scoped, bounded, deterministic). Do **not** modify
  `FindCallers`/`FindReferences` or add occurrence kinds to traversal presets.
- **Must remain unchanged:** all existing query signatures, results and budgets.
- **Acceptance tests:** D3 noninterference; D4 bounded/ordered/refusal.
- **Dependencies:** S3, S4.

### S6 — Coverage, availability and errors
- **Existing component:** `CodeGraphExtraction` (`CodeRelationshipCoverage`,
  `CodeIndexUnitCoverage`, `CodeGraphExtractionResult`), `CodeFreshness`,
  `CodeGraphStoreException`, `CodeGraphPublicationChangedException`.
- **Files:** `src/Andirit.Abstractions/CodeGraphExtraction.cs`,
  `src/Andirit/CodeFreshness.cs`, `src/Andirit/CodeGraphStoreException.cs`,
  `src/Andirit/CodeGraphPublicationChangedException.cs`.
- **Required CI-1A change:** occurrence coverage (disjoint categories) and the
  analysis-unavailable/unsupported-schema/stale vocabulary (I-03).
- **Must remain unchanged:** existing coverage meanings and counters.
- **Dependencies:** S3.

### S7 — Host composition
- **Existing component:** `AndiritHost`, `AndiritHostBuilder`,
  `CodeGraphPluginRegistry`, `CodeLanguageProviderMap`.
- **Files:** `src/Andirit/AndiritHost.cs`, `src/Andirit/CodeGraphPluginRegistry.cs`,
  `src/Andirit/CodeLanguageProviderMap.cs`.
- **Required CI-1A change:** expose the occurrence reader from the composed host
  (`AndiritHost`), additive.
- **Must remain unchanged:** `Indexing`, `Queries`, `Reader` public members.
- **Dependencies:** S3, S5.

### S8 — Conformance and test fixtures
- **Existing component:** `CodeGraphStoreConformanceSuite`,
  `ICodeGraphStoreFixture` (`src/Andirit.Testing/`); test projects under
  `tests/`.
- **Required CI-1A change:** extend conformance to the occurrence store view
  across `InMemoryCodeGraphStore` and `LatticeCodeGraphStore`; add the D3 corpus.
- **Must remain unchanged:** existing conformance assertions/signatures.
- **Dependencies:** S4.

## 2. Unchanged legacy contracts (D3 pin list)

`ICodeGraphReader`; `FindCallers`/`FindReferences`; `CodeGraphTraversalPresets`;
`calls`/`references` ids and cardinality; `ICodeGraphIndexStore` graph
staging/completion semantics; `InMemoryCodeGraphStore.TraverseAsync`;
`LatticeCodeGraphStore` graph labels and schema-2 readability;
`NativeGraphMirror`; `CodePublicationSnapshot` graph sections (graph section
unchanged; occurrence section additive).

## 3. Smallest required additions

1. Occurrence payload/coverage/identity abstractions (`Andirit.Abstractions`).
2. Additive occurrence list on `CodeGraphBatch` and `CodeIndexUnitReplacement`.
3. `ICodeOccurrenceReader` (+ optional `ICodeOccurrenceIndexStore`) and its
   in-memory/Lattice implementations.
4. Occurrence staging/completion folded into the existing `StageIndexUnitAsync` /
   `CompleteIndexRunAsync` path (no second publication coordinator).
5. New LatticeDB labels + schema handling; additive snapshot section.

No new library, no generalized index framework, no background service.
