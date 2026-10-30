# Acceptance contract proposal and CI-1-SCOPE-Q1 plan

These are independent proof obligations, not tests added to the product suite.
The source examples under fixtures/acceptance are planning inputs, not production
implementation. Expected semantic targets follow written C# declarations and
language binding rules, never the extractor's current output. The baseline
known collapse must FAIL future occurrence acceptance.

## Corpus assembly and expected locations

Run the top-level Repeated/References fixture independently of acceptance cases.
For acceptance, Cases.cs, Partial.One/Two.cs and Duplicate.One/Two.cs form one
source group; CrossProject/App+Lib form another. The intentionally invalid
Pick(null)/Missing() examples remain semantic negatives, not compilation-success
requirements. Do not mix edit variants or unlisted source groups into the build.
No test framework package or network is needed to inspect these examples.

[expected-sites.json](fixtures/expected-sites.json) records hand-selected lexemes,
physical line/column spans, intended source-signature target and expected outcome.
Coordinates are mechanically calculated from the manually selected source text,
independent of extractor output; target expectations are authored from source
semantics. The reviewer must verify both and reject wrong expectations.
Mapped repository paths are explicit; fixture-file paths are not graph identities.
This manifest is a proposed oracle, not independently reviewed ground truth yet.

## Controlled matrix

M = mandatory positive/lifecycle case for selected CI-1A.
N = mandatory negative/coverage behavior; feature category remains deferred.
No positive expectation for a deferred category is smuggled into V1.

| Case | Status | Expected positive/negative/unknown distinction |
| --- | --- | --- |
| C01 | M | Repeated.cs lines 5/6/7: exactly three Write invocation sites, one Worker.Execute owner; method-name refs do not double count |
| C02 | M | Cases.cs Hit(1) vs Hit("x"): distinct int/string method definitions and spans, never name-only merged |
| C03 | M | CrossProject/App call resolves to Lib definition; one occurrence with App unit ownership, Lib target; cross-file preserved |
| C04 | M | Echo<int>(1) -> Echo<T>; reduced Mark<string>() -> defining extension Mark<T>; one site per invocation, correct facets |
| C05 | N | Constructor/new()/this/base positive discovery deferred; report unsupported category, never claim zero constructor usage. Constructor bodies can own supported ordinary method calls |
| C06 | M | value.Work() on IWorker binds IWorker.Work only; no Worker/OverrideWorker runtime calls invented |
| C07 | M/N | Worker-typed virtual call binds Worker.Work; base.Work binds Worker.Work; no override-target substitution. Member override impact deferred |
| C08 | N | Target.Work method group is not a supported invocation; action() is delegate Invoke, not confirmed call to Target.Work. Method-group category remains visible/deferred |
| C09 | M/N | Target.Work() contributes one method invocation. typeof(Target)/nameof(Target.Work)/callee type token do not add independent invocations; non-invocation references deferred |
| C10 | N | Console.WriteLine is external with no indexed implementation: coverage records boundary, no fake local target/site confirmation. R1 adds the F01 collision variant: a local declaration sharing the documentation ID must not receive the external call |
| C11 | N | Pick(null) ambiguous string/Exception overload; Missing unresolved. Neither confirms either Pick. Count each attempted invocation's outcome; ambiguity must not disappear through current fall-through |
| C12 | M/N | Two Partial type declarations, one Work declaration, one cross-file Work() usage; do not duplicate usages for each type part. Partial-method unification separately deferred. R1 adds a documented/sealed partial input (F02); plain C12 does not cover it |
| C13 | M | Reordered file/extraction enumeration, equivalent retry, unchanged source context: same occurrence handles, order, payload and coverage; run receipt may differ |
| C14 | M | Replace Repeated.cs with AfterDelete bytes: two sites remain; old second occurrence and old-source-hash facts absent from latest publication; source hash means all old file occurrence IDs retire |
| C15 | M | Replace with AfterShift: three spans shift +1 line, new hash/IDs; no old-source facts or old locations survive latest publication |
| C16 | M | Limit occurrences to 2 for C01: deterministic first two sites, complete=false, reason maxOccurrences and known lower bound >=1 omitted; exercise work/proof/byte/unit/declaration budgets separately |
| C17 | M | Bind publication A, publish B during read/continuation attempt: PublicationChanged and no classified mixed output; cancelled work never returns complete partial data |
| C18 | M | Memory vs Lattice projection vs durable close/new-process reopen: same normalized result, contributor provenance, coverage and handles; native traversal legacy behavior unchanged |
| C19 | M | Duplicate.One/Two Target.Work(): identical-looking calls remain distinct by path/unit/hash/span; never dedup by text or owner display |
| C20 | N | Default CI1_EXTRA undefined: branch is inactive, not confirmed ordinary source coverage; with explicit define it yields the written call. Generator execution/conditional configurations outside selected context stay notEvaluated |

For C20, supplied generated .g.cs bytes can be indexed as ordinary sources;
running generators to discover absent output is not evaluated. Do not claim
all generated text is excluded or all build configurations covered.

