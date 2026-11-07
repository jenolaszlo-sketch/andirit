using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Andirit;
using LatticeDbSharp;
using Microsoft.CodeAnalysis;
using Frozen = AdrCi1Exp.Shared;

// ANDIRIT-CI1A-S2.3-WP-S23-5-QUAL-01 reviewer-owned process harness.
//
// Integrated final-qualification probes across the individually accepted S2.3
// packages. Each verb is one OS process, so fresh-process reopen boundaries are
// real. Expected values are recomputed from the accepted frozen R2 authority and
// identity primitives, not from producer output.
//
// Usage: dotnet run --project ... -c Release -- <verb> <database> <outputJson> [detail]
var action = args[0];
var database = args[1];
var output = args[2];
var detail = args.Length > 3 ? args[3] : "";
var result = await S23Wp5QualProbe.RunAsync(action, database, detail);
File.WriteAllText(output, JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine("RESULT " + (result["ok"]!.GetValue<bool>() ? "OK" : "FAIL") + " " + action +
    " pid=" + result["processId"]!.GetValue<int>());
return result["ok"]!.GetValue<bool>() ? 0 : 1;

internal static class S23Wp5QualProbe
{
    internal static readonly CodeRepositoryId Repo = new("repo:wp5-qual");
    internal static readonly DateTimeOffset Started = new(2026, 11, 7, 12, 0, 0, TimeSpan.Zero);

    internal static async Task<JsonObject> RunAsync(string action, string database, string detail)
    {
        var root = new JsonObject
        {
            ["action"] = action,
            ["processId"] = Environment.ProcessId,
            ["database"] = database,
            ["os"] = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
            ["architecture"] = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString(),
            ["runtime"] = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
        };
        var notes = new JsonArray();
        root["ok"] = true;
        try
        {
            switch (action)
            {
                case "environment":
                    root["head"] = detail;
                    break;
                case "publish":
                    await PublishAsync(database, root, notes);
                    break;
                case "verify":
                    await VerifyAsync(database, root, notes);
                    break;
                case "recovery-fault":
                    await RecoveryFaultAsync(database, root, notes);
                    break;
                case "recovery-verify":
                    await RecoveryVerifyAsync(database, root, notes);
                    break;
                case "namespace-corrupt":
                    await NamespaceCorruptAsync(database, root, notes);
                    break;
                case "namespace-open":
                    await NamespaceOpenAsync(database, root, notes);
                    break;
                case "lifecycle":
                    await LifecycleAsync(database, root, notes);
                    break;
                default:
                    throw new ArgumentException("Unknown action: " + action);
            }
        }
        catch (Exception exception)
        {
            notes.Add(exception.GetType().Name + ": " + exception.Message);
            root["ok"] = false;
        }
        root["notes"] = notes;
        return root;
    }

    // ---- I-01: real multi-file/multi-project fixture, fresh-process reopen ----

    private static async Task PublishAsync(string database, JsonObject root, JsonArray notes)
    {
        var context = Context("run:a", RealProjectFixture());
        var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(context);
        var core = analysis.Projects.Single(p => p.Project.Path == "Core/Core.csproj").Verified!;
        var ping0 = ExpectedTarget(core, Method(core, "Core.Api", "Ping", 0));
        var ping1 = ExpectedTarget(core, Method(core, "Core.Api", "Ping", 1));
        using (var store = new LatticeCodeGraphStore(database))
            await PublishRealAsync(store, "run:a", context);
        root["ping0"] = ping0.Value;
        root["ping1"] = ping1.Value;
        root["rows"] = Counts(database);
        Assert(root, analysis.Universe.IsComplete, "verified universe is complete", notes);
    }

    private static async Task VerifyAsync(string database, JsonObject root, JsonArray notes)
    {
        var context = Context("run:a", RealProjectFixture());
        var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(context);
        var core = analysis.Projects.Single(p => p.Project.Path == "Core/Core.csproj").Verified!;
        var consumer = analysis.Projects.Single(p => p.Project.Path == "Consumer/Consumer.csproj").Verified!;
        var ping0 = ExpectedTarget(core, Method(core, "Core.Api", "Ping", 0));
        var ping1 = ExpectedTarget(core, Method(core, "Core.Api", "Ping", 1));
        using (var store = new LatticeCodeGraphStore(database))
        {
            var zero = await store.FindOccurrencesAsync(Bind(ping0, analysis, consumer));
            var one = await store.FindOccurrencesAsync(Bind(ping1, analysis, consumer));
            root["ping0"] = Describe(zero);
            root["ping1"] = Describe(one);
            root["sourceUnitMatches"] = zero.Occurrences.All(o => o.SourceUnitId == consumer.Descriptor.ProjectUnitId);
            Assert(root, zero.Availability == CodeOccurrenceAvailability.Available && zero.IsComplete && zero.Occurrences.Count == 30,
                "reopened overload target Ping() serves 30 complete real occurrences", notes);
            Assert(root, one.Availability == CodeOccurrenceAvailability.Available && one.IsComplete && one.Occurrences.Count == 20,
                "reopened overload target Ping(int) serves 20 complete real occurrences", notes);
        }
    }

    // ---- I-02: recovery x authority x carrier ----

    private static async Task RecoveryFaultAsync(string database, JsonObject root, JsonArray notes)
    {
        var contextA = Context("run:a", CrossProject("public static int Get() => 1;"));
        var (analysisA, appA, libA) = await LiveAsync(contextA);
        var targetA = ExpectedTarget(libA, Method(libA, "Lib.Value", "Get", 0));
        using (var setup = new LatticeCodeGraphStore(database))
            await PublishRealAsync(setup, "run:a", contextA);

        var arm = false;
        using (var store = new LatticeCodeGraphStore(database, null, point =>
            { if (arm && point == "before-commit") throw new ProbeFault(); }))
        {
            await StageRealAsync(store, "run:b", Context("run:b", CrossProject("public static int Get() => 2;")));
            arm = true;
            try { await CompleteAsync(store, "run:b"); }
            catch (ProbeFault) { notes.Add("PASS: managed before-commit fault raised"); }
            arm = false;
            var recovered = await store.FindOccurrencesAsync(Bind(targetA, analysisA, appA));
            root["recovered"] = Describe(recovered);
            var published = await store.GetPublishedUnitsAsync(Repo);
            root["publishedUnits"] = published.Count;
            root["allRequireAuthority"] = published.All(u => u.RequiresOccurrenceAuthority);
            Assert(root, recovered.Availability == CodeOccurrenceAvailability.Available &&
                recovered.Occurrences.Count == 1 && recovered.Publication!.IndexRunId.Value == "run:a",
                "recovered in-process projection keeps the prior authority publication", notes);
            Assert(root, published.Count == 2 && published.All(u => u.RequiresOccurrenceAuthority),
                "graph-owner correspondence survives the recovery rebuild", notes);
        }
    }

    private static async Task RecoveryVerifyAsync(string database, JsonObject root, JsonArray notes)
    {
        var contextA = Context("run:a", CrossProject("public static int Get() => 1;"));
        var (analysisA, appA, libA) = await LiveAsync(contextA);
        var targetA = ExpectedTarget(libA, Method(libA, "Lib.Value", "Get", 0));
        using var store = new LatticeCodeGraphStore(database);
        var durable = await store.FindOccurrencesAsync(Bind(targetA, analysisA, appA));
        root["durable"] = Describe(durable);
        Assert(root, durable.Availability == CodeOccurrenceAvailability.Available &&
            durable.Occurrences.Count == 1 && durable.Publication!.IndexRunId.Value == "run:a",
            "fresh reopen after the fault reconstructs the prior authority publication", notes);
    }

    // ---- I-04 / SQ-06: isolated namespace mismatch ----

    private static async Task NamespaceCorruptAsync(string database, JsonObject root, JsonArray notes)
    {
        var context = Context("run:a", CrossProject("public static int Get() => 1;"));
        using (var store = new LatticeCodeGraphStore(database))
            await PublishRealAsync(store, "run:a", context);
        using (var db = LatticeDatabase.Open(database, new LatticeDatabaseOptions { Create = false }))
        using (var txn = db.BeginWriteTransaction())
        {
            var row = MetaRow(db, txn);
            root["versionBefore"] = row["SchemaVersion"]!.GetValue<int>();
            row["Namespace"] = "wrong-occurrence-facts/9";
            using var q = db.Prepare("MATCH (s:AndiritOccurrenceMeta) WHERE s.key = $k SET s.payload = $p");
            q.Bind("k", LatticeValue.From(Repo.Value));
            q.Bind("p", LatticeValue.From(Convert.ToBase64String(Encoding.UTF8.GetBytes(row.ToJsonString()))));
            using var r = q.Execute(txn);
            r.ReadAll();
            txn.Commit();
        }
        Assert(root, true, "namespace tamper applied with the schema version left valid", notes);
    }

    private static async Task NamespaceOpenAsync(string database, JsonObject root, JsonArray notes)
    {
        await Task.CompletedTask;
        try
        {
            using var store = new LatticeCodeGraphStore(database);
            root["opened"] = true;
            Assert(root, false, "namespace-mismatch store must not open", notes);
        }
        catch (CodeOccurrenceSchemaException exception)
        {
            root["schemaException"] = true;
            root["expectedVersion"] = exception.ExpectedVersion;
            root["actualVersion"] = exception.ActualVersion;
            root["message"] = exception.Message;
            Assert(root, exception.ExpectedVersion == LatticeCodeGraphStore.CurrentOccurrenceSchemaVersion &&
                exception.ActualVersion == LatticeCodeGraphStore.CurrentOccurrenceSchemaVersion &&
                exception.Message.Contains("namespace", StringComparison.OrdinalIgnoreCase),
                "isolated namespace mismatch (valid version) refused with CodeOccurrenceSchemaException", notes);
        }
    }

    // ---- I-03 / I-07: real source change, stale, deletion, reopen ----

    private static async Task LifecycleAsync(string database, JsonObject root, JsonArray notes)
    {
        var contextA = Context("run:a", CrossProject("public static int Get() => 1;"));
        var (analysisA, appA, libA) = await LiveAsync(contextA);
        var targetA = ExpectedTarget(libA, Method(libA, "Lib.Value", "Get", 0));
        using (var store = new LatticeCodeGraphStore(database))
            await PublishRealAsync(store, "run:a", contextA);

        var contextB = Context("run:b", CrossProject("public static int Get() => 2;"));
        var (analysisB, appB, libB) = await LiveAsync(contextB);
        var targetB = ExpectedTarget(libB, Method(libB, "Lib.Value", "Get", 0));
        using (var store = new LatticeCodeGraphStore(database))
            await PublishRealAsync(store, "run:b", contextB);

        using (var reopened = new LatticeCodeGraphStore(database))
        {
            var current = await reopened.FindOccurrencesAsync(Bind(targetB, analysisB, appB));
            var stale = await reopened.FindOccurrencesAsync(Bind(targetA, analysisA, appA));
            root["current"] = Describe(current);
            root["stale"] = Describe(stale);
            Assert(root, current.Availability == CodeOccurrenceAvailability.Available && current.Occurrences.Count == 1,
                "reopened publication serves the current real fact", notes);
            Assert(root, stale.Availability == CodeOccurrenceAvailability.StalePublication && stale.Examined == 0,
                "superseded publication refuses as stale with no hits", notes);
        }

        var plugin = new CSharpCodeGraphPlugin();
        using (var store = new LatticeCodeGraphStore(database))
        {
            await store.StoreIndexRunAsync(new(Repo, new("run:delete"), Started.AddSeconds(5), plugins: [plugin.Id]));
            await store.StageIndexUnitDeletionAsync(Repo, new("run:delete"), plugin.Id,
                new CodeIndexUnitId(appB.Descriptor.ProjectUnitId));
            await store.CompleteIndexRunAsync(
                new(Repo, new("run:delete"), Started.AddSeconds(5), CodeIndexRunStatus.Completed,
                    Started.AddSeconds(6), [plugin.Id]),
                new(Repo, new("run:delete"), []));
        }
        using var final = new LatticeCodeGraphStore(database);
        var empty = await final.FindOccurrencesAsync(Bind(targetB, analysisB, appB));
        root["afterDelete"] = Describe(empty);
        Assert(root, empty.Availability == CodeOccurrenceAvailability.Available && empty.Occurrences.Count == 0 && empty.IsComplete,
            "retiring the referencing unit leaves a complete-empty answer, not stale/available leftovers", notes);
    }

    // ---- helpers ----

    private static void Assert(JsonObject root, bool condition, string message, JsonArray notes)
    {
        notes.Add((condition ? "PASS: " : "FAIL: ") + message);
        if (!condition) root["ok"] = false;
    }

    private static JsonObject Describe(CodeOccurrenceQueryResult result) => new()
    {
        ["availability"] = result.Availability.ToString(),
        ["occurrences"] = result.Occurrences.Count,
        ["isComplete"] = result.IsComplete,
        ["publicationRun"] = result.Publication?.IndexRunId.Value,
    };

    private static CodeOccurrenceQuery Bind(
        CodeOccurrenceTargetId target, VerifiedCSharpAnalysis analysis, VerifiedCSharpCompilation caller) =>
        VerifiedCSharpOccurrenceAuthority.Bind(new(Repo, target), analysis.Universe, caller.Compilation);

    private static JsonObject Counts(string database)
    {
        var result = new JsonObject();
        using var db = LatticeDatabase.Open(database, new LatticeDatabaseOptions { Create = true });
        using var txn = db.BeginReadTransaction();
        foreach (var label in new[] { "AndiritOccurrenceUnit", "AndiritOccurrenceMeta", "AndiritOccurrencePage", "HetuUnit" })
        {
            using var q = db.Prepare($"MATCH (s:{label}) RETURN count(s)");
            using var rows = q.Execute(txn);
            var all = rows.ReadAll();
            result[label] = all.Count == 0 ? 0 : (int)all[0].GetValue(0).AsInt64();
        }
        txn.Commit();
        return result;
    }

    private static JsonObject MetaRow(LatticeDatabase db, LatticeTransaction txn)
    {
        using var q = db.Prepare("MATCH (s:AndiritOccurrenceMeta) WHERE s.key = $k RETURN s.payload");
        q.Bind("k", LatticeValue.From(Repo.Value));
        using var rows = q.Execute(txn);
        var all = rows.ReadAll();
        if (all.Count != 1) throw new InvalidOperationException("Expected exactly one occurrence meta row.");
        return JsonNode.Parse(Convert.FromBase64String(all[0].GetValue(0).AsString()))!.AsObject();
    }

    // ---- real fixtures / plugin path ----

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
        for (var i = 0; i < 5; i++) files[$"Consumer/File{i}.cs"] = ConsumerFile(i, "Consumer");
        for (var i = 0; i < 5; i++) files[$"Consumer/Sub{i}.cs"] = ConsumerFile(i, "Consumer.Sub");
        return files;
    }

    private static string ConsumerFile(int i, string ns) =>
        $"namespace {ns}; public static class Consumer{i} {{ public static int Run() => " +
        "Core.Api.Ping() + Core.Api.Ping() + Core.Api.Ping() + Core.Api.Ping(1) + Core.Api.Ping(2); }";

    private static CodeGraphPluginContext Context(string run, IReadOnlyDictionary<string, string> files) =>
        new(Repo, null, new CodeIndexRunId(run),
            files.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair =>
            {
                var bytes = Encoding.UTF8.GetBytes(pair.Value);
                return new CodeGraphSource(pair.Key,
                    "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)),
                    _ => ValueTask.FromResult<Stream>(new MemoryStream(bytes, writable: false)));
            }).ToArray());

    private static async Task PublishRealAsync(LatticeCodeGraphStore store, string run, CodeGraphPluginContext context)
    {
        await store.UpsertRepositoryAsync(new(Repo));
        await StageRealAsync(store, run, context);
        await CompleteAsync(store, run);
    }

    private static async Task StageRealAsync(LatticeCodeGraphStore store, string run, CodeGraphPluginContext context)
    {
        var plugin = new CSharpCodeGraphPlugin();
        if (await store.GetIndexRunAsync(Repo, new(run)) is null)
            await store.StoreIndexRunAsync(new(Repo, new(run), Started, plugins: [plugin.Id]));
        await using var sink = new CodeGraphIngestionSink(store);
        await using var session = await plugin.CreateSessionAsync(context);
        await session.ExtractAsync(sink);
        await sink.CompleteAsync();
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
        if (viaIdentity.Value != viaFrozen)
            throw new InvalidDataException("Independent target computations disagree: " + viaIdentity.Value + " vs " + viaFrozen);
        return viaIdentity;
    }

    private sealed class ProbeFault : Exception;
}
