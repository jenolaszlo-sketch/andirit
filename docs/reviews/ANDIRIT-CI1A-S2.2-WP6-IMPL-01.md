# ANDIRIT-CI1A-S2.2-WP6-IMPL-01 — NEW-04 coverage merge correction and qualification preparation

**Commission:** `ANDIRIT-CI1A-S2.2-WP6-IMPL-01` (bounded production implementation).
**Repository:** Andirit. **Canonical branch:** `main`. **Date:** 2026-11-05.
**Baseline:** `25b5b315ef1afa031f8309ea0215d9082499371a` (tree `7c4b870d85471b764dac38bce72af90841f837a7`), clean tree, one worktree, **3 ahead / 0 behind**, nothing pushed — verified before any change.

**Disposition: `IMPLEMENTED — READY FOR INDEPENDENT QUALIFICATION`.**

---

## 1. Baseline commit and tree

As above; the full authority chain (`e0d9110 → … → 2127a9a → f10b472 → f1e7f27 → 25b5b31`) was re-resolved from Git with full hashes and trees, matching the WP5-OWNER record. No superseding WP6 decision exists; no concurrent writer was observed.

## 2. Authoritative contracts consulted

| Source | Rule used |
| --- | --- |
| Obligation register NEW-04 (`adr-ci1-01-ci1a-s2-obligation-register.md` L82) | Defects: first scope/universe selection; max-count aggregation; `declared=0` exemption from `verified<=declared`. Required matrix: matching/different scope, matching/different universe, verified empty vs missing evidence / no analysis / invalid zero declarations, partial+complete, changed membership, native persistence/reopen **if published** |
| S2.2 entry-acceptance owner decision (`…s2-2-entry-acceptance-owner-01.md` L121) | Falsification probes: *P06-style mixed-scope merge cannot claim `Produced`; `declared=0/verified>0` rejected; matching-scope merge; **differing-universe merge refused**; verified-empty vs missing-evidence/no-analysis; partial+complete; changed membership; native persistence/reopen only if published (S2.3)* |
| `coverage-and-availability.md` | Three dimensions; state conjunction: *any unavailable → unavailable; any partial → partial; complete + not analyzed → partial; only complete + complete is complete*; zero-fact `Produced` is a valid complete-empty |
| WP5 owner decision §8 (`…s2-2-wp5-owner-01.md`) | WP6-1…WP6-8 entry contract; test 6 belongs to WP7; heuristic F09 counts must not be silently reinterpreted |
| S2.0 case-matrix NEW-04 row | Model-level assertion only ("two incompatible coverage scopes must not be merged as authoritative"); *not* an oracle for the defect |
| S2.1 authorization boundary §4 | The defective merge must not be used as a provenance oracle; provenance requires explicit membership inventories |

Contract-to-implementation mapping (every merge rule → authority):

| Merge rule | Authority |
| --- | --- |
| State = worst-case conjunction | coverage-and-availability §1/§2 |
| Occurrence/refusal counters and `UnavailableReasons` sum | coverage-and-availability §2 (observations across contributions); register (max defect listed only for scope/universe/unit counts) |
| `DeclaredUnitCount`/`VerifiedUnitCount` **sum** across disjoint contributions | register: max-count is the listed defect; units are disjoint batches (doc comment "several batches or units"); sums preserve `verified<=declared` when each input satisfies it |
| Differing non-null `UniverseId` → `ArgumentException` | entry-acceptance L121: "differing-universe merge **refused**" |
| Differing non-null `ScopeId` → merge allowed, `ScopeId=null`, `State` capped at `Partial` | entry-acceptance L121: "mixed-scope merge cannot claim `Produced`"; register: no arbitrary first-selection; commission §5: separate scopes are not one scope |
| One null + one non-null identity → keep the sole attribution | No conflicting identities exist; legacy store contributions deliberately carry none (documented boundary, tested) |
| Constructor rejects `verified>declared` unconditionally | entry-acceptance L121: "`declared=0/verified>0` rejected"; register lists the exemption as a defect |

## 3. Original NEW-04 defect reproduction (before correction)

`wp6-matrix-before-fix.log`: the 20-test producer matrix ran **9 / 11 / 0 (failed/passed/skipped)** against the unmodified `Merge`. All four register defects reproduced:

| Defect | Reproduction | Before-fix behavior |
| --- | --- | --- |
| Max-count aggregation | `OriginalDefect_UnitCountsMustSum_NotMax` (2/1 + 2/2, same scope) | `declared=2, verified=2` — hid one unverified unit |
| First scope/universe + P06 | `OriginalDefect_MixedScopes_NeverClaimProducedOrSingleScope` (two `Produced` records, different scopes) | `Produced` attributed to the first scope |
| No universe refusal | `OriginalDefect_DifferingUniverses_MergeRefused` | merged, kept the first universe |
| `declared=0` exemption | `OriginalDefect_ZeroDeclaredWithObservedVerified_Rejected` (`verified=2, declared=0`) | accepted silently |

The remaining five before-fix failures are the same max-vs-sum class asserting summed unit counts.

