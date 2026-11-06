using System.Security.Cryptography;
using System.Text;
using Andirit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using F = Andirit.CSharp.Tests.S23NativeAuthorityFixture;

namespace Andirit.CSharp.Tests;

// WP-S23-3A real-fact publication integration. These drive the real C# plugin
// through the accepted batch/sink publication path into LatticeDB and verify
// authority-gated availability. Expected cross-project targets are computed here
// from the accepted frozen identity helpers and the live verified compilation,
// not from the implementation under test.
//
// Durable reopen of a multi-unit (cross-project) publication is blocked by a
// pre-existing, order-dependent graph reconstruction defect outside this
// package; that is recorded in the WP-S23-3A implementation report, and the
// durable reopen test below uses a single-unit publication with no cross-unit
// edges.
public sealed class S23PublicationIntegrationTests : IDisposable
{
    private readonly string _path = Path.Combine(
        Path.GetTempPath(), "andirit-s23-pub-" + Guid.NewGuid().ToString("N") + ".ltdb");

    private static readonly CodeRepositoryId Repo = new("repo:s23-publication");

    public void Dispose()
    {
        foreach (var suffix in new[] { "", "-wal", "-journal", ".wal" })
            if (File.Exists(_path + suffix)) File.Delete(_path + suffix);
    }

    private static class Names
    {
        internal const string LibProject = "Lib/Lib.csproj";
        internal const string AppProject = "App/App.csproj";
        internal const string AppSource = "App/Application.cs";
    }

