# EC-6C.1 Structural Export Shape Inventory Handoff

**Status:** Implementation complete on the isolated branch; ready for independent review. Initial AST and extractor inventory was recorded before production-code edits.
**Scope:** Internal TypeScript/JavaScript export observations only. No graph publication, declaration attribution, identity construction, profile bump, resolver change, or public API change.

## Git binding

- Repository: `jenolaszlo-sketch/penghou-hetu`.
- Implementation branch: `implementation/ec6c-1-export-shape-inventory`.
- Exact EC6C-A2 starting commit: `bf4ffcc25c22a6b8f98fb2b3f513dd95e5f8f49f`.
- Starting tree: `8abb3f12b4344a17b8a530c42c10c01130daabed`.
- Parent: EC6C-A1 owner acceptance `760918174b8228dc0ebe2a16fbcd164316c7a116`.
- Worktree: `C:\Users\Laszlos\source\repos\Penghou.Hetu.ec6c-1-export-shape-inventory`.
- The canonical checkout and prior design, acceptance, and review branches are not being edited.

## Pre-change source and AST inventory

This section records the inspected A2 source before production changes.

| Concern | Existing behavior at A2 | EC-6C.1 implication |
| --- | --- | --- |
| Session lifecycle | `TreeSitterExtractionSession.ExtractAsync` orders sources by ordinal path, checks cancellation between files, reads strict UTF-8, parses once with a session parser, calls the per-path profile, then writes the existing file-unit nodes/declarations/edges. Parsing and profile traversal are synchronous; cancellation is not checked mid-file. | Collect from the same root inside the existing profile call. Keep collector exceptions visible and inherit the existing between-file cancellation behavior. |
| TypeScript/JavaScript plugins | Both plugins instantiate the shared `EcmaScriptExtractionProfile`; TypeScript chooses its TSX grammar by path. The effective shared extraction profile is p8; module resolution is `resolver2.relative-v1`. | Keep p8 and both plugin versions unchanged. Do not alter module-reference observations. |
| AST traversal | `Walk` handles `export_statement` by capturing module references and calling `HandleExport`. Direct declaration exports are walked as their declaration; local lists are traversed as syntax only. File-scope is explicit and becomes false inside declarations/namespaces. Error and missing nodes are skipped during ordinary traversal. | Add export observations at file scope, separately from existing fact emission; do not treat nested namespace exports as module exports. |
| Existing direct declaration extraction | Variable exports go through file-level `variable_declarator.name`; direct function, class, interface, type alias, enum, and namespace declarations use existing declaration handling. | These paths do not currently retain a separate export occurrence or bind an export surface to a declaration. Collector reads the declaration name token but stores only syntax metadata. |
| Existing import/export grammar handling | `CaptureExportModuleReference` sees only export statements with a grammar `source` field, classifies module-reference forms, and uses the EC-6B-R1 type-only helpers. Local export lists have no module reference. | The export collector must explicitly reject a `source` field before visiting specifiers. Existing resolver behavior stays in place. |
| Contextual `type` handling | `HasStatementTypeModifier` inspects unnamed grammar tokens at the statement level. `IsTypeOnlySpecifier` inspects an unnamed `type` token and its following grammar children; a binding named `type` alone is not type-only. These helpers are currently used for module-reference classification. | Reuse the grammar-based distinction where applicable and preserve local `type` as a non-type-only observation when its grammar shape identifies that token as the specifier name. |
| Source locations | `Location` adds one to Tree-sitter rows/columns. The wrapper documents UTF-16 code-unit columns; `CodeLocation` uses one-based positions and end-exclusive ranges. | Capture the exact exported-name token and whole export construct in that existing coordinate system. |
| Strings and name validation | `RawModuleSpecifier` and `BoundedText` are module/declaration helpers with escape restrictions and UTF-16 `.Length` bounds. They are not a strict scalar validator for public export names. | Implement dedicated strict scalar validation; do not reuse lossy or code-unit-counting helpers for export names. |
| Internal test visibility | `AssemblyInfo.cs` grants `InternalsVisibleTo("Penghou.Hetu.TreeSitter.Tests")`. | Tests can parse a source once and inspect the internal profile result without publishing graph facts or adding a public contract. |

### Pinned grammar observations

The shape probe used the existing `TreeSitterSessionParser` and pinned grammar registry at the A2 commit: TypeScript/TSX `tree-sitter-typescript` v0.23.2, grammar commit `75b3874edb2dc714fb1fd77a32013d0f8699989f`; JavaScript `tree-sitter-javascript` v0.25.0, grammar commit `58404d8cf191d69f2674a8fd507bd5776f46cb11`.

