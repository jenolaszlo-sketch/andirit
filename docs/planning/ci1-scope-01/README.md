# CI-1-SCOPE-01: C# usages and change impact

Planning and investigation only. Recorded 2026-10-30 (Asia/Manila).
**CI-1-SCOPE-01 READY WITH QUALIFICATIONS.**
Next gate: independent CI-1-SCOPE-Q1, then explicit owner decision.
CI-1 implementation is not approved.

The practical question is: "I need to change this method. Show me every usage
you can actually prove, where it occurs, and what I should inspect next."

Recommend CI-1A: publication-bound occurrence discovery for supported C# method
invocations, with direct caller grouping. CI-1B indirect impact and additional
usage categories remain separately authorizable. Retaining aggregate edges plus
separate source-owned occurrences is a recommendation, not an accepted decision.

## Git and authority

Repository jenolaszlo-sketch/andirit; canonical checkout D:/projects/Andirit.
Starting HEAD/main/origin/main and remotely advertised main:
cd2b7fb5d73f4dc462b0ef7fcadfd75a91c118a2.
Starting tree: af9202904f8dbf0f696c21b6893b5184fa3b5423.
Initial directory clean; only canonical worktree; no linked worktree created.
See [Git evidence](evidence/git-binding.txt) and [handoff](final-handoff.md).

The supplied milestone request controls authorization. Production source, tests,
schemas, package versions and historical evidence remain unchanged.
The only existing-file edit is current-status language in
[the agent roadmap](../../agent-code-intelligence-roadmap.md).
[EC6D acceptance](../../decisions/ec6d-owner-02-acceptance.md) closes EC-6D with
its recorded qualifications. This packet does not reopen it or transfer
TypeScript-specific semantics into C#.

## Evidence index

| Document | Purpose |
| --- | --- |
| [existing-capability-inventory](existing-capability-inventory.md) | Actual extraction, identities, queries and storage |
| [occurrence-representation-analysis](occurrence-representation-analysis.md) | Reproduction, root cause, alternatives and lifecycle |
| [consumer-scenarios](consumer-scenarios.md) | Practical questions and pinned real-source comparison |
| [scope-proposal](scope-proposal.md) | Selected minimum slice and category matrix |
| [query-contract-proposal](query-contract-proposal.md) | Versioned requests/results, bounds and proposed JSON |
| [acceptance-contract](acceptance-contract.md) | C01-C20 and independent qualification |
| [performance-evaluation-plan](performance-evaluation-plan.md) | Measured baseline and future comparison |
| [architecture-decisions](architecture-decisions.md) | Five recommended ADR choices and owner gates |
| [implementation-sequence](implementation-sequence.md) | Separately authorizable delivery steps |
| [final-handoff](final-handoff.md) | Git, qualifications, inventory and next gate |

Machine-readable evidence:
[fixture](evidence/fixture-baseline.json),
[real core](evidence/real-baseline.json),
[source blobs](evidence/source-binding.json),
[retrieval workflow](evidence/workflow-baseline.json),
[wider extraction failure](evidence/wider-source-failure.txt).
Existing regressions: [C# 47/0](evidence/csharp-regression.txt),
[core selection 20/0](evidence/core-regression.txt),
[durable selection 6/0](evidence/lattice-regression.txt).
They verify the baseline, not the proposed feature.

## Reproduce from canonical root

~~~powershell
dotnet restore docs/planning/ci1-scope-01/analysis/Baseline.csproj --ignore-failed-sources -p:NuGetAudit=false --source C:\Users\Laszlos\.nuget\packages
dotnet run --no-restore -c Release --project docs/planning/ci1-scope-01/analysis/Baseline.csproj -- D:\projects\Andirit fixture
dotnet run --no-build --no-restore -c Release --project docs/planning/ci1-scope-01/analysis/Baseline.csproj -- D:\projects\Andirit real Andirit.Abstractions,Andirit
& docs/planning/ci1-scope-01/analysis/measure-baseline.ps1
~~~

The observation harness calls public plugin sessions, validates batches through
the public sink, atomically completes publication, then runs bound public queries.
It is not a prototype FindUsages implementation and does not time the complete
indexing-service lifecycle. Only evidence outputs and ignored build products
are written. Preserve reviewed evidence before rerunning.

Real inputs are an explicit 47-file subset. Their raw bytes are checked against
pinned Git blobs. The separately written Roslyn walk adds the seven standard SDK
implicit global usings for real sources; the plugin does not add those trees.
This stronger oracle context is recorded, not a same-context recall measurement.

## Qualifications

Three-call collapse is reproduced, including an eight-call real example.
The wider three-project attempt fails in existing AddSymbol diagnostic formatting;
its underlying inconsistent-symbol trigger is unqualified. The two-project
subset also has unresolved coverage. The oracle was authored by this packet's
author and uses the same compiler library; CI-1-SCOPE-Q1 must challenge it.
No candidate query, persistence migration, token savings, complete agent task
timing or production implementation is claimed.
