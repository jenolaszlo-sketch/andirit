# Stage 2 Acceptance Matrix — ADR-CI1-01-CI1A-IMPL-01-S2

**Authority:** `docs/decisions/adr-ci1-01-ci1a-s1-obligation-reconciliation.md`  
**Status:** Specification input. **No implementation authorized.**  
**Parent:** `docs/planning/adr-ci1-01-ci1a-impl-01/stage2-boundary.md`

This matrix binds each Stage 2 requirement to a required observable behavior, a positive test, a negative/adversarial test, an environment, and the required independent review stage.

---

## 1. S-01 Source-Byte Provenance (Prerequisite)

| # | Requirement | Observable Behavior | Positive Test | Negative/Adversarial Test | Env | Independent Stage |
|---|-------------|---------------------|---------------|---------------------------|-----|-------------------|
| 1 | Descriptor hashes actual indexed source bytes | `compctx1` source hash matches bytes fed to Roslyn | Changed source bytes → new `compctx1`/`asmctx3`/`usage4` | Supplied bytes ≠ indexed bytes → refusal | native (real Roslyn) | S2.1 implementation; S2.5 independent acceptance |
| 2 | Compilation options/references in context | `compctx1` includes options + reference identities | Changed options → new `compctx1` | Missing reference → refusal | native | S2.1 implementation; S2.5 independent acceptance |
| 3 | Semantic binding against verified compilation | Roslyn binding uses the exact compilation from `compctx1` | Cross-project target binds to dependency's verified compilation | Target in missing compilation → refusal | native | S2.1/S2.2 implementation; S2.5 independent acceptance |

---

## 2. Semantic Identity (usage4 / asmctx3)

| # | Requirement | Observable Behavior | Positive Test | Negative/Adversarial Test | Env | Independent Stage |
|---|-------------|---------------------|---------------|---------------------------|-----|-------------------|
| 1 | `usage4`: repository, plugin, R2 semantics version, semantic source unit, path, source hash, start line, start column, end line, end column, target document, `asmctx3`, category (exact order) | Exact R2 tuple/encoding → same `CodeOccurrenceId`; site content hash is payload evidence, not an identity field | Frozen R2 vectors independently recompute; same semantic fact via two storage contributors retains ID | Change plugin, semantic unit, source hash, target doc, span or defining context → different ID; forged ID refused | native + independent identity oracle | REM-01 independently verified; S2.2 semantic minting/S2.5 acceptance |
| 2 | `asmctx3`: `asmctx3-context`, `compctx1`, canonical context value, metadata identity, module name, identifier digest, full-declaration digest, shape digest | `asmctx3:sha256:` + accepted digest; exact live defining-compilation/declaration correspondence required before minting | Frozen components and valid cross-project defining context independently reproduce target identity | Different defining context/component → different target; unavailable/external/unbound context → refusal, no fact | native + independent identity oracle | REM-01 structural helper verified; S2.2 live minting/S2.5 acceptance |
| 3 | Storage contributor unit excluded; plugin and semantic source unit included in `usage4` | Identical semantic fact via two storage units → same ID, unioned contributors | Same semantic source unit/site through storage units A/B → 1 fact, 2 contributors | Different semantic source units or plugins → distinct facts | native | S2.2/S2.3 implementation; S2.5 acceptance |
| 4 | Payload hash detects conflict | Same ID, different immutable payload → rejection | Conflicting payload → `CodeGraphBatchRejectedException` | — | native | S2 implementation |

---

