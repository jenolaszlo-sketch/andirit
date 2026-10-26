# EC-6D planning recovery and provenance (EC6D-PLAN-R1)

**Status: R1 remediation record.** This document establishes how the EC-6D
planning artifact was recovered, what is authentic, what is reconstructed,
and which Git identities are authoritative. It authorizes nothing.

## 1. Recovery classification

**RECOVERED EXACTLY.**

The originally reported EC-6D planning commit and tree exist, intact and
connectable, in an isolated mirror repository that hosts the planning
worktree. All reported identifiers were verified against the recovered
objects:

| Reported identifier | Recovered value | Match |
| --- | --- | --- |
| Branch `planning/hetu-ec6d-consumer-intelligence` | `refs/heads/planning/hetu-ec6d-consumer-intelligence` in the mirror | exact |
| Commit `91a3ed53a256e89ed71d7d11c204252c417400a6` | `git cat-file -t` → commit; HEAD of the planning worktree | exact |
| Tree `d79e5814485040a36c77bcde332ebc937f312476` | `git rev-parse 91a3ed5^{tree}` | exact |
| Worktree `C:\Users\Laszlos\source\repos\Solo\hetu-ec6d-consumer-intelligence` | exists; linked worktree; clean; on the planning branch | exact |

Commit metadata of the recovered planning tip:

- Tree: `d79e5814485040a36c77bcde332ebc937f312476`
- Parent: `3b5e8d91e89e082fd236452656c1d66a4afd3662`
- Author: `EC6D Planning <ec6d-planning@localhost>`
- Date: Mon Oct 26 17:42:18 2026 +0800
- Subject: `docs(planning): complete EC6C integration delta manifest`

## 2. Why the Q1 search missed it

The planning worktree is a linked worktree of a **separate bare mirror**
repository, `C:\Users\Laszlos\source\repos\Solo\.hetu-ec6d-planning-mirror.git`,
which maintains its own object database. The canonical clone
(`C:\Users\Laszlos\source\repos\Penghou.Hetu`) shares objects with its own 23
worktrees but **not** with this mirror. The Q1 reviewer's search covered the
canonical clone's object store, all of its worktrees, reflogs, fsck output,
the GitHub remote, and both PR heads — none of which can see objects that
exist only in this mirror. The planning branch was never pushed to `origin`.

## 3. Recovery verification steps (all read-only)

1. Worktree HEAD equals the reported commit; working tree clean
   (`git status`).
2. `.git` file resolves to
   `C:/Users/Laszlos/source/repos/Solo/.hetu-ec6d-planning-mirror.git/worktrees/hetu-ec6d-consumer-intelligence`.
3. `git cat-file -t 91a3ed5…` → `commit`; tree equals the reported tree.
4. `git fsck --connectivity-only` in the mirror reports no missing objects.
5. Ancestry: `91a3ed5` descends linearly from
   `1c9f4c9` (planning evidence: fixture + harness) →
   `2c3090c` (EC-6C owner acceptance) → … → `558f8ac` (canonical main).
   `git merge-base --is-ancestor` confirms both `2c3090c` and `558f8ac` are
   ancestors of the planning tip.
6. Change surface: `git diff --name-status 2c3090c..91a3ed5` lists exactly 33
   added paths, **all under `docs/planning/ec6d/`**. The diff under `src` and
   `tests` is empty: the planning change set contains no production
   implementation (Q1-A.2 is thereby closed on the authentic artifact).
7. No destructive operation was performed on the original worktree or the
   mirror. No reset, clean, checkout, or ref update touched them. The only
   mirror mutation made by R1 is additive (Section 5).

## 4. What the recovered packet contains

The recovered tree contains the complete EC6D-PLAN-01 packet:

- `README.md`, `integration-readiness.md`, `integration-delta.md`,
  `current-capability-inventory.md`, `consumer-use-cases.md`,
  `consumer-baseline.md`, `gap-analysis.md`, `scope-proposal.md`,
  `acceptance-contract.md`, `risk-register.md`, `evidence-index.md`;
- the committed non-production corpus
  `corpus/ts-interface-impact/` (13 TypeScript sources + fixture README,
  tree `014da71f0855cdc957fa9b6a0667bca565145092` at evidence commit
  `1c9f4c9f12e82fe35261096ff3c96343336bdb63`);
- the read-only analysis harness (`analysis/ConsumerBaseline.csproj`,
  `analysis/Program.cs`) with the recorded observation
  (`analysis/observed-results.json`) and four test-suite logs.

