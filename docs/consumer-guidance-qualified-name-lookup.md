# Consumer guidance: name lookup, candidates, and impact traversal

Status: documentation-only amendment from EC-4 (real-repository
qualification), 2026-10-23, incorporated into canonical documentation during
EC-5 closure (2026-10-23). It adds no graph vocabulary and changes no query
API. It records the consumer consequence of qualification finding
`EC3-GLM-Q2 F-Q2-01`, reproduced on real-world code in the EC-4 qualification
report preserved on `review/ec4-real-repository-qualification` at `1fbc113`
(`docs/reviews/ec4-real-repository-qualification.md`).

## One displayed name is not one symbol

Exact qualified-name lookup (`CodeGraphQueryService.FindSymbolsByQualifiedNameAsync`)
matches node `QualifiedName` exactly and preserves ambiguity. Callers must not
assume that the result list contains only resolved declarations, nor that two
candidates with the same displayed name are the same entity.

Result lists can contain, at the same time:

- **Declared symbols** — nodes with a `SymbolId` and one or more physical
  `CodeGraphDeclaration` records. These are the graph's semantic identities.
  EC-5 file-level variables are declared symbols (`field` nodes with a
  `SymbolId` and physical declarations); ECMAScript class fields are
  owner-derived `property` members with member-kind discriminator `field`, not
  `field` nodes. Ownership and declarations distinguish a module variable from
  a class member because both reuse structural vocabulary.
- **Declaration-less candidates** — nodes with `SymbolId == null` in dedicated
  identity namespaces:
  - `{language}:heritage-target:{hash}` for written `extends`/`implements`
    names (structural, never bound to a declaration);
  - `{language}:invoked:{hash}` for written call and `new` target names
    (structural invocation observations; these carry no qualified name, so
    exact-name lookup does not return them, but name-pattern search does);
  - `{language}:import:{hash}` for written import/require specifiers.

All identity hashes render as lowercase hexadecimal.

Observed on a real repository (EC-4): exact lookup for `ProjectRef` returned
the declared interface *and* the declaration-less heritage target written at
`extends ProjectRef`; pattern search for `abs` returned the declared function
*and* the invocation placeholder for its written call sites. EC-5 adds one
more legitimate multi-candidate case: a function-valued file-level variable is
both a declared `field` and the pre-existing callable projection (`GET` is the
recorded example), and the two nodes stay separate under one displayed name.

## Distinguish the five concepts

| Concept | What it means | How to recognize it |
| --- | --- | --- |
| Display-name equality | Two nodes print the same name | nothing else follows from this |
| Graph-node identity | `CodeNodeId` is unique per node | compare ids; same name never implies same node |
| Structural observation | Written syntax with source location and provenance | candidate namespaces above; `EvidenceQuality.Structural` |
| Symbol-bearing declaration | Stable symbol identity with declarations (not a compiler-resolution claim) | `SymbolId != null` plus `GetDeclarationsAsync` |
| Impact traversal | Bounded walk over specific relationship kinds | preset excludes `invokes-name`; see below |

## Impact and invocation guidance

- `GetImpactSetAsync` follows incoming `references`, `calls`, `implements`,
  `inherits`, and `depends-on`. It deliberately excludes `invokes-name`: a
  matching written name is not a resolved call. In Tree-sitter publications
  the result can therefore be empty even when call sites exist.
- To find written call sites, traverse incoming
  `CodeEdgeKinds.InvokesName` explicitly (for example with
  `GetNeighborhoodAsync`) and interpret the result as structural invocation
  observations, never as guaranteed runtime or semantic impact.
- To ask "what members belong to this interface/class?", filter
  `GetNeighborhoodAsync` to outgoing containment from the owner node; the
  neighborhood API traverses both directions.

## Practical rules

1. Filter exact-name candidates by `SymbolId` presence before treating a
   result as a declaration; keep declaration-less candidates visible when the
   question is about written syntax.
2. Never merge, deduplicate, or bind candidates by display name.
3. Treat `:heritage-target:`, `:invoked:`, and `:import:` ids as syntax
   observations with provenance, not as symbols.
4. Report evidence quality alongside every relationship you surface.
5. A `modifiers` value containing `export` records directly written syntax on a
   supported declaration (currently EC-5 file-level variables). It is not a
   resolved export binding, and no export relationship is emitted.
