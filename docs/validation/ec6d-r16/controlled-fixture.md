# EC6D-R16 controlled corpus: current behavior and expected EC-6D result

Corpus: `docs/planning/ec6d/corpus/ts-interface-impact` (committed with the
EC-6D planning evidence; 13 TypeScript files plus README). Selected
declaration: `src/contracts/Renderable.ts` / `Renderable`. Manually reviewed
expected direct implementers (fixture README): `SvgRenderer`, `HtmlRenderer`,
`AbstractRenderer`, `AliasRenderer`.

Everything in the "current" sections was produced by the existing
`ConsumerBaseline` harness on the R16 baseline
([baseline.md](baseline.md)); everything under "expected EC-6D" is a
**contract-derived illustration, not a runtime result**.

## 1. Consumer scenario

A developer needs to change the `Renderable` interface. Before modifying it
they want to know which directly implementing classes require attention and
which modules might have a relevant dependency, with evidence for each
result — and they must be able to distinguish confirmed implementation
bindings from possible consumer modules and unknown relationships.

## 2. Current Andirit behavior (executed)

Command (from the repository root):

~~~powershell
dotnet run --no-build --project docs/planning/ec6d/analysis/ConsumerBaseline.csproj -- docs/planning/ec6d/corpus/ts-interface-impact
~~~

### 2.1 Indexing diagnostics

| Measure | Value |
| --- | ---: |
| Files discovered / unsupported / units completed | 14 / 1 / 14 |
| Node contributions / distinct nodes | 73 / 64 |
| Declarations | 23 |
| Edge contributions / distinct edges | 99 / 99 |
| Unresolved relationships | 1 (aggregate counter; no queryable node) |
| Warnings | 0 |
| Plugin | `penghou.hetu.tree-sitter.typescript` `0.2.0-preview.6+v0.23.2.abi14.p9` |
| Publication consistent | `false` (file-system provider; snapshot identity null) |

Edge counts by kind (identical to the committed baseline): `contains` 23,
`declares` 23, `exports` 13, `implements` 5, `imports` 17, `inherits` 1,
`invokes-name` 1, `resolves-to` 16.

### 2.2 Selected declaration identity

| Item | Value |
| --- | --- |
| Node | `typescript:symbol:b51a8ed921fc9cb1fb535663471cbddd30f86c54397ef59d7c24847cd35e37c4` (`interface`, name `Renderable`) |
| Symbol | `typescript:b51a8ed921fc9cb1fb535663471cbddd30f86c54397ef59d7c24847cd35e37c4` |
| Physical declaration record | `typescript:6eec129b5b77943d1b7f65f8c12f830680b313ae6d3c933530ec78487ed79abc` (one record; `src/contracts/Renderable.ts` 1:8..3:2) |
| Export entry | `typescript:export:95c71b96b353ec39cd9a1d80d28340b24764ad4247ece88ffce6ae71c6e9a218`; occurrence edge `typescript:e5d6c1d0f4b1cf48607d0f7695677f090218fe16525fbaed33bb7c3f8311e2dd` at 1:18..1:28 |
| Contributor | TypeScript plugin unit for `src/contracts/Renderable.ts`, source hash `sha256:df54f92076160c3e98c93df24ac21fc224bab2791edbe5d8b90d90795b0db2dd` |

### 2.3 Known declaration candidates for the name `Renderable`

Exact qualified-name lookup returns eight candidates; the consumer must
supply the file/declaration identity to disambiguate:

| Kind | Node | Meaning |
| --- | --- | --- |
| interface | `typescript:symbol:b51a8ed9…` | the selected declaration (`src/contracts/Renderable.ts`) |
| interface | `typescript:symbol:2d78be0d…` | same-name interface in `src/other/Renderable.ts` |
| export | `typescript:export:95c71b96…` | export entry of the selected declaration |
| export | `typescript:export:3fd6d9b7…` | export entry of `src/other/Renderable.ts` |
| type | `typescript:heritage-target:6a5bc493…` | written `Renderable` in `SvgRenderer.ts` |
| type | `typescript:heritage-target:d0bba5cb…` | written `Renderable` in `HtmlRenderer.ts` |
| type | `typescript:heritage-target:0c190ced…` | written `Renderable` in `AbstractRenderer.ts` |
| type | `typescript:heritage-target:b8583381…` | written `Renderable` in `OtherRenderer.ts` |

