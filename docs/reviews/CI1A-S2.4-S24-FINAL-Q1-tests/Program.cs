using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Andirit;
using LatticeDbSharp;
using Microsoft.CodeAnalysis;
using Frozen = AdrCi1Exp.Shared;

// ANDIRIT-CI1A-S2.4-S24-FINAL-Q1 reviewer-owned final integration probe.
// F1 corpus+partial coverage pagination; F2 real-authority recovery/stale/reindex;
// F3 authority-bearing page/publication mismatch; F4 generated-real 1000 integrated.
// Usage: <verb> <database> <outputJson>.
var action = args[0];
var database = args[1];
var output = args[2];
var result = await FinalQ1Probe.RunAsync(action, database);
File.WriteAllText(output, JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine("RESULT " + (result["ok"]!.GetValue<bool>() ? "OK" : "FAIL") + " " + action + " pid=" + result["processId"]!.GetValue<int>());
return result["ok"]!.GetValue<bool>() ? 0 : 1;

internal static class FinalQ1Probe
{
    internal static readonly CodeRepositoryId Repo = new("repo:s24-final-q1");
    internal static readonly CodePluginId Plugin = new("probe.s24.final.q1");
    internal static readonly DateTimeOffset Started = new(2026, 11, 9, 16, 0, 0, TimeSpan.Zero);

    internal static async Task<JsonObject> RunAsync(string action, string database)
    {
        var root = new JsonObject
        {
            ["action"] = action,
            ["processId"] = Environment.ProcessId,
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
                case "f1": await F1Async(database, root, notes); break;
                case "f2": await F2Async(database, root, notes); break;
                case "f3": await F3Async(database, root, notes); break;
                case "f4": await F4Async(database, root, notes); break;
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

    // ---- F1: authentic corpus pagination under constructed partial coverage ----

    private static async Task F1Async(string database, JsonObject root, JsonArray notes)
    {
        var files = CorpusFiles(variant: false);
        var context = Context("run", files);
        var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(context);
        if (!analysis.Universe.IsComplete) throw new InvalidDataException("universe incomplete");
        var app = analysis.Projects.Single(p => p.Project.Path == "App/App.csproj").Verified!;

        // Real extracted facts from the authentic corpus source (accepted extractor),
        // with a CONSTRUCTED partial coverage state clearly separated from the facts.
        var extraction = VerifiedCSharpOccurrenceExtractor.Extract(analysis.Universe, new(Repo, Plugin, app,
            m => m.Name, m => m.GetDocumentationCommentId() ?? m.Name,
            m => "method|" + m.Arity + "|" + string.Join(",", m.Parameters.Select(p => p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))),
            m => m.GetDocumentationCommentId() ?? m.Name, "1.0.0"));
        var partial = extraction.Coverage.WithState(CodeRelationshipCoverageState.Partial);
        var unit = (extraction with
        {
            Coverage = partial,
            Authority = extraction.Authority! with { Coverage = partial },
        }).PrepareNativeUnit(new CodeFactOrigin(Repo, Plugin, "1.0.0", new CodeIndexRunId("run"), new CodeIndexUnitId(app.Descriptor.ProjectUnitId)));

        Cleanup(database);
        using (var store = new LatticeCodeGraphStore(database))
        {
            await store.UpsertRepositoryAsync(new(Repo));
            await store.StoreIndexRunAsync(new(Repo, new("run"), Started, plugins: [Plugin]));
            await store.StageIndexUnitAsync(unit);
            await store.CompleteIndexRunAsync(new(Repo, new("run"), Started, CodeIndexRunStatus.Completed, Started.AddSeconds(1), [Plugin]), new(Repo, new("run"), []));
        }

        // Independent oracle: real facts for the 2-arg Add overload, sorted by the accepted tuple.
        var target = extraction.Facts
            .GroupBy(f => f.TargetId)
            .OrderByDescending(g => g.Count())
            .First().Key;
        var expected = extraction.Facts.Where(f => f.TargetId == target)
            .OrderBy(f => f.Span.Path, StringComparer.Ordinal).ThenBy(f => f.Span.StartLine).ThenBy(f => f.Span.StartColumn)
            .ThenBy(f => f.Span.EndLine).ThenBy(f => f.Span.EndColumn).ThenBy(f => f.Id.Value, StringComparer.Ordinal)
            .Select(f => $"{f.Span.Path}:{f.Span.StartLine}:{f.Span.StartColumn}").ToArray();
        Assert(root, expected.Length >= 5, $"F1 authentic target has {expected.Length} real facts", notes);

        using var reopened = new LatticeCodeGraphStore(database);
        foreach (var pageSize in new[] { 1, 2, 100 })
        {
            var seen = new List<string>();
            string? token = null;
            while (true)
            {
                var q = VerifiedCSharpOccurrenceAuthority.Bind(new(Repo, target, maxResults: pageSize) { ContinuationToken = token }, analysis.Universe, app.Compilation);
                var r = await reopened.FindOccurrencesAsync(q);
                Assert(root, r.Availability == CodeOccurrenceAvailability.IncompleteAnalysis && !r.IsComplete, $"F1 pageSize={pageSize} IncompleteAnalysis/no false-complete", notes);
                Assert(root, r.Coverage!.State == partial.State && r.UnitCoverage.Any(u => u.IndexUnitId == app.Descriptor.ProjectUnitId && u.State == CodeRelationshipCoverageState.Partial), $"F1 pageSize={pageSize} scope-level partial provenance stable", notes);
                Assert(root, r.TotalMatches == expected.Length && r.Examined == expected.Length, $"F1 pageSize={pageSize} counts", notes);
                foreach (var hit in r.Occurrences) { Assert(root, Same(hit.TargetId, target) && hit.SourceUnitId == app.Descriptor.ProjectUnitId, "F1 target/owner", notes); seen.Add($"{hit.Span.Path}:{hit.Span.StartLine}:{hit.Span.StartColumn}"); }
                if (r.NextContinuationToken is null) break;
                token = r.NextContinuationToken;
            }
            Assert(root, seen.SequenceEqual(expected), $"F1 pageSize={pageSize} authentic order matches oracle (no dup/omission)", notes);
        }
        reopened.Dispose();
        Cleanup(database);
    }

    // ---- F2: real-authority recovery, source change, stale continuation ----

    private static async Task F2Async(string database, JsonObject root, JsonArray notes)
    {
        var ctxA = Context("run:a", CorpusFiles(variant: false));
        await PublishRealAsync(database, "run:a", ctxA);
        var analysisA = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(ctxA);
        var appA = analysisA.Projects.Single(p => p.Project.Path == "App/App.csproj").Verified!;
        var coreA = analysisA.Projects.Single(p => p.Project.Path == "Core/Core.csproj").Verified!;
        var target = ExpectedTarget(coreA, IntAdd(coreA));

        string token;
        using (var store = new LatticeCodeGraphStore(database))
        {
            var q = VerifiedCSharpOccurrenceAuthority.Bind(new(Repo, target, maxResults: 1), analysisA.Universe, appA.Compilation);
            var r = await store.FindOccurrencesAsync(q);
            Assert(root, r.Availability == CodeOccurrenceAvailability.Available && r.NextContinuationToken is not null, "F2 real publication serves a first page", notes);
            token = r.NextContinuationToken!;
        }
        using (var reopened = new LatticeCodeGraphStore(database))
        {
            var r = await reopened.FindOccurrencesAsync(VerifiedCSharpOccurrenceAuthority.Bind(new(Repo, target, maxResults: 1) { ContinuationToken = token }, analysisA.Universe, appA.Compilation));
            Assert(root, r.Availability == CodeOccurrenceAvailability.Available && r.Publication!.IndexRunId.Value == "run:a", "F2 continuation after durable reopen stays in run:a", notes);
        }
        var arm = false;
        using (var store = new LatticeCodeGraphStore(database, null, p => { if (arm && p == "before-commit") throw new Fault(); }))
        {
            await store.StoreIndexRunAsync(new(Repo, new("run:fail"), Started.AddSeconds(5), plugins: [Plugin]));
            await store.StageIndexUnitAsync(new CodeIndexUnitReplacement(
                new CodeFactOrigin(Repo, Plugin, "1.0.0", new CodeIndexRunId("run:fail"), new CodeIndexUnitId("extra")),
                [new CodeGraphNode(new("node:extra"), CodeNodeKinds.Callable, "extra", "extra", new CodeSymbolId("symbol:extra"))]));
            arm = true;
            try { await store.CompleteIndexRunAsync(new(Repo, new("run:fail"), Started.AddSeconds(5), CodeIndexRunStatus.Completed, Started.AddSeconds(6), [Plugin]), new(Repo, new("run:fail"), [])); } catch (Fault) { }
            arm = false;
            var r = await store.FindOccurrencesAsync(VerifiedCSharpOccurrenceAuthority.Bind(new(Repo, target, maxResults: 1) { ContinuationToken = token }, analysisA.Universe, appA.Compilation));
            Assert(root, r.Availability == CodeOccurrenceAvailability.Available && r.Occurrences.Count == 1, "F2 recovery between pages preserves the real-authority publication", notes);
        }
        var ctxB = Context("run:b", CorpusFiles(variant: true));
        await PublishRealAsync(database, "run:b", ctxB);
        var analysisB = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(ctxB);
        var appB = analysisB.Projects.Single(p => p.Project.Path == "App/App.csproj").Verified!;
        using (var store = new LatticeCodeGraphStore(database))
        {
            var stale = await store.FindOccurrencesAsync(VerifiedCSharpOccurrenceAuthority.Bind(new(Repo, target, maxResults: 1) { ContinuationToken = token }, analysisA.Universe, appA.Compilation));
            Assert(root, stale.Availability == CodeOccurrenceAvailability.StalePublication && stale.Occurrences.Count == 0, "F2 stale continuation after source-changed replacement refuses", notes);
            var restarted = await store.FindOccurrencesAsync(VerifiedCSharpOccurrenceAuthority.Bind(new(Repo, target, maxResults: 1), analysisB.Universe, appB.Compilation));
            Assert(root, restarted.Availability == CodeOccurrenceAvailability.Available && restarted.Publication!.IndexRunId.Value == "run:b", "F2 explicit restart serves the replacement universe", notes);
        }
        Cleanup(database);
    }

    // ---- F3: authority-bearing page/publication mismatch ----

    private static async Task F3Async(string database, JsonObject root, JsonArray notes)
    {
        var ctx = Context("run:a", CorpusFiles(variant: false));
        await PublishRealAsync(database, "run:a", ctx);
        // Verify authority state exists, then tamper the occurrence page's publication binding.
        using (var db = LatticeDatabase.Open(database, new LatticeDatabaseOptions { Create = false }))
        using (var txn = db.BeginReadTransaction())
        using (var q = db.Prepare("MATCH (s:AndiritOccurrencePage) RETURN s.key, s.payload"))
        using (var r = q.Execute(txn))
        {
            var rows = r.ReadAll();
            Assert(root, rows.Count >= 1, "F3 authority-bearing publication has occurrence pages", notes);
            txn.Commit();
        }
        using (var db = LatticeDatabase.Open(database, new LatticeDatabaseOptions { Create = false }))
        using (var txn = db.BeginWriteTransaction())
        {
            string key; JsonObject row;
            using (var q = db.Prepare("MATCH (s:AndiritOccurrencePage) RETURN s.key, s.payload"))
            using (var r = q.Execute(txn))
            {
                var first = r.ReadAll().First();
                key = first.GetValue(0).AsString();
                row = JsonNode.Parse(Convert.FromBase64String(first.GetValue(1).AsString()))!.AsObject();
            }
            row["PublicationIndexRunId"] = "run:foreign";
            using (var q = db.Prepare("MATCH (s:AndiritOccurrencePage) WHERE s.key = $k SET s.payload = $p"))
            {
                q.Bind("k", LatticeValue.From(key));
                q.Bind("p", LatticeValue.From(Convert.ToBase64String(Encoding.UTF8.GetBytes(row.ToJsonString()))));
                using var r = q.Execute(txn);
                r.ReadAll();
            }
            txn.Commit();
        }
        var thrown = "none";
        try { using var store = new LatticeCodeGraphStore(database); } catch (Exception e) { thrown = e.GetType().Name; }
        root["exception"] = thrown;
        Assert(root, thrown == "CodeOccurrenceIndexException", "F3 authority-bearing page/publication mismatch refused at open with CodeOccurrenceIndexException", notes);
        Cleanup(database);
    }

    // ---- F4: generated-real 1000 integrated traversal ----

    private static async Task F4Async(string database, JsonObject root, JsonArray notes)
    {
        var expected = new List<(string Path, int Line, int Column)>();
        var files = Generate(1000, expected);
        var context = Context("run", files);
        var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(context);
        var core = analysis.Projects.Single(p => p.Project.Path == "Core/Core.csproj").Verified!;
        var caller = analysis.Projects.First(p => p.Project.Path.StartsWith("Consumer/", StringComparison.Ordinal)).Verified!;
        var ping = (IMethodSymbol)core.Compilation.GetTypeByMetadataName("Core.Api")!.GetMembers("Ping").Single();
        var target = ExpectedTarget(core, ping);
        await PublishRealAsync(database, "run", context);
        var ordered = expected.OrderBy(s => s.Path, StringComparer.Ordinal).ThenBy(s => s.Line).ThenBy(s => s.Column)
            .Select(s => $"{s.Path}:{s.Line}:{s.Column}").ToArray();

        using var store = new LatticeCodeGraphStore(database);
        foreach (var pageSize in new[] { 1, 10, 100, 1000 })
        {
            var seen = new List<string>();
            string? token = null;
            while (true)
            {
                var q = VerifiedCSharpOccurrenceAuthority.Bind(new(Repo, target, maxResults: pageSize) { ContinuationToken = token }, analysis.Universe, caller.Compilation);
                var r = await store.FindOccurrencesAsync(q);
                Assert(root, r.Availability == CodeOccurrenceAvailability.Available && r.IsComplete == false || r.NextContinuationToken is null, "F4 availability/continuation", notes);
                Assert(root, r.Occurrences.Count <= pageSize && r.TotalMatches == 1000 && r.Examined == 1000, $"F4 pageSize={pageSize} bounds/counts", notes);
                Assert(root, r.UnitCoverage.Count >= 1 && r.UnitCoverage.All(u => u.AnalysisPerformed), $"F4 pageSize={pageSize} coverage breakdown present", notes);
                foreach (var hit in r.Occurrences) { Assert(root, Same(hit.TargetId, target), "F4 target identity", notes); seen.Add($"{hit.Span.Path}:{hit.Span.StartLine}:{hit.Span.StartColumn}"); }
                if (r.NextContinuationToken is null) break;
                token = r.NextContinuationToken;
            }
            Assert(root, seen.SequenceEqual(ordered), $"F4 pageSize={pageSize} generated-real traversal matches source oracle", notes);
        }
        // Negative controls: missing authority refuses; after replacement the old token is stale.
        var noAuth = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(Repo, target, maxResults: 10));
        Assert(root, noAuth.Availability == CodeOccurrenceAvailability.AnalysisUnavailable, "F4 missing authority refuses", notes);
        var tok = (await store.FindOccurrencesAsync(VerifiedCSharpOccurrenceAuthority.Bind(new(Repo, target, maxResults: 10), analysis.Universe, caller.Compilation))).NextContinuationToken!;
        store.Dispose();
        var ctx2 = Context("run:2", Generate(1000, new List<(string, int, int)>()));
        await PublishRealAsync(database, "run:2", ctx2);
        using (var reopened = new LatticeCodeGraphStore(database))
        {
            var stale = await reopened.FindOccurrencesAsync(VerifiedCSharpOccurrenceAuthority.Bind(new(Repo, target, maxResults: 10) { ContinuationToken = tok }, analysis.Universe, caller.Compilation));
            Assert(root, stale.Availability == CodeOccurrenceAvailability.StalePublication || stale.Availability == CodeOccurrenceAvailability.CorruptedIndex, "F4 changed publication refuses the old token", notes);
        }
    }

    // ---- helpers ----

    private static bool Same(CodeOccurrenceTargetId a, CodeOccurrenceTargetId b) => a.Value == b.Value;

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
        await store.CompleteIndexRunAsync(new(Repo, new(run), Started, CodeIndexRunStatus.Completed, Started.AddSeconds(1), [plugin.Id]), new(Repo, new(run), []));
    }

    private static IReadOnlyDictionary<string, string> CorpusFiles(bool variant)
    {
        var root = Path.Combine(RepoRoot(), "docs", "planning", "adr-ci1-01-ci1a-s2-0", "corpus");
        var files = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var project in new[] { "App", "Core", "Leaf", "Solo" })
            files[$"{project}/{project}.csproj"] = File.ReadAllText(Path.Combine(root, "repo", project, $"{project}.csproj"));
        foreach (var file in new[] { "App/Bulk.cs", "App/Sites.cs", "Core/Extensions.cs", "Core/LeafUser.cs", "Core/Methods.cs", "Core/Types.cs", "Leaf/Helper.cs", "Solo/Declarations.cs" })
            files[file] = File.ReadAllText(Path.Combine(root, "repo", file.Replace('/', Path.DirectorySeparatorChar)));
        if (variant)
            files["App/Sites.cs"] = File.ReadAllText(Path.Combine(root, "variants", "source-changed", "App", "Sites.cs"));
        return files;
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null) { if (File.Exists(Path.Combine(dir.FullName, "Andirit.slnx"))) return dir.FullName; dir = dir.Parent; }
        throw new InvalidOperationException("root not found");
    }

    private static Dictionary<string, string> Generate(int calls, List<(string Path, int Line, int Column)> expected)
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Core/Core.csproj"] = "<Project Sdk=\"Microsoft.NET.Sdk\" />",
            ["Core/Api.cs"] = "namespace Core; public static class Api { public static int Ping() => 1; }",
            ["Consumer/Consumer.csproj"] = "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><ProjectReference Include=\"../Core/Core.csproj\" /></ItemGroup></Project>",
        };
        var index = 0; var remaining = calls;
        while (remaining > 0)
        {
            var inFile = Math.Min(100, remaining); remaining -= inFile;
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
            files[path] = sb.ToString(); index++;
        }
        return files;
    }

    private static CodeGraphPluginContext Context(string run, IReadOnlyDictionary<string, string> files) =>
        new(Repo, null, new CodeIndexRunId(run), files.OrderBy(p => p.Key, StringComparer.Ordinal).Select(pair =>
        {
            var bytes = Encoding.UTF8.GetBytes(pair.Value);
            return new CodeGraphSource(pair.Key, "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)),
                _ => ValueTask.FromResult<Stream>(new MemoryStream(bytes, writable: false)));
        }).ToArray());

    // The corpus declares four 2-argument Add overloads; select the int,int one.
    private static IMethodSymbol IntAdd(VerifiedCSharpCompilation unit) =>
        unit.Compilation.GetTypeByMetadataName("Corpus.Core.Methods")!.GetMembers("Add").OfType<IMethodSymbol>()
            .Single(m => m.Parameters.Length == 2 && m.Parameters.All(p => p.Type.SpecialType == SpecialType.System_Int32));

    private static CodeOccurrenceTargetId ExpectedTarget(VerifiedCSharpCompilation defining, IMethodSymbol method)
    {
        var mapped = defining.MapR2FaithfulCompctx1();
        var identifier = CodeOccurrenceIdentity.ComputeIdentifierDigest([method.Name]);
        var declaration = CodeOccurrenceIdentity.ComputeDeclarationDigest([method.GetDocumentationCommentId() ?? method.Name]);
        var shape = CodeOccurrenceIdentity.ComputeShapeDigest("method|" + method.Arity + "|" +
            string.Join(",", method.Parameters.Select(p => p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))));
        var viaIdentity = CodeOccurrenceIdentity.ComputeTargetId(mapped.DescriptorDigest, mapped.MetadataIdentity, mapped.ModuleName, identifier, declaration, shape);
        var frozen = Frozen.CompilationProvenance.Describe(Repo.Value, defining.Descriptor.ProjectUnitId,
            defining.Compilation, defining.Sources.Select(s => (s.Path, Encoding.UTF8.GetBytes(s.Text.ToString()))).ToArray());
        var viaFrozen = "asmctx3:sha256:" + Frozen.ProvenanceHashing.Hash("asmctx3-context",
            ["compctx1", frozen.DescriptorDigest, frozen.MetadataIdentity, frozen.ModuleName, identifier, declaration, shape]);
        if (viaIdentity.Value != viaFrozen) throw new InvalidDataException("independent target computations disagree");
        return viaIdentity;
    }

    private static void Assert(JsonObject root, bool condition, string message, JsonArray notes)
    {
        notes.Add((condition ? "PASS: " : "FAIL: ") + message);
        if (!condition) root["ok"] = false;
    }

    private static void Cleanup(string path)
    {
        foreach (var suffix in new[] { "", "-wal", "-journal", ".wal" })
            if (File.Exists(path + suffix)) File.Delete(path + suffix);
    }

    private sealed class Fault : Exception;
}
