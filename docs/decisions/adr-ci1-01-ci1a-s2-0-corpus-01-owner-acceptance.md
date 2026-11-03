# ADR-CI1-01-CI1A-IMPL-01-S2.0-CORPUS-01-OWNER-01: Qualified F12 owner acceptance

**Project:** Andirit. **Date:** 2026-11-03 (Asia/Manila).
**Commission:** `ADR-CI1-01-CI1A-IMPL-01-S2.0-CORPUS-01-OWNER-01`.
**Disposition:** `ADR-CI1-01-CI1A-IMPL-01-S2.0-CORPUS-01-OWNER-01 ACCEPTED WITH QUALIFICATIONS`.
**F12:** `F12 CLOSED — ACCEPTED WITH QUALIFICATIONS` at the corpus/reference-oracle boundary below.
**S2.2:** `S2.2 SEMANTIC EXTRACTION — NOT AUTHORIZED BY OWNER-01`.

## 1. Authority and decision

The user explicitly commissioned an evidence-based owner assessment and local
recording of its decision. Codex acts as the delegated assessment/decision
recorder under that owner-directed authority. This is the OWNER-01 decision;
neither the producer nor Q3 reviewer made it. The Git author identifies the
recorder, not a human signature, personal approval, meeting or additional
independent review. The owner may revise or reject this recorded disposition
through a subsequent decision preserving its evidence and history.

Accept the frozen, source-bound **R2 semantic corpus/oracle** as the independent
correctness reference for subsequent, separately commissioned S2.2 work. All
mandatory corpus requirements are supported, both Q2 defects are independently
closed, and Q3 identifies no material new defect. The retained qualifications
bound reference use without excusing a failed mandatory requirement.

This decision closes the F12 corpus-preparation/freeze/independent-acceptance
prerequisite. It does not complete the native end-to-end path also required by
F12's original definition. Native publication, reopen, internal lookup and
integrated verification remain mandatory at S2.3–S2.5 and earlier dependent
reliance, as assigned by the existing register and acceptance matrix. Closing
the reference prerequisite does not waive those obligations or complete Stage 2.

## 2. Source and evidence binding

Canonical repository: `D:\projects\Andirit`, remote
`https://github.com/jenolaszlo-sketch/andirit.git`, branch `main`.
Pre-edit HEAD exactly equals qualified R2: no advanced source or conflicting
changes; clean tracked/untracked state, one canonical worktree and no stash.

| Source | Full commit | Full tree |
| --- | --- | --- |
| Qualified R2 / OWNER-01 parent | `7f8d035d7fa6bd5da49f71ee86ff54d7350318c5` | `29150eb6ef2b0fe65942f3c8930037f8c0120174` |
| R2 starting source / final R1 | `9fc2115622c93fa54f5f7882b117185a017703bb` | `547af9c3c7d37132b93944cf8b3dbf284872450f` |
| Original S2.0 | `82f101c24e21828a41a30d4342ba73db9376e24e` | `75faf3eb0fcdab8ad78142617d1d7ee3dfce95c0` |
| S2.1 owner baseline | `b7fea0d219409b279b78317e32bdc3c8b5ae61ee` | `b4a764637498ed8e8bddac28fb0b0b3716b7ffee` |

OWNER-01 verified each tree, original→R1→R2 ancestry and S2.1 owner→R2
ancestry; R2's immediate parent is final R1. The owner-baseline→R2 diff for
`src/`, `tests/`, `benchmarks/`, `smoke/` and `docs/decisions/` is empty.
Thus production, frozen vector tests and accepted decisions were preserved.

| Key | Evidence package and authenticated identity |
| --- | --- |
| E2 | `docs/evidence/adr-ci1-01-ci1a-s2-0-r2/r2-source-evidence-manifest.json`; SHA-256 `BCE7DE2A98602E60C7631757B30EBD05B7B388D65FED69E50A43AE0CEC848B13`; 100 files, manifest self-excluded |
| E3 | `C:\Users\Laszlos\source\repos\Solo\adr-ci1-01-ci1a-s2-0-corpus-01-q3/reviewer-manifest.json`; SHA-256 `9EFC098528340178D6A239A9C0137F8F40B8C62ACE2B1D5546561C1B56CCF52A`; 338 files, manifest self-excluded |
| EO | `C:\Users\Laszlos\source\repos\Solo\adr-ci1-01-ci1a-s2-0-corpus-01-owner-01/`; `verify_owner.py`, `owner-verification.json`, documentation validation and final Git/handoff receipts |

