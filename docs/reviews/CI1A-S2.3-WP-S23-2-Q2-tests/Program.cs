using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Andirit;
using LatticeDbSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Frozen = AdrCi1Exp.Shared;

// Q2-owned independent requalification probe. It does not link any producer or
// Q1 reviewer test fixture or process program. It independently reads corpus
// source files, constructs real Roslyn compilations, derives the verified
// universe and the frozen expected target, drives the production store, mutates
// real native rows, and queries from separately constructed live authority.
var action = args[0];
var corpus = args[1];
var database = args[2];
var output = args[3];
var detail = args.Length > 4 ? args[4] : "";
var graph = await Q2Graph.Read(corpus);
var notes = new List<string>();

if (action is "publish" or "publish-extra-graph-only" or "retire" or "retire-both")
{
    var run = "review:" + (detail.Length == 0 ? "publish" : detail);
    using var store = new LatticeCodeGraphStore(database);
    await store.UpsertRepositoryAsync(new(Q2Graph.Repo));
    await store.StoreIndexRunAsync(new(Q2Graph.Repo, new(run), Q2Graph.Started, plugins: [Q2Graph.Plugin]));
    foreach (var source in new[] { graph.Client, graph.Client2, graph.Empty })
        await store.StageIndexUnitAsync(graph.Unit(run, Q2Graph.Plugin, source));
    if (action is "retire" or "retire-both")
    {
        await store.StageIndexUnitDeletionAsync(Q2Graph.Repo, new(run), Q2Graph.Plugin, new(graph.Client.Descriptor.ProjectUnitId));
        if (action == "retire-both")
            await store.StageIndexUnitDeletionAsync(Q2Graph.Repo, new(run), Q2Graph.Plugin, new(graph.Client2.Descriptor.ProjectUnitId));
    }
    await Q2Graph.Complete(store, run);
    if (action == "publish-extra-graph-only")
    {
        const string extraRun = "review:extra";
        await store.StoreIndexRunAsync(new(Q2Graph.Repo, new(extraRun), Q2Graph.Started, plugins: [Q2Graph.ExtraPlugin]));
        await store.StageIndexUnitAsync(new CodeIndexUnitReplacement(
            new CodeFactOrigin(Q2Graph.Repo, Q2Graph.ExtraPlugin, "1.0", new(extraRun), new("Extra.csproj")),
            nodes: [new(new("review-node:Extra"), new("review.project"), "extra")]));
        await Q2Graph.Complete(store, extraRun, Q2Graph.ExtraPlugin);
    }
}
if (action == "mutate-remove-graph-owner")
{
    NativeRows.DeleteNode(database, "HetuUnit", Q2Graph.Repo.Value + "\n" + Q2Graph.Plugin.Value + "\n" + detail);
}
if (action == "mutate-remove-occurrence")
{
    var rows = NativeRows.Read(database);
    var key = Q2Graph.Repo.Value + "\n" + Q2Graph.Plugin.Value + "\n" + detail;
    var unit = rows["AndiritOccurrenceUnit|" + key];
    var facts = unit["Facts"]!.AsArray().Count;
    NativeRows.DeleteNode(database, "AndiritOccurrenceUnit", key);
    var metaEntry = rows.First(p => p.Key.StartsWith("AndiritOccurrenceMeta|", StringComparison.Ordinal));
    var meta = metaEntry.Value;
    meta["UnitCount"] = meta["UnitCount"]!.GetValue<int>() - 1;
    meta["FactCount"] = meta["FactCount"]!.GetValue<int>() - facts;
    NativeRows.UpsertNode(database, "AndiritOccurrenceMeta", metaEntry.Key["AndiritOccurrenceMeta|".Length..], meta);
}
if (action == "mutate-clear-marker")
{
    var rows = NativeRows.Read(database);
    var key = Q2Graph.Repo.Value + "\n" + Q2Graph.Plugin.Value + "\n" + detail;
    var unit = rows["HetuUnit|" + key];
    unit["RequiresOccurrenceAuthority"] = false;
    NativeRows.UpsertNode(database, "HetuUnit", key, unit);
}
if (action == "mutate-corrupt-accounting")
{
    var rows = NativeRows.Read(database);
    var metaEntry = rows.First(p => p.Key.StartsWith("AndiritOccurrenceMeta|", StringComparison.Ordinal));
    var meta = metaEntry.Value;
    meta["UnitCount"] = meta["UnitCount"]!.GetValue<int>() + 1;
    NativeRows.UpsertNode(database, "AndiritOccurrenceMeta", metaEntry.Key["AndiritOccurrenceMeta|".Length..], meta);
}
if (action == "mutate-add-graph-only")
{
    const string extraRun = "review:add-graph-only";
    using var store = new LatticeCodeGraphStore(database);
    await store.StoreIndexRunAsync(new(Q2Graph.Repo, new(extraRun), Q2Graph.Started, plugins: [Q2Graph.ExtraPlugin]));
    await store.StageIndexUnitAsync(new CodeIndexUnitReplacement(
        new CodeFactOrigin(Q2Graph.Repo, Q2Graph.ExtraPlugin, "1.0", new(extraRun), new("Extra.csproj")),
        nodes: [new(new("review-node:Extra"), new("review.project"), "extra")]));
    await Q2Graph.Complete(store, extraRun, Q2Graph.ExtraPlugin);
}
if (action == "mutate-add-marker-no-occurrence")
{
    const string extraRun = "review:add-marker";
    using var store = new LatticeCodeGraphStore(database);
    await store.StoreIndexRunAsync(new(Q2Graph.Repo, new(extraRun), Q2Graph.Started, plugins: [Q2Graph.ExtraPlugin]));
    await store.StageIndexUnitAsync(new CodeIndexUnitReplacement(
        new CodeFactOrigin(Q2Graph.Repo, Q2Graph.ExtraPlugin, "1.0", new(extraRun), new("Marker.csproj")),
        nodes: [new(new("review-node:Marker"), new("review.project"), "marker")])
    { RequiresOccurrenceAuthority = true });
    await Q2Graph.Complete(store, extraRun, Q2Graph.ExtraPlugin);
}

