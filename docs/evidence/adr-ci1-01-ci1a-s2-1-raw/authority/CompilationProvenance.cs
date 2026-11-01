using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AdrCi1Exp.Shared;

/// <summary>
/// Domain-separated, length-prefixed hashing shared by the R2 provenance
/// contract. No wall clock, process id, object address, run id or enumeration
/// order participates in any identity here.
/// </summary>
public static class ProvenanceHashing
{
    public static string Hash(string domain, IEnumerable<string> parts)
    {
        using var sha = SHA256.Create();
        using var stream = new MemoryStream();
        void Part(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            Span<byte> length = stackalloc byte[4];
            System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(length, bytes.Length);
            stream.Write(length);
            stream.Write(bytes);
        }

        Part(domain);
        foreach (var part in parts) Part(part);
        return Convert.ToHexStringLower(sha.ComputeHash(stream.ToArray()));
    }

    public static string Sha256Hex(byte[] bytes) =>
        Convert.ToHexStringLower(SHA256.HashData(bytes));

    public static string NormalizePath(string path) => path.Replace('\\', '/');
}

/// <summary>
/// One exact source input of a compilation: normalized path, byte content hash
/// and the parse options that were applied to it.
/// </summary>
public sealed record SourceInputIdentity(
    string Path,
    string ContentHash,
    string LanguageVersion,
    string PreprocessorSymbols)
{
    public string CanonicalForm => string.Join("|", Path, ContentHash, LanguageVersion, PreprocessorSymbols);
}

/// <summary>
/// Versioned compilation-context provenance (<c>compctx1</c>). A descriptor is a
/// pure function of verified construction inputs: repository, defining project
/// unit, assembly/module metadata, compilation options, exact source content
/// hashes and reference identities. It is recomputable from its own fields, so a
/// persisted or relayed descriptor can be checked rather than trusted.
/// </summary>
public sealed record CompilationDescriptor(
    string ContractVersion,
    string RepositoryId,
    string ProjectUnitId,
    string AssemblyName,
    string MetadataIdentity,
    string ModuleName,
    string OptionsDigest,
    IReadOnlyList<SourceInputIdentity> Sources,
    IReadOnlyList<string> References,
    string DescriptorDigest)
{
    public const string Domain = "compctx1";
    public const string CanonicalPrefix = Domain + ":sha256:";

    public static bool IsCanonical(string value) =>
        value.StartsWith(CanonicalPrefix, StringComparison.Ordinal) &&
        value.Length == CanonicalPrefix.Length + 64 &&
        value.AsSpan(CanonicalPrefix.Length).ToString().All(char.IsAsciiHexDigitLower);

    public string ComputeDigest() => CanonicalPrefix + ProvenanceHashing.Hash(Domain + "-context",
    [
        ContractVersion,
        RepositoryId,
        ProjectUnitId,
        AssemblyName,
        MetadataIdentity,
        ModuleName,
        OptionsDigest,
        .. Sources.Select(source => source.CanonicalForm),
        .. References,
    ]);

    /// <summary>Recomputes the digest and rejects a descriptor that does not verify.</summary>
    public CompilationDescriptor Verified()
    {
        if (!IsCanonical(DescriptorDigest) || ComputeDigest() != DescriptorDigest)
            throw new InvalidDataException(
                "compilation descriptor digest does not verify against its own fields");
        return this;
    }
}

/// <summary>One declared project unit of the bounded analysis scope.</summary>
public sealed record AnalysisScopeUnit(string ProjectUnitId, string ProjectPath)
{
    public string CanonicalForm => ProjectUnitId + "|" + ProjectPath;
}

