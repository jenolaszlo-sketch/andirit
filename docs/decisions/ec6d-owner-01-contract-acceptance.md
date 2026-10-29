# EC6D-OWNER-01: Contract and Architecture Decision Acceptance

**Project:** Penghou.Hetu (future Andirit)

**Milestone:** final EC-6D planning acceptance

**Recorded:** 2026-10-29 (Asia/Manila)

**Decision branch:** `acceptance/hetu-ec6d-owner-01`

**Decision authority:** explicit owner instruction in the EC6D-OWNER-01
execution request, directing all three dispositions below

**Executor:** delegated acceptance executor; the Git author label records
automation provenance, not a human signature or an independent review event

**Disposition: ACCEPT WITH QUALIFICATIONS.**

## A. Git binding and authority

Repository `jenolaszlo-sketch/penghou-hetu`; planning mirror
`C:/Users/Laszlos/source/repos/Solo/.hetu-ec6d-planning-mirror.git`;
isolated worktree `C:/Users/Laszlos/source/repos/Solo/hetu-ec6d-owner-01`.

| Role | Commit | Tree | Parent |
| --- | --- | --- | --- |
| Corrected and accepted contracts | `cc65338f50e585d78c3a1fa8e06d02c184a10451` | `9940965196b35488258501e9513ec1d8025d4007` | `f3de16ffd04015f345fcdd65e8ca996fdb19555d` |
| Q3 final review | `f3de16ffd04015f345fcdd65e8ca996fdb19555d` | `77b5eacf2fdfea8be9f41be10da11c074be566ba` | `9ec3d7f4306cffa33858bcb49a798945f81dd044` |
| Q3 review content | `9ec3d7f4306cffa33858bcb49a798945f81dd044` | `4e4de139b80bd4bef40579f4f2b1edd5ebd8694e` | `3ce7dd93c105e7e7375a04edb083be3a5d97f672` |
| Reviewed R2 | `3ce7dd93c105e7e7375a04edb083be3a5d97f672` | `a10b91d10523515de9c038b9fa2e8cc51000c19d` | `0e1b213737d42c74df20aef4744ab5718058e266` |
| R1 remediation | `0e1b213737d42c74df20aef4744ab5718058e266` | `ddf777aa155852daca40b6ed8748c028c67d5822` | `91a3ed53a256e89ed71d7d11c204252c417400a6` |
| Recovered original planning | `91a3ed53a256e89ed71d7d11c204252c417400a6` | `d79e5814485040a36c77bcde332ebc937f312476` | `3b5e8d91e89e082fd236452656c1d66a4afd3662` |
| EC-6C owner acceptance | `2c3090cba6d8a56102a5c47ceb026c5b5fea3234` | `ece772f1f2b2497891f4019d0d85f4d852b197e1` | `5e79418f8b3864e8b829a4be10ab62c6b5744d89` |
| Canonical main (unchanged) | `558f8ac82ac1b5e07da68f4a71a70240eaf38409` | `08a01a1b7d5d3109b991bcd05d57272fd3f5f018` | `0da1f56d3afc4448fb04e33a6aa84ebff3513599` |

The owner's supplied request expressly directs ADR-1 Option A, ADR-2
qualified results with safeguards, and ADR-3 narrow attribution authority,
conditioned on authenticated contracts, five non-material corrections and
no blocking evidence. Those conditions are satisfied by the bounded audit
below. This acceptance records that instruction; it does not invent a
signature, a separate approval date, consumer confirmation or human review.
The recorded date is execution/recording time only.

This record is committed directly after the corrected-contract commit;
its containing commit/tree are resolved after commit using
`git show -s --format="%H%n%T%n%P" acceptance/hetu-ec6d-owner-01`
and reported in the final handoff. The record intentionally binds to the
already existing contract commit rather than to its own future hash.

## B. Evidence basis

1. Q1 content `5ba43e1cd23871c55e01d9201a492a32dff09ae6`, binding
   `d2bb7b1c6d2c760ed044ad31994a90d8665fd0dc`: original provenance
   rejection, subsequently addressed by exact recovery. [Q1 disposition](../planning/ec6d/q1-findings-disposition.md)
   preserves every finding, F-01 evidence wording and F-08 deferral.
