# ADR-CI1-01-CI1A-S2.5-SQ04-AMEND-OWNER-01 — bounded SQ-04 amendment for experimental codecs without authentic retained artifacts

**Commission:** `ANDIRIT-CI1A-S2.5-SQ04-AMEND-OWNER-01`.
**Project:** Andirit. **Recorded:** 2026-11-10. **Canonical branch:** `main`.
**Owner decision:** **`SQ-04 BOUNDED AMENDMENT APPROVED WITH QUALIFICATIONS`**.
**Amended obligation status:** **`SQ-04 SATISFIED WITH QUALIFICATIONS UNDER AMENDED CONTRACT`** (this is **not** a claim of full historical compatibility).

> **Status: RECORDED — OWNER-AUTHORIZED (bounded contract amendment).**
>
> The governing Stage-2 contracts themselves route formats with no authentic retained
> prior-writer artifact to an explicit owner amendment (REM-01 gate reconciliation §7;
> acceptance matrix §13; register F15/SQ-04). This decision exercises that route narrowly:
> the two authenticated retained prior-writer fixture sets remain binding evidence, the eight
> experimental codecs are excluded from positive historical-compatibility claims, D5 refusal
> remains the operative runtime guarantee for them, and a future-discovery clause keeps the
> obligation alive. No historical record is rewritten; no synthetic artifact is
> represented as authentic; no unsupported compatibility is claimed.

This is an additive owner decision. It changes no production source, test, fixture, reviewer evidence, oracle or corpus, prior decision, or historical review artifact. The authoritative register and plan are updated additively (dated addenda and current-status notes), preserving all historical text.

## 1. Preflight

| Object | Value |
| --- | --- |
| Entry HEAD / tree | `487f17a3eb687430f96a400eabf224997d93adec` / `9622eb170d1ac329fdbe97eceec58aa5a07a2358` |
| Branch / worktrees | `main`; exactly one (`D:/projects/Andirit`); clean tracked/index; no stashes; no concurrent writer |
| Local vs remote | recorded `origin/main` is **0 behind / 70 ahead** (no fetch performed); nothing pushed |
| Environment | Windows `10.0.26200` **x64**; .NET SDK `10.0.401`; runtime `10.0.12` |

All authoritative inputs are present (entry owner, WP-S25-1 owner, BIND-01, SQ04-Q1, S2.5 plan, Stage-2 boundary/matrix, register). Spot identity checks: the three committed fixture masters re-hash to their accepted complete values and the SQ04-Q1 oracle output re-hashes to `97f6c5e5a4cc77e089481c7643732b7a07a31d483c2643bd7a95fdad7ced39c9` (146,851 bytes) — unchanged. No unexpected state, authority conflict, or evidence inconsistency.

## 2. Original SQ-04 obligation (independent reconstruction)

**Requirement chain.** [S1 obligation reconciliation](adr-ci1-01-ci1a-s1-obligation-reconciliation.md) §2 SQ-04: *"Generate immutable historical fixture master(s) for superseded experimental occurrence schemas (`occurrence-facts/1..3`, `usage1..3`, `asmctx1/2`); define a disposable copy procedure with per-instance hashes; execute refusal tests against the historical fixtures"* — classified as a Stage-2 deliverable blocking Stage-2 acceptance. [Acceptance matrix](../planning/adr-ci1-01-ci1a-impl-01/stage2-acceptance-matrix.md) §13 binds the same requirement and adds the availability clause (§3 below). The [obligation register](adr-ci1-01-ci1a-s2-obligation-register.md) §6 (SQ-04) and F15 row carry it forward.

**Claim separation (as required by the commission).**

| # | Claim class | Status under the original contract |
| --- | --- | --- |
| 1 | Authentic prior-writer **compatibility** (old store opens and serves graph data) | Required for available formats; demonstrated for the baseline graph-only master (H-01) |
| 2 | Authentic prior-writer **refusal** (old/incompatible store refused, never reinterpreted) | Required; demonstrated for the pre-REM master (P13b) and for graph-only availability (H-02) |
| 3 | Experimental-format **compatibility** | Required by matrix §13 naming, but **infeasible** — no authentic artifact retained for any named experimental codec |
| 4 | Constructed serialization/refusal controls | Available (synthetic tamper tests; experiment H-04/H-07) — explicitly **not** prior-writer evidence |
| 5 | Historical-format **migration** behavior | **Not an SQ-04 claim at all** — D5 excludes migration entirely (refusal + re-ingestion only); nothing to qualify |
| 6 | Formats with **no authenticated retained artifacts** | The eight experimental codecs — the subject of this amendment |

