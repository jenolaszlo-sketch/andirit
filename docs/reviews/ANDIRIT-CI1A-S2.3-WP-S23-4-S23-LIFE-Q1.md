# ANDIRIT-CI1A-S2.3-WP-S23-4-S23-LIFE-Q1 — Independent native lifecycle qualification with real occurrence facts

**Commission:** `ANDIRIT-CI1A-S2.3-WP-S23-4-S23-LIFE-Q1`.
**Project:** Andirit. **Recorded:** 2026-11-07. **Canonical branch:** `main`.
**Gate:** `S23-LIFE-Q1` (independent reviewer ≠ implementer), WP-S23-4 native lifecycle qualification.
**Reviewed source:** `f0d960af6ec1ffec145a6416d74ef06384b1bafd` (tree `a72f05c69ba722b3600b3d40c59c7adc11c6561b`).
**Qualification tests commit:** `0dca67bf54b5499fce4e70217730ed388e3cbd4a` (tree `690a6ae59709649325605f734555802a962f0ce4`).
**Disposition:** **`S23-LIFE-Q1 SUPPORTED WITH QUALIFICATIONS`**.

> Scope was tests, fixtures, probes and evidence only. No production source, public API,
> persisted schema, accepted test oracle, or historical evidence was modified
> (`production-diff.txt`: 0 lines; `test-additions-diff.txt`: no existing file changed).
> No push, branch, or additional worktree was created.

---

## 1. Initial repository and environment preflight

| Check | Result |
| --- | --- |
| HEAD / tree | `f0d960af6ec1ffec145a6416d74ef06384b1bafd` / `a72f05c69ba722b3600b3d40c59c7adc11c6561b` — matches the expected WP-S23-4 entry HEAD |
| Ancestry | `f0d960a` → `4a775c8` (WP-S23-3A owner acceptance) → `3665717` (S23-PUB-Q2) → `a4d8501` (R2) → `e0cef1c` (R1) → `e2b833a` (WP-S23-3A) → `2a1bfb7` (WP-S23-3) → `80f6177` (WP-S23-2 owner acceptance) |
| Branch / worktrees | canonical `main`, exactly one worktree (`D:/projects/Andirit`), exit-clean |
| Tracked / index / stash | clean; no stashes; nothing pushed |
| Prior S23-LIFE-Q1 work | **none** — no `LIFE`/`S23-4` review or evidence directory existed; nothing to reuse or resume |
| Concurrent writer | none (only an idle `dotnet` language server); state re-checked before each commit |
| Prerequisites | WP-S23-1 (NEW-01), WP-S23-2 (S23-DUR), WP-S23-3A owner acceptances present; entry decision `adr-ci1-01-ci1a-s2-3-wp-s23-4-entry-owner-01.md` present |
| Environment | Windows `10.0.26200` **x64**; .NET SDK `10.0.401`; runtime `.NET 10.0.12` (`P08-environment.json`, `environment.log`) |

Authoritative contracts read: the WP-S23-4 entry owner decision; the S2.3 work-package plan;
the native publication/lifecycle test matrix (L1–L20); the S2.3 independent-qualification plan;
the WP-S23-3A owner decision and S23-PUB-Q2 evidence; the accepted NEW-01 / S23-DUR acceptances;
`CodeOccurrence.cs`, `CodeOccurrenceReader.cs`, `LatticeCodeGraphStore(.OccurrenceAuthority).cs`,
`VerifiedCSharpOccurrenceAuthority.cs`, `VerifiedCSharpOccurrenceExtractor.cs`, `VerifiedUsageMint.cs`.

Baseline before any change: **865 passed / 0 failed / 0 skipped** (50 LatticeDb + 157 TreeSitter +
345 Andirit + 313 CSharp).

---

## 2. Complete L1–L20 result matrix

