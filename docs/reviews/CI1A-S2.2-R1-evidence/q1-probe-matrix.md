| Unchanged Q1 probe/case | Before | After | Interpretation |
| --- | --- | --- | --- |
| `DamagedMetadataStreamOffsetAndTableCountAreRefused(rowCount: False)` | Passed | Passed | Preserved. |
| `DamagedMetadataStreamOffsetAndTableCountAreRefused(rowCount: True)` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "empty", bytes: [], reason: null, text: "")` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "indistinguishable-utf16", bytes: [65, 65], reason: null, text: "AA")` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "malformed-0000FE", bytes: [0, 0, 254], reason: "InvalidSourceEncoding", text: null)` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "malformed-0000FEFF00110000", bytes: [0, 0, 254, 255, 0, ···], reason: "InvalidSourceEncoding", text: null)` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "malformed-80", bytes: [128], reason: "InvalidSourceEncoding", text: null)` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "malformed-C0AF", bytes: [192, 175], reason: "InvalidSourceEncoding", text: null)` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "malformed-EF", bytes: [239], reason: "InvalidSourceEncoding", text: null)` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "malformed-EFBB", bytes: [239, 187], reason: "InvalidSourceEncoding", text: null)` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "malformed-FE", bytes: [254], reason: "InvalidSourceEncoding", text: null)` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "malformed-FEFFD800", bytes: [254, 255, 216, 0], reason: "InvalidSourceEncoding", text: null)` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "malformed-FF", bytes: [255], reason: "InvalidSourceEncoding", text: null)` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "malformed-FFFE00", bytes: [255, 254, 0], reason: "InvalidSourceEncoding", text: null)` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "malformed-FFFE000041", bytes: [255, 254, 0, 0, 65], reason: "InvalidSourceEncoding", text: null)` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "nul", bytes: [0], reason: "AmbiguousBomlessEncoding", text: null)` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "short", bytes: [97], reason: null, text: "a")` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "utf-16-bom", bytes: [255, 254, 47, 0, 47, ···], reason: null, text: "// café 中文 \ud83d\ude00\r\nclass A {}\n")` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "utf-16-bomless", bytes: [99, 0, 108, 0, 97, ···], reason: "AmbiguousBomlessEncoding", text: null)` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "utf-16BE-bom", bytes: [254, 255, 0, 47, 0, ···], reason: null, text: "// café 中文 \ud83d\ude00\r\nclass A {}\n")` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "utf-16BE-bomless", bytes: [0, 99, 0, 108, 0, ···], reason: "AmbiguousBomlessEncoding", text: null)` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "utf-32-bom", bytes: [255, 254, 0, 0, 47, ···], reason: null, text: "// café 中文 \ud83d\ude00\r\nclass A {}\n")` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "utf-32-bomless", bytes: [99, 0, 0, 0, 108, ···], reason: "AmbiguousBomlessEncoding", text: null)` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "utf-32BE-bom", bytes: [0, 0, 254, 255, 0, ···], reason: null, text: "// café 中文 \ud83d\ude00\r\nclass A {}\n")` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "utf-32BE-bomless", bytes: [0, 0, 0, 99, 0, ···], reason: "AmbiguousBomlessEncoding", text: null)` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "utf-8-bom", bytes: [239, 187, 191, 47, 47, ···], reason: null, text: "// café 中文 \ud83d\ude00\r\nclass A {}\n")` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "utf16-bom-only", bytes: [255, 254], reason: null, text: "")` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "utf32-bom-only", bytes: [255, 254, 0, 0], reason: null, text: "")` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "utf8", bytes: [47, 47, 32, 99, 97, ···], reason: null, text: "// café 中文 \ud83d\ude00\r\nclass A {}\n")` | Passed | Passed | Preserved. |
| `EncodingBoundary(name: "utf8-bom-nul", bytes: [239, 187, 191, 0], reason: null, text: "\0")` | Passed | Passed | Preserved. |
| `EncodingRefusalPrecedenceAndMaterializedHandoff` | Passed | Passed | Preserved. |
| `MalformedMetadataRootIsRefused` | Passed | Passed | Preserved. |
| `MappingReconstructsFrozenAuthorityAndSeparatesTransitiveProvenance` | Passed | Passed | Preserved. |
| `MetadataCancellationAndIoStaySeparateFromFormatRefusal` | Passed | Passed | Preserved. |
| `MetadataControlsAndTruncationSurvey` | Passed | Passed | Preserved. |
| `New02MatrixTupleAndFrozenUsageAnchors` | Passed | Passed | Preserved. |
| `ObserveFrozenCorpusReferenceProfileDiffersFromLiveCapture` | Passed | Passed | Preserved. |
| `ObserveMalformedMetadataStillExposesIdentity(damage: "assembly-name")` | Passed | Passed | Preserved. |
| `ObserveMalformedMetadataStillExposesIdentity(damage: "first-type-name")` | Passed | Failed | Expected observation mismatch: capture now refuses; producer regression passes. |
| `ObserveMalformedMetadataStillExposesIdentity(damage: "later-type-name")` | Passed | Failed | Expected observation mismatch: capture now refuses; producer regression passes. |
| `ObserveMalformedMetadataStillExposesIdentity(damage: "method-signature")` | Passed | Failed | Expected observation mismatch: capture now refuses; producer regression passes. |
| `ObserveTextuallyEquivalentLiveInstancesHaveSameIdentity` | Passed | Passed | Preserved. |
| `SeparatorInjection(symbol: ",")` | Passed | Passed | Preserved. |
| `SeparatorInjection(symbol: "A,B")` | Passed | Passed | Preserved. |
| `SeparatorInjection(symbol: "A,B&#124;C")` | Passed | Passed | Preserved. |
| `SeparatorInjection(symbol: "A&#124;B")` | Passed | Passed | Preserved. |
| `SeparatorInjection(symbol: "&#124;")` | Passed | Passed | Preserved. |
| `SymbolsOrderOrdinallyAndKeepUnicodeDistinct` | Passed | Passed | Preserved. |
