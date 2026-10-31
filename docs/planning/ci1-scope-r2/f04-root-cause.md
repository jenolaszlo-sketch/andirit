# F04 root cause — why the previous checker accepted incorrect outcomes

Disposition input: **CI-1-SCOPE-Q2** recorded `Q2-F04-01 / P2`: the cited
independent oracle accepted four invalid outcome mutations, including promoting
the external `System.Console.WriteLine` call to `confirmed`. R1's claim that the
oracle rejects wrong outcomes was broader than the evidence supports.

This document explains the mechanism. It does not rewrite the Q2 finding; it is
the R2 analysis that motivates the correction in
[`analysis/oracle/Program.cs`](analysis/oracle/Program.cs).

## 1. What the prior checker actually validated

The cited checker is the Q1 reviewer oracle (`oracle/Program.cs`), copied
unchanged into Q2. Its outcome-related code is four statements
(`Program.cs:143–146` in the Q2 copy):

```csharp
var positive=outcome.StartsWith("confirmed")||k=="C20";
if(positive && (target is not IMethodSymbol || node is not InvocationExpressionSyntax))errors.Add("positive must be bound method invocation");
if(outcome=="ambiguous-no-confirmation"&&candidateDocs.Length!=2)errors.Add("outcome mismatch");
if(outcome=="unresolved-no-confirmation"&&target is not null)errors.Add("outcome mismatch");
```

Target identity was checked separately (`Program.cs:139–141`) by comparing the
bound symbol's **documentation-comment ID** to a fixed signature→ID table:

```csharp
else if(!hardTargets.TryGetValue(expected,out var expectedDoc)||doc!=expectedDoc)errors.Add("semantic target mismatch");
```

Coordinate/lexeme validation and duplicate detection were present and correct.

## 2. The defect

The declared `ExpectedOutcome` string was used **directly as the specification of
what to check**, rather than being **compared against an outcome derived from
compiler evidence**. Three consequences follow.

1. **Prefix-gated positive check.** A positive was defined as
   `outcome.StartsWith("confirmed")`. For any other string the positive predicate
   was skipped entirely. There was no exhaustively recognised outcome vocabulary,
   so an arbitrary unknown string (`invalid-q2-outcome`) was silently accepted.

2. **Only two negative strings were checked.** `ambiguous-no-confirmation` and
   `unresolved-no-confirmation` were the only negative outcomes that produced an
   error. Every other negative or deferred string
   (`external-no-confirmed-local-target`, `deferred-method-group`, …) bypassed
   outcome validation completely.

3. **"Is a bound method invocation" is not "is an eligible indexed local
   definition".** The positive check only required `target is IMethodSymbol` and
   the node to be an invocation. It never verified that the bound target belongs
   to an **eligible indexed definition in the recorded compilation/dependency
   context**. The platform `System.Console.WriteLine` is a perfectly bound
   `IMethodSymbol`, so relabelling the C10 external call as `confirmed` still
   satisfied the predicate. Target identity was checked only by documentation ID,
   which does not encode assembly/definition identity.

## 3. Exactly why each of the four Q2 mutations passed

| Q2 mutation | Declared value under mutation | Why the prior checker accepted it |
| --- | --- | --- |
| `outcome-external-to-confirmed` | C10 `ExpectedOutcome` = `confirmed` | `positive` is true; `System.Console.WriteLine` binds to an `IMethodSymbol` on an `InvocationExpressionSyntax`, so the only positive predicate is satisfied. Eligibility is never checked. |
| `outcome-confirmed-to-external` | C01 row 0 `ExpectedOutcome` = `external-no-confirmed-local-target` | `positive` is false; the two negative checks do not match this string; no outcome validation runs. |
| `outcome-confirmed-to-deferred` | C01 row 0 `ExpectedOutcome` = `deferred-method-group` | Same: not a recognised negative, so no outcome validation runs. |
| `outcome-invalid-enum` | C01 row 0 `ExpectedOutcome` = `invalid-q2-outcome` | Not a recognised outcome at all; there is no vocabulary check, so the string is inert. |

The reproduced prior-checker result is preserved in
[`evidence/before/before-run.json`](evidence/before/before-run.json) and is the
subject of [`mutation-matrix.md`](mutation-matrix.md).

## 4. Why this matters beyond the four strings

The bug is structural, not a typo. Because the expected string *is* the
specification, any future authoring mistake in an outcome field is invisible: the
oracle cannot distinguish a genuine semantic outcome from a wrong one. That is
exactly the failure F01 warned about — a coincident identifier is treated as
proof of a local target. R1's exact-identity gate was added for targets but not
carried through to outcomes.

Special-casing the four strings would fix the symptom without establishing
general outcome validation. R2 instead derives the outcome class from the
compiler and compares, and rejects any string outside a frozen vocabulary.

## 5. Bounded scope

- This is a **planning-evidence** defect. It changes no production source, test,
  schema, package or workflow.
- The 32 authored manifest rows are correct; R2 does not weaken or rewrite them.
- The production defects P-DEF-01 (false C# semantic confirmation) and P-DEF-02
  (documented partial-type extraction) are untouched and remain open.
- Candidate-result comparison remains unexecuted: no CI-1A implementation exists.
