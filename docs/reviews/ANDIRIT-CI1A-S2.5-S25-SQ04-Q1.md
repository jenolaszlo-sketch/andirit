# ANDIRIT-CI1A-S2.5-S25-SQ04-Q1 — independent SQ-04 qualification of the historical-fixture binding

**Commission:** `ANDIRIT-CI1A-S2.5-S25-SQ04-Q1` (`S25-SQ04-Q1 INDEPENDENT QUALIFICATION AUTHORIZED WITH QUALIFICATIONS`).
**Project:** Andirit. **Recorded:** 2026-11-10. **Canonical branch:** `main`.
**Reviewed source:** `bf1af232575b68fd1c8d61b9397c57608a7be2a6` (tree `f2195c26d1e20dff73513dbffe4090ddab44e67e`) — the BIND-01 deliverable commit.
**Role:** independent reviewer (distinct session from the BIND-01 producer; same-harness limitation disclosed in §9/§11).
**Final disposition:** **`SUPPORTED WITH QUALIFICATIONS`**.

> This qualification covers the bounded WP-S25-1 fixture-binding package: authentication of the
> two authentic prior-writer fixture sets, staging isolation, and independent reproduction of
> the accepted P13b and H-01/H-02 behaviors. It authorizes no production remediation, no
> historical-fixture modification, no SQ-04 amendment, and no `WP-S25-1-OWNER` acceptance. The
> experimental-codec obligations remain open and remain a blocker for any broader SQ-04 or
> Stage-2 claim that requires authentic prior-writer evidence under the unamended contract.

---

## 1. Verified preflight and Git state

| Check | Result |
| --- | --- |
| HEAD / tree | `bf1af232575b68fd1c8d61b9397c57608a7be2a6` / `f2195c26d1e20dff73513dbffe4090ddab44e67e` — both expected; the HEAD commit is the BIND-01 deliverable |
| Branch / worktrees | `main`; exactly one (`D:/projects/Andirit`); clean tracked/index; no stashes; no concurrent writer |
| Local vs remote | recorded `origin/main` is **0 behind / 68 ahead** (no fetch performed); nothing pushed |
| Environment | Windows `10.0.26200` **x64**; .NET SDK `10.0.401`; runtime `10.0.12` |
| Authority chain | S2.5 entry decision §6; [S2.5 work-package plan](../../planning/adr-ci1-01-ci1a-s2-5/work-package-plan.md) WP-S25-1; entry assessment §4; BIND-01 report + `fixtures/PROVENANCE.md`; acceptance matrix §13; REM-01 owner acceptance (P13b); R2 historical matrix (H-01…H-03) |

No unexpected repository state, evidence-integrity failure, or authority conflict. No reset/clean/restore/rebase/prune/worktree/fetch/push was performed. Reviewer artifacts: `docs/reviews/CI1A-S2.5-S25-SQ04-Q1-tests/` (probe + scripts) and `docs/reviews/CI1A-S2.5-S25-SQ04-Q1-evidence/` (receipts + manifest).

## 2. Complete historical fixture identity and hashes (Q1)

Method: complete SHA-256 recomputed **directly from Git blob bytes** (`git cat-file blob HEAD:<path>`), from working-tree bytes, and from the **external originals**; byte lengths verified; complete hashes asserted to be recorded in the committed `PROVENANCE.md` (never abbreviated); writer commits verified **as Git objects in this repository**; binary attributes checked; regeneration check. Receipt: `Q1-AUTH.json` — **26 checks, 0 failed**.

| Artifact | Committed path (BIND-01 evidence) | Complete SHA-256 | Bytes | Git blob == work == external |
| --- | --- | --- | --- | --- |
| Set A: pre-REM occurrence store | `…BIND-01-evidence/fixtures/prerem/prerem-authentic.ltdb` | `7646b98288a9803cff6f9930c78a3e7df5c676f6fc4e0cd2df2af5e79bc68885` | 86,016 | ✓ / ✓ / ✓ |
| Set A: pre-REM WAL | `…BIND-01-evidence/fixtures/prerem/prerem-authentic.ltdb-wal` | `8ca170cdc55a4428a5506bcb447a0667ab10c92c77ef149ae36460a5603aeab4` | 4,096 | ✓ / ✓ / ✓ |
| Set B: R2 baseline graph-only store | `…BIND-01-evidence/fixtures/baseline/baseline-graph-only.master.db` | `f21a72bee2c2e378e16766ca7b84c29010ab4d50360bf3111a82706f947781e0` | 163,840 | ✓ / ✓ / ✓ |

