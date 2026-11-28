# Compatibility and reconstruction

## 1. Affected surfaces

| Surface | Effect |
| --- | --- |
| `refctx1` reference identity | Replaces `file|<absolute path>|sha256:<hash>` |
| `compctx3` descriptor | Replaces `compctx2` for current production |
| `compctx4` defining context | Replaces `compctx1` for current target minting |
| `asmctx4` target | Replaces `asmctx3` |
| `usage5` occurrence | Replaces `usage4` |
| Persisted occurrence schema | 2 (`andirit-occurrence-facts/2`) |
| Authority envelope | Version 2 / identity generation 2 |
| Payload hash | `occurrence-payload5` |

## 2. Existing stores and packages

Existing `v0.2.0-preview.7` stores and packages are **not** identity-compatible
with the successor generation.

- A predecessor occurrence store is detected by schema/namespace/generation and
  refused with `CodeOccurrenceSchemaException` explaining that re-ingestion into
  a new store is required.
- A predecessor authority envelope is refused with
  `CodeOccurrenceAvailability.UnsupportedSchema`.
- Predecessor `asmctx3`/`usage4` values are never reinterpreted as successor
  values.
- No in-place migration, export/import rewrite, destructive rebuild or silent
  promotion exists.

## 3. Reconstruction

Reconstruction is ordinary re-ingestion into a **new** store:

1. open a new database at a new path;
2. run the production plugin over the source corpus;
3. publish the resulting graph and occurrence rows;
4. query with a current verified compilation authority.

The original database is preserved. The regression
`PredecessorGenerationStore_IsRefusedAndReingestionBuildsSuccessorStore` verifies
that refused opens do not change the logical predecessor rows and that a new
store reconstructs the same target and 51 facts.

## 4. Unaffected

- Legacy graph node/edge identities and graph aggregation.
- Mirror manifest, ownership and recovery contracts.
- S6.1 cancellation, S6.2 durability and S6.3 publication control flow.
- Contributor union and occurrence cardinality mechanics.
- Dependency pins and release packaging boundaries.
- The `compctx2`→`compctx1` isolation rule is preserved in successor form:
  `compctx3` never enters `asmctx4`/`usage5`; `compctx4` is the defining input.

## 5. Package smoke

All packages still pack and load. The packed smoke test verifies six standalone
assembly identities, packed Roslyn extraction, durable store reopen, native
mirror, snapshot integrity, no legacy Penghou/Hetu dependencies, and the Testing
conformance laws. See `evidence/local-package-smoke.log`.
