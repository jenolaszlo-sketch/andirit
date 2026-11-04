# ADR-CI1-01-CI1A-S2.2-FOUNDATION-OWNER-01 — Qualified owner acceptance of the WP1–WP3 foundation

**Commission:** `ANDIRIT-CI1A-S2.2-FOUNDATION-OWNER-01`.
**Recording commission:** `ANDIRIT-CI1A-S2.2-FOUNDATION-OWNER-02` (owner-authorized).
**Project:** Andirit. **Recorded:** 2026-11-04.
**Technical candidate under decision:** `d5a785f898ce7877caef10be53074f2c2ae3a1cd`
(tree `5d8aa21d515dcd2d423aa0c7eb8f7e3996eb650c`), canonical `main`.
**Preparation commit:** `526d2c069d5ba93a016cb94b50b1a831442d7071`
(tree `d581f25b85e04239ac043a4c688bb39fee356f76`).
**Independent Q2 verdict under decision:** `SUPPORTED WITH QUALIFICATIONS`.
**Owner decision:** **`ACCEPTED WITH QUALIFICATIONS`.**

> **Status: RECORDED — OWNER-AUTHORIZED.**
> **Owner decision: `ACCEPTED WITH QUALIFICATIONS`.**
>
> The owner authorized recording this acceptance
> (`ANDIRIT-CI1A-S2.2-FOUNDATION-OWNER-02`). This accepts the WP1–WP3 foundation
> **with qualifications only**. It does **not** accept complete S2.2, does **not**
> accept `G-CONFIRM`/`G-MINT`, and authorizes **no** WP4–WP9, S2.3 or downstream
> implementation. The same authorization separately permits committing this
> decision and a fast-forward push of canonical `main`; it authorizes no other
> gate.

This is the recorded owner decision for `ANDIRIT-CI1A-S2.2-FOUNDATION-OWNER-01`,
formally accepted under `ANDIRIT-CI1A-S2.2-FOUNDATION-OWNER-02`. It is additive
documentation: it does not amend any accepted decision and does not close any
gate. Supporting deliverables are in
[`docs/reviews/CI1A-S2.2-FOUNDATION-OWNER-01/`](../reviews/CI1A-S2.2-FOUNDATION-OWNER-01/README.md).

---

## 1. Purpose

Prepare the formal owner acceptance of the S2.2 **WP1–WP3 foundation** (Q-F2
encoding, Q-F3 metadata validation, A-01 symbol hardening plus the corrected
Option A mapping contract) against the independent Q2 requalification, and name
the exact supported contracts, qualifications and excluded guarantees, the two
remaining pre-minting gates (`G-CONFIRM`, `G-MINT`) and the authorization gates
that stay closed.

The preparation commission prepared the decision; the owner has now **authorized
recording** the qualified acceptance and a fast-forward push of canonical `main`
(`ANDIRIT-CI1A-S2.2-FOUNDATION-OWNER-02`). Recording the acceptance does not
authorize `G-CONFIRM`/`G-MINT`, WP4–WP9, S2.3, publication or releases.

---

## 2. Verified candidate and evidence

Independently re-verified in this preparation (full receipts in the
[evidence verification record](../reviews/CI1A-S2.2-FOUNDATION-OWNER-01/evidence-verification-record.md)):

