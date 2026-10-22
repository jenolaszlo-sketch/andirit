# Language feature catalog and coverage matrix

Status: permanent architecture/capability inventory, initial audit 2026-10-21.
Implementation baseline: `5a5e38749570bad36630485d462f9ac92ade39a9` on canonical
`jenolaszlo-sketch/penghou-hetu`. Findings describe code and existing fixture
assertions at that revision, not a new test run or a guarantee about another
publication. Update the baseline and evidence anchors when claims change.

The [language roadmap](language-support-roadmap.md) records priorities. This
catalog contains **46 features** chosen for useful repository questions rather
than grammar completeness. Language-specific candidates are evaluation topics,
not automatic implementation requirements. No universal AST is intended.

## Reading the four independent dimensions

**A. Applicability:** `A` = Applicable; `N` = Not applicable; `C` = Conditional or
dialect-dependent; `U` = Undetermined. Comparable constructs count only where
the stated feature definition fits. Applicability does not imply extraction.

**B. Graph representation:** `E` = Existing representation; `L` = Existing
representation with limitations; `X` = Proposed extension; `?` = Undecided.
The catalog specifies kinds/properties. These are representability assessments,
not claims that a plugin emits them. Proposed strings are design candidates,
not installed vocabulary or a new public enum.

**C. Extraction coverage:** `V` = Verified by a specific existing fixture
assertion for the stated subset; `P` = Partial (subset implementation, missing
dedicated assertions, or both); `NI` = Not implemented in the inspected plugin;
`UA` = Unavailable in an actual analysis context; `NA` = Not applicable;
`NY` = Not yet assessed. `V` never means exhaustive language support. Fixture
source containing syntax without an output assertion is not verification.
Planned languages have a separate `Planned` implementation status, not `V`.

**D. Evidence:** `S` = Structural; `R` = Resolved; `D` = Derived; `I` = Inferred;
`-` = no feature output. They classify how a fact was established, not numeric
confidence. Catalog requirements may allow several qualities, but actual output
must preserve its quality. `calls` requires `R`; `invokes-name` requires `S`.
A matching name does not bind a target. Deterministic pattern derivation is `D`,
never compiler resolution. `I` cannot meet deterministic conformance and only
inferred evidence may carry probabilistic confidence.

Existing-language cells below use `applicability:coverage/evidence`. Join each
row with its catalog representation to obtain all four dimensions. For example,
TypeScript `declaration.enum` is `A:V/S`, but representation is `L`: the enum
node exists without enum-member coverage or a dedicated enum classification.
For nodes/declarations, `S` describes structural discovery and provenance;
`CodeGraphNode` and `CodeGraphDeclaration` do not themselves have an
`EvidenceQuality` field. Relationship qualities are stored in `CodeEvidence`.

## Stable feature identifiers and graph catalog

IDs are lowercase dotted names with kebab-case segments. They describe consumer
concepts, not grammar node names, API enum members, or stored graph identities.
Keep an ID's meaning stable; record a replacement/deprecation rather than reuse
it for a different feature. Future serialization must version the catalog and
its definitions. Applicability for every row appears in the two matrices below.

In this catalog, `type`, `interface`, `callable`, `field`, etc. are the actual
string values in `CodeNodeKinds`; edges use `CodeEdgeKinds` values. Common
properties are `language`, `canonical-key`, `symbol-kind`, `access`, and
`modifiers`. Use `CodeGraphDeclaration`, `SymbolId`, names, and source locations
to connect stable symbols to physical occurrences. A usable node kind alone
does not encode all language semantics.

Each row defines the concept and comprehension value, existing/proposed graph
mapping, required evidence and limits, and feature-specific fixture expectations.
The common conformance requirements below apply to every row.

### Declarations and symbols (18)

