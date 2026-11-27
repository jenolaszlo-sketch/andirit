# ANDIRIT-CI1A-S2.6-WP-S64-MIRROR-REMED-CLOSE-01

## Formal closure of the S6.4 mirror remediation work package

**28 November 2026 · CLOSED — owner acceptance recorded · S6.4 remains open**

The owner accepted the independent qualification disposition **A — SUPPORTED**
and formally closes the S6.4 mirror remediation work package. This record is the
closure companion to the
[owner acceptance](../decisions/adr-ci1-01-s2-6-wp-s64-mirror-remed-acceptance-owner-01.md)
and the
[independent qualification report](ANDIRIT-CI1A-S2.6-WP-S64-MIRROR-REMED-QUAL-01.md).

## Closed findings

| Finding | Closure basis |
| --- | --- |
| F-S64E-01 duplicate physical nodes and edges | Canonical-fact rebuild; independent baseline reproduction and candidate verification |
| F-S64E-02 disabled-publication orphans | Physical deletion on disable plus bounded orphan sweep; independent recovery |
| F-S64R-01 manifest ownership misattribution | Trusted-manifest protocol validation; independent adversarial preservation |
| F-S64R-02 unrelated incident-edge deletion | Fail-closed ownership refusal; independent partial-cleanup rollback control |

These findings are not reopened by this record. Any future regression requires
new evidence and a new owner-authorized disposition.

## Accepted evidence and gates

- Producer implementation `ec984bf…`, tree `8ec0eb7a…`.
- Producer payload `b167fed…`, seal `870b600…`, sealed SHA-256 `b473dd…04f2`.
- Independent qualification payload `1d5350d…`, seal `b3951c7…`, sealed SHA-256
  `3a690d5d…b59d`.
- All 21 remediation qualification gates accepted.
- Candidate hosted CI `46035752304` and seal CI `46036959497` succeeded;
  the tested merge tree is byte-identical to the implementation tree.
- Accepted test inventory 1,034; 29 new tests; 19 inherited S6.3 named cases.
- LatticeDbSharp `0.2.0`; release boundary `v0.2.0-preview.7`.

## Preserved boundaries

S6.1, S6.2 and S6.3 remain closed within their accepted scopes. DF-1,
F-S64E-03 and F-S64E-04 remain open/outstanding. F-S64E-05 is discharged for
mirror remediation only. The P-03 erratum remains binding. Current-query
optimization is excluded. Mirror rebuild/sweep remains O(physical mirror rows).
Engine-assigned IDs remain unstable. macOS regression evidence is not S6.5
platform certification.

No merge to `main`, release tag, package publication, dependency upgrade or
producer/qualification seal modification was performed by this closure.

**Provenance note (R1-06).** The later S6.4 re-entry audit found a pre-seal
`manifest.json` blob in both prior payload commits; the producer's own seal delta
rule therefore reports `M` rather than `A`. Content hashes, reviewed inputs,
trees and binary hashes verify, and neither seal was modified. The anomaly is
recorded for successor adjudication and does not reopen the closed findings.
