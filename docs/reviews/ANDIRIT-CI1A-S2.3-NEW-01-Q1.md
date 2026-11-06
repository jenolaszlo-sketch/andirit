# ANDIRIT-CI1A-S2.3-NEW-01-Q1 — Independent qualification of the NEW-01 recovery-projection remediation

**Commission:** `ANDIRIT-CI1A-S2.3-NEW-01-Q1` (independent adversarial qualification).
**Repository:** Andirit. **Canonical branch:** `main`. **Date:** 2026-11-06.
**Starting HEAD (verified):** `cecb9a88` (tree `0d2ded15935a71ee7c45975a5d0bdfa89e1ae525`), clean, one worktree, no stashes, **22 ahead / 0 behind**, nothing pushed.
**Under review:** implementation `c75c41f` (entry owner `311e97f`; producer report `9a9c502`; producer evidence `cecb9a8`).
**Authoritative subject:** `docs/planning/adr-ci1-01-ci1a-s2-3/new-01-recovery-remediation.md` (required properties §3, scope §4, prohibited operations §6).
**Platform:** Windows x64, .NET SDK `10.0.401`, runtime `10.0.12`.

**Overall disposition: `SUPPORTED WITH QUALIFICATIONS`.**

All eight required remediation properties (§3 of the entry contract) are independently supported by reviewer-owned probes that derive expected state from durable authority, not from producer output. No blocking defect was found; no production remediation is required. Two boundaries are documented: (Q1-Q1) the D3 durable-occurrence boundary for segment records, and (Q1-Q2) the reconstruction-failure reachability boundary. Owner acceptance is not granted here; nothing was pushed.

---

## 1. Baseline, ancestry, and tree integrity

- Starting HEAD `cecb9a88`, tree `0d2ded15935a71ee7c45975a5d0bdfa89e1ae525`; worktree clean; single worktree; no stashes; `main` 22 ahead / 0 behind; nothing pushed.
- Commit identities re-resolved: entry owner `311e97f`; implementation `c75c41f`; producer report `9a9c502`; producer evidence `cecb9a8`. Parent chain consecutive.
- The producer changed exactly one production file (`src/Andirit.LatticeDb/LatticeCodeGraphStore.cs`) and added one test file (`tests/Andirit.LatticeDb.Tests/New01RecoveryProjectionTests.cs`). `docs/reviews/…-evidence/production-diff.txt` records the exact `311e97f..c75c41f` production diff. No decision, planning contract, frozen corpus, or frozen oracle file was modified by the producer (see §2 and §20).

## 2. Commission scope, prohibitions, and producer-change verification

The commission prohibits production changes, producer-test modifications, frozen-oracle changes, schema changes, WP-S23-2 implementation, worktrees, push, remediation, and owner acceptance. This review:

- added only reviewer files under `docs/reviews/CI1A-S2.3-NEW-01-Q1-tests/` (probe project + probe source) and `docs/reviews/CI1A-S2.3-NEW-01-Q1-evidence/` (evidence + manifest), plus this report;
- changed no production/test/decision/planning file; the frozen expected oracle is byte-identical (`274A7AAE…`, §19);
- performed the historical pre-fix reproduction by `git archive 311e97f` into a temporary directory **outside** the repository (no worktree);
- pushed nothing and grants no acceptance.

**Producer change under review.** `c75c41f` delta (+27/−4, one production file): the two `MutateAsync` recovery catch sites — (a) the in-memory apply/validation failure branch and (b) the pre-commit persist failure branch — now call `await RebuildInnerFromDurableStateAsync()` instead of a graph-only replay. The new private helper performs `ReplayAsync(_commands)` → `RestoreBaselines(_inner)` → `RestoreOccurrences(CancellationToken.None)`, i.e. the same sequence the constructor uses on initial open. The unknown-outcome branch is untouched (no replay; `_outcomeUnknown = true`).

## 3. Independent contract reconstruction and required properties

The entry contract §3 requires, after any replay-based recovery:

