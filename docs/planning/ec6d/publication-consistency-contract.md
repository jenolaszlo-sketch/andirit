# EC-6D publication-consistency contract (R1 proposal)

**Status: remediated planning contract for owner review.** Addresses Q1
finding F-05: the shipped filesystem source provider declares
`IsConsistentSnapshot=false` and `SnapshotIdentity=null` for every
publication (verified on both the in-memory and LatticeDB stores in Q1
scenario H; `src/Penghou.Hetu/CodeRepositorySource.cs:254-255`). EC-6D must
define safe consumer semantics for such publications. This document
authorizes no implementation.

## 1. What the flag means and what it does not

- `IsConsistentSnapshot` is a **provider-declared property**: whether
  enumeration and reads are guaranteed to see one immutable source snapshot.
  The filesystem provider cannot guarantee it; the flag is structural, not a
  defect of a particular run.
- Within one run, each file's facts reflect that file's content **at its own
  read time**; files are read sequentially and each file's bytes are
  re-verified against the planned SHA-256 at materialization
  (`CodeSourceChangedDuringIndexingException` aborts on drift). A publication
  can therefore mix state A of file 1 (read at t₀) with state B of file 2
  (read at t₁) even though every hash verified at read time.
- **Repeat-index digest stability is not consistency evidence.** Q1 scenario
  H showed two fresh runs produce identical canonical digests and that the
  digest moves when a source changes: that demonstrates pipeline
  determinism and post-hoc drift detection, never intra-run snapshot
  isolation. Durable reopen preserves facts (67/67 in Q1) — including the
  inconsistency flag itself; it says nothing about source immutability.
  Source consistency and durable fact integrity are separate properties and
  are reported separately everywhere in EC-6D.

## 2. Policy decision

The two permitted designs were evaluated:

1. **Fail closed** — refuse to return confirmed results from an
   inconsistent publication.
2. **Distinctly qualified results** — return results that cannot be
   interpreted as authoritative consistent-snapshot answers.

**Recommendation: design 2 — qualified results with structurally mandatory
qualification — with fail-closed elements.** Reasoning:

- Fail-closed alone would reject **every** publication the repository can
  currently produce (the only shipped provider is always inconsistent),
  making the milestone unusable and untestable end-to-end; it would also
  contradict the accepted evidence model in which per-file facts are
  hash-verified at read time.
- The per-file facts the binding consumes (heritage clause, import
  occurrence, export entry, attribution) are **single-file and
  hash-verified at read**; their internal soundness does not depend on
  snapshot isolation. The snapshot risk concentrates in **cross-file joins**
  (module resolution, binding) and in staleness relative to the live tree.
- Therefore the safe, useful contract is: confirmed bindings are asserted
  **relative to the publication**, with the publication's consistency status
  exposed in a form a consumer cannot accidentally ignore, and hard
  fail-closed behavior at the boundaries where safety cannot be qualified
  (Section 4).

If a future provider publishes `IsConsistentSnapshot=true` with a non-null
`SnapshotIdentity`, the same result shape carries `qualification:
"consistent"` and no qualification prose is required. Nothing else changes.

## 3. How qualification is represented

1. Every result carries `publication { indexIdentity, snapshotIdentity,
   isConsistentSnapshot, qualification }` where `qualification` is
   `"consistent"` or `"qualified"`, derived mechanically:
   `qualification = isConsistentSnapshot ? "consistent" : "qualified"`.
2. The qualification is a **first-class field of the primary result type**,
   not an optional envelope header: a consumer that destructures only the
   result body still receives it, and the coverage statement (consumer-query
   contract §6) embeds it in the fixed profile text.
3. `"qualified"` carries exactly this meaning: "all participating facts were
   produced in this index run from per-file hash-verified reads; the run's
   source provider does not guarantee a single snapshot; results may mix
   per-file states read at different times and may not reflect sources
   edited after the run."
4. Confirmed implementations are asserted **only relative to the
   publication**: "bound within publication P (run R)". The binding rule
   consumes facts from exactly one run; the store retains only the latest
   publication, and a test must pin that no result references facts from
   superseded units.
5. Relationship-level provenance (per confirmed path) is independent of
   publication qualification and remains fully traceable regardless of the
   qualification value.

## 4. When the system must fail closed

Independent of the qualification value, the query fails closed (throws /
returns an error result, never classified output) when:

- the publication moved between binding the view and answering
  (`CodeGraphPublicationChangedException`, existing fail-on-change
  semantics);
- the queried declaration does not exist or is not a supported interface
  declaration node in the publication;
- the facts required to evaluate a candidate are internally inconsistent
  (duplicate identity with different facts — existing fail-closed store
  behavior);
- the caller cancels (no partial classified results).

The query never silently converts an unknown into a confirmed result to
compensate for missing evidence, and never downgrades a confirmed binding to
an unknown because of publication inconsistency — the two dimensions stay
separate.

## 5. Consumer handling obligations (contract text)

The response contract states, as normative text for consumers:

- A `"qualified"` result may be used for navigation, triage, and
  explanation, and must be surfaced to end users with its qualification.
- A `"qualified"` result must not be recorded, exported, or acted upon as an
  authoritative consistent-snapshot impact statement; re-indexing a fresh,
  ideally revision-pinned snapshot is the prescribed way to obtain one.
- Only `"consistent"` results may be used where snapshot authority is
  required (for example, gating an automated change).

## 6. Acceptance linkage

- A17: an inconsistent publication yields `"qualified"` (never an
  unqualified confirmed claim), and the qualification is present in the
  primary result type.
- A15/A16: repeat indexing and durable reopen preserve equivalent canonical
  results **and** the same qualification value.
- A18: publication movement and cancellation fail closed with no partial
  classified results.
- No digest-stability or reopen claim may be cited as snapshot-consistency
  evidence (this Section 1 boundary is itself an acceptance assertion).
