# EC6B-OA2: Owner acceptance and closure reconciliation for EC-6B

**Status:** owner decision record. This document closes EC-6B under the
qualifications recorded below. It authorizes no merge, integration, release,
or EC-6C implementation.
**Project:** Penghou.Hetu (future Andirit)
**Decision ID:** `EC6B-OA2` (closure reconciliation of `EC6B-OA1` §10)
**Milestone:** EC-6B, Conservative Relative Module Resolution
**Date:** 25 October 2026
**Decision authority:** Project owner
**Decision:** **EC-6B ACCEPTED WITH QUALIFICATIONS.**

---

## 1. Decision summary

The owner accepts the EC-6B implementation, the EC-6B-R1 remediation, and the
independent verification chain. EC-6B introduces conservative, policy-scoped
cross-file module resolution through the `resolves-to` relationship inside the
existing Tree-sitter extraction session, under the accepted `relative-v1`
policy. The implementation reuses existing file-node identities, preserves the
publication model, and does not claim import-to-declaration binding, export
binding, or compiler/bundler/runtime equivalence.

The independent `EC6B-GLM-Q1` review returned **REMEDIATION REQUIRED** with
three confirmed defects (F01, F02, F03) and one contract ambiguity (F04). The
bounded EC-6B-R1 remediation corrected them and the independent `EC6B-GLM-Q2`
re-review returned **VERIFIED WITH QUALIFICATIONS** with no P0/P1/P2 findings.
The owner accepts that verdict and the remaining documented limitations.

This record is the owner reconciliation required by `EC6B-OA1` §10 item 8. It
does not authorize production integration (`EC6B-OA1` §10 item 9) and does not
authorize EC-6C implementation.

## 2. Git and evidence binding

### Accepted ancestry

`558f8ac` → `1d11b73` → `8aa083c` → `691a832` → `ffdcdbd` → `b915aed` →
`f8fb9d1` (linear; resolved in full below).

| Stage | Identity |
| --- | --- |
| Repository | `jenolaszlo-sketch/penghou-hetu` |
| Qualified production baseline / canonical `main` | `558f8ac82ac1b5e07da68f4a71a70240eaf38409` (tree `08a01a1b7d5d3109b991bcd05d57272fd3f5f018`) |
| Accepted EC-6A decision tip | `1d11b7330e1d8755d5c3e3b4a830167bcf8b5bf6` (tree `649b3111a273478547d8542770220e043fd5cbd5`) |
| EC-6B implementation | `8aa083ce36e52f666578f5ed6b539ec194178561` (tree `52f877acb0daf2cdf41fedf2d1d1fb0ff6fda034`, parent `1d11b73`) |
| EC-6B documentation/handoff | `691a8327ab7446416de22fe26e023662399b6cf2` (tree `f2bb8c5faa301fdfa8542fc1d39aaeec68040356`, parent `8aa083c`) |
| Q1 independent review | `66690587f4507d47e78fa4e1996e050b830c1642` (tree `fc2bedc73a6503a69598b1e0ac2752cf43fe4831`, parent `691a832`; branch `review/ec6b-glm-q1`, unmerged) |
| R1 implementation correction | `ffdcdbdf57e35f94a1e459a5b393649778c21f69` (tree `4f3f27cc4bc99b6e9293643a908d107c076a6b63`, parent `691a832`) |
| R1 regression test | `b915aed2a795f38d682fcd8289282eecd8110666` (tree `559ec7ba97d15a578498bc02ae03e9d9ca3fd857`, parent `ffdcdbd`) |
| R1 documentation/evidence | `f8fb9d1c2917ef04fdf9bca721bcbeaaecb959e9` (tree `22cc536a8492b1bf00be1d04ae94a72e3db62d87`, parent `b915aed`); docs-only delta from `b915aed` |
| Q2 independent review | `b79bfce9820f08348335db7ca0b19f4c65e3a014` (tree `c13780fa75036c7634ebdb67c0114fd33a59c7bf`, parent `f8fb9d1`; branch `review/ec6b-glm-q2`, unmerged) |
| Owner acceptance | this document; committed on `decisions/ec6b-owner-acceptance` from exactly `f8fb9d1` |

