# CI1A S2.2 identity and provenance handoff

**Recorded:** 2026-11-02. **Commission:** `ADR-CI1-01-CI1A-IMPL-01-S2.1-OWNER-01`.
**Authority:** [S2.1 owner acceptance](../../decisions/adr-ci1-01-ci1a-s2-1-owner-acceptance.md), [S2.2 target identity decision](../../decisions/adr-ci1-01-ci1a-s2-2-target-identity-decision.md), [pre-release compatibility policy](../../decisions/adr-ci1-01-pre-release-compatibility-policy.md), [obligation register](../../decisions/adr-ci1-01-ci1a-s2-obligation-register.md).
**Status:** Planning handoff only. S2.2 implementation is NOT authorized.

**Readiness update (2026-11-03):** [S2.0/F12 OWNER-01](../../decisions/adr-ci1-01-ci1a-s2-0-corpus-01-owner-acceptance.md)
accepts the frozen R2 corpus **WITH QUALIFICATIONS**. Q3 remains **SUPPORTED
WITH QUALIFICATIONS**. The F12 reference prerequisite before S2.2
qualification-test authoring is now complete; other authoring/minting/acceptance
gates are unchanged. **S2.2 SEMANTIC EXTRACTION — NOT AUTHORIZED BY OWNER-01**.
See [current reference status, gate order and work queue](s2-0-owner-handoff.md).
Fixture acceptance supplies no production canonicalization/provenance or native
publication qualification.

## 1. Selected identity direction

**`S2.2-ID OPTION A ACCEPTED`** — `asmctx3` remains the semantic target identity; `compctx2` governs provenance validity and invalidation. No new target identity version is introduced.

Justification summary (full analysis in the decision): `asmctx3` already binds the defining compilation's R2-visible identity (sources with preprocessor symbols, material options, assembly/metadata/module, PE references), so declaration-changing inputs already re-identify the target. The remaining transitive-dependency-only case does not change the declaration, so identity stability is correct and `compctx2` is the invalidation signal. Reviewer-owned experiment: conditional API change → `asmctx3` changes; transitive-only change → `compctx2` changes at all levels while the defining project's R2 material is unchanged; caller source unchanged but target identity follows the defining conditional API.

## 2. Canonical identity relationships (binding on S2.2)

```
defining compilation
├─ compctx1   legacy R2 compilation context (frozen; includes sources/options/PE refs)
│    └─ asmctx3  semantic target identity = f(compctx1, metadata, module,
│                 identifierDigest, declarationDigest, shapeDigest)      [frozen]
├─ compctx2   verified dependency-sensitive provenance (authorized amendment)
│    └─ cross-project reference items: "compilation-context|<dep compctx2>"
├─ analysisscope1            declared unit set
└─ compilation-universe1     verified compctx2-only member set

occurrence
├─ usage4 = f(repository, plugin, semanticsVersion, semanticUnit, path,
│            sourceHash, span, targetDocumentId, asmctx3, category)      [frozen]
└─ validity gate = current compctx2 of the defining context (transitive closure)
                  + compilation-universe1 membership + publication freshness
```

Roles are disjoint: `compctx2` never enters `usage4`/`asmctx3`; `asmctx3` never substitutes for provenance validity; storage schema identity remains separate.

## 3. Mapping contract S2.2 must implement

1. Derive the R2-faithful `compctx1` of each verified defining compilation deterministically (frozen tuple; R2 project-reference representation where the compilation has dependencies). This is a mapping, not a redefinition; it is not a strong verified project-reference claim.
2. Mint `asmctx3` only for targets whose defining compilation is verified and whose target fields are canonical.
3. Gate acceptance/reuse of semantic results on the defining context's current `compctx2` (which embeds its transitive dependency contexts) and universe membership.
4. Record the defining `compctx2` (or an equivalent unambiguous provenance association) in publication/coverage evidence so invalidation survives reopen.
5. On any `compctx2` change of the defining context, invalidate prior occurrence facts for that context: refuse or require re-ingestion. No silent reuse.
6. Keep target identity stable across dependency-context-only changes (no churn).
7. Refuse minting from unverified/incomplete contexts; never produce successful empty analysis from a refusal.

If (4) cannot be enforced, stop and return to the owner before minting (possible Option B).

## 4. No-migration policy

Per the pre-release policy: superseded experimental occurrence identities/schemas are refused or re-ingested; no migration, dual-write, legacy readers or version adapters. Historical vectors, decisions and evidence remain preserved. Existing D3 graph semantics and D5 graph/Hetu compatibility are untouched.

## 5. Required S2.0/F12 semantic corpus (before S2.2 qualification-test authoring)

The corpus must have reproducible C# source, independently established expected relationships, cross-project references, multiple defining compilation contexts, conditional compilation, equivalent and distinct target cases, negative/refusal cases, and frozen expected identity/resolution outcomes, independently accepted before S2.2 qualification tests. **Satisfied at the reference boundary by S2.0 OWNER-01 (2026-11-03):** source-bound R2 base corpus independently supports 11 files, 53 invocations, 21 method references, 5 overloads, 5 namespaces, 5 projects and 52 cross-project invocations. The former 9-file/32-row seed was nonqualifying; its history is preserved. All original native end-to-end and later production gates remain. Frozen S2.0 artifacts are unchanged; the owner handoff records current status.

## 6. Authorization prerequisites (all distinct; none waived by S2.1 acceptance)

| Prerequisite | Gate |
|---|---|
| S2.1 owner acceptance | **Complete** (S21A) |
| S2.2 identity mapping decision | **Complete** (S22ID, Option A) |
| S2.2 mapping contract implemented + independently reviewed | Before S2.2 target minting |
| S2.0/F12 frozen semantic oracle accepted | **Complete with qualifications** (S2.0 OWNER-01, 2026-11-03); prerequisite before S2.2 qualification-test authoring satisfied; F16/NEW-02 confirmation and all other gates remain |
| NEW-04 coverage merge disposition | Before any coverage reliance; otherwise before S2.2 acceptance |
| Q-F2 encoding boundary | Before S2.2 acceptance |
| Q-F3 metadata validity | Before S2.2 semantic acceptance |
| S-01 defining-target binding | During S2.2, independently qualified |
| NEW-01 projection recovery | Before S2.3 / earlier recovery reliance |

## 7. Suggested S2.2 slice decomposition (future authorization only)

1. R2-faithful `compctx1` derivation for verified dependency-bearing compilations + independent vectors.
2. Target minting under the mapping contract with live-instance/live-context gating.
3. Durable `compctx2` provenance association in publication/coverage evidence.
4. Invalidation/refusal paths (context change, stale publication, reopen).
5. Negative/refusal matrix (external/ambiguous/missing defining compilation; unverified context; mixed versions).
6. Integration tests over the accepted S2.0 corpus; independent Q1 review; owner acceptance.

No item is authorized by this handoff.
