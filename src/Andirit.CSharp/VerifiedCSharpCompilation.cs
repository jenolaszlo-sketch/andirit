using System.Collections.Immutable;
using System.Security.Cryptography;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Andirit;

internal sealed class VerifiedMetadataReference
{
    private VerifiedMetadataReference(PortableExecutableReference reference, string identity)
    { Reference = reference; Identity = identity; }
    internal PortableExecutableReference Reference { get; }
    internal string Identity { get; }
    internal static VerifiedMetadataReference Capture(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var fullPath = Path.GetFullPath(path);
        using var stream = File.OpenRead(fullPath);
        if (stream.Length > 128 * 1024 * 1024)
            throw new InvalidDataException("Metadata reference exceeds 128 MiB bound.");
        using var output = new MemoryStream();
        var buffer = new byte[81920];
        int count;
        while ((count = stream.Read(buffer)) != 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (output.Length + count > 128 * 1024 * 1024)
                throw new InvalidDataException("Metadata reference exceeds 128 MiB bound.");
            output.Write(buffer, 0, count);
        }
        var image = ImmutableArray.Create(output.ToArray());
        var digest = Convert.ToHexStringLower(SHA256.HashData(image.AsSpan()));
        PortableExecutableReference reference;
        try
        {
            reference = MetadataReference.CreateFromImage(image, filePath: fullPath);
            // Materialize assembly metadata and validate the bounded Q-F3 name/signature
            // surface before returning identity. This does not verify all PE sections,
            // metadata tables, IL bodies or successful semantic binding.
            using var metadata = reference.GetMetadata();
            if (metadata is not AssemblyMetadata assembly)
                throw new InvalidDataException("UnsupportedMetadataReference");
            foreach (var module in assembly.GetModules())
            {
                var reader = module.GetMetadataReader();
                // A netmodule has no assembly manifest and must not back a
                // captured assembly identity.
                if (!reader.IsAssembly)
                    throw new InvalidDataException("UnsupportedMetadataReference");
                _ = reader.GetAssemblyDefinition();
                MetadataReferenceValidation.Validate(reader, cancellationToken);
            }
        }
        catch (Exception exception) when (exception is BadImageFormatException or InvalidOperationException)
        {
            throw new InvalidDataException("UnsupportedMetadataReference", exception);
        }
        cancellationToken.ThrowIfCancellationRequested();
        return new(reference, "file|" + fullPath.Replace('\\', '/') + "|" + digest);
    }
}

internal sealed class VerifiedCSharpCompilation
{
    private VerifiedCSharpCompilation(CSharpCompilation compilation,
        CompilationProvenanceDescriptor descriptor, ImmutableArray<VerifiedCSharpSource> sources,
        ImmutableArray<VerifiedCSharpCompilation> dependencies)
    { Compilation = compilation; Descriptor = descriptor; Sources = sources; Dependencies = dependencies; }
    internal CSharpCompilation Compilation { get; }
    internal CompilationProvenanceDescriptor Descriptor { get; }
    internal ImmutableArray<VerifiedCSharpSource> Sources { get; }
    internal ImmutableArray<VerifiedCSharpCompilation> Dependencies { get; }
    // R2 stays exact where no CompilationReference occurs. Never upgrade a relayed descriptor to live proof.
    internal CompilationProvenanceDescriptor? LegacyDescriptor => Dependencies.IsEmpty
        ? Legacy(Descriptor) : null;
    private static CompilationProvenanceDescriptor Legacy(CompilationProvenanceDescriptor descriptor)
    {
        var old = descriptor with { ContractVersion = "compctx1", DescriptorDigest = string.Empty };
        return old with { DescriptorDigest = old.ComputeDigest() };
    }

