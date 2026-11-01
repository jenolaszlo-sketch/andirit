# S2.1 acceptance matrix

**Status:** Planning matrix. Contract is owner-authorized; all implementation checks and independent qualification are pending.

| ID | Required behavior | Evidence | Status |
| --- | --- | --- | --- |
| P1 | Immutable verified bytes feed actual decoded Roslyn trees | Raw bytes/hash, normalized path, BOM/encoding, decoded text/tree correspondence | Pending |
| P2 | Frozen compctx1 and R2 vectors remain byte-for-byte unchanged | Independent recomputation of all frozen vectors and legacy helper checks | Pending |
| P3 | Verified compctx2 records actual inputs and options | Field-level observation; source/option/parse mutations change identity | Pending |
| P4 | PE reference identity matches captured image used for metadata | Captured image, normalized path, hash, and Roslyn binding evidence | Pending |
| P5 | Scope and universe cover declared live contexts deterministically | Multi-unit proof; omission/duplicate/repository mismatch negatives; compctx2-only universe | Pending |
| P6 | Project reference uses dependency compctx2 bound to exact live reference instance | Valid dependency DAG positive; recomputation and instance proof | Pending |
| P7 | Stale/unsupported/missing proof refuses without false complete/empty result | Changed bytes, wrong hash, mismatched tree, unsupported encoding/config, non-SHA plan | Pending |

| ID | Adversarial case | Required result | Status |
| --- | --- | --- | --- |
| N01 | Supplied source hash differs from buffer | Refuse | Pending |
| N02 | Roslyn tree differs from decoded verified buffer | Observable refusal | Pending |
| N03 | Disk changes after acquisition | Detect or retain and attribute exact immutable snapshot | Pending |
| N04 | Source, actual options, parse language version, or symbols change | Verified compctx2 changes; stale proof invalidated | Pending |
| N05 | Referenced project options/symbols/sources change | Dependency compctx2 and caller compctx2 change | Pending |
| N06 | Required defining/dependency compilation is absent | Explicit refusal, no old text-only fallback | Pending |
| N07 | Scope incomplete, identities conflict, or universe mixes contract versions | Refuse completeness/universe construction | Pending |
| N08 | Caller-supplied descriptor/digest bypass attempt | Recompute and require actual construction/live authority | Pending |
| N09 | Project-reference inventory or transitive dependency changes | Caller compctx2 changes; stale proof refused | Pending |
| N10 | Only enumeration order changes | Same canonical identities through deterministic ordering | Pending |

P6/N05/N09 now have an owner-authorized design rule but remain unimplemented and untested. All checks require producer evidence and independent review. The earlier supplied diagnostic is only preliminary partial N05 evidence; it is not acceptance evidence for compctx2.

The supplied unchanged-baseline report is 594 passed, 0 failed, 0 skipped on .NET SDK 10.0.401 / .NET 10.0.12, Roslyn 5.9.0, win-x64. It does not qualify these checks.

The proposed API tests use the friend assembly and remain pending: verify source capture properties/refusal, metadata-image identity, compilation descriptor recomputation and InvalidDataException on mismatches, dependency-instance binding, deterministic compctx2, and unchanged frozen compctx1 vectors. No public API is added by this outline.
