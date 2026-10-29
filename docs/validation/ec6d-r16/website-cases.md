# EC6D-R16 website cases

Binding: `jenolaszlo-sketch/website` at
`f9cca1773a32bb517e624cc380e1f6d14e0cc317` (see
[website-baseline.md](website-baseline.md)). All expectations below were
manually reviewed against the actual source; Andirit graph output was used
as comparison, not as ground truth. The TypeScript 5.9.3 oracle
([tools/website-ts-oracle.cjs](tools/website-ts-oracle.cjs)) provides the
independent semantic cross-check.

## Headline finding

**NO ELIGIBLE REAL-WEBSITE INTERFACE-IMPACT CASE.**

The pinned website contains **no class declarations and no `implements`
clauses** anywhere in its supported inventory. The TypeScript 5.9.3 oracle
over the same nine files reports 10 interfaces, 0 classes and 0 implements
clauses; a lexical scan of all `.ts` and `.astro` files confirms that every
interface name (`StatusDefinition`, `ProjectRef`, `SupportingPrimitive`,
`RuntimeLayer`, `Category`, `ProjectSchemaInput`, `BlogPostSchemaInput`,
`BreadcrumbItem`, `NavItem`, `FounderProfile`) appears **only inside its
declaring file**.

This outcome is recorded as required and is not treated as an R16 failure.
The website still contributes real-source coverage for module resolution,
export evidence, an interface-`extends` clause, and honest boundary
behavior, demonstrated by the three genuine cases below. The accepted
design was **not** expanded to manufacture a case.

## Cross-check: TypeScript oracle vs Andirit on the pinned snapshot

| Relationship | Andirit (tree-sitter, syntax) | TypeScript 5.9.3 (semantic, 9-file program) | Agreement |
| --- | --- | --- | --- |
| Classes / `implements` clauses | 0 classes, 0 `implements` edges | 0 classes, 0 implements clauses | exact |
| Interface `extends` | 1 `inherits` edge: `SupportingPrimitive` → declaration-less `Renderable`-style placeholder for `ProjectRef` | 1 heritage clause resolved to the `ProjectRef` interface in `src/lib/ecosystem.ts` | same clause; Andirit has no declaration binding (by design) |
| Relative module imports resolved | 3 (`schema.ts`→`site.ts`, `schema.ts`→`paths.ts`, `robots.txt.ts`→`paths.ts`) | 3, same specifiers and targets | exact |
| Non-relative imports | 6, no resolution edges (external policy) | 6 external specifiers, unresolved by design in this context | consistent |
| Interface references | not represented (no type-use facts at this profile) | every interface referenced only in its own file; `ProjectRef` referenced at `ecosystem.ts` 121 and 200 | Andirit cannot express this; records as not-evaluated |

Context differences: the oracle type-checks the nine supported files
(`.astro` sources are not TypeScript and are outside the program); Andirit
extracts syntax-level facts and deliberately makes no compiler-equivalence
claim. The oracle result strengthens — it does not replace — the manual
review.

## Case W1: `ProjectRef` — interface-extends observability

| Item | Value |
| --- | --- |
| Commit | `f9cca1773a32bb517e624cc380e1f6d14e0cc317` |
| Selected declaration | `src/lib/ecosystem.ts` / `ProjectRef` (interface, line 111; node `typescript:symbol:03239c22fed230cca8f6d62adef0321ada73b04e96a2be6844b02d65fecb8e6f`; export entry `typescript:export:6175ac9c…`) |
| Relevant sites | same file: `RuntimeLayer.projects: ProjectRef[]` (line 121); `export interface SupportingPrimitive extends ProjectRef` (line 200, clause span 200:46..200:56) |
| Source evidence | `SupportingPrimitive` node `typescript:symbol:9a88b6a9…`; stored `inherits` edge `typescript:e84ee870eadac52e19e90c8559601d2f40bd3bad1cccf03b872611a00805ba98` → declaration-less placeholder `typescript:heritage-target:6175ac9c…`; **no** `implements` facts anywhere |

**Why relevant:** it is the only real heritage clause in the repository and
it exercises the accepted rule that interface-extends is observed, refused
confirmation, and reported explicitly.

**Current behavior (executed):** exact-name lookup returns three
name-matched candidates (export, placeholder, interface). Querying the
interface returns seed-only; querying the export returns seed-only;
querying the *placeholder* returns `SupportingPrimitive` — a name-matched
result that conflates `extends` with implementation and carries no
declaration binding. A consumer cannot tell from the result that no class
implements the interface.

**Expected EC-6D (contract illustration):**

- confirmed implementations: none (correct — no `implements` clauses
  exist);
- unknown: `heritage-kind-not-supported` for the `SupportingPrimitive`
  clause, target-associated by its written name, with fact C evidence
  (`[new C]`) and the stored clause span 200:46..200:56 — the relationship
  stays visible instead of silently disappearing;
- possible consumers: none (no supported file imports `ProjectRef`; the
  `.astro` files do not reference it either);
- coverage: ordinary type-use (line 121) and transitive/extends propagation
  are in the not-evaluated frontier.

**Expected negatives / unsupported cases:** the clause is never confirmed;
the placeholder node is never reported as the implementation target; no
possible consumer is invented from the `.astro` inventory.

