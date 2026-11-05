# ANDIRIT-CI1A-S2.2-WP5-R1 — Bounded remediation of the WP5-Q1 F1 ambiguity authority defect

**Commission:** `ANDIRIT-CI1A-S2.2-WP5-R1` (production remediation; F1 only).
**Repository:** Andirit. **Canonical branch:** `main`. **Date:** 2026-11-05.
**Baseline:** `64ee765e6f87961063d2631576d8e29865e093de` (tree `4f295c685b730c7682e8136abfc1f9e1f81cae67`), `main == origin/main` (0/0), clean tree, one canonical worktree, no stashes — verified before any change.
**Implementation under remediation:** `a5f04fda495a5f54d95fd3f6a313620334a61155` (WP5) as qualified by `958d98ac0fc782e659734f8e56889784df9fdbfb` (WP5-Q1, disposition REMEDIATION REQUIRED, finding F1).

**Disposition: `REMEDIATED — READY FOR INDEPENDENT REQUALIFICATION`.**

The original reviewer counterexample now refuses trusted emission (`facts=0, ambiguous=1, unresolved=0`) while uniquely resolved invocations keep minting the exact accepted `usage4`. Owner acceptance is **not** granted; `WP5-Q2` independent requalification is the next stage.

---

## 1. Verified starting commit and tree

`64ee765e6f87961063d2631576d8e29865e093de` / tree `4f295c685b730c7682e8136abfc1f9e1f81cae67`; `origin/main` identical; working tree clean; exactly one worktree (`D:/projects/Andirit`); no stashes; no concurrent writer. Hosted run `42031657097` (the CI-FORMAT-R1 publication) had completed successfully on this commit.

## 2. Original F1 contract and reproduction

- **Finding (WP5-Q1 §4, F1, BLOCKING):** `VerifiedCSharpOccurrenceExtractor.Extract` fell back to `info.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault()` when `GetSymbolInfo().Symbol` is null, selecting an arbitrary overload, resolving it `Complete`, and emitting a trusted `usage4` for a genuinely ambiguous invocation. Coverage recorded neither `Ambiguous` nor `Unresolved`.
- **Accepted contract:** `scope-and-entry-criteria.md` §6.2 — "no confirmed fact for inactive/external/ambiguous/unresolved/unsupported/incomplete"; `stage2-acceptance-matrix.md` §5.3 — `Ambiguous` = refusal among **multiple live bindings** without a unique defining compilation (oracle fixture `@N01 m.Amb(1,1)`).
- **Reproduction before correction** (`f1-probe-before-fix.log`): the untouched WP5-Q1 probe `Wp5_Ambig_Invocation_MustNotEmitTrustedFact` **failed** with `WP5Q1 ambig facts=1 ambiguous=0 unresolved=0` — the trusted fact selected `M.M.Amb(System.Int32,System.Int64)` arbitrarily.

## 3. Before-fix observed behavior

| Probe | Before fix |
| --- | --- |
| WP5-Q1 `Wp5_Ambig_Invocation_MustNotEmitTrustedFact` | FAIL — `facts=1 ambiguous=0 unresolved=0` |
| New producer regressions R1-T1…T6 (`r1-producer-tests-before-fix.log`) | **4 failed / 2 passed** — T1 (ambiguous), T2 (unresolved→fact via 1-candidate fallback), T5 (mixed), T6 (candidate order) failed; T3 (valid overload) and T4 (cross-project) already passed |

T2's before-fix failure additionally demonstrates commission §1 item 3: a one-candidate overload-resolution failure (`H.Only("x")`, `CandidateSymbols.Length == 1`) was also converted into a trusted fact by the fallback.

## 4. Exact production correction

One file, +11/−2 (`git diff` preserved in `implementation-diff.txt`):

```csharp
var info = model.GetSymbolInfo(invocation);
var symbol = info.Symbol as IMethodSymbol;
if (symbol is null)
{
    // WP5-Q1 F1: only a uniquely resolved symbol may establish the
    // trusted method target. Multiple equally-applicable candidates
    // are an `Ambiguous` refusal; any other unresolved binding stays
    // `Unresolved`. Candidate selection never invents a binding.
    if (info.CandidateSymbols.Length > 1) ambiguous++;
    else unresolved++;
    continue;
}
```

