# CI1A S2.2 mapping contract (Option A)

**Commission:** `ADR-CI1-01-CI1A-IMPL-01-S2.2` (WP3 foundation).
**Status:** specification only. **No extraction, minting or native publication is
implemented by this document.** Target minting remains blocked behind `G-MINT`.
**Authority:** [S2.2 target identity decision](../../decisions/adr-ci1-01-ci1a-s2-2-target-identity-decision.md) §5;
[identity contract](../adr-ci1-01-ci1a-s2-rem-01/identity-contract.md);
[S2.1 provenance contract](../adr-ci1-01-ci1a-s2-1/provenance-contract.md);
frozen R2 authority copy `docs/evidence/adr-ci1-01-ci1a-s2-1-raw/authority/CompilationProvenance.cs`;
[entry acceptance](../../decisions/adr-ci1-01-ci1a-s2-2-entry-acceptance-owner-01.md) §3.5.

---

## 1. Identity invariants (binding)

1. **Semantic target identity is `asmctx3`**, computed with the frozen R2
   algorithm over the R2-faithful `compctx1` of the **defining** compilation:
   `asmctx3:sha256:` + SHA-256 over domain `asmctx3-context`, `"compctx1"`,
   canonical `compctx1` value, `metadataIdentity`, `moduleName`,
   `identifierDigest`, `declarationDigest`, `shapeDigest`.
2. **Occurrence identity is `usage4`**, frozen, using the target's `asmctx3`
   (domain `usage4`, semantics `usage4-b2c-exp-r2-20261101`, category
   `method-invocation`, 32-bit big-endian length-prefixed UTF-8 parts).
3. **`compctx2` is provenance validity only.** It governs reuse/invalidation and
   universe membership.
4. **No new identity version** is introduced (Option A).
5. **No conflation.** `compctx2` never enters `asmctx3` or `usage4`; `asmctx3`
   never substitutes for provenance validity; analysis generation, publication
   freshness and storage schema identity stay separate.
6. **No identity churn.** A dependency-context-only change does not re-identify
   an unchanged declaration.
7. All hashing uses the 32-bit big-endian byte-length prefix and lowercase hex;
   canonical target is `asmctx3:sha256:<64 lowercase hex>`; canonical `compctx1`
   is `compctx1:sha256:<64 lowercase hex>`.

## 2. R2-faithful `compctx1` derivation (the mapping)

S2.1 intentionally exposes **no legacy twin** for dependency-bearing
compilations (`VerifiedCSharpCompilation.LegacyDescriptor == null` when
`Dependencies` is non-empty). S2.2 derives the R2-faithful `compctx1` of a
verified defining compilation deterministically. This is a **mapping, not a
redefinition**.

Given the verified `compctx2` descriptor of the defining unit, derive:

```
compctx1 = "compctx1:sha256:" + SHA256("compctx1-context",
  "compctx1", repositoryId, projectUnitId, assemblyName, metadataIdentity,
  moduleName, optionsDigest, sources..., references...)
```

where the parts are exactly the production verified fields already held by
`CompilationProvenanceDescriptor` (repository, normalized project unit,
assembly name, metadata identity, module name, `OptionsDigest`, ordered source
canonical forms), with **one substitution**: each verified project-reference item
`compilation-context|<dependency compctx2>` is replaced by the frozen R2 legacy
project-reference item reconstructed from the **verified live dependency**:

```
"compilation|" + dependencyAssemblyName + "|" + dependencyMetadataIdentity + "|" + trees
trees = join(",", LiveDependency.SyntaxTrees
          .OrderBy(tree => tree.FilePath, StringComparer.Ordinal)
          .Select(tree => NormalizePath(tree.FilePath) + "=" +
                          SHA256Hex(UTF8(tree.GetText().ToString()))))
```

- PE/metadata references keep their verified `file|<normalized path>|<sha256>`
  identity (the captured bytes used to build the Roslyn reference).
- Ordering is ordinal over sources (path, then content hash) and over
  references, matching R2.
- Source content hashes are the **exact verified byte SHA-256**, matching R2
  `Describe` when `sourceBytes` is supplied (not a re-hash of decoded text).
- The dependency tree hashes are the R2 **text/tree** form (decoded-text UTF-8
  SHA-256), which deliberately omits the dependency's options, preprocessor
  symbols as a separate input, and transitive references.

