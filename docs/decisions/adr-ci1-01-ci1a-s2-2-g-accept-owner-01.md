# ADR-CI1-01-CI1A-S2.2-G-ACCEPT-OWNER-01 — Final stage-level acceptance of CI1A S2.2

**Commission:** `ANDIRIT-CI1A-S2.2-G-ACCEPT-OWNER-01` (final stage-level owner acceptance).
**Project:** Andirit. **Recorded:** 2026-11-06. **Canonical branch:** `main`.
**Decision:** **`ACCEPTED WITH EXPLICIT QUALIFICATIONS`**.
**Gate:** **`S2.2 G-ACCEPT PASSED`**.
**Scope:** Bounded verified Roslyn semantic occurrence extraction, target and usage
provenance, coverage/refusal integrity, identity stability, and frozen-corpus
integration.

> **Status: RECORDED — OWNER-AUTHORIZED.**
> **Decision: `ACCEPTED WITH EXPLICIT QUALIFICATIONS`. Gate: `S2.2 G-ACCEPT PASSED`.**
>
> All twelve `GA-1`…`GA-12` requirements from the WP8 owner decision §13 are
> dispositioned. Every mandatory S2.2 requirement is satisfied within the
> documented accepted boundaries, with the retained qualifications explicitly
> preserved and the S2.3/S2.4/S2.5 downstream obligations recorded.
>
> This decision accepts the **S2.2 stage only**. It does **not** authorize S2.3,
> S2.4, S2.5, Stage 6, native persistence/reopen, public Find Usages, package
> publication, or any push. It claims no capability beyond those established by
> the accepted owner and independent-review chain.

This decision is additive documentation. It changes no production code, producer
test, reviewer artifact, frozen expectation, prior owner decision, or accepted
planning contract. It preserves all historical qualification evidence.

---

## 1. Verified repository baseline

Verified before writing, at the start and end of this commission:

| Role | Commit | Tree |
| --- | --- | --- |
| Frozen R2 foundation / WP1–WP3 foundation owner | `e0d911055f4cc9bdbf9aa4cc338f5cc007737a22` | `ba9338d74bf59c584bef340dbda49ed46eb1c5d9` |
| G-CONFIRM owner acceptance | `812584dc56e11983c3ea7de8977fd1cd74ff0297` | `7167557024e1602c81b5e6b169455c0ad10a5864` |
| WP3 owner acceptance | `4fdc23cfd3d760639dd653ad92bb204376a08c05` | `55b5e990cfa16606d7ba09752d18d360bf01db08` |
| G-MINT owner acceptance | `d3da2e009154402a92275874ac4bb94426b97df3` | `029299bf277bc99212ea2ded311c0ec54f70fa09` |
| WP4 owner acceptance | `97c88e58851820468cd21ee66d7a8ba460baf23a` | `27cda08808b4b56ccd6524b82b8315666636bd8d` |
| WP5 owner acceptance | `f1e7f273750b41413621db869542e2415ca24345` | `794d3a3d2f498856cba18ed5633fb41407ee92ca` |
| WP6 owner acceptance | `2b35cd6c7577508b04e75b5bf34d23cbc261f308` | `c3706eb919d1455bd12746b22336d956e3e0c8ae` |
| WP7 owner acceptance | `7cef15aaf45e7c203b28541ed1b24bf27a3bb3b9` | `f43d25af2be04d1e439acab6e7b28d0b6fd8d94a` |
| WP8 harness/tests | `76ac976e4c42fa83f6e1561b4a19c6a5ebc0b06f` | `ac1c039f534da5c85bb49ce7a4c490894b0cb2ab` |
| WP8 implementation evidence | `db5d324febd642695657186cca6f5ac4c026fb55` | `a9d14a104c7a2dcac2b4d618749d67cd88b8ac42` |
| WP8-Q1 independent qualification | `a257c3952a0ac06d583a4dbf554612df459d66b1` | `5db5164302fa53db238dfa33b9f02f1619666944` |
| **WP8 owner acceptance / expected starting HEAD** | **`40b5062a6c04a37a1fe0641a4b5f86f2431baeb7`** | **`4772515e743e486bf124f1edbb62b3bdbc01b3b7`** |

Verified:

- **HEAD** is exactly `40b5062a6c04a37a1fe0641a4b5f86f2431baeb7`; **tree** is exactly
  `4772515e743e486bf124f1edbb62b3bdbc01b3b7` — as commissioned.
- Working tree **clean** (tracked and untracked); exactly **one** worktree
  (`D:/projects/Andirit`); **no stashes**; no in-progress operations.
- `origin/main` = `443c192733a53d2f1da415743debb84e8c81acb5` (an ancestor of HEAD);
  `main` is **15 ahead / 0 behind**; **nothing pushed**.
- The acceptance/reconciliation chain above is a consecutive ancestor chain
  (`git merge-base --is-ancestor`, exit 0 at every step); every tree reproduced
  exactly; no prior commit was rewritten.
- No concurrent writer was observed moving `HEAD`, the branch, or `origin/main`
  during this commission.
- `git diff 7cef15a..HEAD -- src docs/decisions docs/planning` is empty except
  `docs/decisions/adr-ci1-01-ci1a-s2-2-wp8-owner-01.md`: **no production-source,
  planning-contract, or prior owner-decision drift** accompanied WP8 owner
  acceptance.
- `git diff 2b35cd6..HEAD -- src` is empty: **no production-source change** since
  WP6. WP7 added tests only; WP8 added the corpus integration test only.

Additional authoritative identities resolved from the repository (not inferred):

| Authority | Commit |
| --- | --- |
| S2.1 owner acceptance (`ACCEPTED WITH QUALIFICATIONS`) | `96b9db8885997d2194072a7cc39eafd6ca295752` |
| S2.2 entry acceptance / implementation authorization | `febc28d92548fd7323294077c37444048741a8c8` |
| Option A identity direction (`S2.2-ID OPTION A ACCEPTED`) | `b7fea0d219409b279b78317e32bdc3c8b5ae61ee` |
| S2.0/F12 R2 corpus owner acceptance; obligation register last update | `a1cf47083747bae90293ec074b35fe8078bd0d6e` |
| Pre-release compatibility policy | `644167c4812dbb324e0d30de50dfbe55dd124df0` |
| Stage-2 boundary and acceptance matrix | `acd7ecd22516851ac57169fe19d184f6b10ac639` |

---

## 2. Authoritative S2.2 contract

The controlling contract, in precedence order:

1. **Owner decision** `adr-ci1-01-owner-decision.md` §F and its Q1 acceptance.
2. **Frozen R2** `PROVENANCE-CONTRACT.md` §§2–5 and Q3 `TARGET-IDENTITY-REVIEW.md`
   (incorporated by owner §F; transcribed in the identity contract).
3. **Option A** `adr-ci1-01-ci1a-s2-2-target-identity-decision.md`
   (`S2.2-ID OPTION A ACCEPTED`; binding mapping contract §5.1 and required
   tests §5.2).
