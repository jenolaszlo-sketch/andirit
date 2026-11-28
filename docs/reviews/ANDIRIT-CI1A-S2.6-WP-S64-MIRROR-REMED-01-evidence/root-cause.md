# Confirmed root causes

Both defects are producer-scope Andirit defects. LatticeDbSharp `0.2.0` and the
native engine provide correct create, delete, label and adjacency primitives;
the failures come from how Andirit chose physical identities and how it deleted
its own derived rows.

## F-S64E-01

### Mechanism

The defective `NativeGraphMirror.Rebuild` consumed
`IReadOnlyList<CodeIndexUnitReplacement>` and executed:

- one `CreateNode` per node contribution in every unit;
- one `CreateEdge` per edge contribution in every unit;
- `nodes[node.Id.Value] = id` and `edges[edge.Id.Value] = id` in dictionaries.

Shared identities therefore created multiple physical objects while the
manifest retained only the last native id. Cleanup on the next rebuild walked
`existing.Nodes.Values` and deleted incident edges; unmapped duplicates and their
edges were unreachable from that inventory and accumulated.

The authoritative in-memory projection did not share the defect: it merges by
logical identity, rejects non-equivalent contributions, and exposes one
materialized fact per identity. The mirror was re-deriving a weaker aggregation
instead of reusing the accepted one.

### Runtime confirmation

- Real linked-file fixture: 20 distinct logical nodes, 21 physical nodes at the
  first publication; the linked logical id appears twice.
- The same fixture after one enabled reopen: 22 physical nodes, three rows for
  the linked logical id.
- Synthetic two-contributor case: two logical nodes and one logical edge become
  four physical nodes and two physical edges.
- The baseline affected store: 43 physical nodes for 20 identities, 60 physical
  edges, 30 manifest edge entries ⇒ 23 unmapped node ids and 30 unmapped edge
  ids.

### Ownership

Andirit mirror writer. The native engine creates the requested rows faithfully.

## F-S64E-02

### Mechanism

The disabled completion branch deleted only the `HetuMirror` manifest row:

```text
DeleteMany(txn, NativeGraphMirror.MirrorLabel, [repositoryId])
```

It never enumerated or deleted the manifest-owned `HetuGraphNode` rows or
edges. A later enabled open found no manifest, treated the repository as having
no inventory, and rebuilt a full second generation beside the first. Because the
old rows were then unreferenced, no later manifest could delete them either.

### Runtime confirmation

Observed sequence on the entry fixture: 21 → 22 (enabled reopen) → 22 nodes with
0 manifests (disabled publish) → 43 nodes after re-enablement. The intermediate
disabled state retained every physical row while reporting no mirror.

### Ownership

Andirit store lifecycle. The native delete primitives were available and
correct; the disabled path simply did not call them.

## Why the defects were not migratable by inspection alone

A manifest that omits a row is indistinguishable from a manifest that predates
the row unless physical rows can be attributed. The remediation therefore makes
forward writes complete and uses the only provable ownership rule available for
legacy residue: a protocol-marked mirror row (`HetuGraphNode`/`hetu_json`, or a
mirror edge carrying `hetu_json`) that no manifest references is derived state
owned by no repository's current mirror view, and is reconstructable from the
authoritative command log. Rows referenced by any manifest are never touched.
