# ANDIRIT-CI1A-S2.2-Q2 — Independent foundation requalification

Date: 2026-11-04 (Asia/Manila). Canonical repository: `D:\projects\Andirit`, `main`. Candidate: R1 evidence `b4eff01bb736f1725543fc8ba928ff313d65c4d6`. This is independent technical qualification, not owner acceptance or authorization of another work package.

**Overall qualification: SUPPORTED WITH QUALIFICATIONS.** Q1-F01 through Q1-F05 are **CLOSED at the bounded correction/specification scope described below**. No new production defect was established within that scope. The achieved metadata guarantee is explicitly narrower than complete metadata validity or general “usable assembly metadata”; owner acceptance of that boundary remains separate. Production mapping, semantic extraction, pre-mint association enforcement, durability and reopen are not established capabilities.

**G-CONFIRM remains OPEN. G-MINT remains OPEN.** This Q2 report supplies readiness evidence only. No gate verdict, dependent test-authoring permission, WP4–WP9 authorization, target/association minting, S2.3 work, push or publication is issued. Stop after Q2.

## 1. Source authentication and preservation

The first reliable, scoped elevated Git inspection established clean tracked/untracked canonical `main`, exactly the requested candidate HEAD. The initial unelevated Git operation outside the workspace produced unreliable work-tree/permission errors; it was not used as a source attestation. No applicable `AGENTS.md` was found at the checkout, its ancestors, or in its tracked/untracked repository inventory. No branch switch, worktree creation, stash, fetch or remote mutation occurred.

| Role | Commit | Tree |
| --- | --- | --- |
| Authoritative Q1 | `38d4e513951c07578953fda7738d8741d2109004` | `ffca6b84dd8c2c1d9eebcbcd0544d5615dcb7e2a` |
| R1 implementation | `d5644d814b83170f553584529c05f5f8c289ba25` | `bcbf65117c9a1f892b987e4e83ed25eab99ee705` |
| R1 evidence / candidate HEAD | `b4eff01bb736f1725543fc8ba928ff313d65c4d6` | `ca448ef7da813a45130ed3a138850a27457de735` |
| Accepted F12 decision | `a1cf47083747bae90293ec074b35fe8078bd0d6e` | `ebba0daf2977315a5c09551df0de698afeb6696f` |
| Qualified frozen R2 | `7f8d035d7fa6bd5da49f71ee86ff54d7350318c5` | `29150eb6ef2b0fe65942f3c8930037f8c0120174` |
| S2.1 owner baseline | `b7fea0d219409b279b78317e32bdc3c8b5ae61ee` | `b4a764637498ed8e8bddac28fb0b0b3716b7ffee` |
| S2.1 implementation | `c02d5eae157653ba7f123b98aceccb01a0b542a0` | `a53a076dbb482062973f70fd11bf4261cc5368f3` |

Trees and ancestry authenticate. Q1 → R1 implementation → R1 evidence are consecutive parents. Implementation changes exactly three paths: adds `MetadataReferenceValidation.cs`, modifies `VerifiedCSharpCompilation.cs`, adds `S2_2MetadataRemediationTests.cs`. The evidence commit changes the current mapping/G-CONFIRM/G-MINT documents and adds the R1 report/evidence; it has no production delta. The Q1→candidate diff has 58 paths, exactly the producer's bounded source/test/docs scope.

Q2 independently checked protected working files against candidate Git blobs and the candidate against Q1: original Q1 report/tests/evidence, frozen planning/oracles/evidence, accepted decisions, WP1 decoder, public identity helpers, scope/universe and pre-existing producer tests remain unchanged. All production and producer-test files still equal candidate HEAD. Only additive Q2 reviewer paths are delivered.

Locally stored `origin/main` is `31c33a934aa13e373d9a3a6ffbc54d789eef092c`; initial main was 37 commits ahead. One canonical worktree and no stashes were verified. No current remote-state claim is made. [Inherited authentication](CI1A-S2.2-Q2-evidence/inherited-authentication.json) records a clean candidate before Q2 additions; [Q2 authentication](CI1A-S2.2-Q2-evidence/authentication.json) honestly records the later untracked Q2 tests.