**Mandatory under the original contract:** classes 1–2 for available formats (satisfied and qualified by WP-S25-1/SQ04-Q1), and class 3 for the eight named codecs — which is precisely the infeasible part this amendment addresses. Class 4 never satisfied class 3 and never will. Class 5 is out of scope by D5.

## 3. Authority and amendment-permission assessment

The governing contracts **explicitly authorize an owner-level bounded amendment** for exactly this situation — no higher-level architectural change is required:

- [REM-01 gate reconciliation](../planning/adr-ci1-01-ci1a-s2-rem-01/stage2-gate-reconciliation.md) §7: *"formats with no authentic writer are recorded as an explicit limitation, not replaced by fresh-store tampering. **Infeasibility returns to the owner for an amendment.**"*
- [Acceptance matrix](../planning/adr-ci1-01-ci1a-impl-01/stage2-acceptance-matrix.md) §13: *"A format with no authentic prior writer is recorded as an explicit limitation; fresh-store tampering is not a substitute for an authentic prior-writer gate, and **infeasibility returns to the owner for an amendment**."*
- [Obligation register](adr-ci1-01-ci1a-s2-obligation-register.md) F15 and SQ-04 rows: *"no-writer formats explicitly limited **with owner amendment**, not tampered substitute"*; *"available authentic writers and Linux minimum at S2 acceptance"*, full matrix Stage 6.
- S2.3 acceptance §7 and S2.4 acceptance §7 both carry SQ-04 *"as available formats permit"* — the availability ceiling was accepted at every later gate.

**What an owner decision cannot do (limits observed here):** it cannot convert missing evidence into affirmative compatibility claims; it cannot rewrite historical records or decisions (none are rewritten — all updates are dated addenda); it cannot discharge unrelated obligations; and it cannot override the Candidate C architecture decisions (§D.3–D.5), none of which is touched: **D5 remains unchanged** — no migration, no dual-write, refusal of all unsupported schemas, re-ingestion rebuild only. The amendment narrows the *historical-qualification scope*; it does not weaken any runtime guarantee.

**Conclusion.** A bounded owner amendment is **permitted by the applicable authority** (the amendment route is written into the accepted Stage-2 contracts themselves) and does not conflict with any higher-level decision.

## 4. Experimental-format evidence matrix

Grounding: Andirit `src/` history search — none of the eight labels ever appears in any commit touching `src/` (SQ04-Q1 `Q5-CODEC-SEARCH.txt`); retained experiment writer code exists for some generations (`baseline-r1\…\CLatticeOccurrenceStore.cs:48` `occurrence-facts/3`; `OccurrenceContract` `usage3`/`usage1`; `TargetAssemblyContext` `asmctx2`; R2 C store `occurrence-facts/4`; exp-02 final report `usage2-b2c-exp-20261031`/`asmctx1`). **Deployment status: experimental only** — none was ever shipped, released, or deployed by Andirit (the experiments live in separate repositories and were recorded as experiments by their own final reports).

| Format | Historical writer evidence | Lineage | Deployed/released? | Authentic artifact exists? | Currently bound? | Current compat/refusal evidence | Constructed coverage | Original SQ-04 requirement | Unsupported claim | Proposed treatment | Residual risk |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `occurrence-facts/1` | None known for this label (Andirit lineage: 0 commits) | Experiment-era schema generations | **Experimental only** | No | No | D5 refusal (never read); synthetic tamper tests | Version/namespace tamper tests (synthetic) | Immutable master + refusal tests | Authentic prior-writer refusal for this schema | Excluded from positive claims; limitation recorded | An unretained artifact outside the evidence would be unverified until encountered |
| `occurrence-facts/2` | None known for this label | Same | **Experimental only** | No | No | Same | Same | Same | Same | Same | Same |
| `occurrence-facts/3` | R1 C-store writer **retained** (`CLatticeOccurrenceStore.cs:48`); H-07's /3 store was **constructed by the R2 probe**, not written by it | Experiment C adapter (R1 → R2 `/4`) | **Experimental only** | No | No | R2-era C store refused /3; Andirit never reads it | H-07 constructed refusal; synthetic tamper tests | Same | Authentic /3-store refusal | Same | Same |
| `usage1` | R1 `OccurrenceContract` retained (`Part("usage1")`, `usage1-b2c-exp`) | Experiment identity vintage | **Experimental only** | No | No | Not parsed by Andirit | Synthetic identity/oracle tests | Same | Authentic store carrying `usage1` facts | Same | Same |
| `usage2` | exp-02 adapter (`usage2-b2c-exp-20261031`) | First experiment | **Experimental only** | No | No | Not parsed by Andirit | Synthetic | Same | Authentic store carrying `usage2` facts | Same | Same |
| `usage3` | R1 `OccurrenceContract` (`usage3-b2c-exp-r1-20261101`) | R1 experiment | **Experimental only** | No | No | Not parsed by Andirit | H-04 constructed JSON (refused) | Same | Authentic store carrying `usage3` facts | Same | Same |
| `asmctx1` | exp-02 (domain `asmctx1`; `asmctx1:unavailable` sentinel refused by validation) | First experiment | **Experimental only** | No | No | Not parsed by Andirit | Synthetic | Same | Authentic store carrying `asmctx1` targets | Same | Same |
| `asmctx2` | R1 `TargetAssemblyContext` **retained** (full identity construction) | R1 experiment | **Experimental only** | No | No | Not parsed by Andirit (post-R2 uses `asmctx3`) | Synthetic | Same | Authentic store carrying `asmctx2` targets | Same | Same |

