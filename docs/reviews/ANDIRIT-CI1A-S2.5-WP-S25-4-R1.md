# ANDIRIT-CI1A-S2.5-WP-S25-4-R1

**WP-S25-4-R1 — IMPLEMENTED, READY FOR INDEPENDENT QUALIFICATION**

Recorded 2026-11-16 Asia/Manila. Producer verification ran on Windows x64 in the sole Andirit worktree at `D:/projects/Andirit`, on `main`.

## Provenance and governing finding

| Role | Commit | Tree |
| --- | --- | --- |
| Frozen production and recorded origin/main | `48370711a6247b972dfc83628618924e864be374` | `3483bd278c7a628383a372cb788559cbdaf5c717` |
| Entry design | `581b84a5e910626b87fb260becf90aac500dd7bb` | `e43297f5cfa25eca93ab3d30b644ccab1363ac31` |
| Starting HEAD | `8ab2d25d058e5bb4301094078f1737b139868e6a` | `01571db59dfdd1bbb39b42b9096cc0f8d583ce2a` |
| Implementation and test commit | `afa136f4033f35d9a657d8c5f1d5db92d736b679` | `2962442c4c1528193fe79df9ea0ecc31701a463d` |

The start had a clean working tree, one worktree, zero stashes, and main two ahead and zero behind recorded origin/main. Both intervening commits were documentation-only. Before implementation, all 126 QUAL-01 manifest entries and the nine pinned historical manifests with 183 entries passed verification. The sealed R1 manifest binds the evidence to the implementation commit and its tree. The producer handoff records the exact evidence commit and its tree after sealing. [Preflight](CI1A-S2.5-WP-S25-4-R1-evidence/verification/preflight.log) · [Historical seal checks](CI1A-S2.5-WP-S25-4-R1-evidence/verification/history-before.jsonl) · [QUAL-01 integrity](CI1A-S2.5-WP-S25-4-R1-evidence/verification/qual01-manifest-before.log) · [R1 SHA-256 manifest](CI1A-S2.5-WP-S25-4-R1-evidence/manifest.json)

F-S254-01 blocked **M5.2** and the required external-binding control in **M3.1**. M5.2 requires external refusal and zero trusted occurrences for an external target. M3.1 also exercises a source-backed dependency and allows missing defining evidence to retain its unresolved refusal. Mapping §3 requires exactly one verified live defining compilation for minting. S-01 and its v1 clarification govern the source-backed component forms and digests. [QUAL-01 finding](ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-01.md) · [Accepted matrix](../planning/adr-ci1-01-ci1a-impl-01/stage2-acceptance-matrix.md) · [B3 contract](../planning/adr-ci1-01-ci1a-s2-5/wp-s25-4-execution-plan.md) · [Mapping contract](../planning/adr-ci1-01-ci1a-s2-2/mapping-contract.md) · [S-01](../planning/adr-ci1-01-ci1a-s2-5/s01-canonical-form-contract.md) · [Canonical-form clarification](../planning/adr-ci1-01-ci1a-s2-5/s01-canonical-form-v1-clarification-01.md)

## Baseline and root cause

The authority-bound QUAL-01 probe and fixture were copied byte-for-byte and built against the exact starting checkout, whose code and test inputs matched the frozen production baseline. At publication and in a separate reopen process, Roslyn uniquely bound `string.Concat("left", "right")` to `System.Private.CoreLib`. Each phase emitted zero occurrences for that call, returned `MissingDefiningCompilation`, and recorded `External=0`, `Unresolved=1` in the query aggregate and decoded caller unit. Both processes exited 1 with only the M5.2 assertion failing. [Baseline publication](CI1A-S2.5-WP-S25-4-R1-evidence/baseline/b3-publish.json) · [Baseline reopen](CI1A-S2.5-WP-S25-4-R1-evidence/baseline/b3-reopen.json) · [Probe and fixture hashes](CI1A-S2.5-WP-S25-4-R1-evidence/verification/receipt-summary.json)

