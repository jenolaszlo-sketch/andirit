# Changelog

Notable changes to Penghou.Hetu are recorded here. The project follows
[Semantic Versioning](https://semver.org/) for package versions. Preview
releases may still revise public contracts.

## Unreleased

Phase 0: ECMAScript file-level variable declarations (EC-5):

- TypeScript and JavaScript now emit named top-level `const`, `let`, and `var`
  declarators as `field` nodes with file containment/declaration edges, exact
  physical declarator locations, and existing `modifiers` metadata. Directly
  written `export` is retained as a modifier; export lists, bindings, type
  resolution, constant evaluation, local scopes, and destructuring remain
  unsupported. Function-valued variables retain the previous callable
  projection, and initializer calls keep their prior lexical attribution.
- Variable identities use the `variable` discriminator and source path plus
  written name. Duplicate same-file occurrences share a candidate node while
  each physical declaration preserves its location and written metadata; this
  does not claim declaration merging. The shared ECMAScript profile moves to
  revision 6 so p5 source units invalidate and re-extract.
- Deterministic TypeScript/JavaScript fixtures, incremental published-graph
  tests, and the EC-4 website commit comparison are recorded in
  `docs/evidence/ec5-variable-declarations-handoff.md`.

Phase 0: ECMAScript heritage target identity correction (EC-3D):

- Written `extends` and `implements` names now use file-scoped declaration-less
  `type` nodes in the `language:heritage-target:` identity namespace. Their
  spelling and relationship provenance remain available, and same-name class
  or interface declarations stay separate. Heritage edges remain Structural;
  recognized prototype-pattern edges remain Derived.
- The Tree-sitter profile cannot bind heritage names semantically. Revision 5
  changes both ECMAScript plugin versions so p4 manifests invalidate and units
  re-extract. Ordinary reindexing removes old heritage targets and edges.
- The GLM review's three minor observations are documented without expanding
  implementation scope: static-block calls are attributed to the class node,
  decorator calls to the decorated member node, and repeated same-name members
  with the same kind and ownership converge while retaining each declaration.

Phase 0: ECMAScript class identity and class members (EC-3A/3B/3C):

- Class declarations now bind the source path like interfaces, and class
  members derive identity from the owning class node plus member kind and
  static/instance ownership, so same-name classes in separate modules, members
  of unrelated classes, static versus instance members, methods versus
  accessors, and class/interface collisions can never silently converge.
  Nested class declarations retain their owner; duplicate same-file class
  declarations still converge by name with one declaration each, documented as
  a limitation because structural extraction cannot verify declaration
  merging. Top-level functions, enums, namespaces, imports, and invocation
  placeholders keep the previous name-based model.
- Heritage targets use separate file-scoped declaration-less identities, so
  written base and interface names never converge with same-name declarations.
- TypeScript `public_field_definition` and JavaScript `field_definition` nodes
  now emit `property` members with exact declarations, `contains`/`declares`
  edges, and written metadata: sorted `modifiers` (`abstract`, `declare`,
  `optional`, `override`, `readonly`, `static`) and an explicitly written
  `access` keyword without fabricated defaults. Field initializer invocations
  attribute to the field instead of the class. Computed, string, numeric, and
  `#private` names and `accessor` auto-accessor fields are skipped; accessors
  remain callable members. Partial recovery emits no fabricated members.
- The shared ECMAScript extraction-profile revision moved to 3 (identity),
  4 (fields), and 5 (heritage identity correction). Effective plugin versions
  fold it, so previous unit manifests invalidate and re-extract. Class identity,
  member, lifecycle, and heritage regression tests cover collisions, nesting,
  modifiers, initializers, unsupported members, partial parsing, deterministic
  repetition, exact member and enclosing-symbol queries, member/class rename
  and removal, stale-edge cleanup, file deletion, and same-name classes across
  modules.

Phase 0: TypeScript interface member coverage:

- Interface bodies now emit identifier-named method signatures as `callable`
  nodes and property signatures as `property` nodes with `contains`/`declares`
  edges, exact source locations, and `readonly`/`optional` `modifiers` from the
  written `?` and `readonly` tokens. Computed, string, numeric, and private
  member names and call/construct/index signatures are skipped rather than
  guessed; partially recovered signatures emit no facts.
- Interface declarations and their members use path-scoped symbol identities
  with interface-specific discriminators (`interface`, `interface-method`,
  `interface-property`), so unrelated same-name interfaces in separate modules
  stay distinct and a signature never converges with a same-named class member.
  Same-name interfaces in one file still converge by name, and method overloads
  remain one callable symbol with one physical declaration per signature. The
  design and remaining limitations are recorded in
  `docs/ecmascript-symbol-identity.md`.
- The shared ECMAScript extraction-profile revision moves to 2. Effective
  plugin versions fold it, so previous TypeScript and JavaScript unit manifests
  invalidate and re-extract; JavaScript emits unchanged facts. New TypeScript
  fixtures cover members, modifiers, overloads, duplicate names, module
  separation, unsupported members, partial parsing, deterministic repetition,
  incremental stale-fact removal, exact declaration/containment queries, and
  plugin-version invalidation.

Phase 0 capability correction (language feature matrix):

- The TypeScript, TSX and JavaScript descriptors now advertise the
  `Inheritance` capability, matching the structural `inherits`/`implements`
  and Derived prototype `inherits` edges they already emit. Elixir
  deliberately does not advertise `Inheritance` because it emits no such
  facts. New `TreeSitterCapabilityContractTests` bind each advertised flag to
  produced relationship coverage and preserve the Structural/Derived evidence
  distinction.
- Capability flags are live plugin metadata and changed no emitted facts, so
  no extraction-profile revision was bumped. Plugin versions continue to fold
  package, grammar, ABI and extraction-profile components, so previous index
  manifests invalidate when any component changes.

Phase 3B: Tree-sitter structural extraction for TypeScript and JavaScript
(`docs/evidence-quality-and-provider-strategy.md`):

- New `TreeSitterTypeScriptPlugin` (`penghou.hetu.tree-sitter.typescript`,
  language `typescript`, `.ts`/`.mts`/`.cts`/`.tsx`) and
  `TreeSitterJavaScriptPlugin` (`penghou.hetu.tree-sitter.javascript`,
  language `javascript`, `.js`/`.mjs`/`.cjs`/`.jsx`). Registration helpers
  `AddTreeSitterTypeScriptPlugin` and `AddTreeSitterJavaScriptPlugin` were
  added to `HetuHostBuilderExtensions`.
- `.tsx` uses the sibling `tree-sitter-tsx` grammar because JSX changes how
  type assertions parse; JavaScript uses `tree-sitter-javascript`. All three
  grammar pins (versions, commits, ABI) are recorded on the plugins'
  `Grammar` descriptors and in `native/grammar-pins.md`. TypeScript and
  JavaScript natives are supplied by the pinned `TreeSitter.DotNet` binding.
- One `EcmaScriptExtractionProfile` serves both languages: classes,
  interfaces, namespaces, type aliases, enums, functions, methods, and
  arrow/function-expression callables as declarations with containment;
  ES module `import`, re-export, and CommonJS `require` as import edges;
  `call_expression`/`new_expression` names through `invokes-name`; and
  `inherits`/`implements` from class and interface heritage (TypeScript) and
  `extends` (JavaScript). Never `calls`, `references`, or type information;
  JSX elements are not invocations. Malformed regions keep valid facts and
  report `<language>.parse-error` with partial coverage.
- Class/interface heritage is `Structural`. The common prototype-inheritance
  idioms (`X.prototype = Object.create(Y.prototype)`, `X.prototype = new Y()`,
  `util.inherits(X, Y)`, `Object.setPrototypeOf(X.prototype, Y.prototype)`)
  are recognized and emitted as `inherits` with `Derived` evidence, never
  structural or resolved. Relationship coverage is now profile-driven so
  `inherits`/`implements` report `Produced` for TypeScript and `NotProduced`
  for Elixir and (for `implements`) JavaScript.
- The shared extraction session, grammar-version scaffolding, and
  profile interface were factored out of the Elixir plugin so the second
  language added a grammar descriptor and a profile rather than new session
  logic; Elixir behavior and identities are unchanged.

Milestone 8A: dogfood query ergonomics:

- Deterministic symbol discovery (`CodeSymbolDiscoveryQuery`) scoped by
  name pattern, node kind, language, containing symbol, or project, with
  explicit truncation; Solo ranks the candidates.
- Source-location to enclosing-symbol lookup bridging file positions into
  graph identities.
- Symbols by file/project and declarations by project alongside the existing
  declarations-by-file surface. No new store contracts: all three compose
  traversal, lookup, and declaration primitives.
- Elixir symbol nodes now carry the `language` property so discovery
  filters apply uniformly; Elixir declarations cover whole definitions.
  C# and Elixir dogfood plus traversal-policy evidence in
  `tests/Penghou.Hetu.TreeSitter.Tests/QueryDogfoodTests.cs`.

Phase 3A: Tree-sitter structural extraction for Elixir
(`docs/evidence-quality-and-provider-strategy.md`):

- New `Penghou.Hetu.TreeSitter` package with a Tree-sitter Elixir plugin
  (`penghou.hetu.tree-sitter.elixir`, language `elixir`, `.ex`/`.exs`).
  Native runtime via the `TreeSitter.DotNet` binding; the official
  `tree-sitter-elixir` grammar (v0.3.5, ABI 14) ships pinned per runtime
  (`win-x64`, `linux-x64`, `osx-arm64`) with no runtime downloads. See
  `src/Penghou.Hetu.TreeSitter/native/grammar-pins.md`.
- The Elixir profile emits structural evidence only: module/function
  declarations with containment, alias/import edges, and invocation names
  through `invokes-name`. Never `calls`, `references`, or type information.
  Malformed regions keep valid facts and report `elixir.parse-error` with
  partial coverage.
- Grammar identity (source, version, commit, ABI, capabilities) is recorded
  on the plugin's `TreeSitterGrammarDescriptor`; the pinned ABI is validated
  at load (`VersionMismatch` otherwise), and unsupported runtimes report
  `ProviderUnavailable`.
- No Tree-sitter, binding, or native concept appears in the package's public
  API surfaced contracts.

Phase 2: explicit language-provider priority and fallback
(`docs/evidence-quality-and-provider-strategy.md`):

- New `CodeLanguageProviderMap` (language to preferred-first plugin order,
  no numeric priorities), configured with `WithLanguageProviders` on
  `HetuHostBuilder`/`HetuBuilder`. Without a map the strict registry
  contract is unchanged: zero matches select nothing, several throw
  `CodeGraphPluginSelectionException`.
- New `CodePluginFailureKind` taxonomy and `CodePluginFailureException` for
  classified provider failures, plus `ProviderFallbackPolicy`
  (`None`/`OnUnavailable`/`OnFailure`, default `OnUnavailable`). Fallback
  handles unavailable capability, never suspicious analysis, cancellation,
  or live-source conflicts.
- Fallback executes the next configured provider over the union of sources,
  discards the failed provider's staged and previously published units,
  rewrites plan manifests to the recovering runner, and records a
  `CodeProviderSelection` event (full attempt history, classified reason,
  lost capabilities — never reduced to an evidence-quality transition).
- Cross-run ownership hygiene: published units whose owner holds no path in
  the new state are collected at completion (e.g. fallback units after the
  preferred provider recovers).
- Fallback targets of executing plugins are deferred to a sequential phase
  so a takeover runs once; `calls`-style honesty rules from Phase 1 apply to
  every attempt. Store and snapshot schema versions are unchanged.

Phase 1 of the evidence-quality and provider strategy
(`docs/evidence-quality-and-provider-strategy.md`):

- `CodeEvidenceKind` (`Syntax/Semantic/Heuristic/AiInferred`) is replaced by
  `EvidenceQuality` (`Structural/Resolved/Derived/Inferred`). Resolved means
  resolved-by-the-named-provider in its analysis context, not universally
  exact. Confidence remains forbidden on deterministic evidence and allowed
  on inferred evidence.
- New `invokes-name` edge kind for structural invocation evidence. `calls`
  is reserved for resolved calls: the batch validator rejects a `calls`
  edge without resolved evidence
  (`edge.evidence.calls-requires-resolved`).
- The C# plugin now honestly reports references, calls, inheritance,
  imports, declarations, and diagnostics capabilities (new
  `Declarations`/`Diagnostics` capability bits). Declarations, containment,
  and package/project-structure edges are structural evidence;
  `exercised-by` is derived evidence.
- `CodeGraphQueryOptions` and `CodeGraphTraversalQuery` filter on
  `EvidenceQualities` instead of evidence kinds.
- Persistence versions bump (`LatticeCodeGraphStore` and publication
  snapshots move to schema 2): older files and snapshots are rejected
  rather than reinterpreted. Preview releases may revise public contracts.

## 0.2.0-preview.6

- Test-to-production mapping: the C# plugin marks projects that reference an
  exact test-framework package allowlist with a `test-project` property, and
  derives one bounded `exercised-by` edge from each resolved production target
  to the allowlisted test method that calls it (new
  `CodeEdgeKinds.ExercisedBy` and `CodePropertyKeys.TestProject`). External or
  ambiguous callees still never produce an edge.
- Extraction-throughput benchmark baseline recorded in `BENCHMARKS.md`
  (`ExtractionBenchmarks`); the manual benchmark workflow now runs it.

## 0.2.0-preview.5

Milestone 7.5 (useful semantic relationships) is complete:

- C# semantic relationships with honest evidence: explicit base-type
  inheritance and interface implementation, ordinary/implicit/constructor
  calls, smallest-owner references, attributes, and scoped imports with
  alias, `static`, and `global` metadata. No transitive closure, no guessed
  targets.
- Coverage contract: per-kind candidate, internal, cross-project, external,
  ambiguous, unsupported, and emitted counts, plus per-index-unit entries, so
  consumers can distinguish an empty result from an incomplete index.
- Store conformance relationship-kind checks alongside plugin tests; repeat
  and incremental determinism including coverage; stale-relationship removal
  on rebind.
- Tier-A ride-alongs: `<summary>`/`<remarks>` doc properties, modifier and
  attribute properties with test-framework and route allowlists, constant
  literals, package-reference nodes, and solution scoping with diagnostics
  for unevaluated solution constructs.
- Coverage thresholds enforced in CI; LatticeDB benchmark baseline refreshed
  for LatticeDbSharp 0.2.0.

## 0.2.0-preview.1 – 0.2.0-preview.3

Foundation: portable graph identities, vocabulary, properties, evidence, and
locations; extraction sessions, batches, and validation; deterministic
repository discovery, hashing, incremental planning, and bounded indexing;
in-memory and LatticeDB providers behind a shared conformance suite; Roslyn
C# declarations, symbols, containment, and project discovery; exact lookup,
bounded traversal, and impact queries; run-scoped staging with atomic
publication and durable restart semantics; publication receipts, freshness
checks, affected-test queries, immutable publication snapshots with
integrity hashes, and optimistic concurrent-publication ordering.
