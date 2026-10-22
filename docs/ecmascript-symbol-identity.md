# ECMAScript symbol identity for TypeScript and JavaScript

Status: design note recorded **2026-10-22**, extended by EC-3 class identity
and class-member extraction. It documents the audited identity model, the
interface amendment, the class/member identity policy, and the resulting
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
  their owners and member kinds differ.

## EC-3A: class and class-member identity

Class declarations now use the same path-scoped rule as interfaces, and class
members use owner-derived identity:

```text
class declaration: {language}:symbol:{sha256("class" + "\n" + path + "\n" + lexical-qualified-name)}
class member:      {language}:symbol:{sha256(member-kind + "\n" + class-node-id + "\n" + ownership + "\n" + written-name)}

member kinds: method | accessor
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
`tests/Penghou.Hetu.TreeSitter.Tests/TreeSitterEcmaClassIdentityTests.cs`):

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
- **Nested class declarations** retain their containing callable as owner and
  derive identities from it.
- **Duplicate class declarations in one file** converge to one symbol with one
  declaration per physical block: structural extraction cannot verify or
  refute declaration merging, and no offset-based identity is introduced. This
  remains a documented limitation.
- **Unrelated edits** do not move class or member identities.

## Heritage placeholders

Name-based heritage targets are now file-scoped too:

```text
{language}:symbol:{sha256("class" + "\n" + path + "\n" + written-target-name)}
```

A written base name converges with a same-file top-level class declaration
(including when the declaration appears later in the file, because the
symbol-bearing fact replaces the placeholder under the same identity). It
never converges with a class in another file. Interface targets and namespaced
declarations remain separate declaration-less candidate nodes: structural
extraction does not resolve a name across declaration kinds or scopes, and a
converged target is same-file name convergence, not semantic resolution.

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
revision moved from 1 to 2. Class and member identities change with EC-3A,
moving it from 2 to 3. The TypeScript and JavaScript plugin versions fold the
revision, so previous unit manifests invalidate and both providers re-extract;
JavaScript shares the profile and re-extracts with whatever facts its grammar
supports. Capability-flag corrections from the same task series changed no
facts and therefore bumped no revision.

## Remaining limitations

- Class-expression members and object-literal methods keep the name-based
  callable form because no owner declaration identity exists for them.
- A getter and setter pair with one name converge to one callable symbol with
  one physical declaration each; accessor semantics are not modeled.
- `abstract_method_signature` declarations in abstract classes emit no facts,
  and neither do computed, string, numeric, or `#private` member names.
- Top-level functions, enums, namespaces, imports, and invocation
  placeholders remain name-based and converge across files.
- Interface accessibility, `static`, `async`, accessor (`get`/`set`) and
  `override` keywords are not recorded; only `readonly` and the optional `?`
  marker reach the existing `modifiers` property as `readonly` and `optional`.
- Member type expressions, parameters, generic parameters, and inheritance
  enrichment remain unextracted; `calls`, `references`, and resolved type
  relations stay `NotProduced`.
