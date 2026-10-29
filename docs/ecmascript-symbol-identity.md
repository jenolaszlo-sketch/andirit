# ECMAScript symbol identity for TypeScript and JavaScript

Status: implementation identity note recorded **2026-10-22**, extended by
EC-3 class identity and class-member extraction, EC-3D heritage target
identity, EC-5 file-level variables, and EC-6B module-edge identity. It
documents the audited symbol model and the separate resolver-edge identity;
EC-6B adds no new symbol identity and changes no C# semantics.

## Audited model

`EcmaScriptExtractionProfile.TryEmitNamed` derives every declared symbol and
node from one hash:

```text
{language}:symbol:{sha256(discriminator + "\n" + lexical-qualified-name)}
```

Every hash in this document and in the published graph is rendered as lowercase
hexadecimal (`Convert.ToHexStringLower`); the formulas never produce uppercase
output.

- `lexical-qualified-name` is built while walking one file (`Namespace.Member`).
  It has no file, module, signature, or type component.
- Discriminators are `class`, `interface`, `type`, `enum`, `namespace`, and
  `callable`.
- Physical occurrences accumulate as `CodeGraphDeclaration` records keyed by
  symbol identity plus source path and exact span. One symbol can therefore
  carry many declarations; a renamed symbol changes identity.

Consequences at the audit baseline:

- **Overloads and repeated declarations in one file** converge to one node with
  one declaration per physical occurrence. Nothing is lost physically, but the
  symbol has no per-signature identity.
- **Declaration merging is name-derived.** Same discriminator plus the same
  lexical qualified name always yields one symbol. Compatibility of merged
  declarations is never verified, and the profile has no concept of scope
  beyond lexical containment.
- **Cross-file names converge.** Two unrelated files that both declare
  `interface Shape` produce the same symbol identity: unrelated symbols would
  be silently merged.

The first two behaviors are documented profile limitations. The third is the
hazard the interface-member work must not extend.

## Amendment: path-scoped interface declarations and members

Interface declarations are emitted with an additional source-path component;
their member signatures are owner-derived (formula below):

```text
interface declaration: {language}:symbol:{sha256("interface" + "\n" + path + "\n" + lexical-qualified-name)}
interface member:      {language}:symbol:{sha256(kind + "\n" + interface-node-id + "\n" + written-name)}

member kinds: interface-method | interface-property
```

Rationale:

- TypeScript interface merging is a semantic, same-scope rule. Structural
  extraction cannot verify it, so over-merging same-name interfaces from
  separate modules would be a silent factual error; staying distinct is the
  honest structural failure.
- Member discriminators are interface-specific (`interface-method`,
  `interface-property`), so an interface signature never silently converges
  with a same-named class method or function. Structural evidence cannot
  establish that an implementation satisfies a contract.

Resulting behavior (fixture-verified in
`tests/Penghou.Andirit.TreeSitter.Tests/TreeSitterTypeScriptPluginTests.cs`):

- **Same-name interfaces in separate modules** stay distinct symbols with
  distinct declarations and members; exact qualified-name lookup returns
  candidates from both files instead of merging them.
- **Same-name interfaces in one file** converge to one node with one
  declaration per physical interface block; member names converge within that
  node. The convergence remains name-derived, not verified merging.
- **Method overloads** in one interface produce one `callable` node whose
  declaration list contains one record per written signature, with exact
  locations. Individual physical declarations are never dropped.
- **Identically named members on different interfaces** are distinct because
  their owners and member kinds differ.

## EC-3A: class and class-member identity

Class declarations now use the same path-scoped rule as interfaces, and class
members use owner-derived identity:

```text
class declaration: {language}:symbol:{sha256("class" + "\n" + path + "\n" + lexical-qualified-name)}
class member:      {language}:symbol:{sha256(member-kind + "\n" + class-node-id + "\n" + ownership + "\n" + written-name)}

member kinds: method | accessor | field
ownership:    instance | static
```

Rationale:

- Class identity must include the source path because unrelated files can
  declare the same class and member names; the qualified display name alone
  is not an identity.
- Member identity derives from the owning declaration node rather than an
  independently hashed display name, so a member can never converge with a
  same-named member of an unrelated class, and nested declarations inherit
  their owner's scoping transitively.
- Member kind and static/instance ownership are part of the identity:
  methods, accessors, and fields with the same written name stay distinct, and
  a `static` member never converges with an instance member.
- No source offsets are used; identity stays stable under unrelated edits.

Resulting behavior (fixture-verified in
`tests/Penghou.Andirit.TreeSitter.Tests/TreeSitterEcmaClassIdentityTests.cs`):

