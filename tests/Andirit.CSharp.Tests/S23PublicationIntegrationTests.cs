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
// WP-S23-3A R1 (B-WP-S23-3A-01): durable graph reconstruction restores the
// complete multi-unit publication before validation, so cross-project
// publications survive durable reopen. The previous pinned blocker test
// (CrossProjectPublication_DurableReopen_IsBlockedByPreExistingGraphOrderingDefect)
// is superseded by the authorized remediation and replaced by the positive
// fresh-process reopen tests below.
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

    // The same logical cross-project publication with the defining project's
    // unit persisting before the referencing project's unit in HetuUnit key
    // order (the OLib unit hash sorts before every MApp unit hash),
    // complementing the default fixture where the referencing unit persists
    // first.
    private static IReadOnlyDictionary<string, string> CrossProjectFixtureDefiningUnitFirst() =>
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["OLib/OLib.csproj"] = "<Project Sdk=\"Microsoft.NET.Sdk\" />",
            ["OLib/Value.cs"] = "namespace Lib; public static class Value { public static int Get() => 1; }",
            ["MApp/MApp.csproj"] =
                "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><ProjectReference Include=\"../OLib/OLib.csproj\" /></ItemGroup></Project>",
            ["MApp/Application.cs"] =
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
        LiveCrossProjectAsync(CodeGraphPluginContext context) =>
        await LiveCrossProjectAsync(context, Names.AppProject, Names.LibProject);

    private static async Task<(VerifiedCSharpAnalysis Analysis, VerifiedCSharpCompilation App, VerifiedCSharpCompilation Lib)>
        LiveCrossProjectAsync(CodeGraphPluginContext context, string appProject, string libProject)
    {
        var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(context);
        return (analysis,
            analysis.Projects.Single(p => p.Project.Path == appProject).Verified!,
            analysis.Projects.Single(p => p.Project.Path == libProject).Verified!);
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

    [Theory]
    [InlineData("_ = new Sample(); Sample implicitNew = new();", 2)]
    [InlineData("_ = Property; item.Property = 2; _ = item?.Property; _ = item[0]; _ = Sample.StaticProperty;", 5)]
    [InlineData("System.Func<int> assignment = Lib.Value.Get; _ = (System.Func<int>)Lib.Value.Get;", 2)]
    [InlineData("_ = new Sample { Property = 1 }; System.Func<int> group = Lib.Value.Get; _ = item.Property;", 4)]
    [InlineData("_ = (1, 2); _ = item.Field; _ = \"Property and Get\";", 0)]
    public async Task UnsupportedSites_CountOnce_AndSurviveNativePublication(string sites, int expectedUnsupported)
    {
        var files = new Dictionary<string, string>(CrossProjectFixture(), StringComparer.Ordinal)
        {
            [Names.AppSource] = """
                namespace App;
                public sealed class Sample
                {
                    public int Property { get; set; }
                    public static int StaticProperty => 0;
                    public int this[int index] => 0;
                    public int Field;

                    public int Run(Sample item)
                    {
                """ + sites + """
                        _ = string.Concat("left", "right");
                        Missing();
                        return Lib.Value.Get();
                    }
                }
                """
        };
        const string run = "run:unsupported";
        var context = Context(Repo, run, files);
        var (analysis, app, lib) = await LiveCrossProjectAsync(context);
        var target = ExpectedTarget(lib, Method(lib, "Lib.Value", "Get"));
        var query = VerifiedCSharpOccurrenceAuthority.Bind(new(Repo, target), analysis.Universe, app.Compilation);

        async Task VerifyAsync(LatticeCodeGraphStore store)
        {
            var result = await store.FindOccurrencesAsync(query);
            Assert.Equal(CodeOccurrenceAvailability.Available, result.Availability);
            Assert.True(result.IsComplete);
            var hit = Assert.Single(result.Occurrences);
            Assert.Equal(app.Descriptor.ProjectUnitId, hit.SourceUnitId);
            Assert.Equal(target, hit.TargetId);
            Assert.Equal(1, result.Coverage!.InvocationFacts);
            Assert.Equal(expectedUnsupported, result.Coverage.UnsupportedCategories);
            Assert.Equal(expectedUnsupported, result.Coverage.Unsupported);
            Assert.Equal(1, result.Coverage.External);
            Assert.Equal(1, result.Coverage.Unresolved);
            Assert.Equal(0, result.Coverage.Ambiguous);
        }

        using (var store = await PublishAsync(_path, run, context))
            await VerifyAsync(store);
        var unit = PublishedUnit(_path, app.Descriptor.ProjectUnitId);
        Assert.Single(unit["Facts"]!.AsArray());
        Assert.Equal(expectedUnsupported, unit["Coverage"]!["UnsupportedCategories"]!.GetValue<int>());
        Assert.Equal(expectedUnsupported, unit["Coverage"]!["Unsupported"]!.GetValue<int>());
        using var reopened = Open(_path);
        await VerifyAsync(reopened);
    }

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
    public async Task ExternalBinding_CoverageSurvivesAuthorityBoundPublicationAndReopen()
    {
        // The focused producer gate also runs each phase in its own test host.
        var phase = Environment.GetEnvironmentVariable("S254_R1_EXTERNAL_PHASE");
        var path = phase is null ? _path :
            Environment.GetEnvironmentVariable("S254_R1_EXTERNAL_PATH") ??
            throw new InvalidOperationException("S254_R1_EXTERNAL_PATH must name the durable store.");
        if (phase is not null)
            Console.WriteLine($"s254-r1-external phase={phase} pid={Environment.ProcessId} path={path}");

        var files = new Dictionary<string, string>(CrossProjectFixture(), StringComparer.Ordinal)
        {
            [Names.AppSource] = """
                namespace App;
                public static class Application
                {
                    public static int Run() => Lib.Value.Get();
                    public static string External() => string.Concat("left", "right");
                }
                """,
        };
        var context = Context(Repo, "run:external", files);
        var (analysis, app, lib) = await LiveCrossProjectAsync(context);
        var target = ExpectedTarget(lib, Method(lib, "Lib.Value", "Get"));
        var query = VerifiedCSharpOccurrenceAuthority.Bind(new(Repo, target), analysis.Universe, app.Compilation);

        if (phase != "verify")
        {
            using var published = await PublishAsync(path, "run:external", context);
            CheckExternalCoverage(await published.FindOccurrencesAsync(query), target, app.Descriptor.ProjectUnitId);
        }

        if (phase != "publish")
        {
            using var reopened = Open(path);
            CheckExternalCoverage(await reopened.FindOccurrencesAsync(query), target, app.Descriptor.ProjectUnitId);
        }

        var unit = PublishedUnit(path, app.Descriptor.ProjectUnitId);
        Assert.True(unit["RequiresAuthority"]!.GetValue<bool>());
        Assert.Single(unit["Facts"]!.AsArray());
        Assert.Equal(lib.Descriptor.ProjectUnitId,
            unit["Authority"]!["Facts"]![0]!["DefiningProjectUnitId"]!.GetValue<string>());
        Assert.Equal(1, unit["Coverage"]!["External"]!.GetValue<int>());
        Assert.Equal(0, unit["Coverage"]!["Unresolved"]!.GetValue<int>());
    }

    private static void CheckExternalCoverage(CodeOccurrenceQueryResult result, CodeOccurrenceTargetId target, string owner)
    {
        Assert.Equal(CodeOccurrenceAvailability.Available, result.Availability);
        Assert.True(result.IsComplete);
        var occurrence = Assert.Single(result.Occurrences);
        Assert.Equal(target, occurrence.TargetId);
        Assert.Equal(owner, occurrence.SourceUnitId);
        Assert.Equal(1, result.Coverage!.InvocationFacts);
        Assert.Equal(1, result.Coverage.External);
        Assert.Equal(0, result.Coverage.Unresolved);
        Assert.Equal(0, result.Coverage.Ambiguous);
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

    // WP-S23-3A R1: the previously pinned blocker (durable reopen of a
    // multi-unit cross-project publication failed with
    // graph.edge.endpoint-missing) is remediated by reconstructing the
    // complete publication before validation. This test doubles as the
    // fresh-process evidence harness: with S23_REOPEN_FRESH_PROCESS=publish it
    // only publishes to the harness path, and with =verify it only reopens and
    // verifies the file the separate publishing process left behind. Without
    // the variable it runs the full in-process durable round trip.
    private static string? FreshProcessPhase =>
        Environment.GetEnvironmentVariable("S23_REOPEN_FRESH_PROCESS");

    private static string FreshProcessHarnessPath() =>
        Environment.GetEnvironmentVariable("S23_REOPEN_FRESH_PROCESS_PATH") ??
        throw new InvalidOperationException(
            "S23_REOPEN_FRESH_PROCESS_PATH must name the durable file for the fresh-process harness.");

    [Fact]
    public async Task CrossProjectRealFact_SurvivesDurableReopen_WithCorrectUnitsAndCoverage()
    {
        var phase = FreshProcessPhase;
        var path = phase is null ? _path : FreshProcessHarnessPath();
        if (phase is not null)
        {
            // Harness evidence: each phase runs in its own OS process; the
            // verify process only reads the durable file the publish process
            // wrote and never publishes itself.
            Console.WriteLine(
                $"s23-fresh-process phase={phase} pid={Environment.ProcessId} path={path}");
        }

        if (phase == "verify")
        {
            await VerifyCrossProjectReopenAsync(path);
            return;
        }

        var context = Context(Repo, "run:a", CrossProjectFixture());
        (await PublishAsync(path, "run:a", context)).Dispose();
        if (phase == "publish")
            return;

        await VerifyCrossProjectReopenAsync(path);
    }

    private static async Task VerifyCrossProjectReopenAsync(string path)
    {
        var context = Context(Repo, "run:a", CrossProjectFixture());
        var (analysis, app, lib) = await LiveCrossProjectAsync(context);
        var target = ExpectedTarget(lib, Method(lib, "Lib.Value", "Get"));

        using (var reopened = Open(path))
        {
            var units = await reopened.GetPublishedUnitsAsync(Repo);
            Assert.Equal(2, units.Count);

            // The cross-project graph edge from the App unit must resolve to
            // the Lib unit's project node after reconstruction.
            var appNodes = await reopened.FindNodesByQualifiedNameAsync(Repo, Names.AppProject);
            var appProject = Assert.Single(appNodes, node => node.Kind == CodeNodeKinds.Project);
            var traversal = await reopened.TraverseAsync(
                Repo,
                new(appProject.Id, CodeGraphDirection.Outgoing, maxDepth: 1));
            Assert.Contains(
                traversal.Nodes,
                node => node.Kind == CodeNodeKinds.Project && node.QualifiedName == Names.LibProject);

            var query = VerifiedCSharpOccurrenceAuthority.Bind(
                new(Repo, target), analysis.Universe, app.Compilation);
            var result = await reopened.FindOccurrencesAsync(query);
            Assert.Equal(CodeOccurrenceAvailability.Available, result.Availability);
            Assert.Single(result.Occurrences);
            Assert.True(result.IsComplete);
            Assert.Equal(target, result.Occurrences[0].TargetId);
            Assert.Equal(app.Descriptor.ProjectUnitId, result.Occurrences[0].SourceUnitId);
        }

        var unit = PublishedUnit(path, app.Descriptor.ProjectUnitId);
        Assert.True(unit["RequiresAuthority"]!.GetValue<bool>());
        Assert.Equal(
            lib.Descriptor.ProjectUnitId,
            unit["Authority"]!["Facts"]![0]!["DefiningProjectUnitId"]!.GetValue<string>());
    }

    // The defining project's unit persists before the referencing project's
    // unit in HetuUnit key order; the reopened outcome must be identical to
    // the default fixture's (referencing unit first). Unit ids follow the
    // plugin's public project identity convention, so the expected persisted
    // order is computed here, not assumed.
    [Fact]
    public async Task CrossProjectRealFact_DefiningUnitPersistedFirst_ReopensWithIdenticalOutcome()
    {
        var definingPath = "OLib/OLib.csproj";
        var referencingPath = "MApp/MApp.csproj";
        var definingUnitId = CSharpProjectDiscovery.IndexUnitId(definingPath);
        var referencingUnitId = CSharpProjectDiscovery.IndexUnitId(referencingPath);
        Assert.True(string.CompareOrdinal(definingUnitId, referencingUnitId) < 0,
            "Fixture must persist the defining unit before the referencing unit.");

        var context = Context(Repo, "run:defining-first", CrossProjectFixtureDefiningUnitFirst());
        (await PublishAsync(_path, "run:defining-first", context)).Dispose();

        var persistedOrder = F.Rows(_path, "HetuUnit").Keys
            .Where(key => key.StartsWith("HetuUnit|", StringComparison.Ordinal))
            .Select(key => key["HetuUnit|".Length..].Split('\n')[2])
            .ToArray();
        Assert.Equal([definingUnitId, referencingUnitId], persistedOrder);

        var (analysis, app, lib) = await LiveCrossProjectAsync(context, referencingPath, definingPath);
        var target = ExpectedTarget(lib, Method(lib, "Lib.Value", "Get"));

        using (var reopened = Open(_path))
        {
            Assert.Equal(2, (await reopened.GetPublishedUnitsAsync(Repo)).Count);
            var query = VerifiedCSharpOccurrenceAuthority.Bind(
                new(Repo, target), analysis.Universe, app.Compilation);
            var result = await reopened.FindOccurrencesAsync(query);
            Assert.Equal(CodeOccurrenceAvailability.Available, result.Availability);
            Assert.Single(result.Occurrences);
            Assert.True(result.IsComplete);
            Assert.Equal(app.Descriptor.ProjectUnitId, result.Occurrences[0].SourceUnitId);
        }

        var unit = PublishedUnit(_path, app.Descriptor.ProjectUnitId);
        Assert.Equal(
            lib.Descriptor.ProjectUnitId,
            unit["Authority"]!["Facts"]![0]!["DefiningProjectUnitId"]!.GetValue<string>());
    }

    // Stale refusal must survive reopen: a transitive-only defining-context
    // change refuses the reopened publication despite stable target identity.
    [Fact]
    public async Task ReopenedStore_RefusesStaleRealFact_AfterTransitiveDependencyChange()
    {
        var a = Context(Repo, "run:a", CrossProjectFixture());
        var (_, appA, libA) = await LiveCrossProjectAsync(a);
        var target = ExpectedTarget(libA, Method(libA, "Lib.Value", "Get"));
        (await PublishAsync(_path, "run:a", a)).Dispose();

        using var reopened = Open(_path);
        var b = Context(Repo, "run:b", CrossProjectFixture("public static int Get() => 2;"));
        var (analysisB, appB, _) = await LiveCrossProjectAsync(b);
        var query = VerifiedCSharpOccurrenceAuthority.Bind(
            new(Repo, target), analysisB.Universe, appB.Compilation);
        var result = await reopened.FindOccurrencesAsync(query);
        Assert.Equal(CodeOccurrenceAvailability.StalePublication, result.Availability);
        Assert.Empty(result.Occurrences);
        Assert.Equal(0, result.Examined);
        Assert.False(result.IsComplete);
    }

    // Missing current authority must refuse identically on a reopened store,
    // never as a successful empty result.
    [Fact]
    public async Task ReopenedStore_MissingAuthority_IsUnavailable_NotEmptySuccess()
    {
        var context = Context(Repo, "run:a", CrossProjectFixture());
        var (_, app, lib) = await LiveCrossProjectAsync(context);
        var target = ExpectedTarget(lib, Method(lib, "Lib.Value", "Get"));
        (await PublishAsync(_path, "run:a", context)).Dispose();

        using var reopened = Open(_path);
        var result = await reopened.FindOccurrencesAsync(new CodeOccurrenceQuery(Repo, target));
        Assert.Equal(CodeOccurrenceAvailability.AnalysisUnavailable, result.Availability);
        Assert.Empty(result.Occurrences);
        Assert.False(result.IsComplete);
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
