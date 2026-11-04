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

- In the production-capture profile, each PE/metadata reference retains its
  verified `file|<normalized actual captured path>|<sha256>` identity. The
  bytes in that identity are exactly the immutable image used to construct the
  Roslyn reference. The actual inventory comes from
  `CSharpCodeGraphPlugin.BuildPlatformReferenceEvidence`: the host's
  `TRUSTED_PLATFORM_ASSEMBLIES` paths, distinct and sorted
  case-insensitively (or `typeof(object).Assembly.Location` if that host value
  is absent), subject to the existing 512 MiB bound. The inventory is not the
  three-reference frozen fixture. A production comparison records the full
  actual verified inventory and its identities; it does not replace paths with
  fixture labels.
- R2 source entries are sorted ordinally by normalized path, then content
  hash. It projects every `Compilation.References` entry to its legacy identity
  and sorts those identity strings ordinally; it does not deduplicate the
  projected reference list. The production TPA input inventory itself uses
  `Distinct(StringComparer.OrdinalIgnoreCase)` and case-insensitive path sort
  before capture, as stated above; that host inventory construction rule is
  distinct from R2's later ordinal ordering of tuple reference items.
- For each legacy project-reference item, inspect the actual
  `CompilationReference.Compilation`. Sort its syntax trees by their original
  `FilePath` using `StringComparer.Ordinal` **before** normalizing separators;
  each item is normalized path plus lowercase SHA-256 of UTF-8 bytes of
  `tree.GetText().ToString()`, joined by commas. This uses text/tree hashes,
  not captured source-byte hashes. It does not encode the child compilation's
  parse settings, options, separate preprocessor-symbol identity, references,
  or transitive dependencies. Those omissions are why the item is not strong
  dependency proof; live verified `compctx2` association remains required.
- Source content hashes are the **exact verified byte SHA-256**, matching R2
  `Describe` when `sourceBytes` is supplied (not a re-hash of decoded text).
- The dependency tree hashes are the R2 **text/tree** form (decoded-text UTF-8
  SHA-256), which deliberately omits the dependency's options, preprocessor
  symbols as a separate input, and transitive references.

### Comparison profiles and frozen anchors

There are two distinct comparison profiles; each has a specific expected
result. Algorithm fidelity means independently applying R2 `Describe` and this
mapping to the **same complete input profile**, including identical source
bytes, parse/options fields, project-unit identity, reference inventory,
reference identity labels, and dependencies. Equal complete inputs must produce
equal `compctx1`; this comparison makes no promise that an input reconstructed
with different reference labels has the frozen corpus digest.

The frozen R2 fixture profile in S2.0
[`Corpus.cs`](../adr-ci1-01-ci1a-s2-0/oracle/Corpus.cs) and its
[specification](../adr-ci1-01-ci1a-s2-0/semantic-corpus-specification.md)
uses exactly these three framework reference labels (the fixture creates references
in this order; R2 sorts the serialized reference items ordinally). The SHA-256
values below are the observed bytes for the accepted Q1 toolchain/runtime
profile, not universal constants: `Corpus.cs` reads the runtime files and
computes their hashes dynamically. The frozen expected outputs are bound to the
accepted runtime profile. A comparison must hold that accepted byte profile
fixed as well as the labels.

| Frozen fixture item | SHA-256 |
| --- | --- |
| `file|runtime/System.Runtime.dll|76be53624f64b23d0249dfa0bbe85390086de7c44bfd490642ea86c6faaf44af` | `76be53624f64b23d0249dfa0bbe85390086de7c44bfd490642ea86c6faaf44af` |
| `file|runtime/System.Private.CoreLib.dll|1125acc8106c43fc8bad2d203c4c4485df6182d292846c2fff415c1040c54678` | `1125acc8106c43fc8bad2d203c4c4485df6182d292846c2fff415c1040c54678` |
| `file|runtime/Microsoft.CSharp.dll|2000912f2fc52e0c64ed893d79f4e7b143502540fb7170b5ae5fcea7099ba431` | `2000912f2fc52e0c64ed893d79f4e7b143502540fb7170b5ae5fcea7099ba431` |

