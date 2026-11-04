# ANDIRIT-CI1A-S2.2-Q1 — Independent foundation qualification

Date: 2026-11-04 (Asia/Manila). Reviewer: Codex, independently examining the supplied producer implementation and evidence. Repository: canonical `D:\projects\Andirit`, `main`. This is a technical qualification record, not owner acceptance or implementation authorization.

**Overall verdict: REMEDIATION REQUIRED.**

| Work package | Verdict | Reason |
| --- | --- | --- |
| WP1 — Q-F2 | SUPPORTED WITH QUALIFICATIONS | Matches the accepted strict-decode/NUL heuristic, including its deliberate limitations. |
| WP2 — Q-F3 | REMEDIATION REQUIRED | Corrupt type names and a corrupt method-signature heap index still receive captured identities. The implemented reads establish a narrower boundary than usable assembly metadata/full PE validation. |
| WP3 — A-01 and mapping contract | REMEDIATION REQUIRED | A-01 is supported. Mapping algebra is independently reproducible for equal inputs, but the frozen-corpus comparison profile and two authority/identity claims need correction. |

`G-CONFIRM`: **NOT PASSED**, for the row-5 comparison-contract inconsistency described below, not for missing future WP4–WP9 executions. The NEW-02 tuple correction itself is independently confirmed. `G-MINT`: **NOT PASSED**; production mapping, live minting authority and association enforcement are absent, and specification corrections remain. Neither gate is self-certified by producer tests or by a coherent specification.

No production remediation, producer-test edits, WP4–WP9 implementation, extraction authorization, production target minting, native publication, push, new branch or new worktree was performed. Reviewer-only hash calculations and adversarial assemblies are test inputs, not production extraction or minting functionality.

## 1. Authenticated commits, trees and diff

Initial inspection found a clean tracked/untracked canonical `main`, one worktree, no stash, and HEAD exactly the supplied evidence commit. `origin/main` is the locally stored `31c33a934aa13e373d9a3a6ffbc54d789eef092c`, an ancestor; local main was 34 commits ahead. No fetch or remote-state attestation is claimed. No applicable `AGENTS.md` exists at the checkout or its ancestors or within this repository.

| Role | Commit | Tree |
| --- | --- | --- |
| Decision baseline | `febc28d92548fd7323294077c37444048741a8c8` | `e1fa31ec23ef213629dd613e92073641bf2e6861` |
| Implementation | `0c989e2990717b3dc0df629f449885d1d6408622` | `ef7457008bdb74e1b2de3d7e96d3dc0ffef9dad8` |
| Reviewed evidence HEAD | `5cae7f09992f4be86964e4d7af1c893f07d67b47` | `2a7468991f8a3ded8b2372cfbef7bc5461a3b2f3` |
| F12 acceptance | `a1cf47083747bae90293ec074b35fe8078bd0d6e` | `ebba0daf2977315a5c09551df0de698afeb6696f` |
| Qualified R2 | `7f8d035d7fa6bd5da49f71ee86ff54d7350318c5` | `29150eb6ef2b0fe65942f3c8930037f8c0120174` |
| Final R1 | `9fc2115622c93fa54f5f7882b117185a017703bb` | `547af9c3c7d37132b93944cf8b3dbf284872450f` |
| Original S2.0 | `82f101c24e21828a41a30d4342ba73db9376e24e` | `75faf3eb0fcdab8ad78142617d1d7ee3dfce95c0` |
| S2.1 owner baseline | `b7fea0d219409b279b78317e32bdc3c8b5ae61ee` | `b4a764637498ed8e8bddac28fb0b0b3716b7ffee` |
| S2.1 implementation | `c02d5eae157653ba7f123b98aceccb01a0b542a0` | `a53a076dbb482062973f70fd11bf4261cc5368f3` |

All listed trees and ancestry to reviewed HEAD were checked. The three commissioned commits are consecutive parents: decision → implementation → evidence. Implementation changes only two production files (`VerifiedCSharpSource.cs`, +12/−1; `VerifiedCSharpCompilation.cs`, +33/−4) and adds the producer foundation tests and frozen netmodule fixture. Evidence HEAD changes five documentation files only. The total decision→HEAD diff contains nine paths. Frozen S2.0 planning/corpus/oracles, original/R1/R2 evidence, S2.1 raw evidence and existing review reports have an empty committed diff across that interval. Existing public identity helpers, schemas, graph/native code and producer baseline tests are untouched.

The initial sandbox could read this checkout but could not obtain a reliable Git working-tree status there; the scoped elevated Git inspection established the clean canonical state before any edits. The later [authentication receipt](CI1A-S2.2-Q1-evidence/authentication.json) correctly records the then-present additive reviewer test directory as untracked. It does not mislabel that later state as the initial clean state.

## 2. Evidence integrity and controlling authority

The full S2.2 entry package was read: README, scope/entry criteria, obligation matrix, draft commission, risk register, owner handoff, execution-readiness handoff, mapping contract and both gate packages, together with the entry-acceptance and Option A decisions. The issued entry decision and execution handoff control the historical draft's superseded WP numbering. WP1–WP3 in this report use the issued numbering.

Evidence authentication independently reproduced:

| Package | Trust/check | Actual result |
| --- | --- | --- |
| Frozen R2 | Accepted external manifest hash `BCE7DE2A98602E60C7631757B30EBD05B7B388D65FED69E50A43AE0CEC848B13` | Exact match; all 100 entries match size/hash: 8 planning, 17 fixture, 12 oracle, 63 evidence files; existing verifier also verifies Git branch/pinned ancestry. |
| F12 Q3 | Accepted external reviewer-manifest hash `9EFC098528340178D6A239A9C0137F8F40B8C62ACE2B1D5546561C1B56CCF52A` | Exact match; all 338 listed entries match size/hash, including prior review copies and archives. |
| S2.1 producer | Source/evidence manifest's Git blobs and pinned implementation | All 14 source/test and 65 evidence/documentation canonical blobs authenticate; source blobs match `c02d5eae`. Current WP1/WP2 source is intentionally newer, so the old source manifest is not misused as a current-source verifier. |
| S2.1 reviewer | Original 1,635-entry manifest plus corroborating manifest | Reproduced accepted limitation exactly: 1,615 match in place, six preserved copies match, 13 superseded documents and one subsequently rewritten log differ; the corroborating 36-entry manifest verifies 36/36. No new integrity failure. |

The trust anchors are inherited from the accepted F12 decision, not generated from the candidate implementation. Git objects and manifests authenticate supplied artifacts; they do not establish a human signature or every historical action. The accepted S2.1 packaging limitation remains a qualification, not a new BLOCKED condition.

The original EXP-02-R2 `PROVENANCE-CONTRACT.md` and EXP-02-Q3 `TARGET-IDENTITY-REVIEW.md` were read from their existing reviewer workspaces, alongside the repository's identity contract and NEW-02-corrected `stage2-acceptance-matrix.md` §2. F12 Q3's canonical-target, cross-view, Option A and provenance-authority reviews were checked against its authenticated package.

Carried qualifications remain unchanged: Q-F4 diagnostic wording, Q-F5 historical branches, A-02 scope composition, S-01 full live source/target chain, F09 inactive scanner, F10/IF-04 reader continuation, F11 correspondence, F16/NEW-02 gating, NEW-01 recovery, NEW-04 merge, SQ-01…SQ-07, D3/D4/D5 and I-05/MD-03. F12 remains a purpose-built, Windows-only, host-toolchain-bound reference with fixture-declared component forms and modeled coverage/provenance/lifecycle. Closing that reference did not qualify production extraction, native publication, complete-empty replacement, crash/power-loss/multi-process behavior or integrated S2.5 acceptance.

## 3. WP1 — decoding, refusal and identity suppression

Inspected `src/Andirit.CSharp/VerifiedCSharpSource.cs:24` through its return, `VerifiedCSharpCompilation.cs:97` and the full `CSharpCodeGraphPlugin.Compilation.cs` preparation path. Every source reaching verified preparation, including project/solution inputs, passes through `ReadAsync`. Stream acquisition and lifecycle materialized-buffer acquisition converge on the same decoder. The materialized branch relies on the previously accepted lifecycle-owned buffer/hash invariant; it is not an independent re-hash of an arbitrary caller buffer. The reviewer exercised this existing private factory by reflection without changing its accessibility.

The supported decoder policy is strict UTF-8 ±BOM, strict recognized-BOM UTF-16 LE/BE and UTF-32 LE/BE. UTF-32 is tested before its UTF-16 prefix. When no recognized BOM is consumed, decoded U+0000 causes `AmbiguousBomlessEncoding:<path>`. Decoder failure causes `InvalidSourceEncoding:<path>`. Existing hash-plan/hash-mismatch refusal takes precedence via `??=`; repeated inputs produce the same refusal. Graph fallback decoding retains the refusal, and `Create` rejects it as `UnverifiedSource:<reason>` before returning a descriptor. A source's honest raw content hash is still visible on refusal; suppression concerns verified compilation proof, not erasure of the byte digest.

| Category | Independently observed boundary |
| --- | --- |
| Supported successful decodes | UTF-8 with/without BOM, non-ASCII `café 中文 😀`, CRLF/LF preserved; all four BOM UTF-16/32 variants reproduce text; empty/one-byte input and BOM-only UTF-16/32 accepted. |
| Guaranteed by the bounded rule | Any successfully decoded BOM-less UTF-8 text containing NUL refuses, including ASCII-range BOM-less UTF-16/32 and a literal UTF-8 NUL. Strict decoder failures refuse. All tested refusals preserve the exact byte SHA-256 and fail verified construction. |
| Malformed/truncated sequences | `EF`, `EF BB`, `FF`, `FE`, `FF FE 00`, `00 00 FE`, truncated UTF-32 payload, unpaired UTF-16 surrogate, out-of-range UTF-32 scalar, overlong UTF-8 `C0 AF`, and lone continuation `80` refuse as `InvalidSourceEncoding`. |
| Recognized BOM plus NUL | UTF-8 `EF BB BF 00` succeeds as decoded NUL; the ambiguous-BOM-less rule intentionally does not apply. Source provenance acceptance does not certify valid C# syntax. |
| Known heuristic limitation | Bytes `41 41` can mean BOM-less UTF-16 U+4141 or ordinary UTF-8 `AA`. They decode/verify as `AA`. No detector can recover the producer's intended encoding from these bytes alone. This is within the explicitly accepted heuristic, not an additional defect. |
| Unsupported/untested | Other encodings, universal UTF-16/32 detection, arbitrary BOM-less non-NUL UTF-16 interpretation, exhaustive Unicode sequences and cross-platform decoding are not qualified. No supported encoding was added. |

All input bytes, exact hash, encoding label, decoded text and refusal are in reviewer TRX and [results.json](CI1A-S2.2-Q1-evidence/results.json). There are 28 independent encoding cases, plus the materialized-handoff/hash-precedence probe. No path bypassing the policy to obtain current verified compilation proof was found. Existing graph extraction may still consume refusal-marked fallback text under its historical contract; it does not upgrade that graph path into verified occurrence evidence.

