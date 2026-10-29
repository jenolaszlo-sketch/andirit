# EC-6D planning: EC-6C integration readiness

**Owner-01 update:** [Planning acceptance](../../decisions/ec6d-owner-01-contract-acceptance.md)
is complete with qualifications against corrected contract commit
`cc65338f50e585d78c3a1fa8e06d02c184a10451` / tree
`9940965196b35488258501e9513ec1d8025d4007`. R2 prerequisite items 1–4
(contract disposition, ADR-1/2, narrow authority, Q3 verification) are
satisfied for planning. Item 6, R16, remains open before implementation
authorization. The owner-directed sequence is now HETU-INT-01, then
ANDIRIT-RENAME-01, then EC6D-IMPL-01 on the accepted renamed baseline;
the earlier observation that implementation did not technically require
integration first is not authorization to bypass this sequence. Integration,
implementation, migration and release still need separate authorization.
Use the [immutable handoff](owner-01-integration-handoff.md); all earlier
qualification and compatibility requirements below remain in force.

**Disposition: READY WITH QUALIFICATIONS**

This is a readiness assessment only. It does not authorize integration. The
owner's EC6C-OWNER-01 decision explicitly withholds integration, release,
EC-6D, and Track B authorization.

## Git binding and ancestry

| Role | Commit | Tree | Finding |
| --- | --- | --- | --- |
| Canonical main | 558f8ac82ac1b5e07da68f4a71a70240eaf38409 | 08a01a1b7d5d3109b991bcd05d57272fd3f5f018 | Clean, preserved baseline |
| Accepted EC-6A decision | 1d11b7330e1d8755d5c3e3b4a830167bcf8b5bf6 | 649b3111a273478547d8542770220e043fd5cbd5 | Accepted architecture reconciliation |
| EC-6B owner acceptance | 038295f26ce76a8747e923f9e0ef971e773c166e | 8779e00d57bd0b40a3ff89d16ca771e79cc950bf | EC-6B accepted with qualifications |
| EC-6C.2 implementation | 3ab1f144e31155cf3cd4bac87c4739f0a6eccb7a | d8e763a69dadd957df32c872733be5015593c7d6 | Production implementation |
| EC-6C independent qualification | 54ca7156dbc2354843899a619c05128615e3d806 | 3cd18371f9a4b48fd515eb9c79d582fd8ef08851 | Independent report and one qualification-only test; zero production changes |
| EC-6C owner acceptance | 2c3090cba6d8a56102a5c47ceb026c5b5fea3234 | ece772f1f2b2497891f4019d0d85f4d852b197e1 | Planning baseline; accepted with qualifications |
| EC-6D evidence commit | 1c9f4c9f12e82fe35261096ff3c96343336bdb63 | 31486cdea39dee66d1552a4e4af058a59dc4698e | Parent is the EC-6C owner acceptance |

The owner-accepted EC-6C commit is a descendant of canonical main. The
candidate history is linear through EC-6A, EC-6B, EC-6C implementation and
owner acceptance. The independent EC-6C qualification branch is preserved
separately; its production and test changes were confirmed to be
qualification-only. A merge-tree comparison of main and the EC-6C owner tip
reported the owner tree without conflicts. The accepted implementation and
owner-acceptance trees contain identical production files: the owner decision
adds documentation and evidence only.

The original canonical-main worktree was clean at its recorded HEAD. The EC-6C
implementation, qualification, and owner-acceptance worktrees were also
verified clean at their recorded commits before this planning work. No
historical worktree or branch was written by this task.

## Delta and compatibility

Main to the owner-accepted tip is 65 files: 12 source files, 12 test files,
38 documentation files, and three root-level project documents. The source
delta is limited to shared graph vocabulary and Tree-sitter ECMAScript
extraction, module resolution, and export publication. There is no change to
the C# Roslyn provider, core query service, LatticeDB provider, or an explicit
store-schema migration file. New graph vocabulary and producer facts are
additive, but still require durable-store migration validation at integration.
The complete changed-path manifest is in
[integration-delta.md](integration-delta.md).

There is no merge conflict or source-level incompatibility demonstrated by
the reviewed history. The production delta is exactly the one qualified for
EC-6C.2. EC-6B acceptance separately qualifies the p6-to-p8 history-preserving
module-resolution migration; EC-6C independently qualifies p8-to-p9 export
publication. Those reports establish a staged path from main's p6 profile to
the candidate's p9 profile. They do not prove every deployment's direct
p6-to-p9 operational procedure.

