# Producer regression record

Source binding: 093004e02a3438d3c1eb6a668602da4aef563d6a / tree 89767a4d050aa73248c169a0454ec638c4d7d678. Windows, .NET 10.0.401.

Baseline: 391/391. Final local suite: **505 passed, 0 failed, 0 skipped**:
Andirit.Tests 274, CSharp 47, LatticeDB 27, TreeSitter 157. The 114 new tests
exercise public query behavior, both stores, the 37 accepted adversarial cases,
independently selected expected outcomes, integrity/failure paths, lexical
contexts, occurrence coordinates, bounds and qualification.

Commands (from the canonical repository):

```powershell
dotnet restore Andirit.slnx
dotnet build Andirit.slnx -c Release --no-restore
dotnet test Andirit.slnx -c Release --no-build
dotnet format Andirit.slnx --verify-no-changes --no-restore
git diff --check
```

All four existing coverage gates were run with their unchanged CI include
filters and line/branch thresholds: core 55, CSharp 75, TreeSitter 60,
LatticeDB 70. Observed line/branch coverage is recorded in observations.json
and external Cobertura reports. CSharp: 93.63/80.95; TreeSitter: 90.01/76.07;
LatticeDB: 90.49/77.68. The final core report includes the public-query tests.

The initial core coverage attempt failed because feature tests resided in
the TreeSitter suite, outside core's instrumented test invocation. Moving that
suite to the runtime test project corrected measurement and exercised the new
runtime branches; no threshold was lowered and no test duplicated.

All six packages were packed to a local-only feed with a unique smoke package
version. The existing package consumer passed managed providers, three
Tree-sitter native loads, LatticeDB schema 2/reopen/native mirror, snapshot
integrity, exact assembly identities and store conformance. No package was
published and no release/tag was created.

Q2 and R2 oracle commands used the recorded TypeScript 5.9.3 compiler from
the website installation, read-only, with committed oracle scripts. The
website oracle also ran on the verified snapshot. Outputs are external.

Hosted Windows, Ubuntu, macOS and package/native-consumer CI passed on exact
implementation/evidence commit `97c81b784a8b068904c8bfa78df3ab937d5d1a29`:
[run 40955264718](https://github.com/jenolaszlo-sketch/andirit/actions/runs/40955264718).
Every platform passed 274 core + 47 CSharp + 157 TreeSitter + 27 LatticeDB
tests, zero failures/skips, and all unchanged coverage gates. See ci.md.
The user explicitly approved the normal main push and evidence-only follow-ups;
the initial automatic-review rejection was resolved by that approval.
The final evidence-only commit's own CI is verified after it exists and its
exact binding is reported in the final handoff, not borrowed from this run.
No representative performance/operational readiness claim is made.
