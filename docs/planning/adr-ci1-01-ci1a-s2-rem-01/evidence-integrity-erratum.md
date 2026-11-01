# Evidence Integrity Erratum — REM-01

**Scope:** Stage 1 independent review package and the interrupted Codex workspace.
**Rule:** This erratum is append-only. It does not edit frozen reviewer files,
rewrite historical manifests, regenerate a manifest to replace the original, or
invent missing evidence.

---

## 1. Stage 1 reviewer package

Location:
`C:\Users\Laszlos\source\repos\Solo\adr-ci1-01-ci1a-impl-01-s1-q1`

| Item | Finding |
|---|---|
| Manifest rows checked | 22 (16 reviewer + 6 producer-manifest rows) |
| Verified | 21 |
| Mismatch | 1 |
| Missing required document | `REPORT.md` |
| Claimed scheme | `q1-review-v1` in S1A |
| Actual scheme | `sha256-file-v1`; no aggregate-tree algorithm defined |

### 1.1 Exact mismatch

| Field | Value |
|---|---|
| Artifact | `FINAL-HANDOFF.md` |
| Expected in manifest | `1543cd4a80933506a64ddeaf578c08922ff640d2a6d365fd567d0c9e30007f48` |
| Recorded actual in manifest | `283de40be767002d06ac1f05f86cea334b2898414045316aca7caf9ec10bf457` |
| Observed actual | `283de40be767002d06ac1f05f86cea334b2898414045316aca7caf9ec10bf457` |
| Observed matches expected | No |
| Observed matches recorded actual | Yes |

The manifest itself records both values. The package is internally honest about
the mismatch; the defect is that the owner acceptance cites the package as if all
hashes verified.

### 1.2 Missing report

`REPORT.md` is absent. `FINAL-HANDOFF.md` contains an overview but is not treated
as a verified substitute. No replacement report is fabricated here.

### 1.3 Aggregate tree

S1A line 27 claims scheme `q1-review-v1` with tree
`c8d4f2a1e3b7f9e5d2c4a8b1f3e6d9c2a5f8e1b4c7d0a3f6e9b2c5d8a1f4e7b0`.
The actual manifest uses `sha256-file-v1` and defines no aggregate tree, so the
claimed tree is not reproducible from the stated scheme.

### 1.4 Materiality

- `FINDINGS.md` and the technical reviews match their manifest hashes.
- The six producer evidence documents and the producer manifest match pinned Git
  blobs.
- The Stage 1 behavioral/storage conclusions remain supportable.
- The package-integrity claim is overstated; a qualification/erratum is required.
- No unique mandatory evidence is known to exist only in the missing
  `REPORT.md`; the technical review files are present. This does not make the
  missing report acceptable as a substitute for a verified artifact.

---

## 2. Interrupted Codex workspace

Location:
`C:\Users\Laszlos\source\repos\Solo\adr-ci1-01-ci1a-s2-boundary-q1`

| Item | Finding |
|---|---|
| Materialized review docs | none (only `evidence/`) |
| Generator | `evidence/build_review.py`, never executed |
| Intended documents | 15 |
| Intended probes | 14 |
| Source identities | 31/31 Git blobs match |
| SHA-256/byte rows | 30/31 match |
| Producer copies | 7/7 match pinned Git blobs |
| Package manifest | absent |

### 2.1 Exact mismatch

| Field | Value |
|---|---|
| Artifact | `src/Andirit.CSharp/CSharpProjectDiscovery.cs` |
| Declared Git blob | `8f921e383a9320726696694546418cdad9f9cbf2` |
| Actual Git blob | `8f921e383a9320726696694546418cdad9f9cbf2` |
| Declared SHA-256 | `c66c641137e6d3d237e65864d40e52a90348a380784de104544706e20a6a288e` |
| Actual SHA-256 | `38c1bf9fd8a6ca48dd27af4d85e0601d7d592fef4a954fcdece6fc3a5a485a44` |
| Declared bytes | 15,464 |
| Actual bytes | 15,466 |
| Materiality | Evidence-metadata defect only; the pinned Git blob matches and the actual source was readable. |