| Stable identifier | Meaning and repository use | B: representation | Required evidence and known limits | Feature fixture expectations |
| --- | --- | --- | --- | --- |
| `declaration.module` | Module/package/crate boundaries for navigation and ownership. | L: `project`, `file`, `namespace`, or `type` with declarations; module identity convention undecided. | S for written structure; package/crate/module are not interchangeable. | Multiple files/modules; explicit versus implicit boundaries; dynamic names omitted. |
| `declaration.namespace` | Named scopes for qualified symbol discovery. | E: `namespace`, `contains`, `declares`. | S; lexical nesting is not dependency or import binding. | Nested and file-scoped namespaces; exact qualified names. |
| `declaration.class` | Class identities for member discovery and impact. | L: `type`, declarations, `contains`; no universal class discriminator. | S declarations; semantic identity may use provider binding. Do not infer class semantics from every `type`. | Named/nested/abstract classes; reject unnamed identities without a convention. |
| `declaration.struct` | Value/record-like aggregate owners where the language has structs. | L: `type`; struct/value semantics need a convention. | S; do not equate Java classes with Go/Rust/C# structs. | Named structs, fields, receiver/implementation ownership. |
| `declaration.interface` | Explicit contract declarations for implementation discovery. | L: `interface`; Elixir protocols currently use `type`. | S declarations; protocol equivalence is limited and implementation is separate. | Contract node, member signatures, declared heritage; protocol cases separate. |
| `declaration.trait` | Reusable trait contracts for Rust comprehension. | ?: assess `interface`/`type` plus a discriminator before extending vocabulary. | S declaration; trait definitions do not prove implementations. | Required/default members and bounds; negative implementation case. |
| `declaration.enum` | Named finite-domain type for symbol search. | L: `type`; existing `symbol-kind` is not a precise enum discriminator. | S; an enum node does not imply members or values. | Enum identity plus separately checked member coverage. |
| `declaration.enum-member` | Individual named cases for references and change impact. | L: candidate `field` plus `constant-value`; variants with payloads need design. | S declarations; values only from authoritative evaluation or literal syntax with provenance. | Explicit/implicit values, duplicate names in different enums, payload variants. |
| `declaration.type-alias` | Named type synonym for navigation. | L: `type` declaration; alias target meaning remains separate. | S name; no inferred target binding or expanded type expression. | Alias node plus separate positive/negative target assertions. |
| `declaration.callable` | Functions/methods for lookup and caller analysis. | E: `callable`, declarations, `contains`. | S declaration; overload identity and receiver ownership are provider-specific. | Top-level/member/local/generator forms and signature collisions. |
| `declaration.constructor` | Construction entry points and initialization impact. | L: `callable`; no common constructor-kind property. | S explicit declaration, R bound constructor calls; implicit constructors may have no indexed declaration. | Explicit/static/implicit cases and initializer calls checked separately. |
| `declaration.field` | Stored members for state and reference discovery. | E: `field`, declarations, `contains`, applicable `access`/`modifiers`/`constant-value`. | S structure; computed runtime assignments do not prove declarations. | Static/instance/private/constant fields; exclude arbitrary assignments. |
| `declaration.property` | Accessor-backed or declared properties for public-surface queries. | L: `property`; language accessor/descriptor semantics vary. | S declarations; do not relabel every method or assignment a property. | Get/set/indexer/auto forms; JS descriptors require explicit separate scope. |
| `declaration.parameter` | Callable inputs for signature navigation. | L: `parameter`, containment; typing not implied by node presence. | S named parameter; destructuring, receiver and default arguments need scoped rules. | Names/positions/owners, defaults, destructuring and unsupported patterns. |
| `declaration.nested` | Nested owners to connect source locations to enclosing symbols. | E: declarations plus `contains` and qualified names. | S; anonymous callbacks need identity before claiming independent ownership. | Nested types/functions; enclosing-symbol lookup in their bodies. |
| `declaration.generic-parameter` | Type parameters and constraints for generic API comprehension. | X: evaluate a `type-parameter` kind or explicit properties/relationships. | S written parameters; resolved instantiation is a different feature. | Type/method parameters, shadowing, constraints; no guessed specialization. |
| `declaration.anonymous-function` | Lambda/function-expression identities and body ownership. | L: `callable`; stable anonymous identity/locations need design. | S; visiting a callback body does not create an independent callable. | Named variable values, inline callbacks, repeated identical bodies and edits. |
| `declaration.merged` | One symbol with several physical declarations. | L: one symbol/node, multiple `CodeGraphDeclaration` records. | Provider identity must establish the merge; same spelling alone is insufficient. | C# partial files, TS compatible/incompatible merging, same name across modules. |

### Relationships (13)

| Stable identifier | Meaning and repository use | B: representation | Required evidence and known limits | Feature fixture expectations |
| --- | --- | --- | --- | --- |
| `relationship.contains` | Owner/member hierarchy for bounded navigation. | E: `contains`. | S; containment is not a resolved dependency. | Smallest indexed owner, no self-edge, nested and top-level cases. |
| `relationship.declares` | File-to-symbol link for locating physical definitions. | E: `declares` plus physical declarations. | S; one symbol may have multiple occurrences. | Exact file/location links, partial symbols, deletion of one occurrence. |
| `relationship.inherits` | Explicit base relationships for impact and implementations. | L: `inherits`; multi-base order and generic arguments not modeled. | S for written heritage, R for bound base identities; D for pattern-derived cases. No guessed dynamic bases or automatic transitive closure. | Class/interface extension, multiple bases, expression bases and missing targets. |
| `relationship.implements` | Explicit interface adoption for implementation queries. | L: `implements`; no proof that all runtime obligations hold from syntax alone. | S written assertion or R authoritative binding. Implicit satisfaction requires separate analysis. | Class/struct adoption, interface extension classified as inheritance. |
| `relationship.trait-implementation` | Trait/type/implementation association for Rust navigation. | ?: assess `implements` plus implementation-block ownership. | S explicit block; R binding requires analysis context; bounds/blanket impls not guessed. | Inherent versus trait impls, associated members, conditional/blanket impls. |
| `module.import` | Written dependency/import scope for repository navigation. | L: `imports`; C# `import-alias`, `import-static`, `import-global`, `import-scope`. | S source target or R bound namespace/type; import spelling does not bind exported symbols. | Named/default/type-only/side-effect and scoped imports, missing/external targets. |
| `module.export` | Public exports and re-exports for module consumers. | X: export/re-export binding representation to design; `imports` alone is insufficient. | S syntax observations; R only if target binding established. | Local/default/star/renamed/re-export forms, cycles and unresolved sources. |
| `relationship.depends-on` | Project/package dependencies for architecture and impact. | L: `depends-on`, `project`/`package`, `package-version`, `package-condition`. | S declared packages; current C# project links use R in its constructed context. No evaluated conditions or transitive closure. | Internal/missing/cyclic projects, conditional/versionless packages, caps. |
| `relationship.calls` | Bound caller/callee for impact and test selection. | E: `calls`. | R only, relative to named semantic context; no name matching or complete runtime dispatch claim. | Overloads, constructors, generic/extension/interface dispatch, unresolved/ambiguous/external negatives. |
| `relationship.invokes-name` | Unbound invocation spelling for structural comprehension. | E: `invokes-name` to declaration-less `callable`. | S only; a matching node name cannot become a resolved call. | Simple/member/new forms; exclude JSX/control forms and unnameable dynamic targets. |
| `relationship.references` | Symbol usage for change impact. | E: `references`. | R for current C# output; any future structural convention must be explicit and cannot imply binding. | Smallest owner, repeated-use evidence, external/ambiguous omissions. |
| `type.reference` | Uses of types in declarations/expressions for API impact. | L: current C# uses `references`; no dedicated type-use role, `returns`/`accepts` remain unproduced. | R bound type uses or explicitly unbound S names; generic syntax alone is insufficient. | Parameter/return/base/attribute type sites, unresolved `var`, generic arguments. |
| `relationship.alias` | Import/type aliases to follow names to targets. | L: C# import alias properties; general alias binding is undecided. | S alias spelling or R target binding, distinguished explicitly. | Renamed imports, namespace/type aliases, shadowing and ambiguous targets. |

