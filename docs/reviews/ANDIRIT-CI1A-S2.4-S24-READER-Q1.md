# ANDIRIT-CI1A-S2.4-S24-READER-Q1 — Independent qualification of WP-S24-1 (internal occurrence continuation + coverage)

**Commission:** `ANDIRIT-CI1A-S2.4-S24-READER-Q1`.
**Project:** Andirit. **Recorded:** 2026-11-08. **Canonical branch:** `main`.
**Reviewed source:** `de92eeb95463b42914b483534c55a0fa2b0e828b` (tree `bb1b4157360abaeda66e72cd4811fc9898d19f25`).
**Disposition:** **`REMEDIATION REQUIRED`**.

> One normative refusal-contract defect was found and reproduced with a
> fresh-process reviewer probe (F-Q1-01). All other qualified checks pass. No
> production code was modified; the reproducer and receipts are committed for a
> separate remediation authorization.

---

## 1. Preflight verification

| Check | Result |
| --- | --- |
| HEAD | `de92eeb95463b42914b483534c55a0fa2b0e828b` (expected) |
| Tree | `bb1b4157360abaeda66e72cd4811fc9898d19f25` (expected) |
| Branch / worktrees | `main`; exactly one (`D:/projects/Andirit`) |
| Tracked/index, stashes, push | clean; no stashes; nothing pushed |
| Environment | Windows `10.0.26200` **x64**; .NET SDK `10.0.401`; runtime `10.0.12` (`environment.log`) |
| Producer evidence manifest (WP-S24-1-IMPL-01) | **10 entries, 0 mismatches** (re-verified from committed bytes) |

The accepted owner contract ([entry decision](../../decisions/adr-ci1-01-ci1a-s2-4-entry-owner-01.md), [work-package plan](../../planning/adr-ci1-01-ci1a-s2-4/work-package-plan.md)) was treated as normative; the implementation report was treated as evidence to verify.

## 2. D1–D8 compliance matrix

| Requirement | Verdict | Evidence |
| --- | --- | --- |
| **D1** continuation identity (opaque, versioned, strict decode, binding, stateless, non-persisted) | **PARTIAL — defect F-Q1-01** | 15/16 adversarial cases normative; duplicate-key envelope **throws** instead of refusing |
| **D2** publication-change refusal (no restart/blend) | Met | `P04` stale-after-replacement/retirement; error precedence correct |
| **D3** deterministic pagination | Met | `P01` independent oracle exact at page sizes 1/2/3/7/100 |
| **D4** bounded execution | Met (with observation F-OBS-Q1-01) | `P03` + code-path analysis: target-local, `Examined` truthful |
| **D5** authority/availability separation | Met (except the D1 throw) | `P04`/`P05`/`P07`; `Invalid` kept distinct from `StalePublication` |
| **D6** scope-level coverage breakdown | Met | `P05`: per-unit, worst-wins, stable across pages, provenance retained |
| **D7** cross-target scope deferred | Met | no cross-target code; `mode` reserved |
| **D8** boundary (internal only; no public Find Usages; no persistence/migration) | Met | `P06` row invariance; public-type snapshot update limited to the two authorized additive types |

**Public-type snapshot (Q1).** The two additive public types (`Andirit.CodeOccurrenceContinuationState`, `Andirit.CodeOccurrenceUnitCoverage`) remain within the authorized internal reader contract (entry decision §3/§9; MD-03). No public Find Usages surface, endpoint, or unrelated API expansion was introduced. **No unapproved public API exposure was found.**

## 3. Independent pagination oracle results

The reviewer built a 7-occurrence target with mixed positions and equal-position ties, and computed the expected order with a **reviewer-owned** tuple comparison (path ordinal, start line, start column, end line, end column, id ordinal) — not the production helper. Concatenated pages were compared at page sizes 1, 2, 3, 7, 100.

| Page size | Pages | Returned | Distinct | Matches oracle | Terminal | Result |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | 7 | 7 | 7 | yes | yes | PASS |
| 2 | 4 | 7 | 7 | yes | yes | PASS |
| 3 | 3 | 7 | 7 | yes | yes | PASS |
| 7 | 1 | 7 | 7 | yes | yes | PASS |
| 100 | 1 | 7 | 7 | yes | yes | PASS |

