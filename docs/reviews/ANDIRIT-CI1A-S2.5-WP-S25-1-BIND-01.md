# ANDIRIT-CI1A-S2.5-WP-S25-1-BIND-01 — SQ-04 authentic historical fixture binding (producer evidence preparation)

**Commission:** `ANDIRIT-CI1A-S2.5-WP-S25-1-BIND-01` (`WP-S25-1 FIXTURE BINDING AUTHORIZED WITH QUALIFICATIONS`).
**Project:** Andirit. **Recorded:** 2026-11-09. **Canonical branch:** `main`.
**Reviewed source:** `9b158b822a2883503a2dd2a65355192ed144c1dd` (tree `6f09cb73fa12cd6312b1edef51871cef91f81cc4`).
**Role:** producer-side fixture binding and evidence preparation. Independent SQ-04 qualification and the `WP-S25-1-OWNER` acceptance are separate commissions.
**Producer disposition:** **`FIXTURE BINDING COMPLETE — READY FOR INDEPENDENT QUALIFICATION`**.

> This package imports two authentic prior-writer masters as byte-exact committed fixtures,
> defines the disposable staging procedure, reproduces the accepted P13b/refusal and H-01/H-02
> behaviors in fresh processes, and records the experimental codecs that lack authentic
> writers as explicit owner-amendment candidates. It changes no production code, storage
> format, or accepted contract, and claims no SQ-04 discharge and no Linux/S-01 progress.

---

## 1. Verified preflight

| Check | Result |
| --- | --- |
| HEAD / tree | `9b158b822a2883503a2dd2a65355192ed144c1dd` / `6f09cb73fa12cd6312b1edef51871cef91f81cc4` — both expected |
| Branch / worktrees | `main`; exactly one (`D:/projects/Andirit`); clean tracked/index; no stashes; no concurrent writer |
| Local vs remote | `origin` `https://github.com/jenolaszlo-sketch/andirit.git`; recorded `origin/main` is **0 behind / 67 ahead** (no fetch performed); nothing pushed |
| Environment | Windows `10.0.26200` **x64**; .NET SDK `10.0.401`; runtime `10.0.12` |
| Authority chain | S2.5 entry decision `adr-ci1-01-ci1a-s2-5-entry-owner-01.md` §6; [work-package plan](../../planning/adr-ci1-01-ci1a-s2-5/work-package-plan.md) WP-S25-1; S2.5 entry assessment §4; REM-01 owner acceptance (P13b); R2 historical matrix (H-01/H-02); acceptance matrix §13 |

No unexpected repository state or conflicting authority was found. No reset/clean/restore/rebase/prune/worktree/fetch/push was performed. All fixtures, receipts, and logs live under `docs/reviews/CI1A-S2.5-WP-S25-1-BIND-01-evidence/` (probe and scripts under `docs/reviews/CI1A-S2.5-WP-S25-1-tests/`).

## 2. Original fixture provenance and complete hashes

Read from the accepted committed artifacts — **complete hashes only, never abbreviated**. Both external source masters were re-hashed during this commission and match their recorded values; the committed copies were verified byte-exact after import (all three files).

### Set A — pre-REM occurrence store (authentic Andirit prior writer)

