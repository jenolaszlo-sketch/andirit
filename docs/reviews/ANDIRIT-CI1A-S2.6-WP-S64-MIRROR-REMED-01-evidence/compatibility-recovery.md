# Affected-store compatibility and recovery

## Immutable affected stores

The two stores under `affected-stores/` were produced by the unmodified entry
seal `dda4358…` with the entry observer: the real linked-file fixture published
enabled, reopened enabled, published with `UseNativeTraversal=false`, then
reopened enabled. They are the direct defective output, not reconstructions.

`SHA256SUMS.txt` records the byte hashes. The `.ltdb` and matching `.ltdb-wal`
must be treated as a pair. The committed copies are not modified by any test;
recovery is always performed on disposable copies.

| Store | Meaning |
| --- | --- |
| `graph-only.ltdb` | Graph-only lane, no occurrence facts |
| `integrated.ltdb` | One durable occurrence fact through the normal ingestion sink |

## Defective census

| Measure | graph-only / integrated |
| --- | ---: |
| Physical nodes | 43 |
| Distinct logical nodes | 20 |
| Physical edges | 60 |
| Manifest node entries | 20 |
| Manifest edge entries | 30 |
| Unreferenced native node ids | 23 |
| Unreferenced native edge ids | 30 |

The disabled publication left no manifest at the intermediate checkpoint; the
final enabled reopen created the manifest and a second full generation. This is
exactly the F-S64E-02 residue plus the F-S64E-01 duplicates.

## Ownership proof for deletion

A row is removable only if all of the following hold:

1. it is a mirror-protocol row: a node labelled `HetuGraphNode` carrying the
   `hetu_json` property, or an edge carrying `hetu_json` beginning at such a
   node;
2. its native id is not referenced by any **trusted** manifest in the database;
3. deleting it cannot strand an unrelated or retained edge (non-mirror incident
   edges cause an explicit refusal, and retained edges protect their endpoints);
   and
4. the operation runs in the same native transaction as the manifest update or
   deletion that makes it unreferenced.

A manifest is trusted only when every entry resolves to a protocol-marked mirror
object and every referenced edge connects nodes named by the same manifest. A
manifest naming an unrelated node, an authoritative command-log row, an
unrelated edge, or an edge whose endpoint nodes it omits owns nothing and is
never used for deletion or retention. See `ownership-safety.md`.

Mirror rows are derived state. A row satisfying (1) and (2) is not part of any
repository's served mirror view, cannot be reached by any mirror traversal, and
is reconstructable from the authoritative command log. Rows owned by another
repository's manifest are retained, which preserves isolation even when two
repositories share a logical identity. Rows that are not mirror-protocol rows
are never considered, so unrelated native data is preserved.

## Recovery receipts

| Receipt | Result |
| --- | --- |
| `detect-graph-only.json` | 43 nodes / 60 edges / 23 and 30 unmapped ids, no modification |
| `recover-graph-only-unrelated.json` | 43→20 nodes, 60→30 edges, 0 unmapped, 1 unrelated sentinel preserved |
| `recover-integrated.json` | 43→20 nodes, 60→30 edges, 1 occurrence fact preserved |
| `recover-graph-only-second.json` | 20→20 nodes, 30→30 edges; idempotent recovery |

All receipts also require unchanged repository/run/unit/index-state counts and
identical projection/native traversal sequences after recovery.

## Boundaries and refusal

- A malformed, incomplete, duplicate-valued or absent manifest is treated as
  owning no rows; the repository is rebuilt from authoritative facts.
- An unreadable manifest does not prevent a disabled authoritative open; the
  projection remains fully usable.
- Recovery is reconstruction of derived state, not a database migration. No
  authoritative command-log, publication, occurrence or unrelated native rows
  are rewritten.
- A recovery transaction failure rolls back the cleanup. The prior publication
  and a subsequent enabled reopen remain valid; this is covered by
  `RecoveryFailure_RollsBackCleanup_AndSubsequentReopenSucceeds`.
- No blanket deletion of `HetuGraphNode` rows occurs: only unreferenced
  protocol-marked rows are removed, and every retained manifest id is protected.
