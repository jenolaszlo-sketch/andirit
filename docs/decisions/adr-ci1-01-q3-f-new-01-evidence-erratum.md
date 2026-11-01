# Erratum — Q3-F-NEW-01 (R2 freshness-evidence locator)

**Project:** Andirit

**Parent decision:** [`adr-ci1-01-owner-decision.md`](adr-ci1-01-owner-decision.md)
(ADR-CI1-01-OWNER-01)

**Source finding:** Q3-F-NEW-01, recorded in
`C:\Users\Laszlos\source\repos\Solo\adr-ci1-01-exp-02-q3\FINDINGS.md`
(`ADR-CI1-01-EXP-02-Q3`).

**Recorded:** 2026-11-01

**Status:** Non-blocking evidence-packaging defect. Does not invalidate the
independently re-derived Q3 result.

---

## 1. Nature of the defect

Five documents in the external R2 experiment workspace
(`C:\Users\Laszlos\source\repos\Solo\adr-ci1-01-exp-02-r2`) point at the F03
(B2 publication-freshness) evidence artifact under an **incorrect locator**.
The R2 probe `probes/IndexFreshnessR2` reads its output root from `args[0]`,
while `scripts/run-all.ps1` passes `@('run', $root)`, so the artifact was written
to `run/evidence/index-freshness-r2.json`, not `evidence/index-freshness-r2.json`.

## 2. Incorrect reference

The documented locator is:

`evidence/index-freshness-r2.json` — **does not exist** in the R2 workspace.

## 3. Correct verified reference

| Item | Value |
| --- | --- |
| Actual path | `run/evidence/index-freshness-r2.json` (R2 workspace-relative) |
| Bytes | 7,244 |
| SHA-256 | `72224796f9a112df19d3cebb65acb5e9090fc3b9a8b348e0f7f5a802e68d2a2d` |
| Companion artifacts | `run/evidence/index-freshness-r2/b2.db` (`7903612a…`), `run/evidence/index-freshness-r2/b2.db-wal` (`f1fe7856…`) |
| Recorded in R2 manifest | yes, at `run/evidence/index-freshness-r2.json` |
| `evidence/index-freshness-r2.json` in R2 manifest | no entry |

The hash above was recomputed from the live file during this recording and
matches both the file bytes and the R2 manifest entry
(`evidence/artifact-hashes.json`, scheme `r2-inventory-v1`, 960 entries, tree
`a0f69aa636c524e8671c711895c4f0cfb7e834c6ce7e3e570e30b554241d258b`).

## 4. Affected documents

Q3 named four R2 documents (five references). A fifth R2 document references the
artifact by its bare name. All five are hash-recorded in the R2 manifest, so none
may be edited without breaking the frozen manifest.

| # | Document (R2 workspace) | Line(s) | Locator used | R2 manifest SHA-256 |
| --- | --- | --- | --- | --- |
| 1 | `REPORT.md` | 97 | `evidence/index-freshness-r2.json` | `63f755c03daa87b180db0bb8e9e089824201f3833fb65327e29ea802ef5520d2` |
| 2 | `INDEX-FRESHNESS.md` | 3, 94 | `evidence/index-freshness-r2.json` | `bb1d105495bdd0b129ba69c21095fb22c940808236e388ecead0fd0c5762e1a5` |
| 3 | `Q3-REVIEW-HANDOFF.md` | 53 | `evidence/index-freshness-r2.json` | `41ad1bff92a678a183e3c10dee5bc81a288718585fd46eda0e2dbbb8f84be387` |
| 4 | `FINAL-HANDOFF.md` | 46 | `evidence/index-freshness-r2.json` | `323622075564ab5f8ed1d859ca2ce0c9bff2c81203fb574ae817d5531d220974` |
| 5 | `Q2-FINDING-RECONCILIATION.md` | 17 | `index-freshness-r2.json` (bare name, no `evidence/` prefix) | `dc20e95e408bc6c53c3117767f6b3803b76adba5f41fddad9d8c177010814596` |

Reference count reconciliation: Q3's finding text lists `REPORT.md:97`,
`INDEX-FRESHNESS.md:3,94`, `Q3-REVIEW-HANDOFF.md:53`, `FINAL-HANDOFF.md:46`
(four documents, five references using the exact `evidence/` path). The fifth
document (`Q2-FINDING-RECONCILIATION.md:17`) uses the bare artifact name. The
commission's phrase "five R2 documents reference `evidence/index-freshness-r2.json`"
should be read as: five documents reference the artifact, four of them with the
exact `evidence/` path. This is the only documentation inconsistency identified
in the commission's own framing.

## 5. Why the defect does not invalidate the Q3 result

1. The substantive artifact **exists** and is hash-recorded at its real path; it
   is not missing evidence.
2. Q3 **independently re-derived** the F03 conclusion with its own probe
   (`probes/Q3IndexFreshness`, 14/14; evidence
   `evidence/index-freshness-q3.json`, SHA-256 `8b1d5a87…`) and by reading R2's
   `run/evidence/index-freshness-r2.json` directly. The Q3 verdict does not rest
   on the erroneous locator.
3. The defect is a **locator/packaging** error only; it changes no measured F03
   value and no disposition. Q3 recorded it as non-blocking
   (`Blocks architecture selection? No`).

## 6. Handling (preservation)

- The original R2 evidence and historical manifests are **preserved unchanged**:
  no frozen document, artifact, or manifest was edited or regenerated.
- All five affected documents are **hash-bound** (section 4), so this recording
  deliberately does **not** edit them. Per the commission, mutating hash-bound
  historical evidence is not permitted, and "mutable index documents" do not
  apply here because these files participate in the frozen R2 inventory.
- The minimal producer fix recorded by Q3 (relocate the artifact to
  `evidence/index-freshness-r2.json` and make `probes/IndexFreshnessR2` read
  `args[1]`, **or** correct the five document references to
  `run/evidence/index-freshness-r2.json`) is recorded here as a future option.
  It is **not** executed in this commission and is **not** a CI-1A obligation.
- **No regression test rerun is required solely to repair this reference
  defect.**

## 7. Consumer/contract note

Any future consumer or storage contract that cites the R2 F03 evidence should use
the verified path `run/evidence/index-freshness-r2.json` (SHA-256
`72224796…`) or the independent Q3 evidence
(`evidence/index-freshness-q3.json`, SHA-256 `8b1d5a87…`).