`VerifiedCSharpUniverse.ResolveDefining` equated an empty set of matching live source compilations with missing defining evidence. It stopped before seeing whether Roslyn could link the symbol's assembly to a captured metadata reference. `VerifiedCSharpOccurrenceExtractor.Extract` already counted `UnsupportedExternalEvidence` as external and other missing defining evidence as unresolved. It skipped the identity mint on either refusal. `PublishOccurrencesAsync` staged the extraction's authority envelope, facts and coverage. Native publication persisted the counters with the caller unit, and query aggregation consumed the restored unit counters on reopen. The miscount originated in the resolver; publication and persistence propagated it faithfully. [Resolver](../../src/Andirit.CSharp/VerifiedCSharpAnalysis.cs) · [Extractor](../../src/Andirit.CSharp/VerifiedCSharpOccurrenceExtractor.cs) · [Publisher](../../src/Andirit.CSharp/CSharpCodeGraphPlugin.Session.cs) · [Native store](../../src/Andirit.LatticeDb/LatticeCodeGraphStore.cs)

## Bounded change and regression tests

The single production change is in `src/Andirit.CSharp/VerifiedCSharpAnalysis.cs`. After universe completeness and live defining-compilation lookup, the zero-match branch checks whether Roslyn associates the target's actual containing assembly with a `PortableExecutableReference` of the exact caller compilation in the verified universe. Compilation verification already requires exact captured metadata-reference membership. That evidence proves an external binding and supplies no verified source declaration or defining context, so the resolver returns the existing `UnsupportedExternalEvidence` status. Without that evidence the branch retains `MissingDefiningCompilation`. Live source matches and multiple-match ambiguity follow their existing branches. The external branch returns no defining context, reaches no `asmctx3` or `usage4` mint, and uses the extractor's existing external counter.

| Test file | Change and control |
| --- | --- |
| `S2_2Wp5R1AmbiguityTests.cs` | Added `ExternalBinding_MixedWithCrossProjectMissingAndAmbiguousCalls`. Verifies the captured PE reference, refusal without exact caller evidence, missing source compilation refusal, distinct ambiguous/unresolved Roslyn outcomes, independent cross-project target and occurrence expectations, caller ownership, and exact aggregate counters of 1 resolved, 1 external, 1 unresolved and 1 ambiguous. |
| `S23PublicationIntegrationTests.cs` | Added `ExternalBinding_CoverageSurvivesAuthorityBoundPublicationAndReopen`. Exercises the real publisher and authority-bound query, the durable caller row and separate publishing and reopening test hosts. The only fact belongs to the caller and targets the source-backed dependency. |
| `S2_2Wp8CorpusIntegrationTests.cs` | Updated the exact E01 external counter from 0 to 1 for the verified captured PE case. The existing per-site no-fact and other coverage assertions still run against the frozen fixture. |

[Exact implementation inventory](CI1A-S2.5-WP-S25-4-R1-evidence/verification/implementation-files.txt) · [Focused R1 test log](CI1A-S2.5-WP-S25-4-R1-evidence/verification/focused-r1.log) · [Publish test host](CI1A-S2.5-WP-S25-4-R1-evidence/verification/testhost-publish.log) · [Reopen test host](CI1A-S2.5-WP-S25-4-R1-evidence/verification/testhost-verify.log)

## Corrected publication and identity preservation

| Phase | Baseline | R1 | External occurrences |
| --- | --- | --- | --- |
| Authority-bound publication | `MissingDefiningCompilation`, External 0, Unresolved 1 | `UnsupportedExternalEvidence`, External 1, Unresolved 0 | 0 on both revisions |
| Fresh-process native reopen | `MissingDefiningCompilation`, External 0, Unresolved 1 | `UnsupportedExternalEvidence`, External 1, Unresolved 0 | 0 on both revisions |

Corrected publication and reopen exited 0. The caller unit's coverage, embedded authority coverage, and the aggregate query counters agreed. Publication and reopen produced identical decoded caller units on each revision. The missing dependency variant had zero trusted dependency bindings. Its separate CoreLib call qualified as external. The ambiguous overload variant had two candidates, one ambiguous count, and no occurrence at that site. [R1 publication](CI1A-S2.5-WP-S25-4-R1-evidence/corrected/b3-publish.json) · [R1 reopen](CI1A-S2.5-WP-S25-4-R1-evidence/corrected/b3-reopen.json) · [Missing dependency](CI1A-S2.5-WP-S25-4-R1-evidence/corrected/b3-missing.json) · [Ambiguous overload](CI1A-S2.5-WP-S25-4-R1-evidence/corrected/b3-ambiguous.json)

