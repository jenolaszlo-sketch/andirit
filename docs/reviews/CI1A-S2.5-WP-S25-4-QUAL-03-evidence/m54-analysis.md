# M5.4 normative and reachability assessment

Reviewed revision: `25bc6e3b4305c7ce088deb36cfb000138dc76648`, tree `2a1309d8e08f230e61a1cd1bf0f1b596179ea8b3`. Source locations below refer only to this immutable revision. This is a bounded Windows x64 assessment, not universal mathematical unreachability.

## Controlling requirement

The approved `docs/planning/adr-ci1-01-ci1a-s2-5/wp-s25-4-qualification-matrix.md:47` says M5.4 is **Unresolved refusal — Missing defining compilation/universe**, with **Defining compilation not in universe → refusal**, and requires per-case membership/counters, query status and persisted coverage after reopen. The source `adr-ci1-01-ci1a-impl-01/stage2-acceptance-matrix.md` §5.4 has the same missing-defining case. The execution plan's B3 row specifically requires missing-compilation assertions and exact refusal receipts. QUAL-02 and the current commission §§9–14 identify the required continuous `MissingDefiningCompilation` chain. Therefore A (specific outcome on a valid production path) remains the operative qualification requirement; ordinary unresolved calls do not discharge it.

Underlying mapping contract §3 requires unique live defining compilation, complete scope and current context before minting; a failure produces correct coverage and no successful empty. Its N1 says missing defining compilation refuses, without demanding that normal source discovery can manufacture that input. Coverage-and-availability §§1–3 distinguishes resolution, scope completeness and publication. S23 publication integration §§3–5 requires unsupported/ambiguous/unresolved/missing-defining refusal plus durable coverage. These support B/C-style safety guarantees (correct unresolved accounting and a defensive resolver refusal), but do not explicitly prove reachability. There is a tension between the exact integrated matrix receipt and these more general guarantees; it is not reviewer authority to replace the matrix with separate controls.

## Exhaustive direct production call-site inventory

A search of all `src` files finds precisely three calls to `ResolveDefining`:

| Call | Preconditions in the governed production chain | Role |
| --- | --- | --- |
| `src/Andirit.CSharp/VerifiedCSharpOccurrenceExtractor.cs:83` | Verified source unit; source tree matched; invocation has unique non-null IMethodSymbol; MethodKind Ordinary, ReducedExtension or ExplicitInterfaceImplementation | Only first-pass classification entry. Incomplete → unresolved; Ambiguous → ambiguous; UnsupportedExternalEvidence → external; other non-Complete → unresolved. No mint on refusal. |
| `src/Andirit.CSharp/VerifiedCSharpAnalysis.cs:166`, in MintTarget | Nonblank recorded context. Production reaches MintTarget through MintUsage only after the extractor's Complete resolution on the same immutable symbol/caller/universe | Revalidates association before target mint. No production call changes the universe between checks. Isolated callers can exercise defenses but do not prove extraction reachability. |
| `src/Andirit.CSharp/VerifiedCSharpAnalysis.cs:218`, in MintUsage | MintTarget already succeeded for the same immutable inputs | Reads defining entry for fact authority; cannot independently introduce a Missing result under those established inputs. |

The only production call to MintUsage is extractor line 97; the only call to MintTarget is MintUsage line 200. `RequireCompilation`, compilation construction and universe construction also throw an exception **named** MissingDefiningCompilation. They are earlier authority/verification defenses, not the resolver enum outcome, and cannot be substituted for M5.4's chain. `VerifiedCSharpOccurrenceAuthority.Bind` invokes RequireCompilation and not ResolveDefining.

## Exact resolver predicate

For a supplied non-null method target, `ResolveDefining` returns MissingDefiningCompilation iff:

1. `universe.IsComplete` is true;
2. no entry satisfies `BindsDefining(entry,target,caller)`;
3. the exact verified-caller PE association test fails: not (caller non-null, containing assembly non-null, `caller.GetMetadataReference(assembly)` is PortableExecutableReference, and caller is an exact universe entry).

`BindsDefining` matches either the exact live own assembly instance or a direct live CompilationReference to an entry whose assembly metadata identity equals the target containing assembly. A matching assembly name alone is insufficient. Two or more live matches give Ambiguous, not Missing. A recorded-context mismatch is evaluated only after one binding and yields StaleContext. Scope membership is enforced already at universe construction.

## Predicate and invariant table

