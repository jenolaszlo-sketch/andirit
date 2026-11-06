using System.Text.Json;
using Microsoft.CodeAnalysis.CSharp;

namespace Andirit;

/// <summary>
/// Query-scoped current authority derived only from independently verified live
/// compilations. Persisted strings cannot construct this gate. S-01 component
/// digests remain caller-supplied, with no stronger canonicalization claim.
/// </summary>
internal sealed class VerifiedCSharpOccurrenceAuthority : ICodeOccurrenceAuthorityVerifier
{
    private readonly VerifiedCSharpUniverse _universe;

    private VerifiedCSharpOccurrenceAuthority(VerifiedCSharpUniverse universe) => _universe = universe;

    internal static CodeOccurrenceQuery Bind(
        CodeOccurrenceQuery query, VerifiedCSharpUniverse universe, CSharpCompilation currentCaller)
    {
        // Requires exact live membership, not a descriptor-equal substitute.
        universe.RequireCompilation(currentCaller);
        return query with { CurrentAuthority = new VerifiedCSharpOccurrenceAuthority(universe) };
    }

    public CodeOccurrenceAuthorityRefusal? Validate(
        CodeOccurrenceAuthority authority, string repositoryId, string pluginId, string indexUnitId,
        IReadOnlyList<CodeOccurrenceFact> facts, CodeOccurrenceCoverage? coverage)
    {
        static CodeOccurrenceAuthorityRefusal Corrupt(string why) => new(CodeOccurrenceAvailability.CorruptedIndex, why);
        static CodeOccurrenceAuthorityRefusal Stale(string why) => new(CodeOccurrenceAvailability.StalePublication, why);
        if (authority.RepositoryId != repositoryId || authority.PluginId != pluginId ||
            authority.IndexUnitId != indexUnitId || authority.Coverage is null ||
            authority.Members is null || authority.Facts is null || string.IsNullOrWhiteSpace(authority.SourceUnitId))
            return Corrupt("Occurrence authority ownership or required fields are inconsistent.");
        if (coverage is null || JsonSerializer.Serialize(authority.Coverage) != JsonSerializer.Serialize(coverage) ||
            authority.ScopeId != coverage.ScopeId || authority.UniverseId != coverage.UniverseId)
            return Corrupt("Occurrence authority is not bound to the stored coverage.");
        if (authority.Members.Any(m => m is null || !CanonicalContext(m.Compctx2)))
            return Corrupt("Malformed recorded compilation-universe membership.");
        if (authority.Members.Select(m => m.ProjectUnitId).Distinct(StringComparer.Ordinal).Count() != authority.Members.Count)
            return Corrupt("Duplicate recorded compilation-universe membership.");
        if (!_universe.IsComplete || _universe.Scope.RepositoryId != repositoryId ||
            authority.ScopeId != _universe.Scope.Identity || authority.UniverseId != _universe.Identity)
            return Stale("The recorded analysis scope/universe is not the current verified universe.");
        var members = _universe.Entries.Select(e => new CodeOccurrenceCompilationAuthority(
            e.Descriptor.ProjectUnitId, e.Descriptor.DescriptorDigest)).ToArray();
        if (!authority.Members.SequenceEqual(members) || !members.Any(m => m.ProjectUnitId == authority.SourceUnitId))
            return Stale("Recorded members do not equal the current verified compilation membership.");
        if (!coverage.AnalysisPerformed || coverage.InvocationFacts != facts.Count ||
            coverage.DeclaredUnitCount != _universe.Scope.Units.Length || coverage.VerifiedUnitCount != members.Length)
            return Corrupt("Coverage accounting does not match the verified scope and stored facts.");
        if (authority.Facts.Count != facts.Count || authority.Facts.Any(f => f is null) ||
            authority.Facts.Select(f => f.OccurrenceId).Distinct(StringComparer.Ordinal).Count() != facts.Count)
            return Corrupt("Missing, duplicate or orphan per-occurrence defining authority.");
        var byId = authority.Facts.ToDictionary(f => f.OccurrenceId, StringComparer.Ordinal);
        foreach (var fact in facts)
        {
            if (!byId.TryGetValue(fact.Id.Value, out var recorded) ||
                fact.RepositoryId.Value != repositoryId || fact.Contributor.PluginId.Value != pluginId ||
                fact.SourceUnitId != authority.SourceUnitId || recorded.TargetId != fact.TargetId.Value ||
                recorded.PayloadHash != fact.PayloadHash ||
                CodeOccurrenceIdentity.ComputePayloadHash(fact) != recorded.PayloadHash)
                return Corrupt("The defining authority is not bound to the stored occurrence evidence.");
            if (string.IsNullOrWhiteSpace(recorded.Compctx2))
                return new(CodeOccurrenceAvailability.AnalysisUnavailable, "Missing recorded defining compctx2.");
            if (!CanonicalContext(recorded.Compctx2) || string.IsNullOrWhiteSpace(recorded.IdentifierDigest) ||
                string.IsNullOrWhiteSpace(recorded.DeclarationDigest) || string.IsNullOrWhiteSpace(recorded.ShapeDigest))
                return Corrupt("Malformed recorded defining authority.");
            var defining = _universe.Entries.SingleOrDefault(e => e.Descriptor.ProjectUnitId == recorded.DefiningProjectUnitId);
            if (defining is null || defining.Descriptor.DescriptorDigest != recorded.Compctx2)
                return Stale("Recorded defining compctx2 is not the current verified defining context.");
            var mapped = defining.MapR2FaithfulCompctx1();
            var target = CodeOccurrenceIdentity.ComputeTargetId(mapped.DescriptorDigest, mapped.MetadataIdentity,
                mapped.ModuleName, recorded.IdentifierDigest, recorded.DeclarationDigest, recorded.ShapeDigest);
            if (target != fact.TargetId)
                return Corrupt("The stored target is not bound to the recorded defining compilation.");
            var source = _universe.Entries.Single(e => e.Descriptor.ProjectUnitId == authority.SourceUnitId);
            if (!source.Sources.Any(s => s.Path == fact.Span.Path && s.ContentHash == fact.SourceHash))
                return Corrupt("The occurrence source is not in the verified source unit.");
        }
        return null;
    }

    private static bool CanonicalContext(string? value) => value is not null &&
        value.StartsWith("compctx2:sha256:", StringComparison.Ordinal) && value.Length == 80 &&
        value.AsSpan(16).ToString().All(char.IsAsciiHexDigitLower);
}
