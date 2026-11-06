# ANDIRIT-CI1A-S2.3-WP-S23-2-Q2

## Independent requalification: occurrence/graph owner correspondence after R1 remediation

**Role:** independent qualification reviewer. **Repository:** `D:/projects/Andirit`. **Branch:** `main`. **Date:** 2026-11-06.
**Disposition entering review:** `REMEDIATED — READY FOR INDEPENDENT Q2`.
**Overall disposition:** **`SUPPORTED WITH QUALIFICATIONS`.**

F-01 is independently closed within the accepted Windows execution scope: a published graph owner removed while its authority-bearing occurrence unit remains is now refused, durably, across separate processes, without false corruption refusals for valid states or destruction of unrelated publication. No production file was modified by this review. One bounded, non-blocking residual observation (O-Q2-01) and the previously open Q1-Q1/Q1-Q2/S-01 qualifications remain; none is an F-01-class defect. Owner acceptance is not granted here.

## 1. Reviewed source and provenance

| Role | Exact commit | Exact tree |
| --- | --- | --- |
| Q1 reviewed HEAD (defect baseline) | `d94052d2fa8dd14ecbfcc6337b4a262e8954ba85` | `53966a01b69c876bfff29a60c576c595bdb9e84b` |
| **R1 production remediation (reviewed)** | **`1bce70ee2f52ed5100a2e68bc004462f9824d2f2`** | **`a6a908ab69740e4ee114e845cdde9374975dc15a`** |
| R1 handoff / docs HEAD | `0bc509164363013899fa5ddbf825dd7086c9c5df` | `d3840933d6c19f46a20889a9b454b0d06c2c1c14` |

Preflight: canonical checkout on `main`, clean tree (only the additive Q2 evidence/tests directories untracked), single worktree, no stashes. Ancestry confirmed: `1bce70e` is a child of `d94052d`, and `0bc5091` is a child of `1bce70e`. The remediation changes exactly one production file, `src/Andirit.LatticeDb/LatticeCodeGraphStore.OccurrenceAuthority.cs` (+32 lines, no other `src` change), and there is **no** `src` change between the remediation commit and the docs HEAD. `git diff -- src tests` after the review is empty (the working file differs from the Git blob only by CRLF/LF, normalized-equal). The Q1 report/evidence and R1 report/evidence are byte-unchanged (identical `git status` and blob hashes; receipts in `q2-integrity.json`).

The producer handoff that previously occupied this path is preserved additively at `CI1A-S2.3-WP-S23-2-Q2-evidence/producer-q2-handoff.md` (4864 bytes, SHA-256 `5E4F4C…48ED8`, Git blob `78bd586…`); it is not rewritten inside Q1/R1 artifacts.

Evidence: [`CI1A-S2.3-WP-S23-2-Q2-evidence/`](CI1A-S2.3-WP-S23-2-Q2-evidence/). Q2-owned probe/tools: [`CI1A-S2.3-WP-S23-2-Q2-tests/`](CI1A-S2.3-WP-S23-2-Q2-tests/).

## 2. Independent methodology and environment

Environment: Windows x64 10.0.26200, SDK 10.0.401, runtime 10.0.12 (`q2-environment.log`).

Two independent harnesses were built, neither linking any producer fixture or the Q1 probe program:

- **Q2 probe** (`CI1A-S2.3-WP-S23-2-Q2-tests/Q2Probe.csproj` + `Program.cs`): independently reads corpus source files, constructs real Roslyn compilations, derives the verified universe and the frozen R2 expected `asmctx3`/`usage4` identities, drives the production store through its accepted public entry points, mutates real native rows through the native provider, and queries from separately constructed live authority. Its corpus has **two** fact-bearing owners (`Client`, `Client2`) plus `Empty`, so multi-owner preservation is exercised rather than a single-owner fixture.
- **Q1 56-invocation matrix re-execution** (`run-q1-matrix-q2.py`): drives the unchanged Q1 reviewer probe executable (rebuilt against the corrected source) with **Q2-owned output paths** (`rq-*`), reproducing the Q1 statuses without altering any Q1 artifact.

`run-q2.py` runs 19 predicates plus conservation; `run-q2-conflict.py` runs the independent conflict control. All outputs are written under the Q2 evidence directory only.

## 3. Original F-01 reproduction and corrected result

`Q2-P01` reproduces the exact Q1 counterexample with **three distinct PIDs**: publish a valid graph unit with occurrence evidence → remove precisely its published `HetuUnit` record (occurrence rows preserved) → query from a separate reader process.

| Step | Observed |
| --- | --- |
| `Q2-P01-publish` | `Available`, 2 hits, complete |
| `Q2-P01-remove-owner` (`HetuUnit|review:q2-correspondence\nreview.q2.csharp\nClient.csproj` removed) | `CorruptedIndex`, 0 hits, 0 examined, not complete |
| `Q2-P01-fresh-reader` (separate process) | `CorruptedIndex`, 0 hits, 0 examined, not complete, reason "An authority-bearing occurrence unit is orphaned from the published graph units." |

