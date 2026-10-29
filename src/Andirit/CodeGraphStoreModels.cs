namespace Andirit;

/// <summary>Describes one repository known to a graph store.</summary>
public sealed record CodeRepositoryManifest
{
    public CodeRepositoryManifest(
        CodeRepositoryId id,
        string? displayName = null,
        string? sourceUri = null,
        DateTimeOffset? registeredAt = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        DisplayName = string.IsNullOrWhiteSpace(displayName)
            ? null
            : displayName;
        SourceUri = string.IsNullOrWhiteSpace(sourceUri)
            ? null
            : sourceUri;
        RegisteredAt = registeredAt ?? DateTimeOffset.UtcNow;
    }

    public CodeRepositoryId Id { get; }
    public string? DisplayName { get; }
    public string? SourceUri { get; }
    public DateTimeOffset RegisteredAt { get; }
}

public enum CodeIndexRunStatus
{
    Running = 0,
    Completed = 1,
    Failed = 2,
    Cancelled = 3
}

/// <summary>
/// Records one indexing attempt without retaining source content.
/// <see cref="CodeIndexRunManifest.Plugins"/> names the plugins eligible to
/// contribute staged facts for the run (executing plugins plus their
/// configured fallback candidates, which stores require upfront). It must
/// not be read as the executed set: actual execution comes from indexing
/// diagnostics, provider-selection events, and published unit origins.
/// </summary>
public sealed record CodeIndexRunManifest
{
    public CodeIndexRunManifest(
        CodeRepositoryId repositoryId,
        CodeIndexRunId id,
        DateTimeOffset startedAt,
        CodeIndexRunStatus status = CodeIndexRunStatus.Running,
        DateTimeOffset? completedAt = null,
        IReadOnlyCollection<CodePluginId>? plugins = null)
    {
        RepositoryId = repositoryId ??
            throw new ArgumentNullException(nameof(repositoryId));
        Id = id ?? throw new ArgumentNullException(nameof(id));
        if (!Enum.IsDefined(status))
            throw new ArgumentOutOfRangeException(nameof(status));
        if (completedAt < startedAt)
            throw new ArgumentOutOfRangeException(nameof(completedAt));
        if (status == CodeIndexRunStatus.Running && completedAt is not null)
        {
            throw new ArgumentException(
                "A running index manifest cannot have a completion time.",
                nameof(completedAt));
        }

        StartedAt = startedAt;
        Status = status;
        CompletedAt = completedAt;
        Plugins = plugins?.Distinct().ToArray() ?? [];
    }

    public CodeRepositoryId RepositoryId { get; }
    public CodeIndexRunId Id { get; }
    public DateTimeOffset StartedAt { get; }
    public CodeIndexRunStatus Status { get; }
    public DateTimeOffset? CompletedAt { get; }
    public IReadOnlyCollection<CodePluginId> Plugins { get; }
}

/// <summary>
/// Records the source inputs of the latest successfully completed repository index.
/// Index-unit ownership remains plugin-defined and is not inferred from these files.
/// </summary>
public sealed record CodeRepositoryIndexState
{
    public CodeRepositoryIndexState(
        CodeRepositoryId repositoryId,
        CodeIndexRunId indexRunId,
        IReadOnlyList<CodeSourceManifest> sources,
        string? snapshotIdentity = null,
        bool isConsistentSnapshot = false,
        CodeIndexIdentity? indexIdentity = null)
    {
        RepositoryId = repositoryId ?? throw new ArgumentNullException(nameof(repositoryId));
        IndexRunId = indexRunId ?? throw new ArgumentNullException(nameof(indexRunId));
        ArgumentNullException.ThrowIfNull(sources);
        if (sources.Any(source => source is null))
            throw new ArgumentException("Index state cannot contain null sources.", nameof(sources));

        Sources = sources
            .OrderBy(source => source.PluginId.Value, StringComparer.Ordinal)
            .ThenBy(source => source.SourcePath, StringComparer.Ordinal)
            .ToArray();
        if (Sources
            .GroupBy(source => new PluginSourceKey(source.PluginId, source.SourcePath))
            .Any(group => group.Count() > 1))
        {
            throw new ArgumentException(
                "Index state cannot contain duplicate plugin and source-path pairs.",
                nameof(sources));
        }

        SnapshotIdentity = string.IsNullOrWhiteSpace(snapshotIdentity)
            ? null
            : snapshotIdentity;
        IsConsistentSnapshot = isConsistentSnapshot;
        var expectedIndexIdentity = CodeIndexIdentity.Create(
            repositoryId,
            Sources,
            SnapshotIdentity,
            IsConsistentSnapshot);
        if (indexIdentity is not null && indexIdentity != expectedIndexIdentity)
        {
            throw new ArgumentException(
                "The supplied index identity does not match the source state.",
                nameof(indexIdentity));
        }

        IndexIdentity = expectedIndexIdentity;
    }

