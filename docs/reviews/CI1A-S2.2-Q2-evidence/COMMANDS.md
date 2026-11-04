# Q2 executed commands

Date: 2026-11-04 (Asia/Manila). Shell: PowerShell. Cwd: `D:\projects\Andirit` except the collector, which ran in `C:\Users\Laszlos\source\repos\Solo`.
`$stage = 'C:/Users/Laszlos/source/repos/Solo/andirit-ci1a-s2.2-q2'`.
Commands below omit only PowerShell's output redirection/exit-code plumbing; every test's raw output and TRX is preserved in the named destination.

| Command | Log | Exit / result |
| --- | --- | --- |
| `git status --short --branch` | Initial scoped tool result; later clean receipt in inherited-authentication.json | 0; clean main, ahead 37 |
| `git rev-parse --show-toplevel --is-bare-repository` | Initial scoped tool result | D:/projects/Andirit; false |
| `git log -3 --format='%H %T %s'` | Initial scoped tool result; pins in authentication.json | Supplied exact commits/trees |
| `rg --files --hidden -g AGENTS.md -g '!.git'` | Initial scoped tool result | 1; none found; no applicable ancestor AGENTS.md |
| `dotnet --info` | environment-dotnet.txt | 0 |
| `dotnet test Andirit.slnx --nologo -v minimal --logger trx --results-directory "$stage/solution"` | solution.log | 0; 692/0/0 |
| `dotnet test docs/reviews/CI1A-S2.2-Q1-tests/ReviewerTests.csproj --nologo -v minimal --logger 'trx;LogFileName=q1.trx' --results-directory "$stage/q1"` | q1.log | 1; 45/3/0; three historical observation failures |
| `python -X utf8 docs/evidence/adr-ci1-01-ci1a-s2-0-r2/verify-manifest.py` | r2-verification.log | 0; 100 entries |
| `python -X utf8 docs/reviews/CI1A-S2.2-Q1-evidence/authenticate.py D:/projects/Andirit "$stage/inherited-authentication.json"` | inherited-authentication.log | 0; accepted R2/Q3/S2.1 trust anchors |
| `python -X utf8 docs/reviews/CI1A-S2.2-R1-evidence/verify-evidence.py` | r1-manifest.log | 0; 50 entries |
| `dotnet run --project docs/planning/adr-ci1-01-ci1a-s2-0/oracle/SemanticCorpusOracle.csproj -- --corpus docs/planning/adr-ci1-01-ci1a-s2-0/corpus --expected docs/planning/adr-ci1-01-ci1a-s2-0/oracle/expected/oracle-expected-r2.json --output "$stage/oracle-r2.json"` | oracle-r2.log | 0; 185/185 |
| `dotnet test docs/reviews/CI1A-S2.2-Q2-tests/ReviewerTests.csproj --nologo -v minimal --logger 'trx;LogFileName=q2-first.trx' --results-directory "$stage/q2-first"` | q2-first.log | 1; 37/1/0; reviewer fixture assumption corrected |
| `dotnet test docs/reviews/CI1A-S2.2-Q2-tests/ReviewerTests.csproj --nologo -v minimal --logger 'trx;LogFileName=q2-final.trx' --results-directory "$stage/q2-final"` | q2-final.log | 0; 39/0/0; intermediate expanded suite |
| `dotnet test docs/reviews/CI1A-S2.2-Q2-tests/ReviewerTests.csproj --nologo -v minimal --logger 'trx;LogFileName=q2-complete.trx' --results-directory "$stage/q2-complete"` | q2-complete.log | 0; 40/0/0; completed suite |
| `dotnet test docs/reviews/CI1A-S2.2-Q2-tests/ReviewerTests.csproj --no-build --nologo -v minimal --logger 'trx;LogFileName=q2-repeat.trx' --results-directory "$stage/q2-repeat"` | q2-repeat.log | 0; 40/0/0; separate process |
| `$env:ANDIRIT_Q2_INPUTS="$stage/inputs"; dotnet test docs/reviews/CI1A-S2.2-Q2-tests/ReviewerTests.csproj --nologo -v minimal --logger 'trx;LogFileName=q2-inputs.trx' --results-directory "$stage/q2-inputs"` | q2-inputs.log | 0; 40/0/0; exact adversarial and capture-control bytes retained, environment variable removed afterward |
| `dotnet test tests/Andirit.CSharp.Tests/Andirit.CSharp.Tests.csproj --no-build --nologo -v minimal --filter 'FullyQualifiedName~S2_2MetadataRemediationTests' --logger 'trx;LogFileName=remediation.trx' --results-directory "$stage/remediation"` | remediation.log | 0; 40/0/0; subset of solution |
| `dotnet test tests/Andirit.CSharp.Tests/Andirit.CSharp.Tests.csproj --no-build --nologo -v minimal --filter 'FullyQualifiedName~S2_2FoundationBoundaryTests' --logger 'trx;LogFileName=foundation.trx' --results-directory "$stage/foundation"` | foundation.log | 0; 7/0/0; subset of solution |
| `dotnet test tests/Andirit.Tests/Andirit.Tests.csproj --no-build --nologo -v minimal --filter 'FullyQualifiedName~OccurrenceIdentityRemediationTests' --logger 'trx;LogFileName=identity.trx' --results-directory "$stage/identity"` | identity.log | 0; 9/0/0; subset of solution |
| `python -X utf8 "$stage/audit-q2.py" D:/projects/Andirit "$stage"` | authentication.log | 0; independent Q2 pins, protected-file and trust-anchor audit |
| `python -X utf8 andirit-ci1a-s2.2-q2/collect-q2.py D:/projects/Andirit C:/Users/Laszlos/source/repos/Solo/andirit-ci1a-s2.2-q2` | results-summary.log | 0; all case accounting and five stable observations compared |

The Q2 project reuses the existing friend-assembly name in its separate output folder and links frozen authority sources read-only. No project was added to the solution. The full solution ran before Q2 additions, and Q2 ran separately. No producer/historical test was edited, skipped or filtered out of the full solution/Q1 runs. Targeted runs intentionally select the named producer subsets and do not increase the distinct solution total.

The first Q2 run's failure was the assertion that current verified dependency trees retained raw backslashes. `CodeGraphSource` normalizes those paths. The corrected probe asserts that invariant; an additional frozen-only raw-path fixture proves sort-before-normalization and duplicate preservation. The final additional test covers separate captured reference instances and same-byte/different-path distinctions. The first failure remains a reviewer error, not a production regression. The intermediate name `q2-final` does not denote the completed 40-case suite.

Audit receipts bind pre-Q2 candidate HEAD. A later additive commit cannot contain its own SHA. The final response supplies the postcommit SHA and status; no precommit receipt is presented as postcommit cleanliness. Scripts that assert candidate HEAD are intentionally historical audits, not generic postcommit verifiers.

Packaging/final checks: `python -X utf8 "$stage/package-q2.py" "$stage" D:/projects/Andirit`; `python -X utf8 docs/reviews/CI1A-S2.2-Q2-evidence/verify-q2.py D:/projects/Andirit`; `git diff --cached --check`; additive staged-path audit; local commit; postcommit manifest verification and Git status. The first staged whitespace check flagged preserved CRLF raw evidence as trailing whitespace. Package-local attributes now recognize CR-at-EOL, preserve raw log/TRX whitespace and mark binary inputs as binary. Raw outputs were not normalized or rewritten.
