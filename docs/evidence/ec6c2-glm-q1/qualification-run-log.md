# EC-6C.2 GLM-Q1 Evidence — Test Execution Log

**Qualification:** EC-6C.2 independent qualification GLM-Q1
**Date:** 2026-10-26
**Environment:** Windows; .NET SDK `10.0.401`; tests target `net10.0`.
**Worktree:** `C:\Users\Laszlos\source\repos\Penghou.Hetu.review-ec6c2-glm-q1`
(branch `qualification/ec6c-2-glm-q1`), created from implementation HEAD
`3ab1f144e31155cf3cd4bac87c4739f0a6eccb7a`.

## Sequence and commands

| # | Step | Command | Result |
| --- | --- | --- | --- |
| 1 | Baseline restore | `dotnet restore Penghou.Hetu.slnx --verbosity minimal` | succeeded |
| 2 | Baseline focused export suite (J1) | `dotnet test tests\Penghou.Hetu.TreeSitter.Tests\Penghou.Hetu.TreeSitter.Tests.csproj --no-restore --filter "FullyQualifiedName~TreeSitterEcmaExport" --verbosity minimal` | **Passed: 25, Failed: 0, Skipped: 0** (matches recorded baseline) |
| 3 | Baseline full solution (J2) | `dotnet test Penghou.Hetu.slnx --no-restore --verbosity minimal` | TreeSitter 156, Hetu 160, LatticeDb 26, CSharp 46 — **388 passed, 0 failed, 0 skipped** (matches recorded baseline) |
| 4 | Format verification (J3) | `dotnet format Penghou.Hetu.slnx --verify-no-changes --no-restore` | no changes required |
| 5 | Whitespace (J4) | `git diff --check` | clean |
| 6 | Qualification matrix committed | commit `568ea7f` | — |
| 7 | Independent qualification tests written and run | `dotnet test … --filter "FullyQualifiedName~TreeSitterEcmaExportQualificationQ1"` | **27 passed, 0 failed, 0 skipped** (final; intermediate failures were test-authoring defects in the qualification harness itself, corrected and re-run — see "Failed attempts") |
| 8 | Full TreeSitter project with qualification tests | `dotnet test tests\Penghou.Hetu.TreeSitter.Tests\… --no-restore --verbosity minimal` | **183 passed, 0 failed, 0 skipped** |
| 9 | Qualification tests committed | commit `609af02` | — |
| 10 | p8 seed harness run | `dotnet run --project <harness\seed-p8> -- <workspace>` (against detached `8c1d5e3` checkout) | p8 store persisted: 7 nodes / 4 declarations / 12 edges per fixture, plugin versions `…p8` |
| 11 | p9 reindex + comparison harness | `dotnet run --project <harness\reindex-p9> -- <workspace> <report.json>` | p9: 12/4/17 per fixture; +5 entries, +5 edges; existing facts byte-identical (both languages); resolver unchanged; unit origins p9; close/reopen queries verified |
| 12 | Website smoke harness | `dotnet run --project <harness\website-smoke> -- <report.json>` (website clone verified clean at `f9cca17`) | 9 units, 33 entries, 33 edges, no warnings, 0 `.astro` facts |
| 13 | Fixture extraction audit probe (H1) | direct `TreeSitterTypeScriptPlugin` extraction of the provider-test fixture | 7 export edges (export-surface provider), 0 resolves-to edges, all other edges provider `…typescript` version `…p8` |

## Failed attempts and remediations (qualification-harness authoring)

These were defects in the qualification test code, not in production code;
each was corrected and the suite re-run to the final green state:

1. First run: 6 failures — a wrong expectation that changing the internal
   construct span changes the occurrence identity (the contract says it must
   not), a wrong expectation that `LocalName` vs `LocalAlias` internal forms
   produce different identities (both publish `export-via=local`, so equal
   identity is correct), use of a mismatched file-node ID against
   `TreeSitterTestSupport.ExtractSyntax` (which publishes internally against
   `typescript:file:shape-test`), and a wrong multiline end-line expectation.
2. Second run: 3 failures — a movement fixture that kept the token span
   identical (the edge identity therefore correctly did not change) and
   collision snapshots captured before the deliberate conflicting replacement.
3. A C# compiler error (duplicate local name) was introduced while applying
   fix 2 and corrected.
4. Environmental failure: the system disk reached 0 bytes free during the
   website smoke build ("There is not enough space on the disk"); ~2.2 GB of
   disposable harness build output (probe, seed, reindex, smoke `bin`/`obj`)
   was deleted and the smoke was re-run successfully. No repository state was
   affected.
5. The website smoke was first run over the raw working directory, which
   additionally indexed four gitignored generated files under `.astro/`
   (36 occurrences). The harness was corrected to copy only the git-tracked
   supported files (mirroring a pristine checkout) and re-run: 9 units,
   33/33 occurrences, 0 `.astro` facts.

No production code was modified at any point during qualification.

## Final executed results

- Baseline reproduction: focused 25/0/0; full solution 388/0/0; format and
  whitespace clean.
- Independent qualification suite: 27/0/0 (committed at `609af02`).
- TreeSitter project including qualification tests: 183/0/0.
- Migration reproduction and website smoke: see
  `migration-p8-to-p9-report.md` and `website-smoke-inventory.md`.
