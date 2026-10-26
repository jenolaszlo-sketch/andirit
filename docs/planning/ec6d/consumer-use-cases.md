# Consumer use-case assessment

This assessment uses the owner-accepted EC-6C candidate at
2c3090cba6d8a56102a5c47ceb026c5b5fea3234. Canonical main is older and does
not yet contain EC-6B/EC-6C. The hands-on TypeScript exercise and its
limitations are in [consumer-baseline.md](consumer-baseline.md).

## UC-1: Symbol discovery

**Consumer question:** Where is this symbol declared, exported, and implemented?

**Required graph facts:** A declaration identity and physical locations;
public export occurrences; implementer relationships that point to the
selected declaration; ambiguity and evidence.

**Existing support:** Exact qualified-name lookup and declaration lookup are
available. They preserve candidate ambiguity. C# Roslyn symbols can carry
semantic identity within the constructed compilation. EC-6C exposes
TypeScript/JavaScript export-name entries, with Structural source locations
and provider provenance.

**Missing capabilities:** Tree-sitter exports have no SymbolId or declaration
association. TypeScript heritage points to declaration-less placeholders.
An exact name can return declaration, export and placeholder nodes together.
Same-name declarations in different modules remain ambiguous; exported name
does not establish a bound declaration or implementation.

**Reliability risks:** A consumer that selects the first exact-name candidate
or joins only on Name can navigate to another same-name declaration or
mistake a placeholder for an interface. Export inventories are positive-only.

**Expected response shape:** Candidate set grouped by node kind, with physical
declarations, export occurrence, provider/version, source location, and an
explicit ambiguity state. Implementations must be reported only when a
relationship targets the selected declaration under a documented binding
rule.

**Evidence required for acceptance:** Fixture with a same-name negative
declaration, unique and ambiguous lookups, direct export, re-export and alias
cases; assert both the exact candidate set and source spans. No silent
first-result behavior.

## UC-2: Interface-change impact (primary candidate)

**Consumer question:** If I modify this interface, which implementations and
consumers may be affected, and why?

**Required graph facts:** The selected interface declaration; its direct
explicit implementers; imported-name and alias bindings across supported
modules; direct re-export provenance if supported; relevant type-use
locations; inherited implementations if traversal is claimed; unresolved or
ambiguous cases; evidence for every path.

**Existing support:** TypeScript syntax exposes structural implements/inherits
edges and file-level import relationships. EC-6B resolves some relative
module paths to files. EC-6C exposes supported export names. Generic
neighborhood queries can explicitly traverse selected edge kinds. These facts
can demonstrate that modules and syntax exist, but do not join the selected
interface to its actual implementers or users.

**Missing capabilities:** There is no TypeScript import-name extraction and
binding, no export-to-declaration association, no binding through re-exports,
and no TypeScript references/calls. The standard impact preset excludes
imports, exports and resolves-to. The project-rooted public-surface helper is
not useful for this Tree-sitter profile because it emits no Project root.
Inherited implementation propagation is also not semantically resolved.

**Reliability risks:** Name matching yields both false positives and missed
aliases. A missing edge is not evidence of no consumer. A changed interface
member can affect ordinary type uses that are not explicit implementations;
the current Tree-sitter graph cannot enumerate them.

**Expected response shape:** For a supported bounded profile, return:

1. Direct explicitly declared implementers with source location and the
   binding path to the selected declaration.
2. Direct importer modules and imported names, clearly labeled as possible
   consumers unless a use site is also proven.
3. Any supported type-use sites with their own evidence.
4. Unresolved, ambiguous, unsupported and truncated frontiers.
5. A coverage statement that distinguishes the supported candidate set from
   all possible runtime or compile-time effects.

**Evidence required for acceptance:** A manually reviewed corpus with a
same-name negative, imported alias, relative direct import, re-export,
missing module, ambiguous duplicate, inheritance and ordinary type-use
cases. Report precision and recall by each evaluated relationship class.
Require database reopen and repeat-index equivalence. Do not claim global
impact completeness.

## UC-3: Cross-module navigation

**Consumer question:** How does this symbol or dependency cross files and
modules?