Conservation is exact: `p01_distinct_process_ids = true`, the only changed native key is the removed `HetuUnit` row, and the mutation-process and fresh-reader native records are identical (`q2-conservation.json`). The producer's claimed before/after behavior is independently confirmed.

The full Q1 matrix re-execution (`q2-q1-summary.json`) is **56 invocations, 54 predicates met, 0 strict failures**; the F-01 probe `rq-P36-graph-orphan` now meets its recorded `CorruptedIndex` expectation. The only unmet predicates are `rq-P38-page-missing-id` and `rq-P40-meta-complete` — the previously recorded non-blocking exploratory hypotheses O-01/O-02, which were not rewritten.

## 4. Bidirectional association results

| Probe | Condition | Observed |
| --- | --- | --- |
| `Q2-P02-remove-occurrence` | Occurrence association removed, authority-requiring `Client` graph owner retained (manifest kept consistent) | `CorruptedIndex`, 0 hits |
| `Q2-P02-fresh-reader` | same, separate process | `CorruptedIndex`, 0 hits |
| `Q2-P02b-corrupt-accounting` | Occurrence manifest `UnitCount` inflated over units | `CorruptedIndex`, 0 hits |
| `Q2-P06c-add-marker-no-occurrence` | Published non-occurrence-plugin graph unit with `RequiresOccurrenceAuthority = true` and no association | `CorruptedIndex`, 0 hits |
| `Q2-P06-correct` / `Q2-P05-reopen` | Correctly associated owners | `Available`, 2 hits, complete |
| `Q2-P06b-add-graph-only` | Non-authority (marker-false) graph unit alongside authority units | `Available`, 2 hits, complete (no false refusal) |
| `Q2-P06d-missing-current` | Association valid but current authority omitted | `AnalysisUnavailable`, 0 hits |
| `Q2-P06e-stale-context` | Transitive source change, stable target | `StalePublication`, 0 hits |

Both directions of the R1 rule are enforced before positive availability, across plugins, and without false corruption for valid or marker-false states.

## 5. Retirement and complete-empty semantics

- `Q2-P03-retire`: accepted staged deletion of `Client` retires its graph owner and occurrence contribution together; the unaffected `Client2` owner remains `Available`, 1 hit, complete, including after a fresh reopen.
- `Q2-P03b-retire-both`: retiring all fact-bearing owners leaves the verified complete-empty `Empty` owner → `Available`, 0 hits, **`IsComplete = true`**, both in-process and after reopen.
- `Q2-P06d-missing-current`: the same complete-empty publication with current authority omitted → `AnalysisUnavailable`. A genuinely complete empty result is therefore distinguishable from unavailable coverage and from corrupted state.

## 6. Durable reopen verification

Every mutation scenario closes the mutating store and reads from a new process (`Q2-P01-remove-owner`/`Q2-P01-fresh-reader`; `Q2-P05-reopen`; `Q2-P03-retire-fresh`; `Q2-P03b-empty-fresh`; `Q2-P02-fresh-reader`). The refusal is produced by reconstruction from persisted `AndiritOccurrenceUnit`/`AndiritOccurrenceMeta`/`HetuUnit` records — not from a process-local cache — and is derived by the same gate for both the mutating process and the fresh reader (`p01_reader_rows_equal_mutation = true`). No durable row is modified by a refused read.

## 7. Regression matrix

| Verification | Observed result | Evidence |
| --- | --- | --- |
| Q2 independent probe matrix | **19/19** | `q2-summary.json` |
| Q1 56-probe matrix (Q2-owned re-run) | **54/56 met, 0 strict failures**; F-01 probe passes | `q2-q1-summary.json` |
| Conflict refusal control | **6/6** (mixed-universe rejection, stable target, stale mixed query, fresh A available, rows conserved) | `q2-conflict-checks.json` |
| NEW-01 independent probes | **17/0/0** | `q2-new01-tests.log` |
| Full solution | **834/0/0** (LatticeDb 50, TreeSitter 157, core 345, CSharp 282) | `q2-solution-tests.log` |
| Frozen R2 oracle | **185/185**; 146851 bytes; SHA-256 `97F6C5…39C9` unchanged | `oracle-r2.json` |
| Format (`--verify-no-changes`) | exit 0 | `q2-format-verify.log` |
| Release / Debug builds | 0 warnings / 0 errors each | `q2-build-release.log`, `q2-build-debug.log` |

The Q1 matrix re-execution independently confirms preserved stale-context refusal (P05–P07, P31), before-commit/abort/unknown-outcome behavior (P08–P11, P15), replacement/baseline ordering (P12–P14, P16), accepted retirement (P17–P19), graph-only vs complete-empty (P20–P23), staged resumption (P24–P26, P41–P44), missing/unavailable/corrupted states (P27–P35), and Partial coverage (P39). Native mirror consistency and D3/D4/D5 are covered by the unchanged `NativeGraphMirrorTests`, `HistoricalStorageIdentityTests`, and the C# lifecycle suites inside the 834-test solution run.

