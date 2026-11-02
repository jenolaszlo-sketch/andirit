# Compilation option variant — OPT01

`options-changed` uses the same source file paths and exact source bytes as baseline, the same references and parse options, and a different actual Roslyn `CSharpCompilationOptions.CheckOverflow` value (`false` to `true`). The option participates in the accepted R2 `OptionsDigest`.

The oracle verifies the options digest and `compctx1`/`compctx2` change for the affected compilations. Because the defining Core compilation also has the material option change, its `compctx1` changes and the applicable `asmctx3` changes according to the accepted tuple; no artificial target stability is asserted. The S01 Roslyn binding remains the same documentation ID. Rebuilding with the baseline option reproduces the frozen baseline context. The frozen R2 anchor vectors were not changed.
