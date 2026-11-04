# ANDIRIT-CI1A-S2.2-R1 — Foundation qualification remediation

Date: 2026-11-04 (Asia/Manila). Canonical checkout: `D:\projects\Andirit`, `main`.

**Producer disposition: READY WITH EXPLICIT QUALIFICATIONS for independent S2.2-Q2 requalification.** This is a remediation handoff, not independent acceptance. G-CONFIRM and G-MINT remain open/not passed. WP4–WP9, extraction/association minting, S2.3 and later, publication and push remain unauthorized. R1 stops here.

## 1. Authentication, baseline and scope

Before edits, scoped elevated Git inspection established a clean tracked/untracked canonical `main`, exactly one worktree, and HEAD exactly the commissioned Q1 review. The sandbox's first unelevated Git status was unreliable outside its workspace; it was not used to establish cleanliness. No branch switch, branch creation, additional worktree, fetch, push or publication occurred. No applicable AGENTS.md exists at the checkout, its ancestors or within it. The whole Q1 report, all 48 probe cases/source and preserved results were inspected before production edits.

| Role | Commit | Tree |
| --- | --- | --- |
| Commissioned production baseline | `0c989e2990717b3dc0df629f449885d1d6408622` | `ef7457008bdb74e1b2de3d7e96d3dc0ffef9dad8` |
| Producer evidence | `5cae7f09992f4be86964e4d7af1c893f07d67b47` | `2a7468991f8a3ded8b2372cfbef7bc5461a3b2f3` |
| R1 starting point / authoritative Q1 | `38d4e513951c07578953fda7738d8741d2109004` | `ffca6b84dd8c2c1d9eebcbcd0544d5615dcb7e2a` |
| R1 implementation and producer tests | `d5644d814b83170f553584529c05f5f8c289ba25` | `bcbf65117c9a1f892b987e4e83ed25eab99ee705` |

The four commits form a consecutive parent chain. The evidence commit is the separate commit containing this report, the corrected three planning documents and additive R1 evidence directory; its exact commit/tree is supplied in the final handoff (a commit cannot embed its own hash). Locally stored `origin/main` remains `31c33a934aa13e373d9a3a6ffbc54d789eef092c`; no current remote-state assertion is made.

[Authentication receipt](CI1A-S2.2-R1-evidence/authentication.json) replays Q1's unchanged authentication script and records the later, deliberately dirty remediation state. [Integrity receipt](CI1A-S2.2-R1-evidence/integrity.json) binds the production correction to its implementation commit and verifies protected paths. Neither later receipt is misrepresented as the initial clean-tree capture.

Only two production paths change: `VerifiedCSharpCompilation.cs` and the new internal `MetadataReferenceValidation.cs`. The new producer test file is `tests/Andirit.CSharp.Tests/S2_2MetadataRemediationTests.cs`. Documentation changes are limited to the current mapping/G-CONFIRM/G-MINT packages and new R1 report/evidence. The original Q1 report, independent probes and historical evidence are unchanged. No public API, identity algorithm/version, extraction path, native code or unrelated functionality changes.

## 2. Finding-by-finding disposition

| Original Q1 ID | Classification and root cause | R1 correction and evidence | Requalification limit |
| --- | --- | --- | --- |
| Q1-F01 / Q-F3 | Reproduced behavioral defect; row accessors did not dereference names or blobs. The prior full-PE comment overclaimed. | Validate every TypeDef/TypeRef name and namespace, every MethodDef name/signature, and bounded local relationships before capture returns. Exact three Q1 images now refuse; 40 producer regressions pass. | Bounded surface, not universal PE/metadata/IL validation or semantic usability. Q2 must assess this achieved boundary against Q-F3. |
| Q1-F02 | Reproduced contract gap; actual captured reference paths/inventory were conflated with fixture labels/inventory. | Mapping §2 and both gate packages name distinct complete comparison profiles, exact observed anchors and test-only alignment. Original profile-difference probe remains passing. | No production mapper exists; production profile is not promised to equal a different fixture preimage. |
| Q1-F03 | Reproduced documentation overclaim; canonical shape was described as live provenance. | Mapping §6 assigns authority refusal to the future verified resolver/association gate; frozen helpers retain shape-only behavior. | Canonical-shaped fabricated values still pass shape helpers by design; live authority enforcement remains unimplemented. |
| Q1-F04 | Reproduced documentation overstatement; distinct object instances were conflated with distinct complete identity inputs. | Equivalent complete inputs must reproduce equal identities; distinct accepted defining contexts/declarations discriminate through differing R2 preimages. | Equal semantic identity never permits replacing the live object used as binding proof. No process/object IDs or compctx2 are added to identity. |
| Q1-F05 | Reproduced gate-document inconsistency; wholesale test-8 wording pulled S2.3 reopen forward. | Explicit pre-WP5 in-memory authority/association falsification; native persistence/reopen stays the first separately authorized S2.3 gate. | Carrier model/keys/serialization/aggregation remain unresolved; no production minting is allowed. |

