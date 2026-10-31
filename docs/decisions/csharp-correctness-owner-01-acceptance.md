# CSHARP-CORRECTNESS-OWNER-01: Owner Acceptance With Qualifications

**Project:** Andirit

**Milestone:** CSHARP-CORRECTNESS-OWNER-01 — owner acceptance of
ANDIRIT-CSharp-CORRECTNESS-01 following the independent
CSHARP-CORRECTNESS-Q1 qualification

**Recorded:** 2026-10-31

**Decision authority:** explicit owner instruction in the
CSHARP-CORRECTNESS-OWNER-01 request ("Decision: ACCEPT WITH QUALIFICATIONS"),
including its fixed Git binding, evidence requirements and change boundary

**Executor:** delegated acceptance recorder; the Git author label records
automation provenance, not a human signature or an independent review event.
The acceptance recorder is not an independent reviewer, issues no second
review verdict, and does not speak for the implementing agent or the
independent reviewer.

**Disposition: ACCEPT WITH QUALIFICATIONS.**

This record documents an already-made owner decision. It does not reopen
implementation, does not initiate another broad review, and modifies no
production code. The roles remain distinct throughout:

- **Implementing agent's report:** the production-remediation packet
  [`docs/planning/csharp-correctness-01/`](../planning/csharp-correctness-01/README.md)
  (commits `80edf93`, `01365a2`, `92fc99d`).
