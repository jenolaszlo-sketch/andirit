# EC6D-R16: Intended-consumer validation

**Status: EC6D-R16 COMPLETE — CONSUMER ACCEPTED.** The intended consumer
(the repository owner, explicitly evaluating in-session on 2026-10-30)
recorded ACCEPT against this evidence with one qualification (coverage and
the not-evaluated frontier must stay prominent); see
[consumer-decision.md](consumer-decision.md). This closes the R16
consumer-value risk. It does **not** authorize or start EC6D-IMPL-01, does
**not** implement `GetInterfaceImpactV1`, and does not change production
graph behavior.

## Purpose

Close the outstanding R16 consumer-value risk for EC-6D by establishing
whether the accepted bounded TypeScript interface-impact capability
(`GetInterfaceImpactV1`, EC6D-OWNER-01 accepted contracts) is useful to an
actual intended consumer:

> If I change a TypeScript interface, which classes directly implement it,
> which modules might depend on it, and what evidence supports each result?

R16 validates the **value of the proposed functionality**. It does not
prove that the unimplemented functionality already works.

## Scope

- Reuses the committed controlled corpus
  `docs/planning/ec6d/corpus/ts-interface-impact` (selected declaration:
  `src/contracts/Renderable.ts` / `Renderable`).
- Reuses the real `jenolaszlo-sketch/website` repository at the pinned
  qualification commit `f9cca1773a32bb517e624cc380e1f6d14e0cc317`
  (byte-verified `git archive` snapshot; the original checkout was not
  modified).
- Separates **current** Andirit behavior from the **proposed** EC-6D result
  everywhere; proposed answers are labeled illustrations, never executed
  query output.
- Produces a consumer walkthrough and a decision record. An automated agent
  cannot supply intended-consumer approval; the owner (or another
  explicitly identified authorized consumer) must evaluate the walkthrough.

## Evidence index

| Document | Content |
| --- | --- |
| [baseline.md](baseline.md) | Andirit and website Git bindings, environment, verification |
| [controlled-fixture.md](controlled-fixture.md) | Current graph facts for the corpus, negative cases, expected EC-6D result illustration |
| [website-baseline.md](website-baseline.md) | Pinned website indexing results and comparison with prior evidence |
| [website-cases.md](website-cases.md) | Real-source consumer cases, expected outcomes, limitations |
| [consumer-walkthrough.md](consumer-walkthrough.md) | Current vs proposed capability, readable for the consumer |
| [consumer-decision.md](consumer-decision.md) | Consumer evaluation, decision and qualifications |
| [implementation-qualification-plan.md](implementation-qualification-plan.md) | Forward test plan for EC6D-IMPL-01 and independent qualification |
| [final-handoff.md](final-handoff.md) | Git bindings, evidence summary, outstanding work, next milestone |

Reproduction assets:

| Asset | Content |
| --- | --- |
| [harness/WebsiteBaseline](harness/WebsiteBaseline/Program.cs) | Read-only website indexing/query dump harness (public APIs only) |
| [tools/website-ts-oracle.cjs](tools/website-ts-oracle.cjs) | TypeScript 5.9.3 oracle for the supported website inventory |
| `docs/planning/ec6d/analysis/ConsumerBaseline.csproj` | Existing controlled-corpus harness, adapted only for the Andirit rename |

Raw run logs (fixture JSON, website JSON, oracle JSON, test logs) are
preserved outside the checkout in the session evidence store
(`…/Temp/opencode/ec6d-r16/`); the documents record every command needed to
regenerate them.

## Disposition

**EC6D-R16 COMPLETE: CONSUMER ACCEPTED.** Recorded by the repository owner
acting as the intended consumer on 2026-10-30 (Asia/Manila), against the
evidence packet in this directory, with the qualification that coverage and
the not-evaluated frontier remain prominent in every consumer surface.
Exact Git bindings are in [final-handoff.md](final-handoff.md).
