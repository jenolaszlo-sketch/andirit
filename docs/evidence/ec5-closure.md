# EC-5 closure record

**Project:** Penghou.Hetu (future Andirit)
**Work package:** EC-5 closure following the independent `EC5-GLM-Q1` review
**Date:** 2026-10-23
**Scope:** P3 corrections, evidence preservation, documentation reconciliation, and
integration readiness. No production extractor or graph-contract behavior changed.

---

## A. Git binding

| Item | Identity |
| --- | --- |
| Repository | `jenolaszlo-sketch/penghou-hetu` |
| Canonical checkout | `C:\Users\Laszlos\source\repos\Penghou.Hetu` |
| Baseline (canonical `main`) | `1cc2939ea615d627cde616d06e739867be4e56e2` (tree `d97170219fe930fe2e1921853be479e39675bb0d`) |
| EC-5 implementation commits | `c7657f8a5be864d48e7d71815b3b2a83a7652ea8` (extractor) → `e77deeab0f5f53d7f181519999e8b55317b345df` (tests) → `5a7c59c4b0f783980ef7301069edf1a8fba7ecc2` (docs) |
| Qualified implementation tip | `5a7c59c4b0f783980ef7301069edf1a8fba7ecc2`; tree `153a426ac8925d52b6f37cd7089872a86b444c97` (matches the EC5-GLM-Q1 binding) |
| Implementation branch | `implementation/ec5-variable-declarations` = `5a7c59c` |
| Closure worktree | `C:\Users\Laszlos\source\repos\Penghou.Hetu.closure-ec5` |
| Closure branch | `closure/ec5-qualified-integration`, created from exactly `5a7c59c`, direct descendant of the qualified implementation |
| Maintenance commits | `78f22a2` (test, F2) → `dcdac46` (docs, F1/F3/F4) → `cfa0893` (EC-4 guidance) → `0da1f56` (closure status) |
| Pre-report closure tree | `1b52cb4cb985757e90f0c37bbd5c6f42a71379bb` (tree of `0da1f56`) |
| Closure report commit | This document is committed as the tip of `closure/ec5-qualified-integration`. A commit cannot contain its own hash; the exact report commit and final canonical tree are recorded in the closure final report. |

Ancestry was verified before any change: `1cc2939` → `c7657f8` → `e77deea` →
`5a7c59c` is linear, and the qualified tree equals `153a426a`. The closure
branch was created from the implementation tip, not from `main`, and no
existing branch, worktree, or commit was modified, rebased, or deleted.

## B. Independent qualification

The independent reviewer verdict for EC-5 is **VERIFIED WITH QUALIFICATIONS**
(`EC5-GLM-Q1`, review date 2026-10-23). No P0, P1, or P2 defects were reported.

Preserved review binding:

| Item | Identity |
| --- | --- |
| Review branch | `review/ec5-glm-q1` |
| Review preservation commit | `1cad44ba11838bcefe54ed1f45e2f9b5bafcbba4` (tree `9e218aa86c696df6544fc02209b9e4c08221060c`), parent `5a7c59c` |
| Report | `docs/reviews/ec5-glm-q1-variable-declaration-verification.md` |
| Review-only additions | `docs/evidence/review-ec5-glm-q1/` (p5/p6 fixture dumps, p5→p6 migration), `tests/Penghou.Hetu.TreeSitter.Tests/ReviewEc5GlmQ1VariableProbes.cs` (REVIEW-ONLY) |

The review commit adds only review evidence; it was never merged. The review
report was not rewritten; it continues to describe `5a7c59c` and its own
CRLF-worktree failure of `EC5-GLM-F2` exactly as found. The post-closure
maintenance commits are bound and verified independently in §C–G below.

Independently reproduced qualification evidence:

- 21/21 website declarations discovered; 11/11 directly exported.
- Zero false positives or false negatives within the evaluated subset.
- 18/18 independent adversarial probes passed.
- Authentic p5→p6 migration (real p5 extractor built from `1cc2939`).
- Pre-existing invocation relationships byte-identical (imports 9, inherits 1,
  invokes-name 101 unchanged).
- No material correctness defects reported.

## C. F1 disposition — identity hash casing

**Finding:** the implementation handoff described identity hashes as
“uppercase-hex”; actual output is lowercase.

**Source evidence (unchanged, current closure tree):**
`src/Penghou.Hetu.TreeSitter/EcmaScriptExtractionProfile.cs:1110-1111` renders
every hash with `Convert.ToHexStringLower(SHA256.HashData(...))`, and the
variable identity is built at lines 334–336 as
`{language}:{lowercase-hash}` (symbol) and `{language}:symbol:{lowercase-hash}`
(node). The review's probe P9 verified the lowercase formula against emitted
IDs.

**Correction (documentation only):**

- `docs/evidence/ec5-variable-declarations-handoff.md` §B: `uppercase-hex` →
  `lowercase-hex`, with an explicit note that the earlier wording was wrong and
  was corrected during closure.