| Item | Value |
| --- | --- |
| Committed copies | `fixtures/prerem/prerem-authentic.ltdb` — **86,016 bytes**; `fixtures/prerem/prerem-authentic.ltdb-wal` — **4,096 bytes** |
| Complete SHA-256 (`.ltdb`) | `7646b98288a9803cff6f9930c78a3e7df5c676f6fc4e0cd2df2af5e79bc68885` |
| Complete SHA-256 (`.ltdb-wal`) | `8ca170cdc55a4428a5506bcb447a0667ab10c92c77ef149ae36460a5603aeab4` |
| Authoritative source | `C:\Users\Laszlos\source\repos\Solo\adr-ci1-01-ci1a-s2-boundary-rem-01-q1\evidence\` (accepted REM-01-Q1 package; manifest 33/33 verified in the owner acceptance) |
| Writer provenance | Pinned Q1B baseline production code `4c6d26a1d103a62fffe2e2743467eedb5f21afc2` (tree `320e965f219d0b615da29665d7fa3e85f8bbfc15`) extracted with `git archive`; one graph unit + one occurrence fact via the pre-REM production path (old four-field `ComputeTargetId`; **no** canonical `asmctx3:sha256:` prefix); writer sources + README retained in the source package |
| Recorded format/schema metadata | Pre-canonical Andirit occurrence storage generation (`AndiritOccurrenceUnit/Page/Meta` era); accepted current namespace `andirit-occurrence-facts/1` is not asserted by this store — it is refused, not interpreted |
| Accepted evidence relationship | Probe **P13b** (REM-01-Q1): opening a disposable copy with the accepted store throws `CodeOccurrenceSchemaException` — refusal without reinterpretation, store-wide |
| Availability | Present locally; reproducibly accessible; exact copies now committed |

### Set B — R2 baseline graph-only store (authentic Andirit prior writer)

| Item | Value |
| --- | --- |
| Committed copy | `fixtures/baseline/baseline-graph-only.master.db` — **163,840 bytes** |
| Complete SHA-256 | `f21a72bee2c2e378e16766ca7b84c29010ab4d50360bf3111a82706f947781e0` |
| Authoritative source | `C:\Users\Laszlos\source\repos\Solo\adr-ci1-01-exp-02-r2\evidence\historical-master\baseline-graph-only.master.db` (R2 controlling reference incorporated by OWNER §F) |
| Writer provenance | Frozen baseline `CSharpCodeGraphPlugin` + `LatticeCodeGraphStore` at `31c33a934aa13e373d9a3a6ffbc54d789eef092c` (tree `a265f561243de37e1636490f99acb0b34245809e`); graph-only publication over the frozen repro corpus; store schema version 2; publication run `historical-master-1`; per-file writer source hashes recorded in the source package `provenance.json` |
| Recorded format/schema metadata | Graph-only schema-2 store (current `LatticeCodeGraphStore.CurrentSchemaVersion = 2`); no occurrence meta/units/pages |
| Accepted evidence relationship | **H-01**: authentic baseline graph remains usable by legacy callers (callers ≥ 1); **H-02**: occurrence query reports `AnalysisUnavailable`, not successful empty |
| WAL policy | The source package's adjacent `baseline-graph-only.master.db-wal` is **not** part of this fixture: no recorded hash exists for it, and the accepted R2 procedure copied the `.db` only to a fresh disposable copy before opening. It was not imported; no claim is made about it |
| Availability | Present locally; reproducibly accessible; exact copy now committed |

Full provenance/handling record: `fixtures/PROVENANCE.md` (committed; manifest-bound).

## 3. Safe staging procedure

**Import decision.** The fixtures are **imported as exact immutable copies** into a dedicated repository fixture directory (`…BIND-01-evidence/fixtures/`) rather than referenced externally: repository-side binding is this package's purpose, the external Solo paths are outside the repository and not guaranteed reproducible, and the total size is small (253,952 bytes across three files). Import authorization: the commission authorizes "narrowly required fixture bindings or copies"; provenance was verified before import and the copies re-hashed after import. `.gitattributes` (`* -text`; `*.ltdb binary`; `*.db binary`) guards the bytes (verified with `git check-attr`: `text: unset` / `binary: set`).

**Procedure (committed as `docs/reviews/CI1A-S2.5-WP-S25-1-tests/run-probes.ps1`).**

1. Verify the committed masters against their **complete** recorded SHA-256 values; mismatch aborts.
2. Stage exact copies to a disposable work directory under the system temp: the pre-REM `.ltdb` and `.ltdb-wal` **together**; the baseline `.db` alone (the accepted R2 procedure).
3. Execute each case in a **fresh OS process** against a **fresh staged copy** (four runs: two pre-REM, two graph-only, each with distinct process ids).
4. Re-verify the committed masters after every run; the probe also asserts the original bytes are unchanged.
5. Write the integrity receipt (`Q1-INTEGRITY.json`); remove the work directory on success (kept on failure for postmortem).

**Guarantees.** Historical originals are immutable inputs: the committed masters are never opened by a store and were byte-identical before and after every run (receipts). Only disposable copies are opened; recovery/WAL-replay mutations occur only on those copies — the receipts show the staged copies **do** change on open (e.g., pre-REM `.ltdb` `7646b982…` → `bee20756…` / `99223fe8…`; baseline `.db` `f21a72be…` → `67fd76dd…` / `ead17044…`, with an engine-created WAL), which is precisely why the no-open rule for masters is non-negotiable. Temporary artifacts remain outside committed evidence (system temp; the evidence `.gitignore` additionally blocks `staging/`/`work/`). Provenance is preserved across staging via `PROVENANCE.md` + hash receipts.

## 4. Historical refusal/compatibility results

All four runs passed (`ok=true`, exit 0). Evidence: `Q1-P13B.json/.log`, `Q1-P13B-REPEAT.json/.log`, `Q1-GRAPH-ONLY.json/.log`, `Q1-GRAPH-ONLY-REPEAT.json/.log`.

### Pre-REM (Set A) — P13b refusal reproduction

| Field | Observation |
| --- | --- |
| Historical format identity | Pre-canonical Andirit occurrence store (pre-REM generation, noncanonical occurrence/target identities) |
| Supported reader/open operation | `new LatticeCodeGraphStore(copy)` — open |
| Expected under the accepted contract | Store-wide refusal with `CodeOccurrenceSchemaException`; no reinterpretation (P13b, matrix §10/D5) |
| Actual observed | `openSucceeded=false`; `exceptionType = CodeOccurrenceSchemaException`; message: *"The occurrence payload is not canonical or is incompatible with the accepted occurrence contract."* |
| Detection stage | **Open-time**, during store construction (`RestoreOccurrences` → occurrence-payload deserialization); all reads and graph-only access are therefore refused — refusal is store-wide |
| Exception/refusal type | `CodeOccurrenceSchemaException` (the accepted type; internal to the store) |
| Publication/metadata state | Not exposed — construction fails before any reader use; staged-copy mutation disclosed in the receipts |
| Provenance | Authentic historical input (exact copy of the hash-verified master), fresh process (pids 38688, 25568), staged pair including the WAL |
| Reproduces accepted receipt? | **Yes** — same refusal type and store-wide outcome as P13b; deterministic across two independent fresh runs (the concrete message text comes from the current canonical-payload refusal path and is recorded verbatim, not claimed identical to the historical message) |

### R2 baseline graph-only (Set B) — H-01/H-02 reproduction

| Field | Observation |
| --- | --- |
| Historical format identity | Graph-only schema-2 store; no occurrence meta/units/pages; publication run `historical-master-1` |
| Supported reader/open operation | Open; published-unit read; qualified-name lookup; publication-bound callers traversal; bounded occurrence query |
| Expected under the accepted contract | Open succeeds; legacy graph remains usable (H-01, callers ≥ 1); occurrence query reports unavailable/refusal, never successful empty (H-02; matrix §10 rows 1–2) |
| Actual observed | `openSucceeded=true`; `publishedUnitCount=1`; `Logger.Write(string)` found (`loggerNodeCount=1`); publication `historical-master-1`, index identity `968e983704910763df7f897212847882d087e069926557fd51c1328820628274`; **caller edges = 1**, not truncated; occurrence query → `AnalysisUnavailable`, 0 occurrences, `Examined=0`, `IsComplete=false`, no coverage, reason *"Occurrence analysis is unavailable for this publication; reindex required."* |
| Detection stage | Graph-only history detected at open (no occurrence meta → analysis unavailable, never complete-empty); availability reported at query time by the bounded reader |
| Exception/refusal type | No exception: the current accepted contract returns `AnalysisUnavailable` as a status. (The R2-era experiment expressed H-02 as a thrown `OccurrenceAnalysisUnavailableException` in its B2 backend; the accepted current mapping is matrix §10 row 2 / IF-05 — recorded as a mapping note, not a divergence) |
| Publication/metadata state | Authoritative graph publication readable; occurrence authority never fabricated; staged-copy mutation + engine-created WAL disclosed |
| Provenance | Authentic historical input (exact copy of the hash-verified master), fresh processes (pids 20512, 42548) |
| Reproduces accepted receipt? | **Yes** — H-01 (callers ≥ 1) and H-02 (unavailable, not empty) reproduced; results deterministic across two independent fresh runs |

**H-03 applicability.** The R2 H-03 check used `c-empty.db` — a disposable **experiment-lineage** artifact created by the experiment harness under `historical-r2/`, with no recorded immutable provenance and no hash binding; it is not one of the two accepted Andirit prior-writer sets. It is therefore **not bound and not reproduced** in this package (recorded as not applicable to Sets A/B; a future bounded binding would require its own provenance assessment — it is not Andirit prior-writer evidence).

**Authentic vs constructed.** All §4 inputs are exact copies of authentic, hash-verified masters. No constructed control was used in, or labeled as, the historical reproductions.

## 5. Experimental-codec authenticity register

Formats named by the acceptance matrix §13 / S1 reconciliation SQ-04 that lack authentic prior-writer evidence. **No manufactured fixture is labeled authentic; constructed serialization tests are not prior-writer compatibility evidence; no obligation is waived here** — each case is an explicit owner-amendment candidate. Source inspection receipt: `Q1-CODEC-SEARCH.txt` (no superseded codec labels exist in `src/`; the single pattern match is the *current* constant `andirit-occurrence-facts/1`).

| # | Format | Historical writer existence | Authentic artifact availability | Current reader support/refusal | Existing synthetic/constructed coverage | Missing historical evidence | Mandatory for SQ-04? | Genuine writer/artifact locatable? | Required owner amendment/exception | Residual risk | Consequence (WP-S25-1 / Stage-2) |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | `occurrence-facts/1..3` (experiment native-format lineage) | Andirit never shipped these labels; the experiment (exp-02) used them; its "old native" refusal (H-07) was produced against a store **constructed inside the R2 probe run itself** (fresh db + `Ci1OccMeta` nodes written by the probe; probe source retained in the R2 package) | **None** (constructed control only) | Andirit does not read these labels at all (D5; no code paths) | Fresh-store version/namespace tamper tests in `Andirit.LatticeDb.Tests` (synthetic, not historical) | An authentic store written by genuinely historical writer code for each named version | Yes (unless amended) | Not established — would require the original experiment writers; not Andirit prior-writer output | Required: record the limitation per format | An amendment-only claim; no authentic compatibility can ever be asserted from constructed controls | Open → amendment; not a WP-S25-1 binding failure |
| 2 | `usage1..3` (superseded tuple generations) | Only in experiment-era contract documents (`usage4` is the accepted current tuple); no released prior writer in the Andirit lineage | **None** | Not parsed by Andirit (D5) | The R2 H-04 refusal used a **constructed JSON snapshot string** (`{"format":"occurrence-facts/3","semantics":"usage3-…"}` handed to an experiment import API) — a constructed serialization test | Authentic prior-writer stores per tuple generation | Yes (unless amended) | Not established (experiment lineage) | Required | Amendment-only | Open → amendment |
| 3 | `asmctx1/2` (superseded context generations) | Same as above — experiment lineage only; `asmctx3` is the accepted current identity | **None** | Not parsed by Andirit (D5) | None beyond the general version/namespace tamper tests | Authentic prior-writer stores carrying those identities | Yes (unless amended) | Not established | Required | Amendment-only | Open → amendment |

**Amendment template (for `WP-S25-1-OWNER` or the Stage-2 acceptance decision).** For each affected format: affected format; unsupported historical claim; alternative evidence (if any — clearly labeled non-authentic; none is offered here); residual risk; future obligation (re-encountered authentic writer / Stage 6 as applicable); effect on S2.5 and Stage-2 acceptance. **If the contract required unavailable authentic evidence with no permitted exception, the affected Stage-2 acceptance claim stays blocked** — the accepted F15/REM §7 route permits a recorded limitation, but the owner must record it.

## 6. SQ-04 coverage matrix

| Historical format / case | Evidence class | Writer/reader compatibility boundary | Schema/format version | Expected open/refusal behavior | Observed result | Applicable accepted invariant | Evidence artifact | Qualification status | Remaining limitations |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Pre-REM occurrence store (Set A) | **Authentic historical** | Pre-REM writer → current accepted store (reader refuses; no migration) | Pre-canonical Andirit generation | Store-wide refusal (`CodeOccurrenceSchemaException`), no reinterpretation | Refused at open; store-wide; deterministic ×2 | D5 refusal; SQ-04; REM-01 §compatibility | `Q1-P13B.json`, `Q1-P13B-REPEAT.json`; fixtures bound | **Reproduced (producer)**; pending independent qualification | One generation only; refusal message wording differs historically (type identical) |
| Baseline graph-only schema-2 store (Set B) | **Authentic historical** | Baseline graph writer → current accepted store (graph readable; occurrence unavailable) | Schema 2 (current) | Open succeeds; graph usable (H-01); occurrence `AnalysisUnavailable` (H-02) | Reproduced exactly (units=1, callers=1, `AnalysisUnavailable`, not empty); deterministic ×2 | Matrix §10 rows 1–2; register SQ-04/D5 | `Q1-GRAPH-ONLY.json`, `Q1-GRAPH-ONLY-REPEAT.json`; fixtures bound | **Reproduced (producer)**; pending independent qualification | H-02 expressed as a status under the current contract (historical experiment threw a typed exception — mapping recorded) |
| C-era empty store (R2 H-03) | Experiment-lineage disposable artifact | n/a (not an Andirit prior-writer master) | n/a | n/a | **Not reproduced (not applicable)** | None counted | — | Not bound; not claimed | Would need its own provenance assessment before any use |
| `occurrence-facts/1..3` | **Missing authentic evidence** | n/a | n/a | Must never be silently read (D5) | H-07 used a constructed store; not applicable as historical evidence | D5 | §5 register | **Open — amendment candidate** | See §5 |
| `usage1..3` | **Missing authentic evidence** | n/a | n/a | D5 | H-04 was constructed JSON | D5 | §5 register | **Open — amendment candidate** | See §5 |
| `asmctx1/2` | **Missing authentic evidence** | n/a | n/a | D5 | None beyond synthetic tamper | D5 | §5 register | **Open — amendment candidate** | See §5 |
| Version-99 / namespace-tamper / page-mismatch cases | **Synthetic (constructed)** | Current writer + tampered store | Current schema | Explicit refusal (`CodeOccurrenceSchemaException` / `CodeOccurrenceIndexException`) | Covered by existing suites (§7 focused run: 6/0) | Matrix §10 rows 3–5 | `focused-historical-tests.log` | Accepted synthetic controls | Explicitly **not** historical evidence |

**Separation (per B5).** Proven historical compatibility: Set B graph behavior. Proven historical refusal: Set A (store-wide) and Set B occurrence unavailability. Synthetic compatibility/refusal: current-suite tamper tests. Unsupported historical claims: none presented. Missing evidence: the three codec families (§5). Owner-amendment candidates: the three codec families. **SQ-04 is not discharged** by this producer package; it is bound for the available formats and register-complete for the unavailable ones.

## 7. Regression receipts

Environment for all receipts: Windows `10.0.26200` x64; .NET SDK `10.0.401`; runtime `10.0.12`. Commands and outputs are committed verbatim in the evidence logs.

| Gate | Command | Result | Receipt |
| --- | --- | --- | --- |
| Release build | `dotnet build Andirit.slnx -c Release` | **0 warnings / 0 errors**, exit 0 | `release-build.log` |
| Full solution (previous baseline, before any new tests — none added) | `dotnet test Andirit.slnx -c Release --no-build` | **907 passed / 0 failed / 0 skipped** (52 + 157 + 345 + 353), exit 0 | `full-solution-tests.log` |
| Focused historical/open/refusal | `dotnet test tests/Andirit.LatticeDb.Tests --no-build --filter "FullyQualifiedName~Store_Rejects\|FullyQualifiedName~S23Wp5NamespaceRefusalTests"` | **6 passed / 0 failed**, exit 0 | `focused-historical-tests.log` |
| Frozen R2 oracle | `SemanticCorpusOracle -c Release --corpus …/corpus --expected …/oracle-expected-r2.json` | **185/185**, exit 0; `oracle-r2.json` = **146,851 bytes**, SHA-256 `97f6c5e5a4cc77e089481c7643732b7a07a31d483c2643bd7a95fdad7ced39c9` (byte-identical to the accepted value) | `oracle.log`, `oracle-r2.json` |
| Formatting | `dotnet format Andirit.slnx --verify-no-changes` | exit 0 | `format.log` |
| Fixture binding probes | `run-probes.ps1` (4 fresh processes, fresh staged copies) | all `ok=true`, exit 0; committed masters unchanged | `Q1-*.json/.log`, `Q1-INTEGRITY.json` |
| Historical evidence integrity | re-verification of eight committed manifests + the external source masters | 8 packages **0 mismatches**; source masters match committed copies | `Q1-HISTORICAL-INTEGRITY.json` |

**Noninterference.** No changes to production code, storage formats, migrations, or the S2.3/S2.4 contracts (`src/` untouched). No tests were added to `tests/` — the historical cases run as producer probes under `docs/reviews/…-tests/`, deliberately avoiding coupling the standing unit-test suite to historical evidence binaries; the full-solution baseline therefore remains exactly 907/0/0. No regression failure occurred; nothing was repaired or hidden.

## 8. Evidence manifest and integrity verification

- Self-excluding SHA-256 manifest: `manifest.json` — **23 entries, 0 mismatches** (scheme: SHA-256 over exact bytes, sorted relative paths, self-excluded), bound to reviewed HEAD `9b158b82…` / tree `6f09cb73…` (`reviewed-head.txt`).
- The two fixture masters + WAL are committed byte-exact under `fixtures/` with `* -text` / binary attributes (verified via `git check-attr` before staging for commit; the staged-index blob check is executed at commit time and reported in the handoff).
- Historical accepted artifacts were **not modified**: all eight prior evidence packages re-verify 0 mismatches; the external source masters still match the committed copies.
- Safer-open discipline is evidenced: the staged copies mutate on open (disclosed hashes) while every committed original is byte-identical before and after each run.

## 9. Open findings and qualifications

**Findings.** None blocking. No unexpected behavior was found: both authentic sets reproduced their accepted behaviors (P13b refusal; H-01/H-02), deterministically, in fresh processes, with originals untouched. No production defect, no evidence-integrity failure, no fabrication.

**Open items and preserved qualifications.**

1. **F-03 refinement (binding for the no-writer codecs).** "Authentic SQ-04 historical fixtures unavailable" remains true for `occurrence-facts/1..3`, `usage1..3`, `asmctx1/2` — now with a precise register and amendment template; **not fabricated, not discharged**.
2. **H-03 not bound** (experiment-lineage disposable artifact; no provenance binding available in this package).
3. **H-02 mapping note** — current contract returns `AnalysisUnavailable` where the experiment/H-02 expression threw a typed exception (accepted mapping: matrix §10 row 2); recorded, not treated as divergence.
4. **Staged-copy mutation variance** — engine recovery/initialization mutates opened copies (per-run values differ for Set A); disclosed and confined to disposable copies.
5. All carried qualifications remain binding: F-01, F-02, Q1-Q1, Q1-Q2, S-01, Q2-Q01, Q2-Q02, O-Q2-01, Q-I01, Q-I08, F-OBS-Q1-01 (target-local `O(n log n)` pagination), F-OBS-Q1-03 (unkeyed checksum, not authentication), Q2-OBS-01, S1/S3/S4 observations, the same-harness reviewer-independence limitation, Windows-x64 and serialized single-writer, managed fault-injection durability limits, and the restricted real-authority cross-unit union. SQ-05 (Linux), S-01 (integrated proof), D3-full, SQ-06 remainder, and the Stage-2 final integration remain separate, untouched obligations. D4.5 and full S-02 remain Stage 6 assignments.

## 10. Independent qualification handoff

An independent SQ-04 qualification reviewer (distinct from this producer wherever feasible) should:

1. **Independently authenticate both fixture sets** — re-hash the committed fixtures (`fixtures/prerem/prerem-authentic.ltdb` + `.ltdb-wal`, `fixtures/baseline/baseline-graph-only.master.db`) against the complete values in §2/`PROVENANCE.md`; optionally re-hash the external source masters to confirm the committed copies remain faithful.
2. **Verify original and staged hashes** — confirm the staging procedure (`run-probes.ps1`) uses complete hashes, stages the pre-REM pair together, and re-verifies masters after execution.
3. **Independently execute the historical cases** — reviewer-owned probing (fresh processes) against reviewer-staged disposable copies; expected outcomes derived from the accepted contracts (P13b; H-01/H-02; matrix §10 rows 1–2), **not from this producer's receipts**.
4. **Verify exception and detection boundaries** — open-time `CodeOccurrenceSchemaException` for the pre-REM store (store-wide); graph-only open success with callers ≥ 1; occurrence `AnalysisUnavailable` with no complete-empty; no false-available anywhere.
5. **Verify no mutation of original masters** — before/after hashing; the reviewer should also confirm the staged-copy open-mutation disclosure matches observed behavior.
6. **Keep genuine historical artifacts separate from constructed cases** — the fixtures vs the synthetic tamper controls and the experiment-lineage artifacts (H-03/H-04/H-07 provenance analysis in §4/§5).
7. **Verify the experimental-codec limitations** — confirm from source that no superseded codec labels exist in `src/` and that the named formats have no authentic writers; treat §5 as the amendment checklist.
8. **Verify evidence-manifest integrity** — re-seal `manifest.json` (23 entries, self-excluded) and confirm 0 mismatches; verify historical packages remain untouched.
9. **Repeat regression and noninterference checks** — full solution, frozen oracle byte-identical, Release/format; confirm no production diff.
10. **Classify findings** — blocking: hash mismatch, master mutation via opened originals, reinterpretation/migration attempt, false-available or fabricated facts, manifest failure; non-blocking: message-text differences, staged-copy mutation variance, mapping notes.

**Reuse rule.** Accepted historical evidence (REM-01-Q1 P13b; R2 H-01..H-07) may be reused as **inherited** evidence, but must not be represented as newly executed independent qualification. This producer package's receipts are **producer evidence**, not independent qualification.

## 11. Producer disposition

**`FIXTURE BINDING COMPLETE — READY FOR INDEPENDENT QUALIFICATION`.**

Both authentic fixture sets are safely bound: complete hashes verified at import and on every run; byte-exact committed copies with enforced attributes; a reproducible staging procedure that never opens the originals; deterministic fresh-process reproduction of the accepted P13b refusal and H-01/H-02 behaviors; and a complete experimental-codec register. The codec obligations remain explicitly open as owner-amendment candidates and are **not** presented as discharged. No blocking finding exists. Independent SQ-04 qualification and the `WP-S25-1-OWNER` acceptance remain separate commissions.

## 12. Recommended next owner action

1. **Commission the independent SQ-04 qualification** (`WP-S25-1-QUAL`, reviewer ≠ producer per §10) on this bound package; then record the separate **`WP-S25-1-OWNER`** decision (acceptance bounded to the available formats).
2. **Record the no-writer amendment** at that owner decision or at Stage-2 acceptance (§5 template) — do not let the limitation pass silently.
3. **Keep the SQ-05 Linux route decision separate** (route A push authorization or route B native runner) and do not treat entry authorization as push authorization.
4. **Do not reopen WP-S24-3**, and do not expand S2.5 scope: S-01 (WP-S25-3) and the final integration (WP-S25-4) remain separately planned and unauthorized.

---

*Additive producer evidence only. No production code, test, stored format, accepted contract, prior decision, or historical artifact was modified. No Linux execution, no push, no owner acceptance. Receipts, fixtures, probe, and scripts are committed under `docs/reviews/CI1A-S2.5-WP-S25-1-BIND-01-evidence/` and `docs/reviews/CI1A-S2.5-WP-S25-1-tests/`; the handoff reports the final commit/tree identities.*