**Boundary (explicit):** this mapped `compctx1` is **not** a strong verified
project-reference claim. The strong claim is carried by `compctx2`. The mapping
exists solely to preserve frozen `asmctx3` while keeping `compctx2` authoritative
for validity. It must reproduce the frozen R2 corpus vectors for the accepted
defining contexts.

## 3. Extraction-time association invariant

For every candidate `Invocation` occurrence, before any fact is minted:

1. The defining declaration resolves to exactly one verified live compilation
   instance in `compilation-universe1` (no text-only or descriptor-only proof).
2. `compilation-universe1` is complete for the declared `analysisscope1`.
3. The defining compilation's **current verified `compctx2`** equals the context
   under which the occurrence is produced; the defining unit is a universe
   member carrying that same verified context.
4. The target's `asmctx3` is minted from the mapped `compctx1` (§2).
5. The defining `compctx2` (association) is recorded **separately** from the
   identity tuple; it never enters `asmctx3`/`usage4`.

If any of 1–3 fails, no fact is emitted; the case is classified with the correct
coverage state (`External`, `Ambiguous`, `Unresolved`, `Unsupported`,
`ConditionalInactive`, `Incomplete`) and never as successful empty.

## 4. Durable carrier boundary (S2.2/S2.3)

- **S2.2 (in scope):** hold the defining `compctx2` association as an explicit,
  separate provenance record on the occurrence-evidence model; **enforce** it at
  extraction time; invalidate on any defining-`compctx2` change.
- **S2.2 (out of scope):** native persistence and reopen enforcement.
- **S2.3 (separate authorization):** persist the association in the native
  occurrence unit/meta and enforce it on reopen; **the first S2.3 gate** is a
  reopen test proving a stale/changed defining `compctx2` refuses prior facts.
- **Escalation:** if S2.3 cannot persist and enforce the association after
  reopen, stop and return to the owner with the bounded **Option B** alternative
  before any production reliance.

## 5. Positive and negative mapping examples

| Case | Input | Expected |
| --- | --- | --- |
| P1 | Standalone (dependency-free) defining compilation | Frozen R2 `compctx1`/`asmctx3` reproduced exactly |
| P2 | Dependency-bearing defining compilation, dependency unchanged | Mapped `compctx1` deterministic; `asmctx3` stable |
| P3 | Dependency API changes (defining project's own R2 fields change) | `compctx1`/`asmctx3` change; old facts invalid |
| P4 | Transitive-only change (dependency context changes, defining R2 fields unchanged) | `asmctx3` **stable**, `compctx2` changes at all levels, old facts invalid |
| P5 | Caller source changes, target unchanged | `usage4` changes, `asmctx3` stable |
| N1 | Missing defining compilation | Refusal; no target/occurrence |
| N2 | Defining unit outside declared scope | `UnsupportedExternalEvidence`/`External`; no fact |
| N3 | Multiple live defining bindings | `Ambiguous`; no fact |
| N4 | Incomplete declared scope | `Incomplete`; no complete-empty |
| N5 | Unverified/incomplete context | No minting from a caller-supplied boolean/descriptor |

## 6. Collision and conflation adversaries

| Adversary | Required result |
| --- | --- |
| Inject `compctx2` into `asmctx3` or `usage4` input | Structurally impossible / refused |
| Two distinct defining compilations with identical textual descriptors | Distinct `asmctx3` |
| Fabricated canonical-shaped `asmctx3`/`compctx1` | Refused (`CodeOccurrenceTargetId`/`ComputeTargetId`) |
| Stale defining `compctx2` projected as current | Refused |
| Dependency-context-only change presented as identity change | No identity churn |
| Caller boolean asserting completeness/authority | Refused |

## 7. Required falsification probes (G-MINT)

1. Replicate Option A §5.2 tests 1–10.
2. Independent reproduction of the mapped `compctx1` for at least one
   dependency-bearing defining compilation.
3. `compctx2` isolation negative (identity tuple cannot contain it).
4. Determinism across repeated and separate processes.
5. No identity churn on transitive-only change; identity change on
   declaration-changing input.

## 8. Boundary of this document

This contract defines the mapping for later authorization. It implements no
extraction or minting and does not self-certify `G-MINT`. Implementation and
independent review remain the gate before any canonical target is minted.
