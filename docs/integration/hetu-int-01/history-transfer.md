# Original-object transfer and review evidence index

Before checkout integration, both repositories received verified `git bundle create --all` recovery snapshots. The canonical snapshot was refreshed after import and archival preservation. Bundle locations, sizes and SHA-256 values are in [recovery-bundles.json](raw/recovery-bundles.json); [canonical verification](raw/canonical-preserved-verify.txt) and [mirror verification](raw/mirror-bundle-verify.txt). They reside outside paths eligible for retirement.

Transfer command (no force refspec):

```powershell
git -C C:\Users\Laszlos\source\repos\Penghou.Hetu fetch --no-tags C:\Users\Laszlos\source\repos\Solo\.hetu-ec6d-planning-mirror.git 'refs/heads/*:refs/archive/hetu-int-01/mirror/heads/*' 'refs/archive/hetu-int-01/recovered/*:refs/archive/hetu-int-01/mirror/recovered/*'
```

Every required commit, tree and parent was compared after transfer against the pre-transfer manifest. [Transferred refs](raw/imported-refs.txt). No existing reference was overwritten, and no Git object was rewritten.

| Evidence | Immutable commit | Canonical documentary location |
|---|---|---|
| Q1 final review | `d2bb7b1c6d2c760ed044ad31994a90d8665fd0dc` | [Q1 review](../../reviews/ec6d-scope-q1-independent-review.md), [evidence](../../reviews/ec6d-scope-q1-evidence/README.md) |
| Q2 original final binding | `6e34452647fb28ee12be14798142921838f997bd` | Retained as ancestor of the superseding Q2 tip; exact original artifacts recoverable with `git show` |
| Q2 reconciliation | `6630624f976d53161e2657b638c7a67c41f9c0ed` | Original ancestry preserved |
| Q2 superseding attestation | `1bfcc84bfd95974fe9c54637a347e0e4d4b158e7` | [Q2 verification](../../reviews/ec6d-scope-q2-independent-verification.md), [evidence](../../reviews/ec6d-scope-q2-evidence/README.md) |
| Original planning | `91a3ed53a256e89ed71d7d11c204252c417400a6` | [Recovery/provenance](../../planning/ec6d/recovery-and-provenance.md) |
| R1 | `0e1b213737d42c74df20aef4744ab5718058e266` | [Q1 disposition](../../planning/ec6d/q1-findings-disposition.md) |
| R2 | `3ce7dd93c105e7e7375a04edb083be3a5d97f672` | [Q2 disposition](../../planning/ec6d/q2-findings-disposition.md) |
| Q3 final | `f3de16ffd04015f345fcdd65e8ca996fdb19555d` | [Q3 verification](../../reviews/ec6d-scope-q3-independent-verification.md), [evidence](../../reviews/ec6d-scope-q3-evidence/README.md) |
| Contract corrections | `cc65338f50e585d78c3a1fa8e06d02c184a10451` | [Closure record](../../planning/ec6d/owner-01-findings-closure.md) |
| EC6D-OWNER-01 | `2dd78104ed0a4ea3b3b95a7d8d5319ef452f5f84` | [Owner acceptance](../../decisions/ec6d-owner-01-contract-acceptance.md) |

Q1 added five documentary/evidence files. Q2 added 43 documentary/evidence files. Normal non-fast-forward merges retained the original commits and content without conflicts. Their executable review tools remain under docs; neither merge changed `src`, `tests`, workflows, package metadata or accepted contracts. No `ours` merge was used.

Older independent EC-3/4/5/6A/6B/6C review and qualification branches remain exact refs and are included in verified bundles. They were not automatically merged into main: some contain independent probes/tests or historical superseded work outside the accepted integration source. Each checkout HEAD remains protected; [classification](worktree-inventory.md). The separate bare mirror is retained as an archival object database, not a routine development environment.