**WP1: SUPPORTED WITH QUALIFICATIONS.** Q-F2 is independently supported at the accepted bounded heuristic; owner acceptance remains separate.

## 4. WP2 — the exact metadata guarantee and counterexamples

`VerifiedMetadataReference.Capture` is the sole reference-evidence constructor; its constructor is private. Platform preparation uses it. `VerifiedCSharpCompilation.Create` requires the exact captured `Reference` object and rejects unsupported properties, duplicate/uncaptured references and unverified dependency associations. No second identity-exposure constructor or lazy successful fallback was found.

Positive controls capture an emitted assembly and verify the exact byte digest. Independently emitted netmodules without an assembly manifest, empty/tiny/garbage files, damaged metadata-root signature, invalid metadata stream offset, and oversized TypeDef table count refuse as `InvalidDataException("UnsupportedMetadataReference")`. Cancellation remains `OperationCanceledException`; missing-directory IO remains `DirectoryNotFoundException`. Catching every exception as malformed metadata would erase these distinctions and is not required.

Eight truncation boundaries were tested on a deterministic 2,048-byte emitted image whose metadata starts at byte 616 and ends at byte 1,400:

| Retained bytes | Result |
| --- | --- |
| 1, 64, 616, 636, 682, 1,399 | `UnsupportedMetadataReference`; no returned capture |
| 1,400 and 2,047 | Capture succeeds; metadata extent is intact, later PE bytes are absent |

The latter cases invalidate a universal “all truncated PE images refused” claim. They alone do not prove unusable metadata: metadata consumers need not require trailing sections/debug/padding. The stronger counterexamples below corrupt required metadata itself.

### Q1-F01 — deferred malformed metadata still receives proof

**Severity:** Significant. **Class:** behavioral contract failure and associated documentation overclaim. **Affected contract:** Q-F3, entry acceptance §3.2, producer evidence §3 WP2; source comment `VerifiedCSharpCompilation.cs:37` claims “full assembly/PE validity.”

Reproduce with reviewer `ObserveMalformedMetadataStillExposesIdentity`, using `--filter FullyQualifiedName~ObserveMalformedMetadataStillExposesIdentity`. Emit `public class First { public int M() => 1; } public class Later { public int N() => 2; }`, then replace one two-byte heap index with `FF FF`:

| Field | File offset | SHA-256 of damaged image | Capture | Independent deeper read |
| --- | ---: | --- | --- | --- |
| First TypeDef name (row 1, `<Module>`) | 830 | `14ede20fe8ae1585df478f8d3f6a0ebbf1c1501a0c501ebde2bf4828dc5799c7` | Returns identity | `GetString(GetTypeDefinition(row1).Name)` throws `BadImageFormatException` |
| Later TypeDef name (row 3) | 858 | `584974319d734cfb6c452095975335898981ee6261d6bf809e44d2e135116097` | Returns identity | Reading that type's name throws |
| First MethodDef signature blob index | 878 | `506edf234f78729295ce75c22e619211f3c7c745a9f1786d5f056700d9ccc6c7` | Returns identity | `GetBlobBytes(method.Signature)` throws |

The exact offsets are calculated from `PEHeaders.MetadataStartOffset` plus metadata table offsets/row sizes; the test asserts the heap-size assumption. These inputs pass initial assembly identity/table access but have invalid metadata handles beyond their heaps. The captured digest remains honest. This is not byte substitution or a claim of successful semantic binding.

**Root cause:** `AssemblyMetadata.GetModules()` forces assembly identity decoding, and metadata-reader construction validates important layout/table extents. `GetAssemblyDefinition()` and `GetTypeDefinition(first)` return row accessors; discarding those accessors does not dereference every field or validate other rows, strings, blobs, signatures or PE sections. Reading one row never validates the entire image. In contrast, a corrupted assembly-name heap index at offset 996 refuses during `GetModules()`; that useful boundary is real.

**Precise supported guarantee:** an immutable image was captured and hashed; Roslyn can materialize assembly metadata, obtain modules, recognize assembly manifests and reach the selected row accessors, and failures encountered by those operations are refused before return. It is not a guarantee that all assembly metadata is usable, that every malformed/truncated PE refuses, that the entire PE/IL is valid, or that a consuming compilation succeeds.

**Required correction:** the owner must settle an accurate bounded metadata-validity contract. To retain the chosen usable-metadata guarantee, validate the metadata fields/tables required by the supported reference/binding path and add these adversaries; document remaining deferred reads and exception boundaries. Alternatively record an explicit narrower unsupported-reference/validity qualification and independently qualify dependent refusal behavior before semantic acceptance. Replace “full PE validation” with the exact established guarantee. This review does not demand a universal IL verifier, semantic compilation success, or unrelated PE hardening.

The first reviewer run incorrectly expected the damaged assembly-name case to capture: **42 passed/1 failed**. Its stack trace showed eager assembly-identity decoding. The probe was corrected to require that refusal, while retaining the three observed type/blob counterexamples. [reviewer-first.log](CI1A-S2.2-Q1-evidence/reviewer-first.log) and its TRX preserve this reviewer error. The final observation tests pass because they assert the actual counterexamples; they do not certify Q-F3.

Multi-module assemblies, mixed/native images, every metadata table/signature, IL bodies, resources, mid-read file replacement/cancellation, >128 MiB limits and resource exhaustion were not exhaustively probed. Their behavior is not inferred from these tests.

