# S-01 canonical-form contract (version 1) — normative C# semantic canonicalization for occurrence identity

**Status:** normative for S-01, adopted by [`adr-ci1-01-ci1a-s2-5-wp-s25-3-p0-contract-owner-01.md`](../../decisions/adr-ci1-01-ci1a-s2-5-wp-s25-3-p0-contract-owner-01.md) (P0).
**Version:** canonical-forms **v1**, within occurrence semantics **version 4** (`usage4`) and the frozen R2 `asmctx3`/`compctx1` encoding.
**Scope:** C# method-invocation occurrence facts produced by the accepted `CSharpCodeGraphPlugin` extraction path. Everything not listed as supported is out of scope for this contract.

This contract defines the **source-level semantic canonicalization** (the forms), the **canonical byte serialization** and **digest derivation** (the encoding), and the **layer separation** between them. It does not assign any source-level semantic responsibility to the native store: the store validates canonical *shape* (`asmctx3:sha256:<64 hex>`), binds persisted facts to their publication, and refuses non-canonical/unsupported persisted data (D5) — it never re-derives semantic forms.

---

## 1. Layer separation

| Layer | Responsibility | Owner |
| --- | --- | --- |
| L-A Source-level semantic canonicalization | Derive the canonical **identifier form**, **declaration form**, and **shape form** from the verified live `IMethodSymbol` of the defining compilation | The producer (`CSharpCodeGraphPlugin`), per this contract |
| L-B Canonical byte serialization + digest derivation | Domain-separated, length-prefixed SHA-256 of the forms; `asmctx3`/`usage4` assembly | `CodeOccurrenceIdentity` (`Andirit.Abstractions`) — public, independently verifiable |
| L-C Native persistence + authority validation | Canonical-shape validation, publication binding, refusal of non-canonical/unsupported persisted data | `LatticeCodeGraphStore` |

## 2. Normalization (applies to every form)

`N(symbol)` = `method.ReducedFrom ?? method.OriginalDefinition` (and `symbol.OriginalDefinition` for non-method symbols): constructed generic methods normalize to their **original definition**; reduced extension methods normalize to the **reduced-from static method**; every form is derived from the **definition-level** symbol. Partial declarations merge into the single Roslyn definition symbol. Aliases never appear (forms derive from symbols, not syntax). Overrides and explicit interface implementations use the implementing definition's own symbol (their distinct doc-IDs/fallbacks distinguish them per §4).

## 3. Identifier canonical form (v1)

`identifierForm(M) = N(M).Name` — the normalized definition's **simple name**, verbatim.

