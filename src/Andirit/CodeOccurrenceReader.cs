namespace Andirit;

/// <summary>The availability state of occurrence analysis for a repository.</summary>
public enum CodeOccurrenceAvailability
{
    /// <summary>A current, publication-bound occurrence index answered the query.</summary>
    Available = 0,

    /// <summary>The store has no occurrence analysis (for example a graph-only schema-2 store).</summary>
    AnalysisUnavailable = 1,

    /// <summary>The store carries an unsupported occurrence format.</summary>
    UnsupportedSchema = 2,

    /// <summary>The requested publication is not the accepted occurrence publication.</summary>
    StalePublication = 3,

    /// <summary>The occurrence index is missing entries or is internally inconsistent.</summary>
    CorruptedIndex = 4,

    /// <summary>
    /// Occurrence analysis ran, but the declared analysis scope is not proven
    /// complete. Results may be partial; a zero-result answer is not a complete
    /// negative.
    /// </summary>
    IncompleteAnalysis = 5
}

/// <summary>
/// Continuation state of one occurrence query result. Kept separate from
/// <see cref="CodeOccurrenceAvailability"/> so publication staleness, incomplete
/// analysis and invalid continuation are never conflated.
/// </summary>
public enum CodeOccurrenceContinuationState
{
    /// <summary>No continuation was requested and none is offered (refusals/unavailable).</summary>
    NotRequested = 0,

    /// <summary>More occurrences exist within the same publication; a next token is present.</summary>
    More = 1,

    /// <summary>The result set is exhausted for this query (final page or complete result).</summary>
    Complete = 2,

    /// <summary>The supplied continuation token was rejected (malformed or bound to a superseded publication).</summary>
    Invalid = 3
}

/// <summary>
/// One contributing storage unit's coverage in the query/publication scope. This
/// is scope-level provenance, not a property of the currently returned page.
/// </summary>
public sealed record CodeOccurrenceUnitCoverage(
    CodePluginId PluginId,
    string IndexUnitId,
    CodeRelationshipCoverageState State,
    bool AnalysisPerformed,
    int InvocationFacts,
    int UnsupportedCategories,
    int External,
    int Ambiguous,
    int Unresolved,
    int Unsupported,
    int ConditionalInactive,
    string? ScopeId,
    string? UniverseId);

/// <summary>A bounded, target-scoped occurrence request. Not a public Find Usages API.</summary>
public sealed record CodeOccurrenceQuery
{
    /// <summary>Default page size for occurrence reads.</summary>
    public const int DefaultPageSize = 100;

    /// <summary>Hard upper bound on returned occurrences per request.</summary>
    public const int AbsoluteMaxPageSize = 1000;

    public CodeOccurrenceQuery(
        CodeRepositoryId repositoryId,
        CodeOccurrenceTargetId targetId,
        CodeGraphPublication? expectedPublication = null,
        int maxResults = DefaultPageSize)
    {
        RepositoryId = repositoryId ?? throw new ArgumentNullException(nameof(repositoryId));
        TargetId = targetId ?? throw new ArgumentNullException(nameof(targetId));
        if (maxResults < 1)
            throw new ArgumentOutOfRangeException(nameof(maxResults));
        ExpectedPublication = expectedPublication;
        MaxResults = maxResults;
    }

    internal ICodeOccurrenceAuthorityVerifier? CurrentAuthority { get; init; }

    public CodeRepositoryId RepositoryId { get; }
    public CodeOccurrenceTargetId TargetId { get; }
    public CodeGraphPublication? ExpectedPublication { get; }
    public int MaxResults { get; }

    /// <summary>
    /// Opaque, versioned, stateless continuation token obtained from a prior
    /// result's <see cref="CodeOccurrenceQueryResult.NextContinuationToken"/>. Null
    /// requests the first page. Tokens are never persisted and are bound to the
    /// repository, exact target, mode, publication identity and page size.
    /// </summary>
    public string? ContinuationToken { get; init; }
}

/// <summary>One deterministically ordered occurrence result.</summary>
public sealed record CodeOccurrenceHit(
    CodeOccurrenceId Id,
    CodeOccurrenceTargetId TargetId,
    CodeOccurrenceCategory Category,
    CodeLocation Span,
    string SourceUnitId,
    IReadOnlyList<CodeOccurrenceContributor> Contributors,
    int SemanticsVersion);

/// <summary>
/// A bounded occurrence page. <see cref="Examined"/> counts the real index
/// entries read for this query; it is not a scan of unrelated repository facts.
/// </summary>
public sealed record CodeOccurrenceQueryResult(
    CodeOccurrenceAvailability Availability,
    CodeGraphPublication? Publication,
    IReadOnlyList<CodeOccurrenceHit> Occurrences,
    int TotalMatches,
    int Examined,
    string? RefusalReason = null,
    CodeOccurrenceCoverage? Coverage = null,
    string? NextContinuationToken = null,
    CodeOccurrenceContinuationState Continuation = CodeOccurrenceContinuationState.NotRequested,
    IReadOnlyList<CodeOccurrenceUnitCoverage>? CoverageBreakdown = null)
{
    /// <summary>
    /// Scope-level per-unit coverage provenance for the query/publication scope
    /// (never just the current page). Empty when the provider has no breakdown.
    /// </summary>
    public IReadOnlyList<CodeOccurrenceUnitCoverage> UnitCoverage => CoverageBreakdown ?? [];

    public bool Truncated => TotalMatches > Occurrences.Count;

    /// <summary>
    /// True only when the publication is current, the target belongs to a
    /// completely analyzed declared scope, and this result is the final page of
    /// that scope (or a complete single-page/complete-empty answer). An
    /// intermediate (<see cref="CodeOccurrenceContinuationState.More"/>) or
    /// rejected (<see cref="CodeOccurrenceContinuationState.Invalid"/>) result is
    /// never complete.
    /// </summary>
    public bool IsComplete => Availability == CodeOccurrenceAvailability.Available &&
        Coverage?.IsComplete == true &&
        Continuation is CodeOccurrenceContinuationState.Complete or CodeOccurrenceContinuationState.NotRequested;
}

/// <summary>
/// Internal, target-scoped occurrence reader. It is deliberately separate from
/// <see cref="ICodeGraphReader"/> and is not a public user-facing query API.
/// </summary>
public interface ICodeOccurrenceReader
{
    ValueTask<CodeOccurrenceQueryResult> FindOccurrencesAsync(
        CodeOccurrenceQuery query,
        CancellationToken cancellationToken = default);
}