- `docs/ecmascript-symbol-identity.md`: the audited model now states every hash
  renders as lowercase hexadecimal; §EC-5 now spells out the symbol ID, node
  ID, and hash casing explicitly.

No production identity formatting changed; no identity was introduced; no
reindexing is required. The existing duplicate-declaration, discriminator,
path/name input, and stability documentation was checked and remains accurate.

## D. F2 disposition — line-ending-sensitive test

**Root cause:** `TreeSitterEcmaVariableTests.
ExtractAsync_ModelsTypeScriptFileVariablesWithWrittenModifiersAndLocations`
asserted `EndColumn == source.Split('\n')[3].Length`. The raw string literal in
the test source adopts the checkout's line endings, so under
`core.autocrlf=true` the captured line ends with `\r` and the expected value
became 39 while the extractor's correct exclusive-end column stays 38. The
committed blob is LF; the test was correct only on LF checkouts.

**Pre-fix failure reproduced in the closure worktree (CRLF, `w/crlf`):**

```text
dotnet test tests/Penghou.Hetu.TreeSitter.Tests/Penghou.Hetu.TreeSitter.Tests.csproj
  --filter "FullyQualifiedName~ExtractAsync_ModelsTypeScriptFileVariablesWithWrittenModifiersAndLocations"
Assert.Equal() Failure: Values differ
Expected: 39
Actual:   38
  at ...TreeSitterEcmaVariableTests.cs:line 52
Failed! - Failed: 1, Passed: 0, Skipped: 0, Total: 1
```

**Correction (test only):** the assertion now derives the expected location
from the actual fixture content — it locates the declarator text
`projects: Project[] = []` in the fixture line and verifies
`StartColumn = found-column` and
`EndColumn = StartColumn + declarator length` (the documented one-based,
inclusive-start/exclusive-end span contract). No line terminator participates,
so no checkout format is assumed. The test was not disabled, and its location
assertions were strengthened (span start and end are both checked against the
declarator, not just the line length). No production source-location semantics
and no parser behavior were touched.

**LF/CRLF verification** (disposable detached worktrees at the same commit,
created with explicit configuration; line endings verified by content, not only
by config):

| Environment | Creation | Fixture EOL evidence | Focused test |
| --- | --- | --- | --- |
| CRLF | `git -c core.autocrlf=true worktree add --detach … 0da1f56` | `git ls-files --eol` = `i/lf w/crlf`; byte scan: 252 CRLF pairs / 252 LF | 1 passed / 0 failed |
| LF | `git -c core.autocrlf=false worktree add --detach … 0da1f56` | `git ls-files --eol` = `i/lf w/lf`; byte scan: 0 CRLF pairs / 252 LF | 1 passed / 0 failed |

Both environments also passed the full solution suite at `0da1f56`: 322/322
(see §G).

## E. Accepted limitations

**F3 — `field` node-kind reuse (accepted P3 limitation).** File-level variables
reuse the existing generic `field` node kind. ECMAScript class fields are
owner-derived `property` members whose member-kind discriminator is `field`, so
identities never converge, but the node kind alone does not distinguish a
module variable from a class member. Consumers must use ownership (`contains`
from the file node versus the class node) and the `variable` symbol
discriminator. The representation is deliberately unchanged; a dedicated kind
would be a public-contract change not justified by any observed failure.
Documented in `docs/ecmascript-symbol-identity.md` (§EC-5),
`docs/evidence/ec5-variable-declarations-handoff.md` (§G), and the feature
matrix `declaration.field` row.

**F4 — ambient `declare` declarations (accepted P3 limitation).** Ambient
declarations such as `declare const externalValue: string;` are outside the
supported EC-5 subset: the profile models no ambient/global scope anywhere, so
the `ambient_declaration` wrapper is never walked at file level. No ambient
extraction was added during closure. Documented in the same three locations.

Both are preserved as future design considerations, not unresolved defects.

## F. Documentation reconciliation

- **EC-4 consumer guidance transferred.** `15b63ce` on
  `review/ec4-real-repository-qualification` adds
  `docs/consumer-guidance-qualified-name-lookup.md`. That documentation change
  (and only it) is incorporated into the closure branch at `cfa0893`. The EC-4
  report link was converted to the preserved Git binding (`review/ec4-…` at
  `1fbc113`) because review reports remain on their independent branch; the
  EC-4 report and its `review/ec4/harness/**` + evidence files were **not**
  transferred.
- **EC-5 reconciliation in the guidance.** The transferred document now states
  that EC-5 variables are declared, `SymbolId`-bearing `field` symbols; that
  class fields are `property` members with member-kind `field`; that a
  function-valued variable (e.g. `GET`) legitimately returns a `field` and a
  `callable` under one display name; that all identity hashes are lowercase
  hexadecimal; and that a written `export` modifier is not an export binding.
  The table row “semantically resolved symbol” was renamed “Symbol-bearing
  declaration” with an explicit “not a compiler-resolution claim”, keeping the
  documentation truthful for structural Tree-sitter symbols.
