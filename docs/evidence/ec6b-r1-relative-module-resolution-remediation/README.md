# EC-6B-R1: Relative module resolution remediation

- **Project:** `jenolaszlo-sketch/penghou-hetu`
- **Milestone:** EC-6B-R1 (bounded remediation of EC-6B)
- **Status:** IMPLEMENTED · VERIFIED LOCALLY · **READY FOR INDEPENDENT REVIEW**
- **Independent re-qualification:** not performed by this task. EC-6B is not
  accepted or closed by this evidence.

This handoff answers the independent review
(`docs/reviews/ec6b-glm-q1-relative-module-resolution-verification.md`, review
branch `review/ec6b-glm-q1` at `66690587f4507d47e78fa4e1996e050b830c1642`).
That report is historical evidence and was not modified.

## A. Git provenance

| Item | Identity |
| --- | --- |
| Repository | `jenolaszlo-sketch/penghou-hetu` (`origin` verified, not inferred) |
| Accepted pre-implementation baseline | `1d11b7330e1d8755d5c3e3b4a830167bcf8b5bf6` |
| EC-6B implementation | `8aa083ce36e52f666578f5ed6b539ec194178561` (tree `52f877acb0daf2cdf41fedf2d1d1fb0ff6fda034`) |
| EC-6B documentation/handoff | `691a8327ab7446416de22fe26e023662399b6cf2` (tree `f2bb8c5faa301fdfa8542fc1d39aaeec68040356`) |
| Independent review preservation | `66690587f4507d47e78fa4e1996e050b830c1642` (tree `fc2bedc73a6503a69598b1e0ac2752cf43fe4831`, parent `691a832`) |
| Remediation branch | `implementation/ec6b-r1-relative-module-remediation` |
| Remediation worktree | `C:\Users\Laszlos\source\repos\Penghou.Hetu.ec6b-r1` |
| Branch point | exactly `691a8327ab7446416de22fe26e023662399b6cf2` |
| Implementation commit 1 (source + tests) | `ffdcdbdf57e35f94a1e459a5b393649778c21f69` (tree `4f3f27cc4bc99b6e9293643a908d107c076a6b63`, parent `691a832`) |
| Implementation commit 2 (one regression test) | `b915aed2a795f38d682fcd8289282eecd8110666` (tree `559ec7ba97d15a578498bc02ae03e9d9ca3fd857`, parent `ffdcdbd`) |
| Documentation/evidence commit | the commit that contains this file; parent `b915aed` (full hash and tree are recorded in the completion message because this file is part of that commit) |
| Tested source revision | `b915aed2a795f38d682fcd8289282eecd8110666` |
| Operating system / SDK | Windows 10.0.26200 (win-x64); .NET SDK 10.0.401; `net10.0` |

Canonical `main` (`558f8ac`), the EC-6B implementation branch (`691a832`), the
review branch (`6669058`), and every earlier worktree were left unchanged. No
pull, rebase, merge, reset, or push was performed. The p6 historical seed used
a throwaway local clone outside the repository; its uncommitted probe and
database were deleted after the run.

## B. Review findings and dispositions

| Finding | Review disposition | Remediation disposition |
| --- | --- | --- |
| **F02** escaped-specifier truncation | CONFIRMED DEFECT (P2) | **Fixed** in `EcmaScriptExtractionProfile.RawModuleSpecifier`; permanent regression tests; behavior documented |
| **F03** `type` binding misclassified type-only | CONFIRMED DEFECT (P3) | **Fixed** with grammar-structure detection; positive, negative, mixed, and JavaScript tests |
| **F01** coordinate contract mismatch | CONFIRMED DEFECT (P3) + qualification defect | **Corrected as docs/contract only** (preferred remediation): emitted coordinates were already consistently UTF-16 code units; no identity migration; declared-system test added |
| **F04** nested `require` in `export =` | DESIGN/CONTRACT AMBIGUITY (P3) | **Confirmed consistent** with the accepted "literal `require` (top level or nested)" rule; documented explicitly and pinned by a test; no behavior change |

## C. Files changed and rationale

Implementation (`ffdcdbd`, `b915aed`):

- `src/Penghou.Hetu.TreeSitter/EcmaScriptExtractionProfile.cs`
  - F02: `RawModuleSpecifier` now rejects any string literal containing an
    `escape_sequence` child and reads the complete literal text between the
    quotes; it never reads a single fragment and never decodes.
  - F03: `HasStatementTypeModifier` and `IsTypeOnlySpecifier` detect the
    `type` keyword from parser structure. Statement-level `type` is an
    anonymous token; an inline modifier is `type` followed by a binding name.
    `{ type }`, `{ type as x }`, `export { type as u }`, and `import type from`
    are bindings, not modifiers. The grammar's error-recovery representation of
    `export type * from` / `export type * as ns from` is recognized explicitly.
  - Profile revision 7 → 8 for the emitted-fact change (D-16).
