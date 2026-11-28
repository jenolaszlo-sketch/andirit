# R1-06 owner exception record

**Authority:** owner decision in the OCC-ID-REMED-01 commission, §9.

## Scope

Narrow historical procedural exception for:

- producer remediation seal `870b600acf1490337af9f483f8894a5e8166a90e`;
- independent remediation qualification seal `b3951c790768ad257fb3418e5abe41cc5ecc5322`.

## Findings

- The mandatory self-excluding delta rule was violated: both payload commits
  contain a pre-seal `manifest.json`, so the seal commit modifies (`M`) instead
  of adds (`A`) the manifest.
- Evidence content integrity remains independently supported:
  - producer: 34 entries, 7 reviewed inputs, seal SHA-256 `b473dd…04f2`;
  - qualification: 70 entries, seal SHA-256 `3a690d5d…b59d`;
  - trees, payloads, implementation identities and affected-store binary hashes
    verify from Git objects.
- This exception does not characterize the historical seals as procedurally
  compliant.
- Neither seal may be modified or replaced.
- The exception does not establish a general relaxation of the sealing contract.
- All successor seals must add only `manifest.json`.
- Previously closed production findings are not reopened solely because of this
  packaging anomaly.

## Continued obligation

If further verification contradicts the accepted evidence integrity, the
affected gate must be withheld and the discrepancy reported. The re-entry seal
and this remediation seal comply with the mandatory rule.
