# ANDIRIT-CI1A-S2.3-WP-S23-3A-S23-PUB-Q1

## Independent qualification of real-fact publication and durable reconstruction

**Commission:** `ANDIRIT-CI1A-S2.3-WP-S23-3A-S23-PUB-Q1` (independent qualification only).
**Repository:** `D:/projects/Andirit`. **Canonical branch:** `main`. **Date:** 2026-11-07.
**Reviewed HEAD:** `072f52bb3a39dbb5cb4dd7c26e13e7022a0160e6` (tree `3c2f2e1f1504b2e7e9300ba59eb738dae5dc0edb`).
**Reviewed changes:** publication integration `e2b833a35e74d7fa8b1bf9eabe506af0357902b3` + R1 remediation `e0cef1c94f68f2a24a529e1182c0de5862e383c8`.
**No production or test source was modified by this review.** The only added files are this report, the reviewer evidence directory, and the reviewer-owned probe harness under `docs/reviews/` (additive, outside the solution).

**Disposition:** **`S23-PUB-Q1 REMEDIATION REQUIRED`** — one demonstrated, in-scope, pre-existing correctness defect remains (finding **F-Q1-01**, staged-run resumption order-dependence). Publication correctness, authority containment, published-publication durable reopen and corruption refusal are independently supported; F-Q1-01 does not admit incorrect data but blocks the Q1-Q1 re-stage branch for multi-project runs in one of the two persisted key orders.

> **Independence caveat (disclosed for the owner).** This qualification was produced in the
> same agent harness session as the R1 implementation, as a distinct commissioned task.
> Every claim below was re-derived or re-executed by this review — from the commits, from
> source, and from a reviewer-owned probe harness — and never accepted from R1 or producer
> artifacts on trust. If the owner requires strict reviewer-≠-implementer session
> separation, this report can be re-validated by an independent session; all probes are
> reproducible from `docs/reviews/CI1A-S2.3-S23-PUB-Q1-tests/run-probes.ps1`.

## 1. Preflight

HEAD/tree/branch/ancestry verified (publication → IMPL record → R1 → R1 handoff; `e0cef1c` parent is `f8b2b08`); clean tracked/index state; canonical `main`; exactly one worktree; no stashes; **no prior S23-PUB-Q1 review existed**; no concurrent writer was observed (state re-checked before the final commit). Read: the WP-S23-3 entry owner decision, `wp-s23-3-publication-integration.md`, the WP-S23-2 IMPL/Q1/R1/Q2/OWNER records, the NEW-01 Q1 record, and the committed reviewer handoff (whose seven questions structure §2–§8 below).

## 2. Publication-authority audit (§3 of the commission)

Source inspection of both commits (full diffs: evidence `diff-audit-e2b833a.txt`, `diff-audit-e0cef1c.txt`):

- `ICodeOccurrenceAuthoritySink` is internal to `Andirit` (correct layering; no public API, batch contract, storage path, or persisted-format change — verified against the spec's design-tension constraint).
- `CodeGraphIngestionSink` stores the authority keyed by the **full owner key** (`OwnerKey.From(origin)` — repository, plugin, unit) and applies it **only when the completing batch arrives** (`ToReplacement(authority)`); reject/poison/dispose clear it; `CompleteAsync` treats unconsumed authority as an incomplete index unit. Authority can therefore never be published without its facts and coverage, and never for an unrelated unit.
- `PluginScopedSink` validates repository, run, plugin, **and version** for both batches and authority — a plugin cannot associate authority outside its lifecycle scope.
- The plugin publishes each project's occurrence batch (with `SetOccurrenceAuthorityAsync` for the same origin) before that project's completing graph batch, so one replacement stages graph + facts + coverage + authority together. Sinks without the capability receive graph-only output; an incomplete universe (`extraction.Authority is null`) publishes graph-only.
- The persisted query gate (`LatticeCodeGraphStore.OccurrenceAuthority.CheckOccurrenceAuthorityAsync` + `VerifiedCSharpOccurrenceAuthority.Validate`) independently re-binds authority to the exact unit id, live verified universe membership, stored coverage, per-fact defining `compctx2` → recomputed target, and source-hash membership. A mis-associated envelope cannot survive this gate.

Independent probes (P01/P02, fresh processes, real Roslyn inputs through the real plugin → sink → store):

- 2 published units; unit ids follow the plugin's public convention `csharp:project:<sha256(project path)>`; App unit carries 1 real fact, complete coverage; both units carry `RequiresOccurrenceAuthority = true` (Lib's is a complete-empty authority with 0 facts — no unauthorized graph-only substitution).
- Durable receipts: `RequiresAuthority = true`, authority present, `authorityPublicationRun = run:q1:a`, envelope `IndexUnitId` equals the owning row's unit id, `SourceUnitId` = App's project unit, `Members` = 2, App facts = 1 with `DefiningProjectUnitId` = Lib's project unit; `AndiritOccurrenceMeta/Page` rows present.
- Fresh-process verify: expected target recomputed two independent ways (frozen R2 `CompilationProvenance` + accepted identity primitive; both agree), query `Available`, exactly 1 hit, `IsComplete = true`, `Examined = 1`, correct `SourceUnitId`, cross-project graph edge resolves to the Lib project node.

