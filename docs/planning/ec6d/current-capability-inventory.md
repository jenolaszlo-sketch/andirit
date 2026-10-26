# Current consumer capability inventory

## Assessment baseline

The detailed inventory below describes the owner-accepted EC-6C candidate at
2c3090cba6d8a56102a5c47ceb026c5b5fea3234, not canonical main. The two are
distinct: main remains at 558f8ac82ac1b5e07da68f4a71a70240eaf38409 and does
not yet contain the accepted EC-6B/EC-6C chain. The candidate is the assessment
baseline because it combines the accepted implementation with owner-approved
documentation without rewriting prior history. Integration remains
unauthorized.

Terms used here follow the provider evidence model:

1. **Syntax observed:** a parser found source syntax.
2. **Structural:** a relationship is based on that syntax; it does not by
   itself establish a compiler or runtime relationship.
3. **Resolved:** a provider linked facts under its documented analysis
   context. This claim is bounded to that context.
4. **Derived:** a deterministic documented pattern or recovery produced a
   candidate relationship.
5. **Semantically verified:** use only where a provider's analysis context
   supports the specific claim. Do not promote a name match to this category.

## Language profiles

| Profile | Classification | Emitted facts and relationships | Resolution and limitations |
| --- | --- | --- | --- |
| C# / Roslyn | SUPPORTED WITH QUALIFICATIONS | Projects, files, namespaces, types, interfaces, delegates, callables, properties, fields, parameters, declarations and semantic containment; structural imports; resolved references, calls, heritage/implementation and project/package dependencies | Uses a constructed Roslyn compilation per discovered SDK-style project. Project discovery is intentionally lightweight: MSBuild conditions, imports, custom targets and solution configurations are not evaluated. References use host trusted-platform assemblies, so a different target framework may be analyzed against the host .NET 10 surface. |
| TypeScript / Tree-sitter | PARTIALLY SUPPORTED | Documented declaration/member subset, imports, invocation names, structural inherits/implements; p8 policy-scoped relative module-file resolution; p9 positive-only export entries and exports edges | Structural heritage targets are path-scoped placeholders, not declaration identities. Module resolution stops at a file. Export entries have no SymbolId and are not bound to declarations, imports, or re-exports. No calls or references edges. |
| JavaScript / Tree-sitter | PARTIALLY SUPPORTED | Documented declaration/member subset, imports, invocation names, structural inheritance; Derived edges for recognized prototype patterns; p8 relative module-file resolution; p9 positive-only export entries | Prototype relationships are pattern-derived, not resolved. Inline type-like export recovery remains conservative and does not prove runtime validity. No calls or references edges, CommonJS export graph, or import binding. |
| Elixir / Tree-sitter | SUPPORTED WITH QUALIFICATIONS | Documented structural subset of module/function/macro declarations, containment, imports and invocation names | No resolved calls or references; no inheritance/implementation relationship. Grammar-based structural facts only. |
| Python, Java, Go, Rust, Kotlin | NOT SUPPORTED | No production plugin is available in this candidate | Planned roadmap entries are not executable capability. |

Tree-sitter languages share a syntax-based extraction profile and pinned
grammars. Malformed regions can retain valid facts while diagnostics report
parse problems. A syntactically observed relationship must not be described
as complete language semantics.

## Graph and relationship facts

| Capability | Classification | Consumer-visible behavior |
| --- | --- | --- |
| Declarations and containment | SUPPORTED WITH QUALIFICATIONS | Exact source-backed entities and physical declaration spans are available for the documented provider subsets. Identity quality differs by provider; Tree-sitter class/member identities are path and owner scoped. |
| C# calls and references | SUPPORTED WITH QUALIFICATIONS | Roslyn-bound relationships include source location, provider and version within the constructed compilation. Dynamic dispatch, external symbols, framework mismatch and lightweight project discovery limit completeness. |
| TypeScript/JavaScript calls and references | NOT SUPPORTED | Invocation spellings may appear as invokes-name edges to declaration-less callable candidates. They do not identify the callee. No calls or references are emitted. |
| Explicit TypeScript implements / extends | PARTIALLY SUPPORTED | Written heritage creates Structural edges to path-scoped name placeholders. It does not bind aliases, imports, same-name declarations, or declaration identity. Implicit interface satisfaction is not analyzed. |
| JavaScript prototype heritage | SUPPORTED WITH QUALIFICATIONS | A bounded list of recognized idioms can create Derived inherits edges. Dynamic mutation, shadowing and runtime behavior are outside the pattern. |
| Imports | SUPPORTED WITH QUALIFICATIONS | Written import facts are structural. C# preserves alias/static/global/scope details. ECMAScript imports retain syntax; no imported-local-symbol binding is produced. |
| ECMAScript resolves-to | SUPPORTED WITH QUALIFICATIONS | EC-6B's relative-v1 policy links importing files to targets in the same plugin inventory under ordered exact/extension/index rules. Resolved means verified under this policy, not TypeScript compiler, bundler, or runtime equivalence. |
| ECMAScript exports | SUPPORTED WITH QUALIFICATIONS | EC-6C emits an export node per distinct supported module/name and a Structural file-to-entry edge per supported occurrence. It is positive-only, has no declaration identity, and excludes supported consumer claims about completeness or import binding. |
| Re-export binding | NOT SUPPORTED | Re-export source modules can have module-file resolver edges; the export entries do not publish the imported name as a bound symbol origin. Star/namespace closure and alias origin tracing are absent. |
| Project/package depends-on | SUPPORTED WITH QUALIFICATIONS | C# project references and package declarations are represented. Conditions are recorded, not evaluated; no general transitive dependency closure is promised. |

