# S6.4 re-entry findings and gate matrix

## Findings register

| Finding | Disposition |
| --- | --- |
| F-S64E-01 duplicate physical nodes/edges | **Closed** by owner acceptance and formal remediation closure |
| F-S64E-02 disabled-publication orphans | **Closed** by owner acceptance and formal remediation closure |
| F-S64R-01 manifest ownership misattribution | **Closed** by owner acceptance and formal remediation closure |
| F-S64R-02 unrelated incident-edge deletion | **Closed** by owner acceptance and formal remediation closure |
| F-S64E-03 DF-1 evidence gap | **Open**; D3 campaign not executed |
| F-S64E-04 corpus/oracle preparation | **Partially discharged**; C1/C2/C3/C4 graph corpus and C1 occurrence oracle frozen; C4 two-unit occurrence ledger outstanding; H1 copy policy only |
| F-S64E-05 qualification execution | **Discharged for mirror remediation only**; D3 independent execution outstanding |
| R1-01 C4 occurrence ledger | **Open**; exact `usage4`/target ledger for the two-unit contributor union is not derived |
| R1-02 C1 graph non-invariance | **Recorded**; source-changed replaces six `declares` edges; symbols-core replaces one; symbols-leaf adds one node/two edges. These are expected deltas, not defects |
| R1-03 checkout boundary | **Recorded**; tree-equivalent synthetic merge is accepted only for the remediation qualification; D3 must use an exact-candidate execution route |
| R1-04 H1 historical stores | **Not exercised** in re-entry; copy policy is frozen for implementation |
| R1-05 D3 implementation | **Pending**; D3-01…08 tests, raw receipts, failure/reopen sequences and hosted runs remain |
| R1-06 prior-seal packaging | **Recorded**; both prior payloads contain a pre-seal manifest, so their own seal delta rule fails while content hashes/inputs/trees/binaries verify. Seals unmodified; successor adjudication required |

## Re-entry gate matrix

| Gate | Requirement | Result |
| --- | --- | --- |
| R-G1 | Owner acceptance and remediation closure records with exact identities | **Pass** |
| R-G2 | Producer and qualification seals unchanged; content/inputs/trees verified | **Pass with R1-06 packaging note** |
| R-G3 | Original S6.4 contract, D3 obligations, findings and gates recovered | **Pass** |
| R-G4 | C1 corpus frozen; graph and occurrence ledgers derived independently | **Pass** |
| R-G5 | C2 corpus frozen; graph IDs exactly match accepted continuity census | **Pass** |
| R-G6 | C3 linked-context corpus and contributor-removal delta | **Pass** |
| R-G7 | C4 two-unit contributor corpus: graph ledger / exact occurrence ledger | **Pass / Open** |
| R-G8 | Variant expected deltas frozen (source, symbols, reference loss, deletions) | **Pass** |
| R-G9 | Positive and negative controls defined and validated | **Pass** |
| R-G10 | DF-1 obligation reconciliation without inferred closure | **Pass** |
| R-G11 | Self-excluding sealed S6.4 re-entry evidence | **Pass** (after seal) |
| R-G12 | Proposed implementation commission limited to remaining requirements | **Pass** |

## Disposition

**B — S6.4 RE-ENTRY SUPPORTED WITH OUTSTANDING REQUIREMENTS.**

The corpus, graph oracle, C1 occurrence oracle, variants and controls are ready.
The exact C4 occurrence-contributor ledger and the D3 campaign implementation
remain. No mandatory remediation gate is reopened; the outstanding items are
new S6.4 work.
