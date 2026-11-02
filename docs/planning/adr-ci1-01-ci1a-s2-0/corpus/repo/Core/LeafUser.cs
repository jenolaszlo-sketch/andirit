namespace Corpus.Core;

/// <summary>
/// Makes this project dependency-bearing so its R2-faithful legacy compctx1 and
/// its verified compctx2 can be compared.
/// </summary>
public static class LeafUser
{
    public static int UseLeaf() => Corpus.Leaf.Helper.Compute(21);
}
