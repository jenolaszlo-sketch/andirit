# EC6C-A2 — A29 owner decision closure

**Project:** Penghou.Hetu (future Andirit)
**Milestone:** EC6C-A2
**Disposition:** EC6C-A2 ACCEPTED — TRACK A IMPLEMENTATION READY
**Production implementation:** NOT AUTHORIZED

## 1. Git binding

Repository: jenolaszlo-sketch/penghou-hetu
Canonical main baseline: 558f8ac82ac1b5e07da68f4a71a70240eaf38409 (tree 08a01a1b7d5d3109b991bcd05d57272fd3f5f018; unchanged).
D0 design: d12fe1882c814a0863f872e3913ddff921d31bee, tree fed4ff117b28899ccbb9032d0fded2084dc59523. Independent review: 49a01393375a345cefe0ca30b1f58fee1fe24e04, tree 827f83f38221548c168e272700410a9da6cb525f. A1 source: 760918174b8228dc0ebe2a16fbcd164316c7a116, tree b2cb9a75685d0998851422b6e68acf3062965a4d, parent 706a720c736297b59972db89b185d6e528be1b5e.
A2 branch: decisions/ec6c-a29-closure, created directly from the A1 source commit in the isolated worktree C:/Users/Laszlos/source/repos/Penghou.Hetu.ec6c-a29-closure.
The final A2 commit and tree are reported in the completion record; canonical main, D0, review, and A1 branches are preserved.

## 2. Previously open decision and final choice

A1 left one Track A question open: how to measure D0’s maximum exported-name length of 256. The owner selected **256 Unicode scalar values**. **256 UTF-16 code units is rejected as the exported-name counting unit.**

The maximum applies to the canonical exported name alone. It does not impose a limit on the complete qualified name, module path, graph ID, occurrence identity, or serialized payload.

## 3. Unicode scalar definition and counting

A Unicode scalar value is a Unicode code point excluding U+D800–U+DFFF. Iterate the UTF-16 sequence strictly, accept a high surrogate only when followed by a low surrogate, count a valid pair once, and reject unpaired high or low surrogates. Count combining marks separately from their base characters. Do not use .NET string.Length as the scalar-counting operation. Use a decoder/API that reports malformed UTF-16, such as checking for OperationStatus.Done from Rune.DecodeFromUtf16, or equivalent logic; do not accept replacement-character fallback.

A valid name contains 1–256 scalar values. Empty, all-whitespace, control-containing, malformed, and over-bound names are unsupported and produce no export entry or occurrence. Never truncate or replace a name.

## 4. Graph-contract compatibility assessment

Source inspection found:

- CodeGraphNode.Name has no length cap. QualifiedName has no independent length cap and preserves non-whitespace input. Its whitespace-only-to-null behavior does not conflict with the existing allowlist, which excludes all-whitespace names.
- CodeNodeId and other graph identifier wrappers have no length cap; entry IDs are fixed-size ASCII hashes, while the exact exported name remains in Name and QualifiedName.
- Batch property text limits apply only to CodeTextProperty and text-list values, not node names. The export name stays in the graph’s name fields.
- InMemory exact-name lookup uses ordinal string equality and no maximum query length.
- LatticeDb uses JSON UTF-8 command-log serialization, Base64 text persistence, and an optional serialized-JSON string mirror. The pinned LatticeDbSharp 0.2.0 API exposes variable-length string values and documents no smaller string cap. No smaller repository-level name-field byte limit was found.

A 256-scalar name can contain at most 512 UTF-16 code units and 1,024 UTF-8 bytes in its raw Unicode sequence. The source/API compatibility disposition is **COMPATIBLE WITH LOCAL VALIDATION**. The future extractor must strictly validate before hashing and serialization. This conclusion is not a durable runtime qualification: exact persistence and reopen behavior remain EC-6C.3 gates for both stores.

## 5. Normative boundary-fixture inventory

