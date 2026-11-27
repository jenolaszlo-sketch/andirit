using System.Text;
using System.Text.Json;
using Andirit.LatticeDb;
using LatticeDbSharp;

namespace Andirit.LatticeDb.Tests;

/// <summary>
/// S64-MIRROR-REMED-01 ownership-safety controls. A structurally valid manifest
/// is not proof of ownership: these cases inject manifests and native rows that
/// a naive cleanup could misattribute, and assert that unrelated or
/// authoritative objects are preserved (or that the rebuild refuses explicitly).
/// </summary>
public sealed class NativeGraphMirrorOwnershipTests
{
    private const string NodeLabel = "HetuGraphNode";
    private const string MirrorLabel = "HetuMirror";
    private const string UnrelatedLabel = "UnrelatedNative";
    private const string JsonProperty = "hetu_json";

    private static readonly DateTimeOffset Started =
        new(2026, 11, 27, 12, 0, 0, TimeSpan.Zero);
    private static readonly CodePluginId Plugin = new("plugin:s64-ownership");

    [Fact]
    public async Task ManifestReferencingUnrelatedNode_DoesNotDeleteIt()
    {
        var path = TemporaryDatabasePath();
        var repository = new CodeRepositoryId("repo:unrelated-node");
        try
        {
            using (var store = Open(path))
            {
                await PublishAsync(store, repository, "run:one", [Node("node:a")]);
            }

            var unrelated = CreateUnrelatedNode(path, "unrelated-node", 42);
            var mirror = ReadMirrorMap(path).Nodes["node:a"];
            WriteManifest(path, repository.Value,
                new() { ["node:a"] = unrelated.NativeId },
                []);

            using (var store = Open(path))
            {
                await Traverse(store, repository, new CodeNodeId("node:a"));
            }

            AssertUnrelatedNodePreserved(path, unrelated);
            AssertCanonicalMirror(path, repository, ["node:a"], []);
            Assert.NotEqual(unrelated.NativeId, ReadMirrorMap(path).Nodes["node:a"]);
            Assert.NotEqual(0UL, mirror);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Fact]
    public async Task ManifestReferencingAuthoritativeCommandLogRecord_DoesNotDeleteIt()
    {
        var path = TemporaryDatabasePath();
        var repository = new CodeRepositoryId("repo:authoritative-node");
        try
        {
            using (var store = Open(path))
            {
                await PublishAsync(store, repository, "run:one", [Node("node:a")]);
            }

            var before = ReadAuthoritativeRows(path);
            Assert.NotEmpty(before);
            var unit = Assert.Single(before, row => row.Label == "HetuUnit");
            WriteManifest(path, repository.Value,
                new() { ["node:a"] = unit.NativeId },
                []);

            using (var store = Open(path))
            {
                await Traverse(store, repository, new CodeNodeId("node:a"));
            }

            AssertAuthoritativeRowsUnchanged(path, before);
            AssertCanonicalMirror(path, repository, ["node:a"], []);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Fact]
    public async Task ManifestReferencingUnrelatedEdge_DoesNotDeleteIt()
    {
        var path = TemporaryDatabasePath();
        var repository = new CodeRepositoryId("repo:unrelated-edge");
        var source = Node("node:source");
        var target = Node("node:target");
        var edge = Edge("edge:calls", source, target);
        try
        {
            using (var store = Open(path))
            {
                await PublishAsync(store, repository, "run:one",
                    [source, target], [edge]);
            }

            var unrelated = CreateUnrelatedEdge(
                path, "unrelated-a", "unrelated-b", "UnrelatedEdge");
            var map = ReadMirrorMap(path);
            WriteManifest(path, repository.Value,
                new()
                {
                    [source.Id.Value] = map.Nodes[source.Id.Value],
                    [target.Id.Value] = map.Nodes[target.Id.Value]
                },
                new() { [edge.Id.Value] = unrelated.EdgeId });

            using (var store = Open(path))
            {
                await Traverse(store, repository, source.Id);
            }

            AssertUnrelatedEdgePreserved(path, unrelated);
            AssertCanonicalMirror(
                path, repository,
                [source.Id.Value, target.Id.Value],
                [edge.Id.Value]);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Fact]
    public async Task UnrelatedEdgeIncidentToMirrorNode_IsRefusedAndPreserved()
    {
        var path = TemporaryDatabasePath();
        var repository = new CodeRepositoryId("repo:incident-edge");
        var mirrorNode = Node("node:mirror");
        try
        {
            using (var store = Open(path))
            {
                await PublishAsync(store, repository, "run:one", [mirrorNode]);
            }

            var incident = CreateIncidentUnrelatedEdge(
                path, "incident", ReadMirrorMap(path).Nodes[mirrorNode.Id.Value]);

            InvalidDataException? refusal = null;
            try
            {
                using var store = Open(path);
                await Traverse(store, repository, mirrorNode.Id);
            }
            catch (InvalidDataException exception)
            {
                refusal = exception;
            }

            Assert.NotNull(refusal);
            Assert.Contains("ownership", refusal!.Message, StringComparison.OrdinalIgnoreCase);
            AssertUnrelatedEdgePreserved(path, incident);

            // Remove the ambiguous relationship and prove a subsequent reopen
            // rebuilds the mirror without deleting anything unrelated.
            DeleteUnrelatedEdge(path, incident);
            using (var store = Open(path))
            {
                await Traverse(store, repository, mirrorNode.Id);
            }

            AssertCanonicalMirror(path, repository, [mirrorNode.Id.Value], []);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Fact]
    public async Task CrossRepositoryNativeReference_DoesNotDeleteOtherRepository()
    {
        var path = TemporaryDatabasePath();
        var first = new CodeRepositoryId("repo:first");
        var second = new CodeRepositoryId("repo:second");
        var shared = new CodeGraphNode(
            new CodeNodeId("node:shared"),
            CodeNodeKinds.Type,
            "Shared",
            "Example.Shared",
            new CodeSymbolId("symbol:shared"));
        var secondOnly = new CodeGraphNode(
            new CodeNodeId("node:second"),
            CodeNodeKinds.Type,
            "Second",
            "Example.Second",
            new CodeSymbolId("symbol:second"));
        try
        {
            using (var store = Open(path))
            {
                await PublishAsync(store, first, "run:one", [shared]);
                await PublishAsync(store, second, "run:one", [shared, secondOnly]);
            }

            var map = ReadMirrorMap(path);
            var secondNative = map.Nodes[secondOnly.Id.Value];
            WriteManifest(path, first.Value,
                new() { [shared.Id.Value] = secondNative },
                []);

            using (var store = Open(path))
            {
                await Traverse(store, first, shared.Id);
                await Traverse(store, second, shared.Id);
            }

            var after = ReadMirrorMap(path);
            Assert.Equal(2, after.ManifestRepositories.Count);
            AssertRepositoryManifest(path, first, ["node:shared"], []);
            AssertRepositoryManifest(path, second, ["node:second", "node:shared"], []);
            AssertAuthoritativeRowsUnchanged(
                path, ReadAuthoritativeRows(path));
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Fact]
    public async Task MalformedOrIncompleteManifests_RebuildWithoutAuthoritativeLoss()
    {
        var path = TemporaryDatabasePath();
        var first = new CodeRepositoryId("repo:malformed");
        var second = new CodeRepositoryId("repo:incomplete");
        try
        {
            using (var store = Open(path))
            {
                await PublishAsync(store, first, "run:one", [Node("node:first")]);
                await PublishAsync(store, second, "run:one", [Node("node:second")]);
            }

            var authoritative = ReadAuthoritativeRows(path);
            CorruptManifestPayload(path, first.Value, "not-json");
            // Structurally valid but incomplete: one node is omitted.
            DeleteManifestRows(path, second.Value);

            using (var store = Open(path))
            {
                await Traverse(store, first, new CodeNodeId("node:first"));
                await Traverse(store, second, new CodeNodeId("node:second"));
            }

            AssertAuthoritativeRowsUnchanged(path, authoritative);
            AssertRepositoryManifest(path, first, ["node:first"], []);
            AssertRepositoryManifest(path, second, ["node:second"], []);
            Assert.Equal(2, ReadMirrorMap(path).Nodes.Count);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Fact]
    public async Task InconsistentManifestEdgeWithoutEndpointNodes_DoesNotLeaveDanglingEdge()
    {
        var path = TemporaryDatabasePath();
        var repository = new CodeRepositoryId("repo:inconsistent");
        var source = Node("node:source");
        var target = Node("node:target");
        var edge = Edge("edge:calls", source, target);
        try
        {
            using (var store = Open(path))
            {
                await PublishAsync(store, repository, "run:one",
                    [source, target], [edge]);
            }

            var map = ReadMirrorMap(path);
            // Manifest names the edge but omits both endpoint nodes.
            WriteManifest(path, repository.Value,
                new(),
                new() { [edge.Id.Value] = map.Edges[edge.Id.Value] });

            using (var store = Open(path))
            {
                await Traverse(store, repository, source.Id);
            }

            AssertCanonicalMirror(
                path, repository,
                [source.Id.Value, target.Id.Value],
                [edge.Id.Value]);
            AssertNoDanglingMirrorEdges(path);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Fact]
    public async Task UnrelatedSentinelObjects_KeepIdentityPropertiesAndRelationships()
    {
        var path = TemporaryDatabasePath();
        var repository = new CodeRepositoryId("repo:sentinels");
        try
        {
            using (var store = Open(path))
            {
                await PublishAsync(store, repository, "run:one", [Node("node:a")]);
            }

            var sentinels = CreateSentinelGraph(path);
            using (var store = Open(path))
            {
                await Traverse(store, repository, new CodeNodeId("node:a"));
            }

            AssertSentinelGraphPreserved(path, sentinels);
            AssertCanonicalMirror(path, repository, ["node:a"], []);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    // ---------------------------------------------------------------- helpers

    private sealed record UnrelatedNode(ulong NativeId, string Label, string Note, long Number);
    private sealed record UnrelatedEdge(
        ulong NodeId, ulong SourceId, ulong TargetId, ulong EdgeId, string Type,
        string NodeNote, string EdgeNote);
    private sealed record SentinelGraph(
        UnrelatedNode First, UnrelatedNode Second, UnrelatedEdge Edge);

    private sealed record AuthoritativeRow(
        string Label, ulong NativeId, string Key, string Payload);

    private static UnrelatedNode CreateUnrelatedNode(
        string path, string note, long number)
    {
        using var database = LatticeDatabase.Open(
            path, new LatticeDatabaseOptions { Create = false });
        using var transaction = database.BeginWriteTransaction();
        var node = transaction.CreateNode(UnrelatedLabel);
        transaction.SetProperty(node, "note", LatticeValue.From(note));
        transaction.SetProperty(node, "number", LatticeValue.From(number));
        transaction.Commit();
        return new UnrelatedNode(node.Value, UnrelatedLabel, note, number);
    }

    private static UnrelatedEdge CreateIncidentUnrelatedEdge(
        string path, string note, ulong mirrorNativeId)
    {
        using var database = LatticeDatabase.Open(
            path, new LatticeDatabaseOptions { Create = false });
        using var transaction = database.BeginWriteTransaction();
        var unrelated = transaction.CreateNode(UnrelatedLabel);
        transaction.SetProperty(unrelated, "note", LatticeValue.From(note));
        var edge = transaction.CreateEdge(
            unrelated, new LatticeNodeId(mirrorNativeId), "UnrelatedIncident");
        transaction.SetEdgeProperty(edge, "note", LatticeValue.From(note));
        transaction.Commit();
        return new UnrelatedEdge(
            unrelated.Value, unrelated.Value, mirrorNativeId,
            edge.Value, "UnrelatedIncident", note, note);
    }

    private static UnrelatedEdge CreateUnrelatedEdge(
        string path, string firstNote, string secondNote, string type)
    {
        using var database = LatticeDatabase.Open(
            path, new LatticeDatabaseOptions { Create = false });
        using var transaction = database.BeginWriteTransaction();
        var first = transaction.CreateNode(UnrelatedLabel);
        transaction.SetProperty(first, "note", LatticeValue.From(firstNote));
        var second = transaction.CreateNode(UnrelatedLabel);
        transaction.SetProperty(second, "note", LatticeValue.From(secondNote));
        var edge = transaction.CreateEdge(first, second, type);
        transaction.SetEdgeProperty(edge, "note", LatticeValue.From("keep-edge"));
        transaction.Commit();
        return new UnrelatedEdge(
            first.Value, first.Value, second.Value, edge.Value, type,
            firstNote, "keep-edge");
    }

    private static SentinelGraph CreateSentinelGraph(string path)
    {
        using var database = LatticeDatabase.Open(
            path, new LatticeDatabaseOptions { Create = false });
        using var transaction = database.BeginWriteTransaction();
        var first = transaction.CreateNode(UnrelatedLabel);
        transaction.SetProperty(first, "note", LatticeValue.From("sentinel-one"));
        transaction.SetProperty(first, "number", LatticeValue.From(7L));
        transaction.SetProperty(
            first, "tags", LatticeValue.From(
                new LatticeValue[] { LatticeValue.From("a"), LatticeValue.From("b") }));
        var second = transaction.CreateNode(UnrelatedLabel);
        transaction.SetProperty(second, "note", LatticeValue.From("sentinel-two"));
        transaction.SetProperty(second, "number", LatticeValue.From(9L));
        var edge = transaction.CreateEdge(first, second, "SentinelLinks");
        transaction.SetEdgeProperty(edge, "note", LatticeValue.From("sentinel-edge"));
        transaction.Commit();
        return new SentinelGraph(
            new UnrelatedNode(first.Value, UnrelatedLabel, "sentinel-one", 7),
            new UnrelatedNode(second.Value, UnrelatedLabel, "sentinel-two", 9),
            new UnrelatedEdge(
                first.Value, first.Value, second.Value, edge.Value,
                "SentinelLinks", "sentinel-one", "sentinel-edge"));
    }

    private static void DeleteUnrelatedEdge(string path, UnrelatedEdge edge)
    {
        using var database = LatticeDatabase.Open(
            path, new LatticeDatabaseOptions { Create = false });
        using var transaction = database.BeginWriteTransaction();
        transaction.DeleteEdge(
            new LatticeNodeId(edge.SourceId),
            new LatticeNodeId(edge.TargetId),
            edge.Type);
        transaction.Commit();
    }

    private static void AssertUnrelatedNodePreserved(
        string path, UnrelatedNode unrelated)
    {
        using var database = LatticeDatabase.Open(
            path, new LatticeDatabaseOptions { Create = false });
        using var transaction = database.BeginReadTransaction();
        var node = new LatticeNodeId(unrelated.NativeId);
        Assert.True(transaction.NodeExists(node));
        Assert.True(transaction.TryGetProperty(node, "note", out var noteValue));
        Assert.Equal(unrelated.Note, noteValue.AsString());
        Assert.True(transaction.TryGetProperty(node, "number", out var numberValue));
        Assert.Equal(unrelated.Number, numberValue.AsInt64());
        transaction.Commit();
    }

    private static void AssertUnrelatedEdgePreserved(
        string path, UnrelatedEdge edge)
    {
        using var database = LatticeDatabase.Open(
            path, new LatticeDatabaseOptions { Create = false });
        using var transaction = database.BeginReadTransaction();
        Assert.True(transaction.NodeExists(new LatticeNodeId(edge.NodeId)));
        Assert.True(transaction.TryGetProperty(
            new LatticeNodeId(edge.NodeId), "note", out var note));
        Assert.Equal(edge.NodeNote, note.AsString());
        Assert.Contains(
            transaction.GetOutgoingEdges(new LatticeNodeId(edge.NodeId)),
            candidate => candidate.Id.Value == edge.EdgeId &&
                candidate.Target.Value == edge.TargetId &&
                candidate.Type == edge.Type);
        transaction.Commit();
    }

    private static void AssertSentinelGraphPreserved(
        string path, SentinelGraph sentinels)
    {
        using var database = LatticeDatabase.Open(
            path, new LatticeDatabaseOptions { Create = false });
        using var transaction = database.BeginReadTransaction();
        foreach (var sentinel in new[] { sentinels.First, sentinels.Second })
        {
            var node = new LatticeNodeId(sentinel.NativeId);
            Assert.True(transaction.NodeExists(node));
            Assert.True(transaction.TryGetProperty(node, "note", out var note));
            Assert.Equal(sentinel.Note, note.AsString());
            Assert.True(transaction.TryGetProperty(node, "number", out var number));
            Assert.Equal(sentinel.Number, number.AsInt64());
        }

        Assert.True(transaction.TryGetProperty(
            new LatticeNodeId(sentinels.First.NativeId), "tags", out var tags));
        Assert.Equal(["a", "b"], tags.AsList().Select(value => value.AsString()).ToArray());
        Assert.Contains(
            transaction.GetOutgoingEdges(
                new LatticeNodeId(sentinels.First.NativeId)),
            edge => edge.Id.Value == sentinels.Edge.EdgeId &&
                edge.Target.Value == sentinels.Second.NativeId &&
                edge.Type == sentinels.Edge.Type);
        Assert.True(transaction.TryGetEdgeProperty(
            new LatticeEdgeId(sentinels.Edge.EdgeId), "note", out var edgeNote));
        Assert.Equal(sentinels.Edge.EdgeNote, edgeNote.AsString());
        transaction.Commit();
    }

    private static List<AuthoritativeRow> ReadAuthoritativeRows(string path)
    {
        var rows = new List<AuthoritativeRow>();
        using var database = LatticeDatabase.Open(
            path, new LatticeDatabaseOptions { Create = false });
        using var transaction = database.BeginReadTransaction();
        foreach (var label in new[]
                 {
                     "HetuRepository", "HetuRun", "HetuUnit", "HetuIndexState",
                     "AndiritOccurrenceUnit", "AndiritOccurrenceMeta"
                 })
        {
            foreach (var node in transaction.GetNodesByLabel(label))
            {
                Assert.True(transaction.TryGetProperty(node, "key", out var key));
                Assert.True(transaction.TryGetProperty(node, "payload", out var payload));
                rows.Add(new AuthoritativeRow(
                    label, node.Value, key.AsString(), payload.AsString()));
            }
        }
        transaction.Commit();
        return rows.OrderBy(row => row.Label, StringComparer.Ordinal)
            .ThenBy(row => row.Key, StringComparer.Ordinal)
            .ToList();
    }

    private static void AssertAuthoritativeRowsUnchanged(
        string path, IReadOnlyList<AuthoritativeRow> before)
    {
        var after = ReadAuthoritativeRows(path);
        Assert.Equal(before.Count, after.Count);
        foreach (var (expected, actual) in before.Zip(after))
        {
            Assert.Equal(expected, actual);
        }
    }

    private static void AssertNoDanglingMirrorEdges(string path)
    {
        using var database = LatticeDatabase.Open(
            path, new LatticeDatabaseOptions { Create = false });
        using var transaction = database.BeginReadTransaction();
        var nodes = transaction.GetNodesByLabel(NodeLabel)
            .Select(node => node.Value).ToHashSet();
        foreach (var node in transaction.GetNodesByLabel(NodeLabel))
        {
            foreach (var edge in transaction.GetOutgoingEdges(node))
            {
                Assert.Contains(edge.Source.Value, nodes);
                Assert.Contains(edge.Target.Value, nodes);
            }
        }
        transaction.Commit();
    }

    private sealed record MirrorMap(
        Dictionary<string, ulong> Nodes,
        Dictionary<string, ulong> Edges,
        List<string> ManifestRepositories);

    private static MirrorMap ReadMirrorMap(string path)
    {
        using var database = LatticeDatabase.Open(
            path, new LatticeDatabaseOptions { Create = false });
        using var transaction = database.BeginReadTransaction();
        var nodes = new Dictionary<string, ulong>(StringComparer.Ordinal);
        var edges = new Dictionary<string, ulong>(StringComparer.Ordinal);
        foreach (var node in transaction.GetNodesByLabel(NodeLabel))
        {
            if (!transaction.TryGetProperty(node, JsonProperty, out var payload))
                continue;
            using var document = JsonDocument.Parse(payload.AsString());
            nodes[document.RootElement
                .GetProperty("Id").GetProperty("Value").GetString()!] = node.Value;
            foreach (var edge in transaction.GetOutgoingEdges(node))
            {
                if (!transaction.TryGetEdgeProperty(edge.Id, JsonProperty, out var edgePayload))
                    continue;
                using var edgeDocument = JsonDocument.Parse(edgePayload.AsString());
                edges[edgeDocument.RootElement
                    .GetProperty("Id").GetProperty("Value").GetString()!] = edge.Id.Value;
            }
        }

        var manifests = new List<string>();
        foreach (var node in transaction.GetNodesByLabel(MirrorLabel))
        {
            if (transaction.TryGetProperty(node, "key", out var key))
                manifests.Add(key.AsString());
        }
        transaction.Commit();
        return new MirrorMap(nodes, edges, manifests);
    }

    private static void WriteManifest(
        string path,
        string repositoryId,
        Dictionary<string, ulong> nodes,
        Dictionary<string, ulong> edges)
    {
        var json = JsonSerializer.Serialize(new ManifestDto(nodes, edges));
        var stored = Convert.ToBase64String(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(json)));
        using var database = LatticeDatabase.Open(
            path, new LatticeDatabaseOptions { Create = false });
        using var transaction = database.BeginWriteTransaction();
        var manifest = transaction.GetNodesByLabel(MirrorLabel)
            .Single(node =>
            {
                return transaction.TryGetProperty(node, "key", out var key) &&
                    key.AsString() == repositoryId;
            });
        transaction.SetProperty(manifest, "payload", LatticeValue.From(stored));
        transaction.Commit();
    }

    private static void CorruptManifestPayload(
        string path, string repositoryId, string value)
    {
        var stored = Convert.ToBase64String(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value)));
        using var database = LatticeDatabase.Open(
            path, new LatticeDatabaseOptions { Create = false });
        using var transaction = database.BeginWriteTransaction();
        var manifest = transaction.GetNodesByLabel(MirrorLabel)
            .Single(node =>
            {
                return transaction.TryGetProperty(node, "key", out var key) &&
                    key.AsString() == repositoryId;
            });
        transaction.SetProperty(manifest, "payload", LatticeValue.From(stored));
        transaction.Commit();
    }

    private static void DeleteManifestRows(string path, string repositoryId)
    {
        using var database = LatticeDatabase.Open(
            path, new LatticeDatabaseOptions { Create = false });
        using var transaction = database.BeginWriteTransaction();
        var manifest = transaction.GetNodesByLabel(MirrorLabel)
            .Single(node =>
            {
                return transaction.TryGetProperty(node, "key", out var key) &&
                    key.AsString() == repositoryId;
            });
        transaction.DeleteNode(manifest);
        transaction.Commit();
    }

    private static void AssertCanonicalMirror(
        string path,
        CodeRepositoryId repository,
        string[] expectedNodes,
        string[] expectedEdges)
    {
        AssertRepositoryManifest(path, repository, expectedNodes, expectedEdges);
        var map = ReadMirrorMap(path);
        Assert.Equal(
            expectedNodes.OrderBy(id => id, StringComparer.Ordinal).ToArray(),
            map.Nodes.Keys.OrderBy(id => id, StringComparer.Ordinal).ToArray());
        Assert.Equal(
            expectedEdges.OrderBy(id => id, StringComparer.Ordinal).ToArray(),
            map.Edges.Keys.OrderBy(id => id, StringComparer.Ordinal).ToArray());
    }

    private static void AssertRepositoryManifest(
        string path,
        CodeRepositoryId repository,
        string[] expectedNodes,
        string[] expectedEdges)
    {
        var (nodes, edges) = ReadRepositoryManifest(path, repository.Value);
        Assert.Equal(
            expectedNodes.OrderBy(id => id, StringComparer.Ordinal).ToArray(),
            nodes.Keys.OrderBy(id => id, StringComparer.Ordinal).ToArray());
        Assert.Equal(
            expectedEdges.OrderBy(id => id, StringComparer.Ordinal).ToArray(),
            edges.Keys.OrderBy(id => id, StringComparer.Ordinal).ToArray());
        foreach (var (logicalId, nativeId) in nodes)
        {
            Assert.Equal(logicalId, ReadNativeNodeLogicalId(path, nativeId));
        }
        foreach (var (logicalId, nativeId) in edges)
        {
            Assert.Equal(logicalId, ReadNativeEdgeLogicalId(path, nativeId));
        }
    }

    private static (Dictionary<string, ulong> Nodes, Dictionary<string, ulong> Edges)
        ReadRepositoryManifest(string path, string repositoryId)
    {
        using var database = LatticeDatabase.Open(
            path, new LatticeDatabaseOptions { Create = false });
        using var transaction = database.BeginReadTransaction();
        var manifest = transaction.GetNodesByLabel(MirrorLabel)
            .Single(node =>
            {
                return transaction.TryGetProperty(node, "key", out var key) &&
                    key.AsString() == repositoryId;
            });
        Assert.True(transaction.TryGetProperty(manifest, "payload", out var payload));
        var json = JsonSerializer.Deserialize<string>(
            Convert.FromBase64String(payload.AsString()))!;
        var parsed = JsonSerializer.Deserialize<ManifestDto>(json)!;
        transaction.Commit();
        return (
            new Dictionary<string, ulong>(parsed.Nodes, StringComparer.Ordinal),
            new Dictionary<string, ulong>(parsed.Edges, StringComparer.Ordinal));
    }

    private static string ReadNativeNodeLogicalId(string path, ulong nativeId)
    {
        using var database = LatticeDatabase.Open(
            path, new LatticeDatabaseOptions { Create = false });
        using var transaction = database.BeginReadTransaction();
        Assert.True(transaction.TryGetProperty(
            new LatticeNodeId(nativeId), JsonProperty, out var payload));
        using var document = JsonDocument.Parse(payload.AsString());
        var id = document.RootElement
            .GetProperty("Id").GetProperty("Value").GetString()!;
        transaction.Commit();
        return id;
    }

    private static string ReadNativeEdgeLogicalId(string path, ulong nativeId)
    {
        using var database = LatticeDatabase.Open(
            path, new LatticeDatabaseOptions { Create = false });
        using var transaction = database.BeginReadTransaction();
        Assert.True(transaction.TryGetEdgeProperty(
            new LatticeEdgeId(nativeId), JsonProperty, out var payload));
        using var document = JsonDocument.Parse(payload.AsString());
        var id = document.RootElement
            .GetProperty("Id").GetProperty("Value").GetString()!;
        transaction.Commit();
        return id;
    }

    private static LatticeCodeGraphStore Open(string path) =>
        new(path, new LatticeDbStoreOptions { UseNativeTraversal = true });

    private static async Task PublishAsync(
        LatticeCodeGraphStore store,
        CodeRepositoryId repository,
        string run,
        IReadOnlyList<CodeGraphNode> nodes,
        IReadOnlyList<CodeGraphEdge>? edges = null)
    {
        var runId = new CodeIndexRunId(run);
        await store.UpsertRepositoryAsync(new(repository));
        await store.StoreIndexRunAsync(
            new(repository, runId, Started, plugins: [Plugin]));
        await store.StageIndexUnitAsync(new(
            new CodeFactOrigin(repository, Plugin, "1.0.0", runId,
                new CodeIndexUnitId("unit:one")),
            nodes,
            edges: edges));
        await store.CompleteIndexRunAsync(
            new(repository, runId, Started, CodeIndexRunStatus.Completed,
                Started.AddSeconds(1), [Plugin]),
            new(repository, runId, []));
    }

    private static async Task<CodeGraphTraversalResult> Traverse(
        LatticeCodeGraphStore store,
        CodeRepositoryId repository,
        CodeNodeId start) =>
        await store.TraverseAsync(
            repository,
            new CodeGraphTraversalQuery(start, maxDepth: 5, maxNodes: 100, maxEdges: 100));

    private static CodeGraphNode Node(string id)
    {
        var name = id[(id.LastIndexOf(':') + 1)..];
        return new CodeGraphNode(
            new CodeNodeId(id),
            CodeNodeKinds.Type,
            name,
            "Example." + name,
            new CodeSymbolId("symbol:" + id));
    }

    private static CodeGraphEdge Edge(
        string id,
        CodeGraphNode source,
        CodeGraphNode target) =>
        new(
            new CodeEdgeId(id),
            source.Id,
            target.Id,
            CodeEdgeKinds.Calls,
            new CodeEvidence(EvidenceQuality.Resolved, Plugin.Value, "1.0.0"));

    private sealed record ManifestDto(
        Dictionary<string, ulong> Nodes,
        Dictionary<string, ulong> Edges);

    private static string TemporaryDatabasePath() =>
        Path.Combine(
            Path.GetTempPath(), $"andirit-s64-ownership-{Guid.NewGuid():N}.ltdb");

    private static void DeleteDatabase(string path)
    {
        foreach (var suffix in new[] { "", "-wal", "-journal", ".wal" })
        {
            if (File.Exists(path + suffix))
                File.Delete(path + suffix);
        }
    }
}
