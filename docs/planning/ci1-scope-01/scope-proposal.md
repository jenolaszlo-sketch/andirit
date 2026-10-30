# Proposed minimum scope

**Recommendation only: CI-1A invocation occurrences. No implementation approved.**

## Consumer contract

Given one exact local, indexed C# ordinary method definition (including static,
generic, extension, interface-member and virtual method definitions), return
physical declarations and distinct supported InvocationExpression sites bound
to that definition. Include lexical owner/context, exact source/hash/span,
per-site provenance, static dispatch classification, publication, coverage and
hard bounds. Group direct caller owners without losing sites.

"Local" means owned by this indexed repository, not C# local-function syntax.
Caller ownership may be a constructor/property/method/local context where
unambiguous; anonymous functions are lexical contexts, not proven runtime callers.
No source is edited. An empty list means no supported proven site in the reported
coverage, never "unused."

## Inclusion gate

A confirmed occurrence requires: exactly bound non-error target; selected
definition normalization; unique indexed identity in the relevant project
context; physical syntax span; source hash in the bound publication; unambiguous
indexed owner or explicit file context; producer semantics/version and proof.
Candidate-only binding, error-bearing target signature, missing context or
conflicting contributors cannot be promoted. Generic arguments are normalized
to definition; inferred type arguments are not advertised as retained evidence.

V1 covers InvocationExpression in indexed bodies/accessors/constructor bodies,
including expression bodies and nested lambdas with explicit lexical ownership.
Field/property initializers, top-level statements and unsupported owner contexts
must be counted as excluded/unsupported rather than silently lost.
No external implementation nodes are needed. Unit/category coverage summaries
must account for every scanned invocation, including unresolved/ambiguous/external/
unsupported outcomes; current coverage counters alone do not satisfy this gate.
These summaries must be persisted with the publication under an approved codec.

## Category decision matrix

"Current" describes aggregate graph representation, not retained occurrences.
Exact below describes the first retained syntax span only, unless proposed.
All included cases need distinct occurrence retention. Generic/extension/static/
interface/virtual are facets of one primary method-invocation category.

| Category | Current support | Target binding | Location today | Direct/derived | V1 | Required negatives |
| --- | --- | --- | --- | --- | --- | --- |
| Ordinary invocation | Already represented, partial owner/context coverage | GetSymbolInfo -> IMethodSymbol -> definition | Exact whole invocation, first per pair | Direct static binding | Include | Same names, nested calls, inaccessible/error targets, missing owner |
| Static invocation | Represented without usage classification | Bound static method | Exact first invocation | Direct | Include facet | Static/instance overloads; no class-name matching |
| Constructor invocation | Explicit/implicit new and initializers represented if explicit target node exists | Bound constructor | Exact first expression/initializer | Direct | Defer target/category | Synthesized constructors, this/base/new(), constructor vs method |
| Extension invocation | Represented via ReducedFrom | Reduced definition | Exact first expression | Direct | Include facet | Ordinary same-name method; generic reduction; unresolved extension |
| Generic invocation | Represented via OriginalDefinition | Definition identity, not constructed instance | Exact first expression | Direct | Include facet | Arity/overload constraints and invalid type arguments |
| Local-function invocation | Partial indexed fallback identity | IMethodSymbol with fallback key | Exact first expression | Direct but identity risk | Defer target/category | Same local names in different methods; recursion; shadowing |
| Method group/delegate reference | Some SimpleName references; no invocation taxonomy | Method symbol when unique; delegate Invoke often external | First token or unavailable | Direct reference when resolved, runtime delegate target unknown | Defer | Overloaded group, lambda/delegate assignment vs invocation |
| Interface-dispatched invocation | Interface member static binding may be retained | Bound interface method | Exact first expression | Direct static target; runtime targets unevaluated | Include static-target facet | Do not expand to implementation methods; explicit implementation |
| Virtual/override-related reference | Bound invocation partial; no member override edges | Bound method definition | Exact first expression or reference token | Static direct; runtime expansion would be derived | Include invocation facet only | Base/override selected separately; no fabricated derived-class target |
| Property read | Aggregate references only | Bound IPropertySymbol | First name token | Direct target, mode absent | Defer | Getter vs setter, compound assignment, ref returns/indexer |
| Property write | Same as read | IPropertySymbol | First token | Direct target, mode absent | Defer | ++, +=, init-only, object initializer |
| Field read | Aggregate references | IFieldSymbol | First token | Direct target, mode absent | Defer | Same-name locals, ref/out, readonly |
| Field write | Same as read | IFieldSymbol | First token | Direct target, mode absent | Defer | Compound writes, deconstruction, ref alias |
| Event usage | Target nodes absent | Event symbol lacks indexed node | No reliable retained location | Not established | Explicitly unsupported | +=/-= vs invocation; EventFieldDeclaration |
| Attribute reference | Partial reference to attribute type | Constructor -> containing attribute type | First whole attribute plus possible token ref | Direct type target | Defer | Attribute overload/member target vs type; suffix resolution |
| Type reference | Partial references | ITypeSymbol normalization | First name/predefined-type span | Direct | Defer | Alias/qualified segments, typeof/name token overlap, missing metadata |

