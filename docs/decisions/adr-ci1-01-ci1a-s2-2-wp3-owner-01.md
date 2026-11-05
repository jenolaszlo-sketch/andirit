# ADR-CI1-01-CI1A-S2.2-WP3-OWNER-01 — Qualified owner acceptance of the WP3 mapper/resolver

**Commission:** `ANDIRIT-CI1A-S2.2-WP3-OWNER-01` (owner acceptance decision).
**Project:** Andirit. **Recorded:** 2026-11-05. **Canonical branch:** `main`.
**Owner decision:** **`ACCEPTED WITH EXPLICIT QUALIFICATIONS`.**

> **Status: RECORDED — OWNER-AUTHORIZED.**
> **Owner decision: `ACCEPTED WITH EXPLICIT QUALIFICATIONS`.**
>
> This accepts the S2.2 **WP3 mapper/resolver implementation** for its bounded
> scope only. It does **not** pass `G-MINT`, does **not** authorize production
> minting, and authorizes **no** WP4/WP5, S2.3+, WP6 work, publication, package
> release or complete S2.2 acceptance. The next gate is a separately commissioned
> `G-MINT` assessment.

This is the recorded owner decision for `ANDIRIT-CI1A-S2.2-WP3-OWNER-01`. It is
additive documentation: it does not amend any accepted decision, reviewer test,
frozen oracle, production source or gate record.

---

## 1. Decision authority and baseline

Re-verified at recording:

| Role | Commit | Tree |
| --- | --- | --- |
| G-CONFIRM owner acceptance (`G-CONFIRM PASSED`) | `812584dc56e11983c3ea7de8977fd1cd74ff0297` | `7167557024e1602c81b5e6b169455c0ad10a5864` |
| WP3 implementation | `ff58a9290a7583d4493d177e5de5aa6315904a85` | `85cdcedeb261c656893cdd7c2e3f21b741a25dc6` |
| WP3 implementation report | `e67377555bde8b2958292a2baf45e05908af917f` | `0a6a91e95541e28e985269879c2c2e824123fdb7` |
| WP3 independent qualification (Q1) | `2cdcd10e4b225debe0d081280d8a18a6304d0475` | `a3d6dcc92b08c8080f2d2402a9d3a714b01b7409` |
| Recorded HEAD before this decision | `2cdcd10e4b225debe0d081280d8a18a6304d0475` | `a3d6dcc92b08c8080f2d2402a9d3a714b01b7409` |

**Verified.**

- `812584d → ff58a92 → e673775 → 2cdcd10` is a consecutive parent chain
  (`git merge-base --is-ancestor`, exit 0 each); every tree above reproduced exactly.
- The G-CONFIRM owner decision indeed records **`ACCEPTED WITH EXPLICIT
  QUALIFICATIONS`** and **`G-CONFIRM PASSED`** for the 14 F16/NEW-02 rows.
- `docs/reviews/ANDIRIT-CI1A-S2.2-WP3-IMPL-01.md` (producer report) and
  `docs/reviews/ANDIRIT-CI1A-S2.2-WP3-Q1.md` (independent qualification) are present
  and consistent with their commits.
- `git diff --stat e673775..2cdcd10 -- src tests` is **empty**: no production or
  producer-test change occurred during qualification. The review commit adds only
  review documentation, the reviewer probe project and review evidence (14 files).
- Working tree clean; one worktree (`D:/projects/Andirit`); no stashes;
  `origin/main` = `e0d911055f4cc9bdbf9aa4cc338f5cc007737a22`; no concurrent writer was
  observed moving `HEAD`, the branch or `origin/main` during this decision.
- **No equivalent WP3 owner decision exists.** `docs/decisions/` contains only the
  S2.2 entry, foundation and G-CONFIRM owner decisions; no WP3 owner decision record
  was found, so this is not a duplicate.

