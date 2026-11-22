namespace Andirit;

/// <summary>Stages and atomically publishes normalized graph facts.</summary>
/// <remarks>
/// <para>
/// Cancellation contract: a cancellation request is honoured while an operation
/// waits for the managed write gate and immediately before the native write
/// transaction opens. It is <b>not</b> honoured inside the native transaction,
/// because the native dependency exposes no cancellation or interruption entry
/// point: a cancellation that arrives during active native execution is not
/// observed, the transaction commits atomically, and the operation completes.
/// A cancelled operation never exposes partial facts or index entries, and a
/// durable mutation is never reported as cancelled.
/// </para>
/// <para>
/// The truthful outcomes are limited to three: cancelled before native
/// invocation (an <see cref="OperationCanceledException"/> is thrown and no
/// native write occurred), completed, or commit outcome unknown (the store
/// refuses current-state claims until the durable state is corroborated by
/// reopening). A managed timeout, an abandoned task or a disposed caller is
/// <b>not</b> a native cancellation; callers must await the operation to learn
/// its outcome.
/// </para>
/// </remarks>
public interface ICodeGraphIndexStore
{
    ValueTask UpsertRepositoryAsync(
        CodeRepositoryManifest repository,
        CancellationToken cancellationToken = default);

    ValueTask<CodeRepositoryManifest?> GetRepositoryAsync(
        CodeRepositoryId repositoryId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a running, failed, or cancelled run. Successful completion must use
    /// <see cref="CompleteIndexRunAsync"/> so source state is published atomically.
    /// </summary>
    ValueTask StoreIndexRunAsync(
        CodeIndexRunManifest run,
        CancellationToken cancellationToken = default);

    ValueTask<CodeIndexRunManifest?> GetIndexRunAsync(
        CodeRepositoryId repositoryId,
        CodeIndexRunId runId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically publishes all changes staged for the run, completes it, and
    /// publishes its incremental source state. Failed or cancelled runs must
    /// not replace the last successful graph or source state.
    /// </summary>
    ValueTask CompleteIndexRunAsync(
        CodeIndexRunManifest completedRun,
        CodeRepositoryIndexState state,
        CancellationToken cancellationToken = default);

    ValueTask<CodeRepositoryIndexState?> GetLatestIndexStateAsync(
        CodeRepositoryId repositoryId,
        CancellationToken cancellationToken = default);

    /// <summary>Stages an index-unit replacement for its running index run.</summary>
    ValueTask StageIndexUnitAsync(
        CodeIndexUnitReplacement replacement,
        CancellationToken cancellationToken = default);

    /// <summary>Stages an index-unit deletion for a running index run.</summary>
    ValueTask StageIndexUnitDeletionAsync(
        CodeRepositoryId repositoryId,
        CodeIndexRunId indexRunId,
        CodePluginId pluginId,
        CodeIndexUnitId indexUnitId,
        CancellationToken cancellationToken = default);
}

/// <summary>Reads the latest successfully published repository graph.</summary>
public interface ICodeGraphReader
{
    /// <summary>Gets the identity of the latest successful publication.</summary>
    ValueTask<CodeGraphPublication?> GetLatestPublicationAsync(
        CodeRepositoryId repositoryId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets every published index unit for the repository in deterministic
    /// plugin and unit order. Backs integrity-checked publication snapshots.
    /// </summary>
    ValueTask<IReadOnlyList<CodeIndexUnitReplacement>> GetPublishedUnitsAsync(
        CodeRepositoryId repositoryId,
        CancellationToken cancellationToken = default);

    ValueTask<CodeGraphQueryEnvelope<IReadOnlyList<CodeGraphNode>>?>
        FindNodesByQualifiedNameWithProvenanceAsync(
            CodeRepositoryId repositoryId,
            string qualifiedName,
            CancellationToken cancellationToken = default);

    ValueTask<CodeGraphQueryEnvelope<CodeGraphTraversalResult>?>
        TraverseWithProvenanceAsync(
            CodeRepositoryId repositoryId,
            CodeGraphTraversalQuery query,
            CancellationToken cancellationToken = default);

    ValueTask<CodeGraphQueryEnvelope<IReadOnlyList<CodeGraphDeclaration>>?>
        GetDeclarationsWithProvenanceAsync(
            CodeRepositoryId repositoryId,
            CodeSymbolId symbolId,
            CancellationToken cancellationToken = default);

    ValueTask<CodeGraphNode?> GetNodeAsync(
        CodeRepositoryId repositoryId,
        CodeNodeId nodeId,
        CancellationToken cancellationToken = default);

    ValueTask<CodeGraphNode?> FindSymbolAsync(
        CodeRepositoryId repositoryId,
        CodeSymbolId symbolId,
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<CodeGraphNode>> FindNodesByQualifiedNameAsync(
        CodeRepositoryId repositoryId,
        string qualifiedName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds up to <paramref name="maxResults"/> nodes whose qualified or display
    /// name contains <paramref name="pattern"/> (ordinal, case-insensitive),
    /// ordered by qualified name then node identity.
    /// </summary>
    ValueTask<CodeNamePatternResult> FindNodesByNamePatternAsync(
        CodeRepositoryId repositoryId,
        string pattern,
        int maxResults = CodeNamePatternResult.DefaultMaxResults,
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<CodeGraphDeclaration>> GetDeclarationsAsync(
        CodeRepositoryId repositoryId,
        CodeSymbolId symbolId,
        CancellationToken cancellationToken = default);

    ValueTask<CodeGraphTraversalResult> TraverseAsync(
        CodeRepositoryId repositoryId,
        CodeGraphTraversalQuery query,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Combined provider contract for hosts that both index and query repositories.
/// Query-only consumers should depend on <see cref="ICodeGraphReader"/>.
/// </summary>
public interface ICodeGraphStore : ICodeGraphIndexStore, ICodeGraphReader;
