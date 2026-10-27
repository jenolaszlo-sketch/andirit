# EC-6D consumer query contract (R2 revision)

**Status: remediated planning contract for owner review (EC6D-PLAN-R2).**
Supersedes the R1 proposal at commit `0e1b213`. It specifies behavior only;
implementation and API surface require separate owner approval. Existing
queries, presets, and defaults are untouched.

**R2 changes (summary):** entry-driven candidate discovery to reach export
aliases and barrel consumers (Q2-03); module-reference-exact barrier and
possible-consumer predicates (Q2-06); closed reason table with reachability
including qualified/expression/extends heritage observability (Q2-03/07);
physical-declaration identity, witness lists and same-file proof in the
result shape (Q2-02/04); structured coverage with a not-evaluated frontier
(Q2-03/07); full publication identity and consistency truth table
(Q2-05); charged-work budgets and nested total order (Q2-07).

## 1. Why a new contract

`FindImplementationsAsync` returns a seed-only result for TypeScript
interfaces and cannot express classified answers; its semantics are pinned
by existing tests. EC-6D therefore exposes a new, explicitly versioned
query with its own result type, exposed additively and opt-in.

## 2. Proposed query identity

- Working name: `GetInterfaceImpactV1` (behavior changes require a V2).
- Available on the fail-on-change publication view
  (`CodeGraphPublicationQuery`) and, with identical semantics, on
  `CodeGraphQueryService` bound to a publication.
- **Supported input:** one TypeScript interface declaration node
  (`CodeNodeId`) in a publication produced by a plugin set that emits facts
  A, B, C and E (the EC-6D provider set, one publication). Non-interface or
  unknown nodes, and publications without the provider set, are **input
  errors**, not empty results.
- Every join reads facts from exactly one publication; mixed-fact
  inconsistency is handled per §5.

## 3. Result semantics

### 3.1 Confirmed implementation

A confirmed entry is a class whose supported heritage clause (§ symbol
contract §3.1) is bound to the queried declaration through either:

- the **import route** — the complete chain heritage context → unique
  eligible import occurrence → resolved target → export entry → unique
  attributed **physical declaration record** equal to the target; or
- the **same-file route** — the short proof of symbol contract §3.5, with
  no import/resolution/export steps, for interfaces that need not be
  exported.

Each confirmed entry carries the implementing class identity, the binding
route, the target's declaration record id and node id, and **one or more
witnesses**; each witness is a complete ordered step chain with the
computed witness identity (symbol contract §3.6). Distinct witness paths
are preserved, not flattened. A chain that completes to a **different
declaration** is omitted from this target's result (it concerns another
declaration; neither confirmed nor unknown here). This category never
contains an entry whose chain has a missing, ambiguous or unverified step.

### 3.2 Possible consumer

