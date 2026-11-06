# ANDIRIT-CI1A-S2.3-WP-S23-3A-IMPL-01

## Real-fact publication integration — implementation report

**Commission:** `ANDIRIT-CI1A-S2.3-WP-S23-3A-IMPL-01` (implementation and producer verification).
**Repository:** `D:/projects/Andirit`. **Branch:** `main`. **Date:** 2026-11-07.
**Authorization:** WP-S23-3 entry owner decision; package spec `wp-s23-3-publication-integration.md`.
**Disposition:** **`BLOCKED — OWNER DECISION REQUIRED`.**

The occurrence publication integration itself is implemented and verified. A **pre-existing, order-dependent durable graph-reconstruction defect** blocks the authorized fresh-process reopen deliverable for multi-unit (cross-project) publications. Fixing that defect is outside the authorized occurrence-publication scope, so implementation stops here for an owner decision. No WP-S23-3 acceptance is claimed. This report preserves the partial implementation and its evidence.

## 1. Initial repository state and verified contract baseline

| Object | Value |
| --- | --- |
| Starting HEAD | `2a1bfb7913d2a1274eacc8bb45931a8bc5cc29c1` |
| Starting tree | `e724caadeb9a8789f40aff64fc8d025dcf223e5a` |
| Working tree / worktrees | clean; one worktree (`D:/projects/Andirit`) |
| Environment | Windows x64 10.0.26200; SDK 10.0.401; runtime 10.0.12 |
| Baseline | whole solution **834 / 0 / 0**; frozen R2 oracle **185/185** (`97F6C5E5…`) |

Read before editing: the WP-S23-3 entry owner decision, the authorized package specification, the WP-S23-1/NEW-01 and WP-S23-2 (Q1/R1/Q2/OWNER) records, the S2.3 scope inventory (R13–R15), the Stage-2 boundary §3.8, the S2.2 identity handoff §3(4), and the existing occurrence/authority/publication sources.

## 2. Identified integration gaps

1. **No authority handoff through the publication stream.** `CodeGraphBatch` (in `Andirit.Abstractions`) can carry public `Occurrences`/`OccurrenceCoverage`, but not the internal `CodeOccurrenceAuthority` envelope or the `RequiresOccurrenceAuthority` marker. `Andirit` references `Andirit.Abstractions` and the reverse is impossible, so the envelope cannot be a member of the public batch type.
2. **The sink dropped the association.** `CodeGraphIngestionSink.Accumulator.ToReplacement()` built a `CodeIndexUnitReplacement` without `OccurrenceAuthority`/`RequiresOccurrenceAuthority`, so any real facts reaching the sink would publish authority-less (refused as `AnalysisUnavailable`).
3. **The S2.2 extractor was not wired to the plugin.** `CSharpCodeGraphPlugin.Session` emitted graph batches only; `VerifiedCSharpOccurrenceExtractor` output never reached the publication path (R15).

## 3. Selected internal handoff design and rationale

A small internal capability, `ICodeOccurrenceAuthoritySink` (in `Andirit`), implemented by `CodeGraphIngestionSink` and forwarded by the lifecycle's `PluginScopedSink`:

- `SetOccurrenceAuthorityAsync(CodeFactOrigin origin, CodeOccurrenceAuthority authority)` associates the verified envelope with an owner keyed by `(repository, plugin, index-unit)` — the same origin as the batch stream, so it can never bind to an unrelated unit.
- The envelope is applied only when the completing batch is received, so facts, coverage, authority, and marker stage together in one atomic replacement. Rejection/poison/dispose clear any pending association.
- The plugin publishes occurrences only to a sink that implements the capability; other sinks receive graph-only output, so occurrence facts are never published without the association needed to serve them.
- No public API, batch contract, storage path, or persisted format changes; the Option A separation is untouched.

The plugin's `Session.ExtractAsync` now runs the S2.2 extractor per project (in dependency order, before the completing graph batch) with a deterministic caller-supplied component-digest convention (`ComputeIdentifierDigest`/`ComputeDeclarationDigest`/`ComputeShapeDigest`), and publishes the resulting facts + coverage + authority through the sink.

## 4. Exact production changes

| File | Change |
| --- | --- |
| `src/Andirit/CodeOccurrenceAuthority.cs` | Added the internal `ICodeOccurrenceAuthoritySink` handoff contract. |
| `src/Andirit/CodeGraphIngestionSink.cs` | Implement the capability; store pending authority per owner; apply it in `ToReplacement`; clear on reject/poison/dispose; require consumption at `CompleteAsync`. |
| `src/Andirit/CodeIndexingLifecycle.cs` | `PluginScopedSink` forwards the capability after validating lifecycle scope. |
| `src/Andirit.CSharp/CSharpCodeGraphPlugin.Session.cs` | Added occurrence digest convention + `PublishOccurrencesAsync`; wired it into `Session.ExtractAsync` before the completing graph batch. |

Exact diff: `ANDIRIT-CI1A-S2.3-WP-S23-3A-IMPL-01-evidence/production-diff.txt` (+186/-13 across 4 files).

## 5. Q1-Q1 containment mechanism

The authority envelope is `[JsonIgnore]` on `CodeIndexUnitReplacement`, so a segment record cannot persist it. On a store-lifetime interruption, resuming a staged authority-requiring unit either re-runs extraction (fresh authority) or completes with the segment's marker + coverage but no authority; the query gate then refuses with `AnalysisUnavailable` (or `CorruptedIndex` if the association is absent), so graph-only resumption can never serve stale, partial, or fabricated occurrence authority. No durable envelope is added to segments. This is the fail-closed branch of the spec's "re-stage/re-verify or fail closed" requirement; a fresh-process segment test was not added because the same durable-reopen blocker (§7) interrupts it.