2. R1 preserved authentic recovered ancestry; Q2 required lexical,
   physical-cardinality, discovery, provenance, publication and acceptance
   remediation. Original Q2 `6e34452647fb28ee12be14798142921838f997bd`
   is superseded by reconciled content
   `6630624f976d53161e2657b638c7a67c41f9c0ed` / tree
   `5c4447b43c19133fc912ad1402f34405541a74fe`, binding
   `1bfcc84bfd95974fe9c54637a347e0e4d4b158e7`. Q3 authenticated this
   crosswalk/supersession; [R2 dispositions](../planning/ec6d/q2-findings-disposition.md)
   preserve Q2-01..08 and N01..09.
3. [Q3 report](../reviews/ec6d-scope-q3-independent-verification.md),
   bound in §A: **ACCEPT WITH QUALIFICATIONS**, no remaining P0/P1/P2,
   five P3 findings; §§E–I contain binding/attribution/publication/acceptance
   traces and recorded TypeScript 5.9.3 comparisons (23/23 R2 and 14/14
   Q2, zero differences). [Q3 evidence index](../reviews/ec6d-scope-q3-evidence/README.md)
   and committed artifact hashes are verified without rewriting evidence.
4. [Targeted closure and regression audit](../planning/ec6d/owner-01-findings-closure.md)
   applies only Q3-F01..F05. [Validation record](../planning/ec6d/owner-01-validation.md)
   identifies commands, outcomes and limitations. No further general
   architecture review was commissioned.

## C. Findings closure

| Finding | Accepted correction | Contract sections and evidence | Disposition |
| --- | --- | --- | --- |
| Q3-F01 | 12 modified + 6 added; full modified list and separately counted fixture files | Evidence index R2 Git surface; exact R1→R2 name-status | CLOSED |
| Q3-F02 | Ambient fixture corrected; both unsupported-import-form trigger paths/evidence/precedence explicit | Query §3.4; A12/A26; stored contexts/resolver outcomes | CLOSED |
| Q3-F03 | Completed same-file proof to another declaration omitted; ambiguity/context gates retain their reasons | Symbol §3.5; A22; existing import/query disposition | CLOSED |
| Q3-F04 | Merged interface node remains queryable; target record null; import chain exactly ambiguous-export with both contributors and no confirmation | Query §2/§4; acceptance requirement 2/result shape/A24; recorded TS/p9 evidence | CLOSED |
| Q3-F05 | Explicit union of written-name and associated import-local-name branches before classification; occurrence dedup retains discovery provenance | Query §3.3; A02; Q3 alias trace, A07/A27 guard cases | CLOSED |

No new false-confirmation route, inconsistent acceptance row or material
change to Q3's reviewed semantic reading was found. Lexical gates, import
route, attribution algorithm, closed reason codes, possible-consumer
predicate, publication policy, coverage and work-budget rules remain
unchanged. A24's output pinning is Q3's expressly requested safe reading.

## D. Accepted contracts (immutable baseline)

All five artifacts below are accepted as implementation specifications at
**commit `cc65338f50e585d78c3a1fa8e06d02c184a10451`, tree
`9940965196b35488258501e9513ec1d8025d4007`**, under
`docs/planning/ec6d/`. This record supplies owner disposition over their
historical proposal/review status banners; those banners do not grant
implementation permission. The second record commit does not change these
five files. Later substantive amendment requires a new explicit disposition.

| Contract | Exact Git blob |
| --- | --- |
| [symbol-binding-contract.md](../planning/ec6d/symbol-binding-contract.md) | `cb2b59b0d5e848c4b227cc86117ad753421ce784` |
| [consumer-query-contract.md](../planning/ec6d/consumer-query-contract.md) | `8e0bb0f171d3761493fa561d2712d8fa27103c90` |
| [publication-consistency-contract.md](../planning/ec6d/publication-consistency-contract.md) | `91e4f8065b875713fc12c4953afbe2ccf3303f17` |
| [acceptance-contract.md](../planning/ec6d/acceptance-contract.md) | `73b9c6fe7c7305f88c266cdd2c374e75af7800c7` |
| [scope-proposal.md](../planning/ec6d/scope-proposal.md) | `85afde73e203bb59eae53ce40567aaa4cf4621e4` |

## E. Owner architecture decisions

