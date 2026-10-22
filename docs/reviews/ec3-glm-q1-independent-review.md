# EC3-GLM-Q1 — Independent review of EC-3 (class identity and member extraction)

Reviewer: GLM (independent of the implementing agent)
Review date: 2026-10-22
Review type: adversarial implementation and contract verification
Implementation under review: `e4598ac`
Baseline: `92d66c0`

---

## A. Git binding

| Item | Value |
|---|---|
| Repository | `jenolaszlo-sketch/penghou-hetu` |
| Canonical repository | `C:\Users\Laszlos\source\repos\Penghou.Hetu` (`main`, clean, exactly at `e4598ac`; untouched) |
| Review worktree | `C:\Users\Laszlos\source\repos\Penghou.Hetu.review-ec3` (detached `HEAD`) |
| Reviewed commit | `e4598ac9b517654b8346cdcaf96c594fc50bc53b` (tree `7158725553a4f45170ce5f90b39b575f4d6ed56a`) |
| Baseline | `92d66c0777a07801c45851aae7bb59d453781ee9` (tree `90bf3dcd7a3b905a275bb9ebc554274d2370d54d`) |
| EC-3A | `c0b70082d3664ceb47251eeef9cb9642c5210bcd` (tree `c1755068cfc1f4b78e10baf3077503efbac32e42`), parent `92d66c0` |
| EC-3B | `29c0e45967acc3943cda7b8f215f4971da18a336` (tree `63203826e4f9255cf0633ed61f76d6ab68c5cb15`), parent `c0b7008` |
| EC-3C | `e4598ac` (tree `7158725`), parent `29c0e45` |

Ancestry verified linear: `92d66c0 → c0b7008 → 29c0e45 → e4598ac`. The specified roles match the commit subjects and diffs. No discrepancy to report.

Review-worktree-only additions (uncommitted during review, preserved later on the review branch): four review test files and one marked test-project reference (see G). No production source file was modified.

## B. Review scope

Inspected (source, pre-test):

- `src/Penghou.Hetu.TreeSitter/EcmaScriptExtractionProfile.cs` (full read; EC-3A/3B logic)
- `src/Penghou.Hetu.TreeSitter/TreeSitterTypeScriptPlugin.cs`, `TreeSitterJavaScriptPlugin.cs`, `TreeSitterExtractionSession.cs`, `TreeSitterGrammarRegistry.cs`, `TreeSitterSyntax.cs`
- `src/Penghou.Hetu/CodeIndexingLifecycle.cs`, `CodeIndexPlanner.cs`, `CodeGraphBatchValidator.cs`, `CodeGraphIngestionSink.cs`, `InMemoryCodeGraphStore.cs` (unit replacement), `ICodeGraphStore.cs`
- `src/Penghou.Hetu.Abstractions/`: `CodeGraphFacts.cs`, `CodeGraphKinds.cs`, `CodeIdentities.cs`, `CodeGraphBatch.cs`, `CodeGraphExtraction.cs`, `CodeGraphCapabilities.cs`, `CodeGraphValidation.cs`
- `docs/ecmascript-symbol-identity.md`, `docs/language-feature-matrix.md` (diff 92d66c0..e4598ac)
- Per-commit diffs `c0b7008`, `29c0e45`, `e4598ac`

Executed: 23 review-only probes (12 adversarial extraction, 5 lifecycle/publication, 5 final edge probes, 1 fact dump), full solution suite, store conformance indirectly via solution suite.

Out of scope: C# and Elixir plugins beyond non-regression (capability flags, coverage), LatticeDb internals beyond the exercised store contract, cross-platform determinism (Windows-only execution).

## C. Findings

Severity-ranked. Dispositions per the review specification.

### EC3-GLM-F01 — Heritage target converges with a same-file top-level class even under lexical shadowing

