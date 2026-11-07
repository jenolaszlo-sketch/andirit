using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Andirit;
using Andirit.CSharp.Tests;
using LatticeDbSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Frozen = AdrCi1Exp.Shared;

// ANDIRIT-CI1A-S2.3-WP-S23-4-S23-LIFE-Q1 reviewer-owned process harness.
//
// Independent of the producer's test sources and evidence: every phase rebuilds
// live verified authority from its own fixture sources and asserts against the
// accepted frozen R2 authority and identity primitives, not against producer
// output. Each verb is one OS process, so fresh-process reopen boundaries are
// real. See run-probes.ps1 for the schedule.
//
// Usage: dotnet run --project ... -c Release -- <verb> <database> <outputJson> [detail]
var action = args[0];
var database = args[1];
var output = args[2];
var detail = args.Length > 3 ? args[3] : "";
var result = await S23LifeQ1Probe.RunAsync(action, database, detail);
File.WriteAllText(output, JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine("RESULT " + (result["ok"]!.GetValue<bool>() ? "OK" : "FAIL") + " " + action +
    " pid=" + result["processId"]!.GetValue<int>());
return result["ok"]!.GetValue<bool>() ? 0 : 1;

internal static class S23LifeQ1Probe
{
    internal static readonly CodeRepositoryId Repo = new("repo:s23-life-q1");
    internal static readonly DateTimeOffset Started = new(2026, 11, 7, 9, 0, 0, TimeSpan.Zero);

    internal static async Task<JsonObject> RunAsync(string action, string database, string detail)
    {
        var root = new JsonObject
        {
            ["action"] = action,
            ["detail"] = detail,
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
                case "plugin-publish":
                    await PluginPublishAsync(database, root, notes, detail.Length == 0 ? "public static int Get() => 1;" : detail);
                    break;
                case "plugin-verify":
                    await PluginVerifyAsync(database, root, notes, detail.Length == 0 ? "public static int Get() => 1;" : detail);
                    break;
                case "plugin-negative":
                    await PluginNegativeAsync(database, root, notes);
                    break;
                case "plugin-fresh-verify":
                    await PluginFreshVerifyAsync(database, root, notes);
                    break;
                case "authority-reopen":
                    await AuthorityReopenAsync(database, root, notes);
                    break;
                case "lifecycle":
                    await LifecycleAsync(database, root, notes);
                    break;
                case "sq03-schedule":
                    await Sq03ScheduleAsync(database, root, notes);
                    break;
                case "sq03-concurrent":
                    await Sq03ConcurrentAsync(database, root, notes);
                    break;
                case "environment":
                    root["head"] = detail;
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

    // ---- real-plugin cross-project publication (fresh-process boundaries) ----

    private static async Task PluginPublishAsync(string database, JsonObject root, JsonArray notes, string libBody)
    {
        var context = Context("run:a", CrossProject(libBody));
        var plugin = new CSharpCodeGraphPlugin();
        using (var store = new LatticeCodeGraphStore(database))
        {
            await store.UpsertRepositoryAsync(new(Repo));
            await store.StoreIndexRunAsync(new(Repo, new("run:a"), Started, plugins: [plugin.Id]));
            await using (var sink = new CodeGraphIngestionSink(store))
            await using (var session = await plugin.CreateSessionAsync(context))
            {
                await session.ExtractAsync(sink);
                await sink.CompleteAsync();
            }
            await store.CompleteIndexRunAsync(
                new(Repo, new("run:a"), Started, CodeIndexRunStatus.Completed, Started.AddSeconds(1), [plugin.Id]),
                new(Repo, new("run:a"), []));
        }
        var (analysis, app, lib) = await LiveAsync(context);
        var target = ExpectedTarget(lib, Method(lib, "Lib.Value", "Get"));
        root["targetId"] = target.Value;
        root["sourceUnitId"] = app.Descriptor.ProjectUnitId;
        root["rows"] = Rows(database, "AndiritOccurrenceUnit", "AndiritOccurrenceMeta", "AndiritOccurrencePage");
        Assert(root, analysis.Universe.IsComplete, "verified universe is complete", notes);
    }

    private static async Task PluginVerifyAsync(string database, JsonObject root, JsonArray notes, string libBody)
    {
        var context = Context("run:a", CrossProject(libBody));
        var (analysis, app, lib) = await LiveAsync(context);
        var target = ExpectedTarget(lib, Method(lib, "Lib.Value", "Get"));
        root["targetId"] = target.Value;
        using (var store = new LatticeCodeGraphStore(database))
        {
            var query = VerifiedCSharpOccurrenceAuthority.Bind(new(Repo, target), analysis.Universe, app.Compilation);
            var result = await store.FindOccurrencesAsync(query);
            root["availability"] = result.Availability.ToString();
            root["occurrences"] = result.Occurrences.Count;
            root["isComplete"] = result.IsComplete;
            root["observedTarget"] = result.Occurrences.Count == 1 ? result.Occurrences[0].TargetId.Value : null;
            root["sourceUnitId"] = result.Occurrences.Count == 1 ? result.Occurrences[0].SourceUnitId : null;
            root["publicationRun"] = result.Publication?.IndexRunId.Value;
            Assert(root, result.Availability == CodeOccurrenceAvailability.Available &&
                result.Occurrences.Count == 1 && result.IsComplete &&
                result.Occurrences[0].TargetId == target &&
                result.Occurrences[0].SourceUnitId == app.Descriptor.ProjectUnitId,
                "reopened publication serves the exact real cross-project target", notes);
            // Missing current authority refuses as unavailable, never empty-success.
            var noAuthority = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(Repo, target));
            root["noAuthorityAvailability"] = noAuthority.Availability.ToString();
            Assert(root, noAuthority.Availability == CodeOccurrenceAvailability.AnalysisUnavailable && noAuthority.Occurrences.Count == 0,
                "missing current authority refuses as unavailable", notes);
            // Stale expected-publication handle refuses explicitly.
            var stale = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(Repo, target, new CodeGraphPublication(
                Repo, new CodeIndexRunId("run:foreign"), null, true, new CodeIndexIdentity("identity:foreign"))));
            root["staleHandleAvailability"] = stale.Availability.ToString();
            Assert(root, stale.Availability == CodeOccurrenceAvailability.StalePublication && stale.Examined == 0,
                "stale publication handle refuses with no hits", notes);
        }
        root["rows"] = Rows(database, "AndiritOccurrenceUnit", "AndiritOccurrenceMeta");
    }

    private static async Task PluginNegativeAsync(string database, JsonObject root, JsonArray notes)
    {
        var context = Context("run:a", CrossProject("public static int Get() => 1;"));
        var plugin = new CSharpCodeGraphPlugin();
        using (var store = new LatticeCodeGraphStore(database))
        {
            await store.UpsertRepositoryAsync(new(Repo));
            await store.StoreIndexRunAsync(new(Repo, new("run:a"), Started, plugins: [plugin.Id]));
            await using (var sink = new CodeGraphIngestionSink(store))
            await using (var session = await plugin.CreateSessionAsync(context))
            {
                await session.ExtractAsync(sink);
                await sink.CompleteAsync();
            }
            await store.CompleteIndexRunAsync(
                new(Repo, new("run:a"), Started, CodeIndexRunStatus.Completed, Started.AddSeconds(1), [plugin.Id]),
                new(Repo, new("run:a"), []));
            var (_, app, lib) = await LiveAsync(context);
            var target = ExpectedTarget(lib, Method(lib, "Lib.Value", "Get"));
            var result = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(Repo, target));
            root["availability"] = result.Availability.ToString();
            Assert(root, result.Availability == CodeOccurrenceAvailability.AnalysisUnavailable && result.Occurrences.Count == 0,
                "authority-bearing publication without current authority is unavailable, not empty", notes);
        }
    }

    private static async Task PluginFreshVerifyAsync(string database, JsonObject root, JsonArray notes)
    {
        await PluginVerifyAsync(database, root, notes, "public static int Get() => 1;");
    }

    // ---- L8/L17: stale-defining-context refusal with a stable target identity ----

    private static async Task AuthorityReopenAsync(string database, JsonObject root, JsonArray notes)
    {
        var a = await S23NativeAuthorityFixture.Build();
        var b = await S23NativeAuthorityFixture.Build("source"); // Leaf body only; App/Lib contexts shift transitively
        root["stableTarget"] = a.LibTarget.Value == b.LibTarget.Value;
        Assert(root, a.LibTarget.Value == b.LibTarget.Value, "target identity is stable across the transitive-only change", notes);
        Assert(root, a.Lib.Descriptor.DescriptorDigest != b.Lib.Descriptor.DescriptorDigest,
            "defining compctx2 differs across the transitive-only change", notes);
        using (var store = new LatticeCodeGraphStore(database))
            await a.Publish(store, "run:a", a.App, a.App2, a.Empty);
        using (var reopened = new LatticeCodeGraphStore(database))
        {
            var current = await reopened.FindOccurrencesAsync(a.Query(a.LibTarget));
            root["current"] = Describe(current);
            Assert(root, current.Availability == CodeOccurrenceAvailability.Available && current.Occurrences.Count == 2 && current.IsComplete,
                "reopened current authority serves both real cross-project contributors", notes);
            var stale = await reopened.FindOccurrencesAsync(b.Query(b.LibTarget));
            root["stale"] = Describe(stale);
            Assert(root, stale.Availability == CodeOccurrenceAvailability.StalePublication && stale.Occurrences.Count == 0 && stale.Examined == 0,
                "reopened stale defining context refuses with no hits", notes);
            var unavailable = await reopened.FindOccurrencesAsync(new CodeOccurrenceQuery(
                S23NativeAuthorityFixture.Repo, a.LibTarget));
            root["missingAuthority"] = Describe(unavailable);
            Assert(root, unavailable.Availability == CodeOccurrenceAvailability.AnalysisUnavailable,
                "missing current authority refuses as unavailable", notes);
        }
    }

    // ---- real-fact lifecycle: publish -> reopen -> replace -> delete ----

    private static async Task LifecycleAsync(string database, JsonObject root, JsonArray notes)
    {
        var a = await S23NativeAuthorityFixture.Build();
        using (var store = new LatticeCodeGraphStore(database))
        {
            await a.Publish(store, "run:a", a.App, a.Empty);
            var observed = await store.FindOccurrencesAsync(a.Query(a.LibTarget));
            root["afterPublish"] = Describe(observed);
            Assert(root, observed.Availability == CodeOccurrenceAvailability.Available && observed.Occurrences.Count == 1 && observed.IsComplete,
                "initial real-fact publication serves the cross-project contributor", notes);
        }
        root["rowsAfterPublish"] = Counts(database);

        using (var reopened = new LatticeCodeGraphStore(database))
        {
            var observed = await reopened.FindOccurrencesAsync(a.Query(a.LibTarget));
            root["afterReopen"] = Describe(observed);
            Assert(root, observed.Availability == CodeOccurrenceAvailability.Available && observed.Occurrences.Count == 1 && observed.IsComplete,
                "durable reopen reconstructs the same real-fact projection", notes);
        }

        // Same unit id replaced by a real source edit that removes the invocation.
        var retired = await S23NativeAuthorityFixture.Build(retire: true);
        using (var store = new LatticeCodeGraphStore(database))
        {
            await retired.Publish(store, "run:retired", retired.App, retired.Empty);
            var observed = await store.FindOccurrencesAsync(retired.Query(a.LibTarget));
            root["afterReplacement"] = Describe(observed);
            Assert(root, observed.Availability == CodeOccurrenceAvailability.Available && observed.Occurrences.Count == 0 && observed.IsComplete,
                "same-unit replacement (real source edit) retires obsolete App facts to a complete-empty answer", notes);
        }
        root["rowsAfterReplacement"] = Counts(database);

        using (var store = new LatticeCodeGraphStore(database))
        {
            // Delete every analyzed unit: the last deletion invalidates coverage.
            await S23NativeAuthorityFixture.Start(store, "run:delete");
            await store.StageIndexUnitDeletionAsync(S23NativeAuthorityFixture.Repo, new("run:delete"),
                S23NativeAuthorityFixture.Plugin, new(retired.App.Descriptor.ProjectUnitId));
            await store.StageIndexUnitDeletionAsync(S23NativeAuthorityFixture.Repo, new("run:delete"),
                S23NativeAuthorityFixture.Plugin, new(retired.Empty.Descriptor.ProjectUnitId));
            await S23NativeAuthorityFixture.Complete(store, "run:delete");
            var observed = await store.FindOccurrencesAsync(retired.Query(a.LibTarget));
            root["afterDeletion"] = Describe(observed);
            Assert(root, observed.Availability == CodeOccurrenceAvailability.AnalysisUnavailable,
                "deleting the last analyzed unit invalidates coverage to unavailable", notes);
        }
        root["rowsAfterDeletion"] = Counts(database);

        using var final = new LatticeCodeGraphStore(database);
        var last = await final.FindOccurrencesAsync(retired.Query(a.LibTarget));
        root["afterFinalReopen"] = Describe(last);
        Assert(root, last.Availability == CodeOccurrenceAvailability.AnalysisUnavailable,
            "final durable reopen remains unavailable", notes);
    }

    // ---- SQ-03: serialized-write old-or-new complete visibility ----

    private static async Task Sq03ScheduleAsync(string database, JsonObject root, JsonArray notes)
    {
        var a = await S23NativeAuthorityFixture.Build();
        var trace = new JsonArray();
        using (var store = new LatticeCodeGraphStore(database))
        {
            await a.Publish(store, "run:1", a.App, a.Empty);
            trace.Add(Observe(await store.FindOccurrencesAsync(a.Query(a.LibTarget))));

            await S23NativeAuthorityFixture.Start(store, "run:2");
            trace.Add(Observe(await store.FindOccurrencesAsync(a.Query(a.LibTarget))));
            await store.StageIndexUnitAsync(a.Replacement("run:2", a.App));
            trace.Add(Observe(await store.FindOccurrencesAsync(a.Query(a.LibTarget))));
            await store.StageIndexUnitAsync(a.Replacement("run:2", a.App2));
            trace.Add(Observe(await store.FindOccurrencesAsync(a.Query(a.LibTarget))));
            await store.StageIndexUnitAsync(a.Replacement("run:2", a.Empty));
            trace.Add(Observe(await store.FindOccurrencesAsync(a.Query(a.LibTarget))));
            await S23NativeAuthorityFixture.Complete(store, "run:2");
            trace.Add(Observe(await store.FindOccurrencesAsync(a.Query(a.LibTarget))));
        }
        root["schedule"] = trace;
        var expected = new[] { "run:1=1/Available", "run:1=1/Available", "run:1=1/Available",
            "run:1=1/Available", "run:1=1/Available", "run:2=2/Available" };
        Assert(root, trace.Select(n => n!.GetValue<string>()).SequenceEqual(expected),
            "every scheduled observation is a complete old-or-new publication (no partial blend)", notes);
    }

    private static async Task Sq03ConcurrentAsync(string database, JsonObject root, JsonArray notes)
    {
        var a = await S23NativeAuthorityFixture.Build();
        using var store = new LatticeCodeGraphStore(database);
        await a.Publish(store, "run:1", a.App, a.Empty);
        var samples = 0;
        var violations = new List<string>();
        var writer = Task.Run(async () =>
        {
            for (var round = 0; round < 20; round++)
            {
                var expand = "run:n:" + round;
                await S23NativeAuthorityFixture.Start(store, expand);
                await store.StageIndexUnitAsync(a.Replacement(expand, a.App));
                await store.StageIndexUnitAsync(a.Replacement(expand, a.App2));
                await store.StageIndexUnitAsync(a.Replacement(expand, a.Empty));
                await S23NativeAuthorityFixture.Complete(store, expand);

                var shrink = "run:o:" + round;
                await S23NativeAuthorityFixture.Start(store, shrink);
                await store.StageIndexUnitAsync(a.Replacement(shrink, a.App));
                await store.StageIndexUnitAsync(a.Replacement(shrink, a.Empty));
                await store.StageIndexUnitDeletionAsync(S23NativeAuthorityFixture.Repo, new(shrink),
                    S23NativeAuthorityFixture.Plugin, new(a.App2.Descriptor.ProjectUnitId));
                await S23NativeAuthorityFixture.Complete(store, shrink);
            }
        });
        var readers = Enumerable.Range(0, 4).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < 200; i++)
            {
                var result = await store.FindOccurrencesAsync(a.Query(a.LibTarget));
                Interlocked.Increment(ref samples);
                if (result.Availability != CodeOccurrenceAvailability.Available) continue;
                var run = result.Publication!.IndexRunId.Value;
                var count = result.Occurrences.Count;
                var consistent = count switch
                {
                    1 => run == "run:1" || run.StartsWith("run:o:", StringComparison.Ordinal),
                    2 => run.StartsWith("run:n:", StringComparison.Ordinal),
                    _ => false,
                };
                if (!consistent) lock (violations) violations.Add(Observe(result));
            }
        })).ToArray();
        await Task.WhenAll(readers.Append(writer));
        root["samples"] = samples;
        root["violations"] = new JsonArray(violations.Select(v => (JsonNode)v).ToArray());
        Assert(root, violations.Count == 0, "concurrent readers never observe a partial blend", notes);
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

    private static string Observe(CodeOccurrenceQueryResult result) =>
        $"{result.Publication?.IndexRunId.Value}={result.Occurrences.Count}/{result.Availability}";

    private static JsonObject Counts(string database)
    {
        var rows = Rows(database, "AndiritOccurrenceUnit", "AndiritOccurrenceMeta", "AndiritOccurrencePage",
            "HetuUnit", "HetuStage", "HetuRun");
        var counts = new JsonObject();
        foreach (var label in new[] { "AndiritOccurrenceUnit", "AndiritOccurrenceMeta", "AndiritOccurrencePage", "HetuUnit", "HetuStage", "HetuRun" })
            counts[label] = rows[label] is JsonObject o ? o.Count : 0;
        return counts;
    }

    private static JsonObject Rows(string database, params string[] labels)
    {
        var result = new JsonObject();
        using var db = LatticeDatabase.Open(database, new LatticeDatabaseOptions { Create = true });
        using var txn = db.BeginReadTransaction();
        foreach (var label in labels)
        {
            var table = new JsonObject();
            using var query = db.Prepare($"MATCH (s:{label}) RETURN s.key, s.payload");
            using var rows = query.Execute(txn);
            foreach (var row in rows.ReadAll())
                table[row.GetValue(0).AsString()] =
                    JsonNode.Parse(Convert.FromBase64String(row.GetValue(1).AsString()));
            result[label] = table;
        }
        txn.Commit();
        return result;
    }

    // ---- real plugin fixture ----

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

    private static CodeGraphPluginContext Context(string run, IReadOnlyDictionary<string, string> files) =>
        new(Repo, null, new CodeIndexRunId(run),
            files.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair =>
            {
                var bytes = Encoding.UTF8.GetBytes(pair.Value);
                return new CodeGraphSource(pair.Key,
                    "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)),
                    _ => ValueTask.FromResult<Stream>(new MemoryStream(bytes, writable: false)));
            }).ToArray());

    private static async Task<(VerifiedCSharpAnalysis Analysis, VerifiedCSharpCompilation App, VerifiedCSharpCompilation Lib)>
        LiveAsync(CodeGraphPluginContext context)
    {
        var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(context);
        return (analysis,
            analysis.Projects.Single(p => p.Project.Path == "App/App.csproj").Verified!,
            analysis.Projects.Single(p => p.Project.Path == "Lib/Lib.csproj").Verified!);
    }

    private static IMethodSymbol Method(VerifiedCSharpCompilation unit, string type, string name) =>
        (IMethodSymbol)unit.Compilation.GetTypeByMetadataName(type)!.GetMembers(name).Single();

    // Independent target recomputation: the frozen R2 authority and the accepted
    // identity primitives must agree before the probe asserts anything.
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
}
