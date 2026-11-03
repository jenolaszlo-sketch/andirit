# Historical oracle preservation

The original Q1 expected result remains at
`docs/planning/adr-ci1-01-ci1a-s2-0/oracle/expected/oracle-expected.json` and
the R1 expected result at `…/expected/oracle-expected-r1.json`; neither was
modified. R2 uses the separate `oracle-expected-r2.json` and identifies itself
as `CI1A-S2.0-CORPUS-R2`. Historical evidence is not a runtime compatibility
promise.

## Byte preservation against pinned sources

Both historical expected files were byte-compared against clean `git archive`
extractions of their pins during this commission:

| File | SHA-256 (working tree) | Matches pinned archive |
|---|---|---|
| `oracle-expected.json` (pin `82f101c24e21828a41a30d4342ba73db9376e24e`) | `D5F507DDF2220DF556702982E3BE1F86CDE698195AEA66872F488E96EDE5C0D7` | yes |
| `oracle-expected-r1.json` (pin `9fc2115622c93fa54f5f7882b117185a017703bb`) | `1A53633EEF9517128EDFE90E8E715C513056488E430860462AB76BF0DB3AD085` | yes |

## Historical reproductions from isolated pinned archives

Each pin was extracted with `git archive --format=zip` into an ordinary
temporary directory (no linked worktree, no network) and its own oracle was
built and run against its own expected file:

| Baseline | Oracle version | Result | As-produced output SHA-256 |
|---|---|---|---|
| `82f101c…` (Q1) | pre-R1 (no version field) | **114/114 checks passed**, exit 0 | `9831FB36767BE335EC8780E909B63D4E516B2036189CF89718545592604E0B3E` |
| `9fc2115…` (R1) | `CI1A-S2.0-CORPUS-R1` | **130/130 checks passed**, exit 0 | `0A7BB361509AEBEE70A1305679EE22C6C35ECC92E3B973C2A5D08E6854049CCC` |

The R1 reproduction hash equals the canonical hash recorded in the R1 evidence
package and the independent Q2 review. Artifacts: `historical-oracle-82f101c.json/.log`,
`historical-oracle-r1-9fc2115.json/.log`.

## Line-ending convention for stored copies

Oracle JSON output is written with the platform newline (CRLF on this host).
Stored evidence copies are LF-normalized, matching the R1 package convention;
section content is byte-equal after normalization. Stored-copy hashes:

| Stored copy | SHA-256 (LF) | Corroborates |
|---|---|---|
| `historical-oracle-82f101c.json` | `A5C0A4850FB418B92AB97EB4292CD9AFA20DACD3BBA15822FE9276FCF2A6B339` | byte-identical to the R1 package's stored `historical-oracle-82f101c.json` |
| `historical-oracle-r1-9fc2115.json` | `72A18D26A8C26CD283B0B5860CCA3E59CF9909F4810BC9495A90798B84D78A42` | byte-identical to the R1 package's stored `oracle-r1-run-1.json` |

The R2 frozen expected artifact lives under the planning tree whose
`.gitattributes` preserves exact bytes (`* -text -diff`), so its committed
bytes equal its working-tree bytes (SHA-256 `274A7AAE9EB6724879D619416E449C52759B44085D7B718DAD3434B15387CBBF`,
145620 bytes).
