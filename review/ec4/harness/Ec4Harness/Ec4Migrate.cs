// EC-4D: real p4 -> p5 migration. Opens a durable publication written by the
// authentic p4 extractor (built from the historical commit), checks freshness
// without indexing, reindexes with the current p5 profile, and verifies
// invalidation, rewritten heritage identities, and provenance.
using Penghou.Hetu;
using Penghou.Hetu.LatticeDb;
using Penghou.Hetu.TreeSitter;

internal static class Ec4Migrate
{
    public static async Task<int> RunAsync(string repoPath, string dbPath, string outPath)
    {
        using var store = new LatticeCodeGraphStore(dbPath);
        await using var host = new HetuHostBuilder()
            .AddTreeSitterTypeScriptPlugin()
            .AddTreeSitterJavaScriptPlugin()
            .UseStore(store)
            .Build();
        var repositoryId = new CodeRepositoryId("repo:website-ec4-migration");
        var descriptor = new CodeRepositoryDescriptor(repositoryId, repoPath);
        var queries = host.Queries;

        // Phase 1: observe the p4 publication without reindexing.
        var publicationBefore = await store.GetLatestPublicationAsync(repositoryId);
        var freshness = await host.Indexing.CheckFreshnessAsync(descriptor);
        var heritageBefore = await HeritageFactsAsync(queries, repositoryId);

        // Phase 2: reindex with the p5 profile.
        var indexed = await host.IndexRepositoryAsync(
            descriptor,
            new CodeIndexRunId("run:ec4-p5-migration"),
            diagnostics: d => Console.WriteLine(
                $"p5 migration run: status={d.Status} new={d.FilesNew} changed={d.FilesChanged} " +
                $"unchanged={d.FilesUnchanged} deleted={d.FilesDeleted} " +
                $"nodes={d.NodesProduced} decls={d.DeclarationsProduced} edges={d.EdgesProduced}"));
        var heritageAfter = await HeritageFactsAsync(queries, repositoryId);

        var evidence = new
        {
            publicationBeforeRunId = publicationBefore?.IndexRunId.Value,
            freshness = new
            {
                status = freshness.Status.ToString(),
                added = freshness.NewCount,
                changed = freshness.ChangedCount,
                unchanged = freshness.UnchangedCount,
                deleted = freshness.DeletedCount
            },
            heritageBefore,
            migration = new
            {
                status = indexed.Diagnostics.Status.ToString(),
                filesChanged = indexed.Diagnostics.FilesChanged,
                filesUnchanged = indexed.Diagnostics.FilesUnchanged,
                pluginVersions = indexed.Diagnostics.Plugins.Select(plugin => new
                {
                    pluginId = plugin.PluginId.Value,
                    plugin.PluginVersion
                })
            },
            heritageAfter
        };

        Json.Write(outPath, evidence);
        Console.WriteLine($"heritage before: {System.Text.Json.JsonSerializer.Serialize(heritageBefore)}");
        Console.WriteLine($"heritage after:  {System.Text.Json.JsonSerializer.Serialize(heritageAfter)}");
        return 0;
    }

    private static async Task<object> HeritageFactsAsync(
        CodeGraphQueryService queries,
        CodeRepositoryId repositoryId)
    {
        var supporting = (await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "SupportingPrimitive"))
            .Candidates is [var single] ? single : null;
        if (supporting is null)
            return new { error = "SupportingPrimitive not found" };
        var neighborhood = await queries.GetNeighborhoodAsync(
            repositoryId, supporting.Id,
            [CodeEdgeKinds.Inherits, CodeEdgeKinds.Implements],
            new CodeGraphQueryOptions(maxDepth: 1, maxNodes: 50, maxEdges: 50));
        return new
        {
            edges = neighborhood.Edges.Select(edge => new
            {
                kind = edge.Kind.Value,
                targetId = edge.TargetId.Value,
                targetNamespace = edge.TargetId.Value.Contains(":heritage-target:", StringComparison.Ordinal)
                    ? "heritage-target"
                    : edge.TargetId.Value.Contains(":symbol:", StringComparison.Ordinal)
                        ? "symbol"
                        : "other",
                quality = edge.Evidence.Quality.ToString(),
                providerVersion = edge.Evidence.ProviderVersion
            }),
            targets = neighborhood.Nodes
                .Where(node => node.Id != supporting.Id)
                .Select(node => new
                {
                    id = node.Id.Value,
                    name = node.Name,
                    symbolId = node.SymbolId?.Value
                })
        };
    }
}

