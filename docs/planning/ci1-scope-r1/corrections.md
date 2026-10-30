# Corrections

This document maps each finding to the planning correction applied. Corrections
are confined to (a) this R1 packet and (b) clearly-marked `R1 amendment` blocks
appended to the active CI-1 proposal under `docs/planning/ci1-scope-01/`.
Original raw observations and Q1 evidence are preserved; nothing is rewritten.

## C1 — Exact eligible target identity (F01)

**Replaces** the assumption that a unique canonical documentation ID proves the
indexed target.

The CI-1A selection and confirmation rule is strengthened to:

> A confirmed occurrence requires an **exact eligible target identity**. The
> bound symbol's defining compilation/assembly **and** its definition must be
> proven to correspond to the indexed declaration in the applicable dependency
> context. A canonical documentation ID is a lookup aid only and is never proof
> of correspondence. Where correspondence cannot be proven, the site is
> conservatively refused (unresolved/ambiguous) or qualified as external; it is
> never promoted to a confirmed local usage. Existing aggregate edge/ID
> semantics are unchanged.

Applied to:

- `ci1-scope-01/scope-proposal.md` — inclusion gate.
- `ci1-scope-01/query-contract-proposal.md` — request validation / confirmed
  selection.
- `ci1-scope-01/architecture-decisions.md` — ADR-CI1-04.
- `ci1-scope-01/acceptance-contract.md` — C10 collision variant.
- `ci1-scope-01/occurrence-representation-analysis.md` — candidate fact identity.

The exact collision (one indexed local declaration versus a same-documentation-ID
external method) becomes mandatory acceptance, alongside duplicate keys across
projects and linked contexts.

## C2 — Documented/sealed partial-type trigger (F02)

**Names** the previously unidentified wider-source trigger and separates three
distinguishable things:

1. the unsafe diagnostic property cast;
2. the inconsistent symbol properties (declaration-specific `doc-summary`
   required to agree as node data) that activate the diagnostic;
3. whether any genuine partial-type identity/extraction defect exists beyond (1)
   and (2).

R1's reproduction establishes (1) and (2) exist; (3) is not established as an
identity defect here — the node identity is one logical symbol, and the failure
is property-agreement plus diagnostic formatting. Correcting the cast alone
exposes the `InvalidOperationException` and does not resolve the merge conflict.

Applied to:

- `ci1-scope-01/acceptance-contract.md` — C12 extended with a documented/sealed
  partial type and an explicit statement that plain C12 does not cover F02 or
  partial-method unification.
- `ci1-scope-01/architecture-decisions.md` — ADR-CI1-05 records F02 as an
  explicit prerequisite-or-narrowing decision.

CI-1A does **not** include either fix. The owner selects a separately authorized
prerequisite proposal (`prerequisite-defects.md` P-DEF-02) or an explicit
narrowing that requires honest unavailable/failure retention.

## C3 — Exact source-byte provenance (F03)

**Adds** a raw-byte policy to the source/evidence binding requirement:

- Record, for every measured input: path, encoding, line-ending policy, raw
  SHA-256 of the exact bytes read, Git blob identity, and the checkout/filter
  policy (`core.autocrlf`, `.gitattributes` scope).
- Check both raw-byte and Git identity; a clean `git hash-object` match does not
  prove byte identity.
- Never normalize or rewrite fixture bytes and then claim equivalence to the
  original source.

This matters directly to proposed source-hash occurrence IDs, serialized source
qualifications and repeatable byte accounting. Applied to
`ci1-scope-01/performance-evaluation-plan.md`, `occurrence-representation-analysis.md`
and `final-handoff.md`.

## C4 — Manifest oracle strength (F04)

**Keeps** the independently reviewed manifest and its target/owner evidence as
an oracle input, and requires future acceptance to validate:

- full UTF-16 start **and end** coordinates;
- intended semantic target (overload/arity/ref-kind);
- intended outcome;
- row uniqueness.

