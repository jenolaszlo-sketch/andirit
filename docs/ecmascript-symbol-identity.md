# ECMAScript symbol identity for TypeScript and JavaScript

Status: task-scoped design note recorded **2026-10-22** before TypeScript
interface-member extraction. It documents the audited identity model, the
smallest amendment required for safe interface members, and the resulting
limitations. It adds no graph vocabulary and does not change C# semantics.

## Audited model

`EcmaScriptExtractionProfile.TryEmitNamed` derives every declared symbol and
node from one hash:

```text
{language}:symbol:{sha256(discriminator + "\n" + lexical-qualified-name)}
```

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

Interface declarations and interface member signatures are emitted with an
additional source-path component and interface-specific discriminators:

```text
{language}:symbol:{sha256(discriminator + "\n" + path + "\n" + lexical-qualified-name)}

discriminators: interface | interface-method | interface-property
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
`tests/Penghou.Hetu.TreeSitter.Tests/TreeSitterTypeScriptPluginTests.cs`):

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
  their qualified names differ.

## Explicitly out of scope

Changing the identity of classes, functions, enums, namespaces, imports,
invocation placeholders, or heritage placeholders would be a broad migration
of established facts. It is not required for safe interface members, so it is
not done here; the cross-file convergence limitation for those declarations
stays recorded in the feature matrix open questions. Legitimate cross-file
interface merging (global/ambient scopes) is likewise not represented as one
symbol.

## Index invalidation

Interface facts change, so the shared ECMAScript extraction profile revision
moves from 1 to 2. The TypeScript and JavaScript plugin versions fold that
revision, so previous unit manifests invalidate and both providers re-extract;
JavaScript emits unchanged facts. Capability-flag corrections from the same
task series changed no facts and therefore bumped no revision.

## Remaining limitations

- String-literal, numeric, private, and computed member names are unsupported
  and emit no facts.
- Call, construct, and index signatures are unsupported and emit no facts.
- Interface accessibility, `static`, `async`, accessor (`get`/`set`) and
  `override` keywords are not recorded; only `readonly` and the optional `?`
  marker reach the existing `modifiers` property as `readonly` and `optional`.
- Member type expressions, parameters, generic parameters, and inheritance
  enrichment remain unextracted; `calls`, `references`, and resolved type
  relations stay `NotProduced`.
