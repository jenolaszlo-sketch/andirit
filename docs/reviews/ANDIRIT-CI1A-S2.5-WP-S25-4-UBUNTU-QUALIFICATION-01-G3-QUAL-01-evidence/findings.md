# Findings — UBUNTU-QUALIFICATION-01-G3-QUAL-01

Severity classes: BLOCKING (stops M11.4/M14.1) · MATERIAL (requires owner attention; does not
block this commission's rows) · INFORMATIONAL (recorded, no action for this commission).

## Findings

| ID | Severity | Finding | Basis / evidence |
| --- | --- | --- | --- |
| G3-F01 | MATERIAL | **PR #6 (the SQ-05 qualification anchor) is now MERGED.** Fast-forward with merge commit = head SHA `7457284…`, merged 2026-11-10T23:05:29Z — after the SQ-05 G3 assessment recorded it open/unmerged and contrary to the WP-S25-2 §12 cleanup determination that its disposition required a separate bounded remote-resource decision. Content-wise nothing was lost: `7457284…` is an ancestor of remote `main` (`48370711…`), and the SQ-05 G2/G3 packages are committed repository evidence. Recommended disposition: owner reconciles the merge's authorization in a record (or confirms it was intended); no impact on M11.4/M14.1 evidence | GitHub API (`gh pr view 6`: state MERGED, mergeCommit=`7457284…`, mergedAt 2026-11-10T23:05:29Z); local ancestry verification; `ls-remote` |
| G3-F02 | MATERIAL | **The SQ-05 G2 evidence package is incomplete in Git**: its manifest lists 18 entries but the sealed commit `df57a2a` carries only 14 — the four `coverage-results-ubuntu-latest/*.cobertura.xml` artifacts were never committed (rc 128 on `cat-file`). The historical WP-S25-2 record "G2 manifest 18/0" was evidently verified in the producer working tree, not against the committed package. The SQ-05 coverage counters survive in the G3 `SQ05-Q1` package (6/6 verified) and the owner decision; the current commission derives its own counters from the current run. Recommended disposition: an additive errata note in the historical record, or an owner acknowledgment; nothing is reconstructed | `git cat-file` absence; package history single-commit; manifest comparison |
| G3-F03 | INFORMATIONAL | Raw job log preserved as UTF-16LE bytes (captured through PowerShell redirection; content is the gh-emitted log text). Noted for future captures so byte identity is unambiguous | Blob BOM inspection in `verify-g2-evidence.py` |
| G3-F04 | INFORMATIONAL | Coverlet granularity: root `branches-valid` exceeds the line-derived condition half by +2 on Ubuntu (368/466 root vs 368/464 half) — identical to the Windows artifact, i.e., a coverlet artifact, not a platform difference. Other suites show larger, constant offsets (core 20, csharp 26, treesitter 2) | `g3-verification-receipts.json` derivations |
| G3-F05 | INFORMATIONAL | Supplementary counts shifted as expected since SQ-05 (CSharp 353→372 from the membership remediation); recorded so the Linux re-baseline is explicit | Raw-log counts vs SQ-05 G2 record |

## Qualifications carried (non-blocking; restated at G4)

1. Managed fault injection only — no crash/power-loss durability claim (F-02, unchanged).
2. Locking/WAL/mmap internals not directly instrumented; platform claims are suite-observed
   counter parity.
3. Demonstrated environment is Ubuntu 26.04.1 LTS hosted / SDK 10.0.404; no claim about other
   generations or architectures.
4. Hosted-run independence is receipt-based (primary-source re-derivation in this commission).

## Explicitly verified clear

- All six G1 stop conditions; the five identities; workflow identity at pinned/base/merge.
- 75/0/0 without filter; coverage 368/466 and 900/969 from the raw artifacts; all four coverage
  gates above thresholds on Ubuntu; format step exit 0; pack success; full run success.
- G2 evidence package 20/20 byte-exact against its manifest.
- No production/test/CI change from the pinned revision.

## Non-findings (out of scope)

M5.4, WP-S25-4 closure, Stage-2 acceptance, Stage 6, crash/power-loss full certification, and any
broad public-contract claim are outside this commission and are not affected by its evidence.