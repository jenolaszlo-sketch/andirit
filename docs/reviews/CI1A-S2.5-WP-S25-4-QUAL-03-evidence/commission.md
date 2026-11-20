# ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-03
# Independent R2 Qualification and M5.4 Reachability Assessment

## 1. Commission

The owner authorizes independent qualification of the
implemented WP-S25-4-R2 corrections despite the incomplete
overall R2 producer disposition.

The authorized objectives are:

- Independently qualify F-S254-02.
- Independently qualify F-S254-03.
- Preserve the independently established F-S254-01 result.
- Investigate the precise M5.4 production evidence gap.
- Reconcile the complete qualification matrix against
  the corrected production revision.
- Identify remaining Stage-2 acceptance prerequisites.

M5.4 must be assessed as an evidence and reachability
problem unless a production defect is demonstrated.

This commission does not authorize production remediation,
contract amendments, coverage remediation, remote Linux
execution or owner acceptance.

## 2. Mandatory independence gate

Execute in a fresh reviewer session uninvolved in:

- R2 implementation
- R2 producer verification
- Construction of the R2 producer evidence

Record actual reviewer-session identity and relevant
producer/reviewer provenance.

Expected results must be derived independently from
the governing contracts and fixture inputs.

If mandatory independence cannot be established,
stop before qualification execution.

Do not claim organizational independence where only
session separation and methodological independence
have been established.

## 3. Required starting state

Repository: D:\projects\Andirit
Branch: main

Expected HEAD:

9a550c26dcb4559a317a9663ce8fec827e01bd40

Expected tree:

606c17871555a3e2a41ce8eb7536befd332e97dc

Final R2 production revision:

25bc6e3b4305c7ce088deb36cfb000138dc76648

Production revision tree:

2a1309d8e08f230e61a1cd1bf0f1b596179ea8b3

F-S254-02 implementation revision:

786574e34de0e5e1b242a784a5dc7297e0162bb1

F-S254-03 implementation revision:

25bc6e3b4305c7ce088deb36cfb000138dc76648

QUAL-02 revision:

6d4d45f7aedc85f4b52310cf9a3fa6082f885296

R1 production revision:

afa136f4033f35d9a657d8c5f1d5db92d736b679

Original production baseline and recorded origin/main:

48370711a6247b972dfc83628618924e864be374

Expected repository state:

- Clean working tree
- Single worktree
- No stashes
- Eight commits ahead of recorded origin/main
- Zero commits behind recorded origin/main

Verify exact ancestry and production change scope.

Stop on any unexplained deviation.

Do not fetch or push.

## 4. Governing records

Read:

docs/reviews/ANDIRIT-CI1A-S2.5-WP-S25-4-R2.md

docs/reviews/CI1A-S2.5-WP-S25-4-R2-evidence/

docs/reviews/ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-02.md

docs/reviews/CI1A-S2.5-WP-S25-4-QUAL-02-evidence/

docs/reviews/ANDIRIT-CI1A-S2.5-WP-S25-4-R1.md

docs/reviews/ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-01.md

docs/planning/adr-ci1-01-ci1a-s2-5/wp-s25-4-qualification-matrix.md

docs/planning/adr-ci1-01-ci1a-s2-5/wp-s25-4-execution-plan.md

docs/planning/adr-ci1-01-ci1a-s2-5/wp-s25-4-obligation-inventory.md

Read the applicable governing occurrence, binding,
coverage, availability, completeness, authority and
publication contracts.

The approved 64-row qualification matrix remains
controlling.

Do not redefine acceptance criteria during execution.

## 5. Evidence integrity and implementation audit

Independently verify:

- All 184 R2 manifest entries
- All 245 QUAL-02 manifest entries
- All 381 earlier historical manifest entries
- Reviewed revision and tree bindings
- Historical evidence byte preservation
- Frozen R2 oracle identity
- Exact R2 production and regression-test changes

Inspect both implementation commits independently.

