using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace SemanticCorpusOracle;

/// <summary>One declared project unit and the inputs used to construct it.</summary>
public sealed record ProjectBuild(
    string UnitId,
    string AssemblyName,
    IReadOnlyList<string> SourcePaths,
    IReadOnlyDictionary<string, byte[]> SourceBytes,
    CSharpCompilation Compilation,
    string[] PreprocessorSymbols)
{
    public string MetadataIdentity => string.Join("|",
        Compilation.Assembly.Identity.Name,
        Compilation.Assembly.Identity.Version.ToString(),
        Compilation.Assembly.Identity.CultureName ?? string.Empty,
        Compilation.Assembly.Identity.PublicKeyToken.IsDefaultOrEmpty
            ? string.Empty
            : Convert.ToHexStringLower(Compilation.Assembly.Identity.PublicKeyToken.AsSpan()));

    public string ModuleName => Compilation.Assembly.Modules.First().Name;

    public IReadOnlyList<R2Identity.SourceIdentity> SourceIdentities =>
        SourcePaths.Order(StringComparer.Ordinal).Select(path =>
        {
            var parse = (CSharpParseOptions)Compilation.SyntaxTrees.Single(t => t.FilePath == path).Options;
            return new R2Identity.SourceIdentity(
                path,
                "sha256:" + R2Identity.Sha256Hex(SourceBytes[path]),
                parse.LanguageVersion.ToString(),
                string.Join(",", parse.PreprocessorSymbolNames.Order(StringComparer.Ordinal)));
        }).ToArray();

    public IEnumerable<(string Path, string TextHash)> TreeTextHashes =>
        Compilation.SyntaxTrees.OrderBy(t => t.FilePath, StringComparer.Ordinal)
            .Select(t => (t.FilePath, R2Identity.Sha256Hex(System.Text.Encoding.UTF8.GetBytes(t.GetText().ToString()))));
}

/// <summary>A named variation of the frozen corpus inputs.</summary>
public sealed record CorpusVariant(
    string Name,
    string AppSitesLogicalPath,
    string AppSitesPhysicalPath,
    string CoreSymbols,
    string LeafSymbols,
    bool IncludeCoreReference,
    bool IncludeLeafReference)
{
    public static readonly CorpusVariant Baseline =
        new("baseline", "App/Sites.cs", "repo/App/Sites.cs", "", "", true, true);
    public static readonly CorpusVariant SourceChanged =
        new("source-changed", "App/Sites.cs", "variants/source-changed/App/Sites.cs", "", "", true, true);
    public static readonly CorpusVariant SymbolsCoreChanged =
        new("symbols-core-changed", "App/Sites.cs", "repo/App/Sites.cs", "CORE_FEATURE", "", true, true);
    public static readonly CorpusVariant SymbolsLeafChanged =
        new("symbols-leaf-changed", "App/Sites.cs", "repo/App/Sites.cs", "", "LEAF_TRACE", true, true);
    public static readonly CorpusVariant ReferenceMissing =
        new("reference-missing", "App/Sites.cs", "repo/App/Sites.cs", "", "", false, true);
}

/// <summary>A constructed corpus for one variant: all project builds plus the external image.</summary>
public sealed class CorpusBuild
{
    public const string RepoId = "corpus:s2.0";
    public const string PluginId = "corpus.s2.0.oracle";

    private CorpusBuild(CorpusVariant variant, IReadOnlyDictionary<string, ProjectBuild> projects,
        R2Identity.CompilationDescriptor externalDescriptor, byte[] externalAssembly,
        IReadOnlyList<string> frameworkIdentities, string externalReferenceIdentity)
    {
        Variant = variant;
        Projects = projects;
        ExternalDescriptor = externalDescriptor;
        ExternalAssembly = externalAssembly;
        FrameworkIdentities = frameworkIdentities;
        ExternalReferenceIdentity = externalReferenceIdentity;
    }

    public CorpusVariant Variant { get; }
    public IReadOnlyDictionary<string, ProjectBuild> Projects { get; }
    public R2Identity.CompilationDescriptor ExternalDescriptor { get; }
    public byte[] ExternalAssembly { get; }
    public IReadOnlyList<string> FrameworkIdentities { get; }
    public string ExternalReferenceIdentity { get; }

    public ProjectBuild Get(string unitId) => Projects[unitId];

