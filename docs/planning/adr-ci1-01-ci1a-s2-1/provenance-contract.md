# S2.1 provenance contract

**Status:** Owner-authorized narrow amendment. Frozen R2 compctx1 behavior and vectors remain unchanged. Verified production project-reference contexts use compctx2. Implementation and tests are pending.

## 1. Frozen legacy compctx1

Preserve the exact R2 compctx1 descriptor algorithm and frozen vectors exactly. Retain its accepted domain, tuple, source/options serialization, reference representations, length-prefixed UTF-8 encoding, and canonical validation for legacy compatibility. Do not redefine compctx1 or silently substitute a stronger project-reference string under the same domain.

R2 compctx1 must not be treated as authoritative proof of a project dependency's effective compilation context when only its text/tree-based reference representation is available. The R2 project-reference form omits the referenced compilation's options, preprocessor-symbol identity as a separate descriptor input, and its own references/transitive dependency closure. It remains frozen for legacy vectors; verified production project-reference proof uses compctx2.

## 2. Verified production compctx2

Use an explicitly versioned outer identity:

compctx2:sha256: + SHA-256 over domain compctx2-context and the existing R2 descriptor material in the same field order and length-prefixed UTF-8 encoding. Set contract version to compctx2. Preserve R2 fields: repository, normalized defining project-unit ID, assembly name, metadata identity, module, actual options digest, ordered normalized source entries with exact byte SHA-256/language version/preprocessor symbols, and ordered reference identities.

Preserve the R2 options digest inputs and serialization. Preserve PE reference identity as file| + normalized path + | + SHA-256 of the exact captured PE bytes used to construct Roslyn metadata. Hashing a path separately from the metadata image actually used is insufficient.

For a verified project reference, use this ordered reference item:

compilation-context|<verified dependency compctx2>

The dependency context must be computed from the actual referenced CSharpCompilation used by the exact live CompilationReference in the caller compilation. Do not use a caller-supplied dependency digest, descriptor, path/text-only R2 compilation identity, or merely matching assembly name as proof. The dependency context itself includes its own verified reference identities, so effective project dependency changes propagate transitively.

Construct the project-reference dependency graph bottom-up as a deterministic DAG. Sort canonical source and reference entries using ordinal order as specified by the R2 serialization. Refuse cycles, missing/unverified dependency contexts, duplicate/conflicting project identities, or instance mismatch. There is no fallback to the legacy R2 text/tree-only project-reference item for a strong verified context.

## 3. Scope and universe

Keep analysisscope1 formula and normalized repository/project-unit/path inventory unchanged. Keep compilation-universe1 domain and formula unchanged, but set its existing contractVersion input to compctx2 and include only ordered verified compctx2 descriptor digests. A verified universe cannot mix compctx1 and compctx2 entries. Every declared scope member requires a verified live compilation context. Completeness remains relative to declared scope.

## 4. Compatibility boundary

Do not change frozen R2 compctx1 vectors, legacy asmctx3 or usage4 helpers. Existing asmctx3 logic that requires canonical compctx1 rejects compctx2; it must remain that way in S2.1. S2.1 does not mint targets or facts. S2.2 needs a separately reviewed compatibility contract and acceptance evidence before it can consume compctx2 and mint any target/occurrence identity.

## 5. Source-to-tree authority

Retain immutable owned source buffers, decode them with explicit BOM-aware rules, and build Roslyn trees from exactly those decoded buffers. Record normalized path, byte hash, decoded text/tree association, parse language version and actual preprocessor symbols. Use supported regular documents in a bounded one-pass validation. Unsupported encodings/configurations and non-SHA planned hashes refuse provenance. Stale or mismatched inputs may only be used if the retained snapshot is still the exact input Roslyn compiled and is attributed as that snapshot.

Capture actual compilation options. Construct PE references from captured PE images. Bind project references by exact live compilation-instance association and verified dependency compctx2. Refuse when that proof is missing or cyclic; never convert refusal to successful empty analysis.

This is a planning contract. No implementation, test result, or independent acceptance is claimed.

## Planned internal API outline

The implementation plan uses internal-only VerifiedCSharpSource.ReadAsync, VerifiedMetadataReference.Capture, VerifiedCSharpCompilation.Create, and CompilationProvenanceDescriptor. Source capture owns/reuses existing bytes and exposes normalized path, hash, SourceText, encoding, and refusal. Metadata capture binds an immutable PE image to the Roslyn reference and identity. Compilation creation verifies actual options, exact source/tree correspondence, PE references, and dependency wrappers, returning the live compilation and verified descriptor. Construction stays private and invalid inputs throw InvalidDataException. The descriptor holds the R2 fields with immutable arrays and recomputing ComputeDigest/Verified methods. Tests use the existing friend-assembly mechanism; public API changes are not assumed. Exact production source binding and signatures remain to be reviewed before implementation.