Q-F2/WP1 and A-01 were supported findings with qualifications, not additional remediation defects. A-02's scope/inventory distinction is retained. Q1's PE truncation survey is an observation: complete metadata can survive missing trailing PE bytes, so R1 does not turn it into a claim that every truncated PE must fail. The first Q1 assembly-name assumption was a preserved reviewer error, already corrected in Q1; its historical log is untouched. The original unsupported claims about universal PE validity and helper authority are expressly withdrawn by the current correction, not rewritten in historical reports.

## 3. WP2 — correction and precise boundary

`Capture` remains the sole constructor of verified reference evidence; its constructor is private. The platform inventory calls it, and all supported preparation paths use that inventory. `Create` continues requiring the exact captured reference object, supported properties and live dependency associations. No descriptor-only or lazy identity-exposure path was introduced.

After immutable acquisition and hashing, assembly metadata materialization and the existing assembly-manifest guard still run. Before the final cancellation check and `new(reference, identity)`, the new internal validator:

- Dereferences **all** TypeDef and TypeRef name/namespace string handles and rejects empty required names. It applies no C# identifier, separator, Unicode-normalization or display-name grammar to metadata names. Positive controls include punctuation, supplementary Unicode, and distinct composed/decomposed strings.
- Dereferences every MethodDef name and signature blob, consumes the complete signature, and rejects malformed header/type encodings, truncated compressed integers, missing parameters/returns, trailing data and invalid local type handles. Void/byref/pinned/sentinel placements and array shape/count bounds receive bounded structural checks.
- Checks MethodDef calling convention/instance versus static flags, method generic arity against its GenericParam rows, local generic variable indices, local generic-parameter owner/index ordering, non-nil declaring type, and unique Param sequence numbers within return/parameter bounds. Omitted Param rows remain legal; parameter table row count is not equated to signature count. External generic arity/type resolution is not inferred.
- Uses an iterative work stack and count-versus-remaining-byte checks, avoiding recursive signature decoding and count-sized parameter arrays. A 10,000-level valid array signature succeeds. The existing 128 MiB image limit is unchanged; this is not a comprehensive CPU/memory-exhaustion qualification.

