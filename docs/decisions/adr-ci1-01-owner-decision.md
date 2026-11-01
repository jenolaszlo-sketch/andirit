# ADR-CI1-01-OWNER-01: Architecture Selection — Candidate C (with qualifications)

**Project:** Andirit

**Milestone:** ADR-CI1-01-OWNER-01 — owner architecture selection for the
canonical representation of source-owned C# invocation occurrences, closing the
architecture-selection question left open by ADR-CI1-01 after the EXP-01 and
EXP-02 comparative experiments, the Q1/Q2/Q3 independent reviews and the
R1/R2 remediations.

**Recorded:** 2026-11-01

**Decision authority:** explicit owner instruction in the ADR-CI1-01-OWNER-01
request ("Selected architecture: Candidate C, separate source-owned occurrence
facts"; "Owner decision: D3 ACCEPTED"; "D4 ACCEPTED WITH EXPLICIT INVARIANTS";
"D5 ACCEPTED — NO MIGRATION").

**Executor:** delegated decision recorder. The Git author label records
automation provenance, not a human signature or an independent review event. The
recorder did not conduct Q3 and issues no second review verdict; the Q3
disposition is cited from its own report.

**Disposition: `ADR-CI1-01 — CANDIDATE C SELECTED, WITH QUALIFICATIONS`.**

This record documents an already-made owner decision. It does not implement
CI-1A, does not reopen the independent reviews, does not rewrite any experiment,
and does not authorize production code, schema, package publication or release.

**Preservation principle (binding on this record).** Accepted decisions remain
authoritative. Historical decisions, experimental evidence, independent reviews
and failed-before results are not rewritten to make the final choice appear
inevitable. Findings that favoured Candidate B2, and the negative Q1/Q2 verdicts,
are preserved in place.

---

## A. Git and evidence identity

### A.1 Canonical source baseline (verified)

| Item | Value |
| --- | --- |
| Repository | `jenolaszlo-sketch/andirit` (`https://github.com/jenolaszlo-sketch/andirit.git`) |
| Canonical checkout | `D:\projects\Andirit` |
| Branch | `main` |
| HEAD | `31c33a934aa13e373d9a3a6ffbc54d789eef092c` |
| Tree | `a265f561243de37e1636490f99acb0b34245809e` |
| Working tree | clean (no tracked or untracked changes) |
| Worktrees | one (`D:/projects/Andirit 31c33a9 [main]`) |

The expected canonical commit, tree and branch match the observed values. The
local experiment and review directories are **not** Git checkouts or worktrees of
this repository; they are plain directories under
`C:\Users\Laszlos\source\repos\Solo\` and are cited as external evidence. Their
frozen copies are `git archive` exports, not clones.

### A.2 Experiment and review identities (external, not Git)

| Stage | Workspace (external) | Identity |
| --- | --- | --- |
| EXP-01 | `...\Solo\adr-ci1-01-exp-01` | 31-entry ledger; `frozen/` = 712 files from `git archive` of `31c33a9`; no `.git` |
| EXP-02 | `...\Solo\adr-ci1-01-exp-02` | 821-entry manifest; frozen tree `3eee34f038359068b5168da082fe274b042a7bdb552850f09b68d00469695678` (712 files) |
| Q1 | `...\Solo\adr-ci1-01-exp-02-q1` | 877-entry manifest; recorded tree `e66ac703dc3fe4041dae7662072c91ca1ad6adc95f0e0607ccd0127fb4bbf079` |
| R1 | `...\Solo\adr-ci1-01-exp-02-r1` | 1,671-entry manifest; recorded tree `3d7fa1ff…` (self-referential — see G) |
| Q2 | `...\Solo\adr-ci1-01-exp-02-q2` | 1,694-entry manifest |
| R2 | `...\Solo\adr-ci1-01-exp-02-r2` | **960 entries**, scheme `r2-inventory-v1`, tree `a0f69aa636c524e8671c711895c4f0cfb7e834c6ce7e3e570e30b554241d258b`, self-excluded; recomputed by Q3, 0 missing / 0 mismatches |
| Q3 | `...\Solo\adr-ci1-01-exp-02-q3` | **1,018 entries**, scheme `q3-inventory-v1`, tree `32cbbbcf8034f78e774fe1cb16dede5321d4f1d068018f7a898346f605b591a2`, self-excluded |

These manifest "trees" are custom sorted `path\0sha256\n` inventory hashes, not
Git tree objects (Q2-F06). The R1/Q1 recorded trees are self-referential and not
reproducible; their *content* is intact and preserved. R2 and Q3 manifests
exclude themselves and are reproducible.

### A.3 Independent review provenance

| Review | Workspace (external) | Verdict |
| --- | --- | --- |
| Q1 | `...\adr-ci1-01-exp-02-q1` | `SELECT C NOT SUPPORTED` |
| Q2 | `...\adr-ci1-01-exp-02-q2` | `FURTHER REMEDIATION REQUIRED` |
| Q3 | `...\adr-ci1-01-exp-02-q3` | `Q3 SUPPORTED WITH QUALIFICATIONS` |

Q3 is the final independent qualification of the corrected R2 identity and
index behaviour. **Q3's key evidence is cited from Q3's own report and probes,
not from R2 handoff summaries.** Producer tests are not represented as
independent executions except where Q3 separately executed or re-derived them
(sections G and K state that distinction explicitly).

### A.4 Frozen repository-side decisions (unmodified)

`docs/planning/ci1-scope-01/architecture-decisions.md` (ADR-CI1-01…05),
`docs/planning/ci1-scope-r1/**`, `docs/planning/ci1-scope-r2/**`,
`docs/planning/ci1-scope-r3-integration/**`,
`docs/decisions/ci1-scope-owner-01-acceptance.md` and
`docs/decisions/csharp-correctness-owner-01-acceptance.md` are preserved
unchanged. This record supersedes none of them and rewrites none of them.

---

## B. Decision statement

**ADR-CI1-01 selects Candidate C as the canonical representation for
source-owned invocation occurrences.**

Occurrence facts are logically separate from the existing aggregate code graph.
Candidate B2 (a versioned occurrence edge kind alongside the aggregate edges) is
recorded as **EVALUATED — NOT SELECTED**.

The selection is bounded by, and does not exceed, the Q3 supported scope and the
qualifications carried in section G.

---

## C. Decision rationale

### C.1 Accepted rationale (owner)

Candidate C is preferred because:

1. Source occurrence identity has its own semantics, provenance and lifecycle.
2. Occurrences are owned and replaced through source-unit publication.
3. Occurrence retrieval requires bounded, stable, site-ordered indexing.
4. Contributor union, conflict rejection and deletion must preserve independent
   fact identity.
5. Existing aggregate-graph semantics should not acquire occurrence-specific
   visibility exceptions.
6. Candidate C provides a cleaner separation of responsibilities.
7. The experiments support the selected representation within their recorded
   qualification boundaries.

**Performance is supporting evidence, not the sole selection criterion.** This
record does not claim C is universally faster or smaller. The earlier comparison
remains bounded by its datasets, machine, provider behaviour and benchmark
conditions (single host, synthetic storage workloads, single/few benchmark
repeats; see G).

### C.2 What the experiments actually established

- Both candidates passed the shared correctness gate on identical
  producer-derived occurrences (EXP-01 C01–C20 + adversaries, 41/41; EXP-02
  73/73).
- Candidate C native LatticeDB persistence, publication, lifecycle, contributor,
  conflict, index, close/reopen and snapshot parity passed in tested scope
  (EXP-02 Gate B; Q1 §D; Q3 Gate B/C).
- Candidate C has **no legacy graph delta**: zero new nodes, edges or facts; the
  aggregate graph and its consumers are untouched (EXP-01 `COMPATIBILITY-MATRIX`;
  Q2 Gate G3).
- Candidate C provides durable, publication-bound, site-ordered target pages with
  correct bounded lookup (100 returned after 102 examined) and a fast ordinary
  reopen with no rebuild (Q2 §D/§F; Q3 Gate D).
- The corrected shared identity (`usage4` / `asmctx3` with `compctx1` /
  `compilation-universe1`) separates distinct defining compilations and refuses
  unproven contexts (R2; Q3 Gates B/C, 20/20).

### C.3 Correct B2 comparison (preserved, not rewritten)

Candidate B2 is **technically viable**. Its genuine advantages, as measured and
independently reviewed, include:

- reuse of existing aggregate-graph staging, contributor, replacement and
  publication machinery;
- cheaper native publication and deletion than C, and a smaller fresh persistent
  database in the measured U=10,000 workload (e.g. R1: B2 publication
  `3,327 ms` vs C `18,340 ms`; deletion `1,239 ms` vs `6,739 ms`; fresh DB
  `73.6 MB` vs `99.0 MB`, C ≈34% larger);
- a very fast reconstructible derived target index (B2 indexed query
  `0.153 ms` p50 vs C `12.8 ms` at U=10,000 in R1's corrected benchmark; B2
  index-only rebuild `~12.7 ms` vs C `~2.72 s`).

Candidate B2's costs and obligations, as measured and independently reviewed,
include:

- graph visibility/count compatibility handling: total edge counts always
  change, and attached edges change unfiltered/native traversal results
  (EXP-01: `18 → 21` edges; traversal `3 → 6`); an explicit legacy
  visibility/count policy is an owner decision that B2 cannot avoid
  (Q2-F08 / Q2-D3);
- maintaining additional ordered retrieval infrastructure: the derived index is
  in-memory and non-durable and must be bound to the publication generation
  (the R1 index freshness defect Q2-F03, corrected in R2 as `b2-targetindex/2`);
- the work-capped raw path returns an arbitrary subset, not a correct global
  prefix (Q1-F03; R1 retained).

The selection of C is **not** a finding that B2 is incorrect. It is a judgement
that C's separation of responsibilities and its zero legacy-graph exposure
outweigh B2's write/index-speed and reuse advantages once B2's additional
visibility contract and mandatory generation-bound index are counted. The
commission's "closer question" framing and B2's genuine advantages are recorded
here to keep the historical comparison honest.

**B2 disposition:** **EVALUATED — NOT SELECTED.** B2 remains technically viable
and is preserved as a documented, evaluated alternative. Its experimental
implementation and evidence are **not** deleted. B2 must **not** be introduced as
a parallel production representation during CI-1A without a separate
architecture amendment.

### C.4 Why the earlier negative verdicts do not contradict this decision

The Q1 `SELECT C NOT SUPPORTED` and Q2 `FURTHER REMEDIATION REQUIRED` verdicts
were correct at the time and are preserved. The blocker was a **shared** identity
defect (`asmctx1`/`asmctx2` collapsing distinct defining compilations), not a
property of C. R1 corrected path/span collision; R2 corrected the remaining
compilation-universe provenance gap and the B2 index-freshness defect; Q3
independently verified all three blocking findings closed (Q2-F01 CLOSED WITH
QUALIFICATIONS, Q2-F02 CLOSED — INDEPENDENTLY VERIFIED, Q2-F03 CLOSED WITH
QUALIFICATIONS) with no new blocking defect. The path from "not supported" to
"selected" is therefore the recorded remediation chain, not a reinterpretation of
the earlier evidence.

---

## D. D3, D4 and D5 decisions

The experiment/owner labels **D1** and **D2** were resolved or implemented during
remediation and are carried as context (section E). The commission's **D3, D4,
D5** below are the remaining policy choices; their relationship to the Q2
labels is stated so no decision is silently renamed.

> **Label reconciliation (preservation).** Q2 defined D3 = "B2 legacy
> visibility/count policy", D4 = "C maintenance policy", D5 = "historical
> scope". Q3 restyled D1 as the standing declared-scope boundary. This record's
> **D3** consolidates legacy graph compatibility (Q2-D3 plus the C-side
> boundary); its **D4** consolidates index maintenance, publication and
> cancellation (Q2-D2 + Q2-D4); its **D5** is Q2-D5. The underlying Q2 finding
> dispositions (Q2-F08…F13) are retained unchanged in section G.

### D.3 — Legacy graph compatibility: **D3 ACCEPTED**

Source-owned occurrences must **not** be represented as additional legacy
`calls` or `references` edges, or as ordinary graph elements visible to existing
aggregate-graph traversal.

Preserve established behaviour for:

- `FindCallers`;
- `FindReferences`;
- existing `calls` and `references` edges (identity and cardinality);
- legacy graph node and edge counts;
- existing graph traversals;
- existing native graph-store consumers (including the native graph mirror).

Candidate C must use its dedicated occurrence-fact representation and retrieval
APIs. Legacy graph queries must **not** silently incorporate occurrences.

If a future consumer needs both aggregate relationships and source occurrences,
it must request that combination explicitly through a separately defined
interface.

Do **not** introduce compatibility exceptions merely to hide occurrence-specific
graph nodes or edges.

**D3 acceptance obligations (CI-1A must demonstrate):**

- unchanged legacy graph behaviour for the same source corpus;
- correct separate occurrence counts;
- correct target-scoped occurrence lookup;
- no duplicate or invisible aggregate-graph pollution;
- identical relevant occurrence semantics across supported stores.

### D.4 — Index maintenance, publication and cancellation: **D4 ACCEPTED WITH EXPLICIT INVARIANTS**

Select a bounded, publication-consistent occurrence-index model. The
independently verified R2/Q3 index-freshness behaviour forms the experimental
basis.

**Required invariants:**

1. A query may use only an index consistent with the accepted published
   occurrence-fact generation.
2. A stale, unbound or unverifiable derived index must refuse with an explicit
   status.
3. A stale index must not silently trigger an unbounded full-store scan.
4. Failed staging must preserve the last accepted publication and corresponding
   valid index state.
5. Cancelled staging must not expose partial facts or index entries.
6. Superseded publication must not replace a newer accepted generation.
7. Rebuild/reopen must produce a semantically equivalent published index.
8. Contributor changes and scoped deletion must update publication and index
   visibility consistently.
9. Bounded queries must have deterministic ordering and explicit result limits.
10. Query behaviour during concurrent publication must follow a defined snapshot
    or freshness contract.

**Maintenance policy (preferred simple model):**

- Build or update the index within the accepted publication lifecycle.
- Make the index valid only when its publication identity is corroborated.
- Refuse stale or inconsistent state.
- Permit explicit bounded reconstruction where necessary.
- Verify reconstructed state before allowing normal queries.

Do **not** introduce a general background maintenance service, a distributed
index coordinator, or an automatic repair framework without demonstrated need. Do
**not** invent additional concurrency guarantees beyond Q3's evidence.

**D4 implementation qualification:** CI-1A must reproduce the applicable
stale-index, cancellation, failed-publication, supersession and reopen tests
**using actual production storage and APIs**. The previous in-process experiment
does not establish native or cross-process atomicity; record those limits
explicitly.

### D.5 — Historical-store migration and compatibility: **D5 ACCEPTED — NO MIGRATION**

The owner confirms that the existing experimental store format has **no external
users or deployed compatibility obligations**. Therefore:

**CI-1A is not required to support historical-store migration or
backward-compatible reading of superseded experimental occurrence schemas.**

**Policy:**

1. Adopt a new canonical storage representation for Candidate C.
2. Do not implement migration from older experimental representations.
3. Do not maintain dual-write compatibility with B2.
4. Do not introduce compatibility adapters for obsolete experimental occurrence
   layouts.
5. Reject incompatible or unsupported stored schemas explicitly.
6. Do not silently reinterpret historical aggregate edges as occurrence facts.
7. Do not automatically delete or overwrite incompatible databases.
8. Support rebuilding occurrence facts by re-ingesting available source code
   into a fresh compatible store.
9. Preserve historical experiment databases where needed for reproducibility.
10. Document the unsupported-schema behaviour in the future consumer/storage
    contract.

Use existing `AnalysisUnavailable` or unsupported-schema vocabulary where
semantically appropriate. Do not invent a misleading successful empty result
when analysis cannot be performed.

**D5 rationale:** migration would introduce implementation and qualification
complexity without a demonstrated consumer benefit. The current priority is a
correct canonical representation rather than supporting historical experimental
formats. The absence of migration support is an **intentional architecture
decision, not an unresolved engineering gap**. Any future migration requirement
must be separately proposed and approved.

**Note (scope).** D5 covers migration from the superseded *experimental*
occurrence schemas (`occurrence-facts/1…3`, `usage1…3`, `asmctx1…2`). It does
**not** amend the separate, already-accepted historical compatibility contracts
for the aggregate graph and Hetu-labelled stores (ADR-CI1-05 as amended;
EC6D-OWNER-02); those remain in force and are unchanged.

---

## E. Preserved prior decisions

### E.1 Existing CI-1 architecture decisions (unchanged unless noted)

| ID | Authoritative document | Accepted status | Relationship to Candidate C | Unchanged? | Explicit amendment required? |
| --- | --- | --- | --- | --- | --- |
| ADR-CI1-01 | `docs/planning/ci1-scope-01/architecture-decisions.md`; R1 `corrections.md` C5; `ci1-scope-r1/storage-alternatives.md` | Reopened, no winner (previously "recommend C") | **Resolved by this record** (C selected). Original text preserved; a dated addendum + link are appended, not a rewrite. | Yes (historical text preserved) | No — resolved by this owner decision |
| ADR-CI1-02 (public query shape) | `docs/planning/ci1-scope-01/architecture-decisions.md` | Proposed (owner decision required) | Compatible: versioned `FindUsagesV1` is the future consumer of C occurrence facts; not selected here. | Yes | No |
| ADR-CI1-03 (direct/indirect impact) | `docs/planning/ci1-scope-01/architecture-decisions.md`; `implementation-sequence.md` | Proposed; CI-1A alone | Compatible: CI-1A direct groups derive only from returned occurrence rows. | Yes | No |
| ADR-CI1-04 (dispatch/aliases/ambiguity) | `docs/planning/ci1-scope-01/architecture-decisions.md`; R1 amendment (F01) | Proposed as amended | Compatible: exact-identity gate is the same principle C applies at the storage layer. | Yes | No |
| ADR-CI1-05 (publication/lifecycle/historical) | `docs/planning/ci1-scope-01/architecture-decisions.md`; R1 amendment (F02, S02) | Proposed as amended | Compatible with D4 (publication-bound index) and D5 (no experimental migration); the aggregate/Hetu historical contract is untouched. | Yes | No |

### E.2 R1 planning corrections (C1–C9) — preserved

| Correction | Finding | Disposition relative to C |
| --- | --- | --- |
| C1 exact eligible target identity | F01 | Accepted as a shared requirement; C adopts it (`asmctx3` correspondence proof). Unchanged. |
| C2 documented/sealed partial trigger | F02 | Prerequisite/decision, separate from C; unchanged. |
| C3 exact source-byte provenance | F03 | Accepted; C identity includes source-byte hash. Unchanged. |
| C4 manifest oracle strength | F04 (+R2 amendment) | Planning oracle obligation; separate from C. Unchanged. |
| C5 ADR-CI1-01 reopened (no winner) | S01 | Historical; superseded only by this owner decision (C selected), not rewritten. |
| C6 historical compatibility | S02 | Assimilated into D5 (no experimental migration) and the unchanged aggregate/Hetu contract. |
| C7 measurement separation | S03 | Preserved as a bound on all performance claims in this record. |
| C8 coverage/completeness | S04 | Preserved: coverage is publication-owned and disjoint; relevant to D4. |
| C9 bounded CI-1A preserved | — | Preserved: no scope expansion; no `changeKind`/impact fields in CI-1A. |

R1 findings `R1-N1`–`R1-N4` and the F/S findings' dispositions are preserved as
recorded; Q2-F08…F13 are carried forward in section G.

### E.3 Owner decision labels D1 and D2 (resolved / implemented)

| Label | Q2 topic | Status |
| --- | --- | --- |
| D1 | Shared identity remediation (provenance-bearing identity or explicit authoritative universe) | **Implemented in R2** as `compctx1` + `compilation-universe1` + `asmctx3`. Q3-QUAL-01 restyles the residual as a standing *declared-scope boundary*. **Owner confirms the declared-scope boundary is acceptable** as recorded (completeness is scope-relative, not global). Remains a documented qualification, not a blocker. |
| D2 | B2 derived-index generation (if B2 remains) | **Implemented in R2** as `b2-targetindex/2` publication binding. Not part of the C selection, but its evidence is the basis of D4 and is retained for B2 reproducibility. |

### E.4 Other accepted owner decisions preserved (no silent supersession)

| Record | Decision | Relationship |
| --- | --- | --- |
| `docs/decisions/csharp-correctness-owner-01-acceptance.md` | P-DEF-01b (provider-level exact C# target resolution) and P-DEF-02 (partial-type extraction) ACCEPTED WITH QUALIFICATIONS | Unchanged. Pre-existing provider corrections; not modified or absorbed by CI-1A. |
| `docs/decisions/ci1-scope-owner-01-acceptance.md` | CI-1-SCOPE-R3 oracle ACCEPTED WITH QUALIFICATIONS (Q4) | Unchanged. Planning-oracle acceptance; explicitly did **not** select B2/C. This record now closes the selection that R3/Q4 left open. |
| `docs/decisions/ec6d-owner-01-contract-acceptance.md`, `ec6d-owner-02-acceptance.md` | EC-6D binding/contract accepted (incl. historical Hetu labels) | Unchanged. Historical aggregate/Hetu contracts untouched by D5. |
| `docs/planning/ci1-scope-r1/final-handoff.md` decisions 1–8 | Representation, target proof, partial input, small product, identity/bytes, query limits, persistence/history, utility threshold | Items 1 (representation) and 7 (persistence/history) are advanced/completed by this record (C selected; D5 no-migration); the remaining items stay owner decisions for CI-1A scope. |

### E.5 Conflict check (mandatory)

No previously accepted decision conflicts with selecting Candidate C. The
apparent conflict — that ADR-CI1-01's original text "recommended C" — is itself
historical (withdrawn at R1/C5). This decision revives the *choice*, not the
withdrawn *preference*: C is selected on the corrected, independently verified
evidence and with B2 recorded as a viable, evaluated alternative. No prior
decision is overridden without authority; no unauthorized override is made.

---

## F. Accepted technical invariants

The owner selection incorporates the independently qualified R2/Q3 occurrence
identity work. CI-1A must preserve:

- **Domain-separated deterministic occurrence identity.** `usage4` over a
  length-prefixed, ordinal, domain-separated SHA-256 tuple; no wall clock,
  process id, object address, run id or enumeration order participates.
- **Correct target defining-assembly discrimination.** Target identity is
  `asmctx3` = domain-separated SHA-256 over `compctx1`, metadata identity, module
  name, identifier-token digest, full-declaration digest and shape digest.
- **Compilation-universe provenance.** `compctx1` compilation descriptors,
  `analysisscope1` declared scope and `compilation-universe1` verified universe;
  confirmation requires a complete declared scope, exactly one live binding and
  exact declaration correspondence. The unguarded one-argument resolver is gone;
  there is no `IsComplete` flag.
- **Conservative refusal of missing, unresolved or unbound context.** Explicit
  statuses (`Incomplete`, `Ambiguous`, `MissingDefiningCompilation`,
  `MissingCompilationUniverse`, `UnsupportedContextVersion`,
  `UnsupportedExternalEvidence`) plus `CoverageV3.UnavailableReasons`; refused
  occurrences are never promoted to a successful empty result.
- **Distinction between equal-descriptor definitions in different contexts.**
  Distinct defining compilations yield distinct `asmctx3`, distinct C target
  heads/pages and distinct B2 nodes; each query returns only its own definition.
- **Source- and reference-sensitive identity.** Source-byte hash retires
  identities on edit; `compctx1` differentiates on source hash, compilation
  options and reference identity.
- **Contributor union and conflict rejection.** Union applies only to
  semantically identical facts; a conflicting payload is rejected
  (`OccurrenceConflictException`) and the accepted state remains queryable.
- **Correct scoped deletion.** Deleting/replacing one target context removes only
  its fact/head/page (C) or node (B2); derived state is left clean.
- **Consistent retrieval and publication semantics.** A handle is meaningful only
  with its exact publication; a publication change yields an explicit change
  result.

**Do not** replace these with a simpler hash derived solely from documentation
ID, source path or symbol descriptor. **Do not** treat a test-owned identity
oracle as production authority.

**Accepted identity-contract version (recorded).** The accepted contract is R2's
`usage4` / `asmctx3`, with `compctx1` / `analysisscope1` /
`compilation-universe1`, C fact format `occurrence-facts/4` /
`occurrence-codec/4`, and B2 index contract `b2-targetindex/2`, supported by the
Q3 probes (`B4-01…B4-06`, `C1–C4`; 20/20) and Q3's 14/14 index-freshness
probes. Predecessors `usage1` (EXP-01), `usage2`/`asmctx1` (EXP-02),
`usage3`/`asmctx2` (R1) are historical evidence only and are rejected, not
reinterpreted. The architecture decision accepts the **semantics**, not an
unreviewed wholesale copy of experiment code.

---

## G. Qualifications and residual findings

All relevant Q3 limitations and preserved prior qualifications are carried
forward. None is silently resolved by this selection.

### G.1 Q3 qualifications (non-blocking, explicitly retained)

1. **Compilation-scope completeness is relative to the declared inventory**
   (Q3-QUAL-01 / D1). `analysisscope1` is caller-declared (`Declare`) or
   filesystem-discovered (`Discover`); completeness is scope-relative, not
   global. An omitted defining compilation **cannot** produce a false
   confirmation (it refuses), but "complete" is bounded. Recorded as the accepted
   scope boundary, not a defect.
2. **Descriptor source hashes depend on supplied source bytes** (Q3-QUAL-02).
   When `UniverseEntryInput.Sources` is supplied, the descriptor hashes those
   bytes, not a re-read of the on-disk file. The descriptor is exact for the
   compilation input; it is not an independent disk-integrity check.
3. **Concurrency qualification is in-process, not native** (Q3-QUAL-03 /
   Q2-F12). Publication/query interleaving is deterministic in-process; there is
   no native transaction isolation, multi-process or crash/power-loss evidence.
4. **Verification-to-return TOCTOU limitations remain** (Q3-QUAL-03). A newer
   publication could commit after the final re-check but before the caller
   receives the result. A returned result is truthfully attributed to the run it
   was validated against and cannot be misrepresented as a later publication, but
   the guarantee is not native.
5. **Content identity uses a deduplicated summary** (Q3-QUAL-04). B2's
   publication content identity is a deduped per-`(plugin, path)` summary
   (smallest source hash). Non-issue in the supported path because the composed
   receipt also carries the run id and the store forbids re-staging a completed
   run; revisit if one run ever carries multiple contents.
6. **The interleaving hook remains a public test-only surface** (Q3-QUAL-05).
   `QueryInterleavingHook` must not ship to production unchanged; move it behind
   an internal/test-only surface before any production claim.
7. **Q2-F08 through F13 retain their recorded dispositions** (see G.2).

The Q3 qualification limits that apply to the whole evidence base are also
retained: single-host, synthetic workloads; no cross-platform, crash, power-loss
or production-throughput claim; the historical index/benchmark numbers are the
retained comparative evidence, not a fresh campaign.

### G.2 Preserved prior qualifications and residual findings (unchanged)

| ID | Topic | Recorded disposition (retained) |
| --- | --- | --- |
| Q2-F06 | R1/Q1 manifests include themselves; recorded tree not reproducible | Historical evidence-integrity note; preserved, not treated as an R2/C defect. |
| Q2-F07 | Historical fixture hash is run-specific and mutates on open | Accepted bounded limitation; procedure corrected (immutable master + disposable copies). |
| Q2-F08 | B2 changes graph totals; attached mode changes unfiltered traversal | Selection-significant; **owner decision resolved for C by D3**; retained for B2 reproducibility. |
| Q2-F09 | C rebuild blocks queries; no cancellation | Selection-significant operational qualification; **addressed by D4** (maintenance policy; CI-1A must qualify a production-backed rebuild path). |
| Q2-F10 | C write/delete/import cost and larger fresh DB | Preserved selection-significant economic tradeoff; accepted cost of choosing C, not hidden. |
| Q2-F11 | Older-schema/old-reader historical compatibility not demonstrated | Bounded qualification; **resolved for CI-1A by D5** (no experimental migration); aggregate/Hetu history unchanged. |
| Q2-F12 | Crash/power-loss/multi-process/cross-platform not demonstrated | Not demonstrated; carried to CI-1A as an explicit boundary (D4). |
| Q2-F13 | Benchmark absolute timings vary 1.4–1.7×; replacement ordering unstable | Preserved measurement qualification; report ranges, not exact multipliers. |
| Q3-F-NEW-01 | R2 freshness-evidence locator defect | Non-blocking evidence-packaging erratum; see section J and the standalone erratum. |

### G.3 Preserved negative and rejected results (must not be deleted)

- Q1 `SELECT C NOT SUPPORTED` and Q2 `FURTHER REMEDIATION REQUIRED` are preserved
  verbatim as historical verdicts.
- Failed-before reproductions are preserved: EXP-01 v1 identity collision;
  EXP-02 F0–F7 failed iterations; Q1 A2-03 (`asmctx1:sha256:651a8cc6…`;
  occurrence `648d88c2…`); R1 failing-before (`asmctx1:sha256:651a8cc6…`;
  occurrence `ea2e6516…`); Q2 A3-04 (`asmctx2:sha256:32fc2b0d…`); Q3 9/9;
  R2 8/8.
- Retracted producer claims remain retracted: "C uses 23% less persistent
  storage" (C is ≈34–35% larger fresh); "29.6 s index rebuild after reopen"
  (that figure is full fact-table replay; index-only ≈2.5–3.0 s; ordinary reopen
  ≈0.4–0.6 s); unequal byte caps; the B2 1,001-edge work cap returning an
  arbitrary subset.
- Not demonstrated and not claimed: older-schema migration/old-reader
  (experimental occurrence formats); crash/power-loss/torn-write; multi-process
  writer; cross-platform; authentic prior C fact-store writer (none exists);
  candidate-result comparison (no CI-1A implementation exists).
- Unmeasured/uncounted: C's native-id page optimization is `PLAUSIBLE,
  UNMEASURED`; no optimization credit is taken in this record.

---

## H. Implementation implications

CI-1A (`ADR-CI1-01-CI1A-IMPL-01`, section K) must build and verify, at minimum:

1. **Production occurrence-fact representation for Candidate C.** A typed,
   source-owned fact family separate from the aggregate graph, with the accepted
   identity semantics (`asmctx3` / `usage4` and successors agreed with the
   provider layer).
2. **Native LatticeDB persistence obligations.** Fact/unit/stage/publication
   head/target head/target page structures, durable target pages, codec and
   close/reopen; the experiment's native parity must be reproduced against real
   production storage and APIs.
3. **Source-owned publication model.** Project-unit ownership, atomic
   publication, replacement and scoped deletion consistent with the existing
   publication lifecycle.
4. **Identity and provenance handling.** The section F invariants, including
   conservative refusal and recorded coverage reasons.
5. **Target-oriented indexing and bounded queries.** Durable, site-ordered,
   deterministic, explicitly limited retrieval.
6. **Contributor union, conflict and deletion semantics.**
7. **Legacy-graph noninterference (D3).** Prove unchanged legacy behaviour for
   `FindCallers`, `FindReferences`, edge identity/cardinality, node/edge counts,
   traversals and native consumers; occurrences must not appear in legacy
   queries.
8. **Fresh-store initialization and unsupported-schema refusal (D5).** Rebuild
   by re-ingestion; refuse incompatible schemas explicitly with
   `AnalysisUnavailable`/unsupported-schema vocabulary; no silent empty result;
   no automatic deletion.
9. **D4 index lifecycle.** Publication-bound index validity, stale/index
   refusal, failed/cancelled/superseded publication retention, deterministic
   rebuild/reopen, and a production-backed definition of the maintenance window
   (including the cancellation question).
10. **Deterministic and production-backed tests.** Reproduce the applicable
    stale-index, cancellation, failed-publication, supersession and reopen tests
    using production storage; include the D3 noninterference corpus.
11. **Independent implementation review and source-bound evidence**, on the
    repository's normal qualification pattern (producer evidence + independent
    review), with leak, chunk, and immutability evidence where relevant.

CI-1A must **not** expand into unrelated code-graph features or repository-wide
refactoring, and must **not** add a migration phase.

---

## I. Explicit exclusions

This decision does **not**:

1. Implement CI-1A; no `FindUsagesV1`, occurrence storage, persistence schema or
   query API is added by this record.
2. Add historical-store migration or backward-compatible reading of superseded
   experimental occurrence schemas (D5).
3. Introduce B2 as a parallel production representation.
4. Establish production qualification; the experimental evidence remains bounded
   by its declared scope.
5. Initiate another architecture experiment.
6. Publish packages, tag, release, push, or change production source, tests,
   schemas, CI workflows or historical evidence.
7. Claim cross-platform, crash/power-loss, multi-process, native-isolation or
   production-throughput guarantees.
8. Convert any qualification in section G into a mechanical guarantee.

---

## J. Final owner disposition

**ADR-CI1-01 — CANDIDATE C SELECTED, WITH QUALIFICATIONS.**

Candidate C is selected; D3, D4 and D5 are recorded; evidence is traceable;
qualifications are preserved; and no prior decision has been silently
overwritten. B2 is recorded as EVALUATED — NOT SELECTED and remains viable.

The companion evidence-packaging erratum (Q3-F-NEW-01) is recorded separately in
[`adr-ci1-01-q3-f-new-01-evidence-erratum.md`](adr-ci1-01-q3-f-new-01-evidence-erratum.md);
it does not alter frozen evidence and does not invalidate the Q3 result.

**Recommended next verdict: `ADR-CI1-01 SELECTED — READY FOR INDEPENDENT
DECISION REVIEW`.**

---

## K. Implementation handoff (proposal only)

**Proposed next commission: `ADR-CI1-01-CI1A-IMPL-01`. This is a proposal only;
it does not authorize implementation, and it must not be started under this
record.**

**Objective.** Implement the Candidate C occurrence-fact representation for
supported C# method-invocation occurrences, separate from the aggregate graph,
with production-backed identity, publication, indexing and legacy-graph
noninterference, under the section F invariants and section G qualifications.

**Exact production integration surfaces (to be confirmed against the
repository at implementation time):**

- `src/Andirit.CSharp/` — C# provider extraction producing typed occurrence
  facts from the existing Roslyn session; reuse the accepted exact-identity
  semantics, do not re-derive them from documentation IDs.
- `src/Andirit.Abstractions/` — typed occurrence-fact payload, coverage and
  publication-ownership contracts (provider-neutral, versioned).
- `src/Andirit/` — batch/validation, ingestion sink, staging/publication
  lifecycle, store-view and bounded query surface seams.
- `src/Andirit.LatticeDb/` — native fact/unit/stage/head/page persistence,
  codec, close/reopen.
- `src/Andirit.Testing/` — conformance extension for the occurrence-fact store
  view across supported stores.

**Candidate C occurrence-fact interfaces (proposed):**

- Immutable, validated occurrence payload with the accepted identity tuple and
  publication binding.
- Disjoint, persisted coverage (confirmed / external / ambiguous / unresolved /
  unsupported / conditional-inactive) derived from the same scan that emits
  occurrences, never reconstructed from aggregate edges.
- A bounded target-scoped retrieval API with deterministic ordering and explicit
  result limits.

**Native LatticeDB persistence obligations:**

- Durable target pages keyed by target key (doc id + `asmctx3`), single
  publication transaction, staged-row invisibility, contributor records,
  publication head, target head; codec and orderly close/reopen.
- Explicit handling of the provider's `CREATE`/statement limits and the
  measured write/import cost (Q2-F10), without over-claiming an optimization.

**Source-owned publication model:**

- Occurrences owned by the semantic project unit; atomic replacement; scoped
  deletion; failure/cancellation retention of the last accepted publication.

**Identity and provenance handling:**

- Section F invariants; `compctx1` / `analysisscope1` / `compilation-universe1`
  provenance; conservative refusal with recorded reasons; explicit refusal
  vocabulary.

**Target-oriented indexing and bounded queries:**

- D4 invariants; publication-bound index validity; deterministic site order;
  explicit limits; refusal of stale/unverifiable index.

**Contributor union, conflict and deletion semantics:**

- Union only for identical facts; conflict rejection retains accepted state;
  deletion removes only the affected target.

**Legacy-graph noninterference (D3):**

- No new legacy `calls`/`references` edges; unchanged `FindCallers`,
  `FindReferences`, edge IDs, node/edge counts, traversals and native consumers.

**Fresh-store initialization and unsupported-schema refusal (D5):**

- Re-ingestion rebuild; explicit refusal of incompatible schemas; no silent
  reinterpretation of historical aggregate edges; no automatic deletion.

**Required deterministic and production-backed tests:**

- The accepted identity/provenance adversaries (equal-descriptor compilations,
  omitted/unresolved/unbound contexts, linked contexts, cross-project
  positives) reproduced against production storage.
- D4 stale-index / cancellation / failed-publication / supersession / reopen
  tests against production storage and APIs.
- D3 legacy noninterference corpus.
- D5 fresh-store and unsupported-schema refusal tests.

**Independent implementation review and source-bound evidence:**

- Producer evidence plus an independent review on the repository's normal
  pattern; evidence packaged with a self-excluded, reproducible manifest; no
  producer tests represented as independent executions.

**Explicit non-scope:** no B2 parallel representation; no experimental-schema
migration; no `changeKind`/impact fields; no unrelated code-graph features or
repository-wide refactoring; no package publication or release.

---