1. graph **and** occurrence projection (`_occurrences`, `_occurrenceCoverage`, availability, occurrence target index) restored to durable authority;
2. publication identity continuity (recovered availability/publication binding equals the durable accepted publication);
3. coverage and refusal restoration (`Produced`/`Partial`/`NotProduced`/`Unavailable`, refusal counters, complete-empty);
4. cross-unit contributor and conflict validation effective after recovery (no bypass);
5. correct successful-recovery availability (no false `AnalysisUnavailable`);
6. unknown-outcome quarantine preserved;
7. native close/reopen correspondence;
8. D3/D4/D5 preserved (no occurrences as legacy graph nodes/edges; no authority/schema/migration change; `CodeOccurrenceCoverage.Merge` and frozen formulas unchanged).

The reviewer derived expected states from these obligations and from durable native rows decoded independently; it did **not** adopt the producer's before/after observations as ground truth.

## 4. Probe design, independence, and fault mechanics

**Project.** `docs/reviews/CI1A-S2.3-NEW-01-Q1-tests/ReviewerTests.csproj` — `AssemblyName=Andirit.LatticeDb.Tests` reuses the existing `InternalsVisibleTo`, so probes can construct the internal fault-injecting constructor `new LatticeCodeGraphStore(path, options, Action<string> faultInjector)` without touching any `AssemblyInfo`. It references `src/Andirit.LatticeDb` and NuGet `LatticeDbSharp 0.2.0` for **independent** native row reads.

**Fault mechanics (verified in source).** The injector fires at `before-commit` (inside `Persist`, after the switch, before `txn.Commit()` → rollback, original exception rethrown after a recovery rebuild — Branch B) and `after-commit` (after `txn.Commit()` → wrapped as `CodeGraphCommitOutcomeUnknownException` → quarantine, no replay). The reviewer's injected exception is deliberately not a `LatticeDbSharp.LatticeException`, so its identity is preserved through the rethrow and the recovery-branch rethrow identity is provable.

**LatticeDB exclusivity.** `LatticeDatabase.Open` grants single-owner access; therefore every durable read (`Q1.Rows`, `Q1.Snapshot`, mirror/manifest decode) is performed in a window where the store is closed. Probe lifetimes are phase-separated: setup → closed capture → faulted store → closed decode/compare → fresh reopen. Segment (`HetuStage`) rows are excluded from the failed-publication byte comparison because staging is itself a successful durable mutation; the protected labels (`ProtectedLabels`) are compared byte-for-byte.

**Seventeen probes** (`P01`–`P17`) cover: both failure branches; complete/run/stage-replace failure points; complete-empty/Partial/graph-only availability; refusal counters; contributor union; conflict; repeated recovery; child-target staleness; baselines and index state; native mirror + traversal + staged deletion; tamper/schema fail-closed; known-vs-unknown; no-fault control.

## 5. Branch-B recovery evidence (complete, run, stage-replace failures)

| Probe | Trigger | Key result |
| --- | --- | --- |
| P01 | pre-commit failure on `complete` | recovered readers equal durable oracle (2-unit publication + prior snapshot); staged contribution invisible; durable protected labels byte-identical; D3 proof |
| P04 | pre-commit failure on `run` | failed run absent (no run/baseline rows); retry restores the ordering baseline (`run:p1`); same-Owner replacement retires PriorA/PriorB → `NewC` |
| P05 | pre-commit failure on `stage-replace` | failed stage writes nothing durable; the previously staged segment row is unchanged; a valid later publication carries the re-staged facts |
| P06 | pre-commit failure on `complete` with a staged deletion | deletion not applied on failure; retry applies it; availability mapping per REM-01 §3 |
| P07 | pre-commit failure on `complete` (contributor union) | staged union contributions not applied early; retry yields one indexed entry with sorted contributors `[unit:cu1, unit:cu2]` |
| P08 | two consecutive pre-commit failures | reader and durable protected labels stable through both; one retry publishes once |
| P09 | four repositories, one store, four staged runs | complete-empty / Partial / graph-only / refusal-counter matrix after recovery |
| P10 | chained Branch-A → Branch-B → Branch-A → success | each failed mutation leaves durable bytes unchanged; reopen parity |
| P14 | pre-commit failure on an empty staged `complete` | running run keeps its `run:p1` baseline row; index state stays `run:p1` until the retry |

