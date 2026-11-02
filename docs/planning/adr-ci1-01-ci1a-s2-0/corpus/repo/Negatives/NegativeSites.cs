using Corpus.Core;

namespace Corpus.Negatives;

/// <summary>
/// Negative and refusal scenarios. This fixture intentionally does not compile
/// without diagnostics; the oracle records the actual Roslyn result rather than
/// inventing a confirmed target.
/// </summary>
public sealed class NegativeSites
{
    public int Ambiguous()
    {
        var m = new Methods();
        return /*@N01*/ m.Amb(1, 1);
    }

    public int InvalidStaticAccess()
    {
        var m = new Methods();
        return /*@CLS02*/ m.Static(1);
    }

    public int Dynamic()
    {
        dynamic d = new Methods();
        return /*@N03*/ d.Direct(1);
    }

    public int Unresolved()
    {
        return /*@N02*/ Missing.Type.Invoke();
    }

    public int Inactive(Methods m)
    {
#if NEVER_DEFINED_SYMBOL
        return /*@I01*/ m.Direct(99);
#else
        return 0;
#endif
    }
}
