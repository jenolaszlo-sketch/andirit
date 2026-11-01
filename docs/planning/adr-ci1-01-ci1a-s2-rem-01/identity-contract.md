# Identity Contract — REM-B01

**Authority:** R2 `PROVENANCE-CONTRACT.md`; Q3 `TARGET-IDENTITY-REVIEW.md`;
owner decision §F.
**Production:** `src/Andirit.Abstractions/CodeOccurrence.cs`.
**Tests:** `tests/Andirit.Tests/OccurrenceIdentityRemediationTests.cs`.

---

## 1. Exact R2 tuples

### `usage4`

```
usage4 = SHA256(
  "usage4",
  repository,
  plugin,
  "usage4-b2c-exp-r2-20261101",
  semanticUnit,
  path,
  sourceHash,
  startLine,
  startColumn,
  endLine,
  endColumn,
  targetDocumentId,
  asmctx3,
  "method-invocation")
```

The hash input uses a 32-bit **big-endian** byte length before every part. The
result is 64 lowercase hexadecimal characters (no `usage4:` prefix, matching R2
`OccurrenceV3.OccurrenceId`).

### `asmctx3`

```
identifierDigest  = SHA256("asmctx3-identifier",   canonical identifier-site forms)
declarationDigest = SHA256("asmctx3-declarations", canonical declaration-site forms)
shapeDigest       = SHA256("asmctx3-target",        shape)

asmctx3 = "asmctx3:sha256:" + SHA256(
  "asmctx3-context",
  "compctx1",
  compctx1,
  metadataIdentity,
  moduleName,
  identifierDigest,
  declarationDigest,
  shapeDigest)
```

`compctx1` must be canonical `compctx1:sha256:<64 hex>`. A target is not minted
from a caller-supplied boolean, a documentation ID or a source path.

### `compctx1`

R2 `CompilationDescriptor` is:

```
"compctx1:sha256:" + SHA256(
  "compctx1-context",
  contractVersion, repositoryId, projectUnitId, assemblyName,
  metadataIdentity, moduleName, optionsDigest, sources..., references...)
```

Production does not yet construct `compctx1`; Stage 2 S2.1 owns that work.
`ComputeTargetId` accepts only an already-canonical `compctx1` value.

---

## 2. Production mapping

| R2 input | Production source | Notes |
|---|---|---|
| repository | `CodeRepositoryId` | exact value |
| plugin | `CodeOccurrenceContributor.PluginId` | exact value; not the display name |
| semantics version | `CodeOccurrenceIdentity.SemanticsVersion` | fixed R2 string |
| semantic unit | `CodeOccurrenceFact.SourceUnitId` | calling/source unit; not the storage contributor |
| path | `CodeLocation.Path` | caller supplies normalized repository-relative path |
| source hash | `CodeOccurrenceFact.SourceHash` | exact source-content identity |
| start/end | `CodeLocation` | one-based UTF-16 coordinates |
| target document | `CodeOccurrenceFact.TargetDocumentId` | documentation ID of the target |
| `asmctx3` | `CodeOccurrenceTargetId` | canonical only |
| category | `CodeOccurrenceCategory.Invocation` | mapped to `method-invocation` |
| storage contributor | `CodeOccurrenceContributor.IndexUnitId` | **not** in `usage4`; used for union |

The contributing storage unit is deliberately not in `usage4`, matching Q3's
"same occurrence from two storage units yields one fact and two contributors".
Different semantic units remain distinct because `SourceUnitId` is in `usage4`.

---

## 3. Frozen vectors

### 3.1 `usage4`

From R2 `evidence/identity-matrix-r2.json` (independently recomputed by Q3):

| Input | Value |
|---|---|
| repository | `experiment:andirit-31c33a9` |
| plugin | `experiment.csharp.occurrences` |
| unit | `caller-unit` |
| path | `Caller.cs` |
| source hash | `sha256:2f7803daa5910bf2f5fb5927b2e9f999ebd1b7074340096f620df37278df484a` |
| span | `1:72..1:77` |
| target doc | `M:N.C.M` |
| target | `asmctx3:sha256:632e7953365d1dbadbb55414b7cbcb991c9bc2653ec6cafb6f6d9d95a9c2032d` |
| category | `method-invocation` |

Expected occurrence ID:

```
bd0487b749795a3e6b61028c7c9cf2d663530cae901ac6d9f35bc311536dc3af
```

The same site bound to
`asmctx3:sha256:fb3e0561272eb342a549036ecf0095c2ec2a6d5d7bc159fc943dee11fe669c26`
produces:

```
b874bdc5afa194e0e237d0b1d7d36b3fa3dd767799e0dc62f5f1399f00b90bca
```

### 3.2 Target component separators

Any change to metadata identity, module, identifier digest, declaration digest
or shape digest changes `asmctx3`. The target is always
`asmctx3:sha256:<64 lowercase hex>`.

### 3.3 Negative controls

| Control | Result |
|---|---|
| `asmctx3:unavailable` | rejected by `CodeOccurrenceTargetId` |
| `asmctx3:sha256:abc` | rejected |
| 64 uppercase hex occurrence ID | rejected |
| occurrence ID not matching the tuple | rejected by `CodeOccurrenceFact` |
| caller-supplied payload hash | no longer accepted by the constructor |
| non-canonical `compctx1` | rejected by `ComputeTargetId` |

---

## 4. Persistence compatibility

The durable format remains `andirit-occurrence-facts/1` with schema version 1.
REM-01 adds optional JSON fields for coverage/analysis state; it does not migrate
the format.

Pre-REM databases may contain:

- occurrence IDs produced by the old repository/target/category/span/content
  helper;
- target IDs without the `asmctx3:sha256:` prefix;
- units/meta without coverage.

On open:

- non-canonical occurrence/target values are refused by the constructor and
  cannot be read as confirmed facts;
- units without coverage restore as `Partial` if they contain facts or claim
  analysis, never as complete;
- the store does not rewrite, migrate or reinterpret old records.

A pre-REM database must be rebuilt by re-ingesting source under the corrected
identity. This is consistent with D5: no experimental-schema migration.

---

## 5. Verification

`tests/Andirit.Tests/OccurrenceIdentityRemediationTests.cs` covers the frozen R2
vector, distinct defining contexts, semantic-unit/source-hash sensitivity,
canonical target construction, unavailable/fabricated rejection, payload
stability/conflict, and mismatched identity rejection.

`tests/Andirit.LatticeDb.Tests/OccurrenceRemediationNativeTests.cs` verifies that
canonical facts, complete-empty units and coverage survive native publication and
reopen.

No expected value is generated by the helper under test: the `usage4` vectors are
frozen R2/Q3 values.