    internal static VerifiedCSharpCompilation Create(string repositoryId, string projectUnitId,
        CSharpCompilation compilation, IReadOnlyCollection<VerifiedCSharpSource> sources,
        IReadOnlyCollection<VerifiedMetadataReference> metadata,
        IReadOnlyCollection<VerifiedCSharpCompilation> dependencies,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryId);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectUnitId);
        projectUnitId = projectUnitId.Replace('\\', '/');
        var accepted = sources.ToArray();
        if (accepted.Any(s => s.Refusal is not null))
            throw new InvalidDataException("UnverifiedSource:" + accepted.First(s => s.Refusal is not null).Refusal);
        if (accepted.Select(s => s.Path).Distinct(CSharpProjectDiscovery.PathComparer).Count() != accepted.Length)
            throw new InvalidDataException("DuplicateSourcePath");
        var byPath = accepted.ToDictionary(s => s.Path, StringComparer.Ordinal);
        var trees = compilation.SyntaxTrees.ToArray();
        if (trees.Length != accepted.Length || trees.Select(t => t.FilePath).Distinct(StringComparer.Ordinal).Count() != trees.Length)
            throw new InvalidDataException("SourceMembershipMismatch");
        var identities = new List<CompilationSourceIdentity>();
        foreach (var tree in trees)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!byPath.TryGetValue(tree.FilePath, out var source) ||
                !tree.GetText(cancellationToken).ContentEquals(source.Text))
                throw new InvalidDataException("RoslynTextMismatch:" + tree.FilePath);
            if (tree.Options is not CSharpParseOptions parse || parse.Kind != SourceCodeKind.Regular ||
                parse.DocumentationMode != DocumentationMode.Diagnose || parse.Features.Any() || tree.FilePath.Contains('|') ||
                // A-01: preprocessor symbols are joined with ',' in the canonical
                // source item, so a symbol containing ',' or '|' could collide with
                // the field separators. Valid C# symbols are identifiers and cannot
                // contain either character.
                parse.PreprocessorSymbolNames.Any(symbol => symbol.Contains(',') || symbol.Contains('|')))
                throw new InvalidDataException("UnsupportedParseConfiguration");
            identities.Add(new(tree.FilePath.Replace('\\', '/'), source.ContentHash,
                parse.LanguageVersion.ToString(), string.Join(",", parse.PreprocessorSymbolNames.Order(StringComparer.Ordinal))));
        }
        var refs = compilation.References.ToArray();
        if (refs.Length != metadata.Count + dependencies.Count)
            throw new InvalidDataException("ReferenceMembershipMismatch");
        if (dependencies.Select(d => d.Descriptor.ProjectUnitId).Distinct(StringComparer.Ordinal).Count() != dependencies.Count)
            throw new InvalidDataException("ConflictingProjectIdentity");
        // Reject semantic project cycles/conflicting revisions even if the live object graph
        // consists of distinct immutable snapshots. A shared verified DAG node is reusable.
        var closure = new Dictionary<string, VerifiedCSharpCompilation>(StringComparer.Ordinal);
        var pending = new Stack<VerifiedCSharpCompilation>(dependencies);
        while (pending.TryPop(out var dependency))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var unit = dependency.Descriptor.ProjectUnitId;
            if (unit == projectUnitId) throw new InvalidDataException("CyclicProjectDependency");
            if (closure.TryGetValue(unit, out var prior))
            {
                if (!ReferenceEquals(prior, dependency)) throw new InvalidDataException("ConflictingDependencyContext");
                continue;
            }
            closure.Add(unit, dependency);
            foreach (var child in dependency.Dependencies) pending.Push(child);
        }
        var referenceIdentities = new List<string>();
        var used = new HashSet<MetadataReference>(ReferenceEqualityComparer.Instance);
        foreach (var reference in refs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!used.Add(reference) || reference.Properties.Kind != MetadataImageKind.Assembly ||
                !reference.Properties.Aliases.IsDefaultOrEmpty || reference.Properties.EmbedInteropTypes)
                throw new InvalidDataException("UnsupportedReferenceProperties");
            if (reference is CompilationReference project)
            {
                var matches = dependencies.Where(d => ReferenceEquals(d.Compilation, project.Compilation)).ToArray();
                if (matches.Length != 1 || matches[0].Descriptor.RepositoryId != repositoryId ||
                    matches[0].Descriptor.ContractVersion != "compctx2" || matches[0].Descriptor.ProjectUnitId == projectUnitId)
                    throw new InvalidDataException("MissingDefiningCompilation");
                matches[0].Descriptor.Verified();
                referenceIdentities.Add("compilation-context|" + matches[0].Descriptor.DescriptorDigest);
            }
            else
            {
                var matches = metadata.Where(m => ReferenceEquals(m.Reference, reference)).ToArray();
                if (matches.Length != 1) throw new InvalidDataException("UnverifiedMetadataReference");
                referenceIdentities.Add(matches[0].Identity);
            }
        }
        // Unrepresented semantic options cannot silently share a canonical context. These fixed
        // defaults are the established production path; the material R2 fields remain configurable.
        var options = compilation.Options;
        var baseline = new CSharpCompilationOptions(options.OutputKind)
            .WithNullableContextOptions(options.NullableContextOptions).WithAllowUnsafe(options.AllowUnsafe)
            .WithOptimizationLevel(options.OptimizationLevel).WithPlatform(options.Platform)
            .WithOverflowChecks(options.CheckOverflow).WithDeterministic(options.Deterministic)
            .WithWarningLevel(options.WarningLevel).WithGeneralDiagnosticOption(options.GeneralDiagnosticOption)
            .WithSpecificDiagnosticOptions(options.SpecificDiagnosticOptions).WithConcurrentBuild(options.ConcurrentBuild);
        if (!options.Equals(baseline) || options.SpecificDiagnosticOptions.Keys.Any(k => k.Contains(',') || k.Contains('=')))
            throw new InvalidDataException("UnsupportedCompilationConfiguration");
        var descriptor = new CompilationProvenanceDescriptor("compctx2", repositoryId,
            projectUnitId.Replace('\\', '/'), compilation.AssemblyName ?? throw new InvalidDataException("MissingAssemblyName"),
            ProvenanceHash.Metadata(compilation.Assembly.Identity), compilation.Assembly.Modules.First().Name,
            ProvenanceHash.Options(options), identities.OrderBy(s => s.Path, StringComparer.Ordinal)
                .ThenBy(s => s.ContentHash, StringComparer.Ordinal).ToImmutableArray(),
            referenceIdentities.Order(StringComparer.Ordinal).ToImmutableArray(), string.Empty);
        descriptor = descriptor with { DescriptorDigest = descriptor.ComputeDigest() };
        cancellationToken.ThrowIfCancellationRequested();
        return new(compilation, descriptor.Verified(), accepted.ToImmutableArray(), dependencies.ToImmutableArray());
    }

    internal void ValidateExpectedDescriptor(CompilationProvenanceDescriptor expected)
    {
        expected.Verified();
        if (expected.DescriptorDigest != Descriptor.DescriptorDigest)
            throw new InvalidDataException("StaleOrForgedCompilationDescriptor");
    }
}
