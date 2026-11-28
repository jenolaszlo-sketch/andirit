# ANDIRIT-CI1A-S2.6-WP-S64-OCC-ID-QUAL-01

**Independent qualification of the versioned occurrence identity transition**

## C — NOT SUPPORTED

The authorized versioned identity transition is substantially implemented and
much of the evidence reconciles. However, the framework reference-resolution
requirement is **not satisfied**: the implementation selects the host process's
`Microsoft.NETCore.App` implementation assemblies and ignores the project's
declared target framework. `net10.0`, `net8.0` and `netstandard2.0` projects with
identical source produce the same `compctx3` descriptor, and a `netstandard2.0`
project using `System.Half` and `System.Range` is accepted as complete because it
is compiled against .NET 10 implementation assemblies. A successful host-runtime
compilation is not target-framework proof.

Two further qualifications are material:

- The hosted tests executed GitHub synthetic PR merge commits whose trees equal
  the candidate and seal trees, not the candidate/seal commit objects. The prior
  synthetic-merge exception is not extended here; owner acknowledgment is
  required.
- The frozen C4 successor ledger is bound to a 172-reference runtime
  implementation closure, not the S2.0/F12 three-reference oracle. It is
  independently reproducible under the expanded closure but produces a different
  target and context than the three-reference contract.

No owner acceptance, DF-1 closure, S6.4 closure, independent qualification grant
or D3 continuation is made.

## 1. Reviewed identities

| Subject | Identity |
| --- | --- |
| Investigation baseline | `81102d21b4d6f6ed510b0555bb0d034aa1705c44` |
| Candidate implementation | `77ec887368c62b404e0d9a8034d34b7f8cac8007` |
| Candidate tree | `40a2c42123e71bf65746b10198ca2af29c17d928` |
| Evidence payload | `17fe571c5e23c6335ca8b847f2d5ee86da670f93` |
| Payload tree | `5da73b3dfa2c44a24853d4520d49ecd38b394c87` |
| Evidence seal | `880a888da2256608a91010026b1c3f39d0de76b6` |
| Seal tree | `cf55faa07a76a07b76594881d25994a796875491` |
| Seal SHA-256 | `240c5db417b81d932af4782ed5178910b72650a29d94125368a4d276a8a77b1c` |
| Candidate hosted run | `46132247172` |
| Seal hosted run | `46134305730` |
| Dependency | LatticeDbSharp `0.2.0` |
| Release boundary | `v0.2.0-preview.7` |

## 2. Independent seal verification

Verified directly from Git objects, not from the producer `seal.py`:

- seal parent is the payload;
- the seal delta is exactly `A evidence/manifest.json`;
- all 22 manifest entries match payload and seal blobs, byte counts and SHA-256;
- entries are sorted and unique;
- no evidence file is missing or extra;
- the manifest payload commit/tree recompute;
- the investigation evidence tree is unchanged from `81102d2`;
- the two historical R1-06 seals still modify their manifests, while the
  successor seal **adds** its manifest.

Independent result: `evidence/seal-verification.json`.

## 3. Identity contract adjudication — supported

| Surface | Predecessor | Successor | Result |
| --- | --- | --- | --- |
| Reference identity | path-bound `file|path|sha256` | `refctx1` content-bound | PASS |
| Production descriptor | `compctx2` | `compctx3` | PASS |
| Defining context | `compctx1` | `compctx4` | PASS |
| Target | `asmctx3` | `asmctx4` | PASS |
| Occurrence | `usage4` | `usage5` | PASS |
| Persisted generation | 1 | 2 | PASS |

- Successor descriptors reject relabeled predecessor path-bound items.
- `compctx4` is the only accepted target-minting context; `compctx1` and
  `compctx3` are rejected.
- `asmctx4` is the only canonical target domain; `asmctx3` is recognized only
  for safe refusal.
- `usage5` includes the `asmctx4` target and the successor semantics string.
- Provenance, defining context, target, usage and contributor identities remain
  separately constructed.