- **Same-name classes and methods in separate modules** stay distinct symbols
  with distinct declarations and containment.
- **Same-name members on unrelated classes** stay distinct (owner identity).
- **Static and instance members** with the same name stay distinct; both
  remain queryable as separate candidates under one display name.
- **Methods and accessors** with the same name stay distinct (member kind);
  accessors are callable members, never fields.
- **Class and interface declarations** with the same name stay distinct
  (`type` versus `interface`, different owners for members).
- **Method overload signatures** in a class produce one callable whose
  declaration list holds one record per signature, including the
  implementation.
- **Class fields** are owner-derived `property` members with member kind
  `field`, so a field never converges with a same-named method, accessor, or
  field of another class, and static fields stay distinct from instance
  fields. Field initializer invocations are attributed to the field node.
- **Nested class declarations** retain their containing callable as owner and
  derive identities from it.
- **Duplicate class declarations in one file** converge to one symbol with one
  declaration per physical block: structural extraction cannot verify or
  refute declaration merging, and no offset-based identity is introduced. This
  remains a documented limitation.
- **Unrelated edits** do not move class or member identities.

## EC-5: file-level variable declarations

The shared TypeScript/JavaScript profile emits named top-level
`lexical_declaration` and `variable_declaration` declarators as `field` nodes.
Only simple identifier bindings are accepted; block/function locals,
destructuring, and ambient `declare` declarations are skipped. The identity is:

```text
hash      = sha256("variable" + "\n" + source-path + "\n" + written-name)
symbol ID = {language}:{hash}
node ID   = {language}:symbol:{hash}
```

`hash` is lowercase hexadecimal like every other identity in this document.
The `variable` discriminator separates these declarations from classes,
interfaces, callables, aliases, and owner-derived class members. The source
path keeps same-named variables in different files distinct. No physical
offset is included, so unrelated edits preserve identity; renaming the
variable or moving it to another file changes identity.

File-level variables reuse the existing generic `field` node kind (accepted
limitation EC5-GLM-F3). Class fields are owner-derived `property` members whose
member-kind discriminator is `field`, so a file-level variable and a class
field never share identity, but the node kind alone does not tell them apart.
Consumers must use ownership (`contains` from the file node versus the class
node) and the `variable` symbol discriminator to distinguish a module variable
from a class member. The representation is deliberate: a dedicated node kind
would be a public-contract change and is not justified by any observed
consumer-visible failure.

Ambient declarations such as `declare const externalValue: string;` are outside
this subset (accepted limitation EC5-GLM-F4). The profile does not model
ambient or global scopes anywhere, so an ambient declaration emits no variable
fact; this is a deliberate boundary, not a partial extraction.

Each `variable_declarator` produces its own `CodeGraphDeclaration` span, even
when a statement contains multiple declarators or same-file declarations
repeat a name. Same-file same-name variables converge on one candidate symbol
with one physical declaration per occurrence. This is an identity policy, not
a claim that the language permits or semantically merges the declarations.
Each physical declaration retains its own `language` and `modifiers`
properties. If repeated occurrences disagree, the shared symbol's `modifiers`
contains only keywords common to every occurrence (and may be absent), while
the per-declaration properties retain the exact written modifiers.

The existing `modifiers` property records the written declaration keyword
`const`, `let`, or `var`, plus `export` only for a direct `export const/let/var`
declaration. `const` does not imply an immutable initializer value. Export
lists, default expressions, re-export binding, and module relationships remain
unmodeled. No export-binding edge is emitted.

Variable declarations use file `declares` and `contains` edges and exact
structural locations. Initializer invocation ownership retains the existing
lexical behavior. A variable initialized with a function/arrow expression
continues to receive the pre-existing separate `callable` projection; EC-5
adds the variable field identity and does not reinterpret that expression as
a function declaration.

Profile revision 6 changes the TypeScript and JavaScript plugin versions so
previous `.p5` source units re-extract. Ordinary source-unit replacement
removes renamed, removed, and deleted variable facts without a graph migration.

## EC-6B: module relationship identity

The EC-6B `resolves-to` relationship uses existing file-node identities for
both endpoints and does not change any symbol identity. Its separate edge id is:

```text
{language}:{sha256(kind \n source \n target \n discriminator \n span)}
```