/// <summary>
/// The bounded, declared analysis universe for one repository analysis. The
/// scope is the authority boundary: confirmation is only possible for a target
/// whose defining project unit is declared here and whose compilation context
/// was actually constructed and verified. This deliberately does not claim
/// global uniqueness over every compilation in existence.
/// </summary>
public sealed record AnalysisScope(
    string RepositoryId,
    IReadOnlyList<AnalysisScopeUnit> DeclaredUnits,
    string ScopeId)
{
    public const string Domain = "analysisscope1";

    public IReadOnlySet<string> UnitIds =>
        DeclaredUnits.Select(unit => unit.ProjectUnitId).ToHashSet(StringComparer.Ordinal);

    public static AnalysisScope Declare(string repositoryId, IEnumerable<AnalysisScopeUnit> units)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryId);
        var normalized = units
            .Select(unit => unit with
            {
                ProjectUnitId = unit.ProjectUnitId.Replace('\\', '/'),
                ProjectPath = unit.ProjectPath.Replace('\\', '/'),
            })
            .OrderBy(unit => unit.ProjectUnitId, StringComparer.Ordinal)
            .ToArray();
        if (normalized.Select(unit => unit.ProjectUnitId).Distinct(StringComparer.Ordinal).Count() != normalized.Length)
            throw new ArgumentException("analysis scope contains duplicate project units", nameof(units));
        var id = Domain + ":sha256:" + ProvenanceHashing.Hash(Domain + "-scope",
            [repositoryId, .. normalized.Select(unit => unit.CanonicalForm)]);
        return new AnalysisScope(repositoryId, normalized, id);
    }

    /// <summary>
    /// Builds the declared scope from the actual project files under a repository
    /// root. This is construction-derived rather than a caller-supplied flag:
    /// build output, VCS metadata and hidden directories are excluded.
    /// </summary>
    public static AnalysisScope Discover(string repositoryId, string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryId);
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        var root = Path.GetFullPath(repositoryRoot);
        if (!Directory.Exists(root))
            throw new DirectoryNotFoundException("repository root not found: " + root);
        var units = Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories)
            .Where(path => !IsExcluded(root, path))
            .Select(path =>
            {
                var relative = ProvenanceHashing.NormalizePath(Path.GetRelativePath(root, path));
                return new AnalysisScopeUnit(relative, relative);
            })
            .ToArray();
        return Declare(repositoryId, units);
    }

    private static bool IsExcluded(string root, string path)
    {
        var relative = ProvenanceHashing.NormalizePath(Path.GetRelativePath(root, path));
        var segments = relative.Split('/');
        return segments.Any(segment =>
            segment.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
            segment.Equals("obj", StringComparison.OrdinalIgnoreCase) ||
            segment.Equals("node_modules", StringComparison.OrdinalIgnoreCase) ||
            segment.StartsWith(".", StringComparison.Ordinal));
    }
}

/// <summary>
/// One verified compilation context in the analysis universe. The live
/// compilation instance is retained so that binding can be proven by symbol
/// identity; a descriptor without a live instance cannot prove a binding.
/// </summary>
public sealed record CompilationUniverseEntry(
    CompilationDescriptor Descriptor,
    CSharpCompilation? Compilation)
{
    public bool IsLive => Compilation is not null;
}

/// <summary>Construction input for one universe entry.</summary>
public sealed record UniverseEntryInput(
    string ProjectUnitId,
    CSharpCompilation Compilation,
    IReadOnlyCollection<(string Path, byte[] Bytes)>? Sources = null);

/// <summary>Outcome of verifying a target against one analysis universe.</summary>
public enum UniverseContextStatus
{
    Complete = 0,
    Incomplete = 1,
    Ambiguous = 2,
    MissingDefiningCompilation = 3,
    UnsupportedContextVersion = 4,
    UnsupportedExternalEvidence = 5,
}

public sealed record UniverseVerification(
    UniverseContextStatus Status,
    string Detail,
    CompilationUniverseEntry? DefiningEntry = null)
{
    public bool IsComplete => Status == UniverseContextStatus.Complete && DefiningEntry is not null;
}