### Language-specific evaluation candidates (15)

| Stable identifier | Meaning and repository use | B: representation | Required evidence and known limits | Feature fixture expectations |
| --- | --- | --- | --- | --- |
| `javascript.prototype-inheritance` | Legacy prototype relationships for impact. | L: `inherits` between name-based `type` placeholders. | D exact patterns, never R; shadowed builtins and runtime mutation are not analyzed. | `Object.create`, `new`, `util.inherits`, `Object.setPrototypeOf`; dynamic/shadowed negatives. |
| `javascript.commonjs` | CommonJS dependency/public surface discovery. | L: literal `require` uses `imports`; export graph needs `module.export` design. | S written patterns; no runtime module evaluation or loader binding. | Literal/dynamic `require`, `exports`, `module.exports`, aliases and reassignment. |
| `type.union` | Alternatives in types for API comprehension. | ?: type-expression summary or bounded relationships to design. | S syntax or R bound components; alias node alone is insufficient. | Named/inline unions, nesting, dynamic-language comparable forms assessed separately. |
| `type.intersection` | Combined type constraints for API comprehension. | ?: bounded type-expression representation to design. | S syntax or R bound components; do not manufacture inheritance. | Intersection components and negative inheritance assertions. |
| `type.mapped` | TypeScript computed type members for API discovery. | ?: retain bounded declaration-level description versus computed members. | S pattern; R expansion requires authoritative type analysis. | Key remapping/optional modifiers; no invented expanded fields. |
| `type.conditional` | TypeScript conditional type dependencies. | ?: bounded type-expression representation to design. | S written condition; no guessed evaluated branch. | Branches, `infer`, distributive cases, unsupported evaluation. |
| `language.decorator` | Python/TS/dialect-JS decorators for entry points and wiring. | L: `attributes` property can hold bounded names; application/target semantics undecided. | S written decorator; R bound identity only with context, never assumed runtime effects. | Class/member/function targets, aliases, factories and dynamic expressions. |
| `go.embedding` | Promoted member origins and embedded-type dependencies. | ?: explicit embedding role beyond `field`/`references` to design. | S embedded declaration; promotion and method sets require analysis. | Value/pointer embeds, collisions, transitive promotion negatives. |
| `go.implicit-interface` | Implicit implementation discovery. | L: `implements` can carry a result; method-set/context contract undecided. | R authoritative method-set analysis or documented D derivation, never syntax-only proof. | Pointer/value receivers, package visibility, generic constraints, negative satisfaction. |
| `language.macro-boundary` | Rust/Elixir macro declarations versus generated facts. | ?: invocation/declaration observations possible; expansion provenance design required. | S boundary; no claim about generated declarations from unexpanded syntax. | Macro definition/use, expansion unavailable, cfg/context and stale generated facts. |
| `language.annotation` | C#/Java/Kotlin attributes/annotations for tests and framework entry points. | L: bounded `attributes`, `test-method`, `http-endpoint`, `route-template`, plus `references`. | S written metadata; R bound names/references; metadata is not executed behavior. | Allowlist positives/near misses, bounded values and target resolution. |
| `callable.overload` | Distinct signatures for exact method selection. | L: distinct symbol/canonical identities and declarations; no shared cross-language signature schema. | S declarations with unambiguous identity; R chosen target. | Same name/different signatures, ambiguity, arity and declaration-only overloads. |
| `callable.extension` | Receiver-style extensions for navigation. | L: `callable` plus R `calls` can represent bound usage; explicit receiver mapping undecided. | S declaration or R bound usage; member spelling alone is insufficient. | Extension versus ordinary method, receiver type and name collisions. |
| `kotlin.companion-object` | Companion ownership and factory/static-like access. | ?: `type` plus `contains` can hold structure; companion role/access mapping undecided. | S explicit companion; do not equate it to Java static members. | Named/unnamed companions, members and access from enclosing type. |
| `type.sealed` | Restricted hierarchies for impact and exhaustive-case context. | L: `modifiers` can hold `sealed`; permitted-subtype/exhaustiveness relationships undecided. | S modifier; R permitted identities only with context. | Sealed/final distinctions and negative completeness/exhaustiveness claims. |

## Existing-language applicability and audited extraction

All cells use the independent applicability, extraction, and evidence notation
defined above. Representation comes from the catalog; refinements and gaps are
in the findings below. `NI` means no dedicated emitted facts for this feature,
even if the enclosing construct's node or callback-body facts survive.

