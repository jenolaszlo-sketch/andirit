// EC-4A/4B/4C review harness: baseline indexing, full fact dump, Q1-Q8.
using System.Text.Json;
using System.Text.Json.Serialization;
using Penghou.Hetu;
using Penghou.Hetu.TreeSitter;

internal static class Json
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    public static void Write(string path, object value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(value, Options));
        Console.WriteLine($"wrote {path}");
    }
}

internal static class Dto
{
    public static object Node(CodeGraphNode node) => new
    {
        id = node.Id.Value,
        kind = node.Kind.Value,
        name = node.Name,
        qualifiedName = node.QualifiedName,
        symbolId = node.SymbolId?.Value,
        language = Prop(node, CodePropertyKeys.Language),
        modifiers = Prop(node, CodePropertyKeys.Modifiers),
        access = Prop(node, CodePropertyKeys.Access),
        contentHash = Prop(node, CodePropertyKeys.ContentHash)
    };

    public static object Edge(CodeGraphEdge edge) => new
    {
        id = edge.Id.Value,
        kind = edge.Kind.Value,
        source = edge.SourceId.Value,
        target = edge.TargetId.Value,
        evidence = edge.Evidence.Quality.ToString(),
        location = edge.Evidence.Location is null
            ? null
            : $"{edge.Evidence.Location.Path}:{edge.Evidence.Location.StartLine}.{edge.Evidence.Location.StartColumn}-{edge.Evidence.Location.EndLine}.{edge.Evidence.Location.EndColumn}",
        plugin = edge.Evidence.Provider
    };

    public static object Declaration(CodeGraphDeclaration declaration) => new
    {
        id = declaration.Id.Value,
        symbolId = declaration.SymbolId.Value,
        nodeId = declaration.SymbolNodeId.Value,
        location = $"{declaration.Location.Path}:{declaration.Location.StartLine}.{declaration.Location.StartColumn}-{declaration.Location.EndLine}.{declaration.Location.EndColumn}"
    };

    private static string? Prop(CodeGraphNode node, string key) =>
        node.Properties.TryGetValue(key, out var value) && value is CodeTextProperty text
            ? text.Value
            : null;
}