- Only a uniquely resolved `info.Symbol` may establish the method target; the `FirstOrDefault()` candidate fallback is removed.
- `CandidateSymbols.Length > 1` → `Ambiguous`; otherwise (zero candidates, or a single non-applicable candidate) → `Unresolved`. This follows the accepted matrix: ambiguity is defined over *multiple* live bindings; a single failed candidate is an unresolved binding, not an ambiguity. The distinction is recorded here and pinned by R1-T2's premise assertions (0-candidate and 1-candidate cases).
- No double counting: each refused occurrence increments exactly one counter and `continue`s.
- **Unchanged, verified by diff against the baseline:** `MintTarget`, `MintUsage`, `ResolveDefining`, frozen `ComputeTargetId`/`ComputeOccurrenceId`/`CodeOccurrenceFact.Create`, `asmctx3`/`usage4` formulas, recorded-`compctx2` validation, cross-project metadata binding, F09 scanner, S-01 caller-supplied digests. `CodeOccurrence.cs` SHA-256 remains `78FBD358BE875735E3E29A68A7FE8FF22DCE511BA8FC72D1FE9795A6C785B27D`.

## 5. New producer regression tests

`tests/Andirit.CSharp.Tests/S2_2Wp5R1AmbiguityTests.cs` (6 tests, in the normal suite, independent expected values; the extractor is never used to derive expectations):

| ID | Case | Assertions |
| --- | --- | --- |
| R1-T1 | Ambiguous overload (`Amb(int,long)`/`Amb(long,int)`, call `Amb(1,1)`) | premise: `Symbol == null`, 2 candidates; no fact; `Ambiguous == 1`; `Unresolved == 0`; `Produced` |
| R1-T2 | Unresolved: missing receiver (0 candidates) + single non-applicable candidate (`Only("x")`, 1 candidate) | no fact; `Unresolved == 2`; `Ambiguous == 0` |
| R1-T3 | Uniquely resolved overload (`Add(int,int)` vs `Add(int,int,int)`) | exactly one fact; `TargetDocumentId`, `TargetId` (`asmctx3`), and `usage4` equal independently computed values; no false ambiguity |
| R1-T4 | Cross-project invocation (`App` → `Lib`) | fact target = dependency's `asmctx3`; `usage4` = independently computed G1/W3 identity |
| R1-T5 | Mixed source (valid + ambiguous) | exactly one fact — the valid invocation with its exact expected `usage4`; `Ambiguous == 1`; `Unresolved == 0`; `Produced` |
| R1-T6 | Candidate order independence (overloads declared in swapped order) | premise: first candidate *differs* between the two compilations; both extractions emit **no fact**, identical refusal counters |

## 6. After-fix results

| Check | Result |
| --- | --- |
| R1 producer regressions (`r1-producer-tests-after-fix.log`) | **6 / 0 / 0** |
| Original F1 probe (`f1-probe-after-fix.log`) | **PASS** — `WP5Q1 ambig facts=0 ambiguous=1 unresolved=0` (exactly the WP5-Q1 expected result) |
| Full WP5-Q1 probe suite (`wp5-q1-probes-after-fix.log`) | **16 / 0 / 0** (note: the committed probe file defines 16 tests; the WP5-Q1 handoff recorded "15 / 1 / 0" — the same suite, then with the one F1 failure) |

## 7. Full regression results (local, Windows x64)

| Suite | Result |
| --- | --- |
| WP5 targeted (`S2_2Wp5UsageTests`) | 14 / 0 / 0 |
| WP5-R1 regressions (`S2_2Wp5R1AmbiguityTests`) | 6 / 0 / 0 |
| WP4 targeted | 20 / 0 / 0 |
| WP3 targeted | 16 / 0 / 0 |
| WP4-Q1 independent probes | 13 / 0 / 0 |
| WP3-Q1 independent probes | 19 / 0 / 0 |
| G-MINT-Q1 independent probes | 12 / 0 / 0 |
| `Andirit.CSharp.Tests` | **223 / 0 / 0** (217 + 6 new) |
| Whole solution | **748 / 0 / 0** (325 + 223 + 157 + 43; baseline 742 + 6) |

## 8. Frozen R2 authority

Oracle: **185/185, 0 failed** (`oracle-r2.log`); output SHA-256 `97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9` == accepted evidence; expected `274A7AAE9EB6724879D619416E449C52759B44085D7B718DAD3434B15387CBBF` unchanged. `git diff 64ee765e -- src/Andirit.Abstractions src/Andirit.CSharp/VerifiedUsageMint.cs src/Andirit.CSharp/VerifiedCSharpAnalysis.cs docs/decisions docs/planning` is empty; accepted decisions and planning documents untouched.

