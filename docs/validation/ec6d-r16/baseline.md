# EC6D-R16 baseline

## Andirit repository

| Item | Value |
| --- | --- |
| Repository | `https://github.com/jenolaszlo-sketch/andirit` |
| Canonical checkout | `D:\projects\Andirit` |
| Starting commit (`main`) | `be85ac6b89399095491fb8adb50fa1f247faba9d` |
| Starting tree | `d5018a52d21523d4ab6ad65541d97517a5a5f27c` |
| Remote tracking | `main...origin/main` at `+0 -0` at start |
| Worktrees | exactly one (the canonical checkout); no linked worktrees |
| Working tree at start | clean |
| Regression baseline | 391 passing tests (re-verified in this milestone: 160 + 47 + 27 + 157) |

Both the starting commit and tree match the milestone expectation; `main`
had not advanced, so no interim changes required inspection.

The accepted standalone baseline contains the integrated EC-6B (relative
module resolution, `resolver2.relative-v1`) and EC-6C (p9 structural export
surface) work, in-memory and LatticeDB stores, compatible historical Hetu
schema-2 stores, and six validated package consumers. EC-6D remains
specification-only: `GetInterfaceImpactV1` and its facts A–E are not
implemented, which this milestone preserves.

## Website repository

| Item | Value |
| --- | --- |
| Repository | `https://github.com/jenolaszlo-sketch/website` |
| Existing checkout | `C:\Users\Laszlos\source\repos\website` (not modified) |
| Current checkout HEAD | `f9cca1773a32bb517e624cc380e1f6d14e0cc317` (equals the pinned baseline) |
| Pinned qualification commit | `f9cca1773a32bb517e624cc380e1f6d14e0cc317` |
| Snapshot used | `git archive` of the pinned commit, extracted outside Andirit (`…/Temp/opencode/ec6d-r16/website-pinned`) |
| Snapshot verification | all 78 files re-hashed with `git hash-object` and compared to the pinned tree: 78 checked, 0 mismatches (log: `website-snapshot-verification.txt` in the session evidence store) |
| Original checkout status after use | clean, HEAD unchanged |

The pinned commit was used exactly; the checkout HEAD is currently identical
to it, and the comparison below is bound to the pinned commit, not to a
moving branch.

## Environment

| Item | Value |
| --- | --- |
| OS | Windows 11 Home Single Language, 10.0.26200 |
| .NET SDK | 10.0.401 |
| Node.js | v24.21.0 |
| Git | 2.55.0.windows.3 |
| Website TypeScript (oracle) | 5.9.3 from the website checkout's local `node_modules` (read-only) |
| Andirit TypeScript plugin | `0.2.0-preview.6+v0.23.2.abi14.p9` |
| Andirit JavaScript plugin | `0.2.0-preview.6+v0.25.0.abi15.p9` |
| Store used for evidence runs | `InMemoryCodeGraphStore` (read-only indexing; no databases written) |

## Harness adaptation (only rename-related change)

The controlled-corpus analysis harness
(`docs/planning/ec6d/analysis/ConsumerBaseline.csproj`) still referenced the
pre-rename projects `src/Penghou.Hetu` and `src/Penghou.Hetu.TreeSitter` and
the pre-rename namespace `Penghou.Hetu`. The only changes applied:

- project references updated to `src/Andirit/Andirit.csproj` and
  `src/Andirit.TreeSitter/Andirit.TreeSitter.csproj`;
- the single `using` directive updated to `using Andirit;`.

No query, assertion, output shape or examined fact was changed. The harness
still compiles against the public APIs of the standalone Andirit projects,
and its output for the unchanged corpus reproduces the committed baseline
facts exactly (see [controlled-fixture.md](controlled-fixture.md)).

## Reproduction commands

Controlled corpus (from the repository root):

~~~powershell
dotnet restore docs/planning/ec6d/analysis/ConsumerBaseline.csproj --ignore-failed-sources -m:1 -p:RestoreUseStaticGraphEvaluation=true -p:NuGetAudit=false
dotnet build docs/planning/ec6d/analysis/ConsumerBaseline.csproj --no-restore -m:1
dotnet run --no-build --project docs/planning/ec6d/analysis/ConsumerBaseline.csproj -- docs/planning/ec6d/corpus/ts-interface-impact
~~~

Pinned website snapshot and harness:

~~~powershell
git -C C:\Users\Laszlos\source\repos\website archive f9cca1773a32bb517e624cc380e1f6d14e0cc317 -o $env:TEMP\website-pinned.tar
mkdir $env:TEMP\website-pinned
tar -xf $env:TEMP\website-pinned.tar -C $env:TEMP\website-pinned
dotnet restore docs/validation/ec6d-r16/harness/WebsiteBaseline/WebsiteBaseline.csproj --ignore-failed-sources -m:1 -p:RestoreUseStaticGraphEvaluation=true -p:NuGetAudit=false
dotnet build docs/validation/ec6d-r16/harness/WebsiteBaseline/WebsiteBaseline.csproj --no-restore -m:1
dotnet run --no-build --project docs/validation/ec6d-r16/harness/WebsiteBaseline/WebsiteBaseline.csproj -- $env:TEMP\website-pinned
~~~

Website TypeScript oracle:

~~~powershell
node docs/validation/ec6d-r16/tools/website-ts-oracle.cjs $env:TEMP\website-pinned C:\Users\Laszlos\source\repos\website\node_modules\typescript\lib\typescript.js
~~~

## Test execution

All four test projects were run on the starting commit after the harness
adaptation and the new validation assets were added (no production source or
test source changes):

| Project | Result |
| --- | --- |
| `Andirit.Tests` | 160 passed, 0 failed, 0 skipped |
| `Andirit.CSharp.Tests` | 47 passed, 0 failed, 0 skipped |
| `Andirit.LatticeDb.Tests` | 27 passed, 0 failed, 0 skipped |
| `Andirit.TreeSitter.Tests` | 157 passed, 0 failed, 0 skipped |
| **Total** | **391 passed, 0 failed, 0 skipped** |

This matches the documented 391-test baseline; no coverage or assertion was
weakened.