| Required predicate / invariant | Source and how established | Authorized fixture can violate? | Consequence when false | Location |
| --- | --- | --- | --- | --- |
| Exact indexed source bytes and ordinary parse configuration | VerifiedCSharpSource snapshots and compilation source/tree matching | Supply missing/hash-mismatched input as refusal control | No verified compilation / incomplete universe | VerifiedCSharpCompilation.cs:99–129; CSharpCodeGraphPlugin.Compilation.cs:76–96 |
| Every live reference captured as exact verified dependency or PE image | Reference count, exact objects, dependency DAG and metadata capture checks | Real missing reference; cannot silently remove captured evidence from verified object | Construction refusal; fabricated universe inadmissible | VerifiedCSharpCompilation.cs:131–184 |
| Reference aliases empty, no embed interop, assembly images only | Verified reference-properties check | Aliased raw control is outside accepted production properties | UnsupportedReferenceProperties before extraction authority | VerifiedCSharpCompilation.cs:158–162 |
| Caller is an exact verified universe member | PrepareAnalysis creates entries from verified project results; extractor invoked for prepared.Verified | Unverified inputs supported as refusal controls, not trusted member substitution | No current authority / no ordinary authority envelope | CSharpCodeGraphPlugin.Compilation.cs:158–180; Session.cs:69–84; extractor:132–140 |
| Every source dependency belongs to the same universe | Universe.Create iterates dependencies and exact reference equality | Dropping dependency from a candidate universe is an isolated defense only | Constructor throws MissingDefiningCompilation, not resolver result | VerifiedCSharpAnalysis.cs:55–66 |
| Universe complete before Missing enum is possible | Refusals empty and no missing scope unit | Yes, absent declared project/source | ResolveDefining returns Incomplete first; coverage-only partial path may publish zero facts, no complete authority | VerifiedCSharpAnalysis.cs:41–43,98–107; Session.cs:35–45,402–431 |
| Unique usable method symbol exists | GetSymbolInfo(invocation).Symbol as IMethodSymbol, no candidate promotion | Yes, unresolved/ambiguous calls and missing direct reference | Extractor increments unresolved or ambiguous before resolver; candidate symbol is not trusted | VerifiedCSharpOccurrenceExtractor.cs:65–80 |
| Supported MethodKind | Ordinary/reduced extension/explicit interface check | Constructor/delegate/local-function sites | Unsupported coverage, no defining-resolution chain | VerifiedCSharpOccurrenceExtractor.cs:81,173–176 |
| No exact own/direct source match | BindsDefining checks own assembly object or direct live dependency metadata | Foreign isolated symbol can satisfy; transitive production acquisition is the critical open boundary | With one match → Complete; multiple → Ambiguous | VerifiedCSharpAnalysis.cs:109,224–244 |
| No verified caller PE association | R1 checks actual containing assembly → caller metadata reference | Foreign symbol/control or unsupported metadata environment; admissible transitive/retargeting cases need proof | Actual PE-backed invocation → UnsupportedExternalEvidence/External | VerifiedCSharpAnalysis.cs:110–123 |
| Refusal is counted without a fact | Extractor switch and skip | Tested real null-symbol and isolated resolver defenses | Non-Complete cannot reach ordinary mint | VerifiedCSharpOccurrenceExtractor.cs:84–95 |
| Ordinary publication has complete source authority | Extraction canRecord and publisher authority sink/envelope | Missing declared unit now produces only bounded Partial zero-fact coverage | No trusted fact from partial path | extractor:132–140; Session.cs:393–441 |
| Durable counters stay with accepted generation | Sink batch/replacement, PersistOccurrences, CoverageForRestore and AggregateCoverage | Conflict/stale/corrupt controls | Explicit refusal, conservative partial/unavailable or preserved accepted state | CodeGraphIngestionSink.cs; LatticeCodeGraphStore.cs:650–834; LatticeCodeGraphStore.OccurrenceAuthority.cs:15–107 |

The code establishes exact reference closure and caller membership. It does **not itself prove** the semantic lemma that every usable method symbol Roslyn can return for every governed input must have an own/direct-reference assembly recognized by these checks. In particular, transitive signature exposure, symbol retargeting and metadata forwarding/error recovery need an exhaustive compiler-level argument to claim B. Ordinary unsuccessful fixtures are insufficient. No aliases or arbitrary metadata injection were invented to evade production validation.

