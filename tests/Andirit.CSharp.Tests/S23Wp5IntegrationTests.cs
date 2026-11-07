using System.Security.Cryptography;
using System.Text;
using Andirit;
using Microsoft.CodeAnalysis;
using Frozen = AdrCi1Exp.Shared;

namespace Andirit.CSharp.Tests;

// ANDIRIT-CI1A-S2.3-WP-S23-5-QUAL-01 final integration qualification.
//
// Integrated checks across the individually accepted S2.3 packages, on the real
// Windows-x64 native store and the real S2.2 publication path (verified Roslyn
// compilations through the real C# plugin / ingestion sink). Expected values are
// recomputed from the accepted frozen R2 authority and identity primitives, not
// copied from the implementation under test. No production code is modified.
public sealed class S23Wp5IntegrationTests : IDisposable
{
    private readonly string _path = Path.Combine(
        Path.GetTempPath(), "andirit-wp5-" + Guid.NewGuid().ToString("N") + ".ltdb");

    public void Dispose()
    {
        foreach (var suffix in new[] { "", "-wal", "-journal", ".wal" })
            if (File.Exists(_path + suffix)) File.Delete(_path + suffix);
    }

    private static readonly CodeRepositoryId Repo = new("repo:wp5-integration");
    private static readonly DateTimeOffset Started = new(2026, 11, 7, 12, 0, 0, TimeSpan.Zero);

    // ---- I-01: real multi-file/multi-project/multi-namespace end-to-end ----

    [Fact]
    public async Task I01_RealProjectFixture_PublishReopenCrossProject_WithOverloadTargets()
    {
        var files = RealProjectFixture();
        Assert.True(files.Keys.Count(k => k.EndsWith(".cs", StringComparison.Ordinal)) >= 10);
        Assert.True(files.Keys.Count(k => k.EndsWith(".csproj", StringComparison.Ordinal)) >= 2);
        var context = Context("run:a", files);
        var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(context);
        var core = analysis.Projects.Single(p => p.Project.Path == "Core/Core.csproj").Verified!;
        var consumer = analysis.Projects.Single(p => p.Project.Path == "Consumer/Consumer.csproj").Verified!;
        var ping0 = ExpectedTarget(core, Method(core, "Core.Api", "Ping", 0));
        var ping1 = ExpectedTarget(core, Method(core, "Core.Api", "Ping", 1));

        using (var store = new LatticeCodeGraphStore(_path))
            await PublishRealAsync(store, "run:a", context);

        // D3: occurrence facts are never graph nodes/edges, and authority rows
        // never leak into the graph unit rows.
        var graphUnits = await GetGraphAsync(_path);
        Assert.DoesNotContain(ping0.Value, graphUnits);
        Assert.DoesNotContain(ping1.Value, graphUnits);

        using var reopened = new LatticeCodeGraphStore(_path);
        var zero = await reopened.FindOccurrencesAsync(Bind(ping0, analysis, consumer));
        var one = await reopened.FindOccurrencesAsync(Bind(ping1, analysis, consumer));
        Assert.Equal(CodeOccurrenceAvailability.Available, zero.Availability);
        Assert.Equal(CodeOccurrenceAvailability.Available, one.Availability);
        Assert.True(zero.IsComplete && one.IsComplete);
        Assert.Equal(30, zero.Occurrences.Count); // 10 files x 3 ping()
        Assert.Equal(20, one.Occurrences.Count);  // 10 files x 2 ping(int)
        Assert.All(zero.Occurrences, o => Assert.Equal(consumer.Descriptor.ProjectUnitId, o.SourceUnitId));
        Assert.All(one.Occurrences, o => Assert.Equal(consumer.Descriptor.ProjectUnitId, o.SourceUnitId));
    }

    // ---- I-02: recovery x carrier x authority on the real plugin path ----

