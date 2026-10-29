# Repository and checkout continuity

The existing repository was renamed in place through GitHub's supported PATCH operation only after all four required jobs passed on source commit `d2a57c9ea28d3b74b8b1a8e46f12cc9d23debba4` (tree `c319852b7183f827f36123790efe906913cdc805`). No new repository, transfer or history rewrite occurred.

| Identity | Before | After |
|---|---|---|
| Repository | jenolaszlo-sketch/penghou-hetu | jenolaszlo-sketch/penghou-andirit |
| Repository ID | 1345607723 | 1345607723 |
| Node ID | R_kgDOUDRYKw | R_kgDOUDRYKw |
| Default branch | main | main |
| Canonical checkout | C:/Users/Laszlos/source/repos/Penghou.Hetu | D:/projects/Andirit |

The owner explicitly selected `D:\projects\Andirit` during execution, superseding the initially proposed C: destination. `origin` is `https://github.com/jenolaszlo-sketch/penghou-andirit.git`. New URL/API access succeeds; old GitHub web URL returns HTTP 301 with the new canonical Location. Description now identifies Andirit.

All remote branches and tags matched exactly across the rename, including the added `archive/andirit-rename-01/pre-rename` branch binding exact baseline `b8d0966…`. Normalized PR, issue, release and publish-run inventories matched exactly. PR #3, accepted review histories, original v0.2.0-preview.5/.6 tags, original archive refs and Actions run 40805404393 remain reachable. All local original refs are preserved. No release or publication ran.

Windows denied a same-volume folder rename; File Explorer explicitly reported Folder In Use. Build servers were stopped; no user application was terminated. The selected cross-volume relocation used a filesystem copy, verified all 495 tracked files and all 869 Git metadata files byte-for-byte before retiring the original files. No clone/worktree command or second planning mirror was used. HEAD/tree, refs, complete object database and origin match. Across volumes the filesystem directory ID necessarily changes; Git identities do not. Old .git is retired; the old location contains only .vs cache residue held by Windows, not another checkout. There is exactly one canonical Git checkout and no linked worktrees.

Release restore/build, all 391 tests, package creation and a fresh-cache six-package consumer passed from D:. SDK SourceLink now maps D: source paths to the canonical penghou-andirit URL and the exact committed rename source. Raw continuity, relocation, redirect, CI and final local validation evidence is under raw/.
