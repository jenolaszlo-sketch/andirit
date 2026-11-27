using System.Text.Json;
using LatticeDbSharp;
using static Andirit.DurableCommandLog;

namespace Andirit;

/// <summary>
/// An experimental native-graph mirror of published facts for
/// <see cref="LatticeCodeGraphStore"/>.
///
/// The mirror is derived state, never the source of truth: the command log
/// stays authoritative for staging, publication, and recovery, while the
/// mirror serves <c>TraverseAsync</c> reads natively. It is rebuilt from
/// published units on every publication and reopen, so a corrupt or stale
/// mirror is always recoverable by rebuilding. Provenance-bound queries and
/// every other read stay on the materialized projection.
/// </summary>
internal sealed class NativeGraphMirror
{
    // Historical native storage labels and property keys are protocol identities.
    internal const string NodeLabel = "HetuGraphNode";
    internal const string MirrorLabel = "HetuMirror";
    private const string JsonProperty = "hetu_json";

    private readonly MirrorManifest manifest;
    private readonly Dictionary<ulong, string> nodeByNative;
    private readonly Dictionary<ulong, string> edgeByNative;

    internal NativeGraphMirror(MirrorManifest manifest)
    {
        this.manifest = manifest;
        nodeByNative = manifest.Nodes.ToDictionary(
            pair => pair.Value, pair => pair.Key);
        edgeByNative = manifest.Edges.ToDictionary(
            pair => pair.Value, pair => pair.Key);
    }

    internal MirrorManifest Manifest => manifest;

    internal static string WriteManifest(MirrorManifest manifest) =>
        JsonSerializer.Serialize(manifest, SerializerOptions);

    internal static MirrorManifest ReadManifest(string payload) =>
        JsonSerializer.Deserialize<MirrorManifest>(payload, SerializerOptions)
        ?? new(new Dictionary<string, ulong>(), new Dictionary<string, ulong>());

    /// <summary>
    /// Replaces the repository's mirror with the canonical materialized facts,
    /// deleting every mirror object this repository owned first. Runs inside the
    /// caller's transaction.
    /// </summary>
    /// <remarks>
    /// The caller supplies the authoritative projection's node and edge
    /// dictionaries, so one physical object exists per canonical logical
    /// identity. A manifest that omits a native row can only come from a
    /// defective historical rebuild or from a disabled publication that removed
    /// the manifest first; <see cref="SweepOrphans"/> removes such untracked
    /// rows. <paramref name="retainedNodeIds"/> and
    /// <paramref name="retainedEdgeIds"/> carry the native identities still
    /// referenced by every other repository's manifest in the same database, so
    /// the sweep cannot cross repository boundaries.
    /// </remarks>
    internal static NativeGraphMirror Rebuild(
        LatticeTransaction transaction,
        IReadOnlyDictionary<string, CodeGraphNode> nodes,
        IReadOnlyDictionary<string, CodeGraphEdge> edges,
        MirrorManifest? existing,
        IReadOnlySet<ulong>? retainedNodeIds = null,
        IReadOnlySet<ulong>? retainedEdgeIds = null)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(edges);

        DeleteOwned(transaction, existing, retainedNodeIds, retainedEdgeIds);
        SweepOrphans(transaction, retainedNodeIds, retainedEdgeIds);

        var nodeManifest = new Dictionary<string, ulong>(StringComparer.Ordinal);
        foreach (var node in nodes.Values.OrderBy(node => node.Id.Value, StringComparer.Ordinal))
        {
            var id = transaction.CreateNode(NodeLabel);
            transaction.SetProperty(
                id,
                JsonProperty,
                LatticeValue.From(JsonSerializer.Serialize(node, SerializerOptions)));
            nodeManifest.Add(node.Id.Value, id.Value);
        }

        var edgeManifest = new Dictionary<string, ulong>(StringComparer.Ordinal);
        foreach (var edge in edges.Values.OrderBy(edge => edge.Id.Value, StringComparer.Ordinal))
        {
            if (!nodeManifest.TryGetValue(edge.SourceId.Value, out var source) ||
                !nodeManifest.TryGetValue(edge.TargetId.Value, out var target))
            {
                throw new InvalidDataException(
                    $"Published edge '{edge.Id.Value}' references a missing mirrored node.");
            }

            var id = transaction.CreateEdge(
                new LatticeNodeId(source),
                new LatticeNodeId(target),
                edge.Kind.Value);
            transaction.SetEdgeProperty(
                id,
                JsonProperty,
                LatticeValue.From(JsonSerializer.Serialize(edge, SerializerOptions)));
            edgeManifest.Add(edge.Id.Value, id.Value);
        }

