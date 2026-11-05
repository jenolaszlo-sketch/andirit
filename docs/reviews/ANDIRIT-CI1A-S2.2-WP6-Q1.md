# ANDIRIT-CI1A-S2.2-WP6-Q1 — Independent NEW-04 coverage merge qualification

**Commission:** `ANDIRIT-CI1A-S2.2-WP6-Q1` (independent production qualification only).
**Repository:** Andirit. **Canonical branch:** `main`. **Date:** 2026-11-05.
**Starting HEAD (verified):** `4fb20878114d8109f8c7ee1839b48f65d522881b` (tree `5a32bc25e7d47637df9bb7565b65c901c526c8d1`), clean, one worktree, **5 ahead / 0 behind**, nothing pushed.
**Implementation under review:** `6b45bdecf9d9e49a24b3b001b20bcfca72a10afd` (tree `bc8b35863eb4b66de35365739f355e8afe807d2a`) with evidence `4fb2087`.

**Overall disposition: `SUPPORTED WITH QUALIFICATIONS`.**

All four documented NEW-04 defects are independently closed; the full mandatory matrix is satisfied; no permitted merge, storage, sink, or restore path was found to fabricate complete coverage, lose refusal evidence, or misattribute evidence to a scope it does not belong to. Three bounded qualifications (G-Q1A2, G-Q1E1, G-Q1A7) plus the Lattice legacy-flag assessment are recorded for the owner. Owner acceptance is not granted here.

---

## 1. Baseline and Git integrity

Full identities re-resolved: `e0d9110`/`ba9338d7…`, `4fdc23c`/`55b5e990…`, `d3da2e0`/`029299bf…`, `97c88e5`/`27cda088…`, `f1e7f27`/`794d3a3d…`, `25b5b31`/`7c4b870d…`, `6b45bde`/`bc8b3586…`, `4fb2087`/`5a32bc25…`. Parent chain consecutive; no superseding WP6 decision; no concurrent writer.

## 2. Independent NEW-04 contract reconstruction

| Rule | Authority |
| --- | --- |
| State conjunction: unavailable dominates; partial dominates; complete+not-analyzed → partial; only complete+complete complete | `coverage-and-availability.md` §1–§2 |
| Mixed-scope merge cannot claim `Produced` | S2.2 entry-acceptance owner decision (falsification probe list, L121) |
| Differing non-null universes → merge **refused** | same list ("differing-universe merge refused") |
| `declared=0/verified>0` rejected | same list; register NEW-04 lists the exemption as a defect |
| Unit counts: max is the listed defect; contributions are disjoint batches/units | register NEW-04; `CodeOccurrenceCoverage` doc comment |
| Occurrence/refusal counters and `UnavailableReasons` combine by sum | coverage-and-availability §2 (observations) |
| Merge yields coverage only; never facts or identities | WP5-OWNER §8 WP6-3; the type system (`Merge` returns `CodeOccurrenceCoverage`) |
| Legacy unattributed contributions (null identities) are a store compatibility reality | store code paths (`CoverageForRestore`, sink defaults); authority is silent → bounded boundary, not invented algebra |

## 3. Original defect comparison

- **Historical (read-only):** `git show 25b5b31:src/Andirit.Abstractions/CodeOccurrence.cs` confirms all four defects exactly (max unit counts; `ScopeId ?? other.ScopeId` first-selection; no universe refusal; `&& declaredUnitCount != 0` exemption).
- **Producer pre-fix evidence:** `CI1A-S2.2-WP6-IMPL-01-evidence/wp6-matrix-before-fix.log` — 9/20 failing, all four defect cases among them. Independently consistent with the committed historical source.
- **Corrected behavior reproduced currently:** the reviewer matrix below re-establishes each corrected rule from independent expectations (not from the production merge).

## 4. Scope/universe qualification matrix (Q1-A)

30 reviewer probes (`CI1A-S2.2-WP6-Q1-tests/`), all passing:

| Case | Result |
| --- | --- |
| Same scope + universe, disjoint | `Produced`, attribution kept, sums correct |
| Different scopes, same universe (both orders) | `Partial`, `ScopeId=null` — never complete, never single-scope |
| Same scope, different universes | **refused**, both orders |
| Different scopes and universes | refused (universe check first) |
| Null scope + one attributed | sole attribution kept; completeness degrades (`Partial`) |
| Mixed-scope intermediate + further complete record | **never regains `Produced`** (`Partial`, counts accumulate) |
| Null-universe `Produced` + attributed `Produced` | folds into the sole attributed universe (G-Q1A7 qualification) |
| Three-way mixed scopes, three merge orders | `Partial` in every order; attribution label order-dependent but never a completeness claim |
| Reversed incompatible universes | refused both orders |

**G-Q1A2 (QUALIFICATION — report/implementation discrepancy).** The committed `Merge` nulls **both** `ScopeId` **and** `UniverseId` whenever scopes are mixed (`mixedScope ? null : …` applies to the universe argument too), discarding a *matching, non-conflicting* universe attribution. The implementation report §4 described only scope-nulling plus the state cap; the authority (register/entry-acceptance) is silent on the universe under mixed scope. The behavior is conservative — no false claim, no arbitrary first-selection, state containment intact — but loses true universe information on mixed-scope merges. Reproduction: `Q1A2_DifferentScopesSameUniverse_NeverCompleteNorSingleScope` (observed `UniverseId == null`). Not a mandatory-rule violation; the owner should explicitly accept the conservative behavior or commission the smallest correction (preserve the universe when the two inputs agree).

## 5. Counter aggregation matrix (Q1-B)

Distinct, unequal values per counter expose max-vs-sum: `InvocationFacts` 1+2, `External` 7+1, `Ambiguous` 6+2, `Unresolved` 5+3, `Unsupported` 4+4, `ConditionalInactive` 3+1, `UnsupportedCategories` 2+1, declared 3+1, verified 3+1 — all sum exactly; no counter omitted; refusal evidence never lost. `UnavailableReasons` merge by key (4+2, 1+0). Zero contribution (`NotProduced`, all zero): degrades completeness per the conjunction and leaves counts intact. **Overflow boundary:** `int.MaxValue` unit counts self-merged → the summed negative count is rejected by the constructor (`ArgumentOutOfRangeException`) — the contract does not define overflow arithmetic and the observed behavior is **fail-closed** (no wraparound acceptance). **Overlapping contributions:** `Merge` is documented for disjoint contributions; production consumers merge only distinct store owners (`RepositoryCoverage` iterates dictionary values; the sink accumulates per origin; republication replaces rather than merges), so accidental overlap is not reachable through current consumers; self-merge doubles accounting without manufacturing completeness (documented boundary).

## 6. `declared=0` validation (Q1-C)

