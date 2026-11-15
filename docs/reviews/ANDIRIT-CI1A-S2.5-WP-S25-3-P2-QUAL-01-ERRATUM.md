# Erratum — P3-F-01 (P2 host-scoped context, target, and reference-closure values)

**Project** Andirit

**Recorded** 2026-11-15 under `ANDIRIT-CI1A-S2.5-WP-S25-3-CLOSURE-OWNER-01`

**Finding** [P3 owner decision §4.3](ANDIRIT-CI1A-S2.5-WP-S25-3-P3-OWNER-01.md#43-p3-f-01--p2-report-narrative-values-are-not-traceable-to-the-sealed-receipts)

**Disposition** **P3-F-01 — CLOSED BY VERIFIED ADDITIVE ERRATUM**

## 1. Affected record

This erratum corrects the absolute values in the [P2 qualification report](ANDIRIT-CI1A-S2.5-WP-S25-3-P2-QUAL-01.md) §§7–8 and §§10–12. The report, harness, manifests and evidence remain preserved at their original bytes. Use the sealed receipt values below when citing the qualified run. The report supplies abbreviations for most unsupported values. The table reproduces those strings as printed, and their omitted digits and provenance are unknown.

## 2. Authoritative sealed values

| P2 section and observation | Unsupported narrative value as printed | Complete sealed receipt value |
| --- | --- | --- |
| §§7, 10, fresh reviewer reference-closure digest | `4b9637df…` | `a5bc0419e461dc434278a6618a1359a9962122cd6152e51173ead670d68a5941` |
| §8, explicit-interface implicit and explicit dispatch target | `asmctx3:sha256:0ab72b5c834227d3658d8ef6ba29f771a08040d7b7bc257e7bb54f434dcadf34` | `asmctx3:sha256:664e7ffe6a2d3b9b4dd5772a327375324f8d7fd2756cbedef60279a49974f125` |
| §10, value vs ref, side A context | `6fefb305…` | `compctx1:sha256:74137c5c19e6b829064c90f98aeefe84dedbc24b4d55d51a98ab2b9d228d4788` |
| §10, value vs ref, side B context | `e42ff5f4…` | `compctx1:sha256:35cb902358f9c8a00434825b3a41c44e4fda99dfbe70d69f27fdb2d4b5a9de92` |
| §10, value vs ref, side A target | `2e4cef53…` | `asmctx3:sha256:1f0b883a938976b83b873a5231fcb223b5790f0b5e6aec2af4e12cae18848bd0` |
| §10, value vs ref, side B target | `d09d744c…` | `asmctx3:sha256:97612ad461c3c62dce745d52a2d09fc49ef64f5114859c1a5bf475aff43f4e80` |
| §10, ref vs out, side A context | `a21530e4…` | `compctx1:sha256:3fa86dc31811ef68373700838f755be92f7272c0ae0aa1b509028161689026fd` |
| §10, ref vs out, side B context | `83614969…` | `compctx1:sha256:b120cd0e8853b0cc1b6515cb6b3226509cff1e41189bda57337e367d837b80a1` |
| §10, ref vs out, side A target | `f56aa218…` | `asmctx3:sha256:238bb137f9e3d79ebdad72da4bcc381b6ccdeaa7327728364deb311c436b22a7` |
| §10, ref vs out, side B target | `0ca5278f…` | `asmctx3:sha256:360475434f4cdc103e3bbca152eff5d4669fd446ea10a30ad05fba49910c13d1` |
| §10, ref vs in, side A context | `4c4a97c5…` | `compctx1:sha256:f1a4f104dc06666c9bcf7b1ab6fb153af3cb9439da533bab573b956697e4a7f8` |
| §10, ref vs in, side B context | `edf4a61a…` | `compctx1:sha256:0639de895c52f2a4e344db12355d2c0ab2114e6f62b9df255233a9e22a78ed83` |
| §10, ref vs in, side A target | `0d0ae0a5…` | `asmctx3:sha256:2cc6c3b73786a539cd4daf7c24d905adc7ec11afa1bd2c6c4aa948ba20d85dfe` |
| §10, ref vs in, side B target | `8159ff46…` | `asmctx3:sha256:3ad651e487103b84df839a2809e37083528ef83cd517e7b31e6c13b8d6ba2539` |
| §10, out vs in, side A context | `9be41029…` | `compctx1:sha256:c0f0df2a18578977778afdad8999b8312c361c0c95f17215596600b4d9e8753f` |
| §10, out vs in, side B context | `a51f12e3…` | `compctx1:sha256:a3e517bbbda590bb0facce93395c98e68b6d94fae280d3244550eea85576848d` |
| §10, out vs in, side A target | `442d749d…` | `asmctx3:sha256:b1a3375940be3ef2f23d9b145659f7c31444e705853d9674baad602e99fcac3e` |
| §10, out vs in, side B target | `a25fdad8…` | `asmctx3:sha256:523d1f4dc88ccdfbd7bb923582b2388094ddba1760d87fcc25dc1663d05d15a5` |
| §11, coexistence context | `compctx1:sha256:25901398…` | `compctx1:sha256:99f2372a3ce727dab303593d7063f21a9db3aaad8790fe07d7ed90135bc905f9` |
| §11, coexistence plain target | `f8998daf…` | `asmctx3:sha256:dcb0cbfcd4b32c0371199ed147e0c546a4ff021bde7fce9f58a9a704b3774f2e` |
| §11, coexistence ref target | `bb6c05ed…` | `asmctx3:sha256:ddce4e094374c5686d6de48266d68dd99b1bf3bb4258580c8863b01708d61c82` |
| §12, integrated `Add` target (production and independent) | `asmctx3:sha256:f51f0fbd…` | `asmctx3:sha256:f6bd5d282c050f709a3ebe75ebcf7a0663d46a647ae37025d658b5c5404a3cc5` |
| §12, integrated `Greet` target (production and independent) | `asmctx3:sha256:0d92fc2d…` | `asmctx3:sha256:1577c900e2ef79bf90f0c74342c6be01a251c106a0f250886ff5c0d17b57d902` |

| Receipt | Field locator | SHA-256 over exact bytes |
| --- | --- | --- |
| [P2 `Q1-P2A.json`](CI1A-S2.5-WP-S25-3-P2-QUAL-evidence/Q1-P2A.json) | `r13[].sides[].descriptor`, `reviewerReconstruction`, `composed`; `forms[id=explicit-interface].facts[].targetId`; `coexistence.context`, `targetPlain`, `targetRef` | `27e77a9777c9f33aba574938b22b7994a53277cf1824c5d8ad2f29b313bb69db` |
| [P2 `Q2-INT.json`](CI1A-S2.5-WP-S25-3-P2-QUAL-evidence/Q2-INT.json) | `expectations.add` and `expectations.greet`: `productionTarget`, `independentTarget`, `agree` | `fceda415c21390b21f8845752e011f2ef662dc98ffddb0a3c454a2cbd11b540e` |
| [P2 `Q5-ARTIFACT-AUDIT.json`](CI1A-S2.5-WP-S25-3-P2-QUAL-evidence/Q5-ARTIFACT-AUDIT.json) | `delta` and `referenceIdentityLimitation` | `b7ecaa8d3803a85a0399c00fdb8c9e022970145bde4a34e13b17af6e64ac19db` |
| [Delta `Q1-DELTA.json`](CI1A-S2.5-WP-S25-3-P1-DELTA-evidence/Q1-DELTA.json) | Side descriptors' `referencesCount` and `referencesDigest` | `200b89de03bd24ca129275338663b4eef85b93a931408ac618493b686dee3298` |

Checksum authorities are the [P2 manifest](CI1A-S2.5-WP-S25-3-P2-QUAL-evidence/manifest.json) (26 entries) and the [delta manifest](CI1A-S2.5-WP-S25-3-P1-DELTA-evidence/manifest.json) (17 entries). All entries in both manifests were verified against the preserved files.

## 3. Interpretation and limits

- **Historical delta (179 reference identities).** Its side descriptors record the count and digest `b8cdb1ce80017278f839f9ba1778fbcb1251697cde1ae3de342c1bac29e29980`. The 179 identity strings are absent. Artifact-only reconstruction of the historical `compctx1` remains unavailable and Q-P2-02 remains accepted with its limitation.
- **Fresh P2 reviewer run (181 ordered reference identities).** All eight R13 sides report `referencesCount=181`, the `a5bc0419…` digest above, `referencesOrdered=true`, `matchesProduction=true`, `matchesFrozen=true`, and composed-target `agree=true`. The receipt records the count, digest and checks of the ordered list used at runtime. Its descriptor omits the 181 identity strings. Read the Q5 audit's full-list wording and the P2 §10 reference to recorded descriptor fields as descriptions of the live derivation only. The sealed artifact supports citation of the run's values and comparisons. Artifact-only replay of the fresh reference list would require a separately retained input. The [reviewer source](CI1A-S2.5-WP-S25-3-P2-QUAL-tests/Reviewer.cs) shows the live list passed to the encoder and the count and digest written to the receipt.
- **O-1.** Absolute `compctx1` and `asmctx3` values apply to the demonstrated host and build closure. Compare stable component digests and the demonstrated within-run relations downstream. The mismatched report literals are consistent with the host-scoped boundary. Their actual source remains unverified.

The receipt checks, component-digest results, clarified R13 relations, and integration, availability and managed-recovery outcomes still support **S-01 — SATISFIED WITH QUALIFICATIONS**. The correction is confined to narrative values and the precision of their provenance. The P3 acceptance and Q-P2-01 through Q-P2-06 continue to govern downstream use. [Formal closure record](ANDIRIT-CI1A-S2.5-WP-S25-3-CLOSURE-OWNER-01.md)
