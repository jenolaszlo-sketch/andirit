# Acceptance reconciliation

This document reconciles the C01–C20 matrix after the Q1 findings. It preserves
the reviewer's independent audit of the existing 32 selected rows, records every
changed expectation, and adds the mandatory inputs the findings require. It does
not rewrite the original matrix or its fixture bytes.

## Independent audit of the existing matrix (Q1, reproduced by R1)

The reviewer audited **32/32 selected spans and intended semantics pass**. R1's
oracle reproduces this (`reproduction.md` §Acceptance). Per-case outcomes:

| Case | Rows / audit outcome and qualification |
| --- | --- |
| C01 | 3: correct `Write` targets, separate spans, same `Execute` owner; method reference does not add a usage. |
| C02 | 2: int/string overloads correctly distinguished. |
| C03 | 1: App calls Lib definition through its project reference; App owns site. |
| C04 | 2: `Echo<T>` and defining `Mark<T>` extension are correct normalized targets. |
| C05 | 4: this/base/implicit-new/explicit-new bind to written constructors; constructor discovery remains deferred. |
| C06 | 1: `IWorker.Work` static target, no implementation expansion. |
| C07 | 2: Worker-typed and base calls bind `Worker.Work`. |
| C08 | 2: `Target.Work` method group deferred; `action()` binds `System.Action.Invoke`. |
| C09 | 3: `typeof`/`nameof` deferred; only `Target.Work()` is an ordinary positive. |
| C10 | 1: `Console.WriteLine(string)` external; **needs the F01 collision variant added**. |
| C11 | 2: `Pick(null)` ambiguous (`CS0121`); `Missing()` unresolved (`CS0103`). |
| C12 | 1: `Work` resolves across two partial files; **must add a documented/sealed partial; plain C12 does not cover F02**. |
| C13 | No selected row: deterministic enumeration/retry; candidate execution required. |
| C14 | 2: replacement file has first/third calls; deletion/hash retirement unexecuted. |
| C15 | 3: calls shift one line; new-hash lifecycle unexecuted. |
| C16 | No selected row: deterministic prefix and budget tests are future execution. |
| C17 | No selected row: publication movement/cancellation requirements. |
| C18 | No selected row: memory/durable/native/reopen parity is future execution. |
| C19 | 2: separate paths/owners preserve distinct calls. |
| C20 | 1: inactive by default; binds `Target.Work` with `CI1_EXTRA`. |

These rows and their target/owner evidence are preserved as oracle input. No
source row was amended.

## Changed expectations (7)

Only these expectations change; all are required by independently established
findings.

1. **C10 gains a mandatory collision variant.** In the same two-project shape as
   F01, `Z/App.cs` calling the platform `System.Console.WriteLine` while
   `A/Shadow.cs` declares its own `System.Console.WriteLine(string)` must yield
   **no** confirmed local usage (external/refused), not a resolved cross-project
   call. The existing external `Console.WriteLine` expectation is preserved.

2. **C12 gains a documented/sealed partial input.** A `sealed partial` type with
   documentation on one part and none on the other must extract to one logical
   node without exception (after P-DEF-02) or must be reported unavailable with
   retained failure state (before it). Plain C12 does not cover F02 or
   partial-method unification.

3. **Manifest validation is strengthened (F04).** The checked oracle must
   validate full UTF-16 start/end coordinates, semantic target, outcome and row
   uniqueness — not only the start line and column width.

4. **Source-byte provenance for fixtures (F03).** Fixtures and measured inputs
   record encoding, line endings, path, raw SHA-256 and Git blob separately;
   normalization never masquerades as equivalence. The existing fixture bytes
   and `.gitattributes` pinning are preserved.

5. **Confirmation gate is exact-identity, not canonical-name (F01).** Positive
   expectations now require proven definition/assembly correspondence. This does
   not change C01–C09/C19 semantics (their targets are genuinely local and bound
   in-context), but it changes what a "confirmed" row *means*.

6. **ADR-CI1-01 is not a matrix change** but removes the separate-facts
   assumption from the acceptance rationale; C13–C18 lifecycle expectations are
   representation-neutral and apply to C and B2 alike.

7. **Coverage expectations are publication-owned (S04).** C13/C16/C17/C18 and
   the coverage sections must consume persisted disjoint outcomes, not live
   counter reconstruction.

## New mandatory adversary inputs (planning amendments)

