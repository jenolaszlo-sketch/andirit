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
            }).ToArray(),
            settings: FrameworkTestSettings.Net10);

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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MissingDeclaredUnit_ReportsIncompleteAcrossNativeReopen(bool projectIsPresent)
    {
        const string firstRun = "run:full-before-missing";
        const string secondRun = "run:missing-declared-unit";
        var files = new Dictionary<string, string>(CrossProjectFixture(), StringComparer.Ordinal);
        var completeContext = Context(Repo, firstRun, files);
        var (complete, app, lib) = await LiveCrossProjectAsync(completeContext);
        var target = ExpectedTarget(lib, Method(lib, "Lib.Value", "Get"));
        var boundComplete = VerifiedCSharpOccurrenceAuthority.Bind(new(Repo, target), complete.Universe, app.Compilation);
        CodeGraphPublication previous;
        using (var store = await PublishAsync(_path, firstRun, completeContext))
        {
            var result = await store.FindOccurrencesAsync(boundComplete);
            Assert.Equal(CodeOccurrenceAvailability.Available, result.Availability);
            Assert.True(result.IsComplete);
            Assert.Single(result.Occurrences);
            previous = Assert.IsType<CodeGraphPublication>(result.Publication);
        }

        const string missing = "Missing/Missing.csproj";
        files["Repository.slnx"] = """
            <Solution>
              <Project Path="App/App.csproj" />
              <Project Path="Lib/Lib.csproj" />
              <Project Path="Missing/Missing.csproj" />
            </Solution>
            """;
        if (projectIsPresent)
            files[missing] = """<Project><ItemGroup><Compile Include="Uncaptured.cs" /></ItemGroup></Project>""";

        var context = Context(Repo, secondRun, files);
        var incomplete = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(context);
        var missingUnit = CSharpProjectDiscovery.IndexUnitId(missing);
        Assert.Equal(missingUnit, Assert.Single(incomplete.Universe.MissingScopeUnits));
        Assert.Equal(3, incomplete.Universe.Scope.Units.Length);
        Assert.Equal(2, incomplete.Universe.Entries.Length);
        Assert.False(incomplete.Universe.IsComplete);

        async Task VerifyAsync(LatticeCodeGraphStore store)
        {
            var result = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(Repo, target));
            Assert.Equal(CodeOccurrenceAvailability.IncompleteAnalysis, result.Availability);
            Assert.Equal(secondRun, result.Publication!.IndexRunId.Value);
            Assert.False(result.IsComplete);
            Assert.Empty(result.Occurrences);
            Assert.Equal(CodeRelationshipCoverageState.Partial, result.Coverage!.State);
            Assert.Equal(3, result.Coverage.DeclaredUnitCount);
            Assert.Equal(2, result.Coverage.VerifiedUnitCount);
            Assert.Equal(incomplete.Universe.Scope.Identity, result.Coverage.ScopeId);
            Assert.Equal(incomplete.Universe.Identity, result.Coverage.UniverseId);
            Assert.Equal(0, result.Coverage.InvocationFacts);

            var stale = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(Repo, target, previous));
            Assert.Equal(CodeOccurrenceAvailability.StalePublication, stale.Availability);
            Assert.Empty(stale.Occurrences);
            Assert.False(stale.IsComplete);

            var obsoleteAuthority = await store.FindOccurrencesAsync(boundComplete);
            Assert.Equal(CodeOccurrenceAvailability.AnalysisUnavailable, obsoleteAuthority.Availability);
            Assert.Empty(obsoleteAuthority.Occurrences);
        }

        using (var store = await PublishAsync(_path, secondRun, context))
            await VerifyAsync(store);
        var rows = F.Rows(_path, "AndiritOccurrenceUnit");
        Assert.Equal(2, rows.Count);
        Assert.All(rows.Values, row =>
        {
            Assert.Null(row["Authority"]);
            Assert.False(row["RequiresAuthority"]!.GetValue<bool>());
            Assert.Empty(row["Facts"]!.AsArray());
        });
        using var reopened = Open(_path);
        await VerifyAsync(reopened);
    }

    [Fact]
    public async Task MissingDeclaredUnitWithUnrelatedProvenanceRefusal_PreservesUnavailableAnalysis()
    {
        var files = new Dictionary<string, string>(CrossProjectFixture(), StringComparer.Ordinal)
        {
            ["Repository.slnx"] = """
                <Solution>
                  <Project Path="App/App.csproj" />
                  <Project Path="Lib/Lib.csproj" />
                  <Project Path="Missing/Missing.csproj" />
                </Solution>
                """,
            ["Missing/Missing.csproj"] = """<Project><Import Project="Unknown.props" /></Project>"""
        };
        const string run = "run:malformed-scope";
        var context = Context(Repo, run, files);
        var (analysis, _, lib) = await LiveCrossProjectAsync(context);
        Assert.Equal(2, analysis.Universe.Entries.Length);
        Assert.Single(analysis.Universe.MissingScopeUnits);
        Assert.Contains("UnsupportedProjectEvaluation:Missing/Missing.csproj", analysis.Universe.Refusals);
        var target = ExpectedTarget(lib, Method(lib, "Lib.Value", "Get"));
        (await PublishAsync(_path, run, context)).Dispose();

        using var reopened = Open(_path);
        var result = await reopened.FindOccurrencesAsync(new CodeOccurrenceQuery(Repo, target));
        Assert.Equal(CodeOccurrenceAvailability.AnalysisUnavailable, result.Availability);
        Assert.Empty(result.Occurrences);
        Assert.False(result.IsComplete);
        Assert.Null(result.Coverage);
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

    [Theory]
    [InlineData(1, 1, 0)]
    [InlineData(2, 2, 0)]
    [InlineData(3, 3, 0)]
    [InlineData(2, 0, 0)]
    [InlineData(2, 1, 0)]
    [InlineData(2, 2, 1)]
    [InlineData(2, 2, 2)]
    public async Task CompleteScopeMembership_PublishesDisjointRows(int projects, int calls, int missing)
    {
        const string run = "run:membership";
        var files = MembershipFixture(projects, calls, missing);
        var context = Context(Repo, run, files);
        var phase = FreshProcessPhase;
        var path = phase is null ? _path : FreshProcessHarnessPath() + $"-{projects}-{calls}-{missing}";
        if (phase is not null)
            Console.WriteLine($"membership phase={phase} pid={Environment.ProcessId} path={path}");

        if (phase != "verify")
        {
            using var store = await PublishAsync(path, run, context);
            await CheckMembershipAsync(store, context, projects, calls, missing);
        }
        if (phase != "publish")
        {
            using var reopened = Open(path);
            await CheckMembershipAsync(reopened, context, projects, calls, missing);
        }
        CheckMembershipRows(path, projects, calls, missing);
    }

    [Fact]
    public async Task CompleteScopeMembership_ReplacementRestoresWithoutAccumulating()
    {
        var generations = new[] { ("first", 0), ("partial", 1), ("restored", 0), ("repeat", 0) };
        if (FreshProcessPhase is { } phase && phase.Contains('-', StringComparison.Ordinal))
        {
            var parts = phase.Split('-', 2);
            var (action, generation) = (parts[0], parts[1]);
            var (_, missing) = Assert.Single(generations, item => item.Item1 == generation);
            var context = Context(Repo, "run:membership-" + generation, MembershipFixture(2, 2, missing));
            var path = FreshProcessHarnessPath() + "-replacement";
            Console.WriteLine($"membership phase={phase} pid={Environment.ProcessId} path={path}");
            using (var store = action == "publish" ? await PublishAsync(path, context.IndexRunId.Value, context) : Open(path))
                await CheckMembershipAsync(store, context, 2, 2, missing);
            CheckMembershipRows(path, 2, 2, missing);
            return;
        }

        CodeOccurrenceQuery? firstQuery = null;
        CodeGraphPublication? firstPublication = null;
        foreach (var (run, missing) in generations)
        {
            var context = Context(Repo, "run:membership-" + run, MembershipFixture(2, 2, missing));
            using (var store = await PublishAsync(_path, context.IndexRunId.Value, context))
            {
                await CheckMembershipAsync(store, context, 2, 2, missing);
                if (firstQuery is null)
                {
                    firstQuery = await MembershipQueryAsync(context, 2, 0);
                    firstPublication = await store.GetLatestPublicationAsync(Repo);
                    Assert.NotNull(firstPublication);
                }
            }

            CheckMembershipRows(_path, 2, 2, missing);
            using var reopened = Open(_path);
            await CheckMembershipAsync(reopened, context, 2, 2, missing);
            if (run != "first")
            {
                var stale = await reopened.FindOccurrencesAsync(new CodeOccurrenceQuery(Repo, firstQuery.TargetId, firstPublication)
                {
                    CurrentAuthority = firstQuery.CurrentAuthority,
                });
                Assert.Equal(CodeOccurrenceAvailability.StalePublication, stale.Availability);
                Assert.False(stale.IsComplete);
                Assert.Empty(stale.Occurrences);
                Assert.Null(stale.Coverage);
            }
        }
    }

    [Fact]
    public async Task CompleteScopeMembership_ReopensLegacyRowsWithMatchingAuthority()
    {
        const string run = "run:legacy-membership";
        var fixture = await F.Build();
        var phase = FreshProcessPhase;
        var path = phase is null ? _path : FreshProcessHarnessPath() + "-legacy";
        if (phase is not null)
            Console.WriteLine($"membership phase={phase} pid={Environment.ProcessId} path={path}");

        // The controlled native handoff writes the legacy full counts to each row.
        // Its fixed platform evidence can be rebuilt across processes without
        // including the changed application binaries in the platform reference set.
        if (phase != "verify")
        {
            using (var store = Open(path))
                await fixture.Publish(store, run, fixture.Entries);
            var rows = F.Rows(path, "AndiritOccurrenceUnit").Values;
            Assert.Equal(6, rows.Count);
            Assert.All(rows, row => Assert.Equal(6, row["Coverage"]!["VerifiedUnitCount"]!.GetValue<int>()));
        }
        if (phase != "publish")
        {
            using var reopened = Open(path);
            var result = await reopened.FindOccurrencesAsync(fixture.Query(fixture.LibTarget));
            Assert.Equal(CodeOccurrenceAvailability.Available, result.Availability);
            Assert.True(result.IsComplete);
            Assert.Equal(2, result.Occurrences.Count);
            Assert.Equal(6, result.Coverage!.DeclaredUnitCount);
            Assert.Equal(6, result.Coverage.VerifiedUnitCount);
            Assert.Equal(5, result.Coverage.InvocationFacts);
            Assert.Equal(0, result.Coverage.Unresolved);
            Console.WriteLine($"{run} {result.Availability} {result.Coverage.DeclaredUnitCount}/{result.Coverage.VerifiedUnitCount} " +
                $"complete={result.IsComplete} hits={result.Occurrences.Count} facts={result.Coverage.InvocationFacts}");
        }
    }

    private static Dictionary<string, string> MembershipFixture(int projects, int calls, int missing)
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal);
        var solution = new StringBuilder("<Solution>");
        for (var i = 0; i < projects + missing; i++)
        {
            var path = $"P{i}/P{i}.csproj";
            solution.Append($"<Project Path=\"{path}\"/>");
            if (i >= projects)
                continue;

            files[path] = "<Project Sdk=\"Microsoft.NET.Sdk\" />";
            files[$"P{i}/Value.cs"] = $"namespace P{i}; public static class Value {{ " +
                "public static int Get() => 1; " +
                (i < calls ? "public static int Call() => Get(); }" : "}");
        }
        files["Membership.slnx"] = solution.Append("</Solution>").ToString();
        return files;
    }

    private static async Task<CodeOccurrenceQuery> MembershipQueryAsync(CodeGraphPluginContext context,
        int projects, int missing)
    {
        var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(context);
        var declared = Enumerable.Range(0, projects + missing)
            .Select(i => CSharpProjectDiscovery.IndexUnitId($"P{i}/P{i}.csproj")).Order(StringComparer.Ordinal);
        var verified = Enumerable.Range(0, projects)
            .Select(i => CSharpProjectDiscovery.IndexUnitId($"P{i}/P{i}.csproj")).Order(StringComparer.Ordinal);
        Assert.Equal(declared, analysis.Universe.Scope.Units.Select(unit => unit.ProjectUnitId).Order(StringComparer.Ordinal));
        Assert.Equal(verified, analysis.Universe.Entries.Select(unit => unit.Descriptor.ProjectUnitId).Order(StringComparer.Ordinal));
        var first = analysis.Universe.Entries.Single(unit => unit.Descriptor.ProjectUnitId == CSharpProjectDiscovery.IndexUnitId("P0/P0.csproj"));
        var query = new CodeOccurrenceQuery(Repo, ExpectedTarget(first, Method(first, "P0.Value", "Get")));
        return missing == 0 ? VerifiedCSharpOccurrenceAuthority.Bind(query, analysis.Universe, first.Compilation) : query;
    }

    private static async Task CheckMembershipAsync(LatticeCodeGraphStore store, CodeGraphPluginContext context,
        int projects, int calls, int missing)
    {
        var query = await MembershipQueryAsync(context, projects, missing);
        var result = await store.FindOccurrencesAsync(query);
        var complete = missing == 0;
        Assert.Equal(complete ? CodeOccurrenceAvailability.Available : CodeOccurrenceAvailability.IncompleteAnalysis,
            result.Availability);
        Assert.Equal(context.IndexRunId, result.Publication!.IndexRunId);
        Assert.Equal(complete, result.IsComplete);
        Assert.Equal(complete && calls > 0 ? 1 : 0, result.Occurrences.Count);
        Assert.Equal(projects + missing, result.Coverage!.DeclaredUnitCount);
        Assert.Equal(projects, result.Coverage.VerifiedUnitCount);
        Assert.Equal(complete ? calls : 0, result.Coverage.InvocationFacts);
        Assert.Equal(complete ? 0 : calls, result.Coverage.Unresolved);
        Assert.Equal(0, result.Coverage.External);
        Assert.Equal(0, result.Coverage.Ambiguous);
        Assert.Equal(0, result.Coverage.Unsupported);
        Assert.Equal(0, result.Coverage.UnsupportedCategories);
        Assert.Equal(0, result.Coverage.ConditionalInactive);
        Assert.Empty(result.Coverage.UnavailableReasons);
        if (complete)
        {
            var changed = MembershipFixture(projects, calls, missing);
            changed["P0/Value.cs"] = changed["P0/Value.cs"].Replace("Get() => 1", "Get() => 2", StringComparison.Ordinal);
            var current = await MembershipQueryAsync(Context(Repo, context.IndexRunId.Value, changed), projects, missing);
            var mismatched = await store.FindOccurrencesAsync(query with { CurrentAuthority = current.CurrentAuthority });
            Assert.Equal(CodeOccurrenceAvailability.StalePublication, mismatched.Availability);
            Assert.False(mismatched.IsComplete);
            Assert.Empty(mismatched.Occurrences);
            Assert.Null(mismatched.Coverage);
        }
        Console.WriteLine($"{context.IndexRunId.Value} {result.Availability} {result.Coverage.DeclaredUnitCount}/{result.Coverage.VerifiedUnitCount} " +
            $"complete={result.IsComplete} hits={result.Occurrences.Count} facts={result.Coverage.InvocationFacts} unresolved={result.Coverage.Unresolved}");
    }

    private static void CheckMembershipRows(string path, int projects, int calls, int missing)
    {
        var rows = F.Rows(path, "AndiritOccurrenceUnit").Values.ToArray();
        Assert.Equal(projects, rows.Length);
        Assert.Equal(projects + missing, rows.Sum(row => row["Coverage"]!["DeclaredUnitCount"]!.GetValue<int>()));
        Assert.Equal(missing == 0 ? calls : 0, rows.Sum(row => row["Facts"]!.AsArray().Count));
        Assert.Equal(missing == 0 ? 0 : calls, rows.Sum(row => row["Coverage"]!["Unresolved"]!.GetValue<int>()));
        Assert.All(rows, row =>
        {
            Assert.Equal(1, row["Coverage"]!["VerifiedUnitCount"]!.GetValue<int>());
            if (missing == 0)
            {
                Assert.Equal(1, row["Coverage"]!["DeclaredUnitCount"]!.GetValue<int>());
                var authority = row["Authority"]!;
                Assert.Equal(row["IndexUnitId"]!.GetValue<string>(), authority["SourceUnitId"]!.GetValue<string>());
                Assert.Equal(projects, authority["Members"]!.AsArray().Count);
                Assert.Equal(projects, authority["Coverage"]!["DeclaredUnitCount"]!.GetValue<int>());
                Assert.Equal(projects, authority["Coverage"]!["VerifiedUnitCount"]!.GetValue<int>());
            }
            else
                Assert.Null(row["Authority"]);
        });
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