- `src/Penghou.Hetu.TreeSitter/TreeSitterModuleResolution.cs`
  - Resolver revision 1 → 2; provider version becomes
    `{packageVersion}+resolver2.relative-v1`.
- `src/Penghou.Hetu.TreeSitter/TreeSitterSyntax.cs`
  - Column documentation corrected to UTF-16 code units.
- `tests/Penghou.Hetu.TreeSitter.Tests/TreeSitterModuleResolutionTests.cs`
  - Permanent F01–F04 regression tests (see §E).
- `tests/Penghou.Hetu.TreeSitter.Tests/TreeSitterTypeScriptPluginTests.cs`,
  `TreeSitterJavaScriptPluginTests.cs`, `TreeSitterModuleResolutionLifecycleTests.cs`
  - Version expectations `.p7` → `.p8`, `resolver1` → `resolver2`.

Documentation (the documentation/evidence commit):

- `docs/ecmascript-module-resolution.md` — UTF-16 coordinate definition,
  escaped-specifier rejection, nested-`require` rule, current provider version,
  p8/resolver2 invalidation statement.
- `docs/ecmascript-symbol-identity.md` — coordinate and provider-version
  correction, p8 note.
- `docs/design/ec6a-cross-file-resolution-architecture.md` — §7.1.1
  coordinate convention corrected (marked EC-6B-R1) and `.p8`-next milestone
  wording.
- `docs/decisions/ec6a-owner-acceptance.md` — the two coordinate statements
  corrected with an explicit EC-6B-R1 note; substantive acceptance is
  unchanged.
- `docs/evidence/ec6b-relative-module-resolution-handoff.md` — historical
  handoff retained; coordinate wording corrected with a pointer to this
  evidence, and the resolver-revision succession noted.
- `CHANGELOG.md`, `ROADMAP.md`, `docs/language-support-roadmap.md`,
  `docs/language-feature-matrix.md` — status, invalidation, and tracker
  updates (EC-6B = remediation complete, ready for independent re-review; not
  accepted).

## D. Before / after behavior

### F02 — escaped specifiers

Before (review evidence, probe 21): `import './a\b';` with `src/a.ts` present
emitted a `resolves-to` edge with `specifier: ./a` to `src/a.ts`;
`import './caf\u00e9';` resolved `./caf` to `src/caf.ts`; unsupported count 0.

After (permanent test `ExtractAsync_EscapedSpecifiersAreUnsupportedAndNeverResolve`):
six escaped sites (`\b`, `\\`, `\u00e9`, escaped quote, `\t`, escaped
`require` argument) each classify as `unsupported-targets` with no edge and no
retained specifier text; the single unescaped `./plain` still resolves with
`extension-substitution`. Candidates 7, edges 1, unsupported 6, unresolved 0.
The promoted probe-18 fixture additionally proves that `?`, `#`, `//`, `/abs`,
trailing `/`, bare `./`, and a backslash escape never resolve, while
`./a/./b` normalizes and resolves (candidates 8, unsupported 7).

### F03 — type-only classification

Before (review probe 22): `import { type }`, `import { type as t }`,
`export { type as u }` all carried `type-only: true`, and the statement-prefix
heuristic also misread `import type from './m'` (a default binding named
`type`) as a type-only import.

After: all four negative forms carry no `type-only` property. Genuine forms
keep `type-only: true`: `import type { T }`, `import { type T }`,
`import { type T as U }`, `export type { T }`, `export { type T }`,
`export { type T as U }`, `export type * from`, `export type * as ns from`.
Mixed bindings (`{ type T, V }`) are not wholly type-only. JavaScript has no
type modifier in its grammar, so `type` stays an ordinary binding name there.
The TypeScript three-token `type as as as` form is still read as a modifier,
matching TypeScript's documented parser rule.

### F01 — coordinate system

Trace performed:

- **Parser:** the pinned `TreeSitter.DotNet 1.3.0` binding calls
  `ts_parser_parse_string_encoding(..., InputEncoding.UTF16LE)` on the decoded
  .NET string, so positions are UTF-16 code-unit offsets. The review's
  probe 04 verified the divergence directly (`const s = "中文 café";` before
  the statement: published column 22, UTF-8 byte column 27); the permanent
  test additionally covers a supplementary character (surrogate pair).
- **Span construction:** `EcmaScriptExtractionProfile.Location` adds one to
  line and column; end is exclusive.
- **Identity:** columns are hashed into edge ids; no conversion happens.
- **Serialization/persistence:** `CodeLocation` is stored as integers; stores
  and the Lattice mirror treat them opaquely.
