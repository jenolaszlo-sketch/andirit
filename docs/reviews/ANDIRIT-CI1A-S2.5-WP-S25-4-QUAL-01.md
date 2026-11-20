# ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-01

**WP-S25-4 — REMEDIATION REQUIRED**

**Recorded** 2026-11-16 Asia/Manila. **Repository** Andirit, main at D:/projects/Andirit.
**Reviewed production revision** 48370711a6247b972dfc83628618924e864be374, tree
3483bd278c7a628383a372cb788559cbdaf5c717. The initial checkout was clean at the documentation-only
entry commit 581b84a5e910626b87fb260becf90aac500dd7bb, tree e43297f5cfa25eca93ab3d30b644ccab1363ac31,
one ahead of recorded origin/main, with a single worktree and zero stashes. The production, test,
dependency, project and CI surfaces match the qualified Linux revision.
[Preflight](CI1A-S2.5-WP-S25-4-QUAL-01-evidence/preflight.log) · [Owner entry](ANDIRIT-CI1A-S2.5-WP-S25-4-ENTRY-OWNER-01.md)

## Blocking finding F-S254-01

**M5.2 and the required B3 external control.** The reviewer fixture calls string.Concat("left", "right")
once. Roslyn uniquely binds the invocation to string.Concat(string?, string?) in System.Private.CoreLib,
outside the declared project universe. The approved Stage-2 matrix §5.2 requires External or
UnsupportedExternalEvidence and no minted occurrence. The resolver returned MissingDefiningCompilation.
The extractor persisted External=0 and Unresolved=1. The target query and raw occurrence unit agree
on those counters, and no external fact was minted.

The first harness attempt omitted current verified authority and received the expected authority
refusal. The corrected harness supplied authority, returned all 50 independently predicted positive
facts, and reproduced the external classification on publication and fresh-process reopen. The
frozen resolver returns MissingDefiningCompilation when it finds no matching live assembly. The
extractor increments Unresolved for this result. The separate allowance for missing defining
evidence in §3.1 applies to its missing-evidence branch. §5.2 explicitly requires External or
UnsupportedExternalEvidence for the external control. The classification error blocks M5.2 and
the required B3 external control.
[Publication receipt](CI1A-S2.5-WP-S25-4-QUAL-01-evidence/b3-publish.json) · [Fresh-process reopen](CI1A-S2.5-WP-S25-4-QUAL-01-evidence/b3-reopen.json)
· [Preliminary harness attempt](CI1A-S2.5-WP-S25-4-QUAL-01-evidence/attempts/b3-publish.json)
· [Defining resolver](../../src/Andirit.CSharp/VerifiedCSharpAnalysis.cs)
· [Occurrence extractor](../../src/Andirit.CSharp/VerifiedCSharpOccurrenceExtractor.cs)

## Matrix, provenance and independence

The 64 numbered rows are reconciled. Recorded dispositions total 18 blocked, 4 deferred, 2 fail, 27 pass, 10 pass with qualification, 3 reused qualified. The design allocates
57 rows to fresh execution, 3 to reuse of accepted Linux scope and 4 to Stage 6 deferral.
Rows with incomplete fresh evidence identify their missing cases. The two failures refer to
one reproduced finding. The commission authorizes the execution identified as pending in the
approved design. The matrix's stricter external classification remains controlling.
[64-row matrix](CI1A-S2.5-WP-S25-4-QUAL-01-evidence/matrix.md) · [Overlays](CI1A-S2.5-WP-S25-4-QUAL-01-evidence/overlays.md)
· [Obligation reconciliation](CI1A-S2.5-WP-S25-4-QUAL-01-evidence/obligations.md)

Reviewer provenance is the fresh Codex QUAL-01 reviewer conversation. This session had no role in
the frozen implementation or entry design. The repository identifies jenol as the author of the
recorded producer and entry commits. The reviewer had read access to the pinned implementation
and historical records. The new probes derive expected IDs and refusal behavior using raw
source bytes, declared spans and canonical forms, the frozen R2 authority and a reviewer-written
encoder. This establishes session separation and methodological independence for the new probes.
Organizational independence is unestablished. The SQ-04 and P2 harness replays are fresh executions
of earlier sealed code with preserved historical provenance and scope. They earn credit only for
their demonstrated cases. Execution used Windows x64, SDK 10.0.401, runtime 10.0.12 and LatticeDbSharp 0.2.0.
[Toolchain](CI1A-S2.5-WP-S25-4-QUAL-01-evidence/toolchain.log) · [Executable and native library hashes](CI1A-S2.5-WP-S25-4-QUAL-01-evidence/final-probe-executables.json)
· [Reproduction](CI1A-S2.5-WP-S25-4-QUAL-01-evidence/reproduction.md)

