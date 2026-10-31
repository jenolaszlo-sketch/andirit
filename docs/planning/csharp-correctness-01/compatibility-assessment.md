# Compatibility assessment — ANDIRIT-CSharp-CORRECTNESS-01

## Public APIs, persistence and query contracts

| Contract | Status |
| --- | --- |
| `ICodeGraphPlugin` / `ICodeGraphExtractionSession` / `ICodeGraphSink` surface | **Unchanged.** `CSharpPublicApiContractTests` passes; no public type, method or property added/removed. |
| Public FindUsages / occurrence APIs | None added (not authorized, not needed). |
| Graph schema (node kinds, edge kinds, property keys, identities) | **Unchanged.** All `CodeNodeId`/`CodeSymbolId`/`CodeDeclarationId`/`CodeEdgeId` construction rules are byte-identical. |
| Publication lifecycle / index-unit replacement | Unchanged (`Session.ExtractAsync` write path untouched apart from property materialization before the same write). |
| Caller/callee/reference/impact query paths | Full `Andirit.Tests` (312), `LatticeDb.Tests` (27), `TreeSitter.Tests` (157) pass unchanged. |

## Identity stability (proven, not asserted)

The `cross` fixture (genuine cross-project call + inheritance + implements +
import, no defects exercised) produces **byte-identical** extraction JSON
before and after the fix:

| Artifact | Baseline `5c89ac0` | Remediated |
| --- | --- | --- |
| `evidence/comparison/cross.baseline.json` | `bf8681cae584e32cca3b932525aaab6959478a3dd42edd73d3f2b17b69b0ab72` | `bf8681cae584e32cca3b932525aaab6959478a3dd42edd73d3f2b17b69b0ab72` (identical) |

In the `mixed` fixture, every edge that survives in both runs keeps its exact
`CodeEdgeId` (script-verified: *surviving-edge IDs stable: True*). No global
edge-ID rewrite occurred.

## Intentionally changed semantic results

### 1. Removed — false resolved edges (P-DEF-01)

`mixed` fixture: 45 edges → 42. All three removed edges are the defect:

| Removed edge | Source | Target |
| --- | --- | --- |
| `calls` | `App.App.Run()` (`src/Z/App.cs`) | `System.Console.WriteLine(string)` — actually the unrelated `A/Shadow.cs` declaration (`csharp:symbol:8102d2ac…`) |
| `references` | `App.App.Run()` | `System.Console` (shadow type node `csharp:symbol:3f3042d1…`) |
| `references` | `App.App.Run()` | shadow method node `csharp:symbol:8102d2ac…` |

Coverage delta (`calls`): `edgesEmitted 2→1`, `crossProjectTargets 2→1`,
`externalTargets 0→1`. These are accuracy corrections, not regressions.

### 2. Restored — previously suppressed valid edges (P-DEF-01 strictness flip side)

`ambiguous` fixture (same documentation ID in an unrelated project + genuine
dependency): 19 → 22 edges. Baseline refused the provable relationship
(`calls: e=0, unresolved=1, ambiguous=1`); the remediated resolver proves
dependency correspondence and emits:

| Added edge | Source | Target |
| --- | --- | --- |
| `calls` | `App.Program.Run()` | `Example.Helper.Go()` in `src/Lib` (`csharp:symbol:e186634b…`) |
| `references` | `App.Program.Run()` | `Example.Helper.Go()` (`src/Lib`) |
| `references` | `App.Program.Run()` | `Example.Helper` type (`src/Lib`) |

Coverage delta (`calls`): `edgesEmitted 0→1`, `crossProjectTargets 0→1`,
`external 0`, `ambiguous 1→0`.

### 3. Retargeted test expectation

