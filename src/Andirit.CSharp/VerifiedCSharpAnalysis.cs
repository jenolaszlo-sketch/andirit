using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;

namespace Andirit;

internal sealed record CSharpScopeUnit(string ProjectUnitId, string ProjectPath, ImmutableArray<string> SourcePaths);

internal sealed class VerifiedCSharpScope
{
    private VerifiedCSharpScope(string repositoryId, ImmutableArray<CSharpScopeUnit> units, string identity)
    { RepositoryId = repositoryId; Units = units; Identity = identity; }
    internal string RepositoryId { get; }
    internal ImmutableArray<CSharpScopeUnit> Units { get; }
    internal string Identity { get; }
    internal static VerifiedCSharpScope Declare(string repositoryId, IEnumerable<CSharpScopeUnit> units)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryId);
        var ordered = units.Select(u => u with
        {
            ProjectUnitId = u.ProjectUnitId.Replace('\\', '/'),
            ProjectPath = u.ProjectPath.Replace('\\', '/'),
            SourcePaths = u.SourcePaths.Select(p => p.Replace('\\', '/')).Order(StringComparer.Ordinal).ToImmutableArray()
        }).OrderBy(u => u.ProjectUnitId, StringComparer.Ordinal).ToImmutableArray();
        if (ordered.Select(u => u.ProjectUnitId).Distinct(StringComparer.Ordinal).Count() != ordered.Length ||
            ordered.Select(u => u.ProjectPath).Distinct(CSharpProjectDiscovery.PathComparer).Count() != ordered.Length ||
            ordered.Any(u => string.IsNullOrWhiteSpace(u.ProjectUnitId) || u.ProjectUnitId.Contains('|') ||
                string.IsNullOrWhiteSpace(u.ProjectPath) || u.ProjectPath.Contains('|') ||
                u.SourcePaths.Distinct(CSharpProjectDiscovery.PathComparer).Count() != u.SourcePaths.Length))
            throw new InvalidDataException("ConflictingProjectIdentity");
        return new(repositoryId, ordered, "analysisscope1:sha256:" + ProvenanceHash.Hash("analysisscope1-scope",
            [repositoryId, .. ordered.Select(u => u.ProjectUnitId + "|" + u.ProjectPath)]));
    }
}

internal sealed class VerifiedCSharpUniverse
{
    private VerifiedCSharpUniverse(VerifiedCSharpScope scope, ImmutableArray<VerifiedCSharpCompilation> entries,
        ImmutableArray<string> refusals, string identity)
    { Scope = scope; Entries = entries; Refusals = refusals; Identity = identity; }
    internal VerifiedCSharpScope Scope { get; }
    internal ImmutableArray<VerifiedCSharpCompilation> Entries { get; }
    internal ImmutableArray<string> Refusals { get; }
    internal string Identity { get; }
    internal bool IsComplete => Refusals.IsEmpty && MissingScopeUnits.IsEmpty;
    internal ImmutableArray<string> MissingScopeUnits => Scope.Units.Select(u => u.ProjectUnitId)
        .Except(Entries.Select(e => e.Descriptor.ProjectUnitId), StringComparer.Ordinal).ToImmutableArray();

    internal static VerifiedCSharpUniverse Create(string repositoryId, VerifiedCSharpScope scope,
        IEnumerable<VerifiedCSharpCompilation> entries, IEnumerable<string>? refusals = null)
    {
        if (scope.RepositoryId != repositoryId) throw new InvalidDataException("ScopeRepositoryMismatch");
        var ordered = entries.OrderBy(e => e.Descriptor.ProjectUnitId, StringComparer.Ordinal).ToImmutableArray();
        if (ordered.Select(e => e.Descriptor.ProjectUnitId).Distinct(StringComparer.Ordinal).Count() != ordered.Length)
            throw new InvalidDataException("ConflictingProjectIdentity");
        if (ordered.Select(e => e.Compilation).Distinct(ReferenceEqualityComparer.Instance).Count() != ordered.Length)
            throw new InvalidDataException("ConflictingLiveCompilation");
        foreach (var entry in ordered)
        {
            entry.Descriptor.Verified();
            if (entry.Descriptor.ContractVersion != "compctx2" || entry.Descriptor.RepositoryId != repositoryId)
                throw new InvalidDataException("UniverseContextMismatch");
            var unit = scope.Units.SingleOrDefault(u => u.ProjectUnitId == entry.Descriptor.ProjectUnitId);
            if (unit is null || !unit.SourcePaths.SequenceEqual(entry.Descriptor.Sources.Select(s => s.Path)))
                throw new InvalidDataException("ScopeSourceMembershipMismatch");
            if (entry.Dependencies.Any(d => !ordered.Any(e => ReferenceEquals(e, d))))
                throw new InvalidDataException("MissingDefiningCompilation");
        }
        return new(scope, ordered, (refusals ?? []).Distinct().Order(StringComparer.Ordinal).ToImmutableArray(),
            "compilation-universe1:sha256:" + ProvenanceHash.Hash("compilation-universe1-universe",
                [repositoryId, "compctx2", scope.Identity, .. ordered.Select(e => e.Descriptor.DescriptorDigest)]));
    }

    internal VerifiedCSharpCompilation RequireCompilation(CSharpCompilation compilation)
    {
        if (!IsComplete) throw new InvalidDataException("IncompleteCompilationUniverse");
        return Entries.SingleOrDefault(e => ReferenceEquals(e.Compilation, compilation))
            ?? throw new InvalidDataException("MissingDefiningCompilation");
    }
}

internal sealed record CSharpPreparedProject(CSharpProjectModel Project, CSharpCompilation Compilation,
    IReadOnlyDictionary<string, CSharpCompilation> AvailableCompilations,
    VerifiedCSharpCompilation? Verified, ImmutableArray<string> Refusals);

internal sealed record VerifiedCSharpAnalysis(ImmutableArray<CSharpPreparedProject> Projects,
    ImmutableArray<VerifiedCSharpSource> Sources, VerifiedCSharpUniverse Universe,
    ImmutableArray<string> DiscoveryWarnings);

internal interface IVerifiedCSharpSession
{
    // Assigned only after a complete extraction returns successfully; cancellation clears it.
    VerifiedCSharpAnalysis? Provenance { get; }
}
