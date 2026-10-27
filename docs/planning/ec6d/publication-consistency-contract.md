# EC-6D publication-consistency contract (R2 revision)

**Status: remediated planning contract for owner review (EC6D-PLAN-R2).**
Supersedes the R1 proposal at commit `0e1b213`. Addresses Q2 report Q2-05
and the publication portions of Q2-04. It authorizes no implementation.

**R2 changes (summary):** full publication identity in every result;
consistency truth table including the `true`/null contradiction; explicit
integrity failures distinct from ordinary unknowns; qualification preserved
through official display/serialization/export with a checked
consistent-only authority path; failed re-index retention semantics; A17
and A18 expanded.

## 1. What the flag means and what it does not

- `IsConsistentSnapshot` is a **provider-declared property**: whether
  enumeration and reads are guaranteed to see one immutable source
  snapshot. The only shipped provider (`FileSystemCodeRepositorySource`)
  declares `false` with a null `SnapshotIdentity`; the provider interface
  documents the identity as available "when available" and does not forbid
  a `true`/null combination — the truth table below therefore defines the
  behavior rather than assuming provider correctness.
- Within one run, each file's facts reflect that file's content at its own
  read time; per-file hashes are re-verified at materialization. A
  publication can mix per-file states read at different times.
- **Six distinct guarantees are never substituted for one another:** Git
  blob identity, working-tree bytes, extracted graph identity, index
  identity, canonical-facts digest, and publication consistency. Determinism
  (repeat-index digests) and durable reopen survival are not snapshot
  consistency evidence, and semantic graph equivalence is never presented
  as byte-identical indexing.

## 2. Policy decision

The two permitted designs were evaluated: (1) fail closed — refuse
confirmed results from inconsistent publications; (2) distinctly qualified
results with structurally mandatory qualification and fail-closed
boundaries.

**Recommendation: design 2**, as in R1, for the same reasons: the shipped
provider is always inconsistent, so design 1 would reject every publication
the repository can produce; the binding facts are single-file and
hash-verified; the snapshot risk concentrates in cross-file joins, which
are pinned to one publication and one run.

**Alternative recorded:** a consistent-only contract (design 1) is the
fallback if owner review prefers maximum conservatism; its tradeoff is that
EC-6D becomes unusable and untestable end-to-end with the only shipped
provider until a snapshot-consistent provider exists. The publication
policy decision proposal carries the explicit owner choice.

## 3. How qualification is represented

1. Every result carries
   `publication { repositoryId, indexRunId, indexIdentity,
   snapshotIdentity, isConsistentSnapshot, qualification }`. Repository and
   run identifiers are included because publication-bound queries compare
   full publication identity (`CodeGraphPublication` and
   `EnsurePublication`); a partial identity is not sufficient for joins.
2. **Derivation rule (mechanical, no discretion):**

   | `isConsistentSnapshot` | `snapshotIdentity` | `qualification` |
   | --- | --- | --- |
   | `false` | any (null or present) | `qualified` |
   | `true` | non-null, non-blank | `consistent` |
   | `true` | null or blank | `qualified` — an inconsistent provider declaration, treated as qualified; never silently promoted to snapshot authority |

3. Qualification is a first-class, discriminated field of the primary
   result type: a consumer that destructures only the result body still
   receives it, the coverage statement embeds it, and official display,
   serialization and export paths preserve it.
4. `"qualified"` carries exactly this meaning: "all participating facts
   were produced in this index run from per-file hash-verified reads; the
   run's source provider does not guarantee a single snapshot; results may
   mix per-file states read at different times and may not reflect sources
   edited after the run."
5. Confirmed implementations are asserted only relative to the publication:
   every witness step is a stored fact of that publication; the store
   retains only the latest successful publication; a test must pin that no
   result references facts from superseded units. Verified witness steps
   are immutable derivations; recomputation yields the identical witness
   identity.
6. Relationship-level provenance remains fully traceable regardless of the
   qualification value.

## 4. When the system must fail closed

Independent of qualification, the query fails closed (throws or returns an
error result, never classified output) when:

- the publication moved between binding the view and answering
  (`CodeGraphPublicationChangedException`, existing fail-on-change
  semantics — including a same-content/new-run replacement);
- the input is invalid: unknown/non-interface node, unsupported publication
  (missing EC-6D provider set), invalid or contradictory bounds;
- **integrity failures** (distinct from ordinary unknowns): contradictory
  endpoints in a cited fact, mismatched unit versions, superseded units
  referenced by a candidate chain, a missing fact inside an asserted
  witness, duplicate identity with different facts, or any other internal
  fact inconsistency;
- the caller cancels (no partial classified results);
- a query or store failure occurs mid-execution (no partial output).

Ordinary missing attribution (no declaration, no entry, barrier,
unresolved module) is an **unknown**, never an integrity error.

**Index-side failures:** a failed or cancelled re-index publishes no
partial facts and preserves the last successful publication unchanged;
later live source edits need not move or upgrade it. A bound query keeps
answering from that publication and exposes its identity and qualification
— never a freshness claim.

## 5. Consumer handling obligations (contract text)

- A `"qualified"` result may be used for navigation, triage and
  explanation, and must be surfaced with its qualification.
- A `"qualified"` result must not be recorded, exported or acted upon as an
  authoritative consistent-snapshot impact statement; re-indexing a fresh,
  ideally revision-pinned snapshot is the prescribed way to obtain one.
- Only `"consistent"` results may be used where snapshot authority is
  required (for example, gating an automated change).
- **Checked consumption:** the contract defines a checked consistent-only
  authority path (a consistent-only accessor or equivalent discriminated
  guard) that refuses `"qualified"` results for authority use; navigation
  opt-in is explicit. Tests assert (a) serialization/deserialization
  preserves qualification, (b) the authority path refuses qualified
  results, and (c) navigation surfaces label qualified results. A required
  sibling field cannot mechanically prevent a caller from copying a bare
  confirmed list; the wrapper/discriminated shape plus the authority path
  and tested boundaries are the reasonable safeguard, and arbitrary
  downstream misuse cannot be made impossible.
- Consistent output still carries language and coverage caveats; snapshot
  authority never implies whole-program impact completeness.

## 6. Acceptance linkage

- A17 (expanded): inconsistent `false`/null → `qualified`; `true` with
  identified snapshot → `consistent`; `true`/null → `qualified` (policy
  case, never `consistent`); structural integrity errors fail closed;
  serialization preserves qualification; the checked authority path
  refuses qualified results.
- A18 and A18b (expanded): mid-query cancellation; publication movement
  between joins including same-content/new-run replacement; failed indexing
  retains the last successful publication and bound queries still answer
  with unchanged identity/qualification; query/store failures return no
  partial output.
- No digest-stability or reopen claim may be cited as snapshot-consistency
  evidence (this boundary is itself an acceptance assertion).
