# Findings and gates

## 1. Investigation findings

| Finding | Status |
| --- | --- |
| OCC-ID-01 contract inconsistency between S2.1 path-bound identity and S2.0/F12 content-bound identity | **Resolved** by the authorized successor contract |
| OCC-ID-02 ambient process-TPA references | **Resolved** by the managed runtime-directory closure |
| OCC-ID-03 absolute-path reference identity | **Resolved** by `refctx1` content-bound identity |
| OCC-ID-04 cross-host campaign incomplete | **Resolved for Windows console/xUnit and cross-path; hosted Linux recorded below** |
| OCC-ID-05 persisted/published compatibility transition | **Resolved** by explicit generation 2, safe refusal and re-ingestion; no in-place migration |
| R1-01 C4 occurrence ledger unfrozen | **Resolved**: independently derived successor ledger frozen and compared |
| R1-06 historical packaging anomaly | **Owner exception recorded**; unchanged; successor seal adds only `manifest.json` |
| F-S64E-03 / DF-1 | **Open**; not closed by this commission |
| F-S64E-04 | **Open**; not closed by this commission |
| F-S64E-05 | **Open** for D3; not closed by this commission |

## 2. Gate matrix

| Gate | Requirement | Result |
| --- | --- | --- |
| R1-G1 | Investigation baseline and predecessor seals verified | **Pass** |
| R1-G2 | Deterministic semantic reference resolution | **Pass** (managed runtime closure; no process TPA) |
| R1-G3 | Content-bound reference identity | **Pass** (`refctx1`, path-free) |
| R1-G4 | Explicit successor identity versions | **Pass** (`compctx3`, `compctx4`, `asmctx4`, `usage5`, generation 2) |
| R1-G5 | Persisted-generation detection and safe refusal | **Pass** (schema/namespace/generation/authority checks) |
| R1-G6 | Re-ingestion reconstruction, historical stores preserved | **Pass** (new-store test; logical rows unchanged) |
| R1-G7 | Cross-host determinism (console/xUnit) | **Pass** (same target/ledger) |
| R1-G8 | Cross-path determinism | **Pass** |
| R1-G9 | Semantic negative controls | **Pass** (different bytes/options/sources remain distinct) |
| R1-G10 | Completed and frozen C4 oracle | **Pass** (four variants, 51/50/1/51) |
| R1-G11 | Full regression | **Pass** (1,044 tests; 0 failed) |
| R1-G12 | Coverage thresholds | **Pass** (all CI thresholds exceeded) |
| R1-G13 | Package smoke | **Pass** (six packages, packed extraction, store, Testing laws) |
| R1-G14 | Exact-candidate Windows x64 hosted CI | **Pass** (run 46132247172, job 140939754018) |
| R1-G15 | Genuine hosted Linux x64 CI | **Pass** (run 46132247172, job 140939753793) |
| R1-G16 | macOS regression only | **Pass** (run 46132247172, job 140939754002) |
| R1-G17 | Sealed producer evidence | **Pass after seal** |

## 3. Disposition

**A — VERSIONED IDENTITY TRANSITION IMPLEMENTED — READY FOR INDEPENDENT QUALIFICATION.**

Subject to the final seal, the owner-authorized transition is implemented and
verified locally and in the hosted Windows, Linux and macOS campaigns. DF-1 and
S6.4 remain open. No independent qualification, release, merge or publication is
claimed.

