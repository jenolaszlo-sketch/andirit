using System.Text.Json;
using Andirit;
using Microsoft.CodeAnalysis;
using F = Andirit.CSharp.Tests.S23NativeAuthorityFixture;

// Each invocation is a fresh process with new source captures, verified live
// compilations and universe. No in-memory authority crosses this boundary.
var action = args[0];
var variant = args[1];
var expected = Enum.Parse<CodeOccurrenceAvailability>(args[2]);
var path = args[3];
var output = args[4];
var graph = await F.Build(variant);
var method = (IMethodSymbol)graph.Lib.Compilation.GetTypeByMetadataName("B.Lib")!.GetMembers("V").Single();
if (graph.LibTarget.Value != F.ExpectedTarget(graph.Lib, method)) throw new Exception("Frozen target oracle differs");
foreach (var fact in graph.Extract(graph.App).Facts)
    if (fact.Id.Value != F.ExpectedUsage(fact)) throw new Exception("Frozen occurrence oracle differs");
if (action == "publish")
{
    using var store = new LatticeCodeGraphStore(path);
    await graph.Publish(store, "run:" + variant, graph.App, graph.App2, graph.Empty);
}
var before = F.Snapshot(path);
CodeOccurrenceQueryResult result;
using (var reopened = new LatticeCodeGraphStore(path))
    result = await reopened.FindOccurrencesAsync(graph.Query(graph.LibTarget));
if (result.Availability != expected) throw new Exception("Unexpected native result: " + result.Availability);
if (expected == CodeOccurrenceAvailability.Available && result.Occurrences.Count != 2) throw new Exception("Current positive control failed");
if (expected != CodeOccurrenceAvailability.Available && (result.Occurrences.Count != 0 || result.Examined != 0)) throw new Exception("Stale facts leaked");
if (before != F.Snapshot(path)) throw new Exception("Query changed persisted evidence");
var report = new
{
    processId = Environment.ProcessId,
    action,
    variant,
    result.Availability,
    result.RefusalReason,
    result.Publication,
    result.TotalMatches,
    result.Examined,
    result.IsComplete,
    expectedTarget = F.ExpectedTarget(graph.Lib, method),
    currentDefiningCompctx2 = graph.Lib.Descriptor.DescriptorDigest,
    universe = graph.Universe.Identity,
    sources = graph.Entries.Select(e => new
    {
        unit = e.Descriptor.ProjectUnitId,
        context = e.Descriptor.DescriptorDigest,
        options = e.Descriptor.OptionsDigest,
        sources = e.Sources.Select(s => new { s.Path, s.ContentHash, text = s.Text.ToString() })
    }),
    nativeRecords = F.Rows(path),
};
File.WriteAllText(output, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"{action}/{variant}: {result.Availability}, hits={result.Occurrences.Count}, pid={Environment.ProcessId}; {output}");
