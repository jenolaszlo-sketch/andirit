# EC6D-OWNER-01: focused Q3 findings closure

**Scope:** precision corrections to the authenticated Q3 contracts, not a
new architecture review or implementation qualification. Owner disposition
is recorded separately after this corrected contract has a commit/tree.
Authority is the owner's explicit EC6D-OWNER-01 execution request, including
its three directed choices; no signature or independent human review is
inferred. Execution date: 2026-10-29 (Asia/Manila).

## Authenticated evidence

- Q3 final: `f3de16ffd04015f345fcdd65e8ca996fdb19555d`, tree
  `77b5eacf2fdfea8be9f41be10da11c074be566ba`, parent/content commit
  `9ec3d7f4306cffa33858bcb49a798945f81dd044`, content tree
  `4e4de139b80bd4bef40579f4f2b1edd5ebd8694e`, parent R2
  `3ce7dd93c105e7e7375a04edb083be3a5d97f672` (tree
  `a10b91d10523515de9c038b9fa2e8cc51000c19d`).
- Authoritative definitions: [Q3 report §K](../../reviews/ec6d-scope-q3-independent-verification.md#k-new-findings).
  Evidence inventory: [Q3 evidence](../../reviews/ec6d-scope-q3-evidence/README.md).
- Q1 binding `d2bb7b1c6d2c760ed044ad31994a90d8665fd0dc`, content
  `5ba43e1cd23871c55e01d9201a492a32dff09ae6`; planning response:
  [Q1 dispositions](q1-findings-disposition.md). F-01's original provenance
  rejection was addressed by authentic recovery, not reconstructed files;
  its evidence-wording qualification and F-08 deferral survive.
- Q2 original `6e34452647fb28ee12be14798142921838f997bd` is superseded by
  reconciled content `6630624f976d53161e2657b638c7a67c41f9c0ed` / tree
  `5c4447b43c19133fc912ad1402f34405541a74fe` and binding
  `1bfcc84bfd95974fe9c54637a347e0e4d4b158e7`. [Q2 dispositions](q2-findings-disposition.md)
  preserve Q2-01..08 and N01..09; Q3 §§C–D authenticate supersession and
  verify R2 closure. These are immutable branch-qualified sources, not
  files copied into this acceptance worktree.

## Findings closure

| Finding | Original issue (Q3 §K) | Correction and changed sections | Verification and disposition |
| --- | --- | --- | --- |
| Q3-F01 | Index understates R2 surface; omits two modified paths | [Evidence index](evidence-index.md), R2 Git bindings: 12 modified + 6 added; README/index included; fixture files listed separately; exact two-commit diff command | Git name-status gives 18 paths, exclusively planning; search finds no other active repetition. CLOSED, documentation only |
| Q3-F02 | Wrong ambient fixture; missing eligible-form/unsupported-resolution trigger | [Query contract](consumer-query-contract.md) §3.4: ambient-script vs ambient-export separated; both unsupported-import-form paths, fact evidence and precedence explicit. [Acceptance](acceptance-contract.md) A12 adds synthetic resolver-status assertion; A26 retained | Script/ambient context hits gate 3; ambient exported target hits attribution ineligibility; eligible named unsupported resolver hits route step 1. Earlier gates still win. CLOSED, reachability unchanged |
| Q3-F03 | Same-file unique eligible record completing to another declaration lacks outcome | [Symbol contract](symbol-binding-contract.md) §3.5: omit for this target; distinguish ambiguity, other scope and unsupported context. Acceptance A22 pins positive, different-target and ambiguity cases | Same disposition as existing import §3.4 step 4 and query §3.1. Same-file positive still has its short proof. CLOSED, no confirmation broadened |
| Q3-F04 | Requirement 2 could reject merged target, making A24 unobservable | Acceptance supported-profile requirement 2, result shape and A24; query §2/§4: query runs with null target record; merged import clause is ambiguous-export, all contributors, no confirmation | Exactly Q3 §K's safe reading. Oracle and p9 record counts inspected below. CLOSED, required A24 outcome made explicit |
| Q3-F05 | Literal first discovery bullet misses export-alias local name | Query §3.3: construct associated imports, per-file local-name sets, explicit union before classification; provenance, occurrence identity, gates, coverage/frontier unchanged. Acceptance A02 pins Alias ∉ A2 and dedup | Q3's existing bullet-2 route made explicit; no additional hop, no identity from relevance. A02/A07 positives and A27 exclusions retained. CLOSED, classified set unchanged under Q3 reading |

## Reason and identity traces

`ambient-script`: fact C fileModuleContext=script reaches gate 3 (after
recovered-syntax). `ambient-export`: the consumer is a non-ambient module;
fact B excludes the target's ambient declaration and route step 3 emits
`ineligible-declaration`. An ambient owner itself uses gate 3, as A26
already requires. For unsupported resolution, one eligible named import
passes gates 1–12 and route step 1 uses the same-publication resolver's
Unsupported outcome. Unsupported-form gate 9 still wins before duplicates
or conflicts; no missing counterpart is converted into an ordinary reason.
There are still 20 emittable reason codes and two reserved, non-emittable
codes; invalid input and internal integrity failures remain errors.

The Q3 TypeScript 5.9.3 manifest/run were read without rerunning the oracle:

| Fixture | Compiler-observed physical relationship | Bounded contract trace |
| --- | --- | --- |
| merged | types.ts line 1 `export interface Widget { a: string; }`, line 2 `export interface Widget { b: number; }`; zero diagnostics; consumer Merged binds both | p9 has one interface node and two physical records; B=ambiguous-declarations, import route step 3=ambiguous-export; target record null; no confirmation, input error, first record or reserved reason |
| type-value | Widget interface + const; zero diagnostics; compiler associates both declarations | Two physical records before dedup; ambiguous-export; no guessed namespace distinction |
| same-file | Local's clause binds Widget interface; zero diagnostics | Supported context and one physical record=node target: same-file confirmed short proof |
| export-alias | Internal exported as Public imported as Alias; zero diagnostics; Renamed binds Internal | A1 includes Public, associated occurrence links local Alias; union branch (b) selects clause; full direct import proof confirms |
| nested-unrelated | Namespace-local Widget and module import coexist; zero diagnostics; module class binds import | Nested declaration excluded from module candidate count; positive import route preserved |

Exact p9 `merged` contributor ids from Q2 `p9-adversarial-results.json`:
`typescript:d867a49767283b97e4bd60cf146534aefc153ae8a1e21ca952e0bd0c4f6615a7`
and `typescript:f8abdcd8031ced653b80254de2cb1f34cb27ab72b4af204acf94784af6bf135e`.
The interface node is
`typescript:symbol:a3bd3912de7e042d171d76cc7c48b133b02617e14efdd59ff65dd8a68424fc97`.
This is evidence for the fixture, not a claim of implemented EC-6D behavior.

## Focused cross-contract regression audit

| Obligation | Evidence checked | Result |
| --- | --- | --- |
| Positive unique identity | Symbol §§2/3/4, query §3.1, acceptance A01/A03/A06/A07/A24 | Unchanged physical-record-before-node count and complete proof; ambiguous target may be inspected, never confirmed |
| Lexical safety | Symbol §§3.1–3.4, acceptance A05/A21–A23/A26 | Gate sequence byte-identical; nested shadow, duplicates, merges, unsupported contexts cannot bypass it |
| Same-file target identity | Symbol §3.5, query §3.1, A13/A22 | Positive proof retained; different declaration omitted consistently |
| Import/export attribution | Symbol §§1/2/3.4; A02/A10/A10b/A27 | Same attribution statuses, all contributors, resolver join, one-hop association and barrier predicate; no name-only confirmation/re-export closure |
| Discovery, provenance and categories | Query §§3/4/5/6; A02/A07/A08/A13/A14/A25/A27 | Explicit union preserves existing local-name linking; dedup by occurrence identity retains origins and competing facts; no unknown promoted; possible consumers remain independent module-level relationships |
| Publication vs integrity | Publication contract §§1–6; query §5; A17/A18/A18b | Publication contract byte-identical; full identity/truth table, movement/cancellation/store error and authority checks unchanged |
| Coverage and execution bounds | Query §§4–6; A10/A14/A25–A28 | Unsupported records, not-evaluated frontier, pre-dedup charging, truncation and lower-bound counts retained |
| Scope and decisions | Scope proposal R2; both ADR proposals; integration readiness R2 | Choices remain exactly Q3-recommended A/design 2/narrow authority; no expanded Track B or implementation permission |
| Compatibility | A15/A16/A19/A20; readiness, risk R7/R10/R12/R16 | p9 facts, legacy defaults/presets, both stores and migration gates retained; R16 remains before implementation authorization |

No material change to the Q3-reviewed semantic reading and no new blocker
was found. Q3-F04 resolves observable output ambiguity by the precise
reading Q3 requested; it does not add declaration-merging support.
No TypeScript oracle rerun or broad product test suite was warranted:
existing fixture semantics are unchanged, and no production/test code was
edited. Mechanical evidence and exact commands are recorded with the final
owner acceptance; the reproducible checker is
[analysis/owner-01-validate.py](analysis/owner-01-validate.py).
