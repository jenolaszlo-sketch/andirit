# EC6B-GLM-Q2: Independent verification of EC-6B-R1

- **Review ID:** `EC6B-GLM-Q2`
- **Reviewer:** GLM
- **Review type:** independent re-review of the EC-6B-R1 remediation under
  owner decision `EC6B-OA1`
- **Milestone:** EC-6B, Conservative Relative Module Resolution
- **Date:** 25 October 2026
- **Reviewed source:** EC-6B-R1 at `f8fb9d1c2917ef04fdf9bca721bcbeaaecb959e9`
  (tested implementation tree `b915aed`)
- **Verdict:** **VERIFIED WITH QUALIFICATIONS** — no P0/P1/P2 findings; the
  documented and owner-accepted limitations remain. EC-6B is eligible for
  owner closure reconciliation; integration is not authorized by this review.

This review was performed in an isolated worktree. No production source,
implementation test, historical review, decision document, or canonical
branch was modified. The Q1 report and the R1 evidence were read but not
altered.

---

## A. Git binding

| Item | Verified identity |
| --- | --- |
| Repository | `jenolaszlo-sketch/penghou-hetu` (`origin` verified) |
| Canonical `main` | `558f8ac82ac1b5e07da68f4a71a70240eaf38409`, clean |
| Accepted pre-implementation baseline | `1d11b7330e1d8755d5c3e3b4a830167bcf8b5bf6` |
| EC-6B implementation | `8aa083ce36e52f666578f5ed6b539ec194178561` (tree `52f877a`) |
| EC-6B handoff | `691a8327ab7446416de22fe26e023662399b6cf2` (tree `f2bb8c5`) |
| Q1 independent review | `66690587f4507d47e78fa4e1996e050b830c1642` (tree `fc2bedc`, parent `691a832`) |
| R1 implementation correction | `ffdcdbdf57e35f94a1e459a5b393649778c21f69` (tree `4f3f27c`, parent `691a832`) |
| R1 regression test | `b915aed2a795f38d682fcd8289282eecd8110666` (tree `559ec7b`, parent `ffdcdbd`) |
| R1 documentation/evidence | `f8fb9d1c2917ef04fdf9bca721bcbeaaecb959e9` (tree `22cc536`, parent `b915aed`) |
| **Exact reviewed source** | `f8fb9d1c2917ef04fdf9bca721bcbeaaecb959e9`; `git diff b915aed..f8fb9d1` contains **documentation files only**, so the tested source tree is `b915aed` |
| Review worktree / branch | `C:\Users\Laszlos\source\repos\Penghou.Hetu.review-ec6b-glm-q2` · `review/ec6b-glm-q2`, created from `f8fb9d1` |
| Review preservation commit | this commit; parent `f8fb9d1`. Its hash and tree are recorded in the completion message because this report is part of that commit |
| Environment | Windows 10.0.26200 (win-x64); .NET SDK 10.0.401; `net10.0` |

Ancestry verified: `558f8ac` → `1d11b73` → `8aa083c` → `691a832` → `ffdcdbd`
→ `b915aed` → `f8fb9d1`. No pull, rebase, merge, reset, or push was
performed.

## B. Mandate and method

Decision `EC6B-OA1` accepts EC-6B in principle and authorizes this re-review
against the exact R1 source, with gates Q2-A through Q2-H. The review used
source inspection, independent adversarial probes committed on the review
branch, the implementation suite, an authentic p6→p8 migration, and a new
authentic p7→p8 stale-fact experiment.

Historical implementations were executed only from throwaway clones:
p6 = `1d11b73` (real p6 extractor), p7 = `691a832` (real pre-remediation
extractor). The p7 fixture is a synthetic repository containing the confirmed
escaped-specifier counterexample; the website snapshot contains no escaped
specifiers, so the p7→p8 demonstration necessarily uses a focused fixture.

## C. Q2-A / Q2-B: finding closure and false-resolution elimination

### F01 — UTF-16 coordinate contract

- Traced end-to-end: the pinned `TreeSitter.DotNet` binding parses the decoded
  .NET string as UTF-16LE; `EcmaScriptExtractionProfile.Location` publishes
  one-based UTF-16 code-unit columns with exclusive ends; edge identities hash
  those values; persistence stores integers; the only position consumer
  (`FindEnclosingSymbolAsync`) compares line/column in the same system.