**WP2: REMEDIATION REQUIRED.**

## 5. WP3 — symbol hardening and Option A mapping

A-01 is **SUPPORTED**. Comma, pipe, mixed injections, and bare separators all deterministically throw `UnsupportedParseConfiguration`. Reversing `TRACE`/`FEATURE` preserves the descriptor digest and yields `FEATURE,TRACE`. Composed `é` versus decomposed `e\u0301` remains ordinally distinct; no Unicode normalization silently collapses them. The changed API is internal; signatures and frozen source canonical form are unchanged. Rejection concerns the decoded symbol characters, so a separator cannot escape the check by coming from an encoded source file. Arbitrary invalid C# identifiers or exhaustive Unicode parsing are outside this bounded correction.

### Independent reconstruction

Reviewer `Mapped` serializes the documented length-prefixed tuple independently of production hashing. For a verified `App → Lib → Leaf` graph it takes the **defining** descriptor's own byte hashes/options/metadata/module and reconstructs each project-reference item from the actual live dependency's assembly/metadata and ordered decoded-tree text hashes. It does not relabel `compctx2` as `compctx1`. It agrees with the frozen authority `CompilationProvenance.Describe` for all six baseline/changed contexts; that frozen source is linked read-only into the separate reviewer project.

The fixture uses BOM UTF-8 own-source bytes, so own-source byte hashes and dependency UTF-8 text hashes differ deliberately. Leaf symbol-only context change propagates `compctx2` at all three levels while mapped Lib/App `compctx1` stays unchanged. Lib mapped context is `compctx1:sha256:c5a09996c6db673de7b8418d9ccf5604de721a5de4b7d6c8df15549a11877a62`; its reviewer synthetic-component target remains `asmctx3:sha256:e8d2820b7ae6c825225bc6b96b03933c40db47116c4303a8e7a76a454d70571f`. Leaf's own mapped context changes because its own accepted parse inputs change. These are reviewer vectors, not new frozen production expectations or general source-canonicalization rules.

Reference reordering preserves strong/mapped contexts. Missing dependency association refuses `ReferenceMembershipMismatch`; substituting a same-unit changed live dependency refuses `MissingDefiningCompilation`. A complete universe accepts the genuine live objects and refuses a changed instance. Replacing declared source membership preserves `analysisscope1` but fails universe construction with `ScopeSourceMembershipMismatch`, independently supporting A-02. Production live target/declaration resolution is still absent; these are foundation association checks, not implemented minting proof.

### Q1-F02 — frozen corpus and live reference preimages are not specified as the same profile

**Severity:** Significant specification/evidence gap. **Affected contract:** mapping §2, G-MINT §2(3)/§5, F16 row 5. **Reproduction:** `ObserveFrozenCorpusReferenceProfileDiffersFromLiveCapture` builds accepted Leaf/Core inputs, replaces PE references with verified captures and dependencies with their verified live equivalents, then applies the documented map.

| Defining unit | Frozen R2 `compctx1` | Mapped live-capture `compctx1` |
| --- | --- | --- |
| Leaf | `404c5e48c3106b47786f14cad594139477c651fd78d0e050f8fd5bcab18fcb5f` | `b816dcb5512588bc3b5ad01692c20241b31442ab420c8a4b5a6d06d032b9c59b` |
| Core | `942a8e588a8db15afafb30d0292e0b867c767c5f72392b6f458783d961a71db3` | `df8d2932670fc6a15cec10930b0bf573cfce841080e55f9544433983cbbe8a77` |

All values in the table have prefix `compctx1:sha256:`. Source bytes, parse settings, options, assembly and project-unit inputs are held fixed. The difference is PE reference labels: production preserves `file|C:/Program Files/dotnet/shared/Microsoft.NETCore.App/10.0.12/System.Runtime.dll|76be…`, while the frozen corpus deliberately uses `file|runtime/System.Runtime.dll|76be…` (and analogously for the other two references). The actual strings/digests are in [results.json](CI1A-S2.2-Q1-evidence/results.json). Replacing **only the reviewer comparison's** PE reference labels with the authenticated frozen profile reproduces both exact frozen anchors. No production or frozen identity was rewritten.

**Root cause/bounded conclusion:** the map is faithful to original R2 `Describe` for the same construction inputs. But “keep verified file identity unchanged” and “reproduce frozen corpus values” are not interchangeable when the fixture intentionally changes the preimage labels. This is not a hash collision or proof that Option A is unsound. Production preparation also uses its supported platform-reference inventory rather than the oracle's three-reference fixture; like-for-like comparisons must control the entire input profile.

**Required correction:** specify the comparison boundary before row-5 test authoring: distinguish algorithm fidelity over equal verified inputs from exact frozen-corpus profile reproduction; enumerate reference identities/inventory and any **test-only** adaptation, and retain exact source/parse/options preimages. Do not silently normalize production identity fields or alter frozen expectations. Record which exact output is required in each comparison. This is currently an underspecified review input, not simply a missing future test run.

### Q1-F03 — shape validation is described as provenance validation

**Severity:** Minor documentation overclaim with a consequential future trust-boundary risk. **Affected contract:** mapping §6 line 135. It promises fabricated canonical-shaped `asmctx3`/`compctx1` are refused by `CodeOccurrenceTargetId`/`ComputeTargetId`.

