using System.Security.Cryptography;
using System.Text;
using Andirit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Frozen = AdrCi1Exp.Shared;

namespace Andirit.CSharp.Tests;

// WP-S24-2 P2/P4: authentic frozen-R2-corpus real-source qualification of the
// internal occurrence reader. Publishes the compiling corpus projects through the
// real C# plugin, then paginates real cross-project targets. Expected spans and
// ordering are derived independently from the live Roslyn syntax trees and the
// accepted ordering tuple, never from the production mapping/sorting code.
public sealed class S24SurfaceAuthenticCorpusTests : IDisposable
{
    private static readonly CodeRepositoryId Repo = new("repo:s24-corpus");
    private readonly string _path = Path.Combine(
        Path.GetTempPath(), "andirit-s24-corpus-" + Guid.NewGuid().ToString("N") + ".ltdb");

    public void Dispose()
    {
        foreach (var suffix in new[] { "", "-wal", "-journal", ".wal" })
            if (File.Exists(_path + suffix)) File.Delete(_path + suffix);
    }

    [Fact]
    public async Task AuthenticCorpus_RealTargets_PaginateWithIndependentOracle()
    {
        var files = CorpusFiles();
        var context = Context("run:a", files);
        var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(context);
        Assert.True(analysis.Universe.IsComplete, string.Join(";", analysis.Universe.Refusals));
        var app = analysis.Projects.Single(p => p.Project.Path == "App/App.csproj").Verified!;
        var core = analysis.Projects.Single(p => p.Project.Path == "Core/Core.csproj").Verified!;

        using (var store = new LatticeCodeGraphStore(_path))
            await PublishAsync(store, "run:a", context);

        // Independent expected oracle: invocation sites in the App compilation that
        // bind to the chosen overload, ordered by the accepted tuple.
        var addOverload = (IMethodSymbol)core.Compilation.GetTypeByMetadataName("Corpus.Core.Methods")!
            .GetMembers("Add").OfType<IMethodSymbol>().Single(m => m.Parameters.Length == 2 && m.Parameters[0].Type.SpecialType == SpecialType.System_Int32);
        var expected = InvocationSites(app.Compilation, addOverload);
        Assert.True(expected.Count >= 5, "corpus must contain several authentic invocations of the overload");
        var orderedExpected = expected
            .OrderBy(s => s.Path, StringComparer.Ordinal)
            .ThenBy(s => s.StartLine).ThenBy(s => s.StartColumn)
            .ThenBy(s => s.EndLine).ThenBy(s => s.EndColumn)
            .ToArray();

        var target = ExpectedTarget(core, addOverload);

        using var reopened = new LatticeCodeGraphStore(_path);
        // Register the real plugin's authority so the verified reader path is used.
        foreach (var pageSize in new[] { 1, 2, 3, 7, 100 })
        {
            var seen = new List<string>();
            string? token = null;
            var pages = 0;
            while (true)
            {
                var query = VerifiedCSharpOccurrenceAuthority.Bind(
                    new(Repo, target, maxResults: pageSize) { ContinuationToken = token },
                    analysis.Universe, app.Compilation);
                var result = await reopened.FindOccurrencesAsync(query);
                Assert.Equal(CodeOccurrenceAvailability.Available, result.Availability);
                Assert.True(result.Occurrences.Count <= pageSize);
                Assert.Equal(orderedExpected.Length, result.TotalMatches);
                Assert.Equal(orderedExpected.Length, result.Examined); // target-local candidates only
                foreach (var hit in result.Occurrences)
                {
                    Assert.Equal(target, hit.TargetId);
                    Assert.Equal(app.Descriptor.ProjectUnitId, hit.SourceUnitId);
                    seen.Add($"{hit.Span.Path}:{hit.Span.StartLine}:{hit.Span.StartColumn}:{hit.Span.EndLine}:{hit.Span.EndColumn}");
                }
                pages++;
                if (result.NextContinuationToken is null)
                {
                    Assert.Equal(CodeOccurrenceContinuationState.Complete, result.Continuation);
                    break;
                }
                Assert.Equal(CodeOccurrenceContinuationState.More, result.Continuation);
                token = result.NextContinuationToken;
            }

            Assert.True(pages >= 1);
            Assert.Equal(
                orderedExpected.Select(s => $"{s.Path}:{s.StartLine}:{s.StartColumn}:{s.EndLine}:{s.EndColumn}"),
                seen);
            Assert.Equal(seen.Count, seen.Distinct(StringComparer.Ordinal).Count());

            // A single whole-scope page of a complete query is complete and untruncated.
            var whole = await reopened.FindOccurrencesAsync(VerifiedCSharpOccurrenceAuthority.Bind(
                new(Repo, target, maxResults: 1000), analysis.Universe, app.Compilation));
            Assert.Equal(CodeOccurrenceAvailability.Available, whole.Availability);
            Assert.Equal(orderedExpected.Length, whole.Occurrences.Count);
            Assert.False(whole.Truncated);
            Assert.True(whole.IsComplete);
        }
    }

