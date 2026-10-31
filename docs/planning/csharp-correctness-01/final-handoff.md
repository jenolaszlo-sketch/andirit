# Final handoff — ANDIRIT-CSharp-CORRECTNESS-01

**Disposition: `CSHARP-CORRECTNESS-01 READY FOR INDEPENDENT REVIEW`**

(Independent review remains mandatory and has not been performed by the
implementing agent. No push, tag, package or release was made.)

## A. Git / source bindings

| Item | Value |
| --- | --- |
| Repository / canonical checkout | `jenolaszlo-sketch/andirit` / `D:\projects\Andirit` |
| Starting baseline commit | `5c89ac0b3d0e87c0112880cf985826ded42b21b2` |
| Starting baseline tree | `e61994289e5f96203ded37be000b0ed3bb3b79a2` (clean; `main` = `origin/main` = baseline) |
| Implementation commit | `80edf9302d3fa813aebf761db47d16a24667ef11` (tree `2085752eb5c1189f596a0b8baf48662fbf55002a`) |
| Evidence commit | `01365a2213e9d53a11d6ffe2197395c258d9d081` (tree `63e529351cf5c9bc55f9e706eb86df8b92db921a`) |
| Final HEAD | tip of local `main` at delivery (descendant of the evidence commit); exact hash is stated in the delivery report and verifiable via `git rev-parse HEAD` |
| Local vs remote | local `main` is 2 commits ahead of `origin/main`; **not pushed** (no authorization) |
| Worktree model | single checkout, no branches, no linked worktrees created |
| Environment | Windows (win32), .NET SDK `10.0.401`, Roslyn 5.9.0 |

## B. P-DEF-01 root cause and correction

See [`defect-01-analysis.md`](defect-01-analysis.md). Root cause: run-wide
symbol map keyed by canonical documentation ID without defining-assembly or
compilation correspondence; `TryResolveTarget` accepted a globally indexed
method on key match alone. Correction: provenance-carrying `RunSymbols`
registrations (project, compilation-independent definition fingerprint,
declaration paths) plus a fail-closed resolver chain (same-compilation → exact
dependency compilation object → merged-namespace ambiguity → full
`AssemblyIdentity` + fingerprint + declaration-path provenance fallback);
unproven targets are external/ambiguous and counted, never emitted, never
fabricated.

## C. P-DEF-02 root cause and correction

See [`defect-02-analysis.md`](defect-02-analysis.md). Root cause: two layers —
(1) diagnostic cast `modifiers` (`CodeTextProperty`) to `CodeTextListProperty`;
(2) `NodesEquivalent` requiring declaration-specific `doc-summary` agreement
across partial parts. Correction: type-safe `FormatProperty` for all
`CodePropertyValue` variants; explicit property classification with
deterministic, order-independent, bounded (2048) documentation aggregation and
strict invariant agreement; physical declarations preserved; genuine
invariant conflicts still fail with a deterministic diagnostic.

## D. Failing-before evidence

- New permanent regressions executed against an out-of-repo copy of the exact
  baseline commit: **15 failed / 7 passed** of 22 cases
  ([`evidence/failing-before-all-new-tests.txt`](evidence/failing-before-all-new-tests.txt),
  key subset in
  [`evidence/failing-before-key-tests.txt`](evidence/failing-before-key-tests.txt)).
- Baseline-vs-current harness:
  `mixed` → 3 false edges emitted by baseline;
  `partial` → `System.InvalidCastException` from `GraphBuilder.AddSymbol`;
  [`evidence/comparison/*.baseline.json`](evidence/comparison/).

## E. Passing-after evidence

- All 22 new regression cases pass; C# suite 69/69; full solution 565/565
  ([`evidence/passing-after-csharp-summary.txt`](evidence/passing-after-csharp-summary.txt),
  [`evidence/full-suite-results.txt`](evidence/full-suite-results.txt)).
- `partial` fixture: extraction completes with one logical node, merged docs,
  2 physical declarations, honest `ExternalTargets` for the implicit ctor
  ([`evidence/comparison/partial.current.json`](evidence/comparison/partial.current.json)).
- Real-repository qualification:
  [`real-repository-qualification.md`](real-repository-qualification.md).

## F. Identity and compatibility assessment

See [`compatibility-assessment.md`](compatibility-assessment.md). Valid
aggregate identities are byte-stable (`cross` fixture SHA-256 identical
before/after `bf8681ca…0ab72`; surviving-edge IDs stable in the `mixed`
diff). Intentional changes: 3 false edges removed; 3 previously suppressed
valid edges restored (calls `e=0→1`, `ambiguous=1→0`); one test retargeted
with justification; multi-valued documented-partial doc properties now merge
deterministically instead of failing extraction.

## G. Real-repository qualification

See [`real-repository-qualification.md`](real-repository-qualification.md).
Baseline throws `InvalidCastException` on the real three-project input; the
remediated extractor completes it: `T:Andirit.CSharpCodeGraphPlugin` = 1
logical node / 6 physical declarations with the correct merged summary, and
relationship extraction is reached. Windows-only; cross-platform claims
deferred to authorized CI.

