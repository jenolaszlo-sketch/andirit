# C4 occurrence oracle status

## Derived

Using the frozen C4 source, the accepted R2 authority, the documented component
digests and the S2.0 runtime reference set:

- one target for `QualifierDependency.Api.Mark`;
- **51** occurrences;
- **50** in `Caller`, **1** in `Caller2`;
- two contributor units;
- removal variants: 50 (without Caller2), 1 (without Caller);
- caller-change variant: same target, changed usage identity.

Artifacts:

- `evidence/c4-oracle-unvalidated.json`
- `evidence/c4-oracle-variants/` (removal and caller-change variants)

## Not frozen as authoritative

The ledger is **not validated** against a host-independent production contract.
The production target differs because its reference set includes process TPA and
absolute paths. The ledger is preserved as evidence for the proposed identity
transition, not as an accepted target/usage authority.

The C4 graph oracle from the re-entry seal remains frozen and valid.

## Completion condition

After the owner authorizes an identity transition, regenerate the ledger under
the corrected contract, verify all 51 facts and the 50/1 distribution, then seal
it before any candidate comparison. Do not derive expected values from observed
candidate output.