- **No review-only material entered the closure.** Verified at the closure tip:
  no `docs/reviews/`, no `ReviewEc5GlmQ1VariableProbes.cs`, no
  `review/ec4/**`, and no `.csproj` differences versus the baseline.
- **Milestone status documentation** updated in `docs/language-support-roadmap.md`
  (closure table), `docs/language-feature-matrix.md` (post-audit updates,
  `declaration.field`, finding 1), `ROADMAP.md` (Milestone 8B and generated
  plugins), and `CHANGELOG.md`.
- **Evidence preservation verified:** EC-3 Q1 independent review at `ba2ca83`
  (`review/ec3-glm-q1`); EC-3D Q2 remediation verification at
  `1287590ee0c46bd863873d30baea633c3b04ef13` (`review/ec3-glm-q2`, also
  preserved at `9f39eac` on `preservation/ec3-glm-q2-heritage-remediation`);
  EC-4 qualification at `1fbc113` plus guidance at `15b63ce`
  (`review/ec4-real-repository-qualification`); EC-5 handoff at `5a7c59c`; EC-5
  GLM qualification at `1cad44b`. None of these branches was merged or
  rewritten.

## G. Verification

Environment: Windows 11 Home Single Language 10.0.26200 (64-bit); .NET SDK
10.0.401; git 2.55.0.windows.3; no `.gitattributes`; global
`core.autocrlf=true` (hence the closure worktree itself is a CRLF checkout).

| Check | Command (working directory) | Result |
| --- | --- | --- |
| Focused EC-5 tests, pre-fix (CRLF) | `dotnet test …TreeSitter.Tests.csproj --filter FullyQualifiedName~ExtractAsync_ModelsTypeScriptFileVariablesWithWrittenModifiersAndLocations` (closure worktree) | 1 failed (F2 reproduced: 39 vs 38) |
| Focused EC-5 tests, post-fix (CRLF) | same command, closure worktree | 8/8 passed |
| Focused affected test (CRLF) | same command, `closure-crlf` worktree at `0da1f56`, `w/crlf` | 1/1 passed |
| Focused affected test (LF) | same command, `closure-lf` worktree at `0da1f56`, `w/lf` | 1/1 passed |
| Full solution, CRLF | `dotnet test Penghou.Hetu.slnx --verbosity minimal` (closure worktree and `closure-crlf` worktree) | 322/322 passed (core 160, Tree-sitter 90, C# 46, LatticeDb 26) |
| Full solution, LF | same command, `closure-lf` worktree | 322/322 passed (same split) |
| Formatting | `dotnet format Penghou.Hetu.slnx --verify-no-changes --no-restore --verbosity minimal` | exit 0, no changes |
| Git whitespace | `git diff --check` | exit 0, clean |
| Production unchanged | `git diff --name-status 5a7c59c 0da1f56` | docs + one test file only; no `src/**` change |
| Review isolation | `git merge-base --is-ancestor review/ec5-glm-q1 closure/…`, same for `review/ec4-…` | not ancestors (branches remain separate) |
| Working trees | `git status` in closure and disposable worktrees | clean at commit time |

No production extractor or graph-contract behavior changed: the only
non-documentation edit is the F2 test, and the profile revision remains 6 with
byte-identical identity formatting. No reindexing is required for the closure
changes.

## H. Remaining work

- **EC-6:** design pending, implementation not authorized. No scope or
  language is committed.
- **Astro support:** separate future work; `.astro` files remain unindexed and
  were never part of the EC-5 subset.
- **Cross-platform determinism:** not executed on non-Windows hosts; all
  closure evidence is Windows x64 (same boundary as the qualification).
- **Publication (not authorized here):** local `main` is ahead of
  `origin/main`; the qualified implementation, review evidence, and closure
  commits are ready for a separately authorized push. Exact identities are
  listed in the closure final report.
- The review branches and worktrees remain in place and must not be deleted
  automatically.

## I. Closure decision

All acceptance criteria hold: F1 corrected, F2 passes under LF and CRLF, F3/F4
documented as accepted limitations, independent qualification evidence
preserved with its original Git bindings, EC-4 consumer guidance incorporated,
EC-3D/EC-4/EC-5 status accurate, no production graph behavior changed, full
tests and formatting pass, and no review-only material was merged.

The qualified implementation boundary remains exactly
`5a7c59c4b0f783980ef7301069edf1a8fba7ecc2` (tree `153a426a`) as reviewed by
GLM; the closure corrections are bound to `78f22a2`, `dcdac46`, `cfa0893`,
`0da1f56`, and this report commit, and are documentation-only and test-only.

**Disposition: EC-5 CLOSED — INDEPENDENTLY VERIFIED; P3 MAINTENANCE RESOLVED;
QUALIFICATION EVIDENCE PRESERVED.**
