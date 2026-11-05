# ANDIRIT-CI1A-S2.2-WP5-Q2 — Independent requalification after F1 remediation

**Commission:** `ANDIRIT-CI1A-S2.2-WP5-Q2` (independent requalification; no production changes).
**Repository:** Andirit. **Canonical branch:** `main`. **Date:** 2026-11-05.
**Starting HEAD (verified):** `443c192733a53d2f1da415743debb84e8c81acb5` (tree `4c80956d4533667e8f91a10a0950e4315b6ca04e`), `main == origin/main` (0/0), clean tree, one worktree, no stashes.
**Remediation under review:** `2127a9a22c5a54b32b028aea5a947205f6acca06` (tree `85f28f3ccfcebf2b0d6ae12936777eb4c8e98688`) with evidence `443c192`.

**Overall disposition: `SUPPORTED WITH QUALIFICATIONS`.**

F1 is independently closed. WP5 is ready for owner acceptance with explicit, bounded qualifications. No new blocking defect was found; no production file was modified by this review (`git diff 443c192 -- src tests docs/decisions docs/planning` is empty; `CodeOccurrence.cs` SHA-256 `78FBD358…` unchanged). Owner acceptance itself is not granted here.

---

## 1. Verified Git baseline and ancestry

| Role | Commit | Tree |
| --- | --- | --- |
| Frozen R2 foundation | `e0d911055f4cc9bdbf9aa4cc338f5cc007737a22` | `ba9338d74bf59c584bef340dbda49ed46eb1c5d9` |
| WP3 owner | `4fdc23cfd3d760639dd653ad92bb204376a08c05` | `55b5e990cfa16606d7ba09752d18d360bf01db08` |
| G-MINT owner | `d3da2e009154402a92275874ac4bb94426b97df3` | `029299bf277bc99212ea2ded311c0ec54f70fa09` |
| WP4 owner | `97c88e58851820468cd21ee66d7a8ba460baf23a` | `27cda08808b4b56ccd6524b82b8315666636bd8d` |
| WP5 implementation | `a5f04fda495a5f54d95fd3f6a313620334a61155` | `c58781497560cae4bd8fbd420d83b0b41350fb58` |
| WP5 implementation evidence | `4890f30b9ca0b445835840c3a5b33e072f631631` | `2645e79875298da334d79e51e51f539aee4a3fae` |
| WP5-Q1 review | `958d98ac0fc782e659734f8e56889784df9fdbfb` | `9496cf2633ff77fb17af0d5462c0d4fb78cd3431` |
| CI formatting restoration | `64ee765e6f87961063d2631576d8e29865e093de` | `4f295c685b730c7682e8136abfc1f9e1f81cae67` |
| WP5-R1 remediation | `2127a9a22c5a54b32b028aea5a947205f6acca06` | `85f28f3ccfcebf2b0d6ae12936777eb4c8e98688` |
| WP5-R1 evidence | `443c192733a53d2f1da415743debb84e8c81acb5` | `4c80956d4533667e8f91a10a0950e4315b6ca04e` |

Parent chain `443c192 → 2127a9a → 64ee765 → 958d98a → 4890f30 → a5f04fd → 97c88e5 → … → e0d9110` verified consecutive; nothing pushed by this review.

## 2. F1 original defect and remediation comparison

- **Original defect (WP5-Q1 F1):** `info.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault()` selected an arbitrary overload when `GetSymbolInfo().Symbol == null`, minting a trusted `usage4` for ambiguous (and single-candidate) non-bindings.
- **Remediation (verified from `git show 2127a9a`):** only a uniquely resolved `info.Symbol` establishes the target; `Symbol == null` → `CandidateSymbols.Length > 1` ⇒ `Ambiguous`, otherwise `Unresolved`; no fact. One file, +11/−2. `MintTarget`, `MintUsage`, `ResolveDefining`, frozen identity, formulas, `compctx2` validation, and F09 are untouched (verified by diff against `443c192`).

## 3. Independent candidate-resolution matrix

