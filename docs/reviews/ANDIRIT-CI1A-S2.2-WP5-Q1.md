# ANDIRIT-CI1A-S2.2-WP5-Q1 — Independent `usage4`, extraction and coverage qualification

**Commission:** `ANDIRIT-CI1A-S2.2-WP5-Q1` (independent production qualification only).
**Project:** Andirit. **Date:** 2026-11-05. **Canonical branch:** `main`.
**Implementation under review:** `a5f04fda495a5f54d95fd3f6a313620334a61155`.
**Implementation evidence under review:** `4890f30b9ca0b445835840c3a5b33e072f631631`.
**Starting HEAD (verified):** `4890f30b9ca0b445835840c3a5b33e072f631631` (tree `2645e79875298da334d79e51e51f539aee4a3fae`).

**Overall disposition: `REMEDIATION REQUIRED`.**

One **BLOCKING** defect was reproduced: the extractor emits a **trusted `usage4`
fact for an ambiguous invocation** by falling back to `CandidateSymbols.FirstOrDefault()`
when Roslyn overload resolution is ambiguous. This violates the accepted refusal
contract (stage2-acceptance-matrix §5.3; scope §6.2 "no confirmed fact for …
ambiguous …") and produces unauthorized trusted occurrence authority.

WP5 is therefore **not** ready for owner acceptance. This report does not grant owner
acceptance and does not repair production code.

---

## 1. Baseline, ancestry and tree verification

| Role | Commit | Tree | Verified |
| --- | --- | --- | --- |
| R2 foundation | `e0d911055f4cc9bdbf9aa4cc338f5cc007737a22` | `ba9338d74bf59c584bef340dbda49ed46eb1c5d9` | ✅ |
| G-CONFIRM owner | `812584dc56e11983c3ea7de8977fd1cd74ff0297` | `7167557024e1602c81b5e6b169455c0ad10a5864` | ✅ |
| WP3 implementation | `ff58a9290a7583d4493d177e5de5aa6315904a85` | `85cdcedeb261c656893cdd7c2e3f21b741a25dc6` | ✅ |
| WP4 implementation | `eac302295b5bbaf8d71181ddcc70a62089a9a28e` | `9a4ccefc182d5b80123def1dce313350c37e23d6` | ✅ |
| WP4-Q1 qualification | `6ecf9962216b3c7a3c7330da856383c5c5b0dde0` | `c0ce3df6dafe5383cb12c683597c71728ded35e7` | ✅ |
| WP4 owner | `97c88e58851820468cd21ee66d7a8ba460baf23a` | `27cda08808b4b56ccd6524b82b8315666636bd8d` | ✅ |
| WP5 implementation | `a5f04fda495a5f54d95fd3f6a313620334a61155` | `c58781497560cae4bd8fbd420d83b0b41350fb58` | ✅ |
| WP5 evidence | `4890f30b9ca0b445835840c3a5b33e072f631631` | `2645e79875298da334d79e51e51f539aee4a3fae` | ✅ |

Consecutive parent chain; every tree reproduced; working tree clean; one worktree; no
stashes; `origin/main` = `e0d9110`; `main` **17 ahead, 0 behind**; nothing pushed; no
concurrent writer observed.

---

## 2. Independent WP5 contract reconstruction

Reconstructed from accepted repository documents (not the truncated commission):

- **Scope** — `scope-and-entry-criteria.md` §6.2 WP5: "Coverage/refusal emission and
  unconditional-inactive scanner (F09) with no confirmed fact for
  inactive/external/ambiguous/unresolved/unsupported/incomplete."
- **Extraction** — `stage2-boundary.md` §3.6–3.7; `stage2-acceptance-matrix.md` §4:
  supported category `Invocation`; identifier-token span; declaration span; source hash
  = actual indexed bytes; other kinds → `UnsupportedCategories++`, no fact.
- **Refusal/coverage** — `stage2-acceptance-matrix.md` §5: `Confirmed`, `External`,
  `Ambiguous`, `Unresolved`, `Unsupported`, `ConditionalInactive`, `Incomplete`;
  `Ambiguous` = "two live bindings without a unique defining compilation → refusal"
  and, per the oracle N01 fixture, a genuinely ambiguous **invocation** is
  `Resolution.Ambiguous` (no fact).
- **F09** — obligation register F09: "actual disabled-text scanning bound to
  language/preprocessor configuration"; `ConditionalInactive` coverage, no fact.
- **Coverage dimensions** — `coverage-and-availability.md`: `Produced`/`Partial`/
  `NotProduced`; a zero-fact `Produced` is a valid complete-empty, a zero-fact
  `Partial` is not.
- **WP4-OWNER §9** W5-1…W5-10 (binding WP5 entry table).
- **Option A §5.1/§5.2**; mapping contract §3/§6.

No mandatory WP5 requirement was found omitted by the truncated prompt within the
accepted WP5 scope. Integration into the graph/ingestion pipeline is **WP8**, not WP5.

---

## 3. Trusted-emission path inventory

| Path | Authority? | Notes |
| --- | --- | --- |
| `VerifiedCSharpUniverse.MintUsage` (`VerifiedCSharpAnalysis.cs:187`) | **Yes — WP5 authority** | Only mints after `MintTarget` succeeds |
| `VerifiedCSharpUniverse.MintTarget` (`:143`) | WP4 authority | Consumed by `MintUsage` |
| `CodeOccurrenceFact.Create` (`CodeOccurrence.cs:345`, public) | No — structural | Accepted REM-01 factory; computes `usage4` from supplied inputs; claims no association authority |
| `CodeOccurrenceIdentity.ComputeOccurrenceId`/`ComputeTargetId` (public) | No — structural | Pure constructors |
| `VerifiedCSharpOccurrenceExtractor.Extract` | Emitter | Routes every candidate through `MintUsage` |
| `InMemoryCodeGraphStore`/`LatticeCodeGraphStore`/`CodeGraphIngestionSink` `Merge` | No — Stage-1 publication | Not used by the extractor (NEW-04 deferred) |

No alternative production path emits a trusted fact bypassing `MintUsage` **by design**;
however, the extractor itself violates the refusal contract (§4). The public structural
constructors are not themselves authority (they match the accepted W2 boundary).

---

## 4. BLOCKING finding — ambiguous invocation emits a trusted `usage4`

**F1 (BLOCKING).** `src/Andirit.CSharp/VerifiedCSharpOccurrenceExtractor.cs:57-59`:

```csharp
var info = model.GetSymbolInfo(invocation);
var symbol = info.Symbol as IMethodSymbol ?? info.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();
if (symbol is null) { unresolved++; continue; }
```

When overload resolution is ambiguous, `GetSymbolInfo().Symbol` is `null` and
`CandidateSymbols` holds the equally-applicable candidates. The fallback selects an
**arbitrary candidate**, resolves its defining compilation (`Complete`), mints an
`asmctx3`, and emits a trusted `usage4`. The occurrence is then counted as neither
`Ambiguous` nor `Unresolved`.

**Reproduction** (`Wp5_Ambig_Invocation_MustNotEmitTrustedFact`, probe
`docs/reviews/CI1A-S2.2-WP5-Q1-tests/Wp5ReviewerProbes.cs`):

```csharp
public sealed class M {
  public int Amb(int a, long b) => a + (int)b;
  public int Amb(long a, int b) => (int)a + b;
}
public static class Site { public static int Run() { var m = new M(); return m.Amb(1, 1); } }
```

Observed (`probe-observations.txt`): `WP5Q1 ambig facts=1 ambiguous=0 unresolved=0`;
the emitted fact carried `TargetId = asmctx3:sha256:0c656f8a…` and
`TargetDocumentId = M:N.M.Amb(System.Int32,System.Int64)`.

Independent expected (matrix §5.3; oracle N01 `@N01 m.Amb(1,1)` = `Ambiguous`):
**no fact**; coverage `Ambiguous >= 1`.

**Impact.** Unauthorized trusted emission for a refused occurrence — a mandatory WP5
condition. Uses the exact ambiguity pattern the accepted oracle classifies as
`Ambiguous`.

**Smallest bounded correction.** In `Extract`, do not fall back to `CandidateSymbols`
for minting. Recommended:

```csharp
var symbol = info.Symbol as IMethodSymbol;
if (symbol is null)
{
    if (info.CandidateSymbols.Length > 1) ambiguous++;
    else unresolved++;
    continue;
}
```

This is a one-statement change confined to `VerifiedCSharpOccurrenceExtractor.Extract`.
No WP3/WP4/authority change is required. A producer regression test using the oracle
N01 ambiguity pattern should accompany it.

**Requalification obligations.** After correction: re-run the WP5-Q1 ambiguity probe;
add/keep a producer test for ambiguous invocation; re-run WP5 targeted, C# suite, whole
solution, and the frozen R2 oracle (185/185, `97F6C5E5…`); re-confirm the other
findings remain unaffected.

---

## 5. `usage4` formula and authority tests

Positive controls passed (independent expected values from the frozen R2 authority +
an independently written tuple):

| Probe | Result |
| --- | --- |
| `Wp5_DirectInvocation_EmitsExactAcceptedTuple` | emitted `usage4` == independent expected; `asmctx3` == expected; no `compctx2` |
| `Wp5_CrossProject_MultipleDependencies_ExactTargets` | two dependencies → two facts with the correct defining `asmctx3` |
| `Wp5_MultipleInvocations_NoCollisionOrDedup` | two same-target sites → two distinct `usage4`, one target id |
| `Wp5_SeparateProcessDigest` | emitted == expected; identical across two processes (`4dca58df…`) |

Authority-consumption probes passed: `Wp5_StructuralTargetId_NotInjectable` (null /
empty / fabricated / malformed recorded context → no fact; valid → fact);
`Wp5_Mint_BypassPathInventoriesNoDirectEmission`; `Wp5_SubstitutedInstance_NoFact`.
The distinction between metadata-equivalent target identity and exact defining-instance
authority is preserved. `compctx2` isolation holds.

---

## 6. Extractor correctness matrix

`Wp5_Extraction_Matrix` (observed `facts=12 unsupported=2 unresolved=2`) confirms:
static/instance, overloaded, generic, extension (reduced), chained, nested, conditional
(`?:`), async, and calls inside lambdas/local functions are extracted; local-function
and delegate invocations are `Unsupported`. `Wp5_InvocationLikeTextInActiveStrings_NotExtracted`
confirms string-literal text is not extracted.

**Gap:** genuinely ambiguous invocations are **not** classified as `Ambiguous` /
`Unresolved` but emitted as facts (F1). Supported calls are not silently skipped.

---

## 7. F09 disabled-text heuristic

| Probe | Ground truth | Observed | Impact |
| --- | --- | --- | --- |
| disabled real invocation, `DEBUG` off | 1 inactive | `ConditionalInactive=1`, no fact | correct |
| `DEBUG` on | 0 inactive | fact emitted, `ConditionalInactive=0` | correct |
| `#if/#else` (one branch active) | 1 inactive | 1 fact, `ConditionalInactive>=1` | correct |
| generic `H.Id<int>(1)` in disabled text | 1 inactive | `ConditionalInactive=1` | correct |
| **comment + string + real in disabled text** | 1 inactive invocation | **`ConditionalInactive=4`** | **false-positive coverage inflation** |

**F2 (QUALIFICATION).** The heuristic matches invocation-like text inside disabled
comments and string literals, inflating `ConditionalInactive`. It never emits a fact, so
this is a coverage-precision limitation, not unauthorized emission. The F09 register
("contract closure is not scanner completion") and the producer's documented bounded
scanner make this a **qualification**; the WP5 owner should decide the acceptable bound.
It is not classified BLOCKING because the accepted F09 contract does not require
compiler-level precision and no trusted fact is affected.

---

## 8. Coverage/refusal accounting

| Probe | Observed |
| --- | --- |
| mixed corpus (3 confirmed, 1 unresolved, 1 unsupported) | `facts=3 unsupported=1 unresolved=1 produced=Produced` |
| zero occurrence, complete scope | no facts, `Produced`, `IsComplete` |
| incomplete universe | `Partial`, `IsComplete == false` |
| unsupported categories | `Unsupported` and `UnsupportedCategories` both incremented |

**F3 (QUALIFICATION).** One unsupported occurrence increments **both** `Unsupported`
and `UnsupportedCategories`. The acceptance matrix §5.5 specifies
`UnsupportedCategories++`; the additional `Unsupported++` may overstate the
`Unsupported` counter. This is a bounded accounting ambiguity (no fact effect); report
the observable relationship rather than inventing a disjointness invariant.

**F4 (NON-BLOCKING).** The `External` counter is unreachable through `ResolveDefining`
(construction-stage refusal, accepted GM-5/F4); external targets surface as
`Unresolved` via `MissingDefiningCompilation`. Consistent with the accepted boundary.

`Produced`/`Partial` classification is correct; a valid fact coexisting with unsupported
occurrences is handled; counters and status are deterministic.

---

## 9. `targetDocumentId` collision analysis

`Wp5_TargetDocumentId_OverloadsDistinct` (`targetDocuments=M:N.H.M(System.Int32),
M:N.H.M(System.String)`) confirms overloads receive distinct documentation ids and
distinct `usage4`. The `GetDocumentationCommentId() ?? Name` fallback is reached only
for supported symbols whose doc id is null; even then, `usage4` also includes the
defining `asmctx3` (identifier/declaration/shape digests), so a `targetDocumentId`
collision alone cannot collide two `usage4` values. **F5 (NON-BLOCKING):** no
demonstrated collision reaching a trusted emission; bounded qualification.
Local functions (null doc id candidates) are `Unsupported` and never minted.

---

## 10. Option A §5.2 results

| §5.2 | WP5 result |
| --- | --- |
| 1 conditional defining API change | ✅ (WP4-Q1) preserved through `usage4` |
| 2 transitive-only change | ✅ (WP4-Q1) |
| 3 caller unchanged; target follows API → `usage4` | ✅ exercised (`Wp5_DirectInvocation…`) |
| 4 restore → original identity | ✅ (WP4-Q1) |
| 5 distinct defining compilations | ✅ (WP4-Q1) |
| 6 equivalent reference artifact | ✅ same-input profile; cross-profile is a G-MINT comparison item (not closed) |
| 7 re-ingestion determinism | ✅ `Wp5_SeparateProcessDigest` |
| 8 stale publication refused (in-memory) | ✅ `Wp5_StructuralTargetId_NotInjectable`; native reopen S2.3 |
| 9 deterministic mapping | ✅ |
| 10 no minting from unverified/incomplete | ✅ (except the F1 ambiguity bypass — refused classes otherwise) |

Test 6's cross-profile obligation is **not** silently closed. Complete Option A §5.2
acceptance is not claimed.

---

## 11. Separate-process determinism

`Wp5_SeparateProcessDigest` run in two independent processes (fresh graphs): identical
`usage4` (`4dca58df…`), `asmctx3` (`c448587b…`), fact count, and independent expected
(MATCH). Coverage counts/status are deterministic (`Wp5_Coverage_*`).

---

## 12. Regression and oracle evidence

Environment: Windows x64, .NET SDK `10.0.401`, runtime `10.0.12`.

| Suite | Actual |
| --- | --- |
| WP5-Q1 independent probes | **15 / 1 / 0** (the failing test is the F1 ambiguity probe) |
| WP5 targeted (producer) | 14 / 0 / 0 |
| WP4 targeted | 20 / 0 / 0 |
| WP4-Q1 probes | 13 / 0 / 0 |
| WP3 targeted | 16 / 0 / 0 |
| WP3-Q1 probes | 19 / 0 / 0 |
| G-MINT-Q1 probes | 12 / 0 / 0 |
| `Andirit.CSharp.Tests` | 217 / 0 / 0 |
| Whole solution | 742 / 0 / 0 |
| Frozen R2 oracle | 185 / 185, output `97F6C5E5…`, expected `274A7AAE…` |

The producer's 14 WP5 tests **pass** yet miss the F1 ambiguity defect — a demonstration
that passing suites are not sufficient.

---

## 13. WP5/WP6 boundary

`git grep '\.Merge('` shows `Merge` only in `LatticeDb`/`InMemory` stores and
`CodeGraphIngestionSink` (Stage-1 publication) — **not** used by the WP5 extractor,
which constructs `CodeOccurrenceCoverage` directly. No hidden NEW-04 dependency; no
persistence/reopen introduced; WP5 does not claim WP6 coverage semantics.

---

## 14. Findings

| ID | Severity | Finding | Reproduction |
| --- | --- | --- | --- |
| **F1** | **BLOCKING** | Ambiguous invocation emits a trusted `usage4` via `CandidateSymbols.FirstOrDefault()`; `ambiguous=0`. | `Wp5_Ambig_Invocation_MustNotEmitTrustedFact`; `probe-observations.txt` |
| **F2** | **QUALIFICATION** | F09 heuristic inflates `ConditionalInactive` for invocation-like text in disabled comments/strings (observed 4 vs 1); never emits facts. | `Wp5_F09_Matrix` |
| **F3** | **QUALIFICATION** | One unsupported occurrence increments both `Unsupported` and `UnsupportedCategories`; the contract specifies `UnsupportedCategories++`. | `Wp5_Coverage_MixedCorpus_DisjointAndNoLoss` |
| **F4** | **NON-BLOCKING** | `External` counter unreachable via `ResolveDefining` (construction-stage refusal). | inventory §8 |
| **F5** | **NON-BLOCKING** | `targetDocumentId` `Name` fallback is not globally unique, but overloads are distinct and `usage4` also binds `asmctx3`; no demonstrated collision. | `Wp5_TargetDocumentId_OverloadsDistinct` |
| **F6** | **NON-BLOCKING** | Trust boundary: `CodeOccurrenceFact.Create`/`ComputeTargetId`/`ComputeOccurrenceId` are public structural constructors (accepted REM-01); they are not authority. | inventory §3 |
| **F7** | **QUALIFICATION** | Extractor requires repository-relative `tree.FilePath` and uses `contentHash = sourceHash`; absolute paths would throw; both documented/bounded. | code review |
| **F8** | **NON-BLOCKING** | A malformed caller-supplied component digest throws `ArgumentException` mid-extraction (caller error; fail-closed). | `Wp5_StructuralTargetId_NotInjectable` |

Inherited limitations (not new WP5 defects): F4, F5, F6, and the S-01 caller-supplied
component-digest qualification. New WP5 limitations: F1 (blocking), F2, F3, F7.

---

## 15. Recommended owner disposition

**`REMEDIATION REQUIRED`.** WP5 is **not** ready for owner acceptance. The smallest
bounded correction is the single-statement extractor change in §4 (do not mint from
`CandidateSymbols`; classify `Ambiguous`/`Unresolved`). Requalification must re-run the
F1 ambiguity probe plus WP5 targeted, C# suite, whole solution and the frozen oracle,
and re-confirm F2–F8 are unaffected. No WP3/WP4/G-MINT authority change is needed; no
WP6 work is involved.

---

## 16. Final commits, trees, and working-tree state

Reviewed objects: starting HEAD `4890f30` (tree `2645e798…`), implementation `a5f04fd`
(tree `c587814…`). The reviewer adds only
`docs/reviews/ANDIRIT-CI1A-S2.2-WP5-Q1.md`,
`docs/reviews/CI1A-S2.2-WP5-Q1-tests/` and
`docs/reviews/CI1A-S2.2-WP5-Q1-evidence/`. Production source, producer tests, frozen
oracle and accepted decisions are unchanged; `git status` is otherwise clean. The
review commit/tree and final HEAD/ahead-behind/push status are recorded in the handoff.

---

**Final disposition: `REMEDIATION REQUIRED`** — F1 is a demonstrated blocking violation
of a mandatory WP5 refusal condition. No owner acceptance is granted; no production code
was modified; no WP6 work was started.
