# Qualification methodology

## Isolation

The qualification authority cloned the canonical remote into
`C:\Windows\Temp\opencode\s64-qual\repo` and created detached worktrees for the
entry baseline (`dda4358`), the implementation (`ec984bf`), the producer seal
(`870b600`) and the synthetic merge (`c549852`). The producer worktree at
`C:\Users\Laszlos\source\repos\Solo\andirit-wp-s64-entry-01` was not modified.
No production source or producer evidence file was changed.

## Identity and seal verification

`tools/verify_seal.py` reimplements seal verification without importing the
producer script. It checks:

- entry, implementation, payload and seal commit/tree identities;
- payload parentage and the exact payload additions;
- all 34 evidence entries against Git blobs, byte lengths and SHA-256;
- all 7 reviewed implementation inputs against the implementation commit;
- the four affected-store binary hashes against `SHA256SUMS.txt`;
- the seal SHA-256.

It passes on a clean worktree.

## Hosted checkout equivalence

GitHub's `pull_request` event checks out a synthetic merge. The authority
confirmed:

- run `46035752304` head SHA is `ec984bf`;
- the checked-out merge `c549852` has parents `[main, ec984bf]`;
- its tree is `8ec0eb7a…`, exactly the implementation tree;
- `git diff c549852 ec984bf` is empty;
- job, artifact and receipt identities match the run;
- workflow, dependency and package inputs are unchanged between `main` and the
  implementation; only receipts bind `GITHUB_SHA`, and the plugin version tests
  strip build metadata.

The owner authorized tree-equivalent synthetic merge evidence for this
commission. The authority verified every condition; no other provenance
requirement was waived.

## Independent harness

`harness/Program.cs` is a separate console program compiled twice with a
`ProjectReference` to the baseline and candidate source trees. It uses only the
public store API and raw LatticeDbSharp rows. It builds its own expected-state
oracle from the unit definitions and emits JSON censuses.

Scenarios:

- shared logical nodes and shared logical edges across contributors;
- enabled reopen accumulation;
- disabled publication and re-enablement;
- manifest misattribution to an unrelated node and to a `HetuUnit` row;
- unrelated incoming and outgoing relationships;
- cross-repository native references;
- inconsistent, missing, malformed and stale manifests;
- protocol-marked orphan versus unmarked unrelated object;
- cleanup failure and cancellation;
- recovery of the producer-sealed stores and an independently constructed
  affected store.

## Executed versus audited

- Independently **executed** on Windows x64: full solution, coverage, package
  consumer, baseline controls, ownership controls and recovery.
- Independently **audited** from exact hosted artifacts: Linux x64 and macOS
  ARM64 test and coverage results, plus the hosted pack job.
- macOS remains a regression gate; it is not S6.5 platform certification.

## Limits

- The independent harness uses a compact synthetic fixture, not the full S6.4 D3
  corpus. F-S64E-03 and F-S64E-04 remain open.
- Engine-assigned native ids are unstable; only logical identity and physical
  multiplicity are asserted.
- Hosted checkout is the authorized tree-equivalent synthetic merge, not a push
  checkout at the literal commit.
- Cleanup failure/cancellation uses the store's internal fault hook through
  reflection; it is test-only and does not modify production code.