The parser uses standard .NET `MetadataReader`, `BlobReader`, signature-header/type-handle and compressed-integer APIs. Format references are [ECMA-335 Partition II §§22–23](https://www.ecma-international.org/wp-content/uploads/ECMA-335_6th_edition_june_2012.pdf) and the [.NET signature decoder implementation](https://source.dot.net/system.reflection.metadata/System/Reflection/Metadata/Ecma335/SignatureDecoder.cs.html). These describe format/decoder behavior; the evidence for this implementation is the local adversarial and positive tests. The iterative structural traversal adds no production dependency. It preserves the runtime's typedref pointer forms (including ArgIterator) rather than claiming that C# surface syntax exhausts valid reference signatures.

Failures encountered in this validation are `BadImageFormatException` and are translated by the existing capture boundary to `InvalidDataException("UnsupportedMetadataReference")`. Invalid inputs return no capture, so no verified identity is exposed; repeated malformed inputs reproduce the same refusal classification. Cancellation and IO remain separate (`OperationCanceledException`, `DirectoryNotFoundException`); oversized-image classification is unchanged. The byte digest and actual-path identity construction are unchanged for successful captures.

**Exact exclusions:** string-heap dereference/nonempty-name checks do not certify every UTF-8/name/heap canonicalization rule. TypeRef resolution scopes, inheritance graphs, all other tables, field/property/MemberRef/MethodSpec signatures, custom attributes, IL bodies, resources, PE trailing sections and cross-module relationships are not exhaustively validated. Modifier TypeSpec tokens are checked for local row/blob bounds; their full type syntax/semantic resolution is not recursively certified. Generic constraints and external references are not resolved. Nested function-pointer metadata is structurally traversed, not an exhaustive calling-convention/semantic verifier. Multi-module/mixed/native images, arbitrary platforms and resource-exhaustion attacks are not newly qualified. Standard materialization can still refuse unsupported input outside the added checks. Capture success is not successful binding or compilation. This is the smallest bounded correction for the demonstrated Q1 name/signature defects, not a silently broadened complete-ECMA guarantee.

### Exact before/after inputs

The emitted source is `public class First { public int M() => 1; } public class Later { public int N() => 2; }`, assembly `Adversary`, source path `Adversary.cs`, deterministic library, concurrent build false, with `typeof(object).Assembly.Location`. Both original Q1 and new producer tests mutate a two-byte heap index to `FF FF`.

| Mutation | Offset | Exact image SHA-256 | Before | After |
| --- | --- | --- | --- | --- |
| First TypeDef name (row 1, Module) | 830 | `14ede20fe8ae1585df478f8d3f6a0ebbf1c1501a0c501ebde2bf4828dc5799c7` | Capture returned identity; deeper string read failed | UnsupportedMetadataReference; no returned capture |
| Later TypeDef name (row 3) | 858 | `584974319d734cfb6c452095975335898981ee6261d6bf809e44d2e135116097` | Capture returned identity; deeper string read failed | Same deterministic refusal |
| First MethodDef signature blob index | 878 | `506edf234f78729295ce75c22e619211f3c7c745a9f1786d5f056700d9ccc6c7` | Capture returned identity; deeper blob read failed | Same deterministic refusal |

All three exact 2,048-byte damaged images are preserved under [inputs](CI1A-S2.2-R1-evidence/inputs/inputs.json), along with a standalone reproduction tool using the existing Roslyn 5.9.0 dependency. The exact original hashes match both pre-remediation TRX output and the preserved Q1 report. The producer test uses the same source/options/mutations independently of that auxiliary tool.

New negative controls also cover both TypeRef string fields, empty/truncated/wrong/reserved method headers, huge/truncated counts, unknown types, trailing bytes, invalid void/byref/pinned/sentinel placements, generic/table inconsistencies, nil/out-of-range/incorrect-kind type tokens and array bounds. Positive controls cover runtime metadata, emitted C# nested type/method generics, multidimensional/jagged arrays, byref returns/parameters, unmanaged pointers/function pointers, custom modifiers, varargs, synthetic external generic signatures and legal non-C# names. Synthetic structural fixtures intentionally do not prove semantic compilation.

## 4. WP3 — authority reconstruction and corrected profiles

Controlling authority is the accepted [Option A decision](../decisions/adr-ci1-01-ci1a-s2-2-target-identity-decision.md) §5, [identity contract](../planning/adr-ci1-01-ci1a-s2-rem-01/identity-contract.md), [entry acceptance](../decisions/adr-ci1-01-ci1a-s2-2-entry-acceptance-owner-01.md) §3.5, frozen [R2 CompilationProvenance](../evidence/adr-ci1-01-ci1a-s2-1-raw/authority/CompilationProvenance.cs) `Describe`/`ReferenceIdentity` (lines 344–438), and frozen [R2Identity](../planning/adr-ci1-01-ci1a-s2-0/oracle/R2Identity.cs) project-reference item. Corpus construction is independently visible in [Corpus.cs](../planning/adr-ci1-01-ci1a-s2-0/oracle/Corpus.cs) and its accepted specification. Q1 independently reconstructed these relationships; R1 preserves that authority and its results.

The semantic target remains asmctx3 from the **defining** compctx1 and frozen target components. compctx2 remains provenance/current-context authority; frozen usage4 and its established tuple stay unchanged. No compctx2 is added to either identity tuple. analysisscope1 binds the declared unit/path boundary; verified snapshots/tree/universe checks establish the source inventory separately.

The legacy R2 project item is reconstructed from the actual verified live `CompilationReference.Compilation`: `compilation|<assemblyName>|<metadataIdentity>|<trees>`. Trees sort by raw FilePath ordinal **before** separator normalization; each contributes normalized path, `=`, and lowercase SHA-256 of UTF-8 `GetText().ToString()`, comma joined. It does not encode the child's own parse/options/transitive context. Own source identities use captured raw-byte hashes instead. Projected reference strings sort ordinally without R2 deduplication. This is a faithful legacy projection, not a substitute for strong live association.

| Profile | Required comparison | Expected result |
| --- | --- | --- |
| Observed production capture | Actual immutable byte hashes, actual normalized paths and full supported TPA inventory; independent R2 reconstruction over these **same** inputs | Algorithm fidelity; no promise of a different fixture's digest |
| Frozen corpus | All accepted corpus source/parse/options/project/dependency inputs and accepted runtime bytes; exactly System.Runtime, System.Private.CoreLib and Microsoft.CSharp with `file|runtime/...` labels | Exact accepted frozen anchors |
| Reviewer comparison-only alignment | Hold all inputs/bytes fixed and replace only the three captured PE labels with authenticated fixture labels **inside comparison code** | Reproduces the two frozen anchors, without changing production identities or frozen expectations |

| Unit | Frozen fixture compctx1 suffix | Actual-path capture suffix |
| --- | --- | --- |
| Leaf | `404c5e48c3106b47786f14cad594139477c651fd78d0e050f8fd5bcab18fcb5f` | `b816dcb5512588bc3b5ad01692c20241b31442ab420c8a4b5a6d06d032b9c59b` |
| Core | `942a8e588a8db15afafb30d0292e0b867c767c5f72392b6f458783d961a71db3` | `df8d2932670fc6a15cec10930b0bf573cfce841080e55f9544433983cbbe8a77` |

All values have prefix `compctx1:sha256:`. The three exact runtime hashes and labels are in corrected mapping §2 and raw Q1/R1 observations. The hashes are observed accepted-toolchain inputs, not constants independent of runtime version: Corpus.cs hashes its host runtime files. Production TPA construction uses distinct/sorted case-insensitive paths and the existing total image bound; it is not the three-reference corpus. No fixture path substitution is introduced in production.

Current helpers validate canonical shape/tuple fields only; live proof must be supplied by a future verified resolver/association gate. Equivalent complete R2 inputs across separate compilation instances preserve identity. Different accepted defining preimages discriminate identities, while live-instance correspondence remains separately mandatory.

The corrected G-CONFIRM row 5 declares the expected-value profiles before later dependent test authoring. The documentation regression is a 12-check section/content guard, not an independent semantic oracle: before correction only 1/12 passed (the existing isolation invariant); afterward 12/12 pass. Checks 1–6/10 address F02, check 7 F03, check 8 F04, and checks 11–12 F05; check 9 preserves compctx2 isolation. Q1's unchanged algebra/profile/shape/equal-instance probes independently retain their original observation semantics. Neither that script nor passing algebra clears G-CONFIRM or G-MINT.

Pre-WP5 requirements include actual production mapping, unique live defining binding, complete verified universe/current context, per-fact separate association and absent/stale/fabricated/substituted/ambiguous refusal. The concrete carrier's keys, serialization and aggregation remain unresolved/unimplemented. Native persistence/reopen is separately authorized S2.3 work with its first stale-provenance-after-reopen gate. If enforceability fails, the owner decides the bounded Option B alternative. No future carrier or gate is credited as implemented by R1.

## 5. Verification ledger and test accounting

| Run | Actual result | Meaning |
| --- | --- | --- |
| Unchanged Q1 against original source | 48 passed / 0 failed / 0 skipped | Reproduces the three malformed-identity observations, plus profile/shape/equality findings |
| Initial new producer fixtures, original source | 3 passed / 37 failed / 0 skipped | Includes two fixture errors, transparently retained |
| Corrected producer regressions, **original production source** | 5 passed / 35 failed / 0 skipped | Missing refusal reproductions; all positive controls pass |
| First bounded validator attempt | 39 passed / 1 failed / 0 skipped | Valid runtime ArgIterator typedref-pointer rejected; corrected before final implementation |
| Final same 40 producer regressions | **40 passed / 0 failed / 0 skipped** | Original three defects plus negative/positive controls |
| Full applicable solution, unfiltered | **692 passed / 0 failed / 0 skipped** | Core 325, native 43, TreeSitter 157, C# 167 |
| Unchanged Q1 after correction, all cases | **45 passed / 3 failed / 0 skipped** | Exactly three expected observation mismatches, detailed below; no unexpected failures |
| Documentation regression | Before 1/12; after **12/12** | Current contract sections corrected; no semantic gate acceptance |
| Frozen R2 manifest | **100/100 files** | Branch/pinned ancestry also verified |
| Frozen Option A/corpus oracle | **185/185**, 0 failed | Output SHA-256 `97f6c5e5a4cc77e089481c7643732b7a07a31d483c2643bd7a95fdad7ced39c9`, byte-identical to Q1/Q3 |

The first producer fixture run incorrectly placed GenericParam table rows out of coded-owner order and put a vararg method inside a generic type. Both test construction errors were corrected, then the complete 40-case suite was rerun against still-unmodified production source. All raw first/corrected logs and TRX remain. Likewise the initial validator's runtime incompatibility and diagnostic run remain in evidence. They are not hidden, counted as final passes, or attributed to Q1.

The documentation log named `pre-remediation.txt` is the original 1/12 baseline. The intermediate log named `pre-remediation-final.txt` is actually an 11/12 staged-doc result before the final row-5 correction; its filename does not establish baseline status. Both are preserved. `replay-doc-baseline.py` additionally exports the exact three Q1 Git blobs and runs the final unchanged 12-check script against them: `mapping-docs-before.log` independently confirms 1/12, while `mapping-docs-after.log` confirms 12/12 on final canonical documents.

The historical current-solution baseline of 652 is confirmed by Q1's authenticated ledger. The actual final solution is 652 existing cases plus 40 new producer cases = **692**. All seven existing WP1–WP3 foundation tests and the nine frozen identity-remediation tests are included in that full solution run. The 48 reviewer cases are intentionally separate from the solution, as in Q1; none was removed, filtered out, altered, marked skipped, or added to producer totals. There are no hidden test exclusions. Test timestamps/random temporary paths are not claimed byte-identical.

The three post-correction failures are `ObserveMalformedMetadataStillExposesIdentity` with `first-type-name`, `later-type-name`, and `method-signature`. These unchanged tests require capture to succeed in order to demonstrate Q1's defect. The new refusal therefore fails their historical observation assertion. Their preserved expected behavior is explained rather than rewritten. The producer tests require refusal, fail before remediation, and pass afterward. The assembly-name case continues passing with the already-supported refusal. Mapping/profile/shape/equal-instance observations continue passing because the documentation is corrected to describe that behavior accurately.

All 48 before/after case results are listed in §8 and in [the machine-readable results](CI1A-S2.2-R1-evidence/results.json); raw stack traces distinguish expected observation mismatches from unexpected failures. A green observation probe is never counted as evidence that its demonstrated defect was repaired.

Environment remains Windows x64, .NET SDK 10.0.401/runtime 10.0.12, Roslyn package 5.9.0; see [dotnet info](CI1A-S2.2-R1-evidence/environment-dotnet.txt). The auxiliary input-preservation project reused cached existing Roslyn; its standalone restore logged NU1900 when network vulnerability metadata was unavailable, then emitted all three exact inputs successfully. Production/solution projects gained no dependency or restore-policy change.

## 6. Commands and evidence integrity

Exact raw command outputs/TRX are in [R1 evidence](CI1A-S2.2-R1-evidence/results-summary.log). Paths below use `$stage = C:\Users\Laszlos\source\repos\Solo\andirit-ci1a-s2.2-r1`; cwd is canonical Andirit unless stated.

```powershell
git status --short --branch
git rev-parse HEAD 'HEAD^{tree}'
git log -3 --format='%H %T %P'
git worktree list --porcelain
git merge-base --is-ancestor 0c989e2990717b3dc0df629f449885d1d6408622 5cae7f09992f4be86964e4d7af1c893f07d67b47
git merge-base --is-ancestor 5cae7f09992f4be86964e4d7af1c893f07d67b47 38d4e513951c07578953fda7738d8741d2109004
dotnet test docs/reviews/CI1A-S2.2-Q1-tests/ReviewerTests.csproj --nologo -v minimal --logger 'trx;LogFileName=q1-before.trx' --results-directory "$stage/q1-before"
dotnet test tests/Andirit.CSharp.Tests/Andirit.CSharp.Tests.csproj --nologo -v minimal --filter 'FullyQualifiedName~S2_2MetadataRemediationTests' --logger 'trx;LogFileName=regression-before-final.trx' --results-directory "$stage/regression-before"
dotnet test tests/Andirit.CSharp.Tests/Andirit.CSharp.Tests.csproj --nologo -v minimal --filter 'FullyQualifiedName~S2_2MetadataRemediationTests' --logger 'trx;LogFileName=regression-after-final.trx' --results-directory "$stage/regression-after"
dotnet test Andirit.slnx --nologo -v minimal --logger trx --results-directory "$stage/solution-after"
dotnet test docs/reviews/CI1A-S2.2-Q1-tests/ReviewerTests.csproj --nologo -v minimal --logger 'trx;LogFileName=q1-after.trx' --results-directory "$stage/q1-after"
python -X utf8 docs/evidence/adr-ci1-01-ci1a-s2-0-r2/verify-manifest.py
dotnet run --project docs/planning/adr-ci1-01-ci1a-s2-0/oracle/SemanticCorpusOracle.csproj -- --corpus docs/planning/adr-ci1-01-ci1a-s2-0/corpus --expected docs/planning/adr-ci1-01-ci1a-s2-0/oracle/expected/oracle-expected-r2.json --output "$stage/oracle-r2.json"
python -X utf8 docs/reviews/CI1A-S2.2-Q1-evidence/authenticate.py D:/projects/Andirit "$stage/authentication.json"
python -X utf8 "$stage/mapping_docs_regression.py" D:/projects/Andirit
python -X utf8 "$stage/replay-doc-baseline.py" D:/projects/Andirit "$stage"
python -X utf8 "$stage/collect-r1.py" "$stage"
python -X utf8 "$stage/verify-integrity.py" D:/projects/Andirit "$stage/integrity.json" "$stage/oracle-r2.json"
git diff --check
```

Earlier fixture/validator runs use the same producer-test command with their preserved non-final TRX names; the single runtime diagnostic selects `PlatformAndEmittedComplexCSharpStillCapture`. From the writable Solo workspace the auxiliary preservation command was `dotnet run --project andirit-ci1a-s2.2-r1/inputs-tool/PreserveInputs.csproj -- andirit-ci1a-s2.2-r1/inputs`.

The unchanged Q1 authentication revalidates the accepted external R2 manifest trust hash `BCE7DE2A98602E60C7631757B30EBD05B7B388D65FED69E50A43AE0CEC848B13` and all 100 entries; F12 Q3 trust hash `9EFC098528340178D6A239A9C0137F8F40B8C62ACE2B1D5546561C1B56CCF52A` and all 338 entries; S2.1's 14 source/test and 65 evidence/documentation canonical blobs. It reproduces the accepted historical S2.1 limitation unchanged: 1,615 original manifest entries match in place, six preserved copies match, 14 superseded entries differ; corroborating manifest 36/36. No new integrity failure is present.

The additional R1 integrity check verifies all 33 Q1 raw-manifest entries and confirms Git equality of the original Q1 report/probes/evidence, frozen corpus/planning/oracle/evidence, accepted S2.1 raw evidence, WP1 decoder, public identity APIs and scope/universe source. Existing A-01 production lines remain untouched. The replayed oracle's exact output matches the accepted Q3 output digest, providing no observed identity drift. Current mapping-doc edits change expectations about *which preimage profile to compare*, never the frozen tuples or expected values.

The additive evidence manifest binds exact raw file bytes and excludes only itself; this report and current source/docs are Git-bound. Its verifier detects changes without rewriting expectations. No old manifest or evidence file is regenerated. The final commit/status receipt is supplied separately after the evidence commit, so a precommit receipt cannot be mistaken for a postcommit clean status.

## 7. Remaining qualifications and independent requalification

No Q1 defect reproduction remains unexplained. Q2 must independently inspect the exact implementation tree, re-run the three original inputs and broader metadata controls, assess the bounded achieved Q-F3 surface against the accepted decision, inspect the corrected mapping authority and profile reconstruction, and classify the three historical observation mismatches. It must not accept the producer's 40 green tests or 12 textual checks as an independent verdict.

WP1 retains the exact recognized-encoding set, strict decoding, bounded BOM-less NUL heuristic, raw-byte hash and proof-refusal behavior, including its indistinguishable BOM-less/non-NUL limitation. A-01 retains comma/pipe rejection and ordinal valid-symbol behavior. F12 remains Windows/toolchain/fixture-bound; source component forms, modeled coverage and lifecycle are not generalized. S2.1's preserved packaging limitation and Q1's carried diagnostic/recovery/merge/live-chain qualifications remain unchanged at their assigned gates.

G-CONFIRM's authority/expected-value confirmation remains reviewer-owned and open. G-MINT additionally requires a separately authorized implemented mapper/resolver/current-context association plus independent pre-mint falsification. Native reopen is the first separately authorized S2.3 gate; NEW-01/NEW-04 and the full S-01 chain are not closed here. No extraction, association minting, self-acceptance or later-stage work is delivered. There is no new remediation execution blocker, but these are explicit limits on closure and production reliance.

**Next gate: independent S2.2-Q2. Stop after R1.**

## 8. Every unchanged Q1 probe result

| Unchanged Q1 probe/case | Before | After | Interpretation |
| --- | --- | --- | --- |
| `DamagedMetadataStreamOffsetAndTableCountAreRefused(rowCount: False)` | Passed | Passed | Preserved. |
| `DamagedMetadataStreamOffsetAndTableCountAreRefused(rowCount: True)` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "empty", bytes: [], reason: null, text: "")` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "indistinguishable-utf16", bytes: [65, 65], reason: null, text: "AA")` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "malformed-0000FE", bytes: [0, 0, 254], reason: "InvalidSourceEncoding", text: null)` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "malformed-0000FEFF00110000", bytes: [0, 0, 254, 255, 0, ···], reason: "InvalidSourceEncoding", text: null)` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "malformed-80", bytes: [128], reason: "InvalidSourceEncoding", text: null)` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "malformed-C0AF", bytes: [192, 175], reason: "InvalidSourceEncoding", text: null)` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "malformed-EF", bytes: [239], reason: "InvalidSourceEncoding", text: null)` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "malformed-EFBB", bytes: [239, 187], reason: "InvalidSourceEncoding", text: null)` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "malformed-FE", bytes: [254], reason: "InvalidSourceEncoding", text: null)` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "malformed-FEFFD800", bytes: [254, 255, 216, 0], reason: "InvalidSourceEncoding", text: null)` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "malformed-FF", bytes: [255], reason: "InvalidSourceEncoding", text: null)` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "malformed-FFFE00", bytes: [255, 254, 0], reason: "InvalidSourceEncoding", text: null)` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "malformed-FFFE000041", bytes: [255, 254, 0, 0, 65], reason: "InvalidSourceEncoding", text: null)` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "nul", bytes: [0], reason: "AmbiguousBomlessEncoding", text: null)` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "short", bytes: [97], reason: null, text: "a")` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "utf-16-bom", bytes: [255, 254, 47, 0, 47, ···], reason: null, text: "// café 中文 \ud83d\ude00\r\nclass A {}\n")` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "utf-16-bomless", bytes: [99, 0, 108, 0, 97, ···], reason: "AmbiguousBomlessEncoding", text: null)` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "utf-16BE-bom", bytes: [254, 255, 0, 47, 0, ···], reason: null, text: "// café 中文 \ud83d\ude00\r\nclass A {}\n")` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "utf-16BE-bomless", bytes: [0, 99, 0, 108, 0, ···], reason: "AmbiguousBomlessEncoding", text: null)` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "utf-32-bom", bytes: [255, 254, 0, 0, 47, ···], reason: null, text: "// café 中文 \ud83d\ude00\r\nclass A {}\n")` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "utf-32-bomless", bytes: [99, 0, 0, 0, 108, ···], reason: "AmbiguousBomlessEncoding", text: null)` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "utf-32BE-bom", bytes: [0, 0, 254, 255, 0, ···], reason: null, text: "// café 中文 \ud83d\ude00\r\nclass A {}\n")` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "utf-32BE-bomless", bytes: [0, 0, 0, 99, 0, ···], reason: "AmbiguousBomlessEncoding", text: null)` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "utf-8-bom", bytes: [239, 187, 191, 47, 47, ···], reason: null, text: "// café 中文 \ud83d\ude00\r\nclass A {}\n")` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "utf16-bom-only", bytes: [255, 254], reason: null, text: "")` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "utf32-bom-only", bytes: [255, 254, 0, 0], reason: null, text: "")` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "utf8", bytes: [47, 47, 32, 99, 97, ···], reason: null, text: "// café 中文 \ud83d\ude00\r\nclass A {}\n")` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "utf8-bom-nul", bytes: [239, 187, 191, 0], reason: null, text: "\0")` | Passed | Passed | Preserved. |
| `EncodingRefusalPrecedenceAndMaterializedHandoff` | Passed | Passed | Preserved. |
| `MalformedMetadataRootIsRefused` | Passed | Passed | Preserved. |
| `MappingReconstructsFrozenAuthorityAndSeparatesTransitiveProvenance` | Passed | Passed | Preserved. |
| `MetadataCancellationAndIoStaySeparateFromFormatRefusal` | Passed | Passed | Preserved. |
| `MetadataControlsAndTruncationSurvey` | Passed | Passed | Preserved. |
| `New02MatrixTupleAndFrozenUsageAnchors` | Passed | Passed | Preserved. |
| `ObserveFrozenCorpusReferenceProfileDiffersFromLiveCapture` | Passed | Passed | Preserved. |
| `ObserveMalformedMetadataStillExposesIdentity(damage: "assembly-name")` | Passed | Passed | Preserved. |
| `ObserveMalformedMetadataStillExposesIdentity(damage: "first-type-name")` | Passed | Failed | Expected observation mismatch: capture now refuses; producer regression passes. |
| `ObserveMalformedMetadataStillExposesIdentity(damage: "later-type-name")` | Passed | Failed | Expected observation mismatch: capture now refuses; producer regression passes. |
| `ObserveMalformedMetadataStillExposesIdentity(damage: "method-signature")` | Passed | Failed | Expected observation mismatch: capture now refuses; producer regression passes. |
| `ObserveTextuallyEquivalentLiveInstancesHaveSameIdentity` | Passed | Passed | Preserved. |
| `SeparatorInjection(symbol: ",")` | Passed | Passed | Preserved. |
| `SeparatorInjection(symbol: "A,B")` | Passed | Passed | Preserved. |
| `SeparatorInjection(symbol: "A,B&#124;C")` | Passed | Passed | Preserved. |
| `SeparatorInjection(symbol: "A&#124;B")` | Passed | Passed | Preserved. |
| `SeparatorInjection(symbol: "&#124;")` | Passed | Passed | Preserved. |
| `SymbolsOrderOrdinallyAndKeepUnicodeDistinct` | Passed | Passed | Preserved. |