/// <summary>
/// Authoritative compilation universe for the supported analysis mode. Members
/// are constructed from actual <see cref="CSharpCompilation"/> objects; the
/// universe identity is a digest over the verified descriptors and the declared
/// scope, and is evidence only — it is not part of any target identity.
/// </summary>
public sealed record CompilationUniverse(
    string RepositoryId,
    string ContractVersion,
    AnalysisScope Scope,
    IReadOnlyList<CompilationUniverseEntry> Entries,
    string UniverseId)
{
    public const string Domain = "compilation-universe1";
    public const string CanonicalPrefix = Domain + ":sha256:";

    public static CompilationUniverse Create(
        string repositoryId,
        AnalysisScope scope,
        params UniverseEntryInput[] entries)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryId);
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(entries);
        if (!string.Equals(scope.RepositoryId, repositoryId, StringComparison.Ordinal))
            throw new ArgumentException("analysis scope belongs to a different repository", nameof(scope));

        var verified = new List<CompilationUniverseEntry>();
        var units = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            ArgumentNullException.ThrowIfNull(entry);
            var unit = entry.ProjectUnitId.Replace('\\', '/');
            if (!units.Add(unit))
                throw new ArgumentException("duplicate compilation context for project unit '" + unit + "'");
            var descriptor = CompilationProvenance.Describe(repositoryId, unit, entry.Compilation, entry.Sources)
                .Verified();
            verified.Add(new CompilationUniverseEntry(descriptor, entry.Compilation));
        }

        var ordered = verified
            .OrderBy(entry => entry.Descriptor.ProjectUnitId, StringComparer.Ordinal)
            .ToArray();
        var id = CanonicalPrefix + ProvenanceHashing.Hash(Domain + "-universe",
            [repositoryId, CompilationProvenance.ContractVersion, scope.ScopeId,
             .. ordered.Select(entry => entry.Descriptor.DescriptorDigest)]);
        return new CompilationUniverse(repositoryId, CompilationProvenance.ContractVersion, scope, ordered, id);
    }

    public IReadOnlyList<string> MissingScopeUnits()
    {
        var resolved = Entries.Select(entry => entry.Descriptor.ProjectUnitId)
            .ToHashSet(StringComparer.Ordinal);
        return Scope.DeclaredUnits.Select(unit => unit.ProjectUnitId)
            .Where(unit => !resolved.Contains(unit))
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Verifies that <paramref name="target"/> is bound to exactly one live
    /// compilation inside a complete declared scope. Completeness is checked
    /// before binding so that an incomplete universe can never confirm.
    /// </summary>
    public UniverseVerification Verify(IMethodSymbol target, CSharpCompilation? caller = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (!string.Equals(ContractVersion, CompilationProvenance.ContractVersion, StringComparison.Ordinal))
            return new UniverseVerification(UniverseContextStatus.UnsupportedContextVersion,
                $"universe contract '{ContractVersion}' is not '{CompilationProvenance.ContractVersion}'");

        var missing = MissingScopeUnits();
        if (missing.Count > 0)
            return new UniverseVerification(UniverseContextStatus.Incomplete,
                "declared project units without a verified compilation context: " + string.Join(", ", missing));

        var binding = Entries.Where(entry => Binds(entry, target, caller)).ToArray();
        if (binding.Length == 0)
            return new UniverseVerification(UniverseContextStatus.MissingDefiningCompilation,
                "no verified compilation in the declared universe is the symbol's defining assembly");
        if (binding.Length > 1)
            return new UniverseVerification(UniverseContextStatus.Ambiguous,
                $"{binding.Length} verified compilations bind the same target symbol");

        var entry = binding[0];
        if (!Scope.UnitIds.Contains(entry.Descriptor.ProjectUnitId))
            return new UniverseVerification(UniverseContextStatus.UnsupportedExternalEvidence,
                $"defining project unit '{entry.Descriptor.ProjectUnitId}' is outside the declared analysis scope");
        return new UniverseVerification(UniverseContextStatus.Complete,
            "verified defining compilation context '" + entry.Descriptor.ProjectUnitId + "'", entry);
    }

    private static bool Binds(
        CompilationUniverseEntry entry, IMethodSymbol target, CSharpCompilation? caller)
    {
        if (entry.Compilation is null) return false;
        if (SymbolEqualityComparer.Default.Equals(entry.Compilation.Assembly, target.ContainingAssembly))
            return true;
        if (caller is null) return false;
        var targetMetadata = target.ContainingAssembly is { } assembly
            ? CompilationProvenance.MetadataIdentity(assembly.Identity)
            : "(unknown)";
        return caller.References
            .OfType<CompilationReference>()
            .Any(reference =>
                ReferenceEquals(reference.Compilation, entry.Compilation) &&
                string.Equals(
                    CompilationProvenance.MetadataIdentity(reference.Compilation.Assembly.Identity),
                    targetMetadata, StringComparison.Ordinal));
    }
}

/// <summary>Construction of verified compilation descriptors from live compilations.</summary>
public static class CompilationProvenance
{
    public const string ContractVersion = "compctx1";

    public static string MetadataIdentity(IAssemblySymbol? assembly) =>
        assembly is null ? "(unknown)" : MetadataIdentity(assembly.Identity);