### 2.2 Materiality

The Codex draft is directionally consistent with Q1B but was never materialized
and does not meet the Q1B package counts. It is supporting historical evidence,
not an accepted review package. The mismatch does not affect the source-bound
Q1B findings.

---

## 3. Required owner action

Append this erratum to the Stage 1 acceptance trail, or issue a standalone
owner-authorised erratum with the same content. Do not regenerate the original
manifest or alter the frozen package. Future citations must distinguish:

- verified technical evidence;
- the single handoff mismatch;
- the missing report;
- the unreproducible aggregate tree;
- the Codex metadata mismatch.

The Stage 1 storage acceptance is not revoked by this erratum. The claim that
the package is fully source-bound is qualified.

---

## 4. OWNER-01 additive correction (2026-11-02)

Authority: REM-01-Q1 `NEW-FINDINGS.md` NEW-03 and
`EVIDENCE-INTEGRITY-REVIEW.md` §§1–4, manifest-verified by OWNER-01.
This section corrects the statements above additively; it preserves the earlier
text and every frozen package/manifest. The later correction controls citations.

### 4.1 The Stage 1 manifest does not record the observed hash

Section 1.1's “Recorded actual in manifest”, “Observed matches recorded actual”
and “The manifest itself records both values” are inaccurate. The original
`evidence/q1-reviewer-manifest.txt` records only expected
`1543cd4a80933506a64ddeaf578c08922ff640d2a6d365fd567d0c9e30007f48`.
Observed `283de40be767002d06ac1f05f86cea334b2898414045316aca7caf9ec10bf457`
is a reviewer/erratum/OWNER-01 measurement of the current `FINAL-HANDOFF.md`,
not a second self-declared manifest row. No assertion that the original package
self-recorded this mismatch should be inferred. Material discrepancy remains.

OWNER-01 rehashed 15/16 reviewer rows and 6/6 producer rows: **21/22** relevant
rows reproduce. `REPORT.md` is still absent. The claimed Stage 1 aggregate tree
remains unverified; no aggregate algorithm has been recovered. Technical review
files and six producer artifacts remain reproducible; bounded storage conclusions
remain supportable. This does not verify the entire historical package.

### 4.2 Codex row identifies Git blob bytes, not materialized bytes

Artifact `src/Andirit.CSharp/CSharpProjectDiscovery.cs`; Git blob
`8f921e383a9320726696694546418cdad9f9cbf2` at both pinned commits
`76ebaae84d741a38f92d2422c63742197c36bc82` and
`4c6d26a1d103a62fffe2e2743467eedb5f21afc2`.

| Representation | SHA-256 | Bytes | Line-ending condition measured in OWNER-01 |
| --- | --- | ---: | --- |
| Raw pinned Git blob (`git cat-file blob`) | `c66c641137e6d3d237e65864d40e52a90348a380784de104544706e20a6a288e` | 15,464 | 369 LF, zero CRLF pairs |
| Materialized canonical working file at reviewed source | `38c1bf9fd8a6ca48dd27af4d85e0601d7d592fef4a954fcdece6fc3a5a485a44` | 15,466 | 369 LF bytes, two occur in CRLF pairs |

Binary comparison after replacing CRLF pairs with LF is exactly equal.
The two extra CR bytes explain the byte/hash discrepancy. The declared SHA/size
are exactly correct for the pinned blob, and must not be cited as working-file
identity. This refines the cause to line-ending materialization; it is not a
source-content mismatch. No missing hash was reconstructed or invented.

The interrupted Codex draft remains supporting history, not a materialized
accepted independent review. Stage 1 aggregate integrity remains qualified.
OWNER-01's acceptance is the additive historical owner-acceptance reference;
original Stage 1 decision text and frozen evidence remain unchanged.
