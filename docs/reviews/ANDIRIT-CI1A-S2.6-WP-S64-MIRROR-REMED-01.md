# ANDIRIT-CI1A-S2.6-WP-S64-MIRROR-REMED-01

**27 November 2026 · A. REMEDIATION IMPLEMENTED — READY FOR INDEPENDENT QUALIFICATION**

Proposed disposition only. This report does not self-qualify, grant owner
acceptance, or reopen S6.1–S6.3. Independent qualification and the later owner
gate remain separate decisions.

## Scope

Targeted producer remediation of the two mirror defects accepted in the S6.4
entry assessment `B. ENTRY CONDITIONALLY READY`:

- **F-S64E-01** duplicate physical mirror nodes and accumulation across enabled reopen;
- **F-S64E-02** orphaned physical mirror objects after disabled publication and re-enablement.

The authoritative command log, publication, occurrence authority and public
query surfaces are unchanged. The mirror remains derived, reconstructable state.
No LatticeDbSharp upgrade, public API change, release-tag change, NuGet
publication, or promotion to `main` was performed.

## Verified baseline

| Subject | Verification |
| --- | --- |
| Published main | `99b04c0ebb5dfa43065581bb6489fe2468d9a5aa`, tree `3c0288c533db80b7caf769d40cd33a9b567024c6` |
| Entry branch | `codex/andirit-s64-entry-01` |
| Entry evidence seal | `dda4358bc2588368efc6ab6a7bd774857d998026`, SHA-256 `7530597bae147c9abba83be1e9aef8d806d08faca6604b08daf91b180d076701` |
| Entry seal verification | `python …/WP-S64-ENTRY-01-evidence/seal.py verify HEAD` reports 13 payload files and 58 reviewed inputs |
| Dependency | `LatticeDbSharp` `0.2.0` |
| Release boundary | `v0.2.0-preview.7` |
| Local toolchain | Windows x64, SDK 10.0.401, runtime 10.0.12 |

## Confirmed root causes

Both entry hypotheses were reproduced against the unmodified seal and confirmed
in producer scope. The defects are in Andirit, not LatticeDbSharp or the native
engine.

### F-S64E-01 — per-contribution physical creation with a last-writer manifest

`NativeGraphMirror.Rebuild` previously iterated every published index unit and
called `CreateNode`/`CreateEdge` for every contribution. The manifest dictionary
was keyed by logical identity, so a second unit contributing the same identity
overwrote the first native id. The first physical row became untracked and could
never be deleted by a later rebuild, whose cleanup enumerated only the manifest's
last-writer ids. Edges had the same defect; an edge incident to an untracked
duplicate also survived because cleanup walked adjacency only from mapped nodes.

The raw baseline store demonstrates the residue: 43 physical nodes for 20
distinct logical identities, 60 physical edges, and only 30 manifest edge
entries. Twenty-three physical node ids and thirty physical edge ids are
unreferenced. The real linked-file fixture shows 21 physical nodes for 20 logical
ids at the initial publication and 22 after the first enabled reopen.

### F-S64E-02 — manifest-only deletion on disabled publication

The disabled completion path called only
`DeleteMany(txn, MirrorLabel, [repositoryId])`. The `HetuGraphNode`/edge rows
remained. A later enabled open found no manifest, rebuilt without an inventory,
and created a second full set beside the old rows. The reproduced sequence was
21 → 22 → 22 (manifest now 0) → 43 physical nodes.

## Ownership-safety findings (same mirror scope)

The mandated ownership review reproduced two further producer defects before
sealing:

- **F-S64R-01** — a structurally valid manifest could name a non-mirror native
  node, including an authoritative `HetuUnit` command-log record. The cleanup
  deleted it. Confirmed: authoritative row count fell from 4 to 3.
- **F-S64R-02** — cleanup deleted any edge incident to a manifest-owned node,
  including an unrelated edge. There was no refusal for ambiguous ownership.

Both are remediated inside the authorized mirror scope:

- a manifest is ownership evidence only if every entry resolves to a
  protocol-marked mirror object and referenced edges connect nodes named by the
  same manifest;
- untrusted manifests own nothing and are never used for deletion or retention;
- a non-mirror incident edge triggers an explicit ownership refusal and
  transaction rollback;
- retained edges protect their endpoint nodes.

Native probing established that `DeleteNode` does **not** cascade: it leaves
incident edges dangling. Cleanup therefore cannot rely on the engine to protect
unrelated relationships; it must refuse before deleting. Full controls and
before/after content-and-identity checks are in
[`ownership-safety.md`](ANDIRIT-CI1A-S2.6-WP-S64-MIRROR-REMED-01-evidence/ownership-safety.md).

## Remediation

### Canonical facts, not unit re-aggregation

