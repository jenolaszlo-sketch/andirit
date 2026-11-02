# ADR-CI1-01-CI1A-IMPL-01-S2.1-OWNER-01 — owner evidence record

**Recorded:** 2026-11-02. **Commission:** `ADR-CI1-01-CI1A-IMPL-01-S2.1-OWNER-01` (documentation-only).
**Disposition:** `CI1A-S2.1-OWNER-01 ACCEPTED WITH QUALIFICATIONS — S2.2 IDENTITY DECIDED`.

## 1. Reviewed source identities

| Role | Commit | Tree |
|---|---|---|
| S2.1 starting baseline | `acd7ecd22516851ac57169fe19d184f6b10ac639` | `75ebd29380d6331accdc9e5838440d921a4280f9` |
| Contract + authorized amendment | `bd8e6331da9023cebb211f764d81b978a96650dc` | `d6a8fbd36d1c5a20a228ab6d86710025b81d6ba4` |
| Production implementation | `c02d5eae157653ba7f123b98aceccb01a0b542a0` | `a53a076dbb482062973f70fd11bf4261cc5368f3` |
| Reviewed final source (evidence-only tip) | `0fa63ed2a0f7f1b5ffe21f3791e93e81eb4cb329` | `f713cb6e4a1dbfb4f531f6971a37893902b41435` |

Ancestry verified linear; canonical `main`; one worktree; no stashes; clean tracked tree; no push (`origin/main` = `31c33a934aa13e373d9a3a6ffbc54d789eef092c`, an ancestor).

## 2. Independent Q1 package binding

Package: `C:\Users\Laszlos\source\repos\Solo\adr-ci1-01-ci1a-impl-01-s2-1-q1`.
Controlling review: `ADR-CI1-01-CI1A-IMPL-01-S2.1-Q1` — **SUPPORTED WITH QUALIFICATIONS**.

Verified 2026-11-02 against `evidence/REVIEWER-MANIFEST.sha256`:

- **1,635 recorded entries, 0 missing.**
- **1,615 entries verify byte-identically in place** (probes, pinned source copy, TRX, logs, scripts, evidence).
- **6 recorded documents verify byte-identically** in `evidence/prior-review-preserved/` (`ADVERSARIAL-PROBES.md`, `FINAL-HANDOFF.md`, `NEW-FINDINGS.md`, `OWNER-HANDOFF.md`, `REGRESSION-RESULTS.md`, `S2-2-COMPATIBILITY.md`).
- 13 root documents were superseded in the same workspace by a **complete second independent Q1 run** (its own 36-entry self-excluded manifest verifies 36/36; it reproduced 645/0/0, 22/22 probes, frozen vectors 793/793, and independently reproduced Q-F2/Q-F3 via its own probes P27/P28). One log (`evidence/reviewer-final-verify.log`) was rewritten by the earlier run after its manifest was built.
- **Recorded limitation:** the 13 superseded documents are not recoverable; the acceptance binds the material Q1 findings that are corroborated across both independent runs and the preserved/valid evidence above. This is an evidence-packaging limitation, not a source or correctness defect.

## 3. Independent evidence accepted (not rerun by OWNER-01)

