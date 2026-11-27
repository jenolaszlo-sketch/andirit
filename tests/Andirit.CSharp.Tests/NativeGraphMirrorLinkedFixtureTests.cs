using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Andirit;
using Andirit.LatticeDb;
using LatticeDbSharp;

namespace Andirit.CSharp.Tests;

/// <summary>
/// S64-MIRROR-REMED-01 end-to-end regression for the original real linked-file
/// fixture: two genuine project units contribute the same logical file node.
/// The test reads the native mirror rows directly and asserts one physical node
/// per logical identity, complete manifest correspondence and stable
/// cardinality across enabled, disabled and re-enabled publications.
/// </summary>
public sealed class NativeGraphMirrorLinkedFixtureTests
{
    private const string NodeLabel = "HetuGraphNode";
    private const string MirrorLabel = "HetuMirror";
    private const string JsonProperty = "hetu_json";
    private const string LinkedPath = "Shared/Linked.cs";

    private static readonly CodeRepositoryId Repository = new("repo:s64-linked-fixture");
    private static readonly DateTimeOffset Started =
        new(2026, 11, 27, 0, 0, 0, TimeSpan.Zero);

    private static readonly SortedDictionary<string, string> Files =
        new(StringComparer.Ordinal)
        {
            [LinkedPath] =
                "namespace Example; public static class Helper { public static void Go() {} }",
            ["src/ALib/ALib.csproj"] = Project("ALib"),
            ["src/BLib/BLib.csproj"] = Project("BLib"),
            ["src/ALib/Anchor.cs"] = "namespace ALib; public class Anchor {}",
            ["src/BLib/Anchor.cs"] = "namespace BLib; public class Anchor {}",
            ["src/App/App.csproj"] = """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
                  <ItemGroup><ProjectReference Include="../BLib/BLib.csproj" /></ItemGroup>
                </Project>
                """,
            ["src/App/Program.cs"] =
                "namespace App; public class Program { public void Run() => Example.Helper.Go(); }"
        };