    public CodeRepositoryId RepositoryId { get; }
    public CodeIndexRunId IndexRunId { get; }
    public IReadOnlyList<CodeSourceManifest> Sources { get; }
    public string? SnapshotIdentity { get; }
    public bool IsConsistentSnapshot { get; }
    public CodeIndexIdentity IndexIdentity { get; }
}

/// <summary>The complete new contribution of one atomically replaced index unit.</summary>
public sealed record CodeIndexUnitReplacement
{
    public CodeIndexUnitReplacement(
        CodeFactOrigin origin,
        IReadOnlyList<CodeGraphNode>? nodes = null,
        IReadOnlyList<CodeGraphDeclaration>? declarations = null,
        IReadOnlyList<CodeGraphEdge>? edges = null)
    {
        Origin = origin ?? throw new ArgumentNullException(nameof(origin));
        Nodes = nodes?.ToArray() ?? [];
        Declarations = declarations?.ToArray() ?? [];
        Edges = edges?.ToArray() ?? [];
        if (Nodes.Any(node => node is null) ||
            Declarations.Any(declaration => declaration is null) ||
            Edges.Any(edge => edge is null))
        {
            throw new ArgumentException(
                "Index-unit replacements cannot contain null facts.");
        }
    }

    public CodeFactOrigin Origin { get; }
    public IReadOnlyList<CodeGraphNode> Nodes { get; }
    public IReadOnlyList<CodeGraphDeclaration> Declarations { get; }
    public IReadOnlyList<CodeGraphEdge> Edges { get; }
}

public enum CodeGraphDirection
{
    Outgoing = 0,
    Incoming = 1,
    Both = 2
}

/// <summary>Bounds one deterministic graph traversal.</summary>
public sealed record CodeGraphTraversalQuery
{
    public CodeGraphTraversalQuery(
        CodeNodeId startNodeId,
        CodeGraphDirection direction = CodeGraphDirection.Outgoing,
        IReadOnlyCollection<CodeEdgeKind>? edgeKinds = null,
        IReadOnlyCollection<EvidenceQuality>? evidenceQualities = null,
        int maxDepth = 1,
        int maxNodes = 100,
        int maxEdges = 250)
    {
        StartNodeId = startNodeId ??
            throw new ArgumentNullException(nameof(startNodeId));
        if (!Enum.IsDefined(direction))
            throw new ArgumentOutOfRangeException(nameof(direction));
        if (maxDepth < 0)
            throw new ArgumentOutOfRangeException(nameof(maxDepth));
        if (maxNodes < 1)
            throw new ArgumentOutOfRangeException(nameof(maxNodes));
        if (maxEdges < 1)
            throw new ArgumentOutOfRangeException(nameof(maxEdges));

        Direction = direction;
        EdgeKinds = edgeKinds?.Distinct().ToArray() ?? [];
        EvidenceQualities = evidenceQualities?.Distinct().ToArray() ?? [];
        MaxDepth = maxDepth;
        MaxNodes = maxNodes;
        MaxEdges = maxEdges;
    }

    public CodeNodeId StartNodeId { get; }
    public CodeGraphDirection Direction { get; }
    public IReadOnlyCollection<CodeEdgeKind> EdgeKinds { get; }
    public IReadOnlyCollection<EvidenceQuality> EvidenceQualities { get; }
    public int MaxDepth { get; }
    public int MaxNodes { get; }
    public int MaxEdges { get; }
}

[Flags]
public enum CodeGraphTruncationReason
{
    None = 0,
    MaxDepth = 1,
    MaxNodes = 2,
    MaxEdges = 4
}

/// <summary>A bounded, deterministically ordered name-pattern match.</summary>
/// <remarks>Matching is a case-insensitive ordinal substring search over qualified
/// and display names. A future native full-text index may accelerate the scan
/// without changing this contract.</remarks>
public sealed record CodeNamePatternResult
{
    public const int DefaultMaxResults = 50;
    public const int AbsoluteMaxResults = 200;