**OWNER-01 / NEW-02 identity correction (2026-11-02).** Frozen authority is R2
`PROVENANCE-CONTRACT.md` §§4–5 and Q3 `TARGET-IDENTITY-REVIEW.md`, incorporated
by owner §F; production mapping and vectors are in
[`identity-contract.md`](../adr-ci1-01-ci1a-s2-rem-01/identity-contract.md).
`usage4` starts with domain `usage4`; semantics string is
`usage4-b2c-exp-r2-20261101`; category is `method-invocation`. Each UTF-8 part
has a 32-bit big-endian byte-length prefix; coordinates are invariant decimal
strings. SHA-256 yields 64 lowercase hex with no occurrence prefix.
`asmctx3` uses the same encoding; component domains are `asmctx3-identifier`,
`asmctx3-declarations`, `asmctx3-target`. Test both frozen usage vectors and
every tuple/component discriminator using an oracle independent of the helper.
Source-unit identity and storage-contributor identity must not be conflated.

## 3. Cross-Project Binding

| # | Requirement | Observable Behavior | Positive Test | Negative/Adversarial Test | Env | Independent Stage |
|---|-------------|---------------------|---------------|---------------------------|-----|-------------------|
| 1 | Target in dependency binds to dependency's verified compilation | `asmctx3` constructed from dependency's `compctx1` | Project A calls Project B method → target ID uses Project B's `compctx1` | Dependency outside verified universe → no fact; `UnsupportedExternalEvidence`/`External` refusal, or `MissingDefiningCompilation` when defining evidence is missing | native | S2 independent review |
| 2 | Calling unit owns occurrence; defining unit owns target | `SourceUnitId` = caller; `TargetId` = callee's compilation | Project A unit has fact with Project B target | — | native | S2 implementation |

---

## 4. Roslyn Occurrence Extraction

| # | Requirement | Observable Behavior | Positive Test | Negative/Adversarial Test | Env | Independent Stage |
|---|-------------|---------------------|---------------|---------------------------|-----|-------------------|
| 1 | Supported category: Invocation | Ordinary method invocations extracted as `CodeOccurrenceFact` | ≥ 50 invocations in fixture → facts emitted | Constructor/property/local-function → coverage `Unsupported` | native | S2 implementation |
| 2 | Unsupported categories recorded in coverage | Non-invocation usage kinds → coverage, not facts | Property access → `UnsupportedCategories++` | — | native | S2 implementation |
| 3 | Exact invocation site recorded | `span` = identifier-token span; `declarationSpan` = full declaration | Position matches Roslyn syntax tree | Wrong span → test failure | native | S2 implementation |
| 4 | Source hash = actual indexed bytes | `SourceHash` matches file bytes at extraction time | Changed file → new `SourceHash` | — | native | S2 implementation |

---

## 5. Conservative Refusal and Coverage (IF-03)

| # | Requirement | Observable Behavior | Positive Test | Negative/Adversarial Test | Env | Independent Stage |
|---|-------------|---------------------|---------------|---------------------------|-----|-------------------|
| 1 | `Confirmed` coverage | Fully verified occurrence → fact + `Confirmed` | Valid invocation in complete scope → fact with `Confirmed` | — | native | S2 implementation |
| 2 | `External` refusal | Target is external or the defining unit is outside the declared universe | Dependency outside universe → no fact; `External`/`UnsupportedExternalEvidence` refusal | — | native | S2.2 implementation; S2.5 independent acceptance |
| 3 | `Ambiguous` refusal | Multiple live defining bindings → refusal | Two live bindings without a unique defining compilation → `CodeGraphBatchRejectedException`/refusal | — | native | S2.2 implementation; S2.5 independent acceptance |
| 4 | `Unresolved` refusal | Missing defining compilation/universe | Defining compilation not in universe → refusal | — | native | S2.2 implementation; S2.5 independent acceptance |
| 5 | `Unsupported` coverage | Unsupported target kind (ctor, property, etc.) | Constructor call → `UnsupportedCategories++`, no fact | — | native | S2.2 implementation; S2.5 independent acceptance |
| 6 | `ConditionalInactive` coverage | Conditionally compiled out under the declared configuration | `#if DEBUG` method not compiled → `ConditionalInactive`, no confirmed fact | — | native | S2.2 implementation; S2.5 independent acceptance |
| 7 | `Incomplete` coverage | Declared scope not fully analyzed | One declared unit missing → `IncompleteAnalysis`, no complete-empty | — | native | S2.2/S2.4 implementation; S2.5 independent acceptance |
| 8 | Complete-empty | Complete declared scope with zero facts | Complete coverage, target absent → `Available` with zero facts and complete coverage | Incomplete scope → not `Available` | native | S2.3/S2.4 implementation; S2.5 independent acceptance |

