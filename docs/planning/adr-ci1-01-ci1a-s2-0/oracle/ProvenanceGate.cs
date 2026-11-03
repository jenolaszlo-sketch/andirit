namespace SemanticCorpusOracle;

/// <summary>
/// The accepted Option A validity gate, expressed once.
///
/// Candidate evidence is untrusted. A recorded defining <c>compctx2</c> is
/// eligible only when:
///   1. it is present;
///   2. it equals the independently derived current context of the defining
///      compilation;
///   3. the compilation-universe membership carries that same verified current
///      context for the defining unit.
///
/// A syntactically valid digest, a copied digest, a matching count or an
/// <c>asmctx3</c> equality never establishes authority on its own.
/// </summary>
public static class ProvenanceGate
{
    public static bool Eligible(string? recorded,
        IReadOnlyDictionary<string, string> current,
        IReadOnlyDictionary<string, string> universe,
        string unit) =>
        recorded is not null &&
        current.TryGetValue(unit, out var currentContext) &&
        string.Equals(currentContext, recorded, StringComparison.Ordinal) &&
        universe.TryGetValue(unit, out var memberContext) &&
        string.Equals(memberContext, currentContext, StringComparison.Ordinal);

    /// <summary>
    /// Universe membership bound to the verified per-unit contexts of the
    /// supplied analysis snapshot. Membership is not a bare count or unit list.
    /// </summary>
    public static IReadOnlyDictionary<string, string> Universe(
        CorpusIdentity identity, params string[] units) =>
        units.Distinct(StringComparer.Ordinal)
            .ToDictionary(unit => unit, unit => identity.Compctx2[unit], StringComparer.Ordinal);
}