    [Fact]
    public async Task I02_RecoveryOverAuthorityAndCarrier_MatchesDurableReopen_AndKeepsPriorPublication()
    {
        var contextA = Context("run:a", CrossProject("public static int Get() => 1;"));
        var (analysisA, appA, libA) = await LiveAsync(contextA);
        var targetA = ExpectedTarget(libA, Method(libA, "Lib.Value", "Get", 0));

        using (var setup = new LatticeCodeGraphStore(_path))
            await PublishRealAsync(setup, "run:a", contextA);

        var arm = false;
        CodeOccurrenceQueryResult recovered;
        using (var store = new LatticeCodeGraphStore(_path, null, point =>
            { if (arm && point == "before-commit") throw new Wp5FaultException(); }))
        {
            var contextB = Context("run:b", CrossProject("public static int Get() => 2;"));
            await StageRealAsync(store, "run:b", contextB);
            arm = true;
            await Assert.ThrowsAsync<Wp5FaultException>(
                async () => await CompleteAsync(store, "run:b"));
            arm = false;

            recovered = await store.FindOccurrencesAsync(Bind(targetA, analysisA, appA));
            Assert.Equal(CodeOccurrenceAvailability.Available, recovered.Availability);
            Assert.Single(recovered.Occurrences);
            Assert.Equal("run:a", recovered.Publication!.IndexRunId.Value);

            // Graph-owner correspondence survives the recovery rebuild.
            var published = await store.GetPublishedUnitsAsync(Repo);
            Assert.Equal(2, published.Count);
            Assert.All(published, unit => Assert.True(unit.RequiresOccurrenceAuthority));
        }

        using (var reopened = new LatticeCodeGraphStore(_path))
        {
            var durable = await reopened.FindOccurrencesAsync(Bind(targetA, analysisA, appA));
            Assert.Equal(durable.Availability, recovered.Availability);
            Assert.Equal(durable.Publication, recovered.Publication);
            Assert.Equal(
                recovered.Occurrences.Select(o => o.Id.Value).Order(StringComparer.Ordinal),
                durable.Occurrences.Select(o => o.Id.Value).Order(StringComparer.Ordinal));
        }

        // Accepted re-stage branch: fresh verified authority into the still-running run.
        var contextB2 = Context("run:b", CrossProject("public static int Get() => 2;"));
        var (analysisB, appB, libB2) = await LiveAsync(contextB2);
        var targetB = ExpectedTarget(libB2, Method(libB2, "Lib.Value", "Get", 0));
        using (var resumed = new LatticeCodeGraphStore(_path))
        {
            await StageRealAsync(resumed, "run:b", contextB2);
            await CompleteAsync(resumed, "run:b");
        }
        using var final = new LatticeCodeGraphStore(_path);
        Assert.Equal(CodeOccurrenceAvailability.Available,
            (await final.FindOccurrencesAsync(Bind(targetB, analysisB, appB))).Availability);
        Assert.Equal(CodeOccurrenceAvailability.StalePublication,
            (await final.FindOccurrencesAsync(Bind(targetA, analysisA, appA))).Availability);
    }

    // ---- I-03 / I-07: real source change, stale refusal, deletion, reopen ----

    [Fact]
    public async Task I03_I07_RealSourceChange_StaleRefusal_Deletion_AndReopen()
    {
        var contextA = Context("run:a", CrossProject("public static int Get() => 1;"));
        var (analysisA, appA, libA) = await LiveAsync(contextA);
        var targetA = ExpectedTarget(libA, Method(libA, "Lib.Value", "Get", 0));
        using (var store = new LatticeCodeGraphStore(_path))
            await PublishRealAsync(store, "run:a", contextA);

        var contextB = Context("run:b", CrossProject("public static int Get() => 2;"));
        var (analysisB, appB, libB) = await LiveAsync(contextB);
        var targetB = ExpectedTarget(libB, Method(libB, "Lib.Value", "Get", 0));
        using (var store = new LatticeCodeGraphStore(_path))
            await PublishRealAsync(store, "run:b", contextB);

        using (var reopened = new LatticeCodeGraphStore(_path))
        {
            var current = await reopened.FindOccurrencesAsync(Bind(targetB, analysisB, appB));
            Assert.Equal(CodeOccurrenceAvailability.Available, current.Availability);
            Assert.Single(current.Occurrences);
            var stale = await reopened.FindOccurrencesAsync(Bind(targetA, analysisA, appA));
            Assert.Equal(CodeOccurrenceAvailability.StalePublication, stale.Availability);
            Assert.Equal(0, stale.Examined);
        }

        // Retire the referencing unit; the defining unit's zero-fact analysis
        // remains, producing a complete-empty answer (never unavailable).
        var plugin = new CSharpCodeGraphPlugin();
        using (var store = new LatticeCodeGraphStore(_path))
        {
            await store.StoreIndexRunAsync(new(Repo, new("run:delete"), Started.AddSeconds(5), plugins: [plugin.Id]));
            await store.StageIndexUnitDeletionAsync(Repo, new("run:delete"), plugin.Id,
                new CodeIndexUnitId(appB.Descriptor.ProjectUnitId));
            await store.CompleteIndexRunAsync(
                new(Repo, new("run:delete"), Started.AddSeconds(5), CodeIndexRunStatus.Completed,
                    Started.AddSeconds(6), [plugin.Id]),
                new(Repo, new("run:delete"), []));
        }
        using var final = new LatticeCodeGraphStore(_path);
        var empty = await final.FindOccurrencesAsync(Bind(targetB, analysisB, appB));
        Assert.Equal(CodeOccurrenceAvailability.Available, empty.Availability);
        Assert.Empty(empty.Occurrences);
        Assert.True(empty.IsComplete);
    }