OWNER-01 verified E2's external trust hash and all 100 entries against working
bytes, exact R2 Git blobs and the E3-authenticated R2 archive. Groups are 8
planning documents, 17 fixtures, 12 oracle files and 63 evidence files.
The existing R2 verifier also passes in canonical Git. E3's verifier authenticates
all 338 entries and exact covered inventory, including reports, raw commands,
probes, prior review copies and original/R1/R2 archives. Its manifest names the
exact R2 commit/tree and `CI1A-S2.0-CORPUS-01-Q3 SUPPORTED WITH QUALIFICATIONS`;
`REPORT.md`, `FINAL-HANDOFF.md` and `F12-CLOSURE-RECOMMENDATION.md` agree.
This authenticates the supplied reviewer artifacts to the user-provided trust
anchor; it does not invent a reviewer signature or external attestation.

E3 `EVIDENCE-INTEGRITY-REVIEW.md` and `evidence/integrity-*.command.json/.log`
record clean/restored Git-free verification and six refusals: fixture-byte,
expected-byte, missing-source, coordinated payload/manifest edit, wrong trust
and absent trust. These tamper controls were **executed by Q3**, not rerun by
OWNER-01. E3 `SOURCE-BINDING.md`, `evidence/canonical-before.txt`,
`canonical-after.txt`, `preserved-baseline-paths.txt` and `preservation.json`
support canonical nonmutation during Q3 and archive preservation. OWNER-01's
current clean-state check corroborates the endpoint, not every historical action.

## 3. Historical qualification sequence

| Stage | Actual disposition | Owner interpretation |
| --- | --- | --- |
| Initial S2.0 | Ready for Q1 with qualifications | Producer evidence, not independent acceptance; original 114-check oracle |
| Q1 | Remediation required | Seven findings/qualifications retained with original severities |
| R1 | Ready for independent Q2 | Targeted producer corrections; final source `9fc2115622c93fa54f5f7882b117185a017703bb` |
| Q2 | Remediation required | Two correctness defects; Q1-F01/F03 still not closed at R1 |
| R2 | Ready for independent Q3 | Corrected canonical mapping and authority predicate; separately frozen 185-check oracle |
| Q3 | Supported with qualifications | Independent closure recommendation; no owner decision or implementation authorization |
| OWNER-01 | Accepted with qualifications | Accept the R2 reference; retain all production/integration gates |

Original and R1 remain successful reproductions of their historical behavior,
including known defects. Their reviews are not retrospectively successful.
E3 preserves Q1 `NEW-FINDINGS.md`/`REPORT.md` under `evidence/prior-q1/` and
Q2's 108-entry package under `evidence/prior-q2/`.

## 4. Finding closure

Sources for severity are the original Q1/Q2 `NEW-FINDINGS.md`, not producer
summaries. R1 remediation evidence is `docs/evidence/adr-ci1-01-ci1a-s2-0-r1/`;
R2 is E2. Independent locators below are E3-relative.

