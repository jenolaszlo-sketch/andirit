# EC-6D planning evidence index

## Git and worktree bindings

| Item | Binding |
| --- | --- |
| Repository | jenolaszlo-sketch/penghou-hetu; local repository is an isolated mirror/worktree under C:\Users\Laszlos\source\repos\Solo |
| Planning branch | planning/hetu-ec6d-consumer-intelligence |
| Planning worktree | C:\Users\Laszlos\source\repos\Solo\hetu-ec6d-consumer-intelligence |
| Assessment baseline | 2c3090cba6d8a56102a5c47ceb026c5b5fea3234, tree ece772f1f2b2497891f4019d0d85f4d852b197e1 |
| Baseline evidence commit | 1c9f4c9f12e82fe35261096ff3c96343336bdb63, tree 31486cdea39dee66d1552a4e4af058a59dc4698e |
| Corpus tree | 014da71f0855cdc957fa9b6a0667bca565145092 |
| Canonical main | 558f8ac82ac1b5e07da68f4a71a70240eaf38409, tree 08a01a1b7d5d3109b991bcd05d57272fd3f5f018 |

The final planning commit and final tree are reported in the task handoff.
The fixture and harness commit is the immutable reference for the measurements
below. The analysis files are non-production and do not alter extraction or
query behavior.

## Artifact map