var before = NativeRows.Read(database);
CodeOccurrenceQueryResult? result = null;
object[] units = [];
string? openError = null;
try
{
    using var reopened = new LatticeCodeGraphStore(database);
    var query = action == "raw"
        ? new CodeOccurrenceQuery(Q2Graph.Repo, graph.Target)
        : graph.Query();
    result = await reopened.FindOccurrencesAsync(query);
    units = (await reopened.GetPublishedUnitsAsync(Q2Graph.Repo)).Select(u => (object)new
    {
        unit = u.Origin.IndexUnitId.Value,
        plugin = u.Origin.PluginId.Value,
        run = u.Origin.IndexRunId.Value,
        marker = u.RequiresOccurrenceAuthority,
    }).ToArray();
}
catch (CodeOccurrenceSchemaException e) { openError = e.GetType().Name + ": " + e.Message; }
catch (CodeOccurrenceIndexException e) { openError = e.GetType().Name + ": " + e.Message; }
var after = NativeRows.Read(database);
if (JsonSerializer.Serialize(before) != JsonSerializer.Serialize(after))
    throw new Exception("Read changed durable records");

var report = new
{
    processId = Environment.ProcessId,
    action,
    detail,
    corpus,
    notes,
    openError,
    target = graph.Target.Value,
    definingCompctx2 = graph.Api.Descriptor.DescriptorDigest,
    universe = graph.Universe.Identity,
    result,
    graphUnits = units,
    inputs = graph.Entries.Select(e => new
    {
        unit = e.Descriptor.ProjectUnitId,
        compctx2 = e.Descriptor.DescriptorDigest,
        options = e.Descriptor.OptionsDigest,
        sources = e.Sources.Select(s => new { s.Path, s.ContentHash, text = s.Text.ToString() }),
    }),
    nativeRecords = before,
};
File.WriteAllText(output, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"pid={Environment.ProcessId} {action}/{detail}: " +
    $"{result?.Availability}, hits={result?.Occurrences.Count}, complete={result?.IsComplete}, openError={openError}");

internal sealed record Q2Graph(VerifiedCSharpCompilation[] Entries, VerifiedCSharpUniverse Universe)
{
    internal static readonly CodeRepositoryId Repo = new("review:q2-correspondence");
    internal static readonly CodePluginId Plugin = new("review.q2.csharp");
    internal static readonly CodePluginId ExtraPlugin = new("review.q2.extra");
    internal static readonly DateTimeOffset Started = new(2026, 11, 6, 0, 0, 0, TimeSpan.Zero);