For every `complete`-failure probe the recovered in-place reader equals the pre-failure reader and a fresh reopen; the failed publication produced no durable effect on the protected labels.

## 6. Branch-A recovery evidence (in-memory validation failures)

| Probe | Trigger | Key result |
| --- | --- | --- |
| P02 | same-identity/different-`ContentHash` occurrence conflict | `occurrence.payload.conflict` refusal; durable bytes unchanged; valid contribution still publishes; a second conflict is still refused after recovery |
| P03 | staged edge to a missing endpoint | `graph.edge.endpoint-missing` refusal; traversals and durable rows unchanged; a repaired replacement publishes |
| P10 | conflict inside a chained sequence | refusal effective both before and after a Branch-B recovery |
| P12(a) | native store, edge to a missing endpoint | mirror and projection unchanged; repaired replacement publishes |

Branch-A recovery rebuilds the projection from the durable command log, so the recovered instance re-establishes the published occurrence set that drives cross-unit conflict validation; the refusal is therefore effective after recovery (property 4).

## 7. Durable-occurrence oracle correspondence (field-by-field)

P01 publishes a two-unit base publication plus a prior snapshot for a second repository, starts a run, stages an occurrence-bearing replacement, and fails the `complete` at `before-commit`. The recovered in-place reader is compared with the pre-failure reader and with a fresh reopen, and the durable occurrence rows are then decoded with the reviewer's own parser:

- **Unit rows** (`AndiritOccurrenceUnit`): repository/plugin/unit identity, `AnalysisIncluded`, per-fact fields (`Id`, `TargetId`, `TargetDocumentId`, `RepositoryId`, `Category`, `SourceUnitId`, `Contributor.PluginId`/`IndexUnitId`, all `Span` fields, `SourceHash`, `ContentHash`, `PluginVersion`, `SemanticsVersion`, `PayloadHash`) and the full coverage record are asserted against the reviewer's own `CodeOccurrenceFact` objects.
- **Schema manifest** (`AndiritOccurrenceMeta`): `SchemaVersion`, `Namespace`, repository, `PublicationIndexRunId`, `IndexIdentity` (recomputed independently from a freshly constructed `CodeRepositoryIndexState`), `SnapshotIdentity`, `IsConsistentSnapshot`, `FactCount`, `UnitCount`, `PageCount`, `AnalysisIncluded`, `CoverageState`, `ScopeId`, `UniverseId`, `CompleteCoverage`.
- **Target pages** (`AndiritOccurrencePage`): one row per published target, `PublicationIndexRunId` binding and ordered `OccurrenceIds`.
- **Baselines**: the single running run keeps `HetuBaseline` key `repo\nrun:p2` → payload `run:p1`.
- **D3 proof**: the persisted graph-unit record (`HetuUnit`) and the staged segment record contain **no** `Occurrences` (and no `OccurrenceAnalysisIncluded`) property; occurrence facts exist only in the additive `AndiritOccurrence*` tables.

Recovered = durable oracle = pre-failure state for every occurrence label and for the reader projection (availability, publication binding, fact identities, coverage, totals).

## 8. Staged-contribution durable boundary (D3) — qualification Q1-Q1

Independent diagnosis (`ZzDiag`, deleted after use; reproduced in P05/P07/P08) established an honest and pre-existing boundary:

- `CodeIndexUnitReplacement.Occurrences` is `[JsonIgnore]` by the accepted D3 design, so a **segment** record never carries occurrence facts. A run whose occurrence-bearing contribution was staged durably and then **resumed in a different store lifetime** publishes the graph unit but contributes **no** occurrence facts, because `PersistOccurrences` rebuilds from the in-memory replaced command, whose occurrence list is empty after reopen.
- This is **not** a NEW-01 regression and **not** a recovery defect: within a single instance, the reviewed recovery branches restore occurrences from the durable tables (properties 1–5 hold). It is the boundary of the additive occurrence receptacle (occurrence facts become durable at publication, not at stage time).
- All occurrence-publishing probes therefore stage and publish within one lifetime (as production does); the byte-invariance of a failed publication is asserted over the protected labels.

