# EC3-GLM-Q2 — Independent verification of the EC-3D heritage remediation

Reviewer: GLM (independent of the implementing agent)
Review date: 2026-10-23
Review type: focused adversarial remediation verification
Implementation under review: `b1d4827` (EC-3D) plus documentation commit `1cc2939`
Baseline: `e4598ac` (EC-3C, the revision reviewed in EC3-GLM-Q1)
Prior review: `EC3-GLM-Q1` (preserved at `ba2ca83` on `review/ec3-glm-q1`, never merged)

---

## A. Git binding

| Item | Value |
|---|---|
| Repository | `jenolaszlo-sketch/penghou-hetu` |
| Canonical repository | `C:\Users\Laszlos\source\repos\Penghou.Hetu` (`main`, clean, exactly at `1cc2939`; untouched during this review) |
| Review worktree | `C:\Users\Laszlos\source\repos\Penghou.Hetu.review-ec3-q2` |
| Review branch | `review/ec3-glm-q2`, created from `1cc2939`; review-only artifacts committed on top |
| Reviewed implementation commit | `b1d4827489c0aea114952049d11f082650514c02` (tree `43a2a2ae5b1ddd8f13e51195133c2aff8f6e88f5`), parent `e4598ac` |
| Reviewed documentation commit | `1cc2939ea615d627cde616d06e739867be4e56e2` (tree `d97170219fe930fe2e1921853be479e39675bb0d`), parent `b1d4827` |
| Production baseline | `e4598ac9b517654b8346cdcaf96c594fc50bc53b` (tree `7158725553a4f45170ce5f90b39b575f4d6ed56a`), parent `29c0e45` |
| Q1 preservation | `ba2ca83ace500e148a2bfa7de9c0c01d8ef6f6ff` on `review/ec3-glm-q1` (worktree `C:\Users\Laszlos\source\repos\Penghou.Hetu.review-ec3`), parent `e4598ac`; left untouched |

Ancestry verified linear: `e4598ac → b1d4827 → 1cc2939`. `main` pointed exactly at `1cc2939` with a clean status when the review started and was not modified, merged, rebased, or pushed. The Q1 review branch and its worktree were not touched. The original F01 finding was read from `ba2ca83:docs/reviews/ec3-glm-q1-independent-review.md` via `git show` without merging the branch.

Environment: Microsoft Windows NT 10.0.26200 (Windows 11), .NET SDK 10.0.401, .NET runtime 10.0.12, xunit 2.9.3, git worktrees on one local disk.

Source files inspected (review commit `1cc2939`):

- `src/Penghou.Hetu.TreeSitter/EcmaScriptExtractionProfile.cs` — full read; full diff `e4598ac..b1d4827`
- `src/Penghou.Hetu/CodeGraphQueryService.cs` (full), `CodeGraphBatchValidator.cs` (full), `CodeGraphIngestionSink.cs` (full), `CodeGraphTraversalPresets.cs` (full), `CodeIndexPlanner.cs` (planning/invalidation region), `InMemoryCodeGraphStore.cs` (lookup/traversal/unit-replacement regions)
- `src/Penghou.Hetu.Abstractions/CodeIdentities.cs`, `CodeGraphFacts.cs`, `CodeGraphBatch.cs`, `CodeGraphExtraction.cs`, `CodeGraphCapabilities.cs` (model/constructor regions)
- Test diffs `e4598ac..b1d4827` for `TreeSitterEcmaClassIdentityTests.cs`, `TreeSitterEcmaClassLifecycleTests.cs`, `TreeSitterJavaScriptPluginTests.cs`, `TreeSitterTypeScriptPluginTests.cs`
- Doc diffs `1cc2939` for `docs/ecmascript-symbol-identity.md`, `docs/language-feature-matrix.md`, `CHANGELOG.md`
- `tests/Penghou.Hetu.TreeSitter.Tests/TreeSitterTestSupport.cs`, `TreeSitterEcmaClassLifecycleTests.cs` (full)
- Q1 review evidence via `ba2ca83` (`EcmaGlmReviewLifecycleTests.cs` reused as a pattern source only)

Tests actually executed (all in the review worktree, all passing at review end):

