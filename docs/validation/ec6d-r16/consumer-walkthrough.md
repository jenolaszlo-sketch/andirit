# EC6D-R16 consumer walkthrough

This is the material the intended consumer reviews before recording a
decision in [consumer-decision.md](consumer-decision.md). Every "current"
statement was executed against the R16 baseline; every "proposed" statement
is a contract-derived illustration, not running code.

## 1. The scenario

You are about to change a TypeScript interface. In the controlled corpus
the interface is:

```ts
// src/contracts/Renderable.ts
export interface Renderable {
  render(context: RenderContext): string;
}
```

Four classes implement it in the corpus (`SvgRenderer`, `HtmlRenderer`,
`AbstractRenderer`, and `AliasRenderer` via the alias `Drawable`), one
implements a *different* interface with the same name in another file
(`OtherRenderer`), one extends an implementer rather than implementing the
interface (`CanvasRenderer`), one imports through a re-export barrel
(`BarrelPreview`), and one targets a missing module (`MissingImport`).

The consumer question: **which classes directly implement this interface,
which modules might depend on it, and what evidence supports each answer?**

## 2. What happens today (observed)

| Question | Today's answer with public Andirit queries |
| --- | --- |
| Direct implementers of the selected interface? | **None found** (`FindImplementations` returns the seed only; recall 0/4). |
| Known candidates? | An exact-name lookup mixes 2 interfaces, 2 export entries and 4 declaration-less placeholders — you must bring the file identity yourself. |
| Fallback by name? | Querying the placeholders returns 3 correct classes **plus `OtherRenderer`, a false positive from a different same-named interface**, and still misses `AliasRenderer`. |
| Who might depend on it? | Six file-to-file `resolves-to` edges exist (including the export barrel), but nothing records *which symbol* was imported. |
| Why does the graph believe anything? | Edges carry source spans, providers and versions; the critical symbol-binding edge simply does not exist yet. |

Example of the honest current output for the primary question:

```text
FindImplementations(selected Renderable interface)
  -> 1 node (the seed itself), 0 implementers

FindImplementations(placeholder "Renderable" in SvgRenderer.ts)
  -> SvgRenderer  (correct name match, but no declaration binding)

FindImplementations(placeholder "Renderable" in OtherRenderer.ts)
  -> OtherRenderer  (false positive: implements other/Renderable.ts)
```

Current publication qualification is also material: the file-system
provider reports `IsConsistentSnapshot = false`, so today's results can only
ever be navigation-grade, never snapshot-authoritative.

## 3. What EC-6D would answer (proposed, illustrated)

For the same interface, the accepted `GetInterfaceImpactV1` contract would
return three clearly separated categories (illustration in
[controlled-fixture.md](controlled-fixture.md) §3):

**Confirmed (4)** — each with a complete ordered witness:
`SvgRenderer`, `HtmlRenderer`, `AbstractRenderer`, `AliasRenderer` — the
exact four independently reviewed implementers, each bound to the selected
physical declaration through its import route (or the same-file route where
applicable), with the heritage clause span, the import occurrence
(imported/local names, type-only status, spans), the module-resolution step
(rule and policy), the export entry and occurrence, and the attribution
record that ties the export to the declaration.

**Possible consumers (5)** — `SvgRenderer.ts`, `HtmlRenderer.ts`,
`AbstractRenderer.ts`, `AliasRenderer.ts`, `Preview.ts`: modules with a
resolved direct named relative import touching the target module. These are
labeled as *possible* dependency only — never as proof of type use — and
carry the concrete import occurrences.

**Explicit unknowns (never silent)** — `BarrelPreview.ts` →
`re-export-barrier` (with the barrel's re-export records as separate
witnesses); `MissingImport.ts` → `unresolved-module`. Additionally the
result refuses to confirm and explicitly explains unsupported heritage
forms, scope problems, duplicates and ambiguity whenever they occur
(contract reason table), and reports a coverage block plus a mandatory
coverage statement.

The result also carries the full publication identity and its
qualification, and — because the shipped provider is
snapshot-inconsistent — a checked consistent-only authority path exists so
a `qualified` result cannot be promoted to an authoritative impact
statement by accident.

## 4. The evidence chain (why each classification can be trusted)

For every confirmed entry the contract requires an ordered witness whose
steps are stored facts: heritage clause → heritage context (lexical scope,
ambient/recovery flags) → import occurrence → module-resolution join →
export entry + occurrence → declaration attribution → physical declaration
record, ending in a witness identity computed from those fact ids. No step
may cite a value that is not a stored fact; a missing step inside a claimed
witness is a fail-closed integrity error, not a guess. Unknowns are equally
traceable: a `re-export-barrier` lists every contributing re-export record,
never the first one.

## 5. What the proposed query deliberately refuses (and why that is
valuable)

| Refusal | Why it protects you |
| --- | --- |
| `OtherRenderer` never appears for the selected declaration | name equality is not identity; the chain completes to a different declaration and is omitted |
| `CanvasRenderer` never appears | transitive inheritance impact is out of scope; the result says so instead of guessing |
| Same-named placeholders never become implementers | no declaration-bound witness, no confirmation |
| `BarrelPreview` never confirmed through the barrel | re-export closure is not analyzed; it becomes a visible barrier unknown instead |
| Ordinary type-use, calls, `.astro` sources, other languages, multiple-hop re-exports | listed in the not-evaluated frontier — the result is honest about the boundary of the purchase |
| `qualified` results refused by the authority path | an automated change cannot be gated on a non-snapshot-authoritative answer |

## 6. Reality check from the pinned website (real repository)

The real `jenolaszlo-sketch/website` repository was indexed at its pinned
commit. The honest findings (details in
[website-cases.md](website-cases.md)):

1. **No real class implements any interface there** — a genuine
   interface-impact positive case cannot be demonstrated from this
   repository; the value at this consumer therefore comes from visibility,
   refusal quality and navigation honesty rather than from finding
   implementers.
2. The repository's interfaces have same-file type consumers and
   `.astro` consumers; both are outside the confirmed profile. The proposed
   result correctly reports no supported consumers and discloses the
   frontier rather than claiming "no impact".
3. Its one heritage clause (`SupportingPrimitive extends ProjectRef`) would
   be explained as an explicit `heritage-kind-not-supported` unknown —
   strictly better than today's placeholder query, which looks like a
   positive result and is not.
4. Value-navigation questions (for example "who imports `withBase`?") are
   outside the proposed interface-only query; the graph would still need a
   symbol-level navigation query for them.

## 7. Decision questions for the intended consumer

Recorded in [consumer-decision.md](consumer-decision.md):

- **Q1 Utility** — would this result materially help you understand or
  modify the code?
- **Q2 Trust** — is the distinction between confirmed, possible and unknown
  relationships clear enough to avoid misleading conclusions?
- **Q3 Actionability** — would the provided source locations and witnesses
  let you inspect or act on the result without manually reconstructing the
  import chain?
- **Q4 Scope** — are the deliberately unsupported cases acceptable for an
  initial bounded release?

Plus: any required changes before implementation.