## 4. Exact merge rules implemented

`src/Andirit.Abstractions/CodeOccurrence.cs` (+22/−8; `implementation-diff.txt`):

1. **Universe refusal:** different non-null `UniverseId` → `ArgumentException`.
2. **Mixed-scope containment:** different non-null `ScopeId` → merged record gets `ScopeId=null` (no single-scope attribution) and `State` capped at `Partial` even when both inputs are `Produced`.
3. **Unit counts summed** (`DeclaredUnitCount`, `VerifiedUnitCount`) instead of `Math.Max`; sums preserve the `verified<=declared` invariant for valid inputs.
4. **Attribution fallback kept only for the sole-attribution case** (`ScopeId ?? other.ScopeId` when not mixed) — legacy unattributed contributions combine with an attributed record under that record's identity (documented, tested).
5. **Constructor invariant unconditional:** `verifiedUnitCount > declaredUnitCount` always throws (`ArgumentException`, the invariant's established type).
6. State conjunction (`Worse`) and occurrence/refusal/`UnavailableReasons` summation unchanged (already contract-conformant).

Identity formulas (`ComputeTargetId`, `ComputeOccurrenceId`, `ComputePayloadHash`, fact construction) are untouched — verified by diff and by the frozen oracle below.

## 5. `declared=0` behavior

- `declared=0, verified=0` — valid (zero declarations, no observations); `Produced` remains a valid complete-empty for an empty declared scope.
- `declared=0, verified>0` — **rejected** at construction (`ArgumentException`), closing the exemption.
- Mixed zero/non-zero declarations sum correctly (0+2=2).
- Zero declarations are never equated with successful completeness: `Produced` requires the state conjunction, and observed evidence without declarations is now unrepresentable.

## 6. P06 false-`Produced` prevention

- Mixed-scope merges cap at `Partial` and lose single-scope attribution (`IsComplete == false`) — the P06 probe class cannot pass.
- Incomplete evidence cannot become complete: the state conjunction is unchanged and sums cannot reduce refusal evidence.
- Refused occurrences cannot disappear: refusal counters sum (tested across `Produced`/`Partial` mixes).
- **Positive control:** matching scope + universe, disjoint batches (2/2 + 1/1) → `Produced` with `declared=3, verified=3` — legitimate merging still works; the fix is not "always Partial".

## 7. Counter and refusal preservation

`RefusalCounters_PreserveAndSum`, `UnsupportedCategories_Sum`, `UnavailableReasons_Sum`: all counters sum; `UnavailableReasons` merges by key. No counter is dropped, and no counter influences the state rule (coverage observations are not incompleteness signals).

## 8. F09 interaction (WP6-5)

`HeuristicConditionalInactive_IsCountedNeverConverted`: heuristic `ConditionalInactive` counts sum across merges, are never converted into facts (`InvocationFacts` unchanged), and never affect the state rule — an overcount can neither manufacture completeness nor hide incompleteness. The WP5 F2 qualification (heuristic count, not exact disabled-invocation count) is preserved; the scanner was not redesigned.

## 9. Authority preservation (WP6-E)

`Merge` returns a `CodeOccurrenceCoverage` only — no `CodeOccurrenceFact`, `CodeOccurrenceTargetId`, or `usage4` can be produced by it (`Merge_ReturnsCoverageOnly_NoFactAuthority`). No new fact-emission path was added; `MintTarget`, `MintUsage`, `VerifiedCSharpOccurrenceExtractor.Extract`, the Roslyn ambiguity refusal, and the frozen formulas are untouched (`git diff 2127a9a..HEAD -- src` shows only `CodeOccurrence.cs`, confined to the constructor invariant + `Merge`). The WP5 extractor constructs coverage directly and does not call `Merge` (W5-8 maintained; `Coverage_IsConstructedWithoutMerge` still passes).

## 10. Complete NEW-04 test matrix (after correction: 20 / 0 / 0)

`tests/Andirit.Tests/S2_2Wp6CoverageMergeTests.cs` — `wp6-matrix-after-fix.log`:

| Matrix case | Test | Result |
| --- | --- | --- |
| Max-count defect → sum | `OriginalDefect_UnitCountsMustSum_NotMax` | ✅ |
| Mixed scope → no `Produced`, no single-scope claim | `OriginalDefect_MixedScopes_NeverClaimProducedOrSingleScope` (symmetric) | ✅ |
| Differing universe → refused | `OriginalDefect_DifferingUniverses_MergeRefused` | ✅ |
| `declared=0/verified>0` → rejected | `OriginalDefect_ZeroDeclaredWithObservedVerified_Rejected` | ✅ |
| Positive control (matching scope, disjoint) | `PositiveControl_MatchingScopeCompleteBatches_ProduceComplete` | ✅ |
| partial+complete → Partial | `PartialPlusComplete_IsPartial` | ✅ |
| complete+not-analyzed → Partial | `CompletePlusNotAnalyzed_IsPartial` | ✅ |
| not-produced+not-produced | `NotProducedPlusNotProduced_IsNotProduced` | ✅ |
| unavailable dominance | `UnavailableDominates` | ✅ |
| Refusal counters | `RefusalCounters_PreserveAndSum` | ✅ |
| `UnsupportedCategories` | `UnsupportedCategories_Sum` | ✅ |
| `UnavailableReasons` | `UnavailableReasons_Sum` | ✅ |
| Zero declarations (valid) | `ZeroDeclared_NoObservations_IsValid` | ✅ |
| Mixed zero/non-zero declarations | `MixedZeroAndNonZeroDeclared_Sum` | ✅ |
| `verified<=declared` still valid | `VerifiedWithinDeclared_RemainsValid` | ✅ |
| Unattributed + attributed (legacy boundary) | `UnattributedPlusAttributed_KeepsSoleAttribution` | ✅ |
| Unattributed + unattributed | `UnattributedPlusUnattributed_StaysUnattributed` | ✅ |
| F09 heuristic interaction | `HeuristicConditionalInactive_IsCountedNeverConverted` | ✅ |
| Repeated equivalent inputs | `RepeatedEquivalentInput_DoublesAccountingNeverCompleteness` | ✅ |
| Authority boundary | `Merge_ReturnsCoverageOnly_NoFactAuthority` | ✅ |

"Changed membership" is exercised at the record level through differing declared/verified counts under one scope (sum accounting); unit-identity-level membership changes require the persisted store layer and belong to the store-level qualification scope. Algebraic properties (associativity/commutativity) are not mandated by accepted authority and are not claimed; the sums/conjunction are order-insensitive for compatible inputs, which the matrix exercises in both orders where attribution matters.

## 11. Regression results

| Suite | Result |
| --- | --- |
| WP6 NEW-04 matrix (new) | **20 / 0 / 0** |
| `Andirit.Tests` (incl. store/sink merge paths) | **345 / 0 / 0** (325 + 20) |
| Whole solution | **768 / 0 / 0** (43 + 157 + 345 + 223; baseline 748 + 20) |
| WP5-R1 regressions / WP5 targeted / WP4 / WP3 | 6, 14, 20, 16 — all 0 failed |
| WP5-Q2 / WP5-Q1 / WP4-Q1 / G-MINT-Q1 probes | 22, 16, 13, 12 — all 0 failed |
| `dotnet format Andirit.slnx --verify-no-changes` | exit 0 |
| `dotnet build -c Release` | 0 warnings, 0 errors |

The store-level merge consumers (`InMemoryCodeGraphStore.RepositoryCoverage`, `LatticeCodeGraphStore` restore, `CodeGraphIngestionSink`) pass unchanged — the correction makes their unit accounting accurate without altering their state handling. No existing test encoded the defective max/first-scope/exemption behavior (all previously passing suites remain green).

## 12. Frozen oracle integrity

S2.0 oracle **185/185, 0 failed**; output SHA-256 `97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9` == accepted evidence; expected `274A7AAE9EB6724879D619416E449C52759B44085D7B718DAD3434B15387CBBF` unchanged. The NEW-04 corpus fixture is a model-level assertion and is unaffected.

## 13. Platform limitations

Local qualification: Windows x64 (SDK `10.0.401`). Hosted CI covers the solution suites (including the new WP6 tests) on three OSes after the next push, but reviewer probe projects are Windows-local only. Cross-platform reviewer qualification remains with WP6-Q1.

## 14. Remaining qualifications and notes

- Native persistence/reopen is tested **only if published** (S2.3) per the register; no publication occurred, so reopen interaction with the corrected merge is untested and out of scope.
- The Lattice restore path re-applies a persisted `CompleteCoverage` flag via `WithState(Produced)` for records published before this correction; no published legacy corpus exists, and post-correction publications derive the flag from sound coverage. Flagged for WP6-Q1 attention.
- `Merge` remains defined for **disjoint** contributions; combining a record with itself doubles the accounting without manufacturing completeness (documented boundary, tested).
- F09 remains a bounded heuristic (F2); no exact disabled-code parsing was added.

## 15. Recommended WP6-Q1 review targets

1. Re-derive all four original defects independently and confirm refusal/containment.
2. Adversarial mixed-scope and differing-universe matrices, including symmetric order and three-way combinations.
3. `declared=0` boundary (0/0 valid; 0/verified>0 rejected; sums with zero).
4. Sum-vs-max on every counter; refusal evidence preservation; `UnavailableReasons` keys.
5. P06-style false-`Produced` attempts through store-level paths (`RepositoryCoverage`, sink, Lattice restore) with mixed scopes.
6. F09 overcount interaction with merge and availability.
7. Authority boundary: merge produces no facts/identities; WP5 extractor path and frozen formulas unchanged (oracle `97F6C5E5…`).
8. Legacy `CompleteCoverage` restore flag re-application on mixed-scope merges.
9. Regression: solution ≥ 768, oracle 185/185, format gate.

---

**Final disposition: `IMPLEMENTED — READY FOR INDEPENDENT QUALIFICATION`.** No WP6-Q1, owner decision, WP7, or publication work was performed.
