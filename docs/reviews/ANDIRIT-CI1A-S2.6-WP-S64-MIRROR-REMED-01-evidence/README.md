# S64-MIRROR-REMED-01 evidence

This package supports the
[remediation report](../ANDIRIT-CI1A-S2.6-WP-S64-MIRROR-REMED-01.md). It binds
the baseline reproduction, root-cause confirmation, implementation bytes,
physical inventories, recovery receipts and regression results to Git objects.

## Contents

| Path | Purpose |
| --- | --- |
| `baseline-reproduction/baseline-graph-only.json`, `baseline-integrated.json` | Entry observer receipts from the unmodified seal `dda4358…` |
| `baseline-reproduction/candidate-graph-only.json`, `candidate-integrated.json` | Same observer on the remediation candidate |
| `baseline-reproduction/negative-controls.txt` | The new tests copied unmodified into a disposable baseline worktree; exact failures at the original defects |
| `affected-stores/` | The two baseline-produced stores (`.ltdb` and `.ltdb-wal`) and `SHA256SUMS.txt` |
| `recovery/` | Detect, recover, integrated-recover and second-recovery receipts for disposable copies of the affected stores |
| `mirror-recovery/` | Standalone recovery observer and project |
| `root-cause.md` | Confirmed defect mechanism and ownership |
| `oracle.md` | Independent expected-graph oracle definition |
| `findings-remediation-matrix.md` | Contract invariant → implementation → test → evidence |
| `compatibility-recovery.md` | Affected-store detection, ownership proof and recovery boundaries |
| `transactional-consistency.md` | Atomicity, cancellation and failed-publication evidence |
| `repository-isolation.md` | Manifest ownership and cross-repository proof |
| `inherited-impact.md` | S6.1–S6.3 replay and coverage impact |
| `regression-campaign.md` | Required scenario → named test mapping |
| `hosted-ci.md` | Exact-revision hosted CI identities |
| `qualification-proposal.md` | Proposed independent qualification commission |
| `reviewed-inputs.json` | Implementation source/test/CI blobs bound to the exact implementation commit |
| `manifest.json` | Self-excluding evidence seal |

## Reproduce the baseline observation

```powershell
$project = 'docs/reviews/ANDIRIT-CI1A-S2.6-WP-S64-ENTRY-01-evidence/mirror-observer/ObserveMirror.csproj'
$scratch = Join-Path ([System.IO.Path]::GetTempPath()) ('andirit-s64-remed-' + [guid]::NewGuid())
New-Item -ItemType Directory -Path $scratch | Out-Null
dotnet build $project -c Release --artifacts-path $scratch -p:NuGetAudit=false
foreach ($mode in 'graph-only','integrated') {
    dotnet (Join-Path $scratch "bin/ObserveMirror/release/ObserveMirror.dll") (Join-Path $scratch "$mode.ltdb") $mode
}
```

Run the baseline receipts from a checkout of `dda4358…`; run the candidate
receipts from the implementation revision. A fresh database is required.

## Reproduce recovery

Copies are required because recovery mutates the store. The committed affected
stores must remain byte-identical.

```powershell
$project = 'docs/reviews/ANDIRIT-CI1A-S2.6-WP-S64-MIRROR-REMED-01-evidence/mirror-recovery/RecoverMirror.csproj'
$scratch = Join-Path ([System.IO.Path]::GetTempPath()) ('andirit-s64-recover-' + [guid]::NewGuid())
New-Item -ItemType Directory -Path $scratch | Out-Null
dotnet build $project -c Release --artifacts-path $scratch -p:NuGetAudit=false
Copy-Item 'docs/reviews/ANDIRIT-CI1A-S2.6-WP-S64-MIRROR-REMED-01-evidence/affected-stores/graph-only.ltdb*' $scratch
dotnet (Join-Path $scratch 'bin/RecoverMirror/release/RecoverMirror.dll') (Join-Path $scratch 'graph-only.ltdb') recover-unrelated
```

`recover-unrelated` injects an unrelated native sentinel node into the copy and
asserts it survives. `detect` reports the defective census without modifying the
store. `recover` performs the normal recovery.

## Independent oracle

The regression tests and recovery observer derive expectations from fixture
inputs and the governing aggregation rules, not from mirror output. They compare:

- the physical node multiset with the distinct canonical logical node set;
- the physical edge multiset with the distinct canonical edge set whose
  endpoints exist;
- every manifest entry with a physical row of the same logical identity;
- every physical mirror row with exactly one manifest owner;
- native and projection traversal result sequences; and
- authoritative command-log, occurrence and unrelated native data before and
  after recovery.

## Seal verification

Use from the repository root after checking out the seal commit:

```text
python docs/reviews/ANDIRIT-CI1A-S2.6-WP-S64-MIRROR-REMED-01-evidence/seal.py verify HEAD
```

The seal binds every evidence file to its committed Git blob, byte length and
SHA-256, binds the implementation commit's source/test/CI files through
`reviewed-inputs.json`, and verifies the entry-seal ancestry. It also verifies
that binary stores are byte-identical to the sealed objects.

## Limits

The entry observation and the local candidate campaign are Windows x64. Hosted
Linux x64 and macOS results are recorded separately in `hosted-ci.md`. Platform
certification remains under S6.5. Engine-assigned native ids may differ between
generations; only logical mapping and physical multiplicity are asserted.
