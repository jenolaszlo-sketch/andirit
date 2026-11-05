# ADR-CI1-01-CI1A-S2.2-WP8-OWNER-01 — Qualified owner acceptance of the frozen R2 corpus integration, comparison, and F16 evidence

**Commission:** `ANDIRIT-CI1A-S2.2-WP8-OWNER-01` (formal WP8 owner acceptance; final-gate preparation).
**Project:** Andirit. **Recorded:** 2026-11-06. **Canonical branch:** `main`.
**Owner decision:** **`ACCEPTED WITH EXPLICIT QUALIFICATIONS`**.
**Gate/scope:** **WP8 ACCEPTED — frozen R2 corpus integration, independent semantic/identity/coverage comparisons, F16 evidence, and bounded reproducibility.**

> **Status: RECORDED — OWNER-AUTHORIZED.**
> **Owner decision: `ACCEPTED WITH EXPLICIT QUALIFICATIONS`.**
>
> This accepts the WP8 integration/comparison evidence for its bounded scope. It does
> **not** grant final S2.2 acceptance (`G-ACCEPT` remains separately commissioned), does
> not authorize S2.3/S2.4/S2.5, does not close stage-wide F16 authority, and does not
> authorize publication.

This decision is additive documentation. It amends no prior decision, reviewer artifact,
producer test, frozen corpus/oracle, or production source.

---

## 1. Owner decision, gate, and scope

| Field | Value |
| --- | --- |
| Owner decision | `ACCEPTED WITH EXPLICIT QUALIFICATIONS` |
| Gate/scope | WP8 — frozen R2 corpus integration, comparison, F16 evidence, bounded reproducibility (closed) |
| Accepted capability | Authenticated corpus integration through the real S2.2 production chain with independent frozen expectations, full 50/51/53 accounting, declared-profile identity checks, refusal/coverage verification, F16 traceability, and same-host byte-identical determinism |
| Excluded | Final S2.2 acceptance; S2.3/S2.4/S2.5; cross-platform independent qualification; arbitrary-repository completeness; publication |
| Next stage | `ANDIRIT-CI1A-S2.2-G-ACCEPT-OWNER-01` (separately commissioned; entry contract §13) |

## 2. Verified baseline and ancestry

| Role | Commit | Tree |
| --- | --- | --- |
| Frozen R2 authority | `e0d911055f4cc9bdbf9aa4cc338f5cc007737a22` | `ba9338d74bf59c584bef340dbda49ed46eb1c5d9` |
| G-CONFIRM acceptance | `812584dc56e11983c3ea7de8977fd1cd74ff0297` | `7167557024e1602c81b5e6b169455c0ad10a5864` |
| WP3 owner | `4fdc23cfd3d760639dd653ad92bb204376a08c05` | `55b5e990cfa16606d7ba09752d18d360bf01db08` |
| G-MINT owner | `d3da2e009154402a92275874ac4bb94426b97df3` | `029299bf277bc99212ea2ded311c0ec54f70fa09` |
| WP4 owner | `97c88e58851820468cd21ee66d7a8ba460baf23a` | `27cda08808b4b56ccd6524b82b8315666636bd8d` |
| WP5 owner | `f1e7f273750b41413621db869542e2415ca24345` | `794d3a3d2f498856cba18ed5633fb41407ee92ca` |
| WP6 owner | `2b35cd6c7577508b04e75b5bf34d23cbc261f308` | `c3706eb919d1455bd12746b22336d956e3e0c8ae` |
| WP7 owner | `7cef15aaf45e7c203b28541ed1b24bf27a3bb3b9` | `f43d25af2be04d1e439acab6e7b28d0b6fd8d94a` |
| WP8 harness/tests | `76ac976e4c42fa83f6e1561b4a19c6a5ebc0b06f` | `ac1c039f534da5c85bb49ce7a4c490894b0cb2ab` |
| WP8 implementation evidence | `db5d324febd642695657186cca6f5ac4c026fb55` | `a9d14a104c7a2dcac2b4d618749d67cd88b8ac42` |
| WP8-Q1 independent qualification | `a257c3952a0ac06d583a4dbf554612df459d66b1` | `5db5164302fa53db238dfa33b9f02f1619666944` |

