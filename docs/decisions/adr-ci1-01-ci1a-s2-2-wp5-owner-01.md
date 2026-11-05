# ADR-CI1-01-CI1A-S2.2-WP5-OWNER-01 — Qualified owner acceptance of WP5 `usage4` construction, extraction, and coverage/refusal emission

**Commission:** `ANDIRIT-CI1A-S2.2-WP5-OWNER-01` (formal owner decision).
**Project:** Andirit. **Recorded:** 2026-11-05. **Canonical branch:** `main`.
**Owner decision:** **`ACCEPTED WITH EXPLICIT QUALIFICATIONS`**.
**Gate/scope:** **WP5 ACCEPTED — bounded `usage4` construction, extraction, and coverage/refusal emission.**

> **Status: RECORDED — OWNER-AUTHORIZED.**
> **Owner decision: `ACCEPTED WITH EXPLICIT QUALIFICATIONS`.**
>
> This accepts the WP5 production `usage4`/extraction/coverage implementation for its
> bounded scope only, after the WP5-Q1 blocking finding F1 was remediated (WP5-R1) and
> independently requalified (WP5-Q2: SUPPORTED WITH QUALIFICATIONS). It does **not**
> implement or accept WP6 NEW-04, persistence/reopen, cross-platform qualification,
> package publication/release, or complete S2.2, and does **not** close the Option A
> §5.2 test-6 cross-profile comparison. WP6 is eligible for a **separately
> commissioned** implementation.

This decision is additive documentation. It does not amend any prior accepted decision,
reviewer artifact, producer test, frozen oracle or production source, and it preserves
the WP5-Q1 record with its original **REMEDIATION REQUIRED** disposition as historical
evidence.

---

## 1. Owner decision, gate, and scope fields

| Field | Value |
| --- | --- |
| Owner decision | `ACCEPTED WITH EXPLICIT QUALIFICATIONS` |
| Gate/scope | WP5 bounded `usage4` construction, extraction, coverage/refusal emission, F09 scanning (closed) |
| Accepted capability | Internal `VerifiedCSharpUniverse.MintUsage` + `VerifiedCSharpOccurrenceExtractor.Extract` emitting trusted `usage4` only through the accepted WP4 authority boundary, with bounded coverage/refusal accounting and F09 disabled-text scanning |
| Excluded | WP6 NEW-04; persistence/reopen; cross-platform reviewer qualification; publication; complete S2.2; Option A §5.2 test 6 |
| Next stage | WP6 — requires a separate commission (entry contract in §8) |

## 2. Verified authority chain

| Role | Commit | Tree |
| --- | --- | --- |
| R2 foundation | `e0d911055f4cc9bdbf9aa4cc338f5cc007737a22` | `ba9338d74bf59c584bef340dbda49ed46eb1c5d9` |
| G-CONFIRM owner | `812584dc56e11983c3ea7de8977fd1cd74ff0297` | `7167557024e1602c81b5e6b169455c0ad10a5864` |
| WP3 owner | `4fdc23cfd3d760639dd653ad92bb204376a08c05` | `55b5e990cfa16606d7ba09752d18d360bf01db08` |
| G-MINT owner | `d3da2e009154402a92275874ac4bb94426b97df3` | `029299bf277bc99212ea2ded311c0ec54f70fa09` |
| WP4 owner | `97c88e58851820468cd21ee66d7a8ba460baf23a` | `27cda08808b4b56ccd6524b82b8315666636bd8d` |
| WP5 implementation | `a5f04fda495a5f54d95fd3f6a313620334a61155` | `c58781497560cae4bd8fbd420d83b0b41350fb58` |
| WP5 implementation evidence | `4890f30b9ca0b445835840c3a5b33e072f631631` | `2645e79875298da334d79e51e51f539aee4a3fae` |
| WP5-Q1 independent review (REMEDIATION REQUIRED) | `958d98ac0fc782e659734f8e56889784df9fdbfb` | `9496cf2633ff77fb17af0d5462c0d4fb78cd3431` |
| CI formatting restoration | `64ee765e6f87961063d2631576d8e29865e093de` | `4f295c685b730c7682e8136abfc1f9e1f81cae67` |
| WP5-R1 remediation | `2127a9a22c5a54b32b028aea5a947205f6acca06` | `85f28f3ccfcebf2b0d6ae12936777eb4c8e98688` |
| WP5-R1 evidence | `443c192733a53d2f1da415743debb84e8c81acb5` | `4c80956d4533667e8f91a10a0950e4315b6ca04e` |
| WP5-Q2 independent requalification | `f10b47296dde116cff90dfe1b42e79fb202fbd04` | `c7c5680eafd2ecdabf539caec6f5480001a3e1de` |