All 50 independently predicted `usage4` IDs matched the native unit and authority-bound query in each profile, at publication and fresh-process reopen. Every fact retained caller ownership, the dependency target, and the declaration in `Dependency/Api.cs`. The field-level verification reconstructed the defining `compctx1`, target and usage IDs from the sealed probe's recorded preimages. The 50 stable site primitives and all three S-01 component digests matched baseline, QUAL-01 and R1. Each fixture-specific derivation used its own 181 ordered reference identities. Nine probe-local reference identities differed across output locations, including the path and changed hash of the producer C# assembly. The absolute composed IDs therefore differ between profiles under O-1. The receipts preserve those reference differences and the exact IDs for each profile. [Identity reconstruction and profile comparison](CI1A-S2.5-WP-S25-4-R1-evidence/verification/receipt-summary.json)

## Integration and regression

- **B6 and B7.** The two-contributor constructed control retained its remaining contributor after deleting A and reopening, then removed the shared fact after deleting B and reopening. Owner- and plugin-distinct neighbors survived. The immutable-payload conflict refused and preserved the prior publication, coverage and occurrence state through reopen. The F-01 contributor limitation continues to bound this control. [Union](CI1A-S2.5-WP-S25-4-R1-evidence/corrected/b6-publish.json) · [A removed](CI1A-S2.5-WP-S25-4-R1-evidence/corrected/b6-reopen-a.json) · [B removed](CI1A-S2.5-WP-S25-4-R1-evidence/corrected/b6-reopen-b.json) · [Conflict](CI1A-S2.5-WP-S25-4-R1-evidence/corrected/b7-conflict.json) · [Conflict reopen](CI1A-S2.5-WP-S25-4-R1-evidence/corrected/b7-reopen.json)
- **D3.** Graph-only and occurrence-enabled publications agreed over the exercised 37 nodes and 97 edges, caller/reference queries, and native mirror and managed traversal after reopen. The graph-only occurrence query returned analysis unavailable. [D3 replay](CI1A-S2.5-WP-S25-4-R1-evidence/corrected/d3.json)
- **D4.** The before-commit barrier held the reader. It saw the complete post-publication result of 55 facts after the earlier result of 50. The stale handle refused. The B3 authority controls also exercised absent current authority and stale publication refusal. Existing managed recovery and native publication tests passed. [D4 schedule](CI1A-S2.5-WP-S25-4-R1-evidence/corrected/d4-schedule.json) · [Focused native tests](CI1A-S2.5-WP-S25-4-R1-evidence/verification/focused-native.log)
- **D5.** The namespace and schema mutations refused with `CodeOccurrenceSchemaException`. The page-binding mutation refused with `CodeOccurrenceIndexException`. The focused native refusal tests passed. Historical SQ-04 evidence retains its amended scope and authentic-discovery trigger. [Namespace](CI1A-S2.5-WP-S25-4-R1-evidence/corrected/d5-namespace.json) · [Schema](CI1A-S2.5-WP-S25-4-R1-evidence/corrected/d5-schema.json) · [Page](CI1A-S2.5-WP-S25-4-R1-evidence/corrected/d5-page.json) · [Focused refusal tests](CI1A-S2.5-WP-S25-4-R1-evidence/verification/focused-refusal.log)

| Gate | Result | Exit |
| --- | --- | --- |
| New focused R1 tests | 2 passed | 0 |
| Focused separate test hosts | 1 passed at publication and 1 passed at reopen | 0 each |
| C# binding, classification and publication | 145 passed | 0 |
| Native publication, recovery and persistence | 25 passed | 0 |
| Focused native refusal | 8 passed | 0 |
| Whole solution | 909 passed, 0 failed, 0 skipped. Core 345, C# 355, TreeSitter 157, LatticeDb 52 | 0 |
| Frozen R2 oracle | 185/185 passed. Output 146851 bytes, byte-identical at SHA-256 `97f6c5e5a4cc77e089481c7643732b7a07a31d483c2643bd7a95fdad7ced39c9` | 0 |
| Release build | 0 warnings, 0 errors | 0 |
| Format verification | Passed | 0 |