The review commits add only review reports and review-only probe files; they
were never merged. The accepted Q1 report is
`docs/reviews/ec6b-glm-q1-relative-module-resolution-verification.md` at
`6669058`; the accepted Q2 report is
`docs/reviews/ec6b-glm-q2-remediation-verification.md` at `b79bfce`. Both are
preserved unchanged; this decision references them by immutable Git identity.

## 3. Acceptance basis

The owner accepts the following, as independently established by
`EC6B-GLM-Q2` and the preserved evidence:

**A. UTF-16 source positions.** UTF-16 code-unit columns were traced through
parsing, span construction, symbol identity, persistence, and consuming
queries. Supplementary Unicode and both stores with durable reopen were
covered. Enclosing-symbol lookup uses the UTF-16 column, not the UTF-8 byte
column. The earlier "byte columns" contract wording is corrected (F01).

**B. Escaped module specifiers.** Specifiers containing JavaScript escape
sequences cannot create false resolved module edges; they are counted as
unsupported, retain no specifier text, and escaped-only repositories publish
no resolver facts. Ordinary unescaped resolution is unchanged (F02).

**C. Type-only classification.** The independently verified 18-case matrix
supports the grammar-aware treatment of contextual `type` and `as`.
Statement-level, inline, aliased, star, namespace, and string-name forms
retain their specified behavior; mixed forms are not wholly type-only;
JavaScript stays ordinary; pathological forms are handled conservatively
(F03).

**D. Graph compatibility.** No unintended `exports` or `binds` edges exist.
Structural `imports` remain `Structural`. Impact/dependents presets exclude
`resolves-to`. Reversed enumeration and LF/CRLF inputs remain deterministic.

**E. Historical resolver invalidation.** The authentic p7→p8 reproduction
establishes that unchanged sources are re-evaluated, the incorrect historical
`./a` resolution is removed, unaffected resolver edges keep their identities,
the incorrect `type-only` property is removed, provider
`+resolver2.relative-v1` is recorded, non-resolver facts are preserved, and
corrected facts survive durable close/reopen.

**F. Historical preservation.** The authentic p6→p8 reproduction establishes
that historical non-resolver graph facts are retained, exactly the expected
`resolves-to` edges are introduced, stable edge identities match the reviewed
publication, both plugins execute where required, and the pinned website
snapshot matches the expected node, declaration, and edge counts.

**G. Publication behavior.** Add, delete, empty replacement, cancellation
retention, mid-unit limit failure without partial publication, and
endpoint-order rejection were independently probed on both supported stores.

**H. Validation.** The Q2 review reports 408 full-suite tests, 41 focused
module-resolution tests, 45 independent Q2 probe cases, successful formatter
verification, clean whitespace checks, and no confirmed P0/P1/P2 defects.
These are the reviewer's executed results; the present documentation-only
closure did not re-run tests and claims no new results.

## 4. Q1 findings reconciliation

