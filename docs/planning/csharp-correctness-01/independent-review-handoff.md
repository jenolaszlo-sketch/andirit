# Independent review handoff — ANDIRIT-CSharp-CORRECTNESS-01

**This packet is not self-approval.** The implementing agent (this session)
must not pass the gate; a separate reviewer must verify all ten items below.

## What the reviewer receives

- Canonical checkout `D:\projects\Andirit`, local `main`, implementation
  commit recorded in [`final-handoff.md`](final-handoff.md) (baseline
  `5c89ac0b3d0e87c0112880cf985826ded42b21b2`).
- Production diff: `CSharpCodeGraphPlugin.Symbols.cs`, `...Session.cs`,
  `...GraphBuilder.cs` (only files touched in `src/`).
- Regression diff: `tests/Andirit.CSharp.Tests/CSharpCorrectnessRegressionTests.cs`
  (new), `CSharpCodeGraphPluginTests.cs` (one retargeted test + `partial`
  class declaration).
- Evidence: [`evidence/`](evidence/) (SHA-256 values in
  [`final-handoff.md`](final-handoff.md)).

## Gate checklist (all ten must pass)

1. **Original false external-call confirmation eliminated.**
   Run the shadow regression (both orderings). Expect no `calls`/`references`
   edge from the caller to `src/A/Shadow.cs` symbols; `calls` coverage
   `ExternalTargets≥1, EdgesEmitted=0`.
2. **Exact cross-project binding remains correct.**
   Run rows 2–7, 11 of [`regression-matrix.md`](regression-matrix.md); each
   positive asserts the exact scoped target node id of the true dependency.
3. **Valid aggregate relationship identities remain stable.**
   Rebuild the `cross` fixture via the comparison harness (below) and compare
   SHA-256 against `evidence/comparison/cross.current.json`
   (`bf8681ca…0ab72`); baseline and current must remain byte-identical.
4. **Partial-type documentation no longer causes extraction failure.**
   Rows 1–8 of P-DEF-02 matrix; plus real-repository test (row 11).
5. **Genuine property conflicts remain detectable.**
   Row 9: exact `InvalidOperationException` (not `InvalidCastException`), not
   silently accepted.
6. **Diagnostics cannot fail through incorrect property casts.**
   Inspect `FormatProperty` (`CSharpCodeGraphPlugin.GraphBuilder.cs`): no
   payload casts; all five `CodePropertyValue` variants + unknown-type
   fallback handled; verify no other cast sites exist in diagnostic paths.
7. **Real-repository extraction behaves as reported.**
   Run `ExtractAsync_RealAndiritCSharpPluginPartialDeclarationsExtract` and
   spot-check the `T:Andirit.CSharpCodeGraphPlugin` assertions.
8. **Regression coverage is sufficient.**
   Review the matrix against the assignment's mandatory lists (P-DEF-01 items
   1–14, P-DEF-02 items 1–12); identify any gap and require a follow-up test
   rather than approving with a gap.
9. **No unrelated scope expansion occurred.**
   `git diff 5c89ac0..HEAD --stat` must show only the five files listed in
   [`final-handoff.md`](final-handoff.md); no CI-1A, occurrence storage, query
   API, schema, packaging or TS/JS changes.
10. **Existing public APIs and persistence contracts remain valid.**
    `CSharpPublicApiContractTests` passes; no public API diff; identity
    construction rules unchanged.

## Exact re-verification commands

```powershell
# full suites (expected: CSharp 69, Tests 312, LatticeDb 27, TreeSitter 157 — all pass)
dotnet test D:\projects\Andirit\Andirit.slnx --nologo -v minimal

# targeted defect regressions
dotnet test D:\projects\Andirit\tests\Andirit.CSharp.Tests\Andirit.CSharp.Tests.csproj `
  --filter "FullyQualifiedName~CSharpCodeGraphPluginTests.ExtractAsync_ExternalCallIsNeverConfirmedAsAnUnrelatedDocumentationIdShadow|FullyQualifiedName~ExtractAsync_DocumentedSealedPartialTypeMergesDeclarationDocumentation|FullyQualifiedName~ExtractAsync_GenuineInvariantConflictFailsWithTypeSafeDiagnostic|FullyQualifiedName~ExtractAsync_RealAndiritCSharpPluginPartialDeclarationsExtract"
```

Baseline-vs-current harness (no repo changes; builds baseline copy + current):

```powershell
# baseline copy of the repo at 5c89ac0 (test-only `partial` patch for the class declaration)
# harness: C:\Windows\Temp\opencode\comparison\{baseline,current}\Comparison.*.csproj
dotnet run --project C:\Windows\Temp\opencode\comparison\baseline\Comparison.Baseline.csproj -- cross out\cross.baseline.json
dotnet run --project C:\Windows\Temp\opencode\comparison\current\Comparison.Current.csproj -- cross out\cross.current.json
# hashes must be identical: bf8681cae584e32cca3b932525aaab6959478a3dd42edd73d3f2b17b69b0ab72
```

## Known review risks / where to look hardest

- The resolver's **identity fallback** (metadata symbols) accepts a unique
  definition candidate with full `AssemblyIdentity` + fingerprint when the
  target has no source locations. Reviewer should decide whether this remains
  conservative enough for in-run `CompilationReference`-only usage (in-run
  targets normally hit the exact-compilation gate first).
- The **merged-namespace ambiguity** rule (local node wins when the current
  project declares the name) preserves baseline behaviour; confirm it cannot
  be abused to confirm an external namespace as local.
- The retargeted ambiguous test is a **deliberate semantic change**; confirm
  the replacement genuinely covers "genuine ambiguity never guessed" via the
  merged-namespace and provenance-mismatch paths.
