# ADR: Hetu Code Evidence Quality and Parser Provider Strategy
## Status

Accepted

Phase 1 (evidence and capability correctness) is implemented and enforced by
tests. Phases 2–4 remain planned work.

## Context

`Penghou.Hetu` already has the main architectural separation required for multi-language code analysis:

- Hetu owns a normalized code graph rather than parser-specific syntax trees.
- `ICodeGraphPlugin` provides the extension boundary for language-specific analysis.
- `CSharpCodeGraphPlugin` provides project-aware Roslyn analysis.
- Graph facts retain provenance and coverage information.
- Parser-specific types do not leak into Hetu abstractions.

The architecture therefore does not need a new provider abstraction or a redesign around Tree-sitter.

The required change is narrower:

1. make evidence strength explicit,
2. make plugin capabilities accurate,
3. distinguish structural calls from resolved calls,
4. allow explicit language-provider priority and fallback,
5. add Tree-sitter later as an isolated structural plugin.

The existing `CodeEvidenceKind` vocabulary is:

```text
Syntax
Semantic
Heuristic
AiInferred
```

This is insufficient for the expanded provider model.

In particular:

- Tree-sitter facts are structural, but `Syntax` does not express their relationship to compiler-resolved facts.
- deterministic Hetu-derived relationships currently risk being represented as `Semantic` or `Heuristic`,
- `Heuristic` conflates deterministic derivation with probabilistic inference,
- `Semantic` does not clearly distinguish a resolved compiler fact from other forms of semantic analysis.

An earlier proposed `Exact` evidence level was rejected because it overstates certainty. Roslyn results are authoritative relative to the compilation context that was successfully constructed, but are not universally "exact".

## Decision

### 1. Keep the existing plugin architecture

`ICodeGraphPlugin` remains the language-analysis extension point.

It will not be renamed to `ICodeEvidenceProvider`.

The existing session-oriented plugin contract remains useful for project-wide analyzers such as Roslyn and avoids unnecessary churn in:

- plugin identities,
- public API contracts,
- planner behavior,
- Marang integrations,
- Hongxian integrations.

Tree-sitter will be introduced as another implementation of the existing plugin model.

---

## 2. Replace the evidence vocabulary with four evidence qualities

Hetu will use:

```csharp
public enum EvidenceQuality
{
    Resolved,
    Structural,
    Derived,
    Inferred
}
```

### Resolved

A language-aware provider has resolved the identity or relationship using its semantic model.

Examples:

- Roslyn binds an invocation to a specific method symbol.
- Roslyn resolves a type.
- A future language service resolves a reference.
- A future SCIP provider supplies a resolved symbol relationship.

`Resolved` means resolved by the named provider in its available analysis context.

It does not mean universally exact.

### Structural

The fact follows directly from source structure but has not been semantically resolved.

Examples:

- class declaration,
- function declaration,
- containment,
- import syntax,
- package declaration,
- Tree-sitter invocation named `Save`.

Structural evidence is deterministic but must not be treated as semantic binding.

### Derived

Hetu deterministically computes the fact from other evidence.

Examples:

- an `exercised-by` relationship derived from resolved call relationships,
- aggregate graph relationships derived from known graph facts,
- deterministic repository-level relationships.

Derived facts retain provenance sufficient to identify the facts or provider data from which they were computed.

Derived evidence must never present itself as `Resolved`.

### Inferred

The fact is probabilistic or interpretive.

Examples:

- JEV classification,
- LLM architectural interpretation,
- AI-generated conceptual relationships,
- heuristic analysis whose result is not deterministic.

Inferred evidence may carry confidence.

It must never satisfy a deterministic evidence requirement.

---

## 3. Evidence strength is not an ordinal truth hierarchy

The four qualities describe **how a fact was established**, not simply increasing numerical confidence.

In particular:

```text
Structural != weaker Resolved
Derived    != weaker Resolved
Inferred   != approximate Resolved
```

They have different semantics.

Consumers must request the qualities they are prepared to accept.

A query requiring a resolved call target must not be satisfied by a structural invocation with the same name.

A query requiring deterministic evidence must not be satisfied by inferred evidence.

