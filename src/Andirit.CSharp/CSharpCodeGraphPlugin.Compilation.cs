using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Andirit;

public sealed partial class CSharpCodeGraphPlugin
{
    private static readonly Lazy<IReadOnlyList<VerifiedMetadataReference>> PlatformReferenceEvidence =
        new(BuildPlatformReferenceEvidence, LazyThreadSafetyMode.ExecutionAndPublication);

    private static CodeLocation Location(string path, SyntaxNode syntax)
    {
        var span = syntax.GetLocation().GetLineSpan().Span;
        return new(
            path,
            span.Start.Line + 1,
            span.Start.Character + 1,
            span.End.Line + 1,
            span.End.Character + 1);
    }

    private static CSharpParseOptions CreateParseOptions(CSharpProjectModel project)
    {
        var languageVersion = LanguageVersion.Latest;
        if (!string.IsNullOrWhiteSpace(project.LanguageVersion) &&
            LanguageVersionFacts.TryParse(project.LanguageVersion, out var parsed))
        {
            languageVersion = parsed;
        }
        return new CSharpParseOptions(
            languageVersion,
            documentationMode: DocumentationMode.Diagnose,
            preprocessorSymbols: project.DefineConstants);
    }

    private static CSharpCompilationOptions CreateCompilationOptions(CSharpProjectModel project)
    {
        var nullable = project.Nullable?.ToLowerInvariant() switch
        {
            "enable" => NullableContextOptions.Enable,
            "annotations" => NullableContextOptions.Annotations,
            "warnings" => NullableContextOptions.Warnings,
            _ => NullableContextOptions.Disable
        };
        return new(
            OutputKind.DynamicallyLinkedLibrary,
            nullableContextOptions: nullable,
            deterministic: true,
            concurrentBuild: false);
    }

    private static IReadOnlyList<VerifiedMetadataReference> BuildPlatformReferenceEvidence()
    {
        var trustedAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        var paths = string.IsNullOrWhiteSpace(trustedAssemblies)
            ? new[] { typeof(object).Assembly.Location }
            : trustedAssemblies.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToArray();
        if (paths.Sum(path => new FileInfo(path).Length) > 512L * 1024 * 1024)
            throw new InvalidDataException("Platform references exceed the 512 MiB image bound.");
        return paths.Select(path => VerifiedMetadataReference.Capture(path)).ToArray();
    }