- Graph node/edge identities, repository/plugin identities and source-unit
  identities are unchanged.
- No cross-generation mixing path was found in extraction, publication,
  persistence, query or recovery.

## 4. Framework reference resolution — FAIL (blocking)

The producer's closure is:

```text
RuntimeEnvironment.GetRuntimeDirectory() -> managed *.dll -> refctx1 identities
```

It is independent of the project's `TargetFramework`. The independent probe
(`tools/framework-probe`) produced:

| Declared target | Complete | Descriptor | Runtime refs |
| --- | --- | --- | ---: |
| `net10.0` | true | `compctx3:sha256:1c1a6f…335c` | 172 |
| `net8.0` | true | `compctx3:sha256:1c1a6f…335c` | 172 |
| `netstandard2.0` | true | `compctx3:sha256:1c1a6f…335c` | 172 |

The probe source uses `System.Half` and `System.Range`, which are not in the
`netstandard2.0` reference surface, yet the `netstandard2.0` case is accepted as
complete. All three cases compile against the same .NET 10.0.12 implementation
assemblies and produce the same compilation descriptor.

Findings:

- The declared target framework is not an identity input.
- The closure is the host runtime's implementation assemblies, not the target
  framework's reference assemblies.
- Ambient runtime-directory discovery remains.
- A consumer runtime different from the declared target changes the closure.
- Multiple installed runtime versions are not resolved from the target.
- Unsupported target frameworks are accepted rather than refused.

The actual Roslyn references do correspond to the descriptor, and the descriptor
is internally self-consistent. That does **not** make the closure
target-framework correct. The framework-reference gate is withheld.

Evidence: `evidence/framework-adjudication.json` and
`evidence/framework-probe.json`.

## 5. Determinism campaign — supported within scope

| Control | Result |
| --- | --- |
| Windows x64 console host | target `8258e1…555f`, 51 facts |
| Windows x64 xUnit host | same target and 51-ID ledger |
| Two physical corpus roots | identical target, context and 51 IDs |
| Two reference paths with identical bytes | equal `refctx1` identity and target |
| Additional unrelated host assemblies | excluded from the closure |
| Different reference bytes | different identity and target |
| Different options / source | different identity |
| Linux x64 hosted regression | 1044 passed |
| macOS ARM64 hosted regression | 1044 passed |

Exact identity equality is claimed only where the **reference closure content** is
the same. Cross-OS runtime images are not identical; the Linux/macOS runs prove
regression and semantic relations, not cross-platform digest equality.

## 6. C4 occurrence oracle — exact on the producing closure; scope qualified

Independent derivation (Roslyn + `System.Reflection.Metadata`, no production
identity helpers, no producer oracle) reproduced exactly:

| Field | Value |
| --- | --- |
| Target | `asmctx4:sha256:8258e165d53336f87abf3b1d1ae3f220190dfaf848eaae2906222bf49940555f` |
| Defining context | `compctx4:sha256:476de9e8c407f13756a931f8ca285ce7d254c19309be433cdf84d022165e31be` |
| Production descriptor | `compctx3:sha256:f85c46768aa2a3c1c8dbda35a3e2e8f699ceea04ddf7a17d7ec590865ce313ee` |
| Reference set | 172, SHA-256 `d582ffc4…1bcf` |
| Facts | 51 (50 Caller, 1 Caller2) |
| Contributors | 2 |
| Variants | 1 / 50 / 51, targets stable |

All 51 `usage5` IDs match the frozen ledger, and all four variant ledgers match.

**Reference-closure divergence:** the S2.0/F12 contract uses three references
(`System.Runtime`, `System.Private.CoreLib`, `Microsoft.CSharp`). Under that
closure the independent target is
`asmctx4:sha256:1d2ece…f28e` with context `compctx4:sha256:318c55…c6a9`, not the
frozen `8258e…`. The successor ledger is a new oracle under the expanded closure,
not the S2.0 three-reference oracle. Whether the expanded closure is the
authorized reference manifest is an owner decision.

