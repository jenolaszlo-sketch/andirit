# ECMAScript relative module resolution

Hetu's TypeScript and JavaScript Tree-sitter plugins publish a conservative
module-file relationship for supported relative module references. The edge
kind is `resolves-to`, directed from the importing file to the verified target
file. It supplements the existing structural `imports` observation.

The relationship establishes a module-file target only. It does not identify
which exported declaration an imported name binds to. Hetu emits no `exports`
or `binds` relationships as part of this capability.

The EC-6C.1 implementation adds a separate, internal-only inventory of
supported export syntax while the existing Tree-sitter profile walks the same
parsed source. Those observations retain written public/local names, syntax
form, explicit type-only grammar, syntax-only projection, and source spans for
later stages. They are not graph nodes or edges, do not attribute a written
local name to a declaration, and do not change this module-resolution policy,
the p8 profile, or `resolver2.relative-v1`. See the
[EC-6C.1 handoff](evidence/ec6c-1-export-shape-inventory-handoff.md) for its
accepted and excluded syntax and verification evidence.

## Evidence and properties

Every `resolves-to` edge has `Resolved` evidence with this bounded meaning:

> The target was verified under the declared resolution policy and the indexed source inventory of this publication. This does not establish equivalence with TypeScript compiler, bundler, or runtime resolution.

The provider is `penghou.hetu.tree-sitter.ecmascript-module-resolver`. Its
provider version follows
`{packageVersion}+resolver{ResolverRevision}.{policy}`; the current value uses
`resolver2.relative-v1` (resolver revision 2, the EC-6B-R1 corrective pass).
Edge properties are:

| Property | Meaning |
| --- | --- |
| `specifier` | Raw written module specifier, without quotes, up to 256 characters |
| `module-reference` | `import`, `import-type`, `require`, `export-from`, `export-type-from`, `export-star`, or `export-namespace-from` |
| `resolution-policy` | `relative-v1` |
| `resolution-rule` | `exact`, `extension-substitution`, or `directory-index` |
| `type-only` | Present as `true` only for explicitly type-only forms; otherwise absent |

The evidence location is the written import or re-export statement, or the
literal `require` call expression. Lines are one-based; columns are one-based
**UTF-16 code-unit** offsets from the start of the line, and the end location
is exclusive. The pinned parser binding decodes source text to a .NET string
and parses it as UTF-16LE, so columns count UTF-16 code units, not UTF-8
bytes; for non-ASCII source a UTF-8 byte offset differs from the published
column. The edge's unit origin identifies the repository, plugin, effective
version, run, and stable `{language}:modules` unit.

## `relative-v1` policy

The resolver uses only the owning plugin's decodable source inventory. It
does not inspect the filesystem, `package.json`, compiler configuration,
environment variables, or the network.

A written specifier is considered relative only when it starts exactly with
`./` or `../`. The resolver joins it to the importing file's repository-
relative path using POSIX segments. A `.` segment is ignored; a `..` segment
pops one directory, and resolution is rejected if it would cross the
repository root. Empty segments from repeated or trailing separators are
rejected. Comparisons are ordinal and case-sensitive.

For the normalized path, matching proceeds in this order:

1. Exact inventory path.
2. If the final path segment has no dot, append each language suffix in order:
   TypeScript `.ts`, `.tsx`, `.d.ts`, `.mts`, `.cts`; JavaScript `.js`, `.jsx`,
   `.mjs`, `.cjs`.
3. If no file suffix matched, try `index` with those suffixes in the same
   order.

The first match is the declared policy result and its rule is stored on the
edge. An explicit extension is checked only as written; it never gets another
suffix. Thus `./x.js` does not resolve to `x.ts`. A `.js` file owned by the
JavaScript plugin is outside the TypeScript plugin's inventory, even when
both plugins index the same repository.

The resolver declines or classifies these cases without inventing an edge:

- Bare packages, URL schemes, `node:` references, and configuration aliases
  are `external-targets`.
- A relative target absent from the plugin inventory, including case-only
  mismatches or a `.js` reference with only a `.ts` candidate, is
  `unresolved-targets`.