    private static IReadOnlyDictionary<string, string> CrossProjectFixture(
        string libBody = "public static int Get() => 1;") =>
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [Names.LibProject] = "<Project Sdk=\"Microsoft.NET.Sdk\" />",
            ["Lib/Value.cs"] = "namespace Lib; public static class Value { " + libBody + " }",
            [Names.AppProject] =
                "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><ProjectReference Include=\"../Lib/Lib.csproj\" /></ItemGroup></Project>",
            [Names.AppSource] =
                "namespace App; public static class Application { public static int Run() => Lib.Value.Get(); }",
        };

    private static IReadOnlyDictionary<string, string> SingleProjectFixture() =>
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["App/App.csproj"] = "<Project Sdk=\"Microsoft.NET.Sdk\" />",
            ["App/Application.cs"] =
                "namespace App; public static class A { public static int B() => 1; public static int C() => B(); }",
        };

    private static CodeGraphPluginContext Context(CodeRepositoryId repo, string run,
        IReadOnlyDictionary<string, string> files) =>
        new(repo, null, new CodeIndexRunId(run),
            files.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair =>
            {
                var bytes = Encoding.UTF8.GetBytes(pair.Value);
                return new CodeGraphSource(pair.Key,
                    "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)),
                    _ => ValueTask.FromResult<Stream>(new MemoryStream(bytes, writable: false)));
            }).ToArray());

    private static async Task<LatticeCodeGraphStore> PublishAsync(string path, string run,
        CodeGraphPluginContext context)
    {
        var plugin = new CSharpCodeGraphPlugin();
        var started = new DateTimeOffset(2026, 11, 6, 0, 0, 0, TimeSpan.Zero);
        var store = new LatticeCodeGraphStore(path);
        await store.UpsertRepositoryAsync(new(Repo));
        await store.StoreIndexRunAsync(new(Repo, new(run), started, plugins: [plugin.Id]));
        await using (var sink = new CodeGraphIngestionSink(store))
        await using (var session = await plugin.CreateSessionAsync(context))
        {
            await session.ExtractAsync(sink);
            await sink.CompleteAsync();
        }
        await store.CompleteIndexRunAsync(
            new(Repo, new(run), started, CodeIndexRunStatus.Completed, started.AddSeconds(1), [plugin.Id]),
            new(Repo, new(run), []));
        return store;
    }

    private static async Task<(VerifiedCSharpAnalysis Analysis, VerifiedCSharpCompilation App, VerifiedCSharpCompilation Lib)>
        LiveCrossProjectAsync(CodeGraphPluginContext context)
    {
        var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(context);
        return (analysis,
            analysis.Projects.Single(p => p.Project.Path == Names.AppProject).Verified!,
            analysis.Projects.Single(p => p.Project.Path == Names.LibProject).Verified!);
    }

    // Recomputes the target exactly as the accepted identity contract defines it,
    // using the plugin's caller-supplied component-digest convention. This is an
    // expected value from accepted authority, not from the implementation.
    private static CodeOccurrenceTargetId ExpectedTarget(VerifiedCSharpCompilation defining, IMethodSymbol method)
    {
        var mapped = defining.MapR2FaithfulCompctx1();
        return CodeOccurrenceIdentity.ComputeTargetId(
            mapped.DescriptorDigest, mapped.MetadataIdentity, mapped.ModuleName,
            CodeOccurrenceIdentity.ComputeIdentifierDigest([method.Name]),
            CodeOccurrenceIdentity.ComputeDeclarationDigest([method.GetDocumentationCommentId() ?? method.Name]),
            CodeOccurrenceIdentity.ComputeShapeDigest("method|" + method.Arity + "|" +
                string.Join(",", method.Parameters.Select(p => p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))));
    }

    private static IMethodSymbol Method(VerifiedCSharpCompilation unit, string type, string name) =>
        (IMethodSymbol)unit.Compilation.GetTypeByMetadataName(type)!.GetMembers(name).Single();

    private static LatticeCodeGraphStore Open(string path)
    {
        try
        {
            return new LatticeCodeGraphStore(path);
        }
        catch (CodeGraphBatchRejectedException exception)
        {
            throw new Xunit.Sdk.XunitException(
                string.Join("; ", exception.Errors.Select(e => e.Code + "=" + e.Message)));
        }
    }

    private static System.Text.Json.Nodes.JsonObject PublishedUnit(string path, string sourceUnitId) =>
        F.Rows(path, "AndiritOccurrenceUnit").Values.Single(unit =>
            unit["Authority"] is { } authority &&
            authority["SourceUnitId"]!.GetValue<string>() == sourceUnitId);

    [Fact]
    public async Task RealCrossProjectFact_IsPublishedWithAuthority_AndServed()
    {
        var context = Context(Repo, "run:a", CrossProjectFixture());
        var (analysis, app, lib) = await LiveCrossProjectAsync(context);
        var target = ExpectedTarget(lib, Method(lib, "Lib.Value", "Get"));
        var store = await PublishAsync(_path, "run:a", context);
        try
        {
            var query = VerifiedCSharpOccurrenceAuthority.Bind(new(Repo, target), analysis.Universe, app.Compilation);
            var result = await store.FindOccurrencesAsync(query);
            Assert.Equal(CodeOccurrenceAvailability.Available, result.Availability);
            Assert.Single(result.Occurrences);
            Assert.True(result.IsComplete);
            Assert.Equal(target, result.Occurrences[0].TargetId);
            Assert.Equal(app.Descriptor.ProjectUnitId, result.Occurrences[0].SourceUnitId);
        }
        finally
        {
            store.Dispose();
        }

        var unit = PublishedUnit(_path, app.Descriptor.ProjectUnitId);
        Assert.True(unit["RequiresAuthority"]!.GetValue<bool>());
        Assert.Equal(lib.Descriptor.ProjectUnitId,
            unit["Authority"]!["Facts"]![0]!["DefiningProjectUnitId"]!.GetValue<string>());
    }

    [Fact]
    public async Task TransitiveDependencyChange_RefusesStaleRealFact()
    {
        var a = Context(Repo, "run:a", CrossProjectFixture());
        var (_, appA, libA) = await LiveCrossProjectAsync(a);
        var target = ExpectedTarget(libA, Method(libA, "Lib.Value", "Get"));
        var store = await PublishAsync(_path, "run:a", a);
        try
        {
            // Change only the transitive dependency body; App source and the
            // committed target identity are unchanged.
            var b = Context(Repo, "run:b", CrossProjectFixture("public static int Get() => 2;"));
            var (analysisB, appB, _) = await LiveCrossProjectAsync(b);
            var query = VerifiedCSharpOccurrenceAuthority.Bind(new(Repo, target), analysisB.Universe, appB.Compilation);
            var result = await store.FindOccurrencesAsync(query);
            Assert.Equal(CodeOccurrenceAvailability.StalePublication, result.Availability);
            Assert.Empty(result.Occurrences);
            Assert.Equal(0, result.Examined);
            Assert.False(result.IsComplete);
        }
        finally
        {
            store.Dispose();
        }
    }

    [Fact]
    public async Task MissingCurrentAuthority_IsUnavailable_NotEmptySuccess()
    {
        var context = Context(Repo, "run:a", CrossProjectFixture());
        var (_, app, lib) = await LiveCrossProjectAsync(context);
        var target = ExpectedTarget(lib, Method(lib, "Lib.Value", "Get"));
        var store = await PublishAsync(_path, "run:a", context);
        try
        {
            var result = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(Repo, target));
            Assert.Equal(CodeOccurrenceAvailability.AnalysisUnavailable, result.Availability);
            Assert.Empty(result.Occurrences);
            Assert.False(result.IsComplete);
        }
        finally
        {
            store.Dispose();
        }
    }

    [Fact]
    public async Task IncompleteUniverse_PublishesGraphOnly_AndNoOccurrenceRows()
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [Names.AppProject] =
                "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><ProjectReference Include=\"../Lib/Lib.csproj\" /></ItemGroup></Project>",
            [Names.AppSource] =
                "namespace App; public static class Application { public static int Run() => Lib.Value.Get(); }",
        };
        var context = Context(Repo, "run:incomplete", files);
        var store = await PublishAsync(_path, "run:incomplete", context);
        var published = await store.GetPublishedUnitsAsync(Repo);
        store.Dispose();

        Assert.NotEmpty(published);
        Assert.Empty(F.Rows(_path, "AndiritOccurrenceUnit"));
    }

    // Documents, reproducibly, the pre-existing blocker: durable reopen of a
    // multi-unit publication with cross-unit graph edges is order-dependent and
    // fails independently of occurrence publication. Recorded in the WP-S23-3A
    // report; fixing it is outside the authorized occurrence-publication scope.
    [Fact]
    public async Task CrossProjectPublication_DurableReopen_IsBlockedByPreExistingGraphOrderingDefect()
    {
        var context = Context(Repo, "run:a", CrossProjectFixture());
        (await PublishAsync(_path, "run:a", context)).Dispose();
        var exception = Assert.Throws<CodeGraphBatchRejectedException>(
            () => new LatticeCodeGraphStore(_path));
        Assert.Contains(exception.Errors, error => error.Code == "graph.edge.endpoint-missing");
    }

    [Fact]
    public async Task SingleUnitRealFact_SurvivesDurableReopen()
    {
        var context = Context(Repo, "run:a", SingleProjectFixture());
        (await PublishAsync(_path, "run:a", context)).Dispose();

        var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(context);
        var app = analysis.Projects.Single(project => project.Project.Path == "App/App.csproj").Verified!;
        var target = ExpectedTarget(app, Method(app, "App.A", "B"));

        using var reopened = Open(_path);
        var query = VerifiedCSharpOccurrenceAuthority.Bind(new(Repo, target), analysis.Universe, app.Compilation);
        var result = await reopened.FindOccurrencesAsync(query);
        Assert.Equal(CodeOccurrenceAvailability.Available, result.Availability);
        Assert.Single(result.Occurrences);
        Assert.True(result.IsComplete);
        Assert.Equal(app.Descriptor.ProjectUnitId, result.Occurrences[0].SourceUnitId);
    }
}