s’s acceptance-matrix section in docs/design/ec6c-export-adversarial-cases.md#current-ec6c-a2-structural-acceptance-matrix. It covers zero and one scalar; 256/257 ASCII; 256/257 supplementary scalars; mixed ASCII/supplementary boundaries; combining marks; precomposed/decomposed forms; unpaired high/low surrogates; ordinal case differences; escaped syntax that remains unsupported; and exported-name checks alongside UTF-16 source-column assertions. It specifies per-fixture construction, counts, expected support/facts/identities, validation stage, future test path, and independent qualification.

## 6. Identity, comparison, and normalization

Preserve the existing identity formula:

{language}:export:{lowercase-hex(SHA-256(UTF-8(modulePath + LF + exportedName)))}

Validate malformed UTF-16 before invoking the existing UTF-8/hash helpers, because replacement fallback could otherwise map malformed input to U+FFFD. For valid scalar sequences, deterministic UTF-8 preserves the exact name. Comparison remains ordinal and case-sensitive. Apply no Unicode normalization: precomposed U+00E9 and decomposed U+0065 U+0301 remain distinct names, queries, and entry IDs. Aliases remain separate names, and entry identity remains independent of source occurrence.

The A29 choice does not broaden escaped-name support. Syntax eligibility is checked under the existing allowlist before canonical-name validation. The name-slot formula is unchanged.

## 7. UTF-16 source-coordinate contract

Source spans remain one-based, end-exclusive UTF-16 code-unit columns. A supplementary scalar before an occurrence occupies two source columns even though it counts as one scalar in the exported name. The A29 coordinate fixture asserts this distinction. No source-location or global graph-coordinate contract changes.

## 8. Risks and remaining qualification

No source-level name-field conflict was found. The external native engine does not publish a smaller per-string limit in the inspected pinned API, but actual LatticeDb round-trip has not been run here. EC-6C.3 must test both JS and TS plugins, accepted and rejected boundaries, exact-name queries, both stores, and durable close/reopen. It must also verify authentic p8-to-p9 migration, existing identity preservation, replacement/deletion, failure/cancellation, and independent review. No tests or runtime probes were executed in EC6C-A2.

## 9. Track A readiness

All required Track A contracts are specified: name validation and identity; occurrence identity and explicit collision rejection; per-occurrence type/value projection; exact source spans and UTF-16 coordinates; syntax allowlist and re-export exclusions; positive-only semantics; exact-name query compatibility; per-file publication, deletion/replacement, failure/cancellation; p8-to-p9 invalidation; both-store qualification; and independent review requirements.

Disposition: **EC6C-A2 ACCEPTED — TRACK A IMPLEMENTATION READY.** Readiness means the specification is complete and source-compatible. It does not assert implemented, tested, migrated, or durable-qualified behavior.

## 10. Reconciled decisions and Track B boundary

D-EC6C-01 through D-EC6C-14 keep their A1 dispositions, except that D-02’s bound is now final. D-05, D-06, and D-11 from EC-6A remain historically not accepted. D-10 CommonJS deferral remains accepted. The independent review’s historical wording is preserved unchanged; the correction remains in the owner record.

Track B remains **DESIGN REMEDIATION REQUIRED**. No exported-as relationship, declaration attribution, or Resolved/Derived export-binding claim is authorized. EC-6D remains separate design work.

## 11. Rejected alternative

The maximum of 256 UTF-16 code units is rejected for exported-name counting. It would reject valid names of 256 scalar values when supplementary characters use surrogate pairs. UTF-16 remains authoritative only for source coordinates and existing UTF-16 string representation behavior.

## 12. Implementation authorization boundary

EC6C-A2 is documentation-only. It changes no production extraction, graph validation, storage, or publication code and changes no tests. It does not change profile p9, package version, provider version, or resolver2.relative-v1. It creates no package, tag, or release, and does not start EC-6C.1.

A separate implementation authorization is required before EC-6C.1 starts. No merge or push is authorized by this milestone.