Fresh probes (`docs/reviews/CI1A-S2.2-WP5-Q2-tests/Wp5Q2ReviewerProbes.cs`, 22 tests) pin the actual Roslyn premise (`Symbol`, `CandidateSymbols`, `CandidateReason`) before asserting extractor behavior. All expected `usage4`/`asmctx3` values are independently reconstructed from the frozen R2 authority; the extractor is never used as its own oracle.

| # | Case | Roslyn premise (observed) | Extractor outcome | Contract verdict |
| --- | --- | --- | --- | --- |
| 1 | Two ambiguous overloads (`Amb(int,long)`/`Amb(long,int)`, `Amb(1,1)`) | `Symbol=null`, 2 candidates | no fact, `Ambiguous=1`, `Unresolved=0` | ✅ F1 closed (F1-A) |
| 2 | Three candidates (`Pick(null)` over three unrelated classes) | `Symbol=null`, 3 candidates | no fact, `Ambiguous=1` | ✅ |
| 3 | Candidate order swapped | first candidate *differs* between compilations | both refuse, identical counters | ✅ order-independent |
| 4 | One non-applicable candidate (`Only("x")`) | `Symbol=null`, 1 candidate, `OverloadResolutionFailure` | no fact, `Unresolved=1`, `Ambiguous=0` | ✅ |
| 5 | Zero candidates (`Missing.Type.Invoke()`) | `Symbol=null`, 0 candidates | no fact, `Unresolved=1` | ✅ |
| 6 | Dynamic receiver (`d.Direct(1)`, oracle N03 pattern) | `Symbol=null`, 0 candidates | no fact, `Unresolved=1` | ✅ |
| 7 | Inaccessible member (`private Hidden()` called cross-class) | `Symbol=null`, **1 candidate, `Inaccessible`** | no fact, `Unresolved=1` | ✅ — see G1 below |
| 8 | Inapplicable candidate pair (`Add("a")`) | `Symbol=null`, 2 candidates, `OverloadResolutionFailure` | no fact; conservatively counted `Ambiguous` | ✅ safety; see G2 |
| 9 | Valid unique overload (`Add(1,2)` vs 2- and 3-param) | unique `Symbol` | exact fact, `Ambiguous=0`, `Unresolved=0` | ✅ |
| 10 | Generic (`Id<int>(3)`) | unique constructed symbol | exact fact; constructed doc id recorded | ✅ — see G3 |
| 11 | Mixed valid/ambiguous/unresolved | — | 1 exact fact, `Ambiguous=1`, `Unresolved=1`, disjoint | ✅ |

**G1 (NON-BLOCKING, supports F1 closure).** Inaccessible members are a distinct Roslyn non-binding class (`reason=Inaccessible`, single candidate). Pre-R1, `FirstOrDefault()` would have minted a trusted fact for these too; post-R1 they refuse as `Unresolved`. The R1 correction therefore closed an additional latent authority path beyond the reported counterexample. Reproduction: `Q2_InaccessibleMember_UniqueBindingObservation`, observed `facts=0 unresolved=1`.

**G2 (QUALIFICATION).** Every multi-candidate non-binding is counted `Ambiguous`, including `OverloadResolutionFailure` pairs where no candidate is applicable (`Add("a")`). The accepted matrix (§5.3) defines `Ambiguous` over *multiple live defining bindings*; Roslyn-level non-applicable pairs are not separately specified. The mandatory property — no trusted emission without a unique binding — holds in every observed case; only the refusal label is conservative. Bounded, recorded; no remediation required.

**G3 (QUALIFICATION, refines F5).** Constructed generic methods record the *constructed* symbol's documentation id (`M:N.H.Id``1(System.Int32)`), while the definition's is `M:N.H.Id``1(``0)`; symbols without doc comments fall back to `Name` (F5). Distinct constructions yield distinct doc ids, and `usage4` independently binds the defining `asmctx3`, so no identity collision was demonstrated.

## 4. Original reviewer probe results

Unmodified WP5-Q1 probe suite (`wp5-q1-probes.log`): **16 / 0 / 0** — including `Wp5_Ambig_Invocation_MustNotEmitTrustedFact`, which failed before R1 and now passes with `facts=0 ambiguous=1 unresolved=0`.