    [Fact]
    public async Task AuthenticCorpus_CrossProjectTarget_HasCorrectIdentityAndProvenance()
    {
        var files = CorpusFiles();
        var context = Context("run:a", files);
        var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(context);
        Assert.True(analysis.Universe.IsComplete);
        var core = analysis.Projects.Single(p => p.Project.Path == "Core/Core.csproj").Verified!;

        using (var store = new LatticeCodeGraphStore(_path))
            await PublishAsync(store, "run:a", context);

        // Corpus.Core.LeafUser.UseLeaf() invokes Corpus.Leaf.Helper.Compute (cross-project).
        var leaf = analysis.Projects.Single(p => p.Project.Path == "Leaf/Leaf.csproj").Verified!;
        var coreUnit = analysis.Projects.Single(p => p.Project.Path == "Core/Core.csproj").Verified!;
        var appUnit = analysis.Projects.Single(p => p.Project.Path == "App/App.csproj").Verified!;
        var helper = (IMethodSymbol)leaf.Compilation.Assembly.GetTypeByMetadataName("Corpus.Leaf.Helper")!
            .GetMembers("Compute").Single();
        var target = ExpectedTarget(leaf, helper);

        using var reopened = new LatticeCodeGraphStore(_path);
        var query = VerifiedCSharpOccurrenceAuthority.Bind(new(Repo, target), analysis.Universe, coreUnit.Compilation);
        var result = await reopened.FindOccurrencesAsync(query);
        Assert.Equal(CodeOccurrenceAvailability.Available, result.Availability);
        // Two authentic cross-project callers (Core.LeafUser and App.Sites) target the Leaf method.
        Assert.Equal(2, result.Occurrences.Count);
        Assert.All(result.Occurrences, hit => Assert.Equal(target, hit.TargetId));
        Assert.Equal(
            new[] { appUnit.Descriptor.ProjectUnitId, coreUnit.Descriptor.ProjectUnitId }.Order(StringComparer.Ordinal),
            result.Occurrences.Select(o => o.SourceUnitId).Order(StringComparer.Ordinal));
        // The calling unit owns each occurrence; the defining unit owns the target.
        Assert.DoesNotContain(result.Occurrences, o => o.SourceUnitId == leaf.Descriptor.ProjectUnitId);
        Assert.True(result.IsComplete);
    }

    // ---- helpers ----

