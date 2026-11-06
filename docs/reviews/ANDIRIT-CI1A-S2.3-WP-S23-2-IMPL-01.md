# ANDIRIT-CI1A-S2.3-WP-S23-2-IMPL-01 — Persisted defining-compilation authority and native reopen stale-refusal

**Commission:** bounded WP-S23-2 implementation and qualification preparation.
**Repository:** `D:/projects/Andirit`. **Branch:** `main`. **Date:** 2026-11-06.
**Disposition:** **IMPLEMENTED — READY FOR INDEPENDENT QUALIFICATION**.
**Owner acceptance:** not granted by this report. No push or package publication.

## 1. Verified baseline and source binding

- Starting commit: `b576acb004747c89d257d8e61721c17babe6c5a6`.
- Starting tree: `3d7c152eb2d15fe87f940ba165f2b5c2d44cfc2a`.
- Clean canonical main, one worktree, no stashes; 24 ahead / 0 behind local
  `origin/main` (`443c192733a53d2f1da415743debb84e8c81acb5`). No concurrent writer observed.
- Implementation commit: `51369b3b463b6a2f137ee3475b78c9cd43f4db85`.
- Implementation tree: `2deec87bca15b24133be51263a7c9ea137b5b978`.
- Exact consecutive parent is the accepted NEW-01 owner decision. The implementation
  commit changes 17 files (+928/-16), including the controlled test/process harness.
- `source-binding.json` records the exact Git blob SHA-1, raw blob SHA-256 and byte
  length of all 17 files. `production-diff.txt` is the exact UTF-8 Git source diff.

Evidence directory: [`ANDIRIT-CI1A-S2.3-WP-S23-2-IMPL-01-evidence/`](ANDIRIT-CI1A-S2.3-WP-S23-2-IMPL-01-evidence/).
The report/evidence commit is additive to the implementation; its identity is
reported in the handoff rather than embedded recursively here.

## 2. Controlling accepted authority

Read against actual source: S2.3 entry owner decision; WP-S23-2 work-package row;
native-reopen-test-8b gate; scope/authority inventory; NEW-01 owner acceptance;
S2.2 G-ACCEPT; Option A identity decision §5.1/§5.2; WP3 mapping acceptance and
mapping contract; G-MINT; WP4/WP5/WP6/WP7/WP8 owner decisions; Stage 1 native
acceptance/reuse; Stage 2/S2.3/S2.5 boundaries. Exact document byte hashes and
paths are in `authority-inputs.json`; all remained unchanged.

Binding separation: frozen `asmctx3` identifies the target; `usage4` identifies
the source occurrence; defining `compctx2` and verified universe membership
establish validity. A persisted structural identity or digest is not current
semantic authority. Test 8b requires a completed native publication, reopen,
matching-current positive control, and stale refusal despite stable target identity.

## 3. NEW-01 boundaries retained

**Q1-Q1:** occurrence facts are absent from command segments. Publication followed
by fresh-process reopen is supported here; occurrence-bearing run resumption
across a restart is not made supported. §14 specifies the conservative missing-
authority behavior and the WP-S23-3 prerequisite.

**Q1-Q2:** recovery rebuild failure atomicity remains unproven outside the accepted
fault surface. The carrier is opaque JSON during restore and checked at the query
gate. No carrier validator was inserted into reconstruction and no new quarantine
framework was introduced. A malformed/unsupported carrier plus before-commit fault
still rethrows the injected original failure, then refuses at the query gate.
No changed reachability through the supported surface was demonstrated.

## 4. Original gap and pre-implementation contract

`CodeOccurrenceFact`, coverage, batch, and replacement originally had no persisted
per-defining-compilation association. Mint-time `RecordedCompctx2` did not survive
as native validity evidence; the universe digest alone was insufficient.

`carrier-contract.md` was recorded before source edits. The selected representation
is an internal **per-source-owned storage unit envelope with per-fact defining
receipts**, attached to the completed native occurrence unit. It reuses the native
transaction and `AndiritOccurrenceUnit` rather than introducing another store.

## 5. Association key, fields, and conflict rules

Outer ownership key is `(repository, plugin, storage index unit)` (the existing
newline-delimited native unit key). The envelope separately records semantic
`SourceUnitId`; it does not equate source ownership with the defining compilation.
Each unique `OccurrenceId` receipt carries:

- `OccurrenceId`, `PayloadHash`, and `TargetId` (`asmctx3`);
- `DefiningProjectUnitId` and recorded defining `Compctx2`;
- the existing caller-supplied S-01 identifier/declaration/shape components needed
  to recompute the target with the current verified defining compilation.