| Role | Commit | Tree |
| --- | --- | --- |
| Authoritative Q1 qualification | `38d4e513951c07578953fda7738d8741d2109004` | `ffca6b84dd8c2c1d9eebcbcd0544d5615dcb7e2a` |
| R1 implementation | `d5644d814b83170f553584529c05f5f8c289ba25` | `bcbf65117c9a1f892b987e4e83ed25eab99ee705` |
| R1 evidence | `b4eff01bb736f1725543fc8ba928ff313d65c4d6` | `ca448ef7da813a45130ed3a138850a27457de735` |
| Independent Q2 / candidate HEAD | `d5a785f898ce7877caef10be53074f2c2ae3a1cd` | `5d8aa21d515dcd2d423aa0c7eb8f7e3996eb650c` |
| Accepted S2.2 entry decision | `febc28d92548fd7323294077c37444048741a8c8` | `e1fa31ec23ef213629dd613e92073641bf2e6861` |
| Accepted F12 decision | `a1cf47083747bae90293ec074b35fe8078bd0d6e` | `ebba0daf2977315a5c09551df0de698afeb6696f` |
| Qualified frozen R2 | `7f8d035d7fa6bd5da49f71ee86ff54d7350318c5` | `29150eb6ef2b0fe65942f3c8930037f8c0120174` |
| S2.1 owner baseline | `b7fea0d219409b279b78317e32bdc3c8b5ae61ee` | `b4a764637498ed8e8bddac28fb0b0b3716b7ffee` |
| S2.1 reviewed production source | `c02d5eae157653ba7f123b98aceccb01a0b542a0` | `a53a076dbb482062973f70fd11bf4261cc5368f3` |

Verified facts:

- `38d4e51 → d5644d8 → b4eff01 → d5a785f` is a consecutive parent chain; every
  tree above reproduced exactly.
- R1 implementation `d5644d8` changes exactly three paths: adds
  `src/Andirit.CSharp/MetadataReferenceValidation.cs`, modifies
  `src/Andirit.CSharp/VerifiedCSharpCompilation.cs`, adds
  `tests/Andirit.CSharp.Tests/S2_2MetadataRemediationTests.cs`. No other
  production/test path changes.
- R1 evidence `b4eff01` and Q2 `d5a785f` have **no** production or test delta
  (`git diff --name-only … -- src tests` empty). The qualified production tree
  is bounded to WP1–WP3.
- Working tree clean, one canonical worktree, no stashes, no branch switch, no
  worktree creation. No push or remote mutation was performed except a read-only
  `git fetch` that changed no ref.
- Evidence preservation independently re-confirmed: frozen R2 manifest **100
  files** + Git branch/pinned ancestry; R1 evidence manifest **50 files**; Q2
  evidence manifest **93 + 3** files; frozen oracle replay **185/185 checks**,
  output SHA-256 `97f6c5e5a4cc77e089481c7643732b7a07a31d483c2643bd7a95fdad7ced39c9`.

Reconciled Q2 test accounting (historical observation failures are **not**
counted as passing):

| Run | Result | Meaning |
| --- | --- | --- |
| Full unfiltered solution | **692 passed / 0 failed / 0 skipped** | Reproduces producer baseline; includes the 40 remediation and 7 foundation and 9 identity subsets |
| Producer metadata remediation subset | **40 / 0 / 0** | Included in the 692, not additive |
| Producer WP1/WP2/A-01 foundation subset | **7 / 0 / 0** | Included in the 692 |
| Existing frozen identity subset | **9 / 0 / 0** | Included in the 692 |
| Independent Q2 reviewer suite | **40 / 0 / 0** (twice; plus exact-input recording run) | Separate process; independent adversarial metadata/profile/authority checks |
| Historical Q1 reviewer probes | **45 passed / 3 failed / 0 skipped** | The three failures are **expected observation mismatches** invalidated by the remediation; **not** passes |

Independent Q2 total includes one intermediate **37/1** (reviewer path-fixture
error, transparently retained) and one **39/0** intermediate; neither is counted
as the completed suite.

---

## 3. Finding closure

The Q2 report closes all five Q1 findings at the bounded correction/specification
scope, with **no** new demonstrated production defect and **no** remaining OPEN
or INSUFFICIENT-EVIDENCE finding within that scope:

| Q1 finding | Q2 disposition | Retained boundary |
| --- | --- | --- |
| Q1-F01 — malformed metadata identity capture | **CLOSED** (demonstrated name/signature defects and full-PE overclaim) | Closes the demonstrated defects; does **not** establish complete metadata usability or full PE/IL validity. |
| Q1-F02 — inconsistent reference comparison profile | **CLOSED** | Frozen fixture equality is conditional on all accepted inputs, runtime bytes and labels. No production mapper. |
| Q1-F03 — shape conflated with authority | **CLOSED** | Shape alone never licenses a fact; live authority refusal is unimplemented. |
| Q1-F04 — instance identity conflated with complete inputs | **CLOSED** | Equal semantic identity never permits replacing the live binding instance. |
| Q1-F05 — pre-mint versus reopen gate inconsistency | **CLOSED** | Native persistence/reopen is the separately authorized first S2.3 gate; carrier unresolved/unimplemented. |

Closure is a reviewer technical disposition within the stated bounded scope; the
owner records it here as accepted **within that scope only**.

---

## 4. Supported WP1–WP3 contracts (owner-accepted)

The owner **accepts** the following bounded, evidence-backed foundation contracts,
and nothing broader. The complete statement, with exclusions, is in
[qualifications and excluded guarantees](../reviews/CI1A-S2.2-FOUNDATION-OWNER-01/qualifications-and-excluded-guarantees.md).

| Area | Supported contract (bounded) |
| --- | --- |
| **WP1 / Q-F2** | Strict UTF-8 (with or without BOM); strict UTF-16 LE/BE and UTF-32 LE/BE **with** BOM. A BOM-less decode is strict UTF-8; if the decoded text contains U+0000 it is refused as `AmbiguousBomlessEncoding` with **no** verified identity. Raw byte SHA-256 remains honest. |
| **WP2 / Q-F3** | Private `VerifiedMetadataReference.Capture` is the sole identity constructor; immutable acquisition + SHA-256 + Roslyn assembly materialization + manifest guard (`IsAssembly`) + bounded structural metadata validation before any identity is returned. The three original malformed images refuse deterministically. |
| **WP3 / A-01** | Preprocessor symbols containing `,` or `|` are refused (`UnsupportedParseConfiguration`); ordinary symbol ordering is deterministic; ordinal Unicode distinctness preserved. Internal surface only. |
| **WP3 / mapping** | Corrected Option A mapping contract: two declared comparison profiles (production actual-path profile vs frozen fixture profile) with explicit expected results; `compctx2` isolation from `asmctx3`/`usage4`; frozen `usage4`/tuple authority independently reproduced. |
| **Option A association (specification)** | Pre-mint invariants **specified** (live defining instance, complete verified universe, current defining `compctx2`, separate per-fact association, absent/stale/substituted/fabricated/ambiguous refusal). |
| **Durability boundary** | S2.2 scope is extraction-time in-memory enforcement plus an association **requirement**; native persistence/reopen enforcement is deferred to S2.3. |

---

## 5. Qualifications and gate status carried by this acceptance

This acceptance does **not** raise the achieved guarantee to complete metadata
validity, production mapping/minting, association enforcement or durability. The
following remain unqualified and carried:

- Bounded Q-F3 surface rather than universal usable metadata; documented excluded
  surfaces (malformed Field signature, invalid TypeRef ResolutionScope, malformed
  modifier TypeSpec syntax) still capture and no broader capability is claimed.
- No complete PE/IL correctness guarantee; no semantic compilation usability or
  binding guarantee.
- No production R2-faithful `compctx1` mapper/resolver, no per-fact association
  enforcement, no target/occurrence minting.
- No association minting, persistence or reopen behavior implemented.
- Carried accepted limitations: S2.1 packaging limitation; Q-F4/Q-F5; A-02;
  S-01; F09; F10/IF-04; F11; F16/NEW-02; NEW-01; NEW-04; SQ-01…SQ-07; D3/D4/D5;
  I-05/MD-03; bounded F12/toolchain reference.

Gate status at the candidate:

| Gate | Status | Note |
| --- | --- | --- |
| `G-CONFIRM` | **OPEN** | Independent confirmation of the 14 corrected F16/NEW-02 rows still required before dependent test authoring. |
| `G-MINT` | **OPEN** | Production mapper/resolver and pre-WP5 falsification still required before any minting. |
| `G-DUR` | Resolved option **(a)** | Extraction-time enforcement; first S2.3 reopen gate. |
| `G-ACCEPT` | **Not reached** | Requires Q-F2/Q-F3/NEW-04/S-01/mapping satisfaction. |

