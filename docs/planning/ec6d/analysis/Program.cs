using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using Penghou.Hetu;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: ConsumerBaseline <fixture-repository-path>");
    return 2;
}

var corpusPath = Path.GetFullPath(args[0]);
var repositoryId = new CodeRepositoryId("repo:ec6d-ts-interface-impact-fixture");
var runId = new CodeIndexRunId("run:ec6d-baseline-01");
var store = new InMemoryCodeGraphStore();
var indexer = new CodeIndexingService(
    new CodeRepositoryProviderRegistry([new FileSystemCodeRepositoryProvider()]),
    new CodeGraphPluginRegistry([new TreeSitterTypeScriptPlugin()]),
    store);

var indexed = await indexer.IndexAsync(
    new CodeRepositoryDescriptor(repositoryId, corpusPath),
    runId);
var snapshot = await CodePublicationSnapshot.ExportAsync(store, repositoryId);
var units = snapshot.Units;
var nodeContributions = units.SelectMany(unit => unit.Nodes).ToArray();
var declarationContributions = units.SelectMany(unit => unit.Declarations).ToArray();
var edgeContributions = units.SelectMany(unit => unit.Edges).ToArray();
var nodes = nodeContributions.GroupBy(node => node.Id.Value, StringComparer.Ordinal)
    .Select(group => group.First()).OrderBy(node => node.Id.Value, StringComparer.Ordinal).ToArray();
var declarations = declarationContributions.GroupBy(declaration => declaration.Id.Value, StringComparer.Ordinal)
    .Select(group => group.First()).OrderBy(declaration => declaration.Location.Path, StringComparer.Ordinal)
    .ThenBy(declaration => declaration.Location.StartLine).ToArray();
var edges = edgeContributions.GroupBy(edge => edge.Id.Value, StringComparer.Ordinal)
    .Select(group => group.First()).OrderBy(edge => edge.Kind.Value, StringComparer.Ordinal)
    .ThenBy(edge => edge.SourceId.Value, StringComparer.Ordinal).ThenBy(edge => edge.TargetId.Value, StringComparer.Ordinal)
    .ThenBy(edge => edge.Id.Value, StringComparer.Ordinal).ToArray();
var nodeById = nodes.ToDictionary(node => node.Id.Value, StringComparer.Ordinal);
var queries = new CodeGraphQueryService(store);
var nodeOrigins = units.SelectMany(unit => unit.Nodes.Select(node => new
    {
        Id = node.Id.Value,
        Origin = OriginView(unit.Origin)
    }))
    .GroupBy(item => item.Id, StringComparer.Ordinal)
    .ToDictionary(group => group.Key, group => group.Select(item => item.Origin).ToArray(), StringComparer.Ordinal);
var canonicalFacts = new
{
    Nodes = nodes.Select(node => new
    {
        Id = node.Id.Value,
        Kind = node.Kind.Value,
        node.Name,
        node.QualifiedName,
        SymbolId = node.SymbolId?.Value,
        Properties = node.Properties.ToDictionary(pair => pair.Key, pair => PropertyValue(pair.Value), StringComparer.Ordinal)
    }),
    Declarations = declarations.Select(declaration => new
    {
        Id = declaration.Id.Value,
        SymbolId = declaration.SymbolId.Value,
        SymbolNodeId = declaration.SymbolNodeId.Value,
        declaration.Location.Path,
        declaration.Location.StartLine,
        declaration.Location.StartColumn,
        declaration.Location.EndLine,
        declaration.Location.EndColumn,
        Properties = declaration.Properties.ToDictionary(pair => pair.Key, pair => PropertyValue(pair.Value), StringComparer.Ordinal)
    }),
    Edges = edges.Select(edge => new
    {
        Id = edge.Id.Value,
        SourceId = edge.SourceId.Value,
        TargetId = edge.TargetId.Value,
        Kind = edge.Kind.Value,
        Quality = edge.Evidence.Quality.ToString(),
        edge.Evidence.Provider,
        edge.Evidence.ProviderVersion,
        LocationPath = edge.Evidence.Location?.Path,
        StartLine = edge.Evidence.Location?.StartLine,
        StartColumn = edge.Evidence.Location?.StartColumn,
        EndLine = edge.Evidence.Location?.EndLine,
        EndColumn = edge.Evidence.Location?.EndColumn,
        Properties = edge.Properties.ToDictionary(pair => pair.Key, pair => PropertyValue(pair.Value), StringComparer.Ordinal)
    })
};
var canonicalFactsJson = JsonSerializer.Serialize(canonicalFacts);
var canonicalFactsSha256 = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalFactsJson)));
var edgeOrigins = units.SelectMany(unit => unit.Edges.Select(edge => new
    {
        Id = edge.Id.Value,
        Origin = OriginView(unit.Origin)
    }))
    .GroupBy(item => item.Id, StringComparer.Ordinal)
    .ToDictionary(group => group.Key, group => group.Select(item => item.Origin).ToArray(), StringComparer.Ordinal);