    // ---- I-09: integrated availability / refusal across the real publication ----

    [Fact]
    public async Task I09_IntegratedAvailability_NoFalseAvailable_NoFalseEmpty()
    {
        var context = Context("run:a", CrossProject("public static int Get() => 1;"));
        var (analysis, app, lib) = await LiveAsync(context);
        var target = ExpectedTarget(lib, Method(lib, "Lib.Value", "Get", 0));
        using (var store = new LatticeCodeGraphStore(_path))
            await PublishRealAsync(store, "run:a", context);

        using var reopened = new LatticeCodeGraphStore(_path);
        // Missing current authority must refuse as unavailable, never empty-success.
        var noAuthority = await reopened.FindOccurrencesAsync(new CodeOccurrenceQuery(Repo, target));
        Assert.Equal(CodeOccurrenceAvailability.AnalysisUnavailable, noAuthority.Availability);
        Assert.Empty(noAuthority.Occurrences);
        Assert.False(noAuthority.IsComplete);
        // A foreign publication handle refuses as stale with no scan.
        var foreign = new CodeGraphPublication(Repo, new CodeIndexRunId("run:foreign"), null, true,
            new CodeIndexIdentity("identity:foreign"));
        var stale = await reopened.FindOccurrencesAsync(new CodeOccurrenceQuery(Repo, target, foreign));
        Assert.Equal(CodeOccurrenceAvailability.StalePublication, stale.Availability);
        Assert.Equal(0, stale.Examined);
        // The current verified authority is available and complete.
        var current = await reopened.FindOccurrencesAsync(Bind(target, analysis, app));
        Assert.Equal(CodeOccurrenceAvailability.Available, current.Availability);
        Assert.True(current.IsComplete);
    }

    // ---- helpers -----------------------------------------------------------

    private static CodeOccurrenceQuery Bind(
        CodeOccurrenceTargetId target, VerifiedCSharpAnalysis analysis, VerifiedCSharpCompilation caller) =>
        VerifiedCSharpOccurrenceAuthority.Bind(new(Repo, target), analysis.Universe, caller.Compilation);

