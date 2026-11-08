# ANDIRIT-CI1A-S2.4-WP-S24-1-R1-IMPL-01 — F-Q1-01 decoder remediation (internal occurrence continuation)

**Commission:** `ANDIRIT-CI1A-S2.4-WP-S24-1-R1-IMPL-01`.
**Project:** Andirit. **Recorded:** 2026-11-08. **Canonical branch:** `main`.
**Reviewed source (Q1):** `de92eeb95463b42914b483534c55a0fa2b0e828b` (tree `bb1b4157360abaeda66e72cd4811fc9898d19f25`).
**Disposition:** **`REMEDIATED — READY FOR INDEPENDENT REQUALIFICATION`**.

> Bounded remediation of the blocking finding **F-Q1-01** (duplicate-key token
> propagated an exception instead of `Invalid` continuation) plus the
> contract-required **F-OBS-Q1-02** canonical-encoding refusal. Production change
> is limited to the token decoder and the reader's cursor well-formedness check;
> focused tests and evidence added. No independent requalification and no owner
> acceptance is performed.

---

## 1. Preflight

| Check | Result |
| --- | --- |
| Starting HEAD | `04ab118f53d905bad6d8b25acc6fafc6889e2447` (expected) |
| Starting tree | `960dfc9c2dad08254c46018f664dbf6916086d8b` (expected) |
| Branch / worktrees | `main`; exactly one (`D:/projects/Andirit`) |
| Tracked/index, stashes, push | clean; no stashes; nothing pushed |
| Environment | Windows `10.0.26200` **x64**; .NET SDK `10.0.401`; runtime `10.0.12` (`environment.log`) |

The accepted S2.4 owner decision was treated as normative; no conflict was found between the commission and the contract.

## 2. Original failure reproduction

The independent reviewer reproduced **F-Q1-01** before this remediation: a continuation token whose decoded JSON contains a duplicated envelope key (`v` twice) caused

```
System.ArgumentException: An item with the same key has already been added. Key: v
```

to propagate out of `LatticeCodeGraphStore.FindOccurrencesAsync`, instead of returning `Continuation = Invalid`. The committed reviewer reproducer and stack are preserved immutably in `docs/reviews/CI1A-S2.4-S24-READER-Q1-evidence/P02-tokens.json` (row `duplicated-field`). This remediation does not modify that historical evidence.

## 3. Root cause

`src/Andirit/CodeOccurrenceContinuation.cs` `TryDecode` wrapped only `JsonNode.Parse` in a try/catch. `JsonObject` detects duplicate property names **lazily** during dictionary materialization (`JsonObject.Count` / enumeration); that materialization happened at line 100 (`json.Count`) and in `ContainsKey`, i.e. **outside** the try/catch, so the `ArgumentException` escaped. The later `json[name]`/`TryGetValue` field reads were also unguarded.

## 4. Exact remediation

Two minimal, bounded changes:

**(a) `src/Andirit/CodeOccurrenceContinuation.cs` — malformed-token refusal (F-Q1-01) and canonical encoding (F-OBS-Q1-02).**
- The JSON materialization and membership checks (`JsonNode.Parse`, `json.Count`, `ContainsKey`) are now inside a **targeted** boundary: `catch (Exception exception) when (exception is JsonException or ArgumentException) { return false; }`. This catches malformed JSON and duplicate-key detection without an unrestricted catch-all that could conceal unrelated programming faults. Field extraction runs after successful materialization (the validated dictionary is already built).
- A **canonical base64url round-trip check** was added: decode → re-encode with the canonical encoder → require exact ordinal equality with the submitted token; otherwise `false`. This rejects padding, the standard base64 alphabet, and any non-canonical variant, per D1.
- Duplicate keys, missing fields, wrong types, unsupported versions, malformed JSON, invalid checksums, oversized tokens, and out-of-range values all return `false` → the reader returns `Continuation = Invalid`.