## Dependencies and status

| Dependency | Status | Integration relevance |
| --- | --- | --- |
| EC-6A cross-file resolution architecture reconciliation | Accepted | Design basis |
| EC-6B relative module resolution | Accepted with qualifications | p8 module-file facts and p6-to-p8 migration |
| EC-6C.1 export-shape inventory | Accepted with qualifications | Narrow syntax allowlist; no binding |
| EC-6C.2 structural export publication | Accepted with qualifications | p9 positive-only export entries |
| EC-6C owner closure | Accepted with qualifications | Explicitly withholds integration and later milestone authority |

The accepted public surface remains limited. EC-6B resolves selected relative
module references to files. EC-6C records supported public export names as
structural facts. Neither binds an imported name to an exported declaration.

## Unmerged work and preservation

The local refs contain the EC-6A/B/C decision, implementation, design, and
review history, plus preserved EC-3/EC-4/EC-5 reviews and EC-3 heritage
remediation. The latter are unrelated review or preservation branches and
must not be swept into an integration candidate. The EC-6A/B/C prerequisite
history is the relevant dependency chain. The feature LatticeDB work is
already in main. No unrelated milestone implementation is needed for this
candidate.

## Retained qualifications and risks

- The export inventory is positive-only; missing entries do not establish
  absence or completeness.
- The accepted syntax allowlist is narrow. Inline JavaScript type-like
  recovery may emit a conservative structural occurrence and does not prove
  JavaScript runtime validity or complete TypeScript semantics.
- The old migration hash recorded in earlier evidence remains unreproducible.
  Committed tooling and a fresh p8-to-p9 comparison support the substantive
  migration result; the old hash itself remains a limitation.
- The independent EC-6C qualification was on Windows with .NET SDK 10.0.401.
  Its LatticeDB claims are through the public store contract.
- The qualification uses small fixtures and a pinned website smoke, not a
  performance or broad-repository claim.
- The root README support table predates EC-6C's accepted export publication
  details. Documentation synchronization belongs in the integration review.

The full list and source links are in [risk-register.md](risk-register.md).
This planning branch independently ran the four test projects against the
owner-accepted tree: 388 passed, 0 failed, 0 skipped. They completed as
separate invocations; a single solution-level run was not completed in this
environment.

## Required owner authorization before integration

Before any merge, cherry-pick, or deployment, the owner must authorize in
writing:

1. The exact integration candidate commit and tree, including the EC-6A/B/C
   dependency history and EC-6C owner-accepted documentation.
2. The target branch and permitted integration method.
3. The p6-to-p9 migration rehearsal on a representative durable publication,
   including unchanged-fact comparison, added export facts, deletion/reindex,
   close/reopen, and rollback behavior.
4. Supported deployment platforms and whether the existing Windows-only
   qualification is sufficient for the intended release.
5. The integration and compatibility checks required for consumers, including
   the root README/documentation update.
6. Any separate authorization for package publication or release. This
   readiness report grants none.

Until those decisions exist, the candidate remains unintegrated. EC-6D
planning and its proposed scope do not change that boundary.

This readiness assessment does not establish production readiness.

---

## R1 revision (EC6D-PLAN-R1): separated prerequisites for EC-6D

The EC-6C dependency facts above are unchanged and remain authoritative.
This section restates them as explicit prerequisite gates for EC-6D and
separates implementation, integration, and release readiness. None of these
gates has been executed by R1; none is authorized by this packet.

### Evidence base (retained qualifications that EC-6D inherits)

- EC-6B qualifies p6→p8 history-preserving module-resolution migration;
  EC-6C independently qualifies p8→p9 export publication (fresh reproduction
  with committed tooling; the historical migration hash remains
  unreproducible and stays a recorded limitation, QN-1/FQ1-EV-1).
- Independent EC-6C qualification ran on Windows, .NET SDK 10.0.401, through
  public store contracts, on small fixtures and the pinned website smoke.
  No cross-platform (Linux/macOS) qualification exists; no performance
  qualification exists (G7 NOT EVALUATED).
- A direct p6→p9 operational migration has **not** been rehearsed as one
  procedure; the staged p6→p8→p9 evidence covers the components.

### Prerequisites for EC-6D implementation (before any production work)