| ID | Original finding (Q1) | Owner disposition | Verification basis |
| --- | --- | --- | --- |
| **F01** | Published columns were described as byte columns while the pipeline publishes UTF-16 code units (confirmed defect P3; also a qualification defect in the handoff/consumer docs) | **ACCEPT REMEDIATION AS VERIFIED** | Q2 §C (F01) and §J; R1 contract/consumer/identity corrections at `f8fb9d1`; permanent UTF-16 column test and durable-store probe |
| **F02** | Escaped specifiers were truncated at the first fragment and could emit false `Resolved` edges (P2) | **ACCEPT REMEDIATION AS VERIFIED** | Q2 §C (F02), §F (p7→p8 removal); R1 `RawModuleSpecifier` rejection at `b915aed`; escaped regression tests and escaped-only publication probe |
| **F03** | Bindings literally named `type` were marked `type-only` (P3) | **ACCEPT REMEDIATION AS VERIFIED** | Q2 §C (F03), §D; grammar-structure detection at `b915aed`; 18-case matrix, mixed-form, and JavaScript tests |
| **F04** | Legacy module forms were classified asymmetrically (`export = require(...)` captured the nested literal require) (design ambiguity P3) | **ACCEPT DOCUMENTED CLARIFICATION** | Q2 §C (F04); behavior confirmed consistent with the accepted "literal `require` (top level or nested)" rule; consumer doc and regression test add no new production semantics |
| **Q1-Q1** | Qualification subfinding: the implementation handoff and consumer documentation stated "byte columns" | **CORRECTED AND VERIFIED** | F01 remediation; Q2 §C; this closure corrects the remaining illustrative example (P3-D) |
| **Q1-Q2** | Qualification subfinding: regression coverage lacked escaped-specifier and `type`-named-binding cases | **REMEDIATED AND INDEPENDENTLY RE-VERIFIED** | R1 added 19 focused cases; Q2 independently re-executed the behavior matrix |
| **Q1-R** | Q1's "reproduced without finding" observations (lifecycle, occurrence identity, wrong-order rejection, durable reopen, cross-plugin decline, presets, website) | **NO ACTION REQUIRED** | Retained as verified context; Q2 re-confirmed compatibility and publication behavior |

No Q1 finding is left unresolved. F02 is not characterized as a resolved
dependency for escaped specifiers: those sites remain unsupported and
edge-free, as required.

## 5. Q2 nonblocking observation dispositions

| Observation | Disposition | Treatment |
| --- | --- | --- |
| **P3-A / Q2-O1:** pathological grammar forms (`export { type as }`, `export { type as as }`, `import { type as as as }`) are conservatively not type-only | **ACCEPT AS DOCUMENTED LIMITATION** | No parser or classification broadening during closure. Any expansion requires separate design, fixtures, and qualification. No false resolver fact is produced today. |
| **P3-B / Q2-O2:** structural `imports` placeholders for escaped specifiers retain pre-existing first-fragment text | **ACCEPT AS DESIGNED** | The owner-approved distinction between structural observations and resolved module edges is preserved. No resolver fact or retained escaped specifier text is invented. |
| **P3-C / Q2-O3:** `export type * as ns from` is labeled `module-reference = export-type-from` rather than `export-namespace-from` | **ACCEPT AS PRE-EXISTING LIMITATION** | Not introduced by EC-6B-R1 and not a false resolution. Traceability is recorded in the language roadmap improvement queue for future parser/module-fact work. |
| **P3-D / Q2-O4:** the architecture provider-version example still cited `resolver1` | **DOCUMENTATION FOLLOW-UP COMPLETED** | The illustrative example in the accepted architecture is corrected to the current `resolver2.relative-v1` value in this closure. Historical review wording is unchanged. No production code changes. |

## 6. Closure gate assessment (EC6B-OA1 §10)

| # | Requirement | Status | Evidence |
| --- | --- | --- | --- |
| 1 | Independent EC6B-GLM-Q2 review completed and preserved | **Met** | `b79bfce` (tree `c13780f`, parent `f8fb9d1`), branch `review/ec6b-glm-q2`; report preserved unchanged |
| 2 | All material F01–F04 findings verified or explicitly reconciled | **Met** | §4 above; Q2 §C |
| 3 | No unresolved P0 or P1 defects | **Met** | Q2 §K: no P0, P1, or P2 findings |
| 4 | No unresolved P2 permitting false resolution, stale facts, or publication inconsistency | **Met** | F02 closure; Q2 §F p7→p8 removal; Q2 §H publication probes |
| 5 | Version invalidation adequately qualified | **Met** | Q2 §F; profile p8, resolver revision 2 |
| 6 | Existing graph identities preserved | **Met** | Q2 §G: 0 missing p6 lines, exactly 3 added resolver edges, ids match p7 |
| 7 | Scope and unsupported behavior accurately documented | **Met** (P3-D corrected here) | Consumer doc, identity doc, decision §7; P3-D example corrected |
| 8 | Independent findings reconciled into an owner closure record | **Met by this document** | This commit |
| 9 | Production integration separately authorized | **Not granted** — standing boundary | No merge, integration, or release is authorized by this closure |