- `dotnet test tests/Penghou.Hetu.TreeSitter.Tests` — baseline before review probes: **82 passed**
- `dotnet test tests/Penghou.Hetu.TreeSitter.Tests --filter FullyQualifiedName~EcmaGlmQ2Review` — **17 review probes** (9 adversarial, 8 publication)
- `dotnet test Penghou.Hetu.slnx` — **331 passed, 0 failed** (Hetu 160, TreeSitter 99 = 82 + 17 review, LatticeDb 26, CSharp 46)

Review-only artifacts (preserved on `review/ec3-glm-q2`, not on `main`):

- `tests/Penghou.Hetu.TreeSitter.Tests/EcmaGlmQ2ReviewAdversarialTests.cs` (Q2-CE1 … Q2-CE9)
- `tests/Penghou.Hetu.TreeSitter.Tests/EcmaGlmQ2ReviewPublicationTests.cs` (Q2-P1 … Q2-P8, incl. a controllable fake plugin and a durable-store probe)
- One `ProjectReference` addition to `Penghou.Hetu.TreeSitter.Tests.csproj` so the LatticeDb probe compiles (labeled REVIEW-ONLY in the file, mirroring the Q1 review).

Limitations of this review: Windows-only execution; the real `p4 → p5` transition could not be exercised with the old plugin binary (see F-Q2-03); process-kill interruption was not simulated (same residual as Q1); cross-platform determinism not claimed.

## B. F01 verification

The original finding (EC3-GLM-Q1 F01, P2, DESIGN AMBIGUITY): under profile revision 4, `StructuralTypeNode` hashed `class\n{path}\n{name}`, so a written `extends Base` inside a shadowing function converged with the unrelated top-level `Base` declaration; the `inherits` edge attached to a symbol-bearing node and impact queries from the outer `Base` falsely reported `create.Derived` as a dependent.

The remediation (option (b) from the Q1 recommendation) moves written heritage names to declaration-less candidates:

```text
{language}:heritage-target:{sha256(path + "\n" + writtenName)}
```

Source-verified at `EcmaScriptExtractionProfile.cs:441-446` (`Hash($"{_path}\n{name})"`, SHA-256 lowercase hex of UTF-8). The identity is in a dedicated namespace; `{language}:symbol:`, `{language}:import:`, and `{language}:invoked:` prefixes are disjoint, so no `AddNode` symbol-wins convergence can occur across namespaces.

### Original counterexample, executed twice

```typescript
// src/heritage-shadow.ts
class Base {}

function create() {
    class Base {}
    class Derived extends Base {}
}
```

Q2-CE1 executes it at extraction level; Q2-P1 republishes it through the real `CodeIndexingService` over `InMemoryCodeGraphStore` and queries only through `CodeGraphQueryService` (`FindSymbolsByQualifiedNameAsync`, `GetNeighborhoodAsync`, `GetImpactSetAsync`) plus store reads. All eight required checks hold:

| # | Required check | Result |
|---|---|---|
| 1 | `create.Derived` retains declared class identity | Held — single symbol-bearing node with `SymbolId = typescript:{sha256("class\nsrc/heritage-shadow.ts\ncreate.Derived")}` |
| 2 | Written `Base` target has no `SymbolId` | Held — `candidate.SymbolId is null`, empty properties |
| 3 | Heritage candidate is a distinct graph node | Held — id `typescript:heritage-target:{sha256("src/heritage-shadow.ts\nBase")}`, computed independently by this review; ≠ both declared `Base` nodes |
| 4 | Cannot collide with the declared top-level `Base` | Held — disjoint id namespaces; `NotEqual` asserted; candidate has no declaration, no `declares`/`contains` edges |
| 5 | `inherits` edge points to the candidate | Held — single `inherits` edge `create.Derived → candidate` |
| 6 | Edge remains `Structural` | Held — with source location `src/heritage-shadow.ts` |
| 7 | Impact from top-level `Base` excludes `create.Derived` | Held — `GetImpactSetAsync(topLevelBase)` contains neither `create.Derived`, `create.Base`, nor the candidate |
| 8 | Impact from the candidate follows documented traversal semantics | Held — `GetImpactSetAsync(candidate)` contains `create.Derived` (incoming `inherits`, ImpactSet preset includes `Inherits`); impact from the inner `create.Base` contains nothing |

Published-query evidence additionally confirms storage does not reintroduce the ambiguity: unit replacement keeps candidate and declared nodes separate, and no declaration references the candidate node.

**F01 is corrected.**

## C. Candidate identity assessment

### Test A — same written name, different files (Q2-CE2)