The envelope records version 1, repository/plugin/storage/source ownership,
`ScopeId`, `UniverseId`, exact coverage, ordered verified `(project unit, compctx2)`
membership, and the per-fact receipts. Complete-empty units retain this envelope
and scope/membership even when the receipt list is empty.

No global `asmctx3 -> compctx2` map exists. Replacement replaces the unit envelope;
retained unit envelopes survive with their original recorded contexts. Duplicate,
missing, orphan, ownership-conflicting or payload-conflicting receipts refuse.
Different contexts over time are represented in different publications/replacements;
there is no arbitrary first-context selection. A mixed-universe publication retains
the accepted coverage merge rejection, rolls back, and recovers the prior publication.
If one stored receipt is stale while other units are current, the query refuses
conservatively rather than manufacturing global completeness from a subset.

## 6. Additive persisted representation and version interpretation

| Location | Additive fields / meaning |
| --- | --- |
| `AndiritOccurrenceUnit` JSON | `Authority` (opaque versioned JSON), `RequiresAuthority`, `AuthorityPublicationIndexRunId` |
| `AndiritOccurrenceMeta` JSON | `RequiresAuthority` derived from the published unit set |
| Graph unit / command segment JSON | `RequiresOccurrenceAuthority` marker only; facts and full authority remain excluded |
| In-memory replacement | Internal `[JsonIgnore] OccurrenceAuthority` receipt envelope, plus the serialized mandatory marker |

Native graph schema remains 2; occurrence schema remains 1 and namespace remains
`andirit-occurrence-facts/1`. The authority envelope's version is an additive extension
version, not a migration or reinterpretation of the occurrence format. No new table,
secondary publication transaction, dual write, legacy adapter, or automatic upgrade.
Existing records with missing authority are never promoted by the verified gate.
Legacy synthetic/raw storage-query behavior remains as previously accepted; that
storage API is not a semantic verification capability. Authority-required records
refuse ordinary native queries that omit current verified authority.

## 7. Production capture and current-authority boundary

The existing `MintUsage` path still first consumes `MintTarget` and its accepted
WP3 resolver. After successful minting it emits a receipt from the resolved verified
defining entry. Extraction collects these receipts, its exact coverage, and verified
membership only if the source is the exact verified instance in a complete matching-
repository universe. A substituted source receives no native authority envelope.

`VerifiedOccurrenceExtraction.PrepareNativeUnit` is an internal controlled handoff.
It binds the envelope to the storage origin. It does not wire the plugin, batch,
sink or repository lifecycle; those remain WP-S23-3.

`VerifiedCSharpOccurrenceAuthority.Bind` requires an independently constructed
`VerifiedCSharpUniverse` and the exact current live caller in it. No public API
accepting arbitrary context strings was added. The internal caller must supply the
current verified universe; the store does not discover filesystem changes or load
live compilations automatically after reopen.

Before reading a target page, the native gate validates publication identity, native
unit keys/counts, mandatory receipt presence/version, and every analyzed unit against
that current verifier. The verifier checks exact coverage and membership, source
unit membership and source hashes, per-fact identity/payload/target associations,
recorded defining context equality, and the target recomputed through the accepted
R2-faithful mapper for the recorded defining project. Matching two values from the
same persisted record is never sufficient. Validating a caller's compilation does
not substitute its context for the defining compilation.

## 8. Observable refusal and availability mapping

| Input | Existing result vocabulary |
| --- | --- |
| Matching verified current authority, complete analysis | `Available` with facts or legitimate zero hits |
| Matching authority, Partial coverage | `IncompleteAnalysis`; coverage/counters retained |
| Missing current verified context | `AnalysisUnavailable`, explicit reason, no hits |
| Missing recorded envelope / defining context | `AnalysisUnavailable`, re-ingestion required, no hits |
| Changed verified context, universe membership, or publication | `StalePublication`, no hits |
| Unsupported authority version | `UnsupportedSchema`, no hits |
| Malformed/inconsistent authority, ownership, payload or accounting | `CorruptedIndex`, no hits |
| Legitimate graph-only publication | Existing `AnalysisUnavailable` mapping |

Refusals report zero index entries examined and do not claim complete-empty success.
The accepted native-open schema errors remain unchanged for corruption of the base
occurrence schema itself. Malformed additive carrier contents are handled at query
time, so they do not become a new reconstruction validator.

## 9. Graph A / Graph B native test 8b