- Independent probe: a line containing BMP CJK, accented Latin, and a
  supplementary emoji before the import published the UTF-16 column and not
  the UTF-8 byte column, survived publication on both stores, survived durable
  reopen, and produced the expected identity when recomputed.
- Consumer probe: `FindEnclosingSymbolAsync` found the declaration at the
  documented UTF-16 column and returned `null` at the equivalent UTF-8 byte
  column.
- R1 corrected the contract, consumer doc, identity doc, source comment, and
  handoff wording; no coordinate conversion or identity migration occurred.
- **Result: closed. No finding.**

### F02 — escaped specifiers

- Source inspection: `RawModuleSpecifier` rejects any string literal with an
  `escape_sequence` child before policy evaluation and reads the complete
  literal text otherwise; it never reads a single fragment or decodes escapes.
- Independent extraction probe: imports using `\b`, `\\`, `\u00e9`, an escaped
  quote, and `\t`, plus an escaped `export … from` and an escaped `require`
  argument, produced **no** `resolves-to` edge and counted unsupported; the
  unescaped `./plain` and `./a.ts` resolved normally. No resolved specifier
  contained a backslash.
- Publication probe (both stores): a repository whose only references are
  escaped specifiers completed indexing with no resolver edge and a real
  target node present; `UnresolvedRelationships` stayed 0.
- **Result: closed. No finding.**

### F03 — type-only classification

- Source inspection: statement modifiers are detected from the anonymous
  `type` token; inline modifiers require the keyword followed by a binding
  name; the grammar's error-recovery shapes for `export type *` and
  `export type * as ns` are recognized; the three-token `type as as as` form
  follows TypeScript's parser rule.
- Independent matrix (18 cases) confirmed: `{ type }`, `{ type as t }`,
  `import type from`, and `export { type as u }` carry **no** `type-only`;
  statement-level, inline, aliased, star, namespace, and string-name type-only
  forms all carry `type-only: true`; mixed lists are not wholly type-only;
  JavaScript bindings named `type` stay ordinary.
- **Result: closed. No finding.**

### F04 — nested `require` in `export =`

- Independently reproduced: `export = require('./m')` yields one unsupported
  outer site plus exactly one `resolves-to` edge attributed to the `require`
  call (`module-reference: require`), with no `exports`/`binds` facts;
  `import x = require('./m')` and `import type X = require('./m')` capture no
  nested call.
- The behavior matches the accepted "literal `require` (top level or nested)"
  rule and is documented in the consumer doc and pinned by a permanent test.
- **Result: closed as a documented clarification. No finding.**

## D. Q2-C: type-only regression check

Beyond the F03 matrix, the review confirmed the p6→p8 migration and the
website graph still classify only explicitly type-only written forms; no
previously correct `type-only` property disappeared, and no value import or
export gained the property. The pathologically ambiguous forms listed in
§M remain conservative and produce no false resolver fact.

## E. Q2-D: graph compatibility

- Structural `imports` edges remain `Structural` with unchanged placeholder
  behavior; no `exports`, `binds`, or module nodes exist.
- `GetImpactSetAsync` and `FindDependentsAsync` do not traverse or include
  `resolves-to`; the impact preset file is unchanged from `691a832`.
- The p6→p8 migration preserved every p6 node, declaration, and structural
  edge identity byte-for-byte (0 missing lines); the three resolver edge ids
  are identical to those published by the previously reviewed p7
  implementation.
- File containment, exact-name lookup, declaration queries, and durable
  persistence were exercised indirectly by the migration, the website
  qualification, and the implementation suite (408 total cases green).

## F. Q2-E: invalidation and authentic p7→p8 stale-fact removal

A throwaway clone at `691a832` (real p7: plugin
`0.2.0-preview.6+v0.23.2.abi14.p7`, resolver
`0.2.0-preview.6+resolver1.relative-v1`) seeded a durable
`LatticeCodeGraphStore` from a focused repository whose `page.ts` contains:

```typescript
import './a\b';        // written text contains an escape
import './plain';
import { type } from './m';
```