Every case was executed against a real Windows-x64 `LatticeCodeGraphStore` file over the accepted
S2.2 publication path: verified Roslyn compilations driven through the real `CSharpCodeGraphPlugin`
/ `CodeGraphIngestionSink`, or the accepted `S23NativeAuthorityFixture` verified-compilation surface.
Expected targets are recomputed from the accepted **frozen R2 authority** and identity primitives
and cross-checked against `CodeOccurrenceIdentity` (never copied from the implementation under test).

| Case | Governing requirement | N-equiv | Input / transition | Expected | New evidence | Result | Limitation |
| --- | --- | --- | --- | --- | --- | --- | --- |
| L1 | Unit/page/meta persisted; graph+occurrence same transaction | N1 | Real cross-project publish | Durable facts queryable | `L01_*`; P01/P02 | **PASS** | — |
| L2 | Same-unit replacement retires obsolete, preserves others | N2/N3 | Same project unit re-published after real source edit | Old fact gone, new visible | `L02_*`; P05 | **PASS** | — |
| L3 | Source-unit deletion removes only its facts | N4 | Real unit deletion | Deletion reflected | `L03_*`; P05 | **PASS** | — |
| L4 | Identical facts union contributors; distinct units distinct | N4 | Real extracted identity contributed by 2 storage units | 1 fact, 2 contributors | `L04_*`, `L04b_*`; accepted `N4_*` | **PASS (qualified)** | see F-01 |
| L5 | Same identity, different payload rejected; prior intact | N4 | Real identity re-staged with different payload | Refusal; prior retained | `L05_*` | **PASS** | — |
| L6 | `Produced` zero-fact unit persists; reopen restores | — | Real complete-empty publish | Available complete-zero | `L06_*` | **PASS** | — |
| L7 | Reopen reconstructs byte-equivalent ordered results | N5 | Publish → dispose → reopen | Identical results | `L07_*`; P02/P05 | **PASS** | — |
| L8 | Stale handle → explicit status, no hits | N7 | Stable target after transitive-only change | `StalePublication` | `L08_*`; P04 | **PASS** | — |
| L9 | Failed completion preserves prior; graph+occurrence atomic | N6 | Managed `before-commit` fault during real publish | Prior intact, no partial | `L09_*` | **PASS** | managed fault (F-02) |
| L10 | Known-failure recovery = durable; no blind retry | — | Managed `before-commit` fault; recovered vs reopened | Projection correspondence | `L10_*` | **PASS** | managed fault (F-02) |
| L11 | Real source modification/re-index: new identities, supersession | — | Real edit changes invocations | Correct identities/coverage | `L02_*`; P05 | **PASS** | — |
| L12 | Old-or-new complete visibility under serialized writes (SQ-03) | — | Native scheduled + concurrent reads under `_writeGate` | No partial blend | `L12_*`, `L12b_*`; P06/P07 | **PASS** | no MVCC claim |
| L13 | `Produced`/`Partial`/unavailable distinguished | — | Real facts `Partial`; graph-only | Correct availability | `L13_*` | **PASS** | — |
| L14 | Deleting the last analyzed unit → `AnalysisUnavailable` | — | Delete all analyzed units | Unavailable | `L14_*`; P05 | **PASS** | — |
| L15 | Unsupported schema/namespace, units-without-meta refused | — | D5 controls (unchanged) | Explicit refusal | accepted `Store_RejectsUnsupportedOccurrenceSchemaOnOpen`, `Store_RejectsOccurrenceUnitsWithoutMeta` | **PASS** | deterministic control (reused) |
| L16 | D3 graph nodes/edges/counts/traversal unchanged | N8 | Real facts across publish/replace/delete | Identical graph snapshots | `L16_*` | **PASS** | — |
| L17 | Reopen stale defining-`compctx2` refused; current usable | — | Reopen after transitive-only change | Stale refused, current usable | P04; accepted `S23NativeAuthorityTests.Test8b_*` | **PASS** | — |
| L18 | Real cross-project target from verified defining compilation; persisted; reopened | — | Real Lib←App publish; inspect durable; reopen | Correct target | `L18_*`; P01/P02 | **PASS** | — |
| L19 | Missing/corrupt recorded provenance refuses conservatively | — | 19 substitution/corruption mutations | Explicit refusal | accepted `MalformedOrSubstitutedAuthority_*`; R2 `MalformedStagedRecord_IsRefused` | **PASS** | deterministic control (reused) |
| L20 | Unknown-outcome quarantines; reopen reconciles | — | Managed `after-commit` ack loss | Quarantine; reopen durable | `L20_*` | **PASS** | managed fault (F-02) |

