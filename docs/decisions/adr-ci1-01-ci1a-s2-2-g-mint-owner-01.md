# ADR-CI1-01-CI1A-S2.2-G-MINT-OWNER-01 — Formal G-MINT pre-mint gate acceptance

**Commission:** `ANDIRIT-CI1A-S2.2-G-MINT-OWNER-01` (owner gate decision).
**Project:** Andirit. **Recorded:** 2026-11-05. **Canonical branch:** `main`.
**Recorded gate:** `G-MINT`.
**Owner decision:** **`ACCEPTED WITH EXPLICIT QUALIFICATIONS`**.
**Gate:** **`G-MINT PASSED`**.
**Scope:** Pre-mint association and authority qualification only.

> **Status: RECORDED — OWNER-AUTHORIZED.**
> **Decision: `ACCEPTED WITH EXPLICIT QUALIFICATIONS`. Gate: `G-MINT PASSED`.**
>
> `G-MINT` is closed for its bounded pre-mint qualification scope. This authorizes a
> separately commissioned **WP4** implementation; it does **not** authorize WP5/WP6,
> later work packages, native persistence/reopen, publication or releases, and does
> **not** claim that any production minting implementation exists or has passed
> qualification.

This is the recorded owner decision for `ANDIRIT-CI1A-S2.2-G-MINT-OWNER-01`. It is
additive documentation: it does not amend any accepted decision or qualification
evidence, and it does not modify production source, tests, reviewer probes or the
frozen oracle.

---

## 1. Verified authority chain

Re-verified at recording (`git merge-base --is-ancestor`, exact tree comparison):

| Role | Commit | Tree |
| --- | --- | --- |
| R2 foundation | `e0d911055f4cc9bdbf9aa4cc338f5cc007737a22` | `ba9338d74bf59c584bef340dbda49ed46eb1c5d9` |
| G-CONFIRM independent authority | `391abbee8c26d957a5776bd893403dc2916ecfef` | `3f06ecf81c9dff33eb6d5532705092fe292d2457` |
| G-CONFIRM owner acceptance | `812584dc56e11983c3ea7de8977fd1cd74ff0297` | `7167557024e1602c81b5e6b169455c0ad10a5864` |
| WP3 implementation | `ff58a9290a7583d4493d177e5de5aa6315904a85` | `85cdcedeb261c656893cdd7c2e3f21b741a25dc6` |
| WP3 implementation report | `e67377555bde8b2958292a2baf45e05908af917f` | `0a6a91e95541e28e985269879c2c2e824123fdb7` |
| WP3 independent qualification | `2cdcd10e4b225debe0d081280d8a18a6304d0475` | `a3d6dcc92b08c8080f2d2402a9d3a714b01b7409` |
| WP3 owner acceptance | `4fdc23cfd3d760639dd653ad92bb204376a08c05` | `55b5e990cfa16606d7ba09752d18d360bf01db08` |
| G-MINT independent qualification | `1c382d6aaa1c2ceb04b70e65e048357621a73a78` | `c2f7c9ef782e90e70df9c63dfcd62e8cb888f7b9` |

Verified:

- `e0d9110 → 391abbe → 812584d → ff58a92 → e673775 → 2cdcd10 → 4fdc23c → 1c382d6`
  is a consecutive parent chain; every tree reproduced exactly. `1c382d6^ = 4fdc23c`.
- Working tree **clean**; one worktree (`D:/projects/Andirit`); no stashes;
  `origin/main` = `e0d911055f4cc9bdbf9aa4cc338f5cc007737a22`; `main` is **10 ahead, 0
  behind**; **nothing pushed**.
- `git diff --stat 4fdc23c..1c382d6 -- src tests` is **empty**: no production or test
  change occurred during the G-MINT qualification.
- **No equivalent G-MINT owner decision exists** (`docs/decisions/` contains no
  `*g-mint*` record; no superseding decision was found). This is not a duplicate.
