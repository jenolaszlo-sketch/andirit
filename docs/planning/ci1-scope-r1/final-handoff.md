# Final handoff

**CI-1-SCOPE-R1 READY WITH QUALIFICATIONS.**
Planning and contract remediation only. No owner acceptance and no
implementation authorization is issued.

## Git and source binding

| Item | Verified identity |
| --- | --- |
| Repository | `jenolaszlo-sketch/andirit` |
| Canonical checkout / branch | `D:/projects/Andirit` / `main` |
| Starting commit (reviewed Q1 packet) | `7ab0aa7086861a368d91a79b802d538c8c206bb9` |
| Starting tree | `ffb6833e74836e2a318ab3c17e39a9c748310268` |
| Planning content predecessor | `69d5152cb3ac1f9699e82981e11c122f261ada16` |
| Production baseline | `cd2b7fb5d73f4dc462b0ef7fcadfd75a91c118a2` |
| Working tree before R1 | clean |
| Linked worktrees | none |

The R1 documentation commit and tree are resolved externally by:
`git log -1 --format="%H %T" -- docs/planning/ci1-scope-r1/final-handoff.md`.

Changed paths are confined to `docs/planning/ci1-scope-r1/**` plus
`R1 amendment` blocks inside `docs/planning/ci1-scope-01/**`. No production
source, existing test, schema, package, workflow or accepted historical artifact
is changed. Q1 evidence and the original packet's raw observations are preserved.

## Disposition

**CI-1-SCOPE-R1 READY WITH QUALIFICATIONS.**

The Q1 `REMEDIATION REQUIRED` findings are reconstructed, independently
reproduced and corrected at the planning level. The packet is independently
reviewable. It is "with qualifications" because two confirmed existing C#
production defects and one open architecture decision remain, and none is
resolved by planning alone.

## The two existing C# defects requiring a separate production milestone

1. **F01 / P-DEF-01 — canonical-ID false confirmation.** A uniquely-keyed,
   non-error local declaration sharing a Roslyn documentation ID with an
   external method can receive a resolved call from an unrelated caller. The
   CI-1A strict-identity gate prevents new false confirmations; either a
   new-query resolver boundary (P-DEF-01a) or a bounded provider fix
   (P-DEF-01b) is required. See `prerequisite-defects.md`.
2. **F02 / P-DEF-02 — documented partial-type extraction.** A documented
   `sealed partial` type throws `InvalidCastException` before relationship
   extraction (unsafe diagnostic cast plus declaration-specific documentation
   property disagreement). A two-layer bounded fix or an explicit narrow
   qualification is required.

Neither is authorized or scheduled by this packet. No CI-1 implementation may
silently include either fix.

## Decisions required before implementation

1. **Representation (ADR-CI1-01).** Choose C or B2 after the minimum comparative
   experiment; no default winner. Keep the choice reversible until then.
2. **Correct target proof.** Accept the exact-identity gate and collision corpus;
   choose P-DEF-01a or P-DEF-01b.
3. **Documented partial input.** Authorize P-DEF-02 or explicitly narrow
   qualification and require honest unavailable/failure retention.
4. **Small product.** Approve ordinary invocation sites plus direct lexical-owner
   grouping only; freeze the lexical-owner/refusal table.
5. **Identity and bytes.** Approve publication-bound handles, hash churn,
   linked-context separation, context invalidation and the raw-byte policy.
6. **Query limits.** Freeze exact selection/statuses, processing and reserved
   metadata/byte budgets, proof truncation, completeness universe,
   historical-unavailable behaviour and no-continuation V1; remove mandatory
   future-impact fields.
7. **Persistence/history.** Approve the version/codec route, named authentic
   historical fixtures, snapshot limits, old-reader refusal/reindex policy.
8. **Utility threshold.** Approve a fair paired task protocol and the narrow
   real-repository qualification with explicit expansion conditions.

## Preserved qualifications

- The occurrence-collapse observation is real and independently reproduced.
- The Roslyn oracle is a stronger context (implicit SDK global usings); its
  counts are **not** a same-context precision/recall score.
- The 47-file subset and the failed wider extraction are separate facts.
- No speedup, savings, token reduction, total-cost reduction or break-even is
  established; no candidate query exists.
- The real-repository qualification is deliberately narrow.
- `SymbolUnused` and complete-usage claims remain prohibited.
- No crash/power-loss durability is claimed.

## Publication permission

This packet is committed to `main` as documentation. Pushing remains subject to
explicit authorization; the Q1 producer's push was rejected by automatic
approval review, and no push is attempted or claimed here unless separately
authorized. See the task handoff for the resolved publish status.

## No acceptance issued

This packet does not accept CI-1A scope, does not select a storage
representation, does not authorize prerequisite fixes and does not authorize
release or package publication. Owner acceptance and implementation
authorization remain separate, explicit actions.
