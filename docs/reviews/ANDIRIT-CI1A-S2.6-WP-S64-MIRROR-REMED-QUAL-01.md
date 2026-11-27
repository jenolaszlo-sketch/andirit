# ANDIRIT-CI1A-S2.6-WP-S64-MIRROR-REMED-QUAL-01

**Independent qualification report · 28 November 2026**

## A — SUPPORTED

All mandatory remediation qualification requirements are independently
satisfied. The authority recommends owner acceptance of the remediation and
formal closure of the remediation work package. This does **not** close S6.4,
grant integration or publication authorization, or discharge DF-1,
F-S64E-03 or F-S64E-04.

## 1. Reviewed identities and baseline reconciliation

| Artifact | Identity |
| --- | --- |
| Entry baseline | `dda4358bc2588368efc6ab6a7bd774857d998026` |
| Implementation | `ec984bfe63e5fbe13047cee8920e00b634ab17b0` |
| Implementation tree | `8ec0eb7a5e647a171f54fa8969dadbb6c65da179` |
| Evidence payload | `b167fed057dfa2b2b1e4da1fde26826ff5f01ec9` |
| Payload tree | `bb9a0386fdfc4f4a3e9c744b1611c154f2ebca40` |
| Producer seal | `870b600acf1490337af9f483f8894a5e8166a90e` |
| Producer seal SHA-256 | `b473dd41742510d5676c1d8c033244afbc2c769dc52bf3941fa776769a6704f2` |
| Authorized checkout merge | `c549852450d950412ff7eb01f0916134f2539c2a` |
| Candidate hosted run | `46035752304` attempt 1 |
| Seal hosted run | `46036959497` attempt 1 |