- Direct declaration forms are `export_statement` nodes with a `declaration` field. Variables contain `variable_declarator` nodes whose `name` field is an `identifier` for simple names; destructuring uses `object_pattern`. Declaration names use grammar fields (`name`) and can be `identifier` or TypeScript `type_identifier`.
- Named default declarations have an unnamed `default` token and a `declaration` field. An anonymous default function is a `function_expression` in `value`; a default expression such as a call is also in `value`.
- A local list is an `export_clause` containing separate `export_specifier` nodes. Each ordinary specifier exposes `name` and optional `alias` fields. `export { value as default }` exposes the alias as a `default` token; quoted names are `string` nodes.
- A re-export has a `source` field; star and namespace re-exports have `*` or a `namespace_export` child. These forms remain excluded from export-shape observations.
- Statement-level `export type { ... }` has an unnamed direct `type` token before the clause. Inline `export { type Model }` has an unnamed `type` token within the specifier before its `name`. A lone TypeScript specifier `{ type }` has a `type` token but no `name` field; the pinned grammar wraps that token in a one-child `ERROR`, while JavaScript exposes an `identifier`. The collector recognizes only this exact TypeScript recovery shape as a contextual local name and does not mark it type-only absent a statement-level modifier.
- `export declare const ...` wraps the declaration in `ambient_declaration`; exported destructuring has an `object_pattern` name. Both are excluded.
- Nested namespace exports appear below an `internal_module` body. Their AST nodes are visible, but the traversal is no longer at module scope.
- The pinned TypeScript grammar recovers contextual alias `type as kind` as an `export_specifier` with exactly three children: the unnamed `type` token, a one-child `ERROR` around identifier `as`, and the final valid alias token. The collector recognizes only this exact recovery shape so the valid contextual local name is not lost. This inline spelling is not considered type-only unless the whole statement has explicit `export type`. Other error/missing subtrees remain excluded.
- An incomplete alias such as `broken as` is recovered as a valid-looking specifier followed by a sibling `ERROR` token `as`; the collector skips that specifier. A malformed sibling does not invalidate other independently valid specifiers in the list.

Grammar recognition, syntax names and spans, and semantic attribution are separate: the probe established only grammar structure; existing declaration extraction is not used as export attribution, and EC-6C.1 does not add binding logic.

## Baseline verification before production changes

- Environment: Windows 11 x64, .NET SDK 10.0.401, runtime 10.0.12.
- Restore: `dotnet restore tests/Penghou.Hetu.TreeSitter.Tests/Penghou.Hetu.TreeSitter.Tests.csproj --verbosity minimal` — succeeded.
- Existing Tree-sitter tests: `dotnet test tests/Penghou.Hetu.TreeSitter.Tests/Penghou.Hetu.TreeSitter.Tests.csproj --no-restore --logger "console;verbosity=minimal"` — passed 131, failed 0, skipped 0.
- A one-test grammar-shape probe passed and printed the structures summarized above. The temporary probe will be replaced by assertions against the internal observation result.

## Implementation and verification results

### Internal observation contract

The shared `EcmaScriptExtractionProfile` captures observations from the
existing file-scope `export_statement` visit and the already parsed tree. It
does not parse again or alter existing graph result collections. The generic
internal extraction result now carries an internal list of immutable
`EcmaScriptExportObservation` records; the existing test assembly reads this
result through a test-only helper. No public interface, graph kind, edge,
property, evidence, identity, coverage, resolver, or profile revision was
added or changed.

Each accepted occurrence stores source path and language, exact public name,
written local name only when the syntax provides one, named/default category,
syntax form, explicit grammar-derived type-only state, syntax-only namespace
projection, the exact name-token span, and the full export-statement span.
Locations retain the existing one-based, end-exclusive UTF-16 code-unit
coordinate convention. Observations are sorted by ordinal path, name span,
construct span, then explicit syntax fields. Occurrences are not deduplicated.
The collector never looks up or binds a local name.

Public names have dedicated A29 validation: 1–256 Unicode scalar values,
strict malformed-UTF-16 rejection, no empty/all-whitespace/control name, and no
normalization, trimming, replacement, or truncation. Bare identifier spelling
must be an unescaped identifier token recognized by the pinned grammar; quoted
names are retained only when their written string has no escape sequence.
Tree-sitter token recognition is used for ECMAScript identifier start/part
rules (including grammar-recognized Unicode identifiers outside the Unicode
general-category shortcut), while the separate scalar validator enforces A29.
Local names are written syntax and may retain their raw escaped spelling; no
binding is inferred from that text.