4. **S2.1** `adr-ci1-01-ci1a-s2-1-owner-acceptance.md` and the S2.1 provenance
   contract.
5. **Consolidated obligation register** `adr-ci1-01-ci1a-s2-obligation-register.md`
   §§4A, 5, 7, 8, and the S2.0/F12 owner acceptance.
6. **S2.2 entry package** `docs/planning/adr-ci1-01-ci1a-s2-2/` (scope and entry
   criteria, obligation/dependency matrix, risk register, mapping contract,
   execution-readiness handoff) and the **S2.2 entry acceptance** (`febc28d`).
7. **Pre-release compatibility policy** (`644167c`).

**Governing §6.2 sequence and numbering.** The accepted §6.2 work-package
numbering governs. The execution-readiness handoff uses an older draft numbering
in which the integration/comparison package is "WP9" (its WP6=NEW-04, WP7=F09,
WP8=Option A). Under the accepted §6.2 scope, the integration/comparison package
is **WP8** and **there is no separate WP9 work package or WP9 gate**. This is
confirmed by the WP7 owner decision §8 and the WP8 owner decision §12. No WP9
obligation is manufactured here.

**G-ACCEPT gate definition (authoritative):** *"S2.2 acceptance (Q-F2, Q-F3,
NEW-04, S-01, mapping review satisfied)"* (execution-readiness handoff §C). The
WP8 owner decision §13 supplies the `GA-1`…`GA-12` evaluation contract. The
accepted quantitative/qualitative criteria are §6.6; the evidence and
reproducibility rules are §6.7.

---

## 3. GA-1 — Complete work-package closure

Implementation, independent qualification, and owner decision for every accepted
§6.2 package. "Owner" is the formal owner acceptance; "IR" is independent
source-bound review.

| Stage | Implementation | Independent qualification | Owner decision | Accepted scope / retained qualification | Depends on | Current status |
| --- | --- | --- | --- | --- | --- | --- |
| Entry | `febc28d` (entry package at `56877aa`) | — | `febc28d` | Entry accepted; Q-F2/Q-F3/A-01/A-02/S-01/NEW-01/NEW-04/durability/F16-NEW-02 dispositions recorded | S2.1 `96b9db8`; R2 `a1cf470`; Option A `b7fea0d` | Accepted |
| WP1–WP3 foundation | `d5644d8` (R1), `0c989e2` (foundation) | Q1 `38d4e51`; Q2 `d5a785f` | `e0d9110` | Q-F2/Q-F3/A-01 corrections + mapping contract; bounded surfaces retained | Entry | Accepted with qualifications |
| G-CONFIRM | `391abbee` (review pkg) | G-CONFIRM-Q1 `391abbee`/`1f51db3` | `812584d` | 14 F16/NEW-02 rows' authority + expected-value plan; C1–C4; **`G-CONFIRM PASSED`** | Foundation `e0d9110` | Passed |
| WP3 | `ff58a92` | WP3-Q1 `2cdcd10` | `4fdc23c` | R2-faithful `compctx1` mapper + pre-mint resolver; F1–F5 / GM-1…GM-7 carried | G-CONFIRM | Accepted with qualifications |
| G-MINT | (pre-mint gate) | G-MINT-Q1 `1c382d6` | `d3da2e0` | Pre-mint association/authority; G1–G4 preserved; **`G-MINT PASSED`** | WP3 `4fdc23c` | Passed |
| WP4 | `eac3022` | WP4-Q1 `6ecf996` | `97c88e5` | Internal `asmctx3` minting (`VerifiedCSharpUniverse.MintTarget`), mandatory recorded-`compctx2` enforcement, fail-closed refusal; W1–W6 carried | G-MINT | Accepted with qualifications |
| WP5 | `a5f04fd`; F1 remediation `2127a9a` | WP5-Q1 `958d98a` (REMEDIATION REQUIRED); WP5-Q2 `f10b472` | `f1e7f27` | `usage4` construction, extraction, coverage/refusal emission, F09 heuristic scanner; F1 remediated; G1–G3/F2–F8 carried | WP4 + G-MINT | Accepted with qualifications |
| WP6 | `6b45bde` | WP6-Q1 `109b200` | `2b35cd6` | NEW-04 coverage-merge correction; G-Q1A2/E1/A7/FLAG/OVF carried; **NEW-04 CLOSED WITH EXPLICIT QUALIFICATIONS** | WP5 | Accepted with qualifications |
| WP7 | `cdd416d` (tests only) | WP7-Q1 `42ef163` | `7cef15a` | Option A §5.2 tests 2/6/8 (in-memory), declared profiles, stale-publication refusal; no production change | WP6 | Accepted with qualifications |
| WP8 | `76ac976` (harness) | WP8-Q1 `a257c395` | `40b5062` | Frozen R2 corpus integration/comparison, F16 evidence, determinism; Q1-WP8-1…5 carried | WP7 | Accepted with qualifications |

**Mandatory ordering verified.** The accepted ordering
`G-CONFIRM → WP3 → G-MINT → WP4 → WP5 → WP6 → WP7 → WP8` was respected:
independent confirmation preceded dependent qualification-test authoring
(G-CONFIRM), and the mapping/compatibility contract was independently reviewed
before any target minting (G-MINT). WP4/WP5 extraction and minting never ran
before G-MINT cleared. Implementation completion is **not** equated with owner
acceptance anywhere above: every package has a distinct, later owner decision.

**Result: SATISFIED.** Every accepted §6.2 work package has an implementation
identity, an independent qualification identity, and an owner decision. The
numbering is reconciled; there is no independent WP9 obligation.

---

## 4. GA-1 … GA-12 decision matrix

Status vocabulary: **SATISFIED** · **SATISFIED WITH EXPLICIT QUALIFICATIONS** ·
**DEFERRED BY ACCEPTED STAGE BOUNDARY** · **UNSATISFIED** · **DECISION REQUIRED**.