`ExtractAsync_AmbiguousTargetsAreCountedNeverGuessed` →
`ExtractAsync_UnrelatedDuplicateDoesNotMakeAnExactDependencyAmbiguous`
(fixture unchanged; assertion now expects exact resolution). See
[`regression-matrix.md`](regression-matrix.md#retargeted-pre-existing-test).

### 4. Property payload changes (P-DEF-02)

For non-partial symbols the emitted properties are identical (documentation is
materialized with the same single value as before — the merge of a one-element
set is the value itself). For multi-part declarations of one logical symbol:

- `doc-summary`/`doc-remarks` on the logical node become the deterministic
  ordinal-sorted distinct merge (bounded 2048). Previously the input either
  crashed or (undocumented/identical cases) emitted the identical value, so
  only genuinely multi-valued documented partials change — and only from
  "extraction fails" to "deterministic bounded merge".
- A previously impossible output class (documented partial type) now extracts
  with one logical node and N physical declarations.

## Explicitly preserved incorrect-behaviour boundary

- No incorrect relationship is preserved for historical cardinality: the
  three false edges are removed, not reclassified or retained.
- No valid relationship was weakened: `cross` byte-identity plus rows 11–12 of
  the regression matrix demonstrate cross-project calls, references, imports,
  inheritance and implements all still resolve.
- Remaining unsupported/ambiguous contexts are unchanged in kind and now
  counted honestly (`ExternalTargets`, `AmbiguousTargets`, `UnresolvedTargets`,
  `UnsupportedTargets`).

## Not done (out of scope, unchanged)

CI-1A implementation; occurrence-level storage; query API changes; graph
schema migrations; NuGet evaluation; TS/JS changes; release/packaging.

## Post-review reconciliation (owner acceptance CSHARP-CORRECTNESS-OWNER-01)

Added after the independent `CSHARP-CORRECTNESS-Q1` review returned
`SUPPORTED WITH QUALIFICATIONS` and the owner accepted both corrections with
qualifications
([`docs/decisions/csharp-correctness-owner-01-acceptance.md`](../../decisions/csharp-correctness-owner-01-acceptance.md)).
This section is append-only reconciliation documentation; the sections above
are preserved unchanged and no implementation code was modified.

### 5. p18 — unowned-namespace import capture (independently discovered)

The independent reviewer discovered an additional corrected behavior of the
P-DEF-01 defect class that the intentional-changes list above had not
enumerated (probe `p18-local-system-namespace`; REPORT §B item 7, §I-2;
FINDINGS-MATRIX "additional independently discovered case"). It is recorded
here as required by the reviewer before publication.

- **Original (baseline `5c89ac0`) behavior:** a resolved cross-project
  `imports` edge was published from project B's `src/B/Program.cs` (file node
  `csharp:file:d302c984…`, evidence line 1: `using System;`) to the
  `namespace System` node (`csharp:symbol:ff9ec621…`) declared in **unrelated
  project A's** `src/A/Local.cs` (edge `csharp:1a5ac3d2…`, evidence quality
  `Resolved`). Project B does not declare `namespace System;` and does not
  reference project A. Probe evidence:
  `evidence/out/baseline/p18-local-system-namespace.json` in the reviewer
  workspace (11 edges).
- **Corrected behavior:** no edge is emitted. The import target cannot be
  proven to be project A's declaration and is counted conservatively:
  `imports` coverage in the probe changes from `edgesEmitted 1,
  crossProjectTargets 1, ambiguousTargets 0` (state `Produced`) to
  `edgesEmitted 0, unresolvedTargets 1, ambiguousTargets 1` (state `Partial`).
  The probe's edge count drops 11 → 10; the only removed edge is the false
  `imports` edge; the structural `contains`/`declares` edges touching the
  `System` node keep their exact identities.
- **Independently established target identity:** the baseline edge landed on
  the only indexed `namespace System` source declaration in the run —
  unrelated project A's node `csharp:symbol:ff9ec621…` (declared
  `src/A/Local.cs`), verified by declaration-path inspection in the baseline
  probe JSON.
- **Why the previous behavior was semantically wrong:** `using System;` in B
  binds to the merged `System` namespace symbol, which has no single owning
  assembly (`ContainingAssembly == null`). The baseline resolver treated the
  *absence of a competing indexed registration* as proof that project A's
  declaration was the import's target, asserting a cross-project dependency
  that no project reference supports. Uniqueness of registration is not
  identity — the same defect mechanism as the `calls` shadow in §1.
- **Relationship identity/cardinality/coverage impact:** exactly one false
  edge is removed; surviving edge identities are unchanged; no valid
  relationship is lost or reclassified. Coverage counts change only for this
  defect instance as listed above. No valid aggregate identity
  (`cross`-fixture class) is affected.
- **Why this is within P-DEF-01's intended exact-definition correction:** the
  correction is produced by the same fail-closed resolver gates (the
  unowned-namespace gate classifies any indexed contributor of a
  `ContainingAssembly == null` target as `Ambiguous`, never guessed). No new
  mechanism, no public-surface change; the output changes exactly by the
  removal of an unprovable false edge. The producer's assessment above
  under-reported this instance; this subsection completes the record. The
  general statement: **unowned-namespace capture by an unrelated project no
  longer confirms an `imports` (or any) relationship; it is counted
  `Ambiguous` when indexed contributors exist, `External` otherwise.**

### 6. Documentation merge truncation bound (exact rule and completeness limit)

Extends the bounded-output rule in
[`defect-02-analysis.md`](defect-02-analysis.md) and §4 above with the
reviewer-established specifics (PARTIAL-TYPE-AUDIT §3.3/§3.6; REPORT §I-3;
OWNER-DECISIONS item 3):

- **Limit:** merged `doc-summary`/`doc-remarks` values on the logical node are
  bounded to exactly **2048 characters** (`MaxMergedDocumentationLength`;
  `merged[..2048]`), inside the existing `MaxTextPropertyLength` = 32768 sink
  contract.
- **Deterministic ordering:** per-part values are accumulated into
  sorted structures keyed by node id and joined in ordinal order
  (`SortedDictionary`/`SortedSet`; join separator `"\n\n"`); the merge is
  commutative/associative over the per-part value set, so source enumeration
  order cannot change the output (verified byte-identical under reversed
  order, q04 pair).
- **Deduplication:** the joined set is distinct — identical documentation
  across parts is stored exactly once.
