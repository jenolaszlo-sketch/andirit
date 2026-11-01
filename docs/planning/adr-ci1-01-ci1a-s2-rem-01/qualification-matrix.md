# Qualification Matrix — REM-01

**Environment:** Windows 10.0.26200 X64; .NET SDK 10.0.401; Debug; native
provider `LatticeDbSharp` 0.2.0.
**Producer tests, not independent acceptance.** Independent evidence is the
`REM-01-Q1` obligation.

---

## 1. Test commands and results

| Suite | Command | Result |
|---|---|---|
| Managed core | `dotnet test tests/Andirit.Tests/Andirit.Tests.csproj --no-build` | 325 passed, 0 failed, 0 skipped |
| Native LatticeDB | `dotnet test tests/Andirit.LatticeDb.Tests/Andirit.LatticeDb.Tests.csproj --no-build` | 43 passed, 0 failed, 0 skipped |
| C# provider | `dotnet test tests/Andirit.CSharp.Tests/Andirit.CSharp.Tests.csproj --no-build` | 69 passed, 0 failed, 0 skipped |
| Tree-sitter provider | `dotnet test tests/Andirit.TreeSitter.Tests/Andirit.TreeSitter.Tests.csproj --no-build` | 157 passed, 0 failed, 0 skipped |
| Build | `dotnet build Andirit.slnx` | succeeded, 0 warnings, 0 errors |

Total managed/native regression: **594 passed**.

The eight Stage 1 native tests `N1`–`N8` are included in the 43 native passes.
The D3 noninterference test `N8_D3GraphNoninterferenceAcrossOccurrenceLifecycle`
and the D5 schema/namespace tests remain in that suite.

---

## 2. Blocking-finding qualification

| Finding | Test / check | Result |
|---|---|---|
| F01 identity | `Usage4_MatchesTheFrozenR2Vector`; `Usage4_DistinguishesDefiningCompilationContexts`; `Usage4_IncludesSemanticUnitAndSourceHash`; `Target_UsesTheAcceptedAsmctx3ComponentsAndCanonicalFormat` | pass |
| F02 external refusal | `Target_RejectsUnavailableAndFabricatedIdentities`; Stage 2 matrix corrected | pass at production target boundary; Stage 2 extraction remains |
| F03 coverage | `Coverage_DistinguishesNoAnalysisIncompleteAndCompleteEmpty`; `IncompleteCoverageCannotMasqueradeAsCompleteEmpty`; `NoAnalysisWithZeroFactsRemainsUnavailable` | pass |
| F04 zero-fact | `CompleteEmptyAnalysisSurvivesReplacementAndReopen`; `CompleteEmptyCoverage_PropagatesThroughSinkAndQuery` | pass |
| F05 unknown outcome | `AfterCommitAcknowledgementLossRefusesCurrentStateAndReopenSeesDurableState`; existing `N6` | pass for ordinary path |
| F06 gates | Document audit; corrected Stage 2 boundary/matrix/handoff | pass at document level; Q1 review required |

---

## 3. Identity vectors

| Vector | Expected | Source |
|---|---|---|
| `usage4` frozen vector | `bd0487b749795a3e6b61028c7c9cf2d663530cae901ac6d9f35bc311536dc3af` | R2 `identity-matrix-r2.json`, independently recomputed by Q3 |
| Same site, other `asmctx3` | `b874bdc5afa194e0e237d0b1d7d36b3fa3dd767799e0dc62f5f1399f00b90bca` | R2/Q3 |
| Canonical target | `asmctx3:sha256:<64 lowercase hex>` | R2 §4 |
| Canonical occurrence | 64 lowercase hex | R2 §5 / `OccurrenceV3.Validate` |

---

## 4. Zero-fact and coverage lifecycle

Observed in `OccurrenceRemediationNativeTests`:

1. Publish a nonempty analyzed unit.
2. Replace it with a complete-empty unit (`Produced`, zero facts).
3. Query: `Available`, zero facts, complete coverage.
4. Close and reopen: same.
5. Durable labels: one unit, zero pages, one meta.
6. Incomplete coverage: `IncompleteAnalysis`, known hit returned, not complete.
7. No analysis: `AnalysisUnavailable`.
8. Delete the complete unit: `AnalysisUnavailable`.
9. Sync propagation: `CodeGraphIngestionSink` carries complete-empty coverage.

---

## 5. Publication uncertainty

`AfterCommitAcknowledgementLossRefusesCurrentStateAndReopenSeesDurableState`:

- prior publication `P0` exists;
- a post-commit fault occurs while publishing `P1`;
- `CodeGraphCommitOutcomeUnknownException` is raised;
- the instance refuses current-state queries;
- reopening sees durable `P1` (`src/New.cs`).

Known pre-commit failure (`N6`) preserves the prior publication and remains
recoverable. This is deterministic fault injection, not crash/power-loss proof.

---

## 6. D3 / D4 / D5

| Area | Evidence | Status |
|---|---|---|
| D3 | `N8_D3GraphNoninterferenceAcrossOccurrenceLifecycle` | pass in native synthetic corpus |
| D4 | N1–N8, zero-fact/reopen, unknown-outcome, existing supersession/stale tests | pass in applicable tested scope |
| D5 | existing schema/namespace refusal tests; additive coverage JSON; old non-canonical IDs refused | pass in tested scope; authentic historical fixture remains S2 |
| Contributor union/conflict | in-memory + native N4; corrected semantic-unit/storage-unit distinction | pass |

---

## 7. Failing-before note

The pre-REM helper did not accept the R2 tuple inputs, so the frozen-vector test
cannot be run against it without rewriting the test. The failing-before behavior
is established from pinned source (four-field target hash; occurrence hash without
plugin/semantic unit/source hash/target document) and from Q1B, not from a saved
pre-REM test log. This is not claimed as a reproduced historical test failure.

The zero-fact failing-before behavior is established from the pinned sink
predicate (`_occurrences.Count > 0`) and native/in-memory removal paths. The new
test demonstrates the corrected behavior; the pre-REM run is not separately
archived.

---

## 8. Remaining evidence obligations

| Obligation | Owner |
|---|---|
| Full Stage 2 `compctx1`/universe/live binding/extraction | Stage 2 S2.1–S2.2 |
| Conditional-inactive scanner | Stage 2 S2.2 |
| Reader continuation/cross-target surface | Stage 2 S2.4 |
| Qualifying real-project fixture and independent oracle | S2.0/S2.2 |
| Authentic historical fixtures for available writers | S2.5 |
| Linux native suite | S2.5 |
| Crash/power-loss/multi-process recovery | Later CI-1A/Stage 6 |
| Independent REM-01-Q1 requalification | Reviewer |

These are qualifications on Stage 2/later scope, not unresolved original
blocking contract defects.