## 8. New findings

### O-Q2-01 — Non-blocking: additive marker correspondence is one-directional for present owners

**Observation:** clearing the additive `RequiresOccurrenceAuthority` marker on an **existing** graph owner (owner present, not retired) while retaining its authority-bearing occurrence unit yields `Available`/2/complete (`q2-adversarial.json`). The gate's direction-1 check requires the owner to *exist*; direction 2 only checks owners whose marker is `true`. It does not require an authority-bearing occurrence unit's owner to be marker-true.

**Impact assessment:** this is **not** F-01-class. Unlike an orphaned/retired owner, no stale or invalid occurrence is served: the occurrence unit's recorded defining authority is still independently validated against the current verified live compilation before any hit is returned. The observation requires corrupting an existing graph record's additive marker (a different external corruption than the Q1 removal scenario) and only weakens the graph-marker↔authority bijection claim, not the no-stale/no-orphan guarantee. It is recorded as a bounded residual limitation and a candidate for a future separately-commissioned hardening (a symmetric "authority-bearing occurrence ⇒ marker-true owner" clause); it is not required to close F-01 and no production fix was made.

No other gap was found in the adversarial checks: the correspondence uses persisted `(plugin, unit)` identities validated against the durable keys; the per-unit verifier rejects ownership, payload, membership and context mismatches; the check runs before positive availability; no occurrence meta/units exist without a manifest (open throws); a removed manifest with surviving units throws at open; and both projections are reconstructed from the same committed state under the write gate.

## 9. Preserved guarantees

Retained and independently re-exercised: matching/stale authority, accepted retirement, complete-empty vs unavailable, staged-resumption containment (Q1-Q1), known-failure recovery and unknown-outcome quarantine, conflict refusal, coverage/refusal accounting, native mirrors, and D3/D4/D5; the frozen oracle output is byte-identical. No previously accepted guarantee was weakened; no unrelated publication was destroyed by a refusal (`p04_unrelated_occurrence_rows_preserved` / `p04_unrelated_graph_rows_preserved` both true).

## 10. Remaining qualifications

- **Q1-Q1** (segment resumption: occurrence facts/carrier absent from segments; graph-only resumption refuses rather than fabricating verified empty) remains open and is preserved unchanged.
- **Q1-Q2** (recovery-rebuild internal-failure atomicity is not proven beyond the accepted fault surface) remains open.
- **S-01** (caller-supplied component-digest canonicalization/proof) remains S2.5 scope.
- **O-Q2-01** as described above.
- Windows-only execution scope; no abrupt-kill/power-loss, arbitrary-crash, cross-platform, unrestricted-concurrency, or multiple-writer guarantee is claimed. P38/P40 remain the recorded O-01/O-02 exploratory results.
- This review is bounded to WP-S23-2; it does not evaluate WP-S23-3 or general Find Usages.

## 11. Evidence manifest and integrity receipts

`manifest.json` binds 196 Q2 evidence files by sorted relative path, byte length and SHA-256 (excluding itself and the `.gitignore`/`.gitattributes`), and includes the integrity receipt file. `q2-integrity.json` records the reviewed commits/trees, the one-file production change scope, the empty `src` diff after remediation, the unchanged production blob hashes, the preserved Q1/R1/producer-handoff blob hashes, the empty review worktree diff for `src`/`tests`, and the frozen oracle digest (`146851` bytes / `97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9`). No Q1/Q2/R1 historical artifact was rewritten. No push occurred.

## 12. Final disposition and recommendation

**`SUPPORTED WITH QUALIFICATIONS`.**

F-01 is independently closed: after R1, an occurrence whose accepted published graph owner has been removed is refused (`CorruptedIndex`, zero hits, zero examined, not complete, meaningful reason) in the mutating process and in an independent fresh process, with exact native-row conservation, while valid, complete-empty, unaffected-owner, replaced, graph-only, staged-resumption, stale, conflicting and quarantined states retain their accepted semantics. The 56-probe Q1 matrix re-execution shows the F-01 probe now passing with no strict failures.

**Recommended acceptance boundary (for the owner, not granted here):** accept the R1 remediation `1bce70ee2f52ed5100a2e68bc004462f9824d2f2` (tree `a6a908ab69740e4ee114e845cdde9374975dc15a`) as closing F-01 within the accepted Windows execution scope, carrying Q1-Q1, Q1-Q2, S-01 and O-Q2-01 as explicit qualifications. Do not extend acceptance to crash consistency, power-loss recovery, or cross-platform behavior. Do not begin WP-S23-3 on this decision. The next decision belongs to the owner.
