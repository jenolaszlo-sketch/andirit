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