- **Severity:** P2
- **Disposition:** DESIGN AMBIGUITY
- **Violated invariant:** "A name-based placeholder … must not be represented to consumers as an independently resolved declaration" (review spec §5); tension with the identity doc's own convergence policy.
- **Location:** `src/Penghou.Hetu.TreeSitter/EcmaScriptExtractionProfile.cs:440-445` (`StructuralTypeNode` hashes `"class\n{path}\n{name}"` with no lexical scope) against `TryEmitNamed` identity (`:510-513`, `fileScoped` adds path but keeps the lexical qualified name — top-level names therefore collide with written heritage names).
- **Commit:** `e4598ac` (introduced by `c0b7008`).
- **Counterexample (executed, CE1):**

  ```typescript
  // src/heritage-shadow.ts
  class Base {}

  function create() {
      class Base {}
      class Derived extends Base {}
      return Derived;
  }
  ```

- **Expected:** the `inherits` edge either stays attached to a declaration-less placeholder, or converges with the declaration that lexically governs the written name (`create.Base`); it must not present an unrelated declaration as the inheritance target.
- **Observed:** `inherits` (Structural) from `create.Derived` targets the **outer top-level `Base`** node — a symbol-bearing node with its own physical declaration. The inner `create.Base` exists unconnected. Runtime binding resolves to the inner class; the graph makes the outer class look like an inherited base, so impact queries from the outer `Base` will report `Derived` as a dependent.
- **Evidence quality implications:** edge evidence remains `Structural` and the docs disclaim semantic resolution (`docs/ecmascript-symbol-identity.md:133-147`: "a converged target is same-file name convergence, not semantic resolution"), so the *fact* is honest; the *representation* (converged, symbol-bearing target) invites a resolved reading, and the shadowing consequence is neither documented nor tested.
- **Existing coverage:** `ExtractAsync_HeritageTargetConvergesWithSameFileClassDeclaration` covers only the non-shadowed same-file case; `ExtractAsync_HeritagePlaceholdersStayFileScoped` covers cross-file separation. No shadowing fixture exists.
- **Remediation direction (policy decision, not implemented here):** either (a) mark name-converged heritage targets with an explicit property (e.g. `heritage.match = "name"`) so consumers can downgrade them; (b) keep heritage targets always declaration-less, as `implements` already does; or (c) record the shadowing consequence explicitly in the identity doc and feature matrix and accept it as a limitation. Option (b) matches the conservative treatment already applied to interface targets.
- **Remaining uncertainty:** none about the mechanism (executed and reproduced). Consumer severity depends on how impact queries weigh `inherits` edges, which is outside this review's execution scope.

### EC3-GLM-F02 — Static-initializer-block invocations are attributed to the class node

- **Severity:** P3
- **Disposition:** DOCUMENTED LIMITATION (undocumented behavior, consistent with the lexical-containment contract)
- **Location:** `EcmaScriptExtractionProfile.cs:226-230` (`call_expression` → `EmitCall` with `scope.OwnerNode` = class node; class bodies create `ScopeOwner.Class` scopes whose owner is the class node).
- **Executed (CE4):** `class Weird { static { boot(); } }` emits `invokes-name` sourced by the `type` node `Weird`. No fact claims the class executes `boot`; the edge records lexical containment, matching the pre-existing pattern of file-node-sourced invocations for top-level calls. Neither `ecmascript-symbol-identity.md` nor the feature matrix mentions static blocks.
- **Remediation direction:** document static-block invocation ownership (or attribute to a dedicated node in a later EC). Not blocking.

### EC3-GLM-F03 — Decorator invocations are attributed to the decorated member's node