- Authority documents re-read directly: G-CONFIRM owner decision
  [`adr-ci1-01-ci1a-s2-2-g-confirm-owner-01.md`](adr-ci1-01-ci1a-s2-2-g-confirm-owner-01.md)
  §5–§8; WP3 owner decision
  [`adr-ci1-01-ci1a-s2-2-wp3-owner-01.md`](adr-ci1-01-ci1a-s2-2-wp3-owner-01.md) §7;
  G-MINT package
  [`g-mint-package.md`](../planning/adr-ci1-01-ci1a-s2-2/g-mint-package.md) §1–§7;
  Option A decision §5.1/§5.2; mapping contract §2–§3, §7; S2.1 provenance contract
  §1–§5. No missing authority was reconstructed from assumption.

---

## 2. Independent qualification result

`ANDIRIT-CI1A-S2.2-G-MINT-Q1` returned **`SUPPORTED WITH QUALIFICATIONS FOR OWNER
DECISION`**. Findings re-verified against the committed report and evidence:

| Verification | Recorded result | Confirmed |
| --- | --- | --- |
| G-MINT independent probes | 12 / 0 / 0 | ✅ `reviewer-gmint-probes.log`/`.trx` |
| WP3-Q1 independent probes (re-run) | 19 / 0 / 0 | ✅ `wp3-q1-probes-rerun.log` |
| WP3 targeted tests | 16 / 0 / 0 | ✅ `producer-wp3-targeted.log` |
| `Andirit.CSharp` regression | 183 / 0 / 0 | ✅ `csharp-suite.log` |
| Whole solution | 708 / 0 / 0 | ✅ `solution-suite.log` |
| Frozen R2 oracle | 185 / 185, exit 0 | ✅ report §8 |
| R2 output hash | `97F6C5E5…`, unchanged | ✅ `oracle-r2.json` |
| Separate-process mapper determinism | confirmed | ✅ `gmint-observations-summary.txt` |
| Production modification during qualification | none | ✅ empty `src`/`tests` diff |

The qualification found **no BLOCKING violations** and **no DECISION REQUIRED**
findings; all GM-1…GM-7 were assessed; qualifications **G1–G4** were identified. The
findings support bounded owner acceptance. A passing suite is not treated as
authority; each row was grounded in contract text and independently derived evidence.

---

## 3. GM-1 … GM-7 closure matrix

| ID | Accepted authority | Independent evidence | Pre-mint result | Explicit qualification | Pre-mint requirement | Remaining WP4/WP5 obligation | Later qualification evidence |
| --- | --- | --- | --- | --- | --- | --- | --- |
| **GM-1** | Mapping §3.1; S2.1 §5; Option A §5.1(1)–(2) | `Gm1_*` probes; frozen R2 corroboration | Defining instance bound by exact live instance; unequal identity refuses; two candidates ⇒ Ambiguous; substituted caller ⇒ Missing | Foreign identity-equal target binds via metadata identity (F1), matching frozen R2; direct branch stricter | **SATISFIED** (within G1) | Preserve live-instance vs metadata-identity distinction; do not upgrade equivalence into stronger authority | WP4 negative tests for foreign identity-equal targets and substituted instances |
| **GM-2** | Stage2 boundary §5; mapping §3; Option A §5.1 | `Gm2_*` (10 overlaps) | All overlaps refuse; `Complete` iff complete ∧ one binding ∧ (no/equal recorded) | Labels order-dependent; mint-eligibility order-invariant | **SATISFIED** | Enforce "every non-`Complete` prevents minting" at the minting path | WP4/WP5 verification that overlapping failures cannot produce output |
| **GM-3** | Option A §5.1(2)/(5); mapping §3.3 | `Gm3_*` (7 cases) | Supplied contexts validated; `null` ⇒ `Complete`; no validated-recording flag | `Complete` alone is **not** minting authority | **SATISFIED** as a capability; nothing was waived | Supply a non-null recorded `compctx2`; validate; forbid omission bypass | WP4 negative test proving `Complete` without recording cannot mint |
| **GM-4** | Option A §5.2; mapping §7 | `Gm4_*`; WP3-Q1 mapper probes | Pre-mint falsification set demonstrated (stale/fabricated/substituted/incomplete/ambiguous/missing/external/isolation + valid control) | Full `asmctx3`/`usage4` forms (tests 1, 3, 5) and cross-profile test 6 are WP4/WP5 | **SATISFIED** for pre-mint scope | Implement and reproduce bulk minting-form tests | WP4/WP5 runtime-form tests with independent expected values |
| **GM-5** | Mapping §1.4, N2 | `Gm5_*` | Out-of-scope/repo-mismatch/dependency-outside-universe refused at construction; recorded refusal ⇒ incomplete | `UnsupportedExternalEvidence` unreachable via resolver (non-blocking) | **SATISFIED** | Preserve construction-stage refusal | Re-verify at G-MINT successors and WP4 |
| **GM-6** | G-CONFIRM owner §8; register §4 | `git grep CodeOccurrenceCoverage` (none); `Merge` last change `fc515ff` | NEW-04 unchanged and unused by WP3/G-MINT | Authorized deferral to WP6 | **SATISFIED** (deferral preserved) | None at WP4; correct/falsify NEW-04 at WP6 | WP6 independent falsification before any coverage reliance |
| **GM-7** | Option A §5.2(10); mapping §6; `g-mint-package.md` §4 | `Gm7_*`; mapper == frozen authority; separate-process digest | R2-faithful; PE identities intact; `compctx2` isolated; no producer-generated oracle | Fixture-profile anchors are accepted Q1 evidence, not re-derived here | **SATISFIED** | Preserve; enforce trusted provenance at minting | WP4/WP5 trusted-evidence review |