The mirror now consumes the authoritative materialized projection for the
repository through the new internal
`InMemoryCodeGraphStore.GetMaterializedFacts`. That view already applies the
established semantics: deterministic plugin/unit ordering, equivalence-only
merging of shared identities, explicit rejection of conflicting contributions,
and endpoint visibility. The mirror therefore creates exactly one physical node
and one physical edge per canonical logical identity. No first-writer or
last-writer deduplication is introduced in the mirror.

### Explicit ownership and complete correspondence

Each rebuild:

1. deletes every physical row owned by the repository's own persisted manifest;
2. removes protocol-marked mirror rows that **no** manifest in the database
   references;
3. creates the canonical node and edge set once; and
4. writes a complete one-to-one manifest in the same transaction.

A disabled publication now deletes the manifest-owned physical rows, removes the
manifest, and performs the same orphan sweep before commit. A repository with no
readable manifest cannot own rows; its rows are therefore unreferenced derived
state and are reconstructable from the command log.

### Repository isolation

The sweep never deletes a native id referenced by another repository's manifest.
A shared logical identity in two repositories remains two physical rows with two
distinct manifest owners. Disabling one repository removes only that
repository's rows.

### Compatibility

The persisted manifest shape (`Nodes`, `Edges` as `logical id → native id`) and
the mirror labels/property names are unchanged. Existing manifests are readable;
malformed, incomplete, duplicate-valued or missing manifests are detected and
rebuilt from authoritative facts rather than trusted.

## Before and after

Real linked-file fixture, both graph-only and occurrence-integrated lanes:

| Physical census | Baseline initial | Baseline enabled reopen | Baseline disabled publish | Baseline re-enable | Candidate initial | Candidate reopen | Candidate disabled publish | Candidate re-enable |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Nodes | 21 | 22 | 22 | 43 | 20 | 20 | 0 | 20 |
| Nodes with linked logical id | 2 | 3 | 3 | 5 | 1 | 1 | 0 | 1 |
| Manifests | 1 | 1 | 0 | 1 | 1 | 1 | 0 | 1 |
| Occurrence facts (integrated) | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 |

Affected-store recovery (the baseline-produced final store):

| Census | Before recovery | After recovery | Second recovery |
| --- | ---: | ---: | ---: |
| Physical nodes | 43 | 20 | 20 |
| Distinct logical nodes | 20 | 20 | 20 |
| Physical edges | 60 | 30 | 30 |
| Manifest node entries | 20 | 20 | 20 |
| Manifest edge entries | 30 | 30 | 30 |
| Unreferenced native node ids | 23 | 0 | 0 |
| Unreferenced native edge ids | 30 | 0 | 0 |
| Occurrence facts (integrated) | 1 | 1 | 1 |
| Unrelated native sentinel rows | 1 | 1 | 1 |

## Regression campaign

Twenty-eight new LatticeDb tests and one end-to-end CSharp linked-fixture test
cover the required matrix, including direct physical node and edge inventory
reconciliation, ownership-safety adversarial controls, multi-repository
isolation, transactional rollback, cancellation, occurrence parity, recovery and
negative controls. The core remediation tests were copied unmodified into a
disposable worktree at the unmodified entry seal: 10 of 19 LatticeDb tests and
the linked-fixture test fail there with the original duplicate/accumulation
symptoms. All new tests pass on the candidate.

Full solution: 1034 tests passed, 0 failed. Coverage floors retained:
core 82.36% line / 70.89% branch (floor 55), CSharp 95.15% / 88% (floor 75),
TreeSitter 89.85% / 75.45% (floor 60), LatticeDb 93.57% / 80.78% (floor 70).
Repository formatting verification passes.

Hosted exact-revision CI results are recorded in
[`hosted-ci.md`](ANDIRIT-CI1A-S2.6-WP-S64-MIRROR-REMED-01-evidence/hosted-ci.md).

## Retained qualifications and residual risks

- Mirror rebuild and orphan sweep are O(physical mirror rows). The preview mirror
  is derived and correctness-first; current-query computational optimization
  remains excluded by the S6.3 P-03 erratum.
- Engine-assigned native ids remain unstable across rebuilds, as permitted.
- Multi-process writers are serialized by the existing native file lock; the
  ownership proof assumes a manifest and its rows are written in one
  transaction, which the implementation enforces.
- Aggregate counts alone are never accepted as evidence; the oracle checks the
  physical multiset and complete manifest-to-row correspondence.
- This commission does not perform S6.4 full D3 corpus qualification, S6.5
  platform certification, or owner acceptance.
- S6.1–S6.3 remain closed within their accepted scopes. DF-1 remains open. The
  S6.3 P-03 erratum remains binding.

## Independent qualification handoff

The proposed successor commission is
[`ANDIRIT-CI1A-S2.6-WP-S64-MIRROR-REMED-QUAL-01`](ANDIRIT-CI1A-S2.6-WP-S64-MIRROR-REMED-01-evidence/qualification-proposal.md).
It must independently derive the expected ledgers before observing candidate
results, verify the seal from Git objects, replay the native sequence on Windows
x64 and genuine GitHub-hosted Linux x64, and perform its own adversarial
controls.
