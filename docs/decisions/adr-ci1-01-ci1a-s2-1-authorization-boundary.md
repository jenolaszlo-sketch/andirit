# ADR-CI1-01-CI1A-IMPL-01-S2.1: Authorization boundary

**Recorded:** 2026-11-02. **Mode:** documentation only.
**Decision: `ADR-CI1-01-CI1A-IMPL-01-S2.1 ELIGIBLE FOR SEPARATE AUTHORIZATION WITH PRECONDITIONS`.**
**Production implementation: NOT AUTHORIZED by OWNER-01.**

## 1. Source and authority

This boundary follows [qualified REM-01 acceptance](adr-ci1-01-ci1a-s2-boundary-rem-01-owner-acceptance.md)
at reviewed commit `aaea1ca7f32a666bc9c6480e91c75c7dd14779d4`, tree
`4012bf14e680a742da7a3785831fb578d9f92eb4`, and the verified REM-01-Q1
package bound there. Controlling contracts are owner decision §F, frozen R2
`PROVENANCE-CONTRACT.md` §§2–5 and Q3 `TARGET-IDENTITY-REVIEW.md`,
[REM-01 identity](../planning/adr-ci1-01-ci1a-s2-rem-01/identity-contract.md),
[gate reconciliation](../planning/adr-ci1-01-ci1a-s2-rem-01/stage2-gate-reconciliation.md)
and [Stage 2 boundary](../planning/adr-ci1-01-ci1a-impl-01/stage2-boundary.md) §8.
S2.0–S2.5 numbering is retained. This narrows the first implementation commission;
it does not replace the later extraction/publication/reader stages.

## 2. Readiness conditions

| Condition | Owner assessment |
| --- | --- |
| REM-01 owner acceptance | Recorded with every independent qualification. |
| Implemented canonical identity | Independently verified R2 helpers; NEW-02 matrix rows corrected in OWNER-01. |
| Fixed provenance authority | R2 §§2–3 and owner §F specify actual construction inputs and live universe authority. No new architecture decision is required. |
| Existing source integrity | `CodeRepositorySource.cs` opens source streams; `CodeIndexingLifecycle.MaterializeSourcesAsync` captures bytes and verifies SHA-256 when the planned hash uses `sha256:`; `CSharpCodeGraphPlugin.Session.cs` reads those sources into text and parses it. These are reusable inputs, not completed per-document provenance. |
| Coverage/availability boundary | Persisted structural model and durable empty are qualified; semantic completeness remains unimplemented. NEW-04 dependence rule below applies. |
| Publication/failure scope | Quarantine/reopen accepted in tested scope; NEW-01 assigned required follow-up task `ADR-CI1-01-CI1A-S2-FOUNDATION-RECOVERY-01` (separate authorization pending), before S2.3 and any production reliance. |
| Fixture decision | F12 accepted deferred qualification; existing S2.0 preparation route selected for separate commissioning; frozen independently accepted oracle required before S2.2 qualification. |
| Gate/document clarity | NEW-02/NEW-03 corrected; cross-platform exclusion wording aligned. Contracts and implementation/acceptance evidence are distinct. |
| Independent tests and exclusions | Defined below. Future tests are requirements, not credited as passing. |

No completed `compctx1` production construction, live universe or complete
S-01 implementation is required to authorize the very slice that implements it.
Before execution a separate owner commission must bind current source/tree,
reconcile any divergence and authorize this bounded work. Success must receive
independent review and owner acceptance before S2.2 authorization.

## 3. Objective, permitted work and provenance contract

Proposed commission:
**`ADR-CI1-01-CI1A-IMPL-01-S2.1: Verified Source Acquisition and Compilation Provenance`.**

Establish this production-verifiable chain:

`accepted inputs -> verified immutable source buffers -> Roslyn syntax trees -> compilation construction -> compctx1 + analysis-scope/universe provenance`.

The commission may inspect/reuse source-byte acquisition, retain verified
materialized buffers, record source hash per document, prove the decoding and
syntax-tree correspondence, construct accepted context identities, capture
options/references, establish source-unit and universe membership, detect input
change/conflicts, reject inadequate proof, and add focused provenance tests and
small reproducible C# project fixtures. Outputs must be usable by S2.2.

R2 §2 descriptor fields are `ContractVersion`, `RepositoryId`, defining
`ProjectUnitId`, `AssemblyName`, `MetadataIdentity`, `ModuleName`,
`OptionsDigest`, ordered `Sources[]` and ordered `References[]`. Sources bind
normalized path, exact byte SHA-256, language version and preprocessor symbols.
Options bind output kind, nullable, unsafe, optimization, platform, overflow,
deterministic, warning level, general and specific diagnostic options.
References use the accepted compilation-reference content digest or verified
metadata/file identity representation from R2; merely hashing a caller's
descriptor does not verify the reference actually used.

`compctx1:sha256:` hashes `compctx1-context`, contract version, repository,
defining project, assembly name, metadata identity, module, options digest,
ordered sources and references, with the accepted length-prefixed UTF-8
encoding. Persisted/relayed descriptors must recompute. Identity must be stable
without wall-clock, run ID, process ID, object address or enumeration order.