    [Fact]
    public async Task LinkedFixture_HasOnePhysicalNodePerLogicalIdentity_AcrossLifecycle()
    {
        var path = TemporaryDatabasePath();
        try
        {
            string linkedId;
            string[] nativeTraversal;
            using (var store = Open(path))
            {
                await PublishAsync(store, "run:enabled");
                var units = await store.GetPublishedUnitsAsync(Repository);
                var linked = units
                    .SelectMany(unit => unit.Nodes
                        .Where(node => node.QualifiedName == LinkedPath))
                    .ToArray();
                Assert.Equal(2, linked.Length);
                linkedId = linked.Select(node => node.Id.Value)
                    .Distinct(StringComparer.Ordinal).Single();
                nativeTraversal = await Traverse(store, linkedId);
                Assert.Equal(2, nativeTraversal.Length);
            }

            var enabled = ReadInventory(path);
            Assert.Equal(20, enabled.Nodes.Select(node => node.Id)
                .Distinct(StringComparer.Ordinal).Count());
            Assert.Equal(20, enabled.Nodes.Count);
            Assert.Single(enabled.Nodes, node => node.Id == linkedId);
            Assert.Single(enabled.Manifests);
            AssertCorrespondence(enabled);
            Assert.True(enabled.Edges.Count > 0);

            // The projection served while mirror traversal is disabled must
            // return the same ordered logical edges.
            using (var projection = new LatticeCodeGraphStore(path))
            {
                Assert.Equal(nativeTraversal, await Traverse(projection, linkedId));
                await PublishAsync(projection, "run:disabled");
            }

            var disabled = ReadInventory(path);
            Assert.Empty(disabled.Nodes);
            Assert.Empty(disabled.Edges);
            Assert.Empty(disabled.Manifests);

            using (var reenabled = Open(path))
            {
                Assert.Equal(nativeTraversal, await Traverse(reenabled, linkedId));
            }

            var reopened = ReadInventory(path);
            Assert.Equal(enabled.Nodes.Count, reopened.Nodes.Count);
            Assert.Equal(enabled.Edges.Count, reopened.Edges.Count);
            Assert.Single(reopened.Nodes, node => node.Id == linkedId);
            AssertCorrespondence(reopened);

            using (var second = Open(path))
            {
                Assert.Equal(nativeTraversal, await Traverse(second, linkedId));
            }

            var secondReopen = ReadInventory(path);
            Assert.Equal(enabled.Nodes.Count, secondReopen.Nodes.Count);
            Assert.Equal(enabled.Edges.Count, secondReopen.Edges.Count);
            AssertCorrespondence(secondReopen);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    // ---------------------------------------------------------------- helpers

    private static LatticeCodeGraphStore Open(string path) =>
        new(path, new LatticeDbStoreOptions { UseNativeTraversal = true });

    private static async Task PublishAsync(LatticeCodeGraphStore store, string run)
    {
        var plugin = new CSharpCodeGraphPlugin();
        var runId = new CodeIndexRunId(run);
        await store.UpsertRepositoryAsync(new(Repository));
        await store.StoreIndexRunAsync(
            new(Repository, runId, Started, plugins: [plugin.Id]));
        var context = new CodeGraphPluginContext(
            Repository,
            null,
            runId,
            Files.Select(pair =>
            {
                var bytes = Encoding.UTF8.GetBytes(pair.Value);
                return new CodeGraphSource(
                    pair.Key,
                    "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)),
                    _ => ValueTask.FromResult<Stream>(
                        new MemoryStream(bytes, writable: false)));
            }).ToArray());
        await using (var sink = new CodeGraphIngestionSink(store))
        await using (var session = await plugin.CreateSessionAsync(context))
        {
            await session.ExtractAsync(sink);
            await sink.CompleteAsync();
        }

        await store.CompleteIndexRunAsync(
            new(Repository, runId, Started, CodeIndexRunStatus.Completed,
                Started.AddSeconds(1), [plugin.Id]),
            new(Repository, runId, []));
    }

    private static async Task<string[]> Traverse(
        LatticeCodeGraphStore store,
        string root) =>
        (await store.TraverseAsync(
            Repository,
            new CodeGraphTraversalQuery(
                new CodeNodeId(root),
                CodeGraphDirection.Incoming,
                maxDepth: 1,
                maxNodes: 100,
                maxEdges: 100))).Edges
        .Select(edge => edge.Id.Value)
        .ToArray();

    private sealed record PhysicalNode(ulong NativeId, string Id);
    private sealed record PhysicalEdge(
        ulong NativeId,
        string Id,
        string SourceId,
        string TargetId,
        ulong SourceNative,
        ulong TargetNative);
    private sealed record ManifestView(
        IReadOnlyDictionary<string, ulong> Nodes,
        IReadOnlyDictionary<string, ulong> Edges);
    private sealed record Inventory(
        IReadOnlyList<PhysicalNode> Nodes,
        IReadOnlyList<PhysicalEdge> Edges,
        IReadOnlyList<ManifestView> Manifests);

    private static Inventory ReadInventory(string path)
    {
        using var database = LatticeDatabase.Open(
            path, new LatticeDatabaseOptions { Create = false });
        using var transaction = database.BeginReadTransaction();
        var mirrorNodes = transaction.GetNodesByLabel(NodeLabel);
        var nodes = new List<PhysicalNode>();
        foreach (var node in mirrorNodes)
        {
            if (!transaction.TryGetProperty(node, JsonProperty, out var payload))
                continue;
            using var document = JsonDocument.Parse(payload.AsString());
            nodes.Add(new PhysicalNode(
                node.Value,
                document.RootElement
                    .GetProperty("Id").GetProperty("Value").GetString()!));
        }

        var edges = new Dictionary<ulong, PhysicalEdge>();
        foreach (var node in mirrorNodes)
        {
            foreach (var edge in transaction.GetOutgoingEdges(node))
            {
                if (!transaction.TryGetEdgeProperty(edge.Id, JsonProperty, out var payload))
                    continue;
                using var document = JsonDocument.Parse(payload.AsString());
                var root = document.RootElement;
                edges[edge.Id.Value] = new PhysicalEdge(
                    edge.Id.Value,
                    root.GetProperty("Id").GetProperty("Value").GetString()!,
                    root.GetProperty("SourceId").GetProperty("Value").GetString()!,
                    root.GetProperty("TargetId").GetProperty("Value").GetString()!,
                    edge.Source.Value,
                    edge.Target.Value);
            }
        }

        var manifests = new List<ManifestView>();
        foreach (var node in transaction.GetNodesByLabel(MirrorLabel))
        {
            if (!transaction.TryGetProperty(node, "payload", out var payload))
                continue;
            var json = JsonSerializer.Deserialize<string>(
                Convert.FromBase64String(payload.AsString()))!;
            var manifest = JsonSerializer.Deserialize<ManifestDto>(json)!;
            manifests.Add(new ManifestView(
                new Dictionary<string, ulong>(manifest.Nodes, StringComparer.Ordinal),
                new Dictionary<string, ulong>(manifest.Edges, StringComparer.Ordinal)));
        }

        transaction.Commit();
        return new Inventory(nodes, edges.Values.ToArray(), manifests);
    }

    private static void AssertCorrespondence(Inventory inventory)
    {
        var manifest = Assert.Single(inventory.Manifests);
        Assert.Equal(
            inventory.Nodes.Select(node => node.Id)
                .OrderBy(id => id, StringComparer.Ordinal).ToArray(),
            manifest.Nodes.Keys.OrderBy(id => id, StringComparer.Ordinal).ToArray());
        Assert.Equal(
            inventory.Edges.Select(edge => edge.Id)
                .OrderBy(id => id, StringComparer.Ordinal).ToArray(),
            manifest.Edges.Keys.OrderBy(id => id, StringComparer.Ordinal).ToArray());

        var byNative = inventory.Nodes.ToDictionary(node => node.NativeId);
        foreach (var (logicalId, nativeId) in manifest.Nodes)
        {
            Assert.True(byNative.TryGetValue(nativeId, out var node));
            Assert.Equal(logicalId, node.Id);
        }

        var edgesByNative = inventory.Edges.ToDictionary(edge => edge.NativeId);
        foreach (var (logicalId, nativeId) in manifest.Edges)
        {
            Assert.True(edgesByNative.TryGetValue(nativeId, out var edge));
            Assert.Equal(logicalId, edge.Id);
            Assert.Contains(
                inventory.Nodes, node => node.NativeId == edge.SourceNative);
            Assert.Contains(
                inventory.Nodes, node => node.NativeId == edge.TargetNative);
        }

        Assert.Equal(inventory.Nodes.Count, manifest.Nodes.Count);
        Assert.Equal(inventory.Edges.Count, manifest.Edges.Count);
        Assert.Equal(
            inventory.Nodes.Count,
            inventory.Nodes.Select(node => node.NativeId).Distinct().Count());
        Assert.Equal(
            inventory.Edges.Count,
            inventory.Edges.Select(edge => edge.NativeId).Distinct().Count());
    }

    private sealed record ManifestDto(
        Dictionary<string, ulong> Nodes,
        Dictionary<string, ulong> Edges);

    private static string Project(string name) => $"""
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>net10.0</TargetFramework>
            <AssemblyName>{name}</AssemblyName>
          </PropertyGroup>
          <ItemGroup>
            <Compile Include="../../Shared/Linked.cs" />
          </ItemGroup>
        </Project>
        """;

    private static string TemporaryDatabasePath() =>
        Path.Combine(Path.GetTempPath(), $"andirit-s64-linked-{Guid.NewGuid():N}.ltdb");

    private static void DeleteDatabase(string path)
    {
        foreach (var suffix in new[] { "", "-wal", "-journal", ".wal" })
        {
            if (File.Exists(path + suffix))
                File.Delete(path + suffix);
        }
    }
}