## H. Regression and adversarial results

See [`regression-matrix.md`](regression-matrix.md) (mandatory lists 1–14 /
1–12 covered; adversarial probes table; one retargeted pre-existing test
documented).

## I. Remaining findings and limitations

1. **Partial-method definition/implementation unification** remains deferred
   and unqualified (per prerequisite-defects; not claimed as covered).
2. **Cross-platform verification** not executed (Windows-only local); CI-based
   verification is blocked pending publication authorization.
3. **Resolver fallback breadth**: the metadata-symbol fallback (unique
   `AssemblyIdentity` + fingerprint candidate, no source locations to match)
   is accepted as sufficient under in-run `CompilationReference`-only usage;
   flagged for reviewer judgement
   ([`independent-review-handoff.md`](independent-review-handoff.md)).
4. **Merged-namespace local precedence**: when a namespace is declared by both
   the current project and dependencies, the local node wins (baseline
   behaviour preserved); documented for reviewer attention.
5. **Documentation merge truncation** is deterministic ordinal truncation at
   2048 characters; it can cut within a part's text when many parts carry
   long summaries (bounded output takes precedence; rule documented in
   [`defect-02-analysis.md`](defect-02-analysis.md)).
6. No further wider-extraction failures were observed on the three real
   projects; any future failure must be classified separately with preserved
   evidence (not assumed solved by this packet).

## J. Next independent qualification gate

[`independent-review-handoff.md`](independent-review-handoff.md) lists the ten
gate items, exact commands, and the four highest-risk review targets. The
reviewer must re-verify from the commits above; the implementing agent does
not self-approve.

## Changed-file inventory

| File | Change |
| --- | --- |
| `src/Andirit.CSharp/CSharpCodeGraphPlugin.Symbols.cs` | `DefinitionFingerprint`/`TypeIdentity` added (definition identity proof) |
| `src/Andirit.CSharp/CSharpCodeGraphPlugin.Session.cs` | dependency compilation map passed to builder; provenance-carrying `RunSymbols` |
| `src/Andirit.CSharp/CSharpCodeGraphPlugin.GraphBuilder.cs` | exact resolver; declaration-specific documentation aggregation; type-safe diagnostics |
| `tests/Andirit.CSharp.Tests/CSharpCodeGraphPluginTests.cs` | class made `partial`; one test retargeted with justification |
| `tests/Andirit.CSharp.Tests/CSharpCorrectnessRegressionTests.cs` | new permanent regression suite (22 cases) |
| `docs/planning/csharp-correctness-01/**` | this packet + raw evidence |

## Evidence file hashes (SHA-256)

| File | Hash |
| --- | --- |
| `evidence/failing-before-key-tests.txt` | `ae5ae56da71e41bbf2469fa147095137bdb4ee2cb7b2f6fe2d4ccdae16d2c390` |
| `evidence/failing-before-all-new-tests.txt` | `de61f66f30255ce3b86ca61ef1a12130f800a40149ad260decf5909037bc2034` |
| `evidence/passing-after-csharp-summary.txt` | `251ad439f9de4f51bdea87bd8c8a8bcceb2098b4c5d552162d021ee1ce05b9ef` |
| `evidence/full-suite-results.txt` | `1f10623fd64101877c9fbffb0db72d43a45338144ceae479a91db2e98ebfb8a8` |
| `evidence/comparison/cross.baseline.json` | `bf8681cae584e32cca3b932525aaab6959478a3dd42edd73d3f2b17b69b0ab72` |
| `evidence/comparison/cross.current.json` | `bf8681cae584e32cca3b932525aaab6959478a3dd42edd73d3f2b17b69b0ab72` |
| `evidence/comparison/mixed.baseline.json` | `b84ca89c408814502872c54495bef30871f883722b013b6d006e3792724b2002` |
| `evidence/comparison/mixed.current.json` | `735b77a88218aebb9ee1fa9b0b46954570c143fc7624f96d00b4661b4785f52b` |
| `evidence/comparison/ambiguous.baseline.json` | `b2c3720aea799773f53d43b6c358b5c172630220ba5e9d225b9d0bbf668522ec` |
| `evidence/comparison/ambiguous.current.json` | `27680388121ba4393957b3c5a4276aeb035558f93ab59b14a08ed6eb9021b3c5` |
| `evidence/comparison/partial.baseline.json` | `01abcbcdbbb6175147617e735a8d6d7dd60d73fbe2212cf80f64f849d8778dc0` |
| `evidence/comparison/partial.current.json` | `20d426ba316772142b819dd138429824da25d5573c44d73b5c885e32742f8c02` |

## Explicitly not done (unchanged scope controls)

CI-1A implementation; occurrence-level storage; new public FindUsages APIs;
graph schema migrations; unrelated refactoring; compiler infrastructure
replacement; NuGet evaluation; TS/JS changes; sandbox/AAAW changes; release or
package publication; push/tag.
