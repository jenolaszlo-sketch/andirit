# EC-6C.2 GLM-Q1 Evidence — Pinned Website Repository Smoke

**Qualification:** EC-6C.2 independent qualification GLM-Q1
**Date:** 2026-10-26
**Harness:** `harness/website-smoke/`; raw output: `website-smoke-report.json`.
**Source binding:** local clone of `jenolaszlo-sketch/website` at
`C:\Users\Laszlos\source\repos\website`, working tree clean on `main` at
`f9cca1773a32bb517e624cc380e1f6d14e0cc317` (verified before the run).

## Scope verification

`git ls-files` filtered to Tree-sitter-supported extensions
(`.ts .tsx .mts .cts .mjs .cjs .js`) returns exactly the nine files below —
eight TypeScript files plus `astro.config.mjs` (17,325 tracked bytes; 580
newlines). The recorded "35,810 source bytes" is exactly the UTF-16 byte count
of the same text under CRLF normalization:
`(17,325 + 580) × 2 = 35,810`. The recorded byte figure is therefore
reconciled and no tracked supported file is missing from the enumerated
allowlist.

The 22 `.astro` files are `.astro` components and remain unsupported; the
local clone additionally contains a gitignored `.astro/` directory with four
generated build artifacts (`.astro/content.d.ts`, `.astro/types.d.ts`,
`.astro/content-assets.mjs`, `.astro/content-modules.mjs`) that are not part
of the pinned source. The harness copies only the tracked supported files into
a clean root before indexing, mirroring a pristine checkout. (An initial
unrestricted run over the local working directory also indexed those four
generated files and produced 3 extra default/type export entries; that run was
superseded by the tracked-file run recorded here. Zero `.astro` component
facts were produced in either run.)

## Independent allowlist enumeration

Each of the nine files was read manually; every line-initial `export`
statement was enumerated (a secondary scan confirmed no non-initial
`export` occurrences exist):

| File | Independently enumerated exported names | Count |
| --- | --- | ---: |
| `astro.config.mjs` | `default` (expression) | 1 |
| `src/content.config.ts` | `collections` | 1 |
| `src/env.d.ts` | — | 0 |
| `src/lib/ecosystem.ts` | `Status`, `StatusDefinition`, `STATUSES`, `STATUS_BY_KEY`, `Category`, `CATEGORIES`, `CATEGORY_BY_KEY`, `ProjectRef`, `RuntimeLayer`, `RUNTIME_LAYERS`, `SupportingPrimitive`, `SUPPORTING_PRIMITIVES` | 12 |
| `src/lib/format.ts` | `formatDate`, `formatDateISO` | 2 |
| `src/lib/paths.ts` | `withBase` | 1 |
| `src/lib/schema.ts` | `personNode`, `websiteNode`, `profilePageNode`, `ProjectSchemaInput`, `softwareSourceCodeNode`, `BlogPostSchemaInput`, `blogPostingNode`, `BreadcrumbItem`, `breadcrumbNode`, `graph` | 10 |
| `src/lib/site.ts` | `SITE`, `NavItem`, `NAV`, `FounderProfile`, `FOUNDER` | 5 |
| `src/pages/robots.txt.ts` | `GET` | 1 |
| **Total** | | **33** |

## Observed output (p9 plugins)

- Index status: `Completed`; parser warnings: none (`[]`).
- File units: 9 (exactly the tracked supported files).
- Export entries: 33; export occurrence edges: 33.
- Per-file occurrence counts: 1 / 1 / 0 / 12 / 2 / 1 / 10 / 5 / 1 — identical
  to the independent enumeration.
- Every observed name, public-name token span (one-based, end-exclusive
  UTF-16), and projection matched the independently enumerated statement:
  interfaces and type aliases carry `namespace-projection=type`; consts and
  functions `value`; the `astro.config.mjs` default expression
  (`export-via=expression`); all named direct declarations
  (`export-via=direct`). Full detail is in `website-smoke-report.json`
  (names, spans, entry IDs, edge IDs, per-occurrence properties).
- Export entry identities follow the accepted module/name formula (verified
  independently by the qualification test suite, which recomputes the
  formula from the contract text).
- `.astro` component facts: 0.
- No `exported-as` or `binds` edges were produced and no repository-wide
  export completeness is claimed by this smoke.

## Interpretation limits

This smoke asserts only the enumerated positive allowlist for the nine
supported files. It does not establish complete export surfaces for any
module, does not cover `.astro` syntax, and does not support any claim beyond
the written occurrences listed above.