Freeze chronology: the ledger and oracle were added in the implementation commit;
there is no separate pre-comparison freeze commit. Independent reproduction shows
the values are not candidate-derived, but a timestamped freeze is not
demonstrated.

C1–C4 graph oracles, the frozen corpus and the S2.0 expected artifacts are
byte-unchanged.

## 7. Persisted compatibility and refusal — supported with physical qualification

A genuine predecessor store was generated from `81102d2` code:

- schema 1, namespace `/1`, authority version 1, no generation marker,
  `asmctx3` facts, semantics 4.

The successor refuses it with `CodeOccurrenceSchemaException` and re-ingestion
guidance. The logical occurrence rows are unchanged before and after the refused
open. The main database file hash changed and a 4 KB WAL file appeared; this is a
native engine open/checkpoint effect, not an Andirit migration. Detection occurs
after graph schema initialization and command-log replay; for an existing graph
schema those steps are read-only with respect to occurrence rows, but detection is
not literally the first operation.

Adversarial controls all failed closed:

- authority version 1 or missing generation → `UnsupportedSchema`, zero facts;
- meta generation 1, schema 1 or namespace `/1` → open refusal;
- successor fact relabeled `asmctx3` → open refusal;
- predecessor facts relabeled schema 2 / namespace 2 / generation 2 → open refusal.

The successor store carries schema 2, namespace `/2`, generation 2, authority
version/generation 2, 51 `asmctx4` facts and semantics 5.

## 8. Reconstruction — supported with loss limits

Successor ingestion into a new store produced the target, 51 facts, 50/1
distribution and two contributors, and queried `Available`/complete after durable
reopen. A second independent store had an identical logical snapshot. Cancellation
before session creation produced no database and no partial publication.

The original predecessor store is logically preserved. Reconstruction is not
universally lossless: missing historical source bytes, missing reference images
or absent historical run metadata cannot be reconstructed. No in-place migration
or destructive rebuild exists.

## 9. Regression and accepted contracts — supported

| Campaign | Result |
| --- | --- |
| Independent full Release run | 1044 passed, 0 failed |
| Hosted Windows x64 | 1044 passed |
| Hosted Linux x64 | 1044 passed |
| Hosted macOS ARM64 | 1044 passed |
| 29 mirror tests | all present and passed |
| 19 inherited S6.3 named categories | all present and passed |
| Coverage floors | all exceeded (core 82.29/70.6, CSharp 94.36/87.5, TreeSitter 89.85/75.85, LatticeDb 93.51/80.85) |
| Package pack/consume/native/Testing smoke | passed |

Inventory reconciles 1,034 → 1,044: 10 new transition/C4 tests, 10 renamed domain
tests, no test removed without replacement. Thirty-two test files changed; the
changes are identity-domain/version expectations, path-bound → content-bound
expectations, predecessor refusal expectations and independent-oracle helper
replacement. No underlying semantic guarantee was removed.

## 10. Hosted provenance and architecture — qualified

Both hosted runs used GitHub's synthetic PR merge commit:

| Run | API head | Test-subject head | Merge tree | Equals |
| --- | --- | --- | --- | --- |
| `46132247172` | `77ec887` | `1ec6b9d` | `40a2c42` | candidate tree |
| `46134305730` | `880a888` | `bb2ac74` | `cf55faa` | seal tree |

The merge tree is byte-identical to the candidate/seal tree and the merge includes
the candidate as a parent. The test content is therefore the candidate tree, but
the receipts attribute a merge commit, not the candidate commit object. The prior
synthetic-merge exception was commission-specific; this evidence requires an
explicit owner acknowledgment or another hosted run checked out at the exact
commit/tree.

Runner architecture:

| Runner | Actual architecture |
| --- | --- |
| `windows-latest` | X64 |
| `ubuntu-latest` | X64 |
| `macos-latest` | **ARM64** (`osx-arm64`) |