var targetDeclarations = declarations
    .Where(declaration => declaration.Location.Path == "src/contracts/Renderable.ts")
    .ToArray();
var selectedTargetNodeIds = targetDeclarations
    .Select(declaration => declaration.SymbolNodeId.Value)
    .Distinct(StringComparer.Ordinal)
    .ToArray();
var selectedTargetNodes = selectedTargetNodeIds
    .Select(id => nodeById[id])
    .Where(node => node.Kind == CodeNodeKinds.Interface && node.Name == "Renderable")
    .OrderBy(node => node.Id.Value, StringComparer.Ordinal)
    .ToArray();
var exactName = await queries.FindSymbolsByQualifiedNameWithProvenanceAsync(repositoryId, "Renderable");
var queryRuns = new List<object>();
var selectedQueries = new List<object>();
var selectedFileNode = nodes.Single(node =>
    node.Kind == CodeNodeKinds.File && node.QualifiedName == "src/contracts/Renderable.ts");
var moduleImporterQuery = await queries.GetNeighborhoodWithProvenanceAsync(
    repositoryId,
    selectedFileNode.Id,
    [CodeEdgeKinds.ResolvesTo],
    new CodeGraphQueryOptions(maxDepth: 10, maxNodes: 100, maxEdges: 200));
var exportSurfaceQuery = await queries.GetNeighborhoodWithProvenanceAsync(
    repositoryId,
    selectedFileNode.Id,
    [CodeEdgeKinds.Exports],
    new CodeGraphQueryOptions(maxDepth: 2, maxNodes: 100, maxEdges: 200));

foreach (var node in selectedTargetNodes)
{
    var implementationResult = await queries.FindImplementationsWithProvenanceAsync(repositoryId, node.Id);
    var impactResult = await queries.GetImpactSetWithProvenanceAsync(repositoryId, node.Id);
    selectedQueries.Add(new
    {
        Seed = NodeView(node),
        Implementations = implementationResult is null ? null : TraversalView(implementationResult.Result),
        ImplementationProvenance = implementationResult is null ? null : ProvenanceView(implementationResult.Provenance),
        Impact = impactResult is null ? null : TraversalView(impactResult.Result),
        ImpactProvenance = impactResult is null ? null : ProvenanceView(impactResult.Provenance)
    });
}

if (exactName is not null)
{
    foreach (var candidate in exactName.Result.Candidates.OrderBy(node => node.Id.Value, StringComparer.Ordinal))
    {
        var implementationResult = await queries.FindImplementationsWithProvenanceAsync(repositoryId, candidate.Id);
        var impactResult = await queries.GetImpactSetWithProvenanceAsync(repositoryId, candidate.Id);
        queryRuns.Add(new
        {
            Seed = NodeView(candidate),
            Implementations = implementationResult is null ? null : TraversalView(implementationResult.Result),
            ImplementationProvenance = implementationResult is null ? null : ProvenanceView(implementationResult.Provenance),
            Impact = impactResult is null ? null : TraversalView(impactResult.Result),
            ImpactProvenance = impactResult is null ? null : ProvenanceView(impactResult.Provenance)
        });
    }
}