    internal VerifiedCSharpCompilation Api => Entries[2];
    internal VerifiedCSharpCompilation Client => Entries[3];
    internal VerifiedCSharpCompilation Client2 => Entries[4];
    internal VerifiedCSharpCompilation Empty => Entries[5];
    internal CodeOccurrenceTargetId Target => Extract(Client).Facts.Single().TargetId;

    internal static async Task<Q2Graph> Read(string corpus)
    {
        var platform = VerifiedMetadataReference.Capture(typeof(object).Assembly.Location);
        var overflow = File.ReadAllText(Path.Combine(corpus, "overflow.txt")).Trim() == "true";
        var entries = new List<VerifiedCSharpCompilation>();
        foreach (var name in new[] { "Seed", "Bridge", "Api", "Client", "Client2", "Empty" })
        {
            var path = name + ".cs";
            var bytes = File.ReadAllBytes(Path.Combine(corpus, path));
            var hash = "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes));
            var source = await VerifiedCSharpSource.ReadAsync(new CodeGraphSource(path, hash,
                _ => ValueTask.FromResult<Stream>(new MemoryStream(bytes, false))));
            var tree = CSharpSyntaxTree.ParseText(source.Text,
                new CSharpParseOptions(LanguageVersion.CSharp14, DocumentationMode.Diagnose), path);
            var dependencies = name switch
            {
                "Seed" or "Empty" => Array.Empty<VerifiedCSharpCompilation>(),
                "Client" or "Client2" => new[] { entries[2] },
                _ => new[] { entries[^1] },
            };
            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, deterministic: true,
                concurrentBuild: false, checkOverflow: name == "Seed" && overflow);
            var compilation = CSharpCompilation.Create("Review" + name, [tree],
                new MetadataReference[] { platform.Reference }.Concat(dependencies.Select(d => d.Compilation.ToMetadataReference())), options);
            var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
            if (errors.Length > 0) throw new Exception(string.Join("; ", errors.Select(d => d.ToString())));
            entries.Add(VerifiedCSharpCompilation.Create(Repo.Value, name + ".csproj", compilation, [source], [platform], dependencies));
        }
        var scope = VerifiedCSharpScope.Declare(Repo.Value, entries.Select(e => new CSharpScopeUnit(
            e.Descriptor.ProjectUnitId, e.Descriptor.ProjectUnitId, e.Sources.Select(s => s.Path).ToImmutableArray())));
        var graph = new Q2Graph(entries.ToArray(), VerifiedCSharpUniverse.Create(Repo.Value, scope, entries));
        var method = (IMethodSymbol)graph.Api.Compilation.GetTypeByMetadataName("Review.Api")!.GetMembers("Fetch").Single();
        var frozen = Frozen.CompilationProvenance.Describe(Repo.Value, graph.Api.Descriptor.ProjectUnitId,
            graph.Api.Compilation, graph.Api.Sources.Select(s => (s.Path, Encoding.UTF8.GetBytes(s.Text.ToString()))).ToArray());
        var expected = "asmctx3:sha256:" + Frozen.ProvenanceHashing.Hash("asmctx3-context",
            ["compctx1", frozen.DescriptorDigest, frozen.MetadataIdentity, frozen.ModuleName, Id(method), Decl(method), Shape(method)]);
        if (graph.Target.Value != expected) throw new Exception("Independent frozen target oracle mismatch");
        foreach (var fact in graph.Extract(graph.Client).Facts.Concat(graph.Extract(graph.Client2).Facts))
        {
            var usage = Frozen.ProvenanceHashing.Hash("usage4", [Repo.Value, Plugin.Value, "usage4-b2c-exp-r2-20261101",
                fact.SourceUnitId, fact.Span.Path, fact.SourceHash, fact.Span.StartLine.ToString(), fact.Span.StartColumn.ToString(),
                fact.Span.EndLine.ToString(), fact.Span.EndColumn.ToString(), fact.TargetDocumentId, expected, "method-invocation"]);
            if (fact.Id.Value != usage) throw new Exception("Independent frozen usage oracle mismatch");
        }
        return graph;
    }

    private static string Id(IMethodSymbol m) => "review-id:" + m.Name;
    private static string Decl(IMethodSymbol m) => "review-decl:" + m.ContainingType.Name + "." + m.Name;
    private static string Shape(IMethodSymbol m) => "method|" + m.Parameters.Length;

    internal VerifiedOccurrenceExtraction Extract(VerifiedCSharpCompilation source) =>
        VerifiedCSharpOccurrenceExtractor.Extract(Universe, new(Repo, Plugin, source,
            Id, Decl, Shape, m => m.GetDocumentationCommentId() ?? m.Name, "1.0"));

    internal CodeIndexUnitReplacement Unit(string run, CodePluginId plugin, VerifiedCSharpCompilation source)
    {
        var origin = new CodeFactOrigin(Repo, plugin, "1.0", new(run), new(source.Descriptor.ProjectUnitId));
        var prepared = Extract(source).PrepareNativeUnit(origin);
        return new CodeIndexUnitReplacement(origin,
            nodes: [new(new("review-node:" + source.Descriptor.ProjectUnitId), new("review.project"), source.Descriptor.ProjectUnitId)],
            occurrences: prepared.Occurrences, occurrenceAnalysisIncluded: true, occurrenceCoverage: prepared.OccurrenceCoverage)
        { OccurrenceAuthority = prepared.OccurrenceAuthority, RequiresOccurrenceAuthority = true };
    }

    internal CodeOccurrenceQuery Query() =>
        VerifiedCSharpOccurrenceAuthority.Bind(new(Repo, Target), Universe, Client.Compilation);

    internal static async Task Complete(LatticeCodeGraphStore store, string run, params CodePluginId[] plugins) =>
        await store.CompleteIndexRunAsync(
            new(Repo, new CodeIndexRunId(run), Started, CodeIndexRunStatus.Completed, Started.AddSeconds(1),
                plugins.Length == 0 ? new[] { Plugin } : plugins),
            new(Repo, new CodeIndexRunId(run), []));
}

