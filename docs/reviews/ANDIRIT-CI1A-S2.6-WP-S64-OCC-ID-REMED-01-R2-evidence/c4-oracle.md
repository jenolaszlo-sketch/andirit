# C4 target-framework oracle and historical delta

## Production target-framework ledger

The corrected production C4 campaign uses the declared `net10.0` reference pack:

| Field | Value |
| --- | --- |
| Pack | `Microsoft.NETCore.App.Ref` `10.0.12` |
| Reference directory | `ref/net10.0` |
| References | 167 |
| Reference-set SHA-256 | `9973286b42c11decd9ef2bd4111e72067cb67d9f227160bd6ed4fd3969a63c82` |
| Target | `asmctx4:sha256:23ffa6444fb108132fb54e760cac76f5ffd8a3580b0aed1b9aa6709f78eeab68` |
| Defining context | `compctx4:sha256:e7c0a28d959f075a1f5d491e3e642c44d781bb4750de2f107ad78e57a408d6b1` |
| Production descriptor | `compctx3:sha256:4b88c629c25ed6c9b916d5a279304fd07cb06fa64545d3c607ce1326a232989e` |
| Cardinality | 51 (50 Caller, 1 Caller2) |
| Contributors | 2 |

The independent oracle in `tools/independent-c4-oracle` reproduces the target,
descriptor, context, reference hash, all 51 IDs and all variants exactly. It does
not call production identity helpers or the producer oracle.

## Freeze chronology

| Commit | Purpose |
| --- | --- |
| `b8a748e` | bounded implementation and tests |
| `814fe5b` | independent oracle tool + target-framework ledger + variants |
| `82ba813` | final production comparison against the frozen ledger |

The ledger was frozen before the C4 production comparison.

## Variants

| Variant | Cardinality | Contributors | Target |
| --- | ---: | ---: | --- |
| baseline | 51 | 2 | `23ffa6…ab68` |
| without-caller | 1 | 1 | stable |
| without-caller2 | 50 | 1 | stable |
| caller2-changed | 51 | 2 | stable |

## Historical preservation

- The R1 expanded-runtime C4 ledger (`8258e1…555f`) is retained unchanged as
  historical evidence.
- The S2.0/F12 three-reference controlled fixture remains independent. Its
  historical graph/corpus artifacts are unchanged. The production pack closure is
  not substituted for that fixture.
- C1–C4 graph artifacts and the frozen corpus are byte-identical.
- No historical expected result was silently updated.