Projection is conservative and syntax-derived: direct variables and functions
are value, interfaces and type aliases are type, classes and enums are both,
nonambient namespaces are unknown, ordinary local export lists are unknown,
explicitly type-only occurrences are type, and default expressions/functions
are value (default classes are both). No local declaration lookup is performed
to sharpen an unknown projection.

| Accepted shape | Recorded form and limits |
| --- | --- |
| Direct simple variable declarators | One named occurrence per simple identifier declarator; destructuring is excluded. |
| Direct function, generator function, class, TypeScript abstract class, interface, type alias, enum, nonambient namespace | Named direct occurrence using the grammar `name` token; unsupported/error/missing declaration trees are excluded. Overload signatures retain separate occurrences. |
| Local export list, including imported-local names | One occurrence per valid specifier; local name and optional alias are retained. No import binding or declaration attribution. |
| Inline and statement-level TypeScript type-only exports | Type-only state follows pinned grammar tokens and existing EC-6B grammar helpers; projection is type. JavaScript type-only-like recovery is excluded. |
| Named/default declaration, anonymous default declaration, default expression | `default` is the public name and its token is the name span. Written local name is retained for named declarations and simple identifier expressions only. |
| Quoted alias | Raw, unescaped literal content is retained; the name-token span covers the written string token, including quotes. |

Excluded forms include source-bearing re-exports, star and namespace
re-exports, CommonJS assignments, `export =`, ambient wrappers, destructured
variables, nested namespace exports, unsupported/error/missing names, escaped
public identifiers or quoted aliases, and names that fail A29 validation.
Valid siblings in a local list may still be observed when a separate malformed
sibling is present. This is positive-only syntax collection and does not claim
that omitted syntax has no runtime or compiler export.

### Tests added

`TreeSitterEcmaExportShapeTests` contains 14 focused test methods covering:

- direct forms, multiple variable declarators, overload occurrences, local
  names/aliases/duplicate public spellings, defaults, and imported-local export
  syntax;
- explicit type-only and contextual `type` grammar recovery, with no type
  inference or binding;
- escaped/quoted names, A29 scalar boundaries, controls, malformed surrogate
  rejection, supplementary scalars, whitespace, normalization, and a
  grammar-recognized Unicode identifier (`℘`);
- exact token/construct spans, UTF-16 columns, LF/CRLF behavior, deterministic
  repeated extraction, cross-file sorting, and duplicate preservation;
- re-export/CommonJS/ambient/destructuring/nested/malformed exclusions and
  JavaScript value exports;
- an exact pre-change graph fingerprint regression for both TypeScript and
  JavaScript, p8 version preservation, no export graph nodes/edges, and
  `resolver2.relative-v1` resolution facts.

The graph regression fixture was captured at the A2 base before production
changes. Its unchanged outputs are:

| Plugin | Nodes | Declarations | Edges | SHA-256 of serialized graph facts |
| --- | ---: | ---: | ---: | --- |
| TypeScript | 7 | 4 | 12 | `84c13587db0ef90751bbb6f9c561b1294b541dc7fade44d15bdf54a6fb5722bf` |
| JavaScript | 7 | 4 | 12 | `3b2d3761334e5f4b87423cf0f84fa8824c2618450f4e5b98386ee861fdf32cdd` |

Baseline and initial implementation runs included unsuccessful attempts; they
were not hidden. The pre-change `dotnet test --no-restore` attempt did not
discover tests until the Tree-sitter test project was restored. The first
focused implementation run exposed four fixture/collector issues: an incorrect
alias expectation, an incorrect quoted-token UTF-16 width, contextual `type`
grammar recovery, and incomplete-alias recovery that could have admitted a
partial name. A later focused run also showed that the pinned grammar uses
`function_signature` for direct overload declarations; that form was added to
the allowlist. Each issue was investigated against the AST, corrected narrowly,
and retained in focused assertions.

During final cleanup, a newline-normalization attempt rewrote escaped newline
literals in C# sources and produced a failed build plus two focused assertion
failures. The affected tracked sources were restored from the A2 baseline and
the intended narrow edits reapplied; the new raw-string test fixture was
returned to LF physical line endings. The focused, project-wide, and solution-
wide suites then passed on the repaired source tree. This failed attempt is
recorded here rather than treated as a passing verification.

