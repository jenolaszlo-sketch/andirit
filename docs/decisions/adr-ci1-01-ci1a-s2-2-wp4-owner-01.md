# ADR-CI1-01-CI1A-S2.2-WP4-OWNER-01 — Qualified owner acceptance of WP4 `asmctx3` minting

**Commission:** `ANDIRIT-CI1A-S2.2-WP4-OWNER-01` (owner acceptance decision).
**Project:** Andirit. **Recorded:** 2026-11-05. **Canonical branch:** `main`.
**Owner decision:** **`ACCEPTED WITH EXPLICIT QUALIFICATIONS`**.
**Gate/scope:** **WP4 closed for its bounded `asmctx3` minting scope.**

> **Status: RECORDED — OWNER-AUTHORIZED.**
> **Owner decision: `ACCEPTED WITH EXPLICIT QUALIFICATIONS`.**
>
> This accepts the WP4 production `asmctx3` minting implementation for its bounded
> scope only. It does **not** implement or accept WP5 `usage4`, extraction, coverage,
> F09, WP6 NEW-04, persistence/reopen or cross-platform qualification, and does
> **not** authorize publication/release or complete S2.2. WP5 is eligible for a
> **separately commissioned** implementation.

This decision is additive documentation. It does not amend any prior accepted
decision, reviewer artifact, producer test, frozen oracle or production source, and
it does not modify the G-MINT or WP3 acceptance records.

---

## 1. Owner decision, gate, and scope fields

| Field | Value |
| --- | --- |
| Owner decision | `ACCEPTED WITH EXPLICIT QUALIFICATIONS` |
| Gate/scope | WP4 bounded `asmctx3` minting (closed) |
| Accepted capability | Internal production `asmctx3` minting through `VerifiedCSharpUniverse.MintTarget`, with mandatory recorded-`compctx2` enforcement and fail-closed refusal |
| Excluded | WP5 `usage4`/extraction/coverage/F09; WP6 NEW-04; persistence/reopen; cross-platform; publication; complete S2.2 |
| Next stage | WP5 — requires a separate commission |

---

## 2. Verified authority chain

| Role | Commit | Tree |
| --- | --- | --- |
| R2 foundation | `e0d911055f4cc9bdbf9aa4cc338f5cc007737a22` | `ba9338d74bf59c584bef340dbda49ed46eb1c5d9` |
| G-CONFIRM owner acceptance | `812584dc56e11983c3ea7de8977fd1cd74ff0297` | `7167557024e1602c81b5e6b169455c0ad10a5864` |
| WP3 implementation | `ff58a9290a7583d4493d177e5de5aa6315904a85` | `85cdcedeb261c656893cdd7c2e3f21b741a25dc6` |
| WP3 independent qualification | `2cdcd10e4b225debe0d081280d8a18a6304d0475` | `a3d6dcc92b08c8080f2d2402a9d3a714b01b7409` |
| WP3 owner acceptance | `4fdc23cfd3d760639dd653ad92bb204376a08c05` | `55b5e990cfa16606d7ba09752d18d360bf01db08` |
| G-MINT independent qualification | `1c382d6aaa1c2ceb04b70e65e048357621a73a78` | `c2f7c9ef782e90e70df9c63dfcd62e8cb888f7b9` |
| G-MINT owner acceptance | `d3da2e009154402a92275874ac4bb94426b97df3` | `029299bf277bc99212ea2ded311c0ec54f70fa09` |
| WP4 implementation | `eac302295b5bbaf8d71181ddcc70a62089a9a28e` | `9a4ccefc182d5b80123def1dce313350c37e23d6` |
| WP4 implementation evidence | `e05ce93830f9cf2a0ac28e51a9f6980805f58a73` | `7531293c49e5e01586b9d4c3af4aafdf8c45fae6` |
| WP4 independent qualification | `6ecf9962216b3c7a3c7330da856383c5c5b0dde0` | `c0ce3df6dafe5383cb12c683597c71728ded35e7` |

Verified at recording: a consecutive parent chain
`e0d9110 → … → eac3022 → e05ce93 → 6ecf996`; every tree reproduced exactly;
`6ecf996^ = e05ce93`; working tree clean; one worktree; no stashes; `origin/main` =
`e0d9110`; `main` **14 ahead, 0 behind**; nothing pushed. **No equivalent or
superseding WP4 owner decision exists** (`docs/decisions/` contains no `wp4` record);
this is not a duplicate.

