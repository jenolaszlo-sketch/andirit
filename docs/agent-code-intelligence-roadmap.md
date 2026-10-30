# Agent-oriented code intelligence and dependency roadmap

**Status:** Proposed future direction, recorded 2026-10-29; current status updated 2026-10-30. **Documentation only.**
No milestone below is authorized for implementation, release, or publication by
this document. Exact API names are working names, not accepted public contracts.

## Product outcome

Make Andirit the local-first, deterministic code index that coding agents want to
query **instead of repeatedly searching files, opening project manifests, reading
source, and issuing tool calls to reconstruct relationships**.

A good result is an evidence-backed answer to one developer question, with
relevant source locations and follow-up handles, rather than a raw dump of graph
nodes. Andirit provides facts, context, bounded dependencies and unknowns;
the coding agent chooses edits and validation. Do not put model reasoning,
refactoring policy or autonomous source mutation in Andirit core.

Two representative questions:

- "I may change this method. Who calls it, what else uses it, what could break,
  which tests should I run, and which relationships have not been analyzed?"
- "What uses NuGet package X at version Y? Which projects resolve it, why is it
  present, which APIs do our sources call, and what would a version change touch?"

Both must remain answerable without indexing third-party implementation source.

## Existing foundations, not new feature claims

At the recorded standalone Andirit baseline, the implemented foundation includes:

- C# Roslyn-derived, analysis-context-bounded `calls`, `references`, heritage
  and implementation edges, existing callers/callees/references/impact traversals,
  and provenance-aware variants. Existing graph edges do **not** guarantee every
  individual usage occurrence is separately retained.
- C# project references and bounded `PackageReference` syntax-derived `package`
  nodes, version/unevaluated condition metadata and `depends-on` relationships.
  These are **not evaluated NuGet restores** and do not claim exact transitive
  dependency versions or target-framework selection.
- A limited C# `exercised-by` relation from allowlisted test methods to resolved
  production targets; this is not exhaustive test-impact coverage.
- TypeScript/JavaScript structural declarations and `invokes-name` relationships,
  policy-scoped relative **module-file** resolution and positive-only export-name
  facts. There are no resolved TS/JS `calls` or `references` edges, and written
  heritage is not imported-symbol binding.
- Publication identities, deterministic bounded queries, provenance, source
  freshness checks, incremental indexing and two qualified storage providers.

EC-6D implementation and remediation are **accepted and closed with qualifications**
under [EC6D-OWNER-02](decisions/ec6d-owner-02-acceptance.md). That acceptance record
is authoritative for implemented binding behavior and retained qualifications.
The candidate designs below remain proposals; CI-1 scope is pending owner approval.

## Candidate capability sequence

These identifiers label **backlog candidates**, not approved scope or schedules.
Their ordering can change on real consumer evidence.