internal static class NativeRows
{
    private static readonly string[] Labels =
        ["AndiritOccurrenceUnit", "AndiritOccurrenceMeta", "AndiritOccurrencePage",
         "HetuUnit", "HetuStage", "HetuIndexState", "HetuRun", "HetuBaseline"];

    internal static SortedDictionary<string, JsonObject> Read(string path)
    {
        using var db = LatticeDatabase.Open(path, new LatticeDatabaseOptions { Create = true });
        using var transaction = db.BeginReadTransaction();
        var rows = new SortedDictionary<string, JsonObject>(StringComparer.Ordinal);
        foreach (var label in Labels)
        {
            using var command = db.Prepare($"MATCH (s:{label}) RETURN s.key, s.payload");
            using var result = command.Execute(transaction);
            foreach (var row in result.ReadAll())
            {
                var payload = row.GetValue(1).AsString();
                rows[label + "|" + row.GetValue(0).AsString()] = label == "HetuBaseline"
                    ? new JsonObject { ["rawPayload"] = payload }
                    : JsonNode.Parse(Convert.FromBase64String(payload))!.AsObject();
            }
        }
        transaction.Commit();
        return rows;
    }

    internal static void DeleteNode(string path, string label, string key)
    {
        using var db = LatticeDatabase.Open(path, new LatticeDatabaseOptions { Create = true });
        using var transaction = db.BeginWriteTransaction();
        using (var command = db.Prepare($"MATCH (s:{label}) WHERE s.key = $k DELETE s"))
        {
            command.Bind("k", LatticeValue.From(key));
            using var result = command.Execute(transaction);
            result.ReadAll();
        }
        transaction.Commit();
    }

    internal static void UpsertNode(string path, string label, string key, JsonObject node)
    {
        using var db = LatticeDatabase.Open(path, new LatticeDatabaseOptions { Create = true });
        using var transaction = db.BeginWriteTransaction();
        using (var command = db.Prepare($"MATCH (s:{label}) WHERE s.key = $k DELETE s"))
        {
            command.Bind("k", LatticeValue.From(key));
            using var result = command.Execute(transaction);
            result.ReadAll();
        }
        using (var command = db.Prepare($"CREATE (:{label} {{key: $k, payload: $p}})"))
        {
            command.Bind("k", LatticeValue.From(key));
            command.Bind("p", LatticeValue.From(Convert.ToBase64String(Encoding.UTF8.GetBytes(node.ToJsonString()))));
            using var result = command.Execute(transaction);
            result.ReadAll();
        }
        transaction.Commit();
    }
}