/// <summary>EC-4A/4B/4C: baseline indexing, full fact dump, developer queries.</summary>
internal static class Ec4Full
{
    public static async Task<int> RunAsync(string repoPath, string outDir)
    {
        if (!Directory.Exists(repoPath))
        {
            Console.Error.WriteLine($"repository not found: {repoPath}");
            return 1;
        }

        await using var host = new HetuHostBuilder()
            .AddTreeSitterTypeScriptPlugin()
            .AddTreeSitterJavaScriptPlugin()
            .Build();
        var repositoryId = new CodeRepositoryId("repo:website-ec4");
        var descriptor = new CodeRepositoryDescriptor(repositoryId, repoPath);

        var wall = System.Diagnostics.Stopwatch.StartNew();
        var result = await host.IndexRepositoryAsync(
            descriptor,
            new CodeIndexRunId("run:ec4-baseline"),
            diagnostics: d => Console.WriteLine(
                $"diagnostics: status={d.Status} discovered={d.FilesDiscovered} new={d.FilesNew} " +
                $"changed={d.FilesChanged} unchanged={d.FilesUnchanged} deleted={d.FilesDeleted} " +
                $"unsupported={d.FilesUnsupported} excludedDirs={d.DirectoriesExcluded} " +
                $"bytes={d.SourceBytesRead} nodes={d.NodesProduced} decls={d.DeclarationsProduced} " +
                $"edges={d.EdgesProduced} rejected={d.RejectedFacts} warnings=[{string.Join(",", d.WarningCodes)}]"));
        wall.Stop();
        Console.WriteLine($"indexing wall time: {wall.Elapsed.TotalMilliseconds:F1} ms");
        Console.WriteLine(
            $"durations: planning={result.Diagnostics.PlanningDuration.TotalMilliseconds:F1}ms " +
            $"extraction={result.Diagnostics.ExtractionDuration.TotalMilliseconds:F1}ms " +
            $"persistence={result.Diagnostics.PersistenceDuration.TotalMilliseconds:F1}ms");

        var queries = host.Queries;

        // ---- aggregate the published graph by walking every file node ----
        var allNodes = new Dictionary<string, CodeGraphNode>(StringComparer.Ordinal);
        var allEdges = new Dictionary<string, CodeGraphEdge>(StringComparer.Ordinal);
        var perFile = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var fileNodeIds = new Dictionary<string, string>(StringComparer.Ordinal);
        var traversalOptions = new CodeGraphQueryOptions(maxDepth: 3, maxNodes: 20000, maxEdges: 50000);

        foreach (var item in result.Plan.Items)
        {
            if (item.Status is CodeIndexPlanStatus.Deleted)
                continue;
            var itemPath = item.Manifest.SourcePath;
            var fileNodes = await queries.FindSymbolsByQualifiedNameAsync(repositoryId, itemPath);
            var fileNode = fileNodes.Candidates.FirstOrDefault(node => node.Kind == CodeNodeKinds.File);
            if (fileNode is null)
            {
                Console.WriteLine($"MISSING FILE NODE: {itemPath} ({item.Status})");
                continue;
            }

            fileNodeIds[itemPath] = fileNode.Id.Value;
            var neighborhood = await queries.GetNeighborhoodAsync(
                repositoryId, fileNode.Id, null, traversalOptions);
            perFile[itemPath] = [];
            foreach (var node in neighborhood.Nodes)
            {
                if (allNodes.TryAdd(node.Id.Value, node))
                    perFile[itemPath].Add(node.Id.Value);
            }

            foreach (var edge in neighborhood.Edges)
                allEdges.TryAdd(edge.Id.Value, edge);
        }

        var nodeKinds = allNodes.Values.GroupBy(node => node.Kind.Value)
            .OrderBy(group => group.Key)
            .ToDictionary(group => group.Key, group => group.Count());
        var edgeKinds = allEdges.Values.GroupBy(edge => edge.Kind.Value)
            .OrderBy(group => group.Key)
            .ToDictionary(group => group.Key, group => group.Count());
        var evidenceByKind = allEdges.Values
            .GroupBy(edge => $"{edge.Kind.Value}/{edge.Evidence.Quality}")
            .OrderBy(group => group.Key)
            .ToDictionary(group => group.Key, group => group.Count());
        var symbolBearing = allNodes.Values.Where(node => node.SymbolId is not null).ToList();
        var declarations = new Dictionary<string, CodeGraphDeclaration>(StringComparer.Ordinal);
        foreach (var node in symbolBearing)
        {
            foreach (var declaration in await queries.FindDeclarationsAsync(repositoryId, node.SymbolId!))
                declarations.TryAdd(declaration.Id.Value, declaration);
        }

        var declarationKinds = new Dictionary<string, int>();
        foreach (var declaration in declarations.Values)
        {
            var kind = allNodes.TryGetValue(declaration.SymbolNodeId.Value, out var owner)
                ? owner.Kind.Value
                : "?";
            declarationKinds[kind] = declarationKinds.GetValueOrDefault(kind) + 1;
        }

        Console.WriteLine($"nodes: {allNodes.Count} total");
        foreach (var (kind, count) in nodeKinds)
            Console.WriteLine($"  {kind}: {count}");
        Console.WriteLine($"edges: {allEdges.Count} total");
        foreach (var (kind, count) in edgeKinds)
            Console.WriteLine($"  {kind}: {count}");
        Console.WriteLine($"declarations: {declarations.Count}");
        foreach (var (kind, count) in declarationKinds)
            Console.WriteLine($"  declared-{kind}: {count}");

        Json.Write(Path.Combine(outDir, "baseline-diagnostics.json"), new
        {
            status = result.Diagnostics.Status.ToString(),
            publication = new
            {
                repositoryId = result.Publication.RepositoryId.Value,
                indexRunId = result.Publication.IndexRunId.Value,
                snapshotIdentity = result.Publication.SnapshotIdentity,
                isConsistentSnapshot = result.Publication.IsConsistentSnapshot
            },
            counts = new
            {
                filesDiscovered = result.Diagnostics.FilesDiscovered,
                filesNew = result.Diagnostics.FilesNew,
                filesChanged = result.Diagnostics.FilesChanged,
                filesUnchanged = result.Diagnostics.FilesUnchanged,
                filesDeleted = result.Diagnostics.FilesDeleted,
                filesUnsupported = result.Diagnostics.FilesUnsupported,
                directoriesExcluded = result.Diagnostics.DirectoriesExcluded,
                depthLimited = result.Diagnostics.DirectoriesDepthLimited,
                reparsePointsSkipped = result.Diagnostics.ReparsePointsSkipped,
                sourceBytesRead = result.Diagnostics.SourceBytesRead,
                pluginsExecuted = result.Diagnostics.PluginsExecuted,
                indexUnitsCompleted = result.Diagnostics.IndexUnitsCompleted,
                indexUnitsDeleted = result.Diagnostics.IndexUnitsDeleted,
                nodesProduced = result.Diagnostics.NodesProduced,
                declarationsProduced = result.Diagnostics.DeclarationsProduced,
                edgesProduced = result.Diagnostics.EdgesProduced,
                rejectedFacts = result.Diagnostics.RejectedFacts,
                unresolvedRelationships = result.Diagnostics.UnresolvedRelationships
            },
            durations = new
            {
                planningMs = result.Diagnostics.PlanningDuration.TotalMilliseconds,
                extractionMs = result.Diagnostics.ExtractionDuration.TotalMilliseconds,
                persistenceMs = result.Diagnostics.PersistenceDuration.TotalMilliseconds,
                wallMs = wall.Elapsed.TotalMilliseconds
            },
            plugins = result.Diagnostics.Plugins.Select(plugin => new
            {
                pluginId = plugin.PluginId.Value,
                pluginVersion = plugin.PluginVersion,
                status = plugin.Status.ToString(),
                durationMs = plugin.Duration.TotalMilliseconds,
                sourcesSupplied = plugin.SourcesSupplied,
                sourcesExamined = plugin.SourcesExamined,
                sourcesContributingFacts = plugin.SourcesContributingFacts,
                unresolvedRelationships = plugin.UnresolvedRelationships,
                warnings = plugin.WarningCodes
            }),
            providerSelections = result.Diagnostics.ProviderSelections.Select(selection => new
            {
                language = selection.Language,
                preferred = selection.Preferred.Value,
                selected = selection.Selected?.Value,
                failure = selection.FailureKind?.ToString(),
                reason = selection.Reason
            }),
            warnings = result.Diagnostics.WarningCodes,
            aggregate = new
            {
                nodeKinds,
                edgeKinds,
                evidenceByKind,
                declarationKindsByNodeKind = declarationKinds,
                symbolBearingNodes = symbolBearing.Count,
                declarations = declarations.Count
            }
        });

        Json.Write(Path.Combine(outDir, "graph-facts.json"), new
        {
            fileNodeIds,
            nodes = allNodes.Values.OrderBy(node => node.Id.Value, StringComparer.Ordinal).Select(Dto.Node),
            edges = allEdges.Values.OrderBy(edge => edge.Id.Value, StringComparer.Ordinal).Select(Dto.Edge),
            declarations = declarations.Values
                .OrderBy(declaration => declaration.Location.Path, StringComparer.Ordinal)
                .ThenBy(declaration => declaration.Location.StartLine)
                .Select(Dto.Declaration),
            perFile = perFile.ToDictionary(
                pair => pair.Key,
                pair => (object)pair.Value.OrderBy(value => value, StringComparer.Ordinal))
        });

        // ---- Q1..Q8 ----
        var queryResults = new Dictionary<string, object>(StringComparer.Ordinal);
        await Q1(queryResults, queries, repositoryId);
        await Q2(queryResults, queries, repositoryId, allNodes);
        await Q3(queryResults, queries, repositoryId);
        await Q4(queryResults, queries, repositoryId, allEdges, allNodes);
        await Q5(queryResults, queries, repositoryId, allEdges);
        await Q6(queryResults, queries, repositoryId, allNodes);
        await Q7(queryResults, queries, repositoryId);
        await Q8(queryResults, queries, repositoryId);
        Json.Write(Path.Combine(outDir, "developer-queries.json"), queryResults);
        return 0;
    }

