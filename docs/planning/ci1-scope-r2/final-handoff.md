# Final handoff — CI-1-SCOPE-R2

**Disposition: `CI-1-SCOPE-R2 READY FOR INDEPENDENT Q3`.**

This packet corrects the executable F04 planning oracle and its documentation,
with preserved failing-before and passing-after evidence. It does not implement
CI-1A, does not fix production defects, and does not grant owner acceptance.

## A. Git binding

| Binding | Value |
| --- | --- |
| Repository / origin | `jenolaszlo-sketch/andirit` / `https://github.com/jenolaszlo-sketch/andirit.git` |
| Canonical checkout | `D:/projects/Andirit` |
| Branch | `refs/heads/main` |
| Starting HEAD | `f1605e9e02fc383cf776127c67369ab7815cd546` |
| Starting tree | `8b5bb5e95d08318a18698bd606b149f0f12aaf6f` |
| Production baseline | `cd2b7fb5d73f4dc462b0ef7fcadfd75a91c118a2` |
| Advertised `origin/main` at start | `f1605e9e02fc383cf776127c67369ab7815cd546` |
| Worktrees | one (`D:/projects/Andirit f1605e9 [main]`) |
| SDK / compiler | .NET SDK `10.0.401`; Roslyn `5.9.0` |
| R2 delivery commit / tree | `cb2733394053c57a20228d8f61442cb32489ef31` / `ec588ad1ab45507a9addc4d70bc2dd4125012410` |
| Final HEAD after bookkeeping update | recorded in the R2 session report (parent = the R2 delivery commit above) |

No linked worktrees or branches were created. Only `docs/planning/ci1-scope-r2/`
was added and three R1 documents received appended R2 amendments.

### Changed-file inventory

- **New:** every file under `docs/planning/ci1-scope-r2/` (`README.md`,
  `f04-root-cause.md`, `oracle-contract.md`, `mutation-matrix.md`,
  `reproduction.md`, `q3-reviewer-handoff.md`, `final-handoff.md`,
  `analysis/**`, `evidence/**`).
- **Amended (append-only R2 blocks):**
  `docs/planning/ci1-scope-r1/acceptance-reconciliation.md`,
  `docs/planning/ci1-scope-r1/corrections.md`,
  `docs/planning/ci1-scope-r1/findings-matrix.md`.
- **Protected paths unchanged:** `src/**`, `tests/**`, `.github/**`, schemas,
  packages and historical contracts.

## B. Q2 reproduction (independently reproduced)

Reproduced against the prior checker
(`evidence/before/before-run.json`): 32 sites, 0 baseline failures, 13 mutations,
**4 undetected** — `outcome-external-to-confirmed`,
`outcome-confirmed-to-external`, `outcome-confirmed-to-deferred`,
`outcome-invalid-enum`. This matches the Q2 report exactly. At least one
reproduced failure (`outcome-external-to-confirmed`) shows an externally bound
`System.Console.WriteLine` call relabelled `confirmed` without rejection.

## C. Root cause

The prior checker used the declared outcome string as the specification of what
to validate instead of comparing it to a compiler-derived outcome. Its positive
predicate (`outcome.StartsWith("confirmed")`) required only a bound method
invocation, not an **eligible indexed local definition**; its only negative
checks covered two explicit strings; and it had no recognised-outcome
vocabulary. Full analysis: [`f04-root-cause.md`](f04-root-cause.md).

## D. Oracle correction

`analysis/oracle/` derives the occurrence category and outcome class from fixture
source and the recorded compilation/dependency context, and requires a
`confirmed` target to be an eligible indexed local definition proven by
containing assembly identity **and** a source declaration. It enforces an exact
declared-outcome vocabulary and rejects unknown strings. Full rules:
[`oracle-contract.md`](oracle-contract.md).

Independence boundary: the oracle is pure Roslyn, does not reference the product
and does not read any CI-1A output (none exists). Candidate-result comparison
(step 4) is explicitly unqualified.

## E. Mutation results

Before → after (see [`mutation-matrix.md`](mutation-matrix.md)):

| Required result | Before | After | Reason code |
| --- | --- | --- | --- |
| External → confirmed (Q2 regression) | Accepted | Rejected | `OUTCOME_CLASS_MISMATCH` |
| Confirmed → external (Q2 regression) | Accepted | Rejected | `OUTCOME_CLASS_MISMATCH` |
| Confirmed → deferred (Q2 regression) | Accepted | Rejected | `OUTCOME_CLASS_MISMATCH` |
| Unrecognised outcome (Q2 regression) | Accepted | Rejected | `OUTCOME_UNRECOGNIZED` |
| Ambiguous → confirmed | Accepted | Rejected | `OUTCOME_CLASS_MISMATCH` |
| Unresolved → confirmed | n/a | Rejected | `OUTCOME_CLASS_MISMATCH` |
| Confirmed → wrong overload | Rejected | Rejected | `TARGET_IDENTITY_MISMATCH` |
| Confirmed → wrong assembly | n/a | Rejected | `TARGET_IDENTITY_MISMATCH` |
| Confirmed → unrelated indexed method | Rejected | Rejected | `TARGET_IDENTITY_MISMATCH` |
| Correct target → incorrect span | Rejected | Rejected | `COORDINATE_LEXEME_MISMATCH` |
| Correct entry → duplicate entry | Rejected | Rejected | `DUPLICATE_OCCURRENCE` |
| Correct expected outcome (control) | n/a | **Accepted** | — |

No rejection fires for an unrelated reason first. Positive controls: the
unchanged manifest passes 32/32 and the no-op control is accepted.

## F. Acceptance integrity

All 32 original manifest rows retain their intended semantics under independently
derived expectations (`semanticIdentity.allRowsDerivedMatchesDeclared = true`);
class distribution: `confirmed` 21, `external` 2, `ambiguous` 1, `unresolved` 1,
`unsupported` 7. Coordinates, overload, duplicate and target validation from R1
remain satisfied. The F01 collision stays `external` even when an unrelated
indexed method shares the documentation ID.

## G. Preserved qualifications

- **F01–F03, S01–S05:** Q2-supported at planning level; not reopened.
- **P-DEF-01:** existing false C# semantic target confirmation — open; not fixed.
- **P-DEF-02:** existing documented partial-type extraction failure — open; not
  fixed.
- **ADR-CI1-01:** C vs B2 occurrence representation — open; not selected.
- Historical storage and query contracts: unchanged.

## H. Remaining findings

| Item | Severity | Evidence | Required action |
| --- | --- | --- | --- |
| `Q2-F04-01` | P2 | Corrected by R2; four regressions now rejected | Independent Q3 qualification |
| `P-DEF-01` | P1 | F01 collision reproduced; remains external-only in planning | Owner chooses P-DEF-01a/b before any positive confirmation |
| `P-DEF-02` | P1 | Documented partial-type failure | Owner authorizes P-DEF-02 or narrows qualification |
| `ADR-CI1-01` | P2 | No measured winner | Comparative experiment before selection |
| Candidate-result comparison | — | No candidate exists | Future, after CI-1A authorization |

## I. Disposition

**`CI-1-SCOPE-R2 READY FOR INDEPENDENT Q3`.**

The oracle now distinguishes genuine semantic evidence from an incorrect expected
outcome. No production source, tests, schemas, packages or CI workflows were
modified. The branch is local-only and **not pushed**.
