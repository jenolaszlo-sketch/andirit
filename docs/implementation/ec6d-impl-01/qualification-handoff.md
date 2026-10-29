# Independent qualification instructions

Production source: **093004e02a3438d3c1eb6a668602da4aef563d6a**, tree **89767a4d050aa73248c169a0454ec638c4d7d678**. Accepted baseline
and immutable contracts are bound in [baseline](baseline.md); explicit owner
clarifications are recorded separately. This is producer evidence, not an
independent disposition. The reviewer should check the manually reviewed
expected classifications in the accepted fixtures and tests before executing.

1. On the exact implementation revision, restore/build/test/format with the
   commands in [regression](regression.md). Run
   `dotnet test tests/Andirit.Tests/Andirit.Tests.csproj -c Release --filter FullyQualifiedName~InterfaceImpactTests`
   for the focused public-query suite. Verify the controlled four/five set,
   both occurrence unknowns, wrong-target exclusions, physical ambiguity,
   same-file proof, all closed reasons and complete stored evidence.
2. Reproduce an authentic durable p9 seed by building the baseline revision
   `fe1a9e37da49064f8cf6ec7fe9bc5ade7cfd7781` in the reviewer's environment
   and running [seed/Program.cs](seed/Program.cs) against the unchanged corpus.
   Its project must reference that baseline's runtime, TreeSitter and LatticeDB
   projects; the program refuses any non-p9 TypeScript plugin. Save the database,
   p9 snapshot and build provenance before using p10. Do not manufacture p9 facts
   by relabeling p10. The producer's verified external bundle/binaries/seeds are
   also available at the evidence path in README.
3. On the implementation source, use a new external output directory:

   ```powershell
   dotnet run --project docs/implementation/ec6d-impl-01/qualification/Qualification.csproj -c Release -- <source-root> <p9-seed-directory> <new-output-directory>
   ```

   This copies source/seed, compares all old facts, reindexes, replays into
   memory, reopens LatticeDB, compares results/witnesses, injects failed and
   cancelled follow-ups, deletes a disposable source and reopens again.
   The original source/seed is never edited. Check expected outcomes using the
   accepted reference set, not merely the harness's successful exit.
4. Export the exact website commit with command-local `core.autocrlf=false`;
   compare every blob using `git hash-object --no-filters`, then seed p9 and run
   the same harness. Confirm no direct implementers, ProjectRef's unsupported
   extension, and mandatory ordinary-type-use/.astro coverage. Check the module
   versus imported-name distinction documented in website-regression.md.
5. Reproduce TypeScript supporting observations with the committed Q2/R2 and
   website oracle scripts and an actual TypeScript 5.9.3 installation. These
   observations do not replace Andirit's narrower accepted policy.
6. Run existing hosted Windows, Ubuntu, macOS and package-consumer jobs on the
   exact final implementation/evidence commit. Do not substitute older green
   runs. Verify no NuGet publish workflow or release/tag was invoked.

The reviewer must inspect fail-on-change/no-partial-output behavior,
qualification truth table and authority guard, lower-bound truncation, source
and unit provenance, deterministic witness recomputation, and old facts/presets.
No fixture-scale or Windows result establishes representative performance or
deployment readiness. Independent acceptance and any later release remain
separate owner gates.