The canonical workstream is the remote branch
`codex/andirit-s64-entry-01` (draft PR #8). Qualification used an isolated
clone and detached worktrees; the producer worktree was not modified.

Accepted main `99b04c0` inventory (hosted run `45773556513`, exact push SHA):
core 345, CSharp 372, LatticeDb 131, TreeSitter 157 = **1,005**. Candidate:
core 345, CSharp 373, LatticeDb 159, TreeSitter 157 = **1,034**. The delta is
exactly **29** new tests (20 remediation, 8 ownership, 1 linked fixture). No
accepted test was removed or skipped.

## 2. Independent seal verification

The authority reimplemented seal verification and checked Git objects directly.
It verified:

- entry, implementation, payload and seal commits and trees;
- payload parentage and the exact nine payload additions;
- **34 evidence entries** against Git blobs, byte lengths and SHA-256;
- **7 reviewed implementation inputs** against the implementation commit;
- the four affected-store binary hashes against `SHA256SUMS.txt`;
- the seal SHA-256 `b473dd…04f2`.

Verification passes from the untouched seal worktree. The producer seal was not
modified.

## 3. Hosted checkout-equivalence adjudication

Owner authorization for tree-equivalent synthetic merge evidence is established:

- run `46035752304` head SHA is `ec984bf…`;
- checked-out merge `c549852…` has parents `[99b04c0…, ec984bf…]`;
- merge tree `8ec0eb7a…` is exactly the implementation tree;
- `git diff c549852 ec984bf` is empty;
- all four jobs and six artifacts are attributed to the run;
- workflow, dependency, package and build inputs are identical (tree identity;
  no `.github`, project or version-file differences from `main`);
- the only commit-identity-sensitive inputs are the receipt binder
  (`GITHUB_SHA`, intentionally bound to the checkout) and informational version
  metadata, which tests strip before comparison.

No condition failed. This does not create a general checkout-equivalence policy.

## 4. Independent oracle and methodology

The oracle is derived from the unit definitions and the accepted contract, not
from candidate behavior:

- canonical nodes = distinct contributed node identities;
- canonical edges = distinct edge identities with canonical endpoints;
- physical multiplicity = one row per canonical fact;
- manifest ↔ physical correspondence in both directions;
- conflicts rejected, never first/last-writer resolved;
- ownership only for protocol-marked, endpoint-consistent manifests;
- ambiguous foreign relationships fail closed;
- recovery preserves authoritative identity/content, occurrences, unrelated
  sentinels and traversal parity.

A separate console harness (`harness/`) was compiled against both the baseline
and candidate trees. It uses only public store APIs and raw LatticeDbSharp rows.
Details: `oracle.md`, `methodology.md`.

## 5. Baseline negative controls

Independent harness on `dda4358` failed every defect-oriented scenario:

| Baseline scenario | Result |
| --- | --- |
| Shared logical node across contributors | 6 physical nodes / 2 edges instead of 2/1 |
| Shared logical edge across contributors | duplicate physical edges |
| Enabled reopen | cumulative physical growth |
| Disabled publication → re-enable | objects and manifests inconsistent; 8 nodes / 4 edges |
| Manifest → unrelated node | unrelated node deleted |
| Manifest → authoritative `HetuUnit` | authoritative rows changed |
| Unrelated incoming edge | no refusal; unrelated edge deleted |
| Unrelated outgoing edge | no refusal; unrelated edge deleted |
| Cross-repository reference | coverage/cardinality mismatch |
| Inconsistent manifest | duplicate residue |
| Protocol-marked orphan | not swept; manifest coverage incomplete |
| Missing / malformed / stale manifest | duplicate generations |
| Producer test suite on baseline | 14/28 LatticeDb failures; 1/1 linked-fixture failure |

The last row matches the producer claim exactly.

## 6. Candidate remediation results

The candidate passes all independent scenarios:

- shared nodes and edges collapse to one physical row per canonical identity;
- repeated enabled reopen is cardinality-stable;
- disabled publication removes all mirror rows and manifests;
- re-enablement rebuilds the canonical set;
- unrelated and authoritative objects are preserved;
- ambiguous incoming/outgoing foreign relationships are refused with full
  rollback and preserved state;
- missing, malformed, duplicate-valued, inconsistent and stale manifests are
  treated as owning nothing and reconstructed;
- protocol-marked unreferenced rows are swept while unmarked unrelated objects
  keep identity, properties and relationships;
- cleanup failure and cancellation leave the mirror and accepted publication
  unchanged.

## 7. Ownership adversarial results

Independent controls (not copies of producer tests):

| Control | Candidate result |
| --- | --- |
| Authoritative node falsely attributed | preserved; manifest untrusted; canonical rebuild |
| Unrelated unmarked node/edge | preserved with properties |
| Unrelated protocol-marked orphan | swept as derived state; no authoritative effect |
| Inconsistent manifest | untrusted; canonical; no dangling mirror edge |
| Cross-repository reference | other repository intact; shared identity isolated |
| Foreign incoming edge | refused; mirror unchanged; later repair/reopen succeeds |
| Foreign outgoing edge on second node | refused after would-be partial deletion; rollback restored all state |
| Missing / malformed / stale manifest | canonical rebuild; no authoritative loss |
| Sentinel properties and relationships | identity, note, number, tags and edge preserved |
| Cleanup failure / cancellation | accepted publication and mirror preserved |

Independent native probe: `DeleteNode` removes the node but leaves an incident
unrelated edge dangling (`deleteNodeCascades=false`). The authority audited every
`DeleteNode`/`DeleteEdge` call in `src/Andirit.LatticeDb`: they are confined to
the mirror cleanup paths, which now pre-check ownership and protect retained
endpoints.

## 8. Recovery and preservation

Producer-sealed stores, recovered independently:

| Store | Before | After | Occurrence | Second recovery |
| --- | --- | --- | --- | --- |
| graph-only | 43 nodes / 60 edges | 20 / 30 | 0 | 20 / 30 |
| integrated | 43 nodes / 60 edges | 20 / 30 | 1 | 20 / 30 |

For both: zero unmapped ids, complete manifest coverage, no duplicate logical
identities, authoritative rows byte-identical, injected unrelated sentinel
preserved, projection/native traversal identical.

An independently constructed affected store (baseline lifecycle, 10 nodes /
4 edges, one occurrence) recovered to 2 nodes / 1 edge, preserved the
occurrence, authoritative rows and sentinel, and was idempotent on a second
recovery. The committed affected store is under `results/affected-store/` with
SHA-256.

## 9. Regression and package qualification

Independently executed on Windows x64 (SDK 10.0.401):

| Project | Tests | Result |
| --- | ---: | --- |
| core | 345 | pass |
| CSharp | 373 | pass |
| TreeSitter | 157 | pass |
| LatticeDb | 159 | pass |
| **Total** | **1,034** | **pass, 0 skipped** |

All 29 new tests and all 19 inherited S6.3 named cases passed. Formatting
verification passed. Coverage: core 82.36/70.89, CSharp 95.15/88.00,
TreeSitter 89.85/75.45, LatticeDb 93.57/80.78 (line/branch), all above floors.

Independently audited hosted results (exact candidate tree): Windows x64, Linux
x64 and macOS ARM64 each report 1,034/1,034, zero skips, all 29 new tests and all
19 named cases passing; coverage floors exceeded. Package consumer: six packages
packed at a unique version, restored through a fresh cache and executed
successfully, including packed durable reopen and native mirror.

macOS is regression evidence only, not S6.5 platform certification.

## 10. Findings register and gate matrix

| Finding | Disposition |
| --- | --- |
| F-S64E-01 duplicate physical nodes/edges | **Remediated — independently verified** |
| F-S64E-02 disabled-publication orphans | **Remediated — independently verified** |
| F-S64R-01 manifest ownership misattribution | **Remediated — independently verified** |
| F-S64R-02 unrelated incident-edge deletion | **Remediated — independently verified** |
| F-S64E-03 DF-1 evidence gap | **Open** |
| F-S64E-04 corpus/oracle preparation | **Outstanding S6.4** |
| F-S64E-05 qualification execution | **Discharged for this remediation**; broader S6.4 remains |

No new blocking defect was found. `gate-matrix.md` records all 21 gates as pass.

## 11. Retained qualifications and limits

- S6.1, S6.2 and S6.3 remain closed within their accepted scopes.
- DF-1 remains open; F-S64E-03 remains open; F-S64E-04 remains outstanding.
- The P-03 erratum remains binding; current-query optimization is excluded.
- LatticeDbSharp remains `0.2.0`; release boundary remains `v0.2.0-preview.7`.
- Mirror rebuild/sweep remains O(physical mirror rows).
- Native engine-assigned ids remain unstable; only logical mapping is asserted.
- Hosted Linux/macOS results are independently audited, not locally executed.
- Hosted checkout is the owner-authorized tree-equivalent merge, not a literal
  push checkout.
- The independent harness uses synthetic fixtures; it does not discharge the S6.4
  D3 corpus/oracle obligation.
- Ambiguous foreign relationships fail closed and can block an enabled open until
  repaired; this is deliberate for unsupported corrupt state.

## 12. Evidence identities and reproducibility

Qualification evidence is on branch `qualification/s64-mirror-remed-qual-01`,
based on the producer seal `870b600`, and sealed by its own
`manifest.json`. The producer seal remains at `870b600` and is unchanged.
Reproduction commands are in the evidence README; the independent verifier is
`tools/verify_seal.py`, the hosted auditor `tools/qual_hosted.py`, and the local
parser `tools/qual_local.py`.

## 13. Recommended owner decision and successor

Recommended:

1. Accept the remediation qualification disposition **A — SUPPORTED**.
2. Formally close the remediation work package, not S6.4.
3. Keep DF-1, F-S64E-03 and F-S64E-04 open.
4. Authorize S6.4 re-entry to execute the D3 corpus/oracle obligation and DF-1,
   then release `ANDIRIT-CI1A-S2.6-WP-S64-IMPL-01` once re-entry is accepted.
5. Do not merge PR #8 or publish until a separate integration/publication
   decision.

Proposed successor: **`ANDIRIT-CI1A-S2.6-WP-S64-ENTRY-01-R1`** — S6.4 re-entry
after remediation, with the D3 corpus and independent oracle as the mandatory
subject.

The qualification authority grants no owner acceptance and does not close S6.4.
