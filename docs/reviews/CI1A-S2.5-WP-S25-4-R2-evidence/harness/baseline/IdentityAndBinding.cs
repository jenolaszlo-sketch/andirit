using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Andirit;

internal static partial class Probe
{
    private static void IdentityDiscriminators()
    {
        var expected = new Dictionary<string, string>();
        void Compare(string field, CodeRepositoryId repository, CodePluginId plugin, string owner,
            CodeLocation span, string hash, string document, CodeOccurrenceTargetId target)
        {
            var independent = Usage(repository.Value, plugin.Value, owner, span, hash, document, target.Value);
            var actual = CodeOccurrenceFact.Create(repository, target, document, owner, new(plugin, StorageA),
                CodeOccurrenceCategory.Invocation, span, hash, "site-payload").Id.Value;
            expected[field] = independent;
            Check(actual == independent, "R2 discriminator agrees for " + field);
        }

        Compare("canonical", UnionRepo, PluginA, SemanticOwner, SharedSite, SharedSourceHash, SharedDocument, UnionTarget);
        Compare("repository", new("review:other"), PluginA, SemanticOwner, SharedSite, SharedSourceHash, SharedDocument, UnionTarget);
        Compare("plugin", UnionRepo, PluginB, SemanticOwner, SharedSite, SharedSourceHash, SharedDocument, UnionTarget);
        Compare("semanticUnit", UnionRepo, PluginA, OtherSemanticOwner, SharedSite, SharedSourceHash, SharedDocument, UnionTarget);
        Compare("sourcePath", UnionRepo, PluginA, SemanticOwner, new("Controls/Other.cs", 1, 24, 1, 28),
            SharedSourceHash, SharedDocument, UnionTarget);
        Compare("sourceHash", UnionRepo, PluginA, SemanticOwner, SharedSite, FileDigest(Encoding.UTF8.GetBytes("delta")),
            SharedDocument, UnionTarget);
        Compare("start line", UnionRepo, PluginA, SemanticOwner, new(SharedSite.Path, 2, 24, 3, 28),
            SharedSourceHash, SharedDocument, UnionTarget);
        Compare("start column", UnionRepo, PluginA, SemanticOwner, new(SharedSite.Path, 1, 25, 1, 28),
            SharedSourceHash, SharedDocument, UnionTarget);
        Compare("end line", UnionRepo, PluginA, SemanticOwner, new(SharedSite.Path, 1, 24, 2, 28),
            SharedSourceHash, SharedDocument, UnionTarget);
        Compare("end column", UnionRepo, PluginA, SemanticOwner, new(SharedSite.Path, 1, 24, 1, 30),
            SharedSourceHash, SharedDocument, UnionTarget);
        Compare("target document", UnionRepo, PluginA, SemanticOwner, SharedSite, SharedSourceHash,
            "M:QualifierDependency.Api.Other", UnionTarget);
        Compare("defining context", UnionRepo, PluginA, SemanticOwner, SharedSite, SharedSourceHash,
            SharedDocument, new("asmctx3:sha256:" + new string('b', 64)));
        Check(expected.Values.Distinct().Count() == expected.Count, "Every distinct supported input changes its occurrence identity");
        Receipt["independentlyEncoded"] = expected;
        var fact = Fact(PluginA, StorageA, SemanticOwner, "original");
        var alternatePayload = Fact(PluginA, StorageB, SemanticOwner, "different");
        Check(fact.Id == alternatePayload.Id && fact.PayloadHash != alternatePayload.PayloadHash,
            "Storage contributor and site content omit identity and content affects payload");
        try
        {
            _ = new CodeOccurrenceFact(new(new string('0', 64)), fact.TargetId, fact.TargetDocumentId,
                fact.RepositoryId, fact.Category, fact.SourceUnitId, fact.Contributor,
                fact.Span, fact.SourceHash, fact.ContentHash);
            Check(false, "A structurally valid forged ID must be rejected");
        }
        catch (ArgumentException error)
        {
            Receipt["forgery"] = new { error.ParamName, error.Message };
            Check(error.ParamName == "id", "Canonical digest recomputation rejected the forged ID");
        }
    }