**What a coding agent would do with the result:** treat the interface as
having no supported direct implementers; treat `SupportingPrimitive` as a
known unevaluated extension point that must be inspected manually; avoid
the current placeholder trap (which looks like a positive result).

## Case W2: `StatusDefinition` — a change with same-file type consumers only

| Item | Value |
| --- | --- |
| Commit | `f9cca1773a32bb517e624cc380e1f6d14e0cc317` |
| Selected declaration | `src/lib/ecosystem.ts` / `StatusDefinition` (interface, line 8; node `typescript:symbol:d65663eb63f1d1d18e658c67cc66c54a0cc789be4ab401e8d75f5c1fc996343c`; export entry `typescript:export:cdc17ffc56134e233115d26fa9f1cea997cedb73969778c1cc461a56c2f19dae`) |
| Relevant sites | same file: `STATUSES: StatusDefinition[]` (line 19); `Record<Status, StatusDefinition>` (lines 50, 52) — oracle-confirmed semantic type references |
| Cross-file consumers (repository reality) | none: no `.ts` or `.astro` file names `StatusDefinition`. `ProjectStatus.astro` imports the `Status` alias and the `STATUS_BY_KEY` value; other pages import `CATEGORIES`/`STATUSES`/etc. |

**Why relevant:** it is the most common real-world interface shape in this
repository — an internal shape used through annotations in its own module,
with value-level consumption elsewhere. It tests whether the bounded query
communicates its own blind spots.

**Current behavior (executed):** per-symbol questions cannot be asked at
all; only module-file resolution and positive export entries exist, so both
"who imports this name" and "what does it affect" are unanswerable.

**Expected EC-6D (contract illustration):**

- confirmed implementations: none (no classes);
- possible consumers among the supported inventory: none — no supported
  file has a named relative import occurrence of `StatusDefinition`;
- unknowns: none; the same-file annotation references are ordinary
  type-use, which the accepted profile places in the **not-evaluated**
  frontier (they are not even classified as unknowns);
- coverage discloses the not-evaluated frontier; the `.astro` files are
  outside the plugin inventory entirely.

**Expected negatives / unsupported cases:** no fabricated consumer from
the `.astro` imports of *other* names; no claim that changing the interface
is safe. The compiler-visible truth is that a required-member change breaks
lines 19/50 within the same module; the query does not say so.

**What a coding agent would do with the result:** read the coverage text,
recognize the profile boundary, and manually inspect same-file annotations
and Astro call sites. The value delivered is honesty: the result cannot be
mistaken for a whole-repository impact statement — provided the coverage is
surfaced. This case is the strongest argument for the consumer's trust
question (walkthrough Q2).

## Case W3: `withBase` — cross-module named import navigation

| Item | Value |
| --- | --- |
| Commit | `f9cca1773a32bb517e624cc380e1f6d14e0cc317` |
| Selected declaration | `src/lib/paths.ts` / `withBase` (function, line 12; node `typescript:symbol:342482c8f8e1339951967160a97c635d79be76d24a036695c350bada476a8c18`; export entry `typescript:export:a2b6e6cc…`; export occurrence edge span 12:17..12:25) |
| Supported import sites | `src/lib/schema.ts` line 3 (stored imports edge `typescript:d237e614…`, span 3:1..3:36); `src/pages/robots.txt.ts` line 2 (stored imports edge `typescript:4486fc92…`, span 2:1..2:41) |
| Unsupported import sites | 10 `.astro` files (components, layout, pages) import `withBase`; outside the plugin inventory |
| Resolution evidence | stored `resolves-to` edges to `src/lib/paths.ts` from both supported importers (rule `extension-substitution`, provider `…+resolver2.relative-v1`) |

**Why relevant:** it is the strongest genuine cross-module navigation
question with real supported evidence in the repository, and it marks the
boundary of the proposed interface-impact query: `withBase` is a function,
not an interface.

**Current behavior (executed):** the file-level story is correct —
two supported importers resolve to `paths.ts`; the neighborhood query shows
module edges with provenance. But the imported *name* is not recorded, so
"who imports `withBase`" cannot be answered, and the larger set of real
consumers (`.astro`) is invisible.

**Expected EC-6D (contract illustration):** not applicable. `GetInterfaceImpactV1`
accepts one TypeScript **interface** declaration; a callable node is an
input error by contract. The equivalent navigation for values is not part
of this milestone.

**Expected negatives / unsupported cases:** no partial or guessed answer
for a non-interface target; `.astro` sources are disclosed only through the
generic not-evaluated frontier (other languages), not enumerated.

**What a coding agent would do with the result:** use the existing module
neighborhood evidence for a first pass, then fall back to a text/ripgrep
search for the name and for `.astro` consumers. This case shows the
honest scope of the purchase: EC-6D improves interface-shaped questions
and leaves value-navigation questions where they are.

## Limitations recorded by this workstream

1. No real class implementation of any interface exists in the pinned
   website; the confirmed-implementation path of `GetInterfaceImpactV1`
   is demonstrated only by the controlled corpus.
2. The website's real consumers of the lib modules are `.astro` files,
   which no current or proposed EC-6D fact covers.
3. Cross-file use of the website's interfaces is entirely through function
   signatures in the declaring file; that shape is outside the confirmed
   profile by design.
4. Everything above is a single small repository; it does not establish
   accuracy, scale or performance anywhere else.
