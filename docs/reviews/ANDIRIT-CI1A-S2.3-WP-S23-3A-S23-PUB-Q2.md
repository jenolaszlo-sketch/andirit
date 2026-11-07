# ANDIRIT-CI1A-S2.3-WP-S23-3A-S23-PUB-Q2

## Independent requalification of staged-run durable reconstruction

**Commission:** `ANDIRIT-CI1A-S2.3-WP-S23-3A-S23-PUB-Q2` (independent qualification only).
**Repository:** `D:/projects/Andirit`. **Canonical branch:** `main`. **Date:** 2026-11-07.
**Reviewed HEAD:** `a8e7b906cdfb23821245fe2900cd1c44e3bf1105` (tree `29fa9f1c9a3501d8ad74ac19376a701a83a24130`).
**Reviewed change:** R2 implementation `a4d85011b8b753d4b058ed3a2a02fe2ebc73294c` (parent `b76dc4c`, the S23-PUB-Q1 review commit).
**No production or test source was modified by this review.** Added files are this report, the reviewer evidence directory, and the reviewer-owned probe harness under `docs/reviews/` (additive, outside the solution).

**Disposition:** **`S23-PUB-Q2 SUPPORTED WITH QUALIFICATIONS`** — F-Q1-01 is closed. Staged-run replay is order-independent within the accepted contract; integrity, refusal and authority-containment invariants remain intact; and the previously supported WP-S23-3A publication and R1 guarantees are preserved. Bounded WP-S23-3A owner acceptance is recommended (not automatic).

> **Session separation caveat (disclosed for the owner).** This qualification was executed in the
> same agent harness session as the prior commissions, as a distinct commissioned review task
> (model: GLM, z.ai). Every result was re-derived or re-executed by this review from the commits,
> source, and reviewer-owned probes — never accepted from R2 artifacts on trust. All probes are
> reproducible from the committed harnesses (`run-q2-probes.ps1`, `Q2AdversarialProbe`). If the
> owner requires strict session separation, the probes support an independent re-run.

## 1. Preflight

HEAD/tree verified (`a8e7b906…` / `29fa9f1c…`), clean tracked/index state, canonical `main`, one worktree, no stashes; **no prior S23-PUB-Q2 review existed**; no concurrent writer (state re-checked before the final commit). Ancestry verified: `e2b833a` → `f8b2b08` → `e0cef1c` (R1) → `072f52b` → `b76dc4c` (Q1) → `a4d8501` (R2, tree `cfbf67eb…`) → `a8e7b906`. R2 evidence manifest integrity verified (all concrete manifest entries present; family-notation entries exist; `oracle-r2-r2.json` = 146,851 bytes, SHA-256 `97F6C5E5…39C9`). Read: the R2 report and its seven reviewer questions, the S23-PUB-Q1 report (F-Q1-01 and probes P10–P13/P17/P18), the R1/IMPL-01 reports, and the accepted S23-DUR / NEW-01 / Q1-Q1 / publication contracts.

## 2. Independent R2 source audit (§3)

Inspected as committed (`diff-audit-a4d8501-src.txt`): `DurableCommandLog.ReplayAsync` / `Apply` / `Reconstruct` / `ApplyCommandAsync`; `InMemoryCodeGraphStore.RestoreStagedRunAsync` / `StageIndexUnitAsync` / `StageIndexUnitDeletionAsync` / `CompleteIndexRunAsync` / `RestoreCompletedRunAsync` / `ApplyStagedChanges` / `ApplyStagedOccurrences`; and the `LatticeCodeGraphStore` live/open paths.

| # | Requirement | Verdict |
| --- | --- | --- |
| 1 | Grouping by the exact intended `(repository, run)` identity | Yes — the group key derives each field exactly as `StageKey` does (`Replacement.Origin.*` for `stage-replace`; command fields for `stage-delete`) |
| 2 | No cross-repository/run/generation merging | Yes — per-RunKey groups; generation mixing is structurally impossible (below) |
| 3 | Durable command order preserved within a run | Yes — commands are appended to the group in enumeration order and applied in that order |
| 4 | Last-command-wins matches live staging | Yes — replace clears the deletion slot and vice versa, mirroring `StageIndexUnitAsync`/`StageIndexUnitDeletionAsync` bookkeeping; committed end state is identical to live |
| 5 | Ownership/running-run checks on every command | Yes — `EnsureKnownOwnership` per `stage-replace`, `EnsureRunningRun` per `stage-delete`, before any bookkeeping (probe: unregistered-run replace refused) |
| 6 | Unit-local validation not weakened | Yes — intrinsic duplicate-id checks per unit plus the complete-set validation that re-runs them over every staged unit |
| 7 | Graph + occurrence invariants on the complete staged set | Yes — `ValidateMaterializedGraph` and `ValidateOccurrences` over `_units` − deletions + replacements, mirroring `ApplyStagedChanges`/`ApplyStagedOccurrences` semantics |
| 8 | No leakage of other runs' staged data | Yes — the prospective set is built only from `_units` and this run's own maps (probe: interleaved same-unit-id runs isolated both ways) |
| 9 | Deletions + replacements + published units compose correctly | Yes — deletions applied first, then replacements (exact `ApplyStagedChanges` order); published units are the base (probe: staged retirement) |
| 10 | Failed validation cannot partially modify state | Yes — bookkeeping is committed only after both validations; before that the method only reads (probe: durable snapshot byte-identical across refused opens) |