**Writer provenance, verified from Git objects.** Set A writer commit `4c6d26a1d103a62fffe2e2743467eedb5f21afc2` exists as a commit object with tree `320e965f219d0b615da29665d7fa3e85f8bbfc15` ("docs(andirit): accept CI1A Stage 1 with qualifications", 2026-11-01) — exactly the recorded Q1B baseline. Set B writer commit `31c33a934aa13e373d9a3a6ffbc54d789eef092c` exists with tree `a265f561243de37e1636490f99acb0b34245809e` ("docs(ci1): record R3 integration commit identity in final handoff", 2026-10-31) — the frozen R2 baseline. **Format/schema lineage**: Set A = pre-canonical Andirit occurrence generation (refused, never interpreted; accepted current namespace `andirit-occurrence-facts/1`, `CurrentOccurrenceSchemaVersion = 1`); Set B = Andirit graph-only store at schema version 2 (= current `LatticeCodeGraphStore.CurrentSchemaVersion`), publication run `historical-master-1`.

**Not regenerated**: the fixture directory was introduced by exactly one commit (`bf1af23`, the reviewed HEAD) — `git log` over `fixtures/` shows a single commit; Git blob bytes equal working bytes; `git check-attr` confirms `text: unset` (and `binary: set` for both `.ltdb`/`.db`), so no EOL transformation can occur on add/checkout.

## 3. Staging isolation evidence (Q2)

**Producer-implementation audit.** `run-probes.ps1` passes only **staged** paths to the probe; the original paths are used exclusively for hashing (`Get-Sha256`/`Assert-Hash`) and never handed to `LatticeCodeGraphStore`; `BindProbe` opens only `args[1]` (staged) and hashes the original. No code path passes an original to a mutating operation — **no blocking staging path found**.

