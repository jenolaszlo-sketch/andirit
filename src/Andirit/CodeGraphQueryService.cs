namespace Andirit;

/// <summary>Common bounds and evidence filters for provider-neutral graph queries.</summary>
public sealed record CodeGraphQueryOptions
{
    public CodeGraphQueryOptions(
        int maxDepth = 1,
        int maxNodes = 100,
        int maxEdges = 250,
        IReadOnlyCollection<EvidenceQuality>? evidenceQualities = null)
    {
        if (maxDepth < 0)
            throw new ArgumentOutOfRangeException(nameof(maxDepth));
        if (maxNodes < 1)
            throw new ArgumentOutOfRangeException(nameof(maxNodes));
        if (maxEdges < 1)
            throw new ArgumentOutOfRangeException(nameof(maxEdges));
        MaxDepth = maxDepth;
        MaxNodes = maxNodes;
        MaxEdges = maxEdges;
        EvidenceQualities = evidenceQualities?.Distinct().ToArray() ?? [];
    }

    public int MaxDepth { get; }
    public int MaxNodes { get; }
    public int MaxEdges { get; }
    public IReadOnlyCollection<EvidenceQuality> EvidenceQualities { get; }
}

/// <summary>An exact qualified-name lookup that preserves ambiguity.</summary>
public sealed record CodeSymbolLookupResult(IReadOnlyList<CodeGraphNode> Candidates)
{
    public bool IsAmbiguous => Candidates.Count > 1;
    public CodeGraphNode? SingleOrDefault => Candidates.Count == 1 ? Candidates[0] : null;
}

/// <summary>Provides bounded semantic queries without exposing store-specific languages.</summary>
public sealed class CodeGraphQueryService
{
    private const int MaximumBatchSymbols = 100;
    private const int MaximumTraversalSeeds = 32;
    private readonly ICodeGraphReader _store;

    public CodeGraphQueryService(ICodeGraphReader store) =>
        _store = store ?? throw new ArgumentNullException(nameof(store));

