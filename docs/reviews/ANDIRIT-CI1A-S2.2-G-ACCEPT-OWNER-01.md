# ANDIRIT-CI1A-S2.2-G-ACCEPT-OWNER-01 — Stage-level acceptance evidence report

**Commission:** `ANDIRIT-CI1A-S2.2-G-ACCEPT-OWNER-01` (final S2.2 owner acceptance).
**Repository:** Andirit. **Canonical branch:** `main`. **Date:** 2026-11-06.
**Decision:** `ACCEPTED WITH EXPLICIT QUALIFICATIONS`. **Gate:** `S2.2 G-ACCEPT PASSED`.
**Decision record:** [`docs/decisions/adr-ci1-01-ci1a-s2-2-g-accept-owner-01.md`](../decisions/adr-ci1-01-ci1a-s2-2-g-accept-owner-01.md).
**Evidence directory:** `docs/reviews/CI1A-S2.2-G-ACCEPT-OWNER-01-evidence/`.

This report is the stage-level evidence inventory and verification record for the
final S2.2 gate. It performs no new implementation and modifies no production code,
producer test, reviewer artifact, frozen expectation, prior owner decision, or
accepted planning contract.

---

## 1. Baseline

| Object | Value |
| --- | --- |
| Starting/final HEAD | `40b5062a6c04a37a1fe0641a4b5f86f2431baeb7` |
| Starting/final tree | `4772515e743e486bf124f1edbb62b3bdbc01b3b7` |
| Working tree | clean (one worktree `D:/projects/Andirit`), no stashes |
| `origin/main` | `443c192733a53d2f1da415743debb84e8c81acb5` (ancestor) |
| Ahead / behind | 15 / 0 |
| Push status | nothing pushed |
| Environment | Windows x64; SDK `10.0.401`; runtime `10.0.12`; Roslyn `5.9.0`; C# 14 |

All accepted milestone commit/tree identities (`e0d9110`, `812584d`, `4fdc23c`,
`d3da2e0`, `97c88e5`, `f1e7f27`, `2b35cd6`, `7cef15a`, `76ac976`, `a257c39`,
`40b5062`) and their linear ancestry were re-resolved and reproduced exactly.

---

## 2. Verification reproduced at this HEAD

Every result below was executed at `40b5062`; the raw logs are committed in the
evidence directory (`*.log`), and the oracle output in `oracle-r2.json`.

| Verification | Expected | Reproduced | Log |
| --- | ---: | ---: | --- |
| Whole solution (`dotnet test Andirit.slnx -c Release`) | 789 | **789 / 0 / 0** (43 + 157 + 345 + 244) | `solution-suite.log` |
| WP8 integration producer | 9 | **9 / 0 / 0** | (whole-solution / filtered run) |
| WP8-Q1 independent probes | 10 | **10 / 0 / 0** | `wp8-q1-probes.log` |
| WP7 producer | 12 | **12 / 0 / 0** | `wp7-producer-tests.log` |
| WP7-Q1 probes | 19 | **19 / 0 / 0** | `wp7-q1-probes.log` |
| WP6 producer matrix | 20 | **20 / 0 / 0** | `wp6-producer-matrix.log` |
| WP6-Q1 probes | 30 | **30 / 0 / 0** | `wp6-q1-probes.log` |
| WP5 producer (S2_2Wp5) | — | **20 / 0 / 0** | `wp5-producer-tests.log` |
| WP5-Q2 probes | 22 | **22 / 0 / 0** | `wp5-q2-probes.log` |
| WP5-Q1 probes | 16 | **16 / 0 / 0** | `wp5-q1-probes.log` |
| WP4 producer | 20 | **20 / 0 / 0** | `wp4-producer-tests.log` |
| WP4-Q1 probes | 13 | **13 / 0 / 0** | `wp4-q1-probes.log` |
| G-MINT-Q1 probes | 12 | **12 / 0 / 0** | `gmint-q1-probes.log` |
| WP3 producer | 16 | **16 / 0 / 0** | `wp3-producer-tests.log` |
| WP3-Q1 probes | 19 | **19 / 0 / 0** | `wp3-q1-probes.log` |
| Frozen R2 oracle | 185/185 | **185 / 185**, exit 0 | `oracle-r2.log`, `oracle-r2.json` |
| `dotnet format Andirit.slnx --verify-no-changes` | exit 0 | **exit 0** | `format-verify.log` |
| `dotnet build Andirit.slnx -c Release` | 0/0 | **0 warnings, 0 errors** | `build-release.log` |

