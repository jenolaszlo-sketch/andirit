# Requirement-to-evidence disposition matrix — ANDIRIT-CI1A-S2.5-WP-S25-4-GATE-QUALIFICATION-01

Criteria derive from the commissioning instruction, the governing repository contracts
(`.github/workflows/ci.yml` binding gates, `docs/planning/adr-ci1-01-ci1a-s2-5/wp-s25-4-qualification-matrix.md`,
`docs/planning/adr-ci1-01-ci1a-s2-5/wp-s25-4-execution-plan.md`, `docs/planning/adr-ci1-01-ci1a-s2-5/s01-canonical-form-contract.md`)
and the prior qualification record (QUAL-04). Evidence locators are relative to this package.

Dispositions: PASS (independently verified) · PASS-QUAL (verified with recorded qualification) ·
QUAL (qualification recorded, not verifiable from available evidence).

## GQ-01 — Change scope

| # | Requirement | Result | Evidence / disposition |
| --- | --- | --- | --- |
| GQ-01.1 | Production changes limited to the two reported comment relocations | **PASS** | `git diff eafd632^ eafd632` limited to 6 files; production changes are `src/Andirit.CSharp/CSharpProjectDiscovery.cs` (+1/−1) and `src/Andirit.CSharp/VerifiedCSharpCompilation.cs` (+4/−4), both comment-only moves of one comment above an argument-list/blank-line position. No identifier, literal, token or operand is added, removed or reordered; both types are `internal` (`CSharpProjectDiscovery`, `VerifiedCSharpCompilation`), so no public surface is touched |
| GQ-01.2 | Test changes within the authorized coverage remediation scope | **PASS** | Test delta = one modified test (`LatticeCodeGraphStoreTests.Store_AcceptsTuningOptions` widened to set `PageSize`/`EnableAdjacencyCache` alongside `CacheSizeMb`/`EnableWal`) plus two additive files: `LatticeDbDurableStateRegressionTests.cs` (new; 13 cases) and `NativeGraphMirrorTests.cs` (+10 `[Fact]`). 23 added cases, all in `tests/Andirit.LatticeDb.Tests`, using public store/`LatticeDbSharp` APIs only |
| GQ-01.3 | No public-contract, production-semantic, formatter-setting, coverage-exclusion, instrumentation, threshold or CI-policy changes | **PASS** | Diff contains no `.editorconfig`, no `Directory.Build.props`/`.targets`, no project file, no `.github/workflows/ci.yml`, no coverlet filters, no `Exclude`/threshold property. Coverage parameters derive unchanged from the workflow; `Include=[Andirit.LatticeDb]*` unchanged |
| GQ-01.4 | Existing assertions not weakened | **PASS** | Test diff is strictly additive (0 deleted lines in test files, `numstat`: `500/0`, `332/0`); the single modified test only *widens* its input options; its assertion (`CheckHealthAsync().IsHealthy`) is unchanged |

## GQ-02 — Exact-byte formatting

| # | Requirement | Result | Evidence / disposition |
| --- | --- | --- | --- |
| GQ-02.1 | Fresh export from the pinned committed Git blobs, no normalization | **PASS** | `git -c core.autocrlf=false archive eafd632` → Python `tarfile` extraction; independent verifier over all tracked blobs: `git entries=4555 exported=4555 missing=0 extra=0 mismatched=0`. Export identity: tar 169,031,680 bytes, SHA-256 `DBF8CAA1…A231` (`export-identity.txt`). Worktree `core.autocrlf=true` was *not* used to produce the export |
| GQ-02.2 | Independently reproduce the binding exact-byte formatting gate without formatter corrections or transformations | **PASS** | `dotnet format Andirit.slnx --no-restore --verify-no-changes` in the export root → **exit 0** (`gates/format.log`). SDK 10.0.401, Windows 10.0.26200, Python 3.14.8; review host `LAPTOP-KBD8D8JG` (`environment.txt`, `gates/toolchain.log`) |
| GQ-02.3 | Command, SDK version, environment and exit code recorded | **PASS** | `gates/format.log` (command, exit code), `gates/toolchain.log` + `environment.txt` (SDK/OS), `reproduce.md` §2–3 |

## GQ-03 — F-G2-01 coverage