---

## 3. Implementation identity

WP4 implementation commit `eac3022`, three files, purely additive (614 insertions,
no removed production lines, **no `public` API additions**):

| File | Change |
| --- | --- |
| `src/Andirit.CSharp/VerifiedTargetMint.cs` | new: `VerifiedTargetMintStatus`, `VerifiedTargetMint` (internal) |
| `src/Andirit.CSharp/VerifiedCSharpAnalysis.cs` | +`VerifiedCSharpUniverse.MintTarget`, +`MapMintStatus` (additive) |
| `tests/Andirit.CSharp.Tests/S2_2Wp4MintTests.cs` | new: 20 tests |

The accepted WP3 `ResolveDefining`/`BindsDefining` bodies and the frozen
`CodeOccurrenceIdentity.ComputeTargetId` are unchanged. No `usage4`, fact emission,
extraction, coverage, scanner, NEW-04/`Merge`, persistence/reopen or public API was
added. The single authority-minting path is `VerifiedCSharpUniverse.MintTarget`.

---

## 4. Independent qualification identity

WP4 independent qualification `ANDIRIT-CI1A-S2.2-WP4-Q1` (commit `6ecf996`, tree
`c0ce3df…`) returned **`SUPPORTED WITH QUALIFICATIONS`**; no BLOCKING defect, no
DECISION REQUIRED, no unauthorized minting path. The review commit is documentation,
reviewer probes and evidence only; `git diff eac3022..6ecf996 -- src tests` is
**empty** (production unchanged during qualification).

Central finding: **no** unsupported, stale, fabricated, substituted, incomplete,
ambiguous or otherwise unauthorized association caused production code to mint an
`asmctx3` artifact; every refusal returned no artifact; valid associations reproduced
the exact accepted frozen tuple.

---

## 5. Evidence integrity

Independently confirmed against the committed qualification evidence:

| Verification | Result |
| --- | --- |
| WP4 independent probes | 13 / 0 / 0 |
| WP4 producer tests | 20 / 0 / 0 |
| WP3 targeted tests | 16 / 0 / 0 |
| WP3 independent probes | 19 / 0 / 0 |
| G-MINT independent probes | 12 / 0 / 0 |
| `Andirit.CSharp.Tests` | 203 / 0 / 0 |
| Whole solution | 728 / 0 / 0 |
| Frozen R2 oracle | 185 / 185, exit 0 |
| Frozen expected hash | `274A7AAE…` unchanged |
| Oracle output hash | `97F6C5E5…` unchanged |
| Separate-process mint determinism | confirmed (identical `asmctx3`/`compctx1` and independent expected across two processes) |

Reviewed evidence: `docs/reviews/ANDIRIT-CI1A-S2.2-WP4-Q1.md`,
`docs/reviews/CI1A-S2.2-WP4-Q1-tests/`,
`docs/reviews/CI1A-S2.2-WP4-Q1-evidence/`. Production source, producer tests, frozen
oracle and prior accepted decisions are unchanged.

---

## 6. W1–W6 dispositions

| ID | Qualification | Owner disposition |
| --- | --- | --- |
| **W1** | `VerifiedTargetMint` is internally constructible; no unauthorized production construction path was demonstrated. | **Accepted as a bounded qualification.** Type-level construction is not claimed impossible. The distinction between an internally constructible result record and an authorized minting operation is preserved. A later WP5 consumer that treats the result record as trusted authority must itself establish the accepted provenance boundary; no speculative refactor is required absent a demonstrated bypass. |
| **W2** | `CodeOccurrenceIdentity.ComputeTargetId` is a public pure structural constructor; it is not an authority-minting operation. | **Accepted with an explicit trust-boundary qualification.** A structurally valid identifier does **not** independently prove that the underlying compilation association was verified. WP5 must not treat arbitrary structural identifiers as minted authority. The accepted distinction between identity computation and verified provenance is preserved. |
| **W3** | Inherited frozen R2 cross-project metadata-equivalent binding; WP4 uses the defining member's context and does not upgrade metadata equivalence into exact target-instance identity. | **Preserve the accepted G1/F1 qualification.** R2 semantics are not retroactively changed. The distinction between metadata-equivalent targets and exact defining-compilation instance authority is carried into WP5 and all authority-consuming code. Any stronger identity contract requires a separate owner decision. |
| **W4** | S-01: `identifierDigest`/`declarationDigest`/`shapeDigest` are caller-supplied and not independently verified by WP4. | **Accepted only within that explicit scope.** No claim is made that WP4 verifies the provenance or correctness of caller-supplied component digests. WP5/later extraction must preserve the trust assumptions and verification obligations of the authoritative contracts; no additional verification mechanism is invented without authorization. |
| **W5** | Option A §5.2 test 3's `usage4` assertion remains WP5-dependent; test 6's cross-profile comparison remains a separate G-MINT comparison obligation. | **Accept demonstrated WP4 coverage; record the precise remaining obligations.** Complete Option A §5.2 runtime qualification is **not** claimed. |
| **W6** | Windows-only execution; separate-process determinism reproduced on that host. | **Accepted with a Windows-only execution qualification.** Linux/macOS correctness is not inferred; cross-platform validation remains a later obligation under the project's accepted release/qualification requirements. |

