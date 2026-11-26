# Independent qualification evidence

This package supports the [formal independent requalification](../ANDIRIT-CI1A-S2.6-WP-S63-QUAL-01-R1.md) of exact tested revision `a96c19a844f8ea1719508198b47df23c4e203571`. The final manifest binds the report and all payload files to their containing Git objects and the commissioned revisions.

| Member | Purpose |
| --- | --- |
| `findings.md`, `gate-matrix.md`, `owner-decision-p03.md` | Dispositions, authority reconciliation and pending owner clarification |
| `local-receipts.zip` | Independent checkout/SDK/OS receipts; commands, exits, TRX, coverage and process output; R1 and R2 exact original Q1 harness; 20 dedicated D-07 runs; all prepared runs; build and clean worker receipts; both expected failing injections; additional diagnostic controls |
| `hosted-receipts.zip` | Original REST run/jobs/artifact metadata and hosted logs; original Ubuntu R2 result ZIP and R1 coverage ZIP; independently extracted result and coverage trees for all three R2 platforms; historical successful and failed workflows |
| `hosted-verification.log` | Exact names, raw artifact hashes, per-project counts and coverage line/branch fractions, distinct process identities, technology and verifier controls |
| `provenance.log` | Exact comparisons, unaltered originals, manifest/blob IDs and history, historical failed seal and recovery hashes |
| `execution-inventory.log` | Independent local TRX counts and identity checks, dedicated process-run receipts, platform/RID and pinned native asset hashes |
| `contract-scan.py`, `contract-scan.log` | Wider enumeration of owner, plans, contracts and changed implementations, with frozen-R1/R2 blob binding |
| `campaign.py`, `hosted.py`, `provenance.py`, `inventory.py`, `additional/` | Independent collection, replay and review source. The Q1 reproducers and prepared probes are drawn from exact historical Git objects. |
| `seal.py` | Emit a self-excluding manifest from a payload commit, or verify a committed manifest exhaustively |
| `manifest.json` | Evidence and formal report: committed bytes, paths, size, Git blob ID, SHA-256, subject and source-revision bindings |

The ZIP wrappers preserve raw receipt bytes. They contain expected failures as evidence and include generated artifact metadata from GitHub. Historical R1 recovered artifact bytes remain distinguishable from newly generated Windows output and newly collected R2 hosted evidence.

## Replay

Use separately prepared clean detached checkouts of the commissioned R1 and R2 and an untouched Q1 Git object store. Select the pinned SDK and RID and allow the dependency source and GitHub artifact API. The earliest LatticeDb replay must occur before any build of the solution in its R2 checkout. `campaign.py` records every invocation and expectation, parks/restores ignored worker outputs for the negative controls, and exports the exact historical projects. The original Q1 reproducer's R2 exit 1 is expected from its explicit late-success assertion. The direct additional C# probes build against the exact R2 projects and their release outputs. Their native-open timing and injected publication fault use deliberately controlled instrumentation. `hosted.py` independently parses GitHub artifacts, then runs the producer verifier on the original and independently altered copies. `provenance.py` reads the relevant original repository object stores for local historical refs and the separately acquired GitHub artifact. The scripts accept the source checkouts and evidence directory as explicit arguments, where applicable.

Verify the committed evidence from the root of this qualification worktree with:

```text
python docs/reviews/ANDIRIT-CI1A-S2.6-WP-S63-QUAL-01-R1-evidence/seal.py verify HEAD
```

The verifier resolves immutable Git objects, checks all payload and report byte receipts, validates bound commit/root trees and requires that the seal commit add only the manifest relative to the payload. The local branch retains the same tested implementation as the commissioned revision, plus evidence-only history.
