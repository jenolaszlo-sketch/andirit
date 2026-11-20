using System.Text.Json;
using LatticeDbSharp;
using Andirit.LatticeDb;
using Andirit.Testing;

namespace Andirit.LatticeDb.Tests;

public sealed class NativeGraphMirrorTests
{
    [Fact]
    public async Task NativeTraversalMatchesProjection()
    {
        var nativePath = TemporaryDatabasePath();
        var baselinePath = TemporaryDatabasePath();
        try
        {
            using var native = new LatticeCodeGraphStore(
                nativePath, new LatticeDbStoreOptions { UseNativeTraversal = true });
            using var baseline = new LatticeCodeGraphStore(baselinePath);
            var repositoryId = new CodeRepositoryId("repo:parity");
            var pluginId = new CodePluginId("plugin:parity");
            var runId = new CodeIndexRunId("run:parity");
            var started = DateTimeOffset.UtcNow;
            var nodes = new[]
            {
                Node("a", "Example.A"),
                Node("b", "Example.B"),
                Node("c", "Example.C"),
                Node("d", "Example.D")
            };
            var edges = new[]
            {
                Edge("ab", nodes[0].Id, nodes[1].Id, CodeEdgeKinds.Calls),
                Edge("ac", nodes[0].Id, nodes[2].Id, CodeEdgeKinds.References),
                Edge("bd", nodes[1].Id, nodes[3].Id, CodeEdgeKinds.Calls),
                Edge("ca", nodes[2].Id, nodes[0].Id, CodeEdgeKinds.Implements)
            };
            foreach (var store in new LatticeCodeGraphStore[] { native, baseline })
            {
                await store.UpsertRepositoryAsync(new(repositoryId));
                await store.StoreIndexRunAsync(new(repositoryId, runId, started, plugins: [pluginId]));
                await store.StageIndexUnitAsync(new(
                    new CodeFactOrigin(repositoryId, pluginId, "1.0.0", runId, new("unit:parity")),
                    nodes,
                    edges: edges));
                await store.CompleteIndexRunAsync(
                    new(repositoryId, runId, started, CodeIndexRunStatus.Completed, started.AddSeconds(1), [pluginId]),
                    new(repositoryId, runId, []));
            }

            var queries = new[]
            {
                new CodeGraphTraversalQuery(nodes[0].Id, maxDepth: 5, maxNodes: 10, maxEdges: 10),
                new CodeGraphTraversalQuery(
                    nodes[0].Id, CodeGraphDirection.Incoming, maxDepth: 5, maxNodes: 10, maxEdges: 10),
                new CodeGraphTraversalQuery(
                    nodes[0].Id, CodeGraphDirection.Both, maxDepth: 5, maxNodes: 10, maxEdges: 10),
                new CodeGraphTraversalQuery(
                    nodes[0].Id, CodeGraphDirection.Outgoing, [CodeEdgeKinds.Calls],
                    maxDepth: 5, maxNodes: 10, maxEdges: 10),
                new CodeGraphTraversalQuery(nodes[0].Id, maxDepth: 1, maxNodes: 2, maxEdges: 1),
                new CodeGraphTraversalQuery(nodes[3].Id, maxDepth: 5, maxNodes: 10, maxEdges: 10)
            };
            foreach (var query in queries)
            {
                var actual = await native.TraverseAsync(repositoryId, query);
                var expected = await baseline.TraverseAsync(repositoryId, query);
                AssertTraversalEqual(expected, actual);
            }
        }
        finally
        {
            DeleteDatabase(nativePath);
            DeleteDatabase(baselinePath);
        }
    }