    private static async Task MultipleDefinitions(string phase, string database, string fixture)
    {
        var repo = new CodeRepositoryId("review:s254:two-definitions");
        var files = LoadControlFiles(fixture);
        foreach (var key in files.Keys.Where(key => key.StartsWith("Caller/Fixture", StringComparison.Ordinal)).ToArray())
            files.Remove(key);
        const string rival = "Rival/Rival.csproj";
        files["Caller/Only.cs"] = Encoding.UTF8.GetBytes(
            "namespace QualifierCaller; public static class Only { public static void Run() { QualifierDependency.Api.Mark(); } }");
        files[CallerProject] = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(files[CallerProject])
            .Replace("</ItemGroup>", "<ProjectReference Include=\"../Rival/Rival.csproj\" /></ItemGroup>", StringComparison.Ordinal));
        files[rival] = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(files[DependencyProject])
            .Replace("</PropertyGroup>", "<AssemblyName>Dependency</AssemblyName></PropertyGroup>", StringComparison.Ordinal));
        files["Rival/Api.cs"] = files[DependencyPath];
        Receipt["inputs"] = files.ToDictionary(pair => pair.Key, pair => Convert.ToBase64String(pair.Value));
        var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(Context(repo, "ambiguous", files));
        var caller = analysis.Projects.Single(project => project.Project.Path == CallerProject).Verified!;
        var defining = analysis.Projects.Single(project => project.Project.Path == DependencyProject).Verified!;
        var tree = caller.Compilation.SyntaxTrees.Single();
        var site = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
        var info = caller.Compilation.GetSemanticModel(tree).GetSymbolInfo(site);
        var symbol = info.Symbol as IMethodSymbol;
        var resolver = symbol is null ? null : analysis.Universe.ResolveDefining(symbol, null, caller.Compilation);
        var refs = caller.Compilation.References.OfType<CompilationReference>()
            .Select(reference => reference.Compilation.Assembly.Identity.ToString()).ToArray();
        Receipt["binding"] = new { symbol = symbol?.ToDisplayString(), resolution = resolver?.Status.ToString(),
            reason = info.CandidateReason.ToString(), refs };
        Check(refs.Length == 2 && refs.Distinct().Count() == 1 && resolver?.Status == VerifiedDefiningStatus.Ambiguous,
            "Only a distinct multiple-live-defining ambiguity satisfies this control");
        var query = VerifiedCSharpOccurrenceAuthority.Bind(
            new(repo, new(MarkTarget(repo.Value, defining, files[DependencyPath]))), analysis.Universe, caller.Compilation);
        using (var store = new LatticeCodeGraphStore(database))
        {
            if (phase.EndsWith("publish", StringComparison.Ordinal))
                await PublishCSharp(store, repo, "ambiguous", files);
            var result = await store.FindOccurrencesAsync(query);
            Receipt["result"] = result;
            Check(result.IsComplete && result.Coverage?.Ambiguous == 1 && result.Coverage?.External == 0 &&
                result.Coverage?.Unresolved == 0 && result.Occurrences.Count == 0,
                "Multiple verified live definitions produce one persisted ambiguity and no occurrence");
        }
        Receipt["native"] = ReadNative(database);
    }
}

    
internal static partial class Probe
{
    private static async Task CoverageBoundary(string phase, string database, string fixture)
    {
        var repo = new CodeRepositoryId("review:s254:partial-controls");
        var files = LoadControlFiles(fixture);
        var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(Context(repo, "coverage", files));
        var caller = analysis.Projects.Single(project => project.Project.Path == CallerProject).Verified!;
        var foreign = CSharpCompilation.Create("MissingFromDeclaredScope",
            [CSharpSyntaxTree.ParseText("namespace Missing; public static class Definition { public static int Absent() => 4; }")]);
        var symbol = (IMethodSymbol)foreign.GetTypeByMetadataName("Missing.Definition")!.GetMembers("Absent").Single();
        var missing = analysis.Universe.ResolveDefining(symbol, null, caller.Compilation);
        var reference = caller.Compilation.GetMetadataReference(symbol.ContainingAssembly);
        Receipt["missingLiveDefinition"] = new { completeScope = analysis.Universe.IsComplete,
            target = symbol.ToDisplayString(), assembly = symbol.ContainingAssembly.Identity.ToString(),
            status = missing.Status.ToString(), missing.Detail, hasReference = reference is not null };
        Check(analysis.Universe.IsComplete && reference is null &&
            missing.Status == VerifiedDefiningStatus.MissingDefiningCompilation,
            "An unrelated source symbol with no matching live compilation or PE produces MissingDefiningCompilation");
        using (var store = new LatticeCodeGraphStore(database))
        {
            if (phase.EndsWith("publish", StringComparison.Ordinal))
                await Run(store, repo, "partial", [PluginA], async () =>
                {
                    var origin = new CodeFactOrigin(repo, PluginA, "1.0.0", new("partial"), new("review:partial-unit"));
                    await store.StageIndexUnitAsync(new(origin, occurrenceAnalysisIncluded: true,
                        occurrenceCoverage: new(0, 0, CodeRelationshipCoverageState.Partial,
                            analysis.Universe.Scope.Identity, analysis.Universe.Identity, 2, 1, unresolved: 1)));
                });
            var result = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, UnionTarget));
            Receipt["constructedResult"] = result;
            Check(result.Availability == CodeOccurrenceAvailability.IncompleteAnalysis && !result.IsComplete &&
                result.Occurrences.Count == 0 && result.Coverage?.Unresolved == 1 && result.Coverage?.External == 0,
                "A constructed partial-analysis storage control remains incomplete with one unresolved site and no fact");
        }
        Receipt["native"] = ReadNative(database);
    }
}