**Critical review question — does collecting stage commands change effective ordering relative to other durable command types? No:**

1. `Reconstruct` already canonicalizes the replay list (`repositories → running runs → terminals → units → stages`); it was never chronological, so "durable command order" for stage rows was and remains stage-key order.
2. Stage rows exist **only for running runs**: `Apply` folds a run's stage rows into `replace`/`delete` rows on `complete` and removes them on a terminal `run`. Groups therefore always belong to running runs — publication generations cannot mix, and a stage group can never interact with a `complete` command's stage fold (which happens at write time, not replay).
3. Stage processing remains strictly between published-unit restoration (step 3) and terminal-history restoration (step 5) — the interleaving with other command types is unchanged.
4. Per-RunKey staged state is independent (`ApplyStagedChanges`/`ApplyStagedOccurrences` read only `_staged[key]`), so the order in which groups are processed cannot affect any outcome; the dictionary's iteration order is immaterial.
5. Accept/refuse equivalence: pre-R2, the last command of each contiguous group validated that run's complete set; post-R2 the group validation runs over the same final set. The accept/refuse boundary is identical except for the intended removal of order-dependent intermediate refusals. One benign observable difference: staged graph failures now report `"The restored staged graph is invalid."` (was the per-command live message); the error-code vocabulary is unchanged.
6. The live path (`StageIndexUnitAsync`, `StageIndexUnitDeletionAsync`, `ApplyCommandAsync`, `MutateAsync`) is byte-identical to pre-R2.

No log sequence was found where the new grouping reorders incorrectly; the interleaving, completion, retirement, repeated-replacement and failure paths were additionally exercised empirically (§4–§5).

## 3. F-Q1-01 closure evidence (§4)

Fresh-process probes (committed harness rerun, Q2-prefixed, one OS process per phase, durable-file hashes in `Q2-probe-matrix.json`): **24/24 phases pass.**

| Probe | Process ids | Result |
| --- | --- | --- |
| P10 → P11 (referencing-first; formerly failing) | 10060 → 39596 | Valid staged graph **reopens** (no endpoint-missing refusal); run completion publishes 2 valid units with `RequiresOccurrenceAuthority = true`; occurrence gate fails closed: `AnalysisUnavailable`, 0 hits, 0 examined, not complete — no unqualified occurrence served |
| P12 → P13 (referencing-first; formerly failing) | 25136 → 6664 | Reopen; re-staging verifies fresh occurrence authority independently; completion → `Available`, 1 expected hit, complete; source unit = App's project unit; durable defining unit = Lib's |
| P17 → P18 (defining-first controls) | 34596 → 31820 | Previously working behavior preserved; identical fail-closed outcome to P11 — results do not depend on stage-key ordering |

The remaining 18 phases (positive control, graph-only controls, three corruption modes with byte-identical durable snapshots, negative-authority, stale-after-reopen, replay permutation) all pass unchanged.

## 4. Adversarial reconstruction probes (§5)

Replay-level (`Q2-ADV-replay-adversarial.json`, 9 cases — all pass):

1. **Interleaved staged commands from two runs of one repository, same unit identifier:** completing either run publishes only that run's staged units; the other run's staged state remains unpublished and untouched; **delete-then-replace** leaves the unit present.
2. **Replace-then-delete:** the unit is absent from the completed publication.
3. **Deleting a staged unit still referenced by another staged unit's edge:** refused (`graph.edge.endpoint-missing`) — a genuinely inconsistent staged set, correctly refused (corruption control, not an ordering artifact).
4. **Two repositories with identical unit and node identifiers:** groups isolated; both runs complete to their own publications.
5. **Stage-row enumeration reversed** (logical sequence unchanged): identical outcome.
6. **Duplicate staged identity:** refused (`replacement.node.duplicate`).
7. **Conflicting staged identity across staged units:** refused (`graph.node.conflict`).
8. **Genuinely missing endpoint after complete reconstruction:** refused in **both** enumeration orders.
9. **Staged replace for an unregistered run:** refused (`replacement.run.not-running`) — ownership checks execute per command.

