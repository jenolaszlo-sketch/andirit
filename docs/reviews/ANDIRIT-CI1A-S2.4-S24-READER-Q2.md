# ANDIRIT-CI1A-S2.4-S24-READER-Q2 — Independent requalification of the WP-S24-1 R1 remediation

**Commission:** `ANDIRIT-CI1A-S2.4-S24-READER-Q2`.
**Project:** Andirit. **Recorded:** 2026-11-08. **Canonical branch:** `main`.
**Reviewed source:** `39cb0ef18ee787c5c75410bbfa8b58f2f2305bd4` (tree `60c750c19f80a7b35471de2930e1c4632b798f72`); Q1 baseline `de92eeb95463b42914b483534c55a0fa2b0e828b`.
**Disposition:** **`SUPPORTED WITH QUALIFICATIONS`**.

> F-Q1-01 and F-OBS-Q1-02 are independently confirmed resolved with no regression
> to the previously qualified reader behavior. Prior non-blocking observations and
> inherited qualifications remain binding. No production change was made; no owner
> acceptance is recorded.

---

## 1. Verified preflight

| Check | Result |
| --- | --- |
| HEAD | `39cb0ef18ee787c5c75410bbfa8b58f2f2305bd4` (expected) |
| Tree | `60c750c19f80a7b35471de2930e1c4632b798f72` (expected) |
| Branch / worktrees | `main`; exactly one (`D:/projects/Andirit`) |
| Tracked/index, stashes, push | clean; no stashes; nothing pushed |
| Environment | Windows `10.0.26200` **x64**; .NET SDK `10.0.401`; runtime `10.0.12` (`environment.log`) |

No unexpected state, inconsistent evidence, or conflicting authority was found.

## 2. Remediation diff audit (Q2-01)

`git diff de92eeb 39cb0ef` contains only: the Q1 review/evidence (`04ab118`) and the R1 production/test change. The R1-specific change (`04ab118 → 39cb0ef -- src tests`) is exactly three files:

| File | Nature |
| --- | --- |
| `src/Andirit/CodeOccurrenceContinuation.cs` | targeted malformed-token boundary (`catch (… when (JsonException or ArgumentException))`), canonical base64url round-trip check, `IsWellFormedCursor` helper |
| `src/Andirit/InMemoryCodeGraphStore.cs` | cursor well-formedness refusal **after** publication binding |
| `tests/Andirit.CSharp.Tests/S24ReaderContinuationTests.cs` | one focused regression test |

No schema, public-API, persistence, cross-target, ordering, pagination-bound, authority, or coverage-aggregation change was found. The exception boundary is limited to JSON materialization/membership of caller-controlled bytes; field extraction and checksum comparison sit outside it.

## 3. Independent malformed-token results (Q2-02)

Fresh reviewer-owned probe `Q2-P01-malformed.json` — **40 cases, 0 failures**, every case `Invalid` with **no exception** (`threw = false` for all rows): duplicate `v`/`repo`/`pubRun`/`pubId`/`sum`; missing `repo`/`target`/`pubRun`/`afterId`/`size`/`sum`; wrong types (`v` string, `size` string, `repo` number, line string); unexpected extra field; nested object/array where a scalar is required; malformed JSON (truncated, trailing, comment); unsupported versions (`0`, `2`, `-1`); invalid checksums (raw and tampered canonical); oversized (1025, 5000); invalid base64url characters (`%`, space); leading/trailing whitespace; codec-authored out-of-range cursors (size 0/1001, start line 0, negative end column, end line < start line, same-line end column below start, empty path, noncanonical cursor id). A valid canonical token still continues.

Required invariant holds: malformed caller-controlled continuation input produces the accepted `Invalid` refusal without leaking parser/decoder exceptions.

## 4. Canonical encoding results (Q2-03)

`Q2-P02-canonical.json` — **0 failures**: padding variants, same-bytes trailing-pad-bit variants (an alternative base64url string decoding to identical bytes), invalid characters, whitespace, and mixed alphabets are all `Invalid`; the canonical control continues; decode → canonical re-encode is byte-exact (`roundTripEqual = true`).

