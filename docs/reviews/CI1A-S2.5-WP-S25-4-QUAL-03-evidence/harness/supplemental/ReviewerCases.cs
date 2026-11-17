internal static partial class Probe
{
    // Hand-enumerated usage sites, independent of Roslyn traversal/production counters.
    private static readonly Dictionary<string, (string Source, string[] Sites)> ReviewerSites = new()
    {
        ["delegates"] = ("_ = new System.Func<int>(QualifierDependency.Api.Mark);", ["explicit delegate construction", "method group argument"]),
        ["constructors"] = ("_ = new QualifierDependency.Value(); QualifierDependency.Value v = new();", ["explicit constructor", "target-typed constructor"]),
        ["properties"] = ("QualifierDependency.Value v = null!; _ = v.Next.Count2; v.Count2 += 1; _ = v[0];", ["Next receiver read", "Count2 read", "Count2 compound assignment site", "indexer read"]),
        ["groups"] = ("System.Func<int> a = QualifierDependency.Api.Mark; System.Func<int> b = (System.Func<int>)QualifierDependency.Api.Mark;", ["assignment method group", "cast method group"]),
        ["overlap"] = ("_ = new QualifierDependency.Value { Count2 = QualifierDependency.Value.Count };", ["constructor", "initializer property write", "static property read"]),
        ["mixed"] = ("_ = new QualifierDependency.Value(); _ = QualifierDependency.Value.Count; System.Func<int> a = QualifierDependency.Api.Mark;", ["constructor", "property read", "method group"]),
        ["unbound"] = ("_ = new Missing(); _ = QualifierDependency.Value.Missing; System.Func<int> a = QualifierDependency.Api.Missing;", []),
        ["ambiguous"] = ("_ = new QualifierDependency.Choice(null);", []),
        ["external"] = ("_ = new System.Text.StringBuilder(); _ = string.Empty.Length; System.Func<string,string,string> a = string.Concat;", ["metadata constructor", "metadata property", "metadata method group"]),
        ["unrelated"] = ("_ = typeof(QualifierDependency.Value); _ = nameof(QualifierDependency.Value.Count); _ = QualifierDependency.Value.Field; QualifierDependency.Value v = null!;", []),
    };
}

