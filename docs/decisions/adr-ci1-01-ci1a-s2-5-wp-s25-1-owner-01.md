# ADR-CI1-01-CI1A-S2.5-WP-S25-1-OWNER-01 — qualified WP-S25-1 owner acceptance (authentic historical-fixture binding)

**Commission:** `ANDIRIT-CI1A-S2.5-WP-S25-1-OWNER-01`.
**Project:** Andirit. **Recorded:** 2026-11-10. **Canonical branch:** `main`.
**Owner decision:** **`WP-S25-1 ACCEPTED WITH QUALIFICATIONS`**.
**Gate:** **`WP-S25-1-OWNER` — the bounded SQ-04 authentic historical-fixture binding (two available prior-writer fixture sets + reproducible staging + independent reproduction) ACCEPTED within the qualified scope below.**

> **Status: RECORDED — OWNER-AUTHORIZED.**
>
> WP-S25-1 is accepted on the BIND-01 producer evidence and the independent
> `S25-SQ04-Q1` qualification, for the **two available authentic fixture sets** and their
> applicable behaviors. This is **not** an SQ-04 discharge: the eight experimental codecs
> (`occurrence-facts/1..3`, `usage1..3`, `asmctx1/2`) remain open without authentic
> prior-writer artifacts, and **`SQ-04 NOT FULLY DISCHARGED — OWNER AMENDMENT OR ADDITIONAL
> AUTHENTIC EVIDENCE REQUIRED`** is recorded as Decision B. No amendment is granted by this
> decision; no successor work is authorized.

This is an additive owner decision. It changes no production source, test, fixture, reviewer evidence, oracle or corpus, prior decision, or historical review artifact.

## 1. Verified repository preflight

| Object | Value |
| --- | --- |
| Entry HEAD / tree | `3d3980ab168d728b8be0380390ae0e489636083d` / `0ed0221cc47224e836915376e85ed658f7bd228d` |
| Branch / worktrees | `main`; exactly one (`D:/projects/Andirit`); clean tracked/index; no stashes; no concurrent writer |
| Local vs remote | recorded `origin/main` is **0 behind / 69 ahead** (no fetch performed); nothing pushed |
| Environment | Windows `10.0.26200` **x64**; .NET SDK `10.0.401`; runtime `10.0.12` |

No unexpected repository state, authority conflict, or evidence-integrity failure. No reset/clean/restore/rebase/prune/worktree/fetch/push was performed and no qualification was executed (documentation-only commission).

## 2. Qualification-chain verification

| Step | Commit | Record |
| --- | --- | --- |
| S2.5 entry assessment | `918565b` | `ENTRY READY WITH QUALIFICATIONS` |
| S2.5 entry owner | `9b158b8` | `S2.5 ENTRY AUTHORIZED WITH QUALIFICATIONS`; WP-S25-1 **planned, not authorized** |
| WP-S25-1 owner authorization | owner commission (recorded in the BIND-01 report header) | `WP-S25-1 FIXTURE BINDING AUTHORIZED WITH QUALIFICATIONS` — bounded producer evidence preparation |
| BIND-01 producer | `bf1af23` | `FIXTURE BINDING COMPLETE — READY FOR INDEPENDENT QUALIFICATION` (fixtures bound, staging procedure, reproduction receipts, codec register) |
| S25-SQ04-Q1 independent qualification | `3d3980a` | `SUPPORTED WITH QUALIFICATIONS` (26/0 authentication checks; 4/4 fresh-process reproductions; staging isolation verified; no blocking finding) |

Git ancestry is consecutive (`2fbf209` → `918565b` → `9b158b8` → `bf1af23` → `3d3980a`); every step was separately authorized and none implies its successor. The reviewer disposition is **evidence**, and the owner acceptance below is recorded only after the re-verification in §§3–9.

## 3. Authentic fixture identities (complete hashes, not abbreviated)