| Evidence | Independently verified result |
| --- | --- |
| Frozen R2 manifest | Accepted external SHA-256 `BCE7DE2A98602E60C7631757B30EBD05B7B388D65FED69E50A43AE0CEC848B13`; all **100** size/hash entries match: 8 planning, 17 fixtures, 12 oracle, 63 evidence. Existing branch/ancestry verifier also passes. |
| Accepted F12 Q3 | External manifest SHA-256 `9EFC098528340178D6A239A9C0137F8F40B8C62ACE2B1D5546561C1B56CCF52A`; all **338** entries authenticate through the unchanged inherited audit. |
| S2.1 producer | **14** pinned source/test Git blobs and **65** evidence/documentation blobs authenticate; old source expectations are not misapplied to intentionally newer WP1/WP2 files. |
| S2.1 reviewer | Reproduces accepted packaging limitation: 1,615 entries match in place, six preserved copies match, 14 superseded/revised entries differ; corroborating manifest **36/36**. No new integrity failure. |
| Q1 and R1 raw evidence | **33/33 Q1** and **50/50 R1** manifest entries match. Nothing regenerated or overwritten. |
| Frozen oracle output | Q2 replay SHA-256 `97f6c5e5a4cc77e089481c7643732b7a07a31d483c2643bd7a95fdad7ced39c9`, byte-identical to accepted Q3/Q1/R1 output. |

The accepted decisions, original R2 `PROVENANCE-CONTRACT.md`, original Q3 `TARGET-IDENTITY-REVIEW.md`, frozen `CompilationProvenance.cs`, `Corpus.cs`, `R2Identity.cs`, corrected mapping/gate documents, NEW-02 matrix, Q1 findings/probes and R1 implementation/report were inspected. Existing verifiers are corroboration; their green outputs do not substitute for new Q2 adversaries or a review of actual code.

## 2. Finding dispositions

| Q1 finding | Q2 disposition | Independent basis | Retained boundary |
| --- | --- | --- | --- |
| **Q1-F01 — malformed metadata identity capture** | **CLOSED**, demonstrated name/signature defects and full-PE overclaim | Exact three original byte hashes reproduce; each refuses twice before any returned capture. New heap/signature/relationship negatives refuse. Positive controls, runtime inventory and complex emitted generics capture. Source comment and R1 report accurately bound checks. | This closes the demonstrated defects using Q1's explicitly qualified bounded-correction alternative. It does not establish complete metadata usability under entry §3.2's broader wording. Owner must explicitly accept the achieved surface or commission further bounded validation before S2.2 semantic acceptance. |
| **Q1-F02 — inconsistent reference comparison profile** | **CLOSED** | Fresh encoder agrees with frozen `Describe` for identical actual inputs. Independent Leaf/Core reconstruction reproduces actual-path and explicitly aligned fixture digests. Actual host inventory is inspected separately. Mapping §2 and both gate packages identify authority/profile/expected result. | Frozen fixture equality is conditional on all accepted inputs, runtime bytes and labels. No production mapper is implemented or qualified. |
| **Q1-F03 — shape conflated with authority** | **CLOSED** | Canonical fabricated context/target still passes shape helpers as expected. Uncaptured reference, different live dependency instance, wrong repository, mismatched source text and incomplete/refusal-marked universe are refused by the appropriate existing foundation boundaries. Mapping §6 assigns future authority refusal to resolver/association checks. | Foundation refusals are not production per-fact minting refusal. Shape alone never licenses a fact. |
| **Q1-F04 — instance identity conflated with complete inputs** | **CLOSED** | Separately reconstructed compilations and separately captured same-path references reproduce the same established identities. Reordered sources/references preserve canonical values. Changed accepted preimages discriminate; dependency-only provenance changes preserve mapped parent identity. | Equivalent semantic identity does not allow replacing the live binding instance. No process/object identifier or `compctx2` was added to frozen tuples. |
| **Q1-F05 — pre-mint versus reopen gate inconsistency** | **CLOSED** | Current mapping §7/G-MINT §4 explicitly require pre-WP5 in-memory stale/absent/substituted/fabricated/ambiguous/incomplete authority falsification; native persistence/reopen is the separately authorized first S2.3 gate. | Concrete carrier, keys, serialization, aggregation and enforcement remain unresolved/unimplemented. No durable guarantee is credited. |