Consecutive chain; `a257c395^ = db5d324`; working tree clean; one worktree; no stashes;
`origin/main` = `443c192`; `main` **14 ahead / 0 behind**; nothing pushed. **No equivalent
or superseding WP8 owner decision exists** (`docs/decisions/` contains no `wp8-owner`
record).

## 3. Owner verification of the qualification evidence

Re-executed at recording HEAD (logs in
`docs/reviews/CI1A-S2.2-WP8-OWNER-01-evidence/`):

| Verification | Result |
| --- | --- |
| WP8-Q1 independent probes | **10 / 0 / 0** |
| WP8 integration producer tests | **9 / 0 / 0** |
| WP7-Q1 / WP7 producer | 19 / 12 — green |
| WP6-Q1 / WP6 matrix | 30 / 20 — green |
| WP5-Q2 / WP5-Q1 | 22 / 16 — green |
| WP4-Q1 / G-MINT-Q1 | 13 / 12 — green |
| Whole solution | **789 / 0 / 0** (43 + 157 + 345 + 244) |
| Frozen R2 oracle | **185/185**; output `97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9` == accepted; expected `274A7AAE9EB6724879D619416E449C52759B44085D7B718DAD3434B15387CBBF` unchanged |
| `dotnet format` / Release build | exit 0 / 0 warnings, 0 errors |
| Production/decisions/planning drift | `git diff 7cef15a..HEAD -- src docs/decisions docs/planning` empty |

Both determinism claims are reproduced: the producer canonical payload `3f08abc6…`
(12 108 bytes) and the independent reviewer payload `00a46e4e…` (11 821 bytes) match
across separate processes.

## 4. Formal corpus acceptance

- **Authentication:** 11 C# files byte-verified against `fixtureManifest`; frozen expected
  file hash `274A7AAE…`; project topology and parse/option configuration independently
  reproduced.
- **§6.6 quantitative closure:** 11 files, 53 invocations, 21 method references,
  5 overloaded methods, 5 namespaces, 5 projects, 52 cross-project invocations —
  independently recomputed and all above the accepted minima. These are **corpus and
  integration measurements**, not universal production-extraction guarantees.
- **Frozen semantics:** all 50 frozen invocation expectations match (spans, documentation
  ids, cross-project/generic/overload/reduced-extension/explicit-interface/partial forms);
  expectations originate from frozen authority, not the production extractor.
- **Exact supported fact set:** 53 corpus invocations = **51 supported facts + 2
  unsupported invocation forms** (delegate, local function). The 50 frozen rows are a
  strict subset; the extra is the real unmarked `PartialType.Right()`. Set equality over
  stable `(path, span, constructed documentation identity)` tuples was established, not
  numeric equality alone.
- **Deferred categories:** 21 method references and the deferred forms mint no trusted
  usage; the 21-reference corpus minimum is satisfied at corpus level and is **not**
  represented as 21 produced `usage4` facts.

## 5. Identity/provenance, refusal/coverage acceptance

- Within the declared **production profile** and the callers' S-01 inputs, the anchors
  reproduce exactly: S01 `asmctx3 = ece8e803…`, `usage4 = 1027f062…`, Core
  `compctx1 = afcb56d0…`. The frozen fixture-profile `Core compctx1 = 942a8e58…` remains a
  different declared profile and is not compared cross-profile.
- Verified chain guarantees hold: source↔compilation correspondence, R2-faithful
  `compctx1`, current `compctx2`, exact live defining association, mandatory recorded
  context, frozen `asmctx3`/`usage4`, cross-project binding, stale/missing/substituted
  refusal, and no structural-identity elevation.
