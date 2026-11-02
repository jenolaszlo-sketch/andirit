# E02 — resolved target, missing defining provenance

E02 is a frozen semantic record backed by the real Roslyn compilation. Roslyn resolves the marked S01 invocation to `Corpus.Core!M:Corpus.Core.Methods.Direct(System.Int32)` and identifies the defining assembly as `Corpus.Core`.

The E02 refusal gate supplies no authoritative defining `compctx2` association and excludes `Core/Core.csproj` from the verified universe. The symbol is still resolved; confirmed publication is refused because authoritative provenance is unavailable. The positive control supplies the verified Core context and universe membership, changing provenance eligibility. E01 remains distinct: it resolves a metadata-emitted `Corpus.External` symbol with no source-defined `asmctx3` input. The oracle does not claim either gate is production enforcement.
