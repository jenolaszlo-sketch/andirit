# Prior-seal packaging anomaly (re-entry finding R1-06)

## Observation

The producer payload `b167fed057dfa2b2b1e4da1fde26826ff5f01ec9` and the
independent qualification payload `1d5350dd4868c1c4b8ad5531e3e5bf90e8e4b9af`
each contain a pre-seal
`…/ANDIRIT-CI1A-S2.6-WP-S64-…-evidence/manifest.json` blob. Their seal commits
`870b600…` and `b3951c7…` modify that file to the final manifest.

Consequently the producer's own `seal.py verify` fails its delta rule
("Unexpected parents or path changes") because it expects the manifest to be
**added** by the seal commit, while the commit shows `M` (modified). This is a
packaging anomaly introduced when a soft reset left the pre-seal manifest staged
before the payload commit was amended. It is not a content-hash failure.

## What still verifies

Direct object verification confirms:

- producer: 34 evidence entries and 7 reviewed implementation inputs match
  Git blobs, lengths and SHA-256; producer seal SHA-256 `b473dd…04f2`;
- qualification: 70 evidence entries match; qualification seal SHA-256
  `3a690d5d…b59d`;
- payload, implementation and seal trees match the accepted identities;
- affected-store binary hashes match `SHA256SUMS.txt`;
- the seal commits change only `manifest.json`;
- both manifests self-exclude `manifest.json` from their entries.

No evidence file, implementation input, binary store or seal object was altered
by this re-entry.

## Disposition

Record as **R1-06**. The owner accepted the qualification and no production or
evidence content is invalidated. The packaging anomaly is a methodological
limitation for the successor qualification to adjudicate; it is not silently
repaired, and neither prior seal is modified. The re-entry seal itself adds only
`manifest.json` and verifies cleanly.
