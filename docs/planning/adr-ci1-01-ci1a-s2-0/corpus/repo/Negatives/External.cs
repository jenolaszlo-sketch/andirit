namespace Corpus.Negatives;

/// <summary>
/// A call into an in-memory metadata assembly (no source project unit in the
/// declared universe). Roslyn resolves the symbol; Andirit has no accepted
/// defining-compilation provenance for a confirmed source-defined target.
/// </summary>
public sealed class ExternalSites
{
    public int CallExternal() => /*@E01*/ Corpus.External.Api.Value();
}
