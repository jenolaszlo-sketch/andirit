# EC-6B relative module resolution handoff

- **Project:** `jenolaszlo-sketch/penghou-hetu`
- **Milestone:** EC-6B
- **Implementation status:** complete; ready for independent review
- **Independent qualification:** not performed by this implementation task

## A. Git binding

| Item | Identity |
| --- | --- |
| Qualified production baseline | `558f8ac82ac1b5e07da68f4a71a70240eaf38409`, tree `08a01a1b7d5d3109b991bcd05d57272fd3f5f018` |
| Owner-accepted EC-6A decision | `1d11b7330e1d8755d5c3e3b4a830167bcf8b5bf6`, tree `649b3111a273478547d8542770220e043fd5cbd5` |
| Implementation branch | `implementation/ec6b-relative-module-resolution` |
| Implementation worktree | `C:\Users\Laszlos\source\repos\Solo\ec6b-relative-module-resolution` |
| Implementation commit | `8aa083ce36e52f666578f5ed6b539ec194178561`, tree `52f877acb0daf2cdf41fedf2d1d1fb0ff6fda034`; parent is the owner-accepted commit |
| Final documentation/handoff commit | Direct child of the implementation commit on the implementation branch; its full commit and tree identities are included in the task's final completion message because this report is part of that commit |

The implementation worktree was created from the owner-accepted commit after
checking its tree and ancestry. The canonical checkout, `main`, and the design,
review, and decision branches were left unchanged. Nothing was pushed or merged.
After the documentation/handoff commit, the branch worktree was verified clean.

## B. Implementation inventory

- Added the internal `ITreeSitterModuleResolver` seam, the plugin-local
  `EcmaScriptModuleResolver`, inventory/result records, deterministic edge
  creation, and collision rejection in
  `src/Penghou.Hetu.TreeSitter/TreeSitterModuleResolution.cs`.
- Extended the shared ECMAScript profile to capture static imports,
  type-only imports, re-export module references, literal `require`, and
  unsupported dynamic/nonliteral forms without replacing existing structural
  `imports` observations.
- Integrated the pass after per-file extraction. It stages one stable
  `{language}:modules` unit per executing plugin, containing only resolver
  edges. An empty completion replaces stale facts with an empty unit.
- Added the accepted `resolves-to` graph kind and five property keys:
  `specifier`, `module-reference`, `resolution-policy`, `resolution-rule`, and
  `type-only`. No node kinds, exports, binds, impact-preset changes, or graph
  identity migrations were introduced.
- Advanced the ECMAScript extraction profile and both plugin versions from p6
  to p7. Added focused policy, identity, coverage, lifecycle, durable-store,
  failure, cancellation, and endpoint-order tests.

## C. `relative-v1` policy

Resolution uses only decodable source files in the owning TypeScript or
JavaScript plugin inventory. A specifier is relative only when its raw text
starts with `./` or `../`. Paths use POSIX segment arithmetic, ignore `.`
segments, apply `..` only within the repository root, reject empty segments,
and compare paths ordinally and case-sensitively.

Matching order is exact path, ordered suffix substitution for extensionless
paths, then ordered directory index. TypeScript suffixes are `.ts`, `.tsx`,
`.d.ts`, `.mts`, `.cts`; JavaScript suffixes are `.js`, `.jsx`, `.mjs`, `.cjs`.
The first candidate in this declared order wins and its rule is recorded.
An explicit extension is checked only as written: `./x.js` never substitutes
to `x.ts`. A file indexed only by the other plugin is unresolved.

Bare packages, URL schemes, `node:` references, and aliases are external.
Missing and case-mismatched relative targets are unresolved. Absolute/root
escaping paths, backslashes, repeated or trailing separators, query/fragment
suffixes, unsupported extensions, dynamic `import()`, template/nonliteral
`require`, TypeScript `import = require`, `export =`, and overlong text are
unsupported. Specifiers over 256 characters are not retained in graph facts.
Ambiguity remains zero because candidate precedence is explicit. The resolver
does not read files, symlinks, package metadata, compiler configuration,
environment variables, or the network.

## D. Evidence and identity

Every `resolves-to` edge points from the importing file node to an existing
target file node. Its `Resolved` evidence has this exact scope:

