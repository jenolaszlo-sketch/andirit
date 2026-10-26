# EC-6D consumer query contract (R1 proposal)

**Status: remediated planning contract for owner review.** Defines the new,
versioned interface-impact query that Q1 finding F-04 requires. It specifies
behavior only; implementation and API surface require separate owner
approval. Existing queries, presets, and defaults are untouched.

## 1. Why a new contract

Q1 established that `FindImplementationsAsync` on the selected interface
declaration returns a seed-only result (0 relationships on the committed
corpus; 0 edges and a single node in Q1 scenario A). Its result type
(`CodeGraphTraversalResult`) cannot express the classified answer EC-6D owes
consumers, and overloading it would change documented semantics that 388
passing tests pin. The EC-6D answer is therefore a **new, explicitly
versioned query** with its own result type, exposed additively and opt-in.

## 2. Proposed query identity

- Working name: `GetInterfaceImpactV1` (the "V1" suffix is part of the
  contract; behavior changes require a V2, never silent mutation).
- Available on the fail-on-change publication view
  (`CodeGraphPublicationQuery`) and, with identical semantics, on
  `CodeGraphQueryService` bound to a publication.
- Supported input: one TypeScript interface declaration node (`CodeNodeId`)
  from a publication produced by a plugin version that emits the
  symbol-binding facts. Unsupported node kinds/languages are input errors,
  not empty results.

## 3. Result categories (mutually exclusive, structurally separated)

### 3.1 Confirmed implementation

A supported explicit `implements`/`extends-type` relationship whose
declaration identity was established through the five-step binding rule of
[symbol-binding-contract.md](symbol-binding-contract.md). Each entry carries
the implementing class identity, the full step chain (heritage fact, import
occurrence, module resolution, export entry + occurrence, attribution), and
the binding evidence identity. This category never contains an entry whose
chain has an unverified or missing step.

### 3.2 Possible consumer

A module whose **supported import relationship** touches the target
declaration's module — derived from `resolves-to` edges into the target file.
This category asserts module-level potential dependency only. It never
asserts type use, never appears inside the confirmed category, and never
requires binding evidence. An idle import (Q1 scenario G4) is a possible
consumer and only a possible consumer.

### 3.3 Unknown or unresolved

A relationship that plausibly involves the target but whose interpretation
cannot be established. Every unknown carries a machine-readable reason and
the best available evidence spans. Reasons are enumerated and closed:

| Reason | Trigger | Corpus example |
| --- | --- | --- |
| `unresolved-module` | import occurrence with matching imported name whose module did not resolve | `MissingImport.ts` (`../contracts/missing`) |
| `external-module` | matching named import of a bare package specifier | — (not in corpus) |
| `missing-export` | resolved target module publishes no export entry with the imported name | — |
| `re-export-barrier` | resolved target has no matching export entry but has outgoing `resolves-to` edges (re-export barrel) | `BarrelPreview.ts` via `src/api/index.ts` |
| `unattributed-export` | matching export entry has no unique attribution | — |
| `ambiguous-export` | matching export entry has multiple attribution candidates | — |
| `unsupported-import-form` | written heritage name matches the local binding of a default/namespace/`import-equals` import | — |
| `duplicate-local-name` | multiple import occurrences in the file introduce the same local name | — |
| `unresolved-heritage-name` | heritage clause names the target but no supported import or same-file declaration resolves it | — |
| `unsupported-declaration-form` | target or implementer is outside the supported declaration forms | — |

Unknowns must never be silently dropped, and must never be rendered as
confirmed or possible results. Detection uses only stored facts: candidate
heritage clauses are gathered by exact heritage-placeholder name lookup
(name match generates **candidates**; only binding evidence promotes them),
plus import occurrences whose imported name matches the target name.

### 3.4 Publication qualification

A separate, explicit indication of the publication-consistency guarantee
behind the whole result, defined in
[publication-consistency-contract.md](publication-consistency-contract.md).
Relationship uncertainty (categories 3.1–3.3) and publication consistency
are **never conflated**: an unknown relationship says nothing about snapshot
consistency, and an inconsistent publication says nothing about whether a
specific binding chain is valid.