---

## 4. Confidence rules

Confidence remains prohibited for deterministic evidence:

```text
Resolved
Structural
Derived
```

Confidence is allowed, and should normally be required where meaningful, for:

```text
Inferred
```

This preserves the existing principle that deterministic facts should not be decorated with artificial probabilistic scores.

---

## 5. Migration from `CodeEvidenceKind`

Existing evidence maps as follows.

### `Syntax` → `Structural`

Examples:

- declarations,
- containment,
- syntactic dependency declarations,
- package relationships derived directly from syntax.

### `Semantic` → usually `Resolved`

Roslyn-bound relationships such as:

- calls,
- references,
- inheritance,
- symbol relationships,
- type relationships

become `Resolved`.

Existing facts currently marked `Semantic` but actually computed by Hetu must instead become `Derived`.

### `Heuristic` → split

Deterministic heuristics become:

```text
Derived
```

Probabilistic or uncertain heuristics become:

```text
Inferred
```

There should be no generic `Heuristic` quality after migration.

### `AiInferred` → `Inferred`

AI-specific origin remains available through provenance.

Evidence quality does not need to encode which AI or classifier produced the fact.

---

## 6. `calls` is reserved for resolved calls

The existing `calls` edge represents a semantically resolved call relationship.

It must therefore carry `Resolved` evidence.

Tree-sitter and similar structural parsers must not emit `calls` merely because an invocation contains a particular name.

Introduce:

```text
invokes-name
```

for structural invocation evidence.

Example:

```text
Structural:

OrderService.Process
    invokes-name
"Save"
```

compared with:

```text
Resolved:

OrderService.Process
    calls
Repository.Save(Guid)
```

A conformance rule will enforce:

> No `calls` edge may carry `Structural` evidence.

This prevents a structural parser from overstating what it knows.

---

## 7. Provider provenance remains separate from evidence quality

Evidence quality answers:

> How was this fact established?

Provenance answers:

> Who established it, using what implementation and context?

Examples:

```text
quality  = Resolved
provider = roslyn
version  = ...
```

or:

```text
quality  = Structural
provider = tree-sitter
grammar  = tree-sitter-elixir
version  = ...
```

Future providers such as LSP, SCIP, compiler APIs, JEV, or LLMs do not require new evidence quality values merely because they are different technologies.

---

## 8. Plugin capabilities must describe actual output

`CodeGraphCapabilities` will be corrected so plugin declarations match their real behavior.

`CSharpCodeGraphPlugin` currently emits more information than its advertised capability set.

Its capability declaration must be updated to reflect existing support for areas including:

- references,
- calls,
- inheritance,
- imports.

The capability model will also add:

```text
Declarations
Diagnostics
```

Additional capabilities such as incremental parsing may be introduced when there is an actual provider requiring them.

Capabilities must describe implemented behavior, not intended future behavior.

Conformance tests should verify that advertised capabilities and emitted graph facts remain consistent.

---

## 9. Keep strict plugin ambiguity behavior by default

The current registry behavior that rejects multiple matching plugins remains the default.

Hetu must not silently select a lower-fidelity provider.

Explicit language-provider configuration will later allow ordered selection, for example:

```text
C#
    Roslyn
    Tree-sitter
```

or:

```text
Elixir
    future semantic provider
    Tree-sitter
```

Fallback is opt-in behavior.

If fallback occurs, Hetu must record:

- requested provider,
- actual provider,
- reason for fallback,
- resulting capability or evidence downgrade where relevant.

A failed semantic provider must never silently turn a resolved analysis request into structural analysis.

---

## 10. Tree-sitter is an isolated plugin

Tree-sitter will be introduced in a dedicated package.

Conceptually:

```text
Penghou.Hetu
Penghou.Hetu.Abstractions
Penghou.Hetu.CSharp
Penghou.Hetu.TreeSitter
```

The Tree-sitter package owns:

- native runtime loading,
- grammar loading,
- native object lifetime,
- Tree-sitter queries,
- parser diagnostics,
- extraction profiles,
- grammar metadata.

Tree-sitter-native concepts such as `TSNode` and `TSTree` must not enter Hetu core abstractions.