        return new NativeGraphMirror(new MirrorManifest(nodeManifest, edgeManifest));
    }

    /// <summary>
    /// Removes every protocol-marked mirror row that no manifest in the database
    /// references. Runs inside the caller's transaction.
    /// </summary>
    /// <remarks>
    /// Mirror rows are derived state, so an untracked row is never part of any
    /// repository's served mirror; it can be reconstructed from the authoritative
    /// command log. Rows referenced by another repository's manifest are retained
    /// to preserve repository isolation.
    /// </remarks>
    internal static void SweepOrphans(
        LatticeTransaction transaction,
        IReadOnlySet<ulong>? retainedNodeIds,
        IReadOnlySet<ulong>? retainedEdgeIds)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        var mirrorNodes = transaction.GetNodesByLabel(NodeLabel)
            .Where(node => transaction.TryGetProperty(node, JsonProperty, out _))
            .ToArray();

        var mirrorEdges = new Dictionary<ulong, LatticeEdgeInfo>();
        foreach (var node in mirrorNodes)
        {
            foreach (var edge in transaction.GetOutgoingEdges(node))
            {
                if (transaction.TryGetEdgeProperty(edge.Id, JsonProperty, out _))
                {
                    mirrorEdges[edge.Id.Value] = edge;
                }
            }
        }

        foreach (var edge in mirrorEdges.Values)
        {
            if (retainedEdgeIds?.Contains(edge.Id.Value) is true)
            {
                continue;
            }

            transaction.DeleteEdge(edge.Source, edge.Target, edge.Type);
        }

        foreach (var node in mirrorNodes)
        {
            if (retainedNodeIds?.Contains(node.Value) is true)
            {
                continue;
            }

            transaction.DeleteNode(node);
        }
    }

    /// <summary>
    /// Removes the physical objects a manifest owns without creating a
    /// replacement. Runs inside the caller's transaction.
    /// </summary>
    internal static void Drop(
        LatticeTransaction transaction,
        MirrorManifest? manifest,
        IReadOnlySet<ulong>? retainedNodeIds,
        IReadOnlySet<ulong>? retainedEdgeIds) =>
        DeleteOwned(transaction, manifest, retainedNodeIds, retainedEdgeIds);

    private static void DeleteOwned(
        LatticeTransaction transaction,
        MirrorManifest? existing,
        IReadOnlySet<ulong>? retainedNodeIds,
        IReadOnlySet<ulong>? retainedEdgeIds)
    {
        if (existing is null)
        {
            return;
        }

        var deletedEdges = new HashSet<ulong>();
        foreach (var nativeId in existing.Nodes.Values)
        {
            if (retainedNodeIds?.Contains(nativeId) is true)
            {
                continue;
            }

            var node = new LatticeNodeId(nativeId);
            foreach (var edge in transaction.GetOutgoingEdges(node)
                .Concat(transaction.GetIncomingEdges(node)))
            {
                if (retainedEdgeIds?.Contains(edge.Id.Value) is true)
                {
                    continue;
                }

                if (deletedEdges.Add(edge.Id.Value))
                {
                    transaction.DeleteEdge(edge.Source, edge.Target, edge.Type);
                }
            }

            transaction.DeleteNode(node);
        }
    }

    /// <summary>
    /// Runs a bounded traversal against native adjacency with the same
    /// bounds, ordering, and diagnostics as the materialized projection.
    /// The caller must route evidence-filtered queries to the projection:
    /// evidence lives in edge facts, not in traversal results.
    /// </summary>
    internal CodeGraphTraversalResult Traverse(
        LatticeTransaction transaction,
        CodeGraphTraversalQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        ArgumentNullException.ThrowIfNull(query);
        if (query.EvidenceQualities.Count != 0)
        {
            throw new ArgumentException(
                "Native traversal cannot filter by evidence quality.",
                nameof(query));
        }

        var selectedKinds = query.EdgeKinds
            .Select(kind => kind.Value)
            .ToHashSet(StringComparer.Ordinal);
        if (!manifest.Nodes.TryGetValue(query.StartNodeId.Value, out var startNative))
        {
            return new([], [], false);
        }

        var visited = new HashSet<string>(StringComparer.Ordinal)
        {
            query.StartNodeId.Value
        };
        var nodeIds = new List<string> { query.StartNodeId.Value };
        var edgeIds = new List<string>();
        var seenEdges = new HashSet<string>(StringComparer.Ordinal);
        var queue = new Queue<(string HetuId, ulong NativeId, int Depth)>();
        queue.Enqueue((query.StartNodeId.Value, startNative, 0));
        var truncated = false;
        var truncationReason = CodeGraphTruncationReason.None;
        var depthReached = 0;
        var nodesExamined = 0;
        var edgesExamined = 0;
        var omittedFrontierCount = 0;

        while (queue.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (hetuId, nativeId, depth) = queue.Dequeue();
            nodesExamined++;
            depthReached = Math.Max(depthReached, depth);
            if (depth >= query.MaxDepth)
            {
                var omitted = Adjacent(transaction, nativeId, query.Direction)
                    .DistinctBy(edge => edge.NativeId)
                    .Count(edge =>
                        selectedKinds.Count == 0 || selectedKinds.Contains(edge.Kind));
                if (omitted > 0)
                {
                    truncated = true;
                    truncationReason |= CodeGraphTruncationReason.MaxDepth;
                    omittedFrontierCount += omitted;
                }

                continue;
            }

            var candidates = Adjacent(transaction, nativeId, query.Direction)
                .Where(edge => selectedKinds.Count == 0 ||
                    selectedKinds.Contains(edge.Kind))
                .DistinctBy(edge => edge.NativeId)
                .OrderBy(edge => edge.Kind, StringComparer.Ordinal)
                .ThenBy(edge => edge.SourceId, StringComparer.Ordinal)
                .ThenBy(edge => edge.TargetId, StringComparer.Ordinal)
                .ThenBy(edge => edge.EdgeId, StringComparer.Ordinal);

            foreach (var edge in candidates)
            {
                edgesExamined++;
                var adjacent = edge.SourceId == hetuId
                    ? edge.TargetId
                    : edge.SourceId;
                var adjacentNative = edge.SourceId == hetuId
                    ? edge.TargetNative
                    : edge.SourceNative;
                var isNewNode = !visited.Contains(adjacent);
                if (isNewNode && nodeIds.Count >= query.MaxNodes)
                {
                    truncated = true;
                    truncationReason |= CodeGraphTruncationReason.MaxNodes;
                    omittedFrontierCount++;
                    continue;
                }

                if (seenEdges.Add(edge.EdgeId))
                {
                    if (edgeIds.Count >= query.MaxEdges)
                    {
                        truncated = true;
                        truncationReason |= CodeGraphTruncationReason.MaxEdges;
                        break;
                    }

                    edgeIds.Add(edge.EdgeId);
                }

                if (!isNewNode)
                    continue;

                visited.Add(adjacent);
                nodeIds.Add(adjacent);
                queue.Enqueue((adjacent, adjacentNative, depth + 1));
            }

            if (truncated && edgeIds.Count >= query.MaxEdges)
                break;
        }

        return new CodeGraphTraversalResult(
            nodeIds.Select(id => ReadNode(transaction, id)).ToArray(),
            edgeIds.Select(id => ReadEdge(transaction, id)).ToArray(),
            truncated,
            truncationReason,
            depthReached,
            nodesExamined,
            edgesExamined,
            omittedFrontierCount);
    }

    private IReadOnlyList<NativeAdjacency> Adjacent(
        LatticeTransaction transaction,
        ulong nativeId,
        CodeGraphDirection direction)
    {
        var node = new LatticeNodeId(nativeId);
        IEnumerable<LatticeEdgeInfo> edges = direction switch
        {
            CodeGraphDirection.Outgoing => transaction.GetOutgoingEdges(node),
            CodeGraphDirection.Incoming => transaction.GetIncomingEdges(node),
            CodeGraphDirection.Both => transaction.GetOutgoingEdges(node)
                .Concat(transaction.GetIncomingEdges(node)),
            _ => throw new ArgumentOutOfRangeException(nameof(direction))
        };
        return edges.Select(edge => ResolveEdge(edge)).ToArray();
    }

    private NativeAdjacency ResolveEdge(LatticeEdgeInfo edge)
    {
        if (!edgeByNative.TryGetValue(edge.Id.Value, out var hetuEdge) ||
            !nodeByNative.TryGetValue(edge.Source.Value, out var sourceHetu) ||
            !nodeByNative.TryGetValue(edge.Target.Value, out var targetHetu))
        {
            throw new InvalidDataException(
                "The native mirror references an unmapped fact.");
        }

        return new NativeAdjacency(
            edge.Id.Value,
            edge.Type,
            edge.Source.Value,
            edge.Target.Value,
            hetuEdge,
            sourceHetu,
            targetHetu);
    }

    private CodeGraphNode ReadNode(LatticeTransaction transaction, string hetuId)
    {
        if (!manifest.Nodes.TryGetValue(hetuId, out var nativeId) ||
            !transaction.TryGetProperty(
                new LatticeNodeId(nativeId),
                JsonProperty,
                out var value) ||
            JsonSerializer.Deserialize<CodeGraphNode>(value.AsString(), SerializerOptions) is not { } node)
        {
            throw new InvalidDataException(
                $"The native mirror is missing node '{hetuId}'.");
        }

        return node;
    }

    private CodeGraphEdge ReadEdge(LatticeTransaction transaction, string hetuId)
    {
        if (!manifest.Edges.TryGetValue(hetuId, out var nativeId) ||
            !transaction.TryGetEdgeProperty(
                new LatticeEdgeId(nativeId),
                JsonProperty,
                out var value) ||
            JsonSerializer.Deserialize<CodeGraphEdge>(value.AsString(), SerializerOptions) is not { } edge)
        {
            throw new InvalidDataException(
                $"The native mirror is missing edge '{hetuId}'.");
        }

        return edge;
    }

    private sealed record NativeAdjacency(
        ulong NativeId,
        string Kind,
        ulong SourceNative,
        ulong TargetNative,
        string EdgeId,
        string SourceId,
        string TargetId);

    internal sealed record MirrorManifest(
        Dictionary<string, ulong> Nodes,
        Dictionary<string, ulong> Edges);
}