### ADR-1: ACCEPT OPTION A — QUERY-TIME JOINS WITH COMPUTED WITNESSES

Accept the exact bounded profile above: persist source-grounded per-file
import, heritage-context, export-reference and conservative attribution
facts; resolve supported identity relationships through bounded query-time
joins; deterministically compute `binding1.local-v1` witnesses over ordered
stored fact ids and policy, with source/unit/publication provenance.
Ambiguous, unsupported, contradictory and incomplete proof states never
confirm. No independently authoritative semantic binding edges are approved.
The fact representations/providers and occurrence identity inputs in symbol
§§1–2/3.6/5 govern; `export-declares`/attribution container spelling does not
authorize a persisted cross-file semantic binding edge or a second authority.

Basis: Q3 §§E/F/J, symbol §§1–5 and the
[ADR-1 comparison](../planning/ec6d/representation-decision-proposal.md).
Required future tests: A01–A07/A12–A16/A19–A26 (unique physical identity,
aliases, lexical safety, same-file proof, witnesses, deterministic ordering,
replacement/delete/target-inventory changes and reopen) and A27/A28 for
scope boundaries; full A01–A28 contract remains mandatory. Preserve p9
entries/occurrences/evidence, relative-v1/resolver2, existing defaults and
presets. Both stores require qualification and migration gates.

**Alternative considered:** Option B, extraction-time persisted binding
edges, is not authorized. Selecting it, persisting authoritative witnesses,
changing identity/provider policy, broadening lexical scope or changing
cross-file ownership/invalidation requires a new owner decision and
correctness review with revised acceptance tests. Query cost is bounded by
charged work, but performance remains unmeasured.

### ADR-2: ACCEPT QUALIFIED RESULTS WITH MANDATORY SAFEGUARDS

Adopt R2 design 2 exactly. The authoritative truth table is publication
contract §3.2; the interaction table is ADR-2 proposal §3:

| Provider declaration | Required result qualification |
| --- | --- |
| false, any snapshot identity | qualified |
| true, non-null/non-blank snapshot identity | consistent |
| true, null/blank identity | qualified; never snapshot authority |

Every result preserves full repository/run/index/snapshot identity and
qualification in official display, serialization and export. The checked
consistent-only authority path rejects qualified results; navigation/triage
may use them with explicit labels. Unknown consistency never implies
consistency. Coverage caveats remain mandatory even for consistent results.
Durable reopen/digests do not establish immutable-source consistency.

Publication movement during a fail-on-change query (including same-content
new-run replacement), invalid identity/input, contradictory facts, missing
witness facts, superseded/mismatched units, cancellation and query/store
failure return no classified output. Integrity errors cannot be downgraded
to ordinary qualification. Failed/cancelled indexing retains the last
successful publication without a freshness claim. Source consistency and
durable fact integrity remain separate. Required future tests are
A16/A17/A18/A18b, including serialization, authority guard, labels, failure
retention and no partial outputs. Basis: Q3 §G/§J, publication §§1–6,
[ADR-2 proposal](../planning/ec6d/publication-policy-decision-proposal.md).

**Alternative considered:** global fail-closed behavior for every
inconsistent publication. It remains documented but is not selected.
Changing consumer-visible policy or removing safeguards requires separate
owner authorization and correctness review; arbitrary downstream copying of
a bare list remains a documented limitation, not a promise of prevention.

### ADR-3: AUTHORIZE NARROW EC-6D IMPORT/EXPORT ATTRIBUTION ACROSS THE TRACK B DEFERRAL

Accept the narrow exception to D-EC6C-04 and
[EC6C-OWNER-01](ec6c-owner-acceptance.md): import-occurrence facts;
supported direct named relative imports, aliases and type-only specifiers;
conservative direct export-to-physical-declaration attribution; fact C
heritage context; relevant fact E references; supported same-file proof;
computed query witnesses; explicit unresolved/unsupported outcomes.
Necessary persistence/schema additions are within the accepted design scope
but their implementation/activation and integration require separate gates.