`discriminator` is `resolves-to:{module-reference}:{raw-specifier-text}`.
The span is the written import/export-from statement or literal `require`
call, with one-based lines, one-based UTF-16 code-unit columns, and an
exclusive end. The pinned parser binding parses the decoded .NET string as
UTF-16LE, so columns are UTF-16 code units rather than UTF-8 bytes; EC-6B-R1
corrected the earlier "byte columns" wording without changing emitted
coordinates. Edge properties do not participate in identity. Therefore two
same-target import occurrences remain separate; changing the specifier or
moving an occurrence changes its id. LF and CRLF checkouts preserve ids for
the same logical source. An identity collision fails the resolution pass
rather than dropping an edge.

`Resolved` means exactly:

> The target was verified under the declared resolution policy and the indexed source inventory of this publication. This does not establish equivalence with TypeScript compiler, bundler, or runtime resolution.

The resolver provider is
`penghou.hetu.tree-sitter.ecmascript-module-resolver`; its provider
version is `{packageVersion}+resolver2.relative-v1` after the EC-6B-R1
corrective pass. The full matching, classification, provenance, and
publication contract is documented in
[`ecmascript-module-resolution.md`](ecmascript-module-resolution.md).

EC-6B advanced the shared profile revision from 6 to 7; EC-6B-R1 advanced it
to 8 because emitted resolver facts changed, so `.p6`/`.p7` source units
re-extract and both plugins rebuild their stable module unit. Existing file,
symbol, and structural edge identities stay the same; no persistent identity
migration is introduced.

## Heritage target identities

Every written class or interface heritage name uses a file-scoped,
declaration-less type candidate:

```text
{language}:heritage-target:{sha256(path + "\n" + written-target-name)}
```

The dedicated `heritage-target` ID namespace cannot collide with declared
`{language}:symbol:` identities. The node preserves the written name and
qualified name. Its path and name make the candidate deterministic and keep
same-spelled targets in different files separate. Targets with the same
written name in one file share a candidate node; each relationship retains its
own source location and evidence quality.

Tree-sitter does not resolve inheritance targets semantically. Structural
`extends` and `implements` edges therefore point to these candidates
even when a class or interface with the same name is declared in the file.
Recognized JavaScript prototype patterns use the same candidate identities for
their written child and parent names and retain `Derived` evidence. A
candidate identity records syntax and provenance; it does not claim binding.

## Explicitly out of scope

Changing the identity of top-level functions, enums, namespaces, imports,
invocation placeholders, or class-expression members would be a broad
migration of established facts beyond the class/member policy above. Those
declarations keep the discriminator-plus-lexical-name model, so same-named
top-level functions still converge across files; this pathless-declaration
limitation stays recorded in the feature matrix open questions. Legitimate
cross-file declaration merging (global/ambient scopes) is likewise not
represented as one symbol.

## Index invalidation

Interface facts changed with EC-M, so the shared ECMAScript extraction profile
revision moved from 1 to 2; class and member identities changed with EC-3A
(revision 2 to 3); identifier-named class fields were added with EC-3B
(revision 3 to 4); and declaration-less heritage target identities changed
with EC-3D (revision 4 to 5). The TypeScript and JavaScript plugin versions fold
the revision, so p4 unit manifests invalidate and both providers re-extract.
JavaScript shares the profile and re-extracts with whatever facts its grammar
supports. Capability-flag corrections from the same task series changed no
facts and therefore bumped no revision.

## Remaining limitations

- Class-expression members and object-literal methods keep the name-based
  callable form because no owner declaration identity exists for them.
- A getter and setter pair with one name converge to one callable symbol with
  one physical declaration each; accessor semantics are not modeled.
- `abstract_method_signature` declarations in abstract classes emit no facts,
  and neither do computed, string, numeric, or `#private` member names or
  `accessor` auto-accessor fields.
- Top-level functions, enums, namespaces, imports, and invocation
  placeholders remain name-based and converge across files.
- Tree-sitter does not bind written `extends` or `implements` names to
  declarations. Heritage relationships remain structural candidates, so a
  declaration class does not gain dependents through a same-spelled target.
- Invocations inside class `static { }` blocks are attributed to the class
  node, reflecting lexical ownership rather than class-level execution.
- Calls inside decorators are attributed to the decorated member node and keep
  structural invocation evidence; decorator effects are not inferred.
- Same-owner, same-name class members with the same member kind and
  static/instance ownership converge to one node while retaining one physical
  declaration per occurrence. Duplicate-member diagnostics are not produced.
- Interface accessibility, `static`, `async`, accessor (`get`/`set`) and
  `override` keywords are not recorded; only `readonly` and the optional `?`
  marker reach the existing `modifiers` property as `readonly` and `optional`.
- Member type expressions, parameters, generic parameters, and inheritance
  enrichment remain unextracted; `calls`, `references`, and resolved type
  relations stay `NotProduced`.