The plugin converts parser results directly into existing Hetu graph facts.

---

## 11. Tree-sitter extraction is language-profile driven

Tree-sitter grammars do not expose identical node structures across languages.

Small extraction profiles will map grammar-specific syntax to Hetu concepts.

Examples include:

- modules,
- namespaces,
- types,
- functions,
- imports,
- declarations,
- structural invocations.

These profiles are adapters, not parsers.

Hetu will not attempt to define a universal AST.

---

## 12. Tree-sitter grammar versions are explicit

Each configured grammar should record sufficient identity to reproduce analysis, including where practical:

- language,
- grammar source,
- grammar version or commit,
- supported extraction capabilities.

A grammar upgrade is therefore an analysis-provider change and can participate in existing freshness/version invalidation behavior.

---

## 13. Partial parsing is valid evidence

Tree-sitter may understand only part of a file because of:

- unsupported new syntax,
- grammar lag,
- malformed source,
- language extensions.

Hetu may retain valid structural evidence from successfully parsed regions.

However, partial analysis must be visible.

Diagnostics should expose information such as:

```text
PartialResult
ParseFailure
UnsupportedSyntax
```

and, where available:

- parse error count,
- affected regions,
- grammar/provider identity.

Partial analysis must never be represented as complete analysis.

---

## 14. Future enrichment remains separate

The following are intentionally deferred until the parser/provider work has been proven through dogfooding:

- multi-provider evidence combination,
- EmbeddingGemma semantic retrieval,
- JEV classification,
- LLM-derived code interpretation.

These systems may consume normalized Hetu entities later.

Their output must respect the evidence model.

In particular:

```text
Embedding similarity != graph truth
JEV classification   != resolved code fact
LLM interpretation   != deterministic evidence
```

Probabilistic graph facts enter Hetu as `Inferred`.

They cannot overwrite deterministic facts or satisfy deterministic queries.

---

## Consequences

### Positive

Hetu keeps its existing architecture rather than introducing another provider abstraction.

Parser technology becomes replaceable.

Roslyn remains the preferred C# semantic provider.

Tree-sitter can add broad language coverage without requiring Hetu to implement parsers.

Evidence consumers can distinguish:

```text
resolved compiler knowledge
structural parser knowledge
Hetu-derived knowledge
probabilistic interpretation
```

New language providers can improve fidelity later without changing the graph model.

Structural parsers can contribute useful information without pretending to provide semantic resolution.

### Negative

Changing `CodeEvidenceKind` affects public query and storage contracts.

Compatibility handling will be required.

Existing evidence must be audited because some current `Semantic` and `Heuristic` usages need reclassification rather than mechanical renaming.

Queries filtering by evidence kind will require migration.

Stored evidence may require migration or version handling depending on persistence compatibility requirements.

The distinction between `Resolved`, `Structural`, and `Derived` introduces stricter invariants that provider implementations must obey.

---

## Implementation Sequence

### Phase 1: Evidence and capability correctness

1. Introduce the four-level evidence-quality model.
2. Provide compatibility shims for existing public contracts where required.
3. Audit and migrate current evidence production.
4. Reclassify deterministic Hetu-generated relationships such as `exercised-by` as `Derived`.
5. Add `invokes-name`.
6. Enforce that `calls` requires resolved evidence.
7. Correct `CSharpCodeGraphPlugin` capability reporting.
8. Add `Declarations` and `Diagnostics` capability bits.
9. Update query filtering to enforce the new evidence semantics.
10. Add conformance tests around evidence quality and capabilities.

No provider-selection or Tree-sitter behavior is introduced in this phase.

### Phase 2: Explicit provider selection

1. Add language-provider priority configuration.
2. Preserve strict ambiguity failure when no priority is configured.
3. Add explicit fallback behavior.
4. Record requested and actual provider when fallback occurs.
5. Add indexing/provider failure taxonomy.

Implementation notes (landed):

- Preference is an ordered list per language (`CodeLanguageProviderMap`),
  not numeric priority. Referenced identities must be registered; explicit
  configuration is exhaustive per language (an unlisted claimant still
  throws). `CanHandle(path)` remains the final per-path filter.
