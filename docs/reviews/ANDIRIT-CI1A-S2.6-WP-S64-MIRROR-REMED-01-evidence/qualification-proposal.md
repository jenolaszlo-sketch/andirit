# Proposed independent qualification commission

**ANDIRIT-CI1A-S2.6-WP-S64-MIRROR-REMED-QUAL-01**

## Purpose

Independently verify that F-S64E-01 and F-S64E-02 are remediated in producer
scope, that previously affected stores recover without loss of authoritative or
unrelated data, and that public behavior and inherited S6.1–S6.3 qualifications
are preserved. This commission must not grant owner acceptance.

## Reviewer independence

The reviewer must:

1. derive the expected logical node/edge ledgers and physical cardinalities from
   the fixtures and governing graph semantics **before** inspecting candidate
   results;
2. verify the seal from committed Git objects, including the binary affected
   stores and `reviewed-inputs.json`;
3. run its own adversarial controls, not only replay the producer's;
4. record reviewer identity and methodological limits.

## Required replay

- Exact candidate commit/R2 checkout on native Windows x64.
- Genuine GitHub-hosted Linux x64 exact-SHA job.
- Existing macOS CI job as a regression gate, not platform certification.
- Full solution regression, repository formatting and all four coverage jobs.
- The 21 new tests plus existing `NativeGraphMirrorTests`, `NativePublicationCorrectnessTests`,
  `NativeDurabilityTests`, `NativeCancellationPathTests` and occurrence suites.
- Direct raw census of `HetuGraphNode`, mirror edges and `HetuMirror`; aggregate
  counts alone are insufficient.
- Recovery of disposable copies of both committed affected stores, including a
  second recovery for idempotence and an unrelated-native-sentinel control.
- Disabled-to-enabled and enabled-to-disabled-to-enabled transitions with
  repeated reopen.
- Multi-repository isolation with a shared logical identity.

## Adversarial controls

- Inject one extra mirror node and one extra mirror edge outside the manifest;
  the oracle must fail before recovery and pass after.
- Delete only the manifest; the correspondence oracle must fail before recovery.
- Corrupt the manifest with invalid base64, invalid JSON, a missing field and
  duplicate native ids; the disabled open must remain authoritative and the
  enabled open must rebuild.
- Force a `before-commit` failure during enabled and disabled completion; the
  previous manifest and rows must survive.
- Attempt to make the sweep cross repositories; another repository's manifest
  and rows must survive.
- Substituted physical identity or a same-count logical substitution must be
  detected by the manifest correspondence check.

## Closure evidence

- Exact candidate commit, tree, evidence seal and payload/tree.
- Hosted CI run/attempt/job identities and named-case TRX receipts.
- Reviewer-derived oracle and observed deltas.
- Recovery receipts with before/after census and preservation checks.
- Statement of residual risks and retained qualifications.

## Explicit out of scope

S6.4 full D3 corpus qualification, S6.5 platform certification, LatticeDbSharp
upgrade, public API changes, release tag changes, NuGet publication, and
promotion to `main`.
