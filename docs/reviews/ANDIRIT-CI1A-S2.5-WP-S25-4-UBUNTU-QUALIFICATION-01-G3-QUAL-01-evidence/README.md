# Evidence package — UBUNTU-QUALIFICATION-01-G3-QUAL-01

**Class**: REVIEWER (independent G3 qualification) evidence. The producer-side G2 package is
`../ANDIRIT-CI1A-S2.5-WP-S25-4-UBUNTU-QUALIFICATION-01-G2-EXEC-01-evidence/` (re-verified here,
20/20). The preparation package is
`../ANDIRIT-CI1A-S2.5-WP-S25-4-UBUNTU-QUALIFICATION-01-evidence/`.

**Report**: `../ANDIRIT-CI1A-S2.5-WP-S25-4-UBUNTU-QUALIFICATION-01-G3-QUAL-01.md` (bound by Git).

## Package map

| Path | Content |
| --- | --- |
| `verify-g2-evidence.py` | Independent verifier: 33 primary-source receipts (Git objects, sealed blobs, read-only GitHub records) |
| `g3-verification-receipts.json` | Verifier output — all checks PASS |
| `contract-binding.md` | M11.4/M14.1/M14.2 re-derived verbatim from the governing matrix at HEAD |
| `matrix.md` | Requirement-to-evidence disposition matrix (sections A–G) |
| `findings.md` | Findings with severity and recommended dispositions |
| `manifest.json` | Self-excluding SHA-256 manifest of this package |

## Binding method

1. Read-only remote verification (`ls-remote`, GitHub PR/run/job API) — no push, re-run, or PR
   modification.
2. Git-object re-derivation of the pinned revision, tree, workflow blob, merge ref and ancestry.
3. Byte-exact rehash of all 20 G2 manifest entries from the sealed commit.
4. Independent Cobertura parsing of the Ubuntu artifacts (root attributes + condition-sum
   derivation) for 368/466 and 900/969 and the supplementary suites.
5. Raw-log re-derivation of suite counts, environment, checkout ref, format step and native
   dependency.

## Remote findings (recorded, not resolved)

- **G3-F01 (MATERIAL)**: PR #6 is now MERGED (fast-forward, 2026-11-10T23:05:29Z), after the
  SQ-05 G3 assessment recorded it open/unmerged; content is an ancestor of remote `main`; the
  SQ-05 packages are preserved.
- **G3-F02 (MATERIAL)**: the SQ-05 G2 package committed 14 of 18 manifest entries — the four
  Ubuntu coverage artifacts were never committed (manifest lists them; Git lacks them).