| Feature ID | C# / Roslyn | TypeScript / Tree-sitter | JavaScript / Tree-sitter | Elixir / Tree-sitter | Audit anchors |
| --- | --- | --- | --- | --- | --- |
| `declaration.module` | A:P/S | A:P/S | A:P/S | A:V/S | CS-D, EC-D, EL-D |
| `declaration.namespace` | A:V/S | A:V/S | N:NA/- | N:NA/- | CS-D, EC-D |
| `declaration.class` | A:V/S | A:V/S | A:V/S | N:NA/- | CS-D, EC-D |
| `declaration.struct` | A:P/S | N:NA/- | N:NA/- | A:NI/- | CS-D; Elixir defstruct has no dedicated mapping |
| `declaration.interface` | A:V/S | A:P/S | N:NA/- | A:P/S | CS-R, EC-D, EL-D (protocol subset) |
| `declaration.trait` | N:NA/- | N:NA/- | N:NA/- | N:NA/- | Construct definition above |
| `declaration.enum` | A:P/S | A:V/S | N:NA/- | N:NA/- | CS-D, CS-P, EC-D |
| `declaration.enum-member` | A:NI/- | A:NI/- | N:NA/- | N:NA/- | CS-D selector, EC-D dispatch |
| `declaration.type-alias` | A:NI/- | A:V/S | N:NA/- | A:NI/- | CS-D, EC-D, EL-D (@type skipped) |
| `declaration.callable` | A:V/S | A:V/S | A:V/S | A:V/S | CS-D, EC-D, EL-D |
| `declaration.constructor` | A:V/S | A:P/S | A:V/S | N:NA/- | CS-R, EC-D |
| `declaration.field` | A:V/S | A:NI/- | A:NI/- | A:NI/- | CS-D, EC-D, EL-D |
| `declaration.property` | A:V/S | A:NI/- | A:NI/- | N:NA/- | CS-D, EC-D |
| `declaration.parameter` | A:V/S | A:NI/- | A:NI/- | A:NI/- | CS-D, EC-D, EL-D |
| `declaration.nested` | A:V/S | A:V/S | A:P/S | A:P/S | CS-R local functions, EC-D Geometry/helper, EL-D |
| `declaration.generic-parameter` | A:NI/- | A:NI/- | N:NA/- | N:NA/- | CS-D, EC-D |
| `declaration.anonymous-function` | A:NI/- | A:P/S | A:P/S | A:NI/- | CS-D, EC-D HandleDeclarator, EL-D stoplist |
| `declaration.merged` | A:V/S | A:P/S | N:NA/- | A:P/S | CS-D partials; EC-D/EL-D name-derived identities |
| `relationship.contains` | A:V/S | A:V/S | A:V/S | A:V/S | CS-D, EC-D, EL-D |
| `relationship.declares` | A:V/S | A:V/S | A:P/S | A:V/S | CS-D, EC-D, EL-D |
| `relationship.inherits` | A:V/R | A:V/S | A:V/S,D | N:NA/- | CS-R, EC-H |
| `relationship.implements` | A:V/R | A:V/S | N:NA/- | A:NI/- | CS-R, EC-H; EL-D no defimpl mapping |
| `relationship.trait-implementation` | N:NA/- | N:NA/- | N:NA/- | N:NA/- | Rust-specific feature scope |
| `module.import` | A:V/R | A:V/S | A:V/S | A:V/S | CS-R imports; EC-I; EL-I |
| `module.export` | N:NA/- | A:P/S | A:P/S | A:NI/- | EC-I handles declarations/source only; EL-D |
| `relationship.depends-on` | A:V/S,R | A:NI/- | A:NI/- | A:NI/- | CS-P; EC-D/EL-D only imports |
| `relationship.calls` | A:V/R | A:NI/- | A:NI/- | A:NI/- | CS-R; EC-I/EL-I negative assertions |
| `relationship.invokes-name` | A:NI/- | A:V/S | A:V/S | A:V/S | CS-R; EC-I, EL-I |
| `relationship.references` | A:V/R | A:NI/- | A:NI/- | A:NI/- | CS-R; EC-I/EL-I negative assertions |
| `type.reference` | A:P/R | A:NI/- | C:NI/- | A:NI/- | CS-R type uses via references; EC-D, EL-D |
| `relationship.alias` | A:P/R | A:NI/- | A:NI/- | A:P/S | CS-R import properties; EC-I, EL-I |
| `javascript.prototype-inheritance` | N:NA/- | A:P/D | A:V/D | N:NA/- | EC-H shared code, JS prototype assertions |
| `javascript.commonjs` | N:NA/- | C:P/S | C:P/S | N:NA/- | EC-I literal require only |
| `type.union` | N:NA/- | A:NI/- | C:NI/- | A:NI/- | EC-D alias name only; EL-D specs skipped |
| `type.intersection` | N:NA/- | A:NI/- | C:NI/- | N:NA/- | EC-D |
| `type.mapped` | N:NA/- | A:NI/- | N:NA/- | N:NA/- | EC-D |
| `type.conditional` | N:NA/- | A:NI/- | N:NA/- | N:NA/- | EC-D |
| `language.decorator` | N:NA/- | C:NI/- | C:NI/- | N:NA/- | EC-D no decorator mapping |
| `go.embedding` | N:NA/- | N:NA/- | N:NA/- | N:NA/- | Go-specific feature scope |
| `go.implicit-interface` | N:NA/- | N:NA/- | N:NA/- | N:NA/- | Go-specific feature scope |
| `language.macro-boundary` | N:NA/- | N:NA/- | N:NA/- | A:P/S | EL-D macro definition treated as callable; no expansion |
| `language.annotation` | A:V/S,R | N:NA/- | N:NA/- | C:NI/- | CS-P/CS-R; EL-D attributes skipped |
| `callable.overload` | A:V/S,R | A:P/S | N:NA/- | A:P/S | CS-D/CS-R; EC-D name-only identity; EL-D arity |
| `callable.extension` | A:P/S,R | N:NA/- | N:NA/- | N:NA/- | CS-R resolved extension calls; no receiver-role property |
| `kotlin.companion-object` | N:NA/- | N:NA/- | N:NA/- | N:NA/- | Kotlin-specific feature scope |
| `type.sealed` | A:P/S | N:NA/- | N:NA/- | N:NA/- | CS-P modifiers; no permitted-subtype model |

