# ANDIRIT-CI1A-S2.6-WP-S64-OCC-ID-REMED-01-R2

**Bounded framework and compatibility remediation**

## A — FRAMEWORK AND COMPATIBILITY REMEDIATION IMPLEMENTED — READY FOR INDEPENDENT REQUALIFICATION

The owner-accepted qualification finding is remediated. Production compilation
and identity generation now use the declared target framework's locally installed
reference pack, not the consumer runtime's implementation directory. Unsupported
or unavailable frameworks are refused. Persisted compatibility is detected
through a read-only preflight before any writable open, schema initialization or
command-log replay. The corrected C4 ledger is independently derived against the
target-framework closure and frozen before the final comparison.

DF-1 and S6.4 remain open. D3 remains suspended. No independent qualification,
owner acceptance, merge, release or publication is claimed.

## 1. Baseline and authorization

| Subject | Identity |
| --- | --- |
| R1 candidate | `77ec887368c62b404e0d9a8034d34b7f8cac8007` |
| R1 candidate tree | `40a2c42123e71bf65746b10198ca2af29c17d928` |
| R1 producer seal | `880a888da2256608a91010026b1c3f39d0de76b6` |
| R1 qualification seal | `c6c2dcfd5697460318634d34d26eba6b88f6d4e1` |
| R2 branch | `remediation/s64-occ-id-01-r2` |
| R2 implementation | `b8a748e1f992f60f2a5e4d3bc44e7fe47e1ee83e` |
| R2 candidate tree | `7dde06806d37f2b57f4464ee7adc7ffac14659ce` |
| R2 hosted run | `46143882731` |
| C4 freeze commit | `814fe5bceb139a89b1feeb90b35c65cd6b852767` |
| C4 comparison commit | `82ba813fb9eadf95042a8abf46832b31b0a6b2cf` |

The R1-06 exception remains narrowly scoped and unchanged. All predecessor seals
and historical evidence are preserved.

## 2. Framework reference-resolution contract

Explicit supported matrix:

| Target | Pack | Selection |
| --- | --- | --- |
| `net8.0` | `Microsoft.NETCore.App.Ref` | highest installed 8.0 patch with `ref/net8.0` |
| `net9.0` | `Microsoft.NETCore.App.Ref` | highest installed 9.0 patch with `ref/net9.0` |
| `net10.0` | `Microsoft.NETCore.App.Ref` | highest installed 10.0 patch with `ref/net10.0` |
| `netstandard2.1` | `NETStandard.Library.Ref` | highest installed 2.1 patch with `ref/netstandard2.1` |

`netstandard2.0`, `net6.0`, `net7.0`, platform-specific TFMs and missing target
frameworks are refused explicitly. Loose sources require explicit
`csharp.targetFramework`; there is no ambient runtime fallback.

Reference identities remain content-bound `refctx1` with no installation path.
The selected pack's actual files are the Roslyn references; `FrameworkList.xml`
managed entries are checked and every PE is validated/hashed. Equivalent pack
content at different paths has equal identity. The globally newest installed pack
is never selected for a lower target.

## 3. Baseline negatives and corrected results

The independent qualification negatives are reproduced and corrected:

| Control | Before | After |
| --- | --- | --- |
| net10/net8/netstandard2.0 same descriptor | yes | distinct supported descriptors; netstandard2.0 refused |
| `System.Half`/`Range` under netstandard2.0 | accepted | refused |
| consumer runtime selects closure | yes | declared pack selects closure |
| unavailable framework | no refusal | actionable refusal |
| incompatible store mutation | schema/replay before refusal | read-only refusal before writable open |

`net8.0` and `net9.0` are compiled on a .NET 10 consumer host against their own
reference packs, proving the consumer runtime no longer determines the semantic
closure. `netstandard2.1` with `System.Half`/`Range` produces
`FrameworkReferenceErrors` and no `Verified` compilation.

## 4. Compilation/identity consistency

- `compctx3`/`compctx4` and `asmctx4`/`usage5` recipes are unchanged.
- The corrected reference inputs change descriptor and target values.
- Actual Roslyn reference count equals the selected pack inventory plus project
  references; the descriptor records the same content-bound identities.
- Distinct supported TFMs produce distinct descriptors even for identical source.
- Project references are allowed for same TFM and `netstandard2.1` dependencies;
  incompatible caller/dependency TFMs refuse.
- Incidental consumer/test/application assemblies are absent.
- Absolute paths do not participate in identity.

## 5. Identity and persisted-generation reconciliation

`refctx1`, `compctx3`, `compctx4`, `asmctx4` and `usage5` are retained. No new
identity domain was introduced. Corrected experimental stores use generation 3 /
schema 3 / namespace `andirit-occurrence-facts/3` / authority version 3. R1
generation-2 stores are refused; they cannot be mistaken for corrected stores.
Rebuild remains re-ingestion into a new store only.