- **Severity:** P3
- **Disposition:** DOCUMENTED LIMITATION (undocumented, consistent with lexical containment)
- **Location:** `EcmaScriptExtractionProfile.cs:580-607` (`HandleClassField` walks children, including `decorator` subtrees, under the field's scope).
- **Executed (CE9):** `@log() value = compute();` produces two `invokes-name` edges (`log`, `compute`) both sourced by the `property` node `Decorated.value`. The decorator call is lexically inside the field's declaration span; evidence stays Structural. The feature matrix records TS decorators as unmapped (N:NA), which understates this emitted invocation evidence.
- **Remediation direction:** reconcile the feature-matrix decorator row with the observed invocation attribution, or exclude decorator subtrees from member initializer attribution. Not blocking.

### EC3-GLM-F04 — Duplicate same-name members in one class converge to one symbol with one declaration per physical occurrence

- **Severity:** P3
- **Disposition:** DOCUMENTED LIMITATION (member-level case not recorded; class-level analog is)
- **Location:** `EcmaScriptExtractionProfile.cs:682-704` (`EmitClassMember` hash has no occurrence component).
- **Executed (CE3):** `class Dup { x = 1; x = 2; }` yields one `property` symbol with two declarations and two `contains` edges. Nothing is lost physically; the doc records duplicate *class* declarations (`ecmascript-symbol-identity.md:127-130`) but not duplicate *members*.
- **Remediation direction:** add the member case to the documented limitation list. Not blocking.

### EC3-GLM-F05 — Class-expression members keep name-based identity and converge with same-named top-level functions

- **Severity:** P3
- **Disposition:** DOCUMENTED LIMITATION (explicitly recorded)
- **Location:** `EcmaScriptExtractionProfile.cs:186-202, 219-221`; `ecmascript-symbol-identity.md:149-158`.
- **Executed (CE7):** `const A = class { start() {} }; const B = class { start() {} }; function start() {}` converge to one `callable` symbol with three declarations. Honest per the "explicitly out of scope" section; verified rather than assumed. No action for EC-3.

### EC3-GLM-F06 — Cross-file heritage never links declarations

- **Severity:** P3
- **Disposition:** DOCUMENTED LIMITATION (recorded and fixture-tested)
- **Location:** `EcmaScriptExtractionProfile.cs:440-445`; identity doc `:133-147`.
- **Executed (CE6):** `class Derived extends Base {}` in `derived.ts` produces a declaration-less, file-scoped placeholder distinct from `Base` in `base.ts`. Consumers must not expect cross-file impact through ECMAScript `extends`. Recorded in the identity doc; no action for EC-3.

### EC3-GLM-F07 — Class-member partial-recovery guard is weaker than the interface-member guard

- **Severity:** P3
- **Disposition:** NO ISSUE FOUND (consistency observation)
- **Location:** `HandleClassMethod`/`HandleClassField` check only `node.HasError` (`:548, :585`); `EmitInterfaceMembers` additionally checks the next sibling for error/missing nodes (`:779-781`).
- Source-inspection only (no counterexample constructed): an error-free member adjacent to an error sibling still emits facts in classes but not interfaces. `HasError` covers descendants (`TreeSitterSyntax.cs`), so fragments are not emitted from broken members themselves. Behavior is defensible for classes; the asymmetry is undocumented. Minor.

### EC3-GLM-F08 — Symbol identity separation (Area A)

- **Severity:** —
- **Disposition:** NO ISSUE FOUND
- **Verification (executed):** CE2 (three same-named `Widget` classes across two files and a nested scope: distinct node ids, symbol ids, declarations; containment never joins owners), CE10/CE12 (cross-file static fields distinct; TSX variant consistent), CE19 (overloads converge to one callable with three exact declarations at lines 2/3/4), CE20 (getter/setter pair converges to one accessor symbol; `backing` field distinct), CE18 (abstract method signatures emit no facts, per doc), CE11 (unicode member extracted; >256-char identifiers dropped by `BoundedText`), CE21 (`declare`/optional/`protected` recorded, nothing fabricated).
- Structural vs semantic identity: the extractor consistently refrains from claiming semantic binding (implements targets stay declaration-less even same-file; heritage placeholders carry no symbol unless name-converged). The one representation tension is F01.

### EC3-GLM-F09 — Incremental indexing, stale facts, and publication isolation (Area E)

- **Severity:** —
- **Disposition:** NO ISSUE FOUND
- **Verification (executed against real stores via `CodeIndexingService`):**
  - CE13 (in-memory): class rename + member removal replaced all stale nodes, declarations, `declares`/`contains`/`invokes-name` edges; stale symbol ids returned zero declarations and zero node lookups afterward.
  - CE14 (in-memory): file deletion removed the index unit and all its facts.
  - CE15 (in-memory): a plugin crash mid-run left the previous publication, latest publication pointer, and latest index state untouched (`run:1`); a repaired run then replaced facts cleanly.
  - CE16 (durable `LatticeCodeGraphStore`): same rename/delete flow removes stale facts, survives store dispose/reopen, and leaves no `drop.ts` unit.
  - CE17 (in-memory): changing only the plugin `Version` marks the unchanged source `Changed` (matches `CodeIndexPlanner.cs:204-208`), re-extracts, and replaces prior facts — the mechanism the profile-revision 4 bump relies on.
- Source corroboration: unit facts are replaced atomically per (repository, plugin, unit); `completesIndexUnit` gates exposure; staged deletions run before `CompleteIndexRunAsync`.

### EC3-GLM-F10 — Evidence and capability contracts (Area F)

- **Severity:** —
- **Disposition:** NO ISSUE FOUND
- **Verification:** capabilities advertise `Inheritance` for TS/TSX/JS and nothing else beyond the structural set; `implements` coverage is `Produced` for TypeScript and `NotProduced` for JavaScript (relationship kinds exclude it); `calls`/`references` are `NotProduced` by the session's coverage enumeration and the batch validator rejects non-resolved `calls` edges; prototype idioms emit `inherits` with `Derived` evidence only (CE8: three distinct Derived edges for `Object.create`, `util.inherits`, `Object.setPrototypeOf`); field-initializer invocations are `Structural` (CE5, matching the qualification suite); partial parse downgrades coverage to `Partial` (`TreeSitterExtractionSession.cs:171-173`). Elixir capability flags unchanged (`Inheritance` false, tested by the capability contract suite).

### EC3-GLM-F11 — Determinism and resource boundaries (Area G)

- **Severity:** —
- **Disposition:** NO ISSUE FOUND (Windows-only execution)
- **Verification (executed):** repeated extraction across independent sessions produces identical node/edge/declaration ids (CE10); `FieldProperties`/`MemberProperties` use ordinal `SortedSet`; edges dedupe via `TryAdd` on content-derived ids; nodes/declarations keyed by identity in the extraction result.
- **Source-level notes, unverified at runtime limits:** `Walk` is recursive over the syntax tree — pathological nesting is bounded only by parser recovery and stack limits; CE22 verified 200-deep nesting succeeds. Path identity uses the repository-relative forward-slash path (`CodeRepositorySource.cs:73`), so platform separators do not leak into identities; case sensitivity is ordinal (case-insensitive filesystems could theoretically vary casing between runs, but enumeration returns stored casing on NTFS). Cross-platform determinism is **not** claimed.

## D. Counterexample results

| ID | Area | Suspected violation | Outcome |
|---|---|---|---|
| CE1 | B | Shadowed heritage target converges with unrelated outer declaration | **Confirmed** (F01) |
| CE2 | A | Same-name classes/methods across files/scopes might merge | Failed — separation holds |
| CE3 | A/C | Duplicate members might lose a declaration | Partially confirmed — converge with one declaration each (F04) |
| CE4 | C | Unsupported member shapes might emit facts | Failed — all skipped; static-block invocation attributed to class (F02) |
| CE5 | D | Initializer invocations might attribute to the class | Failed — field-owned, class owns none |
| CE6 | B | Cross-file heritage might converge across files | Failed — placeholders file-scoped, declaration-less |
| CE7 | A | Class-expression members might get owner-derived identity | Confirmed convergence (documented limitation, F05) |
| CE8 | B | Prototype idioms might emit Structural or guessed evidence | Failed — exactly three Derived edges, correct pairs |
| CE9 | D | Decorator invocations might vanish or misattribute | Confirmed attribution to field node (F03) |
| CE10 | G | Extraction might be nondeterministic; static modifiers wrong | Failed — deterministic; modifiers correct |
| CE11 | G | Unicode/overlong identifiers might crash or corrupt | Failed — unicode extracted, 300-char dropped |
| CE12 | C/F | TSX grammar variant might skip fields | Failed — fields + access/readonly/static modifiers correct |
| CE13 | E | Rename/removal might leave stale facts | Failed — full cleanup on in-memory store |
| CE14 | E | File deletion might leave units | Failed — unit and facts removed |
| CE15 | E | Failed run might corrupt publication | Failed — publication isolated; recovery clean |
| CE16 | E | Durable store might retain stale facts or lose replacements | Failed — LatticeDb cleanup durable across reopen |
| CE17 | E | Version-only change might not invalidate | Failed — planner marks Changed, facts replaced |
| CE18 | C | Abstract signatures might emit members | Failed — no facts (as documented) |
| CE19 | A | Overloads might lose signatures | Failed — one callable, three exact declarations |
| CE20 | A | Accessor pairs might split or become fields | Failed — one accessor symbol, two declarations |
| CE21 | C | Modifiers might be fabricated or dropped | Failed — declare/optional/access recorded exactly |
| CE22 | G | Deep nesting might crash extraction | Failed — 200-deep nesting survives, class emitted |

Two initial test failures during probe development were probe bugs, not implementation defects (qualified-name mismatch in CE2; single-node assertion across two files in CE10 — the observed behavior actually *demonstrated* correct path-scoped separation).

## E. Lifecycle and publication verification

Exercised the real `CodeIndexingService` over `FileSystemCodeRepositoryProvider` with:

1. `InMemoryCodeGraphStore` — CE13, CE14, CE15, CE17 (stale-node/declaration/edge cleanup, deletion, failure isolation, version invalidation).
2. `Penghou.Hetu.LatticeCodeGraphStore` (durable file database) — CE16, including store disposal and reopen to prove durability.

A fixture comparing only extracted batches was deliberately avoided: all lifecycle probes query the published graph (`FindNodesByQualifiedNameAsync`, `GetDeclarationsAsync`, `GetNodeAsync`, `GetLatestPublicationAsync`, `GetLatestIndexStateAsync`, `GetPublishedUnitsAsync`) after completed runs, and CE15 verifies a *failed* run leaves the prior publication authoritative.

Unverified residual: interrupted indexing (process kill mid-run) was not simulated; cancellation behavior relies on the lifecycle's existing failure path (exercised via plugin crash, which is the same code path).

## F. Evidence and capability verification

See F10. Summary against the required table:

| Relationship/observation | Permitted evidence | Observed |
|---|---|---|
| Written containment / declarations | Structural | Structural only |
| Written `extends`/`implements` | Structural | Structural only (F01 affects target representation, not quality) |
| Recognized prototype patterns | Derived | Derived only, exact patterns |
| Structural invocation names | Structural | Structural (field-, method-, class-, file-sourced per lexical ownership) |
| Resolved calls/references | Not produced | Never produced; validator enforces |

## G. Test execution

Environment: Windows 11 (win32), .NET SDK 10.0.401, .NET runtime 10.0.12, xunit 2.9.3.

Commands run in the review worktree:

- `dotnet test tests/Penghou.Hetu.TreeSitter.Tests` (baseline before review probes): **79 passed**.
- `dotnet test tests/Penghou.Hetu.TreeSitter.Tests --filter FullyQualifiedName~EcmaGlmReview…`: **23 review probes, all passed** after fixing probe-side bugs (12 adversarial + 5 lifecycle + 5 final probes + 1 dump).
- `dotnet test Penghou.Hetu.slnx`: **329 passed, 0 failed** (TreeSitter 97, Hetu 160, LatticeDb 26, CSharp 46). The 311 implementation tests pass unchanged alongside the 18 review-only additions (97 = 79 + 18).

Review-only artifacts (preserved on the review branch, not on `main`):

- `tests/Penghou.Hetu.TreeSitter.Tests/EcmaGlmReviewAdversarialTests.cs` (CE1–CE12)
- `tests/Penghou.Hetu.TreeSitter.Tests/EcmaGlmReviewLifecycleTests.cs` (CE13–CE17 + controllable fake plugin)
- `tests/Penghou.Hetu.TreeSitter.Tests/EcmaGlmReviewFinalProbeTests.cs` (CE18–CE22)
- `tests/Penghou.Hetu.TreeSitter.Tests/EcmaGlmReviewDumpTests.cs` (fact dump)
- One marked `ProjectReference` addition to `Penghou.Hetu.TreeSitter.Tests.csproj` so the LatticeDb lifecycle probe compiles (labeled REVIEW-ONLY in the file).

Limitations: Windows-only execution; no test of process-kill interruption; determinism verified by identity equality of repeated runs, not byte-level batch equality; stack-depth limits beyond 200 levels not probed.

## H. Coverage assessment

- **Proven (executed):** identity separation across files/scopes/kinds/ownership; overload and accessor convergence; field extraction with modifiers/access on TS and TSX and JS; initializer-invocation ownership; unsupported-member conservatism; prototype-pattern Derived evidence; capability/coverage honesty; stale-fact cleanup on rename/member-removal/file-deletion for in-memory and durable stores; failed-run publication isolation; plugin-version invalidation; determinism of repeated extraction.
- **Partially supported (source + limited execution):** partial-parse behavior (existing fixtures + session source; no adversarial fuzzing), heritage placeholder semantics (non-shadowed cases proven; shadowing consequence demonstrated but its consumer impact unquantified), deep-nesting bounds (200 levels only).
- **Documented, unverified here:** cross-file declaration merging (ambient/global) — explicitly out of scope; pathless top-level function/enum/namespace identity convergence (documented; not re-probed).
- **Undocumented behaviors found by this review:** heritage-target shadowing convergence consequence (F01); static-block invocation ownership (F02); decorator invocation attribution (F03); duplicate-member convergence (F04).
- **Not verified:** cross-platform determinism, process-kill recovery, LatticeDb internals, TSX/JS grammar versions other than the pinned ones.

## I. Final verdict

**SUPPORTED WITH QUALIFICATIONS.**

No confirmed blocking defect. The EC-3 identity model holds under every separation counterexample attempted; member extraction is conservative and honest; lifecycle, replacement, failure isolation, and version invalidation behave correctly on both exercised stores; evidence and capability contracts are respected everywhere they were probed. The qualification rests on the execution evidence above, not merely on passing tests.

Qualifications:

1. **F01 (P2, design ambiguity):** same-file name convergence of heritage targets can attach a written `extends Base` to an unrelated, symbol-bearing declaration when the name is lexically shadowed. The fact remains Structural and documented as non-resolution, but the representation invites resolved readings, and neither the doc nor the tests record the shadowing consequence. A policy decision (marker property, declaration-less targets, or explicit documented acceptance) should precede reliance on `inherits`-based impact queries in dogfooding.
2. **F02–F04 (P3):** three undocumented behaviors (static-block invocation ownership, decorator invocation attribution, duplicate-member convergence) are consistent with the lexical-containment contract but should be recorded.
3. Windows-only execution; interruption-during-publication not exercised beyond plugin-crash simulation.

## J. Recommended next actions

1. Resolve F01's policy question (recommended: keep heritage targets declaration-less like `implements`, or add an explicit `heritage.match = "name"` marker property) and add a shadowing fixture either way; document the chosen semantics in `ecmascript-symbol-identity.md`.
2. Record F02–F04 in `ecmascript-symbol-identity.md` / the feature matrix (three short limitation entries).
3. Optionally align the class-member partial-recovery guard with the interface-member next-sibling check (F07) for consistency.
4. Before real-repository dogfooding that uses `inherits` impact analysis, quantify consumer behavior against F01; otherwise proceed.
5. Nonblocking: consider documenting the recursive-walk depth boundary and adding a pathological-nesting fixture to the regular suite.

**EC-3 is qualified to proceed to real-repository dogfooding**, with qualification 1 tracked as a follow-up decision rather than a release blocker for extraction correctness.