---

## 7. Accepted WP4 capabilities

For the bounded WP4 scope only:

1. Internal production `asmctx3` minting through `VerifiedCSharpUniverse.MintTarget`.
2. Mandatory non-null recorded `compctx2` enforcement.
3. Consumption of the accepted WP3 mapper/resolver.
4. Refusal of every non-`Complete` resolver outcome.
5. Prevention of minting when mandatory WP4 preconditions fail (no artifact on refusal).
6. Accepted identity and cross-project association semantics (G1/F1 preserved).
7. Frozen `ComputeTargetId` formula reproduction (independently verified).
8. Correct defining-compilation `compctx1` construction and `compctx2` isolation.
9. WP4-applicable Option A §5.2 runtime behavior.
10. Demonstrated separate-process determinism.
11. Compatibility with existing accepted behavior (additive; WP3 unchanged).
12. Authority-grounded tests and independent qualification evidence.

**Acceptance is limited to these demonstrated capabilities.**

---

## 8. Explicit exclusions (non-acceptance)

This decision does **not** claim: WP5 `usage4` implementation; fact extraction or
emission; coverage/refusal emission; F09 scanner integration; WP6 NEW-04 resolution;
production persistence or native reopen; cross-platform qualification; independent
verification of S-01 component digests; that arbitrary structural identifiers carry
verified authority; that metadata equivalence establishes exact target-instance
identity; fully specified refusal-status precedence; complete Option A §5.2 acceptance;
complete S2.2 acceptance; or package publication/release.

G-MINT and WP3 acceptance remain unchanged.

---

## 9. WP5 entry contract

A separately commissioned WP5 must satisfy the following. Each row identifies the
source contract, existing evidence, remaining implementation work, and the acceptance
condition. Nothing here is a new requirement unsupported by accepted authority.