Determine whether the production changes remain
bounded to F-S254-02 and F-S254-03.

Preserve the original failed qualification outcomes
and the incomplete R2 producer disposition.

Do not modify existing historical reports or evidence.

## 6. F-S254-02 independent qualification

Affected matrix rows:

- M4.1
- M4.2
- M5.5

Reported root cause:

VerifiedCSharpOccurrenceExtractor previously visited
invocation expressions only, leaving applicable
constructor, property and method-group sites outside
unsupported coverage accounting.

R2 counts each applicable bound unsupported site
once in UnsupportedCategories and Unsupported.

Independently assess the correction against the
governing contract.

Verify that no positive occurrence-extraction feature
has been introduced for unsupported constructs.

Reproduce the historical failure against the
appropriate baseline using an isolated arrangement
without creating branches or worktrees.

Against R2 independently exercise:

A. Constructor sites

B. Property sites

C. Method-group sites

D. Mixed supported and unsupported sites

E. Multiple unsupported sites in one analysis unit

F. Overlapping syntax and semantic representations
   that could cause double counting

G. Unbound, ambiguous or otherwise unresolved forms

H. External metadata-only controls

I. Unrelated syntax that must not be counted

Use the exact applicable site definitions from the
governing contract.

Derive expected site inventories and category counts
independently from fixture inputs.

For each applicable isolated unsupported site verify:

- UnsupportedCategories increment as required
- Unsupported increment as required
- Zero unsupported occurrence emission
- Correct aggregate coverage
- Preservation of supported occurrence identities

Verify the actual production path at:

- Extraction
- Publication
- Native persistence
- Fresh-process reopen

Do not accept standalone extraction results as a
substitute for required integrated evidence.

Explicitly investigate potential double counting,
category overlap and incorrect classification of
unbound or external sites.

## 7. F-S254-03 independent qualification

Affected matrix row:

M5.7

Reported root cause:

Publication discarded coverage when an incomplete
universe could supply no occurrence authority.

R2 retains missing declared membership and publishes
zero-fact partial coverage.

Independently assess the corrected compilation and
publication behavior.

Examine the actual changes in:

- CSharpCodeGraphPlugin.Compilation.cs
- CSharpCodeGraphPlugin.Session.cs

Reproduce the historical AnalysisUnavailable result
for the missing-declared-unit case.

Against R2 construct independent fixtures covering:

A. Missing declared unit

B. Multiple missing declared units

C. Partially verified declared membership

D. Fully available declared membership

E. Complete analysis with legitimately zero facts

F. No available analysis

G. Stale publication

H. Authority refusal

I. Applicable schema and namespace refusal controls

Use governing requirements to determine which cases
are applicable and derive the expected outcomes.

Require the applicable missing-unit cases to return:

- IncompleteAnalysis
- IsComplete=false
- Correct declared membership
- Correct verified membership
- No fabricated occurrence facts
- No false-available or false-complete result

Independently verify the reported three-declared,
two-verified fixture and additional boundary cases.

Verify publication, native persistence and
fresh-process reopen.

Ensure partial coverage does not become an authority
bypass.

Investigate whether zero-fact partial publication
can overwrite, conceal or incorrectly supersede
previously accepted state.

Preserve the distinction between:

- IncompleteAnalysis
- AnalysisUnavailable
- StalePublication
- Complete-empty analysis
- Authority and integrity refusals

## 8. Preserve F-S254-01

Independently verify that R2 preserves the R1
external-classification correction.

Expected verified metadata-only result:

Classification: UnsupportedExternalEvidence
External: 1
Unresolved: 0
External occurrences: 0

Verify initial extraction, publication, native
persistence and fresh-process reopen.

Preserve genuinely missing, ambiguous and
source-backed binding behavior.

Execute both existing R1 regression tests.

Independently verify all 50 positive B3 occurrence
identities, including:

