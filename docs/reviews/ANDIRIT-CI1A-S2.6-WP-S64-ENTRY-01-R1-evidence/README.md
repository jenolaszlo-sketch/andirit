# S6.4 re-entry evidence

This package supports
[ANDIRIT-CI1A-S2.6-WP-S64-ENTRY-01-R1](../ANDIRIT-CI1A-S2.6-WP-S64-ENTRY-01-R1.md)
and the
[owner acceptance](../../decisions/adr-ci1-01-s2-6-wp-s64-mirror-remed-acceptance-owner-01.md)
and
[closure record](../ANDIRIT-CI1A-S2.6-WP-S64-MIRROR-REMED-CLOSE-01.md).

## Contents

| Path | Purpose |
| --- | --- |
| `identities.json` | Accepted baseline, qualification and re-entry identities |
| `contract-reconciliation.md` | Original S6.4 requirements and status |
| `corpus.md` | C1–C4 composition, provenance, controls and limits |
| `corpus-manifest.json` | 52 source/project files and 14 expected artifacts, byte-bound |
| `expected/` | Frozen graph/occurrence ledgers and variant deltas |
| `d3-oracle/` | Independent Roslyn graph oracle |
| `validation.json` | 27 passing corpus/oracle checks |
| `d3-case-matrix.md` | D3-01…08 inputs, expectations, controls and implementation requirements |
| `controls.md` | Positive/negative and comparator controls |
| `df1-reconciliation.md` | DF-1 obligation analysis; no inferred closure |
| `seal-anomaly.md` | Prior-seal pre-seal-manifest packaging anomaly (R1-06) |
| `reentry-gates.md` | Findings register and R-G1…R-G12 matrix |
| `corpus/c3-linked`, `corpus/c4-contributors` | Added frozen fixtures |
| `tools/` | Manifest build and validation scripts |
| `seal.py`, `manifest.json` | Self-excluding re-entry seal |

## Reproduce the oracle

```text
dotnet build d3-oracle/D3Oracle.csproj -c Release
dotnet d3-oracle/bin/Release/net10.0/D3Oracle.dll <corpus-root> <out.json>
python tools/build-corpus-manifest.py <repo-root> corpus-manifest.json
python tools/validate-corpus.py <repo-root> validation.json
```

C1 root: `docs/planning/adr-ci1-01-ci1a-s2-0/corpus/repo`.
C2 root: `docs/reviews/CI1A-S2.5-WP-S25-4-QUAL-01-evidence/fixture`.
C3/C4 roots are under this evidence directory.

## Seal verification

```text
python docs/reviews/ANDIRIT-CI1A-S2.6-WP-S64-ENTRY-01-R1-evidence/seal.py verify HEAD
```

The re-entry seal covers the R1 report, this directory, the acceptance and
closure records and the proposed implementation commission. It does not modify
the producer or independent qualification seals.

## Limits

- C4 exact two-unit occurrence ledger is outstanding.
- D3 execution, raw receipts, failure/reopen sequences and hosted runs remain.
- H1 historical stores are copy-policy only in re-entry.
- macOS evidence is regression-only; not S6.5 certification.
- DF-1 and S6.4 remain open.
