# Case matrix

Every case names: the actual source marker, the syntactic operation, the
independently established Roslyn resolution, and the accepted-S2.2 scope
classification. Expected values are frozen in
`oracle/expected/oracle-expected.json`; the symbol key is
`<assembly>!<documentation-comment-id>` (the defining declaration; for
constructed generics the original definition is used).

**Scope classification** (from the S2.2 acceptance matrix): `mandatory` =
supported `Invocation` category; `qualification` = relevant to binding but not a
plain invocation fact; `deferred` = informative fixture outside the initial
supported category (property, field, event, method group, delegate, local
function, type reference, constructor); `negative` = refusal/coverage case.

## 1. Direct and overloaded method references (mandatory)

| ID | Operation | Expected resolution | Target |
|---|---|---|---|
| S01 | instance call | `Corpus.Core!M:Corpus.Core.Methods.Direct(System.Int32)` | `Methods.Direct` |
| S02 | static call | `Corpus.Core!M:Corpus.Core.Methods.Static(System.Int32)` | `Methods.Static` |
| S03 | overload selection | `Corpus.Core!M:Corpus.Core.Methods.Add(System.Int32,System.Int32)` | `Add(int,int)` |
| S04 | generic method | `Corpus.Core!M:Corpus.Core.Methods.Identity``1(``0)` | canonical defining declaration, not the constructed `Identity<int>` |
| S05 | named/optional arguments | `Corpus.Core!M:Corpus.Core.Methods.Named(System.Int32,System.Int32,System.String)` | `Methods.Named` |
| S06 | extension method | `Corpus.Core!M:Corpus.Core.StringExtensions.Shout(System.String)` | unreduced static extension method |
| S21 | 3-arg overload | `Corpus.Core!M:Corpus.Core.Methods.Add(System.Int32,System.Int32,System.Int32)` | `Add(int,int,int)` |
| S22 | double overload | `Corpus.Core!M:Corpus.Core.Methods.Add(System.Double,System.Double)` | `Add(double,double)` |
| S23 | string overload | `Corpus.Core!M:Corpus.Core.Methods.Add(System.String,System.String)` | `Add(string,string)` |
| S24 | optional argument | `Corpus.Core!M:Corpus.Core.Methods.WithOptional(System.Int32,System.Int32)` | `WithOptional` |
| S20 | partial type member | `Corpus.Core!M:Corpus.Core.PartialType.Left` | member of a `partial` type |
| S25 | conditional API | `Corpus.Core!M:Corpus.Core.ConditionalApi.Value` | same doc id in both symbol variants; target identity differs |
| S26 | cross-project call | `Corpus.Leaf!M:Corpus.Leaf.Helper.Compute(System.Int32)` | target defined in `Leaf` |

## 2. Inheritance, dispatch, and interfaces (mandatory)

| ID | Operation | Expected resolution | Distinction tested |
|---|---|---|---|
| S08 | inherited member | `Corpus.Core!M:Corpus.Core.Animal.BaseOnly` | declaration is in the base type |
| S09 | virtual override via base ref | `Corpus.Core!M:Corpus.Core.Animal.Speak` | compile-time symbol is `Animal.Speak`; runtime dispatch to `Dog.Speak` is a separate claim, not recorded |
| S10 | interface member | `Corpus.Core!M:Corpus.Core.IGreeter.Greet(System.String)` | selected interface declaration |
| S11 | hidden member (`new`) | `Corpus.Core!M:Corpus.Core.Animal.Describe` | the base declaration selected by the static type |
| S12 | explicit interface impl. | `Corpus.Core!M:Corpus.Core.IGreeter.Greet(System.String)` | called through the interface type; `qualification` |

## 3. Other reference categories (deferred — informative)

These resolve to a real symbol but are **outside the initial S2.2 supported
`Invocation` fact category**. They are recorded so S2.2 coverage classification
(no confirmed fact) can be tested; they do not enlarge the authorized scope.

