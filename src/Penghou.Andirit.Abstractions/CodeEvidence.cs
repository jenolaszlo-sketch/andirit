namespace Penghou.Andirit;

/// <summary>
/// Describes how a graph fact was established. Quality answers <em>how</em>;
/// <see cref="CodeEvidence.Provider"/> answers <em>who</em>.
/// </summary>
public enum EvidenceQuality
{
    /// <summary>
    /// The fact follows directly from source structure but has not been
    /// semantically resolved (declarations, containment, syntactic imports,
    /// structural invocations).
    /// </summary>
    Structural = 0,
    /// <summary>
    /// A language-aware provider resolved the identity or relationship with
    /// its semantic model, relative to the analysis context it constructed.
    /// Resolved means resolved-by-the-named-provider, not universally exact.
    /// </summary>
    Resolved = 1,
    /// <summary>
    /// Andirit deterministically computed the fact from other evidence.
    /// Derived evidence must never present itself as resolved.
    /// </summary>
    Derived = 2,
    /// <summary>
    /// The fact is probabilistic or interpretive (classifiers, heuristic
    /// guesses, model output). It must never satisfy deterministic queries.
    /// </summary>
    Inferred = 3
}

/// <summary>
/// Explicit evidence-quality predicates. Quality values are intentionally
/// <em>not</em> ordered strength levels: <c>Derived</c> must never read as
/// stronger than <c>Resolved</c> just because its numeric value is larger.
/// Compare qualities by name or by set membership, never by ordinal.
/// </summary>
public static class EvidenceQualityExtensions
{
    /// <summary>
    /// Reports whether the quality is deterministic. Deterministic facts
    /// must not carry probabilistic confidence; only inferred facts may.
    /// </summary>
    public static bool IsDeterministic(this EvidenceQuality quality) =>
        quality is EvidenceQuality.Structural or EvidenceQuality.Resolved or EvidenceQuality.Derived;
}

/// <summary>Describes how a graph relationship was discovered.</summary>
public sealed record CodeEvidence
{
    public CodeEvidence(
        EvidenceQuality quality,
        string provider,
        string? providerVersion = null,
        CodeLocation? location = null,
        double? confidence = null)
    {
        if (!Enum.IsDefined(quality))
            throw new ArgumentOutOfRangeException(nameof(quality));
        if (confidence is < 0 or > 1 ||
            confidence is { } score && !double.IsFinite(score))
        {
            throw new ArgumentOutOfRangeException(nameof(confidence));
        }

        if (confidence is not null && quality.IsDeterministic())
        {
            throw new ArgumentException(
                "Deterministic structural, resolved, and derived evidence cannot declare confidence.",
                nameof(confidence));
        }

        Quality = quality;
        Provider = ContractValue.Identifier(provider, nameof(provider));
        ProviderVersion = string.IsNullOrWhiteSpace(providerVersion)
            ? null
            : providerVersion;
        Location = location;
        Confidence = confidence;
    }

    public EvidenceQuality Quality { get; }
    public string Provider { get; }
    public string? ProviderVersion { get; }
    public CodeLocation? Location { get; }
    public double? Confidence { get; }
}