JavaScript type-related `C` cells refer to optional documentation/type dialects,
not a claim that ECMAScript has TypeScript types. Decorators and CommonJS depend
on the configured language/runtime/dialect. Elixir `merged`/`overload` refer to
module/function clauses and arities, not C# partial types or method overload
resolution. C# using aliases are covered under `relationship.alias`, not emitted
as `declaration.type-alias` nodes. C# access/public-surface queries are not a
module export graph.

### Evidence anchors

These anchors bind claims to implementation paths and named fixture assertions.
`P` rows without a named assertion are code-inspected subsets, not verified
feature conformance. Omission findings follow the declaration selectors/profile
dispatch and emitted kinds, not the absence of a test name.

| Anchor | Implementation | Existing fixture evidence |
| --- | --- | --- |
| CS-D | [Symbols](../src/Penghou.Hetu.CSharp/CSharpCodeGraphPlugin.Symbols.cs), [GraphBuilder.AddDeclarations/AddSymbol](../src/Penghou.Hetu.CSharp/CSharpCodeGraphPlugin.GraphBuilder.cs) | [CSharpCodeGraphPluginTests](../tests/Penghou.Hetu.CSharp.Tests/CSharpCodeGraphPluginTests.cs): `ExtractAsync_ModelsPartialTypesMembersParametersAndContainment`; generic/enum/struct declarations are not independently fully asserted there. |
| CS-R | [GraphBuilder relationship handlers](../src/Penghou.Hetu.CSharp/CSharpCodeGraphPlugin.GraphBuilder.cs), [Session coverage](../src/Penghou.Hetu.CSharp/CSharpCodeGraphPlugin.Session.cs) | Same C# test file: `ExtractAsync_EmitsInheritanceImplementsAndCallsEdges`, `ExtractAsync_ClassifiesExplicitInterfaceRelationshipsWithoutTransitiveEdges`, `ExtractAsync_EmitsImplicitCreationAndConstructorInitializerCalls`, `ExtractAsync_ModelsLocalFunctionsAndUsesThemAsCallOwners`, `ExtractAsync_ReferencesUseTheSmallestIndexedOwner`, `ExtractAsync_ImportsPreserveAliasFlagsAndSemanticScope`, `ExtractAsync_ResolvesGenericMethodCallsToTheirDefinition`, `ExtractAsync_ResolvesExtensionMethodCallsToTheirDefinition`, `ExtractAsync_ResolvesInterfaceDispatchToTheInterfaceMember`, unresolved/ambiguous/cross-project and per-unit coverage cases. |
| CS-P | [PropertyFactory](../src/Penghou.Hetu.CSharp/CSharpCodeGraphPlugin.PropertyFactory.cs), [project discovery](../src/Penghou.Hetu.CSharp/CSharpProjectDiscovery.cs), GraphBuilder | Same C# test file: `ExtractAsync_EmitsReferencesImportsAndCoverage` asserts a constant field, not enum members; `ExtractAsync_EmitsPackageReferenceNodesWithSyntaxEvidence`, exact test/endpoint allowlist tests, `ExtractAsync_EmitsExercisedByEdgesFromAllowlistedTestMethods`. |
| EC-D | [EcmaScriptExtractionProfile.Walk/TryEmitNamed/HandleDeclarator](../src/Penghou.Hetu.TreeSitter/EcmaScriptExtractionProfile.cs) | [TypeScript tests](../tests/Penghou.Hetu.TreeSitter.Tests/TreeSitterTypeScriptPluginTests.cs): `ExtractAsync_ModelsClassesInterfacesAndContainment`, `ExtractAsync_TreatsArrowAndFunctionExpressionsAsCallables` asserts `render` (arrow); [JavaScript tests](../tests/Penghou.Hetu.TreeSitter.Tests/TreeSitterJavaScriptPluginTests.cs): `ExtractAsync_ModelsClassesFunctionsAndContainment` asserts constructor, methods, and `helper` (function expression). |
| EC-I | Same profile: HandleExport, EmitImport, TryEmitRequire, EmitCall, EmitNew | Same TS tests: `ExtractAsync_EmitsImportsAndStructuralInvocationsOnly`, `ExtractAsync_TsxTreatsJsxAsStructureNotInvocation`; same JS tests: `ExtractAsync_TreatsRequireAndImportAsImportsAndOtherCallsAsInvocations`. |
| EC-H | Same profile: EmitHeritage, HeritageName, prototype handlers, StructuralTypeNode | Same TS tests: `ExtractAsync_EmitsStructuralHeritage`; same JS tests: `ExtractAsync_DerivesPrototypeInheritanceAsDerivedEvidence`. |
| EL-D | [ElixirExtractionProfile](../src/Penghou.Hetu.TreeSitter/ElixirExtractionProfile.cs): module/function target sets, Walk, TryEmitModule/Function | [Elixir tests](../tests/Penghou.Hetu.TreeSitter.Tests/TreeSitterElixirPluginTests.cs): `ExtractAsync_ModelsModulesFunctionsAndContainment`; protocol/macro branches lack dedicated output assertions. |
| EL-I | Same profile: ImportTargets, TryEmitImport/Invocation, attribute and control-form skipping | Same Elixir tests: `ExtractAsync_EmitsImportsAndStructuralInvocationsOnly`, `ExtractAsync_ScopesScriptInvocationsToTheirFile`, `ExtractAsync_ReportsCapabilitiesHonestly`. |