- **Consumers:** the only position consumers order declarations or compare a
  caller-supplied line/column (`ContainsPosition`); none requires UTF-8 byte
  semantics, and the C# plugin computes its own positions.

Decision: correct the contract and documentation rather than convert offsets.
Converting would change every existing edge identity and is a separate
identity-migration milestone. Emitted coordinates and identities are
unchanged by this remediation; the migration below shows byte-identical
structural facts.

### F04 — nested `require`

The accepted extraction contract supports literal `require('...')` at any
nesting depth. `export = require('./m')` therefore contributes one
unsupported `export =` site **and** one `resolves-to` edge attributed to the
`require` call span (`module-reference: require`), with no export or binding
fact. `import x = require('./m')` and `import type X = require('./m')` remain
rejected as whole statements and capture no nested call. The asymmetry is now
documented and pinned by
`ExtractAsync_LiteralRequireNestedInExportEqualsIsCaptured`; production
behavior was not changed.

## E. Regression test inventory

Promoted from the independent review probes:

| Test | Source probe / finding | Invariant |
| --- | --- | --- |
| `ExtractAsync_ReportsUtf16CodeUnitColumns` | probe 04 / F01 (extended with a supplementary character) | Published columns equal one-based UTF-16 code units and differ from UTF-8 bytes; end is exclusive; identity recomputes |
| `ExtractAsync_RejectedPathFormsNeverResolveAtCaptureLevel` | probe 18 | `?`, `#`, `//`, absolute, trailing, bare `./`, backslash → unsupported; `./a/./b` resolves |
| `ExtractAsync_EscapedSpecifiersAreUnsupportedAndNeverResolve` | probe 21 / F02 | No escaped or partially extracted specifier produces a `Resolved` edge; `\b`, `\\`, `\u00e9`, quote, tab, escaped require |
| `ExtractAsync_BindingNamedTypeIsNotTypeOnly` | probe 22 / F03 | `{ type }`, `{ type as t }`, `import type from`, `export { type as u }` are not type-only |
| `ExtractAsync_LiteralRequireNestedInExportEqualsIsCaptured` | probe 17b / F04 | Outer `export =` unsupported; nested literal require captured; no export/bind facts |

Additional permanent coverage added in the same pass:

| Test | Invariant |
| --- | --- |
| `ExtractAsync_ExplicitTypeOnlySyntaxStaysTypeOnly` (8 cases) | Statement-level, inline, aliased, string-name, star, and namespace forms stay `type-only: true` |
| `ExtractAsync_MixedTypeAndValueBindingsAreNotWhollyTypeOnly` (2 cases) | Mixed lists are not marked wholly type-only |
| `ExtractAsync_JavaScriptBindingNamedTypeIsNotTypeOnly` | JavaScript `type` bindings remain ordinary |

Existing EC-6B suites remain in place: policy classification and suffix
precedence, `.d.ts`, `.js`→`.ts` refusal, `.json`/`.astro`, root escape and
case mismatch, self-import and occurrence identity, LF/CRLF, provider and
evidence, lifecycle add/delete/empty/failure/cancellation on both stores,
durable reopen, endpoint-order rejection, cross-plugin boundary, and the
planner's profile-version invalidation.

## F. Version and invalidation

Because emitted resolver facts changed, the accepted single invalidation axis
was advanced (D-16):

- `EcmaScriptExtractionProfile.ProfileRevision`: 7 → 8, so both plugin
  versions end in `.p8` and existing p6/p7 publications re-extract.
- `EcmaScriptModuleResolver.ResolverRevision`: 1 → 2, so evidence carries
  `{packageVersion}+resolver2.relative-v1`.
- No second invalidation axis and no schema change were introduced.
- Unaffected sources produce the same identities and facts (§G).

## G. Verification results

Environment: Windows 10.0.26200, .NET SDK 10.0.401, `net10.0`.