    public CodeNamePatternResult(
        IReadOnlyList<CodeGraphNode> candidates,
        int totalMatches)
    {
        Candidates = candidates ?? throw new ArgumentNullException(nameof(candidates));
        if (totalMatches < 0)
            throw new ArgumentOutOfRangeException(nameof(totalMatches));
        if (candidates.Count > totalMatches)
            throw new ArgumentException(
                "Candidates cannot exceed the total match count.",
                nameof(candidates));
        TotalMatches = totalMatches;
    }

    public IReadOnlyList<CodeGraphNode> Candidates { get; }
    public int TotalMatches { get; }
    public bool Truncated => TotalMatches > Candidates.Count;
}

/// <summary>A bounded, deterministically ordered graph traversal result.</summary>
public sealed record CodeGraphTraversalResult
{
    public CodeGraphTraversalResult(
        IReadOnlyList<CodeGraphNode> nodes,
        IReadOnlyList<CodeGraphEdge> edges,
        bool truncated,
        CodeGraphTruncationReason truncationReason = CodeGraphTruncationReason.None,
        int depthReached = 0,
        int nodesExamined = 0,
        int edgesExamined = 0,
        int omittedFrontierCount = 0)
    {
        Nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
        Edges = edges ?? throw new ArgumentNullException(nameof(edges));
        if (depthReached < 0 || nodesExamined < 0 || edgesExamined < 0 || omittedFrontierCount < 0)
            throw new ArgumentOutOfRangeException(nameof(depthReached));
        Truncated = truncated;
        TruncationReason = truncated ? truncationReason : CodeGraphTruncationReason.None;
        DepthReached = depthReached;
        NodesExamined = nodesExamined;
        EdgesExamined = edgesExamined;
        OmittedFrontierCount = omittedFrontierCount;
    }

    public IReadOnlyList<CodeGraphNode> Nodes { get; }
    public IReadOnlyList<CodeGraphEdge> Edges { get; }
    public bool Truncated { get; }
    public CodeGraphTruncationReason TruncationReason { get; }
    public int DepthReached { get; }
    public int NodesExamined { get; }
    public int EdgesExamined { get; }
    public int OmittedFrontierCount { get; }
}

/// <summary>Identifies the successful repository publication observed by a query.</summary>
public sealed record CodeGraphPublication
{
    public CodeGraphPublication(
        CodeRepositoryId repositoryId,
        CodeIndexRunId indexRunId,
        string? snapshotIdentity,
        bool isConsistentSnapshot,
        CodeIndexIdentity indexIdentity)
    {
        RepositoryId = repositoryId ?? throw new ArgumentNullException(nameof(repositoryId));
        IndexRunId = indexRunId ?? throw new ArgumentNullException(nameof(indexRunId));
        SnapshotIdentity = string.IsNullOrWhiteSpace(snapshotIdentity)
            ? null
            : snapshotIdentity;
        IsConsistentSnapshot = isConsistentSnapshot;
        IndexIdentity = indexIdentity ?? throw new ArgumentNullException(nameof(indexIdentity));
    }

    public CodeRepositoryId RepositoryId { get; }
    public CodeIndexRunId IndexRunId { get; }
    public string? SnapshotIdentity { get; }
    public bool IsConsistentSnapshot { get; }
    public CodeIndexIdentity IndexIdentity { get; }
}

public enum CodeGraphFactKind
{
    Node = 0,
    Declaration = 1,
    Edge = 2
}

/// <summary>Traces one returned fact to every index unit that contributed it.</summary>
public sealed record CodeGraphFactProvenance(
    CodeGraphFactKind Kind,
    string FactId,
    IReadOnlyList<CodeFactOrigin> Contributors);

/// <summary>Binds a query result and its contributors to one atomic publication.</summary>
public sealed record CodeGraphQueryDescriptor(
    string Operation,
    string? QualifiedName = null,
    CodeGraphTraversalQuery? Traversal = null,
    IReadOnlyList<string>? QualifiedNames = null,
    IReadOnlyList<CodeGraphTraversalQuery>? Traversals = null);

/// <summary>Binds a query result and its contributors to one atomic publication.</summary>
public sealed record CodeGraphQueryEnvelope<TResult>(
    CodeGraphPublication Publication,
    CodeGraphQueryDescriptor Query,
    TResult Result,
    IReadOnlyList<CodeGraphFactProvenance> Provenance);

/// <summary>Results of independently bounded traversals keyed by seed node identity.</summary>
public sealed record CodeGraphMultiTraversalResult(
    IReadOnlyDictionary<string, CodeGraphTraversalResult> Results)
{
    public bool Truncated => Results.Values.Any(result => result.Truncated);
}

