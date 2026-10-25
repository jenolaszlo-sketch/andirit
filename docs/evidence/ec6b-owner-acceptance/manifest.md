# EC-6B closure evidence manifest

**Purpose:** bind the owner-acceptance decision to the exact preserved review
reports, review-only probes, remediation evidence, handoff, and tested source.
**Date:** 25 October 2026.
**Repository:** `jenolaszlo-sketch/penghou-hetu`.

## Method

Each row records:

- the immutable source commit (full in the binding table of the
  [decision](../../decisions/ec6b-owner-acceptance.md));
- the Git blob identity of the path at that commit
  (`git rev-parse <commit>:<path>`);
- the SHA-256 digest of the **committed blob content** (not of checkout bytes,
  which may differ by line-ending conversion);
- the repository-relative path.

Digests are computed from the raw blob stream, equivalently:

```text
git -C <repo> cat-file blob <commit>:<path> | sha256sum
```

Windows verification used the same raw blob stream and
`System.Security.Cryptography.SHA256`. A commit cannot contain its own hash and
a closure document cannot contain its own digest; therefore the documents
created by this closure (decision, this manifest, the README, the recorded
instruction, and the validation report) are intentionally **not** listed below.
Their commit and tree identities are recorded in the completion report.

## Q1 independent review (`66690587f4507d47e78fa4e1996e050b830c1642`)

| Path | Git blob | SHA-256 |
| --- | --- | --- |
| `docs/reviews/ec6b-glm-q1-relative-module-resolution-verification.md` | `fd183a9c98861835d92ea2653d55decd9b6921f1` | `f1deb46e5af33dfc2319629b22bf769f60126d494d02a1b8e7496a02788018a3` |
| `tests/Penghou.Hetu.TreeSitter.Tests/ReviewEc6bGlmQ1Probes.cs` | `329a7f5cab3e6ca31e557dfa651027c92bcc0312` | `f247d21de283a5bd0fb95dd52c1276ef9c260b6d505a4b57f967f485d3b81c71` |
| `tests/Penghou.Hetu.TreeSitter.Tests/ReviewEc6bGlmQ1CaptureProbe.cs` | `6da0450913c4106307d5110d0d29091d08c80f0c` | `c34522124208594a873a1e0a96d3c5a4097651c74646813f791b63bc0777af79` |
| `tests/Penghou.Hetu.TreeSitter.Tests/ReviewEc6bGlmQ1ImportFormProbe.cs` | `d150c3c483e24fc3d83f855208363db992702fca` | `f52c32a48966bcc87e764acc49a838f667b23a10c007a351e400c2cfbaf08b00` |
| `tests/Penghou.Hetu.TreeSitter.Tests/ReviewEc6bGlmQ1MigrationProbe.cs` | `75f5008b4af5155b65ce7be8b561743eb2649c66` | `13ad3d7b0bd25caec66b0a2b6f1929e51435a48d570d83ad3b7c3a06ae560598` |
| `tests/Penghou.Hetu.TreeSitter.Tests/ReviewEc6bGlmQ1WebsiteProbe.cs` | `7e72a78505b7fdb88689eb3c4b255e6438880010` | `94f7b832c3bb2f1537c55160c6d8153fc32bc49856f5454db15deb605dbdd164` |

## Q2 independent review (`b79bfce9820f08348335db7ca0b19f4c65e3a014`)

| Path | Git blob | SHA-256 |
| --- | --- | --- |
| `docs/reviews/ec6b-glm-q2-remediation-verification.md` | `29d0c27841664b7cd6e2412feed82c2c94e35719` | `2156ce000d11151edbdb0cae9ef918d52dd17509d29d821ae66cf9f1e690ea3b` |
| `tests/Penghou.Hetu.TreeSitter.Tests/ReviewEc6bGlmQ2Probes.cs` | `74a2fcc53f335b4fcb88883c5029356e34805fcb` | `481379db4c80e198b0539aed9faff19afd18408a76056ca88a0bea9f09248aae` |
| `tests/Penghou.Hetu.TreeSitter.Tests/ReviewEc6bGlmQ2LifecycleProbe.cs` | `1400e4f32dee191079d4cde4acffab74d585b5f7` | `05d9d7c5168a3dd602a6acc0a6b3b58cd38113f41d9a9c090eafce381480fa43` |
| `tests/Penghou.Hetu.TreeSitter.Tests/ReviewEc6bGlmQ2HistoricalProbe.cs` | `b15864c13dbb5b84480c87c16b30bc547b9ff1a3` | `237da6b84172c6db356d51cfd4fff83bfad67bba86a3e0e93a6518535e5a50c7` |