### 2.4 Direct structural heritage relationships (stored facts)

Five `implements` edges and one `inherits` edge exist; heritage evidence
carries the fact's own `…abi14.p8` version (the p9 session origin is
deliberately not substituted):

| Class (node) | Clause span | Heritage edge | Placeholder target |
| --- | --- | --- | --- |
| `SvgRenderer` (`typescript:symbol:5a18e743…`) | 4:37..4:47 | `typescript:16277dd5…` | `typescript:heritage-target:6a5bc493…` |
| `HtmlRenderer` (`typescript:symbol:546fc6da…`) | 4:38..4:48 | `typescript:7b32b1cb…` | `typescript:heritage-target:d0bba5cb…` |
| `AbstractRenderer` (`typescript:symbol:c1ff91cb…`) | 4:51..4:61 | `typescript:8817b9c7…` | `typescript:heritage-target:0c190ced…` |
| `AliasRenderer` (`typescript:symbol:f4062fe2…`) | 4:39..4:47 (`Drawable`) | `typescript:52bb45e4…` | `typescript:heritage-target:1d9edae1…` |
| `OtherRenderer` (`typescript:symbol:2bee5…`) | 3:39..3:49 | (negative case) | `typescript:heritage-target:b8583381…` |
| `CanvasRenderer` → `BaseRenderer` (`inherits`) | 3:37..3:49 | `typescript:a2aed451…` | `typescript:heritage-target:8c1d6edc…` |

These facts are **syntactic**: the placeholder targets are declaration-less
nodes scoped to the writing file; none of them is bound to the selected
interface.

### 2.5 Resolved module-file relationships (stored facts)

Six `resolves-to` edges target `src/contracts/Renderable.ts`; all use
`relative-v1` policy, rule `extension-substitution`, provider
`0.2.0-preview.6+resolver2.relative-v1`, quality `Resolved`
(file-to-file facts; they do not prove which symbol was imported or used):

| Importing file | Edge | Specifier |
| --- | --- | --- |
| `src/renderers/SvgRenderer.ts` | `typescript:d1428c0f…` | `../contracts/Renderable` |
| `src/renderers/HtmlRenderer.ts` | `typescript:ece4befc…` | `../contracts/Renderable` |
| `src/renderers/AbstractRenderer.ts` | `typescript:dbc9e9b7…` | `../contracts/Renderable` |
| `src/renderers/AliasRenderer.ts` | `typescript:0ddd64da…` | `../contracts/Renderable` |
| `src/apps/Preview.ts` | `typescript:e86a6ce8…` | `../contracts/Renderable` |
| `src/api/index.ts` | `typescript:6fa7ab24…` | `../contracts/Renderable` (export-side `export-from`) |

Other relevant resolution facts: `BarrelPreview.ts` → `src/api/index.ts`
(rule `directory-index`), `MissingImport.ts` → unresolved (no
`resolves-to`; only the module node
`typescript:import:dc3e727f7652dca0aac417bab39a2663a140081018a41e052b917b4058e2c824`).

### 2.6 Export occurrences on the target module

Two export entries with `Structural` occurrence edges (provider
`0.2.0-preview.6+exports1.local-v1`): `Renderable` (1:18..1:28) and
`RenderContext` (5:18..5:31). The re-export barrel `src/api/index.ts`
writes three `export { … } from` statements; it publishes **0** export
edges (re-exports remain outside the accepted EC-6C export surface).

### 2.7 Current implementation-query behavior

