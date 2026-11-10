# ADR-CI1-01-CI1A-S2.5-WP-S25-3-P0-CONTRACT-OWNER-01 — P0 canonical-form contract resolution for S-01

**Commission:** `ANDIRIT-CI1A-S2.5-WP-S25-3-P0-CONTRACT-OWNER-01` (P0 of the WP-S25-3 gate sequence).
**Project:** Andirit. **Recorded:** 2026-11-10. **Canonical branch:** `main`.
**Owner decision:** **`S-01 CANONICAL-FORM CONTRACT ADOPTED WITH QUALIFICATIONS`** — the normative C# semantic canonical-form contract is **[S-01 canonical-form contract v1](../planning/adr-ci1-01-ci1a-s2-5/s01-canonical-form-contract.md)**, adopted exactly as written. **Contract adoption is not implementation qualification**: P1 (proof execution), P2 (independent qualification), and P3 (owner acceptance) remain separate, unauthorized gates.

> **Status: RECORDED — OWNER-AUTHORIZED (contract resolution only).**
>
> This decision resolves the Case-B finding of the WP-S25-3 entry assessment by formally
> adopting, as the bounded S-01 canonicalization contract, the deterministic derivation the
> production plugin already implements — after independent inspection verified that the
> adoption is consistent with the accepted architecture (producer-side derivation; the native
> store validates canonical shape only), unambiguous for all supported inputs, and precise
> enough for independent oracle construction. It authorizes **no** implementation, oracle
> construction, proof execution, or qualification.

This is an additive owner decision. It changes no production source, test, CI configuration, fixture, reviewer evidence, oracle or corpus, prior decision, or historical review artifact. The contract document is a **new, narrowly scoped, versioned** normative document; no historical text is rewritten.

## 1. Preflight and exact source revision

| Object | Value |
| --- | --- |
| Entry HEAD / tree | `542c5dd9a1117a9586c540ce32d8e3ddfd2b137c` / `2018015bf32045284f84d94aec403b4b7fd805a8` |
| Branch / worktrees | `main`; exactly one (`D:/projects/Andirit`); clean tracked/index; no stashes; no concurrent writer |
| Local vs remote | recorded `origin/main` = `443c1927…`; local `main` **77 ahead**; no fetch/push/remote contact; PR #6 untouched |
| Inspected sources | `src/Andirit.CSharp/CSharpCodeGraphPlugin.Session.cs` (occurrence publication + digest functions), `src/Andirit.CSharp/CSharpCodeGraphPlugin.Symbols.cs` (`Normalize`, `CanonicalSymbolKey`, `TypeIdentity`, `QualifiedName`), `src/Andirit.CSharp/VerifiedCSharpOccurrenceExtractor.cs`, `src/Andirit.Abstractions/CodeOccurrence.cs` (`Compute*Digest`, `ToParts`, `Hash`, `ComputeTargetId`), `tests/Andirit.LatticeDb.Tests/` configuration |

## 2. Authority matrix

| Authority | Role for P0 |
| --- | --- |
| [WP-S25-3 entry decision](adr-ci1-01-ci1a-s2-5-wp-s25-3-entry-owner-01.md) §4 | The Case-B finding this decision resolves; the adoption checklist |
| [Identity contract](../planning/adr-ci1-01-ci1a-s2-rem-01/identity-contract.md) | Normative `usage4`/`asmctx3` **encoding**; binds this contract's byte layer (unchanged) |
| [S-01 canonicalization disposition](../planning/adr-ci1-01-ci1a-s2-3/s01-canonicalization-disposition.md) §5/§7 | The S2.5 completion gate this contract enables; forbids reopening S2.2 |
| [S2.5 entry decision](adr-ci1-01-ci1a-s2-5-entry-owner-01.md) §8 | S-01 proof contract; proof/qualification separation |
| Stage-2 boundary §3.2/§4 + acceptance matrix §1–§2 | The integrated S-01 acceptance rows |
| `CodeOccurrenceIdentity` (source) | The public, independently verifiable encoding primitives |
| Owner architecture decisions (Candidate C; D3/D4/D5) | Layer separation: producer-side derivation, store-side validation |