### Audit findings and priority gaps

1. **TypeScript declarations are a useful subset.** Classes, interfaces, aliases,
   enums, namespaces, functions and methods have nodes and physical declarations.
   Interfaces return immediately after heritage: members are not visited.
   Aliases return after their name; union/intersection/type-expression components
   are not extracted. Enum bodies are walked but no enum-member handler exists.
   Fields/properties, parameters and generic parameters have no node emission.
   The TS fixture contains a constructor, field and parameter-property syntax;
   only JS has a dedicated constructor assertion. None proves member completeness.
2. **Callable ownership and merging need design.** Variable declarators with an
   arrow/function/generator value receive named callables. Inline callbacks do
   not; their body invocations remain attributed to the surrounding scope.
   Method definitions become callables, including constructors/accessors, without
   a property model. ECMAScript symbol IDs hash discriminator plus qualified name,
   without file/module or signature context: name convergence is not verified
   declaration merging or overload-safe identity. Anonymous classes/functions and
   declaration-only signatures have no general independent-symbol contract.
3. **Heritage targets are names.** `HeritageName` accepts identifiers,
   type-identifiers and member expressions; generic/qualified grammar shapes and
   mixin/expression bases are not comprehensively handled. Targets are
   declaration-less `type` nodes keyed with the class discriminator. A same-name
   class may converge, but an interface node has a different discriminator, and
   namespace qualification is not semantic binding. Do not assume TS `implements`
   reaches the declared interface or that this is a complete implementation graph.
4. **Prototype inheritance is derived.** Four JS patterns have assertions for D
   edges; the same shared code is available to TS without a dedicated TS pattern
   fixture. Arbitrary mutations, shadowed builtins, dynamic bases and runtime
   prototype state are not resolved. A structural plugin can therefore contain
   both S and D facts; “all Tree-sitter facts are Structural” is inaccurate.
5. **Imports are not exports or package dependencies.** ESM source strings and
   literal `require` produce imports to placeholder `type` nodes. Import binding
   names/type-only flags are not retained. Exported declarations are traversed;
   re-export source strings become imports, and specifiers/default/public export
   bindings are not modeled. `module.exports`/`exports` assignments have no export
   handler. Dynamic `require` cannot establish an import. No Tree-sitter profile
   emits semantic calls, references, or `depends-on` relationships.
6. **C# preserves its semantic strengths and limits.** Project-aware Roslyn
   provides internal/cross-project calls, references, explicit heritage and scoped
   imports. Generic calls normalize to definitions; this does not emit generic
   parameter nodes or an instantiated type graph. The symbol selector omits
   `EnumMemberDeclarationSyntax`, type parameters, using-alias declarations and
   lambdas; an enum type/constant field is not enum-member support. `returns` and
   `accepts` remain explicitly `NotProduced`. Host runtime platform references,
   lightweight unevaluated project conditions, omitted externals, and the
   available compilation context limit R claims. Interface dispatch binds to the
   interface member, not every possible runtime implementation.
7. **Elixir remains structural.** `defmodule`/`defprotocol` map to `type`; named
   definitions (including macro/guard/delegate forms) map to `callable` with
   written arity. Alias/import/require/use observe module targets; aliases are
   not expanded into bound references. Parameters, specs/attributes, `defstruct`,
   `defimpl`, anonymous-function identities and macro expansion are not modeled.
   Definition heads/control forms are not fabricated invocations.
8. **Broad capability metadata needs a follow-up audit.** C# advertises Syntax,
   Symbols, Types, References, Calls, Inheritance, Imports, Declarations and
   Diagnostics. All current Tree-sitter descriptors advertise only Syntax,
   Declarations, Imports and Diagnostics, even though TS/JS profiles emit
   inheritance and TS implementation edges. This discrepancy is recorded, not
   fixed here. Neither a missing flag nor an emitted edge proves full feature
   coverage. Profile/grammar/package version invalidation should be reviewed
   whenever emitted behavior changes; this task changes no versions or flags.

### Grammar and verification boundaries

The [grammar registry](../src/Penghou.Hetu.TreeSitter/TreeSitterGrammarRegistry.cs)
and [grammar pins](../src/Penghou.Hetu.TreeSitter/native/grammar-pins.md) record
Elixir v0.3.5/ABI 14, TypeScript and TSX v0.23.2/ABI 14, and JavaScript
v0.25.0/ABI 15 with their exact commits. TSX is a separate grammar selected for
`.tsx`; JS/JSX uses the JavaScript descriptor, not the TypeScript descriptor.
Elixir natives are checked in; TS/JS natives come from the pinned binding.
Supported extensions are `.ex`/`.exs`, `.ts`/`.mts`/`.cts`/`.tsx`, and
`.js`/`.mjs`/`.cjs`/`.jsx`. None implies all syntax of that dialect is mapped.

Existing language tests assert partial-file recovery, repeated deterministic
extraction and batch validity; Elixir also asserts undecodable-unit clearing,
and C# asserts stale-call removal on rebinding and incremental project cleanup.
There is no feature-by-feature conformance suite proving all cells. Negative
dynamic heritage, module collisions, complete export forms, TS prototype
patterns, and per-feature stale-fact removal remain verification work.

## Planned-language applicability and intended coverage

Implementation status for **every Python, Java, Go, Rust and Kotlin cell is
Planned: no supported Hetu plugin exists**. Provider/grammar choice is TBD.
These cells record applicability and a candidate extraction milestone only:
`A:S` means applicable, intended useful structural subset; `A:M` means applicable,
requires a separate semantic/derived milestone; `C:Q` means conditional, evaluate
scope first; `U:Q` means applicability/design undetermined; `N:-` is not applicable.
`S` here is an intended milestone, not emitted evidence or verified coverage.
Join these rows with catalog representation requirements; X/? choices remain open.

