# HETU-INT-01 handoff from EC6D-OWNER-01

**Readiness:** planning accepted with qualifications; integration is
separately authorized. See the [owner record](../../decisions/ec6d-owner-01-contract-acceptance.md).
No merge, push, rename, migration or implementation occurred here.

## Immutable baseline and preservation

Accepted contracts: commit `cc65338f50e585d78c3a1fa8e06d02c184a10451`,
tree `9940965196b35488258501e9513ec1d8025d4007`, parent Q3 final
`f3de16ffd04015f345fcdd65e8ca996fdb19555d` / tree
`77b5eacf2fdfea8be9f41be10da11c074be566ba`. The owner-record commit is
the immediate child of the contract commit; obtain its exact final identity
from the completion handoff and verify the record's content before use.
Do not use a floating branch alone as an integration candidate.

Required lineage includes recovered original `91a3ed5`, R1 `0e1b213`,
R2 `3ce7dd9`, Q3 content `9ec3d7f` and final `f3de16f`, corrected
contracts and final owner record. All full identities are in the owner
record and [preflight refs](analysis/owner-01-preflight.json).
Also preserve the off-ancestry Q1 and Q2 reviews (including Q2 supersession),
EC-6B Q1/Q2, EC-6C design/reviews/qualification and prior owner decisions;
their evidence must remain accessible by original commit identities.
Do not sweep unrelated EC-3/4/5 preservation/review branches into the
integration candidate merely to preserve their objects.

## Dependencies and required qualifications

| Dependency | Exact tip | Obligation |
| --- | --- | --- |
| Canonical main | `558f8ac82ac1b5e07da68f4a71a70240eaf38409` | p6 baseline; unchanged during OWNER-01 |
| EC-6A acceptance | `1d11b7330e1d8755d5c3e3b4a830167bcf8b5bf6` | accepted resolution architecture |
| EC-6B remediation | `f8fb9d1c2917ef04fdf9bca721bcbeaaecb959e9` | relative-v1/resolver2 p8 semantics, qualified p6→p8 |
| EC-6B owner acceptance | `038295f26ce76a8747e923f9e0ef971e773c166e` | retained qualifications, separately authorized integration |
| EC-6C.1 inventory | `8c1d5e3faf1fbc33162a2fa87dacfbbdf84f4340` | narrow allowlist, occurrence multiplicity, A29 bounds |
| EC-6C.2 implementation | `3ab1f144e31155cf3cd4bac87c4739f0a6eccb7a` | p9 positive-only export facts, p8 facts/evidence retained |
| EC-6C independent qualification | `54ca7156dbc2354843899a619c05128615e3d806` | preserve separate branch; Windows/public-store/small-fixture limits |
| EC-6C owner acceptance | `2c3090cba6d8a56102a5c47ceb026c5b5fea3234` | migration-hash limitation, QN-1, withheld later authority |

Use [integration readiness](integration-readiness.md) and the
[65-file dependency manifest](integration-delta.md). Verify candidate
production correspondence to qualified EC-6B/EC-6C; OWNER-01 adds no
production changes. Preserve InMemory/LatticeDB public contracts, snapshots,
atomic unit replacement/deletion, fail-on-change full publication identity,
existing queries/defaults/presets and all p9 identity/evidence/properties.

HETU-INT-01 must obtain authorization and perform a representative durable
p6→p9 rehearsal (or explicitly approved staged p6→p8→p9 procedure): compare
unchanged facts and added exports, deletion/re-index, close/process reopen,
failure/cancellation retention and rollback. Preserve historical
FQ1-EV-1/QN-1 qualifications and store-specific requirements. Future EC-6D
schema/provider additions require a separate p9→new-profile rehearsal on
both stores, with existing p9 facts byte-identical for unchanged source.

Run relevant EC-6B relative resolution/remediation and EC-6C inventory,
export publication, identity, provenance, A29, lifecycle and migration
regressions; existing core/CSharp/TreeSitter/LatticeDB compatibility suites,
snapshot import/export and defaults/presets; reconcile the root README's
pre-p9 table. For future EC6D-IMPL-01 the entire accepted A01–A28 matrix
(including A04b/A10b/A18b) remains required on both stores; R16 consumer
confirmation is due at or before implementation authorization. Do not claim
OWNER-01's documentation checks satisfy those implementation tests.

## Separate Git object databases: future procedure constraints

Canonical clone: `C:/Users/Laszlos/source/repos/Penghou.Hetu`;
planning mirror: `C:/Users/Laszlos/source/repos/Solo/.hetu-ec6d-planning-mirror.git`.
Canonical main is authenticated; Q3 is absent from the canonical database.
The mirror's origin is a local planning bundle, not the GitHub remote.

Under HETU-INT-01 authorization, enumerate and archive the exact required
refs/objects, transfer history through a verified bundle or fetch into
explicit preservation refs, then verify full commit/tree/blob identities,
ancestry and connectivity in the canonical database before any integration.
Compare manifests/qualification evidence and preserve off-ancestry reviews
via named refs. This is a procedure requirement, not a transfer executed by
this task. Worktree directories are checkouts, not mergeable object stores.
No force-push, provenance-destroying squash, or manual file copying claimed
as original Git identity preservation. Obtain approval of exact candidate,
target and integration method before merge. Retire historical branches or
worktrees only after their required commits/evidence/refs and any local
state are preserved and verified; current clean state is not retirement
authorization.

## Rename compatibility inventory (documentation only)

| Identity surface | Current assumption / future constraint |
| --- | --- |
| Namespaces and assemblies | `Penghou.Hetu` and Abstractions/CSharp/TreeSitter/LatticeDb/Testing project identities; assembly defaults follow project names. Audit public/serialized/reflection consumers before changing |
| NuGet IDs | `Penghou.Hetu`, `.Abstractions`, `.CSharp`, `.TreeSitter`, `.LatticeDb`, `.Testing`; package compatibility/versioning needs its own plan |
| Public API | CodeNodeId/SymbolId, CodeGraphPublication, publication-bound query types, existing query names; planned GetInterfaceImpactV1 and discriminated response remain contract-bound |
| Graph/source identity | path/owner-derived declaration ids; language-prefixed file/export/heritage-target ids; export hashes use modulePath + LF + public name. A product rename must not silently change graph paths, source hashes or these identities |
| Persisted schema and migration markers | store/snapshot versions, index-unit origins, plugin/provider inventories, p6/p8/p9 and exports1/resolver2 markers must be explicitly preserved or migrated; no mixed publications |
| Provider/witness identity | penghou.hetu.tree-sitter.*; imports1.local-v1, heritage1.local-v1, module1.local-v1, exports2.local-v1, resolver2.relative-v1; planned binding1.local-v1 canonical fact-id/policy tuple. Preserve or explicitly version/re-index; no silent witness reinterpretation |
| CI/CD paths | Penghou.Hetu.slnx, src/tests/benchmarks project paths; .github/workflows/ci.yml restore/format/test/pack and coverage filters, publish.yml project/package list, benchmarks.yml. Audit packaging/native runtime paths and smoke consumers |

No rename or identity migration is authorized here. Sequence after explicit
downstream instruction: **HETU-INT-01 → ANDIRIT-RENAME-01 → EC6D-IMPL-01**.
No release/platform/performance claim follows from planning acceptance.