- Negatives: `Ambiguous=1`, `Unresolved=4`, `ConditionalInactive=1`, `External=0`,
  **0 trusted facts**; `m.Static(1)` (CS0176) refused. App `Produced`, Negatives
  `Produced` with zero facts, Solo verified complete-empty; mixed scope `Partial`,
  incompatible universes refused.
- **`Produced` means completion of the declared bounded analysis, not that every
  syntactically relevant occurrence produced a trusted fact.** Zero facts are not
  evidence of absent candidates.

## 6. WP8-Q1 qualification dispositions

| ID | Disposition |
| --- | --- |
| **Q1-WP8-1** — unmarked `PartialType.Right()` valid fact, accounted | **ACCEPTED QUALIFICATION.** The frozen-marked set and the full supported fact set are distinguished; frozen expectations untouched. |
| **Q1-WP8-2** — production-profile anchors bound to profile and S-01 digests | **ACCEPTED QUALIFICATION.** No universal cross-profile equality; component-digest provenance remains S-01-limited. |
| **Q1-WP8-3** — dynamic → `Unresolved`; external refused via provenance (F4) | **ACCEPTED QUALIFICATION.** The accepted coverage contract does not mandate a distinct observable `Dynamic`/`External` classification here; no trusted fact is required from unresolved dynamic binding. |
| **Q1-WP8-4** — corpus-declared three-reference profile | **ACCEPTED PROFILE QUALIFICATION.** Environment: Windows x64, .NET 10.0.12, Roslyn 5.9.0, C# 14, three captured framework references per the accepted corpus construction; not generalized to arbitrary metadata-reference combinations. |
| **Q1-WP8-5** — canonical payload omits per-negative rows | **ACCEPTED WITH EXPLICIT EVIDENCE-BOUNDARY QUALIFICATION.** (1) canonical production output is byte-identical across tested processes/inputs; (2) negative/refusal semantics are independently supported by executable probes; (3) the payload is not a complete per-negative serialization, and its SHA-256 does **not** authenticate every negative case by itself. §6.7 requires raw logs/TRX/evidence, which exist separately; no conflict identified. |
| Inherited | Preserved: S-01; G1/F1 metadata binding; recorded-`compctx2` enforcement; F09 heuristic (F2); F3 overlap; G-Q1A2; G-Q1A7; incompatible-universe refusal. |

## 7. F16 evidence disposition

All 14 accepted rows carry the nine required fields, map to real production surfaces,
positive and adversarial assertions, environments, committed evidence, and **closure
authority `IR+O`**. WP8 independent review is complete and this owner decision supplies
the WP8-specific owner decision. **Stage-wide F16 closure authority remains for the final
S2.2 `G-ACCEPT` commission**; producer/reviewer execution alone does not close the rows.

## 8. Accepted WP8 capabilities (bounded)

1. Authenticated S2.0/R2 corpus integration through the real production chain.
2. Satisfied §6.6 quantitative minima (corpus-level).
3. Independent frozen-oracle comparison boundary (mutation-challenged).
4. Exact 50/50 frozen invocation-expectation agreement.
5. Exact 51-fact supported-set equality (not merely equal counts).
6. Complete 53-invocation reconciliation.
7. Correct bounded unsupported/deferred behavior.
8. Verified `compctx1`/`compctx2`/`asmctx3`/`usage4` integration within declared profiles.
9. Declared-profile-only identity comparisons.
10. Stale/fabricated/missing/substituted-context refusal.
11. Negative invocation classification and refusal.
12. Verified zero-fact and complete-empty coverage distinctions.
13. WP6 coverage compatibility (mixed-scope `Partial`, universe refusal).
14. WP7 in-memory stale-publication compatibility.
15. F16 evidence traceability.
16. Byte-identical canonical output in the demonstrated profile.
17. Frozen source and expected-file integrity.
18. Preservation of WP3–WP7 accepted authority.

## 9. Explicit non-acceptance

