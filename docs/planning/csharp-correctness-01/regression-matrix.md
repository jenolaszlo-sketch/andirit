# Regression matrix — ANDIRIT-CSharp-CORRECTNESS-01

All rows are permanent tests in
`tests/Andirit.CSharp.Tests/CSharpCorrectnessRegressionTests.cs` plus one
retargeted pre-existing test.

**Failing-before method:** the full new regression set was executed against an
out-of-repo copy of the exact baseline commit `5c89ac0b3d0e87c0112880cf985826ded42b21b2`
(production code unmodified; the only change was making the existing test class
`partial` so the new test file could compile against it). Raw output:
[`evidence/failing-before-key-tests.txt`](evidence/failing-before-key-tests.txt)
and
[`evidence/failing-before-all-new-tests.txt`](evidence/failing-before-all-new-tests.txt)
(copied in full in the commit).

**Result: 15 failed / 7 passed** under baseline; **22/22 pass** under the
remediated implementation. No expectation was written to match incorrect
output; every failing row failed for the intended defect, not a compile error
(each uses compiling fixture code or asserts the exact intended exception).

Legend: **F** = failed before, **P** = passed before (negative guard).

## P-DEF-01 — external/collision target identity

| # | Regression | Before | After | Verifies |
| --- | --- | --- | --- | --- |
| 1 | `ExtractAsync_ExternalCallIsNeverConfirmedAsAnUnrelatedDocumentationIdShadow` (theory, both path orderings) | **F** (F-order: false `calls` edge to `A/Shadow.cs`; P-order: external) | P | Platform `System.Console.WriteLine` never confirmed to the unrelated local shadow; coverage `ExternalTargets≥1, EdgesEmitted=0`; no `references` edge from the caller to the shadow |
| 2 | `ExtractAsync_CrossProjectCallResolvesToTheRealDependencyDespiteDuplicateDocumentationId` | **F** (baseline refused the valid call: `Ambiguous=1, EdgesEmitted=0`) | P | Same documentation ID in two projects; only the dependency-context definition confirms; exact target node id asserted; `Ambiguous=0` |
| 3 | `ExtractAsync_SameAssemblyNameUsesProjectProvenanceNotNameEquality` | **F** | P | Same `<AssemblyName>` collision; resolution uses defining-compilation object + project provenance, not name equality |
| 4 | `ExtractAsync_LinkedSourceFileKeepsDistinctProjectContexts` | **F** | P | One physical file compiled in two projects → two distinct nodes; only the referenced project's node is the call target |
| 5 | `ExtractAsync_GenericDefinitionResolutionIgnoresUnrelatedDocumentationIdCollision` | **F** | P | Constructed generic call resolves to the generic definition in the dependency (`M:Example.Store\`1.Get(\`0)`), not the unrelated project |
| 6 | `ExtractAsync_ReducedExtensionResolutionIgnoresUnrelatedDocumentationIdCollision` | **F** | P | Instance-syntax extension call resolves to the static definition in the dependency (`ReducedFrom`) |
| 7 | `ExtractAsync_OverloadResolutionUsesTheBoundSignatureAndDependencyContext` | **F** | P | `WriteLine(string)` overload target; not the `int` overload, not the unrelated project |
| 8 | `ExtractAsync_SameNameLocalDeclarationRemainsTheCurrentProjectTarget` | P | P | A same-canonical local (private) declaration cannot capture a dependency-bound call; current project's own declaration wins (pre-existing passing negative, kept as guard) |
| 9 | `ExtractAsync_RepeatedExtractionKeepsValidIdentityAndRefusesTheShadow` | **F** | P | Two extractions (reversed source order): valid cross-project edge id identical, shadow refused both times, full JSON equality |
| 10 | `ExtractAsync_RemovedOrReplacedTargetContributionDoesNotLeaveAStaleEdge` | P | P | Removing/renaming the indexed target leaves no stale edge; unresolved/external counting instead |
| 11 | `ExtractAsync_CrossProjectImportsInheritanceAndReferencesRemainResolved` | P | P | Valid cross-project `imports`/`inherits`/`implements`/`references` relationships are not quietly suppressed by the stricter resolver |
| 12 | `ExtractAsync_MergedCrossProjectNamespaceImportStaysAmbiguous` | P | P | Namespace declared by two referenced assemblies (`ContainingAssembly == null`): conservative `AmbiguousTargets>0`, never guessed |
| 13 | Existing caller/callee/reference queries | — | P | Full existing suite (47 baseline tests) plus `Andirit.Tests` 312 and `LatticeDb` 27 pass unchanged |

## P-DEF-02 — documented partial-type extraction