No duplicates, no omissions; strict total order; terminal state, `TotalMatches`, `Truncated`, and `NextContinuationToken` consistent (`P01-oracle.json`).

## 4. Token adversarial matrix (Q2)

16 cases, each a fresh-process reviewer probe (`P02-tokens.json`):

| # | Case | Observed | OK |
| --- | --- | --- | --- |
| 1 | malformed base64url | `Invalid` | yes |
| 2 | noncanonical standard base64/padding | accepted (`More`) — leniency (same bytes) | yes (obs) |
| 3 | oversized | `Invalid` | yes |
| 4 | unsupported version | `Invalid` | yes |
| 5 | **missing field** | `Invalid` | yes |
| 5 | **duplicated field** | **throws `ArgumentException`** | **NO — F-Q1-01** |
| 6 | invalid numeric range (size 0) | `Invalid` | yes |
| 6 | invalid numeric range (size > max) | `Invalid` | yes |
| 7 | wrong repository | `Invalid` | yes |
| 8 | wrong exact target | `Invalid` | yes |
| 9 | wrong mode | `Invalid` | yes |
| 10 | wrong direction | `Invalid` | yes |
| 11 | wrong page size (range) | `Invalid` | yes |
| 12 | wrong publication run | `StalePublication` | yes |
| 13 | wrong publication identity | `StalePublication` | yes |
| 14 | altered cursor (byte change) | `Invalid` | yes |
| 15 | recomputed unkeyed checksum / forged cursor | accepted (`More`) — documented limitation | yes (obs) |
| 19 | replayed after durable reopen | `Available` (same publication) | yes |
| 20 | replayed after recovery | `Available` (same publication) | yes |

**Authority vs corruption.** The `sum` field is confirmed to be **corruption detection only**; a checksum-consistent forged cursor is accepted (case 15) and can skip within one publication — this is the accepted unkeyed limitation and does **not** bypass trusted-state authority validation (wrong repo/target/mode/publication are all rejected against trusted state, not against the token). Preventing forged-cursor skips would require cryptographic authentication, which the accepted decision explicitly does not require for the internal surface.

**Case 5 defect.** A token whose decoded JSON has a duplicated envelope key (e.g. `v` twice) causes an **unhandled `ArgumentException`** instead of a refusal. See F-Q1-01.

## 5. Bounded-execution code-path analysis and scaling evidence (Q4)

**Code path (`InMemoryCodeGraphStore.FindOccurrencesAsync`, lines 937–964).** Per request the reader:
1. looks up the target index by key — **O(1)**, target-local;
2. copies the target entry list and `Array.Sort`s it in full — **O(n log n)** for the target cardinality `n`;
3. linearly scans the sorted array, skipping entries `≤` the cursor and taking `pageSize`;
4. sets `Examined = ordered.Length` (**full target cardinality**) and `TotalMatches = ordered.Length`.

It does **not** enumerate non-target occurrences, does not traverse the repository, does not materialize unbounded intermediates beyond the single target set, and reports the honest full-target examination count (it does not count only returned candidates). `CoverageBreakdown`/`RepositoryCoverage` are bounded by unit count, not occurrence count.

**Scaling probe (`P03-bounded.json`).** Page size 10, first and second (continuation) pages:

| Target cardinality | Examined (page 1 / page 2) | Returned | TotalMatches | page-1 ms | page-2 ms |
| --- | --- | --- | --- | --- | --- |
| 500 | 500 / 500 | 10 / 10 | 500 | 8.4 | — |
| 2,000 | 2,000 / 2,000 | 10 / 10 | 2,000 | 0.5 | — |
| 8,000 | 8,000 / 8,000 | 10 / 10 | 8,000 | 2.3 | — |