Summary: 18/18 new qualification tests passed (`lifecycle-tests.log`); 8/8 native probe phases passed
(`probe-matrix.txt`). No mandatory in-scope case failed or was skipped.

---

## 3. N1–N8-equivalent coverage (real S2.2 facts)

| N | Stage-2 boundary §8 obligation | Real-fact evidence |
| --- | --- | --- |
| N1 ↔ L1 | Initial publication | `L01_*`, P01/P02 |
| N2 ↔ L2 | Replacement ordering/limits | `L02_*`; accepted `N2_*`/`N3_*` |
| N3 ↔ L2 | Replacement retires obsolete | `L02_*`, P05 |
| N4 ↔ L3/L4/L5 | Contributor union/deletion/conflict | `L03_*`, `L04_*`, `L04b_*`, `L05_*` |
| N5 ↔ L7 | Reopen | `L07_*`, P02/P05 |
| N6 ↔ L9 | Failed completion preserves prior | `L09_*` (managed fault) |
| N7 ↔ L8 | Stale refusal | `L08_*`, P04 |
| N8 ↔ L16 | D3 graph noninterference | `L16_*` |

Each is demonstrated with **real S2.2 facts** (real source bytes/hashes, live Roslyn compilations,
real cross-project binding and live defining instances); none relies on synthetic records for the
required S23-LIFE-Q1 result.

---

## 4. SQ-03 native schedule evidence

**Contract.** Under the accepted serialized `_writeGate` model (no MVCC / snapshot isolation claim),
a reader must observe a **complete old-or-new** occurrence publication and truthful attribution,
never a partial blend.

**Schedule (native, real facts).** One `LatticeCodeGraphStore` file; stable verified universe with
two complete publications — `run:1` = {App} (1 Lib-target fact) and `run:2` = {App, App2} (2 facts).

```
publish run:1            -> run:1=1/Available
run:2 started            -> run:1=1/Available
stage App (replace)      -> run:1=1/Available
stage App2               -> run:1=1/Available   (staged contributions not visible)
stage Empty              -> run:1=1/Available
complete run:2           -> run:2=2/Available
```

Recorded in `P06-sq03-schedule.json`; the deterministic test `L12_*` asserts the exact trace. The
staged steps being invisible until completion is the D4.5-adjacent "cancelled/partial staging
exposes no partial" behavior exercised on the accepted path (D4.5 itself remains Stage 6).

**Concurrency.** Four reader tasks sampled the reader 800 times while a writer alternated the two
complete publications (20 expand/shrink rounds; shrink uses staged deletion of App2). Every
`Available` sample matched its publication (`run:o:*`/`run:1` ⇒ 1 occurrence; `run:n:*` ⇒ 2
occurrences); **0 violations** (`P07-sq03-concurrent.json`; `L12b_*`). Reproducible and diagnosable:
each phase is its own OS process with a recorded `processId`; the deterministic trace localizes any
divergence to a single schedule step.

**Bounded claim.** This demonstrates serialized-write old-or-new completeness for this schedule and
interleaving on Windows x64. It does **not** claim deterministic scheduling guarantees, MVCC,
snapshot isolation, crash consistency, or multi-process writers.

---

## 5. SQ-04 coverage and limitations