| # | Regression | Before | After | Verifies |
| --- | --- | --- | --- | --- |
| 1 | `ExtractAsync_DocumentedSealedPartialTypeMergesDeclarationDocumentation` | **F** (`InvalidCastException`) | P | Two-part sealed partial with docs on one part: one logical node, 2 declarations, merged `doc-summary`, no exception; caller→part-method `calls` edge intact |
| 2 | `ExtractAsync_UndocumentedPartialTypeHasNoDocumentationProperties` | P | P | Both parts undocumented: no doc properties, one node, 2 declarations |
| 3 | `ExtractAsync_IdenticalPartialDocumentationIsStoredOnce` | P | P | Identical docs on both parts stored exactly once (no duplication by the merge) |
| 4 | `ExtractAsync_DifferentPartialDocumentationMergesDeterministicallyAndOrderIndependently` | **F** | P | Three parts with different docs: deterministic ordinal join, reversed source order byte-identical (nodes, declarations, edges, coverage) |
| 5 | `ExtractAsync_PartialDocumentationMergeIsBounded` | **F** | P | 6×512-char summaries: merged output capped at 2048 with deterministic prefix |
| 6 | `ExtractAsync_RepeatedDocumentedPartialExtractionIsDeterministic` | **F** | P | Repeated extraction, reversed order: identical JSON |
| 7 | `ExtractAsync_ReplacementRemovesStalePartialDocumentation` | **F** | P | Documented → undocumented replacement leaves no stale `doc-summary` |
| 8 | One logical node / correct structural relationships | covered by #1 (2 declarations, `contains` + `calls` edges asserted) | P | Physical declarations independent of logical aggregation |
| 9 | `ExtractAsync_GenuineInvariantConflictFailsWithTypeSafeDiagnostic` | **F** (`InvalidCastException`) | P | `static class` vs `delegate` sharing a canonical key: exact `InvalidOperationException`, type-safe message listing `modifiers=[existing:static|new:sealed]` |
| 10 | Rejection diagnostic does not crash | covered by #9 | P | Formatting path exercises text payloads without any cast |
| 11 | `ExtractAsync_RealAndiritCSharpPluginPartialDeclarationsExtract` | **F** (`InvalidCastException`) | P | Real 3-project extraction (`Andirit.Abstractions`, `Andirit`, `Andirit.CSharp`): `T:Andirit.CSharpCodeGraphPlugin` one node, 6 declarations, merged summary; `T:Andirit.CSharpProjectDiscovery` single; relationship extraction reached |
| 12 | Wider three-project extraction progresses beyond the original failure | covered by #11 | P | `calls` candidates > 0 after the previously fatal input |

## Retargeted pre-existing test

| Test | Change | Justification |
| --- | --- | --- |
| `ExtractAsync_AmbiguousTargetsAreCountedNeverGuessed` → `ExtractAsync_UnrelatedDuplicateDoesNotMakeAnExactDependencyAmbiguous` | Fixture unchanged. Assertion changed from "must stay ambiguous, no calls edge" to "must resolve to the exact Lib1 dependency (`Ambiguous=0, CrossProject=1`, exact target id asserted)". | The old assertion encoded the defect: the bound target provably belongs to Lib1 (exact dependency), so refusing it suppressed a **valid** relationship — explicitly forbidden by the mission ("do not quietly suppress valid relationships"). Genuine ambiguity is still covered by rows 2 and 12 above. Recorded as an intentionally changed semantic result in [`compatibility-assessment.md`](compatibility-assessment.md). |

## Adversarial probes (section 8 of the assignment)

| Attack | Result |
| --- | --- |
| Equal documentation IDs, unrelated project | Refused (row 1) |
| Equal assembly display names, distinct provenance | Resolved only via exact compilation object (row 3) |
| Equal symbol display names, different compilations | Resolved only for the referenced compilation (rows 2/4) |
| Unrelated source-built assembly in run but not referenced | Not eligible; external/ambiguous (rows 1/2) |
| External metadata target (`System.Console`) | External, never emitted (row 1) |
| Same-name local declaration not the bound target | Cannot capture (row 8) |
| Order-dependent summary selection | Impossible: ordinal-sorted set join (row 4) |
| Lost declaration evidence | 2 physical declarations preserved (rows 1/2) |
| Duplicate logical nodes | Single node per (project, canonical) (rows 1/2/4) |
| Genuine conflict accepted silently | Throws `InvalidOperationException` (row 9) |
| Crash in diagnostic formatting | Type-safe `FormatProperty` (rows 9/10) |
| Stale properties after replacement | Removed (row 7 / row 10 of P-DEF-01) |

## Not claimed

- The new tests do **not** independently qualify their own implementation;
  independent review remains mandatory (see
  [`independent-review-handoff.md`](independent-review-handoff.md)).
- Partial-method definition/implementation unification remains unqualified.
- Windows-only local execution; cross-platform CI claims are out of scope
  until publication is separately authorized.