    [Fact]
    public async Task StagedFactsStayOutOfNativeTraversal()
    {
        var path = TemporaryDatabasePath();
        try
        {
            using var store = new LatticeCodeGraphStore(
                path, new LatticeDbStoreOptions { UseNativeTraversal = true });
            var repositoryId = new CodeRepositoryId("repo:staged");
            var pluginId = new CodePluginId("plugin:staged");
            var runId = new CodeIndexRunId("run:v1");
            var started = DateTimeOffset.UtcNow;
            var node = Node("a", "Example.A");
            await store.UpsertRepositoryAsync(new(repositoryId));
            await store.StoreIndexRunAsync(new(repositoryId, runId, started, plugins: [pluginId]));
            await store.StageIndexUnitAsync(new(
                new CodeFactOrigin(repositoryId, pluginId, "1.0.0", runId, new("unit:one")),
                [node]));
            await store.CompleteIndexRunAsync(
                new(repositoryId, runId, started, CodeIndexRunStatus.Completed, started.AddSeconds(1), [pluginId]),
                new(repositoryId, runId, []));

            var nextRun = new CodeIndexRunId("run:v2");
            await store.StoreIndexRunAsync(new(repositoryId, nextRun, started.AddSeconds(2), plugins: [pluginId]));
            var staged = Node("b", "Example.B");
            await store.StageIndexUnitAsync(new(
                new CodeFactOrigin(repositoryId, pluginId, "1.0.0", nextRun, new("unit:two")),
                [staged],
                edges: [Edge("ab", node.Id, staged.Id, CodeEdgeKinds.Calls)]));
            var during = await store.TraverseAsync(
                repositoryId,
                new CodeGraphTraversalQuery(node.Id, maxDepth: 5, maxNodes: 10, maxEdges: 10));
            Assert.DoesNotContain(during.Nodes, candidate => candidate.Id == staged.Id);

            await store.CompleteIndexRunAsync(
                new(repositoryId, nextRun, started.AddSeconds(2), CodeIndexRunStatus.Completed, started.AddSeconds(3), [pluginId]),
                new(repositoryId, nextRun, []));
            var after = await store.TraverseAsync(
                repositoryId,
                new CodeGraphTraversalQuery(node.Id, maxDepth: 5, maxNodes: 10, maxEdges: 10));
            Assert.Contains(after.Nodes, candidate => candidate.Id == staged.Id);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Fact]
    public async Task EvidenceFilteredTraversalFallsBackToProjection()
    {
        var path = TemporaryDatabasePath();
        try
        {
            using var store = new LatticeCodeGraphStore(
                path, new LatticeDbStoreOptions { UseNativeTraversal = true });
            var repositoryId = new CodeRepositoryId("repo:evidence");
            var pluginId = new CodePluginId("plugin:evidence");
            var runId = new CodeIndexRunId("run:v1");
            var started = DateTimeOffset.UtcNow;
            var source = Node("a", "Example.A");
            var target = Node("b", "Example.B");
            await store.UpsertRepositoryAsync(new(repositoryId));
            await store.StoreIndexRunAsync(new(repositoryId, runId, started, plugins: [pluginId]));
            await store.StageIndexUnitAsync(new(
                new CodeFactOrigin(repositoryId, pluginId, "1.0.0", runId, new("unit:one")),
                [source, target],
                edges: [Edge("ab", source.Id, target.Id, CodeEdgeKinds.Calls)]));
            await store.CompleteIndexRunAsync(
                new(repositoryId, runId, started, CodeIndexRunStatus.Completed, started.AddSeconds(1), [pluginId]),
                new(repositoryId, runId, []));

            var query = new CodeGraphTraversalQuery(
                source.Id,
                CodeGraphDirection.Outgoing,
                [CodeEdgeKinds.Calls],
                [EvidenceQuality.Resolved],
                maxDepth: 5,
                maxNodes: 10,
                maxEdges: 10);
            var result = await store.TraverseAsync(repositoryId, query);
            Assert.Contains(result.Edges, edge => edge.TargetId == target.Id);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Fact]
    public async Task NativeMirrorSurvivesReopen()
    {
        var path = TemporaryDatabasePath();
        var repositoryId = new CodeRepositoryId("repo:reopen");
        var pluginId = new CodePluginId("plugin:reopen");
        var runId = new CodeIndexRunId("run:v1");
        var started = DateTimeOffset.UtcNow;
        var source = Node("a", "Example.A");
        var target = Node("b", "Example.B");
        try
        {
            using (var store = new LatticeCodeGraphStore(
                       path, new LatticeDbStoreOptions { UseNativeTraversal = true }))
            {
                await store.UpsertRepositoryAsync(new(repositoryId));
                await store.StoreIndexRunAsync(new(repositoryId, runId, started, plugins: [pluginId]));
                await store.StageIndexUnitAsync(new(
                    new CodeFactOrigin(repositoryId, pluginId, "1.0.0", runId, new("unit:one")),
                    [source, target],
                    edges: [Edge("ab", source.Id, target.Id, CodeEdgeKinds.Calls)]));
                await store.CompleteIndexRunAsync(
                    new(repositoryId, runId, started, CodeIndexRunStatus.Completed, started.AddSeconds(1), [pluginId]),
                    new(repositoryId, runId, []));
            }

            using var reopened = new LatticeCodeGraphStore(
                path, new LatticeDbStoreOptions { UseNativeTraversal = true });
            var result = await reopened.TraverseAsync(
                repositoryId,
                new CodeGraphTraversalQuery(source.Id, maxDepth: 5, maxNodes: 10, maxEdges: 10));
            Assert.Contains(result.Nodes, node => node.Id == target.Id);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Fact]
    public async Task NativeMirrorRebuildsAfterPublicationWhileTraversalIsDisabled()
    {
        var path = TemporaryDatabasePath();
        var repositoryId = new CodeRepositoryId("repo:mirror-transition");
        var pluginId = new CodePluginId("plugin:mirror-transition");
        var firstRun = new CodeIndexRunId("run:before");
        var secondRun = new CodeIndexRunId("run:after");
        var started = DateTimeOffset.UtcNow;
        var before = Node("before", "Example.Before");
        var after = Node("after", "Example.After");
        try
        {
            using (var enabled = new LatticeCodeGraphStore(
                       path, new LatticeDbStoreOptions { UseNativeTraversal = true }))
            {
                await enabled.UpsertRepositoryAsync(new(repositoryId));
                await enabled.StoreIndexRunAsync(new(repositoryId, firstRun, started, plugins: [pluginId]));
                await enabled.StageIndexUnitAsync(new(
                    new CodeFactOrigin(repositoryId, pluginId, "1.0.0", firstRun, new("unit:mirror")),
                    [before]));
                await enabled.CompleteIndexRunAsync(
                    new(repositoryId, firstRun, started, CodeIndexRunStatus.Completed, started.AddSeconds(1), [pluginId]),
                    new(repositoryId, firstRun, []));
            }

            using (var disabled = new LatticeCodeGraphStore(path))
            {
                await disabled.StoreIndexRunAsync(new(repositoryId, secondRun, started.AddSeconds(2), plugins: [pluginId]));
                await disabled.StageIndexUnitAsync(new(
                    new CodeFactOrigin(repositoryId, pluginId, "1.0.0", secondRun, new("unit:mirror")),
                    [after]));
                await disabled.CompleteIndexRunAsync(
                    new(repositoryId, secondRun, started.AddSeconds(2), CodeIndexRunStatus.Completed, started.AddSeconds(3), [pluginId]),
                    new(repositoryId, secondRun, []));
            }

            using var reopened = new LatticeCodeGraphStore(
                path, new LatticeDbStoreOptions { UseNativeTraversal = true });
            var result = await reopened.TraverseAsync(
                repositoryId,
                new CodeGraphTraversalQuery(after.Id, maxDepth: 1, maxNodes: 10, maxEdges: 10));
            Assert.Contains(result.Nodes, node => node.Id == after.Id);
            Assert.DoesNotContain(result.Nodes, node => node.Id == before.Id);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Fact]
    public void MirrorManifestTreatsNullPayloadAsAnEmptyDerivedManifest()
    {
        var manifest = NativeGraphMirror.ReadManifest("null");

        Assert.Empty(manifest.Nodes);
        Assert.Empty(manifest.Edges);
    }

    [Fact]
    public void RebuildRejectsAnEdgeWhoseSourceIsNotMirrored()
    {
        var path = TemporaryDatabasePath();
        try
        {
            using var database = LatticeDatabase.Open(
                path,
                new LatticeDatabaseOptions { Create = true });
            using var transaction = database.BeginWriteTransaction();
            var source = Node("missing-source", "Example.MissingSource");
            var target = Node("target", "Example.Target");
            var unit = new CodeIndexUnitReplacement(
                Origin("unit:missing-source"),
                [target],
                edges: [Edge("edge:missing-source", source.Id, target.Id, CodeEdgeKinds.Calls)]);

            var exception = Assert.Throws<InvalidDataException>(() =>
                NativeGraphMirror.Rebuild(transaction, [unit], null));

            Assert.Contains("missing mirrored node", exception.Message, StringComparison.Ordinal);
            transaction.Rollback();
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Fact]
    public void RebuildRejectsAnEdgeWhoseTargetIsNotMirrored()
    {
        var path = TemporaryDatabasePath();
        try
        {
            using var database = LatticeDatabase.Open(
                path,
                new LatticeDatabaseOptions { Create = true });
            using var transaction = database.BeginWriteTransaction();
            var source = Node("source", "Example.Source");
            var target = Node("missing-target", "Example.MissingTarget");
            var unit = new CodeIndexUnitReplacement(
                Origin("unit:missing-target"),
                [source],
                edges: [Edge("edge:missing-target", source.Id, target.Id, CodeEdgeKinds.Calls)]);

            var exception = Assert.Throws<InvalidDataException>(() =>
                NativeGraphMirror.Rebuild(transaction, [unit], null));

            Assert.Contains("missing mirrored node", exception.Message, StringComparison.Ordinal);
            transaction.Rollback();
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Fact]
    public void RebuildRejectsNullTransactionAndUnits()
    {
        var path = TemporaryDatabasePath();
        try
        {
            using var database = LatticeDatabase.Open(
                path,
                new LatticeDatabaseOptions { Create = true });
            using var transaction = database.BeginWriteTransaction();

            Assert.Throws<ArgumentNullException>(() =>
                NativeGraphMirror.Rebuild(null!, [], null));
            Assert.Throws<ArgumentNullException>(() =>
                NativeGraphMirror.Rebuild(transaction, null!, null));
            transaction.Rollback();
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Fact]
    public void TraverseRejectsEvidenceFilteredQueries()
    {
        var path = TemporaryDatabasePath();
        try
        {
            using var database = LatticeDatabase.Open(
                path,
                new LatticeDatabaseOptions { Create = true });
            using var transaction = database.BeginReadTransaction();
            var mirror = new NativeGraphMirror(new(
                new Dictionary<string, ulong>(StringComparer.Ordinal),
                new Dictionary<string, ulong>(StringComparer.Ordinal)));
            var query = new CodeGraphTraversalQuery(
                new CodeNodeId("node:missing"),
                evidenceQualities: [EvidenceQuality.Resolved]);

            var exception = Assert.Throws<ArgumentException>(() =>
                mirror.Traverse(transaction, query, CancellationToken.None));

            Assert.Equal("query", exception.ParamName);
            transaction.Commit();
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Fact]
    public void TraverseReturnsAnEmptyResultForAnUnknownStartNode()
    {
        var path = TemporaryDatabasePath();
        try
        {
            using var database = LatticeDatabase.Open(
                path,
                new LatticeDatabaseOptions { Create = true });
            using var transaction = database.BeginReadTransaction();
            var mirror = new NativeGraphMirror(new(
                new Dictionary<string, ulong>(StringComparer.Ordinal),
                new Dictionary<string, ulong>(StringComparer.Ordinal)));

            var result = mirror.Traverse(
                transaction,
                new CodeGraphTraversalQuery(new CodeNodeId("node:missing")),
                CancellationToken.None);

            Assert.Empty(result.Nodes);
            Assert.Empty(result.Edges);
            Assert.False(result.Truncated);
            transaction.Commit();
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Fact]
    public void TraverseRejectsANativeEdgeMissingFromTheManifest()
    {
        var path = TemporaryDatabasePath();
        try
        {
            var (database, mirror, node, _, nativeNodeId, _) = CreateNativeMirror(
                path,
                includeEdgeMapping: false);
            using (database)
            using (var transaction = database.BeginWriteTransaction())
            {
                var exception = Assert.Throws<InvalidDataException>(() =>
                    mirror.Traverse(
                        transaction,
                        new CodeGraphTraversalQuery(node.Id),
                        CancellationToken.None));

                Assert.Contains("unmapped fact", exception.Message, StringComparison.Ordinal);
                transaction.Rollback();
            }

            Assert.NotEqual(0UL, nativeNodeId);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Fact]
    public void TraverseRejectsANativeTargetMissingFromTheManifest()
    {
        var path = TemporaryDatabasePath();
        try
        {
            var (database, mirror, node, _, _, _) = CreateNativeMirror(
                path,
                includeTargetMapping: false);
            using (database)
            using (var transaction = database.BeginWriteTransaction())
            {
                var exception = Assert.Throws<InvalidDataException>(() =>
                    mirror.Traverse(
                        transaction,
                        new CodeGraphTraversalQuery(node.Id),
                        CancellationToken.None));

                Assert.Contains("unmapped fact", exception.Message, StringComparison.Ordinal);
                transaction.Rollback();
            }
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Fact]
    public void TraverseRejectsANullNativeNodePayload()
    {
        var path = TemporaryDatabasePath();
        try
        {
            var (database, mirror, node, _, nativeNodeId, _) = CreateNativeMirror(path);
            using (database)
            using (var transaction = database.BeginWriteTransaction())
            {
                transaction.SetProperty(
                    new LatticeNodeId(nativeNodeId),
                    "hetu_json",
                    LatticeValue.From("null"));

                var exception = Assert.Throws<InvalidDataException>(() =>
                    mirror.Traverse(
                        transaction,
                        new CodeGraphTraversalQuery(node.Id),
                        CancellationToken.None));

                Assert.Contains("missing node", exception.Message, StringComparison.Ordinal);
                transaction.Rollback();
            }
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Fact]
    public void TraverseRejectsANullNativeEdgePayload()
    {
        var path = TemporaryDatabasePath();
        try
        {
            var (database, mirror, node, _, _, nativeEdgeId) = CreateNativeMirror(path);
            using (database)
            using (var transaction = database.BeginWriteTransaction())
            {
                transaction.SetEdgeProperty(
                    new LatticeEdgeId(nativeEdgeId),
                    "hetu_json",
                    LatticeValue.From("null"));

                var exception = Assert.Throws<InvalidDataException>(() =>
                    mirror.Traverse(
                        transaction,
                        new CodeGraphTraversalQuery(node.Id),
                        CancellationToken.None));

                Assert.Contains("missing edge", exception.Message, StringComparison.Ordinal);
                transaction.Rollback();
            }
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    private static (LatticeDatabase Database, NativeGraphMirror Mirror, CodeGraphNode Node,
        CodeGraphEdge Edge, ulong NativeNodeId, ulong NativeEdgeId) CreateNativeMirror(
            string path,
            bool includeTargetMapping = true,
            bool includeEdgeMapping = true)
    {
        var database = LatticeDatabase.Open(
            path,
            new LatticeDatabaseOptions { Create = true });
        var node = Node("a", "Example.A");
        var target = Node("b", "Example.B");
        var edge = Edge("ab", node.Id, target.Id, CodeEdgeKinds.Calls);
        ulong nativeNodeId;
        ulong nativeTargetId;
        ulong nativeEdgeId;
        using (var transaction = database.BeginWriteTransaction())
        {
            var nativeNode = transaction.CreateNode(NativeGraphMirror.NodeLabel);
            nativeNodeId = nativeNode.Value;
            transaction.SetProperty(
                nativeNode,
                "hetu_json",
                LatticeValue.From(JsonSerializer.Serialize(node)));
            var nativeTarget = transaction.CreateNode(NativeGraphMirror.NodeLabel);
            nativeTargetId = nativeTarget.Value;
            transaction.SetProperty(
                nativeTarget,
                "hetu_json",
                LatticeValue.From(JsonSerializer.Serialize(target)));
            var nativeEdge = transaction.CreateEdge(
                new LatticeNodeId(nativeNodeId),
                new LatticeNodeId(nativeTargetId),
                edge.Kind.Value);
            nativeEdgeId = nativeEdge.Value;
            transaction.SetEdgeProperty(
                nativeEdge,
                "hetu_json",
                LatticeValue.From(JsonSerializer.Serialize(edge)));
            transaction.Commit();
        }

        var nodes = new Dictionary<string, ulong>(StringComparer.Ordinal)
        {
            [node.Id.Value] = nativeNodeId
        };
        if (includeTargetMapping)
            nodes[target.Id.Value] = nativeTargetId;
        var edges = new Dictionary<string, ulong>(StringComparer.Ordinal);
        if (includeEdgeMapping)
            edges[edge.Id.Value] = nativeEdgeId;
        return (database, new NativeGraphMirror(new(nodes, edges)), node, edge, nativeNodeId, nativeEdgeId);
    }

    private static CodeFactOrigin Origin(string unit) =>
        new(
            new CodeRepositoryId("repo:mirror-contract"),
            new CodePluginId("plugin:mirror-contract"),
            "1.0.0",
            new CodeIndexRunId("run:mirror-contract"),
            new CodeIndexUnitId(unit));

    private static void AssertTraversalEqual(CodeGraphTraversalResult expected, CodeGraphTraversalResult actual)
    {
        Assert.Equal(expected.Truncated, actual.Truncated);
        Assert.Equal(expected.TruncationReason, actual.TruncationReason);
        Assert.Equal(expected.DepthReached, actual.DepthReached);
        Assert.Equal(expected.NodesExamined, actual.NodesExamined);
        Assert.Equal(expected.EdgesExamined, actual.EdgesExamined);
        Assert.Equal(expected.OmittedFrontierCount, actual.OmittedFrontierCount);
        Assert.Equal(
            expected.Nodes.Select(node => node.Id.Value).ToArray(),
            actual.Nodes.Select(node => node.Id.Value).ToArray());
        Assert.Equal(
            expected.Edges.Select(edge => edge.Id.Value).ToArray(),
            actual.Edges.Select(edge => edge.Id.Value).ToArray());
        foreach (var pair in expected.Nodes.Zip(actual.Nodes))
        {
            Assert.Equal(pair.First.Kind, pair.Second.Kind);
            Assert.Equal(pair.First.Name, pair.Second.Name);
            Assert.Equal(pair.First.QualifiedName, pair.Second.QualifiedName);
            Assert.Equal(pair.First.SymbolId, pair.Second.SymbolId);
        }

        foreach (var pair in expected.Edges.Zip(actual.Edges))
        {
            Assert.Equal(pair.First.Kind, pair.Second.Kind);
            Assert.Equal(pair.First.SourceId, pair.Second.SourceId);
            Assert.Equal(pair.First.TargetId, pair.Second.TargetId);
            Assert.Equal(pair.First.Evidence.Quality, pair.Second.Evidence.Quality);
        }
    }

    private static CodeGraphNode Node(string id, string qualifiedName) =>
        new(
            new CodeNodeId($"node:{id}"),
            CodeNodeKinds.Type,
            qualifiedName[(qualifiedName.LastIndexOf('.') + 1)..],
            qualifiedName,
            new CodeSymbolId($"symbol:{id}"));

    private static CodeGraphEdge Edge(
        string id,
        CodeNodeId source,
        CodeNodeId target,
        CodeEdgeKind kind) =>
        new(
            new CodeEdgeId($"edge:{id}"),
            source,
            target,
            kind,
            new CodeEvidence(EvidenceQuality.Resolved, "mirror"));

    private static string TemporaryDatabasePath() =>
        Path.Combine(Path.GetTempPath(), $"hetu-mirror-{Guid.NewGuid():N}.ltdb");

    private static void DeleteDatabase(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}