| Finding / original severity | Remediation source | Independent confirmation | Residual qualification | Owner disposition |
| --- | --- | --- | --- | --- |
| Q1-F01 source deletion / Blocking | R1 DEL01; R2 canonical lifecycle correction | Q2 `FINDING-CLOSURE-MATRIX.md` left it open; Q3 `LIFECYCLE-REVIEW.md`, `CROSS-VIEW-IDENTITY-REVIEW.md`: 20+32→32, all 52/32 identities; deleted reinjection fails 4 checks | Reference lifecycle, not native deletion | Closed at F12 reference scope after Q3 |
| Q1-F02 material options / Significant | R1 actual CheckOverflow variant | Q2 `COMPILATION-OPTIONS-REVIEW.md`; Q3 `OPTION-A-AND-R2-REVIEW.md`, independent context records: source/parse fixed, options and contexts change, restore exact | Production enforcement still later | Closed |
| Q1-F03 E02 / Significant | R1 actual resolved source target; R2 `ProvenanceGate` and frozen e02/variants | Q2 left it open due Q2-F02; Q3 `PROVENANCE-AUTHORITY-REVIEW.md`, `E02-ADVERSARIAL-REVIEW.md` independently close it | Fixture authority; E01 metadata remains distinct | Closed at F12 reference scope after Q3 |
| Q1-F04 archive verifier / Minor | R1 Git-free trusted verification; R2 trusted manifest | Q2 `MANIFEST-AND-ARCHIVE-REVIEW.md`; Q3 `EVIDENCE-INTEGRITY-REVIEW.md`: clean/restored pass; six tamper refusals | Archive mode does not prove live Git ancestry | Closed |
| Q1-F05 ambiguity / Minor | R1 semantic classification correction | Q2 `SEMANTIC-CORRECTIONS-REVIEW.md`; Q3 `F12-COVERAGE-REVIEW.md` and independent diagnostics: genuine CS0121 differs from CS0176 invalid access | Bounded semantic cases | Closed |
| Q1-F06 event binding / Minor | R1 S15 event member and separate handler | Q2 `SEMANTIC-CORRECTIONS-REVIEW.md`; Q3 `F12-COVERAGE-REVIEW.md`: E: event distinct from M: handler | Event category deferred, no confirmed-fact expansion | Closed |
| Q1-F07 coverage model / Minor / qualification | R1 explicit model labels and contract classification | Q2 `COVERAGE-CLASSIFICATION-REVIEW.md`; Q3 `F12-COVERAGE-REVIEW.md`, `LIFECYCLE-REVIEW.md` | Model-level coverage/publication; NEW-04 open | Classification/documentation closed; qualification retained |
| Q2-F01 inconsistent identities / Blocking | R2 `CanonicalTarget.cs`, canonical reduced/original declaration, parameter-count shape, frozen crossView/lifecycle | Q3 `CANONICAL-TARGET-REVIEW.md`, `CROSS-VIEW-IDENTITY-REVIEW.md`: all nine independent full byte tuples; exact regression fails 12; reduced-normalization mutation fails 9; novel shared-shape mutation fails 15 despite agreeing views | Fixture-declared component forms; not general production canonicalization | Independently closed; accepted at reference scope |
| Q2-F02 stale/substituted compctx2 / Significant | R2 `ProvenanceGate.cs`, candidate/current/universe equality and frozen authority | Q3 `PROVENANCE-AUTHORITY-REVIEW.md`, `E02-ADVERSARIAL-REVIEW.md`: real current/stale/wrong contexts independently derived; zero positive fails 2; presence-only regression fails 12; novel universe and promoted-authority probes fail 5 each | Fixture universe, not durable provenance authority | Independently closed; accepted at reference scope |

E3 `NEW-FINDINGS.md` reports no material unresolved new defect. The mislabeled
producer handoff commission (R3), older default reproduction instructions and
abbreviated inventory prose are nonblocking editorial observations. Exact
source identities, version-specific commands and inventory resolve them without
changing frozen artifacts or accepted contracts.

## 5. Reproducibility and accepted correctness claims

OWNER-01 inspected actual command arrays, exit receipts, JSON summaries and
four TRX counters; it did **not** rerun Roslyn or the regression/mutation suites.

| Q3-executed evidence | Verified preserved result |
| --- | --- |
| Original / R1, own pinned archives and expected files | 114/114 / 130/130, exit 0 |
| R2, two independent processes | 185/185 twice, exit 0; byte-identical and producer-equivalent |
| R2 raw SHA-256 | `97f6c5e5a4cc77e089481c7643732b7a07a31d483c2643bd7a95fdad7ced39c9` |
| R2 LF-only SHA-256 | `577603295884242155facf737066d019c60198de31e6a01fbed644c9ce40891d` |
| Solution regression | 645 passed / 0 failed / 0 skipped: CSharp 120, native 43, core 325, TreeSitter 157 |
| Negative controls | Ten producer mutations rejected; three novel Q3 adversaries rejected; two additional per-site lifecycle controls rejected |
| Historical all-zero substitution | R1 still passes byte-identically; restored R1/R2 reproduce clean baselines |
| Separate Roslyn/Python reference | 26 explicit comparisons; nine cross-view sites; all 52 baseline/32 retained identities |

Recorded environment: Windows x64, SDK 10.0.401, runtime 10.0.12, Roslyn
5.9.0.0. Q3's solution command uses `-p:NuGetAudit=false`; no vulnerability
audit completion is claimed. See E3 `HISTORICAL-ORACLE-REVIEW.md`,
`REGRESSION-RESULTS.md`, `MUTATION-PROBES.md`, `evidence/*-run-*.command.json`,
`review-summary.json`, `mutation-results.json`, `solution-test-direct.log`
and `trx-escalated/`.

Canonical acceptance includes correct defining-target normalization,
ReducedFrom handling and original generic declaration, method **parameter
count** for the fixture shape, source ownership and source deletion expectations.
All nine sites have independently matching encoded usage4 bytes in both views.
Historical anchors and full frozen expected sections supplement cross-view
equality: nine separate hardcoded literals are **not** claimed. S06/INV-31
rely on frozen sections and independent reconstruction. Frozen production R2
vectors are unchanged.