- Root escapes, absolute paths, backslashes, repeated or trailing separators,
  query/fragment suffixes on relative paths, unsupported extensions, dynamic
  `import()`, template or nonliteral `require`, TypeScript `import = require`,
  `export =`, and literal text longer than 256 characters are
  `unsupported-targets`.
- A module specifier written with a JavaScript escape sequence (for example
  `'./a\b'` or `'./caf\u00e9'`) is rejected conservatively as
  `unsupported-targets`: escapes are never decoded and the partial text before
  an escape is never used, so no `resolves-to` edge can be produced from it.
  The site retains no specifier text.
- A literal longer than 256 characters is not retained as a graph property.

A literal `require('...')` call is a captured require site wherever it is
written, including inside an outer form that is itself unsupported. For
example, `export = require('./m')` contributes one unsupported `export =`
site and one `resolves-to` edge attributed to the `require` call span; no
export or binding fact is produced. `import x = require('./m')` and
`import type X = require('./m')` are rejected as whole statements and do not
capture a nested require.

Directory resolution uses only the ordered `index` filenames; it never reads
package metadata. Ambiguity counters remain zero because the policy defines
explicit precedence. An absent edge means **not verified under the active
policy**, never that a dependency does not exist.

## Identity, multiplicity, and lifecycle

Each occurrence is a separate edge. Its id is computed as:

```text
{language}:{sha256(kind \n source \n target \n discriminator \n span)}
```

For `resolves-to`, the discriminator is
`resolves-to:{module-reference}:{raw-specifier-text}`, and `span` is the
written statement or call span. Properties are not identity inputs. Different
specifiers to one target, identical specifiers at different locations, and
imports from separate files therefore retain separate edges. Moving a
statement changes its id; LF and CRLF versions keep the same row and byte
column identities. Duplicate ids fail the resolution pass instead of being
silently dropped.

Each plugin owns one stable `{language}:modules` unit containing edges only.
Whenever that plugin runs, it recomputes and replaces the whole unit,
including an empty completion when no module edges remain. Per-file units are
staged first so every edge endpoint exists; publication remains atomic at the
index-run boundary. Target/importer deletion removes stale edges through the
replacement. A failed or cancelled run retains the previous successful
publication.

Any ECMAScript profile or resolver-fact change must bump the effective plugin
version. EC-6B moved the shared profile from p6 to p7; the EC-6B-R1 corrective
pass changed emitted resolver facts, so the effective profile continues to p8
and the resolver revision becomes 2. Existing p6/p7 source units therefore
re-extract. Resolver revisions and policy-bearing provider versions must
change in lockstep with resolver-fact changes; the effective plugin version
remains the planner's single invalidation axis.

## Coverage and queries

`resolves-to` coverage counts every captured module-reference site as a
candidate. Self-file targets count as `InternalTargets`; other indexed file
targets count as `CrossProjectTargets`. Missing, external, and unsupported
sites use the corresponding existing counters. Resolver coverage is `Partial`
when any owning-plugin source had a parse error or could not be decoded.
`CodeGraphExtractionResult.UnresolvedRelationships` sums only the resolver's
`UnresolvedTargets`; external and unsupported sites are reported by their
coverage counters instead.

Consumers can query module dependencies with an explicit relationship filter:

```csharp
var neighborhood = await queries.GetNeighborhoodAsync(
    repositoryId,
    importingFileId,
    [CodeEdgeKinds.ResolvesTo],
    new CodeGraphQueryOptions(maxDepth: 1, maxNodes: 100, maxEdges: 100));
```

Impact presets are unchanged and do not automatically traverse `resolves-to`.
Structural `imports` edges remain available with `Structural` evidence.

Export binding, re-export closure, `.js` to `.ts` substitution,
`tsconfig` aliases, Node package resolution, dynamic module expressions,
cross-plugin resolution, and compiler/runtime equivalence remain outside this
policy. Export and binding work is reserved for EC-6C/EC-6D; broader
real-repository qualification remains EC-6F.