There are no remaining Q1 findings classified OPEN or INSUFFICIENT EVIDENCE within those stated correction scopes. There are no new demonstrated production defects to assign severity or remediation. The qualifications in §8 are real unsupported/untested boundaries, not speculative defects. Closure is a reviewer technical disposition; owner acceptance remains separate.

## 3. Independent metadata validation

`VerifiedMetadataReference.Capture` still privately constructs evidence only after immutable acquisition, SHA-256, Roslyn assembly materialization, manifest guard, new validation and final cancellation check. Hash/path identity construction is unchanged. No alternate successful proof constructor or lazy fallback was introduced. Existing `BadImageFormatException`/`InvalidOperationException` translation remains `InvalidDataException("UnsupportedMetadataReference")`; cancellation and IO retain distinct classifications.

Review of the actual validator establishes this surface:

- Every TypeRef and TypeDef name/namespace is dereferenced; required names must be nonempty. This is heap access plus nonemptiness, not a C# identifier grammar or complete UTF-8/name-format validator.
- Every MethodDef name and signature blob is dereferenced. The iterative parser consumes the full signature; checks headers, instance/static agreement, counts, placements, local type-token row bounds, local generic indices/arity, arrays and nested function-pointer traversal; rejects trailing bytes. Custom-modifier TypeSpec rows/blobs are checked for accessibility, not recursively certified syntax.
- Type/method GenericParam collections check local owner/index order. Method owner must be non-nil. Param names are read; sequence numbers are unique and within signature count, including legal return sequence zero. Missing Param rows remain legal. Constraints, external arity/type resolution and all unrelated tables are not validated.