| # | Requirement | Result | Evidence / disposition |
| --- | --- | --- | --- |
| GQ-03.1 | Independently execute the binding LatticeDb coverage gate on the pinned revision | **PASS** | CI parameters verbatim (`Include=[Andirit.LatticeDb]*`, `Threshold=70`, `ThresholdType=line,branch`, cobertura, Release, `--no-restore`) → **exit 0** (`gates/coverage-latticedb.log`) |
| GQ-03.2 | Line and branch coverage satisfy their binding thresholds | **PASS** | Gate table: Line 92.87%, Branch 78.96%, Method 98.57%; both ≥ 70%. Root attributes: `lines-covered=900 lines-valid=969` (0.9287), `branches-covered=368 branches-valid=466` (0.7896) |
| GQ-03.3 | Branch coverage ≥ 70% | **PASS** | 368/466 = 0.78959… = 78.96% ≥ 70% |
| GQ-03.4 | Numerator/denominator independently derived from the generated artifact | **PASS** | `parse-coverage.py` re-derives from `<line condition-coverage>` sums: condition sums 736/928 → halved 368/464; the root attributes' 368 agrees exactly with the halved covered sum. `coverage-derivation.json` |
| GQ-03.5 | No coverage manipulation or denominator reduction | **PASS** | Denominator unchanged 466 → 466 across the remediation (R3 baseline artifact in the QUAL-04 package: 305/466). Class set identical (24 classes both revisions) — no include-filter/class-set change; no csproj/props/threshold/CI change in the diff. The root `branches-valid` 466 vs line-derived half 464 offset (+2) is identical in both artifacts (present at R3), i.e., a pre-existing coverlet granularity artifact, not a remediation change |
| GQ-03.6 | Differences from the reported 368/466 explained | **PASS** | None: reviewer's independently generated artifact declares 368/466 with identical rates; the producer's artifact declares the same. Covered-branch delta R3→remediation +63 with valid delta 0; condition-level gains concentrate in `CheckOccurrenceAuthorityAsync` (+64 conditions covered), `NativeGraphMirror` (+34), `FindOccurrencesAsync` (+2) — exactly the areas named by the report |
| GQ-03.7 | New tests: meaningful behavioral assertions, determinism, correspondence to previously uncovered branches | **PASS** | Review of both new/extended files: assertions are behavioral outcomes via public API (availability state, coverage state, refusal reason substrings, exception types), not compile-only or tautological; tampering is done through genuine durable writes (`LatticeDbSharp` transactions) with no production visibility expansion; deterministic (fixed clock, GUID-keyed temp DBs, no network/time dependence). Correspondence: authority-gate branches 10→74 conditions (halved 5→37) in `CheckOccurrenceAuthorityAsync`; mirror bounds/corruption/null-payload branches +17; restore-projection branches in `FindOccurrencesAsync`; plus constructor-guard and options-surface lines. Static test census: parent 52 cases → remediation 75 cases (+23) |

## GQ-04 — Regression and preservation

| # | Requirement | Result | Evidence / disposition |
| --- | --- | --- | --- |
| GQ-04.1 | Whole-solution tests execute and pass | **PASS** | `dotnet test Andirit.slnx -c Release --no-restore --no-build` → exit 0; **949 passed, 0 failed, 0 skipped** = 75 LatticeDb + 157 TreeSitter + 345 core + 372 CSharp (`gates/full.log`, TRX in `gates/trx/`) |
| GQ-04.2 | Release build | **PASS** | exit 0, 0 warnings, 0 errors (`gates/release.log`) |
| GQ-04.3 | R1 preserved (graph noninterference) | **PASS** | `OccurrenceStage1NativeTests.N8_D3GraphNoninterferenceAcrossOccurrenceLifecycle` passes (whole suite + focused filter, 13/7 receipts in `preservation/`) |
| GQ-04.4 | R2 preserved (schema-2 / legacy identity) | **PASS** | `HistoricalStorageIdentityTests.Schema2_LabelsRemainHistoricalHetuProtocolIdentities` passes |
| GQ-04.5 | R3 + Q03-F-01 preserved (complete-empty / incomplete / availability; occurrence availability) | **PASS** | `OccurrenceRemediationNativeTests` (5 cases: complete-empty survival, deletion invalidation, incomplete-not-masquerade, no-analysis-unavailable, unknown-outcome) and `S23PublicationIntegrationTests.MissingCurrentAuthority_IsUnavailable_NotEmptySuccess` pass |
| GQ-04.6 | Merge and identity semantics preserved | **PASS** | `S23LifecycleQualificationTests.L04_IdenticalRealFactIdentity_TwoStorageUnits_UnionToOneFactTwoContributors` (union) and `L05_ContributorConflict_RealIdentity_IsRefusedAtStagingWithPriorIntact` (conflict/staging refusal) pass; membership cases (1/2/3 complete, complete-empty, mixed, missing members) pass in `CompleteScopeMembership_*` (11 cases); frozen S2.0 oracle 185/185 byte-identical (146,851 bytes, SHA-256 `97f6c5…d39c9`) — the R2 semantic identity/encoding surface is bit-preserved |
| GQ-04.7 | Public contracts and historical refusal behavior | **PASS** | No public API change is possible from this diff (internal comment moves; no type/member signature delta in `git diff`); oracle byte-identity fixes the identity encoding; `S24SurfaceRefusalMapping` suite passes; `CodeOccurrenceSchemaException`/`CodeOccurrenceIndexException`/`StalePublication`/`AnalysisUnavailable` refusal paths exercised by native suite (all pass) |