    private static async Task Q1(
        Dictionary<string, object> results,
        CodeGraphQueryService queries,
        CodeRepositoryId repositoryId)
    {
        var lookups = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var name in new[]
                 {
                     "withBase", "formatDate", "ProjectSchemaInput", "graph",
                     "getStaticPaths", "Site", "Status"
                 })
        {
            var lookup = await queries.FindSymbolsByQualifiedNameAsync(repositoryId, name);
            lookups[name] = new
            {
                candidateCount = lookup.Candidates.Count,
                isAmbiguous = lookup.IsAmbiguous,
                candidates = lookup.Candidates.Select(Dto.Node)
            };
        }

        results["Q1-symbol-discovery"] = lookups;
    }

    private static async Task Q2(
        Dictionary<string, object> results,
        CodeGraphQueryService queries,
        CodeRepositoryId repositoryId,
        Dictionary<string, CodeGraphNode> allNodes)
    {
        var members = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var interfaceName in new[] { "RuntimeLayer", "FounderProfile", "NavItem" })
        {
            var lookup = await queries.FindSymbolsByQualifiedNameAsync(repositoryId, interfaceName);
            var owner = Ec4Support.Single(lookup.Candidates);
            if (owner is null)
            {
                members[interfaceName] = new { error = "no single candidate" };
                continue;
            }

            var neighborhood = await queries.GetNeighborhoodAsync(
                repositoryId,
                owner.Id,
                [CodeEdgeKinds.Contains],
                new CodeGraphQueryOptions(maxDepth: 1, maxNodes: 200, maxEdges: 500));
            // GetNeighborhoodAsync traverses both directions; member discovery
            // must keep only outgoing containment from the owner itself.
            var outgoingTargets = neighborhood.Edges
                .Where(edge => edge.SourceId == owner.Id)
                .Select(edge => edge.TargetId.Value)
                .ToHashSet(StringComparer.Ordinal);
            members[interfaceName] = new
            {
                owner = Dto.Node(owner),
                members = neighborhood.Nodes
                    .Where(node => outgoingTargets.Contains(node.Id.Value))
                    .OrderBy(node => node.Name, StringComparer.Ordinal)
                    .Select(Dto.Node)
            };
        }

        results["Q2-member-discovery"] = members;
    }

    private static async Task Q3(
        Dictionary<string, object> results,
        CodeGraphQueryService queries,
        CodeRepositoryId repositoryId)
    {
        // Ground truth hand-derived from the source tree at the indexed commit.
        var expected = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["src/lib/schema.ts"] =
            [
                "Site", "abs", "personNode", "websiteNode", "profilePageNode",
                "ProjectSchemaInput", "softwareSourceCodeNode", "BlogPostSchemaInput",
                "blogPostingNode", "BreadcrumbItem", "breadcrumbNode", "graph"
            ],
            ["src/lib/ecosystem.ts"] =
            [
                "Status", "StatusDefinition", "Category", "ProjectRef",
                "RuntimeLayer", "SupportingPrimitive"
            ],
            ["src/pages/robots.txt.ts"] = ["GET"]
        };
        var files = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var (path, expectedSymbols) in expected)
        {
            var declarations = await queries.GetDeclarationsInFileAsync(repositoryId, path);
            var symbols = await queries.GetSymbolsInFileAsync(repositoryId, path);
            var actualTopLevel = symbols
                .Select(node => node.QualifiedName ?? node.Name)
                .ToHashSet(StringComparer.Ordinal);
            var missing = expectedSymbols.Where(name => !actualTopLevel.Contains(name)).ToList();
            var unexpected = actualTopLevel
                .Where(name => name.Contains('.') is false && !expectedSymbols.Contains(name))
                .ToList();
            files[path] = new
            {
                declarationCount = declarations.Count,
                declarations = declarations.Select(declaration => new
                {
                    location = $"{declaration.Location.Path}:{declaration.Location.StartLine}.{declaration.Location.StartColumn}",
                    nodeId = declaration.SymbolNodeId.Value
                }),
                topLevelSymbols = symbols.Select(Dto.Node),
                expectedSymbols,
                missingFromGraph = missing,
                unexpectedInGraph = unexpected
            };
        }

        results["Q3-file-exploration"] = files;
    }

    private static async Task Q4(
        Dictionary<string, object> results,
        CodeGraphQueryService queries,
        CodeRepositoryId repositoryId,
        Dictionary<string, CodeGraphEdge> allEdges,
        Dictionary<string, CodeGraphNode> allNodes)
    {
        var files = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var path in new[]
                 {
                     "src/lib/schema.ts", "src/pages/robots.txt.ts",
                     "src/content.config.ts", "astro.config.mjs"
                 })
        {
            var lookup = await queries.FindSymbolsByQualifiedNameAsync(repositoryId, path);
            var fileNode = lookup.Candidates.FirstOrDefault(node => node.Kind == CodeNodeKinds.File);
            if (fileNode is null)
            {
                files[path] = new { error = "file node not found" };
                continue;
            }

            var neighborhood = await queries.GetNeighborhoodAsync(
                repositoryId,
                fileNode.Id,
                [CodeEdgeKinds.Imports],
                new CodeGraphQueryOptions(maxDepth: 1, maxNodes: 100, maxEdges: 200));
            files[path] = new
            {
                imports = neighborhood.Edges
                    .Select(edge => new
                    {
                        moduleName = allNodes.TryGetValue(edge.TargetId.Value, out var target)
                            ? target.QualifiedName ?? target.Name
                            : edge.TargetId.Value,
                        location = edge.Evidence.Location is null
                            ? null
                            : $"{edge.Evidence.Location.Path}:{edge.Evidence.Location.StartLine}.{edge.Evidence.Location.StartColumn}",
                        evidence = edge.Evidence.Quality.ToString()
                    })
                    .OrderBy(item => item.moduleName, StringComparer.Ordinal)
            };
        }

        results["Q4-dependency-observations"] = files;
    }

    private static async Task Q5(
        Dictionary<string, object> results,
        CodeGraphQueryService queries,
        CodeRepositoryId repositoryId,
        Dictionary<string, CodeGraphEdge> allEdges)
    {
        var inherits = allEdges.Values.Where(edge => edge.Kind == CodeEdgeKinds.Inherits).ToList();
        var implements = allEdges.Values.Where(edge => edge.Kind == CodeEdgeKinds.Implements).ToList();
        results["Q5-inheritance"] = new
        {
            inheritsEdges = inherits.Select(Dto.Edge),
            implementsEdges = implements.Select(Dto.Edge),
            note = inherits.Count == 0 && implements.Count == 0
                ? "No class/interface heritage exists in the supported subset of this repository; the relationship kinds are advertised by the plugins but cannot be exercised here."
                : null
        };
        await Task.CompletedTask;
    }

    private static async Task Q6(
        Dictionary<string, object> results,
        CodeGraphQueryService queries,
        CodeRepositoryId repositoryId,
        Dictionary<string, CodeGraphNode> allNodes)
    {
        var declaredWithBase = Ec4Support.Single(
            (await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "withBase")).Candidates);
        var outgoing = declaredWithBase is null
            ? null
            : await queries.GetNeighborhoodAsync(
                repositoryId,
                declaredWithBase.Id,
                null,
                new CodeGraphQueryOptions(maxDepth: 1, maxNodes: 200, maxEdges: 500));
        var impactOfDeclared = declaredWithBase is null
            ? null
            : await queries.GetImpactSetAsync(repositoryId, declaredWithBase.Id);

        // The declaration-less invocation placeholder collects structural
        // invocations of the written name from every caller.
        var invokedWithBase = allNodes.Values.FirstOrDefault(node =>
            node.Name == "withBase" &&
            node.SymbolId is null &&
            node.Id.Value.Contains(":invoked:", StringComparison.Ordinal));
        var impactOfPlaceholder = invokedWithBase is null
            ? null
            : await queries.GetImpactSetAsync(repositoryId, invokedWithBase.Id);

        var graphOutgoing = Ec4Support.Single(
            (await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "graph")).Candidates) is { } graphNode
            ? await queries.GetNeighborhoodAsync(
                repositoryId, graphNode.Id, null,
                new CodeGraphQueryOptions(maxDepth: 2, maxNodes: 500, maxEdges: 1000))
            : null;

        results["Q6-impact-exploration"] = new
        {
            declaredWithBase = declaredWithBase is null ? null : Dto.Node(declaredWithBase),
            declaredWithBaseNeighborhood = outgoing is null
                ? null
                : new
                {
                    nodes = outgoing.Nodes.Select(Dto.Node),
                    edges = outgoing.Edges.Select(Dto.Edge)
                },
            impactSetOfDeclaredNode = impactOfDeclared is null
                ? null
                : new { nodeCount = impactOfDeclared.Nodes.Count },
            invocationPlaceholder = invokedWithBase is null ? null : Dto.Node(invokedWithBase),
            impactSetOfPlaceholder = impactOfPlaceholder is null
                ? null
                : new
                {
                    nodeCount = impactOfPlaceholder.Nodes.Count,
                    incoming = impactOfPlaceholder.Edges.Select(Dto.Edge)
                },
            graphNodeNeighborhood = graphOutgoing is null
                ? null
                : new
                {
                    nodes = graphOutgoing.Nodes.Select(Dto.Node),
                    edges = graphOutgoing.Edges.Select(Dto.Edge)
                }
        };
    }

    private static async Task Q7(
        Dictionary<string, object> results,
        CodeGraphQueryService queries,
        CodeRepositoryId repositoryId)
    {
        // Positions chosen from the indexed source:
        //   src/lib/format.ts line 2 col 5 is inside formatDate's body.
        //   src/lib/ecosystem.ts line 9 col 5 is inside interface StatusDefinition.
        //   src/lib/paths.ts line 1 col 1 is module level (no enclosing symbol).
        var format = await queries.FindEnclosingSymbolAsync(repositoryId, "src/lib/format.ts", 2, 5);
        var ecosystem = await queries.FindEnclosingSymbolAsync(repositoryId, "src/lib/ecosystem.ts", 9, 5);
        var moduleLevel = await queries.FindEnclosingSymbolAsync(repositoryId, "src/lib/paths.ts", 1, 1);
        results["Q7-source-navigation"] = new
        {
            formatDateBody = format is null
                ? null
                : new { symbol = Dto.Node(format.Symbol), declaration = Dto.Declaration(format.Declaration) },
            statusDefinitionBody = ecosystem is null
                ? null
                : new { symbol = Dto.Node(ecosystem.Symbol), declaration = Dto.Declaration(ecosystem.Declaration) },
            moduleLevel = moduleLevel is null ? "(null: no enclosing symbol, correct)" : Dto.Node(moduleLevel.Symbol)
        };
    }

    private static async Task Q8(
        Dictionary<string, object> results,
        CodeGraphQueryService queries,
        CodeRepositoryId repositoryId)
    {
        // Declared "abs" and the declaration-less invocation placeholder "abs"
        // share one displayed name; exact-name lookup must return only the
        // declared symbol, and pattern search must expose both.
        var exact = await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "abs");
        var pattern = await queries.FindNodesByNamePatternAsync(repositoryId, "abs", 50);
        var exactSite = await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "site");
        var patternSite = await queries.FindNodesByNamePatternAsync(repositoryId, "site", 50);
        results["Q8-ambiguous-names"] = new
        {
            exactLookupAbs = exact.Candidates.Select(Dto.Node),
            patternSearchAbs = pattern.Candidates.Select(Dto.Node),
            patternSearchAbsTotalMatches = pattern.TotalMatches,
            exactLookupSite = exactSite.Candidates.Select(Dto.Node),
            patternSearchSite = patternSite.Candidates.Select(Dto.Node),
            note = "invocation placeholders carry no qualified name, so exact-name lookup returns only declared symbols; pattern search matches both on display name"
        };
    }
}


/// <summary>Ambiguity-safe single-candidate selection shared by the harness.</summary>
internal static class Ec4Support
{
    internal static CodeGraphNode? Single(IReadOnlyList<CodeGraphNode> candidates) =>
        candidates.Count == 1 ? candidates[0] : null;
}