Added now as explicit planning amendments; executable comparison follows when a
candidate exists.

| Adversary | Required input | Expected |
| --- | --- | --- |
| Canonical-ID collision | Local `System.Console.WriteLine(string)` + unrelated external call | No confirmed local usage; honest external coverage |
| Documented/sealed partial | Two-part `sealed partial` type, documentation on one part | One logical node or honest unavailable; never a crash |
| Duplicate doc ID across projects | Same canonical ID in two projects, one a dependency | Only dependency-context definition confirms |
| Linked file, two contexts | One file linked into two projects, potentially different targets | Distinct identities; no merge |

These join the existing prose adversaries: nested/same-line calls, explicit
interface implementation, shadowed local owners, inaccessible/error signatures,
partial methods, contributor conflicts and proof truncation. All remain
candidate-execution obligations once authorized.

## Owner-context table requirement (S05)

The matrix must eventually freeze a small supported lexical-owner table.
Reviewer-supported state:

- Constructor bodies **may** own ordinary invocations (C05 supporting evidence).
- Lambda ownership is **lexical containment**, not a runtime caller relation.
- Local-function **targets** are deferred; local-function **owners** are not
  automatically collision-free.
- Indexer/event/accessor/initializer contexts are **not qualified** by the
  current helper (`EnclosingCallable` handles methods/local functions/properties)
  and require explicit refusal rules plus a fixture per accepted context.

## Oracle requirements before candidate acceptance

1. The independently reviewed `expected-sites.json` and its target/owner
   evidence remain an oracle input; it is not blessed by implementation output.
2. The checker must reject wrong target, wrong overload, wrong start/end
   coordinate, duplicate row and wrong outcome (R1's oracle does; the producer
   loop does not).
3. Lexical manifest validation is distinct from candidate-result comparison.
   **No candidate exists; no candidate implementation has passed any acceptance
   test.**
4. The 32 selected lexemes are not acceptance-suite completeness.

## R2 amendment (CI-1-SCOPE-R2) — F04 outcome validation

> **Status marker.** This block is appended by CI-1-SCOPE-R2. It does not rewrite
> the R1 text above; it corrects the F04 disposition after independent Q2
> evidence.

**What R1 originally required.** That a manifest oracle validate full UTF-16
start/end coordinates, the intended semantic target, the intended outcome and
row uniqueness (item 2 above).

**What R1 originally claimed was corrected.** Item 2 above asserts, without
qualification, that "R1's oracle does" reject a wrong outcome. The F04 evidence
cell in `findings-matrix.md` repeated that R1's independent oracle "already does"
reject all five corruptions, including a wrong outcome.

**What Q2 disproved.** The cited R1 oracle (Q1 `oracle/Program.cs:143–146`,
copied unchanged into Q2) accepted four outcome mutations, including
`external → confirmed` for the C10 platform call. R1's claim was broader than the
evidence. The oracle validated coordinates, target documentation ID and duplicate
rows, but it validated only two negative outcome strings and used
`outcome.StartsWith("confirmed")` as its positive predicate; it never derived the
outcome from the bound target's eligibility. See
[`../ci1-scope-r2/f04-root-cause.md`](../ci1-scope-r2/f04-root-cause.md).

**The executable R2 correction.** R2 replaces the outcome check with a semantic
oracle (`docs/planning/ci1-scope-r2/analysis/oracle/`) that (a) derives the
occurrence category and outcome class from fixture source and the recorded
compilation/dependency context, (b) requires a `confirmed` target to be an
eligible indexed local definition proven by containing assembly plus source
declaration, (c) enforces an exact declared-outcome vocabulary, and (d) rejects
any unrecognised string. Item 2 above now has an executable, evidence-backed
counterpart rather than an unsupported assertion.

**What the new evidence establishes.** All four Q2 failures are rejected for the
intended semantic reasons; the unchanged 32-row manifest still passes; a no-op
positive control is accepted; and the F01 collision stays `external` even when an
unrelated indexed method shares its documentation ID
(`docs/planning/ci1-scope-r2/evidence/`).

**What remains untested until CI-1A implementation.** Candidate-result
comparison (step 4 of the oracle contract). No candidate `FindUsagesV1` exists.
R2 does **not** claim independent qualification of the corrected checker; that is
Q3's responsibility. F01–F03, S01–S05, P-DEF-01, P-DEF-02 and ADR-CI1-01 remain
open and unchanged.
