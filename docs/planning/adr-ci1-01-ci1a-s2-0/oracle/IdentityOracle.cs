using Microsoft.CodeAnalysis;

namespace SemanticCorpusOracle;

/// <summary>
/// Independently computes the R2-faithful legacy <c>compctx1</c> and the
/// verified dependency-sensitive <c>compctx2</c> for every declared project,
/// plus <c>asmctx3</c> and <c>usage4</c> from explicit inputs. It does not call
/// Andirit production identity code.
/// </summary>
public sealed class CorpusIdentity
{
    private readonly Dictionary<string, ProjectBuild> _projects;

    private CorpusIdentity(CorpusBuild build,
        Dictionary<string, string> compctx1, Dictionary<string, string> compctx2)
    {
        _projects = build.Projects.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal);
        Compctx1 = compctx1;
        Compctx2 = compctx2;
    }

    public IReadOnlyDictionary<string, string> Compctx1 { get; }
    public IReadOnlyDictionary<string, string> Compctx2 { get; }

    public static CorpusIdentity Compute(CorpusBuild build)
    {
        var projects = build.Projects;
        // Deterministic topological order: dependencies before dependents.
        var order = new[] { "Leaf/Leaf.csproj", "Core/Core.csproj", "App/App.csproj", "Solo/Solo.csproj", "Negatives/Negatives.csproj" };
        var compctx1 = new Dictionary<string, string>(StringComparer.Ordinal);
        var compctx2 = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var unit in order)
        {
            if (!projects.ContainsKey(unit))
                continue;
            var project = projects[unit];
            var deps = project.Compilation.References.OfType<CompilationReference>()
                .Select(r => projects.Values.Single(p => ReferenceEquals(p.Compilation, r.Compilation)))
                .ToArray();

            // The external in-memory image is the only metadata reference with no file path.
            var hasExternalImage = project.Compilation.References.OfType<PortableExecutableReference>()
                .Any(r => r.FilePath is null);

            var legacyRefs = new List<string>(build.FrameworkIdentities);
            foreach (var dep in deps)
                legacyRefs.Add(R2Identity.R2ProjectReferenceItem(dep.AssemblyName, dep.MetadataIdentity, dep.TreeTextHashes));
            if (hasExternalImage)
                legacyRefs.Add(build.ExternalReferenceIdentity);

            var verifiedRefs = new List<string>(build.FrameworkIdentities);
            foreach (var dep in deps)
                verifiedRefs.Add(R2Identity.VerifiedProjectReferenceItem(compctx2[dep.UnitId]));
            if (hasExternalImage)
                verifiedRefs.Add(build.ExternalReferenceIdentity);

            compctx1[unit] = Describe(R2Identity.Compctx1, project, legacyRefs).ComputeDigest();
            compctx2[unit] = Describe(R2Identity.Compctx2, project, verifiedRefs).ComputeDigest();
        }

        return new CorpusIdentity(build, compctx1, compctx2);
    }

    private static R2Identity.CompilationDescriptor Describe(string version, ProjectBuild project, List<string> references) =>
        new(version, CorpusBuild.RepoId, project.UnitId, project.AssemblyName, project.MetadataIdentity,
            project.ModuleName, R2Identity.OptionsDigest((Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions)project.Compilation.Options),
            project.SourceIdentities,
            references.Order(StringComparer.Ordinal).ToArray());

    // ---- target and occurrence identities -----------------------------------

    public string TargetId(string definingUnit, string identifierForm, string declarationForm, string shape)
    {
        var project = _projects[definingUnit];
        var identifier = R2Identity.IdentifierDigest([identifierForm]);
        var declaration = R2Identity.DeclarationDigest([declarationForm]);
        var shapeDigest = R2Identity.ShapeDigest(shape);
        return R2Identity.TargetId(Compctx1[definingUnit], project.MetadataIdentity, project.ModuleName,
            identifier, declaration, shapeDigest);
    }

    public string OccurrenceId(string semanticUnit, string path, string sourceHash,
        int startLine, int startColumn, int endLine, int endColumn, string targetDocumentId, string asmctx3) =>
        R2Identity.OccurrenceId(CorpusBuild.RepoId, CorpusBuild.PluginId, semanticUnit, path, sourceHash,
            startLine, startColumn, endLine, endColumn, targetDocumentId, asmctx3);

    public string SourceHash(string unitId, string logicalPath) =>
        "sha256:" + R2Identity.Sha256Hex(_projects[unitId].SourceBytes[logicalPath]);
}
