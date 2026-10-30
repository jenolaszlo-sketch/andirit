# EC6D-QUAL-02 independent re-qualification (preserved)

This directory preserves the independent EC6D-QUAL-02 re-qualification that
gated `EC6D-OWNER-02`. The owner decision is recorded in
[EC6D-OWNER-02 acceptance](../../decisions/ec6d-owner-02-acceptance.md).

- Reviewer/model: DeepSeek V4.1 Flash, fresh session, author-disjoint from the
  implementation and remediation.
- Reviewed production source `701257f4e6f0dbf71ab57072f8204cfa4cd6cad2`
  / tree `80d284029f5a82184bb04fd3d0d11fef7b223dc0`; evidence head
  `503b58152f7a3112c153214c44dd592663722150`.
- Recorded reviewer disposition: `EC6D-QUAL-02 SUPPORTED WITH QUALIFICATIONS`.
- Environment: Windows 10.0.26200 x64, .NET SDK 10.0.401, Node 24.21.0,
  Python 3.14.8, TypeScript 5.9.3. Local execution Windows-only; Linux/macOS
  qualification relies on hosted CI.

| File | Meaning |
| --- | --- |
| [report.md](report.md) | Full report: git binding, independence, F01–F11 dispositions, probe deltas, new adversarial tests, A01–A28, durability, website/corpus, CI, residual findings, qualifications, disposition. |
| [preserved-probe-checks.json](preserved-probe-checks.json) | Unchanged EC6D-QUAL-01 probe against remediated source: 640 checks, 2 failures (both the owner-superseded F08 entry-summary assertions). |
| [f08-supplement.json](f08-supplement.json) | Owner-approved F08 replacement assertions, 15/15. |
| [independent-checks.json](independent-checks.json) | Independently authored 66-check adversarial probe, 0 failures. |

A byte-identical durable copy of the report and a larger evidence set (Authority
40/0, Lifecycle 12/0, separate-process reopen pairs, and the independent probe
source) is kept outside the checkout at
`C:/Users/Laszlos/source/repos/Solo/ec6d-qual-02/`.

These artifacts are review evidence. They are not a production patch and issue
no independent acceptance, release or package publication.
