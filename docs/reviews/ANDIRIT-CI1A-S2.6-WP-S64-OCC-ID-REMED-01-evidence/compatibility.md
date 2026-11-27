# Compatibility assessment

## Affected by an identity transition

| Surface | Effect |
| --- | --- |
| `compctx2` descriptors | Reference set/identity changes for every project |
| `asmctx3` target IDs | Changed for every target whose defining compilation descriptor changes |
| `usage4` occurrence IDs | Changed because they contain the target ID and source identity |
| Persisted `AndiritOccurrenceUnit` / authority | Existing occurrence facts no longer match a rebuilt current authority; they would refuse as stale/corrupt |
| Existing `v0.2.0-preview.7` publications | Not identity-compatible with a changed reference contract |
| Cross-store occurrence continuity | Requires re-ingestion or an explicit migration/rebuild contract |

No migration or compatibility transition was implemented. Existing-store
compatibility is therefore **not claimed**.

## Unaffected

- Legacy graph node/edge IDs and graph aggregation.
- Mirror manifest/ownership/recovery contracts.
- S6.1 cancellation, S6.2 durability and S6.3 publication control flow.
- Contributor union and occurrence cardinality mechanics.
- Dependency pin and release boundary.

## Contract interactions

- The F12/R2 oracle uses the S2.0 runtime reference set. If production adopts the
  same content-bound framework set, C1 target identities can align, but current
  accepted package/store identities still change.
- S2.1 explicitly authorized `file|path|sha256`; a replacement supersedes part of
  that acceptance and needs a new owner decision.
- The S2.2 rule that `compctx2` never enters `asmctx3`/`usage4` remains valid and
  is not the thing being changed.

## Conclusion

A correction is technically definable, but it is an incompatible persisted
identity transition. The commission stops before implementation as required.
