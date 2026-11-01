# S2.1 specification — Verified Source Acquisition and Compilation Provenance

**Status:** Owner-authorized contract amendment, planning only. Production edits have not started; tests and independent acceptance are pending.

## Authority and purpose

Follow the owner-recorded [S2.1 authorization boundary](../../decisions/adr-ci1-01-ci1a-s2-1-authorization-boundary.md), accepted Stage 2 boundary, R2 provenance contract, owner decision §F, and the local [owner amendment draft](../../decisions/adr-ci1-01-ci1a-s2-1-project-reference-amendment.md). The owner authorized a narrow, explicitly versioned project-reference amendment so the mandatory contract is buildable without silently changing frozen R2 compctx1 identities.

The amendment preserves the exact R2 compctx1 descriptor algorithm and vectors for legacy use when no authoritative project references need representation. Verified production contexts use compctx2. This defines design authority only; production implementation and tests are not complete.

## Proposed verified identity

Use a compctx2 outer domain and contract-version field, with the same R2 descriptor material, source serialization, options digest, and PE file-reference identity. Change only the canonical project-reference item to:

compilation-context|<verified dependency compctx2>

The item must refer to the exact dependency CSharpCompilation instance used by the caller's live CompilationReference. Build project contexts bottom-up over a dependency DAG. Canonicalize reference order deterministically. If a required dependency is missing, cannot be verified, or participates in a cycle, refuse the verified context. Never fall back to R2's text-only project-reference item for a strong verified-project-reference claim.

Keep analysisscope1 unchanged. Compute compilation-universe1 using the already defined formula, with contractVersion set to compctx2 and ordered verified compctx2 digests. Reject mixed compctx1/compctx2 members in one verified universe.

The existing asmctx3 and usage4 helpers remain unchanged and reject compctx2. S2.1 must not bridge that gap by changing legacy helpers or minting occurrence facts. Before S2.2 can mint targets/occurrences, its separate commission must reconcile the compctx2-to-target-identity compatibility and acceptance tests.

## Source and compilation chain

Retain the actual immutable source snapshots, decoded texts, syntax trees, live compilations, descriptors, declared scope, and universe in an internal analysis result. Use the smallest internal trusted handoff from existing source materialization; do not add public types merely for plumbing.

- Hash exact immutable bytes that feed Roslyn. Preserve normalized path, BOM-aware decoding, language version, and actual preprocessor symbols.
- Use a fixed regular-document mode and bounded validation. Classify supported linked/generated/additional documents or refuse their provenance.
- Derive options from the actual compilation construction inputs.
- Load PE metadata from the same captured image bytes represented by canonical R2 file|path|SHA-256 identity.
- Bind project references to live instances and dependency compctx2 results, not caller-supplied descriptors.
- Explicitly refuse unsupported encoding/configuration, non-SHA planned source hashes, stale/mismatched inputs, missing dependencies, cycles, or incomplete declared scope. Do not report complete-empty or confirmed semantic facts from a refusal.
- Any legacy graph continuation behavior on an insufficient-provenance unit must preserve D3 and be specified/tested in the authorized implementation commission.

## Scope, exclusions, and gates

This slice establishes source and compilation provenance only. No extraction, usage4 production, asmctx3 minting, public Find Usages API, broad reader/continuation, coverage merge changes, native publication changes/redesign, B2, migration, dual write, background indexing, incremental pages, crash/power-loss claims, or universal completeness claims. Preserve existing graph IDs/counts, FindCallers/FindReferences, traversals, and native mirror behavior.

Do not use occurrence coverage merge as the scope/universe oracle. NEW-04 must be fixed before any S2.1 reliance on it; otherwise it remains due during S2.2. NEW-01 must be fixed before S2.3 or earlier recovery reliance.

The reference contract is now owner-authorized at planning level. P6/N05/N09 are implementable contract checks but remain pending code/tests. All P1–P7 and N01–N10 remain unimplemented and unpassed. No Q1 readiness or acceptance claim is made.

## Planned internal implementation shape

The proposed surface is internal and remains a plan until the commissioned source/tree review binds it:

- VerifiedCSharpSource.ReadAsync(CodeGraphSource, cancellationToken) owns or reuses the exact materialized bytes and exposes normalized Path, ContentHash, Text as Roslyn SourceText, EncodingName, and an optional Refusal reason. Construction remains private/static.
- VerifiedMetadataReference.Capture(path, cancellationToken) captures an immutable PE image, creates the Roslyn MetadataReference from that image, and exposes Reference and canonical Identity.
- VerifiedCSharpCompilation.Create(repositoryId, projectUnitId, CSharpCompilation, source snapshots, captured metadata references, verified dependency wrappers, cancellationToken) returns a wrapper exposing Compilation and Descriptor. Construction is private. It throws InvalidDataException on byte/tree/options/reference mismatch, unverified inputs, or unsupported/non-default reference properties.
- CompilationProvenanceDescriptor is an internal record with ContractVersion, RepositoryId, ProjectUnitId, AssemblyName, MetadataIdentity, ModuleName, OptionsDigest, ImmutableArray source identities, ImmutableArray reference identities, and DescriptorDigest. ComputeDigest and Verified recompute from fields. compctx1 and compctx2 use the exact R2 tuple/options/source serialization; only compctx2 uses verified dependency compctx2 reference items.
- Scope and CompilationUniverse construction remain separate internal types. Use friend-assembly tests rather than public API changes.

These names and signatures are planned, not implemented or accepted. Any required public surface returns to the owner before code.

Until the amendment is placed in the canonical authority set, implemented, and accepted, make no strong authoritative project-reference provenance claim. This local planning record is not itself runtime proof.
