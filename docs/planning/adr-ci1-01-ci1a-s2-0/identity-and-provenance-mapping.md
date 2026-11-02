# Identity and provenance mapping

**Authority.** [S2.2 target identity decision](../../decisions/adr-ci1-01-ci1a-s2-2-target-identity-decision.md) (Option A); [identity contract](../adr-ci1-01-ci1a-s2-rem-01/identity-contract.md); [S2.1 provenance contract](../adr-ci1-01-ci1a-s2-1/provenance-contract.md).

## 1. Accepted R2 tuples (unchanged)

```
compctx1 = "compctx1:sha256:" + SHA256("compctx1-context",
  "compctx1", repositoryId, projectUnitId, assemblyName, metadataIdentity,
  moduleName, optionsDigest, sources..., references...)

compctx2 = "compctx2:sha256:" + SHA256("compctx2-context",
  "compctx2", <same fields>, references...)          # verified dependency-sensitive

asmctx3  = "asmctx3:sha256:" + SHA256("asmctx3-context",
  "compctx1", compctx1, metadataIdentity, moduleName,
  identifierDigest, declarationDigest, shapeDigest)

usage4   = SHA256("usage4", repository, plugin, "usage4-b2c-exp-r2-20261101",
  semanticUnit, path, sourceHash, startLine, startColumn, endLine, endColumn,
  targetDocumentId, asmctx3, "method-invocation")
```

All parts are length-prefixed with a big-endian 32-bit byte length. Source entries are `path|contentHash|languageVersion|preprocessorSymbols`, sorted ordinal. Reference items are metadata `file|<path>|<sha256>` or, for project references, the legacy R2 `compilation|<assembly>|<metadata>|<trees>` (`compctx1`) / the verified `compilation-context|<dependency compctx2>` (`compctx2`). This is a mapping of the frozen contract, not a redefinition; the strong verified project-reference claim is carried by `compctx2`.

## 2. Option A mapping (binding)

1. Semantic target identity is `asmctx3`, built from the **defining** compilation's R2-faithful `compctx1`.
2. `compctx2` is mandatory verified dependency-sensitive provenance.
3. `compilation-universe1` membership is part of the validity gate.
4. `usage4` is unchanged and uses the target's `asmctx3`.
5. A defining-context `compctx2` change invalidates prior occurrence evidence even when `asmctx3` is unchanged.
6. Durable publication must retain sufficient current `compctx2` association.
7. Identity equality does not establish freshness or validity.

## 3. Fixture reference identities

Framework references are content-bound and host-independent:
`file|runtime/System.Runtime.dll|sha256:<hash>`, `file|runtime/System.Private.CoreLib.dll|sha256:<hash>`,
`file|runtime/Microsoft.CSharp.dll|sha256:<hash>`; external image
`file|external/ExternalLib.dll|sha256:<hash>`. The concrete hashes are computed
from the pinned runtime 10.0.12 files and recorded in the frozen oracle.

## 4. Frozen baseline values

`compctx1` (R2-faithful, defining contexts):

| Unit | Value |
|---|---|
| `Leaf/Leaf.csproj` | `compctx1:sha256:404c5e48c3106b47786f14cad594139477c651fd78d0e050f8fd5bcab18fcb5f` |
| `Core/Core.csproj` | `compctx1:sha256:942a8e588a8db15afafb30d0292e0b867c767c5f72392b6f458783d961a71db3` |
| `App/App.csproj` | `compctx1:sha256:ff9b2b7bc2b618c15967d0e8660c98fb970fbbe3960d7188a6fe88a41cd09d99` |
| `Solo/Solo.csproj` | `compctx1:sha256:6c15cd642084bc73d2bb6b8c1014e8babc39405d9d674b6127c5f9af4640eac1` |
| `Negatives/Negatives.csproj` | `compctx1:sha256:53a1194743fc8b9023d693be8df8483095298de50a39860d6f93785d9786dd59` |

`compctx2` (verified):

