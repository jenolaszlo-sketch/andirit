# ANDIRIT-CI1A-S2.6-WP-S64-IMPL-01

**S6.4 D3 implementation and producer execution · 28 November 2026**

## B — IMPLEMENTATION SUPPORTED WITH OUTSTANDING REQUIREMENTS

The implementation commission was opened on the accepted re-entry baseline. The
C4 occurrence identity contract proved insufficient for an independent exact
ledger: production uses a `compctx2` descriptor with host-dependent references,
while the frozen oracle contract uses `compctx1` content-bound inputs. The
blocker is reproduced and preserved. No D3 campaign was executed, DF-1 remains
open, and an owner decision is required.

## 1. Baseline and re-entry seal

| Subject | Identity |
| --- | --- |
| Accepted implementation | `ec984bfe63e5fbe13047cee8920e00b634ab17b0` |
| Implementation tree | `8ec0eb7a5e647a171f54fa8969dadbb6c65da179` |
| Producer seal | `870b600acf1490337af9f483f8894a5e8166a90e` |
| Independent qualification seal | `b3951c790768ad257fb3418e5abe41cc5ecc5322` |
| Re-entry payload | `b7d2bf69f53155b6f083a3505a8bcc20ad0bcad7` |
| Re-entry seal | `1776ea8ca12ba54dc85e817484711663480569a2` |
| Re-entry seal SHA-256 | `4f867e531d8dd0c541cff5e83391f680e5fed414b2e9dd06d706a0b4c2b54268` |

The 69-file re-entry seal and `validation.json` (27 checks) verify before
implementation. The C4 graph ledger remains frozen and valid.

## 2. R1-01 blocker

The independent C4 oracle was completed and produced:

- target `asmctx3:sha256:4f4afa…eb29`;
- 51 occurrences: 50 in `Caller`, 1 in `Caller2`;
- two target-scoped contributor units;
- removal variants: 50 (without Caller2), 1 (without Caller);
- caller-change variant: same target, changed usage identity.

Production publication produced 51 facts and the same two contributors, but a
different target:

- test-host run: `asmctx3:sha256:1844c0…e69e`;
- earlier console host: `asmctx3:sha256:b3cb5e…0ddb`.

The production verified descriptor is
`compctx2:sha256:651e7bac…`, with references containing absolute runtime paths
and test-host assemblies. The frozen contract uses `compctx1` and host-independent
content-bound references. `ComputeTargetId` expects `compctx1`; passing
`compctx2` is not contract-compatible, and adopting observed production targets
would derive the oracle from the candidate.

**Status: BLOCKED.** Owner decision required:

1. reconcile production/oracle to the same host-independent `compctx1`
   reference identity; or
2. authorize a frozen host-bound oracle from a declared environment manifest;
   or
3. explicitly defer exact occurrence target IDs and change the binding contract.

No oracle was adapted to candidate behavior. Reproduction files are under
`blocker/`.

## 3. D3 campaign status

D3-01…08 are **not executed** because the exact occurrence identity gate is
blocked. The graph/oracle preparation remains:

- C1 118 nodes / 350 edges; C2 37/97 exact accepted IDs; C3 14/22; C4 42/107;
- frozen source/variant deltas and 27 validation checks;
- C4 graph and contributor topology valid.

Graph-only, raw-mirror and traversal cases are not affected by the occurrence
identity blocker, but no producer campaign was claimed without the full binding
evidence.

## 4. R1-04 and lifecycle campaigns

The H1 historical-store campaign was not executed. Failure, cancellation,
recovery and reopen sequences were not executed on the D3 corpus. The accepted
S6.1–S6.3 contracts and mirror remediation evidence remain intact; no new
production code or tests were changed.

## 5. R1-06 adjudication

The prior producer and qualification payloads contain a pre-seal
`manifest.json`, so their own seal script reports `M` instead of `A`. Content
integrity is established: all 34/70 entries, 7 reviewed inputs, trees, binary
hashes and seal SHA-256 values verify. The mandatory self-excluding delta rule is
violated, however, and the prior seals were not modified.

**Owner adjudication required.** Permitted claim: accepted evidence content is
intact. Not permitted: a claim that the producer sealing procedure passed as
written. The re-entry and implementation seals add only `manifest.json`.

## 6. Regression, hosted execution and deliverables

No production or test code was committed by this commission; the regression
inventory remains the accepted **1,034** tests, including 29 remediation tests
and 19 inherited S6.3 named cases. No new exact-candidate hosted run exists
because no implementation candidate was produced. Package-consumer results from
the accepted baseline remain valid.

Deliverables 1 and 3–14 are **not completed** because of the blocker:
exact graph/occurrence comparisons, raw mirror receipts, H1, failure/reopen,
DF-1 matrix execution, new hosted receipts and the independent qualification
handoff all remain. The sealed implementation evidence records the blocker,
method, reproduction and R1-06 adjudication; it does not claim D3 completion.

## 7. Findings

| Finding | Status |
| --- | --- |
| R1-01 | **Blocked**; occurrence identity contract mismatch |
| R1-04 | Not executed |
| R1-05 | Not executed; D3 campaign pending |
| R1-06 | Adjudicated; owner decision required |
| F-S64E-03 / DF-1 | **Open** |
| F-S64E-04 | **Open**; C4 occurrence ledger blocked |
| F-S64E-05 | Open for D3; remediation-only discharge retained |

## 8. Recommended owner decision and successor

1. Decide the occurrence identity contract (three options in §2).
2. Keep DF-1, F-S64E-03 and F-S64E-04 open; do not close S6.4.
3. If the contract is reconciled, re-authorize a bounded implementation
   continuation for C4 ledger, D3-01…08, H1, failure/reopen and exact-candidate
   Windows/Linux runs.
4. Only after producer completion, commission
   `ANDIRIT-CI1A-S2.6-WP-S64-QUAL-01` for independent replay.

No production change, dependency upgrade, release, merge, existing seal
modification or S6.4 closure was performed.