var report = new
{
    Corpus = Path.GetRelativePath(Environment.CurrentDirectory, corpusPath).Replace('\\', '/'),
    RepositoryId = repositoryId.Value,
    RunId = runId.Value,
    IndexIdentity = indexed.Publication.IndexIdentity.Value,
    PublicationConsistent = indexed.Publication.IsConsistentSnapshot,
    SnapshotIntegrityHash = snapshot.IntegrityHash,
    CanonicalFactsSha256 = canonicalFactsSha256,
    Diagnostics = new
    {
        indexed.Diagnostics.Status,
        indexed.Diagnostics.FilesDiscovered,
        indexed.Diagnostics.FilesUnsupported,
        indexed.Diagnostics.IndexUnitsCompleted,
        indexed.Diagnostics.NodesProduced,
        indexed.Diagnostics.DeclarationsProduced,
        indexed.Diagnostics.EdgesProduced,
        indexed.Diagnostics.UnresolvedRelationships,
        indexed.Diagnostics.WarningCodes,
        Plugins = indexed.Diagnostics.Plugins.Select(plugin => new
        {
            Plugin = plugin.PluginId.Value,
            plugin.PluginVersion,
            plugin.Status,
            plugin.SourcesExamined,
            plugin.SourcesContributingFacts,
            plugin.UnresolvedRelationships,
            plugin.WarningCodes
        })
    },
    Counts = new
    {
        Units = units.Count,
        NodeContributions = nodeContributions.Length,
        DeclarationContributions = declarationContributions.Length,
        EdgeContributions = edgeContributions.Length,
        Nodes = nodes.Length,
        Declarations = declarations.Length,
        Edges = edges.Length,
        ByEdgeKind = edges.GroupBy(edge => edge.Kind.Value).OrderBy(group => group.Key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal)
    },
    SelectedDeclaration = selectedTargetNodes.Select(NodeView),
    ExactRenderableLookup = exactName is null ? null : new
    {
        CandidateCount = exactName.Result.Candidates.Count,
        Candidates = exactName.Result.Candidates.Select(NodeView),
        Provenance = ProvenanceView(exactName.Provenance)
    },
    DirectQueriesForSelectedDeclaration = selectedQueries,
    SelectedFileQueries = new
    {
        File = NodeView(selectedFileNode),
        ModuleImporters = moduleImporterQuery is null ? null : new
        {
            Result = TraversalView(moduleImporterQuery.Result),
            Provenance = ProvenanceView(moduleImporterQuery.Provenance),
            DirectImporters = moduleImporterQuery.Result.Edges
                .Where(edge => edge.Kind == CodeEdgeKinds.ResolvesTo && edge.TargetId == selectedFileNode.Id)
                .Select(edge => new
                {
                    Source = NodeView(nodeById[edge.SourceId.Value]),
                    Specifier = edge.Properties.TryGetValue(CodePropertyKeys.Specifier, out var specifier)
                        ? PropertyValue(specifier)
                        : null,
                    ResolutionRule = edge.Properties.TryGetValue(CodePropertyKeys.ResolutionRule, out var rule)
                        ? PropertyValue(rule)
                        : null,
                    Location = edge.Evidence.Location is null ? null : new
                    {
                        edge.Evidence.Location.Path,
                        edge.Evidence.Location.StartLine,
                        edge.Evidence.Location.StartColumn,
                        edge.Evidence.Location.EndLine,
                        edge.Evidence.Location.EndColumn
                    }
                })
        },
        ExportEntries = exportSurfaceQuery is null ? null : new
        {
            Result = TraversalView(exportSurfaceQuery.Result),
            Provenance = ProvenanceView(exportSurfaceQuery.Provenance),
            DirectExports = exportSurfaceQuery.Result.Edges
                .Where(edge => edge.Kind == CodeEdgeKinds.Exports && edge.SourceId == selectedFileNode.Id)
                .Select(edge => new
                {
                    Entry = NodeView(nodeById[edge.TargetId.Value]),
                    Location = edge.Evidence.Location is null ? null : new
                    {
                        edge.Evidence.Location.Path,
                        edge.Evidence.Location.StartLine,
                        edge.Evidence.Location.StartColumn,
                        edge.Evidence.Location.EndLine,
                        edge.Evidence.Location.EndColumn
                    }
                })
        }
    },
    CandidateQueriesForExactName = queryRuns,
    Nodes = nodes.Select(node => new
    {
        Node = NodeView(node),
        Contributors = nodeOrigins[node.Id.Value],
        Declarations = declarations.Where(declaration => declaration.SymbolNodeId.Value == node.Id.Value)
            .Select(declaration => new
            {
                declaration.Id.Value,
                declaration.Location.Path,
                declaration.Location.StartLine,
                declaration.Location.StartColumn,
                declaration.Location.EndLine,
                declaration.Location.EndColumn
            })
    }),
    Relationships = edges.Select(edge => new
    {
        Contributors = edgeOrigins[edge.Id.Value],
        edge.Id.Value,
        Kind = edge.Kind.Value,
        Source = NodeView(nodeById[edge.SourceId.Value]),
        Target = NodeView(nodeById[edge.TargetId.Value]),
        Evidence = new
        {
            Quality = edge.Evidence.Quality.ToString(),
            edge.Evidence.Provider,
            edge.Evidence.ProviderVersion,
            Location = edge.Evidence.Location is null ? null : new
            {
                edge.Evidence.Location.Path,
                edge.Evidence.Location.StartLine,
                edge.Evidence.Location.StartColumn,
                edge.Evidence.Location.EndLine,
                edge.Evidence.Location.EndColumn
            }
        },
        Properties = edge.Properties.ToDictionary(pair => pair.Key, pair => PropertyValue(pair.Value), StringComparer.Ordinal)
    })
};

