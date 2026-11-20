# ANDIRIT-CI1A-S2.5-WP-S25-4-M54-B-QUAL-01

**Date**: 2026-11-21
**Class**: PREPARED COMMISSION — commission definition only. **Nothing is executed by this document.**
**Subject**: independent qualification of M5.4 under the amended contract
(`adr-ci1-01-ci1a-s2-5-m54-amend-owner-01.md` §6): the conservative-refusal acceptance basis.
**Status**: **PREPARED — EXECUTION AUTHORIZATION REQUIRED** (owner issues the execution
commission; preparation was authorized by the M5.4 amendment commission §4).

---

## 1. Objective

Independently qualify M5.4 under the amended acceptance basis, distinguishing established facts
from constructed controls, and either (a) support M5.4 as `SATISFIED UNDER AMENDED CONTRACT` with
qualifications, or (b) report failure with escalation to Option A. The commission executes on
Windows x64 only; no Linux re-execution is required or in scope.

## 2. Pinned implementation revision and tree

| Item | Value |
| --- | --- |
| Revision / tree | `eafd632b...` / `823e3c65…` (the sealed gate-remediation revision; production refusal code unchanged since QUAL-02/03/04's audits — verified by `git diff`/`git log` on the refusal call sites at commission time) |
| Preflight assertions | clean `main`; the pinned revision is an ancestor; `src`/`tests`/`.github` identical; prior evidence manifests verify |
| Method precedent | QUAL-04 reviewer-harness pattern: a separate reviewer project using the reserved `InternalsVisibleTo` assembly identity, referencing production projects **without modifying them**; expectations derived from the amended contract, not from execution output |

## 3. Binding amended acceptance criteria (from the amendment §6, verbatim anchors)

1. **Isolated refusal behavior (C1)**: the `MissingDefiningCompilation` refusal
   (`VerifiedDefiningStatus.MissingDefiningCompilation`, its `VerifiedTargetMintStatus` mapping,
   and the `InvalidDataException("MissingDefiningCompilation")` paths in
   `VerifiedCSharpAnalysis`/`VerifiedCSharpCompilation`) is demonstrated on a constructed
   missing-defining case — **labeled as a constructed control; it demonstrates the refusal path,
   not input reachability**.
2. **Pre-mint defining-evidence validation (C2)**: the mint gate (`resolved.Status != Complete`
   → refuse) and the exact source-reference / captured PE-reference / full verified
   dependency-closure checks are exercised at their call sites; no fact is minted when defining
   evidence is missing (positive control: the `Complete` path still mints).
3. **Integrated accounting guarantees (C3)**: the unresolved classification carries through
   publication, native persistence, query and distinct-process reopen with correct counters and
   coverage states — incomplete, unavailable and complete-zero distinguished; no
   false-available, false-complete, wrong-reason, or unsupported-mint outcome anywhere.
4. **Prohibited-outcome screen (C4)**: the amended contract's prohibited outcomes never occur in
   any executed case, positive or negative.

## 4. Independent exercises

| # | Exercise | Constructed or natural | Expected outcome |
| --- | --- | --- | --- |
| E1 | Isolated refusal: call the defining-resolution entry with a defining compilation absent from the declared universe (reviewer-constructed universe) | **Constructed control (labeled)** | `MissingDefiningCompilation` status; zero minted facts; explicit reason string |
| E2 | Mint-gate screen: for every non-`Complete` defining status, the mint path refuses | Constructed (status-tabulation over the `VerifiedDefiningStatus` set) | No mint without `Complete`; status-to-mint mapping exactly as production defines it |
| E3 | Integrated accounting: publish the E1 control through the store, reopen in a distinct process, query | Natural store behavior of a constructed control (labeled) | Unresolved classification observable at query; counters/coverage states correct after reopen; no successful empty |
| E4 | Positive control: a complete admissible case mints and survives reopen | Natural | `Complete` → minted fact with correct identity (frozen-oracle identity where applicable) |
| E5 | Negative/adversarial: absent universe entry, empty universe, stale context, external-only defining evidence, ambiguous bindings | Constructed + natural variants | Correct refusal per status; never a false-positive outcome; never a successful empty |

## 5. Positive, negative and adversarial controls (pre-registered)

- **Positive (P)**: P1 complete mint + reopen; P2 the four R3 probe shapes replayed as
  expectation-registered comparisons (CS0012 gate-unreached; direct `Complete`; retargeted
  `Complete`; forwarding → `UnsupportedExternalEvidence`) — the R3 outcomes are re-derived, and
  any reviewer/producer disagreement is blocking.
- **Negative (N)**: N1 isolated missing-defining refusal (E1); N2 each non-`Complete` status
  refuses the mint (E2); N3 attempted mint with a stripped reference set.
- **Adversarial (A)**: A1 a universe entry that *names* but does not *contain* the defining
  compilation; A2 a tampered/extracted reference that bypasses the closure check; A3 a
  synthesized-symbol/error-recovery shape (the specific residual risk of the amendment §8);
  A4 a cross-unit/authority case where the unresolved classification could be masked as complete.
  Any A-case producing a **prohibited-direction outcome** (mint, false-available, false-complete,
  wrong reason) is a **blocking finding and an escalation trigger** (§7).

## 6. Required evidence and expected outcomes

| Requirement | Content |
| --- | --- |
| Environment record | Windows x64, SDK/runtime versions, host identity, package cache identity (toolchain log) |
| Harness | The reviewer project, its `expected.json` (contract-derived expectations), and the harness source — all inside the sealed package |
| Raw receipts | Per-exercise stdout/stderr logs, status/counter receipts, publication/reopen receipts, query receipts; failed attempts preserved verbatim |
| Coverage of the refusal path | Instrumented or traced evidence that the refusal branches under test were executed (no production visibility change; tracing allowed) |
| Manifest | Self-excluding SHA-256 manifest of exact bytes, lengths, ordinal relative paths (QUAL-04 convention); report bound by Git |
| Disposition rule | `SUPPORTED` / `SUPPORTED WITH QUALIFICATIONS` / `REMEDIATION REQUIRED` / `BLOCKED`; any reviewer/producer disagreement on a pre-registered expectation is blocking; **constructed controls may never support an input-reachability claim** |

## 7. Escalation conditions to Option A

Escalate to the Option A pinned compiler-proof route (`35d9211b…`) — reporting immediately, with
the finding preserved — if any of the following occurs:

1. Any A-case yields a prohibited-direction outcome (minted fact, false-available,
   false-complete, wrong reason) — **blocking for the amended basis**.
2. The isolated refusal cannot be demonstrated even on a constructed input (the path is
   unreachable in principle) — contradicts established claim 1; investigate before disposition.
3. The integrated accounting chain fails to carry the unresolved classification through any
   stage.
4. The reviewer concludes the amended basis is insufficient and the original reachability basis
   is required (a reviewer judgment, restated for the owner).

## 8. Scope restrictions

Independent qualification only: no implementation, remediation, contract change, production/test
modification, CI execution, or owner acceptance in this commission's execution session. The
outcome either supports M5.4 under the amended contract (owner records satisfaction) or returns
to the owner with findings. WP-S25-4 closure, Stage-2 acceptance and Stage 6 remain separate.