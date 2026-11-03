# Independent Q3 handoff

**Commission:** `ADR-CI1-01-CI1A-IMPL-01-S2.0-CORPUS-01-R3` (independent review of the R2 remediation).
**Source:** use the final commit and tree named in the R2 post-commit handoff.
**Oracle:** `CI1A-S2.0-CORPUS-R2` (frozen expected `oracle-expected-r2.json`, 185 runtime checks).

## Scope of what R2 claims

R2 closes exactly two findings and nothing more: Q2-F01 (single canonical
target-description pathway across oracle views) and Q2-F02 (defining-provenance
eligibility gate). It does not change R2 identity formulas, the accepted R2
vectors, Option A semantics, production code, or the S2.2 boundary. Residual
qualifications listed in the Q2 review (runtime-bound references,
fixture-declared canonical component forms, model-only coverage/publication,
Q-F2, Q-F3, A-01, A-02, NEW-01, NEW-04, S-01, D3/D4, Linux, crash/power-loss,
multi-process) remain open.

## Reproduction

From a clean archive extraction, first verify the package using the manifest
SHA-256 supplied in the final R2 source binding:

```powershell
python -X utf8 docs/evidence/adr-ci1-01-ci1a-s2-0-r2/verify-manifest.py --trusted-manifest-sha256 <PINNED-R2-MANIFEST-SHA256>
dotnet build docs/planning/adr-ci1-01-ci1a-s2-0/oracle/SemanticCorpusOracle.csproj
dotnet run --no-build --project docs/planning/adr-ci1-01-ci1a-s2-0/oracle/SemanticCorpusOracle.csproj -- --corpus docs/planning/adr-ci1-01-ci1a-s2-0/corpus --expected docs/planning/adr-ci1-01-ci1a-s2-0/oracle/expected/oracle-expected-r2.json --output <TEMP-OUTPUT-1>
```

Repeat the last command in a second process and compare JSON bytes. Expected:
**185/185 checks passed**, exit 0, both processes. On this producer host the
as-produced (CRLF) output SHA-256 was
`97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9`; compare
content after LF-normalizing if your host writes CRLF, and treat the frozen
expected file as the qualification anchor rather than the output hash.

In a Git checkout the verifier additionally checks `main`, the pinned starting
tree, and ancestry. In archive mode it explicitly reports that live Git
branch/HEAD/ancestry checks were unavailable, and requires the independently
supplied manifest hash as trust anchor.

## Mutation re-execution

Re-apply any `mutations/<case>/mutation.diff` to a fresh copy of the oracle,
rebuild, and run against the frozen expected. Expected outcome for every case:
exit 1 with the failing check IDs listed in `PROVENANCE-MUTATION-RESULTS.md`.
Minimum set for Q3: `q2-f01-lifecycle-regression` (Q2-F01 verbatim) and
`q2-f02-presence-only-gate` (Q2-F02 verbatim); both must fail.

## Historical reproduction

Extract `82f101c24e21828a41a30d4342ba73db9376e24e` and
`9fc2115622c93fa54f5f7882b117185a017703bb` with `git archive` into isolated
directories and run each pin's own oracle against its own expected file.
Expected: 114/114 and 130/130, exit 0. The historical expected files in the
working tree must remain byte-identical to the archive copies
(`HISTORICAL-PRESERVATION.md`).

## Independence rules (unchanged from R1/Q2)

- Do not call producer identity or occurrence helpers from review probes; the
  oracle project references only Roslyn.
- Compare against frozen bytes; a self-computed expectation is not authority.
- The oracle's `crossView`/`e02`/`variants`/`lifecycle`/`identities` sections
  are compared frozen sections; `checks`/`summary`/`environment`/
  `fixtureManifest`/`corpusRoot` are not.

## Commission-text qualification

The commission text for this package is truncated mid-sentence at the end of
its `HISTORICAL-PRESERVATION.md` description ("Original and R1 frozen versions,
byte"). The remaining requirement text was not available to the producer; the
implemented preservation scope (byte-identical retention of both historical
expected files plus archive-based reproduction) follows the R1 convention and
the visible part of the sentence. Record any divergence between this scope and
the full commission text in the Q3 findings.