Not claimed: arbitrary-repository C# extraction completeness; all 53 invocations as
trusted facts; the 21 method references as emitted `usage4`; universal cross-profile
identity equivalence; toolchain-independent bytes; exact F09 counts for arbitrary source;
independent verification of all S-01 digest provenance; native persistence/reopen or
recovery; final S2.2 acceptance; S2.3/S2.4/S2.5 authorization; cross-platform WP8
qualification; complete per-negative serialization in the canonical payload; general
repository completeness; publication/release.

## 10. Platform, profile, and reproducibility limits

Windows x64 only (SDK `10.0.401`, runtime `10.0.12`; corpus toolchain Roslyn 5.9.0,
C# 14). Producer-profile digests are host/input-bound; same-host repeated/separate-process
determinism is demonstrated, cross-machine byte equality is not. Reviewer probes are
Windows-local; hosted CI results are not independent WP8 cross-platform qualification.

## 11. Remaining S2.3 and later obligations

- **S2.3:** native persistence/reopen; first gate = reopen stale-refusal (Option A §5.2
  test 8 second half); **NEW-01** recovery remediation is mandatory before any S2.3
  recovery reliance (`ADR-CI1-01-CI1A-S2-FOUNDATION-RECOVERY-01`, separately authorized).
- **S2.4/S2.5:** continuation surface and integrated independent S-01 proof.
- **Publication/release:** separately controlled; nothing pushed.

## 12. Historical numbering reconciliation

The execution-readiness handoff uses an older draft numbering (its "WP9" is the accepted
§6.2 **WP8** integration/comparison package; its other WP labels also differ from the
accepted §6.2 rows). The accepted §6.2 numbering governs. **There is no separate WP9
work package and no WP9 gate.**

## 13. Final S2.2 `G-ACCEPT` entry contract

Proposed commission: **`ANDIRIT-CI1A-S2.2-G-ACCEPT-OWNER-01`** (name derived from the
accepted `G-ACCEPT` gate in the foundation owner decision and the execution-readiness
handoff). Gate definition (authoritative): *"S2.2 acceptance (Q-F2, Q-F3, NEW-04, S-01,
mapping review satisfied)"*. The commission must evaluate the **entire S2.2 stage**, not
repeat WP8-Q1.