---

## 6. Source Ownership and Lifecycle

| # | Requirement | Observable Behavior | Positive Test | Negative/Adversarial Test | Env | Independent Stage |
|---|-------------|---------------------|---------------|---------------------------|-----|-------------------|
| 1 | Calling unit owns occurrence | `SourceUnitId` = caller's project unit | Project A unit has facts for its invocations | Project B's facts not in Project A unit | native | S2 implementation |
| 2 | Replacement retires caller's facts | Source unit replacement → old facts gone, new facts visible | Change invocation site in Project A → old fact gone, new fact visible | Project B facts unchanged | native | S2 implementation |
| 3 | Deletion removes caller's contributions | Source unit deletion → its facts removed; others preserved | Delete Project A unit → its facts gone; Project B facts remain | — | native | S2 implementation |
| 4 | Contributor union only for identical semantic facts | Same semantic occurrence contributed by two storage units → 1 fact, 2 contributors | Distinct semantic projects/sites → distinct `usage4` facts; same semantic site via two storage units → 1 fact, 2 contributors | Two different semantic units at the same span/target → 2 facts | native | S2 implementation |
| 5 | Conflict rejection preserves state | Conflicting payload for same ID → rejection, prior state intact | Stage 2 equivalent of N4 | — | native | S2 implementation |

---

## 7. Native Publication and Persistence

| # | Requirement | Observable Behavior | Positive Test | Negative/Adversarial Test | Env | Independent Stage |
|---|-------------|---------------------|---------------|---------------------------|-----|-------------------|
| 1 | Same transaction as graph | `CompleteIndexRunAsync` writes graph + occurrences atomically | N1 equivalent with real Roslyn facts | Partial failure → rollback | native | S2 independent review |
| 2 | `AndiritOccurrenceUnit` per source unit | Unit's facts persisted under its `(plugin, unit)` key | Native DB has `AndiritOccurrenceUnit` for each source unit | — | native | S2 independent review |
| 3 | `AndiritOccurrencePage` per target | Target page rebuilt from all units' facts | Page contains ordered IDs from all contributors | Stale page entry from deleted unit → gone after rebuild | native | S2 independent review |
| 4 | `AndiritOccurrenceMeta` binds publication | Meta records `PublicationIndexRunId`, schema v1, namespace | Meta matches graph `CodeRepositoryIndexState` | Page/publication mismatch → refusal on open | native | S2 independent review |
| 5 | Reopen reconstructs from units | `RestoreOccurrences` reads units → rebuilds index | N5 equivalent with real facts | — | native | S2 independent review |

---

## 8. D3 Legacy Graph Noninterference (Real Project)

| # | Requirement | Observable Behavior | Positive Test | Negative/Adversarial Test | Env | Independent Stage |
|---|-------------|---------------------|---------------|---------------------------|-----|-------------------|
| 1 | Graph node/edge identities unchanged | `GetPublishedUnitsAsync` → same nodes/edges before/after occurrence publication | Baseline → occurrence pub → identical | Occurrence facts not in graph | native | S2 independent review |
| 2 | `FindCallers`/`FindReferences` unchanged | Same results before/after | Traversal identical | Occurrence not returned | native | S2 independent review |
| 3 | Native traversal unchanged | `TraverseAsync` (native mirror if enabled) identical | Unfiltered traversal identical | Occurrence kinds not in mirror | native | S2 independent review |
| 4 | Graph counts unchanged | Node/edge totals identical | Counts match baseline | Occurrence ingestion doesn't change totals | native | S2 independent review |

