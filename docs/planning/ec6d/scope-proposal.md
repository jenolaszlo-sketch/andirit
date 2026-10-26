# EC-6D scope proposal

## Recommendation

Plan a single vertical slice: **bounded TypeScript interface impact through
direct named relative imports**. The purpose is to let a consumer select one
interface declaration, find uniquely bound direct explicit implementers, and
see modules that directly import it as possible consumers with evidence and
coverage limits.

The recommendation is based on the baseline: direct implementation lookup
returns zero, a name-only fallback has 0.75 precision and recall in the narrow
fixture class, and type-use consumers are not linked. This scope fixes the
most consequential gap without promising whole-program impact.

No agent interviews or production usage telemetry were available. Confirm
that this bounded workflow is useful to an intended consumer before
authorizing implementation.

## Candidate comparison

| Candidate | Consumer value | Reuse / complexity | Coupling and graph impact | Compatibility and qualification | Testability / main risk |
| --- | --- | --- | --- | --- | --- |
| 1. Query and traversal over current facts | Low to medium. Exposes imports, exports and file edges already present but cannot associate declarations and names. | High reuse of CodeGraphQueryService; low implementation complexity. | Low coupling; no schema change if only a convenience query is added. | Low compatibility cost, but acceptance can only show module neighborhoods, not interface impact. | Easy to test; high risk that query results appear more useful than their unbound semantics. |
| 2. Provenance-backed explanation | Medium. Makes known facts easier to audit but cannot explain a relationship Hetu did not extract. | High reuse of evidence envelopes; medium work to produce witness paths and coverage summaries. | Low to medium coupling; likely query response/API change, no necessary graph schema change. | Moderate API and qualification cost; existing providers have different evidence semantics. | Testable with existing facts; risks making incomplete evidence look complete. |
| 3. TypeScript interface-change impact | High for the primary question. Binds a selected interface to direct implementers and direct named importers under a strict profile. | Reuses p8 resolver, p9 exports, declarations, provenance and bounded queries; high complexity due to import/declaration identity and ambiguity handling. | Medium coupling across Tree-sitter extraction, graph vocabulary, indexing lifecycle and query API. Requires an additive binding representation decision and plugin profile revision. | Medium to high compatibility cost; preserve p9 facts/presets and qualify migration, stores, aliases, negatives and durable reopen. | Strongly testable with the committed corpus. Main risk is false resolved binding from spelling or unsupported module behavior. |

Candidate 1 alone is insufficient because its graph facts do not contain the
symbol association. Candidate 2 is valuable as a cross-cutting contract but
does not close the demonstrated gap on its own. Candidate 3 is the smallest
option that gives a real answer to the primary consumer question; its scope
must remain narrow enough to fail closed.

## Proposed functional boundary

### Supported profile

- TypeScript files in one Tree-sitter plugin inventory.
- A unique named interface declaration in a module.
- Direct named declaration export and direct named import from a relative
  module path already resolved under EC-6B relative-v1.
- Optional imported-name alias and explicit type-only import form.
- A class whose explicit implements clause names the imported binding or a
  same-file interface.
- Direct importing modules returned as **possible consumers**. A direct
  import alone does not prove a type-use site.
- One new bounded consumer query starting from a selected interface
  declaration. It returns direct explicitly bound implementers, direct
  importing modules and per-result source evidence.

### Fail-closed cases

Do not create a symbol binding if the module target is absent, the imported
name is missing, multiple declarations could satisfy the name, the export
surface is not supported, facts conflict, or the case falls outside the
profile. Return an explicit unresolved, ambiguous or unsupported status with
available source evidence and aggregate coverage. Never turn spelling alone
into a Resolved relationship.

### Graph and API design gate

Before implementation, an owner-approved ADR must decide how a unique named
import occurrence and its target declaration are represented. It must choose
an additive relationship or binding node contract; define direction,
identity inputs, occurrence multiplicity, evidence quality/provider, conflict
handling, lifecycle ownership, and query traversal. It must preserve current
Structural imports, p8 resolves-to semantics, p9 exports facts and existing
impact preset membership. The baseline does not settle this design choice.

The consumer-visible query contract is more important than a premature edge
name: it must expose the declaration target, binding chain, explicit
implementers, possible importers, ambiguity, unsupported/unresolved cases,
truncation and publication provenance.

## Acceptance-oriented scenarios

Use the committed fixture tree 014da71f0855cdc957fa9b6a0667bca565145092 as a
starting corpus. Add or reclassify cases only under an approved test plan;
preserve its original manual expected set.

