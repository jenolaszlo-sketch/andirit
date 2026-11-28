# Identity contract reconciliation

## 1. History

| Contract | Decision | Scope |
| --- | --- | --- |
| `compctx1` | S2.0/R2 provenance; S2.1 preserves legacy | Content-bound R2 component of compilation identity |
| `compctx2` | S2.1 project-reference amendment | Dependency-sensitive validity; project references as `compilation-context|<dependency compctx2>` |
| `asmctx3` | S2.2 target-identity decision, Option A | Semantic target identity; defining `compctx1` only; `compctx2` must not enter |
| `usage4` | S2.0/S2.2 | Occurrence identity; `compctx2` must not enter |

`compctx2` was introduced to close the transitive-dependency validity gap. It is
not a replacement for `asmctx3`/`usage4`; the accepted design is `asmctx3` from
the defining R2-faithful `compctx1`, with `compctx2` as a separate validity and
invalidation signal.

## 2. Current implementation

- `VerifiedCSharpCompilation.Create` builds a `compctx2` descriptor.
- `LegacyDescriptor` maps dependency-free `compctx2` to `compctx1` by
  substituting the contract version and recomputing the digest. Reference items
  are preserved verbatim.
- `MintTarget` calls `CodeOccurrenceIdentity.ComputeTargetId` with the mapped
  `compctx1`.
- `VerifiedMetadataReference.Capture` produces `file|<absolute path>|sha256`.
- `BuildPlatformReferenceEvidence` takes all process TPA, including testhost and
  consumer assemblies when running in a test/consumer process.

## 3. Is `compctx2` correct?

Yes. Its introduction was explicitly authorized by the S2.1 amendment, and its
isolation from `asmctx3`/`usage4` is accepted. Reverting to `compctx1` is neither
necessary nor authorized by this commission.

## 4. Why identities differ

The defining compilation's **reference set** differs between the frozen oracle
and production:

| Input class | Oracle | Production |
| --- | --- | --- |
| Framework references | Fixed runtime reference set per S2.0 | Process TPA (includes host assemblies) |
| Reference identity | Content-bound runtime form | Absolute path + content |
| Application/test assemblies | Excluded | Present when ambient |
| Compiler options | Release per S2.0 | Project options; default optimization differs |

Both are accepted in their own scopes, but the inputs are not semantically
equivalent. The divergence affects `compctx1` and therefore `asmctx3`/`usage4`.
It does not affect graph node/edge identity or contributor union.

## 5. Intentional vs incidental

- **Intentional semantic inputs:** source bytes, parse options, symbols,
  assembly identity, project/package references, binding-relevant aliases and
  reference properties, project dependency contexts.
- **Incidental execution-host inputs:** testhost/xunit/consumer assemblies,
  absolute installation paths, process-specific TPA ordering.
- The current `file|path|sha256` identity mixes incidental path information with
  content; the current TPA discovery mixes incidental assemblies into a
  semantic descriptor.

## 6. Published and persisted impact

Changing the reference set or identity canonicalization changes:

- `compctx2` for every project;
- `asmctx3` and `usage4` for affected targets/usages;
- persisted occurrence unit/meta authority IDs;
- acceptance of existing occurrence publications after rebuild.

Existing `v0.2.0-preview.7` packages and stores would not be identity-compatible
without re-ingestion or a migration/rebuild contract. This is an incompatible
identity transition and requires separate owner authorization.

## 7. Required invariants for a correction

1. Equivalent semantic compilation inputs → equivalent context identity.
2. Equivalent targets → identical target identity.
3. Incidental execution-host assemblies excluded.
4. Absolute installation paths excluded when content/semantic properties match.
5. Genuinely different semantic references, aliases, versions and properties
   remain distinguishable.
6. Contributor identity and union unchanged.
7. Distinct semantic contexts are not collapsed.
8. No indiscriminate reference exclusion.

## 8. Bounded remediation proposal (not implemented)

Smallest coherent correction:

1. Resolve platform references from a deterministic target-framework reference
   closure (or a declared runtime reference manifest), not the process TPA.
2. Exclude application/test assemblies that are not project/package references.
3. Canonicalize reference identity to a content/logical framework identity
   without absolute paths, preserving assembly name, version, culture, public key
   token and any binding-relevant aliases/properties.
4. Introduce a new identity contract version for the changed descriptor and
   target/usage domains; do not silently mutate `compctx1`/`asmctx3`/`usage4`.
5. Define the rebuild/re-ingestion transition for persisted occurrences and the
   compatibility boundary for published packages.
6. Add cross-host identity tests and semantic-difference negative controls.

This changes persisted identity semantics and requires an owner-authorized
transition. It was **not** implemented in this commission.
