namespace SemanticCorpusOracle;

/// <summary>
/// Accepted frozen anchors transcribed from the controlling contract documents,
/// not from the helper under test:
///
///   * <c>compctx1</c>/<c>compctx2</c> and the six source-defined <c>asmctx3</c>
///     target vectors are the accepted values in
///     <c>identity-and-provenance-mapping.md</c> §4 and the frozen R2 vectors;
///   * the two <c>usage4</c> values are the R1 main identity vectors that the
///     independent Q2 review confirmed as the correct canonical values.
///
/// They give the R2 cross-view and defining-provenance checks an authority that
/// cannot be produced by the same computation it is checking. A mutation of a
/// computed value, of the supplied evidence, or of the frozen R2 expected file
/// must fail one of these anchors or a cross-view equality.
/// </summary>
public static class FrozenExpectations
{
    public const string CoreUnit = "Core/Core.csproj";
    public const string LeafUnit = "Leaf/Leaf.csproj";

    public const string CoreCompctx1 =
        "compctx1:sha256:942a8e588a8db15afafb30d0292e0b867c767c5f72392b6f458783d961a71db3";
    public const string CoreCompctx2 =
        "compctx2:sha256:7bf4ac5b916ab5dcd2fadaa45c22cc7765ab83839eb7de8ef4d442865d07424d";
    public const string LeafCompctx1 =
        "compctx1:sha256:404c5e48c3106b47786f14cad594139477c651fd78d0e050f8fd5bcab18fcb5f";
    public const string LeafCompctx2 =
        "compctx2:sha256:7f1128703a0a8c3b0490e55a293898bdfb28ba2e4a125d6abc70270d06b7c777";

    public static readonly IReadOnlyDictionary<string, string> Asmctx3 =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["T.Direct"] =
                "asmctx3:sha256:42a30aaf20d663a3115234de4d7adc42cc3b6a18144d0c7e44746f1d1670513a",
            ["T.Static"] =
                "asmctx3:sha256:4eb4bd02228c0a52b09a546c9b872bfe06cb7e9d411256c71440d35c50eb6f6a",
            ["T.Add2"] =
                "asmctx3:sha256:adcc221501d9d95b6dbb5c3f797f0954d05043064e63c75fc904faad6c93a5cc",
            ["T.Add3"] =
                "asmctx3:sha256:46fcba57e8d04a8f3d812d6dbb2f43cb7555d25f0ab5c67aefd5e1a35e84388a",
            ["T.Conditional"] =
                "asmctx3:sha256:82246b216b702a3cf541579cf1f0e0f0625b2a01a2427a468c0f3d92f5731c77",
            ["T.LeafCompute"] =
                "asmctx3:sha256:a8c76853f52d8d49fc64335ed55a11b8ccd0f087559b7b7630d6ba6ccadfc05a",
        };

    public static readonly IReadOnlyDictionary<string, string> Usage4 =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["S01"] =
                "d1ae71fc315d0c1a5b41559e4ba7a6b10c9dc417eb3a52b9cd0781fb67b0b556",
            ["INV-01"] =
                "699a94f4e670ec85df318174f0ec235d58fd33e157b186dc9b30dbd8c76df0c5",
        };
}