The accepted ECMAScript export profile includes an owner-corrected
qualification: selected inline recovered forms may appear as Structural
occurrences with unknown projection and visible parser error. This is not a
runtime-validity assertion. See the owner decision and risk register.

## Provenance, identity and persistence

- Nodes, declarations and edges can be returned with provider/version,
  contributing index unit, source location, evidence quality and properties.
  Provenance-aware envelopes bind a query to a successful publication and the
  applied query.
- Source locations are one-based, end-exclusive UTF-16 code-unit positions.
  They locate evidence; the query API does not return source excerpts.
- Exact qualified-name lookup preserves multiple candidates and ambiguity.
  Tree-sitter syntax placeholders make same-spelling name matches especially
  unsafe as a substitute for identity.
- Stable IDs are provider-defined. C# uses Roslyn semantic identity with
  physical declaration records; Tree-sitter uses path-scoped structural
  identities; export entry IDs are module/name scoped. Profile version
  changes invalidate/re-extract units.
- InMemory and LatticeDB implement the same public graph-store contract.
  LatticeDB is durable and supports close/reopen; EC-6C store qualification
  was made through that public contract. The current baseline harness uses
  only InMemory.
- Indexing supports content-addressed plans, incremental unit replacement,
  atomic publication, freshness checks and publication snapshots. A query
  view binds to the latest publication but does not retain graph history.
  There is no graph-diff API.

## Queries and traversal

The provider-neutral query service provides exact symbol lookup, declarations,
references, callers, callees, implementations, dependencies, dependents,
bounded neighborhoods, impact sets, file declarations and a project-rooted
public surface. Query envelopes expose ambiguity and provenance. Traversals
bound depth, nodes, edges and evidence kinds and report truncation.

Impact presets include selected incoming references, calls, implements,
inherits and depends-on facts; they exclude imports, exports and resolves-to.
The generic neighborhood can explicitly filter relationship kinds, but
TypeScript's current placeholders and unbound imports/exports still limit what
the traversal can mean. The Tree-sitter extractor does not emit a Project node,
so the project-rooted public-surface helper cannot supply the TypeScript
surface by itself. Exact qname lookup can mix declaration, export and
heritage-target candidates; consumers must filter and disambiguate by kind
and provenance.

## Support, persistence and operational qualifications

| Area | Classification | Evidence and limit |
| --- | --- | --- |
| Incremental re-indexing | SUPPORTED WITH QUALIFICATIONS | Unit replacement, deletion, empty replacement, failed/cancelled publication retention and deterministic planning are covered by provider/store tests. The p6-to-p8 and p8-to-p9 migrations have separate historical evidence. |
| Determinism | SUPPORTED WITH QUALIFICATIONS | Stable identity and ordering are tested under documented scenarios, including reversed enumeration and LF/CRLF. The EC6D fixture's fact digest repeated exactly; this is not a guarantee for untested providers or environments. |
| Durable reopen | SUPPORTED WITH QUALIFICATIONS | LatticeDB public contract and historical migration reports cover tested close/reopen cases. The EC6D query baseline itself uses InMemory and is not durability evidence. |
| Performance and scale | NOT EVALUATED | No large-repository benchmark, memory profile or query-latency baseline was collected for this planning task. |
| Diagnostic completeness | SUPPORTED WITH QUALIFICATIONS | Provider counters and stable diagnostics expose bounded unresolved/external/unsupported cases. Absence of a fact is not proof that no source relationship exists. |

## Evidence anchors

- Core query surface: src/Penghou.Hetu/CodeGraphQueryService.cs,
  src/Penghou.Hetu/CodeGraphQueryModels.cs, and
  tests/Penghou.Hetu.Tests/CodeGraphQueryServiceTests.cs.
- Publication, identity and indexing: src/Penghou.Hetu/CodePublicationSnapshot.cs,
  src/Penghou.Hetu/CodeIndexingLifecycle.cs, and
  src/Penghou.Hetu/CodeIndexPlanner.cs.
- Tree-sitter profiles and resolution: src/Penghou.Hetu.TreeSitter/EcmaScriptExtractionProfile.cs,
  src/Penghou.Hetu.TreeSitter/TreeSitterModuleResolution.cs, and
  src/Penghou.Hetu.TreeSitter/EcmaScriptExportGraphPublisher.cs.
- Provider contracts and audited matrix: docs/language-feature-matrix.md,
  docs/language-support-roadmap.md, docs/ecmascript-module-resolution.md,
  and docs/decisions/ec6c-owner-acceptance.md.
- Reproducible exercise: [consumer-baseline.md](consumer-baseline.md).