| Query | Current result |
| --- | --- |
| `FindImplementationsAsync` on the selected interface node | seed only (1 node); **0/4** expected implementers |
| Same on the target's export entry | seed only |
| Same on the four `Renderable` heritage placeholders | returns the name-matched classes: `SvgRenderer`, `HtmlRenderer`, `AbstractRenderer`, and `OtherRenderer` (false positive, different declaration); `AliasRenderer` (writes `Drawable`) is missed |
| Same on the `Drawable` placeholder | returns `AliasRenderer`, but with no declaration binding |
| `GetImpactSetAsync` on the selected interface node | seed only |
| Named-import binding | none exists (imports edges carry the module specifier only; no imported-name facts) |
| Export-to-declaration attribution | none exists |
| Inherited impact (`CanvasRenderer` via `SvgRenderer`) | not computed; transitive impact is outside every current query |

### 2.8 Missing symbol-binding evidence

The accepted EC-6D design requires facts A (import occurrences), C
(heritage contexts), E (export-side references) and B (export
attribution). None exist today: no imported/local names, no per-specifier
occurrence identity, no heritage context records (lexical scope, ambient
flags), no export-reference records (the barrel's `export … from`
statements appear only as resolver module references), and no attribution
records. The unresolved `MissingImport` target remains an aggregate
counter without a queryable per-import outcome.

### 2.9 Determinism and comparison with the committed baseline

Two consecutive runs in this checkout produced identical `IndexIdentity`
and `CanonicalFactsSha256` (`5c866e16…` / `631c0ab3…`). The committed
baseline's recorded digests (`667ec2d4…` / `05bf7c7d…`) are known to be
checkout-environment-dependent (file-node content hashes cover
working-tree bytes; `core.autocrlf` state differs — see
`consumer-baseline.md`, R1 reproduction note). The comparable surface is
**graph identity**: all 64 distinct node ids and all 99 distinct edge ids
matched the committed `observed-results.json` exactly (0 differences), as
did every relationship count and both `FindImplementations` outcomes.

## 3. Expected EC-6D result (contract illustration, not executed)

The following is an **expected response illustration** derived from the
accepted contracts (`consumer-query-contract.md` §3–§6,
`symbol-binding-contract.md` §3-§5, `publication-consistency-contract.md`)
applied to the stored facts above. It is not produced by any implementation
and must not be cited as observed output.

Identifier notation in the illustration:

- **`[stored]`** — the identifier exists in the current publication and is
  cited exactly as recorded;
- **`[new A]` / `[new C]` / `[new B]` / `[new E]`** — records introduced by
  the accepted EC-6D extraction design; no identifier or hash exists before
  implementation, so none is invented;
- witness identity — computed at query time over stored fact ids
  (`binding1.local-v1`); shown as an expression, never as a literal hash.

### 3.1 Illustration (abridged; all ten mandated elements included)

