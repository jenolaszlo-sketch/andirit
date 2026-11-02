namespace Corpus.Leaf;

/// <summary>
/// Dependency-free leaf project. Used to demonstrate transitive dependency
/// context propagation (compctx2) while a defining project's R2 tree material
/// stays unchanged when only this project's preprocessor symbols change.
/// </summary>
public static class Helper
{
    public static int Compute(int value) => value * 2;

    public static int Extra(int value) => value + 1;

#if LEAF_TRACE
    public static string Trace() => "leaf-trace";
#endif
}