**Authority for the accepted capability** (read directly, not via the commission
summary): Option A decision
[`adr-ci1-01-ci1a-s2-2-target-identity-decision.md`](adr-ci1-01-ci1a-s2-2-target-identity-decision.md)
§5.1/§5.2; mapping contract
[`mapping-contract.md`](../planning/adr-ci1-01-ci1a-s2-2/mapping-contract.md) §1–§3,
§5–§7; S2.1 provenance contract
[`provenance-contract.md`](../planning/adr-ci1-01-ci1a-s2-1/provenance-contract.md)
§1–§5; stage 2 boundary
[`stage2-boundary.md`](../planning/adr-ci1-01-ci1a-impl-01/stage2-boundary.md)
§3.4–3.9/§5; G-CONFIRM owner decision
[`adr-ci1-01-ci1a-s2-2-g-confirm-owner-01.md`](adr-ci1-01-ci1a-s2-2-g-confirm-owner-01.md)
§5–§8; frozen R2 authority
`docs/evidence/adr-ci1-01-ci1a-s2-1-raw/authority/CompilationProvenance.cs`.

---

## 2. Implementation scope

Commit `ff58a92`, four files, +649, purely additive:

| File | Change |
| --- | --- |
| `src/Andirit.CSharp/VerifiedCSharpCompilation.cs` | `MapR2FaithfulCompctx1`, `MapReferenceForR2`, `R2LegacyProjectReference`; `using System.Text` |
| `src/Andirit.CSharp/VerifiedCSharpAnalysis.cs` | `VerifiedCSharpUniverse.ResolveDefining`, `BindsDefining`; `using Microsoft.CodeAnalysis` |
| `src/Andirit.CSharp/VerifiedDefiningResolution.cs` | new: `VerifiedDefiningStatus`, `VerifiedDefiningResolution` |
| `tests/Andirit.CSharp.Tests/S2_2Wp3MapperResolverTests.cs` | new: 16 authority-grounded mapper/resolver tests |

The modified production files' diffs add only new internal members and two `using`
directives; no existing method body, public API, frozen evidence, oracle or accepted
decision was changed.

---

## 3. Independent qualification evidence

Independent qualification `ANDIRIT-CI1A-S2.2-WP3-Q1` returned **`SUPPORTED WITH
QUALIFICATIONS`**. The findings were re-verified against the review report and its
committed evidence:

| Evidence | Recorded result | Confirmed |
| --- | --- | --- |
| Independent mapper probes | 9 / 0 / 0 | ✅ report §8 + `reviewer-wp3-probes.trx` |
| Independent resolver probes | 10 / 0 / 0 | ✅ report §8 + `reviewer-wp3-probes.trx` |
| WP3 producer targeted tests | 16 / 0 / 0 | ✅ `producer-wp3-targeted.log` |
| `Andirit.CSharp.Tests` | 183 / 0 / 0 | ✅ `csharp-suite.log` |
| Whole solution | 708 / 0 / 0 | ✅ `solution-suite.log` |
| Frozen R2 oracle | 185 / 185, exit 0 | ✅ `oracle-r2.log` |
| Frozen oracle output hash | `97F6C5E5…` (unchanged) | ✅ `oracle-r2.json`; LF form byte-identical to `oracle-r2-run-1.json` |
| Production changes during qualification | none | ✅ `git diff e673775..2cdcd10 -- src tests` empty |

The review identified **no demonstrated blocking implementation defect**. Its
positive evidence reproduces the accepted frozen R2 `compctx1` (digest-equal to the
frozen authority) and the accepted pre-mint refusal vocabulary. The findings F1–F5
below are represented correctly in the review report.

---

## 4. F1–F5 dispositions

### F1 — Cross-project metadata identity binding — **ACCEPTED AS AN EXPLICIT QUALIFICATION**

The resolver can bind a foreign, identity-equal target instance to an existing
universe member (`Resolver_RefusesForeignIdentityEqualTargetInstance` → `Complete`,
bound to the referenced universe member). The accepted frozen R2 authority
(`AdrCi1Exp.Shared.CompilationUniverse.Verify`/`Binds`) reaches the same verdict for
the same input; the production direct branch (`ReferenceEquals`) is **stricter** than
the frozen authority's identity-equality direct branch, and no WP3 weakening was
introduced.