```text
InterfaceImpactResultV1

publication:
  repositoryId            repo:ec6d-ts-interface-impact-fixture
  indexRunId              run:ec6d-baseline-01
  indexIdentity           [stored] 5c866e16796b6784327a5366b93ba5926e44abdad10841b2e04533ee6a63f5eb
  snapshotIdentity        null (file-system provider declaration)
  isConsistentSnapshot    false
  qualification           qualified        // truth table: false/null => qualified

target:
  declarationNodeId       [stored] typescript:symbol:b51a8ed921fc9cb1fb535663471cbddd30f86c54397ef59d7c24847cd35e37c4
  declarationRecordId     [stored] typescript:6eec129b5b77943d1b7f65f8c12f830680b313ae6d3c933530ec78487ed79abc
  name                    Renderable
  qualifiedName           Renderable
  path                    src/contracts/Renderable.ts
  span                    1:8..3:2

confirmedImplementations[]                         // exactly the four expected classes

  - classNodeId           [stored] typescript:symbol:5a18e74387339f41e01474b965e34a9bd3f80ff0c3cca156dd7c83e49e234a8b
    className             SvgRenderer
    path                  src/renderers/SvgRenderer.ts
    heritageSpan          [stored] 4:37..4:47
    bindingRoute          import
    targetDeclarationRecordId [stored] (same as target.declarationRecordId)
    witnesses[1]:
      witnessId           "binding1.local-v1:" + sha256(ordered fact ids + policy)   // computed at query time
      steps:
        1 heritage        [stored] edge typescript:16277dd5e943d5722f422a5e201d52f53a9fc8f46b38c826b5da1fca2def68d5
                          owner typescript:symbol:5a18e743… (class), 4:37..4:47,
                          provider penghou.hetu.tree-sitter.typescript, Structural,
                          evidence version 0.2.0-preview.6+v0.23.2.abi14.p8
        2 heritage context [new C] heritage1.local-v1: class-implements, simple,
                          module scope, non-ambient, module file context, well-formed
        3 import occurrence [new A] imports1.local-v1: "../contracts/Renderable",
                          imported Renderable, local Renderable, type-only,
                          statement span [stored imports-edge span] 1:1..1:59
        4 module resolution [stored] edge typescript:d1428c0f7ee022586e1b1025e9b612da1eac3ec4ccb7a07bd6938262fd0092b0
                          rule extension-substitution, policy relative-v1,
                          provider …+resolver2.relative-v1, Resolved
        5 export entry     [stored] entry typescript:export:95c71b96…;
                          occurrence edge typescript:e5d6c1d0… at 1:18..1:28,
                          provider …+exports1.local-v1, Structural
        6 attribution      [new B] exports2.local-v1: status attributed;
                          declaration record [stored] 6eec129b… ; node [stored] b51a8ed9…
        7 witness identity computed at query time (no stored literal)

  - HtmlRenderer   classNodeId [stored] typescript:symbol:546fc6da…; heritageSpan 4:38..4:48;
                   heritage edge typescript:7b32b1cb…; import span 1:1..1:59;
                   resolver edge typescript:ece4befc…; same export/attribution steps
  - AbstractRenderer classNodeId [stored] typescript:symbol:c1ff91cb…; heritageSpan 4:51..4:61;
                   heritage edge typescript:8817b9c7…; import span 1:1..1:59;
                   resolver edge typescript:dbc9e9b7…; same export/attribution steps
  - AliasRenderer  classNodeId [stored] typescript:symbol:f4062fe2…; heritageSpan 4:39..4:47 (Drawable);
                   heritage edge typescript:52bb45e4…; import occurrence [new A]
                   "../contracts/Renderable", imported Renderable, local Drawable,
                   type-only, statement span [stored] 1:1..1:71;
                   resolver edge typescript:0ddd64da…; same export/attribution steps;
                   witness step 3 shows localName -> importedName -> entry alias chain

possibleConsumers[]                                // module-level potential dependency only

  - fileNodeId [stored] typescript:file:86e58e91…  path src/renderers/SvgRenderer.ts
      occurrences: [new A] "../contracts/Renderable", imported Renderable,
                   local Renderable, type-only, statement span [stored] 1:1..1:59,
                   resolution rule extension-substitution
  - fileNodeId [stored] typescript:file:84914a70…  path src/renderers/HtmlRenderer.ts
      occurrences: … statement span [stored] 1:1..1:59 …
  - fileNodeId [stored] typescript:file:14938e4b…  path src/renderers/AbstractRenderer.ts
      occurrences: … statement span [stored] 1:1..1:59 …
  - fileNodeId [stored] typescript:file:f30a2868…  path src/renderers/AliasRenderer.ts
      occurrences: … imported Renderable, local Drawable, span [stored] 1:1..1:71 …
  - fileNodeId [stored] typescript:file:b6be2b3d…  path src/apps/Preview.ts
      occurrences: … statement span [stored] 1:1..1:59 …
    // A confirmed class's file may also be a possible consumer (mutual exclusion
    // applies to the same evaluated relationship, not across categories).

unknowns[]                                         // explicit, never confirmed

  - subject: occurrence, file src/apps/BarrelPreview.ts [stored] typescript:file:5019b480…
    reason: re-export-barrier
    evidence: [new A] import of Drawable from "../api" (statement span [stored] 1:1..1:40),
              resolved by [stored] edge typescript:ca5f3b2e… to src/api/index.ts (rule directory-index)
    witnesses: [new E] the barrel's `export { Renderable as Drawable } from "../contracts/Renderable"`
               record — separate list, never first-wins
  - subject: occurrence, file src/apps/MissingImport.ts [stored] typescript:file:749aded9…
    reason: unresolved-module
    evidence: [new A] import of Renderable from "../contracts/missing"
              (statement span [stored] 1:1..1:56); no resolution edge exists

coverage:
  profile qualification   qualified
  partial                 false            // no truncation
  counters                evaluated/unsupported/external/unresolved import occurrences;
                          heritage observed/evaluated; export references observed;
                          confirmedImplementationCount 4; possibleConsumerModuleCount 5;
                          unknownCountsByReason { re-export-barrier: 1, unresolved-module: 1 }
  notEvaluated[]          renamed/multi-hop re-export paths, CommonJS/require, bare packages,
                          ordinary type-use references, transitive impact (CanvasRenderer's
                          inheritance), default/namespace binding forms

truncation:
  state none; chargedWork below defaults; counts exact

fixed coverage statement: "This result covers direct explicit class `implements`
bindings with simple identifiers at module scope, direct named relative imports
under the documented EC-6D profile, and one bounded hop of resolved re-export
alias association for candidate relevance and barrier reporting. Publication
qualification: qualified. It does not evaluate: … ordinary type-use references,
call sites, implicit structural implementations, transitive inheritance …
Absence from the confirmed list is not evidence of absence of impact."
```