**Distinctions preserved.** Historical writers existed for some formats (retained code/documentation) — it is **not** claimed that they never existed. Missing repository artifacts are **not** treated as proof that no artifact ever existed — only that none is retained, authenticated, or bound. The R2 `/4` C-store format (the R2-era experiment's working generation) is distinguished from the earlier `/1..3` experimental generations: no authentic `/4` store artifact is bound either, and it is likewise excluded from positive historical-compatibility claims under this amendment.

## 5. Authentic historical-evidence preservation (unchanged by this amendment)

| Artifact | Complete SHA-256 | Accepted behaviors (unchanged) |
| --- | --- | --- |
| Set A: pre-REM occurrence store + WAL (`…BIND-01-evidence/fixtures/prerem/`) | `7646b98288a9803cff6f9930c78a3e7df5c676f6fc4e0cd2df2af5e79bc68885` / `8ca170cdc55a4428a5506bcb447a0667ab10c92c77ef149ae36460a5603aeab4` | P13b store-wide open-time refusal (`CodeOccurrenceSchemaException`), reproduced independently (SQ04-Q1) |
| Set B: R2 baseline graph-only store (`…BIND-01-evidence/fixtures/baseline/`) | `f21a72bee2c2e378e16766ca7b84c29010ab4d50360bf3111a82706f947781e0` | H-01 legacy graph compatibility (1 unit; `historical-master-1`; `Logger.Write(string)` + its single caller); H-02 occurrence `AnalysisUnavailable`, never successful empty |

Re-verified at this commission (read-only): all three committed masters re-hash to the accepted complete values. **H-03 remains `INAPPLICABLE TO BOUND MASTER`** (C-adapter experiment lineage; inherited evidence) — it is not silently closed globally by this amendment. The amendment preserves these findings without modification; nothing in the bound evidence is weakened, reinterpreted, or rewritten.

## 6. Amendment justification (proportionality)

| Consideration | Finding |
| --- | --- |
| Accepted production/released writer per format? | **No.** None of the eight formats ever had an Andirit production or released writer (0 `src/` history hits); they were experiment-internal generations in separate experiment repositories. |
| Authentic artifacts historically retained? | **No.** The experiment artifacts were disposable per-instance stores (wall-clock timestamps; bytes mutate on open); the R2 record states no authentic prior C fact-store artifact was retained. |
| Temporary experiments? | **Yes** — each generation was superseded inside the experiment lineage (`/3` → `/4`; `usage1..3` → `usage4`; `asmctx1/2` → `asmctx3`) and none reached Andirit. |
| Current reader refuses unsupported formats safely? | **Yes** — D5 (no migration; refusal + re-ingestion only), the store's schema/namespace/payload refusal paths, and the accepted synthetic tamper tests (matrix §10 rows 3–6; focused 6/0 suite). |
| Do the accepted masters cover the relevant compatibility boundary? | **Yes** — the two authentic Andirit prior-writer generations that a current store could realistically encounter (its own immediate predecessors) are bound and qualified; the excluded formats were never Andirit formats. |
| Could additional authentic evidence reasonably be obtained? | **No.** Regenerating a store with retained experiment writer code would create a *new* artifact, which the contract explicitly excludes as a substitute for authentic historical evidence ("fresh-store tampering is not a substitute"; reconstruction ≠ prior-writer output). |
| Would the amendment weaken a real compatibility guarantee? | **No.** It excludes only positive historical-compatibility claims for formats never part of Andirit; the D5 refusal guarantee for them remains fully in force; the future-discovery clause preserves the obligation. |

**Conclusion.** The original SQ-04 breadth (naming experiment-internal formats for authentic-artifact qualification) is disproportionate to what was actually used and retained by Andirit: the excluded formats were temporary experiment generations with no deployment, no release, no retained authentic artifact, and a live refusal guarantee. A lack of authentic evidence is **not** converted into an affirmative compatibility claim — the amendment only narrows the qualification scope and records the limitation. **The amendment is justified and permitted.**

## 7. Approved SQ-04 contract language (exact)

The following wording **replaces, for SQ-04 acceptance purposes only**, the breadth of acceptance matrix §13 / S1 reconciliation SQ-04 with respect to the eight named experimental codecs. All other SQ-04 requirements (immutable masters, per-instance hashes, disposable-copy procedure, refusal tests, M-01 handoff records) remain in force for every format **with** an authentic retained artifact.

> **SQ-04 (amended; owner decision `ANDIRIT-CI1A-S2.5-SQ04-AMEND-OWNER-01`).**
> Historical compatibility/refusal qualification is limited to **authenticated retained
> prior-writer artifacts** whose provenance, exact bytes, format lineage, and expected
> behavior are independently verifiable. The accepted fixtures are: (A) the pre-REM
> occurrence store and WAL (`7646b98288a9803cff6f9930c78a3e7df5c676f6fc4e0cd2df2af5e79bc68885`,
> `8ca170cdc55a4428a5506bcb447a0667ab10c92c77ef149ae36460a5603aeab4`) demonstrating
> store-wide open-time refusal without reinterpretation (P13b); and (B) the R2 baseline
> graph-only store (`f21a72bee2c2e378e16766ca7b84c29010ab4d50360bf3111a82706f947781e0`)
> demonstrating legacy graph compatibility and occurrence `AnalysisUnavailable`, never a
> successful empty (H-01/H-02). The experimental formats `occurrence-facts/1`,
> `occurrence-facts/2`, `occurrence-facts/3`, `usage1`, `usage2`, `usage3`, `asmctx1`, and
> `asmctx2` are **excluded from positive historical-compatibility claims**: no authentic
> retained artifact exists for any of them; their existing constructed serialization/refusal
> evidence remains available only within its demonstrated (synthetic) scope; D5 refusal of
> unsupported schemas remains the operative runtime guarantee. **Future discovery of an
> authentic historical artifact in any excluded format requires a separate
> compatibility/refusal qualification before any historical support claim may be made.**
> SQ-04 is **satisfied with qualifications under this amended contract**; full historical
> compatibility across all conceivable historical formats is **not** claimed.

## 8. Residual risk and exclusions

| Item | Statement |
| --- | --- |
| Excluded formats | `occurrence-facts/1`, `occurrence-facts/2`, `occurrence-facts/3`, `usage1`, `usage2`, `usage3`, `asmctx1`, `asmctx2` — excluded from positive historical-compatibility/refusal claims |
| Excluded claims | "All historical formats supported"; authentic prior-writer compatibility for the excluded formats; any compatibility inference from constructed controls |
| Residual risk | An authentic store in an excluded format could exist outside the retained evidence; its refusal behavior would remain unverified until encountered. Runtime risk is low: Andirit never shipped these schemas, no code path reads them, and D5 refusal (never reinterpret) applies to any unknown format. |
| Constructed evidence scope | Synthetic tamper tests and experiment H-04/H-07 refusals remain valid **only** as constructed controls; they never become prior-writer evidence |
| Future obligation | Authentic-artifact discovery → separate compatibility/refusal qualification (new fixture binding + independent qualification + owner decision) before any claim |
| Not discharged by this amendment | SQ-05 Linux; SQ-06 remainder; S-01 integrated proof; D3-full; applicable D4/D5; WP-S25-4 final integration; Windows-x64/serialized single-writer; managed-fault limits; real-authority union restrictions; reviewer-independence qualifications; S2.3/S2.4 observations; D4.5 and full S-02 (Stage 6); WP-S24-3 (deferred) |

## 9. SQ-04 obligation disposition

**`SQ-04 SATISFIED WITH QUALIFICATIONS UNDER AMENDED CONTRACT`.**

- **Included (supported):** the two authenticated retained prior-writer fixture sets, their provenance/lineage/bytes, and the demonstrated behaviors (P13b refusal; H-01 compatibility; H-02 availability) — bound by WP-S25-1, independently qualified by S25-SQ04-Q1, and preserved unmodified.
- **Excluded (unsupported, limitation recorded):** the eight experimental codecs — no positive historical-compatibility or prior-writer-refusal claim is made or permitted for them; D5 refusal remains their only operative guarantee.
- **Not claimed:** `SQ-04 FULL HISTORICAL COMPATIBILITY PROVEN` — expressly rejected.
- Decision B of `adr-ci1-01-ci1a-s2-5-wp-s25-1-owner-01.md` ("SQ-04 NOT FULLY DISCHARGED — OWNER AMENDMENT OR ADDITIONAL AUTHENTIC EVIDENCE REQUIRED") is hereby **resolved by this amendment**; the F-03 qualification is refined accordingly (satisfied-with-qualification under the amended contract; the unavailability record for the excluded formats is preserved).

## 10. Stage-2 acceptance implications

- **The remaining SQ-04 authenticity blocker is resolved**: the amended contract permits Stage-2 acceptance to record SQ-04 as `SATISFIED WITH QUALIFICATIONS UNDER AMENDED CONTRACT` (available formats demonstrated; eight experimental codecs excluded by the recorded limitation).
- **Unsupported claims remain excluded** — the acceptance decision must restate the limitation; "all historical formats supported" must not appear anywhere.
- **No additional independent qualification is required** for the excluded formats (there is no artifact to qualify); a future artifact discovery reopens per §7.
- **Stage-2 acceptance prerequisites otherwise unchanged**: SQ-05 (Linux), S-01 integrated proof, D3-full real-fixture part, applicable D4/D5 integration, SQ-06 remainder placement, and the WP-S25-4 final integrated qualification remain open; Stage-2 owner acceptance remains unauthorized by this decision.
- **No conflict with higher-level acceptance requirements**: D5, D3, D4, and the Candidate C architecture decisions are untouched; the amendment operates strictly within the amendment route the contracts themselves provide.

## 11. Binding qualifications

**Preserved (unchanged):** F-01, F-02, Q1-Q1, Q1-Q2, S-01, Q2-Q01, Q2-Q02, O-Q2-01, Q-I01, Q-I08; same-harness reviewer-independence limitation; Windows-x64 and serialized single-writer; managed fault-injection durability limits; restricted real-authority cross-unit union; F-OBS-Q1-01 (target-local `O(n log n)` pagination); F-OBS-Q1-03 (unkeyed checksum); Q2-OBS-01; S1/S3/S4 observations; H-03 inapplicability (not globally closed).

**Refined:** **F-03** — authentic SQ-04 historical fixtures: satisfied with qualifications under the amended contract for the two available formats; the excluded experimental codecs retain the recorded unavailability limitation (writer existence ≠ artifact availability), and no fixture was fabricated.

**Unchanged obligations:** SQ-05, SQ-06 remainder, S-01, D3-full, applicable D4/D5, WP-S25-4 final integration; D4.5 and full S-02 (Stage 6); WP-S24-3 (optional, deferred, unauthorized). No qualification is silently discharged.

## 12. Successor authorization status

- **SQ-04: `SATISFIED WITH QUALIFICATIONS UNDER AMENDED CONTRACT`** — the amendment is **approved** by this decision and recorded in the S2.5 work-package plan and the obligation register (dated addenda).
- **WP-S25-1: closed** (per `adr-ci1-01-ci1a-s2-5-wp-s25-1-owner-01.md`).
- **WP-S25-2: planned, NOT authorized** (SQ-05; route A/B decision separate). **WP-S25-3: planned, NOT authorized** (S-01). **WP-S25-4: planned, NOT authorized** (final integration).
- **Stage-2 final integration: incomplete. Stage-2 owner acceptance: unauthorized** (remaining prerequisites above).
- **WP-S24-3: optional, deferred, unauthorized. Public Find Usages: not authorized. Stage 6: not authorized. Optimization: not authorized. Migration/dual-write: forbidden (D5).**
- **Reopening clause:** discovery of an authentic artifact in any excluded format, or any deployment/release of a writer for one, returns SQ-04 to the owner for the required qualification before any claim.

## 13. Repository discipline

Only this decision document, the additive authorization-status update to the S2.5 work-package plan, and a dated additive addendum to the obligation register are added, on canonical `main`, under the single-worktree policy. Historical register text and all prior decisions are preserved verbatim (the register receives a dated addendum, not a rewrite). No production code, test, fixture, CI configuration, historical review, evidence, oracle/corpus, or prior decision is modified. No branch, additional worktree, fetch, push, or package publication is performed. The decision commit/tree identities and final working-tree status are reported in the handoff.