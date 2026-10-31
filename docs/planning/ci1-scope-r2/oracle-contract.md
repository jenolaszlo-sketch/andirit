# Oracle contract — R2 semantic validation rules and limitations

This is the authoritative description of what
[`analysis/oracle/Program.cs`](analysis/oracle/Program.cs) validates, how it
derives evidence, and where its authority ends. It is planning-only validation.

## 1. Independence boundary

The oracle is a **pure Roslyn compiler oracle**. It:

1. does **not** reference the `Andirit` plugin or any production project;
2. does **not** read the output of any CI-1A implementation (none exists);
3. derives expected semantics from **fixture source + the recorded compilation/
   dependency context**, never by comparing one editable manifest field to
   another (for example, `expectedOutcome == storedOutcome` is never used).

The four distinct things are kept separate:

| # | Thing | Role |
| --- | --- | --- |
| 1 | Frozen expected manifest `docs/planning/ci1-scope-01/fixtures/expected-sites.json` | Subject under test; 32 rows, unmodified |
| 2 | Source/compiler-derived semantic evidence | Produced by this oracle from raw bytes + Roslyn |
| 3 | Verification of declared outcomes against that evidence | This oracle's verdict |
| 4 | Future candidate-result comparison | **Not qualified; no candidate exists** |

Step 4 is explicitly unqualified. Nothing here shows that any candidate passes.

## 2. Recognised classification vocabulary

The oracle derives one of these classes for every occurrence from compiler
evidence:

| Class | Meaning |
| --- | --- |
| `confirmed` | A uniquely resolved **eligible indexed local definition** with verified semantic identity. |
| `external` | A resolved target **outside** the eligible indexed definitions in the recorded context. |
| `ambiguous` | No unique eligible target: two or more candidate symbols, no bound symbol. |
| `unresolved` | Binding established no target and no candidates. |
| `unsupported` | The occurrence/category falls outside the accepted analysis boundary (constructors, method groups, `typeof`, `nameof`). |
| `notEvaluated` | The occurrence is inactive in the recorded configuration (C20 default). |

The declared-outcome vocabulary is **exhaustive and exact** (no prefix matching).
Every value below is recognised; any other string is rejected as
`OUTCOME_UNRECOGNIZED`:

| Declared string | Class | Subtype |
| --- | --- | --- |
| `confirmed` | `confirmed` | — |
| `confirmed-interface-static-target` | `confirmed` | — |
| `confirmed-virtual-static-target` | `confirmed` | — |
| `confirmed-base-static-target` | `confirmed` | — |
| `confirmed-cross-project` | `confirmed` | — |
| `confirmed-after-replacement` | `confirmed` | — |
| `inactive-default; confirmed only with CI1_EXTRA` | `confirmed` + default-inactive required | — |
| `external-no-confirmed-local-target` | `external` | — |
| `external-delegate-invoke-not-Target.Work` | `external` | — |
| `ambiguous-no-confirmation` | `ambiguous` | — |
| `unresolved-no-confirmation` | `unresolved` | — |
| `deferred-constructor` | `unsupported` | `constructor` |
| `deferred-method-group` | `unsupported` | `method-group` |
| `deferred-type-reference` | `unsupported` | `type-reference` |
| `deferred-nameof-reference` | `unsupported` | `nameof-reference` |

