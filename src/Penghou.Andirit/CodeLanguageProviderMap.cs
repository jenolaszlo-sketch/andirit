namespace Penghou.Andirit;

/// <summary>
/// Ordered language-to-provider preference: each language maps to its
/// preferred plugin identities, most preferred first. An ordered list —
/// not numeric priority — communicates exactly what selection needs.
///
/// The first entry is the preferred provider; later entries are eligible
/// fallbacks subject to the configured <see cref="ProviderFallbackPolicy"/>.
/// A language with no entry keeps today's strict registry behavior.
/// </summary>
public sealed class CodeLanguageProviderMap
{
    private readonly IReadOnlyDictionary<string, IReadOnlyList<CodePluginId>> _orderings;

    public static CodeLanguageProviderMap Empty { get; } = new CodeLanguageProviderMap();

    public CodeLanguageProviderMap(
        IReadOnlyDictionary<string, IReadOnlyList<CodePluginId>>? orderings = null)
    {
        var copy = new SortedDictionary<string, IReadOnlyList<CodePluginId>>(StringComparer.Ordinal);
        if (orderings is not null)
        {
            foreach (var (language, providerIds) in orderings)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(language, nameof(orderings));
                ArgumentNullException.ThrowIfNull(providerIds, nameof(orderings));
                var ids = providerIds.ToArray();
                if (ids.Length == 0)
                    throw new ArgumentException(
                        $"Language '{language}' must list at least one provider.",
                        nameof(orderings));
                if (ids.Any(id => id is null))
                    throw new ArgumentException(
                        $"Language '{language}' cannot list null provider identities.",
                        nameof(orderings));
                if (ids.Distinct().Count() != ids.Length)
                    throw new ArgumentException(
                        $"Language '{language}' lists a provider more than once.",
                        nameof(orderings));
                copy.Add(language, ids);
            }
        }

        _orderings = copy;
    }

    public bool IsEmpty => _orderings.Count == 0;

    /// <summary>Gets the configured provider orderings by language.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<CodePluginId>> Orderings => _orderings;

    /// <summary>Gets the preferred-first provider order for a language.</summary>
    public bool TryGetOrder(
        string language,
        out IReadOnlyList<CodePluginId> providerIds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(language);
        if (_orderings.TryGetValue(language, out var order))
        {
            providerIds = order;
            return true;
        }

        providerIds = [];
        return false;
    }
}