The exact `EC6B-OA1` §10 item 9 wording is "Production integration separately
authorized." This record documents that production integration remains a
separate authorization. Separately, EC-6C **planning** may be commissioned under
its own assignment; EC-6C implementation and integration are not authorized.

## 7. Accepted qualifications and limitations

- Escaped module specifiers remain unsupported under `relative-v1` and are
  conservatively rejected.
- Pathological TypeScript grammar forms around `type`/`as` remain
  conservatively classified.
- Structural `imports` placeholders retain their pre-existing behavior for
  unsupported escaped text.
- Resolution is limited to the owning plugin's indexed source inventory;
  cross-plugin, package, alias, and generalized compiler/bundler resolution
  remain outside scope.
- Export surfaces and import-to-declaration binding are not implemented.
- Real-repository coverage is limited to the pinned website snapshot and the
  supported language features.
- The `export type * as ns` module-reference label ambiguity is accepted as a
  pre-existing limitation with future traceability.
- No performance, scale, durability-beyond-tested, or additional-platform
  claims are made.

These are coverage limitations, not authorization to emit incorrect resolved
facts. Any future evidence that a limitation produces a false `Resolved`
relationship is a correctness finding and reopens bounded remediation.

## 8. Deferred obligations and future gates

- **EC-6C (export surface):** planning may be commissioned separately. Entry
  conditions: a separately authorized design/planning assignment; the EC-6B
  contract and limitations as inputs; the EC-6C owner gate before
  implementation; no dependency on merging EC-6B. Implementation is not
  authorized by this closure.
- **EC-6D (import-to-export binding):** name equality alone remains
  insufficient evidence; separately accepted contract required.
- **EC-6E (publication/durability qualification):** performance, incremental
  invalidation, durability, and resource-bound qualification beyond the EC-6B
  minimum remain future work.
- **EC-6F (real-repository qualification):** broader real-repository
  evaluation with independent ground truth remains future work.
- **P3-C traceability:** revisiting the `export type * as ns` label requires a
  future parser/module-fact improvement; it is not an EC-6B blocker.

## 9. Integration, preservation, and release boundary

Until a separate integration authorization:

- Canonical `main` remains at `558f8ac`; no merge, cherry-pick, rebase, or
  push is authorized.
- The EC-6B implementation branch (`691a832`), the R1 remediation branch
  (`f8fb9d1`), and the review branches (`6669058`, `b79bfce`) are preserved
  unchanged and unmerged.
- The accepted source is `b915aed`; the accepted evidence/docs tree is
  `f8fb9d1`. A future integration must bind to exactly the authorized tree.
- No release or package publication is authorized.
- EC-6B is qualified only within the tested environments, pinned grammars,
  `relative-v1` policy, and evaluated source inventories.

## 10. Evidence preservation

The Q1 and Q2 reports and their review-only probes remain on their review
branches at the immutable commits above. They were not modified, re-committed,
or merged by this closure. The closure evidence index, artifact manifest, and
validation record are in
[`docs/evidence/ec6b-owner-acceptance/`](../evidence/ec6b-owner-acceptance/README.md).

## 11. Final decision

**EC-6B ACCEPTED WITH QUALIFICATIONS.**

Original Q1 blocking defects: **REMEDIATED AND INDEPENDENTLY VERIFIED.**
Q2 P3 observations: **ACCEPTED OR DEFERRED WITH EXPLICIT DISPOSITIONS.**
EC-6C planning: **ELIGIBLE FOR SEPARATE AUTHORIZATION.**
EC-6C implementation: **NOT AUTHORIZED.**
Integration/merge: **NOT AUTHORIZED.**
Release/publication: **NOT AUTHORIZED.**
