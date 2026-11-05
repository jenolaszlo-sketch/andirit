using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Andirit;

/// <summary>
/// The accepted semantic inputs for one candidate `usage4` occurrence. The
/// component digests and the target document are caller-supplied under the accepted
/// S-01 qualification and are not independently verified here.
/// </summary>
internal sealed record VerifiedUsageInput(
    CodeRepositoryId RepositoryId,
    CodePluginId PluginId,
    IMethodSymbol Target,
    string? RecordedCompctx2,
    string SourceUnitId,
    CodeLocation Span,
    string SourceHash,
    string ContentHash,
    string TargetDocumentId,
    string IdentifierDigest,
    string DeclarationDigest,
    string ShapeDigest,
    CSharpCompilation? Caller = null,
    CodeLocation? DeclarationSpan = null,
    string? PluginVersion = null);

/// <summary>
/// The authority-gated `usage4` result. <see cref="Fact"/> is present only when the
/// WP4 `asmctx3` mint succeeded, so a structural identifier alone cannot produce a
/// trusted occurrence. Non-minted statuses carry no authority-bearing artifact.
/// </summary>
internal sealed record VerifiedUsageMint(
    VerifiedTargetMintStatus Status,
    string Detail,
    CodeOccurrenceFact? Fact = null,
    CodeOccurrenceTargetId? TargetId = null)
{
    /// <summary>True only when a trusted, verified `usage4` fact was produced.</summary>
    internal bool IsMinted => Status == VerifiedTargetMintStatus.Minted && Fact is not null;
}

/// <summary>
/// One bounded extraction pass over a calling compilation: the trusted facts it
/// emitted plus the disjoint coverage counters and analysis state. The coverage is
/// constructed directly, never by the deferred NEW-04 `Merge`.
/// </summary>
internal sealed record VerifiedOccurrenceExtraction(
    ImmutableArray<CodeOccurrenceFact> Facts,
    CodeOccurrenceCoverage Coverage);