Verified at recording: consecutive parent chain; `f10b472` parent = `443c192` and tree =
`c7c5680…` as commissioned; working tree clean; one worktree; no stashes; `origin/main`
= `64ee765`; `main` **1 ahead, 0 behind**; nothing pushed by WP5-Q2. **No equivalent or
superseding WP5 owner decision exists** (`docs/decisions/` contains no `wp5-owner`
record); this is not a duplicate.

## 3. Owner verification of the qualification evidence

Re-executed by the owner at recording HEAD (logs in
`docs/reviews/CI1A-S2.2-WP5-OWNER-01-evidence/`), reproducing the Q2-reported results:

| Verification | Result |
| --- | --- |
| Q2 independent adversarial probes | **22 / 0 / 0** |
| Q1 reviewer probes (unchanged artifacts) | **16 / 0 / 0** |
| R1 producer regressions | **6 / 0 / 0** |
| Whole solution | **748 / 0 / 0** (325 + 223 + 157 + 43) |
| Frozen R2 oracle | **185/185**; output `97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9` == accepted evidence; expected `274A7AAE9EB6724879D619416E449C52759B44085D7B718DAD3434B15387CBBF` unchanged |
| `dotnet format Andirit.slnx --verify-no-changes` | exit 0 |
| Production unchanged since remediation | `git diff 2127a9a..HEAD -- src tests` empty |

WP4 entry contract **W5-1…W5-10** (`adr-ci1-01-ci1a-s2-2-wp4-owner-01.md` §9) is
satisfied in full: W5-1/W5-2 (minted authority + `compctx2` continuity; structural ids
refused), W5-3 (metadata-vs-instance), W5-4 (S-01 retained), W5-5 (fail-closed refusal
matrix), W5-6 (Option A §5.2 test 3 qualified; test 6 preserved at its allocated gate),
W5-7 (only authorized behavior), W5-8 (NEW-04 untouched, `Merge` unused), W5-9
(Windows-only limitation preserved), W5-10 (separate qualification + this decision).

## 4. F1 formal closure — REMEDIATED AND INDEPENDENTLY REQUALIFIED

- **Original finding (WP5-Q1 F1, BLOCKING, preserved unmodified at `958d98a`):** the extractor fell back to `CandidateSymbols.FirstOrDefault()` when Roslyn resolved no unique symbol, minting a trusted `usage4` for an arbitrary overload (`facts=1 ambiguous=0 unresolved=0`).
- **Remediation (WP5-R1, `2127a9a`, +11/−2 in `VerifiedCSharpOccurrenceExtractor.cs`):** only a uniquely resolved `info.Symbol` may establish the trusted target; `Symbol == null` with `CandidateSymbols.Length > 1` → `Ambiguous`; otherwise → `Unresolved`; no refused invocation emits a fact. `MintTarget`, `MintUsage` authority, `ResolveDefining`, frozen identity/formulas, and `compctx2` validation unchanged.
- **Independent requalification (WP5-Q2, `f10b472`):** the original counterexample now refuses (`facts=0 ambiguous=1 unresolved=0`); zero-/single-candidate and **inaccessible-member** bindings (Roslyn `reason=Inaccessible`, single candidate) refuse; candidate order cannot create an arbitrary trusted target; valid uniquely resolved methods keep producing exact accepted `usage4`/`asmctx3`.

**F1 status: REMEDIATED AND INDEPENDENTLY REQUALIFIED.** The correction closed the
demonstrated ambiguous path **and** the demonstrated inaccessible-member and
single-candidate authority paths (Q2 finding G1).

## 5. Remaining finding dispositions