## Bounded experimental outcomes

Plan: [independent-plan.md](independent-plan.md), written before new fixture execution. Six production reference configurations (three families × absent/direct ancestor reference), plus existing metadata, partial-scope and isolated ambiguity/defense controls, remain within the 12-configuration limit. No repair/retry was used to turn a failed semantic hypothesis into a pass.

The subsequent archive-integrity correction re-executed the same six reference configurations against exact Git bytes; it introduced no new candidate family or hypothesis. Across both export profiles there were 12 transitive-family publication attempts and 12 paired reopens; the initial converted-profile receipts remain under attempts/checkout-profile. The top-level exact-byte receipts control qualification. Required F-S254-02/03, R1 and inherited defensive controls were also repeated for their own obligations and do not expand the M5.4 candidate search. Original failed predictions remain failed predictions in both runs.

| Family and receipt prefix | Symbol observation / resolver | Actual extraction, query, native and fresh reopen |
| --- | --- | --- |
| `probes/corrected/m54-transitive-*` | Two inherited calls have no symbol; positive Mark resolves Complete | Unresolved 2, External 0, one fact. Exact Missing sites 0. |
| `m54-direct-*` | Three symbols, each direct source binding Complete | Unresolved 0, External 0, three facts. |
| `m54-returned-*` | Both outer Inherited and inner Get are unavailable; Get has one candidate with OverloadResolutionFailure, not a trusted method | Unresolved 2, External 0, one Mark fact. The preregistered hypothesis expected Get to bind and was disproved. Original exit-1 prediction receipts retained. |
| `m54-interface-*` | Same failure to obtain both method symbols when return type is transitive interface | Same 2/0/1 persisted outcome; original exit-1 hypothesis receipts retained. |
| `m54-returneddirect-*`, `m54-interfacedirect-*` | Explicit ancestor reference restores all three symbols and Complete results | 0 unresolved, 0 external, three facts. |
| `b3-*` and `b3-mixed-*` | Actual captured PE method is UnsupportedExternalEvidence; source Mark binds Complete; mixed missing/overload controls fail before resolver | PE External 1, zero external facts; mixed adds one unresolved and one ambiguous through reopen. |
| `control-provenance-publish`, `control-incomplete-*`, `control-declared-*`, `review-refusal-*` | Missing direct dependency/refusal or incomplete universe blocks complete authority | Refusal / exact IncompleteAnalysis partial coverage / unrelated-refusal AnalysisUnavailable. Not resolver-Missing continuity. |
| `coverage-boundary-*`, `multiple-definitions-*` | Foreign target or absent caller establishes isolated Missing defense; constructed two-live binding gives Ambiguous | Explicitly isolated defensive controls. Their persistence controls are separate and earn no continuous production-Missing credit. |

Intermediate receipts include fixture bytes and hashes, diagnostics, every invocation's symbol and candidate reason, caller direct references, verified descriptors, resolver result when evaluated, extraction, target query, decoded caller coverage/facts and process IDs. Resolver evaluation in the observer uses the same immutable inputs; first-pass entry follows the audited extractor branch. It is not inserted into production. The complete-scope aggregate-membership discrepancy is separately recorded in membership-counterexample.json and does not establish a Missing resolver chain.

## Exactly one technical disposition

**D. REACHABILITY UNRESOLVED — ADDITIONAL EVIDENCE REQUIRED.**

No valid continuous production MissingDefiningCompilation receipt was found. No M5.4 production defect was demonstrated. The source invariants strongly constrain reachability but the exhaustive Roslyn assembly-association lemma is unestablished, so B would overclaim. M5.4 remains BLOCKED.

Smallest next investigation: pin this revision/toolchain; analyze Roslyn's symbol ownership/reference association for supported invocations with transitive signature exposure, retargeted source symbols and platform type forwarding. First obtain a source-backed proof or one concrete symbol that passes the production invocation gate but has neither live direct defining match nor recognized PE association. Only then add one authority-bound publication/reopen fixture. No open-ended fixture sweep or production change to manufacture reachability is recommended.

If the owner instead wants to clarify the intended guarantee, the smallest candidate wording is: require (1) isolated MissingDefiningCompilation defense with zero minted facts and (2) separate authority-bound unresolved accounting through publication/native reopen, explicitly stating whether continuity between those two controls is required. This is a proposal only; until an owner-approved clarification/amendment, the current A-style matrix requirement controls and is not passed.