- Case-sensitive, Unicode-verbatim (UTF-8 encoded at the byte layer).
- Constructors: Roslyn sets `Name` to the containing type's name.
- Operators: Roslyn metadata names (`op_Addition`, `op_Implicit`, …).
- Generic methods: `Name` excludes arity (`M`, not ``M<T>``) — arity participates in the declaration/shape forms.
- Explicit interface implementations: the simple name (distinctness is carried by the declaration form).
- **Identifier identity is not globally unique symbol identity** by design: unrelated symbols may share an identifier form; distinctness is composed by the declaration and shape forms inside `asmctx3` (and `usage4`'s target document).

## 4. Declaration canonical form (v1)

`declarationForm(M) = docID(N(M))` where `docID` = `GetDocumentationCommentId()`:

- For symbols that have one: the Roslyn documentation-comment ID, e.g. `M:Namespace.OwnerType.MethodName(global::System.String)` — it encodes the containing-type identity, member kind/name, and (for methods) parameter types in doc-ID notation, so **overloads are distinguished by the declaration form alone**. Generic methods encode arity in the member name (`` M:Ns.Type.Method``1 ``).
- Fallback (no documentation ID): `"<SymbolKind>:<" + fully-qualified display + ">"` — precisely `$"{N(M).Kind}:{N(M).ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}"` (e.g., `Method:global::Ns.OwnerType.Method(...)`). Used for symbols Roslyn cannot give a documentation ID; whether an explicit-interface implementation receives a doc ID or the fallback is an execution-time fact that P1 must evidence for its fixture case (both are deterministic and distinguished by the containing-type identity in the display).
- No assembly, project, or file identity participates: the declaration form is stable across file relocation and equivalent project boundaries and derives entirely from source-declared names and types.
- Unresolved/error symbols: never reach digest derivation (the extraction refuses unbound defining contexts — no fact, no digest).

## 5. Shape canonical form (v1)

`shapeForm(M) = "method|" + N(M).Arity + "|" + join(",", [TypeIdentity(p.Type) for p in N(M).Parameters])` where `TypeIdentity(t) = t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedGlobalNamespaceFormat)`.

- Method-kind is the fixed literal `method` (the only supported occurrence kind); return type is **intentionally omitted** (R2 design; C# forbids return-type-only overloads, so no in-compilation collision exists).
- Parameter order, count, and arity participate. `ref`/`out`/`in` modifiers appear in the display strings and therefore in the shape. Arrays (`[]`), pointers (`*`), nullable annotations, tuples (with element names as Roslyn displays them), and generic type arguments appear as displayed. Custom modifiers are not represented.
- **Collision analysis:** `|` never occurs in a fully-qualified type display, so the `method|` prefix and arity delimiter are unambiguous. The parameter list is joined with `,` **without escaping**, and type displays can contain commas (inside balanced `<…>`/`(…)`): a collision between different parameter lists would require a balanced type display to equal an unbalanced comma-split fragment, which no valid fully-qualified display can. **No collision exists for supported inputs.** The unescaped join is recorded as a v1 design property.

## 6. Digest derivation and canonical byte encoding (normative, byte-exact)

For `Hash(domain, fields)` = SHA-256 over, for each field in order: a **4-byte big-endian** length followed by the field's **UTF-8** bytes (a null field encodes as length 0); output = 64 **lowercase** hex characters.

- `identifierDigest  = Hash("asmctx3-identifier", [identifierForm])`
- `declarationDigest = Hash("asmctx3-declarations", [declarationForm])`
- `shapeDigest       = Hash("asmctx3-target", [shapeForm])`
- `asmctx3 = "asmctx3:sha256:" + Hash("asmctx3-context", ["compctx1", compctx1, metadataIdentity, moduleName, identifierDigest, declarationDigest, shapeDigest])`
- `usage4 = "usage4" domain over [repositoryId, pluginId, "usage4-b2c-exp-r2-20261101", semanticSourceUnitId, path, sourceHash, startLine, startColumn, endLine, endColumn, targetDocumentId, asmctx3, "method-invocation"]` (per the identity contract).
- Every form must be non-null and non-empty (`ComputeIdentifierDigest`/`ComputeDeclarationDigest` throw otherwise); `asmctx3` construction requires an already-canonical `compctx1:sha256:<64 hex>`.

## 7. Normative invariance and difference matrix

`=` invariant (same canonical form) · `≠` distinct · `—` not applicable. Derivation basis: static inspection of the adopted forms (Roslyn symbol semantics); P1/P2 must evidence every row.

| # | Transformation | identifierForm | declarationForm | shapeForm | Digest relationship | Basis / P1 evidence needed |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | Whitespace / formatting | = | = | = | all digests = | symbol forms derive from bound symbols, not syntax |
| 2 | Comments / trivia | = | = | = | all digests = | same |
| 3 | Declaration relocation (file move) | = | = | = | all digests = | no file identity in any form |
| 4 | Namespace-qualification style in source | = | = | = | all digests = | symbol-resolved forms |
| 5 | Type-alias use (`using X = …`) | = | = | = | all digests = | alias resolves to the semantic type |
| 6 | Overload addition/change | = (existing target) | ≠ | ≠ | identifier = for shared simple name; declaration/shape ≠ | doc-ID parameter types; shape parameters |
| 7 | Method rename | ≠ | ≠ | = | identifier/declaration ≠ | `Name`; doc-ID member name |
| 8 | Parameter-type change | = | ≠ | ≠ | identifier =; declaration/shape ≠ | doc-ID parameter types; shape display |
| 9 | Parameter order change | = | ≠ | ≠ | identifier =; declaration/shape ≠ | doc-ID parameter order; shape order |
| 10 | Generic arity change (M → M<T>) | = | ≠ | ≠ | identifier = (Name excludes arity); declaration/shape ≠ | doc-ID arity encoding; shape arity |
| 11 | Generic substitution (call through constructed type) | = | = | = | all digests = to the definition | `Normalize` → OriginalDefinition |
| 12 | Reduced extension method | = | = | = | all digests = to the reduced-from static method | `Normalize` → ReducedFrom |
| 13 | `ref` vs `out` (vs value) | = | ≠ | ≠ | declaration/shape ≠ | doc-ID `@`/`%` modifiers; display modifiers |
| 14 | Same simple name, different owner | = | ≠ | ≠ (possibly =) | identifier shared; declaration distinguishes | doc-ID containing-type identity |
| 15 | Unresolved / error binding | — | — | — | no fact, no digest (refusal) | extraction refuses unbound defining contexts |

## 8. Stability guarantees (v1)

- **Within one compilation:** deterministic for a given bound definition.
- **Across recompilations of unchanged source:** stable (all inputs are source-derived; no wall-clock, no paths, no ordering dependence beyond the adopted single-part forms).
- **Across file relocation / equivalent formatting:** stable (rows 1–4).
- **Across supported project boundaries:** the declaration form is assembly-independent; the `compctx1`/`metadataIdentity`/`moduleName` components of `asmctx3` still bind the defining compilation — two projects declaring an identical member produce identical component forms but distinct `asmctx3` values.
- **Not guaranteed:** stability across Roslyn/language-version display-format changes (recorded risk, §10); across source renames/refactors (genuine semantic changes → distinct identities by design).

## 9. Versioning and compatibility

- Canonical-forms **v1** operates inside occurrence semantics **version 4** (`usage4`, frozen R2 encoding) and the frozen `asmctx3`/`compctx1` encoding (`CodeOccurrenceIdentity.TargetDomain`/`CompilationContractVersion`).
- Any future change to a form (§3–§5) changes persisted `usage4`/`asmctx3` identities. Per D5 there is **no migration and no dual-write**: affected stores are rebuilt by re-ingestion; superseded data is refused or re-ingested. A forms change therefore requires a new canonical-forms version and an explicit owner decision.
- The forms' display strings depend on the Roslyn `SymbolDisplayFormat` semantics of the accepted SDK/language version; a Roslyn upgrade that alters a display string would alter identities. This is a **recorded v1 risk**: qualification results are bound to the accepted SDK/runtime generation.

## 10. Independent oracle requirements (for P1/P2)

The oracle derives every expectation from this contract — never by invoking the production derivation:

1. **Literal form table:** per fixture case, the exact `identifierForm`/`declarationForm`/`shapeForm` strings as literal values.
2. **Exact encoded bytes:** the domain and part sequences (lengths + UTF-8 bytes) per §6.
3. **Expected SHA-256 digests:** computed independently (separate reference implementation or hand-verified vectors, cross-checked against the frozen R2 vectors).
4. **Invariance and difference pairs:** rows 1–15 of §7 executed on real fixture pairs.
5. **Negative cases:** unbound/external contexts → refusal, no digest.
6. **Chain evidence:** the digests must be equal at extraction, in the persisted native rows, and after durable reopen, and must serve identically through the authoritative query.
7. **Reviewer responsibilities:** re-derive the form table and digests independently; verify the invariance/difference pairs; verify the persisted/reopened/query identity equality; treat any reviewer/producer disagreement as blocking.

## 11. Exclusions (v1)

Everything outside C# method-invocation occurrence facts on the accepted extractor: properties, fields, events as occurrence targets; operators as separate occurrence kinds; delegates/local functions beyond accepted extractor support; non-C# languages; return-type-sensitive identity; custom modifiers; cross-language identity. Recorded as **out of scope**, not as gaps.