    /// <summary>Gets the latest publication and opens a fail-on-change query view.</summary>
    public async ValueTask<CodeGraphPublicationQuery?> OpenLatestPublicationAsync(
        CodeRepositoryId repositoryId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repositoryId);
        var publication = await _store.GetLatestPublicationAsync(
            repositoryId,
            cancellationToken).ConfigureAwait(false);
        return publication is null ? null : Bind(publication);
    }

    /// <summary>Creates a query view that rejects results from any other publication.</summary>
    public CodeGraphPublicationQuery Bind(CodeGraphPublication publication) =>
        new(this, publication ?? throw new ArgumentNullException(nameof(publication)));

    /// <summary>
    /// Looks up candidate symbols by exact qualified name, preserving
    /// ambiguity. Named for the lookup key: the store-level
    /// <c>FindSymbolAsync</c> takes a semantic symbol id instead.
    /// </summary>
    public async ValueTask<CodeSymbolLookupResult> FindSymbolsByQualifiedNameAsync(
        CodeRepositoryId repositoryId,
        string qualifiedName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repositoryId);
        if (string.IsNullOrWhiteSpace(qualifiedName))
            throw new ArgumentException("Qualified name is required.", nameof(qualifiedName));
        var candidates = await _store.FindNodesByQualifiedNameAsync(
            repositoryId,
            qualifiedName,
            cancellationToken).ConfigureAwait(false);
        return new(candidates.OrderBy(node => node.Id.Value, StringComparer.Ordinal).ToArray());
    }

    [Obsolete("Use FindSymbolsByQualifiedNameAsync, which names the lookup key explicitly.")]
    public ValueTask<CodeSymbolLookupResult> FindSymbolAsync(
        CodeRepositoryId repositoryId,
        string qualifiedName,
        CancellationToken cancellationToken = default) =>
        FindSymbolsByQualifiedNameAsync(repositoryId, qualifiedName, cancellationToken);

    /// <summary>
    /// Finds candidate symbols by name-pattern match for ranking by the caller.
    /// <paramref name="maxResults"/> cannot exceed
    /// <see cref="CodeNamePatternResult.AbsoluteMaxResults"/>.
    /// </summary>
    public async ValueTask<CodeNamePatternResult> FindNodesByNamePatternAsync(
        CodeRepositoryId repositoryId,
        string pattern,
        int maxResults = CodeNamePatternResult.DefaultMaxResults,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repositoryId);
        if (string.IsNullOrWhiteSpace(pattern))
            throw new ArgumentException("Search pattern is required.", nameof(pattern));
        return await _store.FindNodesByNamePatternAsync(
            repositoryId,
            pattern,
            maxResults,
            cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<CodeGraphQueryEnvelope<CodeSymbolLookupResult>?>
        FindSymbolsByQualifiedNameWithProvenanceAsync(
            CodeRepositoryId repositoryId,
            string qualifiedName,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repositoryId);
        if (string.IsNullOrWhiteSpace(qualifiedName))
            throw new ArgumentException("Qualified name is required.", nameof(qualifiedName));
        var envelope = await _store.FindNodesByQualifiedNameWithProvenanceAsync(
            repositoryId,
            qualifiedName,
            cancellationToken).ConfigureAwait(false);
        return envelope is null
            ? null
            : new(
                envelope.Publication,
                envelope.Query,
                new(envelope.Result),
                envelope.Provenance);
    }

    [Obsolete("Use FindSymbolsByQualifiedNameWithProvenanceAsync, which names the lookup key explicitly.")]
    public ValueTask<CodeGraphQueryEnvelope<CodeSymbolLookupResult>?>
        FindSymbolWithProvenanceAsync(
            CodeRepositoryId repositoryId,
            string qualifiedName,
            CancellationToken cancellationToken = default) =>
        FindSymbolsByQualifiedNameWithProvenanceAsync(repositoryId, qualifiedName, cancellationToken);

    public ValueTask<IReadOnlyList<CodeGraphDeclaration>> FindDeclarationsAsync(
        CodeRepositoryId repositoryId,
        CodeSymbolId symbolId,
        CancellationToken cancellationToken = default) =>
        _store.GetDeclarationsAsync(repositoryId, symbolId, cancellationToken);

    public ValueTask<CodeGraphQueryEnvelope<IReadOnlyList<CodeGraphDeclaration>>?>
        FindDeclarationsWithProvenanceAsync(
            CodeRepositoryId repositoryId,
            CodeSymbolId symbolId,
            CancellationToken cancellationToken = default) =>
        _store.GetDeclarationsWithProvenanceAsync(
            repositoryId,
            symbolId,
            cancellationToken);

    public ValueTask<CodeGraphTraversalResult> FindReferencesAsync(
        CodeRepositoryId repositoryId, CodeNodeId nodeId,
        CodeGraphQueryOptions? options = null, CancellationToken cancellationToken = default) =>
        TraverseAsync(repositoryId, nodeId, CodeGraphDirection.Incoming,
            CodeGraphTraversalPresets.References, options, cancellationToken);

    public ValueTask<CodeGraphTraversalResult> FindCallersAsync(
        CodeRepositoryId repositoryId, CodeNodeId nodeId,
        CodeGraphQueryOptions? options = null, CancellationToken cancellationToken = default) =>
        TraverseAsync(repositoryId, nodeId, CodeGraphDirection.Incoming,
            CodeGraphTraversalPresets.Calls, options, cancellationToken);

    public ValueTask<CodeGraphTraversalResult> FindCalleesAsync(
        CodeRepositoryId repositoryId, CodeNodeId nodeId,
        CodeGraphQueryOptions? options = null, CancellationToken cancellationToken = default) =>
        TraverseAsync(repositoryId, nodeId, CodeGraphDirection.Outgoing,
            CodeGraphTraversalPresets.Calls, options, cancellationToken);

    public ValueTask<CodeGraphTraversalResult> FindImplementationsAsync(
        CodeRepositoryId repositoryId, CodeNodeId nodeId,
        CodeGraphQueryOptions? options = null, CancellationToken cancellationToken = default) =>
        TraverseAsync(repositoryId, nodeId, CodeGraphDirection.Incoming,
            CodeGraphTraversalPresets.Implementations, options, cancellationToken);

    public ValueTask<CodeGraphTraversalResult> FindDependenciesAsync(
        CodeRepositoryId repositoryId, CodeNodeId nodeId,
        CodeGraphQueryOptions? options = null, CancellationToken cancellationToken = default) =>
        TraverseAsync(repositoryId, nodeId, CodeGraphDirection.Outgoing,
            CodeGraphTraversalPresets.Dependencies, options, cancellationToken);

    public ValueTask<CodeGraphTraversalResult> FindDependentsAsync(
        CodeRepositoryId repositoryId, CodeNodeId nodeId,
        CodeGraphQueryOptions? options = null, CancellationToken cancellationToken = default) =>
        TraverseAsync(repositoryId, nodeId, CodeGraphDirection.Incoming,
            CodeGraphTraversalPresets.Dependencies, options, cancellationToken);

    public ValueTask<CodeGraphTraversalResult> GetNeighborhoodAsync(
        CodeRepositoryId repositoryId, CodeNodeId nodeId,
        IReadOnlyCollection<CodeEdgeKind>? edgeKinds = null,
        CodeGraphQueryOptions? options = null, CancellationToken cancellationToken = default) =>
        TraverseAsync(repositoryId, nodeId, CodeGraphDirection.Both,
            edgeKinds ?? [], options, cancellationToken);

    public ValueTask<CodeGraphQueryEnvelope<CodeGraphTraversalResult>?>
        GetNeighborhoodWithProvenanceAsync(
            CodeRepositoryId repositoryId,
            CodeNodeId nodeId,
            IReadOnlyCollection<CodeEdgeKind>? edgeKinds = null,
            CodeGraphQueryOptions? options = null,
            CancellationToken cancellationToken = default) =>
        TraverseWithProvenanceAsync(
            repositoryId,
            nodeId,
            CodeGraphDirection.Both,
            edgeKinds ?? [],
            options,
            cancellationToken);

    /// <summary>
    /// Returns the incoming impact set: nodes that reference, call, implement,
    /// inherit, or depend on <paramref name="nodeId"/>. Outgoing edges are
    /// intentionally excluded; use neighborhood or dependency traversals for those.
    /// </summary>
    public ValueTask<CodeGraphTraversalResult> GetImpactSetAsync(
        CodeRepositoryId repositoryId, CodeNodeId nodeId,
        CodeGraphQueryOptions? options = null, CancellationToken cancellationToken = default) =>
        TraverseAsync(repositoryId, nodeId, CodeGraphDirection.Incoming,
            CodeGraphTraversalPresets.ImpactSet, options, cancellationToken);

    public ValueTask<CodeGraphQueryEnvelope<CodeGraphTraversalResult>?> FindReferencesWithProvenanceAsync(
        CodeRepositoryId repositoryId, CodeNodeId nodeId,
        CodeGraphQueryOptions? options = null, CancellationToken cancellationToken = default) =>
        TraverseWithProvenanceAsync(repositoryId, nodeId, CodeGraphDirection.Incoming,
            CodeGraphTraversalPresets.References, options, cancellationToken);

    public ValueTask<CodeGraphQueryEnvelope<CodeGraphTraversalResult>?> FindCallersWithProvenanceAsync(
        CodeRepositoryId repositoryId, CodeNodeId nodeId,
        CodeGraphQueryOptions? options = null, CancellationToken cancellationToken = default) =>
        TraverseWithProvenanceAsync(repositoryId, nodeId, CodeGraphDirection.Incoming,
            CodeGraphTraversalPresets.Calls, options, cancellationToken);

    public ValueTask<CodeGraphQueryEnvelope<CodeGraphTraversalResult>?> FindCalleesWithProvenanceAsync(
        CodeRepositoryId repositoryId, CodeNodeId nodeId,
        CodeGraphQueryOptions? options = null, CancellationToken cancellationToken = default) =>
        TraverseWithProvenanceAsync(repositoryId, nodeId, CodeGraphDirection.Outgoing,
            CodeGraphTraversalPresets.Calls, options, cancellationToken);

    public ValueTask<CodeGraphQueryEnvelope<CodeGraphTraversalResult>?> FindImplementationsWithProvenanceAsync(
        CodeRepositoryId repositoryId, CodeNodeId nodeId,
        CodeGraphQueryOptions? options = null, CancellationToken cancellationToken = default) =>
        TraverseWithProvenanceAsync(repositoryId, nodeId, CodeGraphDirection.Incoming,
            CodeGraphTraversalPresets.Implementations, options, cancellationToken);

    public ValueTask<CodeGraphQueryEnvelope<CodeGraphTraversalResult>?> FindDependenciesWithProvenanceAsync(
        CodeRepositoryId repositoryId, CodeNodeId nodeId,
        CodeGraphQueryOptions? options = null, CancellationToken cancellationToken = default) =>
        TraverseWithProvenanceAsync(repositoryId, nodeId, CodeGraphDirection.Outgoing,
            CodeGraphTraversalPresets.Dependencies, options, cancellationToken);

    public ValueTask<CodeGraphQueryEnvelope<CodeGraphTraversalResult>?> FindDependentsWithProvenanceAsync(
        CodeRepositoryId repositoryId, CodeNodeId nodeId,
        CodeGraphQueryOptions? options = null, CancellationToken cancellationToken = default) =>
        TraverseWithProvenanceAsync(repositoryId, nodeId, CodeGraphDirection.Incoming,
            CodeGraphTraversalPresets.Dependencies, options, cancellationToken);

    public ValueTask<CodeGraphQueryEnvelope<CodeGraphTraversalResult>?> GetImpactSetWithProvenanceAsync(
        CodeRepositoryId repositoryId, CodeNodeId nodeId,
        CodeGraphQueryOptions? options = null, CancellationToken cancellationToken = default) =>
        TraverseWithProvenanceAsync(repositoryId, nodeId, CodeGraphDirection.Incoming,
            CodeGraphTraversalPresets.ImpactSet, options, cancellationToken);

    public async ValueTask<CodeGraphMultiTraversalResult> GetImpactSetsAsync(
        CodeRepositoryId repositoryId,
        IReadOnlyCollection<CodeNodeId> seedNodeIds,
        CodeGraphQueryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var envelope = await GetImpactSetsWithProvenanceAsync(
            repositoryId,
            seedNodeIds,
            options,
            cancellationToken).ConfigureAwait(false);
        return envelope?.Result ?? new CodeGraphMultiTraversalResult(
            new Dictionary<string, CodeGraphTraversalResult>(
                StringComparer.Ordinal));
    }

    /// <summary>
    /// Resolves many qualified names in one call. Each name maps to its
    /// candidate list (possibly empty); ambiguous names retain all candidates.
    /// </summary>
    public async ValueTask<IReadOnlyDictionary<string, CodeSymbolLookupResult>> ResolveSymbolsAsync(
        CodeRepositoryId repositoryId,
        IReadOnlyCollection<string> qualifiedNames,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repositoryId);
        var names = NormalizeNames(qualifiedNames);
        if (names.Length > MaximumBatchSymbols)
        {
            throw new ArgumentOutOfRangeException(
                nameof(qualifiedNames),
                $"A symbol batch cannot exceed {MaximumBatchSymbols} distinct names.");
        }
        var results = new Dictionary<string, CodeSymbolLookupResult>(
            StringComparer.Ordinal);
        foreach (var name in names)
        {
            results[name] = await FindSymbolsByQualifiedNameAsync(
                repositoryId,
                name,
                cancellationToken).ConfigureAwait(false);
        }

        return results;
    }

    /// <summary>
    /// Resolves a bounded batch and returns one envelope only when every lookup
    /// observed the same successful publication.
    /// </summary>
    public async ValueTask<CodeGraphQueryEnvelope<
        IReadOnlyDictionary<string, CodeSymbolLookupResult>>?>
        ResolveSymbolsWithProvenanceAsync(
            CodeRepositoryId repositoryId,
            IReadOnlyCollection<string> qualifiedNames,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repositoryId);
        var names = NormalizeNames(qualifiedNames);
        if (names.Length > MaximumBatchSymbols)
        {
            throw new ArgumentOutOfRangeException(
                nameof(qualifiedNames),
                $"A symbol batch cannot exceed {MaximumBatchSymbols} distinct names.");
        }

        var publication = await _store.GetLatestPublicationAsync(
            repositoryId,
            cancellationToken).ConfigureAwait(false);
        if (publication is null)
            return null;
        var results = new Dictionary<string, CodeSymbolLookupResult>(
            StringComparer.Ordinal);
        var provenance = new List<CodeGraphFactProvenance>();
        foreach (var name in names)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var envelope = await FindSymbolsByQualifiedNameWithProvenanceAsync(
                repositoryId,
                name,
                cancellationToken).ConfigureAwait(false);
            EnsurePublication(publication, envelope?.Publication);
            results.Add(name, envelope!.Result);
            provenance.AddRange(envelope.Provenance);
        }

        return new(
            publication,
            new CodeGraphQueryDescriptor(
                CodeQueryOperations.ResolveSymbols,
                QualifiedNames: names),
            results,
            DistinctProvenance(provenance));
    }

    /// <summary>
    /// Returns every declaration whose physical location is inside the
    /// specified source file, ordered by position.
    /// </summary>
    public async ValueTask<IReadOnlyList<CodeGraphDeclaration>> GetDeclarationsInFileAsync(
        CodeRepositoryId repositoryId,
        string sourcePath,
        CodeGraphQueryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        var fileNodes = await _store.FindNodesByQualifiedNameAsync(
            repositoryId,
            sourcePath,
            cancellationToken).ConfigureAwait(false);
        var fileNode = fileNodes.FirstOrDefault(node =>
            node.Kind == CodeNodeKinds.File);
        if (fileNode is null)
            return [];

        var traversal = await TraverseAsync(
            repositoryId,
            fileNode.Id,
            CodeGraphDirection.Outgoing,
            CodeGraphTraversalPresets.Declarations,
            options ?? new(maxDepth: 1, maxNodes: 500, maxEdges: 1000),
            cancellationToken).ConfigureAwait(false);

        var symbolIds = traversal.Nodes
            .Where(node => node.SymbolId is not null)
            .Select(node => node.SymbolId!)
            .Distinct()
            .ToArray();

        var declarations = new List<CodeGraphDeclaration>();
        foreach (var symbolId in symbolIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var forSymbol = await _store.GetDeclarationsAsync(
                repositoryId,
                symbolId,
                cancellationToken).ConfigureAwait(false);
            declarations.AddRange(forSymbol.Where(declaration =>
                string.Equals(declaration.Location.Path, sourcePath, StringComparison.Ordinal)));
        }

        return declarations
            .OrderBy(declaration => declaration.Location.StartLine)
            .ThenBy(declaration => declaration.Location.StartColumn)
            .ToArray();
    }

    public async ValueTask<CodeGraphQueryEnvelope<
        IReadOnlyList<CodeGraphDeclaration>>?>
        GetDeclarationsInFileWithProvenanceAsync(
            CodeRepositoryId repositoryId,
            string sourcePath,
            CodeGraphQueryOptions? options = null,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repositoryId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        options ??= new(maxDepth: 1, maxNodes: 500, maxEdges: 1000);
        var publication = await _store.GetLatestPublicationAsync(
            repositoryId,
            cancellationToken).ConfigureAwait(false);
        if (publication is null)
            return null;
        var fileLookup = await _store.FindNodesByQualifiedNameWithProvenanceAsync(
            repositoryId,
            sourcePath,
            cancellationToken).ConfigureAwait(false);
        EnsurePublication(publication, fileLookup?.Publication);
        var fileNode = fileLookup!.Result.FirstOrDefault(node =>
            node.Kind == CodeNodeKinds.File);
        if (fileNode is null)
        {
            return new(
                publication,
                new CodeGraphQueryDescriptor(
                    CodeQueryOperations.DeclarationsInFile,
                    QualifiedName: sourcePath),
                [],
                []);
        }

        var traversalQuery = new CodeGraphTraversalQuery(
            fileNode.Id,
            CodeGraphDirection.Outgoing,
            CodeGraphTraversalPresets.Declarations,
            maxDepth: options.MaxDepth,
            maxNodes: options.MaxNodes,
            maxEdges: options.MaxEdges);
        var traversal = await _store.TraverseWithProvenanceAsync(
            repositoryId,
            traversalQuery,
            cancellationToken).ConfigureAwait(false);
        EnsurePublication(publication, traversal?.Publication);
        var declarations = new List<CodeGraphDeclaration>();
        var provenance = new List<CodeGraphFactProvenance>();
        foreach (var symbolId in traversal!.Result.Nodes
                     .Where(node => node.SymbolId is not null)
                     .Select(node => node.SymbolId!)
                     .Distinct())
        {
            var envelope = await _store.GetDeclarationsWithProvenanceAsync(
                repositoryId,
                symbolId,
                cancellationToken).ConfigureAwait(false);
            EnsurePublication(publication, envelope?.Publication);
            declarations.AddRange(envelope!.Result.Where(declaration =>
                string.Equals(
                    declaration.Location.Path,
                    sourcePath,
                    StringComparison.Ordinal)));
            provenance.AddRange(envelope.Provenance);
        }

        var ordered = declarations
            .OrderBy(declaration => declaration.Location.StartLine)
            .ThenBy(declaration => declaration.Location.StartColumn)
            .ToArray();
        var ids = ordered.Select(item => item.Id.Value).ToHashSet(
            StringComparer.Ordinal);
        return new(
            publication,
            new CodeGraphQueryDescriptor(
                CodeQueryOperations.DeclarationsInFile,
                QualifiedName: sourcePath,
                Traversal: traversalQuery),
            ordered,
            DistinctProvenance(provenance.Where(item =>
                item.Kind == CodeGraphFactKind.Declaration &&
                ids.Contains(item.FactId))));
    }

    /// <summary>
    /// Discovers symbols by name pattern, kind, language, container, or
    /// project without requiring an exact qualified name. Scoped queries
    /// traverse containment; unscoped queries require a name pattern and
    /// delegate to bounded name-pattern search. Results are deterministically
    /// ordered with explicit truncation; Solo ranks the candidates.
    /// </summary>
    public async ValueTask<CodeSymbolDiscoveryResult> FindSymbolsAsync(
        CodeRepositoryId repositoryId,
        CodeSymbolDiscoveryQuery query,
        CodeGraphQueryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repositoryId);
        ArgumentNullException.ThrowIfNull(query);

        if (query.ContainerId is not null || query.ProjectPath is not null)
            return await FindSymbolsInScopeAsync(repositoryId, query, options, cancellationToken)
                .ConfigureAwait(false);

        var matches = await _store.FindNodesByNamePatternAsync(
            repositoryId,
            query.NamePattern!,
            query.MaxResults,
            cancellationToken).ConfigureAwait(false);
        var symbols = matches.Candidates
            .Where(node => MatchesDiscoveryKind(node, query.NodeKinds, symbolBearingOnly: true))
            .Where(node => MatchesLanguage(node, query.Languages))
            .OrderBy(node => node.QualifiedName is null)
            .ThenBy(node => node.QualifiedName, StringComparer.Ordinal)
            .ThenBy(node => node.Id.Value, StringComparer.Ordinal)
            .ToArray();
        return new(
            symbols.Take(query.MaxResults).ToArray(),
            matches.Truncated || symbols.Length > query.MaxResults);
    }

    /// <summary>
    /// Returns the symbol whose declaration most tightly encloses a source
    /// position, bridging diffs and diagnostics into graph identities.
    /// Returns null when no declaration contains the position.
    /// </summary>
    public async ValueTask<CodeEnclosingSymbolResult?> FindEnclosingSymbolAsync(
        CodeRepositoryId repositoryId,
        string sourcePath,
        int line,
        int column,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repositoryId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        if (line < 1)
            throw new ArgumentOutOfRangeException(nameof(line));
        if (column < 1)
            throw new ArgumentOutOfRangeException(nameof(column));

        var declarations = await GetDeclarationsInFileAsync(
            repositoryId,
            sourcePath,
            null,
            cancellationToken).ConfigureAwait(false);
        var enclosing = declarations
            .Where(declaration => ContainsPosition(declaration.Location, line, column))
            .OrderBy(declaration => SpanSize(declaration.Location))
            .ThenByDescending(declaration => declaration.Location.StartLine)
            .ThenByDescending(declaration => declaration.Location.StartColumn)
            .ThenBy(declaration => declaration.Id.Value, StringComparer.Ordinal)
            .FirstOrDefault();
        if (enclosing is null)
            return null;

        var symbol = await _store.FindSymbolAsync(
            repositoryId,
            enclosing.SymbolId,
            cancellationToken).ConfigureAwait(false);
        return symbol is null ? null : new(symbol, enclosing);
    }

    /// <summary>
    /// Returns the symbol-bearing nodes declared in one file, ordered by
    /// qualified name. Complements declarations-in-file with the symbols a
    /// file contributes to discovery.
    /// </summary>
    public async ValueTask<IReadOnlyList<CodeGraphNode>> GetSymbolsInFileAsync(
        CodeRepositoryId repositoryId,
        string sourcePath,
        CodeGraphQueryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repositoryId);
        var fileNode = await FindFileNodeAsync(repositoryId, sourcePath, cancellationToken)
            .ConfigureAwait(false);
        if (fileNode is null)
            return [];

        var traversal = await TraverseAsync(
            repositoryId,
            fileNode.Id,
            CodeGraphDirection.Outgoing,
            CodeGraphTraversalPresets.Declarations,
            options ?? new(maxDepth: 1, maxNodes: 500, maxEdges: 1000),
            cancellationToken).ConfigureAwait(false);
        return traversal.Nodes
            .Where(node => node.SymbolId is not null)
            .OrderBy(node => node.QualifiedName is null)
            .ThenBy(node => node.QualifiedName, StringComparer.Ordinal)
            .ThenBy(node => node.Id.Value, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Returns the symbol-bearing nodes transitively contained in one
    /// project, ordered by qualified name and bounded by the query options.
    /// </summary>
    public async ValueTask<IReadOnlyList<CodeGraphNode>> GetSymbolsInProjectAsync(
        CodeRepositoryId repositoryId,
        string projectPath,
        CodeGraphQueryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repositoryId);
        var projectNode = await FindProjectNodeAsync(repositoryId, projectPath, cancellationToken)
            .ConfigureAwait(false);
        if (projectNode is null)
            return [];

        var traversal = await TraverseAsync(
            repositoryId,
            projectNode.Id,
            CodeGraphDirection.Outgoing,
            [CodeEdgeKinds.Contains, CodeEdgeKinds.Declares],
            options ?? new(maxDepth: 3, maxNodes: 2000, maxEdges: 4000),
            cancellationToken).ConfigureAwait(false);
        return traversal.Nodes
            .Where(node => node.Id != projectNode.Id && node.SymbolId is not null)
            .OrderBy(node => node.QualifiedName is null)
            .ThenBy(node => node.QualifiedName, StringComparer.Ordinal)
            .ThenBy(node => node.Id.Value, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Returns every declaration in one project ordered by path and position,
    /// bounded by the query options.
    /// </summary>
    public async ValueTask<IReadOnlyList<CodeGraphDeclaration>> GetDeclarationsInProjectAsync(
        CodeRepositoryId repositoryId,
        string projectPath,
        CodeGraphQueryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var symbols = await GetSymbolsInProjectAsync(repositoryId, projectPath, options, cancellationToken)
            .ConfigureAwait(false);
        var declarations = new List<CodeGraphDeclaration>();
        foreach (var symbolId in symbols
                     .Where(node => node.SymbolId is not null)
                     .Select(node => node.SymbolId!)
                     .Distinct())
        {
            cancellationToken.ThrowIfCancellationRequested();
            declarations.AddRange(await _store.GetDeclarationsAsync(
                repositoryId,
                symbolId,
                cancellationToken).ConfigureAwait(false));
        }

        return declarations
            .OrderBy(declaration => declaration.Location.Path, StringComparer.Ordinal)
            .ThenBy(declaration => declaration.Location.StartLine)
            .ThenBy(declaration => declaration.Location.StartColumn)
            .ToArray();
    }

    private async ValueTask<CodeSymbolDiscoveryResult> FindSymbolsInScopeAsync(
        CodeRepositoryId repositoryId,
        CodeSymbolDiscoveryQuery query,
        CodeGraphQueryOptions? options,
        CancellationToken cancellationToken)
    {
        CodeNodeId container;
        if (query.ContainerId is not null)
        {
            container = query.ContainerId;
        }
        else
        {
            var projectNode = await FindProjectNodeAsync(
                repositoryId,
                query.ProjectPath!,
                cancellationToken).ConfigureAwait(false);
            if (projectNode is null)
                return new([], false);
            container = projectNode.Id;
        }

        options ??= new(maxDepth: 5, maxNodes: 1000, maxEdges: 2000);
        var traversal = await _store.TraverseAsync(
            repositoryId,
            new CodeGraphTraversalQuery(
                container,
                CodeGraphDirection.Outgoing,
                [CodeEdgeKinds.Contains, CodeEdgeKinds.Declares],
                options.EvidenceQualities,
                options.MaxDepth,
                options.MaxNodes,
                options.MaxEdges),
            cancellationToken).ConfigureAwait(false);
        var symbols = traversal.Nodes
            .Where(node => node.Id != container)
            .Where(node => MatchesDiscoveryKind(node, query.NodeKinds, symbolBearingOnly: true))
            .Where(node => MatchesLanguage(node, query.Languages))
            .Where(node => query.NamePattern is null ||
                (node.QualifiedName is not null &&
                    node.QualifiedName.Contains(query.NamePattern, StringComparison.OrdinalIgnoreCase)) ||
                node.Name.Contains(query.NamePattern, StringComparison.OrdinalIgnoreCase))
            .OrderBy(node => node.QualifiedName is null)
            .ThenBy(node => node.QualifiedName, StringComparer.Ordinal)
            .ThenBy(node => node.Id.Value, StringComparer.Ordinal)
            .ToArray();
        return new(
            symbols.Take(query.MaxResults).ToArray(),
            traversal.Truncated || symbols.Length > query.MaxResults);
    }

    private static bool MatchesDiscoveryKind(
        CodeGraphNode node,
        IReadOnlyCollection<CodeNodeKind> kinds,
        bool symbolBearingOnly) =>
        kinds.Count > 0
            ? kinds.Contains(node.Kind)
            : !symbolBearingOnly || node.SymbolId is not null;

    private static bool MatchesLanguage(CodeGraphNode node, IReadOnlyCollection<string> languages)
    {
        if (languages.Count == 0)
            return true;
        return node.Properties.TryGetValue(CodePropertyKeys.Language, out var value) &&
            value is CodeTextProperty text &&
            languages.Contains(text.Value);
    }

    private async ValueTask<CodeGraphNode?> FindFileNodeAsync(
        CodeRepositoryId repositoryId,
        string sourcePath,
        CancellationToken cancellationToken)
    {
        var fileNodes = await _store.FindNodesByQualifiedNameAsync(
            repositoryId,
            sourcePath,
            cancellationToken).ConfigureAwait(false);
        return fileNodes.FirstOrDefault(node => node.Kind == CodeNodeKinds.File);
    }

    private async ValueTask<CodeGraphNode?> FindProjectNodeAsync(
        CodeRepositoryId repositoryId,
        string projectPath,
        CancellationToken cancellationToken)
    {
        var projectNodes = await _store.FindNodesByQualifiedNameAsync(
            repositoryId,
            projectPath,
            cancellationToken).ConfigureAwait(false);
        return projectNodes.FirstOrDefault(node => node.Kind == CodeNodeKinds.Project);
    }

    private static bool ContainsPosition(CodeLocation location, int line, int column) =>
        (line > location.StartLine || (line == location.StartLine && column >= location.StartColumn)) &&
        (line < location.EndLine || (line == location.EndLine && column < location.EndColumn));

    private static long SpanSize(CodeLocation location) =>
        ((long)(location.EndLine - location.StartLine) * 1_000_000L) +
        (location.EndColumn - location.StartColumn);

    /// <summary>
    /// Returns all nodes with public accessibility that are transitively
    /// contained in the specified project, bounded by the query options.
    /// </summary>
    public async ValueTask<IReadOnlyList<CodeGraphNode>> GetPublicSurfaceAsync(
        CodeRepositoryId repositoryId,
        string projectPath,
        CodeGraphQueryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);
        var projectNodes = await _store.FindNodesByQualifiedNameAsync(
            repositoryId,
            projectPath,
            cancellationToken).ConfigureAwait(false);
        var projectNode = projectNodes.FirstOrDefault(node =>
            node.Kind == CodeNodeKinds.Project);
        if (projectNode is null)
            return [];

        options ??= new(maxDepth: 10, maxNodes: 2000, maxEdges: 5000);
        var traversal = await TraverseAsync(
            repositoryId,
            projectNode.Id,
            CodeGraphDirection.Outgoing,
            CodeGraphTraversalPresets.Containment,
            options,
            cancellationToken).ConfigureAwait(false);

        return SelectPublicSurface(traversal.Nodes);
    }

    public async ValueTask<CodeGraphQueryEnvelope<IReadOnlyList<CodeGraphNode>>?>
        GetPublicSurfaceWithProvenanceAsync(
            CodeRepositoryId repositoryId,
            string projectPath,
            CodeGraphQueryOptions? options = null,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repositoryId);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);
        var publication = await _store.GetLatestPublicationAsync(
            repositoryId,
            cancellationToken).ConfigureAwait(false);
        if (publication is null)
            return null;
        var projectLookup = await _store.FindNodesByQualifiedNameWithProvenanceAsync(
            repositoryId,
            projectPath,
            cancellationToken).ConfigureAwait(false);
        EnsurePublication(publication, projectLookup?.Publication);
        var projectNode = projectLookup!.Result.FirstOrDefault(node =>
            node.Kind == CodeNodeKinds.Project);
        if (projectNode is null)
        {
            return new(
                publication,
                new CodeGraphQueryDescriptor(
                    CodeQueryOperations.PublicSurface,
                    QualifiedName: projectPath),
                [],
                []);
        }

        options ??= new(maxDepth: 10, maxNodes: 2000, maxEdges: 5000);
        var traversalQuery = new CodeGraphTraversalQuery(
            projectNode.Id,
            CodeGraphDirection.Outgoing,
            CodeGraphTraversalPresets.Containment,
            options.EvidenceQualities,
            options.MaxDepth,
            options.MaxNodes,
            options.MaxEdges);
        var traversal = await _store.TraverseWithProvenanceAsync(
            repositoryId,
            traversalQuery,
            cancellationToken).ConfigureAwait(false);
        EnsurePublication(publication, traversal?.Publication);
        var result = SelectPublicSurface(traversal!.Result.Nodes);
        var ids = result.Select(node => node.Id.Value).ToHashSet(
            StringComparer.Ordinal);
        return new(
            publication,
            new CodeGraphQueryDescriptor(
                CodeQueryOperations.PublicSurface,
                QualifiedName: projectPath,
                Traversal: traversalQuery),
            result,
            DistinctProvenance(traversal.Provenance.Where(item =>
                item.Kind == CodeGraphFactKind.Node &&
                ids.Contains(item.FactId))));
    }

    public async ValueTask<CodeGraphQueryEnvelope<CodeGraphMultiTraversalResult>?>
        GetImpactSetsWithProvenanceAsync(
            CodeRepositoryId repositoryId,
            IReadOnlyCollection<CodeNodeId> seedNodeIds,
            CodeGraphQueryOptions? options = null,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repositoryId);
        ArgumentNullException.ThrowIfNull(seedNodeIds);
        var seeds = seedNodeIds
            .Where(seed => seed is not null)
            .Distinct()
            .OrderBy(seed => seed.Value, StringComparer.Ordinal)
            .ToArray();
        if (seeds.Length > MaximumTraversalSeeds)
        {
            throw new ArgumentOutOfRangeException(
                nameof(seedNodeIds),
                $"An impact batch cannot exceed {MaximumTraversalSeeds} distinct seeds.");
        }

        var publication = await _store.GetLatestPublicationAsync(
            repositoryId,
            cancellationToken).ConfigureAwait(false);
        if (publication is null)
            return null;
        options ??= new();
        var queries = seeds.Select(seed => new CodeGraphTraversalQuery(
            seed,
            CodeGraphDirection.Incoming,
            CodeGraphTraversalPresets.ImpactSet,
            options.EvidenceQualities,
            options.MaxDepth,
            options.MaxNodes,
            options.MaxEdges)).ToArray();
        var results = new Dictionary<string, CodeGraphTraversalResult>(
            StringComparer.Ordinal);
        var provenance = new List<CodeGraphFactProvenance>();
        foreach (var query in queries)
        {
            var envelope = await _store.TraverseWithProvenanceAsync(
                repositoryId,
                query,
                cancellationToken).ConfigureAwait(false);
            EnsurePublication(publication, envelope?.Publication);
            results.Add(query.StartNodeId.Value, envelope!.Result);
            provenance.AddRange(envelope.Provenance);
        }

        return new(
            publication,
            new CodeGraphQueryDescriptor(
                CodeQueryOperations.ImpactSets,
                Traversals: queries),
            new CodeGraphMultiTraversalResult(results),
            DistinctProvenance(provenance));
    }

    /// <summary>
    /// Returns the test entry points that reach each seed through incoming
    /// calls or references. Each seed keeps its own traversal bounds; when a
    /// traversal truncates, tests for that seed may be partial.
    /// </summary>
    public async ValueTask<CodeAffectedTestsResult?> GetAffectedTestsAsync(
        CodeRepositoryId repositoryId,
        IReadOnlyCollection<CodeNodeId> seedNodeIds,
        CodeGraphQueryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var envelope = await GetAffectedTestsWithProvenanceAsync(
            repositoryId,
            seedNodeIds,
            options,
            cancellationToken).ConfigureAwait(false);
        return envelope?.Result;
    }

    public async ValueTask<CodeGraphQueryEnvelope<CodeAffectedTestsResult>?>
        GetAffectedTestsWithProvenanceAsync(
            CodeRepositoryId repositoryId,
            IReadOnlyCollection<CodeNodeId> seedNodeIds,
            CodeGraphQueryOptions? options = null,
            CancellationToken cancellationToken = default)
    {
        var envelope = await GetImpactSetsWithProvenanceAsync(
            repositoryId,
            seedNodeIds,
            options,
            cancellationToken).ConfigureAwait(false);
        if (envelope is null)
            return null;
        var tests = new Dictionary<string, IReadOnlyList<CodeGraphNode>>(
            StringComparer.Ordinal);
        foreach (var (seed, result) in envelope.Result.Results)
            tests[seed] = result.Nodes.Where(IsTestMethod).ToArray();
        return new(
            envelope.Publication,
            new CodeGraphQueryDescriptor(
                CodeQueryOperations.AffectedTests,
                Traversals: envelope.Query.Traversals),
            new CodeAffectedTestsResult(tests, envelope.Result.Truncated),
            envelope.Provenance);
    }

    internal static bool IsTestMethod(CodeGraphNode node) =>
        node.Properties.TryGetValue(CodePropertyKeys.TestMethod, out var value) &&
        value is CodeBooleanProperty { Value: true };

    private ValueTask<CodeGraphTraversalResult> TraverseAsync(
        CodeRepositoryId repositoryId,
        CodeNodeId nodeId,
        CodeGraphDirection direction,
        IReadOnlyCollection<CodeEdgeKind> edgeKinds,
        CodeGraphQueryOptions? options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(repositoryId);
        ArgumentNullException.ThrowIfNull(nodeId);
        options ??= new();
        return _store.TraverseAsync(
            repositoryId,
            new CodeGraphTraversalQuery(
                nodeId,
                direction,
                edgeKinds,
                options.EvidenceQualities,
                options.MaxDepth,
                options.MaxNodes,
                options.MaxEdges),
            cancellationToken);
    }

    public ValueTask<CodeGraphQueryEnvelope<CodeGraphTraversalResult>?>
        TraverseWithProvenanceAsync(
            CodeRepositoryId repositoryId,
            CodeNodeId nodeId,
            CodeGraphDirection direction,
            IReadOnlyCollection<CodeEdgeKind> edgeKinds,
            CodeGraphQueryOptions? options,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(repositoryId);
        ArgumentNullException.ThrowIfNull(nodeId);
        options ??= new();
        return _store.TraverseWithProvenanceAsync(
            repositoryId,
            new(
                nodeId,
                direction,
                edgeKinds,
                options.EvidenceQualities,
                options.MaxDepth,
                options.MaxNodes,
                options.MaxEdges),
            cancellationToken);
    }

    private static IReadOnlyList<CodeGraphNode> SelectPublicSurface(
        IEnumerable<CodeGraphNode> nodes) =>
        nodes
            .Where(node =>
                node.Kind != CodeNodeKinds.Project &&
                node.Kind != CodeNodeKinds.File)
            .Where(node =>
                 node.Properties.TryGetValue(CodePropertyKeys.Access, out var access) &&
                access is CodeTextProperty { Value: "public" })
            .OrderBy(
                node => node.QualifiedName ?? node.Name,
                StringComparer.Ordinal)
            .ToArray();

    private static string[] NormalizeNames(
        IReadOnlyCollection<string> qualifiedNames)
    {
        ArgumentNullException.ThrowIfNull(qualifiedNames);
        return qualifiedNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    internal static void EnsurePublication(
        CodeGraphPublication expected,
        CodeGraphPublication? actual)
    {
        ArgumentNullException.ThrowIfNull(expected);
        if (actual != expected)
            throw new CodeGraphPublicationChangedException(expected, actual);
    }

    private static IReadOnlyList<CodeGraphFactProvenance> DistinctProvenance(
        IEnumerable<CodeGraphFactProvenance> provenance) =>
        provenance
            .GroupBy(item => (item.Kind, item.FactId))
            .Select(group => new CodeGraphFactProvenance(
                group.Key.Kind,
                group.Key.FactId,
                group.SelectMany(item => item.Contributors)
                    .Distinct()
                    .OrderBy(origin => origin.PluginId.Value, StringComparer.Ordinal)
                    .ThenBy(origin => origin.PluginVersion, StringComparer.Ordinal)
                    .ThenBy(origin => origin.IndexUnitId.Value, StringComparer.Ordinal)
                    .ToArray()))
            .OrderBy(item => item.Kind)
            .ThenBy(item => item.FactId, StringComparer.Ordinal)
            .ToArray();
}