`analysisscope1` binds the declared project/unit/path inventory;
`compilation-universe1` binds repository, version, scope and ordered verified
context digests. Completeness means every declared unit has a verified live
context. It is scope-relative. Retain live compilation instances: descriptor-only
evidence cannot prove a symbol binding. Caller booleans cannot assert completeness.
The universe identity is evidence, not a target identity field; unrelated units
must not renumber an existing target. Calling/source ownership and defining
project identity remain distinct. S2.1 proves compilation/reference membership
and prepares exact-definition authority; S2.2 performs full semantic target
correspondence and canonical occurrence minting.

## 4. NEW-04 and NEW-01 dependency analysis

Source inspection at the pinned tree found `CodeOccurrenceCoverage.Merge` in
`CodeOccurrence.cs` (first non-null scope/universe, max counts), called by
`CodeGraphIngestionSink.Accumulator.Append` only when occurrence coverage is
present, `InMemoryCodeGraphStore.RepositoryCoverage`, and native coverage
aggregation/persistence/reopen in `LatticeCodeGraphStore.cs`. The existing
`Session.ExtractAsync` source-to-tree/compilation path does not call this merge
or emit authoritative occurrence coverage. Graph extraction can still use the
shared sink. Thus the sink is reachable, but the defective occurrence merge is
not an unavoidable dependency of source-buffer/context construction.

S2.1 must establish provenance from verified buffers, live compilations and
explicit membership inventories; it must not use merged occurrence coverage
as its scope/universe or source-completeness oracle. If the separately proposed
implementation routes real provenance through this merge, **NEW-04 correction
and independent qualification become prerequisites to that particular S2.1
claim**, before reliance; otherwise the mandatory correction remains during
S2.2 and before S2.2 acceptance. This rule preserves the earlier dependency
gate rather than automatically trusting the review's proposed timing.

Likewise S2.1 acceptance must not rely on known-failure occurrence projection
replay. NEW-01 is required **before S2.3 and before any production recovery
reliance**, including earlier integration. Native publication is S2.3 work;
focused source/compilation tests can verify S2.1 independently of either defect.

## 5. Explicit exclusions

S2.1 does not authorize complete Roslyn usage extraction, full `usage4` fact
production, public Find Usages, external semantic target confirmation, broad
occurrence reader/continuation work, native publication redesign, background
indexing, Tree-sitter usages, B2, migration/dual-write, incremental target-page
maintenance, general crash/power-loss recovery or universal analysis completeness.
Small cross-project provenance tests are required; a complete usage feature is
not necessary to qualify the slice. New correctness work outside this boundary
returns for separate authorization.

## 6. Mandatory qualification for the separate commission

Each case needs independently inspectable actual inputs, observed identities,
expected result established from the accepted contract, reproduction command,
source/tree binding and raw evidence. Producer tests undergo separate independent
review. A hash of supplied content is insufficient if Roslyn compiled different
content.

| Positive case | Required proof |
| --- | --- |
| Verified bytes feed syntax trees | Record immutable bytes/hash, encoding/BOM handling, normalized path, decoded text and actual tree correspondence. |
| Identical inputs | Stable `compctx1` and scope/universe identity on repeated processing. |
| Verified compilation options | Actual parse/compilation option fields included, not an unrelated options object. |
| Verified references | Actual metadata/compilation references bind to the recorded identity/content. |
| Multiple source units | Correct unit membership and source hashes for each compilation. |
| Valid project references | Distinct defining compilation identities preserved; dependency linkage proven from live instances. |
| Deterministic equivalent input | Canonical order and repeat/separate-process reproduction independent of input enumeration. |

| Negative/adversarial case | Required result |
| --- | --- |
| Disk bytes change after acquisition | Detect stale/incompatible acquisition or retain and truthfully attribute the accepted immutable snapshot; never silently substitute new bytes under old provenance. |
| Supplied hash differs from actual buffer | Refuse. |
| Roslyn receives text different from verified buffer | Refuse; demonstrate the disagreement is observable. |
| Options change without text change | New context identity and old provenance invalidated. |
| References change without text change | New context identity and stale reference proof refused. |
| Required defining compilation absent | MissingDefiningCompilation/refusal; no fabricated confirmation. |
| Incomplete scope/universe | Refuse completeness and dependent binding; no successful empty analysis. |
| Conflicting project identities | Reject duplicate/conflicting entries and repository/scope mismatch. |
| Stale source unit attempts reuse | Reject or rebuild from new verified inputs, preserving exact attribution. |
| Caller descriptor bypass attempt | Recompute descriptor, require actual construction/live authority; refuse supplied-only proof. |

Required evidence exposes actual bytes/tree input correspondence; every context
field; options/reference identity; invalidation; conservative refusal; deterministic
reproduction. Focused cross-project tests may prove live compilation membership
and refuse absence without building the full S2.2 extractor.

## 7. Progression and stopping conditions

S2.1 acceptance does not authorize S2.2. S2.2 additionally requires F12's frozen
oracle, verified target-binding/refusal, canonical minting and NEW-04 correction.
S2.3 requires NEW-01 correction and real source-owned native lifecycle proof.
S2.4 requires bounded publication-consistent continuation/coverage. S2.5 requires
integrated independent provenance, semantic, real-project D3/applicable D4,
available historical writers and Linux minimum evidence; Stage 6 retains full
D3/S-02/platform qualification.

Stop the affected work for insufficient source authority, unexplained source
divergence, unresolved architecture, dependence on an uncorrected foundation
defect, weakened D3/D4/D5, or scope expansion. Reconcile obligations before each
dependent commission; no future evidence is already passed.