## Remediation evidence and handoff (`f8fb9d1c2917ef04fdf9bca721bcbeaaecb959e9`)

| Path | Git blob | SHA-256 |
| --- | --- | --- |
| `docs/evidence/ec6b-r1-relative-module-resolution-remediation/README.md` | `319c20f6960cf0e0bbffba0d58edf124149f4514` | `fcd43ec098d9164f55ceb1801170971950c728ece24169d58ef6a98195b0b49b` |
| `docs/evidence/ec6b-relative-module-resolution-handoff.md` | `f0f173459e95f713d03799b0b7b03e333a4b61a9` | `8be84a59a7cc5320e2559935a77975a2b217b8da335f03d64621721cc97a3908` |

## Tested implementation (`b915aed2a795f38d682fcd8289282eecd8110666`)

| Path | Git blob | SHA-256 |
| --- | --- | --- |
| `src/Penghou.Hetu.TreeSitter/TreeSitterModuleResolution.cs` | `c67500a081184ed7295b160cffa45ffacb0751e6` | `24eb1c65c20f7b75496d8aef076a3fc6db25d2d4854b0dc3c8a5269c8a47e866` |
| `src/Penghou.Hetu.TreeSitter/EcmaScriptExtractionProfile.cs` | `6e0e4dfc708689c36b4f74af6309a57b33dc6c53` | `c7c07f232d9f3481181ef9c7c785d06bbc72fc7bab7f5ba0126c4bd634d52681` |
| `src/Penghou.Hetu.TreeSitter/TreeSitterExtractionSession.cs` | `a7dc0451735dcf3bea8f0493b770e90248aee4be` | `e282dea4b45011b6203e39d0cbe3f962b785cf42e0aa2f43f70436a88f0e62e6` |
| `src/Penghou.Hetu.TreeSitter/TreeSitterTypeScriptPlugin.cs` | `176a9d106ffa1ffe13fcfcb505a8c9ba487c619f` | `54825d8a16babda56ea78968c6fcc0fdfdd9576dec27d92c7196cb724a6191be` |
| `src/Penghou.Hetu.TreeSitter/TreeSitterJavaScriptPlugin.cs` | `5bce0a5df5dbe1561e1daac7a04b98b1f2f7903c` | `f6c1bc4376fd1154462295beaec031fc8155b107158470078524992531424337` |
| `tests/Penghou.Hetu.TreeSitter.Tests/TreeSitterModuleResolutionTests.cs` | `4d3d662ba653e3415eaefe9f7e934be81dc8f7a8` | `755f04f2f99bfb9184e082766de510e5015c1e4e9f2fa13de572a351c5b94d91` |
| `tests/Penghou.Hetu.TreeSitter.Tests/TreeSitterModuleResolutionLifecycleTests.cs` | `b0e9eba91a319b0f66c377c94b4e8e710fc2e1ab` | `6368e3b29b6933678c70eb1415e27237b24598b74c6fb9662883a11b80d0f0da` |

## Notes

- The tested source and test blobs are recorded at the R1 test commit
  `b915aed`; `f8fb9d1` changes documentation only, so the accepted source
  content equals these blobs.
- The Q1/Q2 reports and probes are listed at their review commits because the
  review branches are preserved separately and are not merged into the
  acceptance branch.
- No generated graph database, website snapshot, build output, or temporary
  probe is part of the repository; those artifacts were deleted after the
  reviewer's execution and are described in the reviews.
- Hashes were recomputed from the committed blobs during the closure
  validation; the validation report records the checks.