Console.WriteLine(JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
return 0;

static object NodeView(CodeGraphNode node) => new
{
    node.Id.Value,
    Kind = node.Kind.Value,
    node.Name,
    node.QualifiedName,
    SymbolId = node.SymbolId?.Value
};

static object OriginView(CodeFactOrigin origin) => new
{
    Plugin = origin.PluginId.Value,
    origin.PluginVersion,
    IndexUnit = origin.IndexUnitId.Value,
    origin.SourcePath,
    origin.SourceHash
};

static object ProvenanceView(IEnumerable<CodeGraphFactProvenance> provenance) => provenance.Select(item => new
{
    Kind = item.Kind.ToString(),
    item.FactId,
    Contributors = item.Contributors.Select(OriginView)
});

static object TraversalView(CodeGraphTraversalResult result) => new
{
    result.Truncated,
    TruncationReason = result.TruncationReason.ToString(),
    result.DepthReached,
    result.NodesExamined,
    result.EdgesExamined,
    result.OmittedFrontierCount,
    Nodes = result.Nodes.Select(NodeView),
    Edges = result.Edges.Select(edge => new
    {
        Id = edge.Id.Value,
        Kind = edge.Kind.Value,
        SourceId = edge.SourceId.Value,
        TargetId = edge.TargetId.Value,
        Quality = edge.Evidence.Quality.ToString(),
        edge.Evidence.Provider,
        edge.Evidence.ProviderVersion,
        Location = edge.Evidence.Location is null ? null : new
        {
            edge.Evidence.Location.Path,
            edge.Evidence.Location.StartLine,
            edge.Evidence.Location.StartColumn,
            edge.Evidence.Location.EndLine,
            edge.Evidence.Location.EndColumn
        }
    })
};

static object? PropertyValue(CodePropertyValue value) => value switch
{
    CodeTextProperty text => text.Value,
    CodeIntegerProperty integer => integer.Value,
    CodeNumberProperty number => number.Value,
    CodeBooleanProperty boolean => boolean.Value,
    CodeTextListProperty list => list.Values,
    _ => value.GetType().Name
};