| ID | Operation | Expected resolution | Category |
|---|---|---|---|
| S07 | constructor invocation | `Corpus.Core!M:Corpus.Core.Methods.#ctor` | constructor |
| S13 | property read | `Corpus.Core!P:Corpus.Core.Methods.Prop` | property |
| S14 | field read | `Corpus.Core!F:Corpus.Core.Methods.Field` | field |
| S15 | event subscription | `Corpus.App!M:Corpus.App.Sites.OnChanged` | event (local handler) |
| S16 | method group | `Corpus.Core!M:Corpus.Core.Methods.Raise` | method group |
| S17 | delegate invocation | `System.Private.CoreLib!M:System.Func`1.Invoke` | delegate (external target) |
| S18 | local function | `Corpus.App!M:Corpus.App.Sites.Local(System.Int32)` | local function |
| S19 | type reference | `Corpus.Core!T:Corpus.Core.Methods` | type reference |

## 4. Cross-project and dependency cases

| ID | Case | Expected |
|---|---|---|
| S26 | `App` calls `Leaf.Helper.Compute` | resolved to `Corpus.Leaf`; defining unit is `Leaf/Leaf.csproj` |
| A05 | identical declared site forms, different defining compilation (`Solo` vs `Core`) | distinct `asmctx3` (see [identity mapping](identity-and-provenance-mapping.md)) |
| C01 | conditional public API (`symbols-core-changed`) | `Core` `compctx1` and `ConditionalApi.Value` target identity change; `App` source unchanged |
| T01 | transitive dependency change (`symbols-leaf-changed`) | `Core` `compctx1` and target identity unchanged; `compctx2` changes at `Leaf`, `Core`, `App` |

## 5. Negative and refusal cases

| ID | Case | Actual Roslyn result | Expected Andirit behaviour |
|---|---|---|---|
| N01 | ambiguous overloads (`Amb(1,1)`) | `Ambiguous` (overload resolution failure; candidates present) | no confirmed target; `Ambiguous` coverage/refusal |
| N02 | missing reference/type | `Unresolved` (`CS0103`-class) | no confirmed target; `Unresolved` |
| N03 | dynamic invocation | `Dynamic` (no static symbol; `IDynamicInvocationOperation`) | no confirmed target; unsupported/unresolved statically |
| E01 | external metadata target | `Resolved` → `Corpus.External!M:Corpus.External.Api.Value` | resolved symbol is **not** a publishable source-defined target; external refusal |
| I01 | inactive `#if` branch | node absent from active compilation; bytes retained | not an occurrence fact; `ConditionalInactive` coverage, never a confirmed fact |
| RM01 | `reference-missing` variant (`@S01`) | not resolved | unresolved refusal |
| CE01 | `Solo` complete scope | zero invocations | complete-empty `Produced`, distinct from unavailable |
| IN01 | declared scope omits a unit | — | `IncompleteAnalysis`; not a complete-empty negative |
| NEW-04 | two incompatible coverage scopes | — | must not be merged as authoritative (defect preserved, not closed) |

## 6. Volume cases

`/*@INV-01..32*/` are 32 supported invocations binding to `Methods`, `StringExtensions`, and `Leaf.Helper` members; `/*@MR-01..20*/` are 20 method-group references (`qualification`; not occurrence facts). Together with `Sites.cs` they meet the F12 volume minima. Their expected symbol keys are frozen in the oracle.

## 7. Requirement mapping

| Required scenario (commission / F12) | Cases |
|---|---|
| direct / static / overload / generic / named-optional / extension / constructor | S01–S07, S21–S24 |
| inherited / virtual / interface / hidden / explicit interface | S08–S12 |
| property / field / event / method group / delegate / local function / type / partial | S13–S20 |
| two-project resolution | S26, A05 |
| conditional public API | C01 (S25) |
| transitive dependency change | T01 (A02, A08) |
| ambiguity / unresolved / dynamic | N01–N03 |
| external verified / missing defining context | E01, E02 |
| inactive branches | I01 |
| complete-empty / incomplete / NEW-04 | CE01, IN01, NEW-04 |
| Option A identity/provenance | A01–A10 |