| Finding | Disposition |
| --- | --- |
| **G1** — inaccessible-member bindings refuse (`Unresolved`) | **NON-BLOCKING.** Supports F1 closure; no remediation required. |
| **G2** — all multi-candidate non-bindings classified `Ambiguous`, including non-applicable pairs | **ACCEPTED QUALIFICATION.** The label does not distinguish every Roslyn `CandidateReason`; the binding safety property (no trusted fact without a unique binding) is what is accepted. No more precise classification contract is invented. |
| **G3** — constructed generics record the constructed documentation id; doc-id-less symbols fall back to `Name` | **ACCEPTED QUALIFICATION.** Bounded identity semantics; no universal canonicalization claimed. |
| **F2** — F09 disabled-text counts are heuristic (`ConditionalInactive=2` observed for 1 real disabled call; declaration/comment/string text also matched) | **ACCEPTED WITH EXPLICIT COVERAGE LIMITATION.** `ConditionalInactive` is a heuristic count, not a compiler-level count of disabled invocations; no trusted fact is ever minted from disabled text (verified). Consistent with the F09 obligation ("contract closure is not scanner completion"); no exact-count requirement exists in accepted authority. |
| **F3** — one unsupported occurrence increments both `Unsupported` and `UnsupportedCategories` | **ACCEPTED QUALIFICATION** within documented counter semantics; the two measures are not claimed disjoint. |
| **F4** — external refusal occurs at universe construction; extractor `External` counter unreachable on some paths | **NON-BLOCKING.** Construction-stage refusal authority preserved. |
| **F5** — `GetDocumentationCommentId() ?? Name` fallback bounded | **ACCEPTED QUALIFICATION.** No universal collision freedom claimed. |
| **F6** — structural identity constructors are not verified authority | **PRESERVED** per the accepted WP4/WP5 trust distinction. |
| **F7** — repository-relative path and `contentHash = sourceHash` assumptions | **PRESERVED** within the qualified input/canonicalization scope. |
| **F8 / S-01** — caller-supplied component digests not independently verified | **PRESERVED** as the standing S-01 qualification. |

## 6. Accepted production capabilities (bounded)

1. Internal `VerifiedCSharpUniverse.MintUsage` (WP4 `asmctx3` authority consumption).
2. Trusted `usage4` emission only after successful mandatory validation.
3. Internal `VerifiedCSharpOccurrenceExtractor.Extract` over supported `InvocationExpressionSyntax`.
4. Roslyn symbol-binding validation and the F1 refusal behavior.
5. Mandatory recorded-`compctx2` recording continuity and validation.
6. Accepted cross-project identity semantics (G1/W3 metadata-equivalent vs exact instance).
7. Exact frozen `usage4` and `asmctx3` identity construction (S22ID/Option A forms).
8. Bounded coverage and refusal accounting (`Produced`/`Partial`; refusal counters).
9. Bounded F09 conditional-inactive scanning (heuristic, never minting).
10. WP5-applicable Option A §5.2 test 3 runtime `usage4` behavior.
11. Determinism within the demonstrated comparison profile (separate processes).
12. WP3/WP4 compatibility (all targeted suites and prior reviewer probes pass).
13. Independently qualified Windows x64 execution.

## 7. Explicit non-acceptance

This decision does **not** claim: complete C# occurrence extraction; complete
constructor/property/delegate/method-group coverage; exact F09 disabled-code parsing;
universal `targetDocumentId` collision freedom; independently verified S-01 component
digests; exact target-instance identity from metadata equivalence alone; universal
refusal-status precedence; closure of Option A §5.2 test 6; WP6 NEW-04 resolution;
accepted coverage merging through `CodeOccurrenceCoverage.Merge`; native persistence or
reopen; complete S2.2 acceptance; independent Q2 qualification on Linux/macOS (reviewer
probes are Windows x64; hosted CI covers solution suites only); or package
publication/release authorization. All G-CONFIRM, Q2, G-MINT, and WP4 qualifications
remain in force.

## 8. WP6 entry contract

Authoritative scope: `scope-and-entry-criteria.md` §6.2 — *"NEW-04 coverage-authority
correction (or accepted disposition) with declaration/verification/merge semantics
fixed"*; internal gate: *"Independent qualification before any coverage-dependent
claim."* NEW-04 disposition is also a required owner decision (§6.9 item 4).