## Fresh behavior

- **B3.** The real caller used a live ProjectReference to the verified dependency. All 50 expected
  usage IDs, caller owner, dependency target and declaration source matched in the persisted unit
  and on reopen. The missing source yielded unresolved calls and zero trusted dependency bindings.
  The ambiguous overload had two candidates and one Ambiguous count without a fact for that site.
  The external classification is the blocking finding. The outstanding controls are marked in the
  matrix. [Native facts, ordered references and pagination](CI1A-S2.5-WP-S25-4-QUAL-01-evidence/b3-reopen.json)
  · [Missing](CI1A-S2.5-WP-S25-4-QUAL-01-evidence/b3-missing.json) · [Ambiguous](CI1A-S2.5-WP-S25-4-QUAL-01-evidence/b3-ambiguous.json)
- **B6.** One logical occurrence had two ordered storage contributors. Removing A retained B after
  reopen. Removing B removed the shared fact, and same-site facts with a different semantic owner or
  plugin survived. F-01 bounds the constructed contributor control. The real publisher authority path
  was exercised separately. [Union](CI1A-S2.5-WP-S25-4-QUAL-01-evidence/b6-publish.json) · [A removed](CI1A-S2.5-WP-S25-4-QUAL-01-evidence/b6-reopen-a.json)
  · [B removed](CI1A-S2.5-WP-S25-4-QUAL-01-evidence/b6-reopen-b.json)
- **B7.** A repeated same-ID same-payload stage was idempotent. Changing only the immutable content
  hash of another contributor's input yielded CodeGraphBatchRejectedException. The accepted graph,
  publication, coverage and occurrence result matched immediately after refusal. Fresh-process
  reopen retained the exact raw unit, page and meta state. [Conflict](CI1A-S2.5-WP-S25-4-QUAL-01-evidence/b7-conflict.json)
  · [Reopen](CI1A-S2.5-WP-S25-4-QUAL-01-evidence/b7-reopen.json)
- **D3.** Graph-only and occurrence-enabled publications had identical graph snapshots with 37
  distinct nodes, 97 edges, 10 caller edges and 10 reference edges. Both native-mirror and managed
  traversal agreed after reopen. Graph-only analysis was unavailable. Full D3 remains Stage 6.
  [D3 fixture and snapshots](CI1A-S2.5-WP-S25-4-QUAL-01-evidence/d3.json)
- **D4.** The explicit native before-commit barrier held the reader until publication. The old
  read returned 50 complete facts and the queued read returned 55 complete facts attributed to the
  new publication. Superseded and unbound reader requests refused. The sealed-harness replay
  reproduced immediate known-fault preservation, unknown-outcome quarantine of reads and writes,
  and durable reconciliation after reopen within that process. The outstanding fresh-process
  post-unknown case, combined real recovery/conflict and writer supersession are recorded in the
  matrix. D4.5, full S-02 and F-02 crash/power-loss remain Stage 6.
  [Schedule](CI1A-S2.5-WP-S25-4-QUAL-01-evidence/d4-schedule.json) · [Managed recovery](CI1A-S2.5-WP-S25-4-QUAL-01-evidence/d4-established-recover.json)
- **D5 and SQ-04. SQ-04 — SATISFIED WITH QUALIFICATIONS UNDER AMENDED CONTRACT.** The authentic
  pre-REM store and WAL were staged together. The disposable store refused wholesale with
  CodeOccurrenceSchemaException. The authentic R2 graph-only copy retained graph readability, gave
  one expected legacy caller and returned AnalysisUnavailable for occurrences. Database copies
  changed bytes when opened, and both master sets retained their exact hashes. Isolated schema
  version and namespace changes each threw CodeOccurrenceSchemaException. An isolated page-binding
  mutation threw CodeOccurrenceIndexException. The amended scope retains exclusions
  occurrence-facts/1, occurrence-facts/2, occurrence-facts/3, usage1, usage2, usage3, asmctx1 and
  asmctx2, and the obligation to reconsider upon authentic discovery.
  [Historical copies and hashes](CI1A-S2.5-WP-S25-4-QUAL-01-evidence/historical-copy-integrity.json)
  · [Version](CI1A-S2.5-WP-S25-4-QUAL-01-evidence/d5-schema.json) · [Namespace](CI1A-S2.5-WP-S25-4-QUAL-01-evidence/d5-namespace.json)
  · [Page binding](CI1A-S2.5-WP-S25-4-QUAL-01-evidence/d5-page.json)