The primary supported slice should recover the unique direct Renderable
declaration, SvgRenderer, HtmlRenderer, AbstractRenderer and the imported
alias implementer when that alias is a direct named import. The scope should
also return Preview and AliasRenderer's direct import modules as possible
consumers. The following remain explicit negatives or out of scope:

- OtherRenderer is excluded because its same-named interface has another
  declaration identity.
- MissingImport is unresolved and produces no symbol binding.
- BarrelPreview is not bound through api/index.ts until re-export origin
  tracing is separately designed and accepted.
- CanvasRenderer is not returned as a direct implementer; transitive impact
  may be reported only as an explicitly labeled inheritance candidate if
  identity and completeness are separately supported.
- Ordinary call sites and type annotations are not claimed as references in
  this scope.

## Required engineering and qualification work

- Increment the Tree-sitter TypeScript profile version when new facts change
  extraction; define whether JavaScript remains on its existing profile.
- Preserve current p9 export and resolver facts for unchanged sources.
- Keep existing query defaults and presets stable; expose the new behavior
  through an additive bounded query or explicit opt-in.
- Verify identity stability, duplicate occurrences, ambiguity, unit
  replacement/deletion, failed/cancelled runs and atomic publication.
- Run against InMemory and LatticeDB, close/reopen the database, and repeat
  indexing to verify canonical result equivalence.
- Reproduce the supported corpus independently and report precision/recall
  separately for direct implementers and direct potential importers.
- Include the name collision, imported alias, missing target, duplicate
  declaration, unsupported export, re-export, inheritance and cancellation
  negatives.
- Compare old p9 facts to the new profile. Qualify a direct p9-to-new-profile
  durable migration or document an approved staged procedure.
- Measure performance only after a representative repository and a justified
  baseline are selected.

## Explicitly deferred

- Re-export and star/namespace closure, cycles and origin chains.
- Bare package names, package.json, tsconfig paths, path aliases, URL or
  cross-plugin resolution.
- Import-to-runtime or bundler equivalence; whole-program compiler checking.
- Implicit interface satisfaction and duck typing.
- General TypeScript calls/references, all type-use locations, data flow,
  dynamic dispatch and arbitrary runtime effects.
- Transitive/inherited impact unless separately supported and qualified.
- JavaScript/CommonJS export binding and changes to JavaScript inline recovery.
- Graph diff/history, LSP, ranking, AI summaries, source-snippet APIs,
  performance claims and new languages.

The complete proposed consumer contract is in
[acceptance-contract.md](acceptance-contract.md). Risks are tracked in
[risk-register.md](risk-register.md).

---

## R1 revision (EC6D-PLAN-R1): binding-fact prerequisite and vertical-slice success

The independent scope review (Q1) rejected the original submission on
provenance grounds and verified that this proposal's two candidate
prerequisites are genuine gaps: the accepted p9 facts cannot bind imports to
declarations (no import-name facts, no export attribution), and the legacy
implementation query returns a seed-only result. This revision keeps the
recommended candidate 3 boundary unchanged and strengthens it in four ways.

### 1. The milestone includes the two missing fact capabilities

The bounded slice now explicitly includes per-specifier import-occurrence
facts and conservative export-to-declaration attribution, as specified in
[symbol-binding-contract.md](symbol-binding-contract.md). Without them, the
slice cannot produce a single confirmed implementation without name-only
matching, which is prohibited.

### 2. Success is defined at the query boundary, not by graph growth

The milestone succeeds only when the complete vertical slice
(source → import/export attribution → declaration identity → explicit
heritage binding → interface-impact query) returns, for the committed corpus
and supported profile, the four intended directly bound implementations as
**confirmed** results with full provenance chains. Introducing new fact
types, edges, or providers without that query-level outcome does not satisfy
acceptance. The four-case expectation is fixture-scoped; it must never be
reported as a general precision/recall claim.

### 3. The consumer surface is a new versioned query

The bounded query is `GetInterfaceImpactV1` with classified result
categories, closed unknown reasons, provenance chains, deterministic
ordering, declared bounds, and explicit publication qualification
([consumer-query-contract.md](consumer-query-contract.md)). The legacy
`FindImplementations` preset is not modified.

### 4. Publication qualification is part of the scope

Because the shipped source provider never declares a consistent snapshot,
the scope includes explicit snapshot-consistency semantics: qualified
results that cannot be mistaken for authoritative consistent-snapshot
answers, and fail-closed boundaries ([publication-consistency-contract.md](publication-consistency-contract.md)).

The graph/API representation ADR gate from the original proposal remains in
force and now decides: occurrence/attribution representation, whether the
binding join is computed at query time (recommended) or emitted as
extraction-time binding edges, and the resulting provider-version and
lifecycle contract.
