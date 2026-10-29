# Individually qualified worktree retirement

**Pending canonical main advancement. No checkout has yet been removed.**

Retirement requires a final exact common-repository/HEAD/branch check, clean status, classified ignored content, verified bundle coverage, no active dependency and confirmation the path is not canonical. Removal uses `git worktree remove <exact-path>` without force. Each result must be checked against registrations, filesystem absence and surviving branch/commit identities.

All 30 additional linked checkouts are initially eligible; final outcomes and exceptions will be appended. Historical branches are retained; only the fully integrated temporary branch is eligible for deletion. The bare planning mirror will remain an archive. No recursive filesystem deletion, reset, clean, prune, repair or garbage collection is authorized or used.