Classification: **QUALIFICATION** (documented D3 durable boundary; no remediation required by this commission, which prohibits schema change). Whether this boundary needs its own obligation is an owner decision to be recorded elsewhere; it is not invented here.

## 9. Availability mapping matrix

P09 exercises four repositories within one recovered store (and P06 exercises the retired-target case):

| Case | Availability | Coverage | Complete | Hits |
| --- | --- | --- | --- | --- |
| complete-empty (Produced, zero facts) | `Available` | Produced, `IsComplete` | ✓ | 0 |
| Partial | `IncompleteAnalysis` | Partial | ✗ | present |
| graph-only (no occurrence analysis) | `AnalysisUnavailable` | `null` | ✗ | 0 |
| refusal counters (Partial + counters) | `IncompleteAnalysis` | counters preserved | ✗ | present |
| retired target after applied deletion | `Available` | Produced (remaining scope) | ✓ | 0 |

This matches the accepted REM-01 §3 mapping. **Plan correction / refuted hypothesis:** the reviewer's draft expected `AnalysisUnavailable` for a target whose only contribution was deleted; the accepted mapping and the implemented reader answer `Available` with zero hits because availability is repository-scoped and coverage remains complete. The probe asserts the contract (recorded as an `UNSUPPORTED HYPOTHESIS` refutation in §19).

## 10. Coverage, refusal counters, and complete-empty preservation