**(b) `src/Andirit/InMemoryCodeGraphStore.cs` — cursor well-formedness (D1 "an `after` key that is not a well-formed sort key").**
- A `CodeOccurrenceContinuation.IsWellFormedCursor` check (non-empty path; canonical 64-hex occurrence id; one-based, ordered coordinates; page size within `[1, AbsoluteMaxPageSize]`) is applied in the reader **after** the publication binding check, so a token bound to a **superseded** publication still reports `StalePublication` (the reviewer's precedence), while a token for the **current** publication with a malformed cursor reports `Invalid` with the current availability.

No other production behavior was changed. No new token version, envelope field, public API, storage format, ordering, bound, authority rule, coverage aggregation, persistence, or cross-target behavior.

## 5. F-OBS-Q1-02 contract interpretation

The committed owner decision (D1, "Compatibility and invalid-token behavior") explicitly lists **"non-canonical encoding"** among the cases that must produce **invalid continuation**. Strict canonical decoding is therefore **required** by the contract (not merely optional), so the round-trip canonicality check in §4(a) was implemented. It does not change the canonical serialization format or token version, and it accepts every token produced by the production encoder (`Encode` round-trips exactly). The reviewer's original leniency observation (`noncanonical-standard-base64` accepted as `More`) is now resolved to `Invalid`.

## 6. Focused regression evidence

New producer regression `tests/Andirit.CSharp.Tests/S24ReaderContinuationTests.cs::MalformedTokens_FailClosedWithoutPropagating` covers: duplicate `v`, duplicate `repo`, duplicate `pubRun`, duplicate `pubId`, missing `size`, wrong type (`v` as string), malformed JSON, unsupported version, invalid checksum, oversized token, out-of-range size, out-of-range cursor, altered token, and non-canonical base64url — each asserting `Continuation = Invalid`, empty page, no next token, not complete, and **no exception**. Valid-token controls assert pagination continuity and publication staleness.

| Run | Result | Log |
| --- | --- | --- |
| Focused continuation tests (`--filter S24ReaderContinuationTests`) | **16 passed / 0 failed** (15 original + 1 new) | `focused-continuation-tests.log` |

**Independent-harness recheck (supporting evidence).** The committed reviewer adversarial harness (`docs/reviews/CI1A-S2.4-S24-READER-Q1-tests`, unmodified) was re-run against the R1 source and now reports **0 failures** on its token matrix — including `duplicated-field → Invalid` (no throw), `noncanonical-standard-base64 → Invalid`, and preserved `wrong-publication-* → StalePublication`. Its `forged-checksum-consistent-cursor` row is now `Invalid` (cursor well-formedness), a strengthening consistent with D1. All other reviewer verbs (oracle, bounded, publication, coverage, noninterference, authority) also pass (`R1-recheck-*.json`, `R1-recheck-matrix.json`). This is a re-run of committed reviewer tooling, **not** a new independent qualification.

## 7. Full regression receipts

| Gate | Command | Result | Log |
| --- | --- | --- | --- |
| Release build | `dotnet build Andirit.slnx -c Release` | 0 warnings / 0 errors | `release-build.log` |
| Full solution | `dotnet test Andirit.slnx -c Release --no-build` | **904 passed / 0 failed / 0 skipped** (51 + 157 + 345 + 351) | `full-solution-tests.log` |
| Frozen R2 oracle | `SemanticCorpusOracle` | **185/185**, 146,851 bytes, SHA-256 `97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9` | `oracle.log`, `oracle-r2.json` |
| Formatting | `dotnet format Andirit.slnx --verify-no-changes` | exit 0 | `format.log` |

Baseline before R1 was 903 passed; post-change 904 = 903 + the 1 new focused regression. No historical qualification evidence was overwritten.

## 8. Changed-file inventory

| File | Change |
| --- | --- |
| `src/Andirit/CodeOccurrenceContinuation.cs` | malformed-token boundary + canonical base64url check (+ well-formedness helper) |
| `src/Andirit/InMemoryCodeGraphStore.cs` | cursor well-formedness check in the reader (after publication binding) |
| `tests/Andirit.CSharp.Tests/S24ReaderContinuationTests.cs` | +1 focused regression test |
| `docs/reviews/ANDIRIT-CI1A-S2.4-WP-S24-1-R1-IMPL-01.md` | this report |
| `docs/reviews/CI1A-S2.4-WP-S24-1-R1-IMPL-01-evidence/` | R1 evidence + self-excluding manifest (19 entries) |

No public-type surface change (no new public types); no change to ordering, pagination bounds, authority validation, publication-change behavior, coverage aggregation, storage schema, token persistence, or cross-target behavior. The focused-regression count and full-solution count are regression floors, not independent assurance.

## 9. Remaining observations and qualifications

- **F-OBS-Q1-01 (unchanged, non-blocking).** The reader still copies and sorts the full target set per request (`O(n log n)`, target-local) and reports `Examined = target cardinality`; within the D4 target-local bound. No optimization was authorized or performed.
- **Unkeyed checksum (unchanged, per contract).** The `sum` field remains corruption detection, not authentication; a checksum-consistent forged cursor can still skip within one publication and does not bypass trusted-state authority validation. Cryptographic authentication is out of scope.
- **Cursor well-formedness precedence (documented).** A token that is both malformed-cursor and foreign-publication reports `StalePublication` (publication binding first), preserving the reviewer's normative precedence; a malformed cursor on the current publication reports `Invalid`.
- **Preserved qualifications:** F-01, F-02, F-03, Q1-Q1, Q1-Q2, S-01, Q2-Q01, Q2-Q02, O-Q2-01, Q-I01, Q-I08; SQ-04, SQ-05, SQ-06 remainder, D4.5, full S-02, D3-full, IF-02/IF-05 residuals, SQ-07; same-harness independence limitation; Windows-x64 / serialized single-writer boundary. Cross-target remains deferred.
- **Process note (transparency).** While sealing R1 evidence, the committed Q1 reviewer `seal-evidence.ps1` — which is hard-coded to the Q1 evidence directory — transiently rewrote the Q1 `manifest.json` `reviewedHead`/`reviewedTree` fields. The file was **restored byte-exactly from the committed blob** (`04ab118`) and the Q1 evidence is unchanged (verified by Git status); the R1 manifest was generated separately without touching the Q1 evidence. No historical receipt content was altered.

## 10. Requalification readiness

**`REMEDIATED — READY FOR INDEPENDENT REQUALIFICATION`.** F-Q1-01 is fixed (malformed/deferred-duplicate tokens are refused, never propagate) and the D1-required canonical-encoding refusal is implemented and evidenced; the reviewer's own adversarial harness now reports 0 token-matrix failures and all other reviewer verbs pass; the full solution, oracle, build and formatting floors are clean; and the production change is bounded to the decoder plus the reader's post-binding cursor check. A separate **`S24-READER-Q1` requalification** commission (reviewer ≠ this implementer) is required; no owner acceptance, WP-S24-2/WP-S24-3, S2.5, Stage 6, public Find Usages, cross-target, or optimization is authorized.

---

*Remediation report. The F-Q1-01 reproducer remains preserved in the immutable Q1 evidence; this commission fixes the decoder, adds focused regressions, and hands off for independent requalification.*
