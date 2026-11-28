# Persisted compatibility and reconstruction

## Generation 3

Corrected stores use schema 3, namespace `andirit-occurrence-facts/3`, metadata
`IdentityGeneration=3`, authority version 3, `asmctx4` targets and `usage5`
facts with semantics 5.

## Non-mutating refusal

The R1 generation-2 C4 store was used as an authenticated incompatible input.

| File | Before | After refused open |
| --- | --- | --- |
| main | `7d59f9…eca1` / 782336 | `7d59f9…eca1` / 782336 |
| WAL | `e5ad77…a957` / 4096 | `e5ad77…a957` / 4096 |

Result: `CodeOccurrenceSchemaException` with re-ingestion guidance; no schema
initialization, replay, mirror cleanup, recovery write or occurrence mutation.

Regression controls also refuse generation 1 and generation 2 simulations with
unchanged main and WAL hashes. Missing, mismatched and contradictory metadata
fail closed.

LatticeDbSharp 0.2.0 provides `ReadOnly=true` and `EnableWal=false`; a read-only
open of the tested store is physically non-mutating. No dependency upgrade or
ad-hoc database parser was introduced.

## Reconstruction

A corrected new store built from the C4 corpus has 51 facts, 50/1 distribution,
two contributors, target `23ffa6…ab68`, schema/namespace/generation 3, and a
complete `Available` query after durable reopen. A second fresh store has the same
logical SHA-256. Physical container bytes differ because of native page/WAL
allocation.

Original stores remain logically preserved. Reconstruction is re-ingestion into a
new store only; no in-place migration or destructive rebuild exists. It is not
universally lossless when historical source bytes, reference bytes or run metadata
are unavailable.