| ID | Requirement | Authority | Existing evidence | Current disposition | Remaining action | Acceptance condition | Responsibility | Exclusions |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| **GA-1** | Work-package closure chain | §6.2; owner decisions | G-CONFIRM `812584d`; WP3 `4fdc23c`; G-MINT `d3da2e0`; WP4 `97c88e5`; WP5 `f1e7f27`; WP6 `2b35cd6`; WP7 `7cef15a`; WP8 this record | All accepted with qualifications | Verify recorded acceptances + reconciliations | Every package accepted/qualified; numbering reconciled | Owner + IR | Inventing WP9 |
| **GA-2** | Identity/mapping authority | Option A §5; mapping contract §1–3; G-MINT | G-MINT-Q1 `1c382d6`; WP4-Q1 `6ecf996`; WP5-Q2 `f10b472`; WP7-Q1 `42ef163`; WP8-Q1 `a257c395` | Qualified | Verify formulas, `compctx2` isolation, live association, recorded-context enforcement, no churn, stale refusal | All satisfied within qualifications | IR+O | New identity semantics |
| **GA-3** | Q-F2 / Q-F3 / A-01 | Register rows 90–91, 94; S21Q1; stage-2 | `S2_2FoundationBoundaryTests` (`AmbiguousBomlessEncoding`), `S2_2MetadataRemediationTests` (`UnsupportedMetadataReference`), A-01 symbol-hardening tests | Corrected and tested | Verify dispositions and evidence | Encodings/metadata/symbol hardening accepted | IR+O | Re-opening without evidence |
| **GA-4** | NEW-04 closure | Register row 82; WP6-OWNER | `2b35cd6`; WP6 matrix/Q1 | `CLOSED WITH EXPLICIT QUALIFICATIONS` | Verify closure and limits | Preserve disjoint-contribution/scope/universe limits | Owner | Reopening NEW-04 |
| **GA-5** | S-01 boundary | Register row 171; S2.1/S2.2/S2.5 allocation | WP4/WP5/WP7/WP8 evidence; S-01 carried limitations | S2.2 semantic correspondence qualified; full integrated proof S2.5 | Verify the S2.2 portion only | S2.2 correspondence accepted; S2.5 obligation preserved | IR+O | Importing S2.5 work into S2.2 (or erasing the qualification) |
| **GA-6** | F16 / NEW-02 | Register row 70; F12A §8; G-CONFIRM | 14 rows confirmed by `812584d`; WP8 traceability | Rows evidenced; stage-wide closure pending | Verify combined F16 authority and no circular expected-value generation | Stage-wide F16 closure authority recorded | IR+O | Declaring closure from producer tests |
| **GA-7** | A-02 and standing constraints | Register row 95; §6.3 | Scope/universe tests; WP8 scope-mismatch probe | Accepted boundary | Verify no `analysisscope1`-as-inventory misuse; D3/D4/D5 intact | Boundary preserved | Owner | Scope-digest provenance claims |
| **GA-8** | Option A durability boundary | Entry acceptance §3.5; §6.2 | In-memory association evidence; S2.3 explicit deferral | S2.2 extraction-time enforcement accepted; native reopen deferred | Verify deferral and escalation requirement | Boundary recorded; S2.3 first-gate obligation intact | Owner | Claiming native reopen safety |
| **GA-9** | NEW-01 non-reliance | Register row 79 | No S2.2 reliance; recovery remediation tracked separately | Not an S2.2 dependency | Verify S2.2 does not rely on unqualified recovery | Non-reliance confirmed; NEW-01 remains an S2.3 prerequisite | Owner + IR | Implementing NEW-01 in G-ACCEPT |
| **GA-10** | Corpus and reproducibility | §6.6/§6.7; this decision | WP8-IMPL + WP8-Q1 + this owner decision | Accepted with Q1-WP8-5 payload boundary | Verify minima, independence, F16, reproducibility, immutability | All §6.6/§6.7 conditions satisfied | IR+O | Cross-profile/cross-machine byte claims |
| **GA-11** | Platform/publication boundaries | §6.2 EXCL; prior platform records | Windows x64 evidence; hosted CI solution runs | Qualified | Distinguish evidence kinds | Explicit platform limitation recorded | Owner | Promoting hosted CI to independent qualification |
| **GA-12** | Final disposition | §6.9; this contract | This preparation | Pending | Decide the stage | One of: ACCEPTED / ACCEPTED WITH EXPLICIT QUALIFICATIONS / ACCEPTANCE DEFERRED / REMEDIATION REQUIRED, with unresolved requirements and stage allocations | Owner | Granting S2.3 or later |

No mandatory gap is identified for the WP8 portion; the listed remaining actions are
stage-level verification obligations and accepted downstream deferrals (S2.3/NEW-01/S2.5),
not WP8 deficiencies.

## 14. Remaining obligations summary

- **Final S2.2 `G-ACCEPT`** (separately commissioned) — evaluates GA-1…GA-12.
- **S2.3** native persistence/reopen; NEW-01 remediation before recovery reliance.
- **S2.4/S2.5** continuation surface and integrated S-01 proof.
- **Cross-platform** independent qualification; **publication** — separately controlled.

## 15. Final owner decision

**`ACCEPTED WITH EXPLICIT QUALIFICATIONS`** — WP8 is formally accepted for its bounded
frozen R2 corpus integration, independent comparison, F16 evidence, and reproducibility
scope. All accepted §6.2 work packages are now eligible for the separately commissioned
final S2.2 `G-ACCEPT` evaluation. **This decision does not grant final S2.2 acceptance.**

---

Recorded at HEAD `a257c3952a0ac06d583a4dbf554612df459d66b1` (tree
`5db5164302fa53db238dfa33b9f02f1619666944`). This record and its evidence are the only
changes in their commit; the decision commit/tree identities are recorded in the handoff.
