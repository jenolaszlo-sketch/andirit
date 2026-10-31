# ANDIRIT-CSharp-CORRECTNESS-01 — production remediation packet

Bounded production remediation of the two existing C# extractor defects
P-DEF-01 and P-DEF-02, named by the independent Q1 review and recorded in
[`ci1-scope-r1/prerequisite-defects.md`](../ci1-scope-r1/prerequisite-defects.md).
Both defects predate CI-1. CI-1A implementation, occurrence storage, query API
changes and package publication remain out of scope and were not touched.

**Disposition: `CSHARP-CORRECTNESS-01 ACCEPTED AND CLOSED`** (owner
acceptance with qualifications recorded in
[`docs/decisions/csharp-correctness-owner-01-acceptance.md`](../../decisions/csharp-correctness-owner-01-acceptance.md)
after the independent `CSHARP-CORRECTNESS-Q1` review returned `SUPPORTED WITH
QUALIFICATIONS`. Local commits only; not pushed, not tagged, not released.)

## Bindings

| Item | Value |
| --- | --- |
| Repository | `jenolaszlo-sketch/andirit`, canonical checkout `D:\projects\Andirit` |
| Baseline commit / tree | `5c89ac0b3d0e87c0112880cf985826ded42b21b2` / `e61994289e5f96203ded37be000b0ed3bb3b79a2` (clean, `main` = `origin/main`) |
| Environment | Windows (win32), .NET SDK `10.0.401`, Roslyn `Microsoft.CodeAnalysis.CSharp 5.9.0` |
| Worktree model | No branches, no linked worktrees; single cohesive sequence on local `main` |
| Push/publication | **Not performed** — awaiting explicit authorization |

## Contents

| Document | Content |
| --- | --- |
| [`defect-01-analysis.md`](defect-01-analysis.md) | P-DEF-01 root cause, correction design, fail-closed rules |
| [`defect-02-analysis.md`](defect-02-analysis.md) | P-DEF-02 two-layer root cause, deterministic policy |
| [`regression-matrix.md`](regression-matrix.md) | All mandatory + adversarial regressions, failing-before → passing-after |
| [`compatibility-assessment.md`](compatibility-assessment.md) | Edge-ID stability, intentionally changed semantic results, coverage deltas |
| [`real-repository-qualification.md`](real-repository-qualification.md) | Real `Andirit`/`Andirit.Abstractions`/`Andirit.CSharp` extraction results |
| [`independent-review-handoff.md`](independent-review-handoff.md) | Gate checklist, exact re-verification commands |
| [`final-handoff.md`](final-handoff.md) | Git bindings, changed-file inventory, disposition |
| [`evidence/`](evidence/) | Raw test output and baseline-vs-current extraction JSON (SHA-256 pinned) |

## One-paragraph summary

`TryResolveTarget` accepted any globally indexed declaration whose canonical
documentation ID matched a bound Roslyn target, so `Z/App.cs`'s platform
`System.Console.WriteLine` call was published as a resolved cross-project call
to the unrelated shadow in `A/Shadow.cs` (P-DEF-01), and `NodesEquivalent`
treated declaration-specific `doc-summary` disagreement between partial parts
as inconsistent symbol state, failing extraction via a diagnostic that cast
`modifiers` (a `CodeTextProperty`) to `CodeTextListProperty`
(P-DEF-02). The fix proves definition/compilation correspondence before any
confirmation, merges declaration-specific documentation deterministically, and
makes the inconsistency diagnostic type-safe. Valid aggregate relationship
identities are byte-stable (`cross` fixture output hash identical before/after
`bf8681ca…`); only false edges are removed and previously suppressed valid
edges are restored. The full suite passes 565/565.
