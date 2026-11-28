# Successor identity contract

**Status:** implemented under owner authorization (option 1, incompatible
persisted-identity transition). No in-place migration.

## 1. Historical domains (preserved)

| Domain | Meaning | Status |
| --- | --- | --- |
| `compctx1` | R2-faithful defining context used by predecessor target minting | Frozen; readable/verifiable; never minted by current production |
| `compctx2` | Dependency-sensitive predecessor production descriptor | Frozen; readable/verifiable; never minted by current production |
| `asmctx3` | Predecessor target identity | Not canonical; recognised only for safe refusal |
| `usage4` | Predecessor occurrence identity | Not canonical; predecessor stores are refused |

Historical evidence, stores and seals are unmodified.

## 2. Successor domains

### 2.1 Reference identity — `refctx1`

```text
refctx1|<assembly name>|<version>|<culture>|<public key token>|sha256:<image hash>
```

- Content-bound and path-free. Byte-identical references at different absolute
  installation paths have the same identity.
- Assembly/binding metadata plus the exact image hash keep genuinely different
  references distinct.
- The predecessor `file|<absolute path>|sha256:<hash>` form is not accepted in a
  successor descriptor.

### 2.2 Production compilation descriptor — `compctx3`

Dependency-sensitive provenance descriptor whose metadata reference items are
`refctx1|...` and whose project-reference items are
`compilation-context|compctx3:sha256:<dependency context>`.

The descriptor digest is the same length-prefixed domain-separated construction
as the accepted contract, under the `compctx3-context` domain and
`compctx3-options` option digest.

### 2.3 Defining context — `compctx4`

The R2-faithful context used for target minting. It is derived from `compctx3`
by replacing each verified project-reference item with the frozen R2 legacy item

```text
compilation|<assembly>|<metadata identity>|<sorted tree paths and text hashes>
```

and substituting the `compctx4` contract version. Reference items remain
content-bound `refctx1|...`. `compctx3` never enters `asmctx4`/`usage5`.

### 2.4 Target identity — `asmctx4`

```text
asmctx4:sha256:<hash>
```

Hash domain `asmctx4-context`, parts:

```text
compctx4                 (defining-context contract)
<compctx4 digest>
<metadata identity>
<module name>
<identifier digest>
<declaration digest>
<shape digest>
```

The compilation-context argument must be a canonical `compctx4:sha256:<64 hex>`
value. `compctx1`, `compctx2`, and `compctx3` contexts are refused.

### 2.5 Occurrence identity — `usage5`

Hash domain `usage5`, parts:

```text
<repository>
<plugin>
usage5-b2c-exp-r2-20261128
<semantic source unit>
<source path>
<source content hash>
<start line><start column><end line><end column>
<target document id>
<asmctx4 target id>
method-invocation
```

Payload domain: `occurrence-payload5`. Numeric fact semantics generation: `5`.

### 2.6 Persisted generation

| Surface | Successor value |
| --- | --- |
| Occurrence schema | `2` |
| Occurrence namespace | `andirit-occurrence-facts/2` |
| Occurrence meta | `IdentityGeneration = 2` |
| Authority envelope version | `2` |
| Authority `IdentityGeneration` | `2` |

A predecessor schema/namespace/generation or a `compctx2`/`asmctx3`/`usage4`
payload is refused explicitly with re-ingestion guidance. It is never
reinterpreted under the successor contract.

## 3. Framework reference closure

The closure is the managed assembly set of the running
`Microsoft.NETCore.App` shared framework directory
(`RuntimeEnvironment.GetRuntimeDirectory()`), ordered ordinally.

- Excludes testhost, xunit, consumer, plugin and application assemblies.
- Excludes absolute installation paths from identity.
- Excludes native images without managed metadata.
- Project references remain live `CompilationReference`s.
- Package and external references remain refused by the existing project model.

Exact digests are bound to the referenced file contents. A different runtime
build (including a different OS/architecture runtime image) legitimately changes
them; the semantic relations, cardinality, contributor union and domain versions
remain stable. This matches the S2.0 §5 runtime-binding rule.

## 4. Required invariants (verified)

1. Equivalent semantic inputs produce equivalent identities.
2. Equivalent targets produce identical `asmctx4` values.
3. Incidental host/test/application assemblies are excluded.
4. Absolute installation paths do not participate in identity.
5. Different reference bytes, options, sources, aliases and binding properties
   remain distinguishable.
6. Contributor identity and union are unchanged.
7. Distinct semantic contexts are not collapsed.
8. No indiscriminate reference exclusion.

## 5. Compatibility boundary

- Historical stores and packages are **not** identity-compatible with the
  successor generation.
- No in-place migration or destructive rebuild exists.
- Reconstruction is re-ingestion of sources into a **new** store.
- The original store file and its occurrence records are preserved; a refused
  open does not rewrite the logical predecessor records.
