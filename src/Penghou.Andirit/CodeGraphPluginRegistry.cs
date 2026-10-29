namespace Penghou.Andirit;

/// <summary>Immutable, deterministic registry of extraction plugins.</summary>
public sealed class CodeGraphPluginRegistry
{
    private readonly IReadOnlyList<ICodeGraphPlugin> _plugins;

    public CodeGraphPluginRegistry(
        IEnumerable<ICodeGraphPlugin> plugins,
        CodeLanguageProviderMap? providerMap = null)
    {
        ArgumentNullException.ThrowIfNull(plugins);
        _plugins = plugins
            .Select(Validate)
            .OrderBy(plugin => plugin.Language, StringComparer.Ordinal)
            .ThenBy(plugin => plugin.Id.Value, StringComparer.Ordinal)
            .ToArray();
        var duplicate = _plugins
            .GroupBy(plugin => plugin.Id.Value, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new ArgumentException(
                $"Plugin identity '{duplicate.Key}' is registered more than once.",
                nameof(plugins));
        }

        ProviderMap = providerMap ?? CodeLanguageProviderMap.Empty;
        var registered = _plugins.Select(plugin => plugin.Id).ToHashSet();
        foreach (var (language, order) in ProviderMap.Orderings)
        {
            var unknown = order.FirstOrDefault(id => !registered.Contains(id));
            if (unknown is not null)
            {
                throw new ArgumentException(
                    $"Language '{language}' prefers unregistered plugin '{unknown.Value}'. " +
                    "Provider maps must reference registered plugins.",
                    nameof(providerMap));
            }
        }
    }

    public IReadOnlyList<ICodeGraphPlugin> Plugins => _plugins;

    /// <summary>Gets the configured language-provider preference order.</summary>
    public CodeLanguageProviderMap ProviderMap { get; }

    public IReadOnlyList<ICodeGraphPlugin> FindCandidates(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return _plugins.Where(plugin => plugin.CanHandle(path)).ToArray();
    }

    /// <summary>
    /// Selects the plugin for a path. Without a configured provider order this
    /// keeps the legacy contract: no match returns null, several matches
    /// throw. A configured order selects the most preferred candidate for a
    /// mapped language; candidates that no configured provider claims, and
    /// candidates from unmapped languages, keep the legacy behavior.
    /// </summary>
    public ICodeGraphPlugin? Resolve(string path)
    {
        var candidates = FindCandidates(path);
        if (candidates.Count == 0)
            return null;
        if (ProviderMap.IsEmpty)
            return ResolveLegacy(path, candidates);

        var winners = new List<ICodeGraphPlugin>();
        foreach (var group in candidates.GroupBy(
                     plugin => plugin.Language,
                     StringComparer.Ordinal))
        {
            if (!ProviderMap.TryGetOrder(group.Key, out var order))
            {
                // Unmapped languages keep the legacy contract.
                winners.Add(ResolveLegacy(path, group.ToArray()));
                continue;
            }

            var byId = group.ToDictionary(plugin => plugin.Id);
            var unlisted = group.Where(plugin => !order.Contains(plugin.Id)).ToArray();
            if (unlisted.Length > 0)
            {
                // Explicit configuration must be exhaustive for its language:
                // an unlisted claimant is ambiguity, not a silent loser.
                throw new CodeGraphPluginSelectionException(path, candidates);
            }

            ICodeGraphPlugin? winner = null;
            foreach (var id in order)
            {
                if (byId.TryGetValue(id, out var match))
                {
                    winner = match;
                    break;
                }
            }

            // No winner when every configured provider declines this path
            // through CanHandle: the path stays unsupported for the language.
            if (winner is not null)
                winners.Add(winner);
        }

        return winners.Count switch
        {
            0 => null,
            1 => winners[0],
            _ => throw new CodeGraphPluginSelectionException(path, winners)
        };
    }

    /// <summary>
    /// Gets the next eligible fallback provider after a failed plugin: the
    /// entry following it in its language order. Returns false when the
    /// language has no order, the plugin is unlisted, or it is already last.
    /// </summary>
    public bool TryGetFallback(ICodeGraphPlugin plugin, out ICodeGraphPlugin? fallback)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        fallback = null;
        if (!ProviderMap.TryGetOrder(plugin.Language, out var order))
            return false;

        var index = -1;
        for (var position = 0; position < order.Count; position++)
        {
            if (order[position] == plugin.Id)
            {
                index = position;
                break;
            }
        }