- Complete-empty (`Produced`, zero facts) survives recovery as `Available`/complete with zero hits (P01's sibling case, P09 repository A) — it is never downgraded to unavailable or treated as empty-success on a non-analysis.
- Partial coverage is never upgraded to Produced by recovery (P01, P09 repository B/D).
- Refusal counters (`InvocationFacts`, `UnsupportedCategories`, `DeclaredUnitCount`, `VerifiedUnitCount`, `External`, `Ambiguous`, `Unresolved`, `Unsupported`, `ConditionalInactive`, `UnavailableReasons{review:reason:x:2}`) are restored field-by-field and, for the merged reader projection, equal the reviewer's independently constructed expectation (P09 repository D; single-unit and post-retry merged).
- Graph-only repositories expose no fabricated occurrence coverage (P09 repository C: no `AndiritOccurrence*` rows; `Coverage` null; `Publication` non-null).

## 11. Contributor union and conflict after recovery

- **Union (P07):** identical facts (same identity and payload, different storage unit) contributed by two staged units produce exactly one indexed hit whose contributor list is `[unit:cu1, unit:cu2]` (ordered). Durable accounting shows three unit rows and one target page carrying distinct identities only — the segment/occurrence multiplicity does not leak into the index.
- **Conflict (P02, P10):** a same-identity/different-`ContentHash` contribution is refused with `occurrence.payload.conflict` both before and after a recovery; a valid contribution still publishes; the rejection writes nothing durable.
- **Retirement (P04):** a same-Owner replacement retires the obsolete facts (`PriorA`/`PriorB` → `NewC`) exactly once.

## 12. Publication identity, baselines, and index state

- Recovered publication identity equals the durable accepted publication: the prior snapshot stays `run:p1`; a stale `ExpectedPublication` answers `StalePublication` bound to the current (`run:s1`) publication after recovery (P01).
- The running run retains its ordering baseline (`repo\nrun:p2` → `run:p1`); the baseline row is deleted only on a successful completion (P01, P04, P14).
- `HetuIndexState` remains the previous publication until a successful completion; the retried empty completion advances it (P14).

## 13. Native mirror and traversal under recovery

- **P11 (Branch-B, native store):** after the failed `complete`, the native mirror traversal (no evidence filter), the evidence-filtered projection traversal, and the `NativeSnapshot` (graph-node payloads + `HetuMirror` manifest key sets) are byte/identifier-identical to the pre-failure capture — no staged node (`F`) and no staged edges (`F→A`, `C→E`). After the retry, both the mirror and a non-native reopen agree: `F→A` (incoming from `A`) and `C→E` are present, and the `HetuGraphNode`/`HetuMirror` rows contain `node:m:f` and the new edges.
- **P12(b) (native, staged deletion + Branch-B):** mirror, traversal, and occurrence results are unchanged by the failure; the retry removes the deleted unit from both the mirror (`NodeIds=[node:n1a,node:n1b]`, `EdgeIds=[edge:n1ab]`) and the reader (retired target `Available`-zero).
- The mirror is derived and never authoritative; this confirms property 7 (native close/reopen correspondence) and the producer's non-re-run of `RestoreMirrors` is consistent because the failed mutation rolled back.

## 14. Known-versus-unknown outcome boundary (quarantine)

P15 drives an `after-commit` fault: the completed `complete` commits durably, then the injector throws inside the commit scope, producing `CodeGraphCommitOutcomeUnknownException`. The instance then:

- refuses reads (`FindOccurrencesAsync`, `GetPublishedUnitsAsync`, `TraverseAsync`) and mutations (`StoreIndexRunAsync`) with `CodeGraphCommitOutcomeUnknownException` — no replay, no current-state claim;
- still reports schema-level `Healthy` health (`CheckHealthAsync`).

A fresh reopen reconciles: the committed publication (`run:q2`, unit u2) is visible, the instance is not quarantined, and further mutations succeed. This reproduces the `publication-uncertainty.md` outcome model and confirms the unknown-outcome branch is unchanged (property 6).

## 15. Fail-closed tamper and schema contracts

- **P13:** a corrupt (non-base64) occurrence-unit payload fails open with `CodeOccurrenceSchemaException` (`ActualVersion=0`, `ExpectedVersion=1`); a tampered schema `Namespace` fails open with `CodeOccurrenceSchemaException` (`ActualVersion=1`). In both cases the failed open does **not** mutate the tampered rows, and a restored pristine copy opens and answers normally.
- **P16:** `CurrentSchemaVersion == 2`, `CurrentOccurrenceSchemaVersion == 1`, `OccurrenceNamespace == "andirit-occurrence-facts/1"`; a `HetuMetadata` `schemaVersion` tamper (3) fails open with `LatticeCodeGraphSchemaException` (`ActualVersion=3`, `ExpectedVersion=2`); a restored copy opens normally.
- No occurrence or graph schema-format migration is introduced; tampered/foreign occurrence authority is refused rather than reinterpreted.

## 16. Historical pre-fix reproduction (commit `311e97f`)

The reviewer extracted the entry-owner commit `311e97f` with `git archive` into a temporary directory **outside** the repository (no worktree) and ran an independently written slim probe set against that pre-fix source:

| Pre-fix probe | Expected (contract) | Observed pre-fix |
| --- | --- | --- |
| complete-failure occurrence projection | recovered = durable reopen (`Available`, 2 facts) | **FAIL** — recovered `AnalysisUnavailable` vs durable `Available` |
| conflict after recovery | `CodeGraphBatchRejectedException` | **FAIL** — no exception (conflict accepted) |
| unknown-outcome quarantine | `CodeGraphCommitOutcomeUnknownException` | PASS (non-regression control) |

This independently reproduces the exact NEW-01 defect class (graph-only replay loses the occurrence projection; the conflict bypass) at the pre-fix commit and shows the reviewed change closes it. It is reviewer-executed evidence, distinct from source inspection. Log: `prefix-311e97f-probes.log`.

## 17. Reconstruction-failure reachability boundary — qualification Q1-Q2

The entry contract's recommended probes include "failure during reconstruction". The existing fault-injection surface has no fault point inside `RebuildInnerFromDurableStateAsync` (`ReplayAsync` → `RestoreBaselines` → `RestoreOccurrences`) or inside `RestoreOccurrences`; there is therefore **no executable probe** that fails the reconstruction itself. The reviewer therefore handled it by source-order analysis plus tamper-driven open paths (P13/P16), and reports honestly:

- `RebuildInnerFromDurableStateAsync` assigns `_inner = await ReplayAsync(...)` **before** `RestoreBaselines`/`RestoreOccurrences`. If a restore step throws, `_inner` is left as a graph-only (or partially restored) projection and the reconstruction exception propagates in place of the original failure (the `throw;` after the awaited rebuild is not reached). The instance is not quarantined and the durable state is untouched.
- This is a narrow reachability boundary, not reachable through the accepted fault surface and not exercised by the producer tests. Restore paths that **do** fail at open are fail-closed by the accepted schema exceptions (P13, P16), and the recovery rebuild uses the same restore code with `CancellationToken.None`.
- Classification: **QUALIFICATION** (documented boundary). No new quarantine semantics are prescribed, per the commission's prohibition on inventing them.

## 18. Commission questions answered

1. **Were both known-failure recovery branches rebuilt to restore the full projection?** Yes. Both `MutateAsync` catch sites call `RebuildInnerFromDurableStateAsync()` (replay → baselines → occurrences); the unknown-outcome branch is unchanged. (§5, §6.)
2. **Does the recovered instance equal a fresh durable reopen?** Yes for every probe: recovered = pre-failure = reopened for readers, publications, and occurrence rows. (§5, §7.)
3. **Is publication identity continuous across recovery?** Yes; the prior snapshot stays bound, and a stale `ExpectedPublication` is refused as `StalePublication` against the current publication. (§12.)
4. **Are coverage, refusal counters, and complete-empty preserved?** Yes, field-by-field, including `UnavailableReasons` and the complete-empty `Produced`/zero-fact case. (§10.)
5. **Is the deliberate conflict bypass closed?** Yes; `occurrence.payload.conflict` is refused after recovery, and the pre-fix bypass is reproduced and shown closed. (§6, §11, §16.)
6. **Is successful-recovery availability correct (no false `AnalysisUnavailable`)?** Yes; the false-unavailable symptom is gone (recovered `Available` where durable is `Available`). (§5, §7, §16.)
7. **Is the unknown-outcome quarantine preserved?** Yes; reads and mutations are refused, reopen reconciles. (§14.)
8. **Are D3/D4/D5 preserved (no occurrence facts as graph nodes/edges; no schema migration)?** Yes; the graph-unit and segment records exclude occurrences, occurrences live only in the additive tables, and no schema/authority/formula change is present. (§7, §15.)
9. **Is native traversal consistent with recovery?** Yes; native mirror, evidence-filtered projection traversal, and non-native reopen agree before and after recovery, including a staged deletion. (§13.)
10. **Is repeated recovery stable?** Yes; two consecutive failures leave reader and durable protected labels stable, and one retry publishes once. (§5.)
11. **Is a failure during reconstruction reachable/handled?** Not injectable through the accepted fault surface; handled as a documented boundary with source-order analysis and fail-closed open paths. (§17.)
12. **Do tampered/foreign durable records fail closed?** Yes, with `CodeOccurrenceSchemaException` (occurrence) and `LatticeCodeGraphSchemaException` (metadata), without mutating the tampered rows. (§15.)
13. **Is the producer change minimal and authority-preserving?** Yes; one production file (+27/−4), additive test file, no frozen/decision/planning change (`production-diff.txt`). (§2, §20.)
14. **Do regression, frozen oracle, build, and format pass?** Yes: 796/0/0; oracle 185/185 with `97F6C5E5…`; Release build 0/0; `dotnet format --verify-no-changes` exit 0. (§20.)
15. **Any blocking defect or owner decision required?** No blocking defect; two qualifications (Q1-Q1 D3 staged-occurrence durable boundary; Q1-Q2 reconstruction-failure reachability boundary) and one refuted hypothesis; owner acceptance is not granted. (§8, §17, §19.)

## 19. Findings classification and disposition

| Class | Finding |
| --- | --- |
| BLOCKING | None. |
| QUALIFICATION | **Q1-Q1** — the D3 durable-occurrence boundary: occurrence contributions staged durably are restored only within the publishing lifetime because segment records intentionally exclude occurrence facts; a run resumed after a restart publishes graph only. Pre-existing, additive design; recovery-within-instance is unaffected. |
| QUALIFICATION | **Q1-Q2** — reconstruction-failure reachability: no fault point exists inside the recovery rebuild; if a restore step failed it would leave a partially restored `_inner` and mask the original exception. Not reachable through the accepted fault surface; no new quarantine semantics prescribed. |
| DECISION REQUIRED | None raised by the reviewer; whether Q1-Q1 warrants a separate obligation is an owner matter, not a qualification failure. |
| NON-BLOCKING | Corrected reviewer assumption: a retired target answers `Available`-zero under complete repository coverage (not `AnalysisUnavailable`) — matches REM-01 §3. |
| UNSUPPORTED HYPOTHESIS | Refuted: the draft hypothesis that a deleted target's occurrence query returns `AnalysisUnavailable`. |

**Disposition: `SUPPORTED WITH QUALIFICATIONS`.** All eight required remediation properties are supported; no production remediation is required by this commission; owner acceptance is not granted.

## 20. Regression results, frozen oracle, evidence manifest, and reproducibility

Windows x64, SDK `10.0.401`, runtime `10.0.12`.

| Verification | Command | Result |
| --- | --- | --- |
| Reviewer probes (new) | `dotnet test docs/reviews/CI1A-S2.3-NEW-01-Q1-tests/ReviewerTests.csproj -c Release` | **17 / 0 / 0** |
| Pre-fix reproduction | `dotnet test <tmp>/tests/PreFixProbes/PreFixProbes.csproj -c Release` | **2 defect FAILs reproduced, 1 control PASS** |
| Native suite | `dotnet test tests/Andirit.LatticeDb.Tests` | **50 / 0 / 0** |
| Whole solution | `dotnet test Andirit.slnx -c Release` | **796 / 0 / 0** (50 + 157 + 345 + 244) |
| Release build | `dotnet build Andirit.slnx -c Release` | **0 warnings, 0 errors** |
| Formatting gate | `dotnet format Andirit.slnx --verify-no-changes` | **exit 0** |
| Probe formatting | `dotnet format …/ReviewerTests.csproj --verify-no-changes` | **exit 0** |
| Frozen R2 oracle | `dotnet run --project …/oracle/SemanticCorpusOracle.csproj -- …` | **185 / 185** |
| Oracle output hash | SHA-256 | `97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9` |
| Frozen expected hash | SHA-256 | `274A7AAE9EB6724879D619416E449C52759B44085D7B718DAD3434B15387CBBF` |

**Evidence.** `docs/reviews/CI1A-S2.3-NEW-01-Q1-evidence/` contains: `.gitignore` (`!**`), `.gitattributes`, `build-release.log`, `solution-suite.log`, `latticedb-suite.log`, `format-verify.log`, `new01-q1-probes.log`, `prefix-311e97f-probes.log`, `oracle-r2.json`, `oracle-r2.log`, `production-diff.txt`, `verification-summary.txt`, and the self-excluding `manifest.json`.

**Reproducibility.** The probe project runs by path and is not added to `Andirit.slnx` (precedent: prior Q1 reviews); it reuses the accepted `InternalsVisibleTo` via `AssemblyName` and requires no production or `AssemblyInfo` change. The pre-fix reproduction is `git archive 311e97f` into a temporary directory outside the repository; no worktree was created. Nothing was pushed.

**Global boundaries.** Windows-only qualification; broader crash/power-loss/multi-process recovery, MVCC, and cross-restart durability are out of scope (SQ-02/SQ-03/Stage 6); the WP-S23-2 defining-carrier and WP-S23-3 real-fact publication are not implemented; S-01 component-digest canonicalization remains a retained S2.5 obligation. This review grants no acceptance.
