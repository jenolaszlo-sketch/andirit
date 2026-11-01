# ADR-CI1-01-CI1A-IMPL-01-S1 — Native Occurrence Schema and Refusal (SQ-03, D5)

**Scope:** the production occurrence format selected before the first durable
occurrence write. This is the SQ-03 decision required by the independent review and
the D5 refusal contract.

## 1. Decision

The production occurrence storage is a **distinct, additive native format** in new
labels. It is not a byte-compatible copy of the experimental `occurrence-facts/4`
format, and superseded experimental schemas are not migrated or reinterpreted.

| Concern | Decision |
|---|---|
| Namespace | `andirit-occurrence-facts/1` |
| Native labels | `AndiritOccurrenceUnit`, `AndiritOccurrencePage`, `AndiritOccurrenceMeta` (distinct from `Hetu*`) |
| Native schema version | `LatticeCodeGraphStore.CurrentOccurrenceSchemaVersion = 1` |
| Occurrence semantics version | `CodeOccurrenceFact.SemanticsVersion = 4` (accepted `usage4` shape) |
| Page/publication binding | each page records `PublicationIndexRunId`; meta records run id, index identity, snapshot identity, consistency |
| Transaction | occurrence rows/pages/meta are written in the same LatticeDB write transaction as graph completion |
| Graph labels | unchanged (`Hetu*`, schema 2) |

**Two version axes are kept distinct:** the *semantic identity version* (`usage4` /
`asmctx3`, currently 4) and the *native storage format version* (currently 1). A change
to either requires an explicit decision; neither implies the other.

## 2. Durable record shape

- `AndiritOccurrenceUnit` — key `repository\nplugin\nunit`; payload: repository, plugin,
  unit, `analysisIncluded`, facts.
- `AndiritOccurrencePage` — key `repository\ntarget`; payload: target, publication run
  id, ordered occurrence ids.
- `AndiritOccurrenceMeta` — key `repository`; payload: schema version, namespace,
  publication run id, index identity, snapshot identity/consistency, counts,
  `analysisIncluded`.

Records use the same base64-JSON record convention as the existing `Hetu*` tables, with
the shared `DurableCommandLog` serializer. Writes are chunked so large pages cannot
overflow the native planner.

## 3. Refusal behavior

| State | Result |
|---|---|
| Fresh graph-only store (schema 2, no occurrence rows) | opens normally; occurrence query → `AnalysisUnavailable` (never empty-success) |
| Current occurrence format (meta version 1, namespace match) | occurrence query available, bound to the meta publication |
| Unsupported occurrence schema/namespace | **open refuses**: `CodeOccurrenceSchemaException(actual, expected)` |
| Occurrence unit/page rows without a meta manifest | **open refuses**: malformed occurrence state, not empty analysis |
| Page bound to a different publication than meta | **open refuses**: `CodeOccurrenceIndexException` |
| Stale query publication | query → `StalePublication` |
| Corrupted/malformed record payload | open/refusal surfaces an explicit failure |

An unsupported occurrence format is **never** treated as empty occurrence analysis.

## 4. Reopen and version check

On open the store restores graph replay first, then reads the occurrence meta, units and
pages. It validates the meta version/namespace, refuses missing-meta or mismatched-page
state, restores each unit's facts into the in-memory occurrence view, and binds the view
to the durable occurrence publication. A graph-only store leaves the occurrence view
unbound, so queries report `AnalysisUnavailable`.

## 5. D5 — no migration

- Graph-only schema-2 history remains readable exactly as before.
- Superseded experimental occurrence schemas are refused, not migrated, not dual-written
  and not auto-deleted.
- Historical aggregate edges are never reinterpreted as occurrence facts.
- Re-ingestion writes the new compatible format.
- The aggregate/`Hetu` historical contract is untouched.

## 6. Evidence

| Test | Observation |
|---|---|
| `N7_StalePublicationAndGraphOnlyStoresRefuseExplicitly` | graph-only repository → `AnalysisUnavailable` |
| `Store_RejectsUnsupportedOccurrenceSchemaOnOpen` | meta version 99 → `CodeOccurrenceSchemaException` with actual/expected |
| `Store_RejectsOccurrenceUnitsWithoutMeta` | deleted meta with surviving units → refusal |

**Not performed / carried:** a historical prior-writer fixture (SQ-06) and any
multi-version compatibility campaign. Stage 1 claims only safe new-format behavior and
explicit refusal; it does not claim historical occurrence migration.
