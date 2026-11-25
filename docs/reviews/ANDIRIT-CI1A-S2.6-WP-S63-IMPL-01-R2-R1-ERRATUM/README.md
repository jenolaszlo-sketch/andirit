# S6.3 R1 producer evidence extension erratum

**ANDIRIT-CI1A-S2.6-WP-S63-IMPL-01-R2 · F-S63Q-02 · 25 November 2026**

This erratum supplies three missing or mismatched archival byte sequences from the historical R1 extension at `dd484af2d32ff1f5b7951a85f4f3104c4b4b5402`, extension tree `b61cf46a9d28c626faa5b5c904b5ea5f8b9c4565`. The historical manifest is Git blob `4151ae58f839e5276c60d0936f3c7e646dfba9e0`, SHA-256 `c52701c2a01c2531905138bfc05b52165c2d6835769b538dc0f1a2e60987ddb0`, with declared package identity `b17a726de4812f37128e3d9610130543dc7919ea4060977152fc5c26cf14d056`. The historical manifest and incorrect blobs remain available at that commit and at the same paths on main. The original 17-entry producer package is a separate preserved tree.

| Historical R1 path | Expected SHA-256 | Actual committed SHA-256 | Result |
| --- | --- | --- | --- |
| `attribution-verification.txt` | `f1e6bb0e384dcf2d393b4b4c2b253fa012be7039fcd239f14e2e70aaa47a94f9` | `29f9e1bea7e7a915fcc54c2a0fd6f94a4ccee4762f3101fa786de2d179aefced` | CRLF reconstruction matches expected size and SHA-256 |
| `probes/probe-receipt.txt` | `71476201f453967a34025cac4d5c1a1bebf03ec85eaca3f88e01a4ed93257e6d` | `44512dc8f84363ad6add27de7da589374a7a47e8d1b19a50f31aeb84c4a9128e` | CRLF reconstruction matches expected size and SHA-256 |
| `hosted-ci/coverage-latticedb-ubuntu.cobertura.xml` | `d71c16dca4783e9de9f7ebc95131a5350cdfac8ae725ea324ed395542b7bacdd` | Missing | Original hosted artifact member matches expected 219,244 bytes and SHA-256 |

The text checks were performed on Git blobs. Converting only LF to CRLF reproduced the expected hashes and sizes exactly. The committed LF hashes, CRLF working-tree hashes observed during qualification, and lack of path attributes disabling text normalization explain the discrepancy. The current Git configuration reports `core.autocrlf=true`. The byte comparison gives no evidence of an encoding or content edit. The historical manifest was evaluated against committed LF blobs. These bytes establish a sealing input/staging discrepancy even though the original manifest command transcript is unavailable.

The missing path matches the committed `.gitignore` rule `coverage*.xml`. A normal unforced add would omit that new file. The historical index itself is unavailable. R2 recovered the exact unmodified XML from the sealed independent archive at Q1 commit `a69c13ce447839330793be2e4cbc7a1dd732d1dd` after the independent manifest verification. Its recorded source is artifact 15034992001 on run 45639342986, whose head is `d3c9359d1abd8c89a7214e37f074802e5d2fa238`. [Historical hosted run](https://github.com/jenolaszlo-sketch/andirit/actions/runs/45639342986)

The three corrected payloads are under `recovered/`. [Reconciliation receipt](reconciliation.json) records original Git blob identities, exact sizes, expected and actual hashes, and the coverage artifact provenance. The correction restores access to the bytes named by R1 and preserves the qualification finding that R1's original committed seal failed. The XML retains its R1 run attribution and supports aggregate coverage only. R2 named-case attribution is sealed separately.

The erratum manifest was generated against the committed erratum payload. Verify it by running the R2 evidence package's `seal-r2.py verify <repo> <sealing-commit> <repo-relative-erratum-directory>`.
