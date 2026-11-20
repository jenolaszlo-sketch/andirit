# Findings and qualifications — ANDIRIT-CI1A-S2.5-WP-S25-4-GATE-QUALIFICATION-01

Findings are reviewer-owned. None of them changes a gate outcome on their own; each is a
qualification on the evidence record or on the producer report's precision. No gate is
downgraded by F-03…F-06 because the reviewer independently reproduced the underlying values.

## Findings

| ID | Severity | Finding | Basis | Required follow-up |
| --- | --- | --- | --- | --- |
| F-01 | **Qualification** | Producer evidence manifest does not meet the repository's established convention: `evidence/MANIFEST.txt` is a directory tree listing with no SHA-256 digests or lengths, unlike the QUAL-04/R3 self-excluding SHA-256 `manifest.json` packages | Producer package; comparison with `ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-04-evidence/manifest.json` scheme | Producer supplies a digest-bearing manifest, or the owner waives the convention for this package |
| F-02 | **Qualification** | Producer package contains no raw artifacts for three of its headline exit-code claims: exact-byte format exit 0, whole-solution 949 passed, Release build 0 warnings (no format log, no whole-solution TRX, no build log, no toolchain/SDK record, no export-verification record, no coverage-policy exit record). Only the LatticeDb TRX and the Cobertura XML are present | Producer package inventory | Placeholder: impossible for past runs; independent reviewer receipts now exist (`gates/`) |
| F-03 | **Qualification** | Producer report claims "4,553 tracked blobs" in its post-commit export verification; the actual count at the sealed revision `eafd632` is **4,555** entries (4,553 is the parent `f669e107` count). The zero missing/mismatched/unexpected claims are substantively consistent with the reviewer's 4,555-blob exact verification | `git ls-tree -r` at both revisions; reviewer export verifier (`export-identity.txt`) | Documentation correction; no code impact |
| F-04 | **Qualification** | Producer report and commit message claim "24 focused regression tests"; the actual added test-case count is **23** (13 in `LatticeDbDurableStateRegressionTests` — 9 `[Fact]` + 2 `[Theory]`×2 — plus 10 `[Fact]` in `NativeGraphMirrorTests`). The report's table also labels the new file "(14 tests)" where 13 is actual | Static census at parent vs remediation; reviewer TRX (75 unique cases; parent count 52) | Documentation correction |
| F-05 | **Qualification** | Producer report's identity block records HEAD `f669e107` / tree `5df749d2` as "this HEAD" and as the "Seal" of the remediation commit message; the actual sealed commit is `eafd632` / tree `823e3c655f8fa8aea64b68e0935b1048bc9b6f7d`. Embedding a commit's own identity in its content is self-referential in any case; the established convention (QUAL-04) supplies post-commit identity in the external handoff | `git log` identity vs report §Evidence Package / §Post-Commit Verification | The reviewer binds the commission to `eafd632`; the producer identity block is superseded |
| F-06 | Minor | The producer report's authority-branch note ("18/32") could not be reproduced against any published artifact; the reviewer's per-class derivation shows `CheckOccurrenceAuthorityAsync` conditions covered 10→74 of 128 (halved 5→37 of 64), i.e., a remainder exists but the exact "18/32" expression is not derived in the package | `coverage-derivation.json` | Documentation precision |

## Qualifications carried into this commission's disposition

- The evidence record for the three exit-code claims (format, whole-solution, build) rests on
  reviewer reproduction rather than producer artifacts (F-02).
- All executions are Windows-x64; no Linux/Ubuntu execution was performed or claimed (see
  obligations).
- No organizational-independence evidence exists; independence achieved is session/method
  separation, the same standard applied by QUAL-04.

## Things explicitly checked and found clear

- The two production edits are byte-level comment relocations only; no branch/token/API delta.
- Coverage denominator (466) and class set (24) are identical to the R3 baseline artifact; no
  filter/threshold/instrumentation change exists in the diff.
- Test delta contains zero deleted lines; the single modified test widened its inputs and kept
  its assertion.
- Frozen R2 oracle output is byte-identical (146,851 bytes, SHA-256
  `97f6c5e5a4cc77e089481c7643732b7a07a31d483c2643bd7a95fdad7ced39c9`).