## 4. Response shape (proposed, not implemented)

```
InterfaceImpactResultV1
  publication:
    indexIdentity, snapshotIdentity, isConsistentSnapshot
    qualification: "consistent" | "qualified"        // per publication contract
  target:
    declarationId, name, qualifiedName, path, span
  confirmedImplementations[]:
    classNodeId, className, path, heritageSpan
    bindingRoute: "direct-import" | "same-file"
    steps[]: { step, fromNodeId, toNodeId, edgeId,
               evidence { quality, provider, providerVersion, location } }
    bindingEvidence { quality: Resolved, provider: …symbol-binding,
                      providerVersion: binding1.local-v1 }
  possibleConsumers[]:
    fileNodeId, path
    occurrences[]: { specifier, importedName, localName, isTypeOnly,
                     span, resolutionRule }
  unknowns[]:
    subject { nodeId | fileNodeId | writtenName }
    reason (closed enumeration above)
    evidence spans (best available)
  coverage:
    supportedProfile: fixed statement naming the bounded profile
    candidateHeritageClauseCount, unboundHeritageClauseCount
    possibleConsumerModuleCount
  truncation:
    state, omittedFrontierCount, appliedLimits { maxNodes, maxEdges }
```

Ordering is a deterministic total order: confirmed by (class path, class
name, class id); possible consumers by (path, file id); unknowns by (subject
id, reason code); steps in binding-rule order (heritage → import →
resolution → export → attribution). Duplicate suppression: one confirmed
entry per implementing class (multiple heritage clauses or duplicate import
occurrences merge their spans into one entry's step lists); one possible
consumer per importing file (multiple import occurrences merge). Equivalent
queries against the same publication return identical ordered results.

## 5. Semantics and guarantees

- **Input handling:** unknown or non-interface node → explicit input error
  following existing argument-validation conventions; an existing
  declaration with zero supported relationships → a well-formed result with
  empty confirmed/possible lists and a coverage statement — never an error,
  never an implied completeness claim.
- **Bounded traversal:** the query declares and honors node/edge budgets like
  existing traversals; exceeding a budget sets truncation state and reports
  the omitted frontier count. Truncation is visible; it never silently
  narrows a category.
- **Cancellation:** cooperative cancellation at the same boundaries as
  existing traversals; a cancelled query throws and returns **no partial
  classified results**.
- **Provider failures:** the query reads only the published graph; it never
  invokes source providers. Provider failures during re-indexing do not
  affect a bound query (the fail-on-change view fails on publication
  movement instead).
- **Persistence/reopen:** results derive solely from published facts; the
  same publication yields identical results after durable close/reopen
  (Q1 scenario H verified fact survival; the query adds no mutable state).
- **Compatibility:** existing query defaults, presets, and
  `FindImplementationsAsync` semantics are unchanged. Feeding confirmed
  results into the legacy implementations preset is out of scope (Q1 F-08,
  separately authorized compatibility change if ever proposed).

## 6. Coverage statement (mandatory text contract)

Every result includes a fixed coverage statement equivalent to: "This result
covers direct explicit `implements`/extends-type bindings and direct named
relative imports under the documented EC-6D profile on one TypeScript plugin
inventory. It does not cover re-export paths (reported as unknowns where
detected), ordinary type-use references, call sites, implicit structural
implementations, package-resolution, other languages, or transitive
inheritance. Absence from the confirmed list is not evidence of absence of
impact."

This is the F-06 remediation surface: a relationship through an unsupported
barrel appears as `re-export-barrier` unknown; it is not silently omitted,
and the coverage statement prevents false completeness.

## 7. Acceptance linkage

The acceptance matrix in [acceptance-contract.md](acceptance-contract.md)
(A01–A20) is written against this contract. Tests of extraction facts,
binding joins, query semantics, and persistence are distinguished there; a
passing graph-storage test never substitutes for semantic correctness.
