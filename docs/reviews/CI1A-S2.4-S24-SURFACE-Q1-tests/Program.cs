using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Andirit;
using LatticeDbSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Frozen = AdrCi1Exp.Shared;

// ANDIRIT-CI1A-S2.4-S24-SURFACE-Q1 reviewer-owned probe harness.
// Independent qualification of WP-S24-2: IF-05 mapping, IF-02 ordering/sizes,
// SQ-07/MD-02 measurement, coverage/provenance, publication/recovery.
// Usage: <verb> <database> <outputJson> [detail].
var action = args[0];
var database = args[1];
var output = args[2];
var detail = args.Length > 3 ? args[3] : "";
var result = await SurfaceQ1Probe.RunAsync(action, database, detail);
File.WriteAllText(output, JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine("RESULT " + (result["ok"]!.GetValue<bool>() ? "OK" : "FAIL") + " " + action + " pid=" + result["processId"]!.GetValue<int>());
return result["ok"]!.GetValue<bool>() ? 0 : 1;

internal static class SurfaceQ1Probe
{
    internal static readonly CodePluginId Plugin = new("probe.s24.surface.q1");
    internal static readonly CodeRepositoryId Repo = new("repo:s24-surface-q1");
    internal static readonly DateTimeOffset Started = new(2026, 11, 9, 12, 0, 0, TimeSpan.Zero);

    internal static async Task<JsonObject> RunAsync(string action, string database, string detail)
    {
        var root = new JsonObject
        {
            ["action"] = action,
            ["processId"] = Environment.ProcessId,
            ["os"] = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
            ["architecture"] = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString(),
            ["runtime"] = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            ["gc"] = System.Runtime.GCSettings.IsServerGC ? "server" : "workstation",
            ["processors"] = Environment.ProcessorCount,
        };
        var notes = new JsonArray();
        root["ok"] = true;
        try
        {
            switch (action)
            {
                case "environment": root["head"] = detail; break;
                case "mapping": await MappingAsync(database, root, notes); break;
                case "corpus": await CorpusAsync(database, root, notes); break;
                case "genreal": await GenRealAsync(database, root, notes, detail); break;
                case "measure": await MeasureAsync(database, root, notes, detail); break;
                case "coverage": await CoverageAsync(database, root, notes); break;
                case "publication": await PublicationAsync(database, root, notes); break;
                default: throw new ArgumentException("Unknown action: " + action);
            }
        }
        catch (Exception exception)
        {
            notes.Add(exception.GetType().Name + ": " + exception.Message);
            root["stack"] = exception.StackTrace;
            root["ok"] = false;
        }
        root["notes"] = notes;
        return root;
    }

    // ---- S1: independent IF-05 mapping ----

    private static async Task MappingAsync(string database, JsonObject root, JsonArray notes)
    {
        var matrix = new JsonArray();
        var byName = new Dictionary<string, string>(StringComparer.Ordinal);

        async Task<string> OpenCase(string name, Func<string, Task> arrange)
        {
            var path = database + "." + name;
            Cleanup(path);
            try
            {
                await arrange(path);
                try { using var s = new LatticeCodeGraphStore(path); return name + "=open:no-exception"; }
                catch (Exception e) { return name + "=open:" + e.GetType().Name; }
            }
            finally { Cleanup(path); }
        }

        async Task<string> QueryCase(string name, Func<string, Task> arrange, bool staleExpected = false, string? token = null)
        {
            var path = database + "." + name;
            Cleanup(path);
            try
            {
                await arrange(path);
                var repo = new CodeRepositoryId("repo:q1map:" + name);
                using var store = new LatticeCodeGraphStore(path);
                var target = Target(repo, "T");
                var query = staleExpected
                    ? new CodeOccurrenceQuery(repo, target, new CodeGraphPublication(
                        repo, new CodeIndexRunId("run:foreign"), null, true, new CodeIndexIdentity("identity:foreign")))
                    : new CodeOccurrenceQuery(repo, target) { ContinuationToken = token };
                try
                {
                    var r = await store.FindOccurrencesAsync(query);
                    return name switch
                    {
                        "complete-empty" => name + "=" + r.Availability + ":IsComplete=" + r.IsComplete + ":Count=" + r.Occurrences.Count,
                        "partial" => name + "=" + r.Availability + ":IsComplete=" + r.IsComplete,
                        "malformed-token" => name + "=" + r.Availability + ":" + r.Continuation + ":Count=" + r.Occurrences.Count,
                        _ => name + "=" + r.Availability,
                    };
                }
                catch (Exception e) { return name + "=query:" + e.GetType().Name; }
            }
            finally { Cleanup(path); }
        }

        byName["schema-version"] = await OpenCase("schema-version", async p => { await Seed(p, "repo:q1map:schema-version", 1); PatchMeta(p, "repo:q1map:schema-version", j => j["SchemaVersion"] = 99); });
        byName["schema-namespace"] = await OpenCase("schema-namespace", async p => { await Seed(p, "repo:q1map:schema-namespace", 1); PatchMeta(p, "repo:q1map:schema-namespace", j => j["Namespace"] = "wrong/9"); });
        byName["units-without-meta"] = await OpenCase("units-without-meta", async p => { await Seed(p, "repo:q1map:units-without-meta", 1); DeleteRow(p, "AndiritOccurrenceMeta", "repo:q1map:units-without-meta"); });
        byName["malformed-occurrence-payload"] = await OpenCase("malformed-occurrence-payload", async p => { await Seed(p, "repo:q1map:malformed-occurrence-payload", 1); WriteRawRow(p, "AndiritOccurrenceUnit", SingleKey(p, "AndiritOccurrenceUnit"), "not-json"); });
        byName["malformed-graph-payload"] = await OpenCase("malformed-graph-payload", async p => { await Seed(p, "repo:q1map:malformed-graph-payload", 1); WriteRawRow(p, "HetuUnit", SingleKey(p, "HetuUnit"), "not-json"); });
        byName["missing-endpoint"] = await OpenCase("missing-endpoint", async p => { await SeedCrossUnit(p, "repo:q1map:missing-endpoint"); DeleteRow(p, "HetuUnit", SingleKeyContaining(p, "HetuUnit", "\nlib")); });
        byName["page-publication-mismatch"] = await OpenCase("page-publication-mismatch", async p => { await Seed(p, "repo:q1map:page-publication-mismatch", 2); PatchRow(p, "AndiritOccurrencePage", j => j["PublicationIndexRunId"] = "run:foreign"); });

        byName["no-publication"] = await QueryCase("no-publication", _ => Task.CompletedTask);
        byName["graph-only"] = await QueryCase("graph-only", p => SeedGraphOnly(p, "repo:q1map:graph-only"));
        byName["partial"] = await QueryCase("partial", p => Seed(p, "repo:q1map:partial", 2, CodeRelationshipCoverageState.Partial));
        byName["complete-empty"] = await QueryCase("complete-empty", p => Seed(p, "repo:q1map:complete-empty", 0));
        byName["stale-expected-publication"] = await QueryCase("stale-expected-publication", p => Seed(p, "repo:q1map:stale-expected-publication", 2), staleExpected: true);
        byName["malformed-token"] = await QueryCase("malformed-token", p => Seed(p, "repo:q1map:malformed-token", 2), token: "%%%");

        foreach (var kv in byName) matrix.Add(new JsonObject { ["case"] = kv.Key, ["observed"] = kv.Value });
        root["matrix"] = matrix;

        var expected = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["schema-version"] = "schema-version=open:CodeOccurrenceSchemaException",
            ["schema-namespace"] = "schema-namespace=open:CodeOccurrenceSchemaException",
            ["units-without-meta"] = "units-without-meta=open:CodeOccurrenceSchemaException",
            ["malformed-occurrence-payload"] = "malformed-occurrence-payload=open:CodeOccurrenceSchemaException",
            ["malformed-graph-payload"] = "malformed-graph-payload=open:JsonException",
            ["missing-endpoint"] = "missing-endpoint=open:CodeGraphBatchRejectedException",
            ["page-publication-mismatch"] = "page-publication-mismatch=open:CodeOccurrenceIndexException",
            ["no-publication"] = "no-publication=AnalysisUnavailable",
            ["graph-only"] = "graph-only=AnalysisUnavailable",
            ["partial"] = "partial=IncompleteAnalysis:IsComplete=False",
            ["complete-empty"] = "complete-empty=Available:IsComplete=True:Count=0",
            ["stale-expected-publication"] = "stale-expected-publication=StalePublication",
            ["malformed-token"] = "malformed-token=Available:Invalid:Count=0",
        };
        var failed = 0;
        foreach (var (name, want) in expected)
        {
            if (!byName.TryGetValue(name, out var got) || got != want) { failed++; notes.Add($"FAIL: {name} expected '{want}' actual '{got}'"); }
        }
        Assert(root, failed == 0, $"IF-05 mapping: {expected.Count - failed}/{expected.Count} cases match the normative mapping", notes);
    }

    // ---- S2: authentic corpus ordering ----

    private static async Task CorpusAsync(string database, JsonObject root, JsonArray notes)
    {
        var files = CorpusFiles();
        var context = Context("run:a", Repo, files);
        var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(context);
        if (!analysis.Universe.IsComplete) throw new InvalidDataException(string.Join(";", analysis.Universe.Refusals));
        var app = analysis.Projects.Single(p => p.Project.Path == "App/App.csproj").Verified!;
        var core = analysis.Projects.Single(p => p.Project.Path == "Core/Core.csproj").Verified!;
        var add = (IMethodSymbol)core.Compilation.GetTypeByMetadataName("Corpus.Core.Methods")!
            .GetMembers("Add").OfType<IMethodSymbol>().Single(m => m.Parameters.Length == 2 && m.Parameters[0].Type.SpecialType == SpecialType.System_Int32);
        await PublishRealAsync(database, "run:a", context);

        var expected = new List<string>();
        foreach (var tree in app.Compilation.SyntaxTrees)
        {
            var model = app.Compilation.GetSemanticModel(tree);
            foreach (var invocation in tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (!SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(invocation).Symbol, add)) continue;
                var token = invocation.Expression is MemberAccessExpressionSyntax m ? m.Name.Identifier : invocation.Expression.GetLastToken();
                var line = tree.GetLineSpan(token.Span);
                expected.Add($"{tree.FilePath.Replace('\\', '/')}:{line.StartLinePosition.Line + 1}:{line.StartLinePosition.Character + 1}");
            }
        }
        expected.Sort(StringComparer.Ordinal);
        var target = ExpectedTarget(core, add, Repo);

        var matrix = new JsonArray();
        using var store = new LatticeCodeGraphStore(database);
        foreach (var pageSize in new[] { 1, 2, 3, 100 })
        {
            var seen = new List<string>();
            string? token = null;
            var pages = 0;
            while (true)
            {
                var query = VerifiedCSharpOccurrenceAuthority.Bind(
                    new(Repo, target, maxResults: pageSize) { ContinuationToken = token }, analysis.Universe, app.Compilation);
                var r = await store.FindOccurrencesAsync(query);
                if (r.Availability != CodeOccurrenceAvailability.Available) throw new InvalidDataException("availability " + r.Availability);
                if (r.TotalMatches != expected.Count || r.Examined != expected.Count) throw new InvalidDataException("counts");
                foreach (var hit in r.Occurrences)
                {
                    if (hit.TargetId != target) throw new InvalidDataException("target mismatch");
                    if (hit.SourceUnitId != app.Descriptor.ProjectUnitId) throw new InvalidDataException("owner mismatch");
                    seen.Add($"{hit.Span.Path}:{hit.Span.StartLine}:{hit.Span.StartColumn}");
                }
                pages++;
                if (r.NextContinuationToken is null) break;
                token = r.NextContinuationToken;
            }
            var ordered = seen.OrderBy(s => s, StringComparer.Ordinal).ToArray();
            var matches = ordered.SequenceEqual(expected) && seen.Distinct(StringComparer.Ordinal).Count() == expected.Count;
            matrix.Add(new JsonObject { ["pageSize"] = pageSize, ["pages"] = pages, ["count"] = seen.Count, ["matchesOracle"] = matches, ["ok"] = matches });
            Assert(root, matches, $"authentic corpus pageSize={pageSize} matches the Roslyn oracle", notes);
        }
        root["matrix"] = matrix;
        root["expectedCount"] = expected.Count;
    }

    // ---- S3: generated-real scaling ----

    private static async Task GenRealAsync(string database, JsonObject root, JsonArray notes, string detail)
    {
        var sizes = detail.Length == 0 ? new[] { 100, 1000, 10000 } : detail.Split(',').Select(int.Parse).ToArray();
        var results = new JsonArray();
        foreach (var size in sizes)
        {
            var expected = new List<(string Path, int Line, int Column)>();
            var files = Generate(size, expected);
            var context = Context("run", Repo, files);
            var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(context);
            if (!analysis.Universe.IsComplete) throw new InvalidDataException("incomplete");
            var core = analysis.Projects.Single(p => p.Project.Path == "Core/Core.csproj").Verified!;
            var caller = analysis.Projects.First(p => p.Project.Path.StartsWith("Consumer/", StringComparison.Ordinal)).Verified!;
            var ping = (IMethodSymbol)core.Compilation.GetTypeByMetadataName("Core.Api")!.GetMembers("Ping").Single();
            var target = ExpectedTarget(core, ping, Repo);
            var path = database + "." + size;
            Cleanup(path);
            await PublishRealAsync(path, "run", context);
            var ordered = expected.OrderBy(s => s.Path, StringComparer.Ordinal).ThenBy(s => s.Line).ThenBy(s => s.Column)
                .Select(s => $"{s.Path}:{s.Line}:{s.Column}").ToArray();
            using var store = new LatticeCodeGraphStore(path);
            foreach (var pageSize in size >= 10000 ? new[] { 100, 1000 } : new[] { 1, 10, 100, 1000 })
            {
                var seen = new List<string>();
                string? token = null;
                var pages = 0;
                while (true)
                {
                    var q = VerifiedCSharpOccurrenceAuthority.Bind(
                        new(Repo, target, maxResults: pageSize) { ContinuationToken = token }, analysis.Universe, caller.Compilation);
                    var r = await store.FindOccurrencesAsync(q);
                    foreach (var hit in r.Occurrences) seen.Add($"{hit.Span.Path}:{hit.Span.StartLine}:{hit.Span.StartColumn}");
                    pages++;
                    if (r.NextContinuationToken is null) break;
                    token = r.NextContinuationToken;
                }
                var matches = seen.SequenceEqual(ordered) && seen.Distinct(StringComparer.Ordinal).Count() == ordered.Length;
                results.Add(new JsonObject { ["cardinality"] = size, ["pageSize"] = pageSize, ["pages"] = pages, ["matchesOracle"] = matches, ["ok"] = matches });
                Assert(root, matches, $"generated-real size={size} pageSize={pageSize} matches the generation oracle", notes);
            }
            Cleanup(path);
        }
        root["results"] = results;
    }

    // ---- S4: independent measurement ----

    private static async Task MeasureAsync(string database, JsonObject root, JsonArray notes, string detail)
    {
        const int warmup = 2, reps = 7;
        var size = detail.Length == 0 ? 1000 : int.Parse(detail);
        var expected = new List<(string Path, int Line, int Column)>();
        var files = Generate(size, expected);
        var context = Context("run", Repo, files);
        var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(context);
        var core = analysis.Projects.Single(p => p.Project.Path == "Core/Core.csproj").Verified!;
        var caller = analysis.Projects.First(p => p.Project.Path.StartsWith("Consumer/", StringComparison.Ordinal)).Verified!;
        var ping = (IMethodSymbol)core.Compilation.GetTypeByMetadataName("Core.Api")!.GetMembers("Ping").Single();
        var target = ExpectedTarget(core, ping, Repo);
        var publish = new JsonArray(); var reopen = new JsonArray(); var query = new JsonArray();
        for (var i = 0; i < warmup + reps; i++)
        {
            Cleanup(database);
            var sw = Stopwatch.StartNew();
            await PublishRealAsync(database, "run", context);
            sw.Stop();
            if (i >= warmup) publish.Add(Math.Round(sw.Elapsed.TotalMilliseconds, 3));
            Cleanup(database);
            await PublishRealAsync(database, "run", context);
            sw.Restart();
            using (var store = new LatticeCodeGraphStore(database))
            {
                sw.Stop();
                if (i >= warmup) reopen.Add(Math.Round(sw.Elapsed.TotalMilliseconds, 3));
                foreach (var pageSize in new[] { 10, 100, 1000 })
                {
                    var q = VerifiedCSharpOccurrenceAuthority.Bind(new(Repo, target, maxResults: pageSize), analysis.Universe, caller.Compilation);
                    await store.FindOccurrencesAsync(q);
                    var qsw = Stopwatch.StartNew();
                    var r = await store.FindOccurrencesAsync(q);
                    qsw.Stop();
                    if (i >= warmup) query.Add(new JsonObject { ["pageSize"] = pageSize, ["ms"] = Math.Round(qsw.Elapsed.TotalMilliseconds, 3), ["examined"] = r.Examined });
                }
            }
        }
        Cleanup(database);
        root["cardinality"] = size; root["warmup"] = warmup; root["repetitions"] = reps;
        root["publicationMs"] = publish; root["reopenMs"] = reopen; root["queryMs"] = query;
        root["note"] = "reviewer re-measurement; characterization only; no threshold; F-OBS-Q1-01 preserved";
        Assert(root, publish.Count == reps && reopen.Count == reps && query.Count == reps * 3, "measurement harness recorded the required repetitions", notes);
    }

    // ---- S5: coverage/provenance ----

    private static async Task CoverageAsync(string database, JsonObject root, JsonArray notes)
    {
        var repo = new CodeRepositoryId("repo:q1cov");
        var target = Target(repo, "T");
        Cleanup(database);
        using (var store = new LatticeCodeGraphStore(database))
        {
            await store.UpsertRepositoryAsync(new(repo));
            await store.StoreIndexRunAsync(new(repo, new("run"), Started, plugins: [Plugin]));
            var fact = Fact(repo, "u1", target, "src/A.cs", 1, 1);
            await store.StageIndexUnitAsync(Unit(repo, "run", "u1", [fact, Fact(repo, "u1", target, "src/A.cs", 2, 1)], CodeRelationshipCoverageState.Produced));
            await store.StageIndexUnitAsync(Unit(repo, "run", "u2", [], CodeRelationshipCoverageState.Partial));
            await store.CompleteIndexRunAsync(new(repo, new("run"), Started, CodeIndexRunStatus.Completed, Started.AddSeconds(1), [Plugin]), new(repo, new("run"), []));
        }
        using var reopened = new LatticeCodeGraphStore(database);
        var page1 = await reopened.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target, maxResults: 1));
        var page2 = await reopened.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target, maxResults: 1) { ContinuationToken = page1.NextContinuationToken });
        Assert(root, page1.Availability == CodeOccurrenceAvailability.IncompleteAnalysis && page2.Availability == CodeOccurrenceAvailability.IncompleteAnalysis,
            "scope-level worst-wins IncompleteAnalysis on every page", notes);
        Assert(root, page1.Coverage!.State == page2.Coverage!.State && !page1.Coverage.IsComplete && !page1.IsComplete,
            "no false-complete coverage across pages", notes);
        var units = page1.UnitCoverage.Select(u => u.IndexUnitId + ":" + u.State).Order(StringComparer.Ordinal).ToArray();
        Assert(root, units.SequenceEqual(new[] { "u1:Produced", "u2:Partial" }), "per-unit provenance retained (complete + incomplete)", notes);
        Assert(root, page1.UnitCoverage.SequenceEqual(page2.UnitCoverage), "breakdown stable across pages", notes);
        Cleanup(database);
    }

    // ---- S6: publication/recovery continuity ----

    private static async Task PublicationAsync(string database, JsonObject root, JsonArray notes)
    {
        var repo = new CodeRepositoryId("repo:q1pub");
        var target = Target(repo, "T");
        Cleanup(database);
        using (var store = new LatticeCodeGraphStore(database))
        {
            await store.UpsertRepositoryAsync(new(repo));
            await store.StoreIndexRunAsync(new(repo, new("run:1"), Started, plugins: [Plugin]));
            await store.StageIndexUnitAsync(Unit(repo, "run:1", "u", [Fact(repo, "u", target, "src/A.cs", 1, 1), Fact(repo, "u", target, "src/A.cs", 2, 1)], CodeRelationshipCoverageState.Produced));
            await store.CompleteIndexRunAsync(new(repo, new("run:1"), Started, CodeIndexRunStatus.Completed, Started.AddSeconds(1), [Plugin]), new(repo, new("run:1"), []));
        }
        string token;
        using (var store = new LatticeCodeGraphStore(database))
            token = (await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target, maxResults: 1))).NextContinuationToken!;
        // Durable reopen continues.
        using (var reopened = new LatticeCodeGraphStore(database))
        {
            var r = await reopened.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target, maxResults: 1) { ContinuationToken = token });
            Assert(root, r.Availability == CodeOccurrenceAvailability.Available && r.Occurrences.Count == 1, "continuation after durable reopen", notes);
        }
        // Recovery between pages (managed before-commit fault) keeps the publication.
        var arm = false;
        using (var store = new LatticeCodeGraphStore(database, null, p => { if (arm && p == "before-commit") throw new Fault(); }))
        {
            await store.StoreIndexRunAsync(new(repo, new("run:fail"), Started.AddSeconds(5), plugins: [Plugin]));
            await store.StageIndexUnitAsync(Unit(repo, "run:fail", "u9", [Fact(repo, "u9", target, "src/Z.cs", 1, 1)], CodeRelationshipCoverageState.Produced));
            arm = true;
            try { await store.CompleteIndexRunAsync(new(repo, new("run:fail"), Started.AddSeconds(5), CodeIndexRunStatus.Completed, Started.AddSeconds(6), [Plugin]), new(repo, new("run:fail"), [])); } catch (Fault) { }
            arm = false;
            var r = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target, maxResults: 1) { ContinuationToken = token });
            Assert(root, r.Availability == CodeOccurrenceAvailability.Available && r.Publication!.IndexRunId.Value == "run:1", "recovery between pages stays in run:1", notes);
        }
        // Replacement makes the token stale.
        using (var store = new LatticeCodeGraphStore(database))
        {
            await store.StoreIndexRunAsync(new(repo, new("run:2"), Started.AddSeconds(9), plugins: [Plugin]));
            await store.StageIndexUnitAsync(Unit(repo, "run:2", "u", [Fact(repo, "u", target, "src/B.cs", 9, 1), Fact(repo, "u", target, "src/B.cs", 10, 1)], CodeRelationshipCoverageState.Produced));
            await store.CompleteIndexRunAsync(new(repo, new("run:2"), Started.AddSeconds(9), CodeIndexRunStatus.Completed, Started.AddSeconds(10), [Plugin]), new(repo, new("run:2"), []));
            var stale = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target, maxResults: 1) { ContinuationToken = token });
            Assert(root, stale.Availability == CodeOccurrenceAvailability.StalePublication && stale.Occurrences.Count == 0, "stale token after replacement refuses (no restart/blend)", notes);
        }
        Cleanup(database);
    }

    // ---- helpers ----

    private static async Task PublishRealAsync(string database, string run, CodeGraphPluginContext context)
    {
        var plugin = new CSharpCodeGraphPlugin();
        using var store = new LatticeCodeGraphStore(database);
        await store.UpsertRepositoryAsync(new(Repo));
        await store.StoreIndexRunAsync(new(Repo, new(run), Started, plugins: [plugin.Id]));
        await using (var sink = new CodeGraphIngestionSink(store))
        await using (var session = await plugin.CreateSessionAsync(context))
        {
            await session.ExtractAsync(sink);
            await sink.CompleteAsync();
        }
        await store.CompleteIndexRunAsync(
            new(Repo, new(run), Started, CodeIndexRunStatus.Completed, Started.AddSeconds(1), [plugin.Id]),
            new(Repo, new(run), []));
    }

    private static Dictionary<string, string> Generate(int calls, List<(string Path, int Line, int Column)> expected)
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Core/Core.csproj"] = "<Project Sdk=\"Microsoft.NET.Sdk\" />",
            ["Core/Api.cs"] = "namespace Core; public static class Api { public static int Ping() => 1; }",
            ["Consumer/Consumer.csproj"] = "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><ProjectReference Include=\"../Core/Core.csproj\" /></ItemGroup></Project>",
        };
        const int perFile = 100;
        var index = 0;
        var remaining = calls;
        while (remaining > 0)
        {
            var inFile = Math.Min(perFile, remaining);
            remaining -= inFile;
            var path = $"Consumer/Consumer{index:D4}.cs";
            var sb = new StringBuilder();
            sb.Append("namespace Consumer.N").Append(index).Append(";\npublic static class C").Append(index).Append("\n{\n    public static int Run()\n    {\n        var s = 0;\n");
            for (var i = 0; i < inFile; i++)
            {
                var line = sb.ToString().Split('\n').Length;
                var text = "        s += Core.Api.Ping();\n";
                expected.Add((path, line, text.IndexOf("Ping", StringComparison.Ordinal) + 1));
                sb.Append(text);
            }
            sb.Append("        return s;\n    }\n}\n");
            files[path] = sb.ToString();
            index++;
        }
        return files;
    }

    private static IReadOnlyDictionary<string, string> CorpusFiles()
    {
        var root = Path.Combine(RepoRoot(), "docs", "planning", "adr-ci1-01-ci1a-s2-0", "corpus", "repo");
        var files = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var project in new[] { "App", "Core", "Leaf", "Solo" })
            files[$"{project}/{project}.csproj"] = File.ReadAllText(Path.Combine(root, project, $"{project}.csproj"));
        foreach (var file in new[] { "App/Bulk.cs", "App/Sites.cs", "Core/Extensions.cs", "Core/LeafUser.cs", "Core/Methods.cs", "Core/Types.cs", "Leaf/Helper.cs", "Solo/Declarations.cs" })
            files[file] = File.ReadAllText(Path.Combine(root, file.Replace('/', Path.DirectorySeparatorChar)));
        return files;
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null) { if (File.Exists(Path.Combine(dir.FullName, "Andirit.slnx"))) return dir.FullName; dir = dir.Parent; }
        throw new InvalidOperationException("root not found");
    }

    private static CodeGraphPluginContext Context(string run, CodeRepositoryId repo, IReadOnlyDictionary<string, string> files) =>
        new(repo, null, new CodeIndexRunId(run), files.OrderBy(p => p.Key, StringComparer.Ordinal).Select(pair =>
        {
            var bytes = Encoding.UTF8.GetBytes(pair.Value);
            return new CodeGraphSource(pair.Key, "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)),
                _ => ValueTask.FromResult<Stream>(new MemoryStream(bytes, writable: false)));
        }).ToArray());

    private static CodeOccurrenceTargetId ExpectedTarget(VerifiedCSharpCompilation defining, IMethodSymbol method, CodeRepositoryId repo)
    {
        var mapped = defining.MapR2FaithfulCompctx1();
        var identifier = CodeOccurrenceIdentity.ComputeIdentifierDigest([method.Name]);
        var declaration = CodeOccurrenceIdentity.ComputeDeclarationDigest([method.GetDocumentationCommentId() ?? method.Name]);
        var shape = CodeOccurrenceIdentity.ComputeShapeDigest("method|" + method.Arity + "|" +
            string.Join(",", method.Parameters.Select(p => p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))));
        var viaIdentity = CodeOccurrenceIdentity.ComputeTargetId(mapped.DescriptorDigest, mapped.MetadataIdentity, mapped.ModuleName, identifier, declaration, shape);
        var frozen = Frozen.CompilationProvenance.Describe(repo.Value, defining.Descriptor.ProjectUnitId,
            defining.Compilation, defining.Sources.Select(s => (s.Path, Encoding.UTF8.GetBytes(s.Text.ToString()))).ToArray());
        var viaFrozen = "asmctx3:sha256:" + Frozen.ProvenanceHashing.Hash("asmctx3-context",
            ["compctx1", frozen.DescriptorDigest, frozen.MetadataIdentity, frozen.ModuleName, identifier, declaration, shape]);
        if (viaIdentity.Value != viaFrozen) throw new InvalidDataException("independent target computations disagree");
        return viaIdentity;
    }

    private static async Task Seed(string path, string repoValue, int facts, CodeRelationshipCoverageState state = CodeRelationshipCoverageState.Produced)
    {
        var repo = new CodeRepositoryId(repoValue);
        var target = Target(repo, "T");
        using var store = new LatticeCodeGraphStore(path);
        await store.UpsertRepositoryAsync(new(repo));
        await store.StoreIndexRunAsync(new(repo, new("run"), Started, plugins: [Plugin]));
        await store.StageIndexUnitAsync(Unit(repo, "run", "u", Enumerable.Range(1, facts).Select(i => Fact(repo, "u", target, "src/A.cs", i, 1)).ToList(), state));
        await store.CompleteIndexRunAsync(new(repo, new("run"), Started, CodeIndexRunStatus.Completed, Started.AddSeconds(1), [Plugin]), new(repo, new("run"), []));
    }

    private static async Task SeedGraphOnly(string path, string repoValue)
    {
        var repo = new CodeRepositoryId(repoValue);
        using var store = new LatticeCodeGraphStore(path);
        await store.UpsertRepositoryAsync(new(repo));
        await store.StoreIndexRunAsync(new(repo, new("run"), Started, plugins: [Plugin]));
        await store.StageIndexUnitAsync(new CodeIndexUnitReplacement(
            new CodeFactOrigin(repo, Plugin, "1.0.0", new CodeIndexRunId("run"), new CodeIndexUnitId("g")),
            [new CodeGraphNode(new("node:g"), CodeNodeKinds.Callable, "g", "g", new CodeSymbolId("symbol:g"))]));
        await store.CompleteIndexRunAsync(new(repo, new("run"), Started, CodeIndexRunStatus.Completed, Started.AddSeconds(1), [Plugin]), new(repo, new("run"), []));
    }

    private static async Task SeedCrossUnit(string path, string repoValue)
    {
        var repo = new CodeRepositoryId(repoValue);
        using var store = new LatticeCodeGraphStore(path);
        await store.UpsertRepositoryAsync(new(repo));
        await store.StoreIndexRunAsync(new(repo, new("run"), Started, plugins: [Plugin]));
        await store.StageIndexUnitAsync(new CodeIndexUnitReplacement(
            new CodeFactOrigin(repo, Plugin, "1.0.0", new CodeIndexRunId("run"), new CodeIndexUnitId("lib")),
            [new CodeGraphNode(new("node:lib"), CodeNodeKinds.Type, "Lib", "Lib", new CodeSymbolId("symbol:lib"))]));
        await store.StageIndexUnitAsync(new CodeIndexUnitReplacement(
            new CodeFactOrigin(repo, Plugin, "1.0.0", new CodeIndexRunId("run"), new CodeIndexUnitId("app")),
            [new CodeGraphNode(new("node:app"), CodeNodeKinds.Type, "App", "App", new CodeSymbolId("symbol:app"))],
            edges: [new CodeGraphEdge(new("edge:app->lib"), new("node:app"), new("node:lib"), CodeEdgeKinds.DependsOn, new CodeEvidence(EvidenceQuality.Resolved, Plugin.Value, "1.0.0"))]));
        await store.CompleteIndexRunAsync(new(repo, new("run"), Started, CodeIndexRunStatus.Completed, Started.AddSeconds(1), [Plugin]), new(repo, new("run"), []));
    }

    private static void PatchMeta(string path, string repoId, Action<JsonObject> mutate) =>
        PatchRowByKey(path, "AndiritOccurrenceMeta", repoId, mutate);

    private static void PatchRow(string path, string label, Action<JsonObject> mutate) =>
        PatchRowByKey(path, label, SingleKey(path, label), mutate);

    private static void PatchRowByKey(string path, string label, string key, Action<JsonObject> mutate)
    {
        using var db = LatticeDatabase.Open(path, new LatticeDatabaseOptions { Create = false });
        using var txn = db.BeginWriteTransaction();
        JsonObject row;
        using (var q = db.Prepare($"MATCH (s:{label}) WHERE s.key = $k RETURN s.payload"))
        {
            q.Bind("k", LatticeValue.From(key));
            using var r = q.Execute(txn);
            row = JsonNode.Parse(Convert.FromBase64String(r.ReadAll()[0].GetValue(0).AsString()))!.AsObject();
        }
        mutate(row);
        using (var q = db.Prepare($"MATCH (s:{label}) WHERE s.key = $k SET s.payload = $p"))
        {
            q.Bind("k", LatticeValue.From(key));
            q.Bind("p", LatticeValue.From(Convert.ToBase64String(Encoding.UTF8.GetBytes(row.ToJsonString()))));
            using var r = q.Execute(txn);
            r.ReadAll();
        }
        txn.Commit();
    }

    private static void DeleteRow(string path, string label, string key)
    {
        using var db = LatticeDatabase.Open(path, new LatticeDatabaseOptions { Create = false });
        using var txn = db.BeginWriteTransaction();
        using var q = db.Prepare($"MATCH (s:{label}) WHERE s.key = $k DELETE s");
        q.Bind("k", LatticeValue.From(key));
        using var r = q.Execute(txn);
        r.ReadAll();
        txn.Commit();
    }

    private static void WriteRawRow(string path, string label, string key, string payload)
    {
        using var db = LatticeDatabase.Open(path, new LatticeDatabaseOptions { Create = false });
        using var txn = db.BeginWriteTransaction();
        using (var q = db.Prepare($"MATCH (s:{label}) WHERE s.key = $k SET s.payload = $p"))
        {
            q.Bind("k", LatticeValue.From(key));
            q.Bind("p", LatticeValue.From(Convert.ToBase64String(Encoding.UTF8.GetBytes(payload))));
            using var r = q.Execute(txn);
            r.ReadAll();
        }
        txn.Commit();
    }

    private static string SingleKey(string path, string label)
    {
        using var db = LatticeDatabase.Open(path, new LatticeDatabaseOptions { Create = false });
        using var txn = db.BeginReadTransaction();
        using var q = db.Prepare($"MATCH (s:{label}) RETURN s.key");
        using var r = q.Execute(txn);
        var key = r.ReadAll().Single().GetValue(0).AsString();
        txn.Commit();
        return key;
    }

    private static string SingleKeyContaining(string path, string label, string fragment)
    {
        using var db = LatticeDatabase.Open(path, new LatticeDatabaseOptions { Create = false });
        using var txn = db.BeginReadTransaction();
        using var q = db.Prepare($"MATCH (s:{label}) RETURN s.key");
        using var r = q.Execute(txn);
        var key = r.ReadAll().Select(x => x.GetValue(0).AsString()).Single(k => k.Contains(fragment, StringComparison.Ordinal));
        txn.Commit();
        return key;
    }

    private static void Cleanup(string path)
    {
        foreach (var suffix in new[] { "", "-wal", "-journal", ".wal" })
            if (File.Exists(path + suffix)) File.Delete(path + suffix);
    }

    private static void Assert(JsonObject root, bool condition, string message, JsonArray notes)
    {
        notes.Add((condition ? "PASS: " : "FAIL: ") + message);
        if (!condition) root["ok"] = false;
    }

    private static CodeIndexUnitReplacement Unit(CodeRepositoryId repo, string run, string unit, IReadOnlyList<CodeOccurrenceFact> facts, CodeRelationshipCoverageState state) =>
        new(new CodeFactOrigin(repo, Plugin, "1.0.0", new CodeIndexRunId(run), new CodeIndexUnitId(unit)),
            [new CodeGraphNode(new CodeNodeId("node:" + unit), CodeNodeKinds.Callable, unit, unit, new CodeSymbolId("symbol:" + unit))],
            occurrences: facts, occurrenceAnalysisIncluded: true,
            occurrenceCoverage: new CodeOccurrenceCoverage(facts.Count, 0, state));

    private static CodeOccurrenceTargetId Target(CodeRepositoryId repo, string name)
    {
        var context = "compctx1:sha256:" + Hex(repo.Value + "|" + name);
        return CodeOccurrenceIdentity.ComputeTargetId(context, "meta:" + name, "module:" + name,
            CodeOccurrenceIdentity.ComputeIdentifierDigest([$"src/{name}.cs|1|2"]),
            CodeOccurrenceIdentity.ComputeDeclarationDigest([$"src/{name}.cs|1|2|0|10|1|1|1|2|1|1|1|10"]),
            CodeOccurrenceIdentity.ComputeShapeDigest("shape:" + name));
    }

    private static string Hex(string v) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(v)));

    private static CodeOccurrenceFact Fact(CodeRepositoryId repo, string unit, CodeOccurrenceTargetId target, string path, int line, int column) =>
        CodeOccurrenceFact.Create(repo, target, target.Value, unit,
            new CodeOccurrenceContributor(Plugin, unit), CodeOccurrenceCategory.Invocation,
            new CodeLocation(path, line, column, line, column + 4), "sha256:source", "content:invocation");

    private sealed class Fault : Exception;
}