        if (index < 0 || index + 1 >= order.Count)
            return false;

        fallback = _plugins.FirstOrDefault(candidate => candidate.Id == order[index + 1]);
        return fallback is not null;
    }

    private static ICodeGraphPlugin ResolveLegacy(string path, IReadOnlyList<ICodeGraphPlugin> candidates) =>
        candidates.Count switch
        {
            1 => candidates[0],
            _ => throw new CodeGraphPluginSelectionException(path, candidates)
        };

    private static ICodeGraphPlugin Validate(ICodeGraphPlugin plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ArgumentNullException.ThrowIfNull(plugin.Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(plugin.Version);
        ArgumentException.ThrowIfNullOrWhiteSpace(plugin.Language);
        ArgumentNullException.ThrowIfNull(plugin.FileExtensions);
        if (plugin.FileExtensions.Count == 0 ||
            plugin.FileExtensions.Any(extension =>
                string.IsNullOrWhiteSpace(extension) ||
                !extension.StartsWith(".", StringComparison.Ordinal)))
        {
            throw new ArgumentException(
                $"Plugin '{plugin.Id}' must declare normalized file extensions.",
                nameof(plugin));
        }

        return plugin;
    }
}

/// <summary>Reports that more than one plugin claimed the same path.</summary>
public sealed class CodeGraphPluginSelectionException : Exception
{
    public CodeGraphPluginSelectionException(
        string path,
        IReadOnlyList<ICodeGraphPlugin> candidates)
        : base($"Multiple code graph plugins can handle '{path}'.")
    {
        Path = path;
        CandidateIds = candidates
            .Select(candidate => candidate.Id)
            .ToArray();
    }

    public string Path { get; }
    public IReadOnlyList<CodePluginId> CandidateIds { get; }
}

/// <summary>Fluent standalone construction for the Andirit plugin registry.</summary>
/// <remarks>
/// Prefer <see cref="AndiritHostBuilder"/> for complete hosts. Use this builder
/// only when composing registries manually around a caller-owned store or a
/// standalone <see cref="CodeIndexingService"/>.
/// </remarks>
public sealed class AndiritBuilder
{
    private readonly List<ICodeGraphPlugin> _plugins = [];
    private readonly List<ICodeRepositoryProvider> _repositoryProviders =
        [new FileSystemCodeRepositoryProvider()];
    private readonly Dictionary<string, List<CodePluginId>> _providerOrders = new(StringComparer.Ordinal);

    public AndiritBuilder AddPlugin(ICodeGraphPlugin plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        _plugins.Add(plugin);
        return this;
    }

    /// <summary>
    /// Configures preferred-first provider order for a language. The first
    /// identity is preferred; later identities are eligible fallbacks subject
    /// to the indexing fallback policy. Calling again for a language replaces
    /// its order. Referenced identities must be registered before the registry
    /// is built.
    /// </summary>
    public AndiritBuilder WithLanguageProviders(
        string language,
        params CodePluginId[] providerIds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(language);
        ArgumentNullException.ThrowIfNull(providerIds);
        if (providerIds.Length == 0)
            throw new ArgumentException(
                "A language must list at least one provider.",
                nameof(providerIds));
        if (providerIds.Any(id => id is null))
            throw new ArgumentException(
                "Provider identities cannot contain null entries.",
                nameof(providerIds));
        _providerOrders[language] = providerIds.Distinct().ToList();
        if (_providerOrders[language].Count != providerIds.Length)
            throw new ArgumentException(
                $"Language '{language}' lists a provider more than once.",
                nameof(providerIds));
        return this;
    }

    public CodeGraphPluginRegistry BuildPluginRegistry() =>
        new(
            _plugins,
            new CodeLanguageProviderMap(
                _providerOrders.ToDictionary(
                    pair => pair.Key,
                    pair => (IReadOnlyList<CodePluginId>)pair.Value.AsReadOnly(),
                    StringComparer.Ordinal)));

    public AndiritBuilder AddRepositoryProvider(ICodeRepositoryProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _repositoryProviders.Add(provider);
        return this;
    }

    public AndiritBuilder ClearRepositoryProviders()
    {
        _repositoryProviders.Clear();
        return this;
    }

    public CodeRepositoryProviderRegistry BuildRepositoryProviderRegistry() =>
        new(_repositoryProviders);
}