Authentic p7 published the defects exactly as reported:

| Edge | Specifier | Target | Type-only |
| --- | --- | --- | --- |
| `typescript:308fbc7b…f2af` | `./a` (false) | `src/a.ts` | false |
| `typescript:57507f19…b5ff` | `./plain` (correct) | `src/plain.ts` | false |
| `typescript:6e069204…e395` | `./m` (binding named `type`) | `src/m.ts` | **true** (false) |

Reopening that database with the remediated p8 implementation and reindexing
the unchanged sources produced:

- status `Completed`, `PluginsExecuted = 1` (the p7 publication was
  invalidated by the version change);
- the false `./a` edge and `src/a.ts` target were **removed**;
- `./plain` survived with the **same edge id**;
- `./m` survived with the **same edge id** but without the false `type-only`
  property, with provider `+resolver2.relative-v1`;
- **zero** non-resolver p7 lines were lost (structural identities and
  declarations intact);
- after closing and reopening the durable database, the corrected facts
  (`./plain`, `./m`) remained visible under p8.

**Result: the owner's five required invalidation demonstrations all hold.**

## G. Q2-F: authentic p6→p8 preservation

A throwaway clone at `1d11b73` (real p6 extractor) seeded a durable LatticeDb
from the isolated website snapshot
`jenolaszlo-sketch/website@f9cca1773a32bb517e624cc380e1f6d14e0cc317`
(all nine indexed blob SHAs independently verified). The R1 source then
reindexed the unchanged sources.

| Measure | p6 | p8 (R1) |
| --- | --- | --- |
| Units | 9 | 11 |
| Node lines / distinct node ids | 130 / 128 | 130 / 128 |
| Declarations | 83 | 83 |
| Edges | 277 | 280 |
| imports / inherits / invokes-name / contains / declares | 9 / 1 / 101 / 83 / 83 | identical |
| Missing p6 lines | — | **0** |
| Added lines | — | **3 `resolves-to` edges only** |

The three added edge ids (`3ddbda1c…`, `6e6cb958…`, `a9764b44…`) match the p7
publication verified in Q1, confirming that the remediation did not move any
unaffected resolver identity. Both plugins executed during the transition.

## H. Q2-G: publication and failure behavior

Independent probes on both `InMemoryCodeGraphStore` and
`LatticeCodeGraphStore` confirmed:

- target addition publishes the edge with the target node; target deletion
  removes the resolver edge, leaves the structural `imports` observation, and
  leaves no dangling endpoint;
- an empty recomputation publishes an empty `typescript:modules` unit;
- cancellation retained the exact previous resolver-edge ids;
- a module unit that exceeds its ingestion limit mid-unit failed the run and
  retained the previous publication (no partial facts);
- staging the resolution unit before its endpoint file units is rejected with
  `graph.edge.endpoint-missing`;
- corrected facts survive durable close/reopen.

## I. Q2-H: evidence preservation

- The Q1 report, its probes, and the R1 branches are unchanged.
- This review's probes are committed only on `review/ec6b-glm-q2`.
- All temporary clones, databases, and snapshots were deleted after execution;
  the evidence in this report is bound to the commit identities in §A.

## J. Verification inventory and results