## 3. Negative authority controls (§4)

Probed (P14, P15) and corroborated by the S23 suites (55/55, §8):

| Control | Result |
| --- | --- |
| Wrong-plugin authority at the sink handoff | refused (`ingestion.occurrence-authority.ownership-mismatch`) |
| Envelope/origin mismatch (plugin field) | refused (same code) |
| Authority registered but unit never completes | `CompleteAsync` refuses (`ingestion.index-unit.incomplete`) — authority cannot be silently dropped or published alone |
| Missing current authority | `AnalysisUnavailable`, 0 hits, 0 examined, not complete — never a successful empty |
| Persisted occurrence unit moved to another unit id | `CorruptedIndex` (orphaned from the published graph units) |
| Direct defining-source change after fresh reopen | `StalePublication`, 0 hits, 0 examined, not complete |
| Unchanged-input control after reopen | `Available`, complete (proves refusals are staleness-driven) |
| Transitive-only / options-only / stable-`asmctx3` cases | covered by `S23NativeAuthorityTests.Test8b_*` and `S23PublicationIntegrationTests.TransitiveDependencyChange_RefusesStaleRealFact` (passing); the gate's defining-`compctx2` recomputation is the enforcing mechanism |
| Incomplete universe | graph-only publication, zero occurrence rows (P03–P06 pattern; `IncompleteUniverse_PublishesGraphOnly_AndNoOccurrenceRows` passing) |

Counters (`Examined`, `TotalMatches`, `IsComplete`, refusal reasons) were checked in every probe and match the accepted vocabulary.

## 4. Durable reconstruction audit (§5)

Independent inspection of both R1-modified files (success **and** exception paths, not just the lock):

1. **Collect-before-validate:** `ReplayAsync` gathers all `replace` commands (per-item cancellation checks preserved) and issues one `RestorePublishedUnitsAsync` call.
2. **Intrinsic validation preserved:** per-unit duplicate-id checks (nodes/declarations/edges) run in isolation for **every** replacement — including any hypothetically superseded row — plus again over the final set; `Merge` conflict checks and symbol-uniqueness run over the complete set. Probe P16: unit-level duplicate refused (`replacement.node.duplicate`).
3. **Command order/removal semantics:** prospective is built in command order (later row per owner wins — probe P16 confirms `repeatedReplacementLastWins=true`); deletions are materialized as row removals at the provider layer before replay, and `stage-delete` rows replay via the unchanged `StageIndexUnitDeletionAsync` path.
4. **Complete-graph validation:** `ValidateMaterializedGraph` runs once per affected repository with every persisted unit present; endpoint/declaration checks therefore cannot depend on enumeration order.
5. **Repository boundaries:** `Materialize` filters units by owner repository; per-repository validation mirrors staging-time semantics (cross-repository edges are impossible by construction). Probe P16: an invalid unit in a second repository refuses the whole reconstruction (`mixedInvalidRefused=true`).
6. **Invalid reconstruction rejected:** all corruption probes refused (§6).
7. **No external visibility before validation:** `_units` is replaced by a single assignment inside one `_gate` critical section; on the open path `_inner` (volatile) is assigned only after replay + baseline + occurrence restoration, and the constructor's catch disposes the database and rethrows — no partially reconstructed store can escape.
8. **Failed replay exposes nothing:** probes P09 (all three corruption modes) prove reopen refusal **and** a byte-identical durable snapshot afterwards (no write-back, no partial projection).
9. **Order independence:** replay permutations produce identical publications and traversals (P16); graph-only reopen succeeds in both persisted key orders (P03–P06). **Exception — finding F-Q1-01 below** for the staged-run path.
10. **Valid publications reopen:** occurrence-bearing (P02) and graph-only (P04/P06) both reopen in fresh processes.

**Weakening check:** duplicate identity, conflicting fact, missing-endpoint, and malformed-row refusals were independently re-demonstrated after the fix (P09, P16); the 100-error cap and exception text are unchanged; no check was removed or made unreachable. The fix replaces *when* validation runs, not *whether* it runs.