| Artifact | Purpose |
| --- | --- |
| integration-readiness.md | EC-6C integration gate and owner authorization boundary |
| integration-delta.md | Complete 65-path main-to-candidate delta manifest |
| current-capability-inventory.md | Evidence-based provider, graph, query, persistence and operational inventory |
| consumer-use-cases.md | UC-1 through UC-5 consumer requirements |
| consumer-baseline.md | Fixture identity, manual expected set, observed pipeline and relationship-specific scores |
| gap-analysis.md | G1 through G7 priorities and candidate resolutions |
| scope-proposal.md | Candidate comparison, bounded EC-6D recommendation and deferred work |
| acceptance-contract.md | Proposed functional, correctness, provenance, determinism, persistence, compatibility and failure requirements |
| risk-register.md | Retained EC-6C qualifications plus EC-6D planning risks |
| corpus/ts-interface-impact/ | Thirteen TypeScript sources and fixture README |
| analysis/ConsumerBaseline.csproj and Program.cs | Reproducible read-only indexing/query harness |
| analysis/observed-results.json | Observed graph facts and provenance |
| analysis/*-test-run.txt | Completed project test-suite outputs |

## Commands and results

### Git binding and preservation

Before drafting the inventory, the following read-only checks were performed
against the local isolated repository:

- Full commit/tree/parent binding for canonical main, EC-6B owner acceptance,
  EC-6C implementation, independent qualification and owner acceptance.
- Ancestry check from canonical main to EC-6C owner acceptance.
- Merge-tree comparison of main with the owner-accepted candidate; no
  conflicts were reported.
- Diff inventory of main to owner acceptance: 65 files (12 source, 12 tests,
  38 docs, 3 root project documents); no C# provider, core query service or
  LatticeDB provider delta.
- Diff of EC-6C implementation to owner acceptance under src and tests:
  empty.
- Read-only status checks for main and the existing implementation,
  qualification and owner-acceptance worktrees: clean at their authoritative
  commits. The planning worktree began at the owner-accepted commit.

The exact SHAs, trees, dependencies and limitations are in
[integration-readiness.md](integration-readiness.md).

### Baseline reproduction

Run from the repository root:

~~~powershell
dotnet restore docs/planning/ec6d/analysis/ConsumerBaseline.csproj --ignore-failed-sources -m:1 -p:RestoreUseStaticGraphEvaluation=true -p:NuGetAudit=false
dotnet build docs/planning/ec6d/analysis/ConsumerBaseline.csproj --no-restore -m:1
dotnet run --no-build --project docs/planning/ec6d/analysis/ConsumerBaseline.csproj -- docs/planning/ec6d/corpus/ts-interface-impact
~~~

The harness was executed twice. Both runs matched:

- IndexIdentity: 667ec2d4df6b821e63b2f7d1adad6c63492aaf453b9af639f1aa6e9b0fa77ec8
- CanonicalFactsSha256:
  05bf7c7de0e5e1a552c77759cc3bba39aff8d72e093405379e5acfd2d99b0d3c

SnapshotIntegrityHash is publication-specific and is not the comparison
digest. The file-system provider reports PublicationConsistent=false, so the
run does not prove immutable source snapshot isolation.

### Existing regression suites

Each project suite completed as a separate dotnet test invocation against
the EC-6C owner-accepted candidate. To reproduce after the restore command for
the harness, run each project separately:

~~~powershell
dotnet test tests/Penghou.Hetu.Tests/Penghou.Hetu.Tests.csproj --no-restore -m:1
dotnet test tests/Penghou.Hetu.CSharp.Tests/Penghou.Hetu.CSharp.Tests.csproj --no-restore -m:1
dotnet test tests/Penghou.Hetu.LatticeDb.Tests/Penghou.Hetu.LatticeDb.Tests.csproj --no-restore -m:1
dotnet test tests/Penghou.Hetu.TreeSitter.Tests/Penghou.Hetu.TreeSitter.Tests.csproj --no-restore -m:1
~~~

| Project | Result | Output |
| --- | ---: | --- |
| Penghou.Hetu.Tests | 160 passed, 0 failed, 0 skipped | analysis/core-test-run.txt |
| Penghou.Hetu.CSharp.Tests | 46 passed, 0 failed, 0 skipped | analysis/csharp-test-run.txt |
| Penghou.Hetu.LatticeDb.Tests | 26 passed, 0 failed, 0 skipped | analysis/latticedb-test-run.txt |
| Penghou.Hetu.TreeSitter.Tests | 156 passed, 0 failed, 0 skipped | analysis/treesitter-test-run.txt |
| **Total** | **388 passed, 0 failed, 0 skipped** | Separate project runs |

NuGet audit lookup was disabled because online vulnerability data was not
available and NU1900 is treated as an error. The sandboxed one-shot solution
run encountered a testhost connection timeout; a subsequent elevated
one-shot run hit the disk limit. The separate project runs completed
successfully. Build outputs were later removed from the planning worktree.

## Historical qualification evidence

The planning exercise did not modify qualification evidence. The owner decision
and independent reviewer materials remain at their historical commits.
Relevant retained facts include p6-to-p8 migration evidence for EC-6B,
p8-to-p9 evidence for EC-6C, the unreproducible historical EC-6C migration
hash, Windows-only independent qualification, a small pinned website smoke,
and no performance claim. See the owner decision and
[risk-register.md](risk-register.md).

## Preservation and authorization

- EC-6C was not integrated.
- EC-6D was not implemented.
- Track B was not started.
- No package was published; no release or tag was created.
- Canonical main remains unchanged.
- Existing implementation, qualification and owner-acceptance worktrees were
  preserved; no merge, push, rebase or reset was performed.
- This branch contains documentation, test output evidence, the committed
  fixture and the non-production analysis harness only.

## R1 evidence (EC6D-PLAN-R1)

### Git bindings verified during recovery

| Check | Command | Result |
| --- | --- | --- |
| Planning commit exists | `git cat-file -t 91a3ed53a256e89ed71d7d11c204252c417400a6` (mirror) | `commit` |
| Planning tree matches | `git rev-parse 91a3ed5…^{tree}` | `d79e5814485040a36c77bcde332ebc937f312476` |
| Worktree HEAD/branch | `git rev-parse HEAD; git branch --show-current` (original worktree) | `91a3ed5…`, `planning/hetu-ec6d-consumer-intelligence`, clean |
| Connectivity | `git fsck --connectivity-only` (mirror) | clean |
| Ancestry | `git merge-base --is-ancestor 2c3090c 91a3ed5` / `558f8ac 91a3ed5` | both true |
| Change surface | `git diff --name-status 2c3090c..91a3ed5` | 33 added paths, all `docs/planning/ec6d/`; src/tests diff empty |
| Review branch import | `git fetch <Q1 worktree> review/hetu-ec6d-scope-q1:refs/heads/review/hetu-ec6d-scope-q1` (mirror, additive) | new ref at `d2bb7b1c6d2c760ed044ad31994a90d8665fd0dc`; content commit `5ba43e1cd23871c55e01d9201a492a32dff09ae6` present |
| Remediation branch | branch `planning/hetu-ec6d-plan-r1` created at `91a3ed5`; worktree `C:\Users\Laszlos\source\repos\Solo\hetu-ec6d-plan-r1` | clean checkout of the recovered tip |

### Independent baseline reproduction (R1)

Commands identical to "Baseline reproduction" above, executed in the R1
worktree; output committed as `analysis/r1-reproduction-run.txt`:

- Relationship counts, node/edge identity sets: **bit-identical** to the
  committed observation (64 nodes, 99 edges, 14 units, 23 declarations).
- `FindImplementations` on the selected declaration: 0 relationships
  (reproduces the committed claim).
- `PublicationConsistent=false` (reproduces).
- `IndexIdentity` / `CanonicalFactsSha256` differ from the committed values
  (`5c866e16…` / `631c0ab3…` vs `667ec2d4…` / `05bf7c7d…`): file-node
  `content-hash` properties hash working-tree bytes, and this environment
  checks out CRLF (`core.autocrlf=true`) where the original preserved LF.
  `git hash-object` of the R1 working file equals the committed blob hash —
  committed content is byte-identical; only on-disk normalization differs.
  Digest comparisons are valid within byte-identical checkouts only.
  Analysis: [recovery-and-provenance.md](recovery-and-provenance.md) §6.

### Q1 review evidence (external, referenced not modified)

- Review report: `docs/reviews/ec6d-scope-q1-independent-review.md` at
  `review/hetu-ec6d-scope-q1` content commit `5ba43e1cd23871c55e01d9201a492a32dff09ae6`.
- Q1 harness + results:
  `docs/reviews/ec6d-scope-q1-evidence/harness/` (scenarios A, B, C, D, F,
  G1–G5, H and `harness-results.json`) at the same commit.
- Q1's platform validation: 388/388 existing tests passed at the EC-6C
  baseline `2c3090c` (recorded in the review report §C.1).
- R1 did not modify the Q1 worktree or branch; the fetch into the mirror is
  additive and documented in
  [recovery-and-provenance.md](recovery-and-provenance.md) §5.

### R1 environmental limitations

- Recovery and reproduction were performed on Windows x64 with .NET SDK
  10.0.401; no cross-platform execution was attempted.
- The Q1 harness scenarios were not re-executed during R1 (their committed
  results are referenced as evidence; the committed baseline harness was
  re-executed instead to validate the authentic planning measurements).
- No performance measurement, migration rehearsal, or integration activity
  was performed.