- **Truncation rule:** deterministic ordinal truncation. Ordinally earlier
  parts are kept intact; when the bound is reached the cut can fall inside a
  later part's text (q05: six 512-character summaries → merged length exactly
  2048, prefix `a×512`, tail `…dddd`). Pinned by
  `ExtractAsync_PartialDocumentationMergeIsBounded`.
- **No separate truncation indicator:** the truncated value carries no
  ellipsis, count or continuation marker; **a consumer cannot detect from the
  value alone that content was elided**. No meaning *within* a kept part is
  dropped — only whole-or-partial tails of ordinally later parts are cut —
  but the *fact* of truncation is invisible in-band.
- **Completeness limitation:** source documentation beyond the 2048-character
  bound is **not preserved**; no claim is made that all source documentation
  survives aggregation.
- **Future improvement, not a prerequisite:** explicit truncation metadata
  (marker, length or count property) would be a schema-affecting change and is
  recorded as a possible future improvement only. It was deliberately **not**
  introduced during acceptance recording, and its absence does not block this
  acceptance.

### 7. `calls` ambiguity after the resolver correction

The reviewer's finding on the retargeted pre-existing test
(`ExtractAsync_AmbiguousTargetsAreCountedNeverGuessed` →
`ExtractAsync_UnrelatedDuplicateDoesNotMakeAnExactDependencyAmbiguous`),
recorded per REGRESSION-AUDIT §3 and REPORT §I-5:

- **Practically unreachable genuine `calls` ambiguity:** under the corrected
  resolver, the exact-dependency gate ties on assembly *objects*, which are
  unique per compilation, and within one project canonical keys are unique —
  so in the qualified cases the former baseline `calls`-ambiguity scenario
  cannot recur. The only reachable `calls` ambiguity paths are registration
  conflicts and multi-candidate identity-fallback cases.
- **Formerly ambiguous, now provable:** in the retargeted test's fixture the
  caller (`Zpp`) references only `Lib1`; Roslyn binds `Example.Helper.Go()` to
  `Lib1`'s symbol, and `Lib2` (unrelated same-key registration) is not in the
  caller's compilation at all. Baseline counted this provable valid call
  ambiguous and suppressed it; the corrected resolver resolves it exactly, and
  the new assertion pins the exact dependency node id
  (`ScopedSymbolNodeId("src/Lib1/Lib1.csproj", "M:Example.Helper.Go")`),
  `Ambiguous=0`, `CrossProject=1` — strictly stronger than the old assertion.
- **Genuinely ambiguous cases that remain possible:** merged namespaces
  declared by several referenced assemblies (`imports` kind — reachable and
  tested by `ExtractAsync_MergedCrossProjectNamespaceImportStaysAmbiguous`,
  independently confirmed byte-identical to baseline by probe p08),
  conflicting same-project registrations, and multi-candidate fallback
  matches. All remain fail-closed and counted, never guessed.
- **Preserved legitimate intent of the modified test:** the fixture was not
  touched; the old assertion (`AmbiguousTargets > 0`, no calls edge) encoded
  the defect itself — the suppression of a provable valid relationship — and
  passed against baseline precisely because it asserted baseline behavior;
  the new form fails against baseline for the intended reason and passes
  against current. The old test's legitimate purpose ("genuinely ambiguous
  targets are counted, never guessed") remains covered by the merged-
  namespace `imports` test and probe p08. Tests were not weakened, and
  incorrect ambiguity is not resurrected merely to maintain earlier counter
  values; the absence of a `calls`-ambiguity fixture for the old literal
  scenario is an inherent consequence of the fix (that scenario was the
  defect), recorded here so it is not mistaken for a coverage regression.

### 8. Unreachable resolver fallback branch (deferred cleanup)

The independent reviewer identified one unreachable branch in the corrected
resolver (REPORT §I-4; RESOLVER-IDENTITY-AUDIT §9 item 1; OWNER-DECISIONS
item 4):

- **Exact location:** `src/Andirit.CSharp/CSharpCodeGraphPlugin.GraphBuilder.cs`,
  method `TryResolveTarget`, metadata-symbol identity fallback: the **second**
  `if (definitionCandidates.Length == 1)` check, immediately after the
  `targetPaths` branch (lines 722–729 at the reviewed HEAD `92fc99d`).
- **Why unreachable:** the identical condition is handled earlier (lines
  685–692, which return when exactly one candidate exists), and the
  `targetPaths` branch returns on every path; control can therefore reach
  line 722 only with two or more definition candidates, so the guard can
  never be true. The intended "two or more candidates with no source
  locations → `Ambiguous`" behavior is produced by the final
  `return TargetMatch.Ambiguous;` that follows.
- **Reviewer evidence and classification:** "Cosmetic; no behavior impact.
  Remediation: optional cleanup" (REPORT §I-4). No evidence exists that it
  changes current behavior or correctness.
- **Disposition:** **deferred cleanup.** No implementation code was modified
  in this acceptance task; any future removal requires a separately scoped
  code change with its own review. Recorded here so the dead branch is not
  mistaken for a live fail-closed path during future maintenance.
