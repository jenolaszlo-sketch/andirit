# C4 occurrence identity blocker (R1-01)

## Result

The exact C4 two-unit occurrence ledger **cannot be independently derived from
the documented identity contract as frozen**. The graph ledger and contributor
topology are valid, but production occurrence target identities do not match a
`compctx1`-based oracle.

## Observed

| Artifact | Target identity |
| --- | --- |
| Independent oracle, Release options, `compctx1` | `asmctx3:sha256:4f4afa280a38404b03bf240696186a9337c80bc6341b515733de6d91ec64eb29` |
| Independent oracle, Debug options, `compctx1` | `asmctx3:sha256:4454f095debfb08aece9201f9ef6087ff05e6e3bc276343e2e46626b253491e7` |
| Production plugin, test-host process | `asmctx3:sha256:1844c0e5911fa8d253c148d93687501af51bffea3657b306268c88808065e69e` |
| Production plugin, earlier console host | `asmctx3:sha256:b3cb5e361ffe0fb29d912767dcc5b6562776c7c9d8e43ff43f8e735bbc140ddb` |

Production published **51** occurrence facts with **two contributors**, matching
the oracle cardinality and contributor union. Only the target identity differs.

## Cause

The production verified descriptor observed by the campaign is
`compctx2:sha256:651e7bac…`, not the `compctx1` descriptor used by the target
identity contract. Its reference list contains:

- absolute runtime paths such as
  `file|C:/Program Files/dotnet/shared/Microsoft.NETCore.App/10.0.12/...`;
- test-host assemblies (`testhost.dll`, `xunit.*`, `Microsoft.TestPlatform.*`)
  when run in the test process;
- the test output's Andirit assemblies.

The frozen corpus specification instead requires **host-independent,
content-bound** reference identities (`file|runtime/<file>|sha256:<hash>`).
Because the production target context includes process-specific references and
absolute paths, exact target/usage IDs vary by host/process. The earlier console
host and the test host produced different targets for the same source and
plugin.

`CodeOccurrenceIdentity.ComputeTargetId` expects a canonical `compctx1`
compilation context. The production descriptor is `compctx2`. Passing the
production descriptor to the frozen formula is not contract-compatible, and
using the observed production target as expected would derive the oracle from
the candidate, which the commission forbids.

## Reproduced by

`C4OccurrenceBlockerTests.cs` (preserved under `blocker/`) publishes the frozen
C4 corpus through the production plugin, binds the verified authority, and
queries the frozen target. It finds 51 facts but zero for the oracle target.
`production-descriptor.txt` records the production descriptor and reference set.

## Impact

- **R1-01 is blocked**, not failed by an implementation defect.
- D3-01/02/05/06 exact occurrence comparisons are gated.
- Graph-only and raw-mirror cases are not affected by this blocker, but the D3
  campaign was not executed in this commission.
- DF-1 remains open.

## Required owner decision

One of:

1. Reconcile the occurrence identity contract so production and the independent
   oracle use the same host-independent `compctx1` reference identities; or
2. Authorize a frozen host-bound oracle derived from a declared environment
   manifest (not from candidate output), and accept host-scoped target IDs; or
3. Explicitly defer exact occurrence target identities and qualify only
   relational/cardinality semantics, which changes the binding contract.

The producer did not adapt the oracle to observed production output and did not
substitute candidate-derived expectations.
