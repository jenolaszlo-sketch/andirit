# Main-first development policy

Canonical repository: `C:\Users\Laszlos\source\repos\Penghou.Hetu`; primary branch `main`. One working checkout; no additional linked worktrees for normal development. The retained bare planning mirror is a historical archive only.

For ordinary work: synchronize main, make a bounded change directly on main, run relevant tests, commit, push normally to origin/main, and confirm applicable CI. Inspect workflow triggers before pushing. Packages and releases require separately authorized stable milestones.

Use a temporary branch in the same checkout only for risky integration or explicitly required independent isolation. Do not create worktrees for routine specifications, implementations, reviews, remediation, acceptance or documentation. Retain historical review refs without checked-out worktrees. Do not create separate routine planning mirrors.

Independent review still binds an immutable source commit and reproducible evidence. Reviewers must not mutate the artifact under review. If a task cannot be safely isolated in the single checkout, stop and obtain an explicit exception before creating another linked worktree.

Retain the discipline: **Claim → Probe → Evidence → Independent Review → Accept / Revise / Reject**. Workflow simplification changes neither acceptance criteria nor provenance requirements.