For the reviewed R2 corpus version, exact frozen `compctx1` anchors are
`Leaf/Leaf.csproj` =
`compctx1:sha256:404c5e48c3106b47786f14cad594139477c651fd78d0e050f8fd5bcab18fcb5f`
and `Core/Core.csproj` =
`compctx1:sha256:942a8e588a8db15afafb30d0292e0b867c767c5f72392b6f458783d961a71db3`.
They apply only when all remaining corpus inputs and the accepted runtime
reference bytes are also held fixed. With source bytes, parse settings, options,
assembly/project-unit inputs, and dependencies held fixed, the reviewer probe's
production captures used the host's actual
`C:/Program Files/dotnet/shared/Microsoft.NETCore.App/10.0.12/` paths for those
same three DLLs and yielded `Leaf/Leaf.csproj` =
`compctx1:sha256:b816dcb5512588bc3b5ad01692c20241b31442ab420c8a4b5a6d06d032b9c59b`
and `Core/Core.csproj` =
`compctx1:sha256:df8d2932670fc6a15cec10930b0bf573cfce841080e55f9544433983cbbe8a77`.
Replacing only those path labels with the frozen fixture identities **inside
that comparison**, while retaining the accepted bytes and all other inputs,
reproduced both frozen anchors. This is a test-only preimage alignment; it does
not alter production captures, frozen expectations, or mapping rules. Production
must never substitute fixture labels for the actual captured paths. The probe records both inventories and all hashes in
[`results.json`](../../reviews/CI1A-S2.2-Q1-evidence/results.json).

Do not conflate `analysisscope1` with a source inventory. Scope identity binds
the declared repository/project-unit/path boundary; it does not prove that the
provided sources are a complete inventory. Source-to-tree membership and
exactness are established from the verified source snapshots and live
compilation/universe checks, independently of the scope digest.

**Boundary (explicit):** this mapped `compctx1` is **not** a strong verified
project-reference claim. The strong claim is carried by `compctx2`. The mapping
exists solely to preserve frozen `asmctx3` while keeping `compctx2` authoritative
for validity. Exact frozen corpus anchors are required only under the complete
frozen fixture profile above; production capture preserves its actual verified
reference paths and inventory.

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

The following are requirements for separately authorized implementation, not
capabilities delivered by R1. The concrete per-fact carrier, keys,
serialization, aggregation rules and reopen checks remain unresolved and
unimplemented; a scope/universe digest or caller boolean alone cannot replace
the defining-unit/current-context association. No absent authority is supplied
by this document.

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
| P1 | Standalone (dependency-free) defining compilation | Mapping equals R2 for equal complete inputs; a frozen `compctx1`/`asmctx3` anchor is required only when the full accepted fixture profile, including reference bytes and identities, is reproduced |
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
| Distinct accepted defining contexts/declarations despite matching display or partial textual descriptors | Different `asmctx3` when their complete R2 identity preimages differ |
| Separately constructed live instances with equivalent complete accepted inputs | Same mapped `compctx1` and same `asmctx3`; object identity is not an identity field |
| Fabricated canonical-shaped `asmctx3`/`compctx1` supplied to shape-only helpers | Shape may be accepted; live verified resolver/association gate must refuse fabricated authority |
| Stale defining `compctx2` projected as current | Refused |
| Dependency-context-only change presented as identity change | No identity churn |
| Caller boolean asserting completeness/authority | Refused |

## 7. Required falsification probes and stage boundary

Before production minting (WP5), independently execute Option A §5.2 tests
1–7 and 9–10 against the production mapper and live association/resolver. Test
8 is split by the accepted S2.2/S2.3 boundary:

- **S2.2 pre-mint portion:** verify in-memory candidate/current/universe
  mismatch refuses before fact emission. This stale-association check and the
  relevant fabricated, incomplete, duplicate, ambiguous, or substituted live
  authority negatives are prerequisites to WP5, even if their integration is
  assigned to a later WP.
- **S2.3 portion:** native persistence and reopen enforcement are explicitly
  deferred and require separate authorization. The first S2.3 gate is a reopen
  test proving stale defining `compctx2` refuses prior facts; it is not a
  prerequisite to the S2.2 G-MINT verdict.

Also require independent reproduction of dependency-bearing mapped `compctx1`
under the explicitly named profile in §2, `compctx2` isolation, repeated and
separate-process determinism for equal complete inputs, no identity churn on a
transitive-only change, and identity change when the defining R2 preimage
changes. Never compare different reference profiles as if they were equal
inputs.

## 8. Boundary of this document

This contract defines the mapping for later authorization. It implements no
extraction or minting and does not self-certify `G-MINT`. Implementation and
independent review remain the gate before any canonical target is minted.