The real Roslyn fixture is `Leaf -> Mid -> Lib -> App/App2`, plus a verified Empty
unit. App references Lib and Mid and produces two targets with different defining
compilations. App2 produces a second source-owned occurrence of the Lib target.
All source bytes/hashes, options digests, verified contexts, and native decoded
unit/page/meta/graph records are retained in the process JSON evidence.

Independent expected target construction uses the frozen R2 `CompilationProvenance.Describe`
and frozen tuple hashing, not the new carrier or the production mapper. Expected
`usage4` is independently constructed with the frozen tuple. S-01 components remain
explicit caller inputs; this is not a general source-canonicalization proof.

- Stable Lib target: `asmctx3:sha256:9086e2a29560c4405d80cc2d80034fd5ba12b070b7017892443bee7fce4d4ef0`.
- Graph A defining context: `compctx2:sha256:85ed88f23304d1b4750d059c463207bb16f87449827dde70c4cca32b9bfb0e64`.
- Source-only Graph B defining context: `compctx2:sha256:f997c03d2d972c1b7ccc3c85712cd8131755cbbef427d3e1ad6c715c0ded2a32`.
- Options-only changed defining context: `compctx2:sha256:9f3dd05c546d336720ef08924f76ae8ab00525e46d8b78af0c60c959648b6e06`.

Only Leaf source bytes change in the source variant. Only Leaf's overflow-check
compilation option changes in the options variant; source bytes are unchanged.
Lib's `asmctx3` stays identical while its dependency-sensitive `compctx2` changes.
WP7's transitive identity controls also pass unchanged.

## 10. Fresh-process positive, stale, and fresh-B controls

Seven separate process invocations (distinct recorded PIDs) independently acquire
source bytes, construct live verified compilations/universes, and open the native DB:

| Process evidence | Result |
| --- | --- |
| `process-publish-a.json` | Graph A committed; current A `Available`, 2 Lib hits |
| `process-fresh-a.json` | Fresh process reopen with independently verified A: `Available`, 2 hits |
| `process-stale-source.json` | Transitive source-only Graph B: `StalePublication`, 0 hits |
| `process-stale-options.json` | Transitive options-only graph: `StalePublication`, 0 hits |
| `process-publish-b.json` | Fresh Graph B facts/authority replace A: `Available`, 2 hits |
| `process-fresh-b.json` | Fresh process B reopen: `Available`, 2 hits |
| `process-old-a-after-b.json` | Old A context against stored B: `StalePublication`, 0 hits |

`process-evidence-check.json` records independently checked relationships: all seven
share the stable target, stale queries leave the complete native record set exactly
unchanged, persisted A receipts remain A while current context is B, and newly
published B receipts bind B. This does not depend on unsupported segment resumption.

## 11. Cross-project, multi-target and mutation evidence

`CrossProjectMultiContext` independently decodes native receipts and confirms that
App's facts bind Lib and Mid respectively, never automatically App. Two source units
query the same Lib target correctly. `MixedVerifiedContexts` proves the stable target
cannot overwrite or select among A/B universe authority: the mixed completion is
rejected and all occurrence rows remain exactly at A.

Nineteen per-record mutation cases exercise missing envelope/context, malformed context,
unsupported or malformed version, malformed envelope, wrong defining unit, caller-
as-defining substitution, correctly formatted unverified context, changed/duplicate/
missing membership, wrong target/payload, missing/duplicate receipts, wrong owner,
wrong publication, and substituted coverage. Each refuses observably with no hits,
no successful completeness, and no modification of the tampered native records.
The formatted-context mutation challenges a real comparison against independent
verified authority; it is not merely a string-format negative. Separate tests reject
substituted live callers/sources and incomplete universes.

## 12. Complete-empty, Partial, unavailable, and retirement

A verified Empty source has no fact receipts but retains current scope/universe
membership. It returns `Available` with zero hits only with matching verified context;
a changed context refuses, and omitted current context is unavailable. Graph-only
replacement remains unavailable with null coverage. A controlled Partial contribution
persists matching authority/coverage and reopens as `IncompleteAnalysis`, retaining its
facts. This test does not redefine the extractor's accepted refusal-counter semantics.

Replacement with an empty source removes old receipts; deletion removes its native
unit and authority together. A remaining verified complete-empty contribution makes
the retired target correctly return **`Available` with zero hits** after reopen.
A caller-held older publication remains `StalePublication` even if the target is equal.
Coverage retains the accepted bounded-pass/disjoint-contribution semantics; no claim
of arbitrary-repository or all-category completeness is added.

## 13. Atomicity and NEW-01 recovery compatibility

