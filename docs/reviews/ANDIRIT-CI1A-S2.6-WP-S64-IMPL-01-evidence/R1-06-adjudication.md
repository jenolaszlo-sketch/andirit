# R1-06 prior-seal packaging adjudication

## Facts verified from Git objects

| Seal | Payload | Pre-seal manifest in payload | Seal delta | Producer script |
| --- | --- | --- | --- | --- |
| Producer `870b600…` | `b167fed…` | **Yes**, blob `fcd33cd…` | `M` manifest | `seal.py verify` raises "Unexpected parents or path changes" |
| Qualification `b3951c7…` | `1d5350d…` | **Yes** | `M` manifest | same packaging shape |
| Re-entry `1776ea8…` | `b7d2bf6…` | No | `A` manifest | verifies cleanly |

For both prior seals, the manifest is self-excluding from its own `entries`, and
all content hashes verify:

- producer: 34 entries, 7 reviewed implementation inputs, seal SHA-256
  `b473dd…04f2`;
- qualification: 70 entries, seal SHA-256 `3a690d5d…b59d`;
- payload, implementation and seal trees match the accepted identities;
- affected-store binaries match `SHA256SUMS.txt`;
- the seal commits change only `manifest.json`.

## Root cause

A `git reset --soft HEAD~1` left the pre-seal `manifest.json` staged before the
payload commit was amended. The amended payload therefore contains a stale
manifest, and the following seal commit modifies it instead of adding it.

## Mandatory-rule assessment

The producer's own `seal.py` encodes the rule:

```text
payload commit adds the evidence paths; seal commit adds only manifest.json
```

The observed `M` violates that rule. It is not a verifier-only limitation: the
producer script itself fails. Content integrity is nevertheless established
because the sealed manifest self-excludes the manifest path and every referenced
entry, input and binary hash matches.

## Conclusion

- **Evidence integrity:** established for content, inputs, trees and binaries.
- **Mandatory provenance procedure:** violated for the two prior seals.
- **Owner adjudication:** required. The producer does not waive the rule and does
  not modify either seal.
- **New evidence:** the re-entry seal and this implementation seal add only
  `manifest.json`; they do not reproduce the anomaly.

Permitted claim: the accepted remediation evidence content is intact. Not
permitted: a claim that the producer sealing procedure passed as written, or a
general waiver of the self-excluding payload rule.