`a.ts: class A extends Base {}`, `b.ts: class B extends Base {}`, `base.ts: class Base {}`.

Observed: exactly two SymbolId-less candidates with QualifiedName `Base`, identities `typescript:heritage-target:{sha256("src/a.ts\nBase")}` and `…("src/b.ts\nBase")` (both independently computed); each `inherits` edge targets only its own file's candidate; the declared `Base` receives no heritage edge; sources carry the language property and a `SymbolId`. **Held.**

### Test B — same written name, different lexical scopes (Q2-CE3, Q2-P2)

One file: top-level `Alpha extends Base` plus `create.Base` and `Beta extends Base`.

Observed: one candidate `typescript:heritage-target:{sha256("src/shared-candidate.ts\nBase")}` shared by two `inherits` edges. The edges are fully distinguishable: distinct edge ids, distinct source nodes (`Alpha`, `create.Beta`), distinct evidence locations. The candidate is not the inner `create.Base` declaration and carries no symbol.

Assessment (per the review spec's options): **a valid representation of one unresolved written-name candidate scoped to a file**, and a documented limitation (the identity doc states same-spelled targets in one file share a candidate; each relationship keeps its own location and evidence). It is not a graph-model ambiguity that produces misleading dependencies: impact from the shared candidate returns exactly the two classes that wrote the name, which is the correct unresolved-name answer, and impact from either declaration returns nothing. Consumer distinction is possible through edge evidence (source id + location). Incremental behavior is correct — see §E.

### Test C — qualified heritage names (Q2-CE4, Q2-CE9)

`class A extends Models.Base {}`, `class B extends Other.Base {}`, `class C extends Deep.Nested.Inner {}`.

Observed: distinct candidates per written dotted name, each preserving the full written name (`Models.Base`, `Other.Base`, `Deep.Nested.Inner`) with the documented identity formula. The grammar exposes the target as a `member_expression`; `HeritageName` → `MemberName` handles arbitrary dot depth. `extends mixin(Base)` (unsupported expression) emits nothing — no edge, no fabricated candidate. Generic type arguments are not part of the name: `class Generic extends Base<string> {}` produces candidate `Base` (the identifier subset), consistent with the feature matrix's "generic arguments not modeled" and provably collision-free (see F-Q2-04). **Held within the documented subset.**

### Test D — inheritance and implementation distinction (Q2-CE5)

```typescript
interface Repository {}
class Service implements Repository {}
interface Specialized extends Repository {}
```

Observed: one `implements` edge (`Service → candidate Repository`, `Structural`) and one `inherits` edge (`Specialized → the same candidate`, `Structural`); kinds are not confused; neither edge targets the declared `Repository` interface node; the declared interface gains no dependents; no edge claims `Resolved`. **Held.**

### Test E — prototype inheritance (Q2-CE6)

```javascript
function Child() {} function Other() {} function Third() {} class Base {}
Child.prototype = Object.create(Base.prototype);
util.inherits(Other, Base);
Object.setPrototypeOf(Third.prototype, Base.prototype);
Ghost.prototype.__proto__ = Base.prototype;
util.inherits(Dyn, registry[name]);
```

Observed: exactly three `inherits` edges, all `Derived`, each pairing the exact child/parent candidate identities computed by this review. Child and parent candidates are SymbolId-less and disjoint from the declared `Child`/`Other`/`Third` functions and `Base` class. The two dynamic/unsupported patterns produced no edges and no `Ghost`/`Dyn`/`registry` nodes. No guessed resolved relationships. **Held.**

## D. Evidence verification

| Relationship/observation | Permitted evidence | Observed |
|---|---|---|
| Written `extends` | Structural | Structural only (CE1, CE2, CE4, CE9) |
| Written `implements` | Structural | Structural only (CE5) |
| Interface-level `extends` | Structural | Structural only (CE5) |
| Recognized prototype patterns | Derived | Derived only, exact pairs (CE6) |
| Resolved | Never produced | No edge with `Resolved` quality in any probe (asserted) |

Identity invariants verified:

- Declared symbols and candidates occupy disjoint identity domains (`symbol:` vs `heritage-target:`; also `import:` and `invoked:`); asserted for every probe.
- Candidates carry no `SymbolId` and no declarations (checked against published declarations, not just extraction output).
- Candidate names preserve written source names, including dotted names (CE4); generic arguments are outside the name by design (CE9).
- Candidate identities are deterministic across file orderings (CE8) and unchanged republication (P5).
- Relationships keep source provenance: provider id, provider version (`…abi14.p5`/`…abi15.p5` observed), and precise source location (CE6 dump output).
- Existing declared class/interface/member identities are unchanged by EC-3D: CE7 recomputes the exact pre-EC-3D hashes (`class\npath\nqualified`, `interface\n…`, `method\nowner\nownership\nname`, `field\n…`, `interface-method\n…`) and matches every node id — the diff confirms `TryEmitNamed`/`EmitClassMember` were untouched.
- Unsupported expression targets are never fabricated (CE4, CE6, CE9).
- No candidate is promoted into a declared symbol: the `AddNode` symbol-wins rule can never apply across id namespaces, and no storage path reintroduces convergence (P1 verified against the published graph).

## E. Publication lifecycle verification

All probes drive the real `CodeIndexingService` and assert through public query APIs. Stores exercised: `InMemoryCodeGraphStore` (P1–P7) and durable `Penghou.Hetu.LatticeCodeGraphStore` including dispose/reopen (P8).

| # | Requirement | Probe | Result |
|---|---|---|---|
| 1 | Prior plugin-version manifests invalidated | P7 + source `CodeIndexPlanner.cs:206-208` | Held — fake-plugin version bump marked the unchanged source `Changed` (`FilesChanged >= 1`); both real plugin versions end `.p5` (asserted in P7) |
| 2 | Old heritage placeholders removed | P7 | Held — the p4-shaped `{fake}:symbol:{sha256("class\npath\nname")}` placeholder node is gone from the store and published units |
| 3 | New heritage candidates published | P7 | Held — `{fake}:heritage-target:{sha256("path\nname")}` present, SymbolId-less |
| 4 | Stale inheritance/implementation edges removed | P7, P3 | Held — old `inherits` edge gone; rename replaces the relationship (`OldBase` edge gone, `NewBase` edge present, `OldBase` candidate retired); `implements` removal covered by the implementation's own reindex test |
| 5 | Deleting a heritage clause removes obsolete facts | P2 (third step) | Held — removing the last `extends` clause retires the candidate node and edge |
| 6 | Renaming the written target replaces the candidate relationship | P3 | Held — see #4 |
| 7 | Removing a source file removes its owned heritage facts | P4, P8 | Held — candidate and edge gone; declared same-named class elsewhere survives; durable across store reopen |
| 8 | Reindexing unchanged sources deterministic | P5 + CE8 | Held — planner reports 0 changed; published fact identities byte-identical; extraction identical across file orderings |
| 9 | Failed indexing preserves previous publication | P6 | Held — plugin crash mid-run leaves `run:1` publication, latest index state, candidate node, and candidate impact query intact |

Shared-candidate semantics (spec §6 focus): P2 removes one of two occurrences incrementally — the surviving occurrence keeps its edge and the candidate remains; only the final removal retires the candidate. Removal of one occurrence never removes the other's still-valid relationship. P8 additionally shows a file-owned candidate survives deletion of an *unrelated* same-named file (`base.ts` declaring `Base` deleted while `child.ts`'s candidate persists), then retires when its owning file is deleted.

