# QUAL-04 reviewer corrections and control refinements

These corrections concern only the reviewer-owned harness, checker, runner and
negative-control method. No production source, test, CI, contract or repository
history was edited to obtain a passing result. The final `expected.json`
acceptance values were derived from the governing contracts and the documented
R3 design; none was relaxed after candidate execution.

## Pre-candidate harness and checker corrections

| Correction | Reason | Boundary and disposition |
| --- | --- | --- |
| Project unit identity is `csharp:project:` plus the lowercase SHA-256 of the UTF-8 repository-relative project path; `CodeOccurrenceContributor.IndexUnitId` is a string. | The first reviewer implementation used an obsolete identity shape and type. | Corrects the reviewer harness to the production contract; it does not change an acceptance value. Final sources are in `harness/`. |
| `DescribeResult` coverage fields are camelCase (`declaredUnitCount`, `verifiedUnitCount`, `invocationFacts`, `unresolved`, `external`, `ambiguous`, `unsupported`). | The first checker used PascalCase names. | Schema-name correction in `harness/check.py`; the same expected values and assertions remain. |
| The checker accepts absent coverage fields as zero only for a refused availability where no coverage block is produced. | A refusal legitimately has no coverage object. | This distinguishes refusal semantics from an unexpectedly missing observation in an Available result. |
| `expected.json` describes A3 as three projects with calls `[1,1,1]`, not two projects. | The executable A3 fixture builds three projects; the initial description was stale. | Fixture-description correction only. Declared/verified 3/3, one target hit, three disjoint rows and three facts were already the governing expectations. |
| The runner clears a durable store only in the publishing phase; verify reopens the exact file left by the publisher. | Clearing in verify would republish and could hide a persistence defect. | Preserved in `run-review.ps1`; lifecycle generations are explicitly not cleared between generations. |
| Publication is skipped in verify phases; verification reopens the published file in a fresh process. | The first attempts republished during verification and therefore did not prove persistence. | Phase guards were added to `Membership.cs`, `Legacy.cs` and `Preservation.cs`; every final receipt records publish and verify separately. |
| LatticeDB reads and writes use separate database sessions; the reader is closed before a tamper write. | LatticeDB takes an exclusive file lock, so the original read-then-write sequence could fail before reaching the intended assertion. | Sequencing correction only; the tamper boundary remains exercised. |
| The native row reader is schema-tolerant and retains the complete raw row in each receipt. | Earlier code assumed one exact envelope layout and discarded evidence needed for independent checks. | Evidence-completeness correction; raw rows are compared by the Python checker. |
| B3 identity assertions bind native rows to the recorded caller/dependency project-unit IDs, and F-S254-02 gives `Use/Use.csproj` an explicit project reference to `Dep/Dep.csproj`. | The earlier suffix-based association and no-reference fixture did not establish caller ownership/dependency binding. | Fixture and checker strengthening; it avoids false credit from path suffix or an unrelated project graph. |

## Negative-control refinement

The originally registered N1 control was refined before its final execution so
that it reverts **both** R3 remediations: the disjoint session contribution and
the legacy restore projection. That is the exact R2 accounting semantics on R3
code. The final N1 result is 4/4, 4/4 and 9/9 on two complete projects, two
complete-empty projects and three complete projects, respectively, with the
historical whole-universe persisted-row shape. The checker detects all six
publish/verify expectations and the row-shape equality.

A supplemental **N1a** control was added after the initial single-line N1
attempt did not produce the registered aggregate defect. N1a reverts only the
session contribution and retains the restore projection. Its result remains
correct at the aggregate level (2/2, 2/2 and 3/3), while its durable rows keep
the historical whole-universe shape. This establishes that the restore
projection alone carries the corrected aggregate, while the session change
makes newly published rows self-describing. N1a is transparent supplemental
diagnostic evidence, not a substituted pre-registered acceptance control.

N2 removes only the restore projection. A matching-authority historically
shaped three-unit store then reports 9/9 instead of 3/3, so the control is
detected as registered. All three mutation trees are disposable copies under
`C:\Windows\Temp\opencode\qual04\mutation`; the pinned exports and canonical
repository were not modified. See `mutations/N1.diff`, `mutations/N1a.diff`,
`mutations/N2.diff`, `checks/check-mutations.txt` and the mutation receipts.

## Scope

The corrections do not alter the pinned production bytes, the R2 negative
baseline, the frozen oracle, the 64-row matrix criteria, the 78-obligation
routing, M5.4 disposition D, the Ubuntu blockers, exact-byte formatting, or
F-G2-01. Failed and superseded attempts remain visible in the diffs, checker
history and retained receipt sets; they are not rewritten as successes.
