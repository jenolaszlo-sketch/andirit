# EC-6B closure validation record

**Scope:** documentation-only owner-acceptance reconciliation. This record
lists the checks performed for the closure commit. **No build or test run was
performed and no new test result is claimed.** The 408/41/45 validation
figures in the owner decision are the `EC6B-GLM-Q2` reviewer's independently
executed results, recorded in that preserved report.

## A. Authoritative identities

Commands:

```text
git -C <repo> remote -v
git -C <repo> rev-parse <abbreviated-id>
git -C <repo> log -1 --format="%H %T %P %s" <full-id>
```

Verified:

- remote is `https://github.com/jenolaszlo-sketch/penghou-hetu.git`;
- canonical `main` is `558f8ac82ac1b5e07da68f4a71a70240eaf38409`
  (tree `08a01a1b…`), clean;
- `1d11b73`, `8aa083c`, `691a832`, `ffdcdbd`, `b915aed`, `f8fb9d1`,
  `6669058`, and `b79bfce` all resolve to the full identities recorded in the
  [decision](../../decisions/ec6b-owner-acceptance.md) §2 and the
  [manifest](manifest.md);
- every parent link in the accepted chain matches; the review commits have the
  stated parents (`6669058` → `691a832`, `b79bfce` → `f8fb9d1`).

## B. Documentation-only transitions

```text
git diff --name-only b915aed f8fb9d1
```

Result: ten documentation files only (CHANGELOG, ROADMAP, five `docs/`
documents, two evidence documents). No `src/`, test, schema, or project file.

```text
git diff --name-only 691a832 6669058
git diff --name-only f8fb9d1 b79bfce
```

Result: each review commit adds only its review report and review-only probe
files. No production or implementation test was modified by review
preservation.

## C. Ancestry and unmerged reviews

```text
git merge-base --is-ancestor 558f8ac 1d11b73
git merge-base --is-ancestor 1d11b73 8aa083c
git merge-base --is-ancestor 8aa083c 691a832
git merge-base --is-ancestor 691a832 ffdcdbd
git merge-base --is-ancestor ffdcdbd b915aed
git merge-base --is-ancestor b915aed f8fb9d1
git merge-base --is-ancestor 6669058 main
git merge-base --is-ancestor b79bfce main
```

Results: every accepted-source ancestry check returned true; both review
commits are **not** ancestors of canonical `main` (unmerged).

## D. Worktree and branch preservation

Verified at closure time:

- canonical `main` worktree clean;
- EC-6B implementation worktree `C:\Users\Laszlos\source\repos\Solo\ec6b-relative-module-resolution`
  clean at `691a832`;
- R1 remediation worktree clean at `f8fb9d1`;
- Q1 review worktree clean at `6669058`;
- Q2 review worktree clean at `b79bfce`;
- acceptance worktree created from exactly `f8fb9d1`, and clean before the
  closure edits;
- no existing branch, worktree, or commit was modified, rebased, or deleted;
  no merge or push was performed.

## E. Hash manifest verification

The manifest records SHA-256 digests of committed blob content. Verification
recomputed the digest from the raw blob stream for **all 19** artifacts in the
manifest (Q1 report and probes, Q2 report and probes, remediation evidence,
handoff, tested source, tested tests) and compared against the recorded
values. All 19 matched. The Q1 and Q2 commit/tree/parent identities were also
re-read and matched the decision binding.

The closure documents themselves are excluded from the manifest to avoid
circular digests; their commit/tree identities are recorded in the completion
report.

## F. Documentation link and scope checks

- Relative Markdown links in the changed documents were checked against the
  acceptance tree; all local targets resolve.
- `git diff --check` was run on the staged closure content.
- The staged change set contains documentation files only (decision, closure
  evidence, roadmap/CHANGELOG/matrix/roadmap-design updates, and acceptance
  banners in the two evidence handoffs).
- The Q1 and Q2 reports are not present in the acceptance tree and are
  referenced by immutable Git identity rather than by broken local links.

## G. Post-commit checks (recorded in the completion report)

The following checks are meaningful only after the closure commit exists:

```text
git -C <acceptance> log -1 --format="%H %T %P %s"
git -C <acceptance> diff --name-only f8fb9d1 HEAD
git -C <acceptance> status --short
git -C <repo> merge-base --is-ancestor b79bfce decisions/ec6b-owner-acceptance
git -C <repo> merge-base --is-ancestor 6669058 decisions/ec6b-owner-acceptance
```

Expected: parent `f8fb9d1`; changed paths are documentation only; worktree
clean; both review commits remain unmerged into the acceptance branch. The
actual results are recorded in the owner-acceptance completion report.

## H. Not performed

- No build.
- No test execution.
- No migration or website re-run.
- No package publication.
- No merge, cherry-pick, rebase, or push.
- No production or test source edit.