| Artifact | Complete SHA-256 | Bytes | Working == Git blob == external original |
| --- | --- | --- | --- |
| Set A: pre-REM occurrence store | `7646b98288a9803cff6f9930c78a3e7df5c676f6fc4e0cd2df2af5e79bc68885` | 86,016 | ✓ / ✓ / ✓ |
| Set A: pre-REM WAL | `8ca170cdc55a4428a5506bcb447a0667ab10c92c77ef149ae36460a5603aeab4` | 4,096 | ✓ / ✓ / ✓ |
| Set B: R2 baseline graph-only store | `f21a72bee2c2e378e16766ca7b84c29010ab4d50360bf3111a82706f947781e0` | 163,840 | ✓ / ✓ / ✓ |

Owner re-verification at this commission (read-only): working-tree bytes and `HEAD` Git-blob bytes recomputed and equal to the accepted values above; the independent Q1 authentication additionally compared the external originals and recomputed hashes **directly from Git blob bytes** (`Q1-AUTH.json`: 26 checks, 0 failed). **Writer provenance**: Set A written by pinned Q1B baseline production code — commit `4c6d26a1d103a62fffe2e2743467eedb5f21afc2`, tree `320e965f219d0b615da29665d7fa3e85f8bbfc15`; Set B written by the frozen baseline `CSharpCodeGraphPlugin` + `LatticeCodeGraphStore` — commit `31c33a934aa13e373d9a3a6ffbc54d789eef092c`, tree `a265f561243de37e1636490f99acb0b34245809e`. **Both writer commits exist as Git objects in this repository with exactly those trees** (independently verified). The fixture directory was introduced by exactly one commit (`bf1af23`) and carries `* -text`/binary attributes — nothing was regenerated or transformed.

## 4. Historical behavior acceptance

**Set A — P13b store-wide refusal (independently reproduced; pids 27048/27696).** The authentic pre-REM store + WAL, staged together as disposable copies, refuse at open with `CodeOccurrenceSchemaException` ("The occurrence payload is not canonical or is incompatible with the accepted occurrence contract.") — the accepted detection boundary (open-time, during store construction), **store-wide** (graph-only access included), with no fallback, no fabricated authority, no reinterpretation, and deterministic repeatability. Accepted: this reproduces the accepted P13b contract exactly at the contract level (type + detection boundary + store-wide refusal); the concrete message text is recorded verbatim and no message-text-identity claim is made. The refusal is **not** historical compatibility.

**Set B — H-01/H-02 (independently reproduced; pids 4940/41416).** The authentic graph-only store opens; **exactly one** published unit; publication **`historical-master-1`** (index identity `968e983704910763df7f897212847882d087e069926557fd51c1328820628274`); `Logger.Write(string)` resolves to exactly one node; **exactly one** caller edge — independently derived from the committed fixture source `docs/planning/ci1-scope-01/fixtures/Repeated.cs` (`Worker.Execute` is the sole caller); the callers result is not truncated. The occurrence query reports **`AnalysisUnavailable`** with zero facts, `Examined = 0`, `IsComplete = false`, and no fabricated authority — never a successful empty.

**H-02 status/exception distinction (recorded, binding).** The experiment-era H-02 check asserted a thrown `OccurrenceAnalysisUnavailableException` in the experiment's B2 backend. The **current accepted contract** (acceptance matrix §10 row 2; register IF-05; S2.3/S2.4 reader contract) reports this condition as the query status `AnalysisUnavailable` — no exception is thrown. The owner accepts the reproduction of the historical *behavioral claim* ("occurrence analysis unavailable, never a successful empty") under the current status contract and does **not** claim the historical exception is still produced.

## 5. Staging safety

The independent Q1 staging verification (`Q2-STAGING.json`; reviewer-owned `stage-and-run.ps1`) confirms every required invariant: originals are immutable inputs never passed to a mutating operation; tests use independently staged disposable copies; the pre-REM database and WAL are staged **together**; staged input hashes equal the master hashes before any open; the work directory is guarded **outside the repository**; repeated runs never reuse contaminated state (fresh copy per run); mutating opens alter only disposable copies (mutation disclosed per run); committed Git blob identities and the fixtures worktree remain unchanged after every execution; cleanup removes only the disposable directory. The producer audit (`run-probes.ps1`/`BindProbe`) found **no path capable of unintentionally modifying the originals**. No historical master was regenerated or rewritten.