**Assessment.** The D4 normative bound is "work bounded by the target candidate set; a small result page must not hide an unbounded scan; no repository-global scan." The implementation performs a **full-target sort per page** (O(n log n), target-local) and reports `Examined = n`. This is **within** the D4 target-local bound (no unbounded/global work), so it is **not** a D4 violation; it is recorded as **F-OBS-Q1-01** (a per-page re-sort/re-read performance characteristic, not a bound violation). Optimizing it (e.g., a persisted/target-local cursor index) is **not** authorized here. Approving bounded execution does not rest solely on reported `Examined` values.

## 6. Publication/authority findings (Q5)

`P04-publication.json` (fresh-process reviewer probe):
- **Stale after replacement** → `StalePublication` + `Invalid`, empty, not complete.
- **Stale after retirement** → `StalePublication`/`AnalysisUnavailable`, empty.
- **Continuation after durable reopen** → `Available`, correct second page (same publication identity reconstructed).
- **Continuation after recovery** → `Available`, correct second page (recovery rebuild preserves the publication).
- **No silent restart; no mixed-publication page; no false `Available`/`Complete`.**

`P07-authority.json` (real C# plugin publication): real verified authority serves the first page; a continuation presented **without** current authority is refused as `AnalysisUnavailable` with no page — confirming **per-request authority revalidation**.

**Error precedence.** A malformed token is refused (`Invalid`) before publication binding is evaluated; a valid token for a superseded publication yields `StalePublication`+`Invalid`. No case returns a stale/mixed page. The one deviation is F-Q1-01 (throw, not refusal).

## 7. Coverage provenance findings (Q6)

`P05-coverage.json`: two units — one `Produced` (2 facts), one `Partial` (0 facts):
- Every page reports `IncompleteAnalysis` (worst-wins) with `IsComplete = false` — no false-complete coverage on any page, including the terminal page.
- The scope-level per-unit breakdown is **stable across pages** and lists both contributors with `state`/`facts`.
- Complete vs incomplete contributors are distinguished; provenance is retained.
- Deduplicated cross-unit occurrences do not silently drop provenance (contributors unioned on the hit, both units present in the breakdown).

The reviewer used **synthetic constructed fixtures**; this does not establish unrestricted real-authority union (F-01 preserved).

## 8. Compatibility and noninterference (Q7)

`P06-noninterference.json`: graph node identities unchanged across pagination; persisted occurrence/`HetuUnit` row counts unchanged before/after pagination — **no token/session persistence, no new schema label, no dual-write**. Legacy no-token queries return all results unchanged (producer tests + reviewer `P01` page size ≥ count). Existing S2.3 authority/availability semantics are preserved (full suite + `P04`/`P07`).

**`IsComplete` tightening.** The change makes an intermediate/rejected page non-complete, exactly as the accepted contract requires; the full solution suite (903/0/0) confirms no unauthorized compatibility break.

## 9. Regression receipts

| Gate | Command | Result | Log |
| --- | --- | --- | --- |
| Release build | `dotnet build Andirit.slnx -c Release` | 0 warnings / 0 errors | `release-build.log` |
| Full solution | `dotnet test Andirit.slnx -c Release --no-build` | **903 passed / 0 failed / 0 skipped** (51 + 157 + 345 + 350) | `full-solution-tests.log` |
| Frozen R2 oracle | `SemanticCorpusOracle` | **185/185**, 146,851 bytes, SHA-256 `97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9` | `oracle.log`, `oracle-r2.json` |
| Formatting | `dotnet format Andirit.slnx --verify-no-changes` | exit 0 | `format.log` |
| Producer manifest | WP-S24-1-IMPL-01 `manifest.json` | 10 entries, 0 mismatches | re-verified |

Counts are regression floors; overlapping suites are not aggregated as independent assurance.

## 10. Evidence provenance and limitations

- **Fresh reviewer evidence:** probes `P01`–`P08` (each its own OS process; recorded `processId`); the independent pagination oracle; the token adversarial matrix; scaling measurements; the reviewer regression run. Manifest `docs/reviews/CI1A-S2.4-S24-READER-Q1-evidence/manifest.json` (**27 entries, 0 mismatches**).
- **Reused predecessor evidence:** S2.3 acceptance; WP-S24-1 producer tests/manifest (verified, not trusted).
- **Synthetic fixtures:** all reviewer occurrence sets (constructed native facts); the real-project authority leg uses the accepted C# plugin.
- **Static source findings:** F-Q1-01 root cause and the bounded-execution code-path analysis.
- **Not demonstrated / out of scope:** cross-target (deferred); cryptographic token authentication; multi-process/crash; platform beyond Windows x64; unrestricted real-authority union.
- **Same-harness independence limitation** persists (reviewer and producer share the harness); independence rests on reviewer-owned tools, contract-derived expected values, and commit separation.

## 11. Findings and remediation requirements

**F-Q1-01 — Malformed duplicate-key continuation token throws instead of being refused. (BLOCKING)**
- **Severity:** High — normative refusal-contract violation (unhandled exception on malformed input).
- **Affected contract:** D1 ("strictly decoded and validated; reject malformed … tokens") and D5 ("Invalid continuation" must be represented, not thrown).
- **Minimal reproduction:** base64url-encode a decoded JSON envelope with a duplicated key (e.g. `{"v":1,"v":1,"repo":…,…}`) and pass it as `CodeOccurrenceQuery.ContinuationToken` to `LatticeCodeGraphStore.FindOccurrencesAsync`.
- **Expected:** `Availability` unchanged, `Continuation = Invalid`, `Occurrences = 0`, `IsComplete = false`.
- **Actual:** `System.ArgumentException: An item with the same key has already been added. Key: v` propagates out of the reader.
- **Evidence:** `P02-tokens.json` row `duplicated-field` (with reviewer stack); root cause in `src/Andirit/CodeOccurrenceContinuation.cs` `TryDecode` — only `JsonNode.Parse` is wrapped in try/catch, but `JsonObject` duplicate-key detection is deferred to `.Count`/`ContainsKey`/field access, which execute outside the try/catch.
- **Blocks acceptance:** **yes.**
- **Smallest remediation boundary (not implemented here):** wrap the whole `TryDecode` body (including `Count`/`ContainsKey`/field reads) in a single `try/catch` that returns `false`, or enable strict duplicate-property rejection. No production change is made by this commission.

**F-OBS-Q1-01 — Per-page full-target sort/re-read (non-blocking).** Each request copies and sorts the full target set (O(n log n)) and reports `Examined = n`. Target-local, within D4's bound; recorded for transparency. Optimization requires separate authorization.

**F-OBS-Q1-02 — Noncanonical base64 leniency (non-blocking).** Tokens with standard-base64 characters/padding that decode to the same bytes are accepted. No correctness impact.

**F-OBS-Q1-03 — Unkeyed checksum (non-blocking, per contract).** A checksum-consistent forged cursor is accepted and can skip within one publication; this is the accepted unkeyed limitation and does not bypass trusted-state authority validation. Cryptographic authentication would be required to prevent it and is out of scope.

## 12. Final qualification disposition

**`REMEDIATION REQUIRED`.**

The deterministic pagination, publication/authority refusal (except the throwing case), scope-level coverage, bounded-execution (target-local), compatibility and regression requirements are supported by independent evidence. However, **F-Q1-01** is a demonstrated normative violation of the token refusal contract (D1/D5) — a malformed token must be refused, and it instead throws an unhandled `ArgumentException` — so WP-S24-1 cannot be qualified as-is. A passing regression suite does not downgrade this.

## 13. Recommended next owner action

Authorize a **bounded WP-S24-1 remediation** (separate commission) limited to making `CodeOccurrenceContinuation.TryDecode` reject all malformed envelopes (including duplicate/missing fields) by returning `false` for any decode/validation failure, with a focused regression for the duplicate-key reproduction, then a **re-qualification** (`S24-READER-Q1` re-run). No owner acceptance, WP-S24-2/WP-S24-3, S2.5, Stage 6, public Find Usages, cross-target, or optimization is authorized here.

---

*Reviewer qualification report. No production code was modified; F-Q1-01 is preserved with a reproducible reproducer and receipts for separate remediation authorization.*