| Check | Command | Result |
| --- | --- | --- |
| Full solution | `dotnet test Penghou.Hetu.slnx -m:1 --nologo` | **363 passed, 0 failed** (C# 46, LatticeDb 26, core 160, Tree-sitter 131) |
| Focused module resolution | `--filter FullyQualifiedName~TreeSitterModuleResolution` | **41 passed, 0 failed** |
| Formatter | `dotnet format Penghou.Hetu.slnx --verify-no-changes --no-restore` | exit 0, no changes |
| Whitespace | `git diff --cached --check` | clean |

### Authentic p6 → remediated migration

A throwaway clone checked out at `1d11b73` (real p6 extractor,
`0.2.0-preview.6+v0.23.2.abi14.p6`) seeded a durable `LatticeCodeGraphStore`
from the isolated website snapshot
`jenolaszlo-sketch/website@f9cca1773a32bb517e624cc380e1f6d14e0cc317`. The
remediated source (`b915aed`, effective profile p8) reopened that database
and reindexed the unchanged sources.

| Measure | p6 | p8 (remediated) |
| --- | --- | --- |
| Units | 9 | 11 (adds `typescript:modules`, `javascript:modules`) |
| Node lines / distinct node ids | 130 / 128 | 130 / 128 |
| Declarations | 83 | 83 |
| Edges | 277 | 280 |
| imports / inherits / invokes-name / contains / declares | 9 / 1 / 101 / 83 / 83 | identical |
| Missing p6 lines | — | **0** |
| Added lines | — | **3 `resolves-to` edges only** |

Both plugins executed (`PluginsExecuted = 2`) although source content was
unchanged, proving the p6/p7→p8 invalidation. All three new edges carry
`+resolver2.relative-v1`. This is a p6→p8 migration rather than p6→p7 because
D-16 requires the effective version to change with emitted facts; p7 was
never independently qualified. The preservation properties required by the
review (128/83/277 plus three `resolves-to` edges) hold exactly.

### Real-repository re-qualification

Fresh indexing of the same website snapshot through the public API produced:

- 128 distinct nodes, 83 declarations, 280 edges;
- `contains` 83, `declares` 83, `imports` 9, `inherits` 1,
  `invokes-name` 101, `resolves-to` 3;
- `src/lib/schema.ts → src/lib/site.ts` (`./site`),
  `src/lib/schema.ts → src/lib/paths.ts` (`./paths`),
  `src/pages/robots.txt.ts → src/lib/paths.ts` (`../lib/paths`);
- provider `0.2.0-preview.6+resolver2.relative-v1` on every resolver edge;
- no `exports`/`binds` facts.

## H. Failed attempts and corrections during remediation

- The first F03 helper only recognized the direct `type` keyword. The
  `export type *` and `export type * as ns` forms appear through error
  recovery (the keyword is inside an `ERROR` node), so the classification
  regressed to `export-star`/`export-namespace-from`. The permanent
  positive-control theory caught it; the helper now recognizes the
  `ERROR(type) + *` and `ERROR(type) + namespace_export` shapes.
- The first F01 test asserted the end column without the exclusive-end +1
  convention and failed by one. The product was correct; the test expectation
  was fixed (no product change).
- The migration probe is intentionally one-shot: a second run against an
  already-migrated database fails its "latest publication is the p6 seed"
  precondition. The clean sequence (seed → single migration run) was repeated
  and both steps passed.

## I. Remaining limitations and qualifications

- `relative-v1` still rejects escape sequences rather than decoding them; a
  specifier that is semantically valid only after decoding stays unresolved
  by policy. This is the conservative behavior the review asked for.
- The pinned grammar cannot represent a few pathological `type`/`as`
  combinations (`export { type as }`, `import { type as as as }`). Those sites
  are classified conservatively (not type-only); the module edge itself is
  unaffected. The exact TypeScript reading is documented as a grammar
  limitation, not claimed.
- The structural `imports` placeholder for an escaped specifier still uses
  the pre-existing lexical observation (first fragment). This is unchanged
  `Structural` behavior, not a resolver fact; changing it would alter
  existing structural identities and is outside this remediation.
- No TypeScript compiler, bundler, Node.js, or runtime equivalence is
  claimed. Cross-plugin and non-ECMAScript resolution remain out of scope.
- No `.p7`-qualified publication exists; the p8 bump is the D-16-mandated
  invalidation, not a new policy level.
- Behavior is verified on Windows with the pinned grammars and the tested
  fixtures only; no other platform or large-scale resource measurement is
  claimed.

## J. Independent re-review scope

The reviewer should independently verify, in an isolated worktree from
`b915aed` (or the documentation commit):

1. F02: escaped specifiers (`\b`, `\\`, `\uXXXX`, `\t`, escaped quotes) never
   produce a `resolves-to` edge; unescaped relative resolution is unchanged.
2. F03: `import { type }`, `import { type as t }`, `import type from`, and
   `export { type as u }` carry no `type-only`; genuine statement/inline
   type-only forms still do; mixed and JavaScript behavior.
3. F01: published columns equal UTF-16 code units for BMP and supplementary
   characters; the contract and consumer docs say so; no identity migration
   occurred.
4. F04: `export = require('./m')` behavior and its documentation.
5. Preservation: authentic p6→remediated migration still yields
   128 nodes, 83 declarations, 277 structural edges, and only three added
   `resolves-to` edges; both plugins re-execute.
6. Invalidation: plugin versions end in `.p8`, evidence carries
   `resolver2.relative-v1`, and unchanged reindex remains a no-op.
7. No `exports`/`binds`, no impact-preset change, no evidence-quality change
   for pre-existing facts.

Results must be recorded on a preserved review branch. **EC-6B remains
unaccepted and open until that independent re-review and owner acceptance.**