---

## 9. D4 Invariants (Stage 2 Applicable)

| Invariant | Stage 2 Requirement | Positive Test | Negative/Adversarial Test | Env | Independent Stage |
|-----------|---------------------|---------------|---------------------------|-----|-------------------|
| 1: Accepted generation only | Queries use only current publication | Query with stale handle → `StalePublication` | — | native | S2 independent review |
| 2: Stale index refused | Schema/availability checks enforced | Graph-only → `AnalysisUnavailable`; stale → `StalePublication` | — | native | S2 independent review |
| 3: No unbounded scan | `Examined` = target-index entries read, never unrelated repository facts | Query large repo → `Examined` covers the target page only | — | native | S2 independent review |
| 4: Failed staging preserves prior publication and corresponding projection | NEW-01 reproduction with real facts; assert graph AND occurrence availability, coverage, publication binding and conflict validation before reopen, then compare durable authority and reopen | Known pre-commit/apply failure preserves prior facts and successful recovered availability; post-recovery cross-unit conflict still refused | Unknown-outcome quarantine remains distinct; no reopen-only assertion may satisfy this gate | native | NEW-01 independently qualified before S2.3/recovery reliance; S2.5 integrated evidence |
| 5: Cancelled staging exposes no partial data | Stage 1 cancellation behavior preserved | Dedicated native cancel-path test | — | native | Stage 6 (explicitly deferred; not counted as exercised) |
| 6: Superseded cannot replace newer | Baseline check inherited | Superseded run rejected | — | native | S2 independent review |
| 7: Reopen equivalent index | N5 equivalent with real facts | Reopen → byte-equal query results | — | native | S2 independent review |
| 8: Contributor/deletion consistent | N4 equivalent with real facts | Union/deletion/conflict correct | — | native | S2 independent review |
| 9: Deterministic order + limit | N2 equivalent with real facts | Order stable; limit enforced | — | native | S2 independent review |
| 10: Concurrency model | Serialization declared; no false snapshot claim | Queries serialize behind pub | Concurrent stress test (if MVCC added) | native | S2 independent review |

---

## 10. Schema and Refusal (D5)

| # | Requirement | Observable Behavior | Positive Test | Negative/Adversarial Test | Env | Independent Stage |
|---|-------------|---------------------|---------------|---------------------------|-----|-------------------|
| 1 | Graph-only schema 2 opens normally | Legacy graph readable | `Andirit.CSharp.Tests` pass | — | native | S2 independent review |
| 2 | Graph-only occurrence query → `AnalysisUnavailable` | No occurrence facts → explicit refusal | Query graph-only repo → `AnalysisUnavailable` | — | native | S2 independent review |
| 3 | Unsupported occurrence schema refused | Version/namespace mismatch → `CodeOccurrenceSchemaException` on open | Tamper meta version/namespace → exception | — | native | S2 independent review |
| 4 | Page/publication mismatch refused | `CodeOccurrenceIndexException` on open | Tamper page run ID → exception | — | native | S2 independent review |
| 5 | Stale query refused | `StalePublication` with explicit reason | Query with old publication handle → `StalePublication` | — | native | S2 independent review |
| 6 | No migration/dual-write | No code reads old experimental schemas | Search for `occurrence-facts/[1-3]` → none | — | native | S2 independent review |

---

## 11. Performance and Operational

| # | Requirement | Observable Behavior | Evidence Required | Env | Independent Stage |
|---|-------------|---------------------|-------------------|-----|-------------------|
| 1 | Real-project publication cost measured | Publication time recorded for fixture | Measurement log | native | S2 independent review |
| 2 | Real-project reopen cost measured | Reopen time recorded | Measurement log | native | S2 independent review |
| 3 | Query latency bounded | `Examined` = target-index entries read; query time recorded | Measurement log | native | S2 independent review |
| 4 | Cross-platform native tests pass | Linux CI executes `Andirit.LatticeDb.Tests` | CI logs | Linux CI | S2 independent review |

