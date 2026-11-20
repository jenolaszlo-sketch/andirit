# Independent reproduction — ANDIRIT-CI1A-S2.5-WP-S25-4-GATE-QUALIFICATION-01

All executions run against a fresh, disposable exact-byte export of the pinned remediation
revision `eafd632` (tree `823e3c655f8fa8aea64b68e0935b1048bc9b6f7d`). No producer artifact was
used as an execution input. Scripts in this package were used verbatim.

## 1. Exact-byte export and blob verification (GQ-02)

```powershell
# scripts/make-export.ps1 — plain git archive with conversion disabled; Python tarfile
# extraction; independent verifier over every tracked blob (qual-04 verifier, unmodified).
git -c core.autocrlf=false -c core.eol=native archive --format=tar eafd632b... > export.tar
# export-identity.txt: tar-bytes=169031680  tar-sha256=DBF8CAA1CE84DD1AD9609A0ABE03707A8FAB33E8823BD7FBB14625E61CC5A231
python verify-exports.py D:\projects\andirit eafd632b... <export>
# [export] git entries=4555 exported=4555 missing=0 extra=0 mismatched=0  (exit 0)
```

The local repository checkout has `core.autocrlf=true`; the export bypasses all smudge/clean
conversion, so disk bytes equal Git blob bytes exactly (blobs are LF; the worktree is CRLF).

## 2. Toolchain and restore

```powershell
dotnet --info                 # SDK 10.0.401 (Commit e34a38d2ae), MSBuild 18.9.11; Windows 10.0.26200
dotnet restore Andirit.slnx -p:NuGetAudit=false --ignore-failed-sources   # exit 0
```

## 3. Binding exact-byte formatting gate (GQ-02)

```powershell
# Binding CI command (.github/workflows/ci.yml:24), executed in the exact-byte export root:
dotnet format Andirit.slnx --no-restore --verify-no-changes
# Exit code: 0   (gates/format.log)
```

The R3 baseline (QUAL-04 gates: `gates/format.log`) was exit 2 with seven WHITESPACE requests
in the two production files; the two comment relocations at `eafd632` clear them.

## 4. Binding LatticeDb coverage gate (GQ-03)

```powershell
# Binding CI parameters (.github/workflows/ci.yml:57-64), verbatim:
dotnet test tests/Andirit.LatticeDb.Tests/Andirit.LatticeDb.Tests.csproj -c Release --no-restore `
  /p:CollectCoverage=true "/p:CoverletOutput=<logs>/coverage-latticedb/" `
  /p:CoverletOutputFormat=cobertura '/p:Include=[Andirit.LatticeDb]*' `
  /p:Threshold=70 /p:ThresholdType=line%2cbranch
# Exit code: 0; coverlet table Line 92.87% / Branch 78.96% / Method 98.57%
# (gates/coverage-latticedb.log; artifact gates/coverage-latticedb/coverage.cobertura.xml)
```

Independent numerator/denominator derivation: `parse-coverage.py` (also cross-checks the R3
baseline artifact from the QUAL-04 package); output in `coverage-derivation.json`.

## 5. Whole-solution tests and Release build (GQ-04)

```powershell
dotnet build Andirit.slnx -c Release --no-restore -p:NuGetAudit=false --ignore-failed-sources  # exit 0, 0 warnings, 0 errors
dotnet test Andirit.slnx -c Release --no-restore --no-build --logger trx --results-directory <logs>/trx  # exit 0
# Passed! 75 LatticeDb / 157 TreeSitter / 345 core / 372 CSharp  => 949 passed, 0 failed, 0 skipped
```

## 6. Focused preservation gates (GQ-04) and frozen oracle

```powershell
# scripts/run-preservation.ps1
dotnet test tests/Andirit.CSharp.Tests/... --filter FullyQualifiedName~CompleteScopeMembership|MissingCurrentAuthority|L04_IdenticalRealFactIdentity|L05_ContributorConflict|S24SurfaceRefusalMapping|IndexingLifecycle_RemovesProjectUnitWhenSolutionMembershipChanges   # exit 0, 13 passed
dotnet test tests/Andirit.LatticeDb.Tests/... --filter FullyQualifiedName~N8_D3GraphNoninterference|HistoricalStorageIdentityTests|OccurrenceRemediationNativeTests   # exit 0, 7 passed
```

```powershell
# scripts/run-oracle.ps1 — the oracle project is not part of Andirit.slnx and needs its own restore
dotnet restore docs/planning/adr-ci1-01-ci1a-s2-0/oracle/SemanticCorpusOracle.csproj -p:NuGetAudit=false --ignore-failed-sources   # exit 0
dotnet run --project ... -- --corpus docs/planning/adr-ci1-01-ci1a-s2-0/corpus --expected .../oracle-expected-r2.json --output <evidence>/oracle-r2.json
# S2.0 oracle: 185/185 checks passed, 0 failed. (exit 0)
# oracle-r2.json: 146,851 bytes, SHA-256 97f6c5e5a4cc77e089481c7643732b7a07a31d483c2643bd7a95fdad7ced39c9
#   — byte-identical to the frozen R2 output sealed by QUAL-03/QUAL-04.
```

Infrastructure note: the first oracle attempt used `--no-restore` and failed with NETSDK1004
(the oracle project is outside `Andirit.slnx`, hence not covered by the solution restore).
That failure is reviewer-command infrastructure, not a product failure; the successful
attempt above is the recorded receipt. The initial C:-volume exhaustion and relocation is
recorded in `revisions.json`.

## 7. Producer evidence reconciliation (GQ-05)

`parse-producer-artifacts.py` read-only parses the producer package
(`…\GATE-REMEDIATION-01\evidence\`) and independently derives the coverage summary, TRX
counters, and artifact hashes; output in `producer-reconciliation.json`. The producer
artifacts themselves were imported byte-verbatim into the sibling producer package
`docs/reviews/ANDIRIT-CI1A-S2.5-WP-S25-4-GATE-REMEDIATION-01-evidence/`.