**Unverified rather than assumed:** the literal production `p4 → p5` transition with the real plugin binary (the old binary no longer exists in the tree; P7 is a faithful shape-level proxy — old placeholder identity + edge replaced by candidate identity + edge under a version bump, through the real planner/sink/store paths). Process-kill interruption remains unexercised (same as Q1; the plugin-crash path is the same failure path).

## F. Adversarial probes

| ID | Challenge | Outcome |
|---|---|---|
| Q2-CE1 | Original F01 shadowing repro, extraction level | **Invariant held** (all 8 identity/evidence checks) |
| Q2-CE2 | Same-name targets, different files | **Held** — distinct path-scoped candidates; no declared binding |
| Q2-CE3 | Same-name targets, different scopes, one file | **Held** — one shared candidate, distinct edges/locations |
| Q2-CE4 | Qualified and expression targets | **Held** — dotted names preserved distinctly; `mixin(…)` fabricates nothing |
| Q2-CE5 | `implements` vs interface `extends` beside declared interface | **Held** — kinds correct; no declared binding; no `Resolved` |
| Q2-CE6 | Prototype idioms + dynamic patterns | **Held** — exactly 3 `Derived` edges; dynamic invents nothing |
| Q2-CE7 | Declared identity drift | **Held** — every declared class/interface/method/field/interface-member id matches the pre-EC-3D formula |
| Q2-CE8 | Extraction determinism across orderings | **Held** — identical node/declaration/edge serializations |
| Q2-CE9 | Unsupported/under-modeled forms | **Held** — class-expression heritage and dynamic forms fabricate nothing; generic `Base<string>` → name-subset candidate |
| Q2-P1 | F01 repro on the published graph + impact both directions + lookup observation | **Held** (with recorded consumer observation, F-Q2-01) |
| Q2-P2 | Candidate sharing under incremental occurrence removal | **Held** — survivor keeps its relationship; last removal retires candidate |
| Q2-P3 | Written-target rename replaces relationship | **Held** |
| Q2-P4 | Owning-file deletion removes owned facts | **Held** |
| Q2-P5 | Deterministic unchanged reindex | **Held** — 0 changed files; identical published fact identities |
| Q2-P6 | Failed run preserves previous heritage publication | **Held** |
| Q2-P7 | p4 → p5 revision transition | **Held** (fake-plugin shape proxy; real `.p5` versions asserted) |
| Q2-P8 | Durable LatticeDb lifecycle + reopen | **Held** |