    internal static async ValueTask<VerifiedCSharpAnalysis> PrepareAnalysisAsync(
        CodeGraphPluginContext context, CancellationToken cancellationToken = default)
    {
        var sources = new List<VerifiedCSharpSource>();
        long standaloneTotal = 0;
        foreach (var source in context.Sources.OrderBy(s => s.Path, StringComparer.Ordinal))
        {
            var snapshot = await VerifiedCSharpSource.ReadAsync(source, cancellationToken).ConfigureAwait(false);
            if (source.MaterializedBytes is null)
            {
                standaloneTotal += snapshot.ByteLength;
                if (standaloneTotal > 512L * 1024 * 1024)
                    throw new InvalidDataException("Standalone C# analysis exceeds 512 MiB acquisition bound.");
            }
            sources.Add(snapshot);
        }
        var content = sources.ToDictionary(s => s.Path, s => s.Text.ToString(), StringComparer.Ordinal);
        var byPath = sources.ToDictionary(s => s.Path, StringComparer.Ordinal);
        var discovery = CSharpProjectDiscovery.Discover(content);
        var byProject = discovery.Projects.ToDictionary(p => p.Path, CSharpProjectDiscovery.PathComparer);
        var warnings = new HashSet<string>(discovery.Warnings, StringComparer.Ordinal);
        var prepared = new Dictionary<string, CSharpPreparedProject>(CSharpProjectDiscovery.PathComparer);
        var allRefusals = sources.Where(s => s.Refusal is not null).Select(s => s.Refusal!).ToList();
        if (sources.Select(s => s.Path).Distinct(CSharpProjectDiscovery.PathComparer).Count() != sources.Count)
            allRefusals.Add("ConflictingSourcePath");
        // A solution referencing an absent accepted project cannot claim scope completeness.
        if (discovery.Warnings.Contains("csharp.solution.missing-project"))
            allRefusals.Add("MissingDeclaredProject");
        foreach (var solution in sources.Where(s => s.Path.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase) ||
                     s.Path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)))
        {
            if (solution.Path.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase))
            {
                try { _ = XDocument.Parse(solution.Text.ToString()); }
                catch (System.Xml.XmlException) { allRefusals.Add("InvalidSolutionXml:" + solution.Path); }
            }
            foreach (var declared in CSharpProjectDiscovery.DeclaredSolutionProjects(solution.Path, solution.Text.ToString()))
                if (!content.Keys.Contains(declared, CSharpProjectDiscovery.PathComparer))
                    allRefusals.Add("MissingDeclaredProject:" + declared);
        }
        var ordered = OrderProjects(discovery.Projects, byProject, warnings);
        foreach (var project in ordered)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var parse = CreateParseOptions(project);
            var trees = project.SourcePaths.Where(byPath.ContainsKey)
                .Select(path => CSharpSyntaxTree.ParseText(byPath[path].Text, parse, path,
                    cancellationToken: cancellationToken)).ToArray();
            var metadata = PlatformReferenceEvidence.Value;
            var available = project.ProjectReferences.Where(prepared.ContainsKey)
                .ToDictionary(p => p, p => prepared[p].Compilation, CSharpProjectDiscovery.PathComparer);
            var refs = metadata.Select(m => (MetadataReference)m.Reference).ToList();
            refs.AddRange(available.Values.Select(c => c.ToMetadataReference()));
            var compilation = CSharpCompilation.Create(project.AssemblyName, trees, refs, CreateCompilationOptions(project));
            var refusals = new List<string>();
            if (byPath.TryGetValue(project.Path, out var projectSource) && projectSource.Refusal is not null)
                refusals.Add(projectSource.Refusal);
            if (project.ProjectReferences.Any(p => !prepared.ContainsKey(p) || prepared[p].Verified is null))
                refusals.Add("MissingOrCyclicProjectDependency:" + project.Path);
            if (project.ProjectReferences.Any(p => !byProject.ContainsKey(p)))
                warnings.Add("csharp.project.reference-missing");
            if (project.LanguageVersion is not null && !LanguageVersionFacts.TryParse(project.LanguageVersion, out _))
                refusals.Add("UnsupportedLanguageVersion:" + project.Path);
            if (project.Nullable is not null && project.Nullable.ToLowerInvariant() is not
                ("enable" or "disable" or "annotations" or "warnings"))
                refusals.Add("UnsupportedNullableConfiguration:" + project.Path);
            if (project.WarningCodes.Contains("csharp.project.invalid-xml"))
                refusals.Add("InvalidProjectXml:" + project.Path);
            if (project.PackageReferences.Count > 0)
                refusals.Add("UnsupportedPackageResolution:" + project.Path);
            if (content.TryGetValue(project.Path, out var xml) && !project.WarningCodes.Contains("csharp.project.invalid-xml"))
            {
                var document = XDocument.Parse(xml);
                if (document.Descendants().Any(e => e.Attribute("Condition") is not null ||
                    e.Name.LocalName is "Import" or "Reference" ||
                    e.Value.Contains("$(", StringComparison.Ordinal) || e.Value.Contains("@(", StringComparison.Ordinal) ||
                    e.Attributes().Any(a => a.Value.Contains("$(", StringComparison.Ordinal) || a.Value.Contains("@(", StringComparison.Ordinal))))
                    refusals.Add("UnsupportedProjectEvaluation:" + project.Path);
                foreach (var include in document.Descendants().Where(e => e.Name.LocalName == "Compile")
                    .Select(e => e.Attribute("Include")?.Value).Where(v => v is not null))
                {
                    foreach (var item in include!.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    {
                        if (item.IndexOfAny(['*', '?', '$', '@']) >= 0) continue;
                        var path = CSharpProjectDiscovery.Combine(project.Directory, item);
                        if (!byPath.ContainsKey(path)) refusals.Add("MissingDeclaredSource:" + path);
                    }
                }
            }
            VerifiedCSharpCompilation? verified = null;
            if (refusals.Count == 0)
            {
                try
                {
                    verified = VerifiedCSharpCompilation.Create(context.RepositoryId.Value,
                        CSharpProjectDiscovery.IndexUnitId(project.Path), compilation,
                        project.SourcePaths.Select(p => byPath[p]).ToArray(), metadata,
                        project.ProjectReferences.Select(p => prepared[p].Verified!).ToArray(), cancellationToken);
                }
                catch (InvalidDataException error) { refusals.Add(error.Message); }
            }
            allRefusals.AddRange(refusals);
            prepared.Add(project.Path, new(project, compilation, available, verified, refusals.ToImmutableArray()));
        }
        var scope = VerifiedCSharpScope.Declare(context.RepositoryId.Value, discovery.Projects.Select(p =>
            new CSharpScopeUnit(CSharpProjectDiscovery.IndexUnitId(p.Path), p.Path, p.SourcePaths.ToImmutableArray())));
        var universe = VerifiedCSharpUniverse.Create(context.RepositoryId.Value, scope,
            prepared.Values.Where(p => p.Verified is not null).Select(p => p.Verified!), allRefusals);
        cancellationToken.ThrowIfCancellationRequested();
        return new(ordered.Select(p => prepared[p.Path]).ToImmutableArray(), sources.ToImmutableArray(), universe,
            warnings.Order(StringComparer.Ordinal).ToImmutableArray());
    }
}