Additional mandatory adversaries: nested F(F()) two spans; same-line repeated
calls; linked file in two project contexts, potentially different targets;
same canonical doc ID in different projects; explicit interface implementation
identity; shadowed same-name local function; inaccessible/error signature;
partial-method definition/implementation; malformed or conflicting facts;
proof truncation and multicontributor facts. Supported positives are only those
within accepted scope; all other cases need explicit refusal/coverage.

## Independent expected outcomes

A reviewer should first inspect raw bytes and declarations without running
Andirit. For each positive, identify the unique semantic target and owner; check
the expression span against the source and a separate Roslyn/compiler inspection.
For each negative, demonstrate why a single confirmed target is unavailable.
Then compare the independently signed manifest against proposed output.
No implementation-produced snapshot may be automatically blessed as expected.

A baseline regression pass does not establish future occurrence correctness.
The committed harness's separate walk shares the compiler and author; it reduces
circularity but does not replace independent qualification.

## Publication/storage regression requirements

Exercise successful project-unit replacement/deletion, obsolete project removal,
shared contributions, source-hash mismatch, dependency-context change, producer
version change, unchanged-index reuse and run retry idempotence.
Failed/rejected/cancelled staging must retain both old occurrences and old
source state; staged data must not leak. Concurrency ordering must reject
superseded completion. Confirm source freshness states and stale-source refusal
independently of publication movement.

Preserve old public calls/reference IDs and traversal cardinalities on fixed
inputs; enforce evidence predicates on navigation as before.
Qualify authentic historical durable files and schema-2 snapshots:
old queries still work; new query reports analysis unavailable until reindex.
New-format replay/export/import must retain occurrence facts and coverage,
integrity checks, bounds and exact receipts. Unsupported codecs fail clearly;
never silently clear or regenerate user history. Schema/version policy requires
owner approval before code. Close/reopen is mandatory; crash/power-loss remains
a separate unqualified operational topic unless explicitly tested.

## Independent CI-1-SCOPE-Q1 review

Reviewer must not be the packet author. Read the pinned source, accepted EC6D
decision and raw reports BEFORE accepting recommendations. Reviewer may reject
the packet without changing production source.

| Challenge | Required review evidence |
| --- | --- |
| 1 Accurate inventory | Spot-check all eight relations, identity normalization, candidate fall-through, affected-tests preset, snapshot/schema and filters against source |
| 2 Valid collapse | Rerun public-API fixture, independently inspect all three spans/targets, distinguish counters from unique edges and call/reference overlap |
| 3 Storage semantics | Challenge C vs versioned occurrence edges, identity churn, linked-context multiplicity, contributor merge/conflict and bounded lookup |
| 4 Consumer utility | Reconstruct scenarios from source, verify proposed output answers them without full-symbol scans or unsupported precision |
| 5 Honest frontiers | Reject inferred runtime dispatch, invented unknown-site positions, complete-empty from partial counters and unavailable signatures |
| 6 Compatibility/lifecycle | Challenge atomicity, failure/cancellation, freshness, durable replay/export/import, old data and legacy traversal preservation |
| 7 Independent oracle | Review expected-sites manifest from source; mutate target/overload/location/duplicate to prove bad answers fail |
| 8 Total utility economics | Check raw byte/timing accounting, indexing amortization and absence of invented candidate/token savings |
| 9 Small V1 | Require justification for each included facet; reject constructor/read-write/indirect scope creep |
| 10 No hidden architecture | Diff allowlist; no src/tests/schema/package edits or unapproved implementation concealed in harness |

Q1 deliverables: revision/tree binding, reviewed-file/command list, reproduction
logs, findings with severity and evidence, proposed corrections, explicit
disposition (supported / supported with qualifications / remediation required),
and a decision-by-decision owner checklist. Do not mark Q1 complete based on this
author's checks. Owner acceptance follows independently; implementation requires
a separate explicit authorization.

## R1 amendment (CI-1-SCOPE-R1)

**Oracle strength (F04).** The manifest check must validate full UTF-16 start
**and end** coordinates, intended semantic target, intended outcome and row
uniqueness — not only start line and column width. The producer loop accepted a
wrong target, wrong overload, duplicate row and wrong end line. Lexical manifest
validation is distinct from candidate-result comparison; the packet's "author
consistency checks; NOT independent qualification" label is retained, and no
absent candidate has passed any acceptance test.

**Source-byte provenance (F03).** Fixtures and measured inputs record encoding,
line endings, path, raw SHA-256 and Git blob identity separately, with the
checkout/filter policy. A clean `git hash-object` match does not prove byte
identity. Fixture bytes are not silently normalized and claimed equivalent to the
original source.

**New mandatory adversary inputs.** Add the single-indexed-local-versus-external
documentation-ID collision (C10 variant) and the documented/sealed partial type
(C12 variant); see
[`../ci1-scope-r1/acceptance-reconciliation.md`](../ci1-scope-r1/acceptance-reconciliation.md).

**Owner-context table (S05).** Freeze a supported lexical-owner table with
explicit refusal rules; constructor bodies may own ordinary calls; lambda
ownership is lexical; indexer/event/accessor/initializer contexts are not
qualified and need a fixture each.