- Caller ownership
- Dependency target
- Declaration expectations
- Publication
- Native reopen

Respect the accepted host-scoped identity boundary.

Compare stable site inputs, component digests and
fixture-specific independently derived expectations.

Do not treat absolute host-dependent identity
differences as production contradictions without
evaluating the governing contract.

Do not issue formal owner closure of F-S254-01.

## 9. M5.4 investigation objective

M5.4 remains blocked because an exact continuous
production receipt has not been established.

The missing chain is:

Actual production binding
  -> MissingDefiningCompilation
  -> extraction unresolved classification
  -> publication
  -> persisted unresolved counters
  -> fresh-process reopen

QUAL-02 verified the resolver refusal and persisted
unresolved counters through separate controls.

R2 produced an authority-bound transitive fixture
with:

Unresolved=2
External=0

These counters persisted through native reopen.

However, Roslyn returned no method symbol for either
unresolved call, so neither reached ResolveDefining.

Adding the direct reference resolved both calls.

Neither result establishes the exact continuous
M5.4 chain.

Do not conflate general unresolved diagnostics with
MissingDefiningCompilation specifically.

## 10. M5.4 normative requirement assessment

Locate the exact governing M5.4 requirement and
its underlying contract sources.

Determine whether M5.4 requires:

A. A specific MissingDefiningCompilation outcome
   through an authority-bound production path.

B. Contractually correct unresolved accounting
   regardless of the particular internal refusal.

C. Separate resolver-defense and integrated
   unresolved-accounting guarantees.

The approved matrix remains binding.

If the matrix requires A but the underlying contract
appears to require B or C, record the discrepancy.

Do not reinterpret M5.4 to bypass an inconvenient
qualification requirement.

Only the owner may approve a normative clarification
or amendment.

## 11. M5.4 static reachability analysis

Analyze the production control flow leading to:

VerifiedCSharpUniverse.ResolveDefining

Establish the exact necessary conditions for returning
MissingDefiningCompilation.

Trace:

- Verified compilation construction
- Compilation universe membership
- Source and metadata reference capture
- Roslyn symbol acquisition
- Invocation eligibility
- Caller authority establishment
- Containing assembly and declaration information
- Live defining-compilation matching
- Metadata reference recognition
- Missing and ambiguous binding outcomes
- Occurrence extraction
- Diagnostic aggregation
- Publication eligibility
- Persistence

Identify every production call site that can reach
the relevant resolver outcome.

Determine which invariants hold before each call.

Construct an explicit predicate table containing:

- Required predicate
- Source of the invariant
- How production establishes it
- Whether an authorized fixture can violate it
- Consequence when the predicate is false
- Relevant source-code location

Pay particular attention to whether:

- A valid method symbol must exist before the
  resolver is invoked.
- A source-backed symbol necessarily has a
  corresponding verified defining compilation.
- A metadata-backed symbol is necessarily recognized
  as external under the R1 correction.
- Missing references cause Roslyn to return no
  usable method symbol before ResolveDefining.
- Authority establishment excludes other candidate
  scenarios before publication.
- A defensive resolver state remains reachable only
  through fabricated, invalid or non-production inputs.

Do not assume that a defensive branch is unreachable
merely because ordinary fixtures do not reach it.

## 12. M5.4 bounded experimental investigation

Before starting experiments, record a bounded plan
with:

- Candidate fixture families
- Hypothesis for each family
- Necessary semantic and authority conditions
- Expected resolver behavior
- Maximum attempt budget
- Stop conditions

Prefer a small number of discriminating fixtures
over uncontrolled combinatorial experimentation.

Examine applicable scenarios such as:

- Missing direct project reference
- Transitive project reference
- Verified metadata-only reference
- Source-to-metadata boundary
- Source compilation absent from a candidate universe
- Applicable partial universe construction
- Applicable assembly identity ambiguity
- Supported reference aliasing or equivalent
  reference-resolution variations