---

## 12. S-02 / D4 Full Native Matrix (Deferred)

| Invariant | Stage 2 Status | Stage 6 Requirement |
|-----------|----------------|---------------------|
| 1-4, 6-9 | Exercised native (above) | Complete native matrix |
| 5: Cancelled staging | Explicitly deferred to Stage 6 | Cancel path test native |
| 10: Snapshot contract | Serialized freshness declared; native schedule test is a Stage 2 acceptance item | True concurrent qualification |

---

## 13. Historical Fixture (SQ-04)

| Requirement | Observable Behavior | Evidence Required |
|-------------|---------------------|-------------------|
| Immutable fixture master generated | Prior-writer databases for `occurrence-facts/1..3`, `usage1..3`, `asmctx1/2` | Master hashes recorded |
| Disposable copy procedure | Tests use disposable copies with per-instance hashes | Procedure documented + hashes |
| Refusal tests execute against fixtures | Open/import/query old formats → refusal | Test results against historical fixtures |

**Timing and availability.** Authentic prior-writer formats are Stage 2
acceptance evidence. A format with no authentic prior writer is recorded as an
explicit limitation; fresh-store tampering is not a substitute for an authentic
prior-writer gate, and infeasibility returns to the owner for an amendment.

---

## 14. Cross-Platform (SQ-05)

| Requirement | Observable Behavior | Evidence Required |
|-------------|---------------------|-------------------|
| Linux CI executes native tests | Entire current expanded `Andirit.LatticeDb.Tests` suite passes on Linux; retain the original 38 Stage 1 tests plus additions (REM-01 reviewed 43 on Windows) | CI pipeline logs, before S2.5 acceptance |
| Platform-specific behavior documented | File locking, WAL, memory mapping differences noted | Behavior matrix |

**Scope.** The Linux native suite is a Stage 2 acceptance gate. Full
Windows/Linux/macOS certification remains a Stage 6 obligation.

---

## 15. Namespace Mismatch Test (SQ-06)

| Requirement | Observable Behavior | Evidence Required |
|-------------|---------------------|-------------------|
| New test `Store_RejectsUnsupportedOccurrenceNamespaceOnOpen` | Tamper `meta.Namespace` → `CodeOccurrenceSchemaException` | Test passing in `Andirit.LatticeDb.Tests` |

---

## 16. Gate Summary

| Gate | Requirements | Blocking Stage 2 Auth? | Blocking Stage 2 Accept? |
|------|--------------|------------------------|---------------------------|
| S-01 Provenance | 3 | **Yes** (contract fixed by REM-01) | **Yes** (independent evidence) |
| IF-03 Coverage | 8 | **Yes** (contract fixed by REM-01) | **Yes** (production coverage/reopen evidence) |
| Semantic Identity | 4 | No | **Yes** |
| Cross-Project | 2 | No | **Yes** |
| Roslyn Extraction | 4 | No | **Yes** |
| Refusal/Coverage | 6 | No | **Yes** |
| Source Ownership | 5 | No | **Yes** |
| Native Publication | 5 | No | **Yes** |
| D3 Real Project | 4 | No | **Yes** |
| D4 Invariants | 10 | No | **Yes** |
| Schema/D5 | 6 | No | **Yes** |
| SQ-02/03 Concurrency | 2 | **Yes** (policy/model fixed by REM-01) | **Yes** (native schedule; unknown-outcome handling) |
| Historical Fixture | 3 | No | **Yes** |
| Cross-Platform | 2 | No | **Yes** |
| Namespace Test | 1 | No | **Yes** |
| Performance | 4 | No | **Yes** |

**All requirements must pass independent Stage 2 review before Stage 2 owner acceptance.**