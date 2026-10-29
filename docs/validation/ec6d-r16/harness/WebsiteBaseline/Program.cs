using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Andirit;

// EC6D-R16 Workstream B harness: read-only indexing and query dump for the
// pinned website snapshot. Non-production evidence tooling; no Andirit source
// or website source is modified. Output is JSON on stdout.

if (args.Length < 1)
{
    Console.Error.WriteLine("Usage: WebsiteBaseline <repository-path> [repository-id] [run-id]");
    return 2;
}

var sourcePath = Path.GetFullPath(args[0]);
if (!Directory.Exists(sourcePath))
{
    Console.Error.WriteLine($"Repository path does not exist: {sourcePath}");
    return 2;
}

var repositoryId = new CodeRepositoryId(args.Length > 1 ? args[1] : "repo:ec6d-r16-website-pinned");
var runId = new CodeIndexRunId(args.Length > 2 ? args[2] : "run:ec6d-r16-website-01");

var typeScriptPlugin = new TreeSitterTypeScriptPlugin();
var javaScriptPlugin = new TreeSitterJavaScriptPlugin();
var pluginExtensions = typeScriptPlugin.FileExtensions
    .Concat(javaScriptPlugin.FileExtensions)
    .ToHashSet(StringComparer.OrdinalIgnoreCase);

var store = new InMemoryCodeGraphStore();
var indexer = new CodeIndexingService(
    new CodeRepositoryProviderRegistry([new FileSystemCodeRepositoryProvider()]),
    new CodeGraphPluginRegistry([typeScriptPlugin, javaScriptPlugin]),
    store);