These are candidate hypotheses, not mandates to
invent unsupported or unauthorized configurations.

Only execute cases compatible with the governing
production contract.

Use actual production paths and genuine Roslyn
binding behavior.

Capture intermediate receipts establishing:

- Fixture and reference inputs
- Roslyn diagnostics
- Symbol availability and identity
- Verified caller and universe membership
- Resolver invocation or earlier refusal
- Exact resolver result where invoked
- Extraction classifications
- Publication outcome
- Persisted coverage counters
- Fresh-process reopen outcome

A direct isolated resolver call may establish
defensive behavior.

It cannot establish production reachability.

Do not use reflection, fabricated symbols, mocks,
authority bypasses or inconsistent compilation
universes as proof of a legitimate production path.

Such controls may be used to demonstrate isolated
defensive semantics if clearly labeled.

## 13. M5.4 reachability disposition

Issue exactly one M5.4 technical disposition:

A. REACHABLE — CONTINUOUS PRODUCTION EVIDENCE ESTABLISHED

The exact resolver classification is observed through
a valid authority-bound production fixture and
connected to the persisted counters and reopen result.

B. UNREACHABLE UNDER ESTABLISHED PRODUCTION INVARIANTS

The reviewer establishes a reasoned, source-backed
argument that the required preconditions cannot
coexist within the governed production path.

This requires more than unsuccessful fixture attempts.

Provide the invariant chain, exhaustive relevant
production call-site analysis, supporting controls
and the exact boundary of the conclusion.

C. PRODUCTION DEFECT DEMONSTRATED

A valid production path contradicts an applicable
binding, coverage or persistence requirement.

Preserve the counterexample and recommend bounded
remediation.

D. REACHABILITY UNRESOLVED — ADDITIONAL EVIDENCE REQUIRED

The investigation cannot establish either a continuous
receipt or a defensible unreachability conclusion.

Record the remaining hypotheses and precise gap.

Do not claim universal mathematical unreachability.

Any unreachability finding must be limited to the
reviewed revision and governed production invariants.

## 14. M5.4 governance consequence

If disposition A is established:

- Record the independent M5.4 qualification result.
- Preserve the complete continuous receipt.

If disposition B is established:

- Do not mark M5.4 passed automatically.
- Identify the exact requirement requiring clarification.
- Propose the smallest possible owner clarification
  or amendment.
- Explain how existing isolated resolver-defense
  evidence and integrated unresolved-accounting
  evidence could support the intended guarantee.
- Preserve all existing refusal and coverage
  semantics.
- Do not propose changing production solely to
  manufacture reachability of a defensive branch.

If disposition C is established:

- Preserve the demonstrated defect.
- Recommend a separately authorized remediation.

If disposition D is established:

- Keep M5.4 blocked.
- Recommend the smallest next investigation.

No contract amendment or owner waiver is authorized
by this commission.

## 15. Reconcile the complete matrix

Preserve QUAL-02's historical matrix outcome:

- 42 pass
- 10 pass with qualifications
- 4 fail
- 3 blocked
- 1 reused qualified
- 4 deferred

Evaluate the R2 requalification-impact matrix
independently.

Reconcile all 64 rows exactly once against the
corrected production revision.

Explicitly address:

- M4.1
- M4.2
- M5.5
- M5.7
- M5.4
- Previously qualified M3.1 and M5.2
- Any additional affected rows
- Outstanding genuine-Linux rows
- F-G2-01-related rows

For each row record:

- Historical disposition
- Current production impact
- Fresh or reused evidence
- Current qualification disposition
- Residual limitations
- Owner action required

Do not mark a row satisfied through producer evidence
alone where independent execution is required.

## 16. Targeted integration regression

Independently assess the effect of R2 on:

- B3 cross-project binding
- B6 contributor union and deletion
- B7 immutable payload conflict rejection
- D3 graph and native-mirror noninterference
- D4 authority, ordering and managed recovery
- D5 schema and namespace refusal
- Authentic historical fixture behavior

