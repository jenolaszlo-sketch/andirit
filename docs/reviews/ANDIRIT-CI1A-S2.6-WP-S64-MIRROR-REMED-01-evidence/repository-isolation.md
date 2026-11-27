# Repository isolation

## Ownership model

A repository's mirror is defined by its own `HetuMirror` row: a mapping from
logical node/edge identity to native id. Physical mirror rows carry no repository
id; the manifest is the ownership record and is written in the same transaction
as the rows it names.

## Sweep rule

Before a rebuild or after a disabled deletion, the store removes protocol-marked
mirror rows whose native id is not referenced by any manifest in the database.
The retained set is computed from **all other** manifests read inside the same
transaction. It is therefore impossible for a repository's live mirror rows to
be swept by another repository's rebuild.

If two repositories contain the same logical node identity, each gets a distinct
native node and a distinct manifest entry. Both native ids are retained by the
other manifest. A disabled publication for one removes only that repository's
rows.

## Evidence

`MultipleRepositories_WithSharedLogicalIdentity_StayIsolated`:

1. publishes two repositories into one database, both containing
   `node:shared` plus one repository-specific node;
2. reopens enabled twice and traverses both repositories;
3. asserts two physical rows for `node:shared`, four physical nodes total, two
   manifests, and complete correspondence for each repository;
4. publishes a disabled run for the first repository;
5. asserts the second repository's manifest, rows and traversal are intact and
   the first repository's rows are gone.

`DisabledPublication_RemovesPhysicalMirrorObjects_AndReEnableRebuilds` and the
recovery receipts additionally prove that the orphan sweep does not cross into
referenced rows.

## Related isolation evidence

The existing multi-process publication tests remain green. LatticeDbSharp's file
lock serializes writers, so a manifest and its rows are never concurrently
observed halfway through a rebuild.