| Gate | Requirement | Accepted authority | Existing implementation | Independent evidence | Owner decision | Retained qualification | Remaining action | Stage allocation | Result |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| **GA-1** | Work-package closure chain | §6.2; owner decisions | All packages implemented | G-CONFIRM-Q1; WP3-Q1; G-MINT-Q1; WP4-Q1; WP5-Q1/Q2; WP6-Q1; WP7-Q1; WP8-Q1 | `e0d9110`…`40b5062` | Each package retained its own qualifications | None | S2.2 | **SATISFIED** |
| **GA-2** | Identity/mapping authority | Option A §5; mapping contract §1–3; G-MINT | Mapper/resolver/mint/mint-usage/extractor | WP3-Q1; G-MINT-Q1; WP4-Q1; WP5-Q2; WP7-Q1; WP8-Q1 | `d3da2e0`, `97c88e5`, `f1e7f27`, `7cef15a`, `40b5062` | G1/F1 metadata-vs-instance; S-01 component digests; declared profiles | None | S2.2 | **SATISFIED WITH EXPLICIT QUALIFICATIONS** |
| **GA-3** | Q-F2 / Q-F3 / A-01 | Register §4A; S21A §F; entry §3.1–3.3 | `VerifiedCSharpSource`; `VerifiedMetadataReference.Capture`; `VerifiedCSharpCompilation.Create` | WP1–WP3 foundation; G-CONFIRM-Q1 rows 1–3; WP8 corpus | `e0d9110`, `812584d` | Bounded Q-F2 NUL heuristic; bounded Q-F3 surface; A-01 internal-only | None | S2.2 | **SATISFIED WITH EXPLICIT QUALIFICATIONS** |
| **GA-4** | NEW-04 coverage merge | Register §4; WP6-OWNER | Corrected `CodeOccurrenceCoverage`/`Merge` | WP6-Q1; WP8 corpus merge probes | `2b35cd6` | Disjoint-contribution contract; G-Q1A2/E1/A7/FLAG/OVF; native reopen open | None | S2.2 (native reopen → S2.3) | **SATISFIED WITH EXPLICIT QUALIFICATIONS** |
| **GA-5** | S-01 boundary | Register §7; S2.1 §H; S2.0 §7 Q-A; scope §7 | Live defining/source/target correspondence chain | WP4-Q1; WP5-Q2; WP7-Q1; WP8-Q1 | `40b5062` GA-5 | **General source canonicalization of caller-supplied component digests not implemented/independently qualified** | Owner acknowledgment recorded here | S2.2 correspondence; S2.5 integrated proof | **SATISFIED WITH EXPLICIT QUALIFICATIONS** |
| **GA-6** | F16 / NEW-02 | Register §3 F16; F12A §8; G-CONFIRM | 14 nine-part rows; traceability | G-CONFIRM-Q1 (14 rows); WP8-Q1 §12; collective per-WP reviews | `812584d`; stage-wide closure recorded here | Rows 5/6/13 closure authority remain profile/S2.3-bound; row 13 native half open | None | S2.2 | **SATISFIED WITH EXPLICIT QUALIFICATIONS** |
| **GA-7** | A-02 and standing constraints | Register §4A/§7; §6.3 | Scope/universe membership from `VerifiedCSharpUniverse`; no scope-as-inventory | G-CONFIRM-Q1 row 4; WP8 scope-mismatch probe | `e0d9110` | `analysisscope1` is not a source-inventory digest | None | S2.2–S2.5 standing | **SATISFIED WITH EXPLICIT QUALIFICATIONS** |
| **GA-8** | Option A durability boundary | Entry §3.5; Option A §5.1(5); mapping §4 | Extraction-time association + stale refusal (in-memory) | WP5-Q2; WP6-Q1; WP7-Q1 test 8 in-memory | `e0d9110` §3.5; `40b5062` | Native persistence/reopen not implemented; not claimed durable | First S2.3 gate = reopen stale-refusal | S2.2 extraction-time → S2.3 | **DEFERRED BY ACCEPTED STAGE BOUNDARY** |
| **GA-9** | NEW-01 non-reliance | Register §4; S2.1 boundary §4 | S2.2 uses live compilations + in-memory/test facts only | WP5-Q2; WP7-Q1 (in-memory store path) | `e0d9110` §3.7 | NEW-01 recovery defect not fixed; not closed | NEW-01 before S2.3 recovery reliance | S2.2 non-reliance → S2.3 prerequisite | **SATISFIED WITH EXPLICIT QUALIFICATIONS** |
| **GA-10** | Corpus, oracle, reproducibility | §6.6/§6.7; WP8-OWNER | WP8 integration/comparison harness | WP8-IMPL; WP8-Q1; corpus authentication; oracle 185/185 | `40b5062` | Q1-WP8-5 payload boundary; declared-profile anchors | None | S2.2 | **SATISFIED WITH EXPLICIT QUALIFICATIONS** |
| **GA-11** | Platform/publication boundaries | §6.2 EXCL; prior platform records | Windows x64 evidence only | WP8-Q1 §16; all WP reviews | `40b5062` | Windows-only; toolchain-bound bytes; publication excluded | Cross-platform at release stage | S2.2 boundary → S2.5/Stage 6 | **SATISFIED WITH EXPLICIT QUALIFICATIONS** |
| **GA-12** | Final disposition | §6.9; WP8 §13 | — | This decision | This decision | All of the above | S2.3/S2.4/S2.5 as recorded | Stage-wide | **ACCEPTED WITH EXPLICIT QUALIFICATIONS** |

No gate is `UNSATISFIED`. No gate is `DECISION REQUIRED` beyond this record. The
single deferred gate (GA-8) has explicit authority for deferral (entry acceptance
§3.5, Option A §5.1(5), mapping contract §4). GA-5's retained canonicalization
limitation is addressed explicitly in §8 below.

---

## 5. GA-2 — Identity and mapping authority

The accepted Option A model is preserved intact and was independently verified:

- **Frozen `compctx1` mapping** — `asmctx3` is computed with the frozen R2
  algorithm over the R2-faithful `compctx1` of the **defining** compilation.
  `MapR2FaithfulCompctx1` performs the single documented substitution of each
  verified `compilation-context|<dep compctx2>` item by the frozen R2 legacy
  project-reference item, preserves actual verified PE identities, and is
  deterministic; the production mapper digest equals the frozen R2 authority
  (`G-MINT-Q1` GM-7; `WP8-Q1` §7). The documented mapping is a **mapping, not a
  redefinition**, and is not a strong project-reference claim.
- **Additive `compctx2` validity context** — `compctx2` is provenance validity and
  universe membership only. It never enters `asmctx3` or `usage4`
  (`ComputeTargetId` rejects a `compctx2` prefix).
- **Frozen `asmctx3`/`usage4` formulas** — reproduced exactly within the declared
  production profile (S01 `asmctx3 = ece8e803…`, `usage4 = 1027f062…`, Core
  `compctx1 = afcb56d0…`) and independently re-derived from the frozen formulas
  in `WP8-Q1`.
- **Tuple encoding and identity domains** — 32-bit big-endian length prefixes,
  lowercase hex, `asmctx3:sha256:<64 hex>`; text/tree distinction preserved.
- **Verified compilation-universe membership** and **correct defining
  association** — `ResolveDefining` binds the exact live defining instance;
  cross-project binding requires the exact live `CompilationReference` plus a
  matching metadata identity of the target's containing assembly.
- **Mandatory non-null recorded `compctx2`** — `MintTarget` requires a supplied,
  validated, non-null recorded context; `Complete` alone is not minting authority
  (WP4 W-condition G3 satisfied).
- **Current-context validation before minting** and **refusal of stale or
  substituted authority** — stale/fabricated/missing/substituted/substituted-
  instance contexts refuse with no artifact.
- **No identity churn** from transitive-only dependency changes — `asmctx3`
  stable, `compctx2` changes at all levels (WP7 test 2).