/// <summary>
/// Test entry points reaching each seed, keyed by seed node identity. A node
/// counts as a test only when it carries an explicit test marker property;
/// test-ness is never inferred from names or locations.
/// </summary>
public sealed record CodeAffectedTestsResult(
    IReadOnlyDictionary<string, IReadOnlyList<CodeGraphNode>> TestsBySeed,
    bool Truncated);

/// <summary>Privacy-safe counters from one completed index-unit ingestion.</summary>
public sealed record CodeGraphIngestionDiagnostics(
    CodeRepositoryId RepositoryId,
    CodeIndexRunId IndexRunId,
    CodeIndexUnitId IndexUnitId,
    int BatchesReceived,
    int NodesReceived,
    int DeclarationsReceived,
    int EdgesReceived,
    int RejectedFacts,
    IReadOnlyList<string> WarningCodes);

/// <summary>
/// Deterministic symbol discovery request. Scoping dimensions compose:
/// <list type="bullet">
/// <item>A container (or project) scopes discovery to its transitive
/// containment instead of scanning names.</item>
/// <item>A name pattern delegates to bounded name-pattern search.</item>
/// <item>Kind and language filters apply after scoping, never as a scan.
/// </item>
/// </list>
/// At least one of <see cref="CodeSymbolDiscoveryQuery.NamePattern"/>,
/// <see cref="CodeSymbolDiscoveryQuery.ContainerId"/>, or
/// <see cref="CodeSymbolDiscoveryQuery.ProjectPath"/> is required; container
/// and project must not combine. Solo ranks the returned candidates.
/// </summary>
public sealed record CodeSymbolDiscoveryQuery
{
    public CodeSymbolDiscoveryQuery(
        string? namePattern = null,
        IReadOnlyCollection<CodeNodeKind>? nodeKinds = null,
        IReadOnlyCollection<string>? languages = null,
        CodeNodeId? containerId = null,
        string? projectPath = null,
        int maxResults = CodeNamePatternResult.DefaultMaxResults)
    {
        if (string.IsNullOrWhiteSpace(namePattern) && containerId is null && string.IsNullOrWhiteSpace(projectPath))
        {
            throw new ArgumentException(
                "Discovery requires a name pattern, a container, or a project path.",
                nameof(namePattern));
        }
        if (containerId is not null && !string.IsNullOrWhiteSpace(projectPath))
        {
            throw new ArgumentException(
                "Discovery accepts either a container or a project path, not both.",
                nameof(containerId));
        }
        if (maxResults < 1 || maxResults > CodeNamePatternResult.AbsoluteMaxResults)
            throw new ArgumentOutOfRangeException(nameof(maxResults));

        NamePattern = string.IsNullOrWhiteSpace(namePattern) ? null : namePattern;
        NodeKinds = nodeKinds?.Distinct().ToArray() ?? [];
        Languages = languages?.Select(language =>
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(language, nameof(languages));
                return language;
            }).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray() ?? [];
        ContainerId = containerId;
        ProjectPath = string.IsNullOrWhiteSpace(projectPath) ? null : projectPath;
        MaxResults = maxResults;
    }

    /// <summary>Gets the optional case-insensitive substring pattern.</summary>
    public string? NamePattern { get; }
    /// <summary>Gets the optional node-kind filter. Unset means symbol-bearing nodes.</summary>
    public IReadOnlyCollection<CodeNodeKind> NodeKinds { get; }
    /// <summary>Gets the optional language filter matched against the <c>language</c> property.</summary>
    public IReadOnlyCollection<string> Languages { get; }
    /// <summary>Gets the optional containing node scoping discovery to its containment.</summary>
    public CodeNodeId? ContainerId { get; }
    /// <summary>Gets the optional project path scoping discovery to one project.</summary>
    public string? ProjectPath { get; }
    /// <summary>Gets the maximum candidates; excess sets <see cref="CodeSymbolDiscoveryResult.Truncated"/>.</summary>
    public int MaxResults { get; }
}

/// <summary>Deterministically ordered discovery candidates with truncation.</summary>
public sealed record CodeSymbolDiscoveryResult(
    IReadOnlyList<CodeGraphNode> Symbols,
    bool Truncated);

/// <summary>The symbol whose declaration most tightly encloses a source position.</summary>
public sealed record CodeEnclosingSymbolResult(
    CodeGraphNode Symbol,
    CodeGraphDeclaration Declaration);