Probe development notes (honesty record): three initial failures were probe bugs, not implementation defects — an unsorted expected-id array (CE2), a line-range predicate matching all three prototype edges (CE6), and a fake version string `"9.9.9+p4"` failing `EndsWith(".p4")` so the fake emitted the p5 shape during the p4 phase (P7), plus a fake-plugin class name colliding with a real fixture name (P6). All were fixed in the probes; the implementation was never modified.

## G. Findings

### EC3-GLM-Q2-F01 — Exact-name lookup can now return a declaration-less heritage candidate beside a same-named declared symbol

- **Disposition:** DESIGN AMBIGUITY
- **Severity:** P3
- **Location:** `src/Penghou.Hetu.TreeSitter/EcmaScriptExtractionProfile.cs:441-446` (candidate QualifiedName = written name) with `src/Penghou.Hetu/CodeGraphQueryService.cs:68-81` and `InMemoryCodeGraphStore.FindNodesByQualifiedNameAsync` (matches all nodes; result ordered by node id ordinal, where `heritage-target:` sorts before `symbol:`).
- **Violated/unclear contract:** none violated — `FindSymbolsByQualifiedNameAsync` is documented as preserving ambiguity and candidates are honest SymbolId-less nodes. The consequence is new for same-file cases: under revision 4 a same-file written heritage name converged with the declaration and exact-name lookup returned exactly one symbol-bearing candidate; under revision 5 it returns two.
- **Minimal counterexample (executed, Q2-P1):** `class Base {}` + `class Derived extends Base {}` in one file → `FindSymbolsByQualifiedNameAsync(repo, "Base")` returns `[candidate (SymbolId null), declared (SymbolId)]`; `IsAmbiguous` is `true`; `SingleOrDefault` yields `null`; `Candidates[0]` is the SymbolId-less candidate.
- **Expected behavior:** per the EC-3D model this is intended (the candidate is a distinct node and must not be hidden), but consumers relying on unique resolution of previously-unambiguous names will observe a behavior change.
- **Observed behavior:** as above; deterministic and stable.
- **Consumer-query implications:** consumers must filter candidates by `SymbolId is not null` (as `FindSymbolsAsync` discovery and `GetSymbolsInFileAsync` already do) or handle `IsAmbiguous`; legacy `Candidates.FirstOrDefault()` patterns may receive a declaration-less node.
- **Suggested minimal correction:** a short consumer-guidance note in `docs/ecmascript-symbol-identity.md` ("exact-name and name-pattern lookups include heritage candidates; filter by `SymbolId`"). Optionally (larger, not required for closure) a dedicated symbol-only lookup overload.
- **Remaining uncertainty:** which real consumer codepaths depend on unique exact-name resolution is outside this review's scope.

### EC3-GLM-Q2-F02 — Same-file candidate sharing aggregates written-name occurrences into one candidate node

