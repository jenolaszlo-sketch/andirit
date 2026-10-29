# Preflight observations

Canonical main was clean, including no untracked files. It matched the live `origin/main`: `558f8ac82ac1b5e07da68f4a71a70240eaf38409`. Existing local heads, remote tracking heads and tags were inventoried before transfer. Existing tags were `v0.2.0-preview.5` and `v0.2.0-preview.6`; no tag was created.

The mirror was independently verified bare, with its own objects and acceptance/review refs. Canonical: 25 working checkouts (primary + 24 linked). Mirror: six linked working checkouts plus its bare root. Every registered checkout was accessible and clean; no stale registration was found. No new linked worktree was created.

`git fsck --full --no-reflogs --unreachable` completed without connectivity errors in both repositories. Unreachable objects were recorded in [canonical fsck](raw/canonical-fsck.txt) and [mirror fsck](raw/mirror-fsck.txt); recovered commit refs were created before retirement. Standalone unreachable trees/blobs remain in their retained object databases; garbage collection and pruning were not run.

Initial C: free space was approximately 63.47 GiB; exact bytes are in [disk inventory](raw/disk-before.json). Git identities, local/remote refs, ignored-file paths and worktree HEADs are preserved in [raw inventory](raw/worktrees-before.json).

Ignored linked-worktree content consisted of generated build/test outputs, package caches and one restore diagnostic. The diagnostic was copied to the external evidence directory and hashed before retirement. Canonical Visual Studio state, benchmark reports and old package artifacts remain protected in the primary checkout.

Editor/process command-line and current-directory checks found no linked Hetu checkout in use. Existing Hetu chats inspected in the app were idle or not loaded. No lock file or active Git operation was observed in the inventoried checkouts. [Process check](raw/process-cwds.json); [command-line path check](raw/active-process-inventory.json). Retirement rechecks identity and status immediately before each operation.