**Vocabulary note.** These exact strings are the ones already adopted by the
CI-1 planning contracts and the frozen manifest; no new declared vocabulary is
introduced. The class layer (`confirmed`/`external`/`ambiguous`/`unresolved`/
`unsupported`) matches the disjoint-outcome vocabulary already required by the
S04 amendment ("confirmed, unresolved, ambiguous, external, unsupported
owner/category, candidate-only, intrinsic/unsupported invocation"). The only
addition is the explicit R2 rank `unsupported` for the deferred categories that
the existing strings already describe.

## 3. Derivation rules (source/compiler evidence only)

For each manifest row:

1. **Coordinates.** Reconstruct the exact UTF-16 start/end span from the raw file
   text and require the substring to equal the declared lexeme. A coordinate
   failure is reported alone (`COORDINATE_LEXEME_MISMATCH`) and short-circuits;
   it never cascades into semantic noise.
2. **Uniqueness.** Reject a repeated `(fixture path, span)` (`DUPLICATE_OCCURRENCE`).
3. **Context.** Select the compilation from the recorded mapping
   (`Repeated.cs`→`repeated`, `AfterDelete`→`deleted`, `AfterShift`→`shifted`,
   `CrossProject`→`app`, `C20`→`enabled` with `CI1_EXTRA`, else `acceptance`).
   Compilation name, assembly identity, parse options, preprocessor symbols,
   source files and reference counts are recorded.
4. **Occurrence category.** From the syntax node kind: constructor initialiser/
   object creation → `constructor`; `typeof` → `type-reference`; `nameof` →
   `nameof-reference`; a method-group reference that is not the callee of an
   invocation → `method-group`; otherwise ordinary invocation.
5. **Binding.** `GetSymbolInfo` yields the target symbol and candidate symbols.
6. **Derived class.**
   - unsupported categories → `unsupported` with subtype;
   - ordinary invocation: bound `IMethodSymbol` → `confirmed` if eligible local,
     else `external`; no symbol with candidates → `ambiguous`; no symbol and no
     candidates → `unresolved`.
7. **Eligibility (the local/confirmed test).** A bound target is an **eligible
   indexed local definition** only when **both**:
   - its containing assembly identity equals the identity of a compilation built
     from source in the recorded context, **and**
   - its original definition has a source declaration location inside a fixture
     tree.
   A documentation ID or display name is never sufficient. This is what keeps
   the F01 `System.Console.WriteLine` collision external.
8. **Target identity.** The bound documentation ID is compared to the frozen
   target table entry for the declared signature; for eligible-local entries the
   bound target must also be eligible local; constructor/type-token entries must
   bind the corresponding symbol kind.
9. **Outcome agreement.** The declared string's class must equal the derived
   class; for `unsupported` the subtype must also match. The C20 row additionally
   requires the default context to be inactive.

## 4. Reason codes

| Code | Meaning |
| --- | --- |
| `COORDINATE_LEXEME_MISMATCH` | Span/lexeme inconsistent, invalid, or reversed. |
| `DUPLICATE_OCCURRENCE` | Same fixture path and span audited twice. |
| `SYNTAX_MISSING` | No syntax node matches the span. |
| `SYNTAX_CATEGORY_UNKNOWN` | Node kind outside the recognised category set. |
| `TARGET_UNRECOGNIZED` | Declared signature absent from the frozen target table. |
| `TARGET_IDENTITY_MISMATCH` | Bound documentation ID differs from the declared expectation. |
| `TARGET_ELIGIBILITY_MISMATCH` | Declared eligible-local but bound target is not eligible local. |
| `TARGET_KIND_MISMATCH` | Bound symbol kind differs (constructor/type token). |
| `OUTCOME_UNRECOGNIZED` | Declared outcome outside the exact vocabulary. |
| `OUTCOME_CLASS_MISMATCH` | Derived class differs from the declared class. |
| `OUTCOME_SUBTYPE_MISMATCH` | Unsupported subtype differs from declared subtype. |
| `DEFAULT_CONTEXT_ACTIVE` | A context-dependent site is active in the default context. |

## 5. Recorded provenance

`evidence/after/after-run.json` records, for independent re-review:

- the Roslyn assembly versions and language version;
- every fixture source input with raw **SHA-256** and byte count;
- every compilation context (assembly identity, parse options, defines, source
  files, reference counts);
- the frozen target table and the declared-outcome vocabulary;
- per-site evidence: category, bound documentation ID, bound containing assembly,
  bound source declarations, eligibility, candidates, derived/declared classes,
  and error codes;
- the adversarial collision fixture hashes and result.

## 6. Limitations

The following are **not** established and must not be read into a passing run:

- No candidate `FindUsagesV1` output is compared (step 4 unqualified).
- The oracle is a compiler **mechanism** oracle for the manifest, not a
  production resolver; it does not exercise linked files, producer-version
  changes or legacy aggregate behaviour.
- It uses the same Roslyn compiler family as the product; it is not a second,
  independent compiler.
- It does not qualify runtime dispatch, method-group invocation, constructor
  discovery, local-function targets or indirect impact.
- It does not fix P-DEF-01 or P-DEF-02; the F01 collision remains a production
  defect outside this task.
- Independent qualification of this oracle is the responsibility of Q3, not of
  R2. R2 claims only that the four Q2 failures are now rejected for the intended
  semantic reasons and that positive controls pass.
