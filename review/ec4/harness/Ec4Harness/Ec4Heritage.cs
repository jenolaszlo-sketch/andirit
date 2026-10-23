// EC-4B4: focused reproduction of the real-world heritage + F-Q2-01 scenario.
using Penghou.Hetu;
using Penghou.Hetu.TreeSitter;

internal static class Ec4Heritage
{
    public static async Task<int> RunAsync(string repoPath, string outPath)
    {
        await using var host = new HetuHostBuilder()
            .AddTreeSitterTypeScriptPlugin()
            .AddTreeSitterJavaScriptPlugin()
            .Build();
        var repositoryId = new CodeRepositoryId("repo:website-ec4");
        var descriptor = new CodeRepositoryDescriptor(repositoryId, repoPath);
        await host.IndexRepositoryAsync(descriptor, new CodeIndexRunId("run:ec4-heritage"));

        var queries = host.Queries;
        var projectRef = await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "ProjectRef");
        var supporting = (await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "SupportingPrimitive"))
            .Candidates is [var single] ? single : null;
        var declaredProjectRef = projectRef.Candidates
            .FirstOrDefault(node => node.SymbolId is not null);
        var heritageProjectRef = projectRef.Candidates
            .FirstOrDefault(node => node.SymbolId is null);
        var impactOfDeclaredProjectRef = declaredProjectRef is null
            ? null
            : await queries.GetImpactSetAsync(repositoryId, declaredProjectRef.Id);
        var impactOfHeritageProjectRef = heritageProjectRef is null
            ? null
            : await queries.GetImpactSetAsync(repositoryId, heritageProjectRef.Id);

        List<object>? heritageEdges = null;
        if (supporting is not null)
        {
            var neighborhood = await queries.GetNeighborhoodAsync(
                repositoryId, supporting.Id,
                [CodeEdgeKinds.Inherits, CodeEdgeKinds.Implements],
                new CodeGraphQueryOptions(maxDepth: 1, maxNodes: 50, maxEdges: 50));
            heritageEdges = neighborhood.Edges.Select(edge => (object)new
            {
                kind = edge.Kind.Value,
                target = edge.TargetId.Value,
                evidenceQuality = edge.Evidence.Quality.ToString(),
                location = $"{edge.Evidence.Location!.Path}:{edge.Evidence.Location.StartLine}.{edge.Evidence.Location.StartColumn}"
            }).ToList();
        }

        var evidence = new
        {
            exactLookupProjectRef = new
            {
                candidateCount = projectRef.Candidates.Count,
                candidates = projectRef.Candidates.Select(node => (object)new
                {
                    id = node.Id.Value,
                    kind = node.Kind.Value,
                    name = node.Name,
                    qualifiedName = node.QualifiedName,
                    symbolId = node.SymbolId?.Value,
                    declared = node.SymbolId is not null
                })
            },
            supportingPrimitiveHeritage = heritageEdges,
            impactSetOfDeclaredProjectRef = impactOfDeclaredProjectRef is null
                ? null
                : new
                {
                    seededNode = declaredProjectRef!.Id.Value,
                    nodeCount = impactOfDeclaredProjectRef.Nodes.Count,
                    names = impactOfDeclaredProjectRef.Nodes.Select(node => node.Name)
                },
            impactSetOfHeritageTarget = impactOfHeritageProjectRef is null
                ? null
                : new
                {
                    seededNode = heritageProjectRef!.Id.Value,
                    nodeCount = impactOfHeritageProjectRef.Nodes.Count,
                    names = impactOfHeritageProjectRef.Nodes.Select(node => node.Name)
                },
            note = "The declared interface ProjectRef and the declaration-less heritage-target node share one displayed name; exact lookup must preserve both, and the declared interface must not gain dependents through the same-spelled candidate."
        };

        Json.Write(outPath, evidence);
        Console.WriteLine($"ProjectRef exact-lookup candidates: {projectRef.Candidates.Count}");
        foreach (var candidate in projectRef.Candidates)
            Console.WriteLine($"  {candidate.Id.Value} symbolId={(candidate.SymbolId?.Value ?? "none")}");
        return 0;
    }
}
