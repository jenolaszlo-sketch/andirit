# Evidence — ADR-CI1-01-CI1A-S2-BOUNDARY-REM-01

**Status:** producer evidence for independent `REM-01-Q1` review.
**Starting commit:** `4c6d26a1d103a62fffe2e2743467eedb5f21afc2`
**Starting tree:** `320e965f219d0b615da29665d7fa3e85f8bbfc15`
**Contract commit:** `c13137d54e0ee48c0f7c7383b03bf43bed74a936` (tree
`380104122e96e5a164a2a8375ffdd7f37c513e1f`)
**Implementation commit:** `fc515ffbc20adc286fb57339130fa5ccbf466bb8` (tree
`8c2d7d08be0f7294332d48ec8cb3b2c98476a9fa`)
**Evidence commit:** the commit that adds this document and the Q1 handoff.
**Stage 2:** remains unauthorized.

---

## 1. Scope

REM-01 repairs the six Q1B blocking contract/foundation defects:

1. canonical R2 `usage4`/`asmctx3` production identity;
2. external-target refusal instead of false confirmation;
3. separate resolution, coverage and availability;
4. durable complete-empty analysis;
5. conservative unknown-commit/ack-loss behavior;
6. corrected Stage 2 authorization/acceptance gates.

It does **not** implement the Stage 2 Roslyn extractor, `compctx1` construction,
live universe, semantic binding, public Find Usages or a schema migration.

---

## 2. Changed production surfaces

| Path | Change |
|---|---|
| `src/Andirit.Abstractions/CodeOccurrence.cs` | R2 identity helpers; canonical ID formats; `TargetDocumentId`; plugin version; recomputed payload; coverage/analysis contract |
| `src/Andirit.Abstractions/CodeGraphBatch.cs` | optional occurrence coverage on a batch |
| `src/Andirit/CodeGraphBatchValidator.cs` | canonical target/ID/payload recomputation; contributor ownership clarified |
| `src/Andirit/CodeGraphIngestionSink.cs` | coverage merge and analysis-performed propagation |
| `src/Andirit/CodeGraphStoreModels.cs` | replacement carries coverage; analysis flag independent of fact count |
| `src/Andirit/CodeOccurrenceReader.cs` | `IncompleteAnalysis`; coverage in query result |
| `src/Andirit/InMemoryCodeGraphStore.cs` | durable-in-memory coverage; complete-empty; incomplete/stale mapping |
| `src/Andirit.LatticeDb/LatticeCodeGraphStore.cs` | coverage persistence/restore; zero-fact meta; unknown-commit quarantine; canonical restore |
| `tests/Andirit.Tests/OccurrenceIdentityRemediationTests.cs` | R2 vectors, canonical/enforcement, payload/conflict |
| `tests/Andirit.Tests/OccurrenceInMemoryParityTests.cs` | semantic-unit union/conflict corrections |
| `tests/Andirit.Tests/CodeGraphIngestionSinkTests.cs` | complete-empty sink propagation |
| `tests/Andirit.LatticeDb.Tests/OccurrenceRemediationNativeTests.cs` | zero-fact, incomplete, deletion, ack-loss/reopen |
| `tests/Andirit.LatticeDb.Tests/OccurrenceStage1NativeTests.cs` | canonical test targets and complete coverage |
| `tests/Andirit.LatticeDb.Tests/LatticeCodeGraphStoreTests.cs` | public API snapshot includes unknown-outcome exception |
| `tests/Andirit.LatticeDb.Tests/LatticeDbPublicApiContractTests.cs` | same |
| `tests/Andirit.LatticeDb.Tests/OccurrenceStage1PerformanceProbe.cs` | canonical test targets |

Stage 2 documents changed:

- `docs/planning/adr-ci1-01-ci1a-impl-01/stage2-boundary.md`
- `docs/planning/adr-ci1-01-ci1a-impl-01/stage2-acceptance-matrix.md`
- `docs/planning/adr-ci1-01-ci1a-impl-01/stage2-handoff.md`

New REM-01 documents are under
`docs/planning/adr-ci1-01-ci1a-s2-rem-01/`.