## 6. H-03 disposition

**`H-03 — INAPPLICABLE TO BOUND MASTER`.**

- **Reason.** The authoritative H-03 definition (R2 historical matrix: *"C empty/legacy store reports `AnalysisUnavailable`, not a successful empty result"*) was exercised against `c-empty.db` — a store created by the experiment harness itself (experiment C adapter, `CLatticeOccurrenceStore`) under the disposable `historical-r2/` directory, with no recorded immutable provenance. It is a different artifact lineage from the two accepted Andirit prior-writer fixture sets.
- **Supporting provenance.** R2 `HISTORICAL-FIXTURE-PROVENANCE.md` + `historical-matrix-r2.json` (H-03 check and artifact creation); the R2 record "authentic prior C fact-store writer (none exists)" for retained artifacts.
- **Existing accepted evidence.** The H-03 receipt remains **inherited experiment evidence**. For the current Andirit store, the underlying behavioral claim (analysis unavailable, never a successful empty) is accepted via acceptance matrix §10 rows 1–2 and S2.3/S2.4 evidence, and was re-verified through H-02 on the authentic graph-only master.
- **Unresolved obligation outside this fixture.** None for SQ-04's named formats. If C-lineage artifacts are ever to be bound, that requires its own provenance assessment and is not part of WP-S25-1.
- **Stage-2 acceptance consequence.** None blocking. H-03 is **not globally resolved** — it is recorded as inapplicable to the bound master and remains inherited, non-re-qualified experiment evidence.

## 7. Experimental historical-codec authenticity register

**Critical distinction (binding):** *historical writer existence is not equivalent to authenticated historical artifact availability.* The independent review established that writer code and documentation for several of these formats **does exist** in the retained experiment lineage — so no one may assert the writers never existed — while **no authentic prior-writer artifact is retained or bound** for any of the eight formats; no one may assert authentic artifacts exist merely because writer code exists.

| Format | Writer provenance | Historical usage evidence | Authentic artifact | Constructed-test evidence | Unsupported authenticity claim | Required SQ-04 qualification | Missing evidence | Amendment/acquisition decision | Residual risk | Stage-2 consequence |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `occurrence-facts/1` | None known (Andirit lineage never used the label; 0 commits in `src/` history) | None | **None** | Fresh-store version/namespace tamper tests | Authentic prior-writer refusal | Authentic-store refusal test | Immutable prior-writer master | Owner amendment or acquisition | A store in this format outside retained evidence would be unverified until encountered | Blocked for this claim under the unamended contract |
| `occurrence-facts/2` | None known (same history result) | None | **None** | Synthetic tamper tests | Same | Same | Same | Same | Same | Same |
| `occurrence-facts/3` | R1 C-store writer **retained** (`baseline-r1\…\CLatticeOccurrenceStore.cs:48` `FormatVersion`) | Experiment-era usage; H-07's /3 store was **constructed by the R2 probe**, not written by the retained writer | **None** (constructed control only) | H-07 constructed refusal | Authentic /3-store refusal | Same | Same | Same | Same | Same |
| `usage1` | R1 `OccurrenceContract` retained (`Part("usage1")`, `usage1-b2c-exp`) | Frozen-vintage identity parts | **None** | Synthetic identity/oracle tests | Authentic store carrying `usage1` facts | Same | Same | Same | Same | Same |
| `usage2` | exp-02 adapter (`usage2-b2c-exp-20261031`, final report) | Documented experiment semantics | **None** | Synthetic | Authentic store carrying `usage2` facts | Same | Same | Same | Same | Same |
| `usage3` | R1 `OccurrenceContract` (`usage3-b2c-exp-r1-20261101`) | H-04 constructed snapshot references it | **None** | H-04 constructed JSON (refused) | Authentic store carrying `usage3` facts | Same | Same | Same | Same | Same |
| `asmctx1` | exp-02 (target-assembly domain `asmctx1`; `asmctx1:unavailable` sentinel refused) | Documented experiment identity | **None** | Synthetic | Authentic store carrying `asmctx1` targets | Same | Same | Same | Same | Same |
| `asmctx2` | R1 `TargetAssemblyContext` retained (full identity construction) | Experiment identity construction | **None** | Synthetic | Authentic store carrying `asmctx2` targets | Same | Same | Same | Same | Same |