**Owner treatment.** Accepted as an inherited, explicitly qualified behavior within
the WP3 contract. Metadata equivalence is **not** interpreted as proof of exact
target-instance authority. Any stricter enforcement must be justified against the
accepted contract and must **not** be introduced silently. Carried forward as
**GM-1**.

### F2 — Refusal-status precedence — **QUALIFICATION PRESERVED**

No accepted document specifies precedence among
`Incomplete`/`Ambiguous`/`MissingDefiningCompilation`/`UnsupportedExternalEvidence`/`StaleContext`.
The implementation's observed order is deterministic and fail-closed but is **not**
an accepted semantic contract.

**Owner treatment.** Preserve the qualification. Implementation order is not elevated
into authority. Carried forward as **GM-2**.

### F3 — Null recorded context — **ACCEPTED ONLY WITHIN DOCUMENTED SCOPE**

`ResolveDefining(..., recordedCompctx2: null, ...)` can return `Complete`, so
`Complete` alone does not prove that a recorded context was validated.

**Owner treatment.** Accepted only within WP3's documented pre-mint, non-minting
scope. The requirement that a minted association carry a supplied, non-null recorded
`compctx2` that passes association validation is **already established by accepted
authority**, not invented here:

- Option A §5.1(2): "`compctx2` of the defining compilation … must be current and
  equal to the context under which the occurrence was produced. … A bare `asmctx3`
  is not provenance."
- Option A §5.1(5): "S2.2 must record the defining context's `compctx2` (or an
  equivalent unambiguous provenance association) … so the validity gate is
  enforceable."
- Mapping contract §3.3: "The defining compilation's **current verified `compctx2`**
  equals the context under which the occurrence is produced."

Carried forward as **GM-3**; no new semantic contract is created.

### F4 — `UnsupportedExternalEvidence` — **NON-BLOCKING**

The status is unreachable through normal `ResolveDefining` execution because
out-of-scope defining units are refused during `VerifiedCSharpUniverse.Create`
(`ScopeSourceMembershipMismatch`).

**Owner treatment.** Recorded as non-blocking. The construction-stage refusal boundary
is preserved; no artificial requirement is imposed to make the status reachable
through the resolver. Carried forward as **GM-5**.

### F5 — Producer evidence — **VERIFIED, NON-BLOCKING**

Reported totals and hashes reproduced independently (§3), subject to the documented
platform (Windows x64) and scope limitations (no cross-platform inference; bounded
S2.2 surface). Carried forward only as the platform limitation in §8.

---

## 5. Accepted capabilities

If and only if the evidence above remains valid, the owner accepts, for the WP3
bounded scope:

1. The R2-faithful `compctx1` mapper (`MapR2FaithfulCompctx1`), including the single
   `compilation-context|` → `compilation|…` substitution of dependency `compctx2`
   descriptors with frozen R2 legacy project-reference items, preservation of actual
   verified PE identities, correct multi-dependency ordering and no dedup.
2. `compctx2` consumption within the verified compilation model, with `compctx2`
   isolated from `compctx1`.
3. Pre-mint defining-compilation resolution (`ResolveDefining`) and the association
   invariant (complete universe, exact live binding, current-context equality).
4. The in-memory Option A §5.2 association-validation capabilities actually
   demonstrated (stale/fabricated/substituted/incomplete/ambiguous/missing refusal
   and deterministic mapping).
5. The explicit refusal classifications within the qualified scope
   (`Incomplete`, `Ambiguous`, `MissingDefiningCompilation`,
   `UnsupportedExternalEvidence`, `StaleContext`, `Complete`).
6. Existing S2.1 compatibility preservation (dependency-free `LegacyDescriptor`
   behavior; no change to frozen R2 vectors).
7. Authority-grounded test coverage and independent qualification evidence, with no
   producer-generated expected values.

**Acceptance is limited to these capabilities.**

---

## 6. Explicit limitations and non-acceptance

This decision must **not** be read as claiming, and explicitly does **not** accept:

- `G-MINT` has passed; production minting is authorized.
- `asmctx3` or `usage4` is implemented.
- WP4/WP5 functionality exists.
- Resolver success alone proves minting authority.
- Cross-project metadata identity guarantees exact target-instance identity.
- Refusal-status precedence is fully specified.
- Native reopen is supported.
- Cross-platform qualification has occurred.
- WP6 NEW-04 is resolved.
- Production extraction or persistence is implemented.
- Package publication or releases are authorized.
- Complete S2.2 acceptance.

The S-01 caller-supplied component-digest limitation (`identifierDigest`/
`declarationDigest`/`shapeDigest` are caller-supplied; the corpus supplies
fixture-declared canonical forms) and all other applicable Q2 foundation
qualifications remain in force and are **not** closed by this decision.

---

## 7. G-MINT carried-forward obligations

These requirements are carried into the separately commissioned `G-MINT` assessment.
A requirement is **not** satisfied merely because a related WP3 test passes; each
row separates existing accepted authority from work that still needs specification,
implementation or qualification.

| ID | Requirement | Authority | Existing evidence | Remaining gap | Required work | G-MINT acceptance condition |
| --- | --- | --- | --- | --- | --- | --- |
| **GM-1** | Cross-project identity and association-authority validation (F1) | Mapping contract §3.1; S2.1 provenance contract §5; Option A §5.1(1)–(2); frozen R2 `CompilationUniverse.Verify` | `Resolver_RefusesForeignIdentityEqualTargetInstance`, `Resolver_RefusesSubstitutedLiveInstanceWithSameDigest`, `Resolver_BindsCrossProjectViaSharedAssemblySymbol`; frozen-R2 corroboration | Cross-project branch binds the target's containing assembly by metadata identity; a foreign identity-equal target can bind a universe member (accepted-authority-consistent) | Qualification (independent demonstration for the minting path); **specification + owner authorization** only if stricter-than-R2 enforcement is desired | Reviewer demonstrates every association used for minting is bound to exactly one verified live defining instance and metadata equivalence is never the sole authority for a minted target's instance; any stricter enforcement is contract-justified and owner-authorized, never silent |
| **GM-2** | Refusal behavior and any necessary status-precedence decision (F2) | Stage 2 boundary §5; mapping contract §3; Option A §5.1 | `Resolver_RefusesIncompleteUniverse_BeforeBinding`, `Resolver_RefusesAmbiguousTwoLiveBindings` | Precedence is unspecified by accepted authority | **Specification/owner decision**, or a demonstration that all refusal outcomes prevent minting irrespective of order | Either an owner-accepted precedence, or a demonstrated property that no refusal-status pair can yield a mint under any evaluation order |
| **GM-3** | Non-null recorded `compctx2` validation before minting (F3) | Option A §5.1(2) and §5.1(5); mapping contract §3.3 | `Resolver_RefusesStaleAndFabricatedRecordedContext` (refusals); `ResolveDefining` currently accepts `null` | The pre-mint API can return `Complete` with no recorded context | Implementation (WP4 extraction must always supply the recording) + qualification (no new contract) | Every minted association carries a supplied, non-null recorded `compctx2` equal to the defining entry's current verified descriptor digest; `Complete` alone is insufficient evidence |
| **GM-4** | Option A §5.2 association negatives tied to accepted expected outcomes | Option A §5.2 tests 1–10; mapping contract §7 | 19 independent reviewer probes; 16 WP3 producer tests | Full `asmctx3`/`usage4` forms (tests 1–3, 5) and cross-profile equivalent-reference equality (test 6) are not yet implemented | Qualification against the implemented minting path (WP4/WP5) | All Option A §5.2 tests 1–10 demonstrated against the implemented path with expected values grounded only in accepted authority |
| **GM-5** | Construction-stage external-evidence refusal remains effective | Mapping contract §1.4, N2; `VerifiedCSharpUniverse.Create` | `Resolver_RefusesExternalUnitAtConstruction` | None for WP3 | None (preserve); verify at G-MINT | Out-of-scope/unverified defining units continue to be refused at universe construction and cannot reach the resolver or minting |
| **GM-6** | NEW-04 remains deferred to WP6 under the accepted boundary | G-CONFIRM owner §8; obligation register §4; S2.1 boundary §4; entry acceptance §3.8 | `CodeOccurrenceCoverage.Merge` unchanged (last change `fc515ff`) and not consulted by WP3 | NEW-04 correction not implemented | Implementation + independent qualification at WP6 before any coverage reliance | NEW-04 independently corrected/falsified at WP6; no coverage reliance (or minting-adjacent coverage claim) before then |
| **GM-7** | No production minting from unverified extraction, fabricated context or producer-generated oracle | Option A §5.2 test 10; mapping contract §6; `g-mint-package.md` §4; G-CONFIRM-Q1 `REPORT.md` §2 | `Mapper_PreservesActualPeIdentity_AndIsolatesCompctx2`, `Resolver_DoesNotMintAsmctxOrPersistAssociation`, resolver negatives | Minting is not implemented | Qualification | Every minted target/occurrence derives from the exact live verified defining compilation and its current `compctx2`; no fabricated/relayed descriptor, caller boolean or producer-generated expected value establishes authority |