Oracle output SHA-256 `97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9`
== accepted; frozen expected SHA-256
`274A7AAE9EB6724879D619416E449C52759B44085D7B718DAD3434B15387CBBF` unchanged.
Determinism (committed WP8-Q1 evidence, cross-checked): producer canonical payload
`3f08abc616858224b78a9b08a125e167374cbeb40c84c145db2b2c8ab4817ff7` / 12 108 bytes;
reviewer canonical payload
`00a46e4e96205049e93795b574c734fd796a34bf83a49953805a87fc42585f80` / 11 821 bytes.

No failures and no skips in any suite.

---

## 3. Production / frozen drift checks

| Check | Result |
| --- | --- |
| `git diff 2b35cd6..HEAD -- src` | empty (no production change since WP6) |
| `git diff 7cef15a..HEAD -- src docs/decisions docs/planning` | only the WP8 owner-decision document |
| Frozen corpus / `oracle-expected-r2.json` | unmodified |
| `CodeOccurrence.cs` SHA-256 | `A8EF3403A1190CC7C2773C87A8F571A718A8E02CDD0582A096DE11F88B1F2085` (post-WP6 correction; frozen formulas semantically unchanged) |
| Producer tests weakened / prior decisions rewritten | none |

---

## 4. Evidence inventory

`docs/reviews/CI1A-S2.2-G-ACCEPT-OWNER-01-evidence/`:

- `.gitignore`, `.gitattributes` — evidence-encoding/whitespace convention
  (`*.log` tracked via `!**`; raw bytes preserved).
- `format-verify.log`, `build-release.log`, `solution-suite.log`.
- `oracle-r2.log`, `oracle-r2.json` (frozen oracle replay output).
- `wp3-q1-probes.log`, `gmint-q1-probes.log`, `wp4-q1-probes.log`,
  `wp5-q1-probes.log`, `wp5-q2-probes.log`, `wp6-q1-probes.log`,
  `wp7-q1-probes.log`, `wp8-q1-probes.log` — independent reviewer probe suites.
- `wp3-producer-tests.log`, `wp4-producer-tests.log`, `wp5-producer-tests.log`,
  `wp6-producer-matrix.log`, `wp7-producer-tests.log` — producer subsets.

All referenced `.log` files are tracked (the directory `.gitignore` re-includes
them over the repository-level `*.log` rule).

---

## 5. Stage-level disposition

`GA-1`…`GA-12` are dispositioned in the decision record. Overall:

**`ACCEPTED WITH EXPLICIT QUALIFICATIONS` — `S2.2 G-ACCEPT PASSED`.**

The sharpest retained qualification (S-01 general source canonicalization of the
caller-supplied identity component digests) is recorded explicitly and is **not**
silently deferred; the S2.3 durability boundary, NEW-01 non-reliance, NEW-04
coverage limits, declared profiles, platform scope, and the Q1-WP8-5 payload
boundary are all preserved. S2.3 is eligible for a separate entry/authorization
commission; S2.4/S2.5/publication remain independently controlled.

---

**Reported at HEAD `40b5062a6c04a37a1fe0641a4b5f86f2431baeb7` (tree
`4772515e743e486bf124f1edbb62b3bdbc01b3b7`).**
