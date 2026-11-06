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

// Reviewer-owned process harness. No implementation test source is linked.
// Every invocation reconstructs live authority from independently read corpus files.
var action = args[0];
var corpus = args[1];
var database = args[2];
var output = args[3];
var detail = args.Length > 4 ? args[4] : "";
var graph = await ReviewGraph.Read(corpus);
var notes = new List<string>();
var rejected = false;
if (action is "publish" or "partial" or "empty" or "graph-only" or "fault" or "unknown" or "abort" or "stage" or "stage-retain" or "retire" or "mixed")
{
    var arm = false;
    using var store = new LatticeCodeGraphStore(database, null, point =>
    {
        if (arm && point == (action == "unknown" ? "after-commit" : "before-commit"))
            throw new IOException("reviewer fault: " + point);
    });
    var run = "review:" + detail;
    await store.UpsertRepositoryAsync(new(ReviewGraph.Repo));
    await store.StoreIndexRunAsync(new(ReviewGraph.Repo, new(run), ReviewGraph.Started, plugins: [ReviewGraph.Plugin]));
    foreach (var source in action == "empty" ? new[] { graph.Entries[^1] } : action is "stage-retain" or "mixed" ? new[] { graph.Client } : new[] { graph.Client, graph.Entries[^1] })
    {
        var extraction = graph.Extract(source);
        if (action == "partial")
        {
            var coverage = extraction.Coverage.WithState(CodeRelationshipCoverageState.Partial);
            extraction = extraction with { Coverage = coverage, Authority = extraction.Authority! with { Coverage = coverage } };
        }
        var origin = new CodeFactOrigin(ReviewGraph.Repo, ReviewGraph.Plugin, "1.0", new(run), new(source.Descriptor.ProjectUnitId));
        var prepared = extraction.PrepareNativeUnit(origin);
        // A real, independently visible graph node accompanies each occurrence unit.
        var unit = new CodeIndexUnitReplacement(origin,
            nodes: [new(new("review-node:" + source.Descriptor.ProjectUnitId), new("review.project"), detail)],
            occurrences: prepared.Occurrences, occurrenceAnalysisIncluded: true, occurrenceCoverage: prepared.OccurrenceCoverage)
        { OccurrenceAuthority = prepared.OccurrenceAuthority, RequiresOccurrenceAuthority = true };
        if (action == "graph-only") unit = new(origin, nodes: unit.Nodes);
        await store.StageIndexUnitAsync(unit);
    }
    if (action == "retire")
        await store.StageIndexUnitDeletionAsync(ReviewGraph.Repo, new(run), ReviewGraph.Plugin, new(graph.Client.Descriptor.ProjectUnitId));
    if (action == "abort")
        await store.StoreIndexRunAsync(new(ReviewGraph.Repo, new(run), ReviewGraph.Started, CodeIndexRunStatus.Failed,
            ReviewGraph.Started.AddSeconds(1), [ReviewGraph.Plugin]));
    else if (action is not "stage" and not "stage-retain")
    {
        arm = action is "fault" or "unknown";
        try
        {
            await store.CompleteIndexRunAsync(new(ReviewGraph.Repo, new(run), ReviewGraph.Started,
                CodeIndexRunStatus.Completed, ReviewGraph.Started.AddSeconds(1), [ReviewGraph.Plugin]),
                new(ReviewGraph.Repo, new(run), []));
        }
        catch (ArgumentException e) when (action == "mixed")
        {
            rejected = true;
            notes.Add(e.GetType().Name + ": " + e.Message);
        }
        catch (Exception e) when (action is "fault" or "unknown")
        {
            rejected = true;
            notes.Add(e.GetType().Name + ": " + e.Message);
            if (action == "fault" && (e is not IOException || e.Message != "reviewer fault: before-commit")) throw;
            if (action == "unknown")
            {
                try { await store.FindOccurrencesAsync(graph.Query()); }
                catch (CodeGraphCommitOutcomeUnknownException) { notes.Add("read quarantined"); }
                try { await store.UpsertRepositoryAsync(new(ReviewGraph.Repo)); }
                catch (CodeGraphCommitOutcomeUnknownException) { notes.Add("write quarantined"); }
                if (notes.Count != 3) throw new Exception("Unknown-outcome quarantine failed");
            }
        }
        if (action is "fault" or "unknown" && !rejected) throw new Exception("Fault injection was not reached");
        if (action == "mixed" && !rejected) throw new Exception("Conflicting universes accepted");
    }
}
if (action == "resume")
{
    using var store = new LatticeCodeGraphStore(database);
    var run = "review:" + detail;
    await store.CompleteIndexRunAsync(new(ReviewGraph.Repo, new(run), ReviewGraph.Started,
        CodeIndexRunStatus.Completed, ReviewGraph.Started.AddSeconds(1), [ReviewGraph.Plugin]), new(ReviewGraph.Repo, new(run), []));
}
if (action == "mutate") NativeRows.Mutate(database, detail);
var before = NativeRows.Read(database);
CodeOccurrenceQueryResult? result = null;
object[] units = [];
string? openError = null;
using (var document = new MemoryStream())
{
    try
    {
        using var reopened = new LatticeCodeGraphStore(database);
        var query = graph.Query();
        if (action == "raw") query = new(ReviewGraph.Repo, graph.Target);
        if (action == "substitute")
        {
            var other = await ReviewGraph.Read(corpus);
            try { _ = VerifiedCSharpOccurrenceAuthority.Bind(new(ReviewGraph.Repo, graph.Target), graph.Universe, other.Client.Compilation); }
            catch (InvalidDataException e) { notes.Add("substituted instance refused: " + e.Message); rejected = true; }
            if (!rejected) throw new Exception("Substituted caller accepted");
        }
        result = await reopened.FindOccurrencesAsync(query);
        units = (await reopened.GetPublishedUnitsAsync(ReviewGraph.Repo)).Select(u => (object)new
        {
            unit = u.Origin.IndexUnitId.Value, run = u.Origin.IndexRunId.Value,
            nodeNames = u.Nodes.Select(n => n.Name).ToArray(), marker = u.RequiresOccurrenceAuthority
        }).ToArray();
    }
    catch (CodeOccurrenceSchemaException e) { openError = e.GetType().Name + ": " + e.Message; }
    catch (CodeOccurrenceIndexException e) { openError = e.GetType().Name + ": " + e.Message; }
}
var after = NativeRows.Read(database);
if (JsonSerializer.Serialize(before) != JsonSerializer.Serialize(after)) throw new Exception("Read changed durable records");
var report = new
{
    processId = Environment.ProcessId, action, detail, corpus, notes, rejected, openError,
    target = graph.Target.Value, definingCompctx2 = graph.Api.Descriptor.DescriptorDigest,
    universe = graph.Universe.Identity, result, graphUnits = units,
    inputs = graph.Entries.Select(e => new
    {
        unit = e.Descriptor.ProjectUnitId, compctx2 = e.Descriptor.DescriptorDigest, options = e.Descriptor.OptionsDigest,
        sources = e.Sources.Select(s => new { s.Path, s.ContentHash, text = s.Text.ToString() })
    }), nativeRecords = before
};
File.WriteAllText(output, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"pid={Environment.ProcessId} {action}/{detail}: {result?.Availability}, hits={result?.Occurrences.Count}, complete={result?.IsComplete}, openError={openError}");