Lexical manifest validation is distinguished from candidate-result comparison.
The packet's own label ("author planning consistency checks; NOT independent
qualification") is retained. No absent candidate implementation has passed any
acceptance test. Applied to `ci1-scope-01/acceptance-contract.md` "Independent
expected outcomes".

## C5 — ADR-CI1-01 reopened (S01)

**Removes** the recommendation preference for separate facts. Both Option C
(separate source-owned occurrence facts) and Option B2 (new versioned occurrence
edge kind alongside aggregates) are retained as qualified alternatives with no
default winner. The minimum comparative experiment required for an owner
decision is specified in `storage-alternatives.md`. The storage choice is not
made irreversible during remediation.

Applied to `ci1-scope-01/architecture-decisions.md` ADR-CI1-01,
`occurrence-representation-analysis.md` and `final-handoff.md`.

## C6 — Historical compatibility (S02)

**Adds** the requirement to name and hash real prior-writer databases/snapshots,
writer package/commit and consumer executable before schema work; to demonstrate
old-query preservation and `AnalysisUnavailable`/reindex-required for new
queries; to decide old-reader refusal explicitly; and never to fabricate
occurrences from old aggregate edges. Applied to ADR-CI1-05.

## C7 — Measurement separation (S03)

**Restates** the measurements as separate quantities:

| Quantity | Status |
| --- | --- |
| Existing extractor behaviour (occurrence collapse) | independently reproduced |
| Roslyn oracle behaviour (stronger context) | independently reproduced |
| Compilation-context difference | recorded, not a same-context score |
| Successful 47-file measurements | single observations |
| Failed wider extraction | prerequisite defect, not performance evidence |
| Query and source-retrieval costs | accounting sums, not model context |
| Candidate overhead, tokens, total task cost, break-even | **unmeasured** |

No speedup, savings, token reduction, cost reduction or break-even is claimed.
Applied to `ci1-scope-01/performance-evaluation-plan.md`.

## C8 — Coverage and completeness (S04)

**Clarifies** that the new operation persists disjoint semantic outcomes
(confirmed, unresolved, ambiguous, external, unsupported owner/category,
candidate-only, intrinsic/unsupported invocation) as publication-owned
coverage; that existing calls/`EdgesEmitted` counters cannot be the new
denominator; and that "complete within supported scope" is bounded by an
explicit unit/context universe. `SymbolUnused` and complete-usage claims remain
prohibited. Applied to `ci1-scope-01/query-contract-proposal.md` and
`scope-proposal.md`.

## C9 — Bounded CI-1A preserved

No correction expands scope. Still excluded: indirect impact, all references,
runtime dispatch resolution, full refactoring safety, complete MSBuild/NuGet
evaluation, automatic source editing, constructors/local-function targets as
selectable targets, and method groups. The query proposal still removes future
impact fields from *mandatory CI-1A metadata*: `FindUsagesV1` carries no
`changeKind` or impact path fields; those remain design-only for the future
`GetChangeImpactV1`.

## Changed expectations and why

| Where | Preserved | Changed | Reason |
| --- | --- | --- | --- |
| Inclusion gate | non-error signature, normalization | unique canonical ID is no longer sufficient; exact definition/assembly correspondence required | F01 |
| C10 | `Console.WriteLine` external | add same-documentation-ID local-collision variant that must not confirm locally | F01 |
| C12 | simple undocumented partial | add documented/sealed partial; state it does not cover F02 | F02 |
| ADR-CI1-01 | preserve legacy aggregates | remove separate-facts preference | S01 |
| ADR-CI1-04 | static binding, no runtime expansion | add strict-definition mapping gate | F01 |
| ADR-CI1-05 | project-unit atomic replacement | add F02 prerequisite and authentic historical fixture requirement | F02, S02 |
| Acceptance oracle | 32 authored rows | full end coordinate + semantic + uniqueness checks | F04 |
| Byte binding | 47 raw hashes reproducible | distinguish raw bytes from Git blob; record encoding/line endings | F03 |

Every changed expectation is explained; valid historical observations
(three-call collapse, eight-call Positive collapse, 47-file measurements, the
32 correct manifest rows) are preserved unchanged.
