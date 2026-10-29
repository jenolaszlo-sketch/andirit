# EC6D-SCOPE-Q2 review evidence

Reviewed R1: `0e1b213737d42c74df20aef4744ab5718058e266`, tree `ddf777aa155852daca40b6ed8748c028c67d5822`, parent `91a3ed53a256e89ed71d7d11c204252c417400a6`.

[Final report](../ec6d-scope-q2-independent-verification.md): **REMEDIATION REQUIRED**, three open P1 findings (Q2-01/02/03), no P0. This is review evidence, not an EC-6D implementation.

## Provenance and preserved history

This chat created the Q2 worktree at R1 and authored the root-level evidence toolkit on 2026-10-26. It was interrupted by an approval-review usage limit. On resume, commits `575712074cae22ea98312864b78c950ecc56e411` and `6e34452647fb28ee12be14798142921838f997bd` were present with a separate report, fixtures, runs and tools. Those commits and their raw evidence are retained unchanged. The resumed report integrates this chat's evidence and corrects the previous characterization of it as unrelated “setup-generated” material. The previous report remains accessible in Git history.

## Evidence map and classification

| Artifact | Meaning |
| --- | --- |
| [git-before.json](git-before.json) | Read-only original/R1/Q1/canonical Git bindings, object-store separation, change surfaces, protected tracked-file hashes/statuses |
| [git-resumed.json](git-resumed.json), [git-after.json](git-after.json) | Repeated preservation audits compared with original snapshot |
| [digest-reconciliation.json](digest-reconciliation.json) | Per-file normalized/raw blob and SHA-256 evidence; original mixed-byte, LF and CRLF comparisons |
| baseline-original-bytes.json | Fresh original-working-byte reproduction of both original digests |
| baseline-lf.json | Exact Git-blob LF run: a third, correctly different digest pair |
| baseline-crlf.json, baseline-crlf-repeat.json | Fresh repeated CRLF runs matching R1 |
| [cases.json](cases.json) | Fourteen independent adversarial source corpora; sources are literal JSON strings |
| [harness/Program.cs](harness/Program.cs), [harness/Q2.csproj](harness/Q2.csproj) | Non-production p9 harness using unchanged indexing APIs; incremental-origin check |
| [p9-adversarial-results.json](p9-adversarial-results.json) | Measured p9 facts/diagnostics; no new import/attribution/query implementation |
| [ts-oracle.cjs](ts-oracle.cjs), [typescript-results.json](typescript-results.json) | Independent TS 5.9.3 diagnostics/binding oracle for fourteen corpora |
| [audit_evidence.py](audit_evidence.py), [audit-results.json](audit-results.json) | Eighteen assertions on measured existing behavior and digest reconciliation |
| [validation.json](validation.json), [validate_review.py](validate_review.py) | Link, surface, hash, identity and whitespace audit |
| fixtures/adversarial/, runs/, tools/ | Additional evidence already committed during the interruption; retained verbatim |

The root evidence is the basis for the resumed final report. Prior `runs/typescript-oracle-q2.json` independently corroborates namespace-shadowing, invalid collision and type-parameter exclusions. Historical 388-test outcomes are cited as historical evidence; no broad regression suite was rerun. All future A01–A20 assertions are contract assessments, not implementation-pass evidence.

## Reproduce from the Q2 worktree root

```powershell
dotnet restore docs/planning/ec6d/analysis/ConsumerBaseline.csproj --ignore-failed-sources -m:1 -p:RestoreUseStaticGraphEvaluation=true -p:NuGetAudit=false
dotnet build docs/planning/ec6d/analysis/ConsumerBaseline.csproj --no-restore -m:1
python docs/reviews/ec6d-scope-q2-evidence/reproduce.py
dotnet build docs/reviews/ec6d-scope-q2-evidence/harness/Q2.csproj -m:1 -p:NuGetAudit=false
dotnet docs/reviews/ec6d-scope-q2-evidence/harness/bin/Debug/net10.0/Q2.dll docs/reviews/ec6d-scope-q2-evidence
node docs/reviews/ec6d-scope-q2-evidence/ts-oracle.cjs C:/Users/Laszlos/source/repos/website/node_modules/typescript/lib/typescript.js
python docs/reviews/ec6d-scope-q2-evidence/audit_evidence.py
```

`reproduce.py` requires the preserved original planning checkout to read its exact working bytes; it never writes that checkout. Copies live in this review's ignored bin directory. The observed original bytes can also be reconstructed from the LF blobs using the line-ending counts and hashes in digest-reconciliation.json: each original corpus file has its final newline changed to CRLF. The previously committed runs/original-bytes-reconstruction.sha256 provides an additional manifest. Raw observations contain publication-specific metadata that may change on rerun; compare identities/digests and semantic facts, not the whole snapshot integrity hash.

The TS library is an external read-only tool (5.9.3), not a copied dependency or required production component. An equivalent installed 5.9.3 typescript.js may be passed as the argument. The harness materializes cases.json in ignored bin/fixtures/. The idle incremental case appends one declaration only in that scratch copy. Sources and production projects are never edited.

For protected-state verification, run `python docs/reviews/ec6d-scope-q2-evidence/verify_git.py after` with read access to the canonical and Q1 repositories. Do not overwrite git-before.json on a verification rerun. Its original snapshot is immutable review evidence. Scripts use GIT_OPTIONAL_LOCKS=0 for protected reads.

## Final binding and limits

The report's O section records the final content commit/tree/parent through a binding-only follow-up. The exact final containing commit is supplied in the completion handoff; it can be obtained with `git show -s --format="%H%n%T%n%P" review/hetu-ec6d-scope-q2`. No literal self-hash is fabricated inside its own commit.

Windows x64, .NET 10.0.401. No cross-platform, scale, new-feature durable reopen, migration, cancellation injection or release claim. Qualified source consistency is distinct from durable graph integrity. Raw prior evidence stays preserved even where its narrative interpretation has been superseded by the final report.
