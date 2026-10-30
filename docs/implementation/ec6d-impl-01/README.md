# EC6D-IMPL-01 producer implementation

> **Status: ACCEPTED and CLOSED by [EC6D-OWNER-02](../../decisions/ec6d-owner-02-acceptance.md)**
> after EC6D-REM-01 and the independent
> [EC6D-QUAL-02 re-qualification](../../reviews/ec6d-qual-02/README.md)
> (production source `701257f4e6f0dbf71ab57072f8204cfa4cd6cad2` / tree
> `80d284029f5a82184bb04fd3d0d11fef7b223dc0`). This packet remains the
> producer record; the remediation packet is at
> [docs/remediation/ec6d-rem-01](../../remediation/ec6d-rem-01/README.md).
> Package publication, production release, representative performance and
> deployment qualification are not authorized or established.

Recorded 2026-10-30 (Asia/Manila). Production source binding: **093004e02a3438d3c1eb6a668602da4aef563d6a**,
tree **89767a4d050aa73248c169a0454ec638c4d7d678**. Baseline: `fe1a9e37da49064f8cf6ec7fe9bc5ade7cfd7781`.

The additive p10 extraction and `GetInterfaceImpactV1Async` are implemented.
The controlled corpus returns exactly four confirmed implementations, five
possible consumer modules, and both required occurrence unknowns. Producer
validation uses Windows, both stores, authentic p9 durable seeds, and the
pinned website. This packet does not issue independent acceptance or authorize
release. Hosted Windows, Ubuntu, macOS and package/native-consumer CI passed on evidence commit `97c81b784a8b068904c8bfa78df3ab937d5d1a29`; main was synchronized by the user-approved normal push. See [CI qualification](ci.md).

## Evidence index

- [Baseline and immutable authority](baseline.md)
- [Owner-approved implementation clarifications](owner-clarifications.md)
- [Stored facts, identities and ownership](fact-inventory.md)
- [Lexical binding and acceptance matrix](binding-verification.md)
- [Consumer API, work bounds and qualification](consumer-query.md)
- [Compatibility](compatibility.md)
- [Authentic durable rehearsal](durable-rehearsal.md)
- [Pinned website regression](website-regression.md)
- [Regression commands and results](regression.md)
- [Independent reproduction instructions](qualification-handoff.md)
- [Producer handoff](final-handoff.md)
- [Hosted CI](ci.md)
- [Git binding](git-binding.md)
- [Machine-readable producer observations](observations.json)

The accepted owner disposition and five historical contracts remain unchanged.
Their proposal banners are superseded by the owner disposition. The narrowly
approved clarifications in this implementation chat are recorded separately;
none was silently substituted for an accepted expectation.

Raw databases, snapshots, package feeds, preserved p9 binaries, oracle output,
TRX files and execution logs are outside the checkout at
`C:/Users/Laszlos/source/repos/Solo/ec6d-impl-01-evidence/`.