## 9. Formatter and build

- `dotnet format Andirit.slnx --verify-no-changes --no-restore` → **exit 0** (`format-verify.log`); the scoped `CodeOccurrence.cs` encoding policy from CI-FORMAT-R1 is preserved.
- `dotnet build Andirit.slnx -c Release` → **0 warnings, 0 errors** (`build-release.log`).

## 10. Changed-file inventory

Implementation commit `2127a9a22c5a54b32b028aea5a947205f6acca06` (tree `85f28f3ccfcebf2b0d6ae12936777eb4c8e98688`):
- `src/Andirit.CSharp/VerifiedCSharpOccurrenceExtractor.cs` (+11/−2, F1 correction only)
- `tests/Andirit.CSharp.Tests/S2_2Wp5R1AmbiguityTests.cs` (new, 6 regressions)

Evidence commit (this report + `docs/reviews/CI1A-S2.2-WP5-R1-evidence/`): added after the implementation commit; final identities in the handoff.

## 11. Preserved WP5-Q1 qualifications

| Finding | Disposition after R1 |
| --- | --- |
| F2 (F09 `ConditionalInactive` overcount for invocation-like comments/strings) | **unchanged, QUALIFICATION** — the correction executes only on the `Symbol == null` branch and cannot alter disabled-text scanning; WP5-Q1 `Wp5_F09_Matrix` still passes with identical behavior |
| F3 (`Unsupported`/`UnsupportedCategories` overlap) | **unchanged, QUALIFICATION** — unsupported accounting path untouched |
| F4 (external counter unreachable via `ResolveDefining`), F5 (bounded `targetDocumentId` fallback), F6 (structural constructors are not authority) | **unchanged, NON-BLOCKING** |
| F7 (repository-relative path / `contentHash = sourceHash`), F8 (malformed caller digest throws) | **unchanged, QUALIFICATION / NON-BLOCKING** |
| S-01 caller-supplied component digests | unchanged, inherited |

Observed interaction of the F1 correction with F2–F8: none — the modified branch only runs where `GetSymbolInfo().Symbol` is null, a path F2–F8 do not reach. WP6/NEW-04 remains deferred and unused.

## 12. Remaining platform limitations

- Local qualification: Windows x64, SDK `10.0.401`, runtime `10.0.12`. No additional local platforms.
- The independent WP5-Q1 probe project is **not** part of `Andirit.slnx`, so hosted CI does not execute it; its 16/0/0 result is a local reviewer-artifact run. The producer R1 regressions **are** in the suite and do run in hosted CI.
- Hosted CI result for the published commits is recorded in the final handoff.

## 13. Recommended `ANDIRIT-CI1A-S2.2-WP5-Q2` requalification scope

1. **F1 closure:** re-derive the ambiguity counterexample independently (two equally-applicable overloads) and verify no trusted fact, `Ambiguous` refusal, and no arbitrary overload target in any emitted data.
2. **Multiple-candidate resolution:** candidate-order independence (swapped declarations), plus the single-candidate (`Length == 1`) and zero-candidate cases — confirm `Unresolved` classification and no fact.
3. **Refusal accounting:** disjointness of `Ambiguous`/`Unresolved` per refused occurrence, mixed-source counters, `Produced`/`Partial` status, zero-occurrence and all-refused inputs.
4. **Valid invocation non-regression:** independently recomputed `usage4`/`asmctx3` for direct, overloaded, cross-project, and repeated invocations; separate-process determinism.
5. **Trusted authority boundaries:** null/empty/fabricated/stale recorded `compctx2` refusal; metadata-equivalent vs exact-instance distinction; no path from structural constructors to trusted emission.
6. **Frozen `usage4` semantics:** formula, field order, and `compctx2` isolation unchanged versus accepted WP4/WP5 authority.
7. **Unchanged qualifications:** F09 false-positive matrix (F2), unsupported accounting (F3), and F4–F8 behaviors re-observed as unchanged.
8. Regression: full solution suite (expected ≥ 748), frozen R2 oracle 185/185 with `97F6C5E5…`, CI-equivalent formatting gate.

---

**Final disposition: `REMEDIATED — READY FOR INDEPENDENT REQUALIFICATION`.** No owner acceptance granted; no WP6 work; WP5-Q1 review artifacts unmodified.
