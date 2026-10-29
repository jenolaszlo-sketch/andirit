# EC6D-R16 consumer decision

**Status: DECISION RECORDED — ACCEPT.**

R16 closes when an actual intended consumer records ACCEPT against the
evidence and the owner authorizes the documented disposition. The owner,
acting as the intended consumer, explicitly evaluated the workflow in this
session and recorded ACCEPT on 2026-10-30 (Asia/Manila). This record is the
consumer's own recorded evaluation; it was not inferred from tests,
planning acceptance or the agreement to run the experiment.

## Scenario presented for review

- Selected interface: `Renderable` in the controlled corpus
  (`docs/planning/ec6d/corpus/ts-interface-impact/src/contracts/Renderable.ts`),
  with the real-repository complement from the pinned website
  (`f9cca177…`) recorded in [website-cases.md](website-cases.md).
- Evidence packet reference: [README.md](README.md) index — in particular
  [controlled-fixture.md](controlled-fixture.md) (current output vs
  expected EC-6D result) and
  [consumer-walkthrough.md](consumer-walkthrough.md) (readable walkthrough,
  presented in the interactive review session on 2026-10-30);
  [website-baseline.md](website-baseline.md) and
  [website-cases.md](website-cases.md) supply the real-source complement.
- The presentation separated current behavior from proposed behavior,
  showed the refusal list, and included the honest zero-consumer website
  boundary case (W2) and the no-real-implements finding.

## Recorded evaluation

| Field | Value |
| --- | --- |
| Reviewer / consumer identity or role | Repository owner (`jenolaszlo-sketch`), acting as the intended consumer (developer / coding-agent operator), explicit interactive evaluation |
| Date | 2026-10-30 (Asia/Manila) |
| Exact scenario reviewed | Change the `Renderable` interface: which classes directly implement it, which modules might depend on it, and what evidence supports each result — evaluated against the controlled-corpus walkthrough and the pinned-website complement |
| Evidence packet reference | This directory; walkthrough presented in-session; controlled fixture illustrated in [controlled-fixture.md](controlled-fixture.md) §3 |
| Q1 Utility | **Yes — material help.** The result would materially help understand or modify code; strictly more useful than today's name-match behavior (4/4 intended implementers vs. 0 today on the controlled corpus). |
| Q2 Trust | **Mostly, with reservations.** The confirmed/possible/unknown distinction and witnesses are clear enough to avoid misleading conclusions, but trust depends on always reading the coverage block and the not-evaluated frontier; boundary cases (e.g., the website W2 zero-consumer interface with real same-file and `.astro` uses) need care. Recorded as a qualification below, not a blocking issue. |
| Q3 Actionability | **Yes — actionable.** Physical source locations and ordered witness evidence are sufficient to inspect or act on a confirmed result without manually reconstructing the import chain. |
| Q4 Scope | **Yes — acceptable.** The deliberate unsupported cases (no re-export closure, transitive inheritance, ordinary type-use, calls, `.astro`/other languages, non-interface targets; visible unknown/unsupported outcomes) are acceptable for an initial bounded release. |
| Required changes before implementation | None recorded as blocking. |
| Explicit decision | **ACCEPT** |
| Qualifications | (1) Q2 reservation: coverage and the not-evaluated frontier must remain prominent in every consumer surface — this is already pinned by the accepted coverage contract and the A-series tests, and must not be weakened during implementation. (2) Accept derives from the controlled fixture's value plus real-source honesty; the pinned website contributes no positive interface-impact case and cannot on its own evidence the implementer path. (3) Acceptance closes the R16 consumer-value risk only; it does not authorize EC6D-IMPL-01, which requires separate authorization. |

## Supporting analysis (not a substitute for the decision)

The machine-generated assessments in this directory (fixture reproduction,
website indexing, TypeScript oracle comparison) support the decision but do
not constitute it. The decision above is the consumer's explicit record.

## Closure rule

R16 is closed by this ACCEPT per the milestone's closure rule: consumer
ACCEPT recorded against the evidence, with the documented disposition
authorized by the owner in this same evaluation. A material semantic change
request must be routed through the existing owner decision process; the
accepted EC-6D contracts must not be silently amended. This decision
closes the consumer-value risk only — it does not replace implementation
correctness, durability, independent qualification or release gates.