internal sealed record ReviewGraph(VerifiedCSharpCompilation[] Entries, VerifiedCSharpUniverse Universe)
{
    internal static readonly CodeRepositoryId Repo = new("review:q1-native-authority");
    internal static readonly CodePluginId Plugin = new("review.q1.csharp");
    internal static readonly DateTimeOffset Started = new(2026, 11, 6, 0, 0, 0, TimeSpan.Zero);
    internal VerifiedCSharpCompilation Api => Entries[2];
    internal VerifiedCSharpCompilation Client => Entries[3];
    internal CodeOccurrenceTargetId Target => Extract(Client).Facts.Single().TargetId;
    internal static async Task<ReviewGraph> Read(string corpus)
    {
        var platform = VerifiedMetadataReference.Capture(typeof(object).Assembly.Location);
        var overflow = File.ReadAllText(Path.Combine(corpus, "overflow.txt")).Trim() == "true";
        var entries = new List<VerifiedCSharpCompilation>();
        foreach (var name in new[] { "Seed", "Bridge", "Api", "Client", "Empty" })
        {
            var path = name + ".cs";
            var bytes = File.ReadAllBytes(Path.Combine(corpus, path));
            var hash = "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes));
            var source = await VerifiedCSharpSource.ReadAsync(new CodeGraphSource(path, hash,
                _ => ValueTask.FromResult<Stream>(new MemoryStream(bytes, false))));
            var tree = CSharpSyntaxTree.ParseText(source.Text,
                new CSharpParseOptions(LanguageVersion.CSharp14, DocumentationMode.Diagnose), path);
            var dependencies = name is "Seed" or "Empty" ? Array.Empty<VerifiedCSharpCompilation>() : new[] { entries[^1] };
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
        var graph = new ReviewGraph(entries.ToArray(), VerifiedCSharpUniverse.Create(Repo.Value, scope, entries));
        var method = (IMethodSymbol)graph.Api.Compilation.GetTypeByMetadataName("Review.Api")!.GetMembers("Fetch").Single();
        var frozen = Frozen.CompilationProvenance.Describe(Repo.Value, graph.Api.Descriptor.ProjectUnitId,
            graph.Api.Compilation, graph.Api.Sources.Select(s => (s.Path, Encoding.UTF8.GetBytes(s.Text.ToString()))).ToArray());
        var expected = "asmctx3:sha256:" + Frozen.ProvenanceHashing.Hash("asmctx3-context",
            ["compctx1", frozen.DescriptorDigest, frozen.MetadataIdentity, frozen.ModuleName, Id(method), Decl(method), Shape(method)]);
        if (graph.Target.Value != expected) throw new Exception("Independent frozen target oracle mismatch");
        foreach (var fact in graph.Extract(graph.Client).Facts)
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
    internal VerifiedOccurrenceExtraction Extract(VerifiedCSharpCompilation source) => VerifiedCSharpOccurrenceExtractor.Extract(
        Universe, new(Repo, Plugin, source, Id, Decl, Shape, m => m.GetDocumentationCommentId() ?? m.Name, "1.0"));
    internal CodeOccurrenceQuery Query() => VerifiedCSharpOccurrenceAuthority.Bind(new(Repo, Target), Universe, Client.Compilation);
}

internal static class NativeRows
{
    private static readonly string[] Labels = ["AndiritOccurrenceUnit", "AndiritOccurrenceMeta", "AndiritOccurrencePage",
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
    internal static void Mutate(string path, string mutation)
    {
        var rows = Read(path);
        var label = mutation == "graph-orphan" ? "HetuUnit" : mutation is "meta-complete" or "meta-snapshot" ? "AndiritOccurrenceMeta" :
            mutation == "page-missing-id" ? "AndiritOccurrencePage" : "AndiritOccurrenceUnit";
        var entry = rows.First(p => p.Key.StartsWith(label + "|", StringComparison.Ordinal) &&
            (label is "AndiritOccurrenceMeta" or "AndiritOccurrencePage" || p.Key.EndsWith("Client.csproj", StringComparison.Ordinal)));
        var node = entry.Value;
        switch (mutation)
        {
            case "missing": node.Remove("Authority"); break;
            case "null": node["Authority"] = null; break;
            case "malformed": node["Authority"] = "garbage"; break;
            case "unsupported": node["Authority"]!["Version"] = 99; break;
            case "forged-context": node["Authority"]!["Facts"]![0]!["Compctx2"] = "compctx2:sha256:" + new string('c', 64); break;
            case "null-receipts": node["Authority"]!["Facts"] = null; break;
            case "wrong-owner": node["Authority"]!["IndexUnitId"] = "other.csproj"; break;
            case "wrong-payload": node["Authority"]!["Facts"]![0]!["PayloadHash"] = new string('0', 64); break;
            case "wrong-publication": node["AuthorityPublicationIndexRunId"] = "review:foreign"; break;
            case "meta-complete": node["CompleteCoverage"] = true; break;
            case "meta-snapshot": node["SnapshotIdentity"] = "foreign-snapshot"; node["IsConsistentSnapshot"] = true; break;
            case "page-missing-id": node["OccurrenceIds"] = new JsonArray(); break;
            case "graph-orphan": break;
            default: throw new ArgumentException("Unknown mutation: " + mutation);
        }
        using var db = LatticeDatabase.Open(path, new LatticeDatabaseOptions { Create = true });
        using var transaction = db.BeginWriteTransaction();
        var key = entry.Key[(label.Length + 1)..];
        using (var command = db.Prepare($"MATCH (s:{label}) WHERE s.key = $k DELETE s"))
        {
            command.Bind("k", LatticeValue.From(key));
            using var result = command.Execute(transaction);
            result.ReadAll();
        }
        if (mutation != "graph-orphan")
        {
            using var command = db.Prepare($"CREATE (:{label} {{key: $k, payload: $p}})");
            command.Bind("k", LatticeValue.From(key));
            command.Bind("p", LatticeValue.From(Convert.ToBase64String(Encoding.UTF8.GetBytes(node.ToJsonString()))));
            using var result = command.Execute(transaction);
            result.ReadAll();
        }
        transaction.Commit();
    }
}