Durable-level (`Q2-ADV-durable-*.json`, one process per phase):

- Persisted defining-first stage order with a cross-unit edge reopens; zero occurrence rows.
- Missing-endpoint corruption (defining staged row removed) refuses at open; durable snapshot byte-identical.
- Malformed staged payload refuses (`JsonException`); durable snapshot byte-identical.
- **Staged retirement across reopen:** the published unit survives until the staged-deletion run completes, then is retired and absent — published/staged composition correct through a real durable round trip.

Malformed final state still refuses in every case; failed replay never exposed partial state. The corruption model was not weakened to satisfy the positive cases.

## 5. Authority and publication invariants (§6)

- No transient `CodeOccurrenceAuthority` envelope in segment records (P10/P12/P17 read the real `HetuStage` rows: marker present, `hasAuthorityProperty = false`).
- `RequiresOccurrenceAuthority` enforced on reconstructed staged publications; incomplete reconstruction never becomes successful publication (staged state stays unpublished until completion — probes and the staged-retirement probe both assert 0 published units before completion).
- Missing authority is never inferred from graph-only state: with the graph published, the query refuses `AnalysisUnavailable` (0 hits, 0 examined, not complete).
- Fresh re-staging requires independently verified live authority (P13; the live-compiler binding in `VerifiedCSharpOccurrenceAuthority.Bind` was source-audited in Q1 and is unchanged).
- Source/defining-unit and compilation identities verified (P02/P13). Stale and corrupted provenance refuse (P15/P14). Complete-empty and availability semantics unchanged (focused 69-suite).
- Reopening staging state ≠ authorization to publish occurrence facts — the two are asserted separately in P11 (reopen+complete succeeds; occurrence query refuses).

## 6. Regression gates (§7)

| Gate | Producer claim | Q2 result |
| --- | --- | --- |
| R2 staged-replay tests | 14 | **14 / 0 / 0** (within the focused run) |
| Focused S23 suites | 69 | **69 / 0 / 0** |
| NEW-01 | 17 | **17 / 0 / 0** |
| Full solution | 865 | **865 / 0 / 0** (LatticeDb 50, TreeSitter 157, core 345, CSharp 313) |
| Frozen R2 oracle | 185/185 | **185/185**, 146,851 bytes, SHA-256 recomputed `97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9` |
| Release build | clean | **0 warnings, 0 errors** |
| Formatting | passed | **exit 0** |

No existing test or historical oracle data was modified by this review.

## 7. Findings (§8)

**No in-scope findings.** Two documented qualifications (neither a defect):

1. **Two interrupted runs in one repository cannot both complete sequentially after reopen** — the second completion is refused by the accepted stale-context baseline rule (`RequireCurrentBaseline`; the run was planned before the first run's publication). Pre-existing, unchanged by R2, and the accepted recovery path (re-plan under a new run) applies. Source: `InMemoryCodeGraphStore.RequireCurrentBaseline`; governing contract: stale-context rejection (accepted invariant). Consequence: bounded to concurrent-interrupted-run completion ordering; no data-integrity impact.
2. **Error-message text for staged graph failures** changed from the live-staging message to `"The restored staged graph is invalid."` (same exception type and error-code vocabulary). Contract-observable only at the message level; codes unchanged (`graph.edge.endpoint-missing` etc.), consistent with R1's accepted message style.

Unrelated historical limitations observed in passing (not expanded per the commission): none new beyond Q1's carried qualifications (Q1-Q2 atomicity bound, S-01 digests, O-Q2-01, Windows-x64 scope).

## 8. Owner recommendation (§9–§10)

F-Q1-01 is closed. **Bounded WP-S23-3A owner acceptance is recommended** — the combined WP-S23-3A implementation plus R1/R2 remediations now independently satisfy the authorized publication-integration contract within the accepted scope (Windows x64, serialized single-writer store, no crash/power-loss claims). This review makes no acceptance decision and authorizes no successor package; WP-S23-4/5, S2.4/S2.5 and Stage 6 remain unstarted and unauthorized.

## 9. Evidence integrity manifest

See `CI1A-S2.3-WP-S23-3A-S23-PUB-Q2-evidence/manifest.txt` (self-excluding; per-file roles and provenance; binary probe databases excluded and reproducible via `run-q2-probes.ps1` and `Q2AdversarialProbe`). Reviewer harness: `docs/reviews/CI1A-S2.3-S23-PUB-Q2-tests/` (adversarial probe project + verification-results writer); the harness probes reuse the committed `CI1A-S2.3-S23-PUB-Q1-tests/S23PubQ1Probe.csproj` project with Q2-prefixed outputs.
