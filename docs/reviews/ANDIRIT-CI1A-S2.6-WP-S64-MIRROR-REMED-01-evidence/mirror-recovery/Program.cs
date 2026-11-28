using System.Text.Json;
using Andirit;
using Andirit.LatticeDb;
using LatticeDbSharp;

// Read/recover a store produced by the defective S6.4 entry baseline. The
// caller supplies a disposable copy; this tool never writes to its input path
// other than through the normal store recovery transaction.
if (args is not [var destination, "recover" or "detect" or "recover-unrelated"])
{
    throw new ArgumentException(
        "Supply a database path and recover, recover-unrelated, or detect.");
}

var databasePath = Path.GetFullPath(destination);
if (!File.Exists(databasePath))
{
    throw new InvalidOperationException("The supplied database does not exist.");
}

var mode = args[1];
var unrelatedLabel = "UnrelatedNativeSentinel";
if (mode == "recover-unrelated")
{
    using var database = LatticeDatabase.Open(
        databasePath, new LatticeDatabaseOptions { Create = false });
    using var transaction = database.BeginWriteTransaction();
    var sentinel = transaction.CreateNode(unrelatedLabel);
    transaction.SetProperty(sentinel, "note", LatticeValue.From("unrelated-native-data"));
    transaction.Commit();
}

var options = new JsonSerializerOptions { WriteIndented = true };
var before = Census(databasePath, unrelatedLabel);
if (mode == "detect")
{
    Console.WriteLine(JsonSerializer.Serialize(new { mode, before }, options));
    return;
}

var repositories = before.RepositoryIds.Select(id => new CodeRepositoryId(id)).ToArray();
var projectionTraversal = new SortedDictionary<string, string[]>(StringComparer.Ordinal);
using (var disabled = new LatticeCodeGraphStore(databasePath))
{
    foreach (var repository in repositories)
    {
        foreach (var node in await disabled.FindNodesByQualifiedNameAsync(
                     repository, "Shared/Linked.cs"))
        {
            projectionTraversal[node.Id.Value] =
                (await disabled.TraverseAsync(
                    repository,
                    new CodeGraphTraversalQuery(
                        node.Id, CodeGraphDirection.Incoming,
                        maxDepth: 1, maxNodes: 100, maxEdges: 100)))
                .Edges.Select(edge => edge.Id.Value).ToArray();
        }
    }
}

var nativeTraversal = new SortedDictionary<string, string[]>(StringComparer.Ordinal);
using (var enabled = new LatticeCodeGraphStore(
           databasePath, new LatticeDbStoreOptions { UseNativeTraversal = true }))
{
    foreach (var repository in repositories)
    {
        foreach (var node in await enabled.FindNodesByQualifiedNameAsync(
                     repository, "Shared/Linked.cs"))
        {
            nativeTraversal[node.Id.Value] =
                (await enabled.TraverseAsync(
                    repository,
                    new CodeGraphTraversalQuery(
                        node.Id, CodeGraphDirection.Incoming,
                        maxDepth: 1, maxNodes: 100, maxEdges: 100)))
                .Edges.Select(edge => edge.Id.Value).ToArray();
        }
    }
}

var after = Census(databasePath, unrelatedLabel);
Validate(before, after, projectionTraversal, nativeTraversal);

Console.WriteLine(JsonSerializer.Serialize(new
{
    mode,
    before,
    after,
    projectionTraversal,
    nativeTraversal,
    recovered = true
}, options));

static void Validate(
    Census before,
    Census after,
    IReadOnlyDictionary<string, string[]> projectionTraversal,
    IReadOnlyDictionary<string, string[]> nativeTraversal)
{
    Require(after.ManifestCount == after.RepositoryIds.Length,
        "one manifest per repository after recovery");
    Require(after.DistinctNodeCount == after.NodeCount,
        "no duplicate logical node identities after recovery");
    Require(after.ManifestNodeEntries == after.NodeCount,
        "one manifest node entry per physical node");
    Require(after.ManifestEdgeEntries == after.EdgeCount,
        "one manifest edge entry per physical edge");
    Require(after.UnmappedNativeNodeIds == 0 &&
            after.UnmappedNativeEdgeIds == 0,
        "every physical mirror row is owned by a manifest");
    Require(after.NodeCount == after.DistinctNodeCount,
        "canonical node cardinality");
    Require(after.RepositoryIds.SequenceEqual(before.RepositoryIds),
        "repository identity preserved");
    Require(after.RunCount == before.RunCount &&
            after.UnitCount == before.UnitCount &&
            after.IndexStateCount == before.IndexStateCount,
        "authoritative command-log rows preserved");
    Require(after.OccurrenceFacts == before.OccurrenceFacts,
        "occurrence facts preserved");
    Require(after.UnrelatedNodes == before.UnrelatedNodes,
        "unrelated native data preserved");
    Require(projectionTraversal.Count == nativeTraversal.Count &&
            projectionTraversal.All(pair =>
                nativeTraversal.TryGetValue(pair.Key, out var native) &&
                pair.Value.SequenceEqual(native)),
        "public projection and native traversal agree after recovery");
}