- Default policy is `OnUnavailable`: `UnsupportedLanguage`,
  `ProviderUnavailable`, and `VersionMismatch` may fall back;
  `ProjectLoadFailure`, `ParseFailure`, `SemanticModelUnavailable`,
  provider crashes, and partial results fail loudly unless the caller opts
  into `OnFailure`. Cancellation and live-source conflicts never fall back.
- Takeover runs the next provider over the union of sources, deletes the
  failed provider's staged and previously published units, rewrites plan
  manifests to the recovering runner, and records the full attempt history
  with the classified reason and lost capabilities. A takeover runner must
  emit complete facts for every supplied source, including ones flagged
  `Unchanged` against the previous owner's history.
- Published units whose owner holds no path in the new state are collected
  at completion, so a recovered preferred provider displaces fallback units
  without manual reporting.
- Plan-time ordered selection is silent by design: it is deterministic from
  configuration plus `CanHandle`, with no failure and no downgrade. Only
  run-time fallback deviations record `CodeProviderSelection` events, and
  every non-preferred execution happens inside exactly one such event.
  Fallback chains visit each runner at most once in map order and terminate
  deterministically; cross-language runner sharing is not supported by
  planning (it stays ambiguous) and terminates safely if misconfigured.

### Phase 3: Tree-sitter

1. Add isolated `Penghou.Hetu.TreeSitter`.
2. Integrate the Tree-sitter native runtime.
3. Add grammar registry/version metadata.
4. Add language extraction profiles.
5. Start with a small number of languages.
6. Produce structural graph evidence only.
7. Support partial parsing and diagnostics.
8. Verify that no Tree-sitter-native type leaks into Hetu abstractions.

### Phase 4: Semantic enrichment

After dogfooding:

1. EmbeddingGemma for semantic retrieval and code memory.
2. JEV for classification.
3. Optional LLM-derived interpretation.
4. Potential multi-provider evidence combination.

These additions do not change the deterministic evidence contract established by this ADR.

---

## Conformance Invariants

The following invariants should become tests:

1. `calls` cannot carry `Structural` evidence.
2. `invokes-name` does not imply a resolved target.
3. `Inferred` evidence cannot satisfy deterministic-only queries.
4. `Derived` evidence cannot present itself as `Resolved`.
5. deterministic evidence cannot carry probabilistic confidence.
6. plugin capability declarations must be consistent with emitted fact classes.
7. parser-specific native types cannot appear in Hetu public abstractions.
8. fallback to a lower-capability provider must be explicit and diagnosable.
9. partial parsing cannot be reported as complete analysis.
10. provider and provider-version provenance must remain available for graph evidence.

---

## Open questions

### Traversal policy vs answer filter

`EvidenceQualities` currently filters every traversed edge, not just answer
edges. A resolved-only traversal therefore cannot cross structural
containment (`contains`, `declares`) to reach a resolved `calls` edge:

```text
namespace --contains [Structural]--> class --declares [Structural]-->
method --calls [Resolved]--> method
```

"Find resolved calls under namespace X" cannot be expressed with a single
resolved-only traversal today. A future change may distinguish:

```text
Evidence filter:  what evidence is acceptable as an answer/match?
Traversal policy: what edges may be used to navigate the graph?
```

This is intentionally deferred: no current query needs it, and changing
`declares`/`contains` back to `Resolved` merely to survive resolved-only
queries would weaken the evidence vocabulary. The current single-filter
behavior is pinned by
`ResolvedOnlyTraversal_DoesNotCrossStructuralContainment`.

---

## Decision Summary

Hetu remains a **code evidence and relationship system**, not a parser framework.

The existing plugin model remains the integration boundary.

Evidence is classified according to how it was established:

```text
Resolved
Structural
Derived
Inferred
```

Roslyn remains the primary C# provider.

Tree-sitter will provide broad structural language support through an isolated plugin.

Better parsers or semantic providers may be configured later without changing Hetu core.

The governing rule is:

> A provider may contribute only the strength of evidence it can actually establish.

Hetu preserves that distinction through storage, querying, derivation, and future AI enrichment.