Execute fresh controls wherever R2 materially
affects prior integration evidence.

Reuse prior accepted evidence only when the
reviewer establishes that its qualification basis
remains valid.

Preserve the existing F-01 limitation.

Do not repeat unrelated historical qualification
campaigns unnecessarily.

## 17. Regression gates

Execute against the exact R2 production revision:

- All eight new R2 regression tests
- Both R1 regression tests
- Relevant extraction and coverage suites
- Relevant publication and completeness suites
- Required targeted integration controls
- Whole-solution regression suite
- Frozen R2 oracle verification
- Release build
- Formatting verification

Current producer regression floor:

917 passed, 0 failed, 0 skipped

Verify suite completeness and actual execution counts.

Require:

- Zero unexpected failures
- No unauthorized skips or removals
- All 185 frozen oracle cases pass
- Frozen oracle output remains byte-identical
- Release build: zero warnings and errors
- Formatting verification succeeds

Record exact commands, environment, counts,
exit codes and original logs.

Do not aggregate overlapping test counts.

## 18. Linux delta impact

Existing genuine Ubuntu x64 acceptance remains
bound to its previously qualified revision.

Independently assess the combined R1/R2 Linux
qualification requirements.

At minimum preserve the required:

- 52-test native LatticeDb floor
- R1 external classification controls
- R2 unsupported-site accounting controls
- R2 incomplete-analysis controls
- Applicable publication and persistence controls
- Fresh-process reopen controls

Determine whether M5.4 requires additional
genuine-Linux verification based on its technical
disposition.

Document the proposed combined Linux and coverage
qualification route.

No remote Linux execution or CI publication is
authorized.

Do not represent Windows execution as Linux evidence.

Full platform certification remains Stage 6.

## 19. F-G2-01 remains open

Preserve:

F-G2-01 — OPEN

Last independently measured coverage:

296/450 branches = 65.77%

Required threshold:

70%

Do not assume the numerator or denominator remains
unchanged after R2.

Do not:

- Modify coverage thresholds
- Modify coverage filters or exclusions
- Add unrelated coverage tests
- Modify CI
- Close or waive F-G2-01

The subsequent test-only coverage commission must
measure the successor revision using the existing
coverage policy.

Do not represent repository CI as fully green.

## 20. Obligation reconciliation

Reconcile all 78 obligation entries against the
current evidence and owner decisions.

Preserve the governing classifications:

- DISCHARGED
- QUALIFIED
- OPEN-S2.5
- OPEN-STAGE6
- BLOCKED
- AMENDMENT-SUBJECT

Explicitly record:

- F-S254-01 qualification preservation
- F-S254-02 independent qualification result
- F-S254-03 independent qualification result
- M5.4 technical and governance dispositions
- Corrected-revision Linux delta
- F-G2-01
- Remaining Stage 6 obligations

Do not close owner-governed findings solely because
technical tests pass.

## 21. Preserve accepted qualifications

Retain:

- SQ-04 amended qualified acceptance
- Earlier bounded SQ-05 acceptance
- S-01 satisfied with qualifications
- WP-S25-1/2/3 qualified closures
- P3-F-01 closure by additive erratum
- Q-P2-01 through Q-P2-06
- Host-scoped composed-target constraints
- Existing historical fixture limitations
- Existing F-01 contributor limitation

Preserve outstanding Stage 6 obligations:

- Full D3
- D4.5
- Full S-02
- F-02 crash and power-loss qualification
- Full platform certification

Managed fault injection is not crash or power-loss
qualification.

Do not silently expand earlier owner decisions.

## 22. Formal qualification dispositions

Independently disposition:

F-S254-02:
- SUPPORTED
- SUPPORTED WITH QUALIFICATIONS
- REMEDIATION REQUIRED

F-S254-03:
- SUPPORTED
- SUPPORTED WITH QUALIFICATIONS
- REMEDIATION REQUIRED