var indexed = await indexer.IndexAsync(
    new CodeRepositoryDescriptor(repositoryId, sourcePath),
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

// Workspace file classification mirrors the default enumeration exclusions
// (CodeRepositoryEnumerationOptions.DefaultExclusions) so the unsupported
// inventory is visible next to the provider's own discovery counts.
var excludedDirectories = new HashSet<string>(
    [".git", "bin", "obj", "node_modules", "dist", "packages", "vendor"],
    StringComparer.OrdinalIgnoreCase);
var workspaceFiles = new List<string>();
void WalkDirectory(string directory, string relative)
{
    foreach (var file in Directory.EnumerateFiles(directory))
    {
        var name = Path.GetFileName(file);
        workspaceFiles.Add(relative.Length == 0 ? name : $"{relative}/{name}");
    }

    foreach (var child in Directory.EnumerateDirectories(directory))
    {
        var childName = Path.GetFileName(child);
        if (excludedDirectories.Contains(childName))
            continue;
        WalkDirectory(child, relative.Length == 0 ? childName : $"{relative}/{childName}");
    }
}

WalkDirectory(sourcePath, string.Empty);
workspaceFiles.Sort(StringComparer.Ordinal);

// Exact-name lookups for the declarations that the manually reviewed
// consumer questions touch; implementation and impact queries run for every
// returned candidate so current behavior is visible without treating any
// name-matched placeholder as a binding.
var selectedNames = new[]
{
    "StatusDefinition",
    "ProjectRef",
    "SupportingPrimitive",
    "RuntimeLayer",
    "Category",
    "ProjectSchemaInput",
    "BlogPostSchemaInput",
    "BreadcrumbItem",
    "NavItem",
    "FounderProfile",
    "Status",
    "Site",
    "withBase",
    "formatDate"
};
var exactNameQueries = new List<object>();
foreach (var name in selectedNames)
{
    var lookup = await queries.FindSymbolsByQualifiedNameWithProvenanceAsync(repositoryId, name);
    var candidateRuns = new List<object>();
    if (lookup is not null)
    {
        foreach (var candidate in lookup.Result.Candidates.OrderBy(node => node.Id.Value, StringComparer.Ordinal))
        {
            var implementationResult = await queries.FindImplementationsWithProvenanceAsync(repositoryId, candidate.Id);
            var impactResult = await queries.GetImpactSetWithProvenanceAsync(repositoryId, candidate.Id);
            candidateRuns.Add(new
            {
                Seed = NodeView(candidate),
                Implementations = implementationResult is null ? null : TraversalView(implementationResult.Result),
                Impact = impactResult is null ? null : TraversalView(impactResult.Result)
            });
        }
    }

    exactNameQueries.Add(new
    {
        Name = name,
        Found = lookup is not null,
        CandidateCount = lookup?.Result.Candidates.Count ?? 0,
        Candidates = lookup?.Result.Candidates.Select(NodeView) ?? [],
        CandidateRuns = candidateRuns
    });
}

// Neighborhood evidence for the files that host the consumer scenarios.
var neighborhoodPaths = new[]
{
    "src/lib/ecosystem.ts",
    "src/lib/paths.ts",
    "src/lib/schema.ts",
    "src/lib/site.ts",
    "src/pages/robots.txt.ts"
};
var fileQueries = new List<object>();
foreach (var path in neighborhoodPaths)
{
    var fileNode = nodes.FirstOrDefault(node =>
        node.Kind == CodeNodeKinds.File && node.QualifiedName == path);
    if (fileNode is null)
    {
        fileQueries.Add(new { Path = path, Found = false });
        continue;
    }

    var neighborhood = await queries.GetNeighborhoodWithProvenanceAsync(
        repositoryId,
        fileNode.Id,
        [CodeEdgeKinds.Imports, CodeEdgeKinds.ResolvesTo, CodeEdgeKinds.Exports, CodeEdgeKinds.Implements, CodeEdgeKinds.Inherits],
        new CodeGraphQueryOptions(maxDepth: 4, maxNodes: 200, maxEdges: 400));
    fileQueries.Add(new
    {
        Path = path,
        Found = true,
        File = NodeView(fileNode),
        Declarations = declarations
            .Where(declaration => declaration.Location.Path == path)
            .Select(declaration => new
            {
                declaration.Id.Value,
                Kind = nodeById[declaration.SymbolNodeId.Value].Kind.Value,
                Name = nodeById[declaration.SymbolNodeId.Value].Name,
                declaration.Location.StartLine,
                declaration.Location.StartColumn,
                declaration.Location.EndLine,
                declaration.Location.EndColumn
            }),
        Neighborhood = neighborhood is null ? null : new
        {
            Result = TraversalView(neighborhood.Result),
            ProvenanceCount = neighborhood.Provenance.Count()
        }
    });
}

var report = new
{
    RepositoryPath = sourcePath,
    RepositoryId = repositoryId.Value,
    RunId = runId.Value,
    IndexIdentity = indexed.Publication.IndexIdentity.Value,
    PublicationConsistent = indexed.Publication.IsConsistentSnapshot,
    SnapshotIdentity = indexed.Publication.SnapshotIdentity,
    SnapshotIntegrityHash = snapshot.IntegrityHash,
    CanonicalFactsSha256 = canonicalFactsSha256,
    PluginVersions = new[]
    {
        new { Plugin = typeScriptPlugin.Id.Value, typeScriptPlugin.Version, Extensions = typeScriptPlugin.FileExtensions },
        new { Plugin = javaScriptPlugin.Id.Value, javaScriptPlugin.Version, Extensions = javaScriptPlugin.FileExtensions }
    },
    Diagnostics = new
    {
        indexed.Diagnostics.Status,
        indexed.Diagnostics.FilesDiscovered,
        indexed.Diagnostics.FilesNew,
        indexed.Diagnostics.FilesChanged,
        indexed.Diagnostics.FilesUnchanged,
        indexed.Diagnostics.FilesDeleted,
        indexed.Diagnostics.FilesUnsupported,
        indexed.Diagnostics.DirectoriesExcluded,
        indexed.Diagnostics.IndexUnitsCompleted,
        indexed.Diagnostics.NodesProduced,
        indexed.Diagnostics.DeclarationsProduced,
        indexed.Diagnostics.EdgesProduced,
        indexed.Diagnostics.RejectedFacts,
        indexed.Diagnostics.UnresolvedRelationships,
        indexed.Diagnostics.WarningCodes,
        Plugins = indexed.Diagnostics.Plugins.Select(plugin => new
        {
            Plugin = plugin.PluginId.Value,
            plugin.PluginVersion,
            plugin.Status,
            plugin.SourcesSupplied,
            plugin.SourcesExamined,
            plugin.SourcesContributingFacts,
            plugin.UnresolvedRelationships,
            plugin.WarningCodes
        })
    },
    Files = new
    {
        WorkspaceFileCount = workspaceFiles.Count,
        Supported = workspaceFiles
            .Where(file => pluginExtensions.Contains(Path.GetExtension(file)))
            .ToArray(),
        Unsupported = workspaceFiles
            .Where(file => !pluginExtensions.Contains(Path.GetExtension(file)))
            .ToArray(),
        IndexUnits = units.Select(unit => unit.Origin.SourcePath).OrderBy(path => path, StringComparer.Ordinal).ToArray()
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
        ByNodeKind = nodes.GroupBy(node => node.Kind.Value).OrderBy(group => group.Key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal),
        ByEdgeKind = edges.GroupBy(edge => edge.Kind.Value).OrderBy(group => group.Key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal)
    },
    DeclarationsByFile = declarations
        .GroupBy(declaration => declaration.Location.Path, StringComparer.Ordinal)
        .OrderBy(group => group.Key, StringComparer.Ordinal)
        .ToDictionary(
            group => group.Key,
            group => group.Select(declaration => new
            {
                Name = nodeById[declaration.SymbolNodeId.Value].Name,
                Kind = nodeById[declaration.SymbolNodeId.Value].Kind.Value,
                declaration.Location.StartLine,
                declaration.Location.EndLine
            }),
            StringComparer.Ordinal),
    ExactNameQueries = exactNameQueries,
    FileQueries = fileQueries,
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