## 5. New adversarial probe results

Q2 suite: **22 / 0 / 0** (`q2-probes-run1.log`, details in `q2-probe-details-run1.log`).

## 6. Authority-boundary verification

| Probe | Result |
| --- | --- |
| Recorded `compctx2`: null / empty / malformed / fabricated / stale | all refuse (`IsMinted=false`, `Fact=null`) |
| Valid recorded context | mints via `MintUsage → MintTarget` |
| Metadata-equivalent substituted instance | refuses despite equal descriptor digests (G1/W3 exact-instance authority preserved) |
| Structural identifiers | `ComputeOccurrenceId`/`ComputeTargetId` constructible, but the extraction request surface accepts no precomputed fact/id (`Q2_StructuralIdentifiers_CarryNoMintingAuthority`); fabricated context still refuses |
| Alternate emission routes | none found; every fact in every probe traces to `Extract → MintUsage → MintTarget` |

S-01 caller-supplied component digests remain an inherited, explicit limitation (F8).

## 7. Coverage and refusal assessment

| Input | Observed |
| --- | --- |
| Zero occurrences | 0 facts, `Produced`, `IsComplete` |
| Only valid | facts only, `Produced` |
| Only ambiguous (2 sites) | 0 facts, `Ambiguous=2` |
| Only refused (ambiguous + unresolved) | 0 facts, `Ambiguous=1`, `Unresolved=1`, `Produced` (state tracks universe completeness; refusals remain visible in counters — accepted coverage-and-availability semantics) |
| Mixed | 1 exact fact + disjoint refusal counters |
| Repeated extraction | identical facts and counters |
| Supported + unsupported mix | 1 fact, `Unsupported=2`, `UnsupportedCategories=2` (F3 overlap reobserved) |

No double counting within any category; no silent loss; `Produced` vs `Partial` correct.

## 8. F2–F8 dispositions (all reobserved, none upgraded)

| Finding | Reobservation | Disposition |
| --- | --- | --- |
| F2 (F09 overcount) | **Sharpened instance:** a single real disabled call yields `ConditionalInactive=2` because declaration-shaped text (`Run(`) also matches the bounded regex; comment/string noise yields 4 (matches Q1). Disabled text never mints (verified in all F09 probes). | **QUALIFICATION** — bounded documented heuristic |
| F3 (counter overlap) | `Unsupported == UnsupportedCategories` on identical occurrences | **QUALIFICATION** |
| F4 (external unreachable via `ResolveDefining`) | unchanged | **NON-BLOCKING** |
| F5 (`targetDocumentId` fallback) | constructed generics and doc-id-less symbols hit the fallback; no collision reaching `usage4` (G3) | **QUALIFICATION** |
| F6 (structural ≠ authority) | re-probed; holds | **NON-BLOCKING** |
| F7 (repo-relative path / `contentHash = sourceHash`) | unchanged by R1 (modified branch does not touch spans/hashes) | **QUALIFICATION** |
| F8 (caller-supplied digests) | unchanged (S-01) | **QUALIFICATION / inherited** |

F09/NEW-04 boundary: no `CodeOccurrenceCoverage.Merge` dependency in the extraction path; WP6 remains deferred and unclaimed.

## 9. Formula and determinism evidence

- Emitted `usage4` == independently reconstructed value for direct, static, instance, overloaded, generic, cross-project, and repeated-same-target cases; `asmctx3` == frozen-formula reconstruction including cross-project dependencies.
- Separate processes (fresh compilations each run): `WP5Q2-USAGE4=45319e68c4333ced39cc6b87fe47b25c92b6c55f0f8e24e5d142c948d7b863f4`, `WP5Q2-ASMCTX3=asmctx3:sha256:1676b914…` — **MATCH** across runs, emitted == independent expected.
- `compctx2` isolation held in every probe (recorded context never leaks into identities).

## 10. Option A §5.2

- Test 3 (`usage4` follows an API change through the target identity): exercised cross-project with independently expected `asmctx3`/`usage4` — satisfied.
- Test 6 cross-profile comparison: **not closed** — remains a separately allocated comparison obligation; no cross-profile claim is made.
- No unauthorized output when mandatory association checks fail (authority probes above). Complete S2.2 acceptance is not claimed.

