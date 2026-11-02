# R1 changed-case ledger

The original `oracle-expected.json` remains byte-identical to commit `82f101c24e21828a41a30d4342ba73db9376e24e`. R1 uses a new expected identity at `oracle-expected-r1.json`.

| R1 item | Change | Relation to Q1-verified results |
|---|---|---|
| DEL01 | Adds complete authoritative source deletion Snapshot A/B, source ownership, `usage4` projections, and DEL-N1..N3 | New lifecycle expectation; no native store claim |
| OPT01 | Adds actual `CheckOverflow` Roslyn option variant and restoration | New `optionsDigest` dimension; source/parse inputs held fixed |
| E02 | Adds resolved Core symbol with absent defining provenance and positive authority control | Distinct from E01 metadata-only target and A07 model gate |
| CLS01 | Names the existing real CS0121 site as explicit ambiguity classification | Same Roslyn syntax as historical N01; N01 remains preserved |
| CLS02 | Adds static-through-instance CS0176 fixture | New `InvalidInvocation`, not ambiguity |
| S15 | Corrects target from handler method to event member `E:` | Supersedes only the mislabeled S15 expected semantic record in R1 |
| S15-handler | Adds separate handler method observation | New distinction record; method remains `M:` |
| Coverage | Adds explicit `assertionLevel` labels | Clarification only; NEW-04 remains open |
| R1 metadata | Adds `CI1A-S2.0-CORPUS-R1` identifier | New version; historical Q1 oracle not overwritten |

All unchanged semantic expectations and frozen R2 vectors remain represented. The R1 report is compared against the separately frozen expected document.
