# Evidence package — ANDIRIT-CI1A-S2.5-WP-S25-4-GATE-QUALIFICATION-01

**Class**: REVIEWER (independent qualification) evidence. The producer's evidence package is
preserved separately at `../ANDIRIT-CI1A-S2.5-WP-S25-4-GATE-REMEDIATION-01-evidence/`
(see its `PRODUCER-EVIDENCE.md`).

**Reviewed subject**: GATE-REMEDIATION-01 at sealed revision `eafd632`
(tree `823e3c655f8fa8aea64b68e0935b1048bc9b6f7d`, parent `f669e107`).

**Report**: `../ANDIRIT-CI1A-S2.5-WP-S25-4-GATE-QUALIFICATION-01.md` (bound by Git).

## Package map

| Path | Content |
| --- | --- |
| `revisions.json` | Binding identities, environment, independence record, work-root deviation (C: → D:) |
| `reproduce.md` | Independent method and verbatim commands |
| `environment.txt`, `gates/toolchain.log` | Reviewer environment (SDK 10.0.401, Windows 10.0.26200, Python 3.14.8) |
| `export-identity.txt` | Exact-byte export identity and independent blob-verification result (4,555 entries, 0 missing/extra/mismatched) |
| `verify-exports.py` | Independent export verifier (QUAL-04 script, unmodified) |
| `make-export.ps1`, `run-gates.ps1`, `run-preservation.ps1`, `run-oracle.ps1` | Reviewer execution scripts |
| `gates/gate-exits.json` | Gate exit codes: restore 0, format 0, release 0, coverage-latticedb 0, full 0 |
| `gates/format.log` | Binding exact-byte formatting gate: `dotnet format Andirit.slnx --no-restore --verify-no-changes` → exit 0 |
| `gates/coverage-latticedb.log` | Binding LatticeDb coverage gate (CI parameters) → exit 0; table Line 92.87% / Branch 78.96% / Method 98.57% |
| `gates/coverage-latticedb/coverage.cobertura.xml` | Reviewer-generated coverage artifact (root: 368/466 branches, 900/969 lines) |
| `gates/release.log` | Release build: exit 0, 0 warnings, 0 errors |
| `gates/full.log`, `gates/trx/` | Whole-solution run: 949 passed / 0 failed / 0 skipped; per-project TRX files |
| `preservation/` | Focused preservation runs (13 membership/identity + 7 native) and the frozen R2 oracle (185/185, byte-identical, 146,851 bytes) |
| `coverage-derivation.json` | Independent numerator/denominator derivation with R3 cross-check |
| `parse-coverage.py` | Coverage derivation script |
| `producer-reconciliation.json` | Producer artifact hashes, coverage/TRX reconciliation, manifest inventory |
| `parse-producer-artifacts.py` | Producer-package parser |
| `matrix.md` | Requirement-to-evidence disposition table |
| `findings.md` | Findings and qualifications |
| `obligations.md` | Outstanding obligation reconciliation (M5.4, M11.4, M14.1, F-G2-01, closure) |
| `manifest.json` | Self-excluding SHA-256 manifest of this package |

## Binding commands

1. `git -c core.autocrlf=false archive` → Python `tarfile` extraction → blob verification.
2. `dotnet format Andirit.slnx --no-restore --verify-no-changes` (CI line 24).
3. `dotnet test tests/Andirit.LatticeDb.Tests/… -c Release --no-restore /p:CollectCoverage=true
   /p:CoverletOutputFormat=cobertura '/p:Include=[Andirit.LatticeDb]*' /p:Threshold=70
   /p:ThresholdType=line%2cbranch` (CI lines 57–64).
4. `dotnet build Andirit.slnx -c Release --no-restore`; `dotnet test Andirit.slnx -c Release
   --no-restore --no-build`.

## Notes

- The worktree has `core.autocrlf=true`; the exact-byte export bypasses all conversion, so
  disk bytes equal Git blob bytes (blobs are LF; the checkout is CRLF). The QUAL-04 finding
  about checkout-profile vs exact-byte divergence applies unchanged.
- First-attempt notes (reviewer-side only, no product impact): the initial execution lived on
  the exhausted C: volume and was relocated to `D:\gate-qualification\…`; an initial oracle
  attempt used `--no-restore` for a non-solution project and failed NETSDK1004, then passed
  after restoring that project. Both are recorded, not hidden.