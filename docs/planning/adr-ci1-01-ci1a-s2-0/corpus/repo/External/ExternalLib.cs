namespace Corpus.External;

/// <summary>
/// Compiled to an in-memory metadata image by the oracle harness. It is not a
/// declared project unit of the corpus universe; it represents an external,
/// source-out-of-scope target whose symbol Roslyn can still resolve.
/// </summary>
public static class Api
{
    public static int Value() => 42;

    public static string Name() => "external";
}
