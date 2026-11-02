# Independent Q2 handoff

**Commission:** `ADR-CI1-01-CI1A-IMPL-01-S2.0-CORPUS-01-Q2`
**Source:** use the final commit and tree named in the R1 post-commit handoff.
**Oracle:** `CI1A-S2.0-CORPUS-R1`.

## Reproduction

From a clean archive extraction, first verify the R1 package using the manifest SHA-256 supplied in the final source binding:

```powershell
python -X utf8 docs/evidence/adr-ci1-01-ci1a-s2-0-r1/verify-manifest.py --trusted-manifest-sha256 <PINNED-R1-MANIFEST-SHA256>
dotnet build docs/planning/adr-ci1-01-ci1a-s2-0/oracle/SemanticCorpusOracle.csproj
dotnet run --no-build --project docs/planning/adr-ci1-01-ci1a-s2-0/oracle/SemanticCorpusOracle.csproj -- --corpus docs/planning/adr-ci1-01-ci1a-s2-0/corpus --expected docs/planning/adr-ci1-01-ci1a-s2-0/oracle/expected/oracle-expected-r1.json --output <TEMP-OUTPUT-1>
```

Repeat the last command in a second process and compare JSON bytes. In a Git checkout, the verifier additionally checks `main`, the pinned starting tree, and ancestry. In archive mode it explicitly reports that live Git branch/HEAD/ancestry checks were unavailable.

## Mandatory probes

1. DEL01: verify Snapshot A/B authoritative source manifests, completeness, source-owned facts, and compctx identities.
2. DEL-N1: authoritative deletion remains complete.
3. DEL-N2: still-declared but unavailable source is incomplete.
4. DEL-N3: stale deleted-source contribution fails projection comparison.
5. OPT01: source hashes and parse options are identical; actual `CheckOverflow` and accepted options digest differ.
6. OPT01-target: defining identity follows R2; do not demand stability for a defining compilation option change.
7. OPT01-restore: baseline options reproduce baseline contexts.
8. E02: Roslyn symbol remains resolved while missing verified defining context/universe refuses confirmation; add authority as positive control. Contrast E01 metadata-only.
9. CLS01/N01: actual CS0121 ambiguity. CLS02: actual CS0176 invalid invocation is not ambiguous.
10. S15: selected symbol kind is Event (`E:`); handler remains a Method (`M:`).
11. Clean archive, one-byte tamper, missing frozen file, and modified expected value.
12. Historical oracle against commit `82f101c...`; verify its expected file remains byte-identical.
13. Recompute frozen R2 vectors and Option A outcomes independently; do not alter them.
14. Review model-level coverage labels; NEW-04 remains open.

## Required Q2 disposition

Report source/commit/tree, manifest trust anchor, all probe results, independent semantic and identity recomputation, changed-file inventory, and any discrepancy. Q2 and owner acceptance remain outstanding; this handoff does not close F12 or authorize S2.2 production extraction.