- **No promotion of structural identities into trusted facts** — structural
  `CodeOccurrenceIdentity.ComputeTargetId` output is not authority (WP4 W2 /
  WP5 F6); trusted `usage4` is emitted only through the WP4 mint.
- **Metadata-equivalent association vs exact target-instance identity** — the
  accepted qualified distinction is preserved: metadata equivalence is **not**
  proof of exact target-instance identity, and no stronger claim is made.

`compctx2` isolation from `asmctx3`/`usage4` is structurally enforced and
negatively tested (F16 row 6; WP4/WP5 negative suites).

**Claims supported only by producer tests?** None relied upon. Every GA-2 claim is
covered by an independent reviewer probe suite (`WP3-Q1`, `G-MINT-Q1`, `WP4-Q1`,
`WP5-Q2`, `WP7-Q1`, `WP8-Q1`) as well as producer tests. Identity guarantees are
**not** broadened beyond the declared comparison profiles (§14).

**Result: SATISFIED WITH EXPLICIT QUALIFICATIONS.**

---

## 6. GA-3 — Q-F2, Q-F3, and A-01

| Obligation | Accepted contract (bounded) | Implementation | Tests / independent evidence | Owner disposition |
| --- | --- | --- | --- | --- |
| **Q-F2** encoding | Supported: strict UTF-8 ±BOM; strict UTF-16 LE/BE and UTF-32 LE/BE **with BOM**. A BOM-less decode is strict UTF-8; if decoded text contains U+0000 it is refused as `AmbiguousBomlessEncoding:<path>` with **no verified identity**; raw byte SHA-256 stays honest. | `VerifiedCSharpSource.ReadAsync` (`VerifiedCSharpSource.cs`) | WP1 suite (BOM-less UTF-16/32 refusal; BOM-present acceptance; `UnverifiedSource`); foundation owner `e0d9110`; G-CONFIRM-Q1 row 1 | Accepted with qualification |
| **Q-F3** metadata validity | Eager assembly/metadata validation before identity: immutable acquisition + SHA-256 + Roslyn assembly materialization + `IsAssembly` manifest guard + bounded structural metadata validation; a non-usable image refuses (`UnsupportedMetadataReference`) before any identity is produced. No universal PE/metadata validity guarantee. | `VerifiedMetadataReference.Capture` (`VerifiedCSharpCompilation.cs`, `MetadataReferenceValidation.cs`) | WP2 suite (truncated PE, garbage, netmodule refused; valid assembly captured); G-CONFIRM-Q1 row 2 (C1 bounded wording) | Accepted with qualification |
| **A-01** symbol canonicalization | Preprocessor symbols containing `,` or `|` are refused (`UnsupportedParseConfiguration`); ordinary ordering deterministic; ordinal Unicode distinctness preserved. **Internal surface only**; S2.2 creates no public or caller-supplied parse-option reuse. | `VerifiedCSharpCompilation.Create` | WP3 symbol-hardening tests; G-CONFIRM-Q1 row 3 | Accepted with qualification |

- **Q-F2**: the source bytes used for identity are the exact acquired bytes fed to
  Roslyn; a BOM-less NUL-bearing decode cannot receive a trusted verified source
  identity. The NUL heuristic is a documented bounded rule (a legitimate BOM-less
  UTF-8 file containing a literal NUL is refused).
- **Q-F3**: malformed/truncated/non-assembly reference inputs cannot establish
  unsupported authority. Documented excluded surfaces (malformed field signature,
  invalid `TypeRef` resolution scope, malformed modifier `TypeSpec` syntax) still
  capture; **no complete PE/IL correctness or semantic-usability claim is made.**
- **A-01**: unsupported symbols cannot silently alter analysis semantics while
  retaining a valid verified identity.

Closure is **not** inferred from the WP8 corpus alone: each obligation has its own
WP1–WP3 implementation, producer tests, and independent confirmation.

**Result: SATISFIED WITH EXPLICIT QUALIFICATIONS.**

---

## 7. GA-4 — NEW-04 coverage integrity

Formal NEW-04 closure is verified through the WP6 owner decision (`2b35cd6`):

- **Count aggregation:** disjoint-contribution unit/occurrence/refusal/`UnavailableReasons`
  counts now **sum** (not `Math.Max`); the 20-case producer matrix and `Q1-B` use
  distinct unequal values so a max cannot masquerade as a sum.
- **First-scope / mixed-scope:** mixed non-null scopes → `ScopeId=null` and state
  capped at `Partial`; `IsComplete=false`; P06 probes through record, store, and
  sink paths refuse completeness.
- **Incompatible-universe refusal:** differing non-null universes refuse in both
  orders and in multi-step combinations (`G-Q1E1` records the store-level surfacing).
- **Unconditional `verified <= declared`:** rejected without the former
  `declared=0` exemption; `0/0`, `1/0`, `1/1` remain valid.
- **`declared=0` behavior:** `declared=0, verified>0` is rejected.
- **No P06 false `Produced`;** legitimate same-scope complete merges preserved.
- **Refusal counters and `UnavailableReasons` preserved** through merge.

Explicit WP6 qualifications carried: **G-Q1A2** (mixed-scope merge also discards a
matching universe attribution — accepted conservative behavior; agreeing-universe
attribution is not claimed preserved), **G-Q1E1** (cross-universe units make
occurrence queries fail closed at `RepositoryCoverage`), **G-Q1A7** (unattributed
`Produced` acquiring universe identity is reachable only from test fixtures, not
production producers; owner-verified inventory), **G-Q1FLAG** (legacy Lattice
`CompleteCoverage` restore branch is read-only, default `false`, and unreachable
for records current code can write), **G-Q1OVF** (overflow fails closed; no
wraparound).

**NEW-04 is `CLOSED WITH EXPLICIT QUALIFICATIONS` only within this independently
accepted scope.** The register's *"native persistence/reopen if published"* clause
is **not** satisfied by the tested in-memory/store-level behavior and remains open
for **S2.3** (publication has not occurred). Native persistence/reopen acceptance
is **not** inferred.

**Result: SATISFIED WITH EXPLICIT QUALIFICATIONS.**

---

## 8. GA-5 — S-01 source-to-fact correspondence (the canonicalization boundary)

### 8.1 The mandatory chain — satisfied

