# ANDIRIT-CI1A-S2.6-WP-S64-ENTRY-01-R1

**S6.4 re-entry after mirror remediation · 28 November 2026**

## B — S6.4 RE-ENTRY SUPPORTED WITH OUTSTANDING REQUIREMENTS

The owner acceptance and formal remediation closure are recorded. The original
S6.4 contract is recovered, and the D3 corpus, graph oracle, C1 occurrence
oracle, variants and controls are frozen. The exact C4 two-unit occurrence
ledger and the D3 campaign implementation remain. S6.4 is not closed; DF-1 and
F-S64E-03 remain open.

## 1. Accepted baseline and closure

| Artifact | Identity |
| --- | --- |
| Original entry | `dda4358bc2588368efc6ab6a7bd774857d998026` |
| Accepted implementation | `ec984bfe63e5fbe13047cee8920e00b634ab17b0` |
| Implementation tree | `8ec0eb7a5e647a171f54fa8969dadbb6c65da179` |
| Producer seal | `870b600acf1490337af9f483f8894a5e8166a90e` |
| Independent qualification seal | `b3951c790768ad257fb3418e5abe41cc5ecc5322` |
| Candidate hosted CI | `46035752304` |
| Producer seal hosted CI | `46036959497` |

Owner acceptance:
[ADR-CI1-01-S2.6-WP-S64-MIRROR-REMED-ACCEPTANCE-OWNER-01](../decisions/adr-ci1-01-s2-6-wp-s64-mirror-remed-acceptance-owner-01.md).
Formal closure:
[ANDIRIT-CI1A-S2.6-WP-S64-MIRROR-REMED-CLOSE-01](ANDIRIT-CI1A-S2.6-WP-S64-MIRROR-REMED-CLOSE-01.md).
F-S64E-01, F-S64E-02, F-S64R-01 and F-S64R-02 are closed; all 21 remediation
gates are accepted. Both seals are preserved and verify from Git objects.

## 2. Original S6.4 contract recovered

The governing requirements are recovered from the owner D.3 decision, CI1A
§6.1/6.2, the acceptance matrix §3, Stage-2 matrix §8/M8.1–M8.4, I-02, the
obligation register, and the Stage-6 entry. The D3 obligation requires:

- exact legacy graph node/edge/aggregate identity and cardinality;
- nonempty callers/references and ordered traversal;
- native graph reads and raw mirror census;
- separate occurrence facts with correct target-scoped results;
- initial publication, replacement, deletion, reopen and known-failure events;
- real fixtures, contributor union/retirement and no occurrence leakage.

Reconciliation:

| Status | Requirements |
| --- | --- |
| **Previously accepted** | S6.1 cancellation, S6.2 durability, S6.3 publication correctness, F12 occurrence oracle, S2.5 W slice within its boundary |
| **Discharged by remediation** | F-S64E-01/02 and F-S64R-01/02; raw mirror prerequisites for D3 |
| **Remaining open** | DF-1/F-S64E-03; F-S64E-04 corpus/oracle completion; F-S64E-05 D3 execution |
| **Additional implementation** | D3-01…08 campaign, C4 occurrence ledger, raw receipts, H1 copies, hosted runs |
| **Independent qualification** | `ANDIRIT-CI1A-S2.6-WP-S64-QUAL-01` re-derivation and native replay |

Remediation acceptance is not S6.4 completion.

## 3. Corpus and independent oracle

`corpus-manifest.json` binds 52 source/project files and 14 expected artifacts
by byte length and SHA-256.

| Corpus | Composition | Expected graph |
| --- | --- | --- |
| C1 F12/R2 | 5 projects, 10 project sources, external image | 118 nodes / 350 edges; 53 invocations, 21 references, 5 overload sets |
| C2 W continuity | Caller + Dependency | 37 nodes / 97 edges, **exact ID equality** with the accepted S2.5 census |
| C3 linked precedent | ALib + BLib + App | 14 nodes / 22 edges; two contexts; App calls BLib |
| C4 contributors | C2 + Caller2 | 42 nodes / 107 edges; one target, 11 call edges, two units |

The standalone Roslyn oracle reimplements the documented legacy identity
recipes. It does not call the production extractor. Repeated runs are
byte-identical. `validation.json` records 27 passing checks, including the C2
exact census, C1 source/symbol/reference deltas, C1/C2/C3 deletion sets and C4
contributor topology.

