# ADR-CI1-01-S2.6-WP-S64-MIRROR-REMED-ACCEPTANCE-OWNER-01

## Owner acceptance of the S6.4 mirror remediation qualification

**28 November 2026 · A — SUPPORTED · accepted · remediation work package formally closed**

The owner accepts the independent qualification disposition **A — SUPPORTED**
for the exact identities below. This closes the S6.4 mirror remediation work
package. It does **not** close S6.4, discharge DF-1 or F-S64E-03/F-S64E-04, or
authorize integration, publication or release changes.

| Subject | Commit | Tree |
| --- | --- | --- |
| Entry baseline | `dda4358bc2588368efc6ab6a7bd774857d998026` | `dc85aeb34f65d13176ed503ba90cd22d28179b98` |
| Accepted implementation | `ec984bfe63e5fbe13047cee8920e00b634ab17b0` | `8ec0eb7a5e647a171f54fa8969dadbb6c65da179` |
| Producer evidence payload | `b167fed057dfa2b2b1e4da1fde26826ff5f01ec9` | `bb9a0386fdfc4f4a3e9c744b1611c154f2ebca40` |
| Producer seal | `870b600acf1490337af9f483f8894a5e8166a90e` | `a9f4b3f15a48fc88f574aa255261fc3788faf5a2` |
| Independent qualification payload | `1d5350dd4868c1c4b8ad5531e3e5bf90e8e4b9af` | `77134f3b16abaf15507e300e58f555cc347a78ce` |
| Independent qualification seal | `b3951c790768ad257fb3418e5abe41cc5ecc5322` | `61817b107a0fb5cab565c9002a22fae0b858c671` |

Producer seal SHA-256: `b473dd41742510d5676c1d8c033244afbc2c769dc52bf3941fa776769a6704f2`.
Independent seal SHA-256: `3a690d5df4b677845198fecb10b8614f6e0c297bece8148b26f9b3029dd5b59d`.
Both seals remain unmodified and verify from Git objects.

## Accepted findings

| Finding | Disposition |
| --- | --- |
| F-S64E-01 duplicate physical nodes/edges | **Accepted as remediated** |
| F-S64E-02 disabled-publication orphans | **Accepted as remediated** |
| F-S64R-01 manifest ownership misattribution | **Accepted as remediated** |
| F-S64R-02 unrelated incident-edge deletion | **Accepted as remediated** |

## Accepted gates

All 21 remediation qualification gates G-1…G-21 in the independent gate matrix
are accepted as satisfied:

- seal integrity and reviewed-input identity;
- authorized tree-equivalent checkout equivalence;
- baseline reproduction and candidate correction for all four findings;
- ownership adversarial controls, fail-closed refusal and non-cascade handling;
- producer-sealed and independently constructed recovery;
- 1,005 → 1,034 inventory reconciliation, 29 new tests and 19 inherited named
  cases;
- coverage floors, package consumption and inherited S6.1–S6.3 protection.

## Hosted and execution boundaries

- Candidate hosted CI `46035752304` attempt 1 succeeded on Windows x64,
  genuine hosted Linux x64 and macOS ARM64; the tested tree is byte-identical
  to `ec984bf` under the owner-authorized synthetic-merge decision.
- Seal hosted CI `46036959497` attempt 1 succeeded on the same runners.
- Independent qualification executed the full suite and package consumer on
  Windows x64 and independently audited Linux/macOS artifacts. It did not
  execute Linux/macOS locally.
- macOS remains regression evidence, not S6.5 platform certification.

## Provenance note from S6.4 re-entry (R1-06)

The re-entry audit found that both prior payload commits contain a pre-seal
`manifest.json` blob, so the producer's own `seal.py verify` delta rule reports
a modified rather than added manifest. Direct object verification still matches
all 34/70 evidence entries, all 7 implementation inputs, both seal SHA-256
values and the affected-store binaries; neither seal is modified. The anomaly
is recorded in the re-entry evidence as R1-06 for successor adjudication. It
does not change the accepted implementation, findings or gate dispositions.

## Retained boundaries

LatticeDbSharp remains `0.2.0`. The release boundary remains
`v0.2.0-preview.7`. S6.1, S6.2 and S6.3 remain closed within their accepted
scopes. DF-1, F-S64E-03 and F-S64E-04 remain open/outstanding. The P-03 erratum
remains binding. Current-query optimization remains excluded. Mirror rebuild and
sweep remain O(physical mirror rows); engine-assigned native IDs remain
unstable. PR #8 remains draft; no merge, tag or package publication is
authorized by this acceptance.