**No authentic historical prior-writer fixtures were fabricated.** No authentic prior-writer
occurrence format is available in this repository/environment beyond the accepted current schema.
Consistent with the entry decision §4 and the matrix platform/historical-fixtures note, SQ-04
closure remains an **S2.5/Stage-2 acceptance** obligation; fabricating fixtures by fresh-store
tampering is explicitly out of scope. This is recorded as an explicit, nonblocking limitation
(F-03) — not a narrowing of L1–L20 or of D3/D4/D5.

The legacy schema-refusal controls that *are* available (unsupported occurrence schema; occurrence
units without meta) were confirmed unchanged (L15) as deterministic controls and were not weakened.

---

## 6. D3/D4/D5 verification

- **D3 (graph noninterference).** `L16_*` (real facts) compares node/edge/count/traversal snapshots
  across publish → same-unit replacement → scoped deletion and after a durable reopen: identical
  graph. Accepted `N8_*` preserved. Occurrence facts never become graph nodes/edges.
- **D4 invariants.** 1 accepted generation only (L1/L8); 2 stale/unverifiable refuses explicitly
  (L8/L13); 3 no unbounded scan on stale (L8 `Examined == 0`); 4 failed staging preserves prior
  (L9/L10); 6 superseded cannot replace newer (L02/L08); 7 reopen semantically equivalent (L7/L17);
  8 contributor/deletion consistent (L3/L4/L14); 9 deterministic order + limit (L2/L7 ordering);
  10 serialized concurrency (L12). D4.5 remains Stage 6 and is **not** counted.
- **D5 (schema/namespace).** `Store_RejectsUnsupportedOccurrenceSchemaOnOpen` and
  `Store_RejectsOccurrenceUnitsWithoutMeta` pass unchanged; no schema-format change; the occurrence
  labels remain distinct from the `Hetu*` graph labels. Native rows inspected directly
  (`AndiritOccurrenceUnit/Page/Meta`) confirm the additive carrier and the authority envelope.

---

## 7. Real-fact native publication results

`P01-plugin-publish.json` / `P02-plugin-fresh-verify.json` (two OS processes) publish the real
cross-project `Lib←App` fixture and reopen a separate process:

- 2 occurrence units with `RequiresAuthority = true` and a full authority envelope (Version 1,
  `Members`, per-fact `DefiningProjectUnitId`/`Compctx2`/digests, `Coverage`);
- `AndiritOccurrenceMeta`: `PublicationIndexRunId=run:a`, `FactCount=1`, `UnitCount=2`,
  `PageCount=1`, `CompleteCoverage=true`, `RequiresAuthority=true`;
- the reopened reader returns **Available**, 1 occurrence, `IsComplete`, exact target and
  `SourceUnitId = csharp:project:94c630…`; the persisted `DefiningProjectUnitId` is the Lib unit;
- missing current authority → `AnalysisUnavailable` (never empty-success); a foreign expected-
  publication handle → `StalePublication` with `Examined == 0`.

The independent target is cross-checked: the frozen R2 `CompilationProvenance`/`ProvenanceHashing`
`asmctx3` value and `CodeOccurrenceIdentity.ComputeTargetId` agree before any assertion.

---

## 8. Lifecycle and recovery results

`P05-lifecycle.json` (one durable file, reopened between steps):

| Step | Availability | Occurrences | Complete | Publication |
| --- | --- | --- | --- | --- |
| publish (`run:a`) | Available | 1 | true | `run:a` |
| durable reopen | Available | 1 | true | `run:a` |
| same-unit replacement after real source edit (`run:retired`) | Available | 0 | true | `run:retired` |
| delete last analyzed unit (`run:delete`) | **AnalysisUnavailable** | 0 | false | `run:delete` |
| final durable reopen | **AnalysisUnavailable** | 0 | false | `run:delete` |

