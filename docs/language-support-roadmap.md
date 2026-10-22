# Language support roadmap

Status: approved direction; documentation baseline audited on 2026-10-21 against
`5a5e38749570bad36630485d462f9ac92ade39a9`. This roadmap commits to priorities,
not implementation dates, releases, parser choices, or complete language coverage.

Hetu extracts, normalizes, persists, and queries useful code facts for repository
comprehension, symbol discovery, dependencies, change impact, test identification,
architectural analysis, and AI-agent context selection. It does not reproduce
every language's AST or become a universal compiler.

The permanent [feature catalog and coverage matrix](language-feature-matrix.md)
separates language applicability, graph representation, extraction coverage, and
evidence. [ROADMAP Milestone 8B](../ROADMAP.md#milestone-8b--language-feature-catalog-and-phased-language-expansion)
places this work under Hetu's architectural laws and Solo/Marang integration
priorities. Existing decisions in the accepted
[evidence and provider ADR](evidence-quality-and-provider-strategy.md) continue
to govern plugin selection, evidence, partial results, and grammar pinning.
The expansion phases here are separate from that ADR's implementation phases.

## Approved target and sequencing

The nine-language target comprises four existing languages (C#, TypeScript,
JavaScript, Elixir) and five planned languages (Python, Java, Go, Rust, Kotlin).
The five planned languages cannot yet be indexed through a supported Hetu
plugin. Recognizing a file extension or obtaining a grammar is not support.

| Phase | Languages in priority order | Purpose and boundaries |
| --- | --- | --- |
| 0 | C#, TypeScript, JavaScript, Elixir | Establish the feature catalog, audit extractors, map constructs to graph facts, identify node/edge/property gaps, and establish feature verification. Prioritize TypeScript/JavaScript improvements, preserve C# semantic extraction, and maintain Elixir structural support. |
| 1 | 1. Python; 2. Java | Begin with useful structural extraction. Python tests dynamic typing, decorators, multiple inheritance, modules, and dynamic constructs. Java tests packages, interfaces, generics, annotations, overloads, and compilation-unit organization. Semantic enrichment is a separate feature-specific milestone. |
| 2 | 1. Go; 2. Rust | Go tests packages, structs, embedding, receivers, interfaces, and implicit interface satisfaction. Rust tests crates, modules, traits, implementation blocks, generics, and macro boundaries. Structural parsing alone cannot establish implicit interface implementation or resolve macro-generated declarations. |
| 3 | Kotlin | Test extension functions, properties, companion objects, top-level declarations, sealed types, and Kotlin-specific semantics. Reuse suitable Java mappings without assuming Kotlin is Java with different syntax. |

Phase 0 must establish a usable foundation before another language is added.
Creating these documents completes the initial design inventory; it does not
complete Phase 0 improvements or implement any expansion phase.

## Phase 0 foundation and improvement queue

Use the [audited findings](language-feature-matrix.md#audit-findings-and-priority-gaps)
as the starting backlog. Schedule implementation separately, with consumer
questions and scoped fixtures attached to each advertised feature.

1. Keep a stable feature inventory and distinguish implementation inspected in
   code from behavior asserted in fixtures. Document current omissions before
   enlarging support claims.
2. Prioritize useful TypeScript/JavaScript member coverage: interface signatures,
   enum members, fields/properties, parameters, generic parameters, and callable
   identity/ownership. A parser recognizing these constructs is insufficient.
3. Design module/export/alias semantics and type relationships before promising a
   complete public surface or dependency graph. `require("literal")` imports and
   re-export source observations do not establish CommonJS/ESM export binding.
4. Capability declarations were audited against actual output and corrected
   (2026-10-22): TypeScript, TSX and JavaScript now advertise `Inheritance`,
   and Elixir does not. Contract tests bind each flag to emitted relationship
   coverage; live capability metadata changes no persisted facts, and the
   effective plugin version still invalidates indexes when a package, grammar,
   ABI or extraction-profile component changes.
5. Preserve C# resolved calls/references and project-unit behavior, and retain its
   documented host-runtime references and lightweight project-model limitations.
   Do not expand enum-member or generic-parameter claims beyond emitted nodes.
6. Maintain Elixir modules/functions/imports/invocation names and partial parsing.
   Protocol and macro declaration recognition does not establish protocol
   implementation or macro expansion.

Phase 0 is usable when the catalog is maintainable, four-language omissions are
explicit, prioritized changes have feature-level acceptance fixtures specified,
capability/output discrepancies have recorded dispositions, and consumers can
interpret coverage without treating missing facts as proof of absence. Runtime
improvements require their own implementation and validation work; they are not
checked off by this documentation change.

## Deferred candidates

The uncommitted candidate backlog is PHP, C++, C, Swift, Ruby, Dart, and Scala.
It implies neither an order nor a release commitment. SQL and shell languages
need separate consideration because useful graph concepts may differ from
general-purpose languages. Erlang remains parked, not rejected.

## Prioritization principles

Future decisions consider, together:

1. Language adoption and repository prevalence.
2. Usefulness to actual Hetu consumers.
3. Availability and maturity of deterministic parsers.
4. Availability of optional semantic providers.
5. Value in validating the language-neutral graph vocabulary.
6. Implementation and maintenance cost.
7. Evidence quality and testability.

Popularity is an input, not the sole deciding factor. No survey percentage is
needed to establish this direction. Phases may be reordered by a recorded
decision when real consumer demand justifies it. Expansion remains subordinate
to the broader Solo/Marang integration gates; the accepted ADR's consumer-use-case
gate and the reserved future generator are preserved.

## Support and evidence contract

**Structural support** means a documented useful subset of declarations and
relationships extracted by deterministic syntax analysis. It never means
complete language coverage. Pattern-derived facts remain `Derived` even inside
a structural plugin.

**Semantic support** means selected symbols or relationships resolved through a
language-aware semantic model or equivalent authoritative analysis context.
It is feature-specific: resolved calls do not imply resolved types, references,
dynamic constructs, or all possible runtime dispatch targets. One language can
produce a mixture of `Structural`, `Resolved`, and `Derived` relationships.

`Resolved`, `Structural`, `Derived`, and `Inferred` classify how evidence was
established; they are not numeric confidence levels. A matching declaration name
cannot promote `invokes-name` into `calls`. Deterministic prototype patterns
cannot claim compiler resolution. Deterministic evidence carries no probabilistic
confidence. Consumers must accept the relevant qualities explicitly.

Optional semantic providers may eventually enrich Python, Java, TypeScript,
JavaScript, Go, Rust, and Kotlin. No implementation or provider technology is
selected here. Explicit provider priority/fallback, attributable provenance,
bounded queries, and partial-result honesty remain governed by the ADR. The
ADR's deferred probabilistic enrichment is separate from authoritative symbol
resolution and cannot satisfy deterministic queries.

## Publication-bound feature reporting: future design requirement

The intended relationship is:

```text
Feature catalog -> Language applicability -> Plugin capability
  -> Actual extraction -> Graph facts -> Published coverage -> Consumer queries
```

The catalog describes useful concepts. Applicability describes languages.
`CodeGraphCapabilities` advertises broad implemented plugin behavior. Actual facts
and `CodeRelationshipCoverage` describe what an extraction produced. None of
these substitutes for the others or proves complete feature coverage.

A future machine-readable catalog must have a catalog version and stable feature
IDs, and bind claims to plugin identity/version, grammar identity/version/ABI
where relevant, language/dialect, effective extraction capabilities (including
fallback), and the exact index publication. It must distinguish advertised
implementation, verified fixture scope, and observed run/unit coverage. Missing
or unknown historical metadata must stay unknown.

An older publication must not acquire new capability claims merely because an
installed plugin or the catalog was upgraded. Plugin versions and freshness
already exist; a persisted feature-catalog binding does not. Its format,
compatibility policy, and publication storage are future design questions,
not new persistence behavior authorized here. See the matrix for current
coverage-contract limits and unresolved vocabulary choices.

## Verification and scope

Every advertised feature should eventually have deterministic positive and
negative fixtures for expected nodes, physical declarations, relationships,
properties, evidence, unsupported/ambiguous cases, partial parsing, repeat
extraction, and stale-fact removal after source changes. A dynamic inheritance
expression without an authoritative target must not invent a resolved edge.
The [matrix conformance model](language-feature-matrix.md#future-feature-conformance)
defines the expectations; building automation is future work.

This decision authorizes documentation only. It introduces no parsers, grammar
packages, plugins, public APIs or feature-ID enum, graph migrations, storage
schemas, semantic providers, catalog runtime loading, persistence behavior,
extractor refactoring, or runtime capabilities. The graph remains extensible
through existing kind/property contracts. Open representation choices must be
resolved through separately recorded designs, not silently encoded as facts.