> The target was verified under the declared resolution policy and the indexed source inventory of this publication. This does not establish equivalence with TypeScript compiler, bundler, or runtime resolution.

Provider: `penghou.hetu.tree-sitter.ecmascript-module-resolver`. The initial
provider version is
`{packageVersion}+resolver1.relative-v1`. Edges record the raw written
specifier, module-reference form, policy, matching rule, and `type-only: true`
only for explicit type-only syntax.

The edge id follows the accepted formula:

```text
{language}:{sha256(kind \n source \n target \n discriminator \n span)}
```

The discriminator is
`resolves-to:{module-reference}:{raw-specifier-text}`. The span is the
statement or literal `require` call, with one-based lines, one-based byte
columns, and an exclusive end. Properties are not identity inputs. Separate
occurrences remain separate; moving a statement changes its id; LF and CRLF
preserve ids for the same logical source. Duplicate identities fail the pass.
No imported name is claimed to bind to a declaration.

## E. Publication lifecycle

The existing session stages per-file units in ordinal path order and completes
each one. It then resolves against the complete decodable plugin inventory,
stages the edge-only module unit, and completes it. The existing indexing
lifecycle stages obsolete file-unit deletions after the session. Endpoint
validation remains enabled; file nodes are available before the module edge
unit is staged. The store publishes all staged replacements atomically.

Lifecycle tests exercise target deletion/addition, importer specifier changes,
empty replacement, importer deletion, unchanged reindexing, removal of one of
two same-target occurrences, endpoint rejection for wrong staging order,
durable reopen, resolver failure after file staging, and cancellation before
publication. Failure and cancellation tests verify both the in-memory and
LatticeDb stores retain the prior successful publication. The target-deletion
test also verifies the structural `imports` observation remains available.

## F. Tests and verification

Environment: Windows, .NET SDK `10.0.401`, target framework `net10.0`.

| Check | Command or scope | Result |
| --- | --- | --- |
| Focused resolver/lifecycle cases | `dotnet test tests/Penghou.Hetu.TreeSitter.Tests/Penghou.Hetu.TreeSitter.Tests.csproj --no-restore -m:1 --filter FullyQualifiedName~TreeSitterModuleResolution` | 22 passed, 0 failed |
| Full solution | `dotnet test Penghou.Hetu.slnx -m:1 --nologo` | 344 passed, 0 failed: Tree-sitter 112, core 160, C# 46, LatticeDb 26 |
| Formatter | `dotnet format Penghou.Hetu.slnx --verify-no-changes --no-restore --verbosity minimal` | exit 0; no changes required |
| Git whitespace | `git diff --cached --check` and `git diff --check` | clean |
| Authentic p6 website seed | Temporary probe built from owner-accepted commit `1d11b7330e1d8755d5c3e3b4a830167bcf8b5bf6`, filtered to `Ec6bP6HistoricalSeedProbe` | 1 passed; durable LatticeDb graph seeded |
| p6→p7 website upgrade | Temporary probe filtered to `Ec6bWebsiteSmokeProbe` | 1 passed; p7 graph verified against the p6 publication |

The two temporary website probes, source snapshot, and database files were
removed after the results were recorded. They are not part of the implementation
commit.

## G. Compatibility and version invalidation

The TypeScript plugin version advances to
`0.2.0-preview.6+v0.23.2.abi14.p7`; JavaScript advances to
`0.2.0-preview.6+v0.25.0.abi15.p7`. The planner test verifies that a prior
profile version is classified `Changed` when content is unchanged.

An additional real historical migration used the actual p6 extractor source
from the owner-accepted commit to create a durable LatticeDb publication, then
reindexed that same database with p7. The p6 and p7 website graphs had the same
128 unique nodes and 83 physical declarations. All prior node ids, declaration
ids, and structural edge ids were preserved. Structural counts stayed at
9 `imports`, 1 `inherits`, and 101 `invokes-name` edges. The p7 graph added only
three `resolves-to` edges, bringing the total edge count from 277 to 280.
Both Tree-sitter plugins executed during the version transition. No persistent
identity migration was needed.

## H. Real-repository smoke test