Theory matrix: 0/0 valid; 1/0 valid; 1/1 valid; 0/1 **rejected**; 1/2 **rejected** (all with the invariant's established `ArgumentException`); mixed 0/0 + 2/1 → 2/1 `Partial`; empty-complete (0/0 `Produced`) remains a valid complete-empty; empty-complete + empty-partial → `Partial` (not complete). Constructor and merge validation are consistent: sums of valid inputs cannot violate `verified<=declared`, and incompatible universes cannot enter through any input accepted by the constructor.

## 7. P06 false-`Produced` results (Q1-D)

| Case | Result |
| --- | --- |
| Two `Produced`, different scopes (the P06 class) | `Partial`, unattributed, never complete |
| `Produced` + `Partial`, same scope | `Partial` even when counts sum to verified==declared (2/2 + 1/1 → 3/3 `Partial`) |
| Complete + refused contributions | stays `Produced` **with refusal counters intact** — refusal categories are recorded observations, not incompleteness signals (accepted WP5 semantics, locked by probe) |
| Different scopes, matching counts | still `Partial` — matching numbers cannot rescue mixed attribution |
| Mixed-scope intermediate + complete | cannot regain `Produced` |
| Zero-declaration combinations | no completeness manufactured |
| Positive control (same scope, disjoint) | `Produced` with exact sums — legitimate merging is preserved, not universally downgraded |

## 8. Store, sink, and restore integration (Q1-E)

Probes drive the real `InMemoryCodeGraphStore`, `CodeGraphIngestionSink`, and `LatticeCodeGraphStore` paths:

| Path | Result |
| --- | --- |
| Store: two same-scope units (1/1 + 2/2, `Produced`) | query coverage `Produced`, declared=3, verified=3, facts=2, attribution kept, `IsComplete` |
| Store: mixed-scope units (P06 through the real aggregation) | `Partial`, `ScopeId=null`, `IsComplete=false`, availability not `Available` |
| Store: legacy unattributed unit + attributed unit | `Partial`; legacy facts retained; completeness not manufactured |
| **Store: differing universes across units** | **query fails closed** — `ArgumentException` from the mandated merge refusal propagates out of `FindOccurrencesAsync` (G-Q1E1 below) |
| Sink: same-origin batch accumulation | complete + complete → `Produced` 3/3 committed to the store |
| Sink: mixed-scope accumulation | replacement coverage `Partial` — the P06 class cannot enter through the sink |
| Lattice persist → reopen → query | same-scope publication round-trips `Produced` 3/3; mixed-scope publication round-trips `Partial`, `IsComplete=false` |

**G-Q1E1 (QUALIFICATION).** A repository whose published units carry different non-null universes makes occurrence **queries throw** (the mandated merge refusal surfaces at the `RepositoryCoverage` aggregation; publication itself succeeds). This is fail-closed — no false coverage, no silent suppression — but consumer-visible as an exception. Cross-universe units within one repository are anomalous for current producers (one analysis → one universe), but multi-plugin repositories could reach it. The owner should record the fail-closed surfacing; a bounded store-side mapping of the refusal to unavailable coverage would be a separate, future production change.

**Lattice legacy `CompleteCoverage` flag (Q1-E, resolving the WP6-IMPL flag):** the flag is only *read* (`LatticeCodeGraphStore.cs:596`) and only *defined* (default `false`); `git log -S "CompleteCoverage = true"` is empty across all history — no code path has ever produced it. The `WithState(Produced)` restore branch is therefore unreachable for any record current code can write; no published legacy corpus exists. **Bounded schema-compatibility qualification, not a reachable false-completeness path**; round-trip probes confirm sound restore behavior.

## 9. Refusal and F09 interactions (Q1-F)

Refusal counters (external/ambiguous/unresolved/unsupported/conditional-inactive) sum and survive every aggregation; `Unsupported` and `UnsupportedCategories` are treated as the overlapping measures they are (both preserved; disjointness not assumed). F09 heuristic counts remain counts: they aggregate, never become facts, never influence the state rule, and cannot manufacture completeness or hide incompleteness. The WP5 F2 qualification is preserved; the scanner was not touched.

## 10. Frozen identity and source-integrity assessment (Q1-G)

- `git diff 25b5b31..6b45bde -- src/Andirit.Abstractions/CodeOccurrence.cs` contains **no** occurrence of `ComputeTargetId`, `ComputeOccurrenceId`, `ComputePayloadHash`, the `Hash` helper, `asmctx3`, `usage4`, or `compctx2` — the changed hunks are exactly the constructor invariant and `Merge` (+ doc comment).
- The `.editorconfig` scoped `charset = utf-8-bom` rule for `CodeOccurrence.cs` is intact.
- Frozen oracle files: `git diff 25b5b31..HEAD -- docs/planning/adr-ci1-01-ci1a-s2-0/oracle` is empty.
- The file's SHA-256 necessarily changed (`78FBD358…` → `A8EF3403A1190CC7C2773C87A8F571A718A8E02CDD0582A096DE11F88B1F2085`); the frozen *formulas* are semantically unchanged, proven by the diff scope plus the frozen oracle below.
- Frozen R2 oracle: **185/185**; output `97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9` == accepted evidence; expected `274A7AAE9EB6724879D619416E449C52759B44085D7B718DAD3434B15387CBBF` unchanged.

## 11. Regression and determinism evidence (Q1-H)

Windows x64, SDK `10.0.401`, runtime `10.0.12`.

| Suite | Result |
| --- | --- |
| WP6-Q1 reviewer probes | **30 / 0 / 0** (25 facts + 5 theory cases) |
| NEW-04 producer matrix | 20 / 0 / 0 |
| `Andirit.Tests` | 345 / 0 / 0 |
| Whole solution | **768 / 0 / 0** (43 + 157 + 345 + 223) |
| WP5-Q2 / WP5-Q1 / WP4-Q1 / G-MINT-Q1 probes | 22, 16, 13, 12 — all green |
| WP5-R1 / WP5 / WP4 / WP3 targeted | 6, 14, 20, 16 — all green |
| Frozen R2 oracle | 185/185 with accepted hashes |
| `dotnet format Andirit.slnx --verify-no-changes` | exit 0 |
| `dotnet build -c Release` | 0 warnings, 0 errors |

Determinism: a canonical multi-field merge digest (`WP6Q1-DIGEST|state=Produced|scope=S1|universe=U1|declared=3|verified=3|facts=2|ext=2|amb=3|unres=4|unsup=5|cond=6|cats=7|complete=True`) is identical across separate processes.

## 12. Platform limitations

All reviewer qualification is Windows x64. Hosted CI covers the solution suites on three OSes only after the next owner-controlled push; reviewer probe projects run locally on Windows. No Linux/macOS reviewer qualification is claimed.

## 13. Findings and severity

| ID | Severity | Finding |
| --- | --- | --- |
| G-Q1A2 | **QUALIFICATION** | Mixed-scope merge also discards a matching universe attribution (report/implementation discrepancy; authority-silent; conservative; no false claims). Smallest correction if desired: preserve the universe when both inputs agree. |
| G-Q1E1 | **QUALIFICATION** | Cross-universe units within one repository make occurrence queries fail closed (mandated refusal surfaces at the store aggregation boundary). No false coverage; consumers must handle the exception. |
| G-Q1A7 | **QUALIFICATION** | Sole-attribution boundary folds an unattributed `Produced` record into an attributed universe; unreachable from current production producers (they always attribute, or are `Partial` legacy records). |
| G-Q1FLAG | **QUALIFICATION** | Lattice legacy `CompleteCoverage` restore flag: never producible (`git log -S` empty; only read); unreachable false-completeness branch; schema-compat only. |
| G-Q1OVF | **NON-BLOCKING** | Counter overflow fails closed (constructor rejection); contract silent; no wraparound acceptance. |
| — | **UNSUPPORTED HYPOTHESIS (dismissed)** | "Always Partial" concern: the positive controls prove legitimate same-scope complete merging is preserved. |

## 14. Recommended WP6 owner disposition

**Accept WP6 with explicit qualifications.** The four NEW-04 defects are closed; the mandatory matrix passes; the store/sink/restore paths preserve the coverage guarantees; the frozen authority is intact. Record the four qualifications above (G-Q1A2 conservative universe handling — accept explicitly or commission the smallest correction; G-Q1E1 fail-closed query surfacing for cross-universe repositories; G-Q1A7 sole-attribution boundary; G-Q1FLAG legacy flag reachability). The NEW-04 disposition owner decision (scope §6.9 item 4) is then closed as "corrected and independently qualified". WP7 (Option A §5.2 tests 2/6/8) remains separate.

## 15. Exact final commits and trees

Reviewed at `4fb2087` (tree `5a32bc25…`), implementation `6b45bde` (tree `bc8b3586…`). This review adds only `docs/reviews/ANDIRIT-CI1A-S2.2-WP6-Q1.md`, `docs/reviews/CI1A-S2.2-WP6-Q1-tests/`, and `docs/reviews/CI1A-S2.2-WP6-Q1-evidence/`; production, producer tests, accepted decisions, and frozen data are unchanged (`git diff 4fb2087 -- src tests docs/decisions docs/planning` empty). Final review commit/tree and repository status are recorded in the handoff; nothing was pushed.

## 16. Remaining obligations for WP7 and later

- WP6-OWNER: NEW-04 disposition + qualification acceptance (with the §13 qualifications).
- WP7: Option A invalidation/refusal incl. §5.2 tests 2/6/8 (test 6 remains open, allocated to WP7).
- WP8: integration/comparison over the accepted S2.0/R2 corpus with frozen expected results.
- S2.3: native persistence/reopen (the "if published" merge-interaction tests activate only then).
- Publication/release: separately controlled; nothing is pushed by this review.

---

**Final disposition: `SUPPORTED WITH QUALIFICATIONS`.**