A **possible consumer** is a module whose supported **named import
relationship** touches the target declaration's module: a file `F ≠ target
file` with at least one fact A occurrence whose
`form ∈ {named, named-aliased}`, `resolutionStatus = resolved`, and
`resolvedTarget = target file`. Entries list that file's matching
occurrences (specifier, imported name, local name, type-only status, all
spans, resolution rule). This category asserts module-level potential
dependency only: it never asserts type use, never includes default/
namespace/import-equals/dynamic forms, never includes export-side
references, `require()` calls or barrel imports (those resolve to the
barrel), and never appears inside the confirmed category. An idle import is
a possible consumer and only a possible consumer.

### 3.3 Candidate discovery (entry-driven, no closure)

Discovery starts from the target and uses only stored facts; it does not
match globally on the target's written name alone.

**Associated names of the target,** computed canonically:

1. `A0 = { target declaration name }`.
2. `A1 = A0 ∪ { exportedName of every export entry attributed (status =
   attributed) to the target's physical declaration record }` — this is
   what reaches export aliases (`export { Internal as Public }`).
3. `A2 = A1 ∪ { record.exportedName : fact E record with form ∈
   {export-from, export-type-from} and record.importedName ∈ A1 }` — a
   **single bounded hop** of re-export alias association. The association is
   name-level and may over-report relevance (it can only add unknowns,
   never confirmations); resolved records name their source, unresolved
   records remain relevant by name so that unresolved barrels stay visible
   (Q2-06). `export-namespace-from` adds no names in this milestone; names
   beyond one hop and chain barrels are **not evaluated** and are disclosed
   by the coverage statement. `export-star` records add no names; a star
   record whose `resolvedTarget` is the target file marks its file as a
   **target-star-barrel** (see below).

**Candidate relationships** for the target:

- *Heritage candidates:* heritage context records (fact C) whose
  `writtenForm = simple` and written name ∈ A2, plus qualified/expression/
  extends records whose `terminalIdentifier` ∈ A2 (reported through the
  §3.4 reasons for their kind; relevance via terminal identifier may
  over-report unknowns and never confirms).
- *Import bindings:* fact A occurrences (form `named`/`named-aliased`) with
  `importedName ∈ A2`; the local name links the occurrence to the file's
  heritage candidates with that written name.
- *Same-file candidates:* heritage candidates in the target's own file per
  symbol contract §3.5.

**Occurrence-level unknowns (no heritage clause required):** every fact A
occurrence with `importedName ∈ A2` (or from a target-star-barrel) that is
not consumed by a heritage candidate is classified — resolved-to-target-
file with a missing entry → `missing-export` (or `re-export-barrier` per
the predicate); resolved to a barrel that is target-associated (named
alias record match, or star record whose `resolvedTarget = target file`) →
`re-export-barrier`; unresolved → `unresolved-module`; external →
`external-module`. This is how the committed `BarrelPreview` obligation is
met: it imports an alias (`Drawable`) that the barrel publishes for the
target's name and has no heritage clause — it appears as an explicit
occurrence-level `re-export-barrier` unknown, never silently omitted.

**No inventoried relationship:** when no stored fact associates a written
name or occurrence with the target, no relationship is invented; the
not-evaluated frontier (§6) accounts for the declared boundary.

### 3.4 Unknown reasons (closed; reachability recorded)

Reason names are stable codes. "Reachability" states the condition that
emits the reason and a committed fixture or declared synthetic condition.

| Reason | Subject | Trigger (reachable condition) | Fixture |
| --- | --- | --- | --- |
| `unresolved-heritage-name` | heritage | no import or module-scope declaration supplies the written name | `wrong-export` |
| `nested-scope-heritage` | heritage | clause owner is not module-scope | `nested-shadow` |
| `type-parameter-shadowing` | heritage | owner declares a type parameter named `H` | `fixtures/adversarial` `TypeParam` |
| `ambient-or-global-context` | heritage | owner is ambient or the file is not a module | r2 `ambient-script` / `ambient-export` |
| `recovered-heritage-syntax` | heritage | clause or owner touches recovered/error syntax | r2 `recovered-clause` |
| `binding-context-unavailable` | heritage | heritage occurrence has no fact C record | synthetic publication (store-level, A21) |
| `qualified-heritage-unresolved` | heritage | qualified written name with terminal identifier ∈ A2; p9 emits no edge for it | `unsupported` (`NS.Widget`) |
| `unsupported-heritage-form` | heritage | expression heritage with terminal identifier ∈ A2 | r2 `expression-heritage` |
| `heritage-kind-not-supported` | heritage | `class-extends`/`interface-extends` clause naming the target; transitive impact stays excluded | r2 `interface-extends` |
| `unsupported-import-form` | heritage | a default/namespace/import-equals/dynamic import introduces `H` | `unsupported` (`DefaultWidget`) |
| `duplicate-local-name` | heritage | more than one eligible import occurrence introduces `H` | `duplicate-imports` |
| `conflicting-local-binding` | heritage | one eligible import and ≥1 module-scope declaration share `H` | `collision` |
| `ambiguous-local-declaration` | heritage | ≥2 module-scope physical declarations share `H`, no import | r2 `same-file-merged` |
| `ineligible-declaration` | heritage | the only same-name module-scope declaration is ambient/recovered/unmodeled, and for attributed-entry status `ineligible-declaration-context` | r2 `ambient-export` |
| `unresolved-module` | heritage, occurrence | matching occurrence did not resolve | `unsupported` (`MissingAlias`), `barrel` consumers |
| `external-module` | heritage, occurrence | matching occurrence is a bare package specifier | r2 `external-package` |
| `missing-export` | heritage, occurrence | resolved target has no entry and no export-side reference records of the four re-export forms | `barrel` `ordinary.ts` consumer |
| `re-export-barrier` | heritage, occurrence | resolved target has no entry and ≥1 fact E record of the four re-export forms (any resolution status), carried with all matching records as separate witnesses | `barrel` (`named`, `star`, `missing`, `multi`, `no-declaration` consumers), `BarrelPreview` |
| `unattributed-export` | heritage | entry exists; attribution status `missing-declaration` (no module-scope physical declaration; e.g. only a nested declaration exists) | `nested-export`, r2 `ghost` |
| `ambiguous-export` | heritage | entry exists; attribution status `ambiguous-declarations` (merged same-node multiple records, interface+class, type/value pairs, conflicting contributors) | `merged`, `type-value` |
| `unsupported-export-form` | — | **Reserved; non-emittable this milestone** (default/namespace/star entries are outside the name-based channels) | — |
| `unsupported-declaration-form` | — | **Reserved; non-emittable this milestone** (non-interface targets are input errors; chains completing to other declarations are omitted) | — |

Reserved reasons must not be emitted; their absence is not a coverage gap.
Unknowns are never silently dropped and never rendered as confirmed or
possible results. `re-export-barrier` unknowns list the contributing fact E
records as separate witnesses (never first-wins).

### 3.5 Publication qualification

Every result carries `publication { repositoryId, indexRunId,
indexIdentity, snapshotIdentity, isConsistentSnapshot, qualification }`
per the publication-consistency contract. Relationship uncertainty
(§3.1–§3.4) and publication consistency are never conflated.

## 4. Response shape (proposed, not implemented)

```
InterfaceImpactResultV1
  publication:
    repositoryId, indexRunId, indexIdentity, snapshotIdentity,
    isConsistentSnapshot, qualification          // derived per publication contract
  target:
    declarationNodeId, declarationRecordId, name, qualifiedName, path, span
  confirmedImplementations[]:
    classNodeId, className, path, heritageSpan
    bindingRoute: "import" | "same-file"
    targetDeclarationRecordId, targetDeclarationNodeId
    witnesses[]: { witnessId, steps[]: { step, fromNodeId, toNodeId,
                   factId, evidence { quality, provider, providerVersion,
                   location, sourceHash? } } }
  possibleConsumers[]:
    fileNodeId, path
    occurrences[]: { occurrenceId, specifier, importedName, localName,
                     isTypeOnly, statementSpan, moduleSpecifierSpan,
                     bindingSpan, resolutionRule }
  unknowns[]:
    subject: { kind: "heritage" | "occurrence", fileNodeId?, classNodeId?,
               occurrenceId?, heritageSpan? }
    reason
    evidence { spans, associatedName, factIds[] }
    witnesses[]?: fact E record references for re-export barriers
  coverage:
    profile, qualification, partial (boolean, true when truncated)
    evaluatedImportOccurrences, unsupportedImportFormOccurrences,
    externalOccurrences, unresolvedOccurrences
    heritageObserved, heritageEvaluated
    heritageNested, heritageQualified, heritageExpression, heritageExtends,
    heritageContextsUnavailable
    exportReferencesObserved, exportReferencesUnresolved,
    exportReferencesUnsupported
    confirmedImplementationCount, possibleConsumerModuleCount,
    unknownCountsByReason
    notEvaluated[]: fixed statements (renamed/multi-hop re-export paths,
                    CommonJS/require, bare packages, ordinary type-use,
                    transitive impact, default/namespace binding forms)
  truncation:
    state, stage, chargedWork { candidateScans, heritageScans,
    resolverJoins, attributionLookups, witnessSteps }, appliedLimits,
    omittedFrontierCount, countsAreLowerBounds (boolean)
```

**Ordering** is a deterministic total order with nested rules: confirmed by
(class path, class name, class node id, target declaration record id);
witnesses within an entry by witness id; steps by fixed step ordinal then
fact id ordinal; occurrences by (statement span start, binding span start,
form ordinal, local name, occurrence id); unknown subjects by (subject kind
ordinal, class path, subject node id, span start, reason ordinal); absent
values sort last; ties resolve by full fact-id/ordinal string comparison.
**Deduplication:** one confirmed entity per (class node, target declaration
record) preserving distinct witnesses (exact-equal witness ids dedupe); one
possible consumer per file; one unknown per (subject, reason); occurrences
dedupe by occurrence id. Mutual exclusion applies to the **same evaluated
relationship** — a class may be confirmed while its file is also a
possible consumer; a file may hold supported relationships and unknown
occurrences simultaneously; a confirmed clause is never also an unknown.

## 5. Semantics, bounds and guarantees

- **Input handling:** unknown or non-interface node, or a publication
  without the EC-6D provider set → explicit input error following existing
  argument-validation conventions. An existing declaration with zero
  supported relationships → well-formed empty result with coverage.
- **Charged work and truncation:** the query declares budgets over actual
  execution work — `maxCandidateScans`, `maxHeritageScans`,
  `maxResolverJoins`, `maxAttributionLookups`, `maxWitnessSteps` — separate
  from result-materialization caps such as `maxConfirmed`, `maxOccurrences`
  and `maxUnknowns`. Work is charged **before deduplication**, in the fixed
  stage order discovery → context gates → binding → coverage. Invalid
  bounds are input errors; defaults are documented. Exceeding a budget sets
  truncation state with the stage, omitted frontier count and whether
  counts are exact or lower bounds; truncation is visible and never
  silently narrows a category. Return-size caps are never presented as
  execution bounds.
- **Cancellation:** cooperative cancellation at the same boundaries as
  existing traversals; a cancelled query throws and returns **no partial
  classified results**.
- **Provider/indexing failures:** the query reads only the published graph
  and never invokes source providers. A failed or cancelled re-index
  preserves the last successful publication; bound queries continue to
  answer from it with its recorded identity and qualification (freshness is
  not implied). Query/store failures and internal inconsistencies (§5
  bullet below) return no classified output.
- **Internal consistency:** contradictory endpoints, mismatched unit
  versions, superseded units, a missing fact inside an asserted witness, or
  identity conflicts are **integrity failures** (fail closed); ordinary
  missing attribution is an unknown, not an error.
- **Persistence/reopen:** results derive solely from published facts; the
  same publication yields identical results after durable close/reopen. The
  witness identity is recomputed deterministically from stored fact ids.
- **Compatibility:** existing query defaults, presets and
  `FindImplementationsAsync` semantics are unchanged (F-08 deferral).

## 6. Coverage statement (mandatory text contract)

Every result includes a fixed coverage statement equivalent to: "This
result covers direct explicit class `implements` bindings with simple
identifiers at module scope, direct named relative imports under the
documented EC-6D profile, and one bounded hop of resolved re-export alias
association for candidate relevance and barrier reporting. Publication
qualification: {qualification}. It does not evaluate: renamed or multi-hop
re-export paths beyond that hop, unresolved or unsupported export sources
(beyond their recorded not-evaluated counts), CommonJS/`require` forms,
bare package specifiers, namespace or default binding forms, qualified or
expression or extends heritage (reported as unknowns only where their
terminal identifier matches the target), nested-scope heritage, ordinary
type-use references, call sites, implicit structural implementations,
transitive inheritance, other languages or cross-plugin symbols. Absence
from the confirmed list is not evidence of absence of impact."

The structured coverage block accompanies the text. Coverage completeness
is qualified as: **truncated** (partial = true; counts lower bounds),
**incomplete frontier** (not-evaluated counters non-zero), or **complete
within the declared profile** (all counters zero and no truncation).
Truncation and publication consistency interact as: truncation marks
coverage partial in the same result; qualification is embedded in the
statement; neither substitutes for the other.

The stored-evidence obligation from Q2 is satisfied by facts C and E:
qualified/expression/extends heritage and unresolved/unsupported export
references exist as published records before querying; a prose disclaimer
alone is not analysis evidence.

## 7. Acceptance linkage

The acceptance matrix in [acceptance-contract.md](acceptance-contract.md)
(R2 revision) is written against this contract. A passing graph-storage
test never substitutes for a semantic assertion; no proposed test is
evidence that production behavior exists.