Source binding: `jenolaszlo-sketch/website@f9cca1773a32bb517e624cc380e1f6d14e0cc317`;
the recursive Git tree response returned that exact commit SHA and was not
truncated. The repository tree contains 78 tracked files; its supported
Tree-sitter inventory contains 9 files (8 TypeScript and `astro.config.mjs`).
Each source blob was fetched by the SHA in that commit tree and verified with
`git hash-object` before indexing.

| Source path | Verified blob |
| --- | --- |
| `astro.config.mjs` | `264b4e9d3e6f43c82b680d7b2a328a33f27460b9` |
| `src/content.config.ts` | `f5d86a81375048aa1844a3d42791cf933f1d36ff` |
| `src/env.d.ts` | `f964fe0cffd88ccb9fcc20bd3c22e848c879d78a` |
| `src/lib/ecosystem.ts` | `3ba7a3f164d6d84bdeb586dbdd1e1f52b4ae65b9` |
| `src/lib/format.ts` | `2d68e61e3d294e4433f01dc1c65f277366ee7ba8` |
| `src/lib/paths.ts` | `f2e33ba35e6cd9d4a3fce709c83b9dd141446ac5` |
| `src/lib/schema.ts` | `932586352a1f658897c4127027c527c3f8c9ba30` |
| `src/lib/site.ts` | `4f3ed5e64f1ca66e884c683bbf8a05f2cc17a21d` |
| `src/pages/robots.txt.ts` | `76004cb6cc2368b5c2aa94a6382942b8cd37b3ee` |

The p7 publication retained all 21 previously identified file-level variable
declarations and all 83 total physical declarations. It emitted exactly these
three verified module edges:

- `src/lib/schema.ts` → `src/lib/paths.ts`
- `src/lib/schema.ts` → `src/lib/site.ts`
- `src/pages/robots.txt.ts` → `src/lib/paths.ts`

No indexing warnings occurred. Bare package imports remained outside
`relative-v1`; the new edges did not alter existing `imports` or invocation
facts. The `.astro` files and unsupported repository files were not parsed,
and this small source sample does not establish general module-resolution
recall or compiler/bundler/runtime equivalence. Broader real-repository
qualification remains EC-6F.

## I. Known limitations

- `relative-v1` does not perform `.js`→`.ts` substitution, package or Node
  resolution, `tsconfig` alias handling, runtime/bundler inference, or
  cross-plugin TypeScript/JavaScript resolution.
- Dynamic expressions, template/nonliteral calls, import-equals, export-equals,
  unsupported extensions, and overlong specifiers produce no `resolves-to`
  edge. Their coverage outcome is recorded where the syntax site is captured.
- Reparse points are excluded by the existing repository discovery path. The
  resolver itself receives only the resulting inventory and performs no
  filesystem probe; no OS-specific symlink fixture was added in EC-6B.
- Export surfaces, re-export closure, imported-name binding, default export
  interpretation, and `exports`/`binds` facts remain EC-6C/EC-6D work.
- The implementation is ready for independent review; this task did not
  perform that review or claim independent qualification.

## J. Independent review readiness

The reviewer should challenge at least these counterexamples:

1. Root escape, repeated/trailing separators, backslashes, query/fragment
   suffixes, absolute paths, path-case mismatches, and an absent plugin-local
   target must never produce a resolved edge.
2. `./x.js` with only `x.ts`, bare packages, aliases, `node:` and URL
   specifiers, `.json`/`.astro`, dynamic `import()`, template `require`,
   import-equals, export-equals, and overlong text must follow their documented
   non-resolved coverage category.
3. Competing `.ts`/`.tsx` or file/index candidates must follow the declared
   suffix and file-before-index order; `.d.ts` substitution must remain
   reachable.
4. Two identical imports, two different specifiers to one target, imports
   from different files, occurrence deletion, source-line shifts, and LF/CRLF
   input must preserve the stated multiplicity and identity behavior.
5. Target/importer deletion, target addition, empty replacement, p6→p7
   invalidation, failure after file staging, cancellation before publication,
   and wrong endpoint staging order must leave exactly the documented
   published graph.
6. `Resolved` evidence must not be read as import-symbol binding or as
   TypeScript compiler, bundler, or runtime equivalence. Impact presets must
   remain unchanged.

**Disposition: EC-6B IMPLEMENTATION COMPLETE — READY FOR INDEPENDENT REVIEW.**