Necessity: accepted p9 facts cannot confirm even one imported declaration
without the missing occurrence/attribution facts. This self-contained subset
supplies the bounded vertical slice without full scope tables or general
TypeScript resolution. Enforce boundaries through symbol §§1–3 allowlists,
physical-record counts, context gates, one-hop relevance without closure,
closed reasons, structured coverage and A02/A10/A12/A21–A28 negatives.
Basis: Q3 §§D/F/J and integration-readiness R2 item 3.

**Not authorized:** general Track B, full TypeScript semantics, arbitrary
re-export closure, calls/ordinary references, implicit implementations,
package-wide symbols, other languages, unbounded graph traversal or broad
public query expansion. This neither reopens nor completes Track B. Those
alternatives require a new owner decision and correctness review. No
production work is authorized by this exception alone.

## F. Inherited qualifications (remain open)

- Windows-focused evidence only. WSL2 is not native Linux qualification;
  native Linux and macOS have no required evidence here. EC-6C independent
  qualification used Windows/.NET 10.0.401 and public store contracts.
- p9 and EC-6B/EC-6C remain outside canonical main. The historical migration
  hash is still unreproducible (FQ1-EV-1); fresh reproduction supports the
  substance only. QN-1 remains a non-blocking test-maintenance note. Staged
  p6→p8→p9 evidence does not prove a direct p6→p9 deployment procedure.
  Representative durable migration/reopen/rollback rehearsals and
  store-specific requirements remain pending, including p9→new profile.
- Small fixtures and pinned website smoke do not establish broad-repository
  accuracy, scale or performance. G7/performance NOT EVALUATED; no latency,
  throughput, scalability, memory or resource target is invented.
- **R16 remains OPEN:** intended-consumer confirmation at or before
  implementation authorization. Planning acceptance does not satisfy it.
- Narrow TypeScript subset only; unknown/unsupported/frontier outcomes are
  expected and consumer-visible. No whole-program, compiler, bundler or
  runtime equivalence. Legacy FindImplementations remains seed-only (F-08).
- Positive-only p9 exports do not establish completeness. JavaScript inline
  type-like recovery remains conservative structural evidence, not runtime
  validity (FQ1-DOC-1). F-01's historical evidence-wording qualification is
  retained; raw observations and mixed-line-ending digests stay unchanged.
- EC-6D remains specification only: no new facts/query implementation or
  independent implementation qualification exists. EC-6E durability,
  incremental invalidation/resource qualification and deployment-specific
  platform/release gates remain separate.

These are later milestone qualifications, not failures of this bounded
planning acceptance. All limits in Q3 §L/§N, the risk register and earlier
owner decisions remain in force.

## G. Authorization boundaries

This closes planning acceptance and records the three expressly directed
decisions against the corrected immutable contracts. It prepares history
and a reviewable downstream handoff. It does not authorize production
implementation, EC-6B/EC-6C integration, merge, push, rename, migration
activation, publication, package release or deployment. Historical planning,
review, evidence and accepted decision records remain protected.

## H. Integration readiness

Next is **HETU-INT-01: Accepted History Integration and Worktree
Consolidation**, requiring separate explicit authorization of the exact
candidate/tree, target, integration method, durable rehearsals and checks.
Use the [integration handoff](../planning/ec6d/owner-01-integration-handoff.md),
including separate object databases, off-ancestry review preservation,
compatibility obligations and rename identity constraints. No historical
worktree may be retired until its required objects/evidence/refs are safely
preserved and verified. Subsequent sequence: ANDIRIT-RENAME-01, then
EC6D-IMPL-01 on the accepted renamed baseline, with R16 and all applicable
authorization gates satisfied.

## I. Final owner disposition

**ACCEPT WITH QUALIFICATIONS.** Q3 provenance is authenticated; all five P3
findings are closed without material change to reviewed behavior; the three
directed architecture decisions are accepted under the owner's explicit
request. Meaningful inherited qualifications remain, so this is not an
unqualified acceptance. No new blocker was identified.

## J. Evidence and limitations

[Validation](../planning/ec6d/owner-01-validation.md) and
[mechanical results](../planning/ec6d/analysis/owner-01-validation.json)
record focused checks; the manual cross-contract trace is in the closure
record. They establish trustworthy planning/evidence identities and
consistent specifications, not implemented production behavior. No broad
regression suite or 43-case oracle rerun was performed for these
documentation corrections. The final record commit/tree and clean status
are emitted after commit; this document never claims its own future hash.