Recovery (`L09_*`, `L10_*`): a managed before-commit fault during a real-fact completion leaves the
prior published graph/occurrence authority and pages byte-identical, and the recovered in-process
projection equals a fresh durable reopen. Unknown-outcome (`L20_*`): a managed after-commit ack loss
quarantines the instance (no current-state claim, no replay); a fresh reopen reconciles to the
durably committed publication (App + App2). These qualify ordinary known-failure recovery and the
accepted unknown-outcome quarantine only — **not** crash/power-loss consistency.

---

## 9. Corruption/refusal controls

Reused, unchanged, and confirmed passing: schema refusal (L15); recorded-context
missing/corrupt/substituted refusal across 19 mutations with byte-identical durable rows (L19);
staged-run corruption/malformed/duplicate/conflicting refusal (WP-S23-3A R2). New real-fact
controls: `L05_*` (real identity, conflicting payload rejected with prior state byte-identical);
`L08_*`/P04 (real-fact stale-defining-`compctx2` refusal with a stable target; missing authority →
unavailable). No new fabrication or weakening was introduced.

---

## 10. Regression receipts

| Gate | Command | Result | Log |
| --- | --- | --- | --- |
| Full solution | `dotnet test Andirit.slnx -c Release --no-build` | **883 passed / 0 failed / 0 skipped** (50 + 157 + 345 + 331) | `full-solution-tests.log` |
| Focused S23 / NEW-01 / Occurrence / WP8 | `dotnet test <proj> … --filter "…S23|…New01|…Occurrence|…S2_2Wp"` | **189 passed / 0 failed** (166 CSharp + 23 LatticeDb) | `s23-new01-regression.log` |
| New lifecycle tests | `… --filter …S23LifecycleQualificationTests` | **18 passed / 0 failed** | `lifecycle-tests.log` |
| Frozen R2 oracle | `SemanticCorpusOracle` over the frozen corpus/expected | **185/185**, output **146,851 bytes**, SHA-256 `97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9` | `oracle.log`, `oracle-r2.json` |
| Format | `dotnet format Andirit.slnx --verify-no-changes` | exit 0 | `format.log` |
| Release build | `dotnet build Andirit.slnx -c Release` | 0 warnings / 0 errors | `release-build.log` |

Baseline floor 865 → post-change 883 (Δ = the 18 new tests). The frozen oracle is **byte-identical**
to the accepted value (size and SHA-256 recomputed). No existing regression or frozen evidence was
weakened.

---

## 11. Findings and unresolved qualifications

