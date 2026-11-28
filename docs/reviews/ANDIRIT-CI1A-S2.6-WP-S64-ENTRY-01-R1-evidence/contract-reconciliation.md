# Original S6.4 contract reconciliation

## Governing sources recovered

- Owner D.3 decision and CI1A Stage-2 acceptance.
- CI1A specification §6.1/6.2 and acceptance matrix §3.
- Stage-2 acceptance matrix §8 and M8.1–M8.4.
- S2.5 qualification matrix and the W fixture receipt.
- Obligation register (DF-1/F-S64E-03) and Stage-6 entry sequence.
- Original S6.4 entry assessment, traceability matrix and qualification plan.

## Requirement status

| Requirement | Status after re-entry |
| --- | --- |
| Source facts remain in occurrence storage; legacy graph surfaces unchanged | **Previously accepted**; remediation preserves it |
| Real published units, real query outputs, unfiltered traversal, raw mirror observations | **Remediation prerequisites closed**; D3 execution pending |
| Exact graph observations at initial publication, replacement, deletion, reopen, failed publication | **Expected ledgers prepared**; execution pending |
| Nonempty callers/references with multiple results and overload separation | **C1/C2 ledgers prepared**; execution pending |
| Contributor union and retirement | **Graph C3/C4 prepared; C4 occurrence ledger open** |
| Native graph reads and raw mirror census/endpoint mapping | **Expected mirror cardinality prepared**; execution pending |
| Genuine hosted Linux exact-candidate execution | **Not executed**; required |
| Independent reproduction against production storage | **Not executed**; successor qualification required |
| DF-1 closure | **Open** |
| S6.4 closure | **Open** |

## Findings lineage

| Finding | Status |
| --- | --- |
| F-S64E-01 | Closed by remediation acceptance |
| F-S64E-02 | Closed by remediation acceptance |
| F-S64R-01 | Closed by remediation acceptance |
| F-S64R-02 | Closed by remediation acceptance |
| F-S64E-03 / DF-1 | Open; D3 campaign and independent replay required |
| F-S64E-04 | Partially discharged; C4 occurrence ledger and H1 execution outstanding |
| F-S64E-05 | Discharged for remediation only; D3 execution outstanding |

## Gate lineage

The remediation gate matrix G-1…G-21 is accepted and not reopened. The re-entry
matrix R-G1…R-G12 records the current preparation state; R-G7's occurrence half
is open. D3-01…08 remain implementation gates for the successor.