All unsupported authenticity claims are preserved explicitly. No synthetic artifact is represented as authentic; no amendment is granted by this decision.

## 8. SQ-04 amendment treatment (two separate decisions)

**Decision A — WP-S25-1 acceptance.** The independently qualified evidence (§§3–7) supports closure of the bounded WP-S25-1 fixture-binding package: both authentic fixture sets authenticate completely, staging is safe and reproducible, and the applicable historical behaviors reproduce independently without a blocking finding. **`WP-S25-1 ACCEPTED WITH QUALIFICATIONS`** is recorded in §11.

**Decision B — full SQ-04 discharge.** **`SQ-04 NOT FULLY DISCHARGED — OWNER AMENDMENT OR ADDITIONAL AUTHENTIC EVIDENCE REQUIRED`.** The unamended SQ-04 contract (acceptance matrix §13: prior-writer databases for `occurrence-facts/1..3`, `usage1..3`, `asmctx1/2`; immutable masters, per-instance hashes, disposable copies, refusal tests) is **not** satisfied for the eight named experimental codecs: no authentic prior-writer artifact exists for any of them, and constructed/synthetic evidence cannot substitute. The precise outstanding formats and the reviewer's proposed bounded amendment wording (from `ANDIRIT-CI1A-S2.5-S25-SQ04-Q1` §8) are:

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

**This commission does not grant the amendment.** Any future amendment must explicitly define: affected formats (the eight above); original requirement (acceptance matrix §13 / S1 reconciliation SQ-04 / register §6); revised acceptance boundary (available formats qualified; named experimental codecs recorded as an explicit limitation); evidence supporting the revised boundary (BIND-01 + SQ04-Q1 + this verification); historical claims excluded (authentic prior-writer refusal/compatibility for the eight codecs); residual risk; Stage-2 acceptance implications (recorded limitation, restated at acceptance); and owner approval with decision provenance. The amendment must **not** claim authentic compatibility for formats lacking authentic artifacts. If the accepted contract does not permit this amendment, **Stage-2 acceptance remains blocked** until the required authentic evidence is obtained or an authorized higher-level contract decision changes the requirement. **Recommended: a separate, documentation-only SQ-04 amendment commission** (e.g., `ANDIRIT-CI1A-S2.5-SQ04-AMEND-OWNER-01`) to decide Decision B formally; alternatively the owner may fold the amendment into the Stage-2 acceptance decision, restating the limitation there.

## 9. Evidence integrity and provenance

Owner re-verification (read-only, no execution): SQ04-Q1 manifest **20 entries / 0 mismatches**; BIND-01 manifest **24 / 0**; eight predecessor manifests (21/28/27/41/19/17/32/32) **all 0 mismatches**; committed fixture bytes equal their Git-blob bytes and the accepted complete hashes; Q1 receipts re-read from the commit (`Q1-AUTH` 26/0; four probe receipts `ok=true`; `Q2-STAGING` `workDirOutsideRepo=True`, 4 runs); gate receipts (full solution 907/0/0; focused 6/0; Release 0/0; format exit 0); frozen R2 oracle `oracle-r2.json` re-hashed **146,851 bytes, `97f6c5e5a4cc77e089481c7643732b7a07a31d483c2643bd7a95fdad7ced39c9`** — byte-identical. Provenance classes remain distinguished: independent reviewer execution (SQ04-Q1), producer execution (BIND-01), previously accepted historical receipts (P13b, H-01…H-03 — inherited), constructed controls (synthetic tamper tests; experiment H-04/H-07), and unsupported historical claims (§7). No evidence artifact was modified; nothing was resealed.