Focused counts overlap the full suite. The successful gates preserved the 907-test baseline floor. Initial sandboxed Git status did not resolve the external worktree; the approved local invocation recovered the clean starting state. Two local edit wrapper attempts stopped before writing on a Python quoting error. A receipt validation attempt incorrectly asserted cross-profile raw ID equality and was corrected to the contract's host-scoped comparison after inspecting the reference preimages. The resumed validation used the original completed gate outputs. [Full solution and commands](CI1A-S2.5-WP-S25-4-R1-evidence/verification/full-solution.log) · [Focused binding](CI1A-S2.5-WP-S25-4-R1-evidence/verification/focused-binding.log) · [Oracle](CI1A-S2.5-WP-S25-4-R1-evidence/verification/oracle.log) · [Build](CI1A-S2.5-WP-S25-4-R1-evidence/verification/release-build.log) · [Format](CI1A-S2.5-WP-S25-4-R1-evidence/verification/format.log) · [Execution notes](CI1A-S2.5-WP-S25-4-R1-evidence/verification/edit-retry.log)

## Linux impact and outstanding qualification

R1 changes the managed C# resolver and thereby the coverage payload presented to the native publisher on supported platforms. It carries LatticeDbSharp 0.2.0 and the existing native storage path, including WAL-backed durable reopen. The accepted Ubuntu x64 run 42744298084, at `7457284451dff8ddb0417f83ecc19097103316fc`, tree `fba379453ceb3088cab97141ebae5c4c4978e3d3`, remains historical evidence for the 52-test native suite and the accepted platform, authority, and managed recovery scope. The corrected PE binding and persistence behavior traverses that platform scope on a changed producer revision. A separately authorized Linux delta should pin the corrected SHA, rerun the complete 52-test native suite, run the focused C# classification and native publication/reopen regression on Linux, and record platform, native library, toolchain and scope provenance. The R1 execution was Windows-only. [WP-S25-2 owner decision](../decisions/adr-ci1-01-ci1a-s2-5-wp-s25-2-owner-01.md) · [Source impact](CI1A-S2.5-WP-S25-4-R1-evidence/verification/preflight.log) · [Toolchain](CI1A-S2.5-WP-S25-4-R1-evidence/verification/toolchain.log)

WP-S25-1 and WP-S25-2 remain closed in their qualified scopes. WP-S25-3 remains closed with qualifications. SQ-04 remains accepted under the amended contract. S-01 remains satisfied with qualifications and P3-F-01 remains closed by the verified additive erratum. Q-P2-01 through Q-P2-06 retain their methodological independence, historical reconstructability, host scope, finite test scope, Stage 6 deferral and D3/D4/D5 qualifications. The one resolver change precedes minting, and the evidence showed no material contradiction in those earlier technical bases. The Linux delta limits applicability to the corrected revision while preserving prior acceptance. [Entry dispositions](ANDIRIT-CI1A-S2.5-WP-S25-4-ENTRY-OWNER-01.md) · [P2 erratum](ANDIRIT-CI1A-S2.5-WP-S25-3-P2-QUAL-01-ERRATUM.md)

**F-G2-01 — OPEN.** The historical Ubuntu result remains 296/450 branches (65.77%) against 70%. Any subsequent coverage commission must measure the corrected revision and its actual denominator at the existing threshold.

The original 64-row matrix remains 27 pass, 10 pass with qualifications, 2 failed on F-S254-01, 18 outstanding mandatory fresh-evidence rows, 3 reused qualified Ubuntu rows and 4 deferred Stage 6 rows. The two failed rows are M3.1 and M5.2. The impact matrix records producer evidence and the required independent path for every row. All 78 prior obligation entries and downstream routes continue to govern. [R1 requalification impact matrix](CI1A-S2.5-WP-S25-4-R1-evidence/requalification-impact.md) · [QUAL-01 outcomes](CI1A-S2.5-WP-S25-4-QUAL-01-evidence/matrix.md) · [Obligations and downstream routes](CI1A-S2.5-WP-S25-4-QUAL-01-evidence/obligations.md)

## Producer handoff

R1 scope is the resolver, its regression tests and bounded producer evidence. Independent qualification, WP-S25-4 closure, owner acceptance, Stage-2 acceptance and F-G2-01 coverage remediation remain pending under their original authorities. Full D3, D4.5, full S-02, F-02 crash and power-loss qualification and full platform certification retain their Stage 6 routes. Managed injected-fault results support the exercised managed behavior. Public Find Usages, cross-target operation, schema migration, dual-write and performance or pagination optimization remain outside this commission. The work used local commits on main with zero pushes.

Recommend **ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-02** in a fresh reviewer session uninvolved in R1. It should independently qualify the corrected revision, reconcile the affected prior evidence and run the 18 outstanding mandatory controls. Include a pinned Linux delta under separate owner authorization. The producer hands off at R1 and awaits that authorization.