| ID | Requirement | Source contract | Existing evidence | Remaining work | Acceptance condition |
| --- | --- | --- | --- | --- | --- |
| **W5-1** | Minted authority consumption: distinguish verified WP4 mint output from arbitrary structural identifiers. | G-MINT owner §7 GM-3; mapping contract §3; WP4-Q1 W1/W2 | `MintTarget` returns `TargetId` only on `Minted`; single authority path inventory | WP5 emission must consume only minted output and must not treat arbitrary `CodeOccurrenceTargetId` values as authority | Negative test: a structurally valid but unverified identifier does not produce a trusted `usage4` |
| **W5-2** | Recorded-context continuity: preserve mandatory recorded-`compctx2` validation; no alternate `usage4` path bypasses the WP4 boundary. | Option A §5.1(2)/(5); mapping §3.3; WP4-Q1 §5 | WP4 requires non-null recorded `compctx2` | Route emission through the accepted boundary (or an equally gated one) | Demonstration that no emission path can build `usage4` from an unminted/unvalidated association |
| **W5-3** | Identity semantics: preserve the G1/W3 metadata-vs-instance distinction. | G1/F1; WP4-Q1 W3 | WP4 uses the defining member's context | Carry the distinction into occurrence/coverage code | Negative tests for foreign identity-equal and substituted instances at the occurrence boundary |
| **W5-4** | Component provenance: retain S-01; do not claim caller-supplied digests are verified. | S-01; WP4-Q1 W4 | Caller-supplied component digests | WP5 extraction must supply the canonical site forms; no invented verification | Documented; no claim of independent digest verification |
| **W5-5** | Fail-closed: refusal or incomplete authority must not produce a trusted `usage4`. | G2; mapping §3; stage2-boundary §5 | WP4 refuses every non-`Complete` outcome | Enforce at emission/coverage | Refusal matrix with no trusted artifact on refusal |
| **W5-6** | Option A §5.2: implement and independently qualify remaining WP5-applicable runtime assertions (esp. test 3's `usage4`). | Option A §5.2; mapping §7 | WP4 forms verified; test 3 `usage4` open; test 6 cross-profile separate | WP5 `usage4` runtime forms | Independent WP5 qualification of test 3's `usage4`; cross-profile preserved at its allocated gate |
| **W5-7** | Emission and extraction: implement only authorized `usage4`/extraction/coverage/F09 behavior. | stage2-boundary §3.6–3.9; scope §6.2 WP5; acceptance matrix | None (WP4 mints targets only) | Extraction from the same live compilation; `usage4`; coverage/refusal emission; F09 scanner | WP5 implementation + independent qualification + owner acceptance |
| **W5-8** | NEW-04: maintain the WP6 deferral; do not treat existing `Merge` as accepted coverage authority. | G-CONFIRM owner §8; register §4 | `Merge` unchanged (`fc515ff`) and unused by WP3/WP4/G-MINT | None at WP5 | No coverage reliance before WP6 correction/falsification |
| **W5-9** | Platform: preserve Windows-only limitations; identify future platform evidence needs. | prior platform qualifications | Windows x64 | Cross-platform evidence at the accepted release stage | Platform evidence per project requirements; nothing inferred |
| **W5-10** | Independent qualification: WP5 completion requires separate implementation, independent qualification, and owner acceptance. | implementation-commission-draft §6; accepted gate ordering | Each prior stage independently qualified | Separate WP5 commission | WP5-Q1 independent qualification and a WP5 owner decision |

---

## 10. Preserved Q2 / G-MINT / S-01 qualifications

All previously accepted limitations remain in force and are **not** closed by this
decision: the Q2 foundation qualifications; C1 (bounded Q-F3 surface), C2 (declared
comparison profiles/actual PE identities), C3 (`closure authority` terminology), C4
(deferred implementation verification); G1 (F1 metadata binding), G2 (fail-closed, no
precedence contract), G3 (recorded `compctx2`), G4 (Option A §5.2 allocation); G-MINT
GM-1…GM-7; the S-01 caller-supplied component-digest limitation; and the NEW-04 WP6
deferral.

---

## 11. Platform limitations

- WP4 implementation and qualification executed on **Windows x64** (`net10.0`) only;
  Linux/macOS correctness is not inferred.
- Separate-process mint determinism was demonstrated on that host.
- S-01 component digests remain caller-supplied and unverified by WP4.
- Native persistence/reopen (S2.3) and cross-platform release qualification remain
  later, separately controlled obligations.

---

## 12. Next-stage authorization boundary

| Item | Status |
| --- | --- |
| WP4 bounded `asmctx3` minting | **ACCEPTED WITH EXPLICIT QUALIFICATIONS** (this decision) |
| WP4-Q1 independent qualification | Completed |
| WP5 | **ELIGIBLE for a separate commission**; **NOT AUTHORIZED** by this decision |
| WP6 (NEW-04) | **NOT AUTHORIZED** (separate commission; deferral preserved) |
| WP7–WP9 | **NOT AUTHORIZED** |
| S2.3+ (native persistence/reopen) | **NOT AUTHORIZED** |
| Cross-platform qualification | Not established |
| Publication / package release / push | **NOT AUTHORIZED** |

**Preserved ordering:** `G-MINT (PASSED) → WP4 implementation → WP4-Q1 → WP4-OWNER → WP5 → …`.

---

## 13. Final owner decision

**`ACCEPTED WITH EXPLICIT QUALIFICATIONS`.** WP4 is formally closed for its bounded
`asmctx3` minting scope. WP5 is eligible for a separately commissioned implementation;
it does not start automatically. WP6, subsequent work packages, S2.3+, cross-platform
qualification and publication remain separately controlled. G-MINT and WP3 acceptance
are unchanged, and complete S2.2 acceptance is **not** claimed.

**Final disposition:** **`ADR-CI1-01-CI1A-S2.2-WP4-OWNER-01 ACCEPTED WITH EXPLICIT
QUALIFICATIONS`** — WP4 closed; WP5 eligible for separate commission.