These documents are authentic and are reused without rewriting their
measurements. Where R1 revises them, revisions are made in clearly marked
sections and the originals remain in Git history at the recovered commits.

## 5. Mirror mutation made by R1 (additive, documented)

To make the independent Q1 review evidence verifiable from this repository,
R1 fetched the Q1 review branch into the mirror **additively**:

```
git fetch <Q1 review worktree> \
  review/hetu-ec6d-scope-q1:refs/heads/review/hetu-ec6d-scope-q1
```

- New ref: `refs/heads/review/hetu-ec6d-scope-q1` at
  `d2bb7b1c6d2c760ed044ad31994a90d8665fd0dc` (binding-record HEAD; review
  content commit `5ba43e1cd23871c55e01d9201a492a32dff09ae6` verified present
  in the mirror object store).
- No existing ref or object was modified; the planning branch and both
  planning worktrees are untouched.
- The Q1 review worktree itself was not modified; the fetch only read it.

## 6. R1 reproduction of the committed baseline (and its limits)

R1 re-executed the committed analysis harness on the remediation worktree
(tree identical to the planning commit) against the committed corpus:

| Measure | Committed observation | R1 fresh run | Match |
| --- | --- | --- | --- |
| Index units / node contributions / declarations / distinct edges | 14 / 73 / 23 / 99 | 14 / 73 / 23 / 99 | exact |
| Relationship mix (contains/declares/exports/implements/imports/inherits/invokes-name/resolves-to) | 23/23/13/5/17/1/1/16 | 23/23/13/5/17/1/1/16 | exact |
| Node identities (all 64) | — | — | **bit-identical** |
| Edge identities (all 99) | — | — | **bit-identical** |
| `FindImplementations` on the selected declaration | 0 relationships | 0 nodes, 0 edges | exact |
| `PublicationConsistent` | false | false | exact |
| `IndexIdentity` | `667ec2d4df6b821e63b2f7d1adad6c63492aaf453b9af639f1aa6e9b0fa77ec8` | `5c866e16796b6784327a5366b93ba5926e44abdad10841b2e04533ee6a63f5eb` | **differs** |
| `CanonicalFactsSha256` | `05bf7c7de0e5e1a552c77759cc3bba39aff8d72e093405379e5acfd2d99b0d3c` | `631c0ab3960790d714f824259cede7e7da89c3fc930f410b109c8cc4c44ecb29` | **differs** |

The digest divergence is fully explained and locally verified:

- The digest includes each file node's `content-hash` property, which hashes
  **working-tree bytes**.
- This environment checks out with `core.autocrlf=true`, converting the
  corpus's LF line endings to CRLF; the original environment preserved LF.
- `git hash-object` of the R1 working file equals the committed blob hash
  (`79fddb0a…` for `src/other/Renderable.ts`): the **committed content is
  byte-identical**; only the on-disk normalization differs.
- Therefore: all graph identities reproduce cross-environment; the
  content-hash-dependent digests reproduce only within byte-identical
  working trees. The original claim ("two equivalent runs produced the same
  digest") remains true within one checkout and is now precisely bounded.

The fresh run is preserved at `analysis/r1-reproduction-run.txt` on this
branch for independent comparison.

## 7. Authenticity boundaries

| Content | Status |
| --- | --- |
| Planning documents at `1c9f4c9`/`3b5e8d9`/`91a3ed5` | authentic, recovered |
| Corpus fixture and expected set | authentic, recovered |
| `observed-results.json` and test logs | authentic recorded output of the original environment |
| `analysis/r1-reproduction-run.txt` | **new** R1 output (this branch), clearly labeled |
| `recovery-and-provenance.md`, `symbol-binding-contract.md`, `consumer-query-contract.md`, `publication-consistency-contract.md`, `q1-findings-disposition.md` | **reconstructed/new** in R1, with truthful new Git identities |
| Revisions to `README.md`, `scope-proposal.md`, `acceptance-contract.md`, `evidence-index.md`, `consumer-baseline.md`, `gap-analysis.md`, `risk-register.md` | R1 amendments; original text preserved in history at the recovered commits |
| Q1 review and harness evidence | authentic; lives on `review/hetu-ec6d-scope-q1` (`5ba43e1`/`d2bb7b1`), now fetchable in this repository; **not modified** by R1 |

## 8. Preservation boundaries honored

- Original planning worktree: untouched (read-only inspection only).
- Mirror: one additive fetch (Section 5); no destructive operation.
- Canonical main, EC-6C implementation/qualification/owner-acceptance
  worktrees, Q1 review worktree: untouched.
- No push, tag, merge, rebase, or release was performed by R1.