**Reproduction/evidence:** reviewer `ObserveTextuallyEquivalentLiveInstancesHaveSameIdentity` constructs `CodeOccurrenceTargetId("asmctx3:sha256:" + 64 zeros)` successfully and supplies `compctx1:sha256:` plus 64 zeros to `ComputeTargetId`, obtaining `asmctx3:sha256:9558232431295cf558cec711030a577467166c493ac747eecfa71103d0ed1020` with the recorded synthetic components. `src/Andirit.Abstractions/CodeOccurrence.cs:31` and `:110` check canonical shape and tuple inputs; they receive no live proof/universe.

**Required correction:** assign fabricated-authority refusal to the future verified resolver/association gate. Keep shape rejection and authority rejection distinct. This is not a request to change the frozen helper or its accepted API; helper shape-only behavior predates this slice and is explicit in the accepted identity contract.

### Q1-F04 — distinct instances are conflated with distinct identity inputs

**Severity:** Minor specification overstatement. **Affected contract:** mapping §6 line 134. Its unqualified “two distinct defining compilations with identical textual descriptors → distinct asmctx3” could require object-instance identity in a deterministic semantic identity.

**Reproduction:** two separately created live compilations, same repository/project/source/options/reference material, have different object references but the same mapped `compctx1` and reviewer target `asmctx3:sha256:abbc456fcd1c507eb095c9fa6879f5bf7f474e629f6fd12356527d01ebe5ee46`. This is the same R2-identified target and is required by deterministic reconstruction. Different defining project/context/component preimages discriminate targets; the same identity never licenses substituting the wrong live instance as binding proof.

**Required correction:** qualify the adversary to mean distinct accepted defining contexts/declarations despite matching display/textual descriptors. Equivalent full accepted inputs must reproduce the same identity. Do not add process/object IDs or `compctx2` to the frozen tuple. No collision of different complete accepted preimages was demonstrated.

### Association and durable carrier boundary

Mapping §3 correctly separates defining live instance, current defining `compctx2`, complete universe membership, target identity and occurrence identity. The carrier is described conceptually as a separate per-occurrence provenance record and later native occurrence-unit/meta storage. It is **not** a concrete implemented model with keys, serialization, aggregation rules or reopen checks. Implementation must unambiguously associate each fact with its defining unit/current context and universe; a lone scope/universe digest or caller boolean is insufficient. Defining and consuming contexts must never be substituted for one another.

The later entry decision §3.5 expressly defers native persistence/reopen to S2.3, whose first gate must reject prior facts under stale defining context. That accepted deferral controls earlier broad Option A wording. No after-reopen enforcement or durable production capability is established here. If the carrier cannot support it, stop for the owner's Option B decision before production reliance.

**WP3 overall: REMEDIATION REQUIRED** for the mapping-contract corrections; A-01 itself is supported. General source component canonicalization and production mapping implementation remain unimplemented rather than retrospectively qualified by this specification.

## 6. G-CONFIRM — row-by-row readiness and sequencing

Reviewed version: `g-confirm-package.md` and execution-readiness handoff §D at evidence HEAD `5cae7f0`, against `stage2-acceptance-matrix.md` §2 at the same HEAD, the accepted identity contract, original R2 §§4–5, EXP-02 Q3 target review, and authenticated F12 R2/Q3. The exact R2/Q3 trust hashes are in §2 above.

The NEW-02 matrix's exact `usage4` order, semantics/category, `asmctx3` component domains, length-prefix encoding, invariant coordinates and source-unit/storage-contributor distinction are **independently confirmed**. Reviewer `New02MatrixTupleAndFrozenUsageAnchors` reproduces both accepted `bd0487…` / `b874bd…` vectors with an independent encoder and compares production. All 13 usage tuple positions and seven target-tuple positions discriminate when changed; the existing nine identity-remediation tests also pass. This confirms the identity contract, not a production live-binding gate.

Every row has the nine required planning fields. “Evidence pending” below never counts a planned artifact as executed. R2 is the semantic reference where applicable; rows 1–4/11/14 are principally owner contracts or review procedures, so no invented R2 numeric expectation is assigned to them.