---

## 3. Contract corrections

### Identity

`usage4` now uses the exact R2 tuple, including plugin, R2 semantics version,
semantic source unit, source hash, target document, `asmctx3` and category, with
big-endian length-prefixed parts. `asmctx3` uses
`compctx1` + metadata identity + module + identifier digest + declaration digest
+ shape digest, prefix `asmctx3:sha256:`. `CodeOccurrenceFact` recomputes and
validates the identity; the validator recomputes ID and payload; the constructor
no longer accepts a caller-supplied payload hash.

### External target

Non-canonical targets (`asmctx3:unavailable`, fabricated values) are rejected.
Stage 2 requires refusal for a target whose defining compilation is outside the
verified universe; it no longer requires an `External` fact.

### Coverage

`CodeOccurrenceCoverage` has analysis state, resolution counters, refusal
reasons, `ScopeId`/`UniverseId` and declared/verified counts. The state uses the
existing `CodeRelationshipCoverageState` vocabulary. `IncompleteAnalysis` is a
distinct query availability. Complete coverage with zero facts is `Available`;
partial coverage with zero facts is not a complete negative.

### Zero-fact

A complete-empty unit persists unit/coverage/meta with no page; reopen restores
`Available`/zero. A fact count of zero is not treated as no analysis.

### Unknown outcome

A post-commit fault becomes `CodeGraphCommitOutcomeUnknownException`; the store
quarantines the instance and refuses current-state claims. Reopen reconstructs
durable state. Known pre-commit failure remains recoverable.

---

## 4. Tests and results

Environment: Windows 10.0.26200 X64; .NET 10.0.401; Debug; `LatticeDbSharp`
0.2.0.

| Suite | Command | Result |
|---|---|---|
| Andirit.Tests | `dotnet test tests/Andirit.Tests/Andirit.Tests.csproj --no-build` | 325 passed / 0 failed / 0 skipped |
| Andirit.LatticeDb.Tests | `dotnet test tests/Andirit.LatticeDb.Tests/Andirit.LatticeDb.Tests.csproj --no-build` | 43 passed / 0 failed / 0 skipped |
| Andirit.CSharp.Tests | `dotnet test tests/Andirit.CSharp.Tests/Andirit.CSharp.Tests.csproj --no-build` | 69 passed / 0 failed / 0 skipped |
| Andirit.TreeSitter.Tests | `dotnet test tests/Andirit.TreeSitter.Tests/Andirit.TreeSitter.Tests.csproj --no-build` | 157 passed / 0 failed / 0 skipped |
| Build | `dotnet build Andirit.slnx` | 0 warnings / 0 errors |

Total: 594 passed. N1–N8 are included in the native suite.

Failing-before notes are in `qualification-matrix.md` §7: the old helper cannot
consume the R2 inputs, and the zero-fact pre-REM behavior is established from
pinned source, not a saved failing-before test log.

---

## 5. Remaining qualifications

- Full S-01 `compctx1`/scope/universe/live binding/extraction is Stage 2.
- Conditional-inactive scanning is Stage 2.
- Reader continuation/cross-target surface is Stage 2 S2.4.
- The existing fixture is below Stage 2 minima; no qualifying oracle is claimed.
- Authentic historical fixtures, Linux native execution, crash/power-loss and
  multi-process behavior remain later/S2 acceptance obligations.
- Pre-REM databases contain non-canonical identities and must be rebuilt by
  re-ingestion; they are refused, not migrated.
- Producer tests are not independent acceptance.

## 6. Reviewer instructions

1. Verify the implementation commit(s), tree(s), changed paths and linear
   ancestry against this record.
2. Recompute the frozen R2 vectors independently; do not trust the production
   helper as the oracle.
3. Run the four test projects and N1–N8.
4. Exercise the ack-loss test and reopen behavior.
5. Verify no D3 graph change, no D4 publication-authority change, and no D5
   migration.
6. Review the corrected Stage 2 matrix for preauth/implementation/acceptance
   separation.
7. Do not treat producer test success as independent acceptance.