    public static string MetadataIdentity(AssemblyIdentity identity)
    {
        var publicKeyToken = identity.PublicKeyToken.IsDefaultOrEmpty
            ? string.Empty
            : Convert.ToHexStringLower(identity.PublicKeyToken.ToArray());
        return string.Join("|", identity.Name, identity.Version.ToString(),
            identity.CultureName ?? string.Empty, publicKeyToken);
    }

    public static CompilationDescriptor Describe(
        string repositoryId,
        string projectUnitId,
        CSharpCompilation compilation,
        IReadOnlyCollection<(string Path, byte[] Bytes)>? sourceBytes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryId);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectUnitId);
        ArgumentNullException.ThrowIfNull(compilation);

        var bytesByPath = sourceBytes is null
            ? null
            : sourceBytes
                .GroupBy(item => ProvenanceHashing.NormalizePath(item.Path), StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First().Bytes, StringComparer.Ordinal);

        var sources = compilation.SyntaxTrees
            .Select(tree =>
            {
                var path = ProvenanceHashing.NormalizePath(tree.FilePath);
                var hash = bytesByPath is not null && bytesByPath.TryGetValue(path, out var bytes)
                    ? ProvenanceHashing.Sha256Hex(bytes)
                    : ProvenanceHashing.Sha256Hex(Encoding.UTF8.GetBytes(tree.GetText().ToString()));
                var parse = tree.Options as CSharpParseOptions;
                var symbols = parse?.PreprocessorSymbolNames is null
                    ? string.Empty
                    : string.Join(",", parse.PreprocessorSymbolNames.Order(StringComparer.Ordinal));
                return new SourceInputIdentity(path, "sha256:" + hash,
                    (parse?.LanguageVersion ?? LanguageVersion.Default).ToString(), symbols);
            })
            .OrderBy(source => source.Path, StringComparer.Ordinal)
            .ThenBy(source => source.ContentHash, StringComparer.Ordinal)
            .ToArray();

        var references = compilation.References
            .Select(ReferenceIdentity)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        var descriptor = new CompilationDescriptor(
            ContractVersion,
            repositoryId,
            projectUnitId.Replace('\\', '/'),
            compilation.AssemblyName ?? "(anonymous)",
            MetadataIdentity(compilation.Assembly.Identity),
            compilation.Assembly.Modules.FirstOrDefault()?.Name ?? "(unknown)",
            OptionsDigest(compilation.Options),
            sources,
            references,
            string.Empty);
        return descriptor with { DescriptorDigest = descriptor.ComputeDigest() };
    }

    private static string OptionsDigest(CompilationOptions options)
    {
        var specific = (options as CSharpCompilationOptions)?.SpecificDiagnosticOptions is { } map
            ? string.Join(",", map.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => pair.Key + "=" + pair.Value))
            : string.Empty;
        return ProvenanceHashing.Hash("compctx1-options",
        [
            options.OutputKind.ToString(),
            options.NullableContextOptions.ToString(),
            (options as CSharpCompilationOptions)?.AllowUnsafe.ToString() ?? string.Empty,
            options.OptimizationLevel.ToString(),
            options.Platform.ToString(),
            options.CheckOverflow.ToString(),
            options.Deterministic.ToString(),
            options.WarningLevel.ToString(),
            options.GeneralDiagnosticOption.ToString(),
            specific,
        ]);
    }

    private static string ReferenceIdentity(MetadataReference reference) => reference switch
    {
        CompilationReference compilation => CompilationReferenceIdentity(compilation),
        PortableExecutableReference executable when executable.FilePath is { Length: > 0 } path &&
                                                     File.Exists(path) =>
            "file|" + ProvenanceHashing.NormalizePath(path) + "|" +
            ProvenanceHashing.Sha256Hex(File.ReadAllBytes(path)),
        _ => "metadata|" + reference.Display,
    };

    private static string CompilationReferenceIdentity(CompilationReference reference)
    {
        var compilation = reference.Compilation;
        var trees = string.Join(",", compilation.SyntaxTrees
            .OrderBy(tree => tree.FilePath, StringComparer.Ordinal)
            .Select(tree =>
                ProvenanceHashing.NormalizePath(tree.FilePath) + "=" +
                ProvenanceHashing.Sha256Hex(Encoding.UTF8.GetBytes(tree.GetText().ToString()))));
        return "compilation|" + compilation.AssemblyName + "|" +
               MetadataIdentity(compilation.Assembly.Identity) + "|" + trees;
    }
}