The accepted S2.2 S-01 obligation is the correspondence chain
**source bytes → verified compilation → semantic binding → defining target →
canonical identity → trusted usage fact**. Reconstructed from the register §7
(*"S2.2 semantic correspondence"*), the S2.1 acceptance §H (*"S-01 defining-target
binding | During S2.2, independently qualified"*), and the Stage-2 acceptance
matrix §1, the accepted S2.2 portion is:

1. The compiled source content matches the verified source content — **holds**
   (WP8 corpus authentication; `VerifiedCSharpSource`; F16 rows 1/12).
2. Roslyn parse/compilation options are correctly represented — **holds**
   (WP8 parse-option reproduction; `VerifiedCSharpCompilation`).
3. Real metadata and project references are used — **holds** (WP2; WP8 captures the
   real framework PEs and external image).
4. Target binding originates from the verified live compilation — **holds**
   (`ResolveDefining`; WP4-Q1; WP8-Q1 §7).
5. Defining-compilation association is checked — **holds** (exact live instance +
   metadata consistency; G-MINT-Q1).
6. Recorded `compctx2` is validated — **holds** (`MintTarget`; WP4-Q1; WP5-Q2).
7. `asmctx3` and `usage4` follow frozen formulas — **holds** (WP8-Q1 §7).
8. Unsupported or stale evidence does not mint trusted facts — **holds** (WP5-Q2
   refusal matrix; WP8-Q1 §9).
9. The WP8 corpus comparison exercises this chain using **real production
   behavior** — **holds** (WP8-IMPL §6; WP8-Q1 §4) over the frozen R2 corpus.

### 8.2 The component-digest qualification — explicitly preserved, not closed

**The identity component digests `identifierDigest`/`declarationDigest`/
`shapeDigest` remain caller-supplied and are not independently source-canonicalized.**
`VerifiedCSharpOccurrenceExtractor` takes them as `Func<IMethodSymbol,string>`
inputs; the WP8 harness and every WP5/WP7 test supply fixture-declared or
WP8-declared forms (e.g. `m => "wp8-method|" + m.Parameters.Length`). There is no
production canonicalization of these components, and no production wiring of the
extractor to a plugin flow.

**Reconciliation (evidence-grounded, not silent).** The authority corpus contains a
genuine nuance, recorded here without erasing any qualification:

- **Assigning it to S2.2:** S2.0 owner §7 (`Q-A`: *"general S2.2 source
  canonicalization remains to implement and qualify"*), the entry scope §7
  (*"S2.2 must still implement and qualify general source canonicalization (the
  fixture-declared digest forms do not satisfy it)"*), the identity mapping §5
  (*"that is an S2.2/S-01 obligation"*), and the risk register (`Q-A`).
- **Characterizing the S2.2 gate item as correspondence, with the integrated proof
  at S2.5:** register §7 (*"S2.2 semantic correspondence, S2.5 integrated
  independent proof"*), the Stage-2 boundary §8 (S2.2 extracts/binds/mints;
  S2.5 verifies the S-01 proof chain), and the WP8 G-ACCEPT entry contract GA-5
  (*"S2.2 semantic correspondence qualified; full integrated proof S2.5"*;
  acceptance condition *"S2.2 correspondence accepted; S2.5 obligation preserved"*;
  exclusion *"Importing S2.5 work into S2.2 (or erasing the qualification)"*).
- **Every WP owner decision preserved it and did not claim it:** WP3 §8
  (*"remain the accepted S2.2/S-01 obligation"*), WP4 W4 (*"Accepted only within
  that explicit scope"*), WP5 F8/S-01 (*"PRESERVED as the standing S-01
  qualification"*), WP6 §8, WP7, WP8 §5/Q1-WP8-2.

**Determination.** The mandatory S2.2 S-01 **gate item** — the live
defining/source/target correspondence chain — is **implemented and independently
qualified** (§8.1). The **general source canonicalization of the caller-supplied
component digests is *not* implemented or independently qualified, and is *not*
claimed.** It is recorded as an **explicitly preserved S2.2/S-01 qualification**,
consistent with the accepted characterization of the S2.2 item as *semantic
correspondence* and the allocation of the *integrated proof* to S2.5. This record
provides the explicit owner acknowledgment of that boundary; it is therefore
**not** a silent deferral, and it does **not** erase the qualification.

**If the owner instead holds general source canonicalization to be a hard S2.2
acceptance condition**, then the correct disposition would be **ACCEPTANCE
DEFERRED**, and the smallest follow-up would be a bounded package that implements
and independently qualifies the canonical source derivation of
`identifierDigest`/`declarationDigest`/`shapeDigest` from the verified live
compilation. Under the accepted owner decisions, it is a preserved qualification,
not a blocker. No stronger provenance authority than has been established is
asserted here.

**Result: SATISFIED WITH EXPLICIT QUALIFICATIONS.**

---

## 9. GA-6 — F16 and NEW-02

### 9.1 NEW-02

The corrected R2 identity matrix (`usage4` order/domain/components and `asmctx3`
component domains and encoding) was **independently authenticated before dependent
qualification tests were authored**, per the G-CONFIRM decision (`812584d`):
G-CONFIRM-Q1 independently reconstructed both frozen `usage4` anchors
(`bd0487b7…`, `b874bdc5…`) and the `compctx1` self-test anchor (`71d89657…`) with
no Andirit imports. Producer tests are cited only as corroboration; no
implementation-originated expected value is used.

### 9.2 F16 — all fourteen rows

All 14 accepted rows (execution-readiness handoff §D / G-CONFIRM package §2) carry
the nine required fields (**requirement, authority, production surface, observable
behavior, positive test, adversary, environment, evidence artifact, closure
authority**), map to real production surfaces, positive **and** adversarial
assertions, an environment, committed evidence, and closure authority **IR+O**:

| # | Requirement | Authority | Production surface | Evidence artifact | Closure |
| --- | --- | --- | --- | --- | --- |
| 1 | Q-F2 supported-encoding set + BOM-less refusal | S21A §F; REG §4A | `VerifiedCSharpSource` | WP1 TRX / log | IR+O |
| 2 | Q-F3 bounded metadata capture boundary | S21A §F; Q1-F01; Q2 | `VerifiedMetadataReference.Capture` | WP2 TRX / log | IR+O |
| 3 | A-01 symbol canonicalization hardening | S21A §F | `VerifiedCSharpCompilation.Create` | WP3 TRX | IR+O |
| 4 | A-02 scope is not source inventory | S21A §F; S2.1 §3 | `VerifiedCSharpScope`/`Universe` | test + statement | IR+O |
| 5 | R2-faithful `compctx1` under a declared profile | S22ID §5.1(1); mapping §2 | `CompilationProvenanceDescriptor` + frozen oracle | profile/vectors/oracle log | IR+O (G-MINT) |
| 6 | `compctx2` isolation from identity | S22ID §5.1(3)(7) | minting/validation | negative test | IR+O (G-MINT) |
| 7 | Canonical target minting under live authority | S22ID §5.1(2); stage2-boundary §3.5 | extraction + mint | probes + log | IR+O |
| 8 | Invocation extraction fidelity | stage2-boundary §3.6–3.7 | extractor | TRX / log | IR+O |
| 9 | Coverage/refusal classification | stage2-boundary §3.9/§5 | extractor + coverage | matrix + TRX | IR+O |
| 10 | Unconditional-inactive scanner (F09) | REG §3 | extractor | TRX / log | IR+O |
| 11 | Coverage-merge authority (NEW-04) | REG §4; S2.1 §4 | `CodeOccurrenceCoverage`/ctor | falsification probes | IR+O |
| 12 | Live defining/source/target correspondence (S-01) | REG §7; S2.1 §6 | verified analysis + extraction | chain evidence | IR+O |
| 13 | Option A durability association | S22ID §5.1(5) | extraction/coverage model (native excluded) | test + boundary statement | IR+O (S2.3 reopen) |
| 14 | F16/NEW-02 confirmation itself | REG §3; F12A §8 | docs/tests | review record | IR+O |

Verified for every row: exact authority; correct production surface; actual
executed tests; reproducible evidence present in the repository; positive and
negative coverage; applicable qualifications; independent review; owner closure.
No row is a filled-but-circular placeholder.

**Circularity:** none. Expected values come from the immutable frozen
`oracle-expected-r2.json` and the frozen R2 `CompilationProvenance.Describe`
compiled independently into the harness; the production mapper/extractor is never
its own oracle (WP8-Q1 §4 mutation challenge).

### 9.3 Stage-wide closure authority and the stage-wide review

The WP8 owner decision §7 reserved **stage-wide F16 closure authority to this
commission**; producer/reviewer execution alone does not close the rows. This
decision **exercises that authority** for the S2.2 stage.

On the stage-wide independent Q1 question: the execution-readiness handoff §E
(blocker B6) originally envisioned a **single** discrete stage-wide Q1 review. The
accepted sequence that actually ran **superseded** that with a more granular set
of independent, source-bound reviews — **G-CONFIRM-Q1** (F16/NEW-02 authority and
expected values), **WP3-Q1**, **G-MINT-Q1**, **WP4-Q1**, **WP5-Q1**, **WP5-Q2**,
**WP6-Q1**, **WP7-Q1**, and **WP8-Q1** — covering every work package, expected
value, refusal path, and the corpus integration. All nine probe suites were
**reproduced at this HEAD** (§19), not merely cited.

**Determination.** The existing independent reviews **collectively** discharge the
stage-wide independent qualification requirement; WP8-Q1 is **not** substituted
for it in isolation. F16 stage-wide closure is recorded. **No discrete additional
stage-wide Q1 review was performed**, and none is manufactured. If the owner
nevertheless deems a discrete stage-wide Q1 mandatory notwithstanding the
collective reviews, that is the smallest follow-up gate; the accepted per-WP gate
authority is satisfied.

**Result: SATISFIED WITH EXPLICIT QUALIFICATIONS.**

---

## 10. GA-7 — A-02 and standing constraints

- **A-02:** `analysisscope1` binds repository + project-unit/path pairs only; it is
  **not** a source-inventory digest. Authoritative membership comes from the
  verified `VerifiedCSharpUniverse` and the accepted association rules
  (`ScopeSourceMembershipMismatch`; `compctx2` descriptors). Confirmed by
  G-CONFIRM-Q1 row 4 and the WP8 scope-mismatch refusal probe.
- **D3/D4/D5:** preserved — no graph-identity/traversal change, no publication
  authority change, no migration/dual-write/legacy readers (no production change
  since WP6; frozen formulas unchanged).
- **Frozen identity semantics:** preserved (frozen oracle 185/185 at this HEAD).
- **Correct live-instance qualification:** preserved (exact live binding;
  metadata-equivalence not upgraded).
- **Accepted semantic and profile boundaries:** preserved (§14).
- **No unsupported successful empty results:** `Produced` with zero facts occurs
  only under a verified complete scope; `Partial`/`Incomplete` never present as
  successful empty; `AnalysisUnavailable` distinguished from complete-empty.
- **No unqualified recovery dependence:** confirmed (GA-9).
- **No new public Find Usages API:** internal-only; no public surface added.

Existing integration code does not convert an accepted structural observation
(`CodeOccurrenceIdentity.ComputeTargetId` shape) into authoritative semantic
evidence without the WP4/WP5 validation gate.

**Result: SATISFIED WITH EXPLICIT QUALIFICATIONS.**

---

## 11. GA-8 — Option A durability boundary

The accepted S2.2/S2.3 split (option (a), entry acceptance §3.5) is authoritative:

**S2.2 demonstrated:**
- recorded defining `compctx2` association on the occurrence-evidence model,
- extraction-time validation (mandatory non-null recorded context),
- stale-context refusal (in-memory),
- in-memory stale-publication refusal (`StalePublication`, no hits),
- stable identity without stale-authority reuse (no identity churn).

**S2.3 has not yet demonstrated:**
- native persistence of the mandatory association,
- reopen-time validity enforcement,
- recovery-safe stale-publication refusal,
- any required native publication/replacement lifecycle.

**First required S2.3 durability gate (recorded explicitly):** a reopen test
proving that a stale/changed defining `compctx2` **refuses prior facts after
publication** — Option A §5.2 **test 8, second half** (mapping contract §4; WP7
owner §5; WP8 owner §11).

**Accepted escalation preserved:** if S2.3 cannot persist and enforce the
association after reopen, **stop and return to the owner with the bounded Option B
alternative** before any production reliance (Option A §5.1(5)/§7; mapping §4).

S2.2 is **not** claimed durable across native reopen.

**Result: DEFERRED BY ACCEPTED STAGE BOUNDARY** (S2.2 extraction-time portion
satisfied; native reopen explicitly allocated to S2.3 with authority).

---

## 12. GA-9 — NEW-01 non-reliance

- The formal NEW-01 disposition (`REG §4 / Significant`) is verified: the
  known-failure replay loses occurrence projection; **not fixed**; assigned the
  separately authorized follow-up `ADR-CI1-01-CI1A-S2-FOUNDATION-RECOVERY-01`.
- **S2.2 does not depend on unqualified failure recovery or occurrence-projection
  recovery.** Extraction and qualification operate on live compilations and
  in-memory/test occurrence facts and the real `InMemoryCodeGraphStore`
  stage/complete/query path (WP7 test 8 in-memory); no S2.2 path invokes
  `DurableCommandLog.ReplayAsync`, native recovery, or occurrence-projection
  replay.
- The WP7 in-memory stale-publication result is explicitly **not** proof that
  stored or restored publication evidence is safe.
- **NEW-01 remains a mandatory independently-qualified gate before S2.3** and any
  earlier production recovery reliance. It is **not** claimed closed.

**Result: SATISFIED WITH EXPLICIT QUALIFICATIONS** (non-reliance confirmed;
NEW-01 remains an S2.3 prerequisite).

---

## 13. GA-10 — Corpus, oracle, and reproducibility

### 13.1 Quantitative minima (§6.6)

| Metric | §6.6 minimum | Accepted R2 | Met |
| --- | ---: | ---: | --- |
| Source files | 10 | 11 | ✅ |
| Invocations | 50 | 53 | ✅ |
| Method references | 20 | 21 | ✅ |
| Overloaded methods | 5 | 5 | ✅ |
| Namespaces | 3 | 5 | ✅ |
| Projects | 2 | 5 | ✅ |
| Cross-project invocations | 5 | 52 | ✅ |

Independently recomputed at the accepted definitions (WP8-Q1 §3). These are
**corpus/integration measurements**, not universal production-extraction
guarantees. Corpus authentication: 11 C# files byte-verified against
`fixtureManifest`; 5 projects/5 namespaces independently counted.

### 13.2 WP8 integration

- **50** frozen invocation expectations matched exactly (spans, documentation ids;
  direct/static/overload/generic/reduced-extension/explicit-interface/partial/
  cross-project forms).
- **51** trusted facts matched the independently derived supported set **exactly**
  (set equality over stable `(path, span, constructed documentation identity)`
  tuples, not numeric equality alone).
- The additional unmarked `PartialType.Right()` is correctly accounted as the 51st
  supported fact and is not one of the 50 frozen rows.
- **Two** unsupported invocation forms (delegate; local function) do not mint facts
  (`Unsupported=2`).
- Unsupported/deferred forms remain within bounded coverage semantics (21 method
  references and other deferred categories mint **no** trusted `usage4`; the
  corpus minimum is satisfied at corpus level and is **not** represented as 21
  produced facts).
- Negative cases refuse trusted emission (`Ambiguous=1`, `Unresolved=4`,
  `ConditionalInactive=1`, `External=0`, **0 trusted facts**; `m.Static(1)` CS0176
  refused).
- Required identity/profile relationships are correct (S01 production-profile
  anchors; cross-profile not compared).
- Complete-empty (`Solo`, verified) is distinguished from unavailable analysis and
  from zero-fact `Produced` with recorded refusals (`Negatives`).

### 13.3 Frozen oracle

- **185/185** checks passed, exit 0 (reproduced at this HEAD).
- Output SHA-256 `97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9`
  == accepted output.
- Expected SHA-256 `274A7AAE9EB6724879D619416E449C52759B44085D7B718DAD3434B15387CBBF`
  == accepted, unchanged.
- Frozen source and expected files unchanged (no corpus/oracle file modified by
  WP8 or by this commission).

### 13.4 Determinism

- Producer canonical output: **12 108 bytes**, SHA-256
  `3f08abc616858224b78a9b08a125e167374cbeb40c84c145db2b2c8ab4817ff7` (two fresh
  processes, MATCH).
- Independently generated reviewer canonical output: **11 821 bytes**, digest
  `00a46e4e96205049e93795b574c734fd796a34bf83a49953805a87fc42585f80` (MATCH
  across separate processes).
- Separate-process equality holds under the declared profiles.

### 13.5 Carried qualification — Q1-WP8-5

**The producer canonical payload does not contain every per-negative row.**
Canonical-output hash equality and independent negative-case verification are
therefore **distinct** pieces of evidence. No digest is claimed to authenticate
observations omitted from its payload; the refusal matrix is supported separately
by executable probes (and the raw logs/TRX required by §6.7).

**Result: SATISFIED WITH EXPLICIT QUALIFICATIONS.**

---

## 14. GA-11 — Platform, profiles, and publication boundaries

**Supported-environment statement (precise):**

- Independently qualified environment: **Windows x64**; .NET SDK **10.0.401**;
  runtime **10.0.12**; Roslyn **5.9.0**; **C# 14**; accepted three-framework-
  reference corpus profile. Verified from committed evidence and this HEAD.
- The **production capture profile** preserves the host's actual verified
  reference paths/bytes; the **frozen corpus fixture profile** uses three labelled
  framework items. The two are **distinct declared profiles** and are not compared
  cross-profile.

Distinctions preserved:

1. **Independently qualified Windows behavior** — the WP3–WP8 reviewer probes and
   the frozen oracle on Windows x64.
2. **Previous hosted Windows/Linux/macOS CI** — solution-suite execution only; it
   is **not** independent WP8/WP5/WP6 cross-platform qualification.
3. **Platform-specific WP8 semantic qualification** — Windows x64 only.
4. **Toolchain-bound exact identity bytes** — production-profile digests are
   host/input-bound; cross-machine byte equality is **not** claimed.
5. **Profile-specific fixture identities** — frozen anchors (`Leaf 404c5e48…`,
   `Core 942a8e58…`) apply only under the complete fixture profile.
6. **Production-profile identities** — S01 `ece8e803…`/`1027f062…`, Core
   `afcb56d0…`, recorded for this environment.

Byte equality is **not** generalized across unsupported environments. **Package
publication is not part of S2.2 acceptance.** Native persistence/reopen, reader
continuation, cross-platform release qualification, and later integrated
acceptance remain separately authorized.

**Result: SATISFIED WITH EXPLICIT QUALIFICATIONS.**

---

## 15. Accepted production capabilities

Subject to the qualifications in this record, S2.2 may claim the bounded
capabilities established by its owner and independent-review chain:

1. Verified live C# compilation association.
2. R2-faithful `compctx1` mapping.
3. Current recorded `compctx2` validation.
4. Canonical verified `asmctx3` minting (internal `VerifiedCSharpUniverse.MintTarget`).
5. Bounded trusted `usage4` extraction and emission (internal `MintUsage` + `VerifiedCSharpOccurrenceExtractor.Extract`).
6. Conservative refusal for unsupported and invalid authority.
7. Correct NEW-04 coverage merging (disjoint-contribution contract).
8. Bounded conditional-inactive scanning (F09 heuristic).
9. Stable identities under accepted transitive changes.
10. In-memory stale publication refusal.
11. Declared-profile identity comparisons.
12. Frozen corpus integration and independent expectation matching.
13. Demonstrated reproducibility within accepted environments.

All inherited limitations remain applicable.

---

## 16. Explicit non-acceptance

This decision does **not** claim:

- Complete arbitrary C# occurrence extraction.
- Every method reference produces a trusted fact.
- Universal cross-profile identity equality.
- Universal platform-independent identity bytes.
- Exact F09 counts for arbitrary disabled code.
- **Unqualified component digest provenance / general source canonicalization.**
- Native persistence/reopen safety.
- Native source-owned publication/replacement lifecycle.
- NEW-01 recovery closure.
- S2.4 reader continuation.
- S2.5 integrated provenance proof.
- Public Find Usages API.
- Full production recovery.
- Package publication or stable API release.
- Any unqualified capability excluded by an accepted owner decision.

---

## 17. S2.3 entry obligations (bounded handoff, not authorization)

S2.3 remains **unauthorized** by this decision. If/when separately commissioned,
its entry obligations, from actual accepted authority, are:

1. **NEW-01 recovery prerequisite** — the targeted foundation-recovery correction
   (`ADR-CI1-01-CI1A-S2-FOUNDATION-RECOVERY-01`) must be fixed and independently
   qualified **before** any S2.3 recovery reliance.
2. **Persist the defining `compctx2` association** in the native occurrence
   unit/meta.
3. **Native reopen-time enforcement** of the association.
4. **Stale publication refusal after reopen** — the **first S2.3 gate** (Option A
   §5.2 test 8 second half).
5. **Native publication and replacement lifecycle** through the existing
   source-unit lifecycle (same transaction as graph).
6. **Failure handling and evidence preservation** — conservative unknown-outcome
   handling, graph **and** occurrence projection asserted against durable
   authority.
7. **Compatibility with prior accepted scope/universe coverage** — no false
   complete-empty; incompatible-universe refusal preserved.
8. **Preservation of S-01 provenance** — the correspondence chain plus the
   retained component-digest qualification.
9. **Accepted storage-provider boundaries** — no migration/dual-write/legacy
   readers; superseded data refused or re-ingested; D3/D4/D5 preserved.
10. **Independent qualification and owner-acceptance gates** — separate
    implementation, independent review, and an owner decision.

**Escalation:** if Option A durability cannot be enforced on reopen, stop and
return to the owner with the bounded **Option B** alternative rather than
introducing an unauthorized workaround.

---

## 18. Remaining S2.4 / S2.5 requirements

- **S2.4** — internal bounded reader continuation/pagination and coverage
  reporting (`ICodeOccurrenceReader`, IF-04); publication-change handling;
  partial/cross-target reader surface. No public API.
- **S2.5** — independent integrated source/compilation/semantic proof
  (`source bytes → compilation → binding → occurrence identity`, including the
  **general source canonicalization of the identity component digests** and the
  integrated S-01 chain), real-project end-to-end/native reopen, applicable D3/D4,
  authentic available historical refusals, expanded Linux suite, and complete
  obligation reconciliation before owner Stage-2 acceptance.
- **Cross-platform / Stage 6** — release-stage platform evidence and full D3/D4,
  cancellation, crash/power-loss/multi-process qualification (only if separately
  scoped).
- **Publication/release** — separately controlled; nothing pushed.

---

## 19. Verification performed at this commission

Recorded at HEAD `40b5062` (Windows x64, SDK `10.0.401`, runtime `10.0.12`).
Logs in `docs/reviews/CI1A-S2.2-G-ACCEPT-OWNER-01-evidence/`.

| Verification | Expected baseline | Reproduced |
| --- | ---: | ---: |
| Whole solution (`dotnet test Andirit.slnx -c Release`) | 789 | **789 / 0 / 0** (43 + 157 + 345 + 244) |
| WP8 integration producer | 9 | **9 / 0 / 0** |
| WP8-Q1 independent probes | 10 | **10 / 0 / 0** |
| WP7 producer | 12 | **12 / 0 / 0** |
| WP7-Q1 probes | 19 | **19 / 0 / 0** |
| WP6 producer matrix | 20 | **20 / 0 / 0** |
| WP6-Q1 probes | 30 | **30 / 0 / 0** |
| WP5 producer (S2_2Wp5) | — | **20 / 0 / 0** |
| WP5-Q2 probes | 22 | **22 / 0 / 0** |
| WP5-Q1 probes | 16 | **16 / 0 / 0** |
| WP4 producer | 20 | **20 / 0 / 0** |
| WP4-Q1 probes | 13 | **13 / 0 / 0** |
| G-MINT-Q1 probes | 12 | **12 / 0 / 0** |
| WP3 producer | 16 | **16 / 0 / 0** |
| WP3-Q1 probes | 19 | **19 / 0 / 0** |
| Frozen R2 oracle | 185/185 | **185 / 185**, exit 0 |
| `dotnet format Andirit.slnx --verify-no-changes` | exit 0 | **exit 0** |
| `dotnet build Andirit.slnx -c Release` | 0/0 | **0 warnings, 0 errors** |

- Oracle output hash `97F6C5E5…` == accepted; expected hash `274A7AAE…` unchanged.
- Determinism: producer `3f08abc6…`/12 108 bytes; reviewer `00a46e4e…`/11 821 bytes
  (committed WP8-Q1 evidence, cross-checked).
- **Zero** failures and **zero** skips in every suite.
- **No unsupported production drift** (`git diff 2b35cd6..HEAD -- src` empty).
- **Frozen oracle integrity** preserved; **no producer-test weakening**; **no prior
  decision rewritten**.

---

## 20. Final owner disposition (GA-12)

**Decision: `ACCEPTED WITH EXPLICIT QUALIFICATIONS`. Gate: `S2.2 G-ACCEPT PASSED`.**

All mandatory S2.2 requirements are satisfied within the documented accepted
boundaries. Remaining limitations and downstream obligations are explicitly
preserved:

| Qualification | Effective restriction |
| --- | --- |
| S-01 component-digest canonicalization | `identifierDigest`/`declarationDigest`/`shapeDigest` are caller-supplied; general source canonicalization is not implemented/independently qualified; allocated to the S2.5 integrated proof. |
| Option A durability | S2.2 is extraction-time only; native reopen enforcement is the first S2.3 gate; Option B escalation preserved. |
| NEW-01 | Recovery defect not fixed; S2.2 non-reliance confirmed; mandatory before S2.3 recovery reliance. |
| NEW-04 / G-Q1A2 / G-Q1E1 / G-Q1A7 / G-Q1FLAG / G-Q1OVF | Disjoint-contribution contract; mixed-scope universe attribution loss; cross-universe fail-closed queries; fixture-only reachability; native reopen open. |
| F09 / F2 / F3 | `ConditionalInactive` heuristics; `Unsupported`/`UnsupportedCategories` not disjoint. |
| G2 / refusal precedence | Refusal labels are not a precedence contract; fail-closed behavior is. |
| G1 / F1 metadata equivalence | Not exact target-instance identity; R2 semantics not retroactively changed. |
| Declared profiles | No cross-profile equality; production digests host/input-bound. |
| Platform | Windows x64 only; hosted CI is not independent cross-platform qualification. |
| Q1-WP8-5 | Canonical payload omits per-negative rows; hash equality ≠ negative verification. |
| Publication | Not part of S2.2 acceptance. |

**Boundary.** S2.2 does **not** become a universal C# semantic indexer merely
because this gate passes. The boundary between accepted S2.2 behavior and
S2.3+ work is: *bounded, independently qualified, extraction-time verified
Roslyn semantic occurrence extraction with frozen-corpus integration* — **not**
native durability, reader continuation, integrated Stage-2 proof, publication, or
general source canonicalization.

### Next stage

**S2.2 is formally accepted with its recorded scope and qualifications.** **S2.3
becomes eligible for a separate entry and authorization commission** (with NEW-01
and the reopen durability gate as prerequisites). S2.4, S2.5, package publication,
and later releases remain independently controlled.

---

**Final disposition: `ADR-CI1-01-CI1A-S2.2-G-ACCEPT-OWNER-01 ACCEPTED WITH EXPLICIT
QUALIFICATIONS` — `S2.2 G-ACCEPT PASSED`.**

Recorded at HEAD `40b5062a6c04a37a1fe0641a4b5f86f2431baeb7` (tree
`4772515e743e486bf124f1edbb62b3bdbc01b3b7`). This record and its evidence are the
only changes in their commit(s); the decision and evidence commit/tree identities
are recorded in the handoff.