Defining-provenance acceptance includes actual fixture compilation derivation,
current Core `compctx2:sha256:7bf4ac5b916ab5dcd2fadaa45c22cc7765ab83839eb7de8ef4d442865d07424d`,
candidate-independent authority and required per-unit universe membership.
Valid/restored cases permit; missing, all-zero, genuinely stale, wrong-App,
missing-universe and incompatible-universe cases refuse. Current and universe
share a trusted fixture snapshot; independence is from the untrusted candidate,
not three physically separate authority calculations. Semantic resolution
remains distinct from provenance eligibility.

OWNER-01 additionally compared original/R1 expected bytes directly to their
historical Git blobs. Their SHA-256 values remain
`d5f507ddf2220df556702982e3be1f86cde698195aea66872f488e96ede5c0d7`
and `1a53633eef9517128edfe90e8e715c513056488e430860462ab76bf0db3ad085`.

## 6. F12 requirements

The [obligation register](adr-ci1-01-ci1a-s2-obligation-register.md) §5 supplies
the controlling minima, including the stricter five cross-project calls.
Q1 independently counted the base corpus; Q3 confirms the same actual project
graph. Variants do not inflate these counts. The external source is included
among 11 files; five projects/namespaces do not require an invented sixth unit.

| Base corpus requirement | Minimum | Independently supported |
| --- | ---: | ---: |
| C# source files | 10 | 11 |
| Invocations | 50 | 53 |
| Method references | 20 | 21 |
| Overloaded methods | 5 | 5 |
| Namespaces | 3 | 5 |
| Projects | 2 | 5 |
| Cross-project invocations | 5 | 52 |

E3 `F12-COVERAGE-REVIEW.md` maps every mandatory qualitative case: source
edit/replacement; authoritative source deletion; actual compilation-option
change; reference change; valid complete-empty; unsupported semantic kinds;
external metadata E01; resolved source target lacking provenance E02;
incomplete acquisition/analysis; genuine ambiguity; missing binding;
independently frozen expected results; repeatability; canonical cross-view
identity; source ownership/lifecycle; and provenance-authority refusal.
Complete-empty uses actual Solo zero-invocation syntax with modeled Produced
coverage; unavailable declared input uses an actual failed read with a modeled
IncompleteAnalysis result. Neither is production completeness enforcement.
Method references and unsupported kinds remain coverage/reference cases;
Invocation is the initial mandatory confirmed category.

## 7. Bounded qualifications and excluded claims

| Qualification | Accepted limit |
| --- | --- |
| Q-A fixture components | Identifier/declaration/shape digest forms are fixture-declared; general S2.2 source canonicalization remains to implement and qualify |
| Q-B runtime references | Certain identities depend on recorded .NET 10.0.12 artifacts/environment; another runtime requires explicit fixture qualification, not silent normalization |
| Q-C model authority | Coverage, acquisition, lifecycle and provenance include declared models; no native storage, durable publication, complete-empty replacement or production recovery qualification follows |
| Q-D purpose-built corpus | Controlled actual Roslyn project graph, not exhaustive arbitrary-repository qualification |
| Q-E platform | Windows reproducibility only; Linux, crash/power-loss and multi-process qualification remain open |

These are scope boundaries, not corrected defects. No acceptance of completed
production semantic extraction, all canonical digest forms, verified production
defining-compilation binding, durable provenance publication, native
deletion/replacement, correct coverage merging, full recovery, public Find
Usages, full D3/D4 or cross-platform production readiness is made.

## 8. Standing contracts and open obligations

The [Candidate C decision](adr-ci1-01-owner-decision.md),
[Stage 1 acceptance](adr-ci1-01-ci1a-s1-owner-acceptance.md),
[REM-01 acceptance](adr-ci1-01-ci1a-s2-boundary-rem-01-owner-acceptance.md),
[S2.1 acceptance](adr-ci1-01-ci1a-s2-1-owner-acceptance.md),
[R2 identity contract](../planning/adr-ci1-01-ci1a-s2-rem-01/identity-contract.md),
[S2.1 provenance contract](../planning/adr-ci1-01-ci1a-s2-1/provenance-contract.md),
[Option A decision](adr-ci1-01-ci1a-s2-2-target-identity-decision.md),
[acceptance matrix](../planning/adr-ci1-01-ci1a-impl-01/stage2-acceptance-matrix.md)
and [pre-release policy](adr-ci1-01-pre-release-compatibility-policy.md) remain
controlling. Historical packaging qualifications in prior acceptance records
are not erased by authenticating this newer corpus package.