## 5. Finding F-Q1-01 — staged-run resumption order-dependence (remediation required)

- **Exact source:** `DurableCommandLog.ReplayAsync` step 4 — `foreach (var command in commands.Where(command => command.Kind is "stage-replace" or "stage-delete")) await ApplyCommandAsync(store, command, …)` applies persisted stage rows **one at a time in stage-key order** (`StageKey` = repository\nrun\nplugin\nunit), each via `InMemoryCodeGraphStore.StageIndexUnitAsync`, whose prospective set is `_units` + **staged-so-far** and which runs full graph validation (including cross-unit endpoints) against that partial set. Verified identical at the pre-R1 commit `f8b2b08` — **pre-existing**, same class as B-WP-S23-3A-01, in the adjacent path R1 did not modify.
- **Evidence (fresh processes, one per phase):**
  - P10→P11 (`QLib`/`QApp` fixture; referencing unit hash `2810…` < defining `2e8a…`): segments stage correctly in process A (both carry `RequiresOccurrenceAuthority = true`, no `Occurrences` property, no `Authority` property); in process B the store **refuses to open** with 4× `graph.edge.endpoint-missing` — the referencing segment is validated before the defining segment is applied.
  - P12→P13: the **re-stage branch of Q1-Q1 is unreachable** in this order — the store cannot even be opened to re-stage; the interrupted run can never be completed.
  - P17→P18 (counterfactual, same logical fixture renamed so the defining unit's key sorts first): resumption completes; the run publishes 2 valid units with markers; the occurrence query fails closed with `AnalysisUnavailable` ("Missing or malformed recorded occurrence authority; re-ingestion required."). Same logical publication, only persisted key order differs.
- **Affected contract:** commission §5.9 ("reopen results do not depend on `ReadTable` key enumeration order"), §6 ordering control ("valid publications reopen consistently"), the spec's Q1-Q1 requirement (the re-stage/re-verify branch must be available after an interruption), and the accepted staged-run resumption lifecycle (`Store_ReopensStagedRunWithoutPublishingItAndCanResumePublication` pattern, single-unit only).
- **Observable consequence:** a valid multi-project real-fact run interrupted between `sink.CompleteAsync()` and `CompleteIndexRunAsync()` — precisely the Q1-Q1 window — can never be resumed or completed when the referencing unit's stage key sorts first. The fail-closed property is preserved (refusal; no stale, partial, or fabricated authority is ever served), and single-unit resumption and completed-publication reopen are unaffected.
- **Severity:** moderate. Correctness/lifecycle defect, not a data-integrity hazard. It weakens the Q1-Q1 remediation menu for cross-project runs (fail-closed only; re-stage unreachable) and violates this commission's ordering-independence requirement.
- **Smallest remediation boundary:** `DurableCommandLog.ReplayAsync` step 4 — apply each run's staged commands as one set (or validate the staged application once per run against the complete staged set), mirroring the authorized R1 pattern for published units. No contract, schema, or authority change is required. **This review did not implement it** (production changes are forbidden here); it requires a separate bounded authorization.

## 6. Fresh-process probes (§6) — summary

| Probe | Processes | Result |
| --- | --- | --- |
| Positive: real cross-project publish → verify | P01 pid 42576 → P02 pid 36024 | Available, 1 hit, complete, correct defining/source units, target independently recomputed, cross-unit edge resolved |
| Graph-only: cross-unit edges, zero occurrence rows, both persisted orders | P03 pid 22012 → P04 pid 20040; P05 pid 8964 → P06 pid 9828 | reopen ok, cross-unit edge resolved, 0 occurrence rows |
| Corruption: missing endpoint / inconsistent edge / malformed row | 3× publish → mutate → fresh verify (P07–P09) | all refused (`graph.edge.endpoint-missing` / `JsonException`), durable snapshot byte-identical — no partial published state |
| Ordering: replay permutation + both durable key orders | P16; P03–P06 | identical outcomes for valid publications; invalid still refused |
| Staged resumption | P10–P13, P17–P18 | **F-Q1-01** (see §5) |

Commands, process identities, durable-file provenance and exact notes are in `probe-matrix.json` and the per-phase JSON/log files.

## 7. Q1-Q1 segment containment (§7)

- Source inspection: the transient authority envelope is `[JsonIgnore]` on `CodeIndexUnitReplacement`; segment records persist the `RequiresOccurrenceAuthority` marker only. Probes P10/P12/P17 read the real `HetuStage` rows produced by the **real plugin/sink chain** and confirm: `requiresAuthority=True`, no `Occurrences` property, no `Authority` property — a persisted authority envelope in a segment record would have failed the probe.
- Reopening a **completed** publication works (P02, P18) and is distinct from resuming an **incomplete** staging run: staged segments are not published before completion (P11/P18 assert 0 published units pre-completion).
- Fail-closed branch: completing a resumed run without re-staging publishes marker + coverage but no authority; the verified gate refuses `AnalysisUnavailable` and never serves graph-only evidence as qualified occurrence data (P18).
- Re-stage branch: works in the defining-first order (P18 counterfactual; `SegmentResumption_MarkerSurvives_…` covers the fixture path) but is **unreachable in the referencing-first order due to F-Q1-01**.
- The persisted segment contract was not expanded.

## 8. Superseded-test audit (§8)

Full analysis in `superseded-test-audit.txt`. Summary: the superseded test (`CrossProjectPublication_DurableReopen_IsBlockedByPreExistingGraphOrderingDefect`, at `f8b2b08`) pinned the *existence* of B-WP-S23-3A-01 by asserting that a **valid** cross-project publication is refused on reopen — an expected failure made obsolete by the authorized R1 fix, not a corruption-refusal oracle. Its replacement set is strictly stronger on the positive side (units, graph edge, availability, hit count, coverage, target, source unit, defining-unit receipt) and the negative reopen space (missing endpoint, inconsistent edge, malformed row, failed replay) is covered by the new `S23DurableGraphReopenTests` suite and independently re-established by probes P07–P09. F-01 orphan-owner refusal and occurrence-gate corruption refusal remain covered by the passing S23 ownership/authority suites. **No independent negative coverage was lost.**

## 9. Regression receipts (§8) and previously accepted guarantees (§9)

| Gate | Result |
| --- | --- |
| Focused S23 suites | **55 / 0 / 0** |
| NEW-01 reviewer suite | **17 / 0 / 0** |
| Full solution | **851 / 0 / 0** (LatticeDb 50, TreeSitter 157, core 345, CSharp 299) |
| Frozen R2 oracle | **185/185**, 146,851 bytes, SHA-256 recomputed `97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9` (matches the recorded digest) |
| Release build / format | 0 warnings, 0 errors / exit 0 |

Guarantee checks (all green, via the full solution run plus targeted evidence): NEW-01 recovery projection (17/17, fresh reopen); S23-DUR durable consistency and F-01 orphan-owner refusal (S23OccurrenceOwnershipRegressionTests; the orphan direction of the gate also fired in probe P14 as `CorruptedIndex`); stale-context refusal (P15 + suites); complete-empty vs unavailable/corrupted distinction (S23NativeAuthorityTests); conflict handling and retirement (S23NativeAuthorityTests); unknown-outcome quarantine (`New01RecoveryProjectionTests.UnknownOutcome_StillQuarantinesAndReopenSeesDurableState`); native mirror consistency (`NativeGraphMirrorTests`); D3/D4/D5 and frozen identities (core contract suites; frozen oracle). No crash-consistency, power-loss, or multi-process-write claims are made or implied; verification remains Windows-x64, single-writer.

## 10. Findings

**F-Q1-01 (the single finding; disposition-driving).** Staged-run resumption order-dependence — full statement in §5. In-scope, pre-existing, moderate, fail-closed preserved, bounded remediation identified, not implemented here.

No other findings. Probe defects encountered during the review (JSON shape assumption, stale binary artifacts) were probe-side and are documented in the harness history, not product defects.

## 11. Owner recommendation

1. Do **not** proceed to `S23-PUB-OWNER` acceptance yet. Authorize a small **R2 remediation commission** for F-Q1-01 with exactly the boundary in §5 (staged-set reconstruction in `ReplayAsync` step 4, R1-pattern, no contract changes), followed by a re-qualification focused on the staged-resumption matrix (P10–P13/P17–P18) plus the regression gates.
2. The publication integration itself (`A` in §2) is independently supported and does not need re-qualification beyond the regression gates if the R2 change remains within the identified boundary.
3. If the owner prefers, F-Q1-01 can alternatively be recorded as a bounded qualification carried forward (fail-closed is preserved); this review's judgment is that it should be remediated first because it blocks an authorized Q1-Q1 branch for exactly the multi-project scenario WP-S23-3A targets.
4. Historical evidence (producer, R1, and all prior Q1/Q2 records) was not modified; the reviewed tree remains clean apart from this review's additive documentation commit.

## 12. Evidence integrity manifest

See `CI1A-S2.3-S23-PUB-Q1-evidence/manifest.txt` (self-excluding; per-file roles and provenance; binary probe databases excluded and reproducible via `run-probes.ps1`). Reviewer harness: `CI1A-S2.3-S23-PUB-Q1-tests/` (console probe project + orchestration script; independent of implementation test sources).
