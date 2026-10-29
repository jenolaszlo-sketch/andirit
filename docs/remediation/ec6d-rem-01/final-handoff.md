# EC6D-REM-01 current handoff

**EC6D-REM-01 BLOCKED: OWNER DECISION REQUIRED**

| Role | Identity / state |
| --- | --- |
| Starting commit | 514cd7938afff14af20fc5e258577ac2ece6641b |
| Starting tree | c9a2016dc91b15194fba97f197457972c011a6ae |
| Unblocked production checkpoint | f9bfab3d8de133ee1d94050fa779ddaef3173c25 |
| Production checkpoint tree | 59f60b8b8dd60018c23650d8c6a9f7dea487feee |
| Evidence identity | Resolve the containing evidence-only commit with `git rev-parse HEAD` / `git rev-parse 'HEAD^{tree}'`; its post-commit exact binding is also recorded externally in git-bindings.json and the task handoff. This file does not invent its own future hash. |
| Canonical branch | D:/projects/Andirit, main, sole checkout; local commits preserved, no push |
| Remote main | Still the starting reviewed revision; local checkpoint is not synchronized or represented as integrated |

F01, F02 and F03 blocking correctness issues are corrected, with real positive contrasts and fail-closed corruption checks. F04, F05, F06, F07, F09, F10 and F11 are corrected with permanent regression tests. **F08 is open and its correction paused**, because the accepted singular route field and all-witness/entity dedup obligations need an explicit mixed-route representation supplement. The concrete proposed additive witness route/span fields and deterministic summary semantics are reviewable in owner-decision-f08.md. The owner input request is pending; silence is not approval.

All original 505 tests and 36 new cases pass (541 total), zero failures/skips. Original preserved Probe 633/24 → 640/2, with exactly F08 memory/lattice still failing; 22 original failures corrected and seven checks newly reachable. Authority 40/2 → 40/0; Lifecycle 12/2 → 12/0. Four unchanged coverage gates, format, whitespace and local six-package/native/store smoke checks pass. All historical p9 facts are preserved in fresh migrations. Actual old-p10 snapshots are refused until normal replacement, then query/reopen correctly. Controlled exact four/five and website exact module/unsupported/frontier predicates hold.

Hosted final-remediation-source CI is **not run** and no push is claimed. After the owner resolves F08, complete that narrow correction and tests (or retain it explicitly open), update the source/evidence identities, obtain any required approval for the concrete normal fast-forward push, and verify all four hosted jobs on the exact committed remediation. Prior push permission covered the earlier implementation and evidence-only follow-ups; it is not assumed to authorize new production remediation. No force push, linked worktree, tag, release or package publication occurred.

Original authority/producer evidence/qualification bundle are unchanged. Complete raw evidence and harnesses are outside the source tree at `C:/Users/Laszlos/source/repos/Solo/ec6d-rem-01`. This executor is not author-disjoint from implementation or qualification; no independent acceptance or owner acceptance is declared. Representative performance, concurrency/load, crash/power-loss durability, deployment qualification and cross-platform execution of the new external probes remain unestablished. Exact next review steps are in reviewer-handoff.md.
