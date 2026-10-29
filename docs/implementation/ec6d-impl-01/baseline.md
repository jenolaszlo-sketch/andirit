# Baseline and authority

Repository: `jenolaszlo-sketch/andirit`, repository ID `1345607723`.
Canonical checkout: `D:/projects/Andirit`; branch `main`.

Preflight verified local HEAD, main, origin/main and live remote main at
`fe1a9e37da49064f8cf6ec7fe9bc5ade7cfd7781`, tree
`181d232646248d62fcf3b43b58ec057e8daef392`. Working tree was clean;
`git worktree list --porcelain` showed exactly the canonical checkout.
No linked worktree, duplicate implementation checkout, history rewrite,
force push or sibling-repository modification was performed.

All 87 refs and complete history were preserved in an external Git bundle:
`ec6d-impl-01-evidence/baseline-fe1a9e37.bundle`, SHA256
`74ab2013161f783ba1cb02094eecf8806cbaf4880674fc80be921f1838d60aff`.
`git bundle verify` confirmed a complete, recoverable history.

## Accepted contract blobs

Verified against EC6D-OWNER-01 section D at HEAD and the accepted contract
commit `cc65338f50e585d78c3a1fa8e06d02c184a10451`:

| Contract | Blob |
| --- | --- |
| symbol-binding | `cb2b59b0d5e848c4b227cc86117ad753421ce784` |
| consumer-query | `8e0bb0f171d3761493fa561d2712d8fa27103c90` |
| publication-consistency | `91e4f8065b875713fc12c4953afbe2ccf3303f17` |
| acceptance | `73b9c6fe7c7305f88c266cdd2c374e75af7800c7` |
| scope-proposal | `85afde73e203bb59eae53ce40567aaa4cf4621e4` |

R16 evidence commit: `290ec6ec75cdc025dd1ef21684d6630ee9982168`.
The owner acceptance, all five contracts, and the seven requested R16 documents
were read together with current extraction, resolver, exports, publication,
query, serialization, durable-store and CI implementations.

## Baseline validation and fixture preparation

Windows / .NET SDK 10.0.401. Baseline restore and Release build succeeded
with zero warnings/errors. Tests: core 160, CSharp 47, LatticeDB 27,
TreeSitter 157: **391 passed, 0 failed, 0 skipped**.

The controlled corpus, all 14 committed Q2 cases and all 23 R2 cases were
identified before implementation. Existing module/export/class lifecycle,
snapshot, provider-identity and historical-storage tests were retained.
Existing EC-6C migration evidence was inspected without changing historical
fixtures or hashes.

Before modifying source, the baseline p9 binaries seeded a real LatticeDB
publication from the controlled corpus. Those preserved p9 binaries also
seeded the byte-verified website and the ambient-export counterexample.
The latter has no p9 export entry, reproducing the need for the explicit
owner correction. Seeds and their integrity-checked snapshots remain external.