Items **GM-1, GM-2, GM-3, GM-4 and GM-7** are qualification or specification items for
`G-MINT` (with GM-2 possibly needing an owner decision). **GM-5** is preservation of an
existing accepted boundary. **GM-6** is an existing deferral to **WP6**. None is
claimed satisfied by this decision.

---

## 8. Remaining platform limitations

- No minting: `asmctx3`/`usage4` construction, extraction emission and WP4 runtime
  integration are not implemented.
- Caller-supplied component digests (`identifierDigest`/`declarationDigest`/
  `shapeDigest`) remain the accepted S2.2/S-01 obligation.
- Native persistence and reopen enforcement are the first separately authorized
  **S2.3** gate; not required for `G-MINT`.
- No production extraction path is wired to the resolver.
- Platform: all qualification executed on **Windows x64** (`net10.0`); no
  cross-platform run was performed and none is inferred.
- The bounded Q-F2/Q-F3/A-01/A-02 surfaces and the Option A pre-mint association
  specification (without durable enforcement) remain in force.

---

## 9. Authorization boundaries

| Item | Status |
| --- | --- |
| WP3 bounded mapper/resolver | **ACCEPTED WITH EXPLICIT QUALIFICATIONS** (this decision) |
| `G-MINT` | **OPEN** — requires a separate commission; **not** granted here |
| Production target/occurrence minting | **NOT AUTHORIZED** |
| WP4/WP5 | **NOT AUTHORIZED** |
| WP6 (NEW-04) | **NOT AUTHORIZED** (separate commission; remains deferred) |
| WP7–WP9 | **NOT AUTHORIZED** |
| S2.3+ (native persistence/reopen) | **NOT AUTHORIZED** |
| Publication / package releases / push | **NOT AUTHORIZED** |

**Preserved ordering:** `G-CONFIRM (PASSED) → WP3 mapper (ACCEPTED) → G-MINT → WP4 → WP5`.

---

## 10. Final owner disposition

**`ACCEPTED WITH EXPLICIT QUALIFICATIONS`.**

The WP3 mapper/resolver is accepted for its bounded implementation scope on the basis
of independent qualification `SUPPORTED WITH QUALIFICATIONS`, with F1–F5 addressed
above and the carried-forward `G-MINT` obligations recorded in §7. This decision:
does not pass or advance `G-MINT`; does not authorize minting, WP4/WP5, WP6, S2.3+,
or publication; and does not close complete S2.2.

**Final disposition:** **`ADR-CI1-01-CI1A-S2.2-WP3-OWNER-01 ACCEPTED WITH EXPLICIT
QUALIFICATIONS`** — WP3 is closed for its bounded implementation scope and is
eligible for a separately commissioned `G-MINT` assessment. `G-MINT` remains
**OPEN**.
