# Prerequisite production defects

Two existing C# defects in the production baseline
`cd2b7fb5d73f4dc462b0ef7fcadfd75a91c118a2` are confirmed by R1 and by the Q1
reviewer. Both block correct new confirmations and/or real-repository
qualification for CI-1A. Neither is authorized for repair by this packet.

**No production code, test, schema or package change is made or authorized
here.** Each proposal below requires an explicit owner authorization and its own
independent qualification gate, separate from CI-1 implementation.

## Impact on CI-1A

| Defect | Blocked CI-1A activity | Can CI-1A proceed without it? |
| --- | --- | --- |
| P-DEF-01 target identity (F01) | Any new confirmation that trusts existing canonical-name resolution, including the real-repository consumer scenarios | **Only with a strict-definition resolver gate** that refuses collisions; a strict gate can confirm C03/C19 and the controlled positives while refusing the collision. Real-repository claims stay qualified. |
| P-DEF-02 documented partial extraction (F02) | Wider self-indexing, any documented partial type, and any claim over `Andirit.CSharp` sources | **Only after narrowing qualification.** The affected input fails before relationship extraction. Plain C12 does not cover it. |

If the owner does not authorize P-DEF-02, CI-1A must explicitly narrow the
supported qualification to exclude documented partial types and must retain
`unavailable`/failure state for that input (never a complete-empty result).

## P-DEF-01 — exact definition/assembly identity gate for confirmed targets

**Scope.** Prevent a conventional, non-error, uniquely-keyed symbol from being
confirmed as a local target when its containing assembly/compilation and
definition do not correspond to the indexed declaration.

**Boundary options (owner chooses; both preserve legacy aggregate behaviour).**

- **P-DEF-01a — new-query resolver only.** CI-1A's strict occurrence resolver
  proves correspondence (defining compilation/assembly + definition identity)
  before confirmation; the existing extractor and all legacy aggregate edges/IDs
  are untouched. Recommended first: smallest correct boundary, no legacy
  cardinality change.
- **P-DEF-01b — bounded provider fix.** Harden `CanonicalSymbolKey`/
  `RunSymbols`/`TryResolveTarget` so the run-wide map is assembly/compilation
  aware. Higher risk: touches shared extraction and must prove unchanged legacy
  IDs, cardinalities, budgets and frontiers.

**Smallest appropriate correction boundary.** P-DEF-01a. It confines the change
to the new operation and never copies current global-key resolution into
positive facts.

**Out of scope.** No rewrite of `calls`/`references` IDs; no change to
`GetImpactSet`/`GetAffectedTests`; no external metadata indexing.

**Required tests (permanent regression).**

1. Local declaration and external method sharing a documentation ID: the
   external call is refused/qualified external and never confirmed to the local
   declaration (the two-project collision).
2. Same canonical documentation ID in two projects, one a genuine dependency of
   the caller: only the dependency-context definition confirms; the unrelated
   project does not.
3. Linked file in two project contexts with potentially different targets:
   distinct identities; no cross-context merge.
4. Controlled positives (C03 cross-project, C19 duplicate paths, fixture
   `Logger.Write`) still confirm with unchanged legacy aggregate IDs and
   cardinalities.
5. A same-name local declaration that is *not* the bound target does not confirm.

**Acceptance and authorization gate.** The owner authorizes P-DEF-01a as a
bounded milestone; an independent reviewer verifies (a) the collision is
refused, (b) legacy regression identities are byte-identical, (c) no new
false negative for the controlled positives. P-DEF-01a is a prerequisite for
unqualified real-repository confirmation claims.

## P-DEF-02 — documented partial-type extraction

**Scope.** Make a documented/sealed partial type extractable through the existing
pipeline without changing unrelated fact semantics, and make the inconsistency
diagnostic safe.

**Two layers (both required; fixing one is not sufficient).**

1. **Diagnostic formatting correctness.** `GraphBuilder.cs:317–318` casts
   `modifiers` to `CodeTextListProperty`, but `modifiers` is a
   `CodeTextProperty` (`PropertyFactory.cs:59`). Correct the diagnostic to
   format the actual property type (or a type-neutral rendering).
2. **Partial-part property agreement/merge policy.** `NodesEquivalent`
   (`GraphBuilder.cs:366`) requires declaration-specific `doc-summary` to agree
   across partial parts. Define and implement a deterministic policy: merge
   declaration-specific documentation, or exclude declaration-scoped properties
   from the agreement set while still detecting genuine conflicts. Correcting
   layer 1 alone exposes the `InvalidOperationException` throw at
   `GraphBuilder.cs:314`.

**Classification of the three candidate root causes.**

- Unsafe diagnostic property cast: **confirmed defect**.
- Inconsistent symbol properties activating the diagnostic: **confirmed defect**
  (declaration-specific documentation must not make node data inconsistent).
- Genuine partial-type identity/extraction defect: **not established** by R1
  (one logical node identity is produced; the failure is property agreement plus
  diagnostic formatting). Must be re-checked after layers 1–2.

**Required tests (permanent regression).**

1. Minimal documented/sealed two-part type extracts to one logical node with a
   deterministic merge and no exception.
2. The real `T:Andirit.CSharpCodeGraphPlugin` two-part symbol extracts.
3. A deliberately genuine inconsistency still raises the intended
   `InvalidOperationException` with a correctly formatted, type-safe diagnostic.
4. Partial-method definition/implementation unification remains a separately
   deferred, explicitly identified case (not silently claimed as covered).
5. Existing 47 C# tests and the wider three-project extraction succeed under the
   authorized fix (or the specific remaining failures are documented).

**Acceptance and authorization gate.** Owner authorizes P-DEF-02 as a bounded
milestone; an independent reviewer verifies the minimal probe, the real symbol,
the genuine-conflict diagnostic and the regression suite. Until then, CI-1A
qualification is narrowed and this input retains unavailable/failure state.

## Explicit non-authorization

- P-DEF-01 and P-DEF-02 are **proposals only**. This packet does not authorize
  them and does not schedule them.
- No CI-1 implementation may silently include either fix.
- Real-repository qualification may not advertise an unqualified success for
  the documented-partial or the canonical-collision cases until the relevant
  proposal passes its own gate.