**Required graph facts:** Import occurrences and bindings; file-level
resolution; export occurrence and origin; re-export chain; imported alias;
unresolved/external/unsupported classification.

**Existing support:** Written imports are structural. The relative-v1 resolver
can link selected ECMAScript relative import/re-export source paths to a file
in the same plugin inventory. EC-6C publishes supported export names and
occurrences. Diagnostics distinguish some unresolved, external and
unsupported sites.

**Missing capabilities:** resolved file is not resolved symbol. Import
bindings, namespace/star closure, re-export publication/origin tracing,
bare-package and configuration-alias resolution, and cross-plugin resolution
are absent. Some unresolved outcomes are aggregate counters rather than a
queryable per-import graph fact.

**Reliability risks:** A Resolved file edge can be overread as proof that an
imported name exists or binds. An absent edge means only that the active
policy did not verify that target.

**Expected response shape:** A sequence of module edges with each edge's
policy, rule, raw specifier (when retained), source location and status,
followed by a separately represented symbol binding only if that operation
is supported.

**Evidence required for acceptance:** Exact, extension, index, case mismatch,
root escape, missing file, bare package, alias, direct export, alias import,
star re-export, cycle and cross-plugin negatives. Assert that no file-level
resolution is reported as a symbol binding.

## UC-4: Structural change detection

**Consumer question:** What changed in structure between two repository states?

**Required graph facts:** Stable IDs under unchanged source, explicit added,
removed and modified fact sets, source revision binding, and handling for
renames, provider upgrades and relationship changes.

**Existing support:** IndexIdentity is deterministic over repository and
provider/source state; publications and snapshots can be exported with
integrity checks. Incremental indexing replaces affected units and removes
stale facts.

**Missing capabilities:** No graph-diff API or historical publication
retention is provided. Stable IDs alone do not explain semantic changes.
Plugin profile upgrades can intentionally invalidate/recreate facts.

**Reliability risks:** Consumers may compare snapshots themselves and mistake
ID churn due to profile or path changes for a source change. A store exposing
only its latest publication cannot reconstruct prior structure after it is
replaced.

**Expected response shape:** A future diff should bind both source revisions,
provider versions and canonical fact sets, then classify added, removed and
changed facts with old/new evidence. It must expose indeterminate comparisons.

**Evidence required for acceptance:** Known edit/add/delete/rename fixtures;
unchanged and provider-upgrade controls; deterministic reverse-enumeration
tests; compare results against manually derived fact changes. This planning
scope does not recommend implementing graph diff in EC-6D.

## UC-5: Explainability

**Consumer question:** Why does Hetu believe these two code elements are related?

**Required graph facts:** Source location, evidence quality, provider and
version, extraction or resolution rule, graph path, and relevant caveats.
The consumer must distinguish observation from inference and binding.

**Existing support:** Provenance-aware query envelopes bind results to one
publication and applied query. Nodes, declarations and edges expose
contributing unit, provider/version, location, evidence quality and
properties. Traversals report examined counts and truncation.

**Missing capabilities:** The API does not return source excerpts or a
consumer-ready causal explanation. A multi-hop result does not necessarily
name one source-to-target witness path. Tree-sitter placeholder relationships
may look similar to a binding without a second step in the explanation.

**Reliability risks:** Displaying only a relation name such as implements or
resolves-to hides its evidence level. A missing fact or truncated path can be
misread as a negative answer.

**Expected response shape:** Each displayed claim should include relationship,
source and target, location, evidence class, provider/profile, extraction or
resolution policy, path steps, truncation and unsupported-case notes.

**Evidence required for acceptance:** For each supported positive and
negative fixture, verify the exact path, source span, provider/version and
evidence classification. Independent reviewers must reproduce the reason
from source without undocumented interpretation.

## Consumer priority

The strongest demonstrated deficiency is UC-2: the graph contains promising
syntax and file facts, but the public query for an interface declaration
cannot recover even its explicit implementations. UC-1 and UC-3 supply the
necessary declaration/export/import binding prerequisites. UC-5 is a
cross-cutting acceptance condition. UC-4 is valuable but has no narrow
dependency on this impact slice and should remain deferred.
