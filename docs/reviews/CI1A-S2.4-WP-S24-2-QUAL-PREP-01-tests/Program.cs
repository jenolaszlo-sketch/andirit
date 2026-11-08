using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Andirit;
using Microsoft.CodeAnalysis;
using Frozen = AdrCi1Exp.Shared;

// ANDIRIT-CI1A-S2.4-WP-S24-2-QUAL-PREP-01 producer probe harness.
//
// Generated-real scaling workloads (genuine C# source compiled and ingested via
// the real plugin) and an SQ-07/MD-02 measurement harness. Independent expected
// order is computed from the generation parameters, not from production sorting.
// Usage: <verb> <database> <outputJson> [detail].
var action = args[0];
var database = args[1];
var output = args[2];
var detail = args.Length > 3 ? args[3] : "";
var result = await S24Probe.RunAsync(action, database, detail);
File.WriteAllText(output, JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine("RESULT " + (result["ok"]!.GetValue<bool>() ? "OK" : "FAIL") + " " + action + " pid=" + result["processId"]!.GetValue<int>());
return result["ok"]!.GetValue<bool>() ? 0 : 1;

internal static class S24Probe
{
    internal static readonly CodeRepositoryId Repo = new("repo:s24-genreal");
    internal static readonly DateTimeOffset Started = new(2026, 11, 9, 8, 0, 0, TimeSpan.Zero);

    internal static async Task<JsonObject> RunAsync(string action, string database, string detail)
    {
        var root = new JsonObject
        {
            ["action"] = action,
            ["processId"] = Environment.ProcessId,
            ["os"] = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
            ["architecture"] = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString(),
            ["runtime"] = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            ["sdk"] = Environment.Version.ToString(),
            ["gc"] = System.Runtime.GCSettings.IsServerGC ? "server" : "workstation",
            ["processors"] = Environment.ProcessorCount,
        };
        var notes = new JsonArray();
        root["ok"] = true;
        try
        {
            switch (action)
            {
                case "scaling": await ScalingAsync(database, root, notes, detail); break;
                case "measure": await MeasureAsync(database, root, notes, detail); break;
                case "environment": root["head"] = detail; break;
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

    // ---- P3/P4: generated-real scaling + realistic-size pagination ----

    private static async Task ScalingAsync(string database, JsonObject root, JsonArray notes, string detail)
    {
        // detail = "size[:filesPerSize]" - default sizes 100,1000,10000.
        var sizes = detail.Length == 0 ? new[] { 100, 1000, 10000 } : detail.Split(',').Select(int.Parse).ToArray();
        var results = new JsonArray();
        foreach (var size in sizes)
        {
            var expected = new List<(string Path, int Line, int Column)>();
            var files = Generate(size, expected);
            var context = Context("run", files);
            var sw = Stopwatch.StartNew();
            var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(context);
            var compileMs = sw.Elapsed.TotalMilliseconds;
            if (!analysis.Universe.IsComplete)
                throw new InvalidDataException("generated universe incomplete: " + string.Join(";", analysis.Universe.Refusals));
            var core = analysis.Projects.Single(p => p.Project.Path == "Core/Core.csproj").Verified!;
            var caller = analysis.Projects.First(p => p.Project.Path.StartsWith("Consumer/", StringComparison.Ordinal)).Verified!;
            var ping = (IMethodSymbol)core.Compilation.GetTypeByMetadataName("Core.Api")!.GetMembers("Ping").Single();
            var target = ExpectedTarget(core, ping);

            var path = database + "." + size;
            Cleanup(path);
            var plugin = new CSharpCodeGraphPlugin();
            using (var store = new LatticeCodeGraphStore(path))
            {
                var pub = Stopwatch.StartNew();
                await store.UpsertRepositoryAsync(new(Repo));
                await store.StoreIndexRunAsync(new(Repo, new("run"), Started, plugins: [plugin.Id]));
                await using (var sink = new CodeGraphIngestionSink(store))
                await using (var session = await plugin.CreateSessionAsync(context))
                {
                    await session.ExtractAsync(sink);
                    await sink.CompleteAsync();
                }
                await store.CompleteIndexRunAsync(
                    new(Repo, new("run"), Started, CodeIndexRunStatus.Completed, Started.AddSeconds(1), [plugin.Id]),
                    new(Repo, new("run"), []));
                pub.Stop();
                var publishMs = pub.Elapsed.TotalMilliseconds;

                var orderedExpected = expected
                    .OrderBy(s => s.Path, StringComparer.Ordinal).ThenBy(s => s.Line).ThenBy(s => s.Column)
                    .Select(s => $"{s.Path}:{s.Line}:{s.Column}").ToArray();
                var pageSizes = size >= 10000 ? new[] { 10, 100, 1000 } : new[] { 1, 10, 100, 1000 };
                foreach (var pageSize in pageSizes)
                {
                    var qsw = Stopwatch.StartNew();
                    var seen = new List<string>();
                    string? token = null;
                    var pages = 0;
                    long allocatedAtStart = GC.GetAllocatedBytesForCurrentThread();
                    while (true)
                    {
                        var query = VerifiedCSharpOccurrenceAuthority.Bind(
                            new(Repo, target, maxResults: pageSize) { ContinuationToken = token }, analysis.Universe, caller.Compilation);
                        var r = await store.FindOccurrencesAsync(query);
                        if (r.Availability != CodeOccurrenceAvailability.Available)
                            throw new InvalidDataException("unexpected availability " + r.Availability);
                        if (r.Occurrences.Count > pageSize) throw new InvalidDataException("page bound exceeded");
                        if (r.TotalMatches != orderedExpected.Length || r.Examined != orderedExpected.Length)
                            throw new InvalidDataException($"Examined/TotalMatches mismatch {r.Examined}/{r.TotalMatches} vs {orderedExpected.Length}");
                        foreach (var hit in r.Occurrences)
                            seen.Add($"{hit.Span.Path}:{hit.Span.StartLine}:{hit.Span.StartColumn}");
                        pages++;
                        if (r.NextContinuationToken is null) break;
                        token = r.NextContinuationToken;
                    }
                    qsw.Stop();
                    var ordered = seen.SequenceEqual(orderedExpected);
                    results.Add(new JsonObject
                    {
                        ["targetCardinality"] = size,
                        ["pageSize"] = pageSize,
                        ["pages"] = pages,
                        ["distinct"] = seen.Distinct(StringComparer.Ordinal).Count(),
                        ["matchesOracle"] = ordered,
                        ["totalMs"] = Math.Round(qsw.Elapsed.TotalMilliseconds, 1),
                        ["allocatedBytes"] = GC.GetAllocatedBytesForCurrentThread() - allocatedAtStart,
                    });
                    notes.Add((ordered && seen.Distinct(StringComparer.Ordinal).Count() == orderedExpected.Length ? "PASS: " : "FAIL: ") +
                        $"size={size} pageSize={pageSize} pages={pages} oracle={ordered}");
                }
                results.Add(new JsonObject { ["targetCardinality"] = size, ["compileMs"] = Math.Round(compileMs, 1), ["publishMs"] = Math.Round(publishMs, 1), ["files"] = files.Count });
            }
            Cleanup(path);
        }
        root["results"] = results;
    }

    // ---- P5: SQ-07 / MD-02 measurement harness ----

    private static async Task MeasureAsync(string database, JsonObject root, JsonArray notes, string detail)
    {
        const int warmup = 2, reps = 7;
        var size = detail.Length == 0 ? 1000 : int.Parse(detail);
        var expected = new List<(string Path, int Line, int Column)>();
        var files = Generate(size, expected);
        var context = Context("run", files);
        var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(context);
        var core = analysis.Projects.Single(p => p.Project.Path == "Core/Core.csproj").Verified!;
        var caller = analysis.Projects.First(p => p.Project.Path.StartsWith("Consumer/", StringComparison.Ordinal)).Verified!;
        var ping = (IMethodSymbol)core.Compilation.GetTypeByMetadataName("Core.Api")!.GetMembers("Ping").Single();
        var target = ExpectedTarget(core, ping);

        var publish = new JsonArray();
        var reopen = new JsonArray();
        var query = new JsonArray();
        for (var i = 0; i < warmup + reps; i++)
        {
            Cleanup(database);
            var plugin = new CSharpCodeGraphPlugin();
            var sw = Stopwatch.StartNew();
            using (var store = new LatticeCodeGraphStore(database))
            {
                await store.UpsertRepositoryAsync(new(Repo));
                await store.StoreIndexRunAsync(new(Repo, new("run"), Started, plugins: [plugin.Id]));
                await using (var sink = new CodeGraphIngestionSink(store))
                await using (var session = await plugin.CreateSessionAsync(context))
                {
                    await session.ExtractAsync(sink);
                    await sink.CompleteAsync();
                }
                await store.CompleteIndexRunAsync(
                    new(Repo, new("run"), Started, CodeIndexRunStatus.Completed, Started.AddSeconds(1), [plugin.Id]),
                    new(Repo, new("run"), []));
            }
            sw.Stop();
            if (i >= warmup) publish.Add(Round(sw.Elapsed.TotalMilliseconds));

            sw.Restart();
            using (var store = new LatticeCodeGraphStore(database))
            {
                sw.Stop();
                if (i >= warmup) reopen.Add(Round(sw.Elapsed.TotalMilliseconds));
                foreach (var pageSize in new[] { 10, 100, 1000 })
                {
                    var first = await store.FindOccurrencesAsync(VerifiedCSharpOccurrenceAuthority.Bind(
                        new(Repo, target, maxResults: pageSize), analysis.Universe, caller.Compilation));
                    var qsw = Stopwatch.StartNew();
                    var again = await store.FindOccurrencesAsync(VerifiedCSharpOccurrenceAuthority.Bind(
                        new(Repo, target, maxResults: pageSize), analysis.Universe, caller.Compilation));
                    qsw.Stop();
                    if (again.Occurrences.Count != first.Occurrences.Count) throw new InvalidDataException("query mismatch");
                    if (i >= warmup)
                        query.Add(new JsonObject { ["pageSize"] = pageSize, ["ms"] = Round(qsw.Elapsed.TotalMilliseconds), ["examined"] = again.Examined, ["returned"] = again.Occurrences.Count });
                }
            }
        }
        Cleanup(database);
        root["targetCardinality"] = size;
        root["warmup"] = warmup;
        root["repetitions"] = reps;
        root["publicationMs"] = publish;
        root["reopenMs"] = reopen;
        root["queryMs"] = query;
        root["note"] = "characterization only; no latency/throughput/memory threshold is claimed; per-page work is target-local O(n log n) (F-OBS-Q1-01).";
        notes.Add("PASS: measurement harness completed with " + reps + " recorded repetitions after " + warmup + " warmup runs");
    }

    // ---- generated-real fixture ----

    private static Dictionary<string, string> Generate(int calls, List<(string Path, int Line, int Column)> expected)
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Core/Core.csproj"] = "<Project Sdk=\"Microsoft.NET.Sdk\" />",
            ["Core/Api.cs"] = "namespace Core; public static class Api { public static int Ping() => 1; }",
            ["Consumer/Consumer.csproj"] =
                "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><ProjectReference Include=\"../Core/Core.csproj\" /></ItemGroup></Project>",
        };
        const int callsPerFile = 100;
        var fileIndex = 0;
        var remaining = calls;
        while (remaining > 0)
        {
            var inFile = Math.Min(callsPerFile, remaining);
            remaining -= inFile;
            var path = $"Consumer/Consumer{fileIndex:D4}.cs";
            var sb = new StringBuilder();
            sb.Append("namespace Consumer.N").Append(fileIndex).Append(";\n");
            sb.Append("public static class C").Append(fileIndex).Append("\n{\n    public static int Run()\n    {\n        var s = 0;\n");
            for (var i = 0; i < inFile; i++)
            {
                var line = sb.ToString().Split('\n').Length; // 1-based next line number
                var text = "        s += Core.Api.Ping();\n";
                var column = text.IndexOf("Ping", StringComparison.Ordinal) + 1;
                sb.Append(text);
                expected.Add((path, line, column));
            }
            sb.Append("        return s;\n    }\n}\n");
            files[path] = sb.ToString();
            fileIndex++;
        }
        return files;
    }

    private static double Round(double value) => Math.Round(value, 3);

    private static void Cleanup(string path)
    {
        foreach (var suffix in new[] { "", "-wal", "-journal", ".wal" })
            if (File.Exists(path + suffix)) File.Delete(path + suffix);
    }

    private static CodeGraphPluginContext Context(string run, IReadOnlyDictionary<string, string> files) =>
        new(Repo, null, new CodeIndexRunId(run),
            files.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair =>
            {
                var bytes = Encoding.UTF8.GetBytes(pair.Value);
                return new CodeGraphSource(pair.Key, "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)),
                    _ => ValueTask.FromResult<Stream>(new MemoryStream(bytes, writable: false)));
            }).ToArray());

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
        if (viaIdentity.Value != viaFrozen) throw new InvalidDataException("independent target computations disagree");
        return viaIdentity;
    }
}
