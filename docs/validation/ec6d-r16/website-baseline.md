# EC6D-R16 website baseline (pinned repository)

## Binding and source identity

| Item | Value |
| --- | --- |
| Repository | `https://github.com/jenolaszlo-sketch/website` |
| Pinned qualification commit | `f9cca1773a32bb517e624cc380e1f6d14e0cc317` (subject: "Support sub-path deployment and add GitHub Pages preview workflow") |
| Current checkout HEAD | `f9cca1773a32bb517e624cc380e1f6d14e0cc317` (equal to the pinned commit; recorded so a future HEAD is never silently substituted) |
| Snapshot method | `git archive` of the pinned commit extracted outside Andirit; 78 files re-verified with `git hash-object` against the pinned tree (0 mismatches) |
| Original checkout | not modified (clean before and after) |

The snapshot contains 78 files in total: 9 supported TypeScript/JavaScript
files and 69 unsupported files (`.astro`, Markdown, configuration, assets).
The 22 `.astro` files are outside the plugin inventory, exactly as in the
EC-6C.2 smoke.

## Indexing result (executed)

Command (from the repository root; see [baseline.md](baseline.md) for setup):

~~~powershell
dotnet run --no-build --project docs/validation/ec6d-r16/harness/WebsiteBaseline/WebsiteBaseline.csproj -- $env:TEMP\website-pinned
~~~

### Diagnostics

| Measure | Value |
| --- | ---: |
| Files discovered / unsupported | 78 / 69 |
| Supported files | 9 |
| Index units completed | 11 (9 file units + `typescript:modules` + `javascript:modules` resolution units) |
| Node contributions / distinct nodes | 163 / 161 |
| Declarations | 83 |
| Edge contributions / distinct edges | 313 / 313 |
| Rejected facts | 0 |
| Unresolved relationships | 0 |
| Warning codes | none |
| Publication consistent | `false` (file-system provider; snapshot identity null) |
| TypeScript plugin | `penghou.hetu.tree-sitter.typescript` `0.2.0-preview.6+v0.23.2.abi14.p9` (8 sources examined, 7 contributing) |
| JavaScript plugin | `penghou.hetu.tree-sitter.javascript` `0.2.0-preview.6+v0.25.0.abi15.p9` (1 source examined, 1 contributing) |

`src/env.d.ts` is examined but contributes no facts (a triple-slash
reference only), which is why 8 TypeScript sources produce 7 contributing
sources.

### Supported and unsupported inventory

Supported: `astro.config.mjs`, `src/content.config.ts`, `src/env.d.ts`,
`src/lib/ecosystem.ts`, `src/lib/format.ts`, `src/lib/paths.ts`,
`src/lib/schema.ts`, `src/lib/site.ts`, `src/pages/robots.txt.ts`.

Unsupported (69): the 22 `.astro` components/layouts/pages plus Markdown
content, YAML workflows, JSON/configuration, the README, and site assets.

### Relationship counts by kind

| Kind | Count |
| --- | ---: |
| `contains` | 83 |
| `declares` | 83 |
| `exports` | 33 |
| `imports` | 9 |
| `inherits` | 1 |
| `invokes-name` | 101 |
| `resolves-to` | 3 |

Node kinds: `file` 9, `interface` 10, `type` 12 (module nodes and one
heritage placeholder), `export` 33, `callable` 38, `property` 38, `field` 21.

There are **no `implements` edges and no classes** anywhere in the
supported inventory.

### Module-resolution outcomes

Nine written module references; three resolve to files under
`relative-v1` (rule `extension-substitution`, provider
`0.2.0-preview.6+resolver2.relative-v1`, quality `Resolved`):

| Importer | Specifier | Resolved target |
| --- | --- | --- |
| `src/lib/schema.ts` | `./site` | `src/lib/site.ts` |
| `src/lib/schema.ts` | `./paths` | `src/lib/paths.ts` |
| `src/pages/robots.txt.ts` | `../lib/paths` | `src/lib/paths.ts` |

The remaining six references are non-relative specifiers with no
resolution edge: `astro/config` and `@astrojs/sitemap` (astro.config.mjs),
`astro:content` and `astro/loaders` (content.config.ts), `node:url`
(schema.ts), `astro` (robots.txt.ts). The plugin-level unresolved
counter is 0, consistent with the resolver classifying non-relative
specifiers as external; no external module nodes or resolution edges are
invented.

### Export observations

33 export entries with `Structural` occurrence edges (provider
`…+exports1.local-v1`), matching the EC-6C.2 smoke inventory exactly:

| File | Exported names |
| --- | --- |
| `astro.config.mjs` | `default` |
| `src/content.config.ts` | `collections` |
| `src/env.d.ts` | none |
| `src/lib/ecosystem.ts` | `Status`, `StatusDefinition`, `STATUSES`, `STATUS_BY_KEY`, `Category`, `CATEGORIES`, `CATEGORY_BY_KEY`, `ProjectRef`, `RuntimeLayer`, `RUNTIME_LAYERS`, `SupportingPrimitive`, `SUPPORTING_PRIMITIVES` |
| `src/lib/format.ts` | `formatDate`, `formatDateISO` |
| `src/lib/paths.ts` | `withBase` |
| `src/lib/schema.ts` | `personNode`, `websiteNode`, `profilePageNode`, `ProjectSchemaInput`, `softwareSourceCodeNode`, `BlogPostSchemaInput`, `blogPostingNode`, `BreadcrumbItem`, `breadcrumbNode`, `graph` |
| `src/lib/site.ts` | `SITE`, `NavItem`, `NAV`, `FounderProfile`, `FOUNDER` |
| `src/pages/robots.txt.ts` | `GET` |

Interface exports carry `namespace-projection = type`; value exports carry
`value`. Re-exports, star exports and `export =` produce no entries (out of
the accepted EC-6C surface).

### Query and source evidence

- Exact qualified-name lookups return name-matched candidate sets: for
  `ProjectRef` an export entry, a declaration-less `heritage-target`
  placeholder (from the `SupportingPrimitive extends ProjectRef` clause)
  and the interface node; for each selected interface the interface node
  plus its export entry. `Site` (a non-exported type alias) returns a
  single type node.
- `FindImplementationsAsync` on every selected interface node returns the
  seed only. On the `ProjectRef` heritage placeholder it returns
  `SupportingPrimitive`, but that result is name-matched to a
  declaration-less node, conflates `extends` with implementation, and is
  never bound to the interface declaration.
- Neighborhood queries expose module-level `imports`/`resolves-to`/
  `exports` relationships with provenance, but no imported-name facts: the
  graph cannot say *which* names a module imports.

### Determinism

Two consecutive runs produced identical `IndexIdentity`
(`bb2d95e8b6a2d95a4507ce9ae3290b0308b8498657c69fcb53a5b31c4f21c4be`) and
identical `CanonicalFactsSha256`
(`c890e86a8bef6889a0e2fba3efcb1024bcd3040e265780bb0aea4fd39c05617c`).

## Comparison with prior website evidence

| Surface | EC-6B remediated (p8) | EC-6C.2 (p9) | R16 (current main) | Explanation |
| --- | --- | --- | --- | --- |
| Distinct nodes | 128 | (not re-counted) | 161 | +33 export entry nodes added by EC-6C.2 |
| Declarations | 83 | 83 | 83 | unchanged |
| Distinct edges | 280 | (not re-counted) | 313 | +33 export occurrence edges added by EC-6C.2 |
| `imports` / `inherits` / `invokes-name` / `contains` / `declares` | 9 / 1 / 101 / 83 / 83 | same | 9 / 1 / 101 / 83 / 83 | unchanged |
| `resolves-to` | 3 (sites listed above) | same | 3 (same three targets and specifiers) | unchanged |
| `exports` | none | 33 occurrences/entries | 33 | EC-6C.2 surface preserved |
| Units | 11 (after remediation) | 11 | 11 (9 file + 2 resolution units) | unchanged |
| Plugin versions | `…abi14.p8` | `…abi14.p9` | `…abi14.p9` (TS), `…abi15.p9` (JS) | expected profile state |
| Publication consistent | false | false | false | file-system provider |

Every difference is accounted for by the accepted EC-6C.2 export-surface
addition; no unexplained difference was found and nothing was classified as
a regression. The prior smoke recorded its checkout's total source bytes
(35,810); this run reads the LF `git archive` snapshot (17,905 bytes), so
byte counts are not comparable across checkout line-ending states — the
graph facts above are the comparable surface. This small pinned repository
does not establish large-repository accuracy, scale or performance.

## Limitations (unchanged from inherited qualifications)

- Positive-only export evidence; no completeness claim.
- No `.astro` support; the 22 Astro files are outside every graph fact.
- Module resolution is file-level and policy-bounded; it never proves which
  symbol was imported or used.
- No import-name binding, export attribution, or heritage context facts
  exist yet; these are exactly the missing pieces the accepted EC-6D design
  would add, and their absence is what the consumer cases in
  [website-cases.md](website-cases.md) demonstrate.
