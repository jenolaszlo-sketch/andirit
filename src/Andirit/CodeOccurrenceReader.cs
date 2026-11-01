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

/// <summary>A bounded, target-scoped occurrence request. Not a public Find Usages API.</summary>
public sealed record CodeOccurrenceQuery
{
    public CodeOccurrenceQuery(
        CodeRepositoryId repositoryId,
        CodeOccurrenceTargetId targetId,
        CodeGraphPublication? expectedPublication = null,
        int maxResults = 100)
    {
        RepositoryId = repositoryId ?? throw new ArgumentNullException(nameof(repositoryId));
        TargetId = targetId ?? throw new ArgumentNullException(nameof(targetId));
        if (maxResults < 1)
            throw new ArgumentOutOfRangeException(nameof(maxResults));
        ExpectedPublication = expectedPublication;
        MaxResults = maxResults;
    }

    public CodeRepositoryId RepositoryId { get; }
    public CodeOccurrenceTargetId TargetId { get; }
    public CodeGraphPublication? ExpectedPublication { get; }
    public int MaxResults { get; }
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
    CodeOccurrenceCoverage? Coverage = null)
{
    public bool Truncated => TotalMatches > Occurrences.Count;

    /// <summary>
    /// True only when the publication is current, the target belongs to a
    /// completely analyzed declared scope, and the result is a valid
    /// complete-empty answer.
    /// </summary>
    public bool IsComplete => Availability == CodeOccurrenceAvailability.Available &&
        Coverage?.IsComplete == true;
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
