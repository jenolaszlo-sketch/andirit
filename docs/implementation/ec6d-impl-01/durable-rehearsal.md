# Authentic durable p9 to p10 rehearsal

The p9 seed was created from the accepted baseline before source modification,
using the real LatticeDB store and existing public indexing/snapshot APIs.
Preserved p9 binaries and source baseline are externally recoverable. No fake
p9 profile, edited historical fixture, guessed migration hash or duplicate
implementation checkout was used.

The committed [qualification harness](qualification/Program.cs) copies the p9
database and source files into a new disposable output directory. It verifies
the reopened seed against the p9 snapshot integrity hash, indexes through the
supported p10 lifecycle, and compares every old fact by ID and serialized value.

| Corpus | p9 nodes/declarations/edges | p10 nodes/declarations/edges | Replaced units |
| --- | --- | --- | --- |
| Controlled | 64 / 23 / 99 | 64 / 23 / 188 | 14 p9 units replaced; 15 p10 units |
| Website | 161 / 83 / 313 | 161 / 83 / 451 | 11 p9 units replaced; 12 p10 units |

Every pre-existing fact was preserved. No p9 unit remained. New occurrence,
context and attribution identities appeared separately. The same publication
reproduced results after unchanged indexing, in-memory snapshot import and
real close/reopen, including deterministic witness recomputation.

After migration, injected failed and cancelled extraction runs preserved the
last successful publication and exact query JSON despite edited disposable
sources. A subsequent successful delete removed its owning source unit, and
a second durable reopen retained the new result. The controlled delete removed
AbstractRenderer: the contracts target then correctly had three direct results
and four possible modules. This follow-up does not replace the primary
four/five acceptance assertion on the unchanged corpus.

External p9 seeds:

- Controlled snapshot integrity: `bb13f33007fc5281b1b4b209bff9e8e623a1b15e1d8e18195880c170d1e83db4`;
  p9 database SHA256 `dcd8465be8b9821e21961abb5f9f64356dd4944c2a01cc0976df3bd31f03861b`.
- Website snapshot integrity: `9adbc07e95dd70a783f95727b8fe65cfbd9f93a739afd63a0d53613350b77577`;
  p9 database SHA256 `a064df00e49f99787bd815c9072bc3baf2c02af5bfb421b790e5ca376d242fbe`.

These are this rehearsal's observed hashes. The historical EC-6C migration
hash remains unreproducible (FQ1-EV-1); no reproduction of it is asserted.
Durable integrity and deterministic replay do not establish immutable-source
snapshot consistency. Filesystem results remain qualified.