| Unit | Value |
|---|---|
| `Leaf/Leaf.csproj` | `compctx2:sha256:7f1128703a0a8c3b0490e55a293898bdfb28ba2e4a125d6abc70270d06b7c777` |
| `Core/Core.csproj` | `compctx2:sha256:7bf4ac5b916ab5dcd2fadaa45c22cc7765ab83839eb7de8ef4d442865d07424d` |
| `App/App.csproj` | `compctx2:sha256:f73e8c8af3634ab2901082cd33aceeb5f75f03fec10496d866e2ff5e1344046f` |
| `Solo/Solo.csproj` | `compctx2:sha256:a3a393c1452ce77c438e633ae48ac11f94a4fb48fb23035a377c87fbbf85da40` |
| `Negatives/Negatives.csproj` | `compctx2:sha256:10e2260ac1e09f337f0aa9c619cb947f29512e8912dac127eaa2be07f5d637ef` |

`asmctx3` (source-defined targets):

| Target | Value |
|---|---|
| `Methods.Direct(int)` | `asmctx3:sha256:42a30aaf20d663a3115234de4d7adc42cc3b6a18144d0c7e44746f1d1670513a` |
| `Methods.Static(int)` | `asmctx3:sha256:4eb4bd02228c0a52b09a546c9b872bfe06cb7e9d411256c71440d35c50eb6f6a` |
| `Methods.Add(int,int)` | `asmctx3:sha256:adcc221501d9d95b6dbb5c3f797f0954d05043064e63c75fc904faad6c93a5cc` |
| `ConditionalApi.Value` | `asmctx3:sha256:82246b216b702a3cf541579cf1f0e0f0625b2a01a2427a468c0f3d92f5731c77` |
| `Leaf.Helper.Compute(int)` | `asmctx3:sha256:a8c76853f52d8d49fc64335ed55a11b8ccd0f087559b7b7630d6ba6ccadfc05a` |

`usage4` (source occurrences; calling semantic unit = `App/App.csproj`):

| Site | Value |
|---|---|
| `S01` (`App/Sites.cs` 15:30) | `a87ee10ca6c6d0732feca73c5ae73d5abd773399ea370173eddacf2b1cad20cd` |
| `INV-01` (`App/Bulk.cs` 20:30) | `699a94f4e670ec85df318174f0ec235d58fd33e157b186dc9b30dbd8c76df0c5` |

## 5. Component-digest qualification

`identifierDigest`, `declarationDigest` and `shapeDigest` are R2 inputs supplied
by the caller (production's `ComputeTargetId` takes them as parameters). The
corpus supplies them as **fixture-declared canonical forms** (the documentation
id, its defining unit, and a method/arity shape). The corpus does **not** claim to
freeze S2.2's source canonicalization of those forms — that is an S2.2/S-01
obligation. The identity oracle validates the R2 tuple algebra and its
sensitivity; it does not invent a normalization rule. This qualification is
bounded and does not affect the semantic truth, `compctx1`/`compctx2` or `usage4`
results, all of which use only fully specified inputs.

## 6. Option A cases (all pass)

| Case | Expectation | Observed |
|---|---|---|
| A01 | same target, same provenance | recomputed baseline `compctx1`/`compctx2` identical |
| A02 | same target identity, changed transitive dependency provenance | `compctx1(Core)` stable; `compctx2` changed at `Leaf` `7f11…`→`ffcc…`, `Core` `7bf4…`→`c335…`, `App` `f73e…`→`42d0…`; `Solo` unchanged |
| A03 | changed defining API → changed target identity | `compctx1(Core)` changed; `ConditionalApi.Value` `asmctx3` changed; `App` source unchanged |
| A04 | changed calling source, stable target identity | `App/Sites.cs` hash changed; `usage4(S01)` changes; `Methods.Direct` `asmctx3` stable |
| A05 | same textual descriptor, different defining compilation | identical declared forms in `Solo` vs `Core` → distinct `asmctx3` |
| A06 | missing current `compctx2` | validity gate false |
| A07 | missing universe membership | validity gate false |
| A08 | stale dependency provenance | recorded `compctx2` ≠ current → false, re-ingest |
| A09 | re-ingestion determinism | second build produces identical identities |
| A10 | complete-empty verified scope | `Solo` verified, zero occurrences |

`A02` is the mandatory Option A fixture: `targetIdentityUnchanged = true` and
`priorProvenanceValid = false` are represented separately and are not encoded as
one another.

## 7. Coverage and identity separation

`asmctx3` equality never substitutes for `compctx2` validity or universe
membership. The `source-changed` variant shows a new `usage4` with an unchanged
target; `symbols-leaf-changed` shows an unchanged target with invalidated
provenance. No target identity churn is created for a dependency-context-only
change.