Graph, facts, pages, meta and authority use the existing `PersistOccurrences` transaction.
The in-memory authority snapshot is returned only after a known successful commit and
installed under `_writeGate`. A before-commit failure cannot expose a new envelope
with old facts. Retained envelopes rebind to the accepted publication in that same
transaction. The cache is derived state, not an alternative durable authority.

Both NEW-01 catch branches still call the unchanged replay/baselines/occurrences
rebuild. Restore rebuilds the authority snapshot from those same durable occurrence
rows, without parsing the envelope as trusted authority. Focused tests cover apply-time
graph rejection and before-commit rollback/retry (including unsupported carrier data).
Original injected exceptions remain observable. All 17 independent NEW-01 probes pass,
including conflict bypass closure, native mirrors, and unknown-outcome quarantine.
No stronger recovery-failure atomicity or crash guarantee is claimed.

## 14. Q1-Q1 segment resumption and WP-S23-3 prerequisite

`SegmentResumption` stages a new authority-required source unit, closes, reopens, and
completes without re-staging. Direct native inspection proves segment JSON contains
the mandatory marker and excludes both occurrences and the full envelope. Published
graph units are still available, preserving supported graph behavior; both raw native
and verified occurrence queries refuse missing authority rather than reporting a
verified complete-empty result.

This is conservative containment, not segment repair. The complete envelope is durable
only at completed publication. **WP-S23-3 must propagate the internal authority and
mandatory marker through its batch/sink handoff and explicitly re-stage/reverify after
a store-lifetime interruption, or obtain separately authorized segment durability work.**
It must not rely on graph-only segment resumption for occurrence/provenance correctness.
Q1-Q1 remains binding and is not silently broadened or closed.

## 15. D3/D4/D5 and production surface inventory

D3: no occurrence facts or full authority in legacy graph unit/segment JSON; no new
visible occurrence graph nodes/edges. Only the mandatory boolean marker is additive.
D4: same transaction, publication binding, serialized write gate, rollback/recovery,
replacement/deletion and target page/result ordering/limits. Context refusal occurs
before target-page access; a changed universe does not trigger an index fallback scan.
D5: existing native/occurrence versions and namespace, additive JSON fields only;
missing/unsupported authority never silently upgrades to verified semantic trust.

| Production surface | Change |
| --- | --- |
| `Andirit/CodeOccurrenceAuthority.cs` | Internal receipt/envelope records and verifier contract |
| `Andirit/CodeGraphStoreModels.cs` | Internal ignored envelope and serialized mandatory marker |
| `Andirit/CodeOccurrenceReader.cs` | Internal query-scoped current verifier; public constructor/signature unchanged |
| `Andirit/AssemblyInfo.cs` | Friend access for C# implementation and its tests |
| `Andirit.CSharp/VerifiedCSharpAnalysis.cs` | Receipt only after the existing verified mint succeeds |
| `Andirit.CSharp/VerifiedCSharpOccurrenceExtractor.cs` | Unit/member/fact receipt collection with exact-source membership |
| `Andirit.CSharp/VerifiedUsageMint.cs` | Optional receipt results and controlled native-unit preparation |
| `Andirit.CSharp/VerifiedCSharpOccurrenceAuthority.cs` | Independently verified current-context comparison |
| `Andirit.LatticeDb/LatticeCodeGraphStore.cs` | Additive persistence, committed snapshot, restore/query hooks |
| `Andirit.LatticeDb/LatticeCodeGraphStore.OccurrenceAuthority.cs` | Version/ownership/publication checks and current gate |
| `Andirit.LatticeDb/AssemblyInfo.cs` | Native integration test friend access only |

Affected suites: core model/store consumers, C# mint/extraction/WP7/WP8, native
persistence/recovery/conformance, and the dedicated process harness. Existing producer
and reviewer test sources are untouched; only new test files and the C# test project's
native project reference are added. No plugin/batch/sink production wiring is changed.

## 16. Regression results and environment

Windows x64 (`10.0.26200`), SDK `10.0.401`, runtime `10.0.12`, Roslyn `5.9.0.0`.

| Verification | Actual result / evidence log |
| --- | --- |
| New WP-S23-2 tests | **32 / 0 / 0**; `wp-s23-2-tests.log`, `trx/wp-s23-2.trx` |
| Native/LatticeDB conformance + recovery | **50 / 0 / 0**; `native-suite.log` |
| Independent NEW-01 probes | **17 / 0 / 0**; `new01-independent-probes.log` |
| WP7 stale-context/identity tests | **12 / 0 / 0**; `wp7-stale-context.log` |
| WP8 corpus integration | **9 / 0 / 0**; `wp8-corpus.log` |
| Whole solution | **828 / 0 / 0** = native 50 + TreeSitter 157 + core 345 + CSharp 276; `solution-suite.log` |
| Fresh native processes | **7 expected outcomes**, exact stored-record relationships; `process-*.log/json` |
| Frozen R2 oracle | **185/185**, zero failures; `oracle-r2.log/json` |
| Solution format verification | **exit 0**; `format-verify.log` |
| Process-probe format verification | **exit 0**; `process-format-verify.log` |
| Release build | **0 warnings, 0 errors**; `build-release.log` |

