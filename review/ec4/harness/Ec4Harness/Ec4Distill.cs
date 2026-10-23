// EC-4 evidence distillation: compact inventories preserved with the report.
using System.Text.Json;
using Penghou.Hetu;
using Penghou.Hetu.TreeSitter;

internal static class Ec4Distill
{
    public static async Task<int> RunAsync(string repoPath, string outDir)
    {

await using var host = new HetuHostBuilder()
    .AddTreeSitterTypeScriptPlugin()
    .AddTreeSitterJavaScriptPlugin()
    .Build();
var repositoryId = new CodeRepositoryId("repo:website-ec4");
var descriptor = new CodeRepositoryDescriptor(repositoryId, repoPath);
var result = await host.IndexRepositoryAsync(descriptor, new CodeIndexRunId("run:ec4-distill"));
var queries = host.Queries;

var allNodes = new Dictionary<string, CodeGraphNode>(StringComparer.Ordinal);
var allEdges = new Dictionary<string, CodeGraphEdge>(StringComparer.Ordinal);
var options = new CodeGraphQueryOptions(maxDepth: 3, maxNodes: 20000, maxEdges: 50000);
foreach (var item in result.Plan.Items)
{
    if (item.Status is CodeIndexPlanStatus.Deleted)
        continue;
    var fileNode = (await queries.FindSymbolsByQualifiedNameAsync(repositoryId, item.Manifest.SourcePath))
        .Candidates.FirstOrDefault(node => node.Kind == CodeNodeKinds.File);
    if (fileNode is null)
        continue;
    var neighborhood = await queries.GetNeighborhoodAsync(repositoryId, fileNode.Id, null, options);
    foreach (var node in neighborhood.Nodes)
        allNodes.TryAdd(node.Id.Value, node);
    foreach (var edge in neighborhood.Edges)
        allEdges.TryAdd(edge.Id.Value, edge);
}

var nameOf = new Func<CodeNodeId, string>(id =>
    allNodes.TryGetValue(id.Value, out var node)
        ? (node.QualifiedName ?? $"{node.Name} <{node.Kind.Value}{(node.SymbolId is null ? ", no-symbol" : "")}>")
        : id.Value);

var declarationItems = new List<(string Location, object Item)>();
foreach (var node in allNodes.Values.Where(node => node.SymbolId is not null).OrderBy(node => node.Id.Value, StringComparer.Ordinal))
{
    foreach (var declaration in await queries.FindDeclarationsAsync(repositoryId, node.SymbolId!))
        declarationItems.Add(
            ($"{declaration.Location.Path}:{declaration.Location.StartLine}.{declaration.Location.StartColumn}-{declaration.Location.EndLine}.{declaration.Location.EndColumn}",
            new
            {
                symbol = node.QualifiedName ?? node.Name,
                kind = node.Kind.Value,
                language = node.Properties.TryGetValue(CodePropertyKeys.Language, out var value) && value is CodeTextProperty text ? text.Value : null,
                location = $"{declaration.Location.Path}:{declaration.Location.StartLine}.{declaration.Location.StartColumn}-{declaration.Location.EndLine}.{declaration.Location.EndColumn}"
            }));
}
var orderedDeclarations = declarationItems
    .OrderBy(item => item.Location, StringComparer.Ordinal)
    .Select(item => item.Item)
    .ToList();

var invocations = allEdges.Values
    .Where(edge => edge.Kind == CodeEdgeKinds.InvokesName)
    .OrderBy(edge => edge.Evidence.Location!.Path, StringComparer.Ordinal)
    .ThenBy(edge => edge.Evidence.Location!.StartLine)
    .Select(edge => new
    {
        from = nameOf(edge.SourceId),
        target = allNodes.TryGetValue(edge.TargetId.Value, out var target) ? target.Name : edge.TargetId.Value,
        location = $"{edge.Evidence.Location!.Path}:{edge.Evidence.Location.StartLine}.{edge.Evidence.Location.StartColumn}-{edge.Evidence.Location.EndLine}.{edge.Evidence.Location.EndColumn}"
    })
    .ToList();

var imports = allEdges.Values
    .Where(edge => edge.Kind == CodeEdgeKinds.Imports)
    .OrderBy(edge => edge.Evidence.Location!.Path, StringComparer.Ordinal)
    .Select(edge => new
    {
        from = nameOf(edge.SourceId),
        module = allNodes.TryGetValue(edge.TargetId.Value, out var target) ? target.QualifiedName ?? target.Name : edge.TargetId.Value,
        location = $"{edge.Evidence.Location!.Path}:{edge.Evidence.Location.StartLine}.{edge.Evidence.Location.StartColumn}"
    })
    .ToList();

Json.Write(Path.Combine(outDir, "declaration-inventory.json"), new
{
    nodeCounts = allNodes.Values.GroupBy(node => node.Kind.Value).OrderBy(group => group.Key)
        .ToDictionary(group => group.Key, group => group.Count()),
    edgeCounts = allEdges.Values.GroupBy(edge => edge.Kind.Value).OrderBy(group => group.Key)
        .ToDictionary(group => group.Key, group => group.Count()),
    evidenceCounts = allEdges.Values.GroupBy(edge => $"{edge.Kind.Value}/{edge.Evidence.Quality}")
        .OrderBy(group => group.Key).ToDictionary(group => group.Key, group => group.Count()),
    orderedDeclarations,
    invocations,
    imports
});
return 0;
    }
}