static void Require(bool condition, string description)
{
    if (!condition)
    {
        throw new InvalidOperationException("Recovery invariant failed: " + description);
    }
}

Census Census(string path, string unrelatedLabel)
{
    using var database = LatticeDatabase.Open(
        path, new LatticeDatabaseOptions { Create = false });
    using var transaction = database.BeginReadTransaction();
    var mirrorNodes = transaction.GetNodesByLabel("HetuGraphNode");
    var nodeIds = new List<string>();
    var nativeNodeIds = new HashSet<ulong>();
    foreach (var node in mirrorNodes)
    {
        if (!transaction.TryGetProperty(node, "hetu_json", out var payload))
            continue;
        using var document = JsonDocument.Parse(payload.AsString());
        nodeIds.Add(document.RootElement
            .GetProperty("Id").GetProperty("Value").GetString()!);
        nativeNodeIds.Add(node.Value);
    }

    var edgeIds = new HashSet<ulong>();
    foreach (var node in mirrorNodes)
    {
        foreach (var edge in transaction.GetOutgoingEdges(node))
        {
            if (transaction.TryGetEdgeProperty(edge.Id, "hetu_json", out _))
                edgeIds.Add(edge.Id.Value);
        }
    }

    var manifestNodeValues = new List<ulong>();
    var manifestEdgeValues = new List<ulong>();
    var repositoryIds = new List<string>();
    var manifests = 0;
    foreach (var node in transaction.GetNodesByLabel("HetuMirror"))
    {
        if (!transaction.TryGetProperty(node, "key", out var key) ||
            !transaction.TryGetProperty(node, "payload", out var payload))
            continue;
        manifests++;
        repositoryIds.Add(key.AsString());
        var json = JsonSerializer.Deserialize<string>(
            Convert.FromBase64String(payload.AsString()))!;
        using var document = JsonDocument.Parse(json);
        foreach (var entry in document.RootElement.GetProperty("Nodes").EnumerateObject())
            manifestNodeValues.Add(entry.Value.GetUInt64());
        foreach (var entry in document.RootElement.GetProperty("Edges").EnumerateObject())
            manifestEdgeValues.Add(entry.Value.GetUInt64());
    }

    // Repository payloads are base64-encoded command records; read the id from
    // the record's own shape instead of relying on an in-memory store instance.
    repositoryIds.Clear();
    using (var query = database.Prepare(
               "MATCH (s:HetuRepository) RETURN s.payload"))
    using (var result = query.Execute(transaction))
    {
        foreach (var row in result.ReadAll())
        {
            using var document = JsonDocument.Parse(
                Convert.FromBase64String(row.GetValue(0).AsString()));
            repositoryIds.Add(document.RootElement
                .GetProperty("Id").GetProperty("Value").GetString()!);
        }
    }

    using var occurrenceQuery = database.Prepare(
        "MATCH (n:AndiritOccurrenceUnit) RETURN n.payload");
    using var occurrenceResult = occurrenceQuery.Execute(transaction);
    var occurrenceFacts = 0;
    foreach (var row in occurrenceResult.ReadAll())
    {
        using var document = JsonDocument.Parse(
            Convert.FromBase64String(row.GetValue(0).AsString()));
        occurrenceFacts += document.RootElement.GetProperty("Facts").GetArrayLength();
    }

    var census = new Census(
        NodeCount: nodeIds.Count,
        DistinctNodeCount: nodeIds.Distinct(StringComparer.Ordinal).Count(),
        LogicalNodeIds: nodeIds.OrderBy(id => id, StringComparer.Ordinal).ToArray(),
        EdgeCount: edgeIds.Count,
        ManifestCount: manifests,
        ManifestNodeEntries: manifestNodeValues.Count,
        ManifestEdgeEntries: manifestEdgeValues.Count,
        UnmappedNativeNodeIds:
            nativeNodeIds.Count(id => !manifestNodeValues.Contains(id)),
        UnmappedNativeEdgeIds:
            edgeIds.Count(id => !manifestEdgeValues.Contains(id)),
        RepositoryIds: repositoryIds.OrderBy(id => id, StringComparer.Ordinal).ToArray(),
        RunCount: Count("HetuRun"),
        UnitCount: Count("HetuUnit"),
        IndexStateCount: Count("HetuIndexState"),
        OccurrenceFacts: occurrenceFacts,
        UnrelatedNodes: transaction.GetNodesByLabel(unrelatedLabel).Count);
    transaction.Commit();
    return census;

    int Count(string label) => transaction.GetNodesByLabel(label).Count;
}



internal sealed record Census(
    int NodeCount,
    int DistinctNodeCount,
    string[] LogicalNodeIds,
    int EdgeCount,
    int ManifestCount,
    int ManifestNodeEntries,
    int ManifestEdgeEntries,
    int UnmappedNativeNodeIds,
    int UnmappedNativeEdgeIds,
    string[] RepositoryIds,
    int RunCount,
    int UnitCount,
    int IndexStateCount,
    int OccurrenceFacts,
    int UnrelatedNodes);