Reproduce after dependency restore with:

```text
python docs/reviews/CI1A-S2.3-WP-S23-2-tests/run-verification.py
```

The driver records explicit dotnet arguments, all raw stdout/stderr, and a fresh
process corpus/database lifecycle using a temporary database (no checkout/worktree).
The harness is not a second production implementation; it links the same controlled
test fixture and frozen authority. Log/JSON/TXT/TRX evidence uses `-text` attributes
and a dedicated `!**` ignore override so exact evidence bytes are tracked.

## 17. Frozen identity and source integrity

Unchanged: all `compctx1`/`compctx2`/`asmctx3`/`usage4` formulas, verified compilation
construction, accepted metadata capture, coverage `Merge`, frozen corpus/oracle,
planning/decisions, NEW-01 reviewer source/evidence and owner record. `source-integrity.json`
records unchanged protected paths and unchanged NEW-01 rebuild method body. All accepted
authority input hashes were rechecked after verification.

Observed R2 verification output SHA-256 over exact bytes:
`97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9`.
Frozen expected input SHA-256 over exact bytes:
`274A7AAE9EB6724879D619416E449C52759B44085D7B718DAD3434B15387CBBF`.
Their distinction remains the accepted one: the observed report appends nine passing
frozen-section comparison checks to the shared 176 and has summary 185/185; the
frozen expected file remains 176/176. No identity or expected content was revised.

## 18. Evidence manifest and limitations

`manifest.json` is deterministic by sorted relative path, exact byte length and
SHA-256, excluding itself. Git binds this report and implementation; the manifest
binds all evidence files, including authority inputs, raw process snapshots, TRX,
source bindings, verification commands/results, logs and this pre-edit carrier contract.
The evidence supports the Windows production-capture fixture profile only. No
Linux/macOS, universal cross-profile identity equality, arbitrary repository or
all-category extraction guarantee is inferred.

Current authority is explicitly supplied by an internal caller from verified live
compilations; it is not autonomous filesystem freshness detection. Legacy raw storage
queries are not semantic authority; the new gated path never promotes missing legacy
provenance. S-01 caller-supplied components, G1/F1 metadata-equivalence qualifications,
coverage/refusal limitations, Q1-Q1 and Q1-Q2 all remain explicit.

Validation checks the complete analyzed unit set conservatively before successful
repository coverage. It adds per-unit/per-fact validation work on valid queries; no
performance optimization, latency guarantee or new reader continuation is claimed.
The target index still controls hit ordering, result bounds, and `Examined` counts.

## 19. Independent qualification handoff

Highest-value falsification targets:

1. Promote persisted `compctx2`, `asmctx3`, or `usage4` into current authority without verified live input.
2. Bypass stale refusal with the unchanged Lib target after source-only or options-only transitive changes.
3. Substitute caller context for the defining context, or substitute a descriptor-equal live instance.
4. Conflate two defining contexts under one unit or the same target across units/publications.
5. Bypass missing/corrupt/unsupported carrier and native owner/publication checks.
6. Accept complete-empty without independently verified source/scope/universe evidence.
7. Reject every reopened fact rather than accepting both current A and fresh current B.
8. Return stale/unverified evidence as zero-hit `Available`.
9. Retain orphan or unrelated receipts after replacement/deletion.
10. Mix publication identities or expose uncommitted envelope/fact state on failure.
11. Promote Q1-Q1 graph-only resumption into verified occurrence authority.
12. Introduce a reachable Q1-Q2 carrier reconstruction failure or stronger quarantine claim.
13. Change D3/D4/D5 or frozen formulas, or overclaim S-01/platform/performance scope.

## 20. Stop and successor boundary

**IMPLEMENTED — READY FOR INDEPENDENT QUALIFICATION.** Independent WP-S23-2-Q1
(or the planned S23-DUR-Q1 gate) and an explicit owner decision remain required.
This implementation report grants no acceptance and starts no successor package.
WP-S23-3 publication integration and its Q1-Q1 prerequisites remain separately
commissioned. No branches, linked worktrees, secondary checkouts, history rewrite,
push, package publication, public authority API, migration, or general recovery work.