**F-01 — L4 identical-fact cross-unit contributor union is not naturally producible on the real
verified-authority path (qualification, not a defect).**
Real verified provenance binds `SourceUnitId` to the analysis unit, and the real C# plugin maps one
project to exactly one storage/analysis unit, so one published set cannot contain two storage units
carrying the *same* authority-validated identical fact. The union algebra is therefore demonstrated
on a real-fact identity contributed by two storage units on the accepted raw publication path
(`L04_*`: real extracted target/span/sourceHash/semantic unit; two contributors ⇒ 1 fact, 2
contributors), combined with the accepted synthetic-contribution union and distinct-semantic-unit
evidence (`N4_*`, `L04b_*`). Source: `src/Andirit.CSharp/VerifiedCSharpOccurrenceExtractor.cs`
(`SourceUnitId = sourceUnit.Descriptor.ProjectUnitId`), `src/Andirit/CodeGraphBatchValidator.cs`
(contributor must equal origin unit). Impact: bounded; the merge contract itself is unchanged and
exercised. Smallest remediation boundary if a fully real two-unit union is ever required: none
within WP-S23-4 (would be a producer/plugin concern, out of this gate's tests-only scope).

**F-02 — Fault-injection scope (carried, not new).** L9/L10 use the store's managed `before-commit`
injector and L20 the managed `after-commit` injector. These qualify ordinary known-failure recovery
and the accepted unknown-outcome quarantine only; no crash, power-loss, abrupt-kill, arbitrary
crash-point, or multi-process-writer guarantee is claimed or inferred from process restart.

**F-03 — SQ-04 historical fixtures unavailable (carried).** No authentic prior-writer format is
available; none was fabricated. SQ-04 closure remains S2.5/Stage-2.

**Carried qualifications preserved (binding, not "fixed" here):** Q1-Q1 (segment resumption, closed
fail-closed by WP-S23-3A); Q1-Q2 (recovery-rebuild internal-failure atomicity bound); S-01
(caller-supplied component digests; the probe/fixture `IdentifierDigest`/`DeclarationDigest`/
`ShapeDigest` remain caller-supplied); Q2-Q01 (concurrent interrupted-run stale-baseline — the
binding concurrent qualification; `P07`/`L12b_*` do **not** supersede it); Q2-Q02 (failure-message
text); O-Q2-01 (separate hardening candidate).

**Independent-fact — no production defect found.** Every mandatory in-scope defect-free behavior was
reproduced against a real native store; the reproducers and receipts are preserved. No production
defect was discovered that would require stopping this qualification path.

---

## 12. Exact final commits/trees

| Artifact | Commit | Tree |
| --- | --- | --- |
| Reviewed source (production unchanged) | `f0d960af6ec1ffec145a6416d74ef06384b1bafd` | `a72f05c69ba722b3600b3d40c59c7adc11c6561b` |
| Qualification tests (bounded test change) | `0dca67bf54b5499fce4e70217730ed388e3cbd4a` | `690a6ae59709649325605f734555802a962f0ce4` |
| Evidence + probe harness + this report | `e4b64ebd6892a6ff6892f84840e907303f0de01e` | `8d8f134aa7c3f423be78003a4f0ab13f1dc2bc4e` |

Reviewed HEAD equals the expected WP-S23-4 entry HEAD. The test change and the report/evidence are
committed separately per repository discipline. The final row is the commit that introduced this
report; any immediately following additive commit only records these exact hashes and changes no
report content, evidence, test, or production byte.

---

## 13. Evidence integrity manifest

The integrity manifest is `docs/reviews/CI1A-S2.3-S23-LIFE-Q1-evidence/manifest.json`, produced by
`CI1A-S2.3-S23-LIFE-Q1-tests/seal-evidence.ps1`: **SHA-256 over the exact bytes of every committed
evidence file, sorted relative paths, self-excluded** (probe databases and the manifest itself are
excluded). It binds the reviewed HEAD/tree. The reviewer probe harness is committed under
`docs/reviews/CI1A-S2.3-S23-LIFE-Q1-tests/` (`S23LifeQ1Probe.csproj`, `Program.cs`, `run-probes.ps1`,
`seal-evidence.ps1`); it references only `src/` projects plus the frozen R2 authority, reuses the
accepted `S23NativeAuthorityFixture`, and asserts against frozen-R2/identity-derived expected
values, not producer output.

---

## 14. Owner recommendation

**`S23-LIFE-Q1 SUPPORTED WITH QUALIFICATIONS`.**

All mandatory in-scope L1–L20, N1–N8-equivalent, SQ-03 and applicable D3/D4/D5 obligations are
supported by genuine native real-fact evidence at reviewed HEAD `f0d960a`:
18 new lifecycle tests, 8 native probe phases, 883/0/0 whole-solution regression, the frozen R2
oracle byte-identical (`185/185`, `146,851` bytes, `97F6C5E5…39C9`), clean format and Release build,
and zero production change. The only qualifications are explicitly bounded and nonblocking: F-01
(real-authority cross-unit union shape), F-02 (managed fault-injection scope), F-03 (SQ-04
historical fixtures) and the preserved carried qualifications in §11.

Recommended next step: record the separate **`S23-LIFE-OWNER`** decision. WP-S23-5, S2.4, S2.5,
Stage 6, and public Find Usages remain unauthorized.
