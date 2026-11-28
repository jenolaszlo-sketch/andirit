# Implementation record

## 1. Production changes

| File | Change |
| --- | --- |
| `src/Andirit.Abstractions/CodeOccurrence.cs` | `asmctx4`/`usage5` domains, generation/authority constants, `compctx4` context validation, predecessor-target recognition, successor payload domain and semantics 5 |
| `src/Andirit.CSharp/VerifiedCSharpCompilation.cs` | Content-bound `refctx1` capture; `compctx3` production descriptor; `compctx4` defining-context mapping; successor reference-item guard |
| `src/Andirit.CSharp/CompilationProvenanceDescriptor.cs` | `compctx3`/`compctx4` accepted; predecessor items rejected in successor descriptors; versioned option digest |
| `src/Andirit.CSharp/CSharpCodeGraphPlugin.Compilation.cs` | Deterministic managed runtime-directory reference closure replaces process TPA |
| `src/Andirit.CSharp/VerifiedCSharpAnalysis.cs` | `compctx3` universe membership/identity; `compctx4` minting input; semantics 5 |
| `src/Andirit.CSharp/VerifiedCSharpOccurrenceAuthority.cs` | `compctx3` recorded context; authority version/generation checks; predecessor-context refusal |
| `src/Andirit.CSharp/VerifiedCSharpOccurrenceExtractor.cs` | Authority version 2 / generation 2 |
| `src/Andirit/CodeOccurrenceAuthority.cs` | `IdentityGeneration` on the persisted envelope |
| `src/Andirit/CodeGraphBatchValidator.cs` | Accepts only the successor semantics generation and target domain |
| `src/Andirit.LatticeDb/LatticeCodeGraphStore.cs` | Occurrence schema 2 / namespace 2; meta generation check; generation written on publication |
| `src/Andirit.LatticeDb/LatticeCodeGraphStore.OccurrenceAuthority.cs` | Authority version 2 check with re-ingestion refusal |

## 2. Persisted identity-generation detection

On open, `RestoreOccurrences` requires:

```text
SchemaVersion      == 2
Namespace          == andirit-occurrence-facts/2
IdentityGeneration == 2
```

Otherwise `CodeOccurrenceSchemaException` is thrown with an explicit
"re-ingestion into a new store is required" reason. At query time the authority
gate independently requires `Version == 2` and `IdentityGeneration == 2`, and
refuses predecessor `compctx2` contexts/targets with
`CodeOccurrenceAvailability.UnsupportedSchema`.

Refusal does not rewrite the logical predecessor occurrence rows. The test
`PredecessorGenerationStore_IsRefusedAndReingestionBuildsSuccessorStore` verifies:

1. publish a successor store;
2. simulate a genuine predecessor meta row (schema 1, namespace `/1`,
   generation 1);
3. opening is refused with re-ingestion guidance;
4. the logical predecessor rows and generation marker are unchanged;
5. re-ingestion into a **new** store yields the successor target and 51 facts;
6. the new store serves the facts as `Available`.

## 3. Reference closure

`BuildPlatformReferenceEvidence` now enumerates managed assemblies in
`RuntimeEnvironment.GetRuntimeDirectory()`, filters via `PEReader` metadata, sorts
ordinally and captures content-bound identities. Testhost/xunit/consumer and
application assemblies are absent. No process `TRUSTED_PLATFORM_ASSEMBLIES` input
remains.

## 4. Reconstruction, not migration

No migration code path exists. Reconstruction is the ordinary ingestion path
(plugin session + sink) into a new `LatticeCodeGraphStore`. The original database
is never opened for mutation by the transition.

## 5. Test additions

| Test | Purpose |
| --- | --- |
| `OccurrenceIdentityTransitionTests.SuccessorDomains_AreExplicitAndPredecessorsAreNotCanonical` | Explicit successor domains and predecessor refusal |
| `...SuccessorContexts_RefusePredecessorReferenceItems` | Successor descriptors cannot be relabeled path-bound contexts |
| `...FrameworkClosure_ExcludesHostAndTestAssembliesAndAbsolutePaths` | Closure equals managed runtime set; no host/test refs or absolute paths; deterministic |
| `...SameCorpusAtDifferentPhysicalPaths_ProducesIdenticalTargetAndFacts` | Cross-path determinism of target and all 51 facts |
| `...DifferentSemanticReferenceContent_StillChangesIdentity` | Semantic negative control |
| `...PredecessorGenerationStore_IsRefusedAndReingestionBuildsSuccessorStore` | Persisted-generation refusal, preservation and re-ingestion |
| `SuccessorIdentityOracle` | Independent successor contract implementation used for expected values |
| `S64D3C4SuccessorTests` | Frozen C4 successor ledger across four variants |

## 6. Historical tests retained

The historical `compctx1`/`compctx2` synthetic tuple vectors remain in
`VerifiedCompilationProvenanceTests` and still verify. Predecessor-specific
expectations that encoded path-bound identity were converted to the successor
semantics (for example, "different path" now asserts content-bound equality).
S6.1–S6.3, mirror, publication, recovery and S24/S25 contracts were not weakened.