1. Owner approval of the R1 contracts:
   [symbol-binding-contract.md](symbol-binding-contract.md) (fact contracts
   A and B, binding rule, evidence identity),
   [consumer-query-contract.md](consumer-query-contract.md)
   (`GetInterfaceImpactV1`),
   [publication-consistency-contract.md](publication-consistency-contract.md)
   (qualified-results policy), and the revised
   [scope-proposal.md](scope-proposal.md) / acceptance matrix.
2. Owner decision on the representation ADR gate (occurrence/attribution
   representation; query-time binding join vs extraction-time binding edges;
   provider-version and lifecycle contract).
3. The EC-6C owner-accepted baseline (2c3090c, p9) used as the working base —
   EC-6D implementation does not require EC-6C to be merged first.
4. A Q2 scope-review acceptance of this packet.

### Prerequisites for EC-6D integration (before entering canonical main)

5. EC-6C (and the EC-6B chain) integrated into canonical main by separate
   owner authorization; merge order matters because EC-6D facts assume the
   p9 extraction, `exports1`, and `resolver2` provider versions.
6. A p9-to-new-profile durable migration rehearsal repeating the accepted
   p8→p9 pattern (unchanged-fact comparison, added facts, deletion/reindex,
   close/reopen, rollback) on both stores; EC-6D's profile/provider bumps
   invalidate prior indexes exactly as p9 did.
7. A19/A20 compatibility demonstrated: pre-existing p9 facts
   byte-identical for unchanged sources; existing queries and presets
   unmodified.

### Prerequisites for release / production-readiness claims

8. EC-6E-class durability, incremental invalidation, and resource-bound
   qualification (recorded deferral) — explicitly out of EC-6D's scope.
9. Cross-platform qualification if any non-Windows deployment is intended
   (today's evidence is Windows-only).
10. A representative-repository performance baseline before any performance
    claim (no targets may be set from the thirteen-file fixture).
11. Documentation synchronization (root README support table predates p9) —
    integration-review item, unchanged.

### Authorization boundaries preserved by R1

EC6C-OWNER-01 explicitly withholds integration, release, EC-6D, and Track B
authorization. This R1 packet is planning and remediation only: it
implements nothing, changes no production schema, executes no migration
rehearsal, and performs no integration. Every gate above requires its own
written owner decision.

## R2 revision (EC6D-PLAN-R2): updated implementation prerequisites

The Q2 review (reconciled register Q2-01..Q2-08) authenticated the recovery
and required targeted contract repairs, delivered by R2. The prerequisite
list for EC-6D implementation is updated as follows; integration and
release gates are unchanged.

### Prerequisites for EC-6D implementation (before any production work)

1. Owner approval of the R2 contracts:
   [symbol-binding-contract.md](symbol-binding-contract.md) (facts A–E;
   lexical-binding rule; physical-declaration cardinality; witnesses),
   [consumer-query-contract.md](consumer-query-contract.md) (entry-driven
   discovery; closed reasons with reachability; coverage),
   [publication-consistency-contract.md](publication-consistency-contract.md)
   (truth table; integrity failures; consumption), and the R2
   [acceptance-contract.md](acceptance-contract.md) matrix.
2. Owner decisions on the two prepared proposals:
   [representation-decision-proposal.md](representation-decision-proposal.md)
   (ADR-1; query-time joins recommended) and
   [publication-policy-decision-proposal.md](publication-policy-decision-proposal.md)
   (ADR-2; qualified results with safeguards recommended). These decisions
   are correctness-relevant and are gated before implementation; no
   implementation authorization may be inferred while either is unresolved.
3. **Narrow attribution authority:** explicit written owner authorization of
   the narrow export-attribution and one-hop relevance subset (physical
   declarations, export aliases, barrier reporting), recorded against the
   prior Track B deferral. This does not authorize general Track B.
4. Targeted independent re-review (Q3) of the repaired contracts —
   binding algorithm, candidate/coverage rules, publication truth table and
   the corrected acceptance rows — before owner acceptance binds to a
   repaired commit/tree.
5. The EC-6C owner-accepted baseline (`2c3090c`, p9) used as the working
   base — EC-6D implementation does not require EC-6C to be merged first.
6. Intended-consumer confirmation of the bounded workflow value (R16 of the
   risk register) at or before implementation authorization.

Items 5–11 of the R1 prerequisite lists (integration, migration rehearsal,
A19/A20 compatibility, durability/incremental qualification, cross-platform
qualification, performance baseline, documentation synchronization) remain
in force unchanged. R2 performed no implementation, integration, migration,
publication or release action, and preserves all R1/R2/Q1/Q2 evidence.