The producer report described macOS as x64. That is inaccurate. The actual ARM64
run satisfies the inherited bounded macOS arm64 regression gate, but no S6.5
platform certification and no macOS x64 claim is made.

TRX hashes match `subject.json` on all three runners, and the test counts per
runner are 345/383/157/159 = 1044.

## 11. Findings

| ID | Finding | Severity | Disposition |
| --- | --- | --- | --- |
| Q-01 | Target framework ignored; host runtime implementation closure used | **Blocking** | Framework gate withheld |
| Q-02 | `netstandard2.0` source using .NET 10 APIs accepted as complete | **Blocking** | Same gate |
| Q-03 | Successor C4 ledger uses 172-ref runtime closure, not S2.0 3-ref oracle | Material | Owner scope decision |
| Q-04 | Hosted receipts attribute synthetic merge commits, not candidate/seal commits | Material | Owner acknowledgment required |
| Q-05 | Producer reported macOS x64; actual runner is ARM64 | Reporting | Correct report; ARM64 gate satisfied |
| Q-06 | Refused predecessor open changes physical DB file/WAL, though logical rows preserved | Qualification | Engine-level; record and explain |
| Q-07 | Incompatible-generation detection occurs after schema init/replay | Qualification | No occurrence mutation before refusal |
| Q-08 | C4 ledger freeze lacks a separate pre-comparison commit | Qualification | Independently reproducible, not candidate-derived |
| Q-09 | Reconstruction is not universally lossless | Limitation | Retain; do not overclaim |
| OCC-ID-01…05 | Transitional findings | Resolved/qualified as above | Do not close owner acceptance |
| R1-01 | C4 ledger | Exact on producing closure; scope qualified | Not a full S6.4 corpus closure |
| R1-04/R1-05 | Broader historical-store campaign / D3 | Not executed here | Remain open |
| R1-06 | Historical exception | Narrowly preserved | Successor seal adds only manifest |
| F-S64E-03/DF-1, F-S64E-04, F-S64E-05 | Broader defects | Open | Not closed |

## 12. Gate matrix

| Gate | Result |
| --- | --- |
| Q-G1 Seal verification | **Pass** |
| Q-G2 Exact identity reconciliation | **Pass** |
| Q-G3 Version/domain isolation | **Pass** |
| Q-G4 Framework reference resolution | **FAIL** |
| Q-G5 Cross-host/path determinism | **Pass within equal-reference scope; synthetic-merge attribution qualified** |
| Q-G6 C4 oracle | **Exact on producing closure; reference-scope and freeze qualifications** |
| Q-G7 Persisted compatibility | **Pass logically; physical/detection-order qualifications** |
| Q-G8 Reconstruction | **Pass with loss limits** |
| Q-G9 Regression/contracts/package | **Pass** |
| Q-G10 Hosted provenance/architecture | **Qualified; owner decision required** |
| Q-G11 Disposition | **C — NOT SUPPORTED** |

## 13. Required owner decision

1. Decide whether the authorized reference-resolution contract permits the
   host-runtime implementation closure instead of a target-framework reference
   closure. If not, require a bounded correction that resolves references from the
   declared target framework or explicitly refuses unsupported target frameworks
   before minting.
2. Decide whether the 172-reference runtime closure is the authorized C4 oracle
   reference manifest, or require the C4 ledger to be derived under the S2.0
   three-reference contract.
3. Acknowledge or reject the synthetic-merge hosted attribution. If exact-commit
   receipts are required, rerun the hosted campaign with a checkout at the
   candidate/seal commit or an explicit owner exception.
4. Correct the macOS architecture record to ARM64; preserve the inherited
   bounded macOS arm64 limitation and make no S6.5 claim.

No production code, producer test, producer evidence or historical seal was
modified. DF-1 and S6.4 remain open. D3 continuation is not authorized by this
qualification.