No all-reference result is promised. Requests for deferred categories return
an explicit UnsupportedCategory status (with no classified usage output);
advertised supported categories remain discoverable. Default request asks only
method-invocation. Constructor/local/method-group omissions remain listed even
when the consumer did not request them.

## CI-1A versus adding CI-1B

| Delivery | Benefit | Additional proof/cost | Recommendation |
| --- | --- | --- | --- |
| CI-1A alone, with direct caller grouping | Recover every supported call location; reduce joins/source reconstruction | New occurrences, coverage and bounded target lookup; two-store/lifecycle parity | First implementation candidate |
| CI-1A plus narrow reverse-call impact depth <=2 | Explain possible indirect inspection candidates and paths | Separate work/path budgets, cycles, test-evidence classification, misleading-breakage risk | Design now, defer implementation |
| All references/dispatch/tests/dataflow | Broader apparent answer | Far larger correctness and coverage boundary | Exclude |

Grouping direct callers is a projection of returned occurrences, not a blast-radius
analysis. Groups report returned counts and whether totals are partial.
Candidate tests and indirect paths are NotEvaluated in CI-1A; existing APIs remain
usable independently with their existing qualifications.

## Read-only change semantics (future CI-1B contract)

| Change kind | Legitimate interpretation | Never established merely by graph reachability |
| --- | --- | --- |
| Internal body | Verified usages; direct/indirect inspection candidates, candidate behavioral tests | Callers need source edits |
| Signature | Sites likely worth compatibility inspection | A specific overload call fails without old/new signature and compiler proof |
| Rename | Proven references may require inspection | Reflection/generated/dynamic/external usages are covered |
| Delete | Proven usages are inspection candidates; outside-repo callers unknown | Repository or external consumers will compile |
| Accessibility | Inspect bound references plus context limits | Accessibility correctness (current model ignores accessibility) |
| Interface contract | Static interface usages and separately proven declaration relations | Runtime receiver closure or complete implementing-member impact |

Classification vocabulary: verifiedRelationship; potentiallyAffectedDependent
(derived reachability); candidateRequiringInspection (task framing);
unknownOrUnsupported; notEvaluated. No "safe", "unaffected" or "will break" claim
has an accepted proof definition. A change descriptor alone cannot prove breakage.

## Dependencies and boundaries

Approval dependencies: storage/identity/lifecycle ADRs; explicit coverage summary
semantics; query validation/budgets; independently reviewed fixtures; historical
compatibility route and comparison thresholds. Broader compilation-context fixes
are separate decisions, not hidden platform work. Existing real-source failures
may narrow qualification but may never be concealed.

Preserve language-neutral core and Roslyn-private extraction, logical vs physical
identity, explicit ownership, atomic publication and failed/cancelled retention,
historical compatibility, aggregate queries/legacy traversals, in-memory/LatticeDB
parity and ordinary local offline operation. No source modification, graph diff,
whole-program flow/dispatch, full test impact, MSBuild/NuGet evaluator, assembly
indexing, TS/JS call binding, MCP/LLM orchestration, package or release work.

## R1 amendment (CI-1-SCOPE-R1)

**Inclusion gate — exact eligible target identity (F01).** The requirement
"unique indexed identity in the relevant project context" (Inclusion gate above)
is strengthened. A confirmed occurrence requires an **exact eligible target
identity**: the bound symbol's defining compilation/assembly **and** its
definition must be proven to correspond to the indexed declaration in the
applicable dependency context. A canonical Roslyn documentation ID is a lookup
aid only, never proof of correspondence. Where correspondence cannot be proven,
the site is conservatively refused (unresolved/ambiguous) or qualified external;
it is never promoted to a confirmed local usage. Existing aggregate edge/ID
semantics are unchanged.

**Coverage is publication-owned (S04).** Category/unit coverage summaries must be
persisted disjoint outcomes (confirmed, unresolved, ambiguous, external,
unsupported owner/category, candidate-only, intrinsic/unsupported invocation),
not reconstructed from existing `calls`/`EdgesEmitted` counters. No
`SymbolUnused` or complete-usage claim.

**Documented partial types (F02).** The scope's partial-type support excludes
documented partial types until the separate prerequisite
[P-DEF-02](../ci1-scope-r1/prerequisite-defects.md) is authorized; those inputs
must report unavailable/failure state, never a complete-empty result.