## Predecessor and platform dispositions

**WP-S25-3 — CLOSED WITH QUALIFICATIONS. S-01 — SATISFIED WITH QUALIFICATIONS.
P3-F-01 — CLOSED BY VERIFIED ADDITIVE ERRATUM.** The clarified canonical-forms v1 contract
governs the bounded fresh binding proof and the already accepted R13 results. Historical delta
records 179 reference identities and fresh P2 records 181. The ordered strings are absent from
both original receipts. The fresh B3 receipt retains its own ordered reference inputs.
Q-P2-01 through Q-P2-06 retain, in order, the earlier methodological independence limitation,
artifact-only reconstruction limits, O-1 host-scoped composed targets and stable component-digest
comparison surface, finite case scope, Stage 6 deferrals and the qualified historical D3/D4/D5
boundary. Fresh positive binding, contributor controls and real graph comparison are bounded
additions to the evidence.
[P2 additive erratum](ANDIRIT-CI1A-S2.5-WP-S25-3-P2-QUAL-01-ERRATUM.md)
· [WP-S25-3 closure](ANDIRIT-CI1A-S2.5-WP-S25-3-CLOSURE-OWNER-01.md)
· [Fresh provenance primitives](CI1A-S2.5-WP-S25-4-QUAL-01-evidence/b3-publish.json)

**SQ-05 — SATISFIED WITH QUALIFICATIONS.** Genuine Ubuntu 24.04.5 x64 run 42744298084 executed the
accepted complete expanded native suite with 52 passed, 0 failed and 0 skipped. The authorized
commit and CI synthetic merge resolve to the same tree. The synthetic merge is a distinct CI
object outside main's ancestry. The accepted Linux minimum remains satisfied on the unchanged
protected baseline. B3/B6/B7, the real D3 mirror comparison, the new D4 schedule, the new D5
refusal and authentic-copy cases, and the fresh pagination receipts are Windows x64 evidence.
No new Linux job ran. Full platform certification remains Stage 6.
[SQ-05 owner decision](../decisions/adr-ci1-01-ci1a-s2-5-wp-s25-2-owner-01.md)
· [Revision and writer identity](CI1A-S2.5-WP-S25-4-QUAL-01-evidence/source-audit.log)

**F-G2-01 — OPEN.** Recorded Ubuntu branch coverage was 296/450, 65.77%, against 70%. The hosted
workflow failed the repository coverage gate. The passing Windows functional regression and
the WP-S25-4 findings have separate dispositions.
[Coverage record](../decisions/adr-ci1-01-ci1a-s2-5-wp-s25-2-owner-01.md)

## Regression, integrity and next commission

Fresh Release tests recorded 907 passed, 0 failed and 0 skipped, including 345 core, 353 C#,
157 TreeSitter and 52 LatticeDb. The overlapping focused runs passed 8 historical refusals,
25 native publication/recovery cases and 71 C# binding cases. The frozen R2 oracle passed 185/185
and returned exactly 146851 bytes at SHA-256
97f6c5e5a4cc77e089481c7643732b7a07a31d483c2643bd7a95fdad7ced39c9. Release build had zero warnings
and zero errors. Formatting exited 0. All nine historical manifests verified 183 entries at
their pinned artifact lengths and digests, with verified Git ancestry and reviewed trees.
[Whole suite](CI1A-S2.5-WP-S25-4-QUAL-01-evidence/full-solution-tests.log) · [TRX counters](CI1A-S2.5-WP-S25-4-QUAL-01-evidence/regression-summary.json)
· [Oracle](CI1A-S2.5-WP-S25-4-QUAL-01-evidence/oracle.log) · [Release build](CI1A-S2.5-WP-S25-4-QUAL-01-evidence/release-build.log)
· [Format](CI1A-S2.5-WP-S25-4-QUAL-01-evidence/format.log) · [Historical seal check](CI1A-S2.5-WP-S25-4-QUAL-01-evidence/historical-manifests.jsonl)
· [Evidence manifest](CI1A-S2.5-WP-S25-4-QUAL-01-evidence/manifest.json)

Next commission should address F-S254-01 with a bounded change and fresh requalification of the
affected obligations and all outstanding controls. The separate test-only coverage commission
at the existing 70% threshold remains the owner's stated direction before Stage-2 acceptance.
Work-package owner acceptance, S2.5 completion and Stage-2 acceptance remain separate decisions.