---

## 6. Effect of this recorded decision

As recorded (`ACCEPTED WITH QUALIFICATIONS`), this decision:

- records bounded **technical** closure of Q1-F01–F05 and accepts the WP1–WP3
  foundation contracts of §4 **with** the qualifications of §5;
- leaves `G-CONFIRM` and `G-MINT` **open**, `G-DUR` resolved as (a), and
  `G-ACCEPT` unreached;
- authorizes **no** WP4–WP9 implementation, no target/occurrence minting, no
  extraction, no S2.3/S2.4/S2.5/Stage 6 work, no public API and no
  publication/release.

Foundation acceptance (item 1 below) is recorded; the fast-forward push (item 5)
is separately authorized by `ANDIRIT-CI1A-S2.2-FOUNDATION-OWNER-02`. The owner
separately decides the remaining gates: (2) an independent `G-CONFIRM`
commission; (3) `G-MINT` implementation + independent review; (4) WP4–WP9
authorization; (6) S2.3 and later.

---

## 7. Recording rules and boundaries

- The owner **explicitly authorized** recording this acceptance
  (`ANDIRIT-CI1A-S2.2-FOUNDATION-OWNER-02`); the status is now
  **`ACCEPTED WITH QUALIFICATIONS`**.
- This record adds documentation only: this decision plus the supporting package
  under `docs/reviews/CI1A-S2.2-FOUNDATION-OWNER-01/`. No production code, test,
  frozen manifest, oracle, accepted decision or historical report is modified.
- Frozen R2/Q3 evidence, historical failed reviews, manifests, oracles and prior
  qualifications remain preserved. Historical observation failures stay classified
  as failures.
- The separate `ANDIRIT-CI1A-S2.2-FOUNDATION-OWNER-02` authorization permits a
  **fast-forward** push of canonical `main` only. No force-push, branch deletion,
  history rewrite, remote pruning or publication is authorized. The
  [git audit](../reviews/CI1A-S2.2-FOUNDATION-OWNER-01/git-branch-audit-and-push-readiness.md)
  findings stand and branch cleanup remains out of scope.

---

## 8. Next eligible decisions

1. **Owner foundation decision** (this record): **`ACCEPTED WITH QUALIFICATIONS`**
   — recorded.
2. **Independent `G-CONFIRM`** commission
   ([draft](../reviews/CI1A-S2.2-FOUNDATION-OWNER-01/g-confirm-commission-draft.md)).
3. **`G-MINT`** implementation and independent review
   ([readiness and draft](../reviews/CI1A-S2.2-FOUNDATION-OWNER-01/g-mint-readiness-and-commission-draft.md)).
4. WP4–WP9 authorization, then S2.3 (NEW-01 closed) and later — each a separate
   owner decision.

**Final disposition:** **`ADR-CI1-01-CI1A-S2.2-FOUNDATION-OWNER-01 ACCEPTED WITH
QUALIFICATIONS`** — the WP1–WP3 foundation is accepted with every documented
qualification and exclusion; `G-CONFIRM` and `G-MINT` remain **OPEN**; WP4–WP9
remain **NOT AUTHORIZED**. This is **not** acceptance of complete S2.2.

---

## 9. Recording authorization

- `ANDIRIT-CI1A-S2.2-FOUNDATION-OWNER-02` (owner-authorized) recorded this
  acceptance and committed only this decision-record update.
- The same authorization permits a **fast-forward** push of canonical `main` to
  `origin`. It does **not** cover `G-CONFIRM`/`G-MINT`, WP4–WP9, S2.3 or later,
  publication/releases, or branch deletion/consolidation.
- Remaining prerequisite gates and the separate non-blocking EC3 heritage
  documentation follow-up are tracked outside this decision.