## GQ-05 — Evidence integrity

| # | Requirement | Result | Evidence / disposition |
| --- | --- | --- | --- |
| GQ-05.1 | Producer evidence package received | **PASS** | `C:\Windows\Temp\opencode\gate-remediation\…\GATE-REMEDIATION-01\` — report copy, `evidence/coverage-latticedb.cobertura.xml`, `evidence/latticedb-testresults.trx`, `evidence/MANIFEST.txt` |
| GQ-05.2 | Producer manifest verified | **QUAL** | `MANIFEST.txt` is a directory tree listing only, with no SHA-256 digests and no lengths; it does not meet the repository's established self-excluding SHA-256 manifest convention (`ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-04-evidence/manifest.json`, `CI1A-S2.5-WP-S25-4-R3-evidence/manifest.json`). Reviewer rehashed the imported artifacts (`PRODUCER-EVIDENCE.md`); the listing itself matches the package contents |
| GQ-05.3 | Reported results reconciled against raw artifacts | **PASS-QUAL** | Coverage 368/466 and LatticeDb 75/75 VERIFIED against producer raw artifacts (`producer-reconciliation.json`). Format-gate exit 0, whole-solution 949, build 0 warnings have **no producer raw artifacts** (no logs/TRX in the package) — recorded QUAL; independently reproduced by the reviewer. Producer inaccuracies recorded: blob count 4,553 vs actual 4,555 at `eafd632` (4,553 is the parent's count); "24 tests" vs 23; report identity block records the parent `f669e107`/`5df749d2` as "this HEAD"/seal instead of `eafd632`/`823e3c65` (see `findings.md` F-03…F-05) |
| GQ-05.4 | Producer evidence preserved in the permanent structure with producer/reviewer separation | **PASS** | Producer artifacts imported byte-verbatim to `docs/reviews/ANDIRIT-CI1A-S2.5-WP-S25-4-GATE-REMEDIATION-01-evidence/` with `PRODUCER-EVIDENCE.md` provenance; reviewer evidence is the sibling `ANDIRIT-CI1A-S2.5-WP-S25-4-GATE-QUALIFICATION-01-evidence/` package |
| GQ-05.5 | No silent reconstruction of missing producer evidence | **PASS** | Nothing was reconstructed; gaps are listed as qualifications (GQ-05.3) and in `findings.md` |
| GQ-05.6 | Historical qualification evidence preserved | **PASS** | QUAL-04, R1–R3, QUAL-01…03, WP-S25-4 planning records are tracked files untouched by this commission; the remediation diff itself left them untouched |

## Outstanding obligations (reconciled, not resolved)

| # | Obligation | Status after this commission | Evidence / disposition |
| --- | --- | --- | --- |
| OB-1 | M5.4 contract amendment (`Unresolved` refusal reachability) | **STILL BLOCKED — UNCHANGED** | The remediation changes no production behavior (comment moves) and adds no compiler-symbol-association evidence; the M5.4 disposition D (reachability unresolved — additional evidence required) from QUAL-04 stands; no amendment is made here |
| OB-2 | M11.4 genuine Ubuntu execution evidence | **STILL BLOCKED — now scope-stale** | The previously qualified genuine-Ubuntu run (WP-S25-2 G2/G3 chain) covered the 52-case LatticeDb suite; this remediation raises the suite to 75 cases, so a corrected-revision genuine-Ubuntu execution is required. This commission executed Windows only |
| OB-3 | M14.1 genuine Ubuntu execution evidence | **STILL BLOCKED — now scope-stale** | Same as OB-2; M14.1 requires the *entire current expanded* LatticeDb suite on Linux |
| OB-4 | F-G2-01 (LatticeDb coverage gate) | **TECHNICALLY SATISFIED — owner decision outstanding** | Independently reproduced 368/466 = 78.96% ≥ 70% at `eafd632` with the unchanged CI policy; closure is owner-governed (the register requires a separate binding owner decision), not granted by this commission |
| OB-5 | Exact-byte formatting (QUAL-04 finding) | **TECHNICALLY SATISFIED — owner disposition outstanding** | Independently reproduced exit 0 on the pinned revision; the profile disposition remains with the owner |
| OB-6 | WP-S25-4 closure / Stage 2.5 acceptance | **NOT GRANTED** | Passing this commission does not constitute WP-S25-4 owner acceptance or Stage 2.5 closure |

## Disposition

| Scope | Disposition |
| --- | --- |
| Overall commission | **SUPPORTED WITH QUALIFICATIONS** (qualifications F-01…F-06 in `findings.md`) |
| QUAL-04 exact-byte formatting gate | **SUPPORTED** |
| F-G2-01 LatticeDb branch coverage gate | **SUPPORTED** |