| Row | Authority confirmable now? / expected-value basis | Implementation evidence now and still pending | Circularity guard | Dependent gate |
| --- | --- | --- | --- | --- |
| 1 Q-F2 | Yes; entry §3.1/S21A §F define the exact bounded rule, not R2 semantic rows. | Producer 7-case suite plus reviewer encoding probes support current boundary; owner closure pending. | Use explicit input bytes/decoder contract, not extraction output as expectation. | WP1, S-01, G-ACCEPT |
| 2 Q-F3 | Yes as an accepted validity/refusal requirement; its achieved strength is challenged by Q1-F01. | Positive/netmodule/layout refusals support a narrower boundary; malformed type/blob acceptance requires correction or explicit owner qualification. | Independent deeper metadata reads expose the gap; a passing producer test is insufficient. | WP2, semantic acceptance, minting prerequisite |
| 3 A-01 | Yes; entry §3.3/S21A §F. | Comma/pipe refusal and ordinal behavior independently reproduced. | Fixed separator inputs and descriptor digest comparison. | WP3; caller parse reuse/API boundary |
| 4 A-02 | Yes; scope is unit/path identity; universe/descriptors enforce source membership. | Reviewer same-scope/different-source-membership falsification and live-universe checks support foundation; extraction use still pending. | Scope digest equality cannot establish inventory/completeness. | WP4/WP5/WP7, G-ACCEPT |
| 5 mapped compctx1 | **Partial:** frozen tuple/legacy project item accepted and independently reconstructable, but exact frozen-versus-live PE comparison profile is inconsistent (Q1-F02). | Independent synthetic equal-input vector and Leaf/Core preimage-difference reproduction exist; production mapper absent. | An expected digest generated by the production mapper, or comparison after an undocumented field rewrite, would be circular. | G-CONFIRM comparison plan, WP3, G-MINT |
| 6 compctx2 isolation | Yes; Option A §5.1(3)(7), frozen identity tuples. | Independent reconstruction keeps strong provenance separate; direct compctx2 supplied to target helper refuses. Future verified resolver/association negatives pending. | Shape validity is not live authority (Q1-F03); do not encode strong context into the target. | G-MINT, WP5/WP8 |
| 7 live target minting | Yes; Option A §5.1(2), R2 defining-context and Q3 correspondence authority. | Foundation live dependency checks exist; production unique binding/absent/external/ambiguous refusal and per-declaration correspondence pending. | Never infer authority from matching docs, assembly names or candidate descriptors. | WP5, G-MINT, S-01 |
| 8 invocation fidelity | Yes; stage boundary §§3.6–3.7; R2 actual Roslyn token/full-declaration/source evidence, with fixture-component limits. | Production occurrence extraction/token/hash evidence pending. | Expected spans/source bytes from accepted inputs/independent Roslyn inspection, never extractor output. | WP4/WP9, S2.5 |
| 9 coverage/refusal | Yes; accepted coverage contract and bounded R2 classification models. | Production coverage emission and complete-empty/refusal matrix pending. | Model output is not proof of real production completeness. | WP7/WP9; S2.3/S2.4 for durable/query claims |
| 10 inactive scanner | Yes; F09 and declared parse configuration; accepted inactive-site semantic expectations. | Real disabled-text scanner/test evidence pending. | No inactive site becomes a confirmed fact merely because a model labels it. | WP7/WP9 |
| 11 NEW-04 merge | Yes; entry §3.8 exact mismatch/count/state rules; R2 explicitly does not use defective merge as oracle. | Correction and independent scope/universe/count falsification remain pending. | Never use current Merge as its own expected-value oracle. | WP6 before coverage reliance/G-ACCEPT |
| 12 S-01 chain | Yes; source→buffer→tree→live compilation→binding→target correspondence, accepted R2/Q3 bounded reference. | S2.1 foundation plus reviewer source/dependency checks supported; per-fact production chain pending. | Same exact compiled bytes/live defining instance must support every fact. | WP4/WP5/WP9; S2.5 integrated proof |
| 13 Option A association | Yes; Option A + later entry §3.5; frozen A02 separates identity equality from invalid provenance. | Reviewer context propagation/no-churn proof supports algebra; production separate carrier and stale-fact enforcement pending; native reopen deferred. | Derive current/universe from verified inputs independent of candidate; asmctx3 equality proves no freshness. | WP8, G-MINT pre-mint association prerequisite; S2.3 first reopen gate |
| 14 F16/NEW-02 review | Yes; register F16/NEW-02 and F12 acceptance §8. | This report supplies independent matrix review and names row-5 inconsistency; no whole-slice closure. | Reviewer reviews fixed authority and row completeness, not a producer's claim that the reviewer has passed it. | Pre-authoring G-CONFIRM; complete evidence before G-ACCEPT |

**Sequencing resolution:** G-CONFIRM checks authority, traceability and the expected-value plan **before** dependent qualification-test authoring. It does not require WP4–WP9 producer executions to exist then. Those executions later verify implementation against already confirmed expectations and populate F16 evidence for G-ACCEPT. Rows 4–14 therefore legitimately retain pending implementation evidence. The row-14 review records this distinction rather than using future tests to confirm their own authority.

**Readiness verdict:** NEW-02 corrected tuple authority is confirmed, and the other row authorities resolve. The whole gate is **NOT PASSED** because row 5 lacks a consistent exact comparison profile required to ground dependent expected values. Correct that planning defect and record independent confirmation before dependent test authoring. Q1-F01 separately blocks a successful Q-F3 implementation closure; lack of future row-7–13 test runs is not used as a pre-authoring blocker. No WP4–WP9 tests were authored by this review; the standalone reviewer foundation probes are the explicitly commissioned qualification work.

## 7. G-MINT — required implementation and falsification

The §2 mapping formula is precise enough to reproduce original R2 algebra for equal verified inputs, including dependency-bearing defining contexts. It is not yet an implementable complete **authority/minting** contract without Q1-F02–F04 clarification and a concrete checked association model. There is no production mapper or resolver introduced by this slice, and dependency-bearing `LegacyDescriptor` deliberately stays null.

Before WP5, a separately authorized implementation must provide: deterministic mapped defining `compctx1`; exact live defining-declaration/source correspondence; complete verified universe/current-context checks; a separate per-fact defining-context association whose absent/stale/substituted value refuses; explicit refusal classification; isolated frozen target/usage inputs; and accepted NEW-04 disposition before any coverage reliance. Implement and independently execute the following probes against those surfaces, not only reviewer reconstruction:

| Option A §5.2 / mapping falsifier | Required before production minting | Evidence in this review / remaining stage |
| --- | --- | --- |
| 1 E1 conditional defining API | Real defining parse/API change alters mapped target; old supporting context invalid. | Frozen oracle and reviewer tuple sensitivity support authority; production gate pending. |
| 2 E2 transitive-only | Same defining R2 material → stable target, changed compctx2 at all levels, prior association refused. | Strong propagation/no-churn independently demonstrated; stale production fact refusal pending. |
| 3 E3 caller unchanged | Target follows defining conditional API, independent of consuming source. | Frozen accepted oracle supports expectation; production correspondence pending. |
| 4 restoration | Restore all inputs → exact original identities and valid association. | Frozen oracle checks and separate-process reconstruction support expectation; production mapper restore probe pending. |
| 5 T03/T10 | Different accepted defining contexts/declarations discriminate; equivalent full inputs remain equal. | Q1-F04 counterexample requires accurate case wording; production live resolver pending. |
| 6 T07 reference equivalence | Same accepted reference identity reproduces identity; changed path is not silently “equivalent.” | Exact captured hashes/reordering and Q1-F02 profile probe; production comparison policy pending. |
| 7 T08 determinism | Repeated/separate-process reconstruction and later re-ingestion agree for equal inputs. | 50 reviewer observations stable across two processes; production re-ingestion not qualified. |
| 8 T09 stale evidence | In-memory candidate/current/universe mismatch refuses before fact emission. | Production association gate pending. **Native reopen part deferred to S2.3 by entry §3.5.** |
| 9 mapped dependencies | Independently reconstruct original R2 project item and compctx1, including source-byte versus tree-text distinction, ordering and normalization. | Reviewer synthetic vectors and exact Leaf/Core aligned-profile anchors reproduce; production implementation pending. |
| 10 fabricated/incomplete/isolation | Missing, unverified, stale, out-of-universe, duplicate/ambiguous or substituted live contexts cannot mint; compctx2 never becomes a target/usage tuple field. | Foundation substitution/membership/compctx2 rejection supported; production minting/refusal negatives absent. |

### Q1-F05 — G-MINT falsification list needs the accepted stage partition

**Severity:** Minor gate-document inconsistency. **Affected contract:** `g-mint-package.md` §4 and mapping §7 request Option A §5.2 tests 1–10 wholesale before minting, while test 8 includes native reopen and the later accepted entry §3.5 excludes it from S2.2.

**Reproduction:** read those lists together with Option A §5.2(8), entry §3.5 and execution handoff WP5/WP8. Applying the list literally would require unauthorized S2.3 work to pass a S2.2 gate, and could leave stale-association prerequisites scheduled after minting.

**Required correction/qualified interpretation:** record the above pre-mint in-memory subset explicitly; retain native reopen as the first separately authorized S2.3 gate. Schedule the necessary mapper/live-association falsification before WP5 even if later integration is assigned WP8/WP9. The existing accepted durability deferral is not reopened or waived here. Future-case planning may precede G-MINT after G-CONFIRM, but no production minting may precede the required independent pre-mint checks.

The proposed durable carrier is a design direction, not evidence of implementation or enforceability. Missing in-memory authority/association currently blocks minting. Lack of S2.3 native reopen proof is an accepted later qualification, not a demand to implement S2.3 now. An unenforceable design discovered later triggers the specified owner/Option B escalation.

**G-MINT: NOT PASSED.** A documented map, a reviewer demonstrator, and passing algebra tests cannot substitute for implemented and independently qualified production gates.

## 8. Executed tests, commands and evidence

Environment: Windows x64, OS 10.0.26200, SDK 10.0.401, .NET runtime 10.0.12, MSBuild 18.9.11, Roslyn package 5.9.0. The reviewer project uses the existing `Andirit.CSharp.Tests` friend-assembly name in its own output directory; it runs separately from producer tests and is not added to the solution. The linked frozen sources are not edited. See [environment-dotnet.txt](CI1A-S2.2-Q1-evidence/environment-dotnet.txt).

| Run | Actual result |
| --- | --- |
| Decision baseline from Git archive, no branch switch/worktree | **645 passed / 0 failed / 0 skipped**: core 325, native 43, TreeSitter 157, C# 120 |
| Reviewed HEAD full solution before reviewer additions | **652 / 0 / 0**: core 325, native 43, TreeSitter 157, C# 127 |
| Producer foundation target | **7 / 0 / 0** |
| Existing frozen identity-remediation target | **9 / 0 / 0** (also included in full solution) |
| Frozen R2 oracle | **185/185**, zero failed; raw output SHA-256 `97f6c5e5a4cc77e089481c7643732b7a07a31d483c2643bd7a95fdad7ced39c9`, identical to the accepted Q3 output |
| Reviewer first version | **42 passed / 1 failed**; defective assembly-name expectation corrected transparently (§4) |
| Expanded reviewer version | **46 / 0 / 0** |
| Completed reviewer suite, two separate processes | **48 / 0 / 0 twice**; 50 logged observations agree after excluding only random metadata temp-file paths |

TRX run IDs/timestamps/durations are not claimed byte-identical. The comparison checks exact logged input bytes, mutation hashes, encoding/refusal results and mapped identities; only the captured metadata identity's random temporary filename is excluded. Hashes still compare exactly. No passing count erases the metadata counterexamples or open production gates.

Exact executed command forms (PowerShell; repository cwd unless stated; `$reviewStage` is `C:\Users\Laszlos\source\repos\Solo\andirit-ci1a-s2.2-q1`):

