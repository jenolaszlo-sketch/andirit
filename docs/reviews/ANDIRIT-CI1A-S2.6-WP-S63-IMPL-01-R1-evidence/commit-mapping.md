# Evidence provenance and revision mapping

Commission: ANDIRIT-CI1A-S2.6-WP-S63-IMPL-01-R1 · Date: 2026-11-25

The three revision classes the commission requires to be distinguished are
separated below. Evidence is never re-attributed across them.

## A. Implementation revisions (original, pre-rebase — preserved, unpublished)

Recovery reference `refs/backups/s63-impl-original` → `d9461c0…`

| Original | Subject |
| --- | --- |
| `1cbce3b36325d445b4554443d4b715087a3d65c4` | S6.3 entry assessment |
| `33b5867b593c7c7e43108b538bdcddad6fc4fb15` | S6.3 native publication-correctness implementation (P-01…P-16), D-05 remediation |
| `31673f5c0476f530b4d4b1985413448b5c8c23a1` | S6.3 erratum (remote movement, §15 of the implementation record) |
| `d9461c06b9ba7a42b20a3ce100d28f39d1935a40` | repository formatting final-newline correction |

**Historical producer evidence bound to class A.** The sealed package
`…-IMPL-01-evidence/` (17 entries, manifest hash `fcab38f1…`) was generated against
the class-A implementation revision `33b5867`. Its test receipts, coverage receipt
(92.87 % / 78.96 %) and probe receipts describe class A. Nothing in that package
was modified by the rebase or by this remediation: all 17 entries still verify
byte-exact against the same hashes.

## B. Integrated (published) implementation revision

Produced by the owner-authorized rebase of class A onto `origin/main` (`4ef34b5`):

| Original | Integrated | Subject |
| --- | --- | --- |
| `1cbce3b…` | `94a8c19` | S6.3 entry assessment |
| `33b5867…` | `ece6002` | S6.3 implementation |
| `31673f5…` | `89ae722` | erratum |
| `d9461c0…` | `7a64ab2` | formatting |
| _(new)_ | `ed216e8` | integration reconciliation |

`ed216e8` was published by the first push (`4ef34b5..ed216e8`).

**Historical failure evidence bound to `ed216e8`.** Hosted run
[45513525415](https://github.com/jenolaszlo-sketch/andirit/actions/runs/45513525415)
conclusion **failure** (LatticeDb 112/114 on all three platforms) because the worker
project was not built by the per-project CI test steps. Preserved verbatim in
`hosted-ci/ubuntu-x64-evidence.txt`.

## C. Tested revision — the remediation candidate

| Revision | Subject |
| --- | --- |
| `d3c9359d1abd8c89a7214e37f074802e5d2fa238` | `test(stage6): remediate S6.3 per-project build and D-07 execution integrity (F-S63-01/02)` |

Published fast-forward `ed216e8..d3c9359`. **This is the revision the successful
hosted evidence was collected for** and the subject of every receipt in this package:

- hosted run [45639342986](https://github.com/jenolaszlo-sketch/andirit/actions/runs/45639342986), `headSha = d3c9359…`, conclusion **success**, all four jobs green;
- local gates on `d3c9359` (see `local/verification.txt`);
- clean-checkout verification on `d3c9359` (see `local/clean-checkout-verification.txt`);
- probes A–I 27/27 (receipt in `probes/probe-receipt.txt`).

## D. Evidence-only commits

No commit after `d3c9359` contains code, tests or qualification content. The commit
that carries this evidence-extension package contains only files under
`docs/reviews/ANDIRIT-CI1A-S2.6-WP-S63-IMPL-01-R1-evidence/` and no source, test,
project, workflow, threshold or dependency change. Its identity is discoverable
from Git; because a manifest cannot contain its own commit, this file records the
separation instead.

## Attribution

Every commit in classes B, C and D carries author and committer
`jenol <jenolaszlo-sketch@users.noreply.github.com>` with no `Co-authored-by:`
trailers and no AI/agent identities. Previously published history was not rewritten.