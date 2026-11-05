using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
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

    // S2.2 / WP3: pre-mint defining-compilation resolution and association
    // invariant (mapping contract §3, Option A §5.1). This resolves the exact
    // live defining compilation and validates the current verified `compctx2`
    // association before any target/occurrence is minted. It performs no
    // extraction, minting or persistence, and is independently testable without
    // WP4 runtime output.
    //
    // Ordering matches the accepted contract: completeness is checked before
    // binding, binding is proven by live instance (never by descriptor or
    // assembly name alone), scope membership is checked before association, and a
    // recorded context is only accepted when it equals the current verified
    // context. A caller-supplied boolean, a matching descriptor shape, or an equal
    // `asmctx3` never establishes authority here.
    internal VerifiedDefiningResolution ResolveDefining(
        IMethodSymbol target, string? recordedCompctx2 = null, CSharpCompilation? caller = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (!IsComplete)
        {
            var missing = MissingScopeUnits;
            var detail = missing.IsEmpty
                ? "declared scope carries refusals"
                : "declared project units without a verified compilation context: " + string.Join(", ", missing);
            if (!Refusals.IsEmpty) detail += "; refusals: " + string.Join(", ", Refusals);
            return new(VerifiedDefiningStatus.Incomplete, detail);
        }

        var binding = Entries.Where(entry => BindsDefining(entry, target, caller)).ToArray();
        if (binding.Length == 0)
            return new(VerifiedDefiningStatus.MissingDefiningCompilation,
                "no verified compilation in the declared universe is the symbol's defining assembly");
        if (binding.Length > 1)
            return new(VerifiedDefiningStatus.Ambiguous,
                binding.Length + " verified compilations bind the same target symbol");

        var entry = binding[0];
        if (!Scope.Units.Any(unit =>
                string.Equals(unit.ProjectUnitId, entry.Descriptor.ProjectUnitId, StringComparison.Ordinal)))
            return new(VerifiedDefiningStatus.UnsupportedExternalEvidence,
                "defining project unit '" + entry.Descriptor.ProjectUnitId + "' is outside the declared analysis scope");

        if (recordedCompctx2 is not null &&
            !string.Equals(recordedCompctx2, entry.Descriptor.DescriptorDigest, StringComparison.Ordinal))
            return new(VerifiedDefiningStatus.StaleContext,
                "recorded defining context does not equal the current verified context", entry);

        return new(VerifiedDefiningStatus.Complete,
            "verified defining compilation context '" + entry.Descriptor.ProjectUnitId + "'",
            entry, entry.MapR2FaithfulCompctx1());
    }

    // S2.2 / WP4: authority-gated `asmctx3` target minting (Option A §5.1(1); mapping
    // contract §3; G-MINT owner §7). This is the production minting boundary. It
    // enforces the binding G3 precondition — a supplied, non-null recorded defining
    // `compctx2` — before delegating to the accepted WP3 resolver, so that
    // `ResolveDefining(..., null, ...)` returning Complete can never mint. Every
    // non-Complete resolution refuses (G2), the defining compilation's identity is
    // taken from the resolved live entry (G1), and the frozen
    // `CodeOccurrenceIdentity.ComputeTargetId` constructs the artifact from the
    // mapped R2-faithful `compctx1` only after those checks pass. The caller-supplied
    // component digests remain the accepted S-01 obligation and are not independently
    // verified here. No `usage4`/fact/persistence is produced (WP5/S2.3).
    internal VerifiedTargetMint MintTarget(
        IMethodSymbol target,
        string? recordedCompctx2,
        string identifierDigest,
        string declarationDigest,
        string shapeDigest,
        CSharpCompilation? caller = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (string.IsNullOrWhiteSpace(recordedCompctx2))
            return new(VerifiedTargetMintStatus.MissingRecordedContext,
                "a supplied, non-null recorded defining compctx2 is required before minting");

        var resolved = ResolveDefining(target, recordedCompctx2, caller);
        if (resolved.Status != VerifiedDefiningStatus.Complete)
            return new(MapMintStatus(resolved.Status), resolved.Detail);

        var compctx1 = resolved.Compctx1!;
        var targetId = CodeOccurrenceIdentity.ComputeTargetId(
            compctx1.DescriptorDigest, compctx1.MetadataIdentity, compctx1.ModuleName,
            identifierDigest, declarationDigest, shapeDigest);
        return new(VerifiedTargetMintStatus.Minted,
            "minted from verified defining compilation context '" +
            resolved.Defining!.Descriptor.ProjectUnitId + "'",
            targetId, compctx1);
    }

    private static VerifiedTargetMintStatus MapMintStatus(VerifiedDefiningStatus status) => status switch
    {
        VerifiedDefiningStatus.Complete => VerifiedTargetMintStatus.Minted,
        VerifiedDefiningStatus.Incomplete => VerifiedTargetMintStatus.Incomplete,
        VerifiedDefiningStatus.Ambiguous => VerifiedTargetMintStatus.Ambiguous,
        VerifiedDefiningStatus.MissingDefiningCompilation => VerifiedTargetMintStatus.MissingDefiningCompilation,
        VerifiedDefiningStatus.UnsupportedExternalEvidence => VerifiedTargetMintStatus.UnsupportedExternalEvidence,
        VerifiedDefiningStatus.StaleContext => VerifiedTargetMintStatus.StaleContext,
        _ => throw new InvalidDataException("UnsupportedDefiningStatus"),
    };

    // S2.2 / WP5: authority-gated `usage4` occurrence construction (mapping contract
    // §3; Option A §5.1(3); WP4-OWNER §9 W5-1/W5-2/W5-5). This consumes the WP4
    // `asmctx3` mint and only then builds the occurrence fact, so a structurally
    // valid `CodeOccurrenceTargetId` alone can never produce a trusted usage. A
    // non-minted status yields no fact. The caller-supplied component digests remain
    // the accepted S-01 obligation and are not independently verified here.
    internal VerifiedUsageMint MintUsage(VerifiedUsageInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var mint = MintTarget(input.Target, input.RecordedCompctx2,
            input.IdentifierDigest, input.DeclarationDigest, input.ShapeDigest, input.Caller);
        if (!mint.IsMinted)
            return new(mint.Status, mint.Detail);

        var fact = CodeOccurrenceFact.Create(
            input.RepositoryId,
            mint.TargetId!,
            input.TargetDocumentId,
            input.SourceUnitId,
            new CodeOccurrenceContributor(input.PluginId, input.SourceUnitId),
            CodeOccurrenceCategory.Invocation,
            input.Span,
            input.SourceHash,
            input.ContentHash,
            input.DeclarationSpan,
            4,
            input.PluginVersion);
        return new(VerifiedTargetMintStatus.Minted, mint.Detail, fact, mint.TargetId);
    }

    private static bool BindsDefining(VerifiedCSharpCompilation entry, IMethodSymbol target, CSharpCompilation? caller)
    {
        // Direct in-compilation defining assembly: the exact live assembly
        // instance, never an identity-equal assembly from a substituted
        // compilation instance.
        if (ReferenceEquals(entry.Compilation.Assembly, target.ContainingAssembly))
            return true;
        if (caller is null) return false;
        // Cross-project: the exact live CompilationReference instance plus the
        // verified metadata identity of the target's containing assembly.
        var targetMetadata = target.ContainingAssembly is { } assembly
            ? ProvenanceHash.Metadata(assembly.Identity)
            : "(unknown)";
        return caller.References.OfType<CompilationReference>().Any(reference =>
            ReferenceEquals(reference.Compilation, entry.Compilation) &&
            string.Equals(
                ProvenanceHash.Metadata(reference.Compilation.Assembly.Identity),
                targetMetadata, StringComparison.Ordinal));
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
