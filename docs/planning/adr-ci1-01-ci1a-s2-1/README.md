# S2.1 planning

**Status: owner-authorized contract amendment implemented and committed. Final producer regression passed 645/645; independent Q1 qualification and owner acceptance remain pending.**

- [Specification](specification.md)
- [Provenance contract](provenance-contract.md)
- [Acceptance matrix](acceptance-matrix.md)
- [Qualification plan](qualification-plan.md)
- [Canonical owner amendment](../../decisions/adr-ci1-01-ci1a-s2-1-project-reference-amendment.md)

The owner authorized Option 1: preserve frozen R2 compctx1 and its vectors, and explicitly version the changed authoritative production identity as compctx2. The six requirements are recorded in the canonical owner amendment. Implementation is committed; the final regression passed. This plan does not claim independent qualification or owner acceptance.

Verified production project-reference identity will use the referenced dependency's verified compctx2, tied to the actual live CompilationReference instance. Dependency contexts are constructed bottom-up as a DAG. Missing dependencies and cycles refuse verified context; there is no fallback to R2's text-only project-reference item. Preserve R2 source/options serialization and PE file identities. Keep analysisscope1 stable; use the existing compilation-universe1 formula with contract version compctx2 and ordered compctx2 digests, with no mixed-version universe.

Frozen compctx1 vectors remain unchanged. Legacy asmctx3/usage4 helpers remain unchanged and reject compctx2. No occurrence extraction, occurrence minting, or native publication redesign is authorized here; S2.2 must separately reconcile target identity compatibility before any occurrence minting. Implementation and producer tests are complete; independent review and owner acceptance are pending.

See the [S2.1 authorization boundary](../../decisions/adr-ci1-01-ci1a-s2-1-authorization-boundary.md), the [canonical project-reference amendment](../../decisions/adr-ci1-01-ci1a-s2-1-project-reference-amendment.md), and [S2.1 provenance evidence](../../evidence/adr-ci1-01-ci1a-s2-1-provenance.md).