### Final verification

- Focused export inventory: `dotnet test tests/Penghou.Hetu.TreeSitter.Tests/Penghou.Hetu.TreeSitter.Tests.csproj --no-restore --filter FullyQualifiedName~TreeSitterEcmaExportShapeTests --logger "console;verbosity=minimal"` — 14 passed, 0 failed, 0 skipped.
- Full Tree-sitter project: `dotnet test tests/Penghou.Hetu.TreeSitter.Tests/Penghou.Hetu.TreeSitter.Tests.csproj --no-restore --logger "console;verbosity=minimal"` — 145 passed, 0 failed, 0 skipped.
- Dedicated module resolution: `dotnet test tests/Penghou.Hetu.TreeSitter.Tests/Penghou.Hetu.TreeSitter.Tests.csproj --no-restore --filter FullyQualifiedName~TreeSitterModuleResolutionTests --logger "console;verbosity=minimal"` — 29 passed, 0 failed, 0 skipped.
- Solution restore: `dotnet restore Penghou.Hetu.slnx --verbosity minimal` — succeeded.
- Full solution: `dotnet test Penghou.Hetu.slnx --no-restore --logger "console;verbosity=minimal"` — Tree-sitter 145, Hetu 160, LatticeDb 26, CSharp 46 passed; 377 total, 0 failed, 0 skipped.
- Formatting: `dotnet format Penghou.Hetu.slnx --verify-no-changes --no-restore` — succeeded, no changes required.
- Whitespace: `git diff --check` — succeeded.
- Existing module resolution remains `resolver2.relative-v1`; the exact TypeScript and JavaScript p8 graph fingerprints above pass, and focused assertions find no export graph nodes or edges.

Environment: Windows 11 x64, .NET SDK 10.0.401, runtime 10.0.12.

### Known limitations and next-stage boundary

- Tree-sitter reports parse recovery for the valid contextual TypeScript
  spellings `export { type }` and `export { type as kind }` with the pinned
  grammar. The collector handles only the exact observed shapes documented
  above. Broader malformed or recovered forms are omitted, and the parse-error
  result remains visible to the caller.
- Escaped public names are deliberately omitted. EC-6C.1 does not decode
  escapes, establish declaration identity, distinguish type/value through
  binding, or promise complete export coverage.
- No export facts are published, no profile bump is appropriate, and no
  p8-to-p9 migration, graph-store lifecycle, query, or real-repository
  qualification is part of this stage. Those are later gates.

### EC-6C.2 integration prerequisites

- Consume the immutable observations from the existing extraction result and
  do not reparse source or infer a local-name binding.
- Apply the A1/A2 accepted export-entry and occurrence identity, collision,
  evidence, property, and per-file ownership contracts in EC-6C.2; keep this
  inventory's occurrence multiplicity and exact spans available to that stage.
- Add export graph facts only in the authorized publication stage. Advance both
  ECMAScript plugin profile versions to p9 when those facts are emitted; keep
  `resolver2.relative-v1` unchanged.
- Prove existing p8 node, declaration, structural-edge, and resolution-edge
  identities survive the p8-to-p9 transition. Cover deterministic identity,
  duplicate names, collision failure, empty replacement, source deletion,
  cancellation/failure atomicity, query compatibility, and both graph stores
  under their specified EC-6C.2/6C.3 gates.
- Keep declaration attribution, import binding, re-export closure, coverage
  completeness, and real-repository qualification outside EC-6C.2 unless a
  later accepted owner decision explicitly adds them.

### Independent review focus

Review the allowlist against the accepted EC6C-A1/A2 contract; challenge the
exact A29 scalar/name handling, grammar-recovery whitelists, name and construct
spans, projection conservatism, deterministic ordering, and positive-only
omission semantics. Confirm the collector is invoked only at file scope and
the fingerprint test binds the unchanged published graph. This handoff is
implementation evidence, not independent approval or owner closure.

### Git and disposition

The changes are on `implementation/ec6c-1-export-shape-inventory`, based
directly on EC6C-A2 commit `bf4ffcc25c22a6b8f98fb2b3f513dd95e5f8f49f`. The
implementation is isolated from the canonical checkout. The final branch head,
tree, parent, and clean-worktree result are reported in the task completion
message; this document is included in that implementation commit.

**Disposition:** EC-6C.1 implementation complete — ready for independent
review. No integration, merge, push, or release is authorized by this stage.
