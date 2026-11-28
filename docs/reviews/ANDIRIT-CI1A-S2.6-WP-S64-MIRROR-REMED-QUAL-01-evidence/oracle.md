# Independently derived oracle

The oracle is derived from the published unit definitions and the accepted graph
contract. It never calls the mirror, the projection, or the candidate store to
obtain an expected identity or count.

## Logical expectations

Given the current published units:

1. **Canonical node identities** are the distinct `Id.Value` values across all
   node contributions. Equivalent duplicate contributions collapse to one
   identity. A non-equivalent contribution with the same identity is a conflict;
   the accepted publication path rejects it, so it is outside the expected set.
2. **Canonical edge identities** are the distinct edge `Id.Value` values whose
   source and target are canonical node identities.
3. **Contributor aggregation** is the accepted materialization rule: deterministic
   plugin/unit ordering, equivalence-only merging, and explicit conflict
   rejection. The oracle does not choose a first or last writer.
4. **Physical multiplicity** is exactly one mirror node per canonical node and
   one mirror edge per canonical edge. Each physical row carries the canonical
   payload.
5. **Manifest correspondence** is bidirectional: every manifest entry resolves
   to a physical row with the same logical identity, and every physical mirror
   row is referenced by exactly one manifest entry. Native ids are distinct.
6. **Traversal parity** is the exact ordered edge identity sequence from the
   native mirror versus the authoritative projection for the same query.

## Ownership expectations

- A manifest is ownership evidence only when every node entry is a mirror node
  (`HetuGraphNode` plus `hetu_json`) and every edge entry is a mirror edge whose
  endpoints are nodes named by the same manifest.
- A node or edge that is not protocol-marked is never deleted by mirror cleanup.
- An unreferenced protocol-marked row is derived mirror state; it may be swept
  and reconstructed.
- A non-mirror relationship incident to a candidate mirror node makes ownership
  ambiguous. The operation must fail closed before any destructive change.
- A retained edge protects both endpoint nodes.
- `DeleteNode` is independently observed not to cascade (see
  `results/probe-delete.json`), so cleanup must not rely on the engine to
  preserve foreign edges.

## Recovery expectations

Recovery of a defective store must produce the canonical physical multiset,
preserve every authoritative row byte-for-byte (identity, key and payload),
preserve occurrence facts, preserve unrelated sentinel objects with their
properties and relationships, and make projection and native traversal agree.
A second recovery must be idempotent.

## Negative controls

The same oracle is applied to the entry baseline `dda4358`. It must fail on the
defect scenarios. The producer test suite is also replayed on that baseline as
an audited control: 14 of 28 LatticeDb tests and the linked-fixture test fail.
The independent harness fails all defect-oriented baseline scenarios.