    private static IReadOnlyDictionary<string, string> CrossProject(string libBody) =>
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Lib/Lib.csproj"] = "<Project Sdk=\"Microsoft.NET.Sdk\" />",
            ["Lib/Value.cs"] = "namespace Lib; public static class Value { " + libBody + " }",
            ["App/App.csproj"] =
                "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><ProjectReference Include=\"../Lib/Lib.csproj\" /></ItemGroup></Project>",
            ["App/Application.cs"] =
                "namespace App; public static class Application { public static int Run() => Lib.Value.Get(); }",
        };

    // A genuine multi-file, multi-project, multi-namespace real fixture with
    // overloaded cross-project targets and >50 invocations.
    private static IReadOnlyDictionary<string, string> RealProjectFixture()
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Core/Core.csproj"] = "<Project Sdk=\"Microsoft.NET.Sdk\" />",
            ["Core/Api.cs"] =
                "namespace Core; public static class Api { " +
                "public static int Ping() => 1; public static int Ping(int a) => a; " +
                "public static int Ping(int a, int b) => a + b; " +
                "public static int Pong() => 2; public static int Pong(int a) => a; " +
                "public static int Pong(int a, int b) => a + b; }",
            ["Consumer/Consumer.csproj"] =
                "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><ProjectReference Include=\"../Core/Core.csproj\" /></ItemGroup></Project>",
        };
        for (var i = 0; i < 5; i++)
            files[$"Consumer/File{i}.cs"] = ConsumerFile(i, "Consumer");
        for (var i = 0; i < 5; i++)
            files[$"Consumer/Sub{i}.cs"] = ConsumerFile(i, "Consumer.Sub");
        return files;
    }

    private static string ConsumerFile(int i, string ns) =>
        $"namespace {ns}; public static class Consumer{i} {{ public static int Run() => " +
        "Core.Api.Ping() + Core.Api.Ping() + Core.Api.Ping() + " +
        "Core.Api.Ping(1) + Core.Api.Ping(2); }";

    private static CodeGraphPluginContext Context(string run, IReadOnlyDictionary<string, string> files) =>
        new(Repo, null, new CodeIndexRunId(run),
            files.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair =>
            {
                var bytes = Encoding.UTF8.GetBytes(pair.Value);
                return new CodeGraphSource(pair.Key,
                    "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)),
                    _ => ValueTask.FromResult<Stream>(new MemoryStream(bytes, writable: false)));
            }).ToArray());

    private static async Task PublishRealAsync(
        LatticeCodeGraphStore store, string run, CodeGraphPluginContext context)
    {
        await store.UpsertRepositoryAsync(new(Repo));
        await StageRealAsync(store, run, context);
        await CompleteAsync(store, run);
    }

    private static async Task StageRealAsync(
        LatticeCodeGraphStore store, string run, CodeGraphPluginContext context)
    {
        var plugin = new CSharpCodeGraphPlugin();
        if (await store.GetIndexRunAsync(Repo, new(run)) is null)
            await store.StoreIndexRunAsync(new(Repo, new(run), Started, plugins: [plugin.Id]));
        await using (var sink = new CodeGraphIngestionSink(store))
        await using (var session = await plugin.CreateSessionAsync(context))
        {
            await session.ExtractAsync(sink);
            await sink.CompleteAsync();
        }
    }

    private static Task CompleteAsync(LatticeCodeGraphStore store, string run)
    {
        var plugin = new CSharpCodeGraphPlugin();
        return store.CompleteIndexRunAsync(
            new(Repo, new(run), Started, CodeIndexRunStatus.Completed, Started.AddSeconds(1), [plugin.Id]),
            new(Repo, new(run), [])).AsTask();
    }

    private static async Task<(VerifiedCSharpAnalysis Analysis, VerifiedCSharpCompilation App, VerifiedCSharpCompilation Lib)>
        LiveAsync(CodeGraphPluginContext context)
    {
        var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(context);
        return (analysis,
            analysis.Projects.Single(p => p.Project.Path == "App/App.csproj").Verified!,
            analysis.Projects.Single(p => p.Project.Path == "Lib/Lib.csproj").Verified!);
    }

    private static IMethodSymbol Method(VerifiedCSharpCompilation unit, string type, string name, int arity) =>
        unit.Compilation.GetTypeByMetadataName(type)!.GetMembers(name)
            .OfType<IMethodSymbol>().Single(m => m.Parameters.Length == arity);

    private static CodeOccurrenceTargetId ExpectedTarget(VerifiedCSharpCompilation defining, IMethodSymbol method)
    {
        var mapped = defining.MapR2FaithfulCompctx1();
        var identifier = CodeOccurrenceIdentity.ComputeIdentifierDigest([method.Name]);
        var declaration = CodeOccurrenceIdentity.ComputeDeclarationDigest([method.GetDocumentationCommentId() ?? method.Name]);
        var shape = CodeOccurrenceIdentity.ComputeShapeDigest("method|" + method.Arity + "|" +
            string.Join(",", method.Parameters.Select(p => p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))));
        var viaIdentity = CodeOccurrenceIdentity.ComputeTargetId(
            mapped.DescriptorDigest, mapped.MetadataIdentity, mapped.ModuleName, identifier, declaration, shape);
        var frozen = Frozen.CompilationProvenance.Describe(Repo.Value, defining.Descriptor.ProjectUnitId,
            defining.Compilation, defining.Sources.Select(s => (s.Path, Encoding.UTF8.GetBytes(s.Text.ToString()))).ToArray());
        var viaFrozen = "asmctx3:sha256:" + Frozen.ProvenanceHashing.Hash("asmctx3-context",
            ["compctx1", frozen.DescriptorDigest, frozen.MetadataIdentity, frozen.ModuleName, identifier, declaration, shape]);
        Assert.Equal(viaIdentity.Value, viaFrozen);
        return viaIdentity;
    }

    // Reads the durable graph unit payloads as raw text for a D3 node/edge check.
    private static async Task<string> GetGraphAsync(string path)
    {
        using var store = new LatticeCodeGraphStore(path);
        var units = await store.GetPublishedUnitsAsync(Repo);
        return string.Join("|",
            units.SelectMany(u => u.Nodes).Select(n => n.Id.Value)
                .Concat(units.SelectMany(u => u.Edges).Select(e => e.Id.Value)));
    }

    private sealed class Wp5FaultException : Exception;
}