    public static CorpusBuild Build(string corpusRoot, CorpusVariant variant)
    {
        var corelib = typeof(object).Assembly.Location;
        var runtimeDir = Path.GetDirectoryName(corelib)!;

        // Portable, content-bound reference identities (no absolute host paths).
        var framework = new (MetadataReference Reference, string Identity)[]
        {
            FrameworkReference(Path.Combine(runtimeDir, "System.Runtime.dll")),
            FrameworkReference(corelib),
            FrameworkReference(Path.Combine(runtimeDir, "Microsoft.CSharp.dll")),
        };
        var frameworkReferences = framework.Select(f => f.Reference).ToArray();

        ProjectBuild Compile(string unitId, string assemblyName, (string Logical, string Physical)[] sources,
            string symbols, MetadataReference[] projectReferences)
        {
            var parse = new CSharpParseOptions(LanguageVersion.CSharp14, DocumentationMode.Diagnose,
                SourceCodeKind.Regular,
                string.IsNullOrEmpty(symbols) ? Array.Empty<string>() : symbols.Split(';'));
            var bytes = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            var trees = new List<SyntaxTree>();
            foreach (var (logical, physical) in sources.OrderBy(s => s.Logical, StringComparer.Ordinal))
            {
                var content = File.ReadAllBytes(Path.Combine(corpusRoot, physical));
                bytes[logical] = content;
                trees.Add(CSharpSyntaxTree.ParseText(SourceText.From(new MemoryStream(content, writable: false)), parse, logical));
            }

            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Release,
                nullableContextOptions: NullableContextOptions.Disable,
                deterministic: true,
                concurrentBuild: false);
            var references = frameworkReferences.Concat(projectReferences).ToArray();
            var compilation = CSharpCompilation.Create(assemblyName, trees, references, options);
            return new ProjectBuild(unitId, assemblyName, bytes.Keys.Order(StringComparer.Ordinal).ToArray(),
                bytes, compilation, parse.PreprocessorSymbolNames.Order(StringComparer.Ordinal).ToArray());
        }

        var leaf = Compile("Leaf/Leaf.csproj", "Corpus.Leaf", [("Leaf/Helper.cs", "repo/Leaf/Helper.cs")],
            variant.LeafSymbols, []);

        var core = Compile("Core/Core.csproj", "Corpus.Core",
            [
                ("Core/Types.cs", "repo/Core/Types.cs"),
                ("Core/Methods.cs", "repo/Core/Methods.cs"),
                ("Core/Extensions.cs", "repo/Core/Extensions.cs"),
                ("Core/LeafUser.cs", "repo/Core/LeafUser.cs"),
            ], variant.CoreSymbols, [leaf.Compilation.ToMetadataReference()]);

        var appReferences = new List<MetadataReference>();
        if (variant.IncludeCoreReference)
            appReferences.Add(core.Compilation.ToMetadataReference());
        if (variant.IncludeLeafReference)
            appReferences.Add(leaf.Compilation.ToMetadataReference());
        var app = Compile("App/App.csproj", "Corpus.App",
            [(variant.AppSitesLogicalPath, variant.AppSitesPhysicalPath), ("App/Bulk.cs", "repo/App/Bulk.cs")],
            "", appReferences.ToArray());

        var solo = Compile("Solo/Solo.csproj", "Corpus.Solo",
            [("Solo/Declarations.cs", "repo/Solo/Declarations.cs")], "", []);

        var externalSource = File.ReadAllBytes(Path.Combine(corpusRoot, "repo/External/ExternalLib.cs"));
        var externalTree = CSharpSyntaxTree.ParseText(
            SourceText.From(new MemoryStream(externalSource, writable: false)),
            new CSharpParseOptions(LanguageVersion.CSharp14, DocumentationMode.Diagnose, SourceCodeKind.Regular),
            "External/ExternalLib.cs");
        var externalCompilation = CSharpCompilation.Create("Corpus.External", [externalTree],
            frameworkReferences,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, deterministic: true, concurrentBuild: false));
        using var externalImage = new MemoryStream();
        var emit = externalCompilation.Emit(externalImage);
        if (!emit.Success)
            throw new InvalidOperationException("External image failed to emit: " + string.Join("; ", emit.Diagnostics));
        var externalAssembly = externalImage.ToArray();
        var externalReference = MetadataReference.CreateFromImage(externalAssembly);

        var negatives = Compile("Negatives/Negatives.csproj", "Corpus.Negatives",
            [
                ("Negatives/NegativeSites.cs", "repo/Negatives/NegativeSites.cs"),
                ("Negatives/External.cs", "repo/Negatives/External.cs"),
            ], "", [core.Compilation.ToMetadataReference(), externalReference]);

        var projects = new Dictionary<string, ProjectBuild>(StringComparer.Ordinal)
        {
            [leaf.UnitId] = leaf,
            [core.UnitId] = core,
            [app.UnitId] = app,
            [solo.UnitId] = solo,
            [negatives.UnitId] = negatives,
        };

        var externalReferenceIdentity =
            R2Identity.MetadataReferenceItem("external/ExternalLib.dll", R2Identity.Sha256Hex(externalAssembly));
        var externalDescriptor = new R2Identity.CompilationDescriptor(R2Identity.Compctx2, RepoId,
            "(external)", "Corpus.External", "Corpus.External|0.0.0.0||", "Corpus.External.dll", "",
            [], [externalReferenceIdentity]);

        return new CorpusBuild(variant, projects, externalDescriptor, externalAssembly,
            framework.Select(f => f.Identity).ToArray(), externalReferenceIdentity);
    }

    private static (MetadataReference Reference, string Identity) FrameworkReference(string path)
    {
        var reference = MetadataReference.CreateFromFile(path);
        var identity = R2Identity.MetadataReferenceItem("runtime/" + Path.GetFileName(path),
            R2Identity.Sha256Hex(File.ReadAllBytes(path)));
        return (reference, identity);
    }
}
