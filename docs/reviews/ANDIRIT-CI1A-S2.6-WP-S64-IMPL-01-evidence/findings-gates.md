# Implementation findings and gate matrix

## Findings

| Finding | Status |
| --- | --- |
| R1-01 C4 occurrence ledger | **BLOCKED**; production `compctx2` host-dependent references do not match the frozen `compctx1` contract. Owner decision required |
| R1-02 C1 graph non-invariance | Expected deltas recorded in re-entry; no new defect |
| R1-03 checkout boundary | Remains remediation-specific; no new checkout-equivalence claim |
| R1-04 H1 historical stores | **Not executed** |
| R1-05 D3 implementation | **Not executed**; exact occurrence gate blocked; graph/raw cases still pending |
| R1-06 prior seal packaging | **Adjudicated**: content integrity established, mandatory self-excluding delta rule violated, owner decision required |
| F-S64E-03 / DF-1 | **Open** |
| F-S64E-04 | **Open**; C4 occurrence ledger blocked |
| F-S64E-05 | **Open** for D3; remediation-only discharge retained |

## Gate matrix

| Gate | Requirement | Result |
| --- | --- | --- |
| I-G1 | Exact baseline and 69-file re-entry seal verified | **Pass** |
| I-G2 | Independently derive C4 target/usage/contributor ledger | **Blocked** (contract mismatch) |
| I-G3 | Compare C4 oracle to production without candidate-derived expectations | **Not executed** (G2 blocked) |
| I-G4 | Implement and execute D3-01…08 | **Not executed** |
| I-G5 | Raw mirror and native traversal receipts | **Not executed** |
| I-G6 | H1 historical-store campaign | **Not executed** |
| I-G7 | Failure/cancellation/recovery/reopen campaign | **Not executed on corpus** |
| I-G8 | R1-06 adjudication without modifying prior seals | **Pass, owner decision required** |
| I-G9 | Regression inventory and named cases | **Unchanged 1,034**; no committed code/test change, no new hosted candidate |
| I-G10 | Sealed producer implementation evidence, clean-checkout verify | **Pass after seal** |
| I-G11 | Proposed independent qualification commission | **Pass** |

## Disposition

**B — IMPLEMENTATION SUPPORTED WITH OUTSTANDING REQUIREMENTS.**

The C4 occurrence identity blocker is a mandatory owner decision. No D3 campaign
may be claimed complete, and DF-1/S6.4 remain open.
