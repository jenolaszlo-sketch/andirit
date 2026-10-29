# EC6D implementation qualification plan (forward)

**This is a forward test plan for EC6D-IMPL-01 and its independent
qualification. None of these tests are executed or evidence that anything
works; no part of EC-6D is implemented.** It does not authorize
implementation. It specifies what must be checked once the accepted
contracts are implemented, in addition to the full A01–A28 acceptance
matrix of `docs/planning/ec6d/acceptance-contract.md`.

## 1. Controlled fixture (primary behavioral gate)

Run against `docs/planning/ec6d/corpus/ts-interface-impact` with the
implemented provider set (facts A–E) on both stores:

1. **Four confirmed direct implementations** — `SvgRenderer`,
   `HtmlRenderer`, `AbstractRenderer`, `AliasRenderer` — each with a
   complete ordered witness terminating at the selected physical
   declaration record; no additional confirmations (A01/A02/A06/A07).
2. **Same-name negative** — `OtherRenderer` absent from the result
   (chain completes to a different declaration; omitted, not unknown)
   (A03/A07).
3. **Imported alias** — `AliasRenderer` binds through
   `Renderable as Drawable`; the witness shows localName → importedName →
   entry; literal alias spelling is never matched against entry names (A02).
4. **Same-file binding** — same-file route positive from the Q2 `same-file`
   fixture (`bindingRoute = same-file`, short witness, interface need not be
   exported); `same-file-merged` yields `ambiguous-local-declaration` (A22).
5. **Missing import** — `MissingImport.ts` → occurrence-level
   `unresolved-module`; no confirmation; no possible consumer from that
   import (A11).
6. **Ambiguous and merged declarations** — Q2 `merged` / `type-value`:
   query runs with `declarationRecordId = null`; chains return
   `ambiguous-export` with all contributor records; nothing confirmed;
   no record selected by order (A24).
7. **Unsupported heritage forms** — qualified (`NS.Widget`), expression,
   class/interface extends, nested scope, type-parameter shadowing,
   ambient/global, recovered syntax: each produces its closed reason with
   stored evidence; none confirms (A12/A21/A25/A26).
8. **Re-export barriers** — `BarrelPreview` → occurrence-level
   `re-export-barrier`; named/star/missing/multi/no-declaration barrels →
   clause-level barriers listing every fact E record as separate witnesses,
   never first-wins; unrelated barrels never contaminate (A10/A10b/A27).
9. **Complete source evidence** — every confirmed entry exposes every
   witness step with spans, provider, provider version (heritage evidence
   cites its own `…abi14.p8`), evidence quality, source hash and unit
   origin; no step cites a non-existent fact (A13).
10. **Deterministic witness identity** — `binding1.local-v1` recomputed
    identically after reopen; identical ordered results, witnesses,
    occurrences and unknowns across repeated queries and reversed
    enumeration (A14).
11. **Structured unknown reasons** — only the closed reason table is
    emitted; reserved reasons never appear; unknown counts reconcile with
    the coverage block (A25/A26).
12. **Bounded result ordering and truncation** — the nested total order
    holds; charged-work budgets mark `partial = true` with stage and
    omitted frontier; return-size caps are never presented as execution
    bounds (A14, consumer-query contract §4–§5).

## 2. Website (real-source regression and honesty gate)

Run against the pinned snapshot
`f9cca1773a32bb517e624cc380e1f6d14e0cc317`:

1. **Existing indexed facts remain intact** — p9 export entries, import
   facts, `resolves-to` edges, declarations and structural heritage facts
   keep ids, evidence and properties (A19); the R16 baseline numbers in
   [website-baseline.md](website-baseline.md) are the comparison surface
   (161 nodes / 83 declarations / 313 edges before new facts).
2. **Applicable interface-impact cases** — all ten website interfaces
   return well-formed results: zero confirmed (no classes exist), zero
   supported possible consumers for interface names, and the
   `SupportingPrimitive extends ProjectRef` clause appears as
   `heritage-kind-not-supported` with fact C evidence.
3. **Unsupported source forms do not produce false confirmations** —
   `.astro` sources remain outside the inventory; no consumer or
   implementer is invented from them; no name-matched placeholder is ever
   returned as an implementation (the current trap in
   [website-cases.md](website-cases.md) Case W1 must be gone for the new
   query; `FindImplementations` legacy behavior remains pinned by F-08 and
   is unchanged).
4. **Repeat indexing preserves identity** — two runs over the unchanged
   snapshot produce identical canonical facts and query results (A15);
   the new facts A/C/E/B appear deterministically.
5. **Durable reopen preserves results and provenance** — LatticeDB:
   commit, close, reopen; the website query results, witnesses (identity
   recomputed identically), unknowns, coverage and qualification survive
   (A16).
6. **No misleading whole-repository impact claim** — every result carries
   the mandatory coverage statement and qualification; the not-evaluated
   frontier names ordinary type-use, other languages and multi-hop
   re-exports, so the W2/W3 boundary cases cannot be read as "no impact"
   without the caveats.

## 3. Technical qualification (in addition to A01–A28)

1. **In-memory and LatticeDB behavior** — identical classified results,
   witnesses and coverage on both stores; store-specific lifecycle tests
   (A16/A18b).
2. **Authentic p9-to-new-profile compatibility** — a durable p9
   publication seeded before implementation must migrate/re-extract to the
   new profile preserving all pre-existing p9 facts byte-for-byte (A19);
   the accepted migration rehearsal pattern applies; evidence must state
   that the historical migration hash remains unreproducible (FQ1-EV-1
   qualification).
3. **Publication consistency** — A17 truth table including the
   `true`/null contradiction; serialization preserves qualification; the
   checked authority path refuses `qualified` results; no digest/reopen
   claim is ever cited as snapshot consistency.
4. **Cancellation and failure behavior** — cancellation and mid-query
   failures return no partial classified results; failed/cancelled
   indexing retains the last successful publication without a freshness
   claim; publication movement during a fail-on-change query (including
   same-content/new-run) raises the existing failure (A18/A18b).
5. **Existing query and traversal compatibility** — all 391 baseline tests
   pass unmodified; `FindImplementations` semantics and presets unchanged
   (A20); new behavior additive and opt-in.
6. **Windows, Ubuntu and macOS CI** — the repository's existing
   cross-platform CI must pass on the implementation commit.
7. **Performance qualification is a separate later gate** — no numeric
   target exists; measure indexing wall time, peak memory, query latency
   by result size, durable reopen time and repeated-index cost on a
   representative repository before any threshold is set (G7/performance
   remain NOT EVALUATED).

## 4. Independent qualification requirements

The independent reviewer must be able to check out the exact accepted
source and corpus revisions, follow documented restore/build/run steps,
and reproduce the selected declaration, direct implementers, possible
importers, negative cases, provenance, canonical facts and test results
**without using implementation-generated counts as ground truth**
(acceptance contract §Independent consumer qualification). Platform or
store gaps must be recorded, not inferred.