| Feature ID | Python (Phase 1 first) | Java (Phase 1 second) | Go (Phase 2 first) | Rust (Phase 2 second) | Kotlin (Phase 3) |
| --- | --- | --- | --- | --- | --- |
| `declaration.module` | A:S | A:S | A:S | A:S | A:S |
| `declaration.namespace` | C:Q | N:- | N:- | N:- | N:- |
| `declaration.class` | A:S | A:S | N:- | N:- | A:S |
| `declaration.struct` | N:- | N:- | A:S | A:S | N:- |
| `declaration.interface` | C:Q | A:S | A:S | N:- | A:S |
| `declaration.trait` | N:- | N:- | N:- | A:S | N:- |
| `declaration.enum` | C:Q | A:S | N:- | A:S | A:S |
| `declaration.enum-member` | C:Q | A:S | N:- | A:S | A:S |
| `declaration.type-alias` | C:Q | N:- | A:S | A:S | A:S |
| `declaration.callable` | A:S | A:S | A:S | A:S | A:S |
| `declaration.constructor` | C:Q | A:S | N:- | N:- | A:S |
| `declaration.field` | C:Q | A:S | A:S | A:S | A:S |
| `declaration.property` | C:Q | N:- | N:- | N:- | A:S |
| `declaration.parameter` | A:S | A:S | A:S | A:S | A:S |
| `declaration.nested` | A:S | A:S | A:S | A:S | A:S |
| `declaration.generic-parameter` | C:Q | A:S | A:S | A:S | A:S |
| `declaration.anonymous-function` | A:S | A:S | A:S | A:S | A:S |
| `declaration.merged` | N:- | N:- | N:- | C:Q | N:- |
| `relationship.contains` | A:S | A:S | A:S | A:S | A:S |
| `relationship.declares` | A:S | A:S | A:S | A:S | A:S |
| `relationship.inherits` | A:S | A:S | N:- | A:S | A:S |
| `relationship.implements` | C:Q | A:S | A:M | N:- | A:S |
| `relationship.trait-implementation` | N:- | N:- | N:- | A:S | N:- |
| `module.import` | A:S | A:S | A:S | A:S | A:S |
| `module.export` | C:Q | C:Q | A:S | A:S | A:S |
| `relationship.depends-on` | A:S | A:S | A:S | A:S | A:S |
| `relationship.calls` | A:M | A:M | A:M | A:M | A:M |
| `relationship.invokes-name` | A:S | A:S | A:S | A:S | A:S |
| `relationship.references` | A:M | A:M | A:M | A:M | A:M |
| `type.reference` | C:Q | A:S | A:S | A:S | A:S |
| `relationship.alias` | A:S | N:- | A:S | A:S | A:S |
| `javascript.prototype-inheritance` | N:- | N:- | N:- | N:- | N:- |
| `javascript.commonjs` | N:- | N:- | N:- | N:- | N:- |
| `type.union` | C:Q | C:Q | C:Q | N:- | N:- |
| `type.intersection` | U:Q | C:Q | C:Q | N:- | C:Q |
| `type.mapped` | N:- | N:- | N:- | N:- | N:- |
| `type.conditional` | N:- | N:- | N:- | N:- | N:- |
| `language.decorator` | A:S | N:- | N:- | N:- | N:- |
| `go.embedding` | N:- | N:- | A:S | N:- | N:- |
| `go.implicit-interface` | N:- | N:- | A:M | N:- | N:- |
| `language.macro-boundary` | N:- | N:- | N:- | A:S | N:- |
| `language.annotation` | N:- | A:S | N:- | A:S | A:S |
| `callable.overload` | C:Q | A:S | N:- | N:- | A:S |
| `callable.extension` | N:- | N:- | N:- | N:- | A:S |
| `kotlin.companion-object` | N:- | N:- | N:- | N:- | A:S |
| `type.sealed` | N:- | A:S | N:- | N:- | A:S |

These are conceptual assessments to refine against a selected language version,
dialect and provider, not language-lawyer completeness claims. Python contracts,
enum/property/decorator patterns and overload declarations can depend on libraries
and typing conventions; Rust attributes are comparable annotation syntax, but
procedural macro effects require the macro boundary. Rust trait inheritance here
means explicit supertrait constraints, not class inheritance. Rust multiple impl
blocks need an explicit ownership rule rather than automatic symbol merging.
Go unions/intersections concern interface type sets/constraints, not TypeScript
union values; Java/Kotlin restricted type contexts likewise need scoped designs.
Dynamic Python inheritance and decorator effects remain unresolved absent an
authoritative provider. Java overload declarations can be structural while
selecting a call target remains semantic. Kotlin extension receivers, companions,
top-level declarations, properties and sealed hierarchies need their own fixtures.

## Capability and published-coverage architecture

Preserve the accepted [evidence/provider ADR](evidence-quality-and-provider-strategy.md)
and the existing extensible [kinds](../src/Penghou.Hetu.Abstractions/CodeGraphKinds.cs),
[properties](../src/Penghou.Hetu.Abstractions/CodePropertyKeys.cs),
[capabilities](../src/Penghou.Hetu.Abstractions/CodeGraphCapabilities.cs), and
[coverage contracts](../src/Penghou.Hetu.Abstractions/CodeGraphExtraction.cs).

```text
Feature catalog -> Language applicability -> Plugin capability
  -> Actual extraction -> Graph facts -> Published coverage -> Consumer queries
```

