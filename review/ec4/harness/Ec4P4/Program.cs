// EC-4 historical p4 publication builder (authentic extractor built from e4598ac).
using Penghou.Hetu;
using Penghou.Hetu.LatticeDb;
using Penghou.Hetu.TreeSitter;

var repoPath = args[0];
var dbPath = args[1];

using var store = new LatticeCodeGraphStore(dbPath);
await using var host = new HetuHostBuilder()
    .AddTreeSitterTypeScriptPlugin()
    .AddTreeSitterJavaScriptPlugin()
    .UseStore(store)
    .Build();
var repositoryId = new CodeRepositoryId("repo:website-ec4-migration");
var descriptor = new CodeRepositoryDescriptor(repositoryId, repoPath);
var indexed = await host.IndexRepositoryAsync(
    descriptor,
    new CodeIndexRunId("run:ec4-p4-publication"),
    diagnostics: d => Console.WriteLine(
        $"p4 indexing: status={d.Status} nodes={d.NodesProduced} decls={d.DeclarationsProduced} edges={d.EdgesProduced}"));
Console.WriteLine($"p4 plugin versions:");
foreach (var plugin in indexed.Diagnostics.Plugins)
    Console.WriteLine($"  {plugin.PluginId.Value} = {plugin.PluginVersion}");
var queries = host.Queries;
var supporting = (await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "SupportingPrimitive")).Candidates;
Console.WriteLine($"SupportingPrimitive candidates: {supporting.Count}");
var heritageEdges = await queries.TraverseWithProvenanceAsync(
    repositoryId,
    supporting[0].Id,
    CodeGraphDirection.Outgoing,
    [CodeEdgeKinds.Inherits, CodeEdgeKinds.Implements],
    null,
    CancellationToken.None);
foreach (var edge in heritageEdges!.Result.Edges)
    Console.WriteLine($"  heritage: {edge.Kind.Value} -> {edge.TargetId.Value} [{edge.Evidence.Quality}]");
Console.WriteLine("p4 publication written.");