Nothing above labels a future runtime obligation as implemented.

---

## 4. G1–G4 owner treatment

### G1 — Cross-project metadata identity — **QUALIFICATION PRESERVED**

The accepted R2 authority permits cross-project target binding through metadata
identity; the **defining** compilation is bound by the required live-instance
relationship. The reviewer reproduced the inherited foreign identity-equal target
behavior and confirmed it matches frozen R2.

Owner treatment: preserve as an explicit qualification. Metadata identity equivalence
is **not** proof of exact target-instance identity, and the frozen R2 contract is not
retroactively changed. **WP4 must** preserve the distinction between target metadata
identity and defining-compilation live-instance identity; must not silently upgrade an
identity-equivalent association into a stronger authority claim; must use only the
identity guarantees actually established by the accepted contract; and must supply
independent negative tests for foreign identity-equal targets and substituted
compilation instances. Stronger identity requirements require a separate owner
decision and implementation scope.

### G2 — Refusal status and precedence — **FAIL-CLOSED ACCEPTED; NO PRECEDENCE CONTRACT**

The reviewer demonstrated order-independent fail-closed behavior. Refusal-status label
precedence is not specified by accepted authority and is **not** ratified by this
decision.

Owner treatment: every status other than `Complete` must prevent minting; no caller may
infer safety from a particular refusal label when conditions overlap; any consumer
requiring a stable precedence rule must obtain an explicit contract decision. A new
precedence contract is **not** necessary to accept `G-MINT`, because the gate
requirements are satisfied by order-independent refusal.

### G3 — Mandatory recorded `compctx2` — **BINDING WP4 CONDITION**

The reviewer confirmed that a null recorded context can produce `Complete`, and that
no separate validated-recording flag is exposed. Option A §5.1(2)/(5) and mapping
contract §3.3 already establish the recorded-context validation requirement.

Owner treatment: accepted only within WP3's bounded non-minting scope. It is explicitly
recorded that **`Complete` alone is not sufficient minting authority.** Carried forward
as a mandatory WP4 condition:

1. The minting path must supply a non-null recorded `compctx2`.
2. The supplied context must be validated against the required verified defining-compilation context.
3. Null, missing, fabricated, substituted, or stale recordings must not authorize minting.
4. A caller cannot bypass the validation by omitting the recorded context.
5. Successful resolver status must not substitute for independently required association checks.

A negative test proving that a `Complete` result obtained without a recorded context
cannot mint authority is required. This is a binding implementation obligation, not a
recommendation.

