# CI1A S2.0 — independent C# semantic corpus and frozen oracle

**Commission:** `ADR-CI1-01-CI1A-IMPL-01-S2.0-CORPUS-01`.
**Stage:** S2.0 / F12. **Sources:** ADR-CI1-01 Candidate C; R2 provenance and identity contracts; S2.1 owner acceptance; S2.2 Option A identity decision; pre-release compatibility policy; Stage 2 obligation register §5 (F12).
**Status:** Producer package complete and frozen for independent Q1. **S2.2 production implementation is NOT authorized.**

The purpose of S2.0 is to establish, independently of Andirit's future S2.2
occurrence extractor, **what the correct semantic occurrences should be** — the
source, the syntax, the Roslyn-resolved symbol, the defining compilation, the
accepted R2 identity, and the provenance-validity outcome. S2.2 must be judged
against this frozen oracle, not allowed to define its own expected behaviour.

## Documents

- [Semantic corpus specification](semantic-corpus-specification.md) — fixture model, projects, compilation inputs, variants, environment.
- [Case matrix](case-matrix.md) — every case, its category, scope classification and independently established expectation.
- [Oracle contract](oracle-contract.md) — how expected results are established independently of production.
- [Identity and provenance mapping](identity-and-provenance-mapping.md) — R2 `compctx1`/`asmctx3`/`usage4`, Option A `compctx2`, invalidation.
- [Qualification plan](qualification-plan.md) — R0–R6 sequence, evidence, gaps.
- [Q1 handoff](q1-handoff.md) — reviewer instructions and adversarial probes.

## Artifacts

- `corpus/repo/` — frozen C# fixture sources (a genuine multi-project compilation set).
- `corpus/variants/source-changed/` — the single declared source variant.
- `oracle/` — the standalone Roslyn inspector that re-implements the R2 tuple algebra and never references Andirit production code.
- `oracle/expected/oracle-expected.json` — the frozen oracle document.
- `../../evidence/adr-ci1-01-ci1a-s2-0/` — run output, regression TRX, environment, self-excluded SHA-256 manifest.

## Producer result

- Oracle: **114/114 checks passed, 0 failed** (`oracle-run.log`).
- Existing regression suite: **645/645 passed**, unchanged from the accepted S2.1 baseline. No production extraction code changed.
- F12 minima: 11 C# files, 53 invocations, 21 method references, 5 overloaded methods, 5 namespaces, 5 projects, 52 cross-project invocations.
- Determinism: two independent processes produce byte-identical run output.

## Disposition

**`CI1A-S2.0-CORPUS-01 READY FOR Q1 WITH QUALIFICATIONS`** — see
[qualification-plan.md §6](qualification-plan.md) for the bounded qualifications.
Producer completion does not constitute independent acceptance; F12 closes only
with independent qualification and an explicit owner disposition.
