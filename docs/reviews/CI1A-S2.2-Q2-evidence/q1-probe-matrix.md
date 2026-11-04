| Unchanged Q1 case | Historical | Q2 execution | Classification |
| --- | --- | --- | --- |
| `IndependentFoundationReview.FoundationQualificationProbes.DamagedMetadataStreamOffsetAndTableCountAreRefused(rowCount: False)` | Passed | Passed | Preserved observation |
| `IndependentFoundationReview.FoundationQualificationProbes.DamagedMetadataStreamOffsetAndTableCountAreRefused(rowCount: True)` | Passed | Passed | Preserved observation |
| `IndependentFoundationReview.FoundationQualificationProbes.EncodingBoundary(name: "empty", bytes: [], reason: null, text: "")` | Passed | Passed | Preserved observation |
| `IndependentFoundationReview.FoundationQualificationProbes.EncodingBoundary(name: "indistinguishable-utf16", bytes: [65, 65], reason: null, text: "AA")` | Passed | Passed | Preserved observation |
| `IndependentFoundationReview.FoundationQualificationProbes.EncodingBoundary(name: "malformed-0000FE", bytes: [0, 0, 254], reason: "InvalidSourceEncoding", text: null)` | Passed | Passed | Preserved observation |
| `IndependentFoundationReview.FoundationQualificationProbes.EncodingBoundary(name: "malformed-0000FEFF00110000", bytes: [0, 0, 254, 255, 0, ···], reason: "InvalidSourceEncoding", text: null)` | Passed | Passed | Preserved observation |
| `IndependentFoundationReview.FoundationQualificationProbes.EncodingBoundary(name: "malformed-80", bytes: [128], reason: "InvalidSourceEncoding", text: null)` | Passed | Passed | Preserved observation |
| `IndependentFoundationReview.FoundationQualificationProbes.EncodingBoundary(name: "malformed-C0AF", bytes: [192, 175], reason: "InvalidSourceEncoding", text: null)` | Passed | Passed | Preserved observation |
| `IndependentFoundationReview.FoundationQualificationProbes.EncodingBoundary(name: "malformed-EF", bytes: [239], reason: "InvalidSourceEncoding", text: null)` | Passed | Passed | Preserved observation |
| `IndependentFoundationReview.FoundationQualificationProbes.EncodingBoundary(name: "malformed-EFBB", bytes: [239, 187], reason: "InvalidSourceEncoding", text: null)` | Passed | Passed | Preserved observation |
| `IndependentFoundationReview.FoundationQualificationProbes.EncodingBoundary(name: "malformed-FE", bytes: [254], reason: "InvalidSourceEncoding", text: null)` | Passed | Passed | Preserved observation |
| `IndependentFoundationReview.FoundationQualificationProbes.EncodingBoundary(name: "malformed-FEFFD800", bytes: [254, 255, 216, 0], reason: "InvalidSourceEncoding", text: null)` | Passed | Passed | Preserved observation |
| `IndependentFoundationReview.FoundationQualificationProbes.EncodingBoundary(name: "malformed-FF", bytes: [255], reason: "InvalidSourceEncoding", text: null)` | Passed | Passed | Preserved observation |
| `IndependentFoundationReview.FoundationQualificationProbes.EncodingBoundary(name: "malformed-FFFE00", bytes: [255, 254, 0], reason: "InvalidSourceEncoding", text: null)` | Passed | Passed | Preserved observation |
| `IndependentFoundationReview.FoundationQualificationProbes.EncodingBoundary(name: "malformed-FFFE000041", bytes: [255, 254, 0, 0, 65], reason: "InvalidSourceEncoding", text: null)` | Passed | Passed | Preserved observation |
| `IndependentFoundationReview.FoundationQualificationProbes.EncodingBoundary(name: "nul", bytes: [0], reason: "AmbiguousBomlessEncoding", text: null)` | Passed | Passed | Preserved observation |
| `IndependentFoundationReview.FoundationQualificationProbes.EncodingBoundary(name: "short", bytes: [97], reason: null, text: "a")` | Passed | Passed | Preserved observation |
| `IndependentFoundationReview.FoundationQualificationProbes.EncodingBoundary(name: "utf-16-bom", bytes: [255, 254, 47, 0, 47, ···], reason: null, text: "// café 中文 \ud83d\ude00\r\nclass A {}\n")` | Passed | Passed | Preserved observation |
| `IndependentFoundationReview.FoundationQualificationProbes.EncodingBoundary(name: "utf-16-bomless", bytes: [99, 0, 108, 0, 97, ···], reason: "AmbiguousBomlessEncoding", text: null)` | Passed | Passed | Preserved observation |
| `IndependentFoundationReview.FoundationQualificationProbes.EncodingBoundary(name: "utf-16BE-bom", bytes: [254, 255, 0, 47, 0, ···], reason: null, text: "// café 中文 \ud83d\ude00\r\nclass A {}\n")` | Passed | Passed | Preserved observation |
| `IndependentFoundationReview.FoundationQualificationProbes.EncodingBoundary(name: "utf-16BE-bomless", bytes: [0, 99, 0, 108, 0, ···], reason: "AmbiguousBomlessEncoding", text: null)` | Passed | Passed | Preserved observation |
| `IndependentFoundationReview.FoundationQualificationProbes.EncodingBoundary(name: "utf-32-bom", bytes: [255, 254, 0, 0, 47, ···], reason: null, text: "// café 中文 \ud83d\ude00\r\nclass A {}\n")` | Passed | Passed | Preserved observation |
| `IndependentFoundationReview.FoundationQualificationProbes.EncodingBoundary(name: "utf-32-bomless", bytes: [99, 0, 0, 0, 108, ···], reason: "AmbiguousBomlessEncoding", text: null)` | Passed | Passed | Preserved observation |
| `IndependentFoundationReview.FoundationQualificationProbes.EncodingBoundary(name: "utf-32BE-bom", bytes: [0, 0, 254, 255, 0, ···], reason: null, text: "// café 中文 \ud83d\ude00\r\nclass A {}\n")` | Passed | Passed | Preserved observation |
| `IndependentFoundationReview.FoundationQualificationProbes.EncodingBoundary(name: "utf-32BE-bomless", bytes: [0, 0, 0, 99, 0, ···], reason: "AmbiguousBomlessEncoding", text: null)` | Passed | Passed | Preserved observation |
| `IndependentFoundationReview.FoundationQualificationProbes.EncodingBoundary(name: "utf-8-bom", bytes: [239, 187, 191, 47, 47, ···], reason: null, text: "// café 中文 \ud83d\ude00\r\nclass A {}\n")` | Passed | Passed | Preserved observation |
| `IndependentFoundationReview.FoundationQualificationProbes.EncodingBoundary(name: "utf16-bom-only", bytes: [255, 254], reason: null, text: "")` | Passed | Passed | Preserved observation |
| `IndependentFoundationReview.FoundationQualificationProbes.EncodingBoundary(name: "utf32-bom-only", bytes: [255, 254, 0, 0], reason: null, text: "")` | Passed | Passed | Preserved observation |
| `IndependentFoundationReview.FoundationQualificationProbes.EncodingBoundary(name: "utf8", bytes: [47, 47, 32, 99, 97, ···], reason: null, text: "// café 中文 \ud83d\ude00\r\nclass A {}\n")` | Passed | Passed | Preserved observation |
| `IndependentFoundationReview.FoundationQualificationProbes.EncodingBoundary(name: "utf8-bom-nul", bytes: [239, 187, 191, 0], reason: null, text: "\0")` | Passed | Passed | Preserved observation |
| `IndependentFoundationReview.FoundationQualificationProbes.EncodingRefusalPrecedenceAndMaterializedHandoff` | Passed | Passed | Preserved observation |
| `IndependentFoundationReview.FoundationQualificationProbes.MalformedMetadataRootIsRefused` | Passed | Passed | Preserved observation |
| `IndependentFoundationReview.FoundationQualificationProbes.MappingReconstructsFrozenAuthorityAndSeparatesTransitiveProvenance` | Passed | Passed | Preserved observation |
| `IndependentFoundationReview.FoundationQualificationProbes.MetadataCancellationAndIoStaySeparateFromFormatRefusal` | Passed | Passed | Preserved observation |
| `IndependentFoundationReview.FoundationQualificationProbes.MetadataControlsAndTruncationSurvey` | Passed | Passed | Preserved observation |
| `IndependentFoundationReview.FoundationQualificationProbes.New02MatrixTupleAndFrozenUsageAnchors` | Passed | Passed | Preserved observation |
| `IndependentFoundationReview.FoundationQualificationProbes.ObserveFrozenCorpusReferenceProfileDiffersFromLiveCapture` | Passed | Passed | Preserved observation |
| `IndependentFoundationReview.FoundationQualificationProbes.ObserveMalformedMetadataStillExposesIdentity(damage: "assembly-name")` | Passed | Passed | Preserved observation |
| `IndependentFoundationReview.FoundationQualificationProbes.ObserveMalformedMetadataStillExposesIdentity(damage: "first-type-name")` | Passed | Failed | Historical observation invalidated by remediation |
| `IndependentFoundationReview.FoundationQualificationProbes.ObserveMalformedMetadataStillExposesIdentity(damage: "later-type-name")` | Passed | Failed | Historical observation invalidated by remediation |
| `IndependentFoundationReview.FoundationQualificationProbes.ObserveMalformedMetadataStillExposesIdentity(damage: "method-signature")` | Passed | Failed | Historical observation invalidated by remediation |
| `IndependentFoundationReview.FoundationQualificationProbes.ObserveTextuallyEquivalentLiveInstancesHaveSameIdentity` | Passed | Passed | Preserved observation |
| `IndependentFoundationReview.FoundationQualificationProbes.SeparatorInjection(symbol: ",")` | Passed | Passed | Preserved observation |
| `IndependentFoundationReview.FoundationQualificationProbes.SeparatorInjection(symbol: "A,B")` | Passed | Passed | Preserved observation |
| `IndependentFoundationReview.FoundationQualificationProbes.SeparatorInjection(symbol: "A,B&#124;C")` | Passed | Passed | Preserved observation |
| `IndependentFoundationReview.FoundationQualificationProbes.SeparatorInjection(symbol: "A&#124;B")` | Passed | Passed | Preserved observation |
| `IndependentFoundationReview.FoundationQualificationProbes.SeparatorInjection(symbol: "&#124;")` | Passed | Passed | Preserved observation |
| `IndependentFoundationReview.FoundationQualificationProbes.SymbolsOrderOrdinallyAndKeepUnicodeDistinct` | Passed | Passed | Preserved observation |