- **Disposition:** DOCUMENTED LIMITATION
- **Severity:** P3
- **Location:** `EcmaScriptExtractionProfile.cs:441-446`; documented in `docs/ecmascript-symbol-identity.md` ("Targets with the same written name in one file share a candidate node; each relationship retains its own source location and evidence quality").
- **Evidence (executed):** CE3 and P2 — sharing is real but harmless: per-occurrence edges carry distinct sources and locations; impact from the shared candidate is the union of genuine written occurrences; removal cascades correctly (P2); no misleading dependency is produced because candidates are never presented as declarations.
- **Expected vs observed:** consistent with the documented model; no correctness defect found.
- **Suggested correction:** none required; the existing documentation plus the edge-evidence distinguishability suffice. Consumers needing per-occurrence semantics should read edges, not the candidate node.
- **Remaining uncertainty:** none about mechanism; consumer severity judged low.

### EC3-GLM-Q2-F03 — Generic heritage names resolve to the name-subset candidate

- **Disposition:** DOCUMENTED LIMITATION
- **Severity:** P3
- **Location:** `EcmaScriptExtractionProfile.cs:448-454` (`HeritageName` returns the identifier/member-expression name; type arguments are separate grammar children).
- **Executed (Q2-CE9):** `class Generic extends Base<string> {}` produces candidate `Base` with the standard identity — the written name is preserved as the identifier subset, not verbatim with `<string>`. This matches the feature matrix's "multi-base order and generic arguments not modeled" and causes no collision: the candidate equals the one a plain `extends Base` would produce in the same file (consistent sharing of one unresolved name).
- **Suggested correction:** none required; optionally one sentence in the identity doc that type arguments are not part of the candidate name.
- **Remaining uncertainty:** none for identifier/member targets; exotic targets (conditional/import types) were not probed and fall outside the documented subset.

### EC3-GLM-Q2-F04 — Production p4 → p5 transition verified by shape proxy, not the old binary

- **Disposition:** DOCUMENTED LIMITATION (review-methodological)
- **Severity:** P3
- **Evidence:** the real p4 binary is gone from the tree, so the transition was exercised via a controllable fake plugin emitting the exact old placeholder identity and edge shape, then the exact new candidate shape, through the real planner (`Changed` on version bump), ingestion sink, unit replacement, and stores (P7, P8). Real plugin versions ending `.p5` are asserted; the planner's version comparison is source-verified at `CodeIndexPlanner.cs:204-208`. The first real-repository reindex after this change will confirm end-to-end; no anomaly is expected on this evidence.
- **Remaining uncertainty:** none identified beyond the unavoidable absence of the old binary.

No CONFIRMED DEFECT and no PLAUSIBLE DEFECT was found. Two probe development failures recorded in §F were probe bugs. The disk-full incident during one full-solution run caused two spurious LatticeDb I/O failures that disappeared after freeing space and did not reproduce (full suite green afterwards).

## H. Verdict

**VERIFIED WITH QUALIFICATIONS.**

F01 is corrected: an unresolved heritage name can no longer create a false relationship to a declared symbol, demonstrated at extraction level and through the published graph with public query APIs, with identities recomputed independently by this review. Graph identity (declared identities bit-for-bit stable, CE7), evidence (Structural/Derived only, never Resolved), determinism (CE8, P5), and publication correctness (P2–P8, both stores) all hold under every adversarial probe attempted. No material regression was found within the stated review scope.

Qualifications (all P3, nonblocking):

1. F-Q2-01: exact-name lookups are now ambiguous for declared names that also appear as written heritage targets; consumer guidance should be recorded (candidate sorts first by ordinal id; `SingleOrDefault` returns `null`).
2. F-Q2-02/F-Q2-03: candidate sharing in one file and name-subset handling of generic targets are documented limitations of the structural model.
3. F-Q2-04: the production p4 → p5 invalidation is proven by a faithful shape-level proxy plus source verification; the first real-repository reindex provides the final end-to-end confirmation.
4. Verification is Windows-only; process-kill interruption was not simulated.

This verdict applies to EC-3D only — not to the entire ECMAScript extractor and not to the broader Hetu code graph. No cross-platform qualification is claimed.

## I. Recommended next steps

1. Add the one-paragraph consumer-guidance note for heritage candidates in name lookups to `docs/ecmascript-symbol-identity.md` (F-Q2-01). Documentation only.
2. Optionally record that generic type arguments are excluded from candidate names (F-Q2-03). Documentation only.
3. Confirm the p4 → p5 invalidation once on the first real-repository reindex (F-Q2-04). No code change expected.
4. No production code changes are recommended by this review. EC-3D is qualified to close.