    private static List<(string Path, int StartLine, int StartColumn, int EndLine, int EndColumn)> InvocationSites(
        CSharpCompilation compilation, IMethodSymbol method)
    {
        var sites = new List<(string, int, int, int, int)>();
        foreach (var tree in compilation.SyntaxTrees)
        {
            var model = compilation.GetSemanticModel(tree);
            foreach (var invocation in tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (!SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(invocation).Symbol, method))
                    continue;
                var nameToken = invocation.Expression is MemberAccessExpressionSyntax member
                    ? member.Name.Identifier
                    : invocation.Expression.GetLastToken();
                var line = tree.GetLineSpan(nameToken.Span);
                sites.Add((tree.FilePath.Replace('\\', '/'),
                    line.StartLinePosition.Line + 1, line.StartLinePosition.Character + 1,
                    line.EndLinePosition.Line + 1, line.EndLinePosition.Character + 1));
            }
        }
        return sites;
    }

    private static IReadOnlyDictionary<string, string> CorpusFiles()
    {
        var root = CorpusRoot();
        var files = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var project in new[] { "App", "Core", "Leaf", "Solo" })
            files[$"{project}/{project}.csproj"] = File.ReadAllText(Path.Combine(root, project, $"{project}.csproj"));
        foreach (var file in new[]
        {
            "App/Bulk.cs", "App/Sites.cs", "Core/Extensions.cs", "Core/LeafUser.cs", "Core/Methods.cs",
            "Core/Types.cs", "Leaf/Helper.cs", "Solo/Declarations.cs",
        })
            files[file] = File.ReadAllText(Path.Combine(root, file.Replace('/', Path.DirectorySeparatorChar)));
        return files;
    }

    private static string CorpusRoot() => Path.Combine(Root(), "docs", "planning", "adr-ci1-01-ci1a-s2-0", "corpus", "repo");

    private static string Root()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Andirit.slnx"))) return directory.FullName;
            directory = directory.Parent;
        }
        throw new InvalidOperationException("Repository root not found.");
    }

    private static CodeGraphPluginContext Context(string run, IReadOnlyDictionary<string, string> files) =>
        new(Repo, null, new CodeIndexRunId(run),
            files.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair =>
            {
                var bytes = Encoding.UTF8.GetBytes(pair.Value);
                return new CodeGraphSource(pair.Key,
                    "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)),
                    _ => ValueTask.FromResult<Stream>(new MemoryStream(bytes, writable: false)));
            }).ToArray());

    private static async Task PublishAsync(LatticeCodeGraphStore store, string run, CodeGraphPluginContext context)
    {
        var plugin = new CSharpCodeGraphPlugin();
        await store.UpsertRepositoryAsync(new(Repo));
        await store.StoreIndexRunAsync(new(Repo, new(run), new DateTimeOffset(2026, 11, 9, 0, 0, 0, TimeSpan.Zero), plugins: [plugin.Id]));
        await using (var sink = new CodeGraphIngestionSink(store))
        await using (var session = await plugin.CreateSessionAsync(context))
        {
            await session.ExtractAsync(sink);
            await sink.CompleteAsync();
        }
        await store.CompleteIndexRunAsync(
            new(Repo, new(run), new DateTimeOffset(2026, 11, 9, 0, 0, 0, TimeSpan.Zero), CodeIndexRunStatus.Completed,
                new DateTimeOffset(2026, 11, 9, 0, 0, 1, TimeSpan.Zero), [plugin.Id]),
            new(Repo, new(run), []));
    }

    private static CodeOccurrenceTargetId ExpectedTarget(VerifiedCSharpCompilation defining, IMethodSymbol method)
    {
        var mapped = defining.MapR2FaithfulCompctx1();
        var identifier = CodeOccurrenceIdentity.ComputeIdentifierDigest([method.Name]);
        var declaration = CodeOccurrenceIdentity.ComputeDeclarationDigest([method.GetDocumentationCommentId() ?? method.Name]);
        var shape = CodeOccurrenceIdentity.ComputeShapeDigest("method|" + method.Arity + "|" +
            string.Join(",", method.Parameters.Select(p => p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))));
        var viaIdentity = CodeOccurrenceIdentity.ComputeTargetId(mapped.DescriptorDigest, mapped.MetadataIdentity,
            mapped.ModuleName, identifier, declaration, shape);
        var frozen = Frozen.CompilationProvenance.Describe(Repo.Value, defining.Descriptor.ProjectUnitId,
            defining.Compilation, defining.Sources.Select(s => (s.Path, Encoding.UTF8.GetBytes(s.Text.ToString()))).ToArray());
        var viaFrozen = "asmctx3:sha256:" + Frozen.ProvenanceHashing.Hash("asmctx3-context",
            ["compctx1", frozen.DescriptorDigest, frozen.MetadataIdentity, frozen.ModuleName, identifier, declaration, shape]);
        Assert.Equal(viaIdentity.Value, viaFrozen);
        return viaIdentity;
    }
}
