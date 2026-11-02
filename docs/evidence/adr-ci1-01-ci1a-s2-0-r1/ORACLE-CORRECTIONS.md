# Oracle corrections

- **Candidate errors:** `CS0121`/Roslyn ambiguity is classified `Ambiguous`; the actual CS0176 invalid static-member access is `InvalidInvocation`. CLS02 freezes the latter. N01 remains the genuine CS0121 case.
- **Event symbol:** S15 now locates `methods.Changed` and freezes the event symbol `Corpus.Core!E:Corpus.Core.Methods.Changed`. `OnChanged` remains a distinct handler method and is not the semantic target of S15.
- **Coverage:** complete-empty, incomplete and NEW-04 checks are marked `model-assertion`; report values carry explicit `assertionLevel` text. They do not call production coverage APIs. NEW-04 remains open.
