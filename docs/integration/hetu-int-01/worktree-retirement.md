
# Individually qualified worktree retirement

**PASS: 30 additional linked worktrees removed; zero retained; zero blockers.** Before: 31 working checkouts, including canonical primary. After: one canonical primary on main. The separate bare planning mirror remains intact as an archive with no linked checkouts.

Each removal revalidated the resolved absolute path and repository common directory, exact HEAD and branch, clean tracked/untracked status, classified ignored paths, original evidence archive digest, process dependency inventory, Git locks and operation state, exact verified-bundle HEAD coverage and off-machine preservation. The canonical path and bare mirror root were explicitly forbidden targets.

Removal command for each exact row was `git -C <owning-repository> worktree remove <absolute-path>`. No force option was used. Each operation then verified registration absence, target directory absence, original branch-to-commit binding and canonical object availability. Only the qualified target was passed to Git; no unrelated deletion was attempted. [Machine-readable per-removal record](raw/retirement-results.json); [append-only execution journal](raw/retirement-journal.jsonl).

| Removed absolute checkout | Original HEAD | Preserved history | Postcondition |
|---|---|---|---|
| `C:\Users\Laszlos\source\repos\Penghou.Hetu.closure-ec5` | `558f8ac82ac1b5e07da68f4a71a70240eaf38409` | reachable from synchronized canonical main + verified bundle | removed; branch and commit survive |
| `C:\Users\Laszlos\source\repos\Penghou.Hetu.design-ec6a` | `aa4031cd14b6ed8b7089c9471648a8d9d677f782` | reachable from synchronized canonical main + verified bundle | removed; branch and commit survive |
| `C:\Users\Laszlos\source\repos\Penghou.Hetu.design-ec6c` | `d12fe1882c814a0863f872e3913ddff921d31bee` | reachable from synchronized canonical main + verified bundle | removed; branch and commit survive |
| `C:\Users\Laszlos\source\repos\Penghou.Hetu.ec5-variable-declarations` | `5a7c59c4b0f783980ef7301069edf1a8fba7ecc2` | reachable from synchronized canonical main + verified bundle | removed; branch and commit survive |
| `C:\Users\Laszlos\source\repos\Penghou.Hetu.ec6b-owner-acceptance` | `038295f26ce76a8747e923f9e0ef971e773c166e` | reachable from synchronized canonical main + verified bundle | removed; branch and commit survive |
| `C:\Users\Laszlos\source\repos\Penghou.Hetu.ec6b-r1` | `f8fb9d1c2917ef04fdf9bca721bcbeaaecb959e9` | reachable from synchronized canonical main + verified bundle | removed; branch and commit survive |
| `C:\Users\Laszlos\source\repos\Penghou.Hetu.ec6c-1-export-shape-inventory` | `8c1d5e3faf1fbc33162a2fa87dacfbbdf84f4340` | reachable from synchronized canonical main + verified bundle | removed; branch and commit survive |
| `C:\Users\Laszlos\source\repos\Penghou.Hetu.ec6c-2-structural-export-publication` | `3ab1f144e31155cf3cd4bac87c4739f0a6eccb7a` | reachable from synchronized canonical main + verified bundle | removed; branch and commit survive |
| `C:\Users\Laszlos\source\repos\Penghou.Hetu.ec6c-a29-closure` | `bf4ffcc25c22a6b8f98fb2b3f513dd95e5f8f49f` | reachable from synchronized canonical main + verified bundle | removed; branch and commit survive |
| `C:\Users\Laszlos\source\repos\Penghou.Hetu.ec6c-owner-acceptance` | `2c3090cba6d8a56102a5c47ceb026c5b5fea3234` | reachable from synchronized canonical main + verified bundle | removed; branch and commit survive |
| `C:\Users\Laszlos\source\repos\Penghou.Hetu.ec6c-track-a-owner-acceptance` | `760918174b8228dc0ebe2a16fbcd164316c7a116` | reachable from synchronized canonical main + verified bundle | removed; branch and commit survive |
| `C:\Users\Laszlos\source\repos\Penghou.Hetu.preserve-ec3-q2` | `9f39eacbb3c979e14f61a55212cb5a3e05ca017e` | refs/heads/preservation/ec3-glm-q2-heritage-remediation + verified bundle | removed; branch and commit survive |
| `C:\Users\Laszlos\source\repos\Penghou.Hetu.review-ec3` | `ba2ca83ace500e148a2bfa7de9c0c01d8ef6f6ff` | refs/heads/review/ec3-glm-q1 + verified bundle | removed; branch and commit survive |
| `C:\Users\Laszlos\source\repos\Penghou.Hetu.review-ec3-q2` | `1287590ee0c46bd863873d30baea633c3b04ef13` | refs/heads/review/ec3-glm-q2 + verified bundle | removed; branch and commit survive |
| `C:\Users\Laszlos\source\repos\Penghou.Hetu.review-ec4` | `15b63cebc69fa40337ed197d005efe00ae286ebd` | refs/heads/review/ec4-real-repository-qualification + verified bundle | removed; branch and commit survive |
| `C:\Users\Laszlos\source\repos\Penghou.Hetu.review-ec5` | `1cad44ba11838bcefe54ed1f45e2f9b5bafcbba4` | refs/heads/review/ec5-glm-q1 + verified bundle | removed; branch and commit survive |
| `C:\Users\Laszlos\source\repos\Penghou.Hetu.review-ec6-ar1` | `c66ed605b00d1bff7645f3cd52ce47b275aa5817` | refs/heads/archive/hetu-int-01/review/ec6-glm-ar1 + verified bundle | removed; branch and commit survive |
| `C:\Users\Laszlos\source\repos\Penghou.Hetu.review-ec6b-glm-q1` | `66690587f4507d47e78fa4e1996e050b830c1642` | refs/heads/archive/hetu-int-01/review/ec6b-glm-q1 + verified bundle | removed; branch and commit survive |
| `C:\Users\Laszlos\source\repos\Penghou.Hetu.review-ec6b-glm-q2` | `b79bfce9820f08348335db7ca0b19f4c65e3a014` | refs/heads/archive/hetu-int-01/review/ec6b-glm-q2 + verified bundle | removed; branch and commit survive |
| `C:\Users\Laszlos\source\repos\Penghou.Hetu.review-ec6c-glm-ar1` | `49a01393375a345cefe0ca30b1f58fee1fe24e04` | refs/heads/archive/hetu-int-01/review/ec6c-glm-ar1 + verified bundle | removed; branch and commit survive |
| `C:\Users\Laszlos\source\repos\Penghou.Hetu.review-ec6c2-glm-q1` | `54ca7156dbc2354843899a619c05128615e3d806` | refs/heads/archive/hetu-int-01/qualification/ec6c-2-glm-q1 + verified bundle | removed; branch and commit survive |
| `C:\Users\Laszlos\source\repos\Penghou.Hetu.review-ec6d-scope-q1` | `d2bb7b1c6d2c760ed044ad31994a90d8665fd0dc` | reachable from synchronized canonical main + verified bundle | removed; branch and commit survive |
| `C:\Users\Laszlos\source\repos\Penghou.Hetu.worktrees\ec6a-owner-acceptance` | `1d11b7330e1d8755d5c3e3b4a830167bcf8b5bf6` | reachable from synchronized canonical main + verified bundle | removed; branch and commit survive |
| `C:\Users\Laszlos\source\repos\Solo\ec6b-relative-module-resolution` | `691a8327ab7446416de22fe26e023662399b6cf2` | reachable from synchronized canonical main + verified bundle | removed; branch and commit survive |
| `C:\Users\Laszlos\source\repos\Solo\hetu-ec6d-consumer-intelligence` | `91a3ed53a256e89ed71d7d11c204252c417400a6` | reachable from synchronized canonical main + verified bundle | removed; branch and commit survive |
| `C:\Users\Laszlos\source\repos\Solo\hetu-ec6d-owner-01` | `2dd78104ed0a4ea3b3b95a7d8d5319ef452f5f84` | reachable from synchronized canonical main + verified bundle | removed; branch and commit survive |
| `C:\Users\Laszlos\source\repos\Solo\hetu-ec6d-plan-r1` | `0e1b213737d42c74df20aef4744ab5718058e266` | reachable from synchronized canonical main + verified bundle | removed; branch and commit survive |
| `C:\Users\Laszlos\source\repos\Solo\hetu-ec6d-plan-r2` | `3ce7dd93c105e7e7375a04edb083be3a5d97f672` | reachable from synchronized canonical main + verified bundle | removed; branch and commit survive |
| `C:\Users\Laszlos\source\repos\Solo\hetu-ec6d-scope-q2` | `1bfcc84bfd95974fe9c54637a347e0e4d4b158e7` | reachable from synchronized canonical main + verified bundle | removed; branch and commit survive |
| `C:\Users\Laszlos\source\repos\Solo\hetu-ec6d-scope-q3` | `f3de16ffd04015f345fcdd65e8ca996fdb19555d` | reachable from synchronized canonical main + verified bundle | removed; branch and commit survive |

All historical branches remain: accepted heads are reachable from main; independent/superseded heads remain archival history with verified bundle and exact remote preservation. No unrelated active branch was deleted. `integration/hetu-int-01` alone was deleted normally after incorporation; its tip `4aadf0e659e3e536e12c4eb64214a9796f4b323b` is recorded and reachable from main. [Branch retirement](raw/integration-branch-retired.txt).

Final registered inventories: [canonical](raw/canonical-worktrees-after.txt), [bare mirror](raw/mirror-worktrees-after.txt). The canonical HEAD shown in this snapshot precedes the final documentation-only retirement commit; final main/tree are in the handoff. Both `git fsck --full` checks exited 0 after retirement.

Ignored build/test/native assets and caches were classified as generated. The ignored EC-6B restore diagnostic was independently preserved and hashed outside removed paths. Canonical Visual Studio state, old packages and benchmark artifacts were left in place. Recovery bundles and rehearsal stores remain outside every removed checkout.

No linked worktree was created. No `git clean`, hard reset, force-removal, recursive filesystem deletion, branch history replacement, prune, repair, garbage collection, or deletion of the bare mirror was used.
