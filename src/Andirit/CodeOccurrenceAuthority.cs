namespace Andirit;

// Internal, versioned validity evidence. These records do not define semantic
// identity and do not by themselves establish current authority.
internal sealed record CodeOccurrenceCompilationAuthority(string ProjectUnitId, string Compctx2);

internal sealed record CodeOccurrenceDefiningAuthority(
    string OccurrenceId, string PayloadHash, string TargetId, string DefiningProjectUnitId,
    string Compctx2, string IdentifierDigest, string DeclarationDigest, string ShapeDigest);

internal sealed record CodeOccurrenceAuthority(
    int Version, string RepositoryId, string PluginId, string IndexUnitId, string SourceUnitId,
    string ScopeId, string UniverseId, CodeOccurrenceCoverage Coverage,
    IReadOnlyList<CodeOccurrenceCompilationAuthority> Members,
    IReadOnlyList<CodeOccurrenceDefiningAuthority> Facts);

internal sealed record CodeOccurrenceAuthorityRefusal(CodeOccurrenceAvailability Availability, string Reason);

// Implemented by the C# verified-compilation boundary, never by persisted data.
internal interface ICodeOccurrenceAuthorityVerifier
{
    CodeOccurrenceAuthorityRefusal? Validate(
        CodeOccurrenceAuthority authority, string repositoryId, string pluginId, string indexUnitId,
        IReadOnlyList<CodeOccurrenceFact> facts, CodeOccurrenceCoverage? coverage);
}

// WP-S23-3A internal handoff. Andirit.Abstractions cannot depend on Andirit, so
// the accepted authority envelope cannot travel on the public CodeGraphBatch.
// A publication sink implements this internal capability to associate the
// verified envelope with the owning staged unit, keyed by the same
// (repository, plugin, index-unit) origin as the batch stream. Sinks that do not
// implement it receive graph-only output: occurrence facts are never published
// without the authority association required to serve them.
internal interface ICodeOccurrenceAuthoritySink
{
    ValueTask SetOccurrenceAuthorityAsync(
        CodeFactOrigin origin,
        CodeOccurrenceAuthority authority,
        CancellationToken cancellationToken = default);
}
