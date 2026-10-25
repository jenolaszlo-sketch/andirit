# EC-6B owner acceptance and closure evidence

- **Milestone:** EC-6B, Conservative Relative Module Resolution
- **Decision:** [`EC6B-OA2`](../../decisions/ec6b-owner-acceptance.md) —
  **EC-6B ACCEPTED WITH QUALIFICATIONS**
- **Status:** closure record committed; no production, test, integration, or
  release change is authorized by this evidence directory.
- **Date:** 25 October 2026

This directory indexes the owner-authorized closure of EC-6B. It is a
documentation-only record. The independent Q1 and Q2 review reports are
preserved on their own branches by immutable Git identity and are not copied,
re-committed, or modified here.

## A. Owner instruction and decision

- [`owner-instruction.md`](owner-instruction.md) records the operative
  directives of the owner's closure assignment.
- [`../../decisions/ec6b-owner-acceptance.md`](../../decisions/ec6b-owner-acceptance.md)
  is the owner decision (`EC6B-OA2`), including the findings reconciliation,
  Q2 observation dispositions, and the `EC6B-OA1` §10 closure-gate assessment.

## B. Implementation and remediation source binding

| Stage | Identity |
| --- | --- |
| Qualified baseline | `558f8ac82ac1b5e07da68f4a71a70240eaf38409` (tree `08a01a1b7d5d3109b991bcd05d57272fd3f5f018`) |
| Accepted EC-6A decision | `1d11b7330e1d8755d5c3e3b4a830167bcf8b5bf6` (tree `649b3111a273478547d8542770220e043fd5cbd5`) |
| EC-6B implementation | `8aa083ce36e52f666578f5ed6b539ec194178561` (tree `52f877acb0daf2cdf41fedf2d1d1fb0ff6fda034`, parent `1d11b73`) |
| EC-6B handoff | `691a8327ab7446416de22fe26e023662399b6cf2` (tree `f2bb8c5faa301fdfa8542fc1d39aaeec68040356`, parent `8aa083c`) |
| R1 source correction | `ffdcdbdf57e35f94a1e459a5b393649778c21f69` (tree `4f3f27cc4bc99b6e9293643a908d107c076a6b63`, parent `691a832`) |
| R1 regression test | `b915aed2a795f38d682fcd8289282eecd8110666` (tree `559ec7ba97d15a578498bc02ae03e9d9ca3fd857`, parent `ffdcdbd`) |
| R1 documentation/evidence | `f8fb9d1c2917ef04fdf9bca721bcbeaaecb959e9` (tree `22cc536a8492b1bf00be1d04ae94a72e3db62d87`, parent `b915aed`); documentation-only delta from the tested source |
| **Tested implementation** | exactly `b915aed2a795f38d682fcd8289282eecd8110666` |
| **Accepted evidence/docs tree** | exactly `f8fb9d1c2917ef04fdf9bca721bcbeaaecb959e9` |

The acceptance branch `decisions/ec6b-owner-acceptance` was created from
exactly `f8fb9d1`. No implementation, remediation, review, or canonical branch
was modified.

## C. Independent review identities

| Review | Identity |
| --- | --- |
| Q1 report | `docs/reviews/ec6b-glm-q1-relative-module-resolution-verification.md` at `66690587f4507d47e78fa4e1996e050b830c1642` (tree `fc2bedc73a6503a69598b1e0ac2752cf43fe4831`, parent `691a832`), branch `review/ec6b-glm-q1`, unmerged |
| Q1 verdict | **REMEDIATION REQUIRED** — F01 (P3), F02 (P2), F03 (P3), F04 (P3) |
| Q2 report | `docs/reviews/ec6b-glm-q2-remediation-verification.md` at `b79bfce9820f08348335db7ca0b19f4c65e3a014` (tree `c13780fa75036c7634ebdb67c0114fd33a59c7bf`, parent `f8fb9d1`), branch `review/ec6b-glm-q2`, unmerged |
| Q2 verdict | **VERIFIED WITH QUALIFICATIONS** — no P0/P1/P2 findings; Q2-O1–Q2-O4 nonblocking observations |

The Q1 and Q2 commits add only review reports and review-only probe files.
They were never merged into `main` or the acceptance branch.

## D. Evidence chain and categories

| Stage | Evidence |
| --- | --- |
| EC-6B implementation | `8aa083c` (source) and `691a832` (handoff/documentation) |
| Q1 independent review | `6669058` (report and five review-only probe files) |
| Remediation | `ffdcdbd` (source/tests) and `b915aed` (additional regression test) |
| Remediation evidence | `f8fb9d1` (documentation and evidence only) |
| Q2 independent review | `b79bfce` (report and three review-only probe files) |
| Owner acceptance | this commit (decision and closure evidence) |