## 3. Confirmed Case-B assessment (independent inspection)

| Element | Classification |
| --- | --- |
| Domain separation (`asmctx3-identifier` / `-declarations` / `-target`; `-context` for `asmctx3`) | **Normatively defined and implemented** |
| Length-prefix encoding (32-bit big-endian per part, UTF-8) | **Normatively defined and implemented** |
| SHA-256 derivation, lowercase-hex output | **Normatively defined and implemented** |
| Frozen known-answer vectors (R2) | **Present** (frozen R2 vectors + oracle) |
| Publicly verifiable primitives | **Present** (`CodeOccurrenceIdentity` public statics) |
| `identifierDigest` deterministic source-level producer | **Present** (`OccurrenceIdentifierDigest`: normalized definition's simple name) |
| `declarationDigest` deterministic source-level producer | **Present** (`OccurrenceDeclarationDigest`: doc-ID-first `CanonicalSymbolKey` of the normalized definition) |
| `shapeDigest` deterministic source-level producer | **Present** (`OccurrenceShapeDigest`: `"method|arity|FQ-param-types"`) |
| Canonical semantic forms | **Implemented but not normatively defined** — confirmed verbatim: the source states the forms are "this plugin's deterministic caller convention, not an independent canonicalization claim" |
| Native store consumes caller-provided digests without re-deriving C# semantics | **Confirmed** (L-C: canonical-shape validation + publication binding only) |
| Case-C implementation gap | **None found** — every supported category (ordinary/constructor/extension/generic/nested methods within the accepted invocation scope) is deterministically derivable today |

**Case B is confirmed, not Case C.** The prior assessment remains accurate against the current source.

## 4–7. Adopted specifications (identifier, declaration, shape, normalization)

The owner adopts, **as the normative S-01 canonicalization contract v1**, the specifications recorded in the [contract document](../planning/adr-ci1-01-ci1a-s2-5/s01-canonical-form-contract.md) §§2–9, which were independently derived from the inspected source and are hereby binding:

- **§3 Identifier form (v1):** `identifierForm(M) = N(M).Name` — the normalized definition's simple name, verbatim (case-sensitive, Unicode-verbatim; constructors carry the type name; operators carry metadata names; `Name` excludes generic arity). **Identifier identity is deliberately not global symbol identity**: sharing is legitimate and distinctness is composed by the declaration and shape forms.
- **§4 Declaration form (v1):** `declarationForm(M) = docID(N(M))` — the Roslyn documentation-comment ID (`M:Namespace.OwnerType.Method(global::System.String)`, distinguishing overloads by parameter types and generic arity by member-name arity encoding), with the deterministic fallback `"<SymbolKind>:<fully-qualified display>"` when Roslyn supplies no doc ID. No assembly/project/file identity participates.
- **§5 Shape form (v1):** `shapeForm(M) = "method|" + Arity + "|" + join(",", fully-qualified parameter-type displays)` — return type intentionally omitted (R2 design; C# forbids return-type-only overloads, so no in-compilation collision); `ref`/`out`/`in` modifiers, arrays, pointers, nullable annotations, tuples (as displayed), and generic arguments participate; custom modifiers do not.
- **§2 Normalization (v1):** `N(M) = M.ReducedFrom ?? M.OriginalDefinition` — constructed generics collapse to original definitions; reduced extensions collapse to the reduced-from static method; partial declarations merge; aliases are invisible (symbol-derived forms).
- **§6 Encoding (byte-exact, unchanged):** SHA-256 over domain + 4-byte big-endian length-prefixed UTF-8 parts; lowercase hex; the `asmctx3`/`usage4` assembly per the identity contract.

## 8. Collision and ambiguity assessment

- `|` cannot occur in a fully-qualified type display → the `method|`/`|` delimiters are unambiguous.
- The parameter join uses `,` **without escaping**, and type displays can contain commas (generic arguments, tuples): a cross-parameter-list collision would require a balanced type display to equal an unbalanced comma-split fragment — impossible for valid fully-qualified displays. **No collision exists for supported inputs.** The unescaped join is recorded as a v1 design property (any future form change must revisit it).
- Tuple element names (where Roslyn displays them) participate in the shape → tuples differing only in element names are distinct targets (adopted).
- Return-type-only differences: impossible within one compilation (C# rule) and intentionally excluded from identity by the R2 design (recorded).
- Custom modifiers: not represented (recorded exclusion).

## 9. Invariance/difference matrix (adopted as normative)

Contract §7 rows 1–15 are adopted as the normative expected-behavior table (`INVARIANT`/`DISTINCT`/refusal), derived from the adopted forms by static inspection of Roslyn symbol semantics: formatting/comments/trivia/relocation/qualification-style/aliases → **invariant** across all three forms; overload addition → identifier invariant, declaration+shape distinct; rename → identifier+declaration distinct, shape invariant; parameter-type/order change → identifier invariant, declaration+shape distinct; generic arity change → identifier invariant, declaration+shape distinct; generic substitution and reduced extensions → invariant to the definition/reduced-from; `ref`/`out` → declaration+shape distinct; same simple name under different owners → identifier shared, declaration distinct; unresolved bindings → refusal (no digest). **P1/P2 must evidence every row**; no expected output is invented — each follows from the adopted forms and Roslyn semantics.

## 10. Contract-versioning assessment

- Canonical-forms **v1** operates inside occurrence semantics **version 4** (`usage4`) and the frozen `asmctx3`/`compctx1` encoding (`TargetDomain = "asmctx3"`, `CompilationContractVersion = "compctx1"` — both constants in the source). **These existing versions are sufficient**: the forms are a refinement of what semantics-v4 already hashes, not a new format.
- **Compatibility:** any future form change alters persisted `usage4`/`asmctx3` identities → per D5 there is **no migration and no dual-write**; affected stores are rebuilt by re-ingestion and superseded data is refused. A forms change therefore requires a **new canonical-forms version plus an explicit owner decision**.
- **Recorded v1 risk:** the display-string forms depend on the accepted Roslyn/SDK generation's `SymbolDisplayFormat` semantics; a future SDK/language upgrade that changes a display string would change identities. Qualification results are bound to the accepted SDK/runtime generation.

## 11. Independent oracle specification (feasibility confirmed; executable oracle deferred to P1)

The adopted contract provides everything an independent implementation needs **without invoking the production derivation**: the exact form rules (§§3–5 of the contract), the byte-exact encoding (§6), the invariance/difference matrix (§7), the stability guarantees and limits (§8), and the versioning boundary (§9). The P1 oracle will: (a) hold a **literal expected-form table** per fixture case (e.g., for the frozen-corpus `Logger.Write(string)` target: identifier form `"Write"`, declaration form `"M:Corpus.Worker.Logger.Write(global::System.String)"`, shape form `"method|0|global::System.String"`); (b) compute expected digests with an independent implementation of contract §6, cross-checked against the frozen R2 vectors; (c) execute the invariance/difference pairs on real fixture sources; (d) verify digest equality at extraction, in persisted native rows, after durable reopen, and through the authoritative query; (e) treat any disagreement as blocking. **Feasibility: confirmed.** The full executable oracle is **not** constructed here, and no qualification runs.

## 12. Conflict and remediation register

| ID | Class | Statement | Disposition |
| --- | --- | --- | --- |
| C-1 … C-14 | **C1 — contract and implementation agree** | The adopted form rules (§§3–5) are exactly what the inspected production code implements (verified by direct source inspection; the encoding primitives verified byte-exact) | Ready for independent proof (P1/P2) |
| C-15 | **C2 — prior ambiguity, resolved by adoption** | "Canonical identifier-site/declaration-site forms" lacked a normative C#-level definition (the Case-B finding) | **Resolved** by this adoption (contract v1) |
| C-16 | **C2 → adopted caveat** | Explicit-interface implementations: doc-ID presence is Roslyn-dependent; doc-ID or deterministic fallback both distinguish the member | P1 must evidence the fixture case (both paths deterministic) |
| C-17 | **C4 — out of scope** | Tuples/element-name sensitivity, custom-modifier representation, return-type identity, operators/accessors as separate occurrence kinds | Recorded exclusions (contract §11); no S-01 expansion |
| — | **C3 — implementation contradicts adopted semantics** | **None identified** | — |

No C3 finding exists; no C2 finding remains open. The adoption conceals nothing: every adopted rule is the inspected implementation's rule, now normative and independently testable.

## 13. Formal P0 owner disposition

**`S-01 CANONICAL-FORM CONTRACT ADOPTED WITH QUALIFICATIONS`.**

1. Appropriate owner authority: this is the P0 decision the WP-S25-3 entry decision required (Case B route). 2. Exact supported semantic scope: C# method-invocation occurrence facts on the accepted extractor (contract §11 lists exclusions). 3. Unambiguous canonical-form definitions: contract §§3–5, with the collision analysis (§8 of the contract). 4. Defined normalization: contract §2. 5. Explicit invariance/difference requirements: contract §7. 6. Independent oracle feasibility: confirmed (§11 herein). 7. No mandatory semantic contradiction: the conflict register records zero C3 findings. 8. Explicit exclusions and residual risks: contract §11 + the recorded Roslyn-display risk. 9. Clear versioning: canonical-forms v1 within semantics v4; D5 re-ingestion on any future change.

**Qualifications (binding):** (a) Roslyn/SDK display-format dependence — qualification results are bound to the accepted SDK/runtime generation; (b) explicit-interface doc-ID behavior is execution-time-evidenced at P1; (c) the unescaped parameter join is a recorded v1 design property; (d) **contract adoption is not implementation qualification** — S-01 remains unsatisfied until P1/P2/P3 complete; (e) any future forms change requires a new canonical-forms version, an explicit owner decision, and re-ingestion per D5.

## 14. Residual qualifications

All prior qualifications are carried unchanged (F-01, F-02, Q1-Q1, Q1-Q2, S-01 **still unsatisfied**, Q2-Q01, Q2-Q02, O-Q2-01, Q-I01, Q-I08, same-harness limitation, Windows-x64/serialized single-writer, managed-fault limits, SQ-04 amended boundary, SQ-05 bounded scope, **F-G2-01 open**). S-01's status advances from "canonical semantics unadopted" to "canonical contract adopted — proof pending (P1) and independent qualification pending (P2)".

## 15. Proposed P1 execution boundary (explicitly NOT authorized)

**Proposed:** `ANDIRIT-CI1A-S2.5-WP-S25-3-P1-PROOF-01` — a bounded tests/probes/evidence commission implementing the independent oracle (contract §10), exercising the adopted canonical forms and the S1–S8 chain (§6 of the entry decision) on the frozen R2 corpus plus the adversarial canonicalization fixture, with digest-level assertions at every stage, negative/refusal cases, the regression floor (full solution 907/0/0; frozen oracle 185/185 byte-identical), and a self-excluding evidence manifest. P1 must **not** change the adopted contract; any necessary implementation change requires a separate remediation authorization (inserting a new qualification gate). **P2 (independent S-01 qualification) and P3 (owner acceptance) remain separate and unauthorized.**

## 16. Explicit successor authorization status

- **P0: recorded (this decision). P1: proposed, NOT authorized. P2: NOT authorized. P3: NOT authorized.**
- **S-01: not yet satisfied** (canonical contract adopted; integrated proof pending).
- **WP-S25-1/SQ-04/WP-S25-2/SQ-05: accepted/satisfied with qualifications as recorded. F-G2-01: OPEN.**
- **WP-S25-4: planned, NOT authorized. Stage-2 final integration: incomplete. Stage-2 owner acceptance: unauthorized.**
- **WP-S24-3: optional, deferred, unauthorized. Public Find Usages: not authorized. Stage 6: not authorized. Optimization: not authorized. Migration/dual-write: forbidden (D5).**
- No production change, test change, oracle implementation, proof execution, or qualification is authorized by this decision.

## 17. Repository discipline

Only this decision document, the new versioned contract document, and the additive WP-S25-3 status-note update to the S2.5 work-package plan are added, on canonical `main`, under the single-worktree policy. No production code, test, CI configuration, fixture, historical review, evidence, oracle/corpus, prior decision, or existing planning contract is modified. No remote operation, fetch, push, or additional worktree is performed. The decision commit/tree identities and final working-tree status are reported in the handoff.