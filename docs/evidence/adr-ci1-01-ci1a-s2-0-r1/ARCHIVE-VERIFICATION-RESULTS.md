# Clean archive and tamper verification

Executed against a clean `git archive` extraction of commit `e877b8c` (tree before this results document), with the archive verifier's externally supplied trust anchor `D15E41AC06D7226B77348742645FCE62C4AAC35655CB2A3711F3A5EC422C032F`.

| Probe | Result | Evidence observed |
|---|---|---|
| Valid archive | PASS | 58 files match the trusted manifest; verifier reports Git-only claims unavailable |
| One-byte change to `Core/Methods.cs` | DETECTED | `hash drift` for the fixture |
| Remove `oracle-expected-r1.json` | DETECTED | required frozen file missing |
| One-byte change to `oracle-expected-r1.json` | DETECTED | hash drift for expected oracle |
| Restore all source bytes | PASS | 58 files again match |

The archive verifier did not claim branch, HEAD, or ancestry verification. A separate final Git-checkout verification is run on the committed source. Archive trust comes from the manifest SHA-256 passed by the Q2 handoff, not from trusting a self-declared manifest inside the archive.