The [official .NET decoder source](https://raw.githubusercontent.com/dotnet/runtime/main/src/libraries/System.Reflection.Metadata/src/System/Reflection/Metadata/Ecma335/SignatureDecoder.cs) corroborates the distinction between decoding and provider/type resolution; it is format context, not an oracle establishing this candidate's correctness. Q2 evidence comes from the local source review and executions.

| Original input | Exact SHA-256 | Historical Q1 | Q2 independent positive-remediation probe |
| --- | --- | --- | --- |
| First TypeDef name, offset 830, `FFFF` | `14ede20fe8ae1585df478f8d3f6a0ebbf1c1501a0c501ebde2bf4828dc5799c7` | Captured identity; deeper read failed | Refuses twice; no returned capture |
| Later TypeDef name, offset 858, `FFFF` | `584974319d734cfb6c452095975335898981ee6261d6bf809e44d2e135116097` | Same defect | Same deterministic refusal |
| MethodDef signature heap index, offset 878, `FFFF` | `506edf234f78729295ce75c22e619211f3c7c745a9f1786d5f056700d9ccc6c7` | Same defect | Same deterministic refusal |

Q2 reads the authenticated exact 2,048-byte original images, asserts literal hashes from Q1, and independently requires refusal with a null result. The unchanged original Q1 probe also independently re-emits/mutates those inputs and now fails at capture. New tests do not merely rely on the producer's 40 positive assertions.

Additional Q2 challenges and outcomes:

| Challenge | Observation/classification |
| --- | --- |
| Ten new heap/relationship mutations | Empty required TypeDef/TypeRef/method names; out-of-range TypeDef/TypeRef namespaces, TypeRef/method names and method blob; GenericParam index gap; out-of-signature Param sequence: deterministic refusal, no returned evidence. |
| Fifteen compound signature adversaries | Reserved/truncated compressed integers; incomplete/reserved custom modifiers; local generic-instantiation arity mismatch; malformed array bounds; void/byref generic arguments; reserved/truncated function-pointer signature and invalid/repeated sentinels; nested unbound type variable: refusal twice per case. |
| Four non-language identifier controls | Unicode including supplementary characters, spaces/punctuation/separators, generated-style names, composed and decomposed accents: capture succeeds without language-level filtering or normalization. |
| Minimal/empty structures where legal | Module-only minimal assembly, omitted Param rows, return Param sequence zero: capture succeeds. A zero-byte PE and raw metadata without a PE refuse; those are not legal empty assembly images. |
| Complex positive signatures | Generic return/array parameter, chained custom modifiers/void pointer, nested vararg function pointer, emitted constrained nested type/method generics, multidimensional/jagged arrays, byrefs and unmanaged function pointers: capture succeeds. Synthetic structural controls are not asserted to bind/execute. |
| Actual production reference builder in this test host | Invoked `BuildPlatformReferenceEvidence`; **193** TPA entries all capture and each actual normalized path/content digest matches its file. This is the test host's inventory, not a universal production inventory size. |
| Truncation/non-assembly | Retained lengths 0, 2, 128, metadata start and metadata-end-minus-one refuse. Retention through the complete metadata extent captures despite missing later PE bytes. Emitted netmodule and raw metadata-only bytes refuse. Confirms bounded metadata extent, not universal truncation detection. |
| Documented excluded surfaces | Malformed Field signature `FF`, invalid TypeRef ResolutionScope, malformed modifier TypeSpec syntax `FF` **still capture**. Their exact bytes are preserved. These are observed false acceptance under a hypothetical complete-metadata-validity claim, but outside R1's expressly documented surface; no such broad capability is qualified. |
| Semantic compilation negative | A captured structurally supported synthetic assembly and verified compilation provenance can coexist with compiler errors from missing core semantic references. Capture/provenance success does not certify semantic compilation usability. |

No false rejection was found in the tested supported positives; no new false acceptance was established within the bounded name/signature/relationship surface. This is not exhaustive metadata validity, binding or PE/IL correctness. Malformed UTF-8/name canonicalization, field/property/MemberRef/MethodSpec syntax, resolution scopes, inheritance/constraints, full TypeSpec resolution, IL/resources, multi-module relationships, arbitrary platforms and resource-exhaustion bounds remain unqualified.

The optional `ANDIRIT_Q2_INPUTS` recorder writes only reviewer outputs. The final recording run retains **50** exact content-addressed binary inputs under [inputs](CI1A-S2.2-Q2-evidence/inputs); each `captured`/`refused` observation maps to its byte hash in [results.json](CI1A-S2.2-Q2-evidence/results.json). Runtime DLLs are recorded by exact path/hash inventory rather than copied into reviewer evidence. The three authenticated historical inputs are also included among these binaries.

## 4. Reference profiles, mapping and frozen identities

Authority is the accepted Option A decision §5, later entry decision §§3.2/3.5/3.6, accepted identity contract, actual frozen R2 `Describe`/`ReferenceIdentity`, and corpus construction—not a green production helper or a producer summary. Mapping/G-CONFIRM/G-MINT corrections were checked against those sources.

The production profile is the immutable captured `file|<actual normalized path>|<lowercase byte hash>` inventory. In this host the production builder captures 193 distinct/case-insensitively sorted TPA paths, within existing bounds. The frozen corpus constructs precisely three framework entries labelled `runtime/System.Runtime.dll`, `runtime/System.Private.CoreLib.dll` and `runtime/Microsoft.CSharp.dll`, over accepted runtime 10.0.12 bytes. Its three-reference comparison is **not** evidence that the live production inventory has three references.

Q2 independently builds corpus Leaf/Core from authoritative source/parse/options/project inputs, captures those three real DLLs, constructs genuine verified dependencies and compares a fresh length-prefixed encoder with frozen `Describe` over those **same** actual inputs. Both algorithms agree. Actual-path outputs differ from fixture outputs as they must. Aligning **only comparison preimage labels**, with all bytes/other inputs fixed, exactly recovers frozen anchors:

| Unit | Actual-path `compctx1:sha256:` suffix | Fixture/aligned suffix |
| --- | --- | --- |
| Leaf | `b816dcb5512588bc3b5ad01692c20241b31442ab420c8a4b5a6d06d032b9c59b` | `404c5e48c3106b47786f14cad594139477c651fd78d0e050f8fd5bcab18fcb5f` |
| Core | `df8d2932670fc6a15cec10930b0bf573cfce841080e55f9544433983cbbe8a77` | `942a8e588a8db15afafb30d0292e0b867c767c5f72392b6f458783d961a71db3` |

The exact three identities/byte hashes and all 193 actual host identities are in the raw observations. No fixture label was substituted into a production capture or frozen expectation. The old frozen overview's illustrative hash-label shorthand is not treated as byte-level authority over actual `Corpus.cs`/R2 serialization. No frozen document was rewritten.

Legacy project-reference reconstruction is `compilation|assembly|metadata|trees`, trees sorted by **raw** ordinal FilePath before separator normalization and hashed as UTF-8 decoded text. Own source hashes use exact captured raw bytes, including BOM, and differ from dependency tree-text hashes. A separate raw R2 fixture proves `d/B.cs` precedes raw `d\A.cs`; normalizing before sort changes the item. Two identical projected reference entries are preserved, not deduplicated. Current `CodeGraphSource` normalizes paths earlier, so the raw-path adversary is a frozen-rule test rather than a claim of backslashes surviving current verified source construction.

Independent reconstruction/reversal/equivalence probes preserve canonical contexts and target values for equal complete inputs across separate objects. Same bytes at the **same path** recaptured into distinct reference instances remain equal. Same bytes at **different paths** remain distinct accepted reference preimages; byte equality alone is not identity equality under this path-bound contract. Changing defining source parse material changes its mapped context. Changing only child provenance changes the parent's `compctx2` while leaving mapped parent `compctx1`/target stable. Restoration/every tuple discriminator is additionally exercised by frozen oracle and unchanged Q1 probes.

One Q2 synthetic vector produces parent `compctx1:sha256:74b294d95290ac2e3f7af4a3e4ee05449e4b36d35058f3a69f1f708a24c07f2d` and `asmctx3:sha256:55e4be2e9c21876d22facc4c3c9f0613ecab4b9c5cc709dd7b20410a1fe63a79` for original, equivalent reconstruction and child-only provenance variation. Strong context changes from `edc4641f…` to `a84c97cd…`. These are reviewer synthetic components, not new production canonicalization or frozen target expectations.

The two frozen `usage4` vectors `bd0487b7…` and `b874bdc5…`, all tuple positions, domain/version/category, big-endian byte lengths, invariant coordinates and semantic-source-unit/storage-contributor distinction remain verified by unchanged Q1 and nine existing identity tests. The full frozen oracle reproduces all **185** accepted checks and exact output bytes. `compctx2` never enters `asmctx3`/`usage4`; passing it directly as a `compctx1` to the helper refuses. No accepted identity is redefined.

## 5. Shape, authority and association boundaries

The fabricated canonical `compctx1` of 64 zeroes still yields helper target `asmctx3:sha256:9558232431295cf558cec711030a577467166c493ac747eecfa71103d0ed1020`. Canonical target shape also accepts fabricated values. This is expected shape validation, not authority. The corrected contract assigns authority rejection to the future live resolver and per-fact association gate.

Q2 independently observes current foundation refusals for uncaptured equivalent-byte references (`UnverifiedMetadataReference`), wrong-repository dependency and substituted equivalent live instance (`MissingDefiningCompilation`), mismatched compiled/source text (`RoslynTextMismatch`), missing declared units or explicit unsupported-profile refusal (`IncompleteCompilationUniverse`). A complete universe admits the original exact instance and refuses a different equivalent instance. Existing unchanged Q1 additionally covers source membership and scope/inventory distinction. Unsupported provenance is not silently upgraded by a shape helper.

Those observations qualify only the existing foundation. No current production mapped-target resolver or per-occurrence carrier is present, so unsupported/missing authority cannot be claimed to have passed a production minting rejection test. A caller-provided `UnsupportedProfile` refusal tests universe behavior, not a newly implemented profile classifier. Frozen model authority and live production authority remain separate.

The stage boundaries are now consistent:

| Boundary | Established status |
| --- | --- |
| S2.2 pre-mint invariants | Specified: actual defining instance, verified complete universe, current defining `compctx2`, no absent/stale/substituted/fabricated authority. Must be implemented and independently falsified before WP5. |
| Extraction-time association | Required separately from target/occurrence identity; no fact if association invalid. Not implemented by R1/Q2. |
| Proposed durable carrier | Requirement/design direction only. Concrete keys, serialization, aggregation and model enforcement unresolved. A scope/universe digest alone is insufficient. |
| S2.3 native persistence/reopen | Separate authorization; first gate must refuse prior facts under changed/stale defining `compctx2` after reopen. No present durability/reopen guarantee. Option B escalation remains if enforceability fails. |

Defining `compctx1`, semantic `asmctx3`, provenance `compctx2` and frozen `usage4` stay distinct. The consuming context is not substituted for the defining context. Equal semantic identity does not prove current association or freshness.

## 6. Historical Q1 accounting and independent executions

All **48 original Q1 cases ran unchanged**. Result: **45 passed, 3 failed, 0 skipped**. [Full per-case matrix](CI1A-S2.2-Q2-evidence/q1-probe-matrix.md) compares the exact same 48 names with authenticated historical results; [raw Q1 log/TRX](CI1A-S2.2-Q2-evidence/q1.log) preserves every trace. Passing observation tests are not automatically successful remediation tests.

| Original failure | Read/reproduction/result | Classification |
| --- | --- | --- |
| `ObserveMalformedMetadataStillExposesIdentity(first-type-name)` | Historical test/Q1-F01 require successful capture to demonstrate deferred bad-name acceptance. Q2 execution now throws `UnsupportedMetadataReference` at validator `Name`/TypeDef traversal, before its identity assertion. Exact-hash Q2 positive-remediation test passes. | Historical observation invalidated by remediation; **failed historical test**, not a pass. |
| `…(later-type-name)` | Same original defect, later row; now fails at string dereference inside capture. Exact-hash independent refusal passes. | Same classification. |
| `…(method-signature)` | Original bad blob handle; now fails at `GetBlobReader` inside capture before identity exposure. Exact-hash independent refusal passes. | Same classification. |

No failure had an unrelated production cause. Original assembly-name refusal and the mapping/profile/shape/equal-instance observations still pass. None was rewritten, skipped, removed or silently omitted.

Environment: Windows x64 10.0.26200, .NET SDK **10.0.401**, runtime **10.0.12**, MSBuild **18.9.11**, existing Roslyn **5.9.0**. Reviewer tests use the existing friend-assembly name in a separate project/output directory; frozen sources link read-only. No new package/version, solution membership, production/test configuration or restore-policy change was introduced.

| Q2-executed run | Actual result | Meaning |
| --- | --- | --- |
| Full unfiltered solution | **692 passed / 0 failed / 0 skipped** | Core 325; native 43; TreeSitter 157; C# 167. Reproduces producer baseline. |
| Producer metadata remediation subset | **40/0/0** | Included in the 692, not additive. |
| Producer WP1/WP2/A-01 foundation subset | **7/0/0** | Included in solution; unchanged WP1/A-01 contracts. |
| Existing frozen identity-remediation subset | **9/0/0** | Included in solution. |
| Original Q1 reviewer probes | **45/3/0** | Exactly the three invalidated historical observations above. |
| First Q2 reviewer version | **37/1/0** | Reviewer path-fixture assumption error; transparently retained. |
| Intermediate Q2 expanded suite | **39/0/0** | Corrected normalization assertion plus separate frozen raw-order test. |
| Completed Q2 suite, separate processes | **40/0/0 twice** | Independent adversarial metadata, profile/equivalence/authority checks. |
| Exact-input recording of completed Q2 suite | **40/0/0** | Added only reviewer output recording; 50 exact input binaries retained. |
| Frozen R2 manifest/oracle | **100 files; 185/185 checks** | Fixed trust anchor and accepted output hash match. |

Counts are deliberately not summed into a single green number. The 692 includes targeted producer subsets; repeated Q2 executions are the same 40 cases; historical failures remain failures.

The initial Q2 fixture incorrectly expected raw backslashes to survive current verified-source construction; the failure occurred in the reviewer assertion, not production. It was corrected after inspecting `CodeGraphSource`. First raw log/TRX remains. Five stable observations (mapping, two frozen profiles, raw ordering and complete inventory) match across the two identical completed builds. Rebuilding the reviewer DLL solely to add optional input recording changes that DLL's own entry in the test host's TPA inventory, exactly as a content-bound identity should. The collector initially compared that rebuilt inventory indiscriminately; its assertion failure is preserved in `collector-inputs-first.log`. The final collector explicitly verifies that the only two-sided inventory delta is the rebuilt Q2 reviewer DLL; the other four observations and runtime/framework bytes remain identical. No production failure was hidden or reclassified.

Exact test/audit commands, working directory, output paths and exit classifications are in [COMMANDS.md](CI1A-S2.2-Q2-evidence/COMMANDS.md). Raw logs/TRX, oracle output, source/manifest receipts, every test result, observations and input hashes are in [Q2 evidence](CI1A-S2.2-Q2-evidence/results.json). The reviewer evidence manifest binds exact bytes; accepted manifests are only verified, never regenerated.

## 7. Gate readiness only

Reviewed current 14 nine-part rows at candidate `b4eff01`, corrected mapping/G-CONFIRM/G-MINT, execution handoff §D, NEW-02 matrix `docs/planning/adr-ci1-01-ci1a-impl-01/stage2-acceptance-matrix.md` §2, accepted decisions and frozen authority. All nine planning fields are present in each row. The previous row-5 comparison ambiguity is resolved; future executions are not credited as passing.

| Row | Q2 readiness and confirmation evidence still required |
| --- | --- |
| 1 — Q-F2 | Accepted bounded encoding authority remains consistent; current foundation and unchanged Q1 encoding results support it. Formal owner closure separate. |
| 2 — Q-F3 | Replace prior overbroad reliance with exact bounded surface and Q2 exclusions/inputs/refusal evidence. Owner must resolve broad usable-metadata wording before semantic acceptance. |
| 3 — A-01 | Separator refusal/order/Unicode authority and evidence remain consistent. |
| 4 — A-02 | Scope identifies declared unit/path boundary; verified universe/source membership supplies inventory. Foundation evidence exists; extraction consumption pending. |
| 5 — mapped compctx1 | Corrected declared profiles and both expected-value comparisons are independently supported. Future confirmation must name exact inventory/bytes/labels and disclose comparison-only alignment. Production mapper absent. |
| 6 — compctx2 isolation | Frozen tuple authority and algebraic negatives supported; future live mapper/association negative still required. |
| 7 — live target minting | Authority resolves; real unique/missing/ambiguous/external binding and per-declaration correspondence not implemented/qualified here. |
| 8 — extraction fidelity | Accepted spans/source bytes/category authority unchanged; production invocation evidence pending. |
| 9 — coverage/refusal | Accepted states remain reference expectations; production completeness/complete-empty matrix pending. |
| 10 — inactive scanner | F09 authority remains; real disabled-text scanner evidence pending. |
| 11 — NEW-04 merge | Accepted correction and falsification remain pending; R2 model does not certify current production merge. |
| 12 — S-01 chain | Foundations supported; exact live byte→binding→fact correspondence and integrated acceptance still pending. |
| 13 — Option A association | Pre-mint current defining association must precede WP5, even if later integration is WP8. Concrete carrier/enforcement pending; native reopen is first S2.3 gate. |
| 14 — independent confirmation | Q2 provides updated evidence/readiness. Separate independent G-CONFIRM verdict and owner disposition still required before dependent authoring. |

**G-CONFIRM readiness:** corrected row 5 is technically reviewable; row 2 must carry the exact metadata qualification and rows 6/7/12/13 must retain shape/authority/stage distinctions. Rows 8–13 legitimately retain future implementation evidence. The original handoff remains historical authority; confirmation must explicitly reference the current corrected package and this Q2 record rather than claiming its older terse rows establish broader capabilities. NEW-02 tuples remain independently reproduced. **No G-CONFIRM verdict is issued.**

**G-MINT readiness:** mapping formula, exact legacy representation, comparison policy, identity equivalence and pre-mint/reopen partition are sufficiently precise as review inputs. The package is not ready to pass a production minting qualification: implemented mapping/resolver, complete verified universe/current defining association, concrete per-fact model, refusal classifications and independent pre-WP5 falsification are still required. Repeated reconstruction here is not production re-ingestion; frozen modeled gates do not prove production refusal. Independent mapping qualification remains mandatory before WP5. **No G-MINT verdict is issued.**

## 8. Remaining qualifications and next owner decision

| Qualification | Reproducible evidence / supported contract | Required disposition / owner |
| --- | --- | --- |
| Bounded Q-F3 rather than universal usable metadata | Exact original defects refuse; excluded field/scope/TypeSpec malformed inputs still capture. Capture is immutable byte/path identity plus described materialization/checks. | Owner explicitly accepts this bounded achieved guarantee and exclusions, or commissions further bounded validation before semantic acceptance. Q2 does not silently amend entry §3.2 or certify binding/full PE/IL validity. |
| Future semantic authority | Shape-only helpers accept canonical fabrications; current foundations refuse tested mismatches; production mapper/resolver/per-fact association absent. | Separately commissioned implementation and independent pre-mint review. No downstream permission follows from Q2. |
| Durability | Carrier undefined; no defining-context persistence/reopen implementation. | Retain S2.2 extraction-time requirements; S2.3 separately authorized first stale-reopen gate; owner Option B decision if unenforceable. |
| Bounded F12/toolchain reference | Accepted Windows/runtime bytes, purpose-built corpus, fixture-declared target components and modeled lifecycle/coverage/provenance. | Preserve accepted qualifications; do not generalize oracle green to production correctness, full canonicalization, other runtimes/platforms or integrated S2.5. |
| Carried accepted limitations | S2.1 packaging limitation, Q-F4/Q-F5, A-02, S-01, F09, F10/IF-04, F11, F16/NEW-02, NEW-01, NEW-04, SQ-01…SQ-07, D3/D4/D5, I-05/MD-03. | Remain at their existing owner/gate assignments. This review closes none through implication. |
| Untested boundaries | Exhaustive names/UTF-8/table validity, external generic/constraint resolution, multi-module/native images, IL/resources, resource exhaustion, arbitrary platforms; per-fact production stale association, persistence/reopen/recovery. | No capability claimed. Further work requires its proper commission; no hypothetical defect is invented. |

**Exact next owner decision:** accept or reject this **SUPPORTED WITH QUALIFICATIONS** Q2 foundation result and explicitly settle the achieved Q-F3 boundary. If accepted, record bounded closure of Q1-F01–F05 while retaining all qualifications and both open gates. A separate independent G-CONFIRM review must confirm the updated 14 rows/NEW-02/profile plan before dependent test authoring; implemented mapping/live-association qualification must precede WP5. This is the next decision, not permission for WP4–WP9 or S2.3.

Only this report, separate Q2 reviewer project/tests, and additive Q2 evidence are committed. The commit's own SHA and final postcommit Git status are supplied in the final handoff; candidate/precommit receipts are not misrepresented as final status. No production, producer-test, historical-probe, frozen-evidence or accepted-decision modification is included. **Stop after Q2. Owner acceptance remains separate.**