`CodeGraphCapabilities` is a broad plugin declaration, not this feature list.
For example, `Types` does not promise type-parameter nodes or every type relation;
`Declarations` does not promise every declaration production. There is no new
feature-ID enum or capability API in this design.

`CodeRelationshipCoverage` reports a relationship kind and counts, with these
existing runtime states. Documentation labels do not add states to the enum:

| Runtime state | Meaning and distinction from feature inventory |
| --- | --- |
| `Produced` | The implemented relationship scan ran; zero edges may be a valid empty result for its scope. Does not mean all catalog features or language cases are supported. |
| `Partial` | Some output is available with incomplete analysis; inspect diagnostics and counts. Not the same as a static P cell's permanent subset. |
| `NotProduced` | Deliberately not emitted by this plugin/unit (for example C# returns/accepts, Tree-sitter calls/references). Does not establish language inapplicability. |
| `Unavailable` | Coverage could not be determined in this context; never an empty-result claim. |
| `NotApplicable` | Relationship kind does not apply to the plugin/unit. Different from an unimplemented applicable feature. |

Counts distinguish candidates, emitted edges, unresolved, internal/cross-project,
external, ambiguous and unsupported targets. C# reports run-wide and per-project
unit entries for selected kinds. Tree-sitter reports a run-wide fixed list of
contains/declares/imports/invokes-name/inherits/implements/calls/references; kinds
outside each profile's produced set are `NotProduced` (including JS implements
and Elixir inherits/implements). Missing kinds are not implied `Produced` or `NA`.
Its counts reflect emitted observations, not all grammar sites; a parse-error
flag makes produced kinds Partial across the session. Undecodable inputs warn
and clear their units but do not themselves set that parse-error partial flag.
Zero unresolved counts cannot prove complete extraction.

This inventory records feature semantics and fixtures. Actual published coverage
and attributable graph facts govern consumer answers for a publication. Runtime
coverage does not currently contain a versioned feature-inventory binding.
A future machine-readable format must bind catalog version, plugin ID/version,
grammar ID/version/ABI, language/dialect, effective capabilities/provider fallback,
index publication and relevant unit scope. An old graph cannot gain new support
claims from an upgraded installed plugin. Unknown historical claims remain
unknown. Serialization, persistence, compatibility, and catalog loading are
future designs; no storage or API change is made here.

## Future feature conformance

Every advertised feature should eventually bind a scoped claim to deterministic
fixtures. For each applicable provider/dialect, fixtures must check:

1. Exact expected nodes and stable symbol identities, including duplicate names.
2. Physical declarations, locations, owner links and declaration multiplicity.
3. Expected relationships, directions, endpoint identity and explicit omissions.
4. Required properties, value bounds and unsupported property cases.
5. Correct S/R/D/I evidence and provider/version provenance; no deterministic
   confidence scores or S invocation masquerading as an R call.
6. Unsupported, external, ambiguous and dynamic cases with honest coverage and
   diagnostics. Include negative fixtures: unresolvable mixin/dynamic inheritance
   must not invent a resolved edge; dynamic require must not invent an import;
   prototype patterns must remain D even when names match indexed declarations.
7. Partial parsing that retains valid understood facts without claiming complete
   analysis, plus unavailable/undecodable contexts.
8. Repeated extraction and input-order changes yielding deterministic normalized
   facts and coverage within batch/query bounds.
9. Source-change/deletion/rebinding scenarios removing stale nodes, declarations,
   properties and relationships at atomic publication, while failed runs preserve
   the prior graph.

The feature-specific fixture column adds the cases that make each claim useful.
Test identification should also preserve the existing exact allowlists and D
`exercised-by` derivation from R calls; it is not a new language feature merely
because an annotation/decorator parser exists. Semantic support remains specific
to resolved features and the provider's context. Future authoritative providers
for Python, Java, TypeScript, JavaScript, Go, Rust and Kotlin are possible, with no
technology selected. Feature automation is future work, not a framework delivered
by this documentation task.

## Open design questions and maintenance

- Which graph discriminator distinguishes class/struct/enum/alias/module/trait
  without turning the vocabulary into a universal AST? Which distinctions earn
  new kinds versus bounded properties?
- How should generic parameters, bounded type expressions, signature roles,
  Rust impl ownership, embedding, extensions and companion access be normalized?
  Keep C# returns/accepts unproduced until their cross-language meaning is agreed.
- What are stable ECMAScript module-scoped/overload identities and legitimate
  declaration-merging rules? How should heritage placeholders connect to
  interface identities without pretending syntax establishes semantic binding?
- What export/re-export/CommonJS and alias facts answer real consumer queries?
  Which properties preserve written names and which require authoritative binding?
- What analysis context and evidence justify Go satisfaction, Python dynamic
  constructs, Rust expansion and context-dependent annotation/decorator effects?
- How should feature fixture claims be versioned and bound to publications,
  effective fallback capabilities and per-unit coverage without retroactive claims?
  What are the unknown/unsupported and compatibility rules?
- How should the TS/JS capability mismatch and profile-version discipline be
  corrected in future implementation work? Do not change runtime behavior to
  make an aspirational table look complete.
- The ADR's traversal-policy versus answer-filter split stays deferred:
  evidence filters currently apply to every traversed edge. A resolved-only
  traversal cannot cross structural containment. Catalog entries do not change it.

When adding or upgrading support, revise affected cells and evidence anchors,
record the exact audited revision/provider/grammar, add the scoped fixtures in
that implementation task, and preserve prior decision history. Do not mark a
feature verified from a parser rule, a capability flag, or an unasserted fixture.