**Reviewer-owned staging verification** (`stage-and-run.ps1` — written independently of the producer's script; receipt `Q2-STAGING.json`):

| Invariant | Evidence |
| --- | --- |
| Originals never passed to a mutating operation | probe receives staged paths only; originals hashed before/after each run |
| Database and WAL staged together | Set A staging copies `.ltdb` + `.ltdb-wal` pair; staged hashes asserted equal to masters before any open |
| Fresh disposable copies per execution | 4 runs (run1/run2 pre-REM, run3/run4 graph-only), each with a freshly staged copy; no copy reused across runs |
| Staged input hashes match originals before execution | asserted in `Stage-SetA`/`Stage-SetB` and recorded in `Q2-STAGING.json` |
| Mutations confined to disposable directories | work directory guarded to be **outside the repository** (`workDirOutsideRepo=True`); staged-copy mutation disclosed in receipts (e.g., pre-REM copy `7646b982…` → `64e855bb…`) |
| Original Git blobs unchanged after execution | masters re-hashed (0 mismatches), `git status --porcelain -- fixtures/` empty, `HEAD:` blob hashes re-verified |
| Cleanup cannot delete/modify committed artifacts | work directory lives under the system temp and is removed only on success |
| Repeated runs never reuse contaminated state | each run re-stages from the committed masters |

All four fresh-process runs passed (`ok=true`, exit 0). **No demonstrated path can unintentionally modify the original masters** — the isolation invariant holds.

## 4. P13b independent reproduction (Q3)

Reviewer probe `Sq04QualProbe` (reviewer-owned code, not BindProbe), fresh processes on fresh reviewer-staged copies of the authentic pre-REM database **and WAL** (staged hashes equal to the masters before open). Receipts: `SQ04-P13B.json/.log`, `SQ04-P13B-REPEAT.json/.log` (pids 27048 / 27696).

| Verification | Observation |
| --- | --- |
| Authentic input identity | staged copy SHA-256 = master `7646b982…68885`; WAL `8ca170cd…3aeab4`; both staged together |
| Store-open operation | `new LatticeCodeGraphStore(stagedCopy)` |
| Detection point | **open-time**, during store construction (occurrence-payload canonical deserialization); the store never reaches a usable state — graph-only access is refused too (store-wide) |
| Exception type | `CodeOccurrenceSchemaException` — the accepted type (internal to the store) |
| Refusal message (recorded verbatim) | *"The occurrence payload is not canonical or is incompatible with the accepted occurrence contract."* |
| No fallback / no fabricated results | `openSucceeded=false`; no availability, no occurrence authority, no publication identity was served; nothing was reinterpreted or repaired |
| Repeatability | two independent fresh processes, identical refusal type and store-wide outcome |
| Original fixture integrity | committed `.ltdb`, `.ltdb-wal`, and baseline `.db` byte-identical after execution (hash receipts); staged-copy mutation confined to the disposable copy and disclosed |

**Comparison with the accepted P13b evidence.** The accepted historical receipt (REM-01-Q1 probe P13b) recorded refusal of the authentic pre-REM database at open with `CodeOccurrenceSchemaException`, without reinterpretation. The independent reproduction matches that contract **exactly at the contract level** (exception type, open-time detection, store-wide refusal, no reinterpretation). The concrete message text comes from the current canonical-payload refusal path and is recorded verbatim; no claim is made that the historical message text is byte-identical. **The refusal is not treated as historical compatibility** — it is a refusal claim only.

## 5. H-01/H-02 independent reproduction (Q4)

Fresh processes (pids 4940 / 41416) on fresh staged copies of the authentic baseline graph-only master. Receipts: `SQ04-GRAPH-ONLY.json/.log`, `SQ04-GRAPH-ONLY-REPEAT.json/.log`.

### H-01 — graph compatibility

| Verification | Observation |
| --- | --- |
| Store opens successfully | `openSucceeded=true` |
| Exactly one expected published unit | `publishedUnitCount = 1` |
| Publication identity | run **`historical-master-1`**; index identity `968e983704910763df7f897212847882d087e069926557fd51c1328820628274` |
| `Logger.Write(string)` resolves | exactly **one** node |
| Exactly one expected caller | **one** caller edge, not truncated — independently derived from the committed fixture source `docs/planning/ci1-scope-01/fixtures/Repeated.cs`: `Worker.Execute` is the sole caller of `Logger.Write` (three call sites, one distinct caller) |
| Graph identity/ownership/relationships | node and caller-edge observations match the independently derived expectation; results deterministic across both runs |

### H-02 — occurrence availability

| Verification | Observation |
| --- | --- |
| No accepted occurrence facts available | `occurrenceCount = 0`, `TotalMatches = 0`, `Examined = 0` |
| Availability | `AnalysisUnavailable` — refusal reason *"Occurrence analysis is unavailable for this publication; reindex required."* |
| `IsComplete` | **false** — never a successful empty |
| No fabricated authority | no occurrence authority, coverage, or facts were fabricated from the legacy graph data |

**Experiment-era exception versus current accepted contract (explicit reconciliation).** The R2-era H-02 check asserted a thrown `OccurrenceAnalysisUnavailableException` from the experiment's B2 backend. The **current accepted contract** (acceptance matrix §10 row 2; register IF-05; S2.3/S2.4 reader contract) reports this condition as the query status `AnalysisUnavailable` on `CodeOccurrenceQueryResult` — no exception is thrown. This qualification therefore does **not** claim that the old exception is still produced; it verifies the historical *behavioral claim* ("occurrence analysis unavailable, never a successful empty") under the current accepted status contract. The producer receipt and this independent receipt agree.

## 6. H-03 applicability assessment

**Authoritative definition** (R2 historical matrix): *H-03 — "C empty/legacy store reports `AnalysisUnavailable`, not a successful empty result"*, exercised against `c-empty.db` — a store created **by the experiment harness itself** (experiment C adapter, `CLatticeOccurrenceStore`) under the disposable `historical-r2/` directory, with no recorded immutable provenance.

| Question | Assessment |
| --- | --- |
| Legitimately inapplicable to the bound graph-only master? | **Yes.** H-03's artifact is a C-adapter experiment store, not one of the two accepted Andirit prior-writer fixture sets (pre-REM occurrence store; baseline graph-only store). The bound graph-only master cannot exercise the "C empty/legacy store" case. |
| Supported by another authentic artifact? | No authentic artifact for the C-adapter lineage exists in the bound inventory; the experiment-lineage artifacts are disposable and unbound. |
| Independently qualified through accepted evidence? | The underlying *behavioral claim* (analysis unavailable, never successful empty) is accepted for the current store via the acceptance matrix §10 rows 1–2 and S2.3/S2.4 evidence, and is re-verified here through H-02 on the authentic graph-only master. The C-adapter-specific H-03 execution remains **inherited experiment evidence**, not re-qualified here. |
| Requires additional qualification? | Not for SQ-04 as scoped: H-03 is not one of the SQ-04 prior-writer formats named by acceptance matrix §13. If the owner ever wants C-lineage artifacts bound, that requires its own provenance assessment. |
| Remains open? | Recorded as **inapplicable to the available historical fixture** — explicitly not closed by this review, and not silently converted into an SQ-04 claim. |

## 7. Experimental codec authenticity register (Q5)

Grounding: Andirit `src/` history search — `occurrence-facts/2`, `occurrence-facts/3`, `asmctx1`, `usage1` **never appear** in any commit touching `src/` (receipt `Q5-CODEC-SEARCH.txt`); the accepted current namespace is `andirit-occurrence-facts/1` (`LatticeCodeGraphStore.cs:43`). However — per the commission's caution — **writers did exist historically in the experiment lineage**: retained R1-baseline code contains writer implementations for `occurrence-facts/3` (`CLatticeOccurrenceStore`/`CReferenceOccurrenceStore` `FormatVersion`), `usage3` (`OccurrenceContract` `SemanticsVersion = "usage3-b2c-exp-r1-20261101"`, `IdentityDomain = "usage3"`), `usage1` (`Part("usage1")` frozen-vintage parts), and `asmctx2` (`TargetAssemblyContext`, full identity construction); the R2-era retained C store writes `occurrence-facts/4`; and the exp-02 final report documents `usage2-b2c-exp-20261031` with the `asmctx1` domain. **No authentic store artifact written by any of these writers is retained or bound** — the R2 historical-matrix statement "authentic prior C fact-store writer (none exists)" is a statement about demonstrated *artifacts*, not about the historical existence of writer code.

| Format | Known historical writer & provenance | Authentic prior-writer artifact | Current reader compat/refusal | Constructed/synthetic evidence | Unsupported historical claim | Applicable SQ-04 obligation | Required owner amendment | Consequence (WP-S25-1 / Stage-2) |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `occurrence-facts/1` | None known in the Andirit lineage; experiment lineage used labels /3 and /4 (retained code) | **None** | Never read by Andirit (D5); no label in `src/` history | Fresh-store version/namespace tamper tests (synthetic) | Authentic prior-writer refusal for this schema | Yes (matrix §13) unless amended | Required | Open → amendment; not a WP-S25-1 binding failure |
| `occurrence-facts/2` | Same as above | **None** | Never read by Andirit (D5) | Synthetic tamper tests | Same | Yes unless amended | Required | Open → amendment |
| `occurrence-facts/3` | R1 C-store writer **retained** (`baseline-r1\…\CLatticeOccurrenceStore.cs:48`); H-07's /3 store was **constructed by the R2 probe itself**, not written by that writer | **None** (constructed control only) | R2-era C store refused /3 (`InvalidDataException`); Andirit never reads it | H-07 constructed refusal; synthetic tamper tests | Authentic /3-store refusal | Yes unless amended | Required | Open → amendment |
| `usage1` | R1 `OccurrenceContract` retained (`Part("usage1")`, `usage1-b2c-exp`) | **None** | Not parsed by Andirit | Synthetic identity/oracle tests | Authentic store carrying `usage1` facts | Yes unless amended | Required | Open → amendment |
| `usage2` | exp-02 adapter (`usage2-b2c-exp-20261031`, final report) | **None** | Not parsed by Andirit | Synthetic | Authentic store carrying `usage2` facts | Yes unless amended | Required | Open → amendment |
| `usage3` | R1 `OccurrenceContract` (`usage3-b2c-exp-r1-20261101`) | **None** | Not parsed by Andirit | H-04 constructed JSON snapshot (refused) | Authentic store carrying `usage3` facts | Yes unless amended | Required | Open → amendment |
| `asmctx1` | exp-02 (target-assembly domain `asmctx1`, final report; `asmctx1:unavailable` sentinel refused by validation) | **None** | Not parsed by Andirit | Synthetic | Authentic store carrying `asmctx1` targets | Yes unless amended | Required | Open → amendment |
| `asmctx2` | R1 `TargetAssemblyContext` **retained** (full identity construction) | **None** | Not parsed by Andirit (post-R2 uses `asmctx3`) | Synthetic | Authentic store carrying `asmctx2` targets | Yes unless amended | Required | Open → amendment |

No reviewer amendment or waiver is granted here; no synthetic artifact is represented as authentic.

## 8. SQ-04 coverage and amendment matrix (Q6)

| SQ-04 claim | Classification |
| --- | --- |
| Pre-REM occurrence store: authentic prior-writer refusal at open (store-wide, no reinterpretation) | **Authentically demonstrated refusal** (accepted P13b + this independent reproduction) |
| Baseline graph-only store: authentic prior-writer graph compatibility (open; callers query) | **Authentically demonstrated compatibility** (accepted H-01 + this independent reproduction) |
| Baseline graph-only store: occurrence analysis unavailable, never successful empty | **Authentically demonstrated refusal/availability** (accepted H-02 + this independent reproduction, current status contract) |
| C-era empty/legacy store behavior (H-03) | **Inapplicable to the available historical fixture** (experiment-lineage artifact; inherited evidence; not re-qualified) |
| `occurrence-facts/1..3`, `usage1..3`, `asmctx1/2` authentic prior-writer refusal/compat | **Unsupported due to missing authentic evidence** — demonstrated through constructed controls only (synthetic tamper tests; constructed experiment probes) |
| Those experimental formats' acceptance | **Pending explicit owner amendment** (wording proposed below) |
| Any Stage-2 acceptance claim requiring authentic prior-writer evidence for the named experimental formats | **Blocking under the unamended acceptance contract** (stays blocked until amendment or authentic artifacts appear) |

**Decision A — does WP-S25-1 provide independently qualified authentic historical-fixture evidence?** **Yes, for the two bound sets, with the qualifications in §11.** Authentication is complete and reproducible, staging isolation holds, and the accepted historical behaviors reproduce independently.

**Decision B — is the complete SQ-04 obligation discharged?** **No.** The formats named by acceptance matrix §13 (`occurrence-facts/1..3`, `usage1..3`, `asmctx1/2`) have no authentic prior-writer artifacts; only constructed/synthetic coverage exists. These two decisions are **not equivalent**, and this review keeps them separate.

**Proposed amendment wording (for owner consideration; NOT recorded or applied here).**

> *SQ-04 amendment proposal — experimental occurrence codecs.* The authentic prior-writer
> evidence requirement of SQ-04 is satisfied for the available Andirit prior-writer formats
> (pre-REM occurrence store; baseline graph-only schema-2 store) via the bound fixtures of
> `ANDIRIT-CI1A-S2.5-WP-S25-1-BIND-01`, independently qualified by
> `ANDIRIT-CI1A-S2.5-S25-SQ04-Q1`. For the superseded experimental codecs
> `occurrence-facts/1`, `occurrence-facts/2`, `occurrence-facts/3`, `usage1`, `usage2`,
> `usage3`, `asmctx1`, and `asmctx2`: no authentic prior-writer artifact is retained or
> accessible; the unsupported claims are authentic prior-writer refusal and compatibility for
> stores in those formats; the residual risk is that an authentic store in one of these
> formats could exist outside the retained evidence and its refusal behavior would remain
> unverified until encountered; Andirit never shipped these schemas and no code path reads
> them (D5; verified by history search), so no runtime compatibility risk arises from current
> code. Future obligation: if an authentic artifact in any of these formats is ever obtained,
> SQ-04 refusal evidence for it is required before any compatibility or refusal claim.
> Effect: Stage-2 acceptance may record SQ-04 as **satisfied for the available formats with
> an explicit, named limitation** for the eight experimental codecs above, without blocking
> the bounded Stage-2 acceptance; the limitation must be restated in the acceptance decision
> and must not be presented as full SQ-04 discharge.

## 9. Evidence provenance classification (Q7)

| Class | Items |
| --- | --- |
| Authentic historical masters | The two fixture sets; external originals (`…rem-01-q1\evidence\prerem-authentic.ltdb[.wal]`, `…exp-02-r2\evidence\historical-master\baseline-graph-only.master.db`) re-hashed and matching |
| Byte-exact committed copies | `…BIND-01-evidence/fixtures/**` — verified against Git blob bytes and the external originals (Q1) |
| Disposable staged copies | Four reviewer-staged copies (system temp), each hash-verified before open, mutated by the engine (disclosed), removed after success |
| Independent reviewer execution | This commission: `Sq04QualProbe` (fresh reviewer-owned code), `verify-fixtures.ps1`, `stage-and-run.ps1`; four fresh OS processes (pids 27048, 27696, 4940, 41416); all commands, counts, hashes and exit codes in the receipts |
| Producer execution | BIND-01 `run-probes.ps1` receipts — used **only as cross-checks**, never as expectation sources |
| Previously accepted historical receipts | P13b (REM-01-Q1), H-01…H-03 (R2 historical matrix) — inherited evidence, not re-executed |
| Constructed controls | Existing synthetic tamper tests (version 99 / namespace / page-mismatch); experiment-constructed H-04/H-07 artifacts |
| Unsupported historical claims | The eight experimental codecs (§7) — no authentic artifacts; constructed evidence never treated as authentic |

**Commands and process boundaries.** `dotnet build …Sq04QualProbe.csproj -c Release` (0/0); `verify-fixtures.ps1` (single process, git-only + hashing, no store opened); `stage-and-run.ps1` (four `dotnet run` fresh processes, each against its own staged copy); gates (§10). **Independence disclosure:** the reviewer session is distinct from the BIND-01 producer session, but both ran in the same agent harness — the same-harness reviewer-independence limitation applies and is carried bindingly.

## 10. Regression and integrity receipts (Q8)

| Check | Result | Receipt |
| --- | --- | --- |
| BIND-01 evidence manifest | **24 entries, 0 mismatches** | `Q8-MANIFESTS.json` |
| Eight predecessor manifests (QUAL-PREP 21, SURFACE-Q1 28, READER-Q1 27, READER-Q2 41, R1 19, FINAL-Q1 17, S23-LIFE-Q1 32, S23-WP-S23-5-QUAL-01 32) | all **0 mismatches** | `Q8-MANIFESTS.json` |
| Committed fixture hashes | all three byte-exact (`7646b982…`, `8ca170cd…`, `f21a72be…`) | `Q1-AUTH.json`, `Q8-MANIFESTS.json` |
| Frozen R2 oracle | **185/185**, exit 0; `oracle-r2.json` **146,851 bytes**, SHA-256 `97f6c5e5a4cc77e089481c7643732b7a07a31d483c2643bd7a95fdad7ced39c9` (byte-identical) | `oracle.log`, `oracle-r2.json` |
| Full solution | `dotnet test Andirit.slnx -c Release --no-build` → **907 passed / 0 failed / 0 skipped** (52 + 157 + 345 + 353), exit 0 | `full-solution-tests.log` |
| Focused historical/refusal | `dotnet test tests/Andirit.LatticeDb.Tests --no-build --filter "FullyQualifiedName~Store_Rejects\|FullyQualifiedName~S23Wp5NamespaceRefusalTests"` → **6 passed / 0 failed**, exit 0 | `focused-historical-tests.log` |
| Release build | `dotnet build Andirit.slnx -c Release` → **0 warnings / 0 errors**, exit 0 | `release-build.log` |
| Formatting | `dotnet format Andirit.slnx --verify-no-changes` → exit 0 | `format.log` |
| Fresh independent historical probes | 4/4 `ok=true`, exit 0 (Q3/Q4) | `SQ04-*.json/.log` |

Previously accepted evidence was never resealed or modified; the Q1-local evidence package is self-contained with its own self-excluding manifest (`manifest.json`, sealed at commit time).

## 11. Findings and binding qualifications

**Findings.** No blocking finding. Observations (non-blocking, carried):

| ID | Observation | Disposition |
| --- | --- | --- |
| Q4-OBS-01 | Same-harness reviewer-independence limitation (distinct session, same harness as the BIND-01 producer) | Preserved bindingly; independence rests on reviewer-owned probes, independently derived expectations, and the immutable committed fixtures |
| Q4-OBS-02 | H-02 is expressed by the current contract as the `AnalysisUnavailable` status; the experiment-era thrown exception is **not** produced | Mapping recorded (§5); no divergence claimed |
| Q4-OBS-03 | H-03 is inapplicable to the bound graph-only master and remains inherited experiment evidence | Recorded (§6); not closed |
| Q4-OBS-04 | Staged-copy byte mutation on open varies per run (recovery/initialization) | Disclosed and confined to disposable copies; masters never opened |
| Q4-OBS-05 | The pre-REM refusal message text comes from the current canonical-payload refusal path (type identical to the accepted refusal) | Recorded verbatim; no message-text-identity claim |

**Binding qualifications carried forward (unchanged):** F-01, F-02, F-03 (now refined: unavailable for the eight experimental codecs, satisfied-and-qualified for the two bound formats), Q1-Q1, Q1-Q2, S-01, Q2-Q01, Q2-Q02, O-Q2-01, Q-I01, Q-I08; same-harness reviewer independence; Windows-x64 and serialized single-writer; managed fault-injection durability limits; restricted real-authority cross-unit union; F-OBS-Q1-01; F-OBS-Q1-03; Q2-OBS-01; S1/S3/S4 observations. Outstanding S2.5 obligations (SQ-05 Linux, S-01 integrated proof, D3-full, SQ-06 remainder, Stage-2 final integration) are untouched; **D4.5 and full S-02 remain assigned to Stage 6**. No public Find Usages, no cross-target implementation, no storage migration, no dual-write, no pagination optimization, no Stage 6 development.

## 12. Final qualification disposition

**`SUPPORTED WITH QUALIFICATIONS`.**

The authentic fixtures, safe staging, and applicable historical behaviors of WP-S25-1 are independently supported without a blocking defect: both fixture sets authenticate completely (Git-blob, working-tree, and external-original hashes; writer commits verified as Git objects; no regeneration), staging isolation holds under a reviewer-owned procedure, and the accepted P13b refusal and H-01/H-02 behaviors reproduce deterministically in fresh processes with the originals untouched. The qualification is **qualified** because (1) the eight experimental codecs lack authentic prior-writer artifacts — an unresolved obligation that remains an **explicit blocker for any broader SQ-04 or Stage-2 claim requiring authentic prior-writer evidence under the unamended contract**; (2) H-03 remains inapplicable/unre-qualified for the C-lineage artifact; (3) the H-02 status-contract mapping and the same-harness limitation are recorded; (4) no owner amendment exists yet, and this review grants none.

## 13. Recommendation for WP-S25-1 owner acceptance and separate SQ-04 amendment handling

1. **Proceed to the separate `WP-S25-1-OWNER` decision**, bounded to the two available formats: accept the bound fixture package and its independent qualification, carry §11's qualifications, and record that SQ-04 is **satisfied for the available formats with an explicit, named limitation** for the eight experimental codecs.
2. **Record the SQ-04 amendment** (§8 proposed wording, adjusted as the owner sees fit) **at that owner decision or at the Stage-2 acceptance decision** — the amendment is an owner action, not a reviewer output, and until recorded the affected Stage-2 claims remain blocked under the unamended contract.
3. **Do not treat SQ-04 as fully discharged** in any later document; restate the limitation wherever SQ-04 is cited.
4. **Keep the SQ-05 Linux route decision and the WP-S25-3 (S-01) commission separate and unauthorized**; WP-S25-4 (final integrated Stage-2 qualification) follows only after WP-S25-1/2/3 are owner-accepted.

---

*Additive reviewer evidence only. No production code, test, stored format, accepted contract, prior decision, or historical artifact was modified; previously accepted evidence was never resealed. No push occurred. The final commit/tree identities and working-tree state are reported in the handoff.*