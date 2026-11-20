# Reviewer plan recorded before new fixture execution

Reviewer 01a1e38c-27d1-7491-a12c-63e0eecb704f. Reviewed production 25bc6e3b4305c7ce088deb36cfb000138dc76648. R1 baseline afa136f4033f35d9a657d8c5f1d5db92d736b679. Windows only; exported directories are not worktrees. Existing regression suites and sealed integration harnesses are replays, not newly independent oracle implementations.

## F-S254-02 expectations

The approved M4.1/M4.2/M5.5 require ordinary invocations as facts and bound constructor/property/method-group usage sites as unsupported coverage, without unsupported facts. Count usage sites rather than both name and enclosing member-access syntax. `nameof`, type mentions, fields and unresolved non-invocation expressions do not establish bound usage sites. Local-function invocations retain their existing unsupported classification. External ordinary method invocations retain external classification. Unsupported non-invocation constructs remain unsupported even where their target is in metadata; the positive external-invocation rule must not fabricate facts for those constructs.

New hand-enumerated snippets extend the sealed 50-site B3 fixture; each retains its 50 positive source-backed facts and one external ordinary invocation unless an explicit complete-empty/partial profile removes them. Expected inventories are stored in ReviewerCases.cs before execution. Observe syntax/operation symbols diagnostically, but do not calculate expected counts by copying the production predicate. Include constructor initializer overlap, nested receiver properties, indexer/read/write operations, explicit delegate construction plus method group, unresolved and ambiguous forms, and unrelated syntax. Baseline isolated constructor/property/method-group cases should contradict the required count with zero unsupported coverage. Preserve their failures.

## F-S254-03 expectations

One absent project and one absent-source project separately require 3 declared / 2 verified, IncompleteAnalysis, zero facts and no usable authority. Two absent projects require 4/2; a mixed absent-project/absent-source case requires 4/2. All available members preserve complete publication. A legitimately empty complete scope returns Available with zero facts under current authority. No usable analysis and unrelated provenance refusals remain AnalysisUnavailable. A complete-to-incomplete replacement must make a former handle stale, reject old authority, expose current partial coverage, and never preserve obsolete positive facts as current. Observe query, decoded native rows and fresh-process reopen. Existing schema/namespace and conflict controls are fresh replays.

## M5.4 bounded investigation

Maximum: six production fixture families, two input configurations per family (12 configurations), each publish plus separate-process reopen. At most two harness-repair attempts per family; preserve failed attempts. Do not change production, mutate verified universes, use fabricated symbols, reflection or authority bypasses for reachability.

| Family | Hypothesis / necessary conditions | Expected discriminating outcome |
| --- | --- | --- |
| Transitive source inheritance / direct reference control | Complete verified caller and all declared projects, but ancestor is not a direct caller reference | Missing direct reference may prevent method acquisition before resolver; direct control binds Complete |
| Transitive returned instance | Direct library returns an ancestor-defined object; chained invocation could acquire an ancestor method symbol | Either genuine MissingDefiningCompilation with authority, or diagnostic/null symbol before resolver |
| Transitive interface exposure | Direct library exposes ancestor interface through return type | Same discrimination, with interface dispatch |
| Captured platform metadata and source boundary | Genuine platform method plus source positive method | UnsupportedExternalEvidence versus Complete; no missing receipt inferred |
| Missing declared direct project / partial universe | Missing dependency or declared unit prevents complete authority | Earlier verification refusal or Incomplete; never count this as continuous complete-authority missing resolution |
| Assembly identity ambiguity / alias applicability | Inspect supported reference construction and existing two-live control | Ambiguous defense; aliases rejected by verified reference properties are excluded from production experiments |

Stop a family when the relevant symbol/reference/authority discriminator is established. Stop the investigation on a valid continuous receipt, demonstrated defect, or exhausted bounded plan. Failed attempts alone cannot establish unreachability. A source-backed unreachability disposition requires an exhaustive argument covering relevant production callers and admissible Roslyn symbols; otherwise record D with precise residual hypotheses. M5.4 stays blocked absent its exact receipt or an owner clarification. No contract reinterpretation is authorized.
