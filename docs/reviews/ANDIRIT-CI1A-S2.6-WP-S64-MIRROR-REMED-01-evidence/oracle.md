# Independent expected-graph oracle

The regression and recovery oracles are derived from the fixture inputs and the
governing graph semantics. They do not call the mirror, the projection, or the
store to obtain expected identities or counts.

## Inputs

For each scenario the test defines the complete current publication as a set of
index units, each with an explicit origin `(repository, plugin, unit)` and
explicit node and edge records.

## Derived expectations

1. **Canonical node set.** The union of node `Id.Value` across the current
   published units. Equivalent duplicate contributions collapse to one identity.
   A contribution with the same identity but a different payload is a conflict
   and the publication is rejected, so it can never enter the expected set.
2. **Canonical edge set.** The union of edge `Id.Value` across the current units,
   restricted to edges whose source and target identities exist in the canonical
   node set. This matches the legacy materialization's endpoint visibility rule.
3. **Retained contributor semantics.** Equivalent contributions do not change the
   canonical payload; non-equivalent contributions are rejected rather than
   ordered. No first-writer or last-writer decision is part of the oracle.
4. **Physical census.** For a single-repository database the physical node
   multiset must equal the canonical node set exactly once per identity; the
   physical edge multiset must equal the canonical edge set once per identity.
   Multi-repository expectations are the disjoint union of each repository's
   canonical sets when logical identities coincide.
5. **Manifest correspondence.** Every manifest entry `(logical id, native id)`
   must resolve to a physical row carrying that logical id; every physical
   protocol-marked row must be referenced by exactly one manifest entry; native
   ids are unique within a repository manifest.
6. **Endpoint correspondence.** Each physical edge's native source and target
   must be physical nodes, and its payload source/target logical ids must match
   the canonical edge.
7. **Traversal parity.** The ordered edge id sequence from the native mirror must
   equal the ordered sequence from the authoritative projection for the same
   query, including bounds and direction.
8. **Preservation.** Authoritative repository/run/unit/index-state counts,
   occurrence fact counts, and unrelated native sentinel rows must be unchanged
   by recovery.

## Negative controls

The oracle is applied to deliberately defective stores:

- injecting the exact residue of the old per-contribution rebuild (duplicate
  node rows and duplicate edge rows outside the manifest) must fail the oracle;
- deleting only the manifest while retaining physical rows must fail the
  correspondence oracle;
- the same test files copied unmodified to the unmodified entry seal must fail
  on the original production code with the original duplicate/accumulation
  symptoms.

The candidate must pass the same oracle after recovery. `baseline-reproduction/
negative-controls.txt` records the exact baseline failures.
