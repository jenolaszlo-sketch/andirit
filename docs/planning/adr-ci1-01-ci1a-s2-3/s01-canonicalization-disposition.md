# S-01 outstanding digest-canonicalization disposition

**Commission:** `ANDIRIT-CI1A-S2.3-ENTRY-OWNER-01`. Documentation/assessment only.
**Authority:** S2.0 owner §7 `Q-A`; S2.2 entry scope §7; identity-and-provenance
mapping §5; obligation register §7; S2.2 `G-ACCEPT` §8; WP3/WP4/WP5/WP7/WP8 owner
decisions.

The S2.2 `G-ACCEPT` decision passed with a **retained qualification**: general
canonical derivation of the caller-supplied `identifierDigest`, `declarationDigest`
and `shapeDigest` is **not implemented or independently qualified**. This document
records that boundary precisely and does not rewrite or reopen the S2.2 decision.

---

## 1. Exact earlier mandatory language

- **S2.0 owner §7 (`Q-A`):** *"Identifier/declaration/shape digest forms are
  fixture-declared; general S2.2 source canonicalization remains to implement and
  qualify."*
- **S2.2 entry scope §7:** *"S2.2 must still implement and qualify general source
  canonicalization (the fixture-declared digest forms do not satisfy it)."*
- **identity-and-provenance-mapping §5:** the corpus *"does not claim to freeze
  S2.2's source canonicalization of those forms — that is an S2.2/S-01
  obligation."*
- **risk register `Q-A`:** general S2.2 source canonicalization still to implement
  and qualify.

## 2. Final S2.2 owner's accepted interpretation

- **Register §7:** S-01 allocation is *"S2.1 byte/tree/context/options/reference/
  scope proof, **S2.2 semantic correspondence**, S2.5 integrated independent
  proof."*
- **Stage-2 boundary §8:** S2.2 extracts/binds/mints; **S2.5** verifies the S-01
  proof chain.
- **S2.2 `G-ACCEPT` §8:** the mandatory S2.2 gate item — the live
  defining/source/target correspondence chain — is satisfied and independently
  qualified; the **general source canonicalization of the component digests is not
  implemented/independently qualified** and is a **preserved qualification**
  allocated to the S2.5 integrated proof. The WP8 G-ACCEPT entry contract GA-5
  records *"S2.2 correspondence accepted; S2.5 obligation preserved"* and forbids
  *"erasing the qualification."*

## 3. Production capability actually established

- `asmctx3` is minted with the frozen R2 algorithm over the R2-faithful `compctx1`
  plus `metadataIdentity`, `moduleName`, and the three component digests.
- `VerifiedCSharpOccurrenceExtractor` receives the three digests as
  `Func<IMethodSymbol, string>` **inputs**; the WP8 harness and all WP5/WP7 tests
  supply declared/fixture forms. There is **no** production canonical derivation
  of the component digests from the verified live compilation, and no production
  wiring of the extractor to a plugin/publication flow.
- Consequently, the three component digests are **caller-supplied** identity
  inputs, not independently source-canonicalized values.

## 4. Does native publication rely on them as trusted, independently derived identity inputs?

**Yes — as identity inputs; but their derivation is caller-supplied.** `asmctx3`
(and therefore `usage4`) depends on the three component digests. S2.3 persisting
and reopening occurrence facts will persist whatever digests the producer supplied.
Native publication does **not** independently verify that the digests were derived
canonically from source, because no such canonical derivation exists.

This is **not** made worse by S2.3, and it does **not** block S2.3 publication
authorization, because:

- the accepted S2.2 contract already treats the digests as caller-supplied under
  the retained S-01 qualification;
- `asmctx3` remains deterministic for equal supplied inputs, so persistence/reopen
  reproducibility holds for the supplied inputs;
- the native store validates the canonical **shape/encoding** of the identity, not
  its source provenance (REM-01 §4.2).

**It does mean** S2.3 must not describe a persisted `asmctx3`/`usage4` as a
"source-canonicalized" identity; it is a deterministic identity over the supplied
component digests.

## 5. Required disposition

The unresolved obligation requires an **explicit S2.5 completion gate**, not a scope
amendment to S2.2, and not a silent resolution:

- **Do not** declare the S2.0/entry-scope mandatory language satisfied.
- **Do not** reopen or rewrite the S2.2 `G-ACCEPT` decision.
- **Record** the obligation as an S2.5 integrated-proof deliverable: implement and
  independently qualify the canonical source derivation of
  `identifierDigest`/`declarationDigest`/`shapeDigest` from the verified live
  compilation, and prove the full S-01 chain end to end.
- **Earliest stage at which stronger digest provenance must be established:**
  **S2.5** (integrated independent S-01 proof), before Stage-2 owner acceptance.

## 6. Is a stronger guarantee necessary for safe S2.3 publication?

**No**, under the accepted authority. S2.3 publication is safe with respect to its
own contract because:

- the persisted identity is deterministic and canonically shaped;
- the store refuses non-canonical identities (D5);
- the receipt of the digests is already an accepted S2.2 qualification;
- the S2.5 gate remains the closure condition for the stronger guarantee.

If a future S2.3 consumer required source-canonical digests **as trusted
independently derived inputs**, that would be a **stronger S-01 requirement** and
would require a **separate owner decision** (bounded canonicalization package)
before that reliance — it is not authorized here and is not required by the
accepted S2.3 contract.

## 7. Preserved limitation and closure condition

- **Limitation preserved:** component digests remain caller-supplied.
- **Downstream closure condition:** S2.5 integrated independent S-01 proof
  (including canonical component-digest derivation), before Stage-2 owner
  acceptance.
- **No stronger provenance authority is asserted than has been established.**