| Check | Command / scope | Result |
| --- | --- | --- |
| Full solution | `dotnet test Penghou.Hetu.slnx -m:1 --nologo` | **408 passed, 0 failed** (C# 46, LatticeDb 26, core 160, Tree-sitter 176) |
| Implementation-only | same suite excluding review probes | **363 passed, 0 failed** |
| Focused module resolution | `--filter FullyQualifiedName~TreeSitterModuleResolution` | **41 passed, 0 failed** |
| Review probes | three Q2 probe classes | **45 cases passed** (32 closure/compatibility + 10 lifecycle + 3 historical) |
| Formatter | `dotnet format Penghou.Hetu.slnx --verify-no-changes --no-restore` | exit 0 |
| Whitespace | `git diff --cached --check` | clean |
| p7→p8 stale-fact removal | authentic p7 seed + p8 reindex | passed (see §F) |
| p6→p8 preservation | authentic p6 seed + p8 reindex | passed (see §G) |
| Website ground truth | fresh indexed snapshot | 128 nodes / 83 declarations / 280 edges; exactly the three expected edges |

Review-process corrections (not product findings): the first compatibility
probe expected `FindDependentsAsync` to exclude the seed node (it includes it
at depth 0), and the nested-require fixture candidate count initially omitted
the outer `export = item` site. Both were probe-authoring errors, corrected
before the final records.

## K. Findings

No P0, P1, or P2 findings. No unresolved defect permits false resolution,
stale graph facts, or publication inconsistency.

### Observations (non-blocking, P3)

- **Q2-O1 (documented limitation):** the pinned grammar's lossy reading of
  pathological `type`/`as` forms (`export { type as }`,
  `export { type as as }`, `import { type as as as }`) is handled
  conservatively: the edge is still emitted, the `type-only` property is
  absent, and no false resolver fact is produced. This is explicitly covered
  by the owner-accepted limitation for unusual grammar forms.
- **Q2-O2 (documented limitation):** the structural `imports` placeholder for
  an escaped specifier retains its pre-existing first-fragment text. It is a
  `Structural` observation, not a resolver fact, and the owner accepted this
  behavior in `EC6B-OA1` §9.
- **Q2-O3 (design ambiguity, pre-existing):** `export type * as ns from './m'`
  is labeled `module-reference = export-type-from` rather than
  `export-namespace-from`. The `type-only` flag is correct and the target is
  correct; only the combined-form label is under-specified. Recording it for
  EC-6C documentation does not block EC-6B.
- **Q2-O4 (documentation nit):** the accepted architecture §8 still uses
  `0.2.0-preview.6+resolver1.relative-v1` as an illustrative provider-version
  example. It is not a current-value claim; the consumer doc and identity doc
  state `resolver2.relative-v1`. Optional touch-up in the owner closure record.

## L. Qualification boundaries

Established: F01–F04 closure, false-resolution elimination, type-only
classification, graph compatibility, p7→p8 invalidation with stale-edge
removal, authentic p6→p8 preservation, publication/failure behavior,
determinism (reversed enumeration and LF/CRLF), and website ground truth on
Windows with .NET SDK 10.0.401 and the pinned grammars.

Not claimed: compiler/bundler/Node.js/runtime equivalence; cross-plugin or
non-ECMAScript resolution; import/export binding; large-scale performance;
platforms other than the exercised Windows environment; behavior of forms
outside the accepted policy.

## M. Verdict and closure recommendation

**EC6B-GLM-Q2: VERIFIED WITH QUALIFICATIONS.**

The R1 remediation is correct within the demonstrated scope. Every material
Q1 finding is closed with executable evidence, including the previously
missing demonstration that authentic p7 false facts are invalidated and
removed by p8 while preserving unrelated identities. The remaining
qualifications are the owner-accepted coverage limitations and the
non-blocking observations in §K.

Closure-gate assessment against `EC6B-OA1` §10:

1. Independent Q2 review completed and preserved — **met by this commit**.
2. F01–F04 verified or explicitly reconciled — **met**.
3. No unresolved P0/P1 — **met**.
4. No unresolved P2 permitting false resolution, stale facts, or publication
   inconsistency — **met**.
5. Version invalidation qualified — **met** (authentic p7→p8).
6. Existing graph identities preserved — **met**.
7. Scope and unsupported behavior documented — **met** (minor Q2-O4 nit).
8. Independent findings reconciled into an owner closure record — **owner
   action pending**.
9. Production integration separately authorized — **not authorized here**.

EC-6B may therefore proceed to owner closure reconciliation. Integration of
the exact reviewed tree (`b915aed` source, `f8fb9d1` docs) remains a separate
owner authorization.

## N. Review preservation

- Review branch: `review/ec6b-glm-q2`
- Review worktree: `C:\Users\Laszlos\source\repos\Penghou.Hetu.review-ec6b-glm-q2`
- Q2 probe files: `ReviewEc6bGlmQ2Probes.cs`,
  `ReviewEc6bGlmQ2LifecycleProbe.cs`, `ReviewEc6bGlmQ2HistoricalProbe.cs`
- Preservation commit: this commit (parent `f8fb9d1`); full hash and tree are
  recorded in the completion message.
