# Defining provenance review (Q2-F02)

## Defect

R1's E02 positive control (`optionA.E02-positive`) established eligibility from
dictionary key presence plus universe list membership; it never compared the
supplied defining `compctx2` to the current verified context, and `variants`
was not among the compared frozen sections. The reviewer's one-line zero-digest
patch therefore left R1 at 130/130 with byte-identical output — reproduced here
before any correction (`q2-f02-{clean,mutated}-r1.json`, both 130/130, both
as-produced SHA-256 `0A7BB361509AEBEE70A1305679EE22C6C35ECC92E3B973C2A5D08E6854049CCC`).

## Correction

`oracle/ProvenanceGate.cs` expresses the accepted Option A validity gate once:
candidate evidence is untrusted, and a recorded defining `compctx2` is eligible
only when it equals the independently derived current context of the defining
compilation **and** the compilation-universe membership carries that same
verified context for the defining unit. Universe membership is bound to
per-unit verified contexts, not a bare count or unit list. `RunE02` derives the
current authority from the verified fixture compilation and validates every
candidate against it; the derived Core context is itself anchored against the
accepted contract value (`e02.authority.frozen-compctx2`). The `e02` and
`variants` sections are compared frozen sections of `oracle-expected-r2.json`.

## Independently derived authority (qualified run)

- Resolved symbol: `Corpus.Core!M:Corpus.Core.Methods.Direct(System.Int32)`
  (Roslyn resolution intact — provenance gates never alter semantic truth).
- Current defining `compctx2`: `compctx2:sha256:7bf4ac5b916ab5dcd2fadaa45c22cc7765ab83839eb7de8ef4d442865d07424d`
  (= accepted frozen contract value).
- Defining `compctx1`: `compctx1:sha256:942a8e588a8db15afafb30d0292e0b867c767c5f72392b6f458783d961a71db3`.
- Target `asmctx3`: `asmctx3:sha256:42a30aaf20d663a3115234de4d7adc42cc3b6a18144d0c7e44746f1d1670513a` (= `T.Direct`).

## Case matrix (all pass)

| Case | Supplied context | Universe | Eligible (expected) |
|---|---|---|---|
| `E02-R2-01-valid` | current verified Core context | full, matching | true |
| `E02-R2-02-missing` | none | full | false |
| `E02-R2-03-stale` | changed-Core context | full | false |
| `E02-R2-04-all-zero` | `compctx2:sha256:` + 64 zeroes | full | false |
| `E02-R2-05-wrong-compilation` | App's context (different compilation) | full | false |
| `E02-R2-06-missing-universe` | current verified Core context | Core absent | false |
| `E02-R2-07-incompatible-universe` | current verified Core context | membership carries changed-Core context | false |
| `E02-R2-08-restored` | current verified Core context | full, matching | true |

## PROV-1..PROV-8 invariants (all pass)

1. A supplied digest cannot establish its own authority.
2. Changing the supplied candidate does not redefine current authority.
3. A changed verified defining compilation invalidates incompatible prior evidence.
4. A correct `compctx2` without universe membership is insufficient.
5. Universe membership must carry the same verified defining context.
6. A resolved Roslyn symbol can remain nonpublishable without provenance.
7. `asmctx3` equality does not prove current `compctx2` validity (transitive-only
   Leaf change keeps `T.Direct` stable while Core's `compctx2` changes).
8. Expected authority and eligibility are independently anchored and frozen.

The Option A gate checks (`A01-gate`, `A06`, `A07`, `A08`) route through the
same `ProvenanceGate`, so missing, out-of-universe, and stale recorded contexts
are refused by one predicate.

## External mutation controls

See `PROVENANCE-MUTATION-RESULTS.md`: `q2-f02-presence-only-gate`,
`e02-zero-supplied-context`, `frozen-expected-e02-authority-zeroed`,
`universe-membership-removed`, `universe-context-equality-removed` — all exit 1
with the expected failing check IDs, including `expected.e02` whenever the
frozen provenance record or its comparison is touched.
