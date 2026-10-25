# EC6-GLM-AR1 — Decision reconciliation for owner acceptance

**Status:** concise decision table for owner acceptance, distilled from
[ec6-glm-ar1-architecture-review.md](ec6-glm-ar1-architecture-review.md).
**Baseline:** design tip `aa4031c` on `design/ec6a-cross-file-resolution`.

Note on identifiers: the review brief's §15 labels for D-07 ("resolution-unit
ownership") and D-14 ("publication assumptions") do not match the design
register (D-12/D-13 cover ownership/invalidation; D-07 is binding rules; D-14
is coverage counting). This table assesses the actual register entries and
covers the brief's intent under both IDs.

| Decision | Design proposal | GLM assessment | Correction required | Stage | Owner acceptance | Blocks EC-6B |
| --- | --- | --- | --- | --- | --- | --- |
| D-01 resolver placement | Plugin-embedded pass after per-file units; internal seam | **Accepted.** Ordering/visibility verified; violations fail loudly (probes P1/P3); core phase correctly deferred | State ordering as a normative contract (F02) | EC-6B docs | No (architecture-level) | No |
| D-02 module identity = file node | No new `module` kind | **Accepted.** File nodes are stable, path-addressed, queryable; minimal vocabulary | — | — | No | No |
| D-03 evidence quality | `Resolved` for resolver edges | **Accepted for `resolves-to`** with a mandated policy-specific contract sentence; `exports`/`binds` classification deferred to their gates under preconditions (F12); name-match-only binding forbidden in either quality | Insert the contract sentence into consumer docs + identity doc | EC-6B (sentence), EC-6C/D (gates) | **Yes** (vocabulary meaning) | **Yes — must be accepted before EC-6B emits evidence** |
| D-04 kinds `resolves-to`/`exports`/`binds` | Additive edge kinds; `imports` untouched | **Accepted.** Additive; no preset/API/schema impact (F08); property-based edge model sufficient (probe P4) | Normative per-kind discriminator statement (F09) | EC-6B (statement), EC-6C/D (fixtures) | **Yes** (public vocabulary) | Kind set for EC-6B (`resolves-to` only) must be accepted |
| D-05 name-based export targets; dual-projection ambiguity | Accept with documented caveat | **Conditionally accepted.** Verified real (probe P5); unattributable by node identity alone | Require `exported-from` attribution property or eligibility restriction at EC-6D (F01) | EC-6C/D | **Yes** | No |
| D-06 export of imported bindings | Deferred | **Accepted** as deferred | — | post-6F | No | No |
| D-07 binding rules + type-only semantics | Unique table lookup; namespace → module node; value-import of type-only export refused | **Accepted** in principle; final contract text due at EC-6D gate | Star type-only propagation (F07) | EC-6D | No (design review gate) | No |
| D-12/D-13 resolution-unit ownership + invalidation | Stable `{language}:modules` unit; plugin-execution-driven full rebuild | **Accepted.** Replacement/empty-clear/no-op verified (probes P2, P6); planner invalidation verified | Lifecycle fixtures in acceptance (F02) | EC-6B/6E | No | No |
| D-14 coverage counting | Map onto existing coverage fields; sum UnresolvedRelationships | **Accepted** with the §12.2 mapping table as normative; escalate to additive fields only if the table cannot carry the semantics honestly | — | EC-6B | No | No |
| D-15 query/impact semantics | No API or preset changes; module facts stay out of impact | **Accepted and endorsed.** Presets are closed lists (verified); impact inclusion would change existing consumer outcomes | Document traversal patterns + absence semantics in EC-6B docs | EC-6B docs | **Yes** if impact inclusion is ever proposed | No |
| D-16 version/invalidation identity | Single emitted-fact revision; resolver provider version in lockstep | **Accepted.** Version-driven invalidation verified (`CodeIndexPlanner.cs:204-208`) | — | EC-6B | No | No |
| D-17 bounds and degradation | Guidance limit + deterministic degradation + loud hard caps | **Accepted in principle**; numbers and preference order are owner parameters | Fixture in EC-6E; exact limits to owner (O-07) | EC-6E | **Yes** (numbers) | No |
| D-18 cross-plugin resolution deferred | Per-language scope; measured recall cost | **Accepted.** Concurrent execution offers no safe cross-plugin ordering today | Measure in EC-6F | EC-6F | **Yes** (scope limitation acknowledgment) | No |
| D-19 property vocabulary | 12 new bounded keys | **Accepted** subject to owner approval of the key set; values bounded; documented | — | EC-6B–D | **Yes** (vocabulary) | Key set for EC-6B (`specifier`, `module-reference`, `resolution-policy`, `resolution-rule`, `type-only`) must be accepted |
| O-01 Resolved vs Derived | Open | **Closed by this review:** `Resolved` for `resolves-to` with mandated wording (D-03 above); `Derived` is a workable fallback with no structural change | — | EC-6B | **Yes** | **Yes** (same as D-03) |
| O-02 name-based export identity | Open | **Closed for EC-6B** (irrelevant — no exports in B); EC-6D must adopt F01's correction options | F01 | EC-6C/D | **Yes** | No |
| O-04 star-edge representation | Open | **Deferred to EC-6D design** (F07) with conservative-refusal fallback acceptable | F07 | EC-6D | No (review gate) | No |
| R-02 identity convergence risk | Accepted caveat | **Confirmed real** (probe P5); mitigation mandatory at EC-6D (F01) | F01 | EC-6C/D | **Yes** | No |
| R-03 per-plugin recall | Accepted limitation | **Accepted;** measure in EC-6F | — | EC-6F | No | No |
| R-05 performance cost | Correctness-first full rebuild | **Accepted** with bounds analysis (review §I); degradation fixture in EC-6E; no invented benchmarks | D-17 fixture | EC-6E/6F | No | No |
| R-07 test-rescoping integrity | Blanket assertions rescoped with intent preserved | **Endorsed** as an acceptance requirement per emitting milestone; reviewers audit the diff | — | EC-6B–D | No | No |

## Owner acceptance checklist (minimum to start EC-6B)

1. **D-03/O-01:** accept `Resolved` for `resolves-to` with the mandated
   contract sentence (or select `Derived`; no structural change either way).
2. **D-04/D-19 (EC-6B subset):** accept the `resolves-to` kind and its five
   property keys as public vocabulary proposals for implementation.
3. **Corrections to fold into the EC-6B contract text and acceptance
   criteria:** F02 (staging-order contract + lifecycle fixtures), F04
   (normalization/classification table), F05 (cancellation requirement), F09
   (identity/discriminator statement for `resolves-to`).
4. Acknowledge D-18 (per-language scope) and D-17 parameters (to be fixed at
   EC-6E).

No P0/P1 findings; EC-6B is implementable within the verified runtime
guarantees once items 1–3 are accepted. Implementation authorization remains
a separate owner decision.
