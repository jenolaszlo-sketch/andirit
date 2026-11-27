# Hosted CI receipts

## Terminal run for the qualified candidate

| Field | Value |
| --- | --- |
| Workflow | `ci` |
| Run | `46035752304` |
| Attempt | `1` |
| Event | `pull_request` (draft PR #8) |
| Run head SHA | `ec984bfe63e5fbe13047cee8920e00b634ab17b0` |
| Checkout SHA in receipts | `c549852450d950412ff7eb01f0916134f2539c2a` |
| Conclusion | **success** |
| Created / updated | `2026-11-27T16:41:40Z` / `2026-11-27T16:46:20Z` |

### Checkout equivalence

GitHub's `pull_request` event checks out a synthetic merge commit. For this run:

- merge commit `c549852450d950412ff7eb01f0916134f2539c2a`
- parents `[99b04c0ebb5dfa43065581bb6489fe2468d9a5aa, ec984bfe63e5fbe13047cee8920e00b634ab17b0]`
- merge tree `8ec0eb7a5e647a171f54fa8969dadbb6c65da179`
- candidate tree `8ec0eb7a5e647a171f54fa8969dadbb6c65da179`

The tested tree is **byte-identical** to the qualified implementation tree, and
the candidate commit is the merge's second parent. The receipts bind the merge
SHA; the equivalence above binds it to the candidate. This is the only hosted
route available without pushing to `main` (explicitly prohibited) or modifying
the inherited CI workflow. Independent qualification may require a true push/ref
checkout; that limitation is recorded rather than hidden.

## Jobs

| Job | Conclusion | Job ID |
| --- | --- | --- |
| `pack` | success | `140630601312` |
| `test (windows-latest)` | success | `140630601784` |
| `test (ubuntu-latest)` | success | `140630601694` |
| `test (macos-latest)` | success | `140630601628` |

Artifacts:

| Artifact | ID |
| --- | --- |
| `test-results-windows-latest` | `15218020630` |
| `test-results-ubuntu-latest` | `15217445058` |
| `test-results-macos-latest` | `15215918388` |
| `coverage-results-windows-latest` | `15218055594` |
| `coverage-results-ubuntu-latest` | `15217310281` |
| `coverage-results-macos-latest` | `15216028183` |

## Named test attribution

`verify-hosted.py` parses the downloaded TRX and coverage artifacts. It passed
for all three runners with no failure and no `notExecuted` result.

| Project | Windows x64 | Ubuntu x64 | macOS ARM64 |
| --- | ---: | ---: | ---: |
| core | 345/345 | 345/345 | 345/345 |
| CSharp | 373/373 | 373/373 | 373/373 |
| TreeSitter | 157/157 | 157/157 | 157/157 |
| LatticeDb | 159/159 | 159/159 | 159/159 |
| **Total** | **1034/1034** | **1034/1034** | **1034/1034** |
| notExecuted | 0 | 0 | 0 |

New tests, all passed on every runner:

| Class | Passed/expected |
| --- | ---: |
| `NativeGraphMirrorRemediationTests` | 20/20 |
| `NativeGraphMirrorOwnershipTests` | 8/8 |
| `NativeGraphMirrorLinkedFixtureTests` | 1/1 |

All nineteen inherited S6.3 named cases (P-01…P-16, D-05, D-07, D-07-early-exit)
are present and passed on every runner, including the required output markers.
The repository's own `verify-s63-results.ps1` step also passed inside each CI job.

## Coverage

| Project | Floor | Windows | Ubuntu | macOS |
| --- | ---: | --- | --- | --- |
| core | 55 | 82.36 / 70.74 | 82.36 / 70.74 | 82.36 / 70.74 |
| CSharp | 75 | 95.15 / 87.93 | 95.11 / 87.85 | 95.11 / 87.85 |
| TreeSitter | 60 | 89.85 / 75.85 | 89.89 / 75.95 | 89.93 / 76.06 |
| LatticeDb | 70 | 93.57 / 80.78 | 93.57 / 80.78 | 93.57 / 80.78 |

Values are line / branch percentages. Every floor is retained and exceeded.

## Toolchain and platform

- Hosted SDK reported by the receipt: `11.0.100`.
- Local producer SDK: `10.0.401`.
- Runners: `Windows/X64`, `Linux/X64`, `macOS/ARM64`.
- macOS remains a regression gate; it is not S6.5 platform certification.

## Reconciliation

Published main `99b04c0…` run `45773556513` Windows receipts: core 345, CSharp
372, LatticeDb 131, TreeSitter 157 = **1005**. The candidate adds 28 LatticeDb
and one CSharp test, giving **1034**. No accepted test was removed or skipped.

The earlier candidate run `46034139976` on `579fd6a` also succeeded but predates
the ownership-safety campaign and is superseded by this run. It is retained for
lineage only.

## Reproduction

```text
gh run download 46035752304 -n test-results-windows-latest -D <dir>
gh run download 46035752304 -n coverage-results-windows-latest -D <dir2>
python docs/reviews/ANDIRIT-CI1A-S2.6-WP-S64-MIRROR-REMED-01-evidence/verify-hosted.py <root>
```

Full parsed output: `hosted-verification.json`.

## Methodological limit

The hosted checkout is GitHub's PR merge commit, not the literal candidate
commit. Tree identity and parentage are proven above. The producer does not
claim that this substitutes for a reviewer-controlled exact-SHA checkout; the
successor must decide whether to require one.
