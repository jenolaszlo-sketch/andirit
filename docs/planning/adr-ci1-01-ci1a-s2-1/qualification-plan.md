# S2.1 qualification plan

**Status:** Pending. The owner-authorized design is documented; production implementation, test results, and independent qualification are pending.

## Evidence package

For every check retain pinned Andirit source/tree identity; exact frozen R2 compctx1 implementation and vectors; compctx2 contract implementation; reproducible commands/environment; immutable source and PE bytes/hashes; decoding/tree/options; actual live references and dependency graph; scope/universe/context identities; raw positive/refusal results; and independent digest recomputation. Producer tests are not independent qualification.

## Required sequence

1. Implement the frozen R2 compctx1 path without changing its identity outputs or vectors.
2. Implement verified compctx2 with the same R2 material/options/source serialization and PE identities, but project references use compilation-context|<verified dependency compctx2> tied to the exact live CompilationReference instance.
3. Build dependency contexts bottom-up in deterministic order. Test source/options/parse/reference mutations propagate through dependency and caller compctx2. Refuse missing dependencies, instance mismatch, conflicts, mixed universe versions, and cycles; do not fallback to the old project-reference text/tree identity.
4. Keep analysisscope1 unchanged and use the existing compilation-universe1 formula with contractVersion compctx2 and ordered compctx2 digests only.
5. Verify that legacy asmctx3/usage4 helpers remain unchanged and reject compctx2. Do not mint targets or occurrence facts in S2.1. S2.2 must separately reconcile this compatibility boundary.
6. Complete P1–P7/N01–N10 and obtain independent review that binds results to source/tree, recomputes IDs, checks refusals, and inspects raw evidence. Owner accepts S2.1 before S2.2 authorization.

## Baseline and earlier diagnostic

The supplied baseline report records 594 passed, 0 failed, 0 skipped on .NET SDK 10.0.401, .NET 10.0.12, Roslyn 5.9.0, win-x64. This is baseline-only.

The earlier diagnostic reported unchanged caller compctx1 after referenced parse-symbol and nullable mutations. That is preliminary partial N05 evidence of the frozen R2 behavior, not compctx2 implementation or acceptance. No independent qualification has occurred.

## Stop conditions

Stop if frozen compctx1 outputs/vectors change; compctx2 requires an unapproved identity alteration; source/tree or PE image binding fails; a project dependency is missing, cyclic, or unverified; a mixed-version universe is accepted; refusal becomes complete-empty; D3/D4/D5 is weakened; or scope expands. S2.1 acceptance does not authorize S2.2.

Future canonical evidence destination: [S2.1 provenance evidence](../../evidence/adr-ci1-01-ci1a-s2-1-provenance.md).

## Proposed internal surfaces (not implemented)

- VerifiedCSharpSource.ReadAsync(CodeGraphSource, ct): owns/reuses lifecycle bytes; exposes Path, ContentHash, SourceText Text, EncodingName, and Refusal; private construction.
- VerifiedMetadataReference.Capture(path, ct): immutable captured PE image, Roslyn Reference, canonical Identity.
- VerifiedCSharpCompilation.Create(repo, unit, live CSharpCompilation, source snapshots, captured metadata references, verified dependency wrappers, ct): wrapper exposes Compilation and Descriptor; private construction; InvalidDataException for mismatch, unverified input, or non-default reference properties.
- CompilationProvenanceDescriptor: internal immutable record fields for contract version, repository/project, assembly/metadata/module, options digest, immutable source/reference arrays, descriptor digest; ComputeDigest and Verified recompute. Keep compctx1/compctx2 R2 material serialization exact; compctx2 project refs are verified dependency context identities.

Use friend-assembly tests, not public API exposure. Scope and universe construction remain separate. Names/signatures must be rechecked against the pinned source before production work.
