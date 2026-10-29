# EC6D-OWNER-01 validation evidence

Execution: 2026-10-29, Asia/Manila; Windows. This is a focused planning
acceptance pass, not qualification of production EC-6D. No production code
or tests changed; no broad regression suite or TypeScript oracle rerun was
performed. Existing Q3/Q2 semantic evidence was read and authenticated.

## Environment and isolation

All commands below ran from
`C:/Users/Laszlos/source/repos/Solo/hetu-ec6d-owner-01` unless stated.
Preflight commands ran from its parent `Solo` using
`git --git-dir=.hetu-ec6d-planning-mirror.git ...`.
Available space at preflight was approximately 63.8 GiB; exact free bytes
are in [mirror preflight](analysis/owner-01-preflight.json).

| Command / check | Observed result |
| --- | --- |
| `git --git-dir=.hetu-ec6d-planning-mirror.git show-ref` and `worktree list --porcelain` | Existing refs and all five non-bare planning/review worktrees snapshotted; all clean |
| `git show -s --format="%H %T %P" <each baseline>` | Q3 final/content, R2, R1, recovered original, EC-6C owner and main exactly match requested identities; see checker JSON for invocations |
| `git merge-base --is-ancestor <ancestor> <tip>` | R1→R2, recovered→R1, EC-6C→Q3, main→Q3, Q3→acceptance all exit 0 |
| `git fsck --full` | Exit 0; dangling blobs only, no missing/corrupt connectivity; exact output in checker JSON |
| `git --git-dir=.hetu-ec6d-planning-mirror.git worktree add -b acceptance/hetu-ec6d-owner-01 C:/Users/Laszlos/source/repos/Solo/hetu-ec6d-owner-01 f3de16ffd04015f345fcdd65e8ca996fdb19555d` | Dedicated branch/worktree created at exact Q3 final commit; clean starting state |
| `git diff --name-status 0e1b213737d42c74df20aef4744ab5718058e266 3ce7dd93c105e7e7375a04edb083be3a5d97f672` | 12 modified, 6 added, all planning; full path output in checker JSON |
| `rg -n <count-pattern> docs/planning/ec6d` (exact pattern below) | Initial scan found the single incorrect index entry; corrected index and active Markdown scan now pass |
| `git show 6630624f976d53161e2657b638c7a67c41f9c0ed:docs/reviews/ec6d-scope-q2-independent-verification.md` / `git rev-parse <same commit>:<same path>` | Reconciled content report blob `ff2d18826a350c809785a8d24c5355abbfb36f38`; Q2 binding child changes only attestation (§O), as expected |
| `git show <Q3>:docs/reviews/ec6d-scope-q3-evidence/<artifact>` + SHA-256 against Q3 validation index | All 11 indexed artifact digests match committed bytes; Git blobs used to avoid checkout line-ending transformations |
| Read Q3 oracle input/run/comparison plus Q2 p9 merged observation via `git show 1bfcc84:docs/reviews/ec6d-scope-q2-evidence/p9-adversarial-results.json` | TS 5.9.3 merged case has zero diagnostics/two declarations; p9 one interface node/two physical records; alias and same-file positives retained; no oracle execution |

The first unprivileged canonical read was denied by the filesystem sandbox;
the permitted read-only escalation succeeded. It was not a Git provenance
failure. Canonical remote is
`https://github.com/jenolaszlo-sketch/penghou-hetu.git`; canonical main is
exactly `558f8ac82ac1b5e07da68f4a71a70240eaf38409` / tree
`08a01a1b7d5d3109b991bcd05d57272fd3f5f018`.

Exact initial count-search command:

```powershell
rg -n 'ten modified|five added|10 modified|5 added|12 modified|6 added' docs/planning/ec6d
```

## Focused contract checks

The manual [findings/audit record](owner-01-findings-closure.md) documents
Q3-F01..F05 closure and declaration identity, lexical scope, import/export
attribution, categories, provenance, publication, coverage and compatibility
checks. The mechanical checker compares load-bearing sections directly to
Q3, rather than simulating a product binding implementation.

```powershell
python docs/planning/ec6d/analysis/owner-01-validate.py
python docs/planning/ec6d/analysis/owner-01-validate.py --contracts cc65338f50e585d78c3a1fa8e06d02c184a10451 --output docs/planning/ec6d/analysis/owner-01-validation.json
& './docs/planning/ec6d/analysis/owner-01-canonical-verify.ps1'
git diff --check
git diff --cached --check
git diff --name-only f3de16ffd04015f345fcdd65e8ca996fdb19555d -- src tests docs/reviews docs/evidence
git status --porcelain=v1 --untracked-files=all
```

- Pre-contract-commit checker: PASS, 55 checks, 23 Markdown documents,
  74 relative links, zero missing. It verified the original protected
  mirror refs and five worktrees, evidence hashes, identities and semantic
  invariants. The checker was then extended to include final owner-record,
  accepted-blob and canonical-report checks.
- Final extended checker: **PASS**; exact check count, document/link count,
  changed-path manifest, Git command exit codes and all check outcomes are
  in [owner-01-validation.json](analysis/owner-01-validation.json).
- Canonical before/after checker: **PASS**; all original refs and worktree
  inventory unchanged, **25 worktrees unchanged and clean**, exact main
  identity matches. Q3 object absent in canonical (`cat-file -e` exit 1,
  expected separate-database condition). See
  [canonical preflight](analysis/owner-01-canonical-preflight.json) and
  [canonical validation](analysis/owner-01-canonical-validation.json).
- Lexical gates, import route and attribution definition unchanged;
  publication contract/scope/risk register unchanged; same closed 20
  emittable + 2 reserved reason codes; possible-consumer predicate,
  coverage, budget/cancellation/integrity rules unchanged. All five accepted
  contract files remain exactly those at the corrected contract commit.
- `git diff --check` and staged check: clean. Historical report/evidence,
  production and test diff: empty. Full changed-path manifest is restricted
  to `docs/planning/ec6d/` and the one new owner record in `docs/decisions/`.
- Relative Markdown link targets: zero missing; anchors are not validated.
  Branch-qualified historical evidence references were resolved via Git,
  without copying them into this worktree.

The first commit is `cc65338f50e585d78c3a1fa8e06d02c184a10451`, tree
`9940965196b35488258501e9513ec1d8025d4007`, parent Q3 final. It changes
four planning Markdown files and adds the closure/checker plus two
preflight snapshots (eight paths). The record commit is its immediate
child, preserving all reviewed ancestry. Final record commit/tree/parent
and post-commit clean status are verified and emitted in the completion
handoff, avoiding self-referential hashes in the committed record.

## Limits and protected boundaries

No production implementation, integration, rename, publication/release,
push or merge. All original mirror refs and historical planning/review
worktrees remain unchanged; all canonical refs/25 worktrees remain
unchanged. Read-only status commands may refresh Git's stat cache but
change no source, ref or staged content. Checker reproduction uses the
committed local Windows worktree inventory; other machines must map paths
without claiming fresh evidence for this original environment.

The inherited Windows/store/small-fixture, migration-hash, staged migration,
performance, R16 and narrow-semantic qualifications remain as recorded in
the [owner record](../../decisions/ec6d-owner-01-contract-acceptance.md).
WSL2 does not establish native Linux qualification. No check here proves
implemented EC-6D behavior or independent production qualification.