M5.4:
- Use the technical disposition defined in section 13.
- Record the corresponding governance consequence.

Then issue exactly one overall WP-S25-4 disposition:

A. SUPPORTED

B. SUPPORTED WITH QUALIFICATIONS

C. REMEDIATION REQUIRED

Do not issue an overall supported disposition while
a mandatory unresolved requirement prohibits it.

Separately distinguish:

- Successful R2 correction qualification
- M5.4 evidence completeness
- Functional qualification status
- Linux qualification status
- CI coverage-gate status
- Owner-acceptance readiness

An M5.4 governance blocker must not be misrepresented
as a failure of an otherwise independently verified
R2 production correction.

## 23. Evidence deliverables

Create:

docs/reviews/ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-03.md

Create the corresponding additive evidence package.

Include:

1. Preflight and independence record
2. Exact reviewed revision
3. Independent implementation audit
4. F-S254-02 baseline and corrected receipts
5. F-S254-03 baseline and corrected receipts
6. R1 preservation evidence
7. Positive B3 identity verification
8. M5.4 normative requirement analysis
9. M5.4 production call-site inventory
10. M5.4 predicate and invariant table
11. Bounded experimental plan
12. Experimental receipts and actual outcomes
13. Reachability technical disposition
14. Governance recommendation
15. Complete 64-row reconciled matrix
16. Complete 78-entry obligation reconciliation
17. Regression and frozen-oracle results
18. Updated Linux delta assessment
19. Remaining findings and qualifications
20. Reproducible reviewer probes
21. Self-excluding SHA-256 manifest

Preserve all underlying primitives needed for
independent recomputation.

Make every narrative claim traceable to sealed
evidence.

## 24. Scope restrictions

Permitted:

- Independent qualification execution
- Read-only production control-flow analysis
- Bounded M5.4 investigation
- Additive reviewer probes and fixtures
- Evidence collection
- Qualification documentation
- Local evidence commit on main

Prohibited:

- Production source changes
- Modification of existing regression tests
- Contract or matrix amendments
- Authority bypasses
- Fabricated production reachability claims
- Package, project or dependency changes
- CI changes
- Coverage remediation
- Historical evidence alteration
- Remote Linux execution
- Formal finding closure
- Owner acceptance
- Stage-2 acceptance
- Stage 6 implementation
- Branch creation
- Additional worktrees
- Fetching or pushing

Do not implement remediation during qualification.

## 25. Commit and post-commit verification

Commit the additive report and evidence on main.

Suggested commit:

docs(review): independently qualify R2 and assess M5.4 production reachability (ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-03)

Verify:

- Starting and final HEAD/tree
- Exact reviewed production revision
- Commit ancestry
- Changed-file inventory
- All 184 R2 manifest entries
- All 245 QUAL-02 manifest entries
- All 381 earlier historical entries
- New QUAL-03 manifest
- Historical evidence byte preservation
- Protected paths unchanged
- Clean working tree
- Single worktree
- No stashes
- Local divergence from recorded origin/main

Do not push.

## 26. Final handoff

Report:

- Overall WP-S25-4 qualification disposition
- Reviewer independence actually achieved
- Exact reviewed production revision
- F-S254-02 qualification result
- F-S254-03 qualification result
- M4.1/M4.2/M5.5/M5.7 outcomes
- F-S254-01 preservation
- All 50 positive B3 identity results
- M5.4 normative interpretation
- M5.4 static reachability analysis
- Bounded experimental results
- Formal M5.4 technical disposition
- Required owner action
- Complete reconciled matrix summary
- Complete obligation reconciliation
- Regression and frozen-oracle results
- Linux delta requirements
- F-G2-01 status
- Starting and final HEAD/tree
- Commit identity and changed-file inventory
- Manifest verification
- Post-commit verification
- Push status
- Recommended next commission

Stop after the QUAL-03 handoff.

Await separate owner authorization.