| Evidence | Result |
|---|---|
| Regression | 645 passed / 0 failed / 0 skipped (325 core, 43 native, 157 TreeSitter, 120 C#) in both independent runs; producer final TRX counters parsed; build log 0 warnings / 0 errors |
| Reviewer probes | 27/27 (P01–P22, X01–X05, controlling package) + 22/22 (corroborating run, P02–P28) |
| Frozen R2 / `compctx2` | `usage4` vectors `bd0487b7…`, `b874bdc5…`; `compctx1` vector `71d89657…`; `compctx2` tuple `9e55d2db…`; `asmctx3`/`usage4` helper bytes unchanged |
| Live dependency propagation | Conditional public API `int`→`string`; transitive propagation; deterministic restore; missing/cyclic/conflicting refusal |
| D3/D4/D5 | Preserved in tested scope; zero graph/native/publication/schema changes; +51 tests, 0 removed/disabled |

## 4. S2.2 identity experiment (this commission, reviewer-owned, outside canonical)

Executed against the pinned S2.1 production seams (`S22IdentityExperiment.cs`; 3/3 passed; raw output `evidence/s22-identity-experiment.json` in the Q1 workspace). No canonical source modified.

- **E1** (conditional API in the defining project): `compctx1` `ff933d5d…`→`d8a22e2a…`; **`asmctx3` `5ac32e15…`→`4f9d5031…`**.
- **E2** (transitive-only Leaf change): `compctx2` changes at all levels (Leaf `ecb5815a…`→`f9921ead…`; Lib `ad98ee4d…`→`741545dd…`; App `c15ec417…`→`232964d3…`); Lib's own R2 material unchanged; only dependency reference items differ; no legacy `compctx1` twin exposed for dependency-bearing compilations.
- **E3** (caller source unchanged): target `Lib.Value`'s `asmctx3` `5ac32e15…`→`4f9d5031…`.

Conclusion recorded in `adr-ci1-01-ci1a-s2-2-target-identity-decision.md`: `asmctx3` already discriminates declaration-changing inputs; the transitive-only case changes provenance validity, not declaration identity; **Option A** is correct.

## 5. Owner dispositions

| Item | Disposition |
|---|---|
| S2.1 acceptance | **ACCEPTED WITH QUALIFICATIONS** (`adr-ci1-01-ci1a-s2-1-owner-acceptance.md`) |
| Pre-release compatibility | **ADOPTED** — POL-01..POL-10 (`adr-ci1-01-pre-release-compatibility-policy.md`) |
| S2.2 target identity | **`S2.2-ID OPTION A ACCEPTED`** — retain `asmctx3`, mandatory `compctx2` validity/invalidation (`adr-ci1-01-ci1a-s2-2-target-identity-decision.md`) |
| Q-F2 encoding boundary | Accepted limitation; fix or explicit supported-encoding decision **before S2.2 acceptance** |
| Q-F3 metadata validity | Accepted limitation; qualify **before S2.2 semantic acceptance** |
| Q-F4 wording | Record; optional later clarification |
| Q-F5 branches | Record; housekeeping outside this commission |
| A-01 symbol separator | Bounded hardening before public exposure / caller-supplied parse options |
| A-02 scope composition | Documented boundary |

## 6. Documentation changes (this commission)

Commit A (`96b9db8`) — S2.1 acceptance:

- `docs/decisions/adr-ci1-01-ci1a-s2-1-owner-acceptance.md` (new)

Commit B (`644167c`) — pre-release policy:

- `docs/decisions/adr-ci1-01-pre-release-compatibility-policy.md` (new)

Commit C — identity decision, register, handoff, evidence, roadmap:

- `docs/decisions/adr-ci1-01-ci1a-s2-2-target-identity-decision.md` (new)
- `docs/decisions/adr-ci1-01-ci1a-s2-obligation-register.md` (updated: S21A/S21Q1/PRE/S22ID authority keys; F11 status; new §4A S2.1-Q1 gates; S2.1/S2.2 progression rows)
- `docs/planning/adr-ci1-01-ci1a-impl-01/s2-2-identity-handoff.md` (new)
- `docs/evidence/adr-ci1-01-ci1a-s2-1-owner-01.md` (this record)
- `ROADMAP.md`, `docs/agent-code-intelligence-roadmap.md` (CI-1A status paragraph updated)

No source, test, schema, CI, native or package file changed. No push.

## 7. Remaining obligations (preserved, not closed)

Q-F2/Q-F3 gates; S2.0/F12 frozen semantic oracle before S2.2 qualification-test authoring; S-01 defining-target binding during S2.2; NEW-01 before S2.3/earlier recovery reliance; NEW-04 before dependent coverage claims or S2.2 acceptance; S2.2 mapping contract implementation/review; reader continuation S2.4; real-project end-to-end S2.5; full D3/D4, historical fixtures and Linux qualification at assigned gates; crash/power-loss/multi-process outside current guarantees.

## 8. Nonclaims

OWNER-01 did not rerun tests, probes or the oracle; it verified identities, manifests, TRX counters and recorded evidence. It performed no independent security review and issues no second review verdict. S2.2 implementation was not begun; no target/occurrence identity was minted; no database, fixture or user file was deleted.