| ID | Requirement | Authoritative source | Current evidence | Remaining work | Mandatory acceptance condition |
| --- | --- | --- | --- | --- | --- |
| **WP6-1** | NEW-04 `CodeOccurrenceCoverage.Merge` correction per its actual contract | Obligation register NEW-04: `Merge` takes first scope/universe and max counts; constructor exempts `declared=0` from `verified<=declared`; P06 mixed-scope `Produced` claim | `Merge` unchanged since `fc515ff`; unused by WP3/WP4/WP5 (W5-8 maintained) | Implement the correction (or a bounded accepted disposition) in `CodeOccurrence.cs`/`Merge` semantics | Independent qualification before any coverage-dependent claim |
| **WP6-2** | Coverage accounting through merge | Same register row: test matching/different scope, matching/different universe, verified empty vs missing evidence/no analysis/invalid zero declarations, partial+complete, changed membership | WP5 counters are extraction-local; no merge semantics accepted | Define and test which WP5 refusal classifications survive merging; distinguish aggregate counters from occurrence categories | No invented merge algebra where authority is silent; document every semantic choice |
| **WP6-3** | Authority preservation | G-CONFIRM/G-MINT/WP4/WP5 authority; W5-1/W5-5 | Structural ids and unverified associations never mint | Merged coverage must not create trusted facts or authority absent from verified WP4/WP5 emission | Negative tests: merge cannot fabricate facts or `Produced` claims from refused/partial input |
| **WP6-4** | `Produced`/`Partial` and refusal semantics when combining results | `coverage-and-availability.md` dimension separation | WP5 preserves the dimensions extraction-locally | Preserve the dimensions under merge; mixed-scope results must not claim `Produced` (P06 defect class) | Refusal/partial evidence survives merge; fail-closed on ambiguity |
| **WP6-5** | F09 interaction | F2 acceptance here: heuristic counts, not exact occurrences | F09 counts never mint; no merge dependency in WP5 | Determine whether heuristic `ConditionalInactive` counts interact with NEW-04 merge correctness; do not silently reinterpret heuristic counts as exact occurrence counts | Any interpretation is explicit and qualified |
| **WP6-6** | Identity and evidence integrity | Frozen `CodeOccurrence.cs` formulas (S2-REM-01) | `asmctx3`/`usage4`/`compctx1`/`compctx2` relationships accepted and frozen | Corrections must not alter the frozen identity formulas; scope/universe identity handling in `Merge` must stay consistent | Frozen oracle 185/185 with `97F6C5E5…` unchanged; formula hashes intact |
| **WP6-7** | Option A §5.2 test 6 (cross-profile comparison) | §6.2 allocates tests 2/6/8 to **WP7** (Option A invalidation/refusal) | Test 6 open; explicitly excluded here (§7) | **Not a WP6 obligation.** WP6 must not absorb it; its authority/completion gate is WP7's Option A durability acceptance | Preserved outstanding; recorded as WP7 entry input |
| **WP6-8** | Independent qualification + owner decision | W5-10 pattern; §6.4/§6.9 | WP6 has no implementation yet | Separate WP6 commission: implementation → independent qualification → owner decision (including the §6.9 NEW-04 disposition) | No coverage-dependent claim before qualification |

Native persistence/reopen remains S2.3 (register S2.2 row); WP6 does not include it
except where the NEW-04 register row explicitly requires testing "if published" —
publication itself stays separately controlled.

## 9. Outstanding cross-profile comparison obligation

Option A §5.2 test 6 (equivalent artifacts across profiles) remains **open** and is
allocated to WP7's Option A durability/invalidation acceptance (§6.2), per W5-6 and the
mapping contract. Its completion gate is the WP7 independent qualification; WP5's
acceptance neither assumes nor performs it.

## 10. Platform limitations

WP5 reviewer qualification is Windows x64 (SDK `10.0.401`, runtime `10.0.12`). Hosted CI
executes the solution suites (including the six R1 producer regressions) on
Windows/Ubuntu/macOS, but reviewer probe projects run only locally on Windows. No
Linux/macOS reviewer qualification is claimed; cross-platform evidence obligations
remain per W5-9 and the release stage.

## 11. Authorization boundaries

- Publication/release remains separately controlled; this decision does not push (main is 1 ahead of `origin/main` at recording).
- WP6, WP7, WP8, S2.3+, and any `CodeOccurrenceCoverage.Merge` change each require their own commissions.
- The frozen R2 oracle, `CodeOccurrence.cs` authority (SHA-256 `78FBD358BE875735E3E29A68A7FE8FF22DCE511BA8FC72D1FE9795A6C785B27D`), and all accepted decisions remain untouched by this record.

## 12. Final owner decision

**`ACCEPTED WITH EXPLICIT QUALIFICATIONS`** — WP5 is formally closed for its bounded
`usage4` construction, extraction, and coverage/refusal emission scope, with F1
remediated and independently requalified, and the qualifications of §5 carried forward
as explicit limitations. **WP6 becomes eligible for a separately commissioned
implementation** under the entry contract of §8.

---

Recorded at HEAD `f10b47296dde116cff90dfe1b42e79fb202fbd04` (tree
`c7c5680eafd2ecdabf539caec6f5480001a3e1de`). This record is the only change in its
commit; the decision commit/tree identities are recorded in the handoff.