- **Independent reviewer's verdict:** `CSHARP-CORRECTNESS-Q1 SUPPORTED WITH
  QUALIFICATIONS`, preserved outside the checkout (section C). That review
  explicitly issued no owner acceptance.
- **This document:** the owner's acceptance of the two production corrections
  within the independently verified scope, retaining every documented
  qualification.

The decision applies specifically to **P-DEF-01** (assembly/compilation-aware
C# semantic target resolution) and **P-DEF-02** (deterministic partial-type
documentation handling and type-safe inconsistency diagnostics). It does not
authorize CI-1A implementation, occurrence-storage changes, package
publication, release or push.

## A. Authority and Git binding

Repository `jenolaszlo-sketch/andirit`; canonical checkout
`D:\projects\Andirit`; branch `main`; single worktree (no branches, no linked
worktrees); clean tracked state.

| Role | Commit | Tree |
| --- | --- | --- |
| Starting production baseline | `5c89ac0b3d0e87c0112880cf985826ded42b21b2` | `e61994289e5f96203ded37be000b0ed3bb3b79a2` |
| Production implementation | `80edf9302d3fa813aebf761db47d16a24667ef11` | `2085752eb5c1189f596a0b8baf48662fbf55002a` |
| Implementation evidence | `01365a2213e9d53a11d6ffe2197395c258d9d081` | `63e529351cf5c9bc55f9e706eb86df8b92db921a` |
| Independently reviewed HEAD | `92fc99df2323ef1bd8282c6a1cb3b1f79995779b` | `52e74a04aa6a9e8bd9199a1c4facbdf397a8dccb` |

State verified by this recorder immediately before recording:

- `origin/main` = `5c89ac0…` (the baseline); local `main` is three commits
  ahead (`80edf93`, `01365a2`, `92fc99d`) and **not pushed**.
- `git diff 80edf93..92fc99d` touches only
  `docs/planning/csharp-correctness-01/**` — no production or test change
  after the implementation commit.
- `git diff 5c89ac0..92fc99d -- src tests` shows exactly the five files
  declared in the packet's changed-file inventory (three
  `src/Andirit.CSharp` partials, two `tests/Andirit.CSharp.Tests` files).
- Working tree byte-identical to the reviewed HEAD (`git diff 92fc99d` empty)
  before any acceptance documentation was written.

The final accepted production source tree is therefore
`52e74a04aa6a9e8bd9199a1c4facbdf397a8dccb` — the tree of the independently
reviewed HEAD. The acceptance documents themselves are documentation-only
commits on top of it (see section G).

## B. Decision

**`ACCEPT WITH QUALIFICATIONS`** — the owner accepts both production
corrections within the independently verified scope and retains all
documented qualifications.

### P-DEF-01 — accepted as the provider-level correction (P-DEF-01b)

The owner accepts **P-DEF-01b, the provider-level correction** to C#
relationship target resolution, rather than limiting protection to a future
CI-1A query (P-DEF-01a). This choice is justified because the defect affected
**existing graph relationships**: the baseline resolver published false
cross-project edges into the aggregate graph that CI-1A's existing consumers
read. A new-query-only boundary would have left the defect class in the
extractor (including the independently discovered p18 instance, section D).

The prerequisite packet
([`ci1-scope-r1/prerequisite-defects.md`](../planning/ci1-scope-r1/prerequisite-defects.md))
had recommended P-DEF-01a as the smallest boundary and listed P-DEF-01b as an
owner option conditional on proving unchanged legacy identities and
cardinalities. The independent reviewer assessed this deviation as acceptable
(COMPATIBILITY-AUDIT §6): the condition is proven for every exercised surface
(byte-identical `cross` fixture `bf8681ca…0ab72`; surviving edge ids with
identical endpoints; full suite 565/565). The residual — legacy
cardinalities for *unexercised* repository shapes argued by construction
(unchanged id rules) rather than exhaustive fixture coverage — is retained as
a qualification (section E).

### P-DEF-02 — accepted as implemented

The owner accepts:

1. Type-safe formatting of conflicting code properties (`FormatProperty`; no
   property-payload cast in any diagnostic path).
2. Deterministic aggregation of declaration-specific documentation
   (`doc-summary`/`doc-remarks`) on partial types.
3. Preservation of strict comparison for genuine logical-symbol invariants
   (`NodesEquivalent` not globally weakened; genuine conflicts still raise the
   intended `InvalidOperationException`).
4. Preservation of physical declarations (distinct declaration ids, `declares`
   edges, per-part containment).

### Not authorized by this decision

CI-1A implementation; occurrence-storage changes; new public FindUsages or
occurrence APIs; graph schema or codec changes; package versions; build
configuration; CI workflows; package publication or release; push.

## C. Independent qualification basis

Independent reviewer disposition: **`CSHARP-CORRECTNESS-Q1 SUPPORTED WITH
QUALIFICATIONS`** (REPORT.md "Final disposition"). No owner acceptance was
issued by that review. The review re-executed producer claims independently
("no producer evidence accepted without independent re-execution") and
performed ~33 adversarial probe scenarios of its own.

Independently reproduced results (owner acceptance basis):

| Qualification | Result | Reviewer source |
| --- | --- | --- |
| New permanent regressions | 22/22 pass | REPORT §F; REGRESSION-AUDIT §1 |
| C# test suite | 69/69 pass | REPORT §F |
| Full solution | 565/565 pass (27 LatticeDb + 157 TreeSitter + 312 Andirit + 69 CSharp) | REPORT §F |
| Failing-before evidence | 15 of 22 new tests fail on the baseline `5c89ac0` for the intended defects (7 negative guards pass before) | REPORT §F; REGRESSION-AUDIT §2 (per-test failure classes) |
| Original false confirmation (`System.Console.WriteLine` shadow) | Reproduced on baseline and corrected (no edge; `external=1`) in both enumeration orders | REPORT §B items 1–2; FINDINGS-MATRIX row 1 |
| Partial-type failure (`InvalidCastException`) | Reproduced on baseline (q01, q04 both orders, q05, q06, q09, q07-masking, real repo) and corrected | REPORT §C item 1; PARTIAL-TYPE-AUDIT §1 |
| Real three-project extraction (`Andirit.Abstractions`, `Andirit`, `Andirit.CSharp`) | Completes on current; baseline throws before relationship extraction | REPORT §G |
| Real extracted relationships | 9,401 structurally valid edges (2,322 nodes / 2,317 declarations / 56 sources) | REPORT §G |
| Dangling relationship endpoints | 0 (also 0 duplicate node ids, 0 duplicate edge ids) | REPORT §G; FINDINGS-MATRIX §"real-repository qualification" |
| Independent execution platform | Windows (Windows-local only; no cross-platform claim) | REPORT §G; COMPATIBILITY-AUDIT §7 |
| Producer evidence hashes | All 12 SHA-256 values in `final-handoff.md` re-hashed independently: 12/12 match | REPORT §A |

These are bounded observed results, not guarantees of overall product
correctness. The independent review is not producer testing, and this
acceptance record is not an independent review.

### Independent artifact identities

The reviewer's artifacts are preserved outside the repository at
`C:\Users\Laszlos\source\repos\Solo\csharp-correctness-q1\` and are referenced
by path and SHA-256 (re-hashed by this recorder on 2026-10-31; the reviewer's
own hash ledger is `evidence/probe-output-hashes.txt` in the same directory):

| Artifact | SHA-256 |
| --- | --- |
| `REPORT.md` | `27b08e1d0509ed756d2d27ab98d0323cd6aab07ab62923b32048b097c34602e0` |
| `FINDINGS-MATRIX.md` | `e0347afcdd5be356c741d75afe5a7427408946cecc2d19d82e002c77cdcec3bf` |
| `RESOLVER-IDENTITY-AUDIT.md` | `eb7909d9e655c64fc6f4400d667841fcf68f78f5cfa6089911f3cb9f4abe6c5f` |
| `PARTIAL-TYPE-AUDIT.md` | `b92b2189f64428cdc23f169cc11f04e987913d81ff5eed05f6ec6382c3951b7e` |
| `REGRESSION-AUDIT.md` | `bbddf133fd24b7ac3f2a855221446fe6cd8859a581488ce55c1fd507bc855dbc` |
| `COMPATIBILITY-AUDIT.md` | `922058befe8823a6abe81058757077ae6d0a8899785b00fbb52e26974bc3c54f` |
| `OWNER-DECISIONS.md` | `238c8dc5a12474a1768f4b8a52e1c3e93dfbe1a807a2d2fe6415ce013095b155` |
| `REPRODUCE.md` | `1554a6808d9a3b9175bfafdc4acec2798fb848c8a3505a2276473d55659b64b4` |
| `evidence/probe-output-hashes.txt` | `ebb50fb7b67ef2471be4dd53561e50a4124d8d1aa829fc6e6afd7a840213d260` |
| `evidence/q1-baseline-newtests-failing.txt` | `1747fa9c10ec29fb91cee3e573b6cb04f556196daec91c4d80cae177b054b125` |

Original producer evidence and independent reviewer evidence are preserved
unchanged; nothing in this acceptance rewrites either.

## D. Semantic corrections accepted

### 1. False relationships removed (P-DEF-01)

- `mixed` fixture: 45 → 42 edges. The three removed edges are the defect — a
  resolved cross-project `calls` edge from `App.App.Run()` (`src/Z/App.cs`) to
  the platform-bound `System.Console.WriteLine(string)` landing on the
  unrelated shadow declaration in unreferenced `src/A/Shadow.cs`
  (`csharp:symbol:8102d2ac…`), plus two `references` edges to the shadow's
  type/method nodes (`csharp:symbol:3f3042d1…`, `8102d2ac…`). Surviving edge
  ids keep their exact identity (no global rewrite). Independent confirmation:
  REPORT §D; probe p12.

### 2. Valid relationships restored (P-DEF-01 strictness flip side)

- `ambiguous` fixture: 19 → 22 edges. Baseline refused a provable valid
  cross-project call because an unrelated project shared the canonical
  documentation ID; the corrected resolver proves dependency correspondence
  and emits `calls` + 2 × `references` from `App.Program.Run()`
  (`csharp:symbol:a558da59…`) to `Example.Helper.Go()` in `src/Lib`
  (`csharp:symbol:e186634b…`), order-independently (probe p17).
  `ambiguous=1→0`, `crossProjectTargets 0→1`.

### 3. Additional p18 correction (independently discovered; now documented)

The reviewer discovered (probe `p18-local-system-namespace`) a further
instance of the P-DEF-01 defect class that the producer's compatibility
assessment had not enumerated. It is now recorded in
[`compatibility-assessment.md`](../planning/csharp-correctness-01/compatibility-assessment.md)
§5. Summary:

- **Original behavior:** baseline published a resolved cross-project
  `imports` edge (`csharp:1a5ac3d2…`, evidence `src/B/Program.cs:1`) from
  project B's `Program.cs` file node to the `namespace System` node
  (`csharp:symbol:ff9ec621…`) declared in unrelated project A's
  `src/A/Local.cs` — although B does not declare `namespace System;` and does
  not reference A.
- **Corrected behavior:** no edge; the unproven import target is counted
  `Ambiguous` (`imports` coverage in the probe: `edgesEmitted 1→0`,
  `crossProjectTargets 1→0`, `ambiguousTargets 0→1`, `unresolvedTargets
  0→1`).
- **Why it was semantically wrong:** the import binds to the merged `System`
  namespace symbol (no single owning assembly); baseline treated the *absence
  of a competing indexed registration* as proof that the only indexed
  contributor (unrelated project A) was the import's target, asserting a
  cross-project dependency no project reference supports. Uniqueness of
  registration is not identity.
- **Why it is within P-DEF-01's intended exact-definition correction:** the
  same fail-closed resolver gates produce the correction (the unowned-
  namespace gate classifies any indexed contributor as `Ambiguous`); no new
  mechanism, no public-surface change; exactly one unprovable false edge is
  removed and no valid relationship is lost. Relationship identity/cardinality
  changes only by that false edge; surviving edge ids are unchanged.

## E. Preserved qualifications (carried-forward obligations)

Every qualification below is accepted as a documented limit or carried
forward as an obligation. None is silently resolved by this acceptance.

1. **Tested proof boundary — not complete semantic resolution.** The decision
   does not establish complete semantic resolution for every C# compilation
   configuration or every possible metadata-reference topology. The proven
   boundary is the exercised surface: the 22 permanent regressions, the ~33
   independent probe scenarios (original defect, identity collisions, order
   permutations, merged namespaces, transitive bindings, real three-project
   extraction), and the full suite. Repository shapes not represented by the
   suite/probes (e.g. exotic duplicate full-assembly-identity plus linked-file
   plus metadata combinations) are argued fail-closed by construction but were
   not exercised (COMPATIBILITY-AUDIT §8). A lack of *observed* false
   confirmations is not proof that none can exist.
2. **Documentation truncation (2048 characters, un-signaled).** Merged
   `doc-summary`/`doc-remarks` values are bounded to exactly 2048 characters
   with deterministic ordinal ordering and deduplication, and carry **no
   in-band truncation indicator**; a consumer cannot detect elision from the
   value alone. Source documentation beyond the bound is not preserved.
   Accepted as a documented limitation for this milestone; explicit truncation
   metadata is recorded as a possible future improvement (would be a
   schema-affecting change) and is **not** a prerequisite of this acceptance.
   No public property or schema was introduced to address it during
   acceptance recording. (PARTIAL-TYPE-AUDIT §3.3/§3.6; REPORT §I-3;
   [`compatibility-assessment.md`](../planning/csharp-correctness-01/compatibility-assessment.md)
   §6.)
3. **`calls` ambiguity reachability.** Under the corrected resolver, genuine
   `calls` ambiguity is practically unreachable in the qualified cases
   (assembly objects are unique per compilation; canonical keys unique within
   a project). The formerly ambiguous relationship in the retargeted test's
   fixture is now provable and pinned to its exact dependency node. Remaining
   reachable ambiguity paths (registration conflicts, multi-candidate
   fallback, merged-namespace `imports`) stay fail-closed and counted;
   `imports` ambiguity remains reachable and tested. No test was weakened and
   no incorrect ambiguity was resurrected to preserve earlier counter values.
   (REGRESSION-AUDIT §3; REPORT §I-5;
   [`compatibility-assessment.md`](../planning/csharp-correctness-01/compatibility-assessment.md)
   §7.)
4. **Unreachable fallback branch (deferred cleanup).** The second
   `definitionCandidates.Length == 1` check in `TryResolveTarget`
   (`src/Andirit.CSharp/CSharpCodeGraphPlugin.GraphBuilder.cs`, lines
   722–729, immediately after the `targetPaths` branch) is unreachable: the
   identical condition returns earlier (lines 685–692), and control reaches
   line 722 only with two or more candidates, so the intended
   "≥2 candidates, no source locations → Ambiguous" behavior is produced by
   the final `return TargetMatch.Ambiguous;`. Reviewer evidence: REPORT §I-4,
   RESOLVER-IDENTITY-AUDIT §9 item 1, OWNER-DECISIONS item 4 ("cosmetic; no
   behavior impact"). Classified as **deferred cleanup** — no evidence that it
   changes current behavior or correctness; no implementation change was made
   in this acceptance task, and any future cleanup requires its own scoped
   change and review. (Also
   [`compatibility-assessment.md`](../planning/csharp-correctness-01/compatibility-assessment.md)
   §8.)
5. **Platform limitation — Windows-only qualification.** Both producer and
   independent qualification ran on Windows only. Linux and macOS
   qualification requires the hosted CI results on the final published
   commit; until then all cross-platform claims remain suppressed. Known
   OS-sensitive areas: `CSharpProjectDiscovery.PathComparer` case-sensitivity
   and path-provenance matching in the resolver fallback
   (COMPATIBILITY-AUDIT §7; REPORT §I-1). Results are also version-bound to
   the pinned .NET SDK 10.0.x / Roslyn 5.9.0 toolchain.
6. **Operational limits.** Performance, deployment readiness, power-loss/crash
   durability and complete real-world compilation-context coverage are **not
   established**. The real-repo extraction is honest but partial: 2,225
   `calls` and 5,231 `references` unresolved and 303/5,540 external (compile-
   error symbols from the partial three-project view), 502 unsupported,
   `ambiguous=0` — a completed extraction does not establish complete
   semantic resolution (REPORT §G).
7. **Deferred partial-method unification.** Partial-method
   definition/implementation unification remains explicitly deferred and
   unqualified by both producer and reviewer; the documentation-merge policy
   is generic over physical declarations and changes no method-role
   semantics (REPORT §I-7).
8. **Resolver design residuals (reviewer-assessed conservative).** The
   metadata-symbol identity fallback's source-location branch is effectively
   reachable only for exotic duplicate-identity setups given the plugin's
   compilation model (platform-only metadata references +
   `CompilationReference`s), and the merged-namespace local-precedence rule
   preserves baseline behavior; both remain documented reviewer attention
   points, not newly accepted risks (REPORT §I-6;
   [`final-handoff.md`](../planning/csharp-correctness-01/final-handoff.md) §I).

## F. Explicit non-claims

This acceptance does **not** claim:

1. **No CI-1A implementation acceptance.** CI-1A remains unimplemented and
   unauthorized; occurrence storage is unchanged. **ADR-CI1-01 (B2 versus C
   occurrence representation) remains open**
   ([`ci1-scope-01/architecture-decisions.md`](../planning/ci1-scope-01/architecture-decisions.md));
   the separate CI-1-SCOPE-R3 oracle work remains subject to its own
   independent Q4 qualification gate.
2. **No complete C# usage analysis.** The extractor counts unresolved,
   external, ambiguous and unsupported targets honestly; a completed
   extraction (even 9,401 valid edges) does not establish that all semantic
   relationships of a repository are resolved or known.
3. **No general refactoring-safety guarantee.** Absence of a relationship
   from the graph is not evidence of absence of impact.
4. **No package publication or release.** No push, tag, package or release is
   authorized or performed; the three reviewed commits plus the acceptance
   documentation commits await separate publication authorization.
5. **No cross-platform, performance, deployment or durability claims.**
   Windows-local qualification only; see section E items 5–6.
6. **No unconditional correctness claims.** Bounded observed results are not
   guarantees of overall product correctness; absence of observed false
   confirmations is not proof of absence (section E item 1).
7. **No claim that all source documentation is preserved without truncation**
   (section E item 2).

## G. Closure

- **P-DEF-01: ACCEPTED AND CLOSED** (provider-level correction, P-DEF-01b).
- **P-DEF-02: ACCEPTED AND CLOSED** (type-safe diagnostics + deterministic
  documentation aggregation).
- **ANDIRIT-CSharp-CORRECTNESS-01: ACCEPTED AND CLOSED.**

The independently observed qualifications in section E remain **carried-
forward obligations** — owned, documented residuals — not silently resolved
defects. In particular: cross-platform CI qualification on the final published
commit, the un-signaled documentation truncation bound, the deferred fallback
cleanup, and deferred partial-method unification.

This record is committed directly after the independently reviewed HEAD
`92fc99df2323ef1bd8282c6a1cb3b1f79995779b` and intentionally binds to the
reviewed identities above rather than to its own future hash; the exact
acceptance commit and tree hashes are recorded in
[`final-handoff.md`](../planning/csharp-correctness-01/final-handoff.md)
("Owner acceptance record") and in the acceptance session report. No
production code, test, schema, workflow or accepted historical evidence blob
was modified in preparing this record; only the authorized documentation files
were changed.