## 10. Binding qualifications

**Preserved from S2.3/S2.4 (unchanged):** F-01, F-02, Q1-Q1, Q1-Q2, S-01, Q2-Q01, Q2-Q02, O-Q2-01, Q-I01, Q-I08; the same-harness reviewer-independence limitation; Windows-x64 and the serialized single-writer scope; managed recovery versus crash/power-loss durability (F-02); restricted real-authority cross-unit union (F-01); target-local `O(n log n)` pagination (F-OBS-Q1-01); unkeyed continuation checksum (F-OBS-Q1-03); Q2-OBS-01; S1/S3/S4 observations.

**Refined by this decision:** **F-03** — authentic SQ-04 historical fixtures are now **bound and independently qualified for the two available formats**; for the eight experimental codecs the unavailability remains (writer existence ≠ artifact availability) and no fixture was fabricated.

**Preserved S2.5 obligations (open, not discharged):** H-03 applicability limitation (§6); experimental-codec authentic-artifact gaps (§7); full SQ-04 obligation pending amendment or authentic evidence (§8); SQ-05 native Linux (route decision pending); SQ-06 remainder; independent S-01 proof; D3-full; applicable D4/D5; final integrated Stage-2 qualification. **D4.5 and full S-02 remain Stage 6 assignments.** No qualification is silently discharged.

## 11. Formal owner disposition

**`WP-S25-1 ACCEPTED WITH QUALIFICATIONS`.**

The two authentic historical fixture sets (pre-REM occurrence store + WAL; R2 baseline graph-only store) and their applicable behaviors — the P13b store-wide open-time refusal and the H-01/H-02 graph-compatibility and availability behaviors — are independently qualified and accepted, subject to: the F-03 refinement above, the H-03 inapplicability record, the experimental-codec gaps (§7), Decision B (§8), and every carried qualification in §10. **Acceptance does not mean SQ-04 is fully discharged.**

## 12. Stage-2 acceptance consequences

- SQ-04 may be cited as **satisfied for the two available formats** with the §7/§8 limitation restated wherever cited; the limitation **must not** be presented as full SQ-04 discharge.
- Until Decision B is formally resolved (amendment recorded or authentic evidence obtained), any Stage-2 acceptance claim **requiring authentic prior-writer evidence for the eight experimental codecs remains blocked** under the unamended contract.
- All other Stage-2 prerequisites remain open and unchanged: SQ-05 Linux qualification, S-01 integrated proof, D3-full real-fixture part, applicable D4/D5 integration, SQ-06 remainder placement, and the final integrated Stage-2 qualification (WP-S25-4).

## 13. Explicit successor authorization status

- **WP-S25-1: closed within its bounded qualified scope.**
- **Full SQ-04 obligation: open under the unamended contract. SQ-04 amendment: NOT granted by this commission** (recommended separate documentation-only amendment commission, or explicit recording at the Stage-2 acceptance decision).
- **WP-S25-2: planned, NOT authorized** (SQ-05; route A/B decision separate). **WP-S25-3: planned, NOT authorized** (S-01 integrated proof). **WP-S25-4: planned, NOT authorized** (final integrated Stage-2 qualification).
- **Stage-2 final integration: incomplete. Stage-2 owner acceptance: unauthorized.**
- **WP-S24-3: optional, deferred, unauthorized. Public Find Usages: not authorized. Stage 6: not authorized. Pagination/index optimization: not authorized.**
- No production or test change, fixture modification, qualification execution, or new public API is authorized by this decision.

## 14. Repository discipline

Only this decision document and the additive authorization-status note to the S2.5 work-package plan are added, on canonical `main`, under the single-worktree policy. No production code, test, CI configuration, fixture, historical review, evidence, oracle/corpus, prior decision, or existing planning contract is modified. No branch, additional worktree, fetch, push, or package publication is performed. The decision commit/tree identities and final working-tree status are reported in the handoff.