Option A is preserved: `asmctx3` binds the defining R2-faithful `compctx1`;
`compctx2` governs dependency-sensitive validity/invalidation with explicit
universe membership; `usage4` stays frozen. A transitive-only context change
invalidates evidence without re-identifying an unchanged target. Production
mapping and enforceable after-reopen provenance association remain mandatory;
if that association cannot be enforced, return to the owner before minting.

| Open obligation | Existing controlling gate retained |
| --- | --- |
| Q-F2 | Correct decoding/refusal or explicit supported-encoding decision, with independent evidence/owner acceptance before S2.2 acceptance; earlier dependent claims must be bounded |
| Q-F3 | Qualify invalid/netmodule/lazy PE validity/refusal or explicit unsupported-reference decision before S2.2 semantic acceptance |
| A-01 | Resolve comma-symbol canonicalization before public exposure or dependent caller-supplied parse-option reuse |
| A-02 | Preserve scope digest's unit/path composition; scope alone is not source inventory; qualify dependent claims |
| S-01 | Establish actual live defining compilation/source/target correspondence during S2.2; integrated independent chain at S2.5 |
| NEW-04 | Correct/qualify coverage merge before any dependent claim; otherwise independently qualify correction before S2.2 acceptance |
| NEW-01 | Separately authorized foundation-recovery correction, independently qualified before S2.3 and any earlier recovery reliance |
| F16 / NEW-02 | Nine-part traceability before each dependent slice's test authoring; independently confirm corrected identity matrix before S2.2 test authoring |
| Native integration / F12 end-to-end | Real source→Roslyn→native publication→reopen→internal lookup, source-owned lifecycle, complete-empty/refusal and verified identities at S2.3–S2.5 |
| D3/D4/D5 | Preserve graph identity/cardinality/query/traversal and publication authority; real-project/applicable native proofs at S2.3–S2.5; full D3/S-02 ten-invariant matrix at Stage 6 |
| Authentic historical native fixtures | Immutable masters/per-instance copies/refusal for available formats before Stage 2 acceptance; unavailable authentic writers require explicit owner amendment, not fresh-store tampering |
| Linux | Expanded current native suite before S2.5 acceptance; full platform certification remains Stage 6 |
| Crash/power-loss/multi-process | Separate durability/concurrency qualification if claimed; no present guarantee |

The register's other obligations remain open at their existing gates. Earlier
reliance moves a prerequisite before that reliance; this table grants no deferral.
Pre-release policy remains no production consumer compatibility obligation,
no experimental migration, legacy readers, dual-write or adapters; explicit
versions, fail-closed unsupported schema, accepted re-ingestion and historical
evidence retention. No disposal is authorized. D3 graph and D5 aggregate/Hetu
compatibility remain protected.

## 9. S2.2 readiness and next eligible commission

F12 reference readiness is now satisfied, alongside accepted S2.1 provenance
and Option A direction. This permits **preparation of a separate S2.2 scope
and entry-criteria commission**. It authorizes no extractor or production work.

That preparation must bind the initial Invocation scope to actual verified
Roslyn bindings; keep deferred categories explicit; define production canonical
target mapping, real defining-context authority and enforceable provenance;
compare source-derived output to the accepted R2 reference; and specify
unsupported/unresolved/ambiguous/stale/unverified refusal and completeness.
It must retain Option A §5.1/§5.2 tests and §7 minting prerequisites, Q-F2/Q-F3,
coverage reliance gates, F16 traceability and independent review/owner closure.
Mapping implementation and its independent review precede production target
minting, under separately granted implementation authority. These are future
deliverables, not satisfied by accepting fixture evidence.

S2.3 native publication must have its own authorization and NEW-01 closure
before reliance. S2.4/S2.5 and later stages remain gated. Neither fixture
acceptance nor a readiness table authorizes downstream implementation.

## 10. Recording and verification boundary

Current status and work queue are maintained in the obligation register,
project roadmaps and the
[OWNER-01 handoff](../planning/adr-ci1-01-ci1a-impl-01/s2-0-owner-handoff.md).
Frozen S2.0 planning/README, fixtures, oracles, expected results and R1/R2/Q3
evidence remain byte-preserved; their pending-owner wording is historical.
The companion handoff supplies current S2.0 status without invalidating E2.

OWNER-01 performs manifest/Git/evidence reconciliation and documentation/link/
whitespace checks only. No new technical qualification or security audit is
claimed; the 645-test suite is not rerun for documentation changes. The local
documentation-only commit, final tree, changed paths, clean state, worktree
count and no-push result are recorded in EO's final handoff/post-commit receipt
to avoid a self-referential commit identity in this decision.
