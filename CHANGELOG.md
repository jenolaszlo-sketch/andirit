# Changelog

Notable changes to Penghou.Hetu are recorded here. The project follows
[Semantic Versioning](https://semver.org/) for package versions. Preview
releases may still revise public contracts.

## Unreleased

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