**Gate-boundary determination.** The accepted boundary does not require this
enforcement *before* WP4. Mapping contract §4 places the explicit `compctx2`
association record and its extraction-time enforcement at S2.2, with **native
persistence/reopen** at S2.3; G-CONFIRM owner §8 assigns **runtime extraction
integration** to WP4 ("WP4 runtime extraction integration then consumes the cleared
mapper"). The pre-WP5 gate requirement (`g-mint-package.md` §4) is the in-memory
candidate/current/universe mismatch refusal and stale-association enforcement, which
is satisfied. Therefore there is **no conflicting authority**: the "always supply a
non-null recording" obligation belongs to WP4 extraction. Had the gate required it
before WP4, acceptance would have been deferred rather than weakened.

### G4 — Option A §5.2 minting-path tests — **PRE-MINT SCOPE ACCEPTED; RUNTIME FORMS DEFERRED**

The reviewer independently demonstrated the required pre-mint falsification cases. The
full `asmctx3`/`usage4` forms for tests 1, 3 and 5, and cross-profile test 6, require
subsequent WP4/WP5 functionality.

Owner treatment: accept the demonstrated pre-mint scope; preserve the remaining
runtime-form tests as mandatory obligations; do not represent pre-mint evidence as
completion of the future minting-path tests. Mapping of the Option A §5.2 tests to work
packages (based on accepted specifications):

| §5.2 test | Pre-mint (WP3/G-MINT) | Runtime form (WP4/WP5) |
| --- | --- | --- |
| 1 conditional defining API change | mapped `compctx1` change ✅ | `asmctx3` change (WP4) |
| 2 transitive-only: identity stable, `compctx2` changes | ✅ | — |
| 3 caller unchanged; target follows defining API | mapped `compctx1` ✅ | `usage4` (WP5) |
| 4 restore inputs → original identity | ✅ | — |
| 5 distinct defining compilations → distinct targets | mapped `compctx1` ✅ | `asmctx3` (WP4) |
| 6 equivalent reference artifact equality | actual-identity preservation ✅ | cross-profile equality (WP4/WP5) |
| 7 re-ingestion determinism | ✅ | separate-process at minting (WP4/WP5) |
| 8 stale publication refused (in-memory) | ✅ | native reopen (S2.3) |
| 9 deterministic documented mapping | ✅ | — |
| 10 no minting from unverified/incomplete; isolation | ✅ | — |

WP4/WP5 qualification must reproduce the relevant tests with actual minting behavior,
validate both rejection and legitimate acceptance, preserve independent expected
values, demonstrate that no mandatory association check is bypassed, and keep pre-mint
and runtime qualification distinct.

---

## 5. Accepted scope

The owner accepts, for the `G-MINT` pre-mint scope only:

1. The R2-faithful `compctx1` mapper and the accepted WP3 mapper/resolver behavior.
2. The pre-mint association invariant (complete universe, exact live defining binding,
   recorded-context equality) and its explicit refusal classifications.
3. Order-independent fail-closed refusal behavior.
4. The demonstrated in-memory Option A §5.2 pre-mint negatives and valid controls.
5. Construction-stage unsupported/external-evidence refusal.
6. The authorized NEW-04 deferral to WP6.
7. Trusted-evidence properties: R2 fidelity, PE-identity preservation, `compctx2`
   isolation, and no authority from producer-generated values.

`G-MINT` prerequisites for beginning separately commissioned WP4 work are satisfied.

---

## 6. Explicit non-acceptance

This decision must **not** be read as claiming:

- `asmctx3` or `usage4` minting is implemented.
- WP4 or WP5 is complete.
- Runtime Option A §5.2 forms are already qualified.
- `Complete` alone authorizes minting.
- Metadata-equivalent targets have exact-instance identity.
- Refusal-status precedence is specified.
- Production extraction or persistence exists.
- Native reopen is supported.
- Cross-platform qualification is established.
- NEW-04 is resolved.
- Complete S2.2 acceptance has been achieved.
- Package publication or releases are authorized.

The S-01 caller-supplied component-digest limitation (`identifierDigest`/
`declarationDigest`/`shapeDigest`) and all other applicable Q2 foundation
qualifications remain in force and are **not** closed by this decision.

---

## 7. WP4 entry contract

A separately commissioned WP4 must satisfy the following. Items marked **WP3/G-MINT:**
are already accepted and need only be consumed; items marked **WP4:** must be
implemented and demonstrated.

| # | Entry requirement | Status |
| --- | --- | --- |
| 1 | Consume the accepted semantic authority confirmed by G-CONFIRM (14 F16/NEW-02 rows, C1–C4) | **WP3/G-MINT:** accepted |
| 2 | Consume the accepted WP3 mapper/resolver behavior and the mapped R2-faithful `compctx1` | **WP3/G-MINT:** accepted |
| 3 | Consume the G-MINT-qualified association constraints (complete universe, exact live binding, recorded equality) | **WP3/G-MINT:** accepted |
| 4 | Always supply a non-null recorded `compctx2`; validate it; forbid omission/ bypass; `Complete` is not itself authority | **WP4:** implement + negative test |
| 5 | Preserve exact-live-instance vs metadata-identity distinctions (G1); no equivalence upgrade | **WP4:** implement + negative tests |
| 6 | Fail-closed: every non-`Complete` status prevents minting; no reliance on label precedence | **WP4:** implement + verify |
| 7 | Reproduce Option A §5.2 runtime-form tests (1, 3, 5, 6) with independent expected values | **WP4/WP5:** implement + qualify |
| 8 | Trusted evidence: mint only from verified live defining compilations and current `compctx2`; no producer-generated oracle | **WP4:** implement + qualify |
| 9 | No reliance on WP5/WP6 behavior; NEW-04 stays deferred; no coverage reliance | **WP4:** constraint |
| 10 | Independent qualification and owner acceptance before claiming WP4 completion | **WP4:** process gate |

WP4 must not claim `asmctx3`/`usage4` completeness, runtime Option A §5.2 completion,
persistence, or WP5/WP6 work.

---

## 8. Remaining WP4/WP5 obligations and NEW-04

- **WP4 (target minting):** supply and validate the recorded `compctx2`; preserve the
  identity distinctions; fail-closed refusal; construct `asmctx3` from the mapped
  `compctx1`; minting-path Option A §5.2 tests 1, 5 and the runtime aspects of 3 and 6.
- **WP5 (occurrence emission):** `usage4` construction, extraction emission, and the
  remaining runtime-form tests.
- **NEW-04:** remains deferred to **WP6** under the accepted boundary
  (G-CONFIRM owner decision §8); `CodeOccurrenceCoverage.Merge` is unchanged
  (`fc515ff`) and unused by WP3/G-MINT; no coverage reliance is permitted before WP6
  correction/falsification.
- **S2.3:** native persistence and reopen enforcement remain separately authorized and
  are not `G-MINT`/WP4 prerequisites.

---

## 9. Platform and evidence limitations

- All qualification executed on **Windows x64** (`net10.0`); no cross-platform result
  is inferred.
- Fixture-profile `compctx1` anchors are independent G-CONFIRM-Q1 evidence plus a frozen
  oracle section comparison; the production mapper was independently validated against
  the frozen R2 authority on the actual-path profile.
- Migration, dual-write, crash/power-loss and multi-process qualification remain out of
  scope.

---

## 10. Next-stage authorization boundary

| Item | Status |
| --- | --- |
| `G-MINT` (pre-mint association/authority qualification) | **PASSED WITH EXPLICIT QUALIFICATIONS** (this decision) |
| WP3 | Accepted (prior decision) |
| WP4 implementation | **ELIGIBLE** for a separate commission |
| WP5 | **NOT AUTHORIZED** |
| WP6 (NEW-04) | **NOT AUTHORIZED** (separate commission; deferral preserved) |
| WP7–WP9 | **NOT AUTHORIZED** |
| S2.3+ (native persistence/reopen) | **NOT AUTHORIZED** |
| Publication / package release / push | **NOT AUTHORIZED** |

**Preserved ordering:** `G-CONFIRM → WP3 → WP3-Q1 → WP3-OWNER → G-MINT-Q1 → G-MINT-OWNER → WP4 → WP5`.

---

## 11. Final owner decision

**`ACCEPTED WITH EXPLICIT QUALIFICATIONS`** — **`G-MINT PASSED`** for the bounded
pre-mint association and authority qualification only.

`G-MINT` is closed for its bounded pre-mint qualification scope. WP4 is eligible for a
separately commissioned implementation. This decision does not claim a production
minting implementation exists, does not authorize WP5/WP6/later work or publication,
and does not close complete S2.2. G1–G4 are preserved as explicit qualifications and
the WP4 entry contract in §7 is binding.

**Final disposition:** **`ADR-CI1-01-CI1A-S2.2-G-MINT-OWNER-01 ACCEPTED WITH EXPLICIT
QUALIFICATIONS` — `G-MINT PASSED`.**