Frozen expected deltas include:

- C1 source-changed: all node IDs stable, six `declares` edges replaced;
- C1 symbols-core: one `declares` replacement;
- C1 symbols-leaf: +1 node / +2 edges;
- C1 reference-missing: −79 edges;
- C1 no-Solo: −11 / −20;
- C2 no-Caller: −32 / −91;
- C3 no-ALib: −4 / −6.

The accepted C1 occurrence oracle is included with its target/usage identities,
variant checks, complete-empty Solo and RM01 unresolved control.

## 4. D3 case readiness

`d3-case-matrix.md` defines D3-01…08 inputs, expected ledgers, controls and
implementation requirements. The graph and occurrence preparation supports:

- calls/references and overload separation (D3-01);
- exact aggregate node/edge/evidence and contributor sets (D3-02);
- traversal and raw mirror census expectations (D3-03);
- initial/invisibility and occurrence separation (D3-04);
- replacement and symbol/reference deltas (D3-05);
- C1/C2/C3 deletion and contributor retention (D3-06);
- known-failure and retry expectations (D3-07);
- reopen and historical-copy policy (D3-08).

The D3 campaign itself is not executed in re-entry.

## 5. DF-1 reconciliation

`df1-reconciliation.md` records why prior credits are insufficient and what D3
adds. DF-1 still lacks the executed joint graph/occurrence/raw-mirror campaign,
genuine hosted Linux exact-candidate evidence and independent replay. It remains
**open**; no closure is inferred from remediation acceptance or corpus
preparation.

## 6. Findings and gates

| Finding | Disposition |
| --- | --- |
| F-S64E-01/02, F-S64R-01/02 | **Closed** |
| F-S64E-03 / DF-1 | **Open** |
| F-S64E-04 | **Partially discharged**; C4 occurrence ledger and H1 execution outstanding |
| F-S64E-05 | **Discharged for remediation only**; D3 execution outstanding |
| R1-01 | C4 exact occurrence ledger **open** |
| R1-02 | C1 graph non-invariance deltas recorded as expected, not defects |
| R1-03 | Synthetic-merge checkout authorization is remediation-specific |
| R1-04 | H1 stores not exercised in re-entry |
| R1-05 | D3 implementation pending |
| R1-06 | Prior producer/qualification payloads contain a pre-seal `manifest.json`; their own seal delta rule fails while content hashes, inputs, trees and binaries verify. Seals unmodified; recorded for successor adjudication |

The re-entry gate matrix records R-G1…R-G10 as pass, R-G7 occurrence as open,
R-G11 seal as pass, R-G12 proposed commission as pass. The open R-G7 item is the
sole corpus/oracle preparation gap.

## 7. Remaining implementation

The proposed
[`ANDIRIT-CI1A-S2.6-WP-S64-IMPL-01`](ANDIRIT-CI1A-S2.6-WP-S64-IMPL-01.md) is
limited to:

- deriving the C4 two-unit occurrence ledger from the accepted authority;
- implementing D3-01…08 and their controls on the frozen corpus;
- raw mirror/traversal/reopen/disable-enable receipts;
- known-failure, retry and fresh-process reopen;
- H1 historical copy checks;
- exact-candidate Windows and genuine hosted Linux runs;
- sealed producer evidence and the separate D3 qualification handoff.

## 8. Retained boundaries

S6.1–S6.3 remain closed within their accepted scopes. The P-03 erratum remains
binding. Current-query optimization is excluded. LatticeDbSharp remains `0.2.0`;
release boundary remains `v0.2.0-preview.7`. Mirror rebuild/sweep remains
O(physical mirror rows); native IDs remain unstable. macOS evidence is not S6.5
certification. No production code, dependency, workflow, release or existing
seal was modified by this re-entry.

## 9. Evidence and successor

The sealed re-entry evidence is in
`ANDIRIT-CI1A-S2.6-WP-S64-ENTRY-01-R1-evidence/`; the manifest binds the corpus,
oracle, ledgers, validation, contract reconciliation, DF-1 analysis and the
acceptance/closure records. Verification instructions are in the evidence
README.

Proposed successor: **`ANDIRIT-CI1A-S2.6-WP-S64-IMPL-01`** for the remaining
implementation, followed by **`ANDIRIT-CI1A-S2.6-WP-S64-QUAL-01`** for
independent D3 qualification. Neither is authorized by this report.