Categories used in this closure:

- **Implementation-generated evidence:** the EC-6B and R1 commits, tests, and
  the R1 evidence README.
- **Independently reproduced evidence:** Q1 and Q2 probe executions and the
  Q2 p6→p8 and p7→p8 reproductions, recorded in their reports.
- **Historical evidence inspected by GLM:** the p6 extractor at `1d11b73`, the
  pre-remediation p7 extractor at `691a832`, and the pinned website snapshot.
- **Conclusions accepted by the owner:** F01–F04 remediation, the Q2 verdict,
  and the accepted limitations in decision §7.
- **Deferred obligations:** EC-6C–EC-6F gates and the P3-C parser traceability
  recorded in decision §8.

The owner did not re-execute tests in this documentation-only assignment and
claims no new test, build, migration, or platform result.

## E. Findings and observations

The authoritative reconciliation is decision §4 (Q1 findings) and
decision §5 (Q2 observations). Summary:

| Item | Disposition |
| --- | --- |
| F01 UTF-16 coordinate contract | Accepted remediation as verified |
| F02 escaped-specifier false resolution | Accepted remediation as verified |
| F03 contextual `type-only` classification | Accepted remediation as verified |
| F04 nested `require` ambiguity | Accepted documented clarification |
| Q1-Q1 "byte columns" documentation qualification defect | Corrected and verified |
| Q1-Q2 missing escaped/type-name regression coverage | Remediated and independently re-verified |
| Q1-R "reproduced without finding" observations | No action required |
| P3-A pathological grammar forms | Accepted documented limitation |
| P3-B escaped structural placeholders | Accepted as designed |
| P3-C `export type * as ns` label ambiguity | Accepted pre-existing limitation; roadmap traceability |
| P3-D stale `resolver1` example | Documentation follow-up completed in this closure |

## F. Evidence artifact manifest

[`manifest.md`](manifest.md) records the immutable source commits, Git blob
identities, and SHA-256 hashes for:

- the Q1 report and its five review-only probe files;
- the Q2 report and its three review-only probe files;
- the R1 evidence README and the EC-6B handoff;
- the tested source and test files at `b915aed`.

Hashes are computed over committed Git blob content, not checkout bytes. The
documents created by this closure are not self-hashed to avoid circular
digests; their commit and tree identities are recorded in the completion
report and in decision §2.

## G. Validation

[`validation.md`](validation.md) records the documentation-only checks
performed for this closure: Git identity resolution, ancestry and
documentation-only transition checks, unmerged-review checks, worktree
cleanliness, hash-manifest verification, and relative-link checks. No build or
test run is claimed.

## H. Outstanding obligations and integration restrictions

- **EC-6C planning** may be commissioned under a separate assignment.
  **EC-6C implementation and integration are not authorized.**
- **Production integration** of EC-6B is not authorized. A future integration
  decision must bind to the exact accepted tree.
- **Release/publication** is not authorized.
- The accepted limitations in decision §7 remain in force. Evidence of a
  false `Resolved` fact from any limitation reopens bounded remediation.
- The P3-C parser traceability item remains future work.

## I. Files created or changed by this closure

Created:

- `docs/decisions/ec6b-owner-acceptance.md`
- `docs/evidence/ec6b-owner-acceptance/README.md`
- `docs/evidence/ec6b-owner-acceptance/owner-instruction.md`
- `docs/evidence/ec6b-owner-acceptance/manifest.md`
- `docs/evidence/ec6b-owner-acceptance/validation.md`

Updated (documentation only):

- `docs/language-support-roadmap.md` — EC-6B status, EC-6C eligibility, P3-C
  traceability.
- `ROADMAP.md` — milestone status and boundaries.
- `CHANGELOG.md` — acceptance record (documentation-only; no behavior claim).
- `docs/language-feature-matrix.md` — EC-6B accepted status.
- `docs/design/ec6a-implementation-roadmap.md` — EC-6C entry conditions and
  profile-revision planning correction (p8 → p9; EC-6D p9 → p10).
- `docs/design/ec6a-cross-file-resolution-architecture.md` — P3-D provider
  example corrected to `resolver2.relative-v1`.
- `docs/evidence/ec6b-relative-module-resolution-handoff.md` — acceptance
  status banner; historical content retained.
- `docs/evidence/ec6b-r1-relative-module-resolution-remediation/README.md` —
  acceptance status banner; historical content retained.

No `src/`, test, schema, or production file is changed by this closure.
