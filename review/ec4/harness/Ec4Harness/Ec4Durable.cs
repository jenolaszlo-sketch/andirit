// EC-4D: durable LatticeDb store, reopen-and-query lifecycle.
using Penghou.Hetu;
using Penghou.Hetu.LatticeDb;
using Penghou.Hetu.TreeSitter;

internal static class Ec4Durable
{
    public static async Task<int> RunAsync(string repoPath, string outDir, string dbPath)
    {
        if (!Directory.Exists(repoPath))
        {
            Console.Error.WriteLine($"repository not found: {repoPath}");
            return 1;
        }

        var evidence = new Dictionary<string, object>(StringComparer.Ordinal);
        string? firstNodeId;
        string? firstPublicationRunId;
        int firstNodeCount;

        // Phase 1: index into the durable store, query, dispose.
        {
            using var store = new LatticeCodeGraphStore(dbPath);
            await using var host = new HetuHostBuilder()
                .AddTreeSitterTypeScriptPlugin()
                .AddTreeSitterJavaScriptPlugin()
                .UseStore(store)
                .Build();
            var repositoryId = new CodeRepositoryId("repo:website-ec4-durable");
            var descriptor = new CodeRepositoryDescriptor(repositoryId, repoPath);
            var indexed = await host.IndexRepositoryAsync(
                descriptor,
                new CodeIndexRunId("run:ec4-durable-1"),
                diagnostics: d => Console.WriteLine(
                    $"[durable-1] status={d.Status} nodes={d.NodesProduced} " +
                    $"decls={d.DeclarationsProduced} edges={d.EdgesProduced}"));
            var queries = host.Queries;
            var withBase = (await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "withBase"))
                .Candidates is [var initial] ? initial : null;
            var declarations = withBase is null
                ? []
                : await queries.FindDeclarationsAsync(repositoryId, withBase.SymbolId!);
            firstNodeId = withBase?.Id.Value;
            firstPublicationRunId = indexed.Publication.IndexRunId.Value;
            firstNodeCount = indexed.Diagnostics.NodesProduced;
            evidence["phase1-after-index"] = new
            {
                status = indexed.Diagnostics.Status.ToString(),
                publicationRunId = firstPublicationRunId,
                withBaseNodeId = firstNodeId,
                withBaseDeclarations = declarations.Select(declaration =>
                    $"{declaration.Location.Path}:{declaration.Location.StartLine}.{declaration.Location.StartColumn}")
            };
        }

        // Phase 2: reopen the same database file and query without reindexing.
        {
            using var store = new LatticeCodeGraphStore(dbPath);
            var repositoryId = new CodeRepositoryId("repo:website-ec4-durable");
            var queries = new CodeGraphQueryService(store);
            var publication = await store.GetLatestPublicationAsync(repositoryId);
            var withBase = (await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "withBase"))
                .Candidates is [var single] ? single : null;
            var schemaDeclarations = await queries.GetDeclarationsInFileAsync(
                repositoryId, "src/lib/schema.ts");
            evidence["phase2-after-reopen"] = new
            {
                publicationRunId = publication?.IndexRunId.Value,
                withBaseNodeId = withBase?.Id.Value,
                sameNodeIdentity = withBase?.Id.Value == firstNodeId,
                schemaDeclarationCount = schemaDeclarations.Count
            };
            Console.WriteLine(
                $"reopen: runId={publication?.IndexRunId.Value} withBase={withBase?.Id.Value} " +
                $"sameIdentity={withBase?.Id.Value == firstNodeId} schemaDecls={schemaDeclarations.Count}");
        }

        evidence["database"] = new { dbPath, firstNodeCount };
        Json.Write(Path.Combine(outDir, "durable.json"), evidence);
        return 0;
    }
}