### 3.2 Negative expectations (contract-pinned, unchanged)

| Case | Expected classification |
| --- | --- |
| `OtherRenderer` | not a confirmed implementer of the selected declaration: its chain completes to the different same-name declaration (`src/other/Renderable.ts`), so it is omitted from this target's result (neither confirmed nor unknown) |
| `CanvasRenderer` | not a direct implementer; inheritance propagation (via `SvgRenderer`) is outside this query; no confirmation, and no unknown is fabricated for it |
| `MissingImport` | no confirmed binding; occurrence-level `unresolved-module` unknown |
| `BarrelPreview` | not a confirmed binding; occurrence-level `re-export-barrier` unknown; the barrel's re-export records appear as separate witnesses, never as a binding |
| Ambiguous/merged declaration | explicit ambiguity, never arbitrary confirmation: a merged/ambiguous target runs with `declarationRecordId = null` and its import chains return `ambiguous-export` with all contributor record ids (A24). The fixture contains no merged declaration; this rule is pinned by the Q2 `merged` corpus and A24 |
| Unsupported syntax | explicit unsupported/unknown result where the contract requires one (`qualified-heritage-unresolved`, `unsupported-heritage-form`, `heritage-kind-not-supported`, `unsupported-import-form`, etc.; reserved reasons are non-emittable). The fixture contains only supported simple heritage; these rules are pinned by Q2/r2 fixtures and A12/A21/A25/A26 |

These expectations are copied from the accepted design and were **not**
rewritten to match current implementation behavior.

### 3.3 Scope of the resulting impact claim

The result answers exactly one bounded question: direct explicit simple
`implements` bindings to the selected unique declaration, plus direct named
relative importers as possible consumers, inside the declared profile. It
does not claim whole-program, compiler, bundler or runtime impact; the two
`preview` functions in `Preview.ts` and `BarrelPreview.ts` are ordinary
type-annotated references whose impact is deliberately not evaluated, and
`CanvasRenderer` may be affected transitively without appearing anywhere in
the result.