## 11. Regression results and frozen oracle integrity

Environment: Windows x64, SDK `10.0.401` (runtime `10.0.12`); all commands in the evidence logs.

| Suite | Result |
| --- | --- |
| Q2 independent probes | 22 / 0 / 0 |
| WP5-Q1 independent probes (unchanged) | 16 / 0 / 0 |
| R1 producer regressions | 6 / 0 / 0 |
| WP5 producer tests | 14 / 0 / 0 |
| WP4 producer tests | 20 / 0 / 0 |
| WP4-Q1 probes | 13 / 0 / 0 |
| WP3 producer tests | 16 / 0 / 0 |
| WP3-Q1 probes | 19 / 0 / 0 |
| G-MINT-Q1 probes | 12 / 0 / 0 |
| `Andirit.CSharp.Tests` | 223 / 0 / 0 |
| Whole solution | **748 / 0 / 0** (325 + 223 + 157 + 43) |
| Frozen R2 oracle | **185/185**; output `97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9` == accepted; expected `274A7AAE9EB6724879D619416E449C52759B44085D7B718DAD3434B15387CBBF` unchanged |
| `dotnet format Andirit.slnx --verify-no-changes` | exit 0 |
| `dotnet build -c Release` | 0 warnings, 0 errors |

Hosted CI coverage vs local qualification: prior hosted runs execute the solution suites (including the six R1 regressions) on Windows/Ubuntu/macOS, but **not** the reviewer probe projects; all reviewer qualification evidence here is locally executed on Windows x64.

## 12. Explicit platform limitations

Local qualification is Windows x64 only; cross-platform execution is evidenced solely by hosted CI of the solution suites, not by reviewer probes.

## 13. Final findings and severity

| ID | Severity | Finding |
| --- | --- | --- |
| G1 | NON-BLOCKING | R1 also refuses inaccessible-member bindings (`reason=Inaccessible`, 1 candidate) — an additional authority path closed by the remediation |
| G2 | QUALIFICATION | All multi-candidate non-bindings (including inapplicable pairs) are conservatively labelled `Ambiguous`; no trusted emission in any case |
| G3 | QUALIFICATION | Constructed-generic/doc-id-less targets record constructed doc id or `Name`; `usage4` remains collision-free via `asmctx3` binding (refines F5) |
| F2 | QUALIFICATION | F09 counts declaration-shaped text in disabled regions (2 for 1 real disabled call); never mints |
| F3 | QUALIFICATION | `Unsupported`/`UnsupportedCategories` overlap reconfirmed |
| F4–F8, S-01 | NON-BLOCKING / QUALIFICATION | unchanged |

## 14. WP5 owner-acceptance recommendation

**Recommend owner acceptance of WP5 with explicit qualifications**, carrying forward: the inherited G-MINT/WP4 qualifications and S-01 (caller-supplied digests); F2 (F09 bounded heuristic, no fact from disabled text), F3 (counter overlap), F5/G3 (bounded `targetDocumentId` fallback), F7 (path/hash assumptions), F8, and G2 (conservative `Ambiguous` labelling of multi-candidate non-bindings). F1 is closed; no remediation is required before the owner decision. WP6 (NEW-04) and the Option A §5.2 test-6 cross-profile comparison remain outside this recommendation.

## 15. Final commit/tree identities and repository status

Reviewed at `443c192` (tree `4c80956d…`). This review adds only `docs/reviews/ANDIRIT-CI1A-S2.2-WP5-Q2.md`, `docs/reviews/CI1A-S2.2-WP5-Q2-tests/`, and `docs/reviews/CI1A-S2.2-WP5-Q2-evidence/`; production, producer tests, prior reviewer probes, frozen oracle, and accepted decisions are unchanged. Final review commit/tree and repository status are recorded in the handoff; nothing was pushed.

---

**Final disposition: `SUPPORTED WITH QUALIFICATIONS`** — F1 independently closed; WP5 ready for owner acceptance; no additional remediation identified; accepted WP5 scope unchanged.