| Candidate | Agent question / outcome | Relationship to existing capability |
| --- | --- | --- |
| **CI-1: Find Usages and Change Impact (C#)** | "Where is this method used, which callers and references are affected, and why?" | Enrich existing Roslyn graph/query features with occurrence-level evidence, categorized direct/indirect impact and bounded explanation |
| **DI-1: NuGet Dependency Intelligence** | "Who declares/resolves package X version Y, directly or transitively, and through which path?" | Distinguish source-declared references from authoritative restore output |
| **DI-2: External .NET API Metadata** | "Which exact third-party APIs do our symbols use?" | Connect source references to public assembly metadata and package ownership **without requiring external source** |
| **CI-2: TS/JS Calls and References** | "Which actual symbol does this invocation/reference mean?" | Follow EC-6D with conservative, versioned binding contracts; do not promote name-only candidates |
| **RF-1: Refactoring Preflight and Verification** | "If I rename/move/delete/change this symbol, what needs inspection, and what changed after editing?" | Read-only preflight, before/after publication comparison, never an automatic proof of safety |
| **TI-1: Test Impact** | "What tests directly exercise or may cover this change?" | Expand on the narrow existing `exercised-by` evidence |
| **DI-3: npm / Workspace Dependency Intelligence** | "Why is this module installed, where is it imported and which export is used?" | Separate manifests, lockfile/workspace resolution and actual symbol binding |
| **DI-4: Dependency Upgrade Impact** | "Which local call sites rely on API surface that changes from X to Y?" | Requires DI-1/DI-2 and explicit old/new context; static compatibility != runtime behavioral compatibility |
| **DF-1: Bounded Data Flow / Value Origin** | "Where does this argument/value come from and where can it go?" | Start intraprocedural and explicitly bounded; no whole-program claim |

**Current order:** EC-6D implementation/qualification is accepted and closed; see
[EC6D-OWNER-02](decisions/ec6d-owner-02-acceptance.md). CI-1 and DI-1 remain
high-priority candidates requiring separate scope approval; DI-2 remains a
separate candidate. Nothing in this proposal changes EC-6D owner decisions.

## Agent query surface: answer-oriented, not graph-vocabulary-oriented

Working operations (names provisional):

| Operation | Minimum useful answer |
| --- | --- |
| `FindUsages` | Declaration plus classified call/read/write/reference/override locations where supported; exact evidence, duplicates and omissions |
| `GetCallHierarchy` | Incoming/outgoing methods with depth/budget, dispatch classification, witnesses and frontier |
| `GetChangeImpact` | Direct/indirect dependency paths, evidence quality, potential tests, missing coverage and declared change kind |
| `GetRefactoringPreflight` | Expected impacted declarations/files, symbol conflicts, required edits, unsafe/unknown cases; no edit execution |
| `CompareCodeGraph` | Versioned before/after declaration, signature and relationship changes, newly unresolved facts, and evidence |
| `GetTestImpact` | Proven direct test relations separately from candidate tests and unknown areas |
| `ExplainDependency` | Shortest (or bounded ordered) project/package inclusion paths with declared/resolved classification and origin |
| `FindPackageConsumers` | Effective version context, direct/transitive projects and local source symbols using external APIs |
| `GetUpgradeImpact` | Version-context comparison and potentially affected local API usages, compatibility limits |
| `GetRelevantContext` | Minimal, source-backed relevant declarations/paths plus drill-down identifiers, not full-repository text |

One request should usually answer one ordinary engineering question without a
long series of shell searches. Keep low-level traversal available for advanced
consumers, but do not force agents to reconstruct join semantics themselves.

Response envelope requirements:

- Exact repository, source revision, publication, analyzer/provider versions and
  query-contract identity; explicit freshness or stale state.
- Classification: **verified relationship**, **possible dependency**, **unknown
  / unsupported / unresolved**, and **not evaluated**. A verified dependency
  is **not** a verified breaking change.
- Exact source file and span **when known**, logical symbol identities, evidence
  chains, and stable drill-down handles. Unknown location must remain unknown.
- Coverage/analysis context, unsupported files, external boundaries, query
  budget, truncation, omitted frontier and diagnostic reasons.
- Small summaries and controlled pagination/continuation; avoid uncontrolled
  source dumps or giant graph payloads. When excerpts are offered, bind them
  to content hashes and reject mismatched source states.

## DI-1: declared versus effective NuGet dependency graphs

Preserve separate evidence categories rather than collapsing all version
strings into one "uses package X" claim:

1. **Declared inputs:** `PackageReference`, applicable
   `Directory.Packages.props` / central package versions, relevant imported
   properties, version ranges/expressions, conditions, and project references.
   Source syntax alone cannot establish an evaluated dependency.
2. **Evaluated restore:** consume trusted local `project.assets.json` and,
   where present, `packages.lock.json` as distinct restore/lock evidence.
   Derive exact resolved package versions and direct/transitive chains only
   for the recorded project, target framework, runtime identifier, restore
   inputs and asset selection context.
3. **Reference ownership:** connect compile/runtime assemblies, analyzers and
   other asset types to resolved package identity where a verified artifact
   mapping exists. Framework references and project references must not be
   misclassified as ordinary NuGet packages.
4. **Provenance and drift:** record manifest/restore hashes, relevant paths,
   restore context, freshness and conflicts. An absent or stale assets file,
   unevaluated conditional input or unsupported project format yields
   **unknown/not evaluated**, not a fabricated resolved version.

Example future question:

> For `SomeLibrary` version `3.2.0`, list projects with that effective
> version in each TFM/RID context, distinguish direct from transitive inclusion,
> explain every inclusion path, and show which of our methods call its APIs.

Support differences across multi-target projects, centrally managed versions,
version ranges, conditional references, and conflicting contexts without
selecting a single arbitrary "project version". Do not run NuGet restore,
download packages or access registries as a side effect of an ordinary query.

## DI-2: external API intelligence without third-party source indexing

Read local assembly **public metadata** only when needed. Candidate facts
include assembly identity, public type/member signatures, accessibility,
inheritance/interfaces, attributes and authoritative version identity. Do not
decompile or ingest third-party implementations by default.

Connect, with explicit evidence:

`our source symbol -> resolved member/type reference -> external assembly
-> resolved package asset -> package ID/version in its restore context`.

Preserve ambiguity when a referenced assembly cannot be uniquely mapped to a
NuGet package, a target framework or a binary artifact. Dynamic/reflection
usage, native P/Invoke boundaries, DI conventions and generated source can
escape static analysis; report those limits. A package may own multiple
assemblies and an assembly may be selected differently by TFM/RID.

Incrementally materialize external metadata on demand and cache by an
immutable artifact identity/hash. Package cache content is input data, not
permission to read arbitrary neighboring files or contact remote services.

## CI-1 / RF-1: practical refactoring safety

C# should be the first full-stack proving ground because resolved calls and
references already exist. Prioritize:

- **Find usages by category:** calls, references, read/write where provable,
  overrides, implementations, delegates/method groups and selected
  attribute/annotation uses. Retain **occurrence-level** locations; today some
  repeated references consolidate into one graph edge.
- **Change kind matters:** internal behavior change, parameter/signature change,
  rename, move, delete and contract change imply different likely effects.
  Classification must stay distinct from speculative "will break" predictions.
- **Blast-radius explanation:** bounded reverse paths, exact relationship
  witnesses, directly affected symbols, indirectly connected symbols and
  unresolved/external frontiers. Deterministic deduplication and honest
  truncation are required.
- **Preflight and verification:** accept a proposed edit description or
  selected source revision, return candidate edits and conflicts, then compare
  a separately indexed resulting publication. Never mutate source from the
  query API or equate an empty result with safe deletion.
- **Tests:** distinguish established `exercised-by` relations from additional
  test candidates. A smaller candidate test list does not prove full coverage.

Candidate workflow: **Locate -> Explain usages -> Preview impact -> Agent edits
-> Reindex affected units -> Compare before/after -> Suggest validation**.
Andirit supplies evidence; the agent performs edits and runs tests.

## CI-2 / DI-3: JavaScript and TypeScript extensions

EC-6D's interface/import binding work and independent qualification are accepted
and closed under [EC6D-OWNER-02](decisions/ec6d-owner-02-acceptance.md). Any
generalization to calls and references remains separately proposed. Start with tightly
bounded, provable named imports/exports and ordinary invocation forms, with
same-name, alias, shadowing, re-export and dynamic-call negatives. Resolved
module files alone are not evidence of a resolved target callable.

For package intelligence, distinguish `package.json` declarations from
lockfiles, workspace linking and actual module/asset resolution. Each package
manager and resolution policy must have separately verified semantics; do not
assume npm, pnpm, Yarn and bundlers choose identical exports or versions.
External source code is optional, not a requirement to answer package-level
ownership and inclusion questions.

## Performance, cost and correctness acceptance hypotheses

The product goal is fewer agent interactions and less context ingestion at a
controlled indexing cost, **not** merely fast graph lookup in isolation.

Measure at least:

- Cold index construction, repeated unchanged indexing, incremental edit
  processing and warm query p50/p95 latency under pinned environments.
- End-to-end agent task completion: shell searches/file opens/tool calls, total
  transferred bytes and context tokens, time-to-actionable answer, and accuracy.
- Correct direct callers/references/versions, false-confirmed relationships,
  false omissions within the declared supported boundary, and explicit
  unknowns/truncation. Use independently reviewed expected answers, not
  implementation-produced values as ground truth.
- Representative large multi-project, multi-target NuGet solutions; pinned
  website repository for TS/JS; small adversarial fixtures for ambiguity,
  aliases, restore/lock drift, stale publications, version conflicts and
  identical symbol names across assemblies.
- Local-only input handling and no automatic external package/source fetch.
  Compare operational cost and persistence size across incremental revisions.

**No fabricated speedup or precision target.** Establish real baseline
measurements before setting pass/fail thresholds. A single very fast query
does not prove total task savings if indexing or repeated tool round-trips are
more expensive.

## Architecture and authorization constraints

- Keep language plugins and dependency resolvers replaceable; do not force
  NuGet/MSBuild semantics into the language-neutral core graph.
- Preserve source/project/package/assembly/member identity distinctions.
  Do not bind a package version to a coincidentally present working-tree clone.
- Durable graph/protocol identities, historical Hetu storage labels and
  previously accepted provider versions require their own compatibility gates.
- Prefer additive, versioned query contracts; failure and cancellation cannot
  corrupt the last successful publication.
- All results are publication-bound and bounded. "No supported usage found"
  must never be presented as "the symbol is unused".
- Existing ETAs, phases, contracts and R16 remain unchanged. Each candidate
  requires a focused consumer scenario, approved scope, independent
  qualification, storage/lifecycle compatibility and separate release
  authorization before implementation or publication.

## Next planning trigger

With EC-6D accepted and closed under
[EC6D-OWNER-02](decisions/ec6d-owner-02-acceptance.md), review and separately
approve the first small **agent-useful** vertical slice using a real repository:

1. C# method usages/change-impact explanation (CI-1), or
2. NuGet declared-versus-resolved package provenance (DI-1).

Prefer the slice with the clearest reproducible consumer benefit. Capture one
representative before/after agent workflow and independently review its
answers, unknowns and measured overhead before expanding scope.