## 6. Positive real-input publication evidence

`S23PublicationIntegrationTests.RealCrossProjectFact_IsPublishedWithAuthority_AndServed` drives the real C# plugin (`Lib` ← `App`) through `CodeGraphIngestionSink` into LatticeDB. A real cross-project `App → Lib.Value.Get` occurrence is published with `RequiresOccurrenceAuthority = true`; its durable receipt records `DefiningProjectUnitId = Lib` and `SourceUnitId = App`; the accepted target (recomputed independently in the test from the frozen identity helpers and the live verified compilation) returns `Available`, one hit, `IsComplete = true`, with the correct source unit. A single-unit publication survives a real durable reopen and returns `Available` (`SingleUnitRealFact_SurvivesDurableReopen`).

## 7. Negative and stale-authority results

- `TransitiveDependencyChange_RefusesStaleRealFact`: changing only the transitive `Lib` body (App unchanged, target identity stable) refuses `StalePublication`, zero hits, zero examined, not complete — in-process against the published store.
- `MissingCurrentAuthority_IsUnavailable_NotEmptySuccess`: omitting current authority refuses `AnalysisUnavailable` (never a successful empty).
- `IncompleteUniverse_PublishesGraphOnly_AndNoOccurrenceRows`: an incomplete universe publishes graph only and writes no occurrence rows.

## 8. Fresh-process reopen and blocker

**Blocker `B-WP-S23-3A-01` (pre-existing).** Durable reopen of a multi-unit publication with cross-unit graph edges fails: `CodeGraphBatchRejectedException: The restored graph is invalid.` with four `graph.edge.endpoint-missing` errors. `DurableCommandLog.ReplayAsync` restores the stored units one at a time in `ReadTable` key order, and `InMemoryCodeGraphStore.RestorePublishedIndexUnitAsync` validates each step against only the units restored so far; when a unit referencing another unit's node (the C# plugin's cross-project `DependsOn`/`Calls` edges) is restored first, its endpoints are missing. Publication succeeds because staging occurs in dependency order.

This is **independent of occurrence publication**: a graph-only cross-project publication with no occurrence rows fails identically, while a single-unit publication reopens successfully. It is pinned reproducibly by `CrossProjectPublication_DurableReopen_IsBlockedByPreExistingGraphOrderingDefect` and captured in `blocker-reproduction.txt`. The accepted design already frames this boundary (`docs/design/ec6a-cross-file-resolution-architecture.md:162–175`: endpoint integrity is validated at staging time and cross-unit edges "work when ordering is guaranteed"); replay does not preserve that ordering.

Consequently the authorized cross-project fresh-process reopen evidence cannot be produced without fixing graph reconstruction ordering — a change outside the authorized occurrence-publication scope. Per the commission's stop conditions, implementation stops for an owner decision.

## 9. Regression matrix

| Gate | Result | Evidence |
| --- | --- | --- |
| New publication tests | **6 / 0 / 0** | `new-tests.log` |
| Full solution | **840 / 0 / 0** (LatticeDb 50, TreeSitter 157, core 345, CSharp 288) | `solution-tests.log` |
| NEW-01 regression | **17 / 0 / 0** | `new01-tests.log` |
| Frozen R2 oracle | **185/185**; 146851 bytes; `97F6C5E5…39C9` unchanged | `oracle-r2.json` |
| Format (`--verify-no-changes`) | exit 0 | `format.log` |
| Release build | 0 warnings, 0 errors | `release-build.log` |

No existing test was removed, disabled, or weakened; the capability gating left the S2.2 `RecordingSink` boundary behavior unchanged.

## 10. Remaining qualifications and unproven claims

- Cross-project (or any multi-unit) durable reopen is **unproven and currently failing** (blocker above). Single-unit reopen is proven.
- Q1-Q1 segment-resumption containment is argued from the fail-closed gate behavior; a fresh-process segment test is not included because of the same reopen blocker.
- S-01 component digests remain caller-supplied (the plugin's convention makes no canonicalization claim).
- Q1-Q2, S-01/S2.5, SQ-03/SQ-04, and WP-S23-4/5 remain outstanding.
- Verification is Windows-only; no crash/power-loss/cross-platform/concurrency claim.

## 11. Production commit/tree and evidence receipts

- Starting HEAD: `2a1bfb7913d2a1274eacc8bb45931a8bc5cc29c1` (tree `e724caadeb9a8789f40aff64fc8d025dcf223e5a`).
- Bounded implementation commit: **`e2b833a35e74d7fa8b1bf9eabe506af0357902b3`** (tree `a196cb2ce21163c9b45c2abd03f11fd7d4754382`).
- Evidence directory: `docs/reviews/ANDIRIT-CI1A-S2.3-WP-S23-3A-IMPL-01-evidence/` (production/test diffs, blocker reproduction, verification logs, oracle, manifest). The report/evidence commit identity is reported in the external handoff.

## 12. Independent reviewer handoff

Because the package is blocked, an independent `S23-PUB-Q1` qualification of the full deliverable is not yet appropriate. If the owner authorizes the graph-reconstruction ordering fix, the reviewer must independently reproduce: real cross-project publication with matching authority and correct targets **across a fresh-process reopen**; stale (direct/transitive/options) refusal after reopen; missing/corrupt/unsupported refusal; incomplete→graph-only; Q1-Q1 segment containment; occurrence/publication consistency; and no weakening of S23-DUR or D3/D4/D5. Until then, the owner decision required is whether to authorize a bounded fix to durable reconstruction ordering (restore all units, then validate the reconstructed graph once) or to re-scope WP-S23-3A.
