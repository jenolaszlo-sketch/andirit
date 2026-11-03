# Canonical identity review (Q2-F01)

## Defect

R1's oracle described the same source occurrence through two incompatible target
pathways:

- `RunIdentities` (main view) declared fixture targets with parameter-count
  shapes (`T.Direct` = `method|1`) and derived `usage4` from them.
- The lifecycle projection `AppFacts` re-derived the target from Roslyn using
  `method.OriginalDefinition.GetDocumentationCommentId()` and the generic
  `method.Arity` (`method|0` for non-generic methods), and skipped
  `ReducedFrom` normalization for reduced extension methods.

An independent probe (`q2-f01-preflight.json`, 6 designated sites) reproduced
the divergence exactly as the Q2 review recorded it: S01
`d1ae71fc315d0c1a5b41559e4ba7a6b10c9dc417eb3a52b9cd0781fb67b0b556` (main) vs
`60e2b717a50fa54c27757a900d53310ad2f47c20884a91952eb8e207316edef2` (lifecycle);
INV-01 `699a94f4…` vs `cb13f965…`; first divergent field `asmctx3` (shape
`method|1` vs `method|0`) for direct calls and `targetDocumentId` for reduced
extensions (S06: `…Shout(System.String)` vs `…Shout`).

## Correction

`oracle/CanonicalTarget.cs` is now the single target-description pathway:

- normalization: `ReducedFrom ?? OriginalDefinition` (the accepted R1
  `SemanticInspector` rule, now shared by `SemanticInspector.SymbolKey`);
- document id: the canonical defining declaration's documentation id;
- declaration form: `<definingUnit>::<documentId>`;
- shape: `method|<parameterCount>` — the fixture-declared convention of the
  named R2 vectors, never Roslyn generic arity.

`RunIdentities`, the lifecycle `AppFacts`, and the new `RunCrossView` all
construct identities through it. The component-digest forms remain
fixture-declared per the qualification plan; no R2 formula changed.

## Cross-view results (qualified run, `oracle-r2-run-1.json`)

All nine designated sites agree field-by-field and byte-for-byte across the
main identity view, the lifecycle projection, and the accepted frozen anchors:

| Site | Canonical declaration | Shape | asmctx3 | usage4 |
|---|---|---|---|---|
| S01 | `Core/Core.csproj::M:Corpus.Core.Methods.Direct(System.Int32)` | `method|1` | `42a30aaf…` | `d1ae71fc315d0c1a5b41559e4ba7a6b10c9dc417eb3a52b9cd0781fb67b0b556` |
| S02 | `Core/Core.csproj::M:Corpus.Core.Methods.Static(System.Int32)` | `method|1` | `4eb4bd02…` | `9ed8a31fe0b9362be8ad3dacd85ac03f82be3e03c6c47f38db4c5d55b9888d4f` |
| S03 | `Core/Core.csproj::M:Corpus.Core.Methods.Add(System.Int32,System.Int32)` | `method|2` | `adcc2215…` | `2d7f8b3a5cb11d07eeddeeed14845dd2b581e2034662d04c8c41ed8a56a8b815` |
| S06 | `Core/Core.csproj::M:Corpus.Core.StringExtensions.Shout(System.String)` | `method|1` | `3caa27a9…` | `561e22873c87a75039076837615fc8fb60c27834bc9887e0c6a8d1fedd05f02a` |
| S21 | `Core/Core.csproj::M:Corpus.Core.Methods.Add(System.Int32,System.Int32,System.Int32)` | `method|3` | `46fcba57…` | `18baf7d15bbf98a5965a20e10adbadfe0da56c7210c67244b6d775e4b66a5ed5` |
| S26 | `Leaf/Leaf.csproj::M:Corpus.Leaf.Helper.Compute(System.Int32)` | `method|1` | `a8c76853…` | `d9269f4d248f9f738016b3cc72b7fd66f3cc8f817c93fa6440e9666dd22f4fe7` |
| INV-01 | `Core/Core.csproj::M:Corpus.Core.Methods.Add(System.Int32,System.Int32)` | `method|2` | `adcc2215…` | `699a94f4e670ec85df318174f0ec235d58fd33e157b186dc9b30dbd8c76df0c5` |
| INV-11 | `Core/Core.csproj::M:Corpus.Core.Methods.Direct(System.Int32)` | `method|1` | `42a30aaf…` | `62e2ea8b95e35e69d16fe86bd26b18e818f49a32f03761e2392a824d1f083d66` |
| INV-31 | `Core/Core.csproj::M:Corpus.Core.StringExtensions.WordCount(System.String)` | `method|1` | `9b9d1c7d…` | `da3a32d1a790dac4855bbb6c8af5419237e3d4185a167c5e2de5d3f376a73e4f` |

The corrected S01 and INV-01 values equal the R1 frozen main-identity vectors
verbatim — the main view was already canonical; the lifecycle view now agrees
with it.

## Independence of the frozen section

`FrozenExpectations.cs` hard-codes the accepted anchors transcribed from the
controlling contract documents, not computed from the code under test: Core and
Leaf `compctx1`/`compctx2`, the six named `asmctx3` vectors, and the S01/INV-01
`usage4` vectors. `RunCrossView` checks every designated site against these
anchors (`crossView.*.frozen`, `crossView.all-frozen-anchors`), so a mutation of
the shared pathway that also changed the main view cannot self-confirm.

## Invariants and negative controls (all pass in the qualified run)

- ID-1 all sites agree; ID-2 S01/INV-11 share `T.Direct` asmctx3 with distinct
  usage4; ID-3 source ownership keyed by owning source unit; ID-4 changed
  calling source changes usage4 while asmctx3 is stable; ID-5 frozen anchors.
- ID-N1 shape mutation (`method|1`→`method|2`) changes both identities;
  ID-N2 disabling reduced normalization produces the reduced doc-id and a
  different usage4; ID-N3 reusing a changed-Core snapshot's identity changes
  both; ID-N4 reintroducing deleted `App/Sites.cs` contributions fails the
  Snapshot B projection; ID-N5 overload substitution (`Static(int)` for
  `Direct(int)`, same shape) changes both identities.

External mutation controls for this finding are recorded in
`PROVENANCE-MUTATION-RESULTS.md` (`q2-f01-lifecycle-regression`,
`reduced-normalization-disabled`, `overload-substitution-s01`,
`deleted-contribution-reinjected`): each exits 1 with the expected failing
check IDs.
