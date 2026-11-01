# S2.1 owner-authorized amendment — project-reference provenance identity

**Recorded:** 2026-11-02 (Asia/Manila). **Authority:** explicit user/owner direction in the S2.1 commission chat; additive amendment.

**Status:** Owner has authorized this narrow contract amendment. This local document records the authorization and its bounded design. Production code and tests remain pending. It does not rewrite any earlier accepted decision.

## Owner direction (as relayed)

> Preserve frozen R2 compctx1/vectors; explicitly version canonical changes; bind actual verified dependency inputs/output; source/options/parse changes propagate caller identity; deterministic order/unresolved/cyclic refusal; conditional public API +/- tests; refuse strong project ref proof until amended contract accepted and implemented.

The owner selected Option 1 and authorized the narrow amendment described below. No broader extraction, publication, schema, or architecture change is authorized.

## Amendment

1. **Freeze legacy R2.** Preserve the exact R2 compctx1 descriptor algorithm, reference forms, canonical validation, and frozen vectors for legacy contexts that do not claim authoritative project-reference provenance. Do not change the meaning of compctx1 in place.
2. **Version verified production contexts.** Use compctx2 as the outer digest domain and contract version for verified contexts. Preserve R2 descriptor material and field ordering, length-prefixed UTF-8 encoding, options/source serialization, and PE file reference identities.
3. **Bind project references to real dependency contexts.** Encode each verified project-reference item as compilation-context|<verified dependency compctx2>. The digest must be computed for the dependency CSharpCompilation actually held by the exact live CompilationReference instance in the caller's compilation. No supplied descriptor, display name, path/tree-only digest, or caller assertion substitutes for this binding.
4. **Propagate relevant dependency changes.** Because the dependency compctx2 includes its actual source bytes, parse settings/symbols, compilation options, and its own verified dependencies, changes in those inputs change the dependency identity and propagate into caller compctx2.
5. **Determinism and refusal.** Order source/reference items canonically. Construct dependency contexts bottom-up as a deterministic DAG. Missing or unverified dependency, live-instance mismatch, ambiguous/conflicting project identity, or cyclic dependency refuses verified context. Never fall back to the legacy project-reference text/tree identity for a strong verified claim.
6. **Scope and universe.** Keep analysisscope1 unchanged. Keep the compilation-universe1 domain/formula, setting its existing contractVersion input to compctx2 and hashing ordered compctx2 descriptor digests only. A verified universe cannot mix compctx1 and compctx2.
7. **Compatibility boundary.** Keep legacy asmctx3 and usage4 helpers unchanged; they continue to require compctx1 and reject compctx2 as defined by R2. S2.1 does not mint asmctx3 or occurrence facts. S2.2 must separately reconcile how verified compctx2 contexts feed target/occurrence identity; no compatibility behavior is assumed here.
8. **Public API and tests.** Keep new context machinery internal unless an implementation necessity is separately brought to the owner. Add positive/negative tests for the approved behavior. Do not expose a public provenance or test-hook API by default.
9. **Stage boundary.** No extraction, native publication redesign/change, coverage merge change, migration, dual-write, or expansion into unrelated work.

## Consequences and required evidence

- Old compctx1 vectors remain stable and must be independently rechecked.
- Verified project-reference identities change to compctx2, while target identity helpers do not accept compctx2. This intentional compatibility gap remains until the separately authorized S2.2 reconciliation.
- Tests must establish propagation for dependency source, parse-symbol, compilation-option, and dependency-reference changes; stability under enumeration reorder; and conservative refusal for missing, cyclic, conflicting, or mismatched live references.
- Merely changing caller metadata inputs is insufficient; evidence must connect the exact dependency CSharpCompilation instance, its compctx2, and the caller's CompilationReference.
- All proposed tests are pending. No implementation or independent qualification is claimed.

This amendment is additive. Earlier owner decisions and frozen R2/Q3 evidence remain preserved and unchanged.

**Conditional API/test rule:** provenance remains internal by default. If a later authorized implementation requires public visibility, record that decision and its API boundary first, then add positive and negative tests for the approved public/internal surface. Do not make the conditional into an implicit public API authorization.

Until the amended contract is canonically recorded, implemented, and accepted, strong authoritative project-reference proof remains unavailable. No production result is claimed by this planning record.
