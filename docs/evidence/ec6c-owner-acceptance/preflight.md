# EC6C-OWNER-01 Preflight Record

**Task:** EC-6C owner acceptance and milestone closure (EC6C-OWNER-01)
**Date:** 2026-10-26
**Acceptance branch:** `decisions/hetu-ec6c-owner-acceptance`
**Acceptance worktree:** `C:\Users\Laszlos\source\repos\Penghou.Hetu.ec6c-owner-acceptance`
**Baseline SHA (branch base):** `3ab1f144e31155cf3cd4bac87c4739f0a6eccb7a`
**Baseline tree:** `d8e763a69dadd957df32c872733be5015593c7d6`

## 1. Repository identity and status

- Canonical repository: `C:\Users\Laszlos\source\repos\Penghou.Hetu`, remote
  `jenolaszlo-sketch/penghou-hetu`, branch `main` at
  `558f8ac82ac1b5e07da68f4a71a70240eaf38409`, working tree clean (zero
  uncommitted changes).
- No merge, rebase, reset, cherry-pick, push, or release was performed before
  or during acceptance.

## 2. Implementation binding (verified)

| Binding | Value | Result |
| --- | --- | --- |
| EC-6C.2 implementation commit | `3ab1f144e31155cf3cd4bac87c4739f0a6eccb7a` | present |
| Tree | `d8e763a69dadd957df32c872733be5015593c7d6` | matches assignment |
| Parent | `160c9cff0598ab07ade402e206c553e58687900e` | matches assignment |
| EC-6C.1 handoff `8c1d5e3faf1fbc33162a2fa87dacfbbdf84f4340` | ancestor of implementation HEAD | verified |
| EC-6C.1 owner acceptance `d81108dd51d61fe79767e15c4adb4c10659836de` | ancestor of implementation HEAD | verified |
| Production `3bf418d`, export tests `b54eb3b`, compatibility tests `b61b45e` | in ancestry | verified |

## 3. Independent qualification binding (verified)

| Binding | Value | Result |
| --- | --- | --- |
| Qualification branch | `qualification/ec6c-2-glm-q1` | present |
| Qualification worktree | `C:\Users\Laszlos\source\repos\Penghou.Hetu.review-ec6c2-glm-q1` | clean (zero uncommitted) |
| Final qualification HEAD | `54ca7156dbc2354843899a619c05128615e3d806` | present |
| Qualification tree | `3cd18371f9a4b48fd515eb9c79d582fd8ef08851` | matches `3cd1837` |
| Independent test commit | `609af02` (1,218-line `TreeSitterEcmaExportQualificationQ1Tests.cs`) | present |
| Qualification commits | `568ea7f` (matrix), `609af02` (tests), `75d23e9` (findings), `54ca715` (attestation) | present |

Correspondence check: the qualification branch contains no production or
test-suite changes on top of the implementation HEAD — `git diff
3ab1f14..54ca715` touches only `docs/` and the single qualification test
file. The acceptance branch is created from the identical implementation
commit, so the accepted implementation is exactly the qualified one.

Qualification artifacts confirmed present at the qualification HEAD:

- `docs/reviews/ec6c2-glm-q1-qualification-report.md`
- `docs/reviews/ec6c2-glm-q1-qualification-matrix.md`
- `docs/reviews/ec6c2-glm-q1-compatibility-audit.md`
- `docs/evidence/ec6c2-glm-q1/qualification-run-log.md`
- `docs/evidence/ec6c2-glm-q1/migration-p8-to-p9-report.md` + `migration-report.json`
- `docs/evidence/ec6c2-glm-q1/website-smoke-inventory.md` + `website-smoke-report.json`
- `docs/evidence/ec6c2-glm-q1/harness/` (seed-p8, reindex-p9, website-smoke)
- `docs/evidence/ec6c2-glm-q1/final-attestation.md`

## 4. Sources of acceptance claims

| Claim | Source |
| --- | --- |
| EC-6C.2 implementation scope and local results | `docs/evidence/ec6c-2-structural-export-publication-handoff.md` at `3ab1f14` |
| Independent dispositions EC-6C.1 / EC-6C.2 | `docs/reviews/ec6c2-glm-q1-qualification-report.md` at `54ca715` (VERIFIED WITH QUALIFICATIONS / QUALIFIED WITH QUALIFICATIONS) |
| Requirement-by-requirement results | qualification matrix at `568ea7f` |
| Test execution baseline (25/0/0 focused; 388/0/0 full) | qualification run log at `54ca715` |
| Independent suite (27 tests) | `tests/Penghou.Hetu.TreeSitter.Tests/TreeSitterEcmaExportQualificationQ1Tests.cs` at `609af02`; 183/0/0 TreeSitter project with tests |
| p8→p9 migration demonstration | migration report + JSON at `54ca715`; independent conversion evidenced byte-identical existing facts |
| Compatibility audit | `docs/reviews/ec6c2-glm-q1-compatibility-audit.md` at `54ca715` |
| Website smoke | smoke inventory + JSON at `54ca715` (9 files, 33/33, zero `.astro` facts) |
| Findings FQ1-DOC-1, FQ1-EV-1, QN-1 | qualification report §6 at `75d23e9` |

## 5. Phase-2 prerequisites identified

1. FQ1-DOC-1: correct the overstated JavaScript type-only-like exclusion claim
   in the EC-6C.1 handoff (documentation only; no parser change; no diagnostic
   suppression; no new exclusion rule).
2. FQ1-EV-1: commit the smallest sufficient migration reproduction tooling and
   instructions, run a fresh reproduction, and preserve any unresolved hash
   discrepancy as a visible limitation.
3. QN-1: preserve as a non-blocking test-maintenance note; no test change in
   this assignment.

## 6. Anomalies

None. All hashes, ancestry, and cleanliness checks passed on first
verification; no binding discrepancy was found.