**Bounded test-construction note (Q2-OBS-01).** For these envelope tokens the canonical base64url string contains no `-`/`_` (the ASCII envelope's base64url and standard-base64 forms coincide), so the "standard-alphabet substitution" and "padded-standard" sub-cases are not applicable as distinct strings for the available tokens. The equivalent D1 noncanonical-encoding requirement is exercised by the padding, trailing-pad-bit, invalid-character, whitespace and mixed-alphabet cases, all refused. This is a probe-construction limitation, not an implementation gap.

## 5. Publication/refusal precedence (Q2-04)

`Q2-P03-precedence.json` — **0 failures**:

| Case | Observed | Normative |
| --- | --- | --- |
| valid-current | `Available` / `Complete` | control OK |
| wrong-pub-run | `StalePublication` / `Invalid` | OK |
| wrong-pub-id | `StalePublication` / `Invalid` | OK |
| malformed-cursor-current | `Available` / `Invalid` | OK |
| malformed-envelope-current | `Available` / `Invalid` | OK |
| stale-after-replacement | `StalePublication` / `Invalid` | OK |
| stale-after-retirement | `AnalysisUnavailable` / `NotRequested` | OK |
| recovery-between-pages | `Available` / `More`, publication `run:x` | OK (no blend) |

A structurally valid but superseded token never restarts or resumes against current state; a malformed cursor on the current publication is refused; invalid tokens never bypass publication or authority checks; recovery does not blend publication universes. The reviewer additionally re-ran the Q1 authority verb: a continuation without current authority is `AnalysisUnavailable`. No precedence ambiguity was encountered; publication binding precedes cursor well-formedness (documented, matching Q1 expectations).

## 6. Regression against Q1 (Q2-05)

Fresh-process re-execution of the **committed Q1 reviewer probes** (reused tooling, marked as reuse) all pass: `oracle`, `tokens` (**0 failures**), `publication`, `coverage`, `noninterference`, `authority`, `bounded`. Fresh Q2 `pagination` reproduces the reviewer-owned total-order oracle exactly at page sizes 1/2/3/7/100 with no duplicates or omissions and correct terminal state. Fresh Q2 `noninterference` shows graph identities unchanged and no persisted token/session rows.

## 7. Bounded-execution assessment (Q2-06)

`Q2-P05-bounded.json` and `Q2-reuse-bounded.json`: for cardinalities 500/2000/8000, `Examined` equals the target cardinality on both the first and continuation pages, the returned page is 10, and a 100,000-character token is refused quickly (`Invalid`, no exception), confirming the length check precedes costly processing. No new unbounded decoder allocation or parsing was introduced by R1.

**F-OBS-Q1-01 preserved:** per-page full-target sorting remains `O(n log n)` within the accepted target-local execution boundary; `Examined` retains its accepted meaning (honest full-target read). No D4 violation is demonstrated; optimization remains out of scope.

## 8. Historical evidence integrity (Q2-07)

| Check | Result |
| --- | --- |
| Q1 `manifest.json` Git blob | `29f6ca6b70b70b5fc96e196e380c965ced386099` at **both** `04ab118` and HEAD (byte-identical) |
| Q1 evidence changed files since `04ab118` | **0** |
| R1 `manifest.json` blob | `679ed12bc225535bacd910a8c6d75a40718cc743` at `39cb0ef` and HEAD |
| R1 manifest verification | 19 entries, 0 mismatches |
| Oracle | 146,851 bytes, SHA-256 `97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9` (exact; see `historical-evidence.json`) |

No historical evidence was modified. The previously reported `seal-evidence.ps1` hazard was avoided: the Q2 evidence package was generated with a **Q2-local** seal script that writes only into the Q2 directory. `historical-evidence.json` records the blob identities.

## 9. Fresh regression receipts (Q2-08)

| Gate | Command | Result | Log |
| --- | --- | --- | --- |
| Release build | `dotnet build Andirit.slnx -c Release` | 0 warnings / 0 errors | `release-build.log` |
| Full solution | `dotnet test Andirit.slnx -c Release --no-build` | **904 passed / 0 failed / 0 skipped** (51 + 157 + 345 + 351) | `full-solution-tests.log` |
| Focused continuation | `--filter S24ReaderContinuationTests` | **16 passed / 0 failed** | `focused-continuation-tests.log` |
| Frozen R2 oracle | `SemanticCorpusOracle` | **185/185**, 146,851 bytes, SHA-256 `97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9` | `oracle.log`, `oracle-r2.json` |
| Formatting | `dotnet format Andirit.slnx --verify-no-changes` | exit 0 | `format.log` |

Overlapping suites are not aggregated as independent assurance. Full probe matrix: `probe-matrix.txt` (14 phases, 0 failed).

## 10. Finding-by-finding disposition

| Finding | Status | Evidence |
| --- | --- | --- |
| **F-Q1-01** (duplicate-key token threw instead of `Invalid`) | **RESOLVED** | `Q2-P01-malformed.json` (duplicate cases → `Invalid`, no throw); `Q2-reuse-tokens.json` (0 failures) |
| **F-OBS-Q1-02** (noncanonical base64url accepted) | **RESOLVED** | `Q2-P02-canonical.json` (padding / trailing-pad-bit / invalid-char / whitespace / mixed → `Invalid`) |
| **F-OBS-Q1-01** (per-page full-target sort) | **PRESERVED (non-blocking)** | `Q2-P05-bounded.json`, `Q2-reuse-bounded.json`; target-local, `Examined` = cardinality |
| **F-OBS-Q1-03** (unkeyed checksum) | **PRESERVED (per contract)** | No authority bypass: wrong repo/target/mode/publication refused against trusted state; cryptographic authentication out of scope |
| **Q2-OBS-01** (canonical alphabet sub-cases N/A for ASCII tokens) | **OBSERVATION (non-blocking)** | §4; equivalent noncanonical variants exercised |

No new blocking finding. No material D1/D5 violation remains.

## 11. Binding qualifications

Inherited S2.3 and S2.4 qualifications remain binding and are not reinterpreted: **F-01, F-02, F-03, Q1-Q1, Q1-Q2, S-01, Q2-Q01, Q2-Q02, O-Q2-01, Q-I01, Q-I08**, the same-harness reviewer independence limitation, and the Windows-x64 / serialized single-writer boundary. Deferred S2.5/Stage-2 obligations remain open: **SQ-04, SQ-05, SQ-06 remainder, S-01 integrated proof, D4.5, full S-02, D3-full, IF-02/IF-05 residuals, SQ-07**. Cross-target remains deferred. The bounded-execution observation F-OBS-Q1-01 and the unkeyed-checksum limitation F-OBS-Q1-03 are preserved.

## 12. Final independent qualification disposition

**`SUPPORTED WITH QUALIFICATIONS`.**

The R1 remediation fixes the blocking F-Q1-01 refusal defect and implements the D1-required canonical-encoding refusal without regression to deterministic ordering, pagination completeness, exact-target identity, publication binding, per-request authority validation, scope-level coverage, incomplete-analysis semantics, D3 noninterference, legacy compatibility, or non-persisted token behavior. Independent reviewer probes (40 malformed cases, canonical variants, precedence, pagination oracle, bounded scaling, noninterference) and fresh-process re-execution of the committed Q1 reviewer probes all pass; the regression floor, oracle, build and formatting are clean; historical evidence is byte-identical. The remaining items are the explicitly preserved non-blocking observations and inherited qualifications.

## 13. Recommendation for the next owner decision

A separate **`WP-S24-1-OWNER`** decision (owner acceptance of the qualified WP-S24-1 result after Q2) is the appropriate next step; it is **not** authorized by this commission. WP-S24-2 remains a separate bounded package requiring its own commission; WP-S24-3 (cross-target), S2.5, Stage 6, public Find Usages, and any optimization remain unauthorized. No production code was modified and no remediation is required.

---

*Independent requalification report. Reviewer tests/probes/evidence are additive; historical Q1 and R1 evidence is preserved byte-exactly.*
