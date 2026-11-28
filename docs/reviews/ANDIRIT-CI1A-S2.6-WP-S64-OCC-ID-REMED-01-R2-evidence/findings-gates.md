# Findings and gates

## Findings

| ID | Finding | Status |
| --- | --- | --- |
| R2-F01 | Host-runtime implementation closure used as semantic authority | **Resolved** target-framework reference packs |
| R2-F02 | Declared target framework ignored | **Resolved** per-project TFM resolution |
| R2-F03 | APIs unavailable under declared target accepted | **Resolved** framework-reference error refusal |
| R2-F04 | C4 oracle bound to expanded runtime closure | **Resolved** target-framework ledger; historical fixtures preserved |
| R2-F05 | Incompatible store physically mutated before refusal | **Resolved** read-only preflight before writable open |
| R2-F06 | Oracle freeze chronology unclear | **Resolved** separate freeze commit `814fe5b` before comparison `82ba813` |
| R2-F07 | Hosted synthetic-merge attribution | **R1 exception accepted; R2 recorded; exact-object execution still requires owner/workflow decision** |
| R2-F08 | macOS reported as x64 | **Corrected** actual ARM64/`osx-arm64`; regression only |
| R2-F09 | Reconstruction loss limits | **Retained** no universal lossless claim |
| OCC-ID-01 | Contract inconsistency | **Resolved** by the transition and R2 input correction |
| OCC-ID-02 | Ambient host references | **Resolved** pack resolution; no runtime fallback |
| OCC-ID-03 | Absolute-path identity | **Resolved** `refctx1` |
| OCC-ID-04 | Cross-host campaign | **Supported within equal-reference scope; hosted synthetic-merge qualification** |
| OCC-ID-05 | Compatibility transition | **Resolved** generation 3 + read-only refusal + re-ingestion |
| R1-01 | C4 occurrence ledger | **Resolved** target-framework ledger frozen |
| R1-06 | Historical procedural exception | **Narrowly preserved** |
| R1-04 / R1-05 | Broader historical-store campaign / D3 | **Outstanding** |
| DF-1 / F-S64E-03 / F-S64E-04 / F-S64E-05 | Broader defects | **Open** |

## Gate matrix

| Gate | Requirement | Result |
| --- | --- | --- |
| R2-G1 | Baseline/seal reconciliation | **Pass** |
| R2-G2 | Reproduce independent findings | **Pass** |
| R2-G3 | Supported TFM matrix and deterministic pack selection | **Pass** |
| R2-G4 | Target-framework compilation and identity consistency | **Pass** |
| R2-G5 | Explicit refusal for unsupported/missing frameworks | **Pass** |
| R2-G6 | Identity version/generation reconciliation | **Pass** (generation 3; domains retained) |
| R2-G7 | C4 target-framework oracle and freeze chronology | **Pass** |
| R2-G8 | Historical oracle preservation | **Pass** |
| R2-G9 | Non-mutating compatibility detection | **Pass** (main/WAL unchanged) |
| R2-G10 | Reconstruction and preservation | **Pass with loss limits** |
| R2-G11 | Cross-host/cross-platform determinism | **Pass within equal-reference scope; Windows exact, WSL Linux exact, hosted Windows/Linux/macOS ARM64 tree-equivalent** |
| R2-G12 | Regression/named cases/coverage/package | **Pass** (1,059; 29/19; floors; smoke) |
| R2-G13 | Hosted provenance and macOS record | **Pass with synthetic-merge qualification; candidate and seal runs succeeded; ARM64 corrected** |
| R2-G14 | Sealed producer evidence | **Pass after seal** |

## Disposition

**A — FRAMEWORK AND COMPATIBILITY REMEDIATION IMPLEMENTED — READY FOR INDEPENDENT
REQUALIFICATION.**

The hosted receipts executed the exact candidate **tree** via a synthetic PR
merge commit. If exact candidate-commit attribution is mandatory, another owner
decision or workflow change is required; this is recorded, not waived. DF-1 and
S6.4 remain open; D3 remains suspended.

