# HETU-INT-01: accepted history integration and consolidation

Date: 2026-10-29 (Asia/Manila). **HETU-INT-01 COMPLETE: INTEGRATED AND CONSOLIDATED.**

The authenticated EC-6B/EC-6C implementation and accepted EC-6D owner contracts are integrated into canonical main. The temporary integration branch has been retired. Original Q1 and superseding Q2 review histories were merged normally, without conflicts or production changes. This milestone implements no EC-6D feature.

| Record | Contents |
|---|---|
| [Source binding](source-binding.md) | Exact commits, trees and parent identities |
| [Preflight](preflight-inventory.md) | Original canonical and mirror state |
| [History transfer](history-transfer.md) | Original-object transfer, bundles and independent review index |
| [Delta](integration-delta.md) | Accepted implementation and documentary surface |
| [Migration](migration-rehearsal.md) | Fresh direct and staged durable-store gates |
| [Validation](validation.md) | Build, 388 tests, coverage, consumer and package smoke |
| [Worktree inventory](worktree-inventory.md) | All 31 checkouts and classifications |
| [Retirement](worktree-retirement.md) | Per-checkout removal evidence and blockers |
| [Development policy](post-consolidation-policy.md) | Main-first development and no linked worktrees |
| [Rename readiness](rename-readiness.md) | Controlled Andirit rename prerequisites |
| [Hashes](SHA256SUMS) | Stable evidence file SHA-256 values |

These are fresh integration observations, except where explicitly labeled inherited acceptance or review evidence. Historical documents remain unchanged. Original raw logs are retained outside the checkout at `C:\Users\Laszlos\source\repos\Solo\hetu-int-01-evidence`; committed text copies normalize line endings to LF and trim output-only trailing whitespace; original outputs remain in the external evidence directory. Recovery bundles stay outside every retired path.

Verify hashes and local Markdown links: `python docs/integration/hetu-int-01/tools/check_evidence.py`.

The next milestone is **ANDIRIT-RENAME-01: Controlled Hetu → Andirit Rename**. R16 intended-consumer validation remains open before EC-6D implementation authorization.

All 388 tests, durable direct/staged migration rehearsals, coverage and package smoke checks passed. Integration CI passed on Windows, Ubuntu and macOS. All 30 additional linked worktrees were removed without blockers; one canonical checkout remains on main. See [post-integration binding](raw/post-integration-binding.json) and [retirement journal](raw/retirement-results.json). The final documentation-only commit and exact rename baseline are supplied in the final handoff.