```powershell
git status --short --branch
git branch -vv
git worktree list --porcelain
git stash list
git log --format='%H %T %P' -4
git diff --name-status febc28d HEAD
git diff --numstat febc28d 0c989e2
python -X utf8 "$reviewStage\authenticate.py" D:\projects\Andirit "$reviewStage\authentication.json"
python -X utf8 docs/evidence/adr-ci1-01-ci1a-s2-0-r2/verify-manifest.py
dotnet --info
dotnet test Andirit.slnx --nologo -v minimal --logger trx --results-directory "$reviewStage\baseline-trx"
git archive --format=zip --output="$reviewStage\decision-baseline.zip" febc28d92548fd7323294077c37444048741a8c8
Expand-Archive -LiteralPath "$reviewStage\decision-baseline.zip" -DestinationPath "$reviewStage\decision-baseline"
dotnet test "$reviewStage\decision-baseline\Andirit.slnx" --nologo -v minimal --logger trx --results-directory "$reviewStage\decision-baseline-trx"
dotnet test tests/Andirit.CSharp.Tests/Andirit.CSharp.Tests.csproj --no-restore --nologo -v minimal --filter 'FullyQualifiedName~S2_2FoundationBoundaryTests' --logger 'trx;LogFileName=producer-targeted.trx' --results-directory "$reviewStage\producer-targeted-trx"
dotnet test tests/Andirit.Tests/Andirit.Tests.csproj --no-build --no-restore --nologo -v minimal --filter 'FullyQualifiedName~OccurrenceIdentityRemediationTests' --logger 'trx;LogFileName=frozen-contract.trx' --results-directory "$reviewStage\frozen-contract-trx"
dotnet run --project docs/planning/adr-ci1-01-ci1a-s2-0/oracle/SemanticCorpusOracle.csproj -- --corpus docs/planning/adr-ci1-01-ci1a-s2-0/corpus --expected docs/planning/adr-ci1-01-ci1a-s2-0/oracle/expected/oracle-expected-r2.json --output "$reviewStage\oracle-r2.json"
dotnet test docs/reviews/CI1A-S2.2-Q1-tests/ReviewerTests.csproj --nologo -v minimal --logger 'trx;LogFileName=reviewer-complete.trx' --results-directory "$reviewStage\reviewer-trx"
dotnet test docs/reviews/CI1A-S2.2-Q1-tests/ReviewerTests.csproj --no-build --no-restore --nologo -v minimal --logger 'trx;LogFileName=reviewer-repeat.trx' --results-directory "$reviewStage\reviewer-trx"
python -X utf8 "$reviewStage\collect-results.py" "$reviewStage"
```

Earlier reviewer invocations use the same `dotnet test` command with `reviewer-first.trx` and `reviewer-final.trx`. Raw stdout/stderr logs and TRX are preserved in [CI1A-S2.2-Q1-evidence](CI1A-S2.2-Q1-evidence/). `authenticate.py` and `collect-results.py` preserve exact audit/comparison logic. The baseline ZIP/extracted build remains in external review staging rather than adding a redundant source archive to this repository. The additive evidence manifest excludes itself, the report and reviewer source; Git binds the report/tests, and the manifest binds the included raw evidence files.

## 9. Residual qualifications and unsupported conclusions

No universal encoding detector, full PE/IL verifier, general source canonicalizer, arbitrary-repository semantic guarantee, native durability proof, Linux qualification or production completeness claim follows. Canonical identity shape is not provenance. Semantic binding success is not metadata structural validity, and metadata capture success is not semantic compilation success. All accepted F12/S2.1 limitations remain at their assigned gates.

Reviewer hashes prove the specified algebra and expose input-profile differences. They do not implement production association or prove that compctx2 can never be smuggled into a future unimplemented resolver. The current supported claim is that the frozen tuple excludes it and the legacy target helper rejects a direct compctx2 context. Future consuming code must be tested independently.

No evidence of frozen-source alteration, historical-report rewriting, a new identity version, or unrelated production changes was found. Authentication succeeded, so the overall verdict is REMEDIATION REQUIRED rather than BLOCKED. The actual defects, documentation overclaims, unimplemented future capabilities and known heuristic limitations are distinguished above.

## 10. Owner decisions and remaining gates

1. Resolve **Q1-F01**: retain a meaningful usable-metadata boundary with bounded validation/remediation and independent tests, or explicitly accept a narrower supported validity/refusal contract. Do not close Q-F3 using “full PE validation” or the seven producer tests alone. Production correction was outside this review and remains undone.
2. Resolve **Q1-F02** before confirming row 5: specify the same-input/frozen-fixture comparison profile without altering frozen tuples, evidence or silently rewriting production references. Independently review that correction, then record G-CONFIRM authority/traceability disposition. The NEW-02 tuple correction itself is confirmed here.
3. Correct **Q1-F03/Q1-F04** in current mapping/gate documentation: future live proof must refuse fabricated authority; equivalent full R2 inputs must remain deterministic across distinct instances. This requires no frozen helper change or new identity version.
4. Record **Q1-F05**'s pre-mint versus S2.3-reopen partition and schedule actual authority/association checks before WP5. The accepted option-(a) durability boundary remains in force.
5. Separately authorize any mapper/extraction/minting follow-up. Implemented mapping, live correspondence, complete universe/current-context checks, separate association, refusal/isolation/determinism probes and independent G-MINT review remain mandatory before targets are minted. This report authorizes none of WP4–WP9.
6. Preserve NEW-04 correction/independent qualification before coverage reliance, WP7/WP8/WP9 and G-ACCEPT evidence, Q-F2 owner acceptance, full S-01 correspondence and all F16 evidence. Foundation review is not full S2.2 acceptance.
7. S2.3 still requires separate authorization, NEW-01 recovery closure before reliance, and its first stale-provenance-after-reopen test. S2.4/S2.5/Stage 6, publication and integrated acceptance stay separate. Escalate to the owner for Option B if durable association cannot be enforced; do not quietly weaken validity or churn frozen target identity.

The review stops after this independent foundation qualification and an additive local commit on canonical main. Owner acceptance and every dependent implementation authorization remain separate decisions.
