# Changelog

Notable changes to Penghou.Hetu are recorded here. The project follows
[Semantic Versioning](https://semver.org/) for package versions. Preview
releases may still revise public contracts.

## Unreleased

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