## 6. C4 oracle

| Field | Value |
| --- | --- |
| Pack | `Microsoft.NETCore.App.Ref` `10.0.12`, `ref/net10.0` |
| References | 167 |
| Reference set | `9973286b…63c82` |
| Target | `asmctx4:sha256:23ffa6444fb108132fb54e760cac76f5ffd8a3580b0aed1b9aa6709f78eeab68` |
| Defining context | `compctx4:sha256:e7c0a28d959f075a1f5d491e3e642c44d781bb4750de2f107ad78e57a408d6b1` |
| Descriptor | `compctx3:sha256:4b88c629c25ed6c9b916d5a279304fd07cb06fa64545d3c607ce1326a232989e` |
| Facts | 51 (50/1) |
| Contributors | 2 |
| Variants | 51 / 1 / 50 / 51 |

The independent oracle does not call production identity helpers. The ledger and
variants were committed at `814fe5b` before the production comparison at
`82ba813`. The R1 runtime-closure ledger and the S2.0/F12 three-reference fixture
are preserved separately; C1–C4 graph/corpus artifacts are unchanged.

## 7. Persisted compatibility and reconstruction

A real R1 generation-2 store is refused with
`CodeOccurrenceSchemaException` and re-ingestion guidance. Main DB and WAL hashes
are unchanged before and after refusal. Generation-1 and generation-2 simulations
also refuse without physical mutation. Detection uses
`ReadOnly=true, EnableWal=false` on LatticeDbSharp 0.2.0 before any writable
open, schema initialization, replay, mirror cleanup or recovery write.

A corrected new store has 51 facts, 50/1 distribution, two contributors and a
complete `Available` query after durable reopen. A second fresh store has the same
logical SHA-256. Reconstruction remains new-store re-ingestion, not in-place
migration; it is not universally lossless when historical source/reference/run
inputs are absent.

## 8. Regression, coverage and packaging

| Campaign | Result |
| --- | --- |
| Local Release suite | **1,059 passed, 0 failed** |
| WSL Linux x64 exact candidate | **1,059 passed, 0 failed** |
| R1 inventory | 1,044; +15 new controls |
| Mirror tests | 29/29 |
| Inherited S6.3 named cases | 19/19 |
| Andirit.CSharp coverage | 93.92% line / 87.16% branch (floor 75%) |
| Andirit.LatticeDb coverage | 92.81% line / 80.43% branch (floor 70%) |
| Core / TreeSitter coverage | floors preserved |
| Package pack/consume/native/Testing smoke | pass |

The 15 additions are target-framework resolution controls and generation-1/2
non-mutation controls. No accepted test was removed or weakened.

## 9. Cross-host and hosted campaign

Local Windows x64 exact execution, an exact-candidate WSL Ubuntu x86_64 run
(1,059 passed) and the framework matrix controls pass. The R2 hosted run
`46143882731` succeeded on Windows x64, Linux x64 and macOS ARM64. The seal run
`46145138043` also succeeded on all four jobs. Both executed synthetic PR merge
commits (`722b7315…` candidate tree `7dde068…`; `476beeec…` seal tree
`73d2a83…`) whose trees are byte-identical to the candidate/seal trees and whose
parents include them. The R1 hosted runs are accepted only under the owner's
narrow tree-equivalence exception and are not characterized as exact candidate
checkouts. Exact commit-object hosted execution for R2 still requires an owner
decision or workflow change; none was made. macOS is ARM64/`osx-arm64`
regression only; no macOS x64 or S6.5 claim is made.

## 10. Findings

| ID | Finding | Status |
| --- | --- | --- |
| R2-F01 | Incorrect host-runtime framework closure | **Resolved** |
| R2-F02 | Declared target framework ignored | **Resolved** |
| R2-F03 | Unavailable framework APIs accepted | **Resolved** |
| R2-F04 | C4 oracle closure mismatch | **Resolved for production pack; historical fixture preserved** |
| R2-F05 | Incompatible-store physical mutation | **Resolved via read-only preflight** |
| R2-F06 | Oracle freeze chronology | **Resolved separate freeze commit** |
| R2-F07 | Hosted synthetic-merge attribution | **R1 exception accepted; R2 recorded** |
| R2-F08 | macOS x64 misreport | **Corrected to ARM64** |
| R2-F09 | Reconstruction loss limits | **Retained** |
| R1-06 | Historical procedural exception | **Narrowly preserved** |
| DF-1 / F-S64E-03 / F-S64E-04 / F-S64E-05 | Broader defects | **Open** |
| R1-04 / R1-05 | Broader historical-store / D3 | **Outstanding** |

## 11. Required successor

Independent requalification is proposed as
**`ANDIRIT-CI1A-S2.6-WP-S64-OCC-ID-QUAL-01-R1`**. No D3 continuation, DF-1
closure or S6.4 closure